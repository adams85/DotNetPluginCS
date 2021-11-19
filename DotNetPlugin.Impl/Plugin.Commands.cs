namespace DotNetPlugin
{
    partial class Plugin
    {
        [Command(Tracer.TraceCallsCommand, DebugOnly = true)]
        public static bool TraceCalls(string[] args) => Tracer.Instance.Execute(args);

        [Command(Tracer.SetSwitchConditionCommand, DebugOnly = true)]
        public static bool TraceCallsSetSwitchCondition(string[] args) => Tracer.Instance.SetSwitchCondition(args);
    }
}
