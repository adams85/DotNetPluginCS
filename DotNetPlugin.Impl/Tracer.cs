using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using DotNetPlugin.NativeBindings;
using DotNetPlugin.NativeBindings.Script;
using DotNetPlugin.NativeBindings.SDK;

namespace DotNetPlugin
{
    public class Tracer
    {
        public const string TraceCallsCommand = "tracecalls";

        public delegate void OnTraceDelegate(ref Plugins.PLUG_CB_TRACEEXECUTE info);

        public static readonly Tracer Instance = new Tracer();

        private OnTraceDelegate _onTrace;
        private XmlWriter _writer;
        private Stack<UIntPtr> _callStack;

        private Tracer()
        {
            _onTrace = OnTraceNoop;
        }

        private void BeginCall(UIntPtr addr)
        {
            UIntPtr srcModuleBase;
            if (Bridge.DbgGetModuleAt(addr, out var srcModule))
                srcModuleBase = Bridge.DbgModBaseFromName(srcModule);
            else
                (srcModule, srcModuleBase) = (null, UIntPtr.Zero);

            var destAddr = Bridge.DbgGetBranchDestination(addr);
            if (destAddr == UIntPtr.Zero)
                return;

            UIntPtr destModuleBase;
            if (Bridge.DbgGetModuleAt(destAddr, out var destModule))
                destModuleBase = Bridge.DbgModBaseFromName(destModule);
            else
                (destModule, destModuleBase) = (null, UIntPtr.Zero);

            if (!Bridge.DbgGetLabelAt(destAddr, Bridge.SEGMENTREG.SEG_DEFAULT, out var destLabel))
                destLabel = null;

            _writer.WriteStartElement("Call");

            _writer.WriteAttributeString("Module", destModule ?? "<unknown>");
            _writer.WriteAttributeString("RVA", (destModuleBase != UIntPtr.Zero ? (nuint)destAddr - destModuleBase : (nuint)destAddr).ToPtrString());
            if (destLabel != null)
                _writer.WriteAttributeString("Label", destLabel);

            _writer.WriteAttributeString("CallerModule", srcModule ?? "<unknown>");
            _writer.WriteAttributeString("CallerRVA", (srcModuleBase != UIntPtr.Zero ? (nuint)addr - srcModuleBase : (nuint)addr).ToPtrString());

            var stackFrameAddr = Register.GetCSP() - UIntPtr.Size;
            _callStack.Push(stackFrameAddr);

            _writer.Flush();
        }

        private void EndCall()
        {
            _callStack.Pop();

            _writer.WriteEndElement();

            _writer.Flush();
        }

        public bool Execute(string[] args)
        {
            try
            {
                var cip = Register.GetCIP();

                var instr = new Bridge.BASIC_INSTRUCTION_INFO();
                Bridge.DbgDisasmFastAt(cip, ref instr);

                if (!instr.call)
                {
                    Console.Error.WriteLine("Debugger is not paused at a call instruction!");
                    return false;
                }

                _callStack = new Stack<UIntPtr>();

                _writer =
                    args.Length > 1 ?
                    XmlWriter.Create(new StreamWriter(args[1], append: false), new XmlWriterSettings { Indent = true, CloseOutput = true }) :
                    XmlWriter.Create(Console.Out, new XmlWriterSettings { Indent = true, CloseOutput = false });

                BeginCall(cip);

                _onTrace = OnTraceImpl;

                Plugins._plugin_debugskipexceptions(true);

                if (!Bridge.DbgCmdExec($"TraceIntoConditional cip=={(cip + instr.size).ToPtrString()}"))
                {
                    Console.Error.WriteLine($"Tracing could not be started!");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error occurred in {nameof(Tracer)}.{nameof(Execute)}. Details: {ex}");
                return false;
            }
        }

        private void OnTraceImpl(ref Plugins.PLUG_CB_TRACEEXECUTE info)
        {
            while ((nuint)Register.GetCSP() > _callStack.Peek())
                EndCall();

            if (!info.stop && _callStack.Count > 0)
            {
                var instr = new Bridge.BASIC_INSTRUCTION_INFO();
                Bridge.DbgDisasmFastAt(info.cip, ref instr);

                if (instr.call)
                    BeginCall(info.cip);
            }
            else
            {
                _writer.Flush();
                _writer.Dispose();
                _writer = null;

                _callStack = null;

                _onTrace = OnTraceNoop;
            }
        }

        private void OnTraceNoop(ref Plugins.PLUG_CB_TRACEEXECUTE info) { }

        public OnTraceDelegate OnTrace => _onTrace;
    }
}
