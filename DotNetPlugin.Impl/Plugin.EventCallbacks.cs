
using DotNetPlugin.NativeBindings.SDK;

namespace DotNetPlugin
{
    partial class Plugin
    {
        [EventCallback(Plugins.CBTYPE.CB_TRACEEXECUTE)]
        public static void OnTraceExecute(ref Plugins.PLUG_CB_TRACEEXECUTE info) => Tracer.Instance.OnTrace(ref info);
    }
}
