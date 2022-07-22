﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetPlugin.NativeBindings;

namespace Dotx64Dbg
{
    /// <summary>
    /// Wrapper to simplify the scripting environment.
    /// </summary>
    public static partial class Scripting
    {
        public static void Print(string line)
        {
            PLogTextWriter.Default.WriteLine(line);
        }
        public static void Print(string fmt, params object[] args)
        {
            PLogTextWriter.Default.WriteLine(fmt, args);
        }

        public static void Sti()
        {
            Debugger.StepIn();
        }

        public static void Sti(int steps)
        {
            for (int i = 0; i < steps; i++)
                Sti();
        }

        public static void Sto()
        {
            Debugger.StepOver();
        }

        public static void Sto(int steps)
        {
            for (int i = 0; i < steps; i++)
                Sto();
        }

        public static void Run()
        {
            Debugger.Run();
        }

        public static void Pause()
        {
            Debugger.Pause();
        }

        public static void Stop()
        {
            Debugger.Stop();
        }

        public static void Skip(int numInstructions = 1)
        {
            Debugger.RunCommand($"skip {numInstructions}");
        }
    }
}
