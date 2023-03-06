using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xnyu_debug_studio
{
    // Usage:
    // SigScanSharp sigScan = new SigScanSharp(Process, "all/main/'specificmodule[,moremodules]'");
    // SigPattern pattern = new SigPattern("01 02 03 ?? ?? 04 05 XX ?? ?? ?? ?? 06 07 08");
    // long address = sigScan.FindPattern(pattern, true);

    public class SigScan
    {
        private Process hProc;
        private IntPtr g_hProcess { get; set; }
        public List<byte[]> g_arrModuleBuffers;
        public List<long> g_lpModuleBases;

        public bool module_error;

        public SigScan(Process _hProc, string _target)
        {
            // Process
            this.hProc = _hProc;

            // Process ID
            this.g_hProcess = hProc.Handle;

            // Fill lists
            g_arrModuleBuffers = new List<byte[]>();
            g_lpModuleBases = new List<long>();

            // Catch error if something occured
            this.module_error = SelectModule(_target.ToLower());
        }

        public bool SelectModule(string _target)
        {
            // Clear lists
            if (g_arrModuleBuffers.Count > 0) g_arrModuleBuffers.Clear();
            if (g_lpModuleBases.Count > 0) g_lpModuleBases.Clear();

            try
            {
                if (_target == "all")
                {
                    foreach (ProcessModule pm in hProc.Modules)
                    {
                        long tmp_g_lpModuleBase = (long)pm.BaseAddress;
                        byte[] tmp_g_arrModuleBuffer = new byte[pm.ModuleMemorySize];
                        Win32.ReadProcessMemory(g_hProcess, tmp_g_lpModuleBase, tmp_g_arrModuleBuffer, pm.ModuleMemorySize);
                        g_arrModuleBuffers.Add(tmp_g_arrModuleBuffer);
                        g_lpModuleBases.Add(tmp_g_lpModuleBase);
                    }
                }
                else if (_target.Contains(","))
                {
                    string[] module_names = _target.Split(',');
                    List<ProcessModule> pms = new List<ProcessModule>();

                    for (int t = 0; t < module_names.Length; t++)
                    {
                        foreach (ProcessModule pm in hProc.Modules)
                        {
                            if (pm.FileName.ToLower().Contains(module_names[t]))
                            {
                                pms.Add(pm);
                                break;
                            }
                        }
                    }

                    if (pms.Count > 0)
                    {
                        foreach (ProcessModule pm in pms)
                        {
                            long tmp_g_lpModuleBase = (long)pm.BaseAddress;
                            byte[] tmp_g_arrModuleBuffer = new byte[pm.ModuleMemorySize];
                            Win32.ReadProcessMemory(g_hProcess, tmp_g_lpModuleBase, tmp_g_arrModuleBuffer, pm.ModuleMemorySize);
                            g_arrModuleBuffers.Add(tmp_g_arrModuleBuffer);
                            g_lpModuleBases.Add(tmp_g_lpModuleBase);
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    ProcessModule targetModule = null;

                    if (_target == "main")
                    {
                        targetModule = hProc.MainModule;
                    }
                    else
                    {
                        foreach (ProcessModule pm in hProc.Modules)
                        {
                            if (pm.FileName.ToLower().Contains(_target))
                            {
                                targetModule = pm;
                                break;
                            }
                        }
                    }

                    if (targetModule != null)
                    {
                        long tmp_g_lpModuleBase = (long)targetModule.BaseAddress;
                        byte[] tmp_g_arrModuleBuffer = new byte[targetModule.ModuleMemorySize];
                        Win32.ReadProcessMemory(g_hProcess, tmp_g_lpModuleBase, tmp_g_arrModuleBuffer, targetModule.ModuleMemorySize);
                        g_arrModuleBuffers.Add(tmp_g_arrModuleBuffer);
                        g_lpModuleBases.Add(tmp_g_lpModuleBase);
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                return true;
            }

            return false;
        }


        public long FindPattern(SigPattern pattern, bool time_output = false)
        {
            // Module is null
            if (g_arrModuleBuffers.Count == 0 || g_lpModuleBases.Count == 0) return 0;

            // Pattern has noting
            if (pattern.bytes.Count == 0) return 0;

            // Time measureing for debugging
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Final address to return
            long final_address = 0;

            // Trigger for found the pattern
            bool found = true;

            for (int z = 0; z < g_arrModuleBuffers.Count; z++)
            {
                byte[] g_arrModuleBuffer = g_arrModuleBuffers[z];
                for (int nModuleIndex = 0; nModuleIndex < g_arrModuleBuffer.Length; nModuleIndex++)
                {
                    if (nModuleIndex + pattern.pattern_size < g_arrModuleBuffer.Length)
                    {

                        // Index for the pattern data
                        int unknown_index = 0;
                        int byte_index = 0;
                        int array_index = nModuleIndex;

                        // Iterations
                        int iterations = (pattern.bytes.Count > pattern.unknowns.Count ? pattern.bytes.Count : pattern.unknowns.Count);

                        // Start with unknown
                        if (pattern.unknown_start)
                        {
                            array_index += pattern.unknowns[unknown_index];
                            unknown_index++;
                        }

                        // Iterate for unknowns and bytes
                        for (int i = 0; i < iterations; i++)
                        {
                            // Equal the bytes
                            if (byte_index < pattern.bytes.Count)
                            {
                                // Take sublist of bytes
                                List<byte> sub_bytes = pattern.bytes[byte_index];
                                for (int b = 0; b < sub_bytes.Count; b++)
                                {
                                    // If a single byte is wrong, instant return
                                    if (g_arrModuleBuffer[array_index] != sub_bytes[b])
                                    {
                                        found = false;
                                        break;
                                    }
                                    array_index++;
                                }
                                byte_index++;
                            }
                            else
                            {
                                // We found all we need, so break this for-loop
                                break;
                            }

                            // Add the unkowns offset
                            if (unknown_index < pattern.unknowns.Count)
                            {
                                array_index += pattern.unknowns[unknown_index];
                                unknown_index++;
                            }

                            // Not found, break this loop
                            if (!found) break;
                        }

                        if (found)
                        {
                            //Baseaddress + byte offset + address offset
                            final_address = g_lpModuleBases[z] + (long)nModuleIndex + (long)pattern.address_entrypoint;
                            break;
                        }
                    }
                }

                if (found)
                {
                    //Break if found
                    break;
                }
            }

            // Time output?
            if (time_output) Console.WriteLine("Time elapsed: " + stopwatch.ElapsedMilliseconds.ToString() + " milliseconds");

            // Destroy watch
            stopwatch.Stop();
            stopwatch = null;

            // Return address
            return final_address;
        }

        private static class Win32
        {
            [DllImport("kernel32.dll")]
            public static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead = 0);
        }
    }

    public class SigPattern
    {
        // Original pattern
        public string pattern;

        // Bytes we know in 1-Byte hexadecimal format
        public List<List<byte>> bytes;

        // Bytes we don't know, marked with an ??
        public List<int> unknowns;

        // Start with an unknown
        public bool unknown_start;

        // The offset to the address to search for, marked with an XX
        public int address_entrypoint;

        // Size of the whole pattern, except the address entrypoint 
        public int pattern_size;

        // Error with the pattern string
        public bool pattern_error;

        // Error message
        public string pattern_error_message;

        public SigPattern(string _pattern)
        {
            // Save formatted pattern
            pattern = _pattern.Replace(" ", "").ToLower();

            // Create lists
            bytes = new List<List<byte>>();
            unknowns = new List<int>();

            // Pattern size is 0 at the beginning
            pattern_size = 0;

            // Set error to false
            pattern_error = false;

            // Parse the pattern
            ParsePatternString();
        }

        private void ParsePatternString()
        {

            if (pattern.Length % 2 == 0)
            {
                // Find all 'X'
                int x_amount = 0;
                foreach (char c in pattern) if (c == 'x') x_amount++;

                // Check for too low 'X'
                if (x_amount > 1)
                {
                    // Check for too much 'X'
                    if (x_amount < 3)
                    {
                        // Check for the right format
                        foreach (char c in pattern)
                        {
                            if (!(c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9' || c == 'a' || c == 'b' || c == 'c' || c == 'd' || c == 'e' || c == 'f' || c == '?' || c == 'x'))
                            {
                                pattern_error = true;
                                pattern_error_message = "Pattern has not supported characters included (Only '0123456789abcdef?x' are supported)";
                            }
                        }

                        if (!pattern_error)
                        {
                            // Temporary values
                            List<byte> tmp_bytes = new List<byte>();
                            int tmp_unknowns = 0;

                            // Everything is fine, continoue the parsing
                            for (int i = 0; i < pattern.Length; i += 2)
                            {
                                string hex_pair = pattern.Substring(i, 2);

                                if (hex_pair == "??" || hex_pair == "xx")
                                {
                                    // Address offset
                                    if (hex_pair == "xx") address_entrypoint = pattern_size;

                                    // Set to true if it is the first in the pattern
                                    if (bytes.Count == 0 && unknowns.Count == 0 && tmp_bytes.Count == 0 && tmp_unknowns == 0) unknown_start = true;

                                    // Dump the gathered bytes
                                    if (tmp_bytes.Count > 0)
                                    {
                                        bytes.Add(new List<byte>());
                                        bytes[bytes.Count - 1] = tmp_bytes.ToList();
                                        tmp_bytes.Clear();
                                    }

                                    // Increase unknowns
                                    tmp_unknowns++;

                                    // Increase pattern size
                                    pattern_size++;
                                }
                                else
                                {
                                    // Dump the gathered unknowns
                                    if (tmp_unknowns > 0)
                                    {
                                        unknowns.Add(tmp_unknowns);
                                        tmp_unknowns = 0;
                                    }

                                    // Convert to byte and add to list
                                    tmp_bytes.Add(byte.Parse(hex_pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture));

                                    // Increase pattern size
                                    pattern_size++;
                                }
                            }

                            // Add to lists if something is there is some rest
                            if (tmp_bytes.Count > 0)
                            {
                                bytes.Add(new List<byte>());
                                bytes[bytes.Count - 1] = tmp_bytes.ToList();
                            }
                            if (tmp_unknowns > 0) unknowns.Add(tmp_unknowns);

                        }

                    }
                    else
                    {
                        pattern_error = true;
                        pattern_error_message = "Pattern has to many X characters included, only a single 'XX' is allowed to mark the adress offset";
                    }
                }
                else
                {
                    pattern_error = true;
                    pattern_error_message = "Pattern has no XX included to mark the position of the address to search";
                }
            }
            else
            {
                pattern_error = true;
                pattern_error_message = "Pattern has an odd amount of characters, something must be wrong tho (Pattern is: " + pattern + ")";
            }
        }

        private void WriteDebugMessages()
        {
            // Use this to get extra output from your parsed pattern, if something doesn't work well
            if (!pattern_error)
            {
                Console.WriteLine("Bytes List size: " + bytes.Count.ToString());
                for (int p = 0; p < bytes.Count; p++)
                {
                    string bytes_string = "";
                    for (int i = 0; i < bytes[p].Count; i++)
                    {
                        bytes_string = bytes_string + string.Format("{0:X}", bytes[p][i]) + (i + 1 < bytes[p].Count ? "-" : "");
                    }
                    Console.WriteLine("[" + p.ToString() + "] Bytes: " + bytes_string);
                }

                Console.WriteLine("\nUnknowns List size: " + unknowns.Count.ToString());
                for (int i = 0; i < unknowns.Count; i++)
                {
                    Console.WriteLine("[" + i.ToString() + "] Unknowns: " + unknowns[i]);
                }

                Console.WriteLine("\nAddress entry point offset: " + address_entrypoint.ToString());

                Console.WriteLine("\nUnknown Start Bool: " + unknown_start.ToString());
            }
            else
            {
                Console.WriteLine("Error while parsing: " + pattern_error_message);
            }
        }
    }
}
