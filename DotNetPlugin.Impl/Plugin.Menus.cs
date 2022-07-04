using System;
using System.Windows.Forms;
using DotNetPlugin.Properties;
using Dotx64Dbg;

namespace DotNetPlugin
{
    partial class Plugin
    {
        protected override void SetupMenu(Menus menus)
        {
            menus.Main
                .AddAndConfigureItem("&About...", OnAboutMenuItem).SetIcon(Resources.AboutIcon);

            menus.Disasm
                .AddItem("Print selection", TestMenu01)
                .AddItem("Another Menu/Sub Entry", TestMenu02);

            menus.Memmap
                .AddItem("Do something with selected memory", TestMenu03);

            menus.Stack
                .AddItem("Do something with selected stack", TestMenu04);

            menus.Dump
                .AddItem("Do something with selected dump", TestMenu05);
        }

        public void OnAboutMenuItem(MenuItem menuItem)
        {
            MessageBox.Show(HostWindow, "DotNet Plugin For x64dbg\nCoded By <your_name_here>", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void TestMenu01(MenuItem menuItem)
        {
            var selection = UI.Disassembly.GetSelection();
            LogInfo($"Disassembly selection, Start {selection.Start:X}, End {selection.End:X}, Len: {selection.Size:X}");
        }

        public static void TestMenu02(MenuItem menuItem)
        {
            LogInfo($"Nesting Menus is easy.");
        }

        public static void TestMenu03(MenuItem menuItem)
        {
            Console.WriteLine("Do something with selected memory");

            var selection = UI.MemoryMap.GetSelection();
            LogInfo($"Memory Map selection, Start {selection.Start:X}, End {selection.End:X}, Len: {selection.Size:X}");
        }

        public static void TestMenu04(MenuItem menuItem)
        {
            Console.WriteLine("Do something with selected stack");

            var selection = UI.Stack.GetSelection();
            LogInfo($"Stack selection, Start {selection.Start:X}, End {selection.End:X}, Len: {selection.Size:X}");
        }

        public static void TestMenu05(MenuItem menuItem)
        {
            Console.WriteLine("Do something with selected dump");

            var selection = UI.Dump.GetSelection();
            LogInfo($"Dump selection, Start {selection.Start:X}, End {selection.End:X}, Len: {selection.Size:X}");
        }
    }
}
