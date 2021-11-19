using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using DotNetPlugin.NativeBindings;
using DotNetPlugin.NativeBindings.Script;
using DotNetPlugin.NativeBindings.SDK;
using DotNetPlugin.NativeBindings.Win32;
using Fclp;

namespace DotNetPlugin
{
    public class Tracer
    {
        public const string TraceCallsCommand = "tracecalls";
        public const string SetSwitchConditionCommand = "tracecalls_setsc";

        public delegate void OnTraceDelegate(ref Plugins.PLUG_CB_TRACEEXECUTE info);

        public static readonly Tracer Instance = new Tracer();

        private OnTraceDelegate _onTrace;

        private bool _lastSwitchCondition;
        private bool _lastStepInto, _stepInto;
        private Func<string, bool> _shouldStepInto;

        private XmlWriter _writer;
        private Stack<nuint> _callStack;

        private Tracer()
        {
            _onTrace = OnTraceNoop;
        }

        private void BeginCall(nuint addr, ref Bridge.BASIC_INSTRUCTION_INFO instr)
        {
            Thread.Sleep(1);

            var destAddr = Bridge.DbgGetBranchDestination(addr);
            if (destAddr == 0)
                return;

            nuint srcModuleBase;
            if (Bridge.DbgGetModuleAt(addr, out var srcModule))
                srcModuleBase = Bridge.DbgModBaseFromName(srcModule);
            else
                (srcModule, srcModuleBase) = (null, 0);

            nuint destModuleBase;
            if (Bridge.DbgGetModuleAt(destAddr, out var destModule))
                destModuleBase = Bridge.DbgModBaseFromName(destModule);
            else
                (destModule, destModuleBase) = (null, 0);

            if (!Bridge.DbgGetLabelAt(destAddr, Bridge.SEGMENTREG.SEG_DEFAULT, out var destLabel))
                destLabel = null;

            _writer.WriteStartElement("Call");

            _writer.WriteAttributeString("Module", destModule ?? "<unknown>");
            _writer.WriteAttributeString("RVA", (destModuleBase != 0 ? destAddr - destModuleBase : destAddr).ToPtrString());
            if (destLabel != null)
                _writer.WriteAttributeString("Label", destLabel);

            _writer.WriteAttributeString("CallerModule", srcModule ?? "<unknown>");
            _writer.WriteAttributeString("CallerRVA", (srcModuleBase != 0 ? addr - srcModuleBase : addr).ToPtrString());
            _writer.WriteAttributeString("CallInstr", instr.instruction);

            _writer.Flush();

            _stepInto = /*destModule != null && */_shouldStepInto(destModule);

            if (_stepInto)
                _callStack.Push(Register.GetCSP() - UIntPtr.Size); // save stack frame ptr
            else
                _writer.WriteEndElement();
        }

        internal bool SetSwitchCondition(string[] args)
        {
            if (_lastStepInto ^ _stepInto)
            {
                if (!_lastSwitchCondition)
                {
                    Bridge.DbgValToString("$traceswitchcondition", 1);
                    _lastSwitchCondition = true;
                }
            }
            else
            {
                if (_lastSwitchCondition)
                {
                    Bridge.DbgValToString("$traceswitchcondition", 0);
                    _lastSwitchCondition = false;
                }
            }

            _lastStepInto = _stepInto;

            return true;
        }

        private void EndCall()
        {
            _callStack.Pop();

            _writer.WriteEndElement();

            _writer.Flush();
        }

        private static Func<string, bool> BuildShouldStepInto(CommandArguments args)
        {
            var includedModules = (args.IncludedModules ?? Enumerable.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var excludedModules = (args.ExcludedModules ?? Enumerable.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!includedModules.Any() && !excludedModules.Any())
                return _ => true;

            if (includedModules.Any() && excludedModules.Any())
                return module => includedModules.Contains(module) || !excludedModules.Contains(module);

            return includedModules.Any() ? (module => includedModules.Contains(module)) : (module => !excludedModules.Contains(module));
        }

        public bool Execute(string[] args)
        {
            try
            {
                var splitArgs = CommandLineHelper.SplitArgs(args[0]).ToArray();

                var parser = new FluentCommandLineParser<CommandArguments>();

                parser.SkipFirstArg();

                parser.Setup(a => a.OutputFilePath).As('o', "output").WithDescription("Path to output file.");
                parser.Setup(a => a.IncludedModules).As('i', "include").WithDescription("Module(s) to include.");
                parser.Setup(a => a.ExcludedModules).As('e', "exclude").WithDescription("Module(s) to exclude.");
                parser.Setup(a => a.MaxCount).As('m', "max-steps").WithDescription("Maximum step count. Default is 100000.");

                parser.SetupHelp("?", "help")
                      .WithCustomFormatter(new CommandLineHelpFormatter("Usage:"))
                      .Callback(text => Console.WriteLine(text));

                var parserResult = parser.Parse(splitArgs);

                if (parserResult.HelpCalled)
                    return true;

                if (parserResult.HasErrors)
                {
                    parser.HelpOption.ShowHelp(parser.Options);
                    return false;
                }

                var cip = Register.GetCIP();

                var instr = new Bridge.BASIC_INSTRUCTION_INFO();
                Bridge.DbgDisasmFastAt(cip, ref instr);

                if (!instr.call)
                {
                    Console.Error.WriteLine("Debugger is not paused at a call instruction!");
                    return false;
                }

                _callStack = new Stack<nuint>();

                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                }

                _writer =
                    parser.Object.OutputFilePath != null ?
                    XmlWriter.Create(new StreamWriter(parser.Object.OutputFilePath, append: false), new XmlWriterSettings { Indent = true, CloseOutput = true }) :
                    XmlWriter.Create(Console.Out, new XmlWriterSettings { Indent = true, CloseOutput = false });

                _shouldStepInto = BuildShouldStepInto(parser.Object);

                _lastStepInto = _stepInto = true;
                _lastSwitchCondition = false;

                _onTrace = OnTraceImpl;

                BeginCall(cip, ref instr);

                Plugins._plugin_debugskipexceptions(true);

                if (!Bridge.DbgCmdExecDirect($"TraceSetCommand {SetSwitchConditionCommand}, 1") ||
                    !Bridge.DbgCmdExec($"TraceIntoConditional cip=={(cip + instr.size).ToPtrString()}, {parser.Object.MaxCount ?? 100000}"))
                {
                    Console.Error.WriteLine($"Tracing could not be started!");
                    _onTrace = OnTraceNoop;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error occurred in {nameof(Tracer)}.{nameof(Execute)}. Details: {ex}");
                _onTrace = OnTraceNoop;
                return false;
            }
        }

        private void OnTraceImpl(ref Plugins.PLUG_CB_TRACEEXECUTE info)
        {
            while (_callStack.Count > 0 && Register.GetCSP() > _callStack.Peek())
                EndCall();

            if (_callStack.Count > 0 && !info.stop)
            {
                var instr = new Bridge.BASIC_INSTRUCTION_INFO();
                Bridge.DbgDisasmFastAt(info.cip, ref instr);

                _stepInto = true;

                if (instr.call)
                    BeginCall(info.cip, ref instr);
            }
            else
            {
                _writer.Flush();
                _writer.Dispose();
                _writer = null;

                _callStack = null;

                _shouldStepInto = null;

                _onTrace = OnTraceNoop;
            }
        }

        private void OnTraceNoop(ref Plugins.PLUG_CB_TRACEEXECUTE info) { }

        public OnTraceDelegate OnTrace => _onTrace;

        // TODO: remove if unused
        public void OnDebugEvent(ref Plugins.PLUG_CB_DEBUGEVENT info)
        {
            if (_onTrace == OnTraceNoop)
                return;

            var debugEvent = info.DebugEvent.HasValue ? info.DebugEvent.Value : default;

            if (debugEvent.dwDebugEventCode == DebugEventType.OUTPUT_DEBUG_STRING_EVENT)
            {
                // https://maximumcrack.wordpress.com/2009/06/22/outputdebugstring-awesomeness/
                //var ret = Win32.ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, Win32.ContinueStatus.DBG_CONTINUE);

                // try to ignore exception
                //var ret = Bridge.DbgCmdExecDirect("skip");
            }
        }

        private sealed class CommandArguments
        {
            public string OutputFilePath { get; set; }
            public List<string> IncludedModules { get; set; }
            public List<string> ExcludedModules { get; set; }
            public int? MaxCount { get; }
        }
    }
}
