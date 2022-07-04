using System;
using Dotx64Dbg;

namespace DotNetPlugin
{
    partial class Plugin
    {
        // Works at any given time.
        [Command("Test1")]
        void MyCommand(string[] args)
        {
            Console.WriteLine($"Hello World: {args[0]}");
        }

        // Works only when the debugger is active.
        [Command("Test2", DebugOnly = true)]
        void MyCommand2(string[] args)
        {
            Console.WriteLine("Debugger active, lets go!");
        }

        [Command("Test3")]
        bool MyCommand3(string[] args)
        {
            Console.WriteLine("Oh no");
            return false; // Indicates failure.
        }

        [Command("SetStatusText")]
        void SetStatusBarText(string[] args)
        {
            UI.StatusBar.Text = args[1] ?? "";
        }

        [Command("Selection")]
        void PrintSelection(string[] args)
        {
            var sel = UI.Disassembly.GetSelection();
            if (sel == null)
            {
                Console.WriteLine("No selection");
            }
            Console.WriteLine($"Selection Start: {sel.Start:X}, End: {sel.End:X}");
        }

        [Command("CmdNoArgs")]
        void NoArgsCmd()
        {
            Console.WriteLine("Yup");
        }

        [Command("CmdNoArgs2")]
        bool NoArgsCmd2()
        {
            Console.WriteLine("Yup");
            return false;
        }
    }
}
