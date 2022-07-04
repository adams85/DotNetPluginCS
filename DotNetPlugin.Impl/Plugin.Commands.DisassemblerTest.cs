using System;
using Dotx64Dbg;
using static Dotx64Dbg.Operands;

namespace DotNetPlugin
{
    partial class Plugin
    { 
        [Command("TestAssembler")]
        public bool BasicAssembly(string[] args)
        {
            nuint ip = Thread.Active != null ? Thread.Active.Nip : 0;
            using (var asm = new Assembler(ip))
            {
#if _X64_
            // Create some assembly.
            asm
                .Mov(R9, R10)
                .Shl(R9, Imm(1))
                .Push(Rax)
                .Pop(Rdx)
                .Lea(Rsp, QwordPtr(Rsp, -8))
                .Xchg(Rax, Rdx)
                .Ret()
                ;

            // Insert at the beginning.
            asm.Cursor = null;
            asm
                .Push(R9)
                .Pop(R10)
                ;

#else
                asm
                    .Mov(Eax, Edx)
                    .Shl(Edx, Imm(1))
                    .Push(Eax)
                    .Pop(Edx)
                    .Lea(Esp, Ptr(Esp, -4))
                    .Xchg(Eax, Edx)
                    .Ret()
                    ;
#endif
                // Serialize the nodes into x86.
                asm.Finalize();

                // Write into process.
                var bytes = asm.GetData();

                var bytesWritten = Memory.Write(ip, bytes);
                Console.WriteLine($"Wrote {bytesWritten} bytes");

                UI.Disassembly.Update();

            }

            return true;
        }

        [Command("AssembleFromIP")]
        public bool EncodeIntoAssembler(string[] args)
        {
            var decoder = Decoder.Create();
            nuint ip = Thread.Active.Nip;
            var asm = new Assembler(ip);

            var instr = decoder.Decode(ip);
            asm.Emit(instr);

            // Serialize the nodes into x86.
            asm.Finalize();

            // Write into process.
            var bytes = asm.GetData();

            var bytesWritten = Memory.Write(ip, bytes);
            Console.WriteLine($"Wrote {bytesWritten} bytes");

            UI.Disassembly.Update();

            return true;
        }

        [Command("AssembleWithLabel")]
        public bool AssemblerWithLabels(string[] args)
        {
            nuint ip = Thread.Active.Nip;

            var asm = new Assembler(ip);

            var myLabel = asm.CreateLabel();

            asm.Mov(Eax, Imm(12))
                .Xor(Edx, Edx)
                .Cmp(Eax, Edx)
                .Jmp(myLabel)
                .Nop()
                .Nop()
                .BindLabel(myLabel)
                .Ret()
                ;

            // Serialize the nodes into x86.
            asm.Finalize();

            // Write into process.
            var bytes = asm.GetData();

            var bytesWritten = Memory.Write(ip, bytes);
            Console.WriteLine($"Wrote {bytesWritten} bytes");

            UI.Disassembly.Update();

            return true;
        }

    }
}
