using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xnyu_debug_studio
{
    public class Template
    {
        public struct Module
        {
            public string target_module_name;
            public string target_module_size;
            public string target_module_productname;
            public string target_module_internalname;
            public string target_module_companyname;
            public string target_module_version;
            public string config_modname;
            public string config_processname;
            public string config_version;
            public string config_tashook;
            public string config_mousedriver_set;
            public string config_mousedriver_get;
            public string config_keyboarddriver_set;
            public string config_keyboarddriver_get;
            public string config_joystickdriver_set;
            public string config_joystickdriver_get;
            public string config_graphicdriver;
            public string config_d3d9_hook;
            public string config_overclocker_hooks;
            public string config_winactive_hooks;
            public string config_rawinput_demand;
            public string config_script_directory;
            public string config_working_directory;
            public string config_log_directory;
            public string config_debugmod_directory;
            public string config_debugfunction_directory;
            public string config_debugaddress_directory;
            public string config_editormode_settings_directory;
            public string config_editormode_actions_directory;
            public string config_supervision_directory;
            public string config_inputmapping_directory;
            public string config_savefile_directory;
            public string config_debugconfig_directory;
            public string config_anticheat_file;
            public string config_frame_skip;
            public string config_tas_delay;
            public bool is64bit;
        };

        public Module target_module;
        public List<Module> modules_64;
        public List<Module> modules_32;

        public string root_dir;

        public bool is64bit;
        public Process process;
        public IntPtr window;

        public bool template_initialized;

        public List<long> debug_function_addresses;
        public Template()
        {
            // Template data and relative paths
            root_dir = Directory.GetCurrentDirectory();

            modules_64 = new List<Module>();
            modules_32 = new List<Module>();

            is64bit = IntPtr.Size == 8 ? true : false;
        }

        public Process FindTargetProcess()
        {
            try
            {
                List<Module> targetModules = is64bit ? modules_64 : modules_32;

                foreach (Module module in targetModules)
                {
                    Process[] processes = Process.GetProcessesByName(module.config_processname);
                    if (module.target_module_name == "") return processes[0];
                    if (processes.Length > 0)
                    {
                        foreach (Process target in processes)
                        {
                            foreach (ProcessModule pm in target.Modules)
                            {
                                bool moduleFound = true;
                                if (pm.ModuleName != module.target_module_name) moduleFound = false;
                                if (module.target_module_size != "" && module.target_module_size != null) if (pm.ModuleMemorySize != int.Parse(module.target_module_size)) moduleFound = false;
                                if (module.target_module_productname != "" && module.target_module_productname != null) if (pm.FileVersionInfo.ProductName != module.target_module_productname) moduleFound = false;
                                if (module.target_module_internalname != "" && module.target_module_internalname != null) if (pm.FileVersionInfo.InternalName != module.target_module_internalname) moduleFound = false;
                                if (module.target_module_companyname != "" && module.target_module_companyname != null) if (pm.FileVersionInfo.CompanyName != module.target_module_companyname) moduleFound = false;
                                if (module.target_module_version != "" && module.target_module_version != null) if (pm.FileVersionInfo.FileVersion != module.target_module_version) moduleFound = false;
                                if (moduleFound)
                                {
                                    target_module = module;
                                    return target;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Something failed, set the init bool to false
                MessageBox.Show("Error while targeting process: \n" + e.Message);
                return null;
            }
            return null;
        }

        public string CheckTemplateParsing()
        {
            if (modules_32.Count == 0 && modules_64.Count == 0) return "No module32/module64 declaration was found in the template.";
            if (is64bit && modules_64.Count == 0) return "Current assembly is 64-bit, but no module64 declaration was found in the template.";
            if (!is64bit && modules_32.Count == 0) return "Current assembly is 32-bit, but no module32 declaration was found in the template.";
            return "";
        }

        public void DebugShowModuleParameter(int mode)
        {
            string dbgOut = "";
            if (mode == 0)
            {
                dbgOut = dbgOut + "config_debugaddress_directory: " + modules_64[0].config_debugaddress_directory + "\n";
                dbgOut = dbgOut + "config_debugconfig_directory: " + modules_64[0].config_debugconfig_directory + "\n";
                dbgOut = dbgOut + "config_debugfunction_directory: " + modules_64[0].config_debugfunction_directory + "\n";
                dbgOut = dbgOut + "config_debugmod_directory: " + modules_64[0].config_debugmod_directory + "\n";
                dbgOut = dbgOut + "config_inputmapping_directory: " + modules_64[0].config_inputmapping_directory + "\n";
                dbgOut = dbgOut + "config_log_directory: " + modules_64[0].config_log_directory + "\n";
                dbgOut = dbgOut + "config_savefile_directory: " + modules_64[0].config_savefile_directory + "\n";
                dbgOut = dbgOut + "config_script_directory: " + modules_64[0].config_script_directory + "\n";
                dbgOut = dbgOut + "config_working_directory: " + modules_64[0].config_working_directory + "\n";
                dbgOut = dbgOut + "config_processname: " + modules_64[0].config_processname + "\n";
                dbgOut = dbgOut + "config_modname: " + modules_64[0].config_modname + "\n";
                dbgOut = dbgOut + "config_version: " + modules_64[0].config_version + "\n";
                dbgOut = dbgOut + "target_module_companyname: " + modules_64[0].target_module_companyname + "\n";
                dbgOut = dbgOut + "target_module_internalname: " + modules_64[0].target_module_internalname + "\n";
                dbgOut = dbgOut + "target_module_name: " + modules_64[0].target_module_name + "\n";
                dbgOut = dbgOut + "target_module_productname: " + modules_64[0].target_module_productname + "\n";
                dbgOut = dbgOut + "target_module_size: " + modules_64[0].target_module_size + "\n";
                dbgOut = dbgOut + "target_module_version: " + modules_64[0].target_module_version + "\n";
                dbgOut = dbgOut + "config_keyboarddriver_get: " + modules_64[0].config_keyboarddriver_get + "\n";
                dbgOut = dbgOut + "config_mousedriver_get: " + modules_64[0].config_mousedriver_get + "\n";
                dbgOut = dbgOut + "config_joystickdriver_get: " + modules_64[0].config_joystickdriver_get + "\n";
                dbgOut = dbgOut + "config_keyboarddriver_set: " + modules_64[0].config_keyboarddriver_set + "\n";
                dbgOut = dbgOut + "config_mousedriver_set: " + modules_64[0].config_mousedriver_set + "\n";
                dbgOut = dbgOut + "config_joystickdriver_set: " + modules_64[0].config_joystickdriver_set + "\n";
            }
            if (mode == 1)
            {
                dbgOut = dbgOut + "config_debugaddress_directory: " + modules_32[0].config_debugaddress_directory + "\n";
                dbgOut = dbgOut + "config_debugconfig_directory: " + modules_32[0].config_debugconfig_directory + "\n";
                dbgOut = dbgOut + "config_debugfunction_directory: " + modules_32[0].config_debugfunction_directory + "\n";
                dbgOut = dbgOut + "config_debugmod_directory: " + modules_32[0].config_debugmod_directory + "\n";
                dbgOut = dbgOut + "config_inputmapping_directory: " + modules_32[0].config_inputmapping_directory + "\n";
                dbgOut = dbgOut + "config_log_directory: " + modules_32[0].config_log_directory + "\n";
                dbgOut = dbgOut + "config_savefile_directory: " + modules_32[0].config_savefile_directory + "\n";
                dbgOut = dbgOut + "config_script_directory: " + modules_32[0].config_script_directory + "\n";
                dbgOut = dbgOut + "config_working_directory: " + modules_32[0].config_working_directory + "\n";
                dbgOut = dbgOut + "config_processname: " + modules_32[0].config_processname + "\n";
                dbgOut = dbgOut + "config_modname: " + modules_32[0].config_modname + "\n";
                dbgOut = dbgOut + "config_version: " + modules_32[0].config_version + "\n";
                dbgOut = dbgOut + "target_module_companyname: " + modules_32[0].target_module_companyname + "\n";
                dbgOut = dbgOut + "target_module_internalname: " + modules_32[0].target_module_internalname + "\n";
                dbgOut = dbgOut + "target_module_name: " + modules_32[0].target_module_name + "\n";
                dbgOut = dbgOut + "target_module_productname: " + modules_32[0].target_module_productname + "\n";
                dbgOut = dbgOut + "target_module_size: " + modules_32[0].target_module_size + "\n";
                dbgOut = dbgOut + "target_module_version: " + modules_32[0].target_module_version + "\n";
                dbgOut = dbgOut + "config_keyboarddriver_get: " + modules_32[0].config_keyboarddriver_get + "\n";
                dbgOut = dbgOut + "config_mousedriver_get: " + modules_32[0].config_mousedriver_get + "\n";
                dbgOut = dbgOut + "config_joystickdriver_get: " + modules_32[0].config_joystickdriver_get + "\n";
                dbgOut = dbgOut + "config_keyboarddriver_set: " + modules_32[0].config_keyboarddriver_set + "\n";
                dbgOut = dbgOut + "config_mousedriver_set: " + modules_32[0].config_mousedriver_set + "\n";
                dbgOut = dbgOut + "config_joystickdriver_set: " + modules_32[0].config_joystickdriver_set + "\n";
            }
            if (mode == 2)
            {
                dbgOut = dbgOut + "config_debugaddress_directory: " + target_module.config_debugaddress_directory + "\n";
                dbgOut = dbgOut + "config_debugconfig_directory: " + target_module.config_debugconfig_directory + "\n";
                dbgOut = dbgOut + "config_debugfunction_directory: " + target_module.config_debugfunction_directory + "\n";
                dbgOut = dbgOut + "config_debugmod_directory: " + target_module.config_debugmod_directory + "\n";
                dbgOut = dbgOut + "config_inputmapping_directory: " + target_module.config_inputmapping_directory + "\n";
                dbgOut = dbgOut + "config_log_directory: " + target_module.config_log_directory + "\n";
                dbgOut = dbgOut + "config_savefile_directory: " + target_module.config_savefile_directory + "\n";
                dbgOut = dbgOut + "config_script_directory: " + target_module.config_script_directory + "\n";
                dbgOut = dbgOut + "config_working_directory: " + target_module.config_working_directory + "\n";
                dbgOut = dbgOut + "config_processname: " + target_module.config_processname + "\n";
                dbgOut = dbgOut + "config_modname: " + target_module.config_modname + "\n";
                dbgOut = dbgOut + "config_version: " + target_module.config_version + "\n";
                dbgOut = dbgOut + "target_module_companyname: " + target_module.target_module_companyname + "\n";
                dbgOut = dbgOut + "target_module_internalname: " + target_module.target_module_internalname + "\n";
                dbgOut = dbgOut + "target_module_name: " + target_module.target_module_name + "\n";
                dbgOut = dbgOut + "target_module_productname: " + target_module.target_module_productname + "\n";
                dbgOut = dbgOut + "target_module_size: " + target_module.target_module_size + "\n";
                dbgOut = dbgOut + "target_module_version: " + target_module.target_module_version + "\n";
                dbgOut = dbgOut + "config_keyboarddriver_get: " + target_module.config_keyboarddriver_get + "\n";
                dbgOut = dbgOut + "config_mousedriver_get: " + target_module.config_mousedriver_get + "\n";
                dbgOut = dbgOut + "config_joystickdriver_get: " + target_module.config_joystickdriver_get + "\n";
                dbgOut = dbgOut + "config_keyboarddriver_set: " + target_module.config_keyboarddriver_set + "\n";
                dbgOut = dbgOut + "config_mousedriver_set: " + target_module.config_mousedriver_set + "\n";
                dbgOut = dbgOut + "config_joystickdriver_set: " + target_module.config_joystickdriver_set + "\n";
            }
            MessageBox.Show(dbgOut);
        }

        public string CheckTemplateConfig()
        {
            if (target_module.config_modname == "") return "No config_modname was set.";
            if (target_module.config_processname == "") return "No config_processname was set.";
            if (target_module.target_module_size != "")
            {
                int tryParser = 0;
                if (!int.TryParse(target_module.target_module_size, out tryParser)) return "target_module_size is not a number.";
            }

            if (target_module.config_mousedriver_set != "rawinput" && target_module.config_mousedriver_set != "directinput8" &&
                target_module.config_mousedriver_set != "getmessagea" && target_module.config_mousedriver_set != "getmessagew" &&
                target_module.config_mousedriver_set != "sendinput" && target_module.config_mousedriver_set != "") return "Wrong paramter for config_mousedriver_set.";
            if (target_module.config_mousedriver_get != "rawinput" && target_module.config_mousedriver_get != "directinput8" &&
                target_module.config_mousedriver_get != "getmessagea" && target_module.config_mousedriver_get != "getmessagew" &&
                target_module.config_mousedriver_set != "") return "Wrong paramter for config_mousedriver_set.";
            if (target_module.config_keyboarddriver_set != "rawinput" && target_module.config_keyboarddriver_set != "directinput8" &&
                target_module.config_keyboarddriver_set != "getmessagea" && target_module.config_keyboarddriver_set != "getmessagew" &&
                target_module.config_keyboarddriver_set != "sendinput" && target_module.config_keyboarddriver_set != "") return "Wrong paramter for config_keyboarddriver_set.";
            if (target_module.config_keyboarddriver_get == "") return "No config_keyboarddriver_get was set.";
            if (target_module.config_keyboarddriver_get != "rawinput" && target_module.config_keyboarddriver_get != "directinput8" &&
                target_module.config_keyboarddriver_get != "getmessagea" && target_module.config_keyboarddriver_get != "getmessagew") return "Wrong paramter for config_keyboarddriver_get.";
            if (target_module.config_joystickdriver_get != "xinput1_4" && target_module.config_joystickdriver_get != "xinput1_3" && target_module.config_joystickdriver_get != "directinput8" &&
                target_module.config_joystickdriver_get != "") return "Wrong paramter for config_keyboarddriver_get.";
            if (target_module.config_joystickdriver_set != "xinput1_4" && target_module.config_joystickdriver_set != "xinput1_3" && target_module.config_joystickdriver_set != "directinput8" &&
                target_module.config_joystickdriver_set != "") return "Wrong paramter for config_keyboarddriver_get.";
            if (target_module.config_graphicdriver != "directx9" && target_module.config_graphicdriver != "directx10" &&
                target_module.config_graphicdriver != "directx11" && target_module.config_graphicdriver != "directx12" &&
                target_module.config_graphicdriver != "opengl" && target_module.config_graphicdriver != "vulcan" &&
                target_module.config_graphicdriver != "") return "Wrong paramter for config_graphicdriver.";
            if (target_module.config_rawinput_demand != "true" && target_module.config_rawinput_demand != "false") return "Wrong paramter for config_rawinput_demand.";

            
            target_module.config_script_directory = ParseRelativePaths(target_module.config_script_directory);
            if (target_module.config_script_directory == "") return "No config_script_directory was set.";
            if (!target_module.config_script_directory.Contains(root_dir)) return "config_script_directory doesn't act inside the root directory.";
            if (!Directory.Exists(target_module.config_script_directory)) Directory.CreateDirectory(target_module.config_script_directory);

            target_module.config_working_directory = ParseRelativePaths(target_module.config_working_directory);
            if (target_module.config_working_directory == "") return "No config_working_directory was set.";
            if (!target_module.config_working_directory.Contains(root_dir)) return "config_working_directory doesn't act inside the root directory.";
            if (!Directory.Exists(target_module.config_working_directory)) Directory.CreateDirectory(target_module.config_script_directory);

            target_module.config_log_directory = ParseRelativePaths(target_module.config_log_directory);
            if (target_module.config_log_directory == "") return "No config_log_directory was set.";
            if (!target_module.config_log_directory.Contains(root_dir)) return "config_log_directory doesn't act inside the root directory.";
            if (!Directory.Exists(target_module.config_log_directory)) Directory.CreateDirectory(target_module.config_script_directory);

            target_module.config_debugmod_directory = ParseRelativePaths(target_module.config_debugmod_directory);
            if (target_module.config_debugmod_directory == "") return "No config_debugmod_directory was set.";
            if (!target_module.config_debugmod_directory.Contains(root_dir)) return "config_debugmod_directory doesn't act inside the root directory.";

            target_module.config_debugfunction_directory = ParseRelativePaths(target_module.config_debugfunction_directory);
            if (target_module.config_debugfunction_directory == "") return "No config_debugfunction_directory was set.";
            if (!target_module.config_debugfunction_directory.Contains(root_dir)) return "config_debugfunction_directory doesn't act inside the root directory.";

            target_module.config_debugaddress_directory = ParseRelativePaths(target_module.config_debugaddress_directory);
            if (target_module.config_debugaddress_directory == "") return "No config_debugaddress_directory was set.";
            if (!target_module.config_debugaddress_directory.Contains(root_dir)) return "config_debugaddress_directory doesn't act inside the root directory.";

            target_module.config_editormode_settings_directory = ParseRelativePaths(target_module.config_editormode_settings_directory);
            if (target_module.config_editormode_settings_directory == "") return "No config_editormode_settings_directory was set.";
            if (!target_module.config_editormode_settings_directory.Contains(root_dir)) return "config_editormode_settings_directory doesn't act inside the root directory.";

            target_module.config_editormode_actions_directory = ParseRelativePaths(target_module.config_editormode_actions_directory);
            if (target_module.config_editormode_actions_directory == "") return "No config_editormode_actions_directory was set.";
            if (!target_module.config_editormode_actions_directory.Contains(root_dir)) return "config_editormode_actions_directory doesn't act inside the root directory.";

            target_module.config_supervision_directory = ParseRelativePaths(target_module.config_supervision_directory);
            if (target_module.config_supervision_directory == "") return "No config_supervision_directory was set.";
            if (!target_module.config_supervision_directory.Contains(root_dir)) return "config_supervision_directory doesn't act inside the root directory.";

            target_module.config_inputmapping_directory = ParseRelativePaths(target_module.config_inputmapping_directory);
            if (target_module.config_inputmapping_directory == "") return "No config_inputmapping_directory was set.";
            if (!target_module.config_inputmapping_directory.Contains(root_dir)) return "config_inputmapping_directory doesn't act inside the root directory.";

            target_module.config_savefile_directory = ParseRelativePaths(target_module.config_savefile_directory);
            if (target_module.config_savefile_directory == "") return "No config_savefile_directory was set.";
            if (!target_module.config_savefile_directory.Contains(root_dir)) return "config_savefile_directory doesn't act inside the root directory.";

            target_module.config_debugconfig_directory = ParseRelativePaths(target_module.config_debugconfig_directory);
            if (target_module.config_debugconfig_directory == "") return "No config_debugconfig_directory was set.";
            if (!target_module.config_debugconfig_directory.Contains(root_dir)) return "config_debugconfig_directory doesn't act inside the root directory.";

            if (target_module.config_anticheat_file != "" && target_module.config_anticheat_file != null) if(!File.Exists(target_module.config_anticheat_file)) return "No anti-cheat file was found.";

            if (target_module.config_frame_skip != "")
            {
                int tryParser = 0;
                if (!int.TryParse(target_module.config_frame_skip, out tryParser)) return "config_frame_skip is not a number.";
            }
            else target_module.config_frame_skip = "0";

            if (target_module.config_tas_delay != "")
            {
                int tryParser = 0;
                if (!int.TryParse(target_module.config_tas_delay, out tryParser)) return "config_tas_delay is not a number.";
            }
            else target_module.config_tas_delay = "100";

            return "";
        }

        public string ParseRelativePaths(string target)
        {
            if (target != "")
            {
                target = target.Replace("\\\\", "\\");
                target = target.Replace("//", "\\");
                target = target.Replace("/", "\\");
                target = target.Replace("%root%", root_dir);
                target = target.Replace("%modname%", target_module.config_modname);
            }
            return target;
        }

        public bool ParseTemplateSettings(string template_filename)
        {
            try
            {
                if (!File.Exists(template_filename)) return false;

                string template_text = File.ReadAllText(template_filename);

                while (true)
                {
                    int commentIndex = template_text.IndexOf("//");
                    if (commentIndex == -1) break;
                    int newNIndex = template_text.IndexOf("\n", commentIndex);
                    if (newNIndex == -1) template_text = template_text.Remove(commentIndex, template_text.Length - commentIndex);
                    else template_text = template_text.Remove(commentIndex, (newNIndex + 1) - commentIndex);
                }
                template_text = template_text.Replace("\r", "");
                template_text = template_text.Replace("\n", "");
                template_text = template_text.Replace("\t", "");

                Module module = new Module();

                module.target_module_name = "";
                module.target_module_size = "";
                module.target_module_productname = "";
                module.target_module_internalname = "";
                module.target_module_companyname = "";
                module.target_module_version = "";
                module.config_tashook = "graphics";
                module.config_modname = "";
                module.config_processname = "";
                module.config_version = "";
                module.config_mousedriver_set = "";
                module.config_mousedriver_get = "";
                module.config_keyboarddriver_set = "";
                module.config_keyboarddriver_get = "";
                module.config_joystickdriver_set = "";
                module.config_joystickdriver_get = "";
                module.config_graphicdriver = "";
                module.config_d3d9_hook = "present";
                module.config_overclocker_hooks = "QueryPerformanceCounter";
                module.config_winactive_hooks = "WndProc";
                module.config_rawinput_demand = "true";
                module.config_script_directory = "";
                module.config_working_directory = "";
                module.config_log_directory = "";
                module.config_debugmod_directory = "";
                module.config_debugfunction_directory = "";
                module.config_debugaddress_directory = "";
                module.config_editormode_settings_directory = "";
                module.config_editormode_actions_directory = "";
                module.config_supervision_directory = "";
                module.config_inputmapping_directory = "";
                module.config_savefile_directory = "";
                module.config_debugconfig_directory = "";
                module.config_anticheat_file = "";
                module.config_frame_skip = "";
                module.config_tas_delay = "";
                module.is64bit = false;

                bool moduleFound = false;
                bool moduleOpen = false;
                bool moduleClose = false;
                while (template_text.Length > 0)
                {
                    if (template_text.Length == 0) break;
                    if (!moduleFound && !moduleOpen && !moduleClose)
                    {
                        // Find module statement
                        if (template_text.Length >= "module00".Length)
                        {
                            string moduleID = template_text.Substring(0, "module00".Length);
                            if (moduleID.ToLower() == "module32")
                            {
                                module = new Module();
                                module.is64bit = false;
                                template_text = template_text.Substring("module00".Length, template_text.Length - "module00".Length);
                                moduleFound = true;
                            }
                            if (moduleID.ToLower() == "module64")
                            {
                                module = new Module();
                                module.is64bit = true;
                                template_text = template_text.Substring("module00".Length, template_text.Length - "module00".Length);
                                moduleFound = true;
                            }
                        }
                        else
                        {
                            break;
                        }
                        if(!moduleFound) template_text = template_text.Remove(0, 1);
                    }
                    if (moduleFound && !moduleOpen && !moduleClose)
                    {
                        // Find module open section
                        if (template_text.Length >= "{".Length)
                        {
                            if (template_text[0] == '{')
                            {
                                moduleOpen = true;
                                template_text = template_text.Remove(0, 1);
                            }
                        }
                        else
                        {
                            break;
                        }
                        if (!moduleOpen) template_text.Remove(0, 1);
                    }
                    if (moduleFound && moduleOpen && !moduleClose)
                    {
                        // Find module config
                        bool foundConfig = false;

                        if (template_text.Contains(";") && template_text.Contains("="))
                        {
                            // target_module_name
                            if (template_text.Length > "target_module_name".Length)
                            {
                                if (template_text.Substring(0, "target_module_name".Length) == "target_module_name")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.target_module_name = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // target_module_size
                            if (template_text.Length > "target_module_size".Length)
                            {
                                if (template_text.Substring(0, "target_module_size".Length) == "target_module_size")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.target_module_size = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // target_module_productname
                            if (template_text.Length > "target_module_productname".Length)
                            {
                                if (template_text.Substring(0, "target_module_productname".Length) == "target_module_productname")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.target_module_productname = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // target_module_internalname
                            if (template_text.Length > "target_module_internalname".Length)
                            {
                                if (template_text.Substring(0, "target_module_internalname".Length) == "target_module_internalname")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.target_module_internalname = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // target_module_companyname
                            if (template_text.Length > "target_module_companyname".Length)
                            {
                                if (template_text.Substring(0, "target_module_companyname".Length) == "target_module_companyname")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.target_module_companyname = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // target_module_version
                            if (template_text.Length > "target_module_version".Length)
                            {
                                if (template_text.Substring(0, "target_module_version".Length) == "target_module_version")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.target_module_version = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_tashook
                            if (template_text.Length > "config_tashook".Length)
                            {
                                if (template_text.Substring(0, "config_tashook".Length) == "config_tashook")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_tashook = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_modname
                            if (template_text.Length > "config_modname".Length)
                            {
                                if (template_text.Substring(0, "config_modname".Length) == "config_modname")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_modname = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_processname
                            if (template_text.Length > "config_processname".Length)
                            {
                                if (template_text.Substring(0, "config_processname".Length) == "config_processname")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_processname = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_version
                            if (template_text.Length > "config_version".Length)
                            {
                                if (template_text.Substring(0, "config_version".Length) == "config_version")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_version = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_mousedriver_set
                            if (template_text.Length > "config_mousedriver_set".Length)
                            {
                                if (template_text.Substring(0, "config_mousedriver_set".Length) == "config_mousedriver_set")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_mousedriver_set = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_mousedriver_get
                            if (template_text.Length > "config_mousedriver_get".Length)
                            {
                                if (template_text.Substring(0, "config_mousedriver_get".Length) == "config_mousedriver_get")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_mousedriver_get = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_keyboarddriver_set
                            if (template_text.Length > "config_keyboarddriver_set".Length)
                            {
                                if (template_text.Substring(0, "config_keyboarddriver_set".Length) == "config_keyboarddriver_set")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_keyboarddriver_set = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_keyboarddriver_get
                            if (template_text.Length > "config_keyboarddriver_get".Length)
                            {
                                if (template_text.Substring(0, "config_keyboarddriver_get".Length) == "config_keyboarddriver_get")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_keyboarddriver_get = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_joystickdriver_set
                            if (template_text.Length > "config_joystickdriver_set".Length)
                            {
                                if (template_text.Substring(0, "config_joystickdriver_set".Length) == "config_joystickdriver_set")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_joystickdriver_set = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_joystickdriver_get
                            if (template_text.Length > "config_joystickdriver_get".Length)
                            {
                                if (template_text.Substring(0, "config_joystickdriver_get".Length) == "config_joystickdriver_get")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_joystickdriver_get = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_graphicdriver
                            if (template_text.Length > "config_graphicdriver".Length)
                            {
                                if (template_text.Substring(0, "config_graphicdriver".Length) == "config_graphicdriver")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_graphicdriver = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_d3d9_hook
                            if (template_text.Length > "config_d3d9_hook".Length)
                            {
                                if (template_text.Substring(0, "config_d3d9_hook".Length) == "config_d3d9_hook")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_d3d9_hook = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_overclocker_hooks
                            if (template_text.Length > "config_overclocker_hooks".Length)
                            {
                                if (template_text.Substring(0, "config_overclocker_hooks".Length) == "config_overclocker_hooks")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_overclocker_hooks = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_winactive_hooks
                            if (template_text.Length > "config_winactive_hooks".Length)
                            {
                                if (template_text.Substring(0, "config_winactive_hooks".Length) == "config_winactive_hooks")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_winactive_hooks = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_rawinput_demand
                            if (template_text.Length > "config_rawinput_demand".Length)
                            {
                                if (template_text.Substring(0, "config_rawinput_demand".Length) == "config_rawinput_demand")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_rawinput_demand = parameter.ToLower();
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_script_directory
                            if (template_text.Length > "config_script_directory".Length)
                            {
                                if (template_text.Substring(0, "config_script_directory".Length) == "config_script_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_script_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_working_directory
                            if (template_text.Length > "config_working_directory".Length)
                            {
                                if (template_text.Substring(0, "config_working_directory".Length) == "config_working_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_working_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_log_directory
                            if (template_text.Length > "config_log_directory".Length)
                            {
                                if (template_text.Substring(0, "config_log_directory".Length) == "config_log_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_log_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_debugmod_directory
                            if (template_text.Length > "config_debugmod_directory".Length)
                            {
                                if (template_text.Substring(0, "config_debugmod_directory".Length) == "config_debugmod_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_debugmod_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_debugfunction_directory
                            if (template_text.Length > "config_debugfunction_directory".Length)
                            {
                                if (template_text.Substring(0, "config_debugfunction_directory".Length) == "config_debugfunction_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_debugfunction_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_debugaddress_directory
                            if (template_text.Length > "config_debugaddress_directory".Length)
                            {
                                if (template_text.Substring(0, "config_debugaddress_directory".Length) == "config_debugaddress_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_debugaddress_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_editormode_settings_directory
                            if (template_text.Length > "config_editormode_settings_directory".Length)
                            {
                                if (template_text.Substring(0, "config_editormode_settings_directory".Length) == "config_editormode_settings_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_editormode_settings_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_editormode_actions_directory
                            if (template_text.Length > "config_editormode_actions_directory".Length)
                            {
                                if (template_text.Substring(0, "config_editormode_actions_directory".Length) == "config_editormode_actions_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_editormode_actions_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_supervision_directory
                            if (template_text.Length > "config_supervision_directory".Length)
                            {
                                if (template_text.Substring(0, "config_supervision_directory".Length) == "config_supervision_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_supervision_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_inputmapping_directory
                            if (template_text.Length > "config_inputmapping_directory".Length)
                            {
                                if (template_text.Substring(0, "config_inputmapping_directory".Length) == "config_inputmapping_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_inputmapping_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_savefile_directory
                            if (template_text.Length > "config_savefile_directory".Length)
                            {
                                if (template_text.Substring(0, "config_savefile_directory".Length) == "config_savefile_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_savefile_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }
                            
                            // config_debugconfig_directory
                            if (template_text.Length > "config_debugconfig_directory".Length)
                            {
                                if (template_text.Substring(0, "config_debugconfig_directory".Length) == "config_debugconfig_directory")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_debugconfig_directory = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_anticheat_file
                            if (template_text.Length > "config_anticheat_file".Length)
                            {
                                if (template_text.Substring(0, "config_anticheat_file".Length) == "config_anticheat_file")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_anticheat_file = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_frame_skip
                            if (template_text.Length > "config_frame_skip".Length)
                            {
                                if (template_text.Substring(0, "config_frame_skip".Length) == "config_frame_skip")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_frame_skip = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }

                            // config_tas_delay
                            if (template_text.Length > "config_tas_delay".Length)
                            {
                                if (template_text.Substring(0, "config_tas_delay".Length) == "config_tas_delay")
                                {
                                    int equalIndex = template_text.IndexOf("=");
                                    string parameter = "";
                                    bool parameterStart = false;
                                    while (template_text[equalIndex] != ';')
                                    {
                                        if (!parameterStart)
                                        {
                                            if (template_text[equalIndex] != '=' && template_text[equalIndex] != ' ')
                                            {
                                                parameter = parameter + template_text[equalIndex];
                                                parameterStart = true;
                                            }
                                        }
                                        else
                                        {
                                            parameter = parameter + template_text[equalIndex];
                                        }
                                        equalIndex++;
                                    }
                                    module.config_tas_delay = parameter;
                                    template_text = template_text.Remove(0, equalIndex + 1);
                                    foundConfig = true;
                                }
                            }
                        }

                        if (template_text[0] == '}')
                        {
                            moduleClose = true;
                            foundConfig = true;
                            template_text = template_text.Remove(0, 1);
                        }

                        if(!foundConfig)
                        {
                            template_text = template_text.Remove(0, 1);
                        }
                    }
                    if (moduleFound && moduleOpen && moduleClose)
                    {
                        // Find module close section
                        if (!module.is64bit) modules_32.Add(module);
                        if (module.is64bit) modules_64.Add(module);
                        moduleFound = false;
                        moduleOpen = false;
                        moduleClose = false;
                    }

                }


            }
            catch (Exception e)
            {
                // Something failed, set the init bool to false
                MessageBox.Show("Error while parsing template: \n" + e.Message);
                return false;
            }

            return true;
        }

    }

}
