using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xnyu_debug_studio
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        public static bool Is64Bit(Process process)
        {
            if (!Environment.Is64BitOperatingSystem)
                return false;

            bool isWow64;
            if (!IsWow64Process(process.Handle, out isWow64))
                throw new Win32Exception();
            return !isWow64;
        }
    }

    public class Conversions
    {
        public static string HexFillZeroes(long number, int zeroes, bool reverse = false)
        {
            string formatted = "";

            string number_string = $"{number:X}";

            if (number_string.Length >= zeroes) return number_string;

            for (int i = 0; i < zeroes - number_string.Length; i++) formatted = formatted + "0";

            if(reverse) return number_string + formatted;

            return formatted + number_string;
        }

        public static string HexFillZeroes(ulong number, int zeroes, bool reverse = false)
        {
            string formatted = "";

            string number_string = $"{number:X}";

            if (number_string.Length >= zeroes) return number_string;

            for (int i = 0; i < zeroes - number_string.Length; i++) formatted = formatted + "0";

            if (reverse) return number_string + formatted;

            return formatted + number_string;
        }

        public static string HexFillZeroes(int number, int zeroes, bool reverse = false)
        {
            string formatted = "";

            string number_string = $"{number:X}";

            if (number_string.Length >= zeroes) return number_string;

            for (int i = 0; i < zeroes - number_string.Length; i++) formatted = formatted + "0";

            if (reverse) return number_string + formatted;

            return formatted + number_string;
        }

        public static string HexFillZeroes(uint number, int zeroes, bool reverse = false)
        {
            string formatted = "";

            string number_string = $"{number:X}";

            if (number_string.Length >= zeroes) return number_string;

            for (int i = 0; i < zeroes - number_string.Length; i++) formatted = formatted + "0";

            if (reverse) return number_string + formatted;

            return formatted + number_string;
        }

        public static string HexFillZeroes(byte number, int zeroes, bool reverse = false)
        {
            string formatted = "";

            string number_string = $"{number:X}";

            if (number_string.Length >= zeroes) return number_string;

            for (int i = 0; i < zeroes - number_string.Length; i++) formatted = formatted + "0";

            if (reverse) return number_string + formatted;

            return formatted + number_string;
        }
    }
}
