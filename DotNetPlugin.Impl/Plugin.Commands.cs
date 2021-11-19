namespace DotNetPlugin
{
    partial class Plugin
    {
        [Command(Tracer.TraceCallsCommand, DebugOnly = true)]
        public static bool TraceCalls(string[] args) => Tracer.Instance.Execute(args);
    }
}
