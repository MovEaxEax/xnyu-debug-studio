using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static gh.ghapi;

namespace xnyu_debug_studio
{
    public class SharedFunctions
    {
        [DllImport("kernel32.dll")]
        public static extern UIntPtr LoadLibraryA(string moduleName);

        [DllImport("psapi.dll")]
        static extern bool EnumProcessModules(UIntPtr hProcess, [Out] UIntPtr[] lphModule, uint cb, [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(UIntPtr hProcess, UIntPtr hModule, [Out] char[] lpBaseName, uint nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern UIntPtr GetProcAddress(UIntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll")]
        static extern bool FreeLibrary(UIntPtr hModule);

        public static UIntPtr targetDLLHandle = UIntPtr.Zero;
        public static UIntPtr initDebuggerPointer = UIntPtr.Zero;
        public static UIntPtr playScriptTASPointer = UIntPtr.Zero;
        public static UIntPtr recordScriptTASPointer = UIntPtr.Zero;
        public static UIntPtr enableFrameByFrameTASPointer = UIntPtr.Zero;
        public static UIntPtr playToRecordTASPointer = UIntPtr.Zero;
        public static UIntPtr windowStayActiveTASPointer = UIntPtr.Zero;
        public static UIntPtr receiveFrameTASPointer = UIntPtr.Zero;
        public static UIntPtr toggleDevConsolePointer = UIntPtr.Zero;
        public static UIntPtr toggleDevModePointer = UIntPtr.Zero;
        public static UIntPtr toggleOverclockPointer = UIntPtr.Zero;
        public static UIntPtr ejectDebuggerPointer = UIntPtr.Zero;
        public static UIntPtr checkIfRecordScriptIsDoneTASPointer = UIntPtr.Zero;
        public static UIntPtr checkIfPlayScriptIsDoneTASPointer = UIntPtr.Zero;
        public static UIntPtr toggleTASIgnoreMousePointer = UIntPtr.Zero;



        public static Process proc = null;
        public static string debugDLL = null;
        public SharedFunctions(Process _proc, string _debugDLL)
        {
            debugDLL = _debugDLL;
            proc = _proc;

            const int initialSize = 1024;
            UIntPtr[] moduleHandles = new UIntPtr[initialSize];

            // Call EnumProcessModules to get the module handles
            uint bytesNeeded;
            bool success = EnumProcessModules(new UIntPtr((ulong)_proc.Handle.ToInt64()), moduleHandles, (uint)(moduleHandles.Length * UIntPtr.Size), out bytesNeeded);

            if (!success)
            {
                foreach(ProcessModule module in _proc.Modules)
                {
                    if(module.FileName.Contains("xnyu-debug"))
                    {
                        targetDLLHandle = new UIntPtr((ulong)module.BaseAddress.ToInt64());
                        break;
                    }
                }
            }
            else
            {

                // Calculate the number of module handles returned
                int count = (int)(bytesNeeded / UIntPtr.Size);

                // Iterate through the module handles to find the desired module
                for (int i = 0; i < count; i++)
                {
                    char[] moduleName = new char[1024];
                    uint moduleNameLength = GetModuleFileNameEx(new UIntPtr((ulong)_proc.Handle.ToInt64()), moduleHandles[i], moduleName, (uint)moduleName.Length);

                    if (moduleNameLength > 0)
                    {
                        string moduleFileName = new string(moduleName, 0, (int)moduleNameLength);
                        if (moduleFileName.EndsWith("xnyu-debug.dll"))
                        {
                            // Found the module, return the handle
                            targetDLLHandle = moduleHandles[i];
                            break;
                        }
                    }
                }
            }

            UIntPtr shadowDLL = LoadLibraryA(debugDLL);
            Thread.Sleep(100);

            initDebuggerPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "initDebugger").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            playScriptTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "playScriptTAS").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            recordScriptTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "recordScriptTAS").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            enableFrameByFrameTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "enableFrameByFrameTAS").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            playToRecordTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "playToRecordTAS").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            windowStayActiveTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "windowStayActive").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            receiveFrameTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "receiveFrameTAS").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            toggleDevConsolePointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "toggleDevConsole").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            toggleDevModePointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "toggleDevMode").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            toggleOverclockPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "toggleOverclocker").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            ejectDebuggerPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "ejectDebugger").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            checkIfRecordScriptIsDoneTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "checkIfRecordScriptIsDoneTAS").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            checkIfPlayScriptIsDoneTASPointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "checkIfPlayScriptIsDoneTAS").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            toggleTASIgnoreMousePointer = new UIntPtr((ulong)((GetProcAddress(shadowDLL, "toggleTASIgnoreMouse").ToUInt64() - shadowDLL.ToUInt64()) + targetDLLHandle.ToUInt64()));
            
            FreeLibrary(shadowDLL);
            Thread.Sleep(100);
        }

        public int initDebugger(string parameter)
        {
            return InvokeFunction(initDebuggerPointer, parameter, proc.ProcessName);
        }

        public int playScriptTAS(string parameter)
        {
            return InvokeFunction(playScriptTASPointer, parameter, proc.ProcessName);
        }

        public int recordScriptTAS(string parameter)
        {
            return InvokeFunction(recordScriptTASPointer, parameter, proc.ProcessName);
        }

        public int checkIfRecordScriptIsDoneTAS(string parameter)
        {
            return InvokeFunction(checkIfRecordScriptIsDoneTASPointer, parameter, proc.ProcessName);
        }

        public int checkIfPlayScriptIsDoneTAS(string parameter)
        {
            return InvokeFunction(checkIfPlayScriptIsDoneTASPointer, parameter, proc.ProcessName);
        }

        public int enableFrameByFrameTAS(string parameter)
        {
            return InvokeFunction(enableFrameByFrameTASPointer, parameter, proc.ProcessName);
        }

        public int playToRecordTAS(string parameter)
        {
            return InvokeFunction(playToRecordTASPointer, parameter, proc.ProcessName);
        }

        public int windowStayActive(string parameter)
        {
            return InvokeFunction(windowStayActiveTASPointer, parameter, proc.ProcessName);
        }
        
        public int receiveFrameTAS(string parameter)
        {
            return InvokeFunction(receiveFrameTASPointer, parameter, proc.ProcessName);
        }

        public int toggleDevConsole(string parameter)
        {
            return InvokeFunction(toggleDevConsolePointer, parameter, proc.ProcessName);
        }

        public int toggleDevMode(string parameter)
        {
            return InvokeFunction(toggleDevModePointer, parameter, proc.ProcessName);
        }

        public int toggleOverclock(string parameter)
        {
            return InvokeFunction(toggleOverclockPointer, parameter, proc.ProcessName);
        }

        public int toggleTASIgnoreMouse(string parameter)
        {
            return InvokeFunction(toggleTASIgnoreMousePointer, parameter, proc.ProcessName);
        }

        public int ejectDebugger(string parameter)
        {
            return InvokeFunction(ejectDebuggerPointer, parameter, proc.ProcessName);
        }

    }

}




