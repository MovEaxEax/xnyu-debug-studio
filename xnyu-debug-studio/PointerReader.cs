using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xnyu_debug_studio
{
    public class BasePointer
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        private Process process;
        private IntPtr processHandle;
        private IntPtr baseAddress;
        private string baseModule;
        private ulong baseOffset;
        private int[] offsets;

        public BasePointer(Process _process, string _baseModule, long baseOffset, int[] _offsets)
        {
            // Settings
            process = _process;
            processHandle = process.Handle;
            baseModule = _baseModule.ToLower();
            offsets = _offsets;

            if (baseModule == "main")
            {
                // Take main module
                baseAddress = process.MainModule.BaseAddress;
            }
            else
            {
                // Take arbitrary module
                foreach (ProcessModule pm in process.Modules)
                {
                    if (pm.FileName.Contains(baseModule, StringComparison.OrdinalIgnoreCase)) baseAddress = pm.BaseAddress;
                }
            }

            // Calculate finale base address
            baseAddress = (IntPtr)((long)baseAddress + (long)baseOffset);
        }

        public int ReadInt()
        {
            // Memory handler
            Memory mem = new Memory(process);

            return BitConverter.ToInt32(mem.ReadMemoryPointer((long)baseAddress, offsets, "int"), 0);
        }

        public long ReadLong()
        {
            // Memory handler
            Memory mem = new Memory(process);

            return BitConverter.ToInt64(mem.ReadMemoryPointer((long)baseAddress, offsets, "long"), 0);
        }
    }

    public class Memory
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);



        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_WM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;
        static public Process proc_name;
        static public IntPtr processHandle;



        public Memory(Process procy)
        {
            proc_name = procy;
            processHandle = proc_name.Handle;//OpenProcess(PROCESS_VM_OPERATION, false, proc_name.Id);
        }


        public byte[] ReadMemory(long address, string type = "int")
        {
            byte[] ret;

            if (type == "short")
            {
                ret = new byte[2];
            }
            else if (type == "long")
            {
                ret = new byte[8];
            }
            else
            {
                ret = new byte[4];
            }

            var to_read = ret.Length;

            ReadProcessMemory((int)processHandle, address, ret, ret.Length, ref to_read).ToString();

            return ret;
        }

        public bool WriteMemory(long address, byte[] bytes)
        {
            var to_write = bytes.Length;
            return WriteProcessMemory((int)processHandle, address, bytes, bytes.Length, ref to_write);
        }

        public byte[] ReadMemoryPointer(long address, int[] offsets, string type = "int")
        {
            byte[] ret;

            long pointer_address = address;

            if (offsets.Length > 0)
            {
                pointer_address = BitConverter.ToInt64(ReadMemory(address, "long"), 0);

                for (int i = 0; i < offsets.Length - 1; i++)
                {
                    pointer_address = BitConverter.ToInt64(ReadMemory(pointer_address + offsets[i], "long"), 0);
                }

                pointer_address = pointer_address + offsets[offsets.Length - 1];
            }

            if (type == "short")
            {
                ret = new byte[2];
            }
            else if (type == "long")
            {
                ret = new byte[8];
            }
            else
            {
                ret = new byte[4];
            }

            var to_read = ret.Length;

            return ReadMemory(pointer_address, type);
        }

        public bool WriteMemoryPointer(long address, int[] offsets, byte[] bytes, string type = "int")
        {
            long pointer_address = BitConverter.ToInt64(ReadMemory(address, "long"), 0);
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                pointer_address = BitConverter.ToInt64(ReadMemory(pointer_address + offsets[i], "long"), 0);
            }
            pointer_address = pointer_address + offsets[offsets.Length - 1];

            var to_write = bytes.Length;
            return WriteProcessMemory((int)processHandle, pointer_address, bytes, bytes.Length, ref to_write);
        }

    }

}
