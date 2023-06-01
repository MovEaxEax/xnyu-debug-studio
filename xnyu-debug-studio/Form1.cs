using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using gh;
using SharpMonoInjector;
using static gh.ghapi;

namespace xnyu_debug_studio
{
    public partial class Workspace : Form
    {
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int MEM_COMMIT = 0x1000;
        private const int MEM_RESERVE = 0x2000;
        private const int MEM_RELEASE = 0x8000;
        private const int MEM_DECOMMIT = 0x4000;
        private const int PAGE_EXECUTE_READWRITE = 0x40;

        // DLL Imports
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(Keys ArrowKeys);

        // Window names
        public static string applicationVersion = "0.9.0";
        public static string[] applicationVersionNumbers = { "0", "9", "0" };

        public static string xnyu_window_short_name = "NTS v0.9";
        public static string xnyu_window_long_name = "xNyu TAS Studio v0.9";

        // Copies files etc, when opened in visual studio
        public static bool visualStudioMode = false;

        // Template
        public static Template CurrentTemplate = null;

        // xNyu DLL names
        public static string xnyu_tas_dll = "xnyu-debug.dll";

        // Form variables
        public static Thread FormResizer = null;
        public static bool FormExpanded = false;
        public static bool FormExpantionRunning = false;
        public static bool FormExpandTrigger = false;
        public static bool FormShrinkTrigger = false;
        public static bool FormExpandTriggerInit = false;
        public static bool FormShrinkTriggerInit = false;

        public static int[] FormShrinkSize = new int[2] { 259, 172 };
        public static int[] FormExpandSize = new int[2] { 500, 600 };

        public static int[] TitlePositionShrink = new int[2] { FormShrinkSize[0] / 2 - 139, 3 };
        public static int[] TitlePositionExpand = new int[2] { FormExpandSize[0] / 2 - 148, 3 };

        // Server pipe thread
        public static Thread HotkeyThread = null;

        // Buttons GUI
        public static Dictionary<string, InputButton> InputButtons = new Dictionary<string, InputButton>();

        // Folder Structure
        public static List<string> FolderStructure = new List<string>();
        public static string dir_root = Directory.GetCurrentDirectory();
        public static string dir_mods = dir_root + @"\mods";
        public static string dir_templates = dir_root + @"\templates";
        public static string dir_bin = dir_root + @"\bin";
        public static string dir_config = dir_root + @"\config";

        // Bool for axis InputButton
        public bool AxisOnDrag = false;

        // Bool for debugging purposes
        public static bool Dev_Debug = false;

        // Trigger to end the response checker thread
        public static bool ThreadResponseCheckerExit = false;
        public static Thread ThreadResponseChecker = null;

        public static SharedFunctions sharedFunctions;

        // Max tries to retry injection
        public static int injectionAttemptMax = 3;
        public static int initAttemptMax = 10;

        public struct StudioSettings
        {
            public int firstTime;
        };

        public static StudioSettings GlobalSettings = new StudioSettings();

        public static void CreateSettings()
        {
            if (File.Exists(dir_config + @"\settings.cfg")) return;
            string file = dir_config + @"\settings.cfg";
            string[] settings = new string[1] {
                "firstTime=1",
            };
            File.WriteAllLines(file, settings);
        }

        public static void UpdateSettings(string[] entry, string[] value)
        {
            if (entry.Length == 0) return;
            string file = dir_config + @"\settings.cfg";
            string[] settings = File.ReadAllLines(file);
            for (int k = 0; k < entry.Length; k++)
            {
                for (int i = 0; i < settings.Length; i++)
                {
                    if (settings[i].Contains(entry[k]))
                    {
                        settings[i] = entry[k] + "=" + value[k];
                        break;
                    }
                }
            }
            File.WriteAllLines(file, settings);
            ReadSettings(entry, value);
        }

        public static void ReadSettings(string[] entries = null, string[] values = null)
        {
            string[] settings = null;
            if (entries == null && values == null)
            {
                string file = dir_config + @"\settings.cfg";
                settings = File.ReadAllLines(file);
            }
            else
            {
                settings = new string[entries.Length];
                for (int i = 0; i < entries.Length; i++) settings[i] = entries[i] + "=" + values[i];
            }
            for (int k = 0; k < settings.Length; k++)
            {
                string parameter = settings[k].Split('=')[0];
                string value = settings[k].Split('=')[1];

                if (parameter == "firstTime")
                {
                    GlobalSettings.firstTime = int.Parse(value);
                }
            }
        }

        public async Task<string> GetOnlineDataAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    using (var response = await httpClient.GetAsync(url, cts.Token).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return content;
                    }
                }
            }
        }

        public string GetOnlineData(string url)
        {
            try
            {
                return GetOnlineDataAsync(url).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("The request timed out.");
            }
        }

        public Workspace()
        {
            // Form init
            InitializeComponent();

            // Center window to screen
            CenterToScreen();

            //Add folder structure of TAS Studio to list
            FolderStructure.Add(dir_mods);
            FolderStructure.Add(dir_templates);
            FolderStructure.Add(dir_bin);
            FolderStructure.Add(dir_bin + @"\x64");
            FolderStructure.Add(dir_bin + @"\x86");
            FolderStructure.Add(dir_config);
            FolderStructure.Add(dir_config + @"\history");

            // Create folder of TAS Studio structure
            CreateFolderStructure();

            if (visualStudioMode)
            {
                string cD = Directory.GetCurrentDirectory();

                string src64 = cD.Substring(0, cD.IndexOf('x')) + @"xnyu-debug\x64\Debug\xnyu-debug.dll";
                string dst64 = cD + @"\bin\x64\xnyu-debug.dll";
                if (File.Exists(src64)) File.Copy(src64, dst64, true);

                string src86 = cD.Substring(0, cD.IndexOf('x')) + @"xnyu-debug\Debug\xnyu-debug.dll";
                string dst86 = cD + @"\bin\x86\xnyu-debug.dll";
                if (File.Exists(src64)) File.Copy(src86, dst86, true);

                string srcMM = cD.Substring(0, cD.IndexOf('x')) + @"xnyu-debug-studio-mod-manager\xnyu-debug-studio-mod-manager\bin\Debug\xnyu-mod-manager.exe";
                string dstMM = cD + @"\xnyu-mod-manager.exe";
                if (File.Exists(src64)) File.Copy(srcMM, dstMM, true);

                string srcMod = cD.Substring(0, cD.IndexOf('x')) + @"xnyu-game-mods\Kengeki\Debug\kengeki-mod.dll";
                string dstMod = cD + @"\mods\Kengeki\mod\kengeki-mod.dll";
                if (File.Exists(srcMod)) File.Copy(srcMod, dstMod, true);
            }
            else
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    string currentVersion = "http://raw.githubusercontent.com/MovEaxEax/xnyu-debug-studio/main/version.txt";
                    MessageBox.Show("111");
                    string version = GetOnlineData(currentVersion);
                    MessageBox.Show("222");
                    string[] versionNumbers = version.Split('.');
                    bool shouldUpdate = false;
                    if (version != applicationVersion)
                    {
                        for (int i = 0; i < versionNumbers.Length; i++)
                        {
                            if (int.Parse(versionNumbers[i]) > int.Parse(applicationVersionNumbers[i]))
                            {
                                shouldUpdate = true;
                                break;
                            }
                        }
                    }
                    if(shouldUpdate)
                    {
                        DialogResult result = MessageBox.Show("There is a newer verison of the debug studio available to download. Do you wish to update now? (recommended)", "Version v" + version + " is available", MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes)
                        {
                            // Go for the update
                            Process.Start(Directory.GetCurrentDirectory() + @"\xnyu-studio-updater.exe");
                            Thread.Sleep(1000);
                            Application.Exit();
                            Environment.Exit(0);
                        }
                    }
                }
            }

            // Create and read settings
            CreateSettings();
            ReadSettings();

            //Picture Box Transparent
            Play_Button.BackColor = Color.Transparent;
            Inject_Button.BackColor = Color.Transparent;
            Record_Button.BackColor = Color.Transparent;
            Title_Picture.BackColor = Color.Transparent;
            Joystick_Picture.BackColor = Color.Transparent;
            JOYA.BackColor = Color.Transparent;
            JOYB.BackColor = Color.Transparent;
            JOYX.BackColor = Color.Transparent;
            JOYY.BackColor = Color.Transparent;
            JOYLB.BackColor = Color.Transparent;
            JOYRB.BackColor = Color.Transparent;
            JOYUP.BackColor = Color.Transparent;
            JOYDOWN.BackColor = Color.Transparent;
            JOYLEFT.BackColor = Color.Transparent;
            JOYRIGHT.BackColor = Color.Transparent;
            Joystick_LAxis.BackColor = Color.Transparent;

            //Form init
            this.Size = new Size(FormShrinkSize[0], FormShrinkSize[1]);

            //Form Resize Thread
            FormResizer = new Thread(() => {

                int ShrinkXFactor = 0;
                int ShrinkYFactor = 0;
                int ExpandXFactor = 0;
                int ExpandYFactor = 0;

                int ShrinkXDistance = 0;
                int ShrinkYDistance = 0;
                int ExpandXDistance = 0;
                int ExpandYDistance = 0;

                int ShrinkXFactorTitle = 0;
                int ShrinkYFactorTitle = 0;
                int ExpandXFactorTitle = 0;
                int ExpandYFactorTitle = 0;

                int ShrinkXDistanceTitle = 0;
                int ShrinkYDistanceTitle = 0;
                int ExpandXDistanceTitle = 0;
                int ExpandYDistanceTitle = 0;

            while (true)
                {
                    if (FormShrinkTrigger)
                    {
                        //Shrink the form
                        if (FormShrinkTriggerInit)
                        {
                            //Shrink init
                            ShrinkXDistance = Math.Abs(this.Size.Width - FormShrinkSize[0]);
                            ShrinkYDistance = Math.Abs(this.Size.Height - FormShrinkSize[1]);
                            ShrinkXFactor = (int)Math.Floor((decimal)ShrinkXDistance / 100);
                            ShrinkYFactor = (int)Math.Floor((decimal)ShrinkYDistance / 100);

                            ShrinkXDistanceTitle = Math.Abs(Title_Picture.Location.X - TitlePositionShrink[0]);
                            ShrinkYDistanceTitle = Math.Abs(Title_Picture.Location.Y - TitlePositionShrink[1]);
                            ShrinkXFactorTitle = (int)Math.Floor((decimal)ShrinkXDistanceTitle / 100);
                            ShrinkYFactorTitle = (int)Math.Floor((decimal)ShrinkYDistanceTitle / 100);

                            FormShrinkTriggerInit = false;
                            FormExpantionRunning = true;
                        }

                        if (this.Size.Width < FormShrinkSize[0] + ShrinkXFactor + 5)
                        {
                            //Shrink is done
                            this.BeginInvoke(new MethodInvoker(delegate ()
                            {
                                this.Size = new Size(FormShrinkSize[0], FormShrinkSize[1]);
                            }));
                            this.BeginInvoke(new MethodInvoker(delegate ()
                            {
                                Title_Picture.Location = new Point(TitlePositionShrink[0], TitlePositionShrink[1]);
                            }));
                            this.BeginInvoke(new MethodInvoker(delegate ()
                            {
                                this.Text = xnyu_window_short_name;
                            }));

                            FormShrinkTrigger = false;
                            FormExpantionRunning = false;
                            FormExpanded = false;
                        }
                        else
                        {
                            //Resize window
                            this.BeginInvoke(new MethodInvoker(delegate () {
                                this.Size = new Size(this.Size.Width - ShrinkXFactor, this.Size.Height - ShrinkYFactor);
                            }));
                            this.BeginInvoke(new MethodInvoker(delegate () {
                                Title_Picture.Location = new Point(Title_Picture.Location.X - ShrinkXFactorTitle, Title_Picture.Location.Y - ShrinkYFactorTitle);
                            }));

                            ShrinkXFactor = (int)Math.Ceiling((decimal)ShrinkXFactor * (decimal)1.45);
                            ShrinkYFactor = (int)Math.Ceiling((decimal)ShrinkYFactor * (decimal)1.45);
                            ShrinkXFactorTitle = (int)Math.Ceiling((decimal)ShrinkXFactorTitle * (decimal)1.3);
                            ShrinkYFactorTitle = (int)Math.Ceiling((decimal)ShrinkYFactorTitle * (decimal)1.3);
                        }

                    }
                    else if (FormExpandTrigger)
                    {
                        //Expand the form
                        if (FormExpandTriggerInit)
                        {
                            //Expand init
                            ExpandXDistance = Math.Abs(this.Size.Width - FormExpandSize[0]);
                            ExpandYDistance = Math.Abs(this.Size.Height - FormExpandSize[1]);
                            ExpandXFactor = (int)Math.Floor((decimal)ExpandXDistance / 100);
                            ExpandYFactor = (int)Math.Floor((decimal)ExpandYDistance / 100);

                            ExpandXDistanceTitle = Math.Abs(Title_Picture.Location.X - TitlePositionExpand[0]);
                            ExpandYDistanceTitle = Math.Abs(Title_Picture.Location.Y - TitlePositionExpand[1]);
                            ExpandXFactorTitle = (int)Math.Floor((decimal)ExpandXDistanceTitle / 100);
                            ExpandYFactorTitle = (int)Math.Floor((decimal)ExpandYDistanceTitle / 100);

                            FormExpandTriggerInit = false;
                            FormExpantionRunning = true;
                        }

                        if (this.Size.Width > FormExpandSize[0] - ExpandXFactor - 5)
                        {
                            //Expand is done
                            this.BeginInvoke(new MethodInvoker(delegate ()
                            {
                                this.Size = new Size(FormExpandSize[0], FormExpandSize[1]);
                            }));
                            this.BeginInvoke(new MethodInvoker(delegate ()
                            {
                                Title_Picture.Location = new Point(TitlePositionExpand[0], TitlePositionExpand[1]);
                            }));
                            this.BeginInvoke(new MethodInvoker(delegate ()
                            {
                                this.Text = xnyu_window_long_name;
                            }));

                            FormExpandTrigger = false;
                            FormExpantionRunning = false;
                            FormExpanded = true;
                        }
                        else
                        {
                            //Resize window
                            this.BeginInvoke(new MethodInvoker(delegate () {
                                this.Size = new Size(this.Size.Width + ExpandXFactor, this.Size.Height + ExpandYFactor);
                            }));
                            this.BeginInvoke(new MethodInvoker(delegate () {
                                Title_Picture.Location = new Point(Title_Picture.Location.X + ExpandXFactorTitle, Title_Picture.Location.Y + ExpandYFactorTitle);
                            }));

                            ExpandXFactor = (int)Math.Ceiling((decimal)ExpandXFactor * (decimal)1.45);
                            ExpandYFactor = (int)Math.Ceiling((decimal)ExpandYFactor * (decimal)1.45);
                            ExpandXFactorTitle = (int)Math.Ceiling((decimal)ExpandXFactorTitle * (decimal)1.3);
                            ExpandYFactorTitle = (int)Math.Ceiling((decimal)ExpandYFactorTitle * (decimal)1.3);
                        }
                    }

                    Thread.Sleep(25);
                }

            });

            FormResizer.Start();

            if (GlobalSettings.firstTime == 1)
            {
                UpdateSettings(new string[] { "firstTime" }, new string[] { "0" });
                MessageBox.Show("Welcome to the xNyu Debug Studio! To get started you need a template of the game you want to debug.\n" +
                                "Go on the github repository to gain additional information how to do.\n" +
                                "If you want to search for an available mod online, you can use the textfield and the \"Download Mod\" button");
            }
        }

        public static void UpdateSettings()
        {
            string file = Directory.GetCurrentDirectory() + @"\settings.cfg";
            string[] settings = new string[2] { "", "" };
            File.WriteAllLines(file, settings);
        }


        private void Play_Button_Hover(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);
            if (me.Tag.ToString() == "Pause")
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_hover;
            }
            else
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.play_hover;
            }
        }

        private void Play_Button_UnHover(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);
            if (me.Tag.ToString() == "Pause")
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_normal;
            }
            else
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.play_normal;
            }
        }

        private void Play_Button_Click(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);

            //Unset Focus
            UnsetFocus();

            if (me.Tag.ToString() == "Pause")
            {
                sharedFunctions.playScriptTAS("");

                // Enable record button
                //EnableRecordButton();

                // Change Play button icon to play symbol
                //EnablePlayButton();

                // Enable Eject Button
                //EnableInjectButton();

                // Set target window to foreground
                SetForegroundWindow(CurrentTemplate.window);
            }
            else
            {
                // Check for script selected
                if (Box_Script.Items.Count > 0)
                {
                    if (Box_Script.SelectedIndex > -1)
                    {
                        // Set target window to foreground
                        SetForegroundWindow(CurrentTemplate.window);

                        // Additional wait
                        Thread.Sleep(int.Parse(CurrentTemplate.target_module.config_tas_delay));

                        // Get item text
                        string comboItem = Box_Script.Items[Box_Script.SelectedIndex].ToString();

                        string playScriptTASParams = comboItem + ";";
                        sharedFunctions.playScriptTAS(playScriptTASParams);

                        // Disable record button
                        //DisableRecordButton();

                        // Disable Eject Button
                        //DisableInjectButton();

                        //Change Me
                        //me.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_hover;
                        //me.Tag = "Pause";
                    }
                }

            }
        }

        private void Record_Button_Hover(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);
            if (me.Tag.ToString() == "Stop")
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_hover;
            }
            else
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.record_hover;
            }
        }

        private void Record_Button_UnHover(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);
            if (me.Tag.ToString() == "Stop")
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_normal;
            }
            else
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.record_normal;
            }
        }

        private void Record_Button_Click(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);

            //Unset Focus
            UnsetFocus();

            if (me.Tag.ToString() == "Stop")
            {
                sharedFunctions.recordScriptTAS("");

                // Enabled play button
                //EnablePlayButton();

                // Reset record button
                //EnableRecordButton();

                // Enable Eject Button
                //EnableInjectButton();

                // Set target window to foreground
                SetForegroundWindow(CurrentTemplate.window);
            }
            else
            {
                // Set target window to foreground
                SetForegroundWindow(CurrentTemplate.window);

                // Additional wait
                Thread.Sleep(int.Parse(CurrentTemplate.target_module.config_tas_delay));

                // Get the record script name
                string TASRecordScriptName = Textbox_Record_Name.Text;

                if(string.IsNullOrWhiteSpace(TASRecordScriptName))
                {
                    TASRecordScriptName = "Script_" + DateTime.Now.ToString("yyyy-MM-dd") + "_" + DateTime.Now.ToString("H-mm-ss");
                }

                if (TASRecordScriptName.Length > 3)
                {
                    if (TASRecordScriptName.Substring(TASRecordScriptName.Length - 4, 4) != ".nts")
                    {
                        TASRecordScriptName = TASRecordScriptName + ".nts";
                    }
                }
                else
                {
                    TASRecordScriptName = TASRecordScriptName + ".nts";
                }

                string recordScriptTASParams = TASRecordScriptName + ";";
                sharedFunctions.recordScriptTAS(recordScriptTASParams);

                // Disable play button
                //DisablePlayButton();

                // Disable Eject Button
                //DisableInjectButton();

                // Change to pause icon
                //me.Tag = "Stop";
                //me.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_hover;
            }
        }

        private void Inject_Button_Hover(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);
            if (me.Tag.ToString() == "Eject")
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.eject_hover;
            }
            else
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.inject_hover;
            }
        }

        private void Inject_Button_UnHover(object sender, System.EventArgs e)
        {
            PictureBox me = (sender as PictureBox);
            if (me.Tag.ToString() == "Eject")
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.eject_normal;
            }
            else
            {
                me.BackgroundImage = xnyu_debug_studio.Properties.Resources.inject_normal;
            }
        }

        private void Inject_Button_Click(object sender, System.EventArgs e)
        {
            try
            {
                PictureBox me = (sender as PictureBox);

                //Unset Focus
                UnsetFocus();

                if (me.Tag.ToString() == "Eject")
                {
                    if (Play_Button.Tag.ToString() != "Pause" && Record_Button.Tag.ToString() != "Stop")
                    {
                        // Reset InputButtons selected
                        ResetInputButtonSelection();

                        // Reset variables
                        ResetVariables();

                        // Disable the lower Controls
                        DisableDownControls();

                        ThreadResponseCheckerExit = true;

                        Thread.Sleep(600);

                        sharedFunctions.ejectDebugger("");

                        me.Tag = "Inject";
                        me.BackgroundImage = xnyu_debug_studio.Properties.Resources.inject_hover;

                        // Shrink the form
                        ShrinkForm();
                    }
                    else
                    {
                        MessageBox.Show("Can't eject when recording or playing a script");
                    }

                }
                else
                {
                    if (Box_Templates.Items.Count > 0)
                    {
                        if (Box_Templates.SelectedIndex > -1)
                        {
                            // Error buffer
                            string error = "";

                            // Get selected template file path
                            string template_filename = dir_templates + "\\" + Box_Templates.SelectedItem.ToString();

                            Progressbar_Inject.Minimum = 0;
                            Progressbar_Inject.Maximum = 6;
                            Progressbar_Inject.Value = 1;
                            Progressbar_Inject.Step = 1;
                            Progressbar_Inject.Visible = true;

                            // Create new template object for selected template
                            CurrentTemplate = new Template();
                            if(!CurrentTemplate.ParseTemplateSettings(template_filename))
                            {
                                MessageBox.Show("Error occured while parsing the template. There is probably a syntax conflict.");
                                Progressbar_Inject.Visible = false;
                                return;
                            }
                            Progressbar_Inject.PerformStep();

                            CurrentTemplate.process = CurrentTemplate.FindTargetProcess();
                            if (CurrentTemplate.process == null)
                            {
                                MessageBox.Show("No target process could be found. Check your settings in your template, also make sure the process is running.");
                                Progressbar_Inject.Visible = false;
                                return;
                            }

                            CurrentTemplate.window = CurrentTemplate.process.MainWindowHandle;
                            Progressbar_Inject.PerformStep();

                            error = CurrentTemplate.CheckTemplateParsing();
                            if (error != "")
                            {
                                MessageBox.Show(error);
                                Progressbar_Inject.Visible = false;
                                return;
                            }
                            Progressbar_Inject.PerformStep();

                            error = CurrentTemplate.CheckTemplateConfig();
                            if (error != "")
                            {
                                MessageBox.Show(error);
                                Progressbar_Inject.Visible = false;
                                return;
                            }
                            Progressbar_Inject.PerformStep();

                            if (CurrentTemplate.target_module.config_anticheat_file != "" && CurrentTemplate.target_module.config_anticheat_file != null)
                            {
                                string file = CurrentTemplate.target_module.config_anticheat_file;
                                if (file.Substring(file.Length - 4, 4).ToLower() == ".exe")
                                {
                                    // Run anti-cheat exe
                                    Process ac_result = Process.Start(file);
                                    int seconds = 0;
                                    while(true)
                                    {
                                        if (ac_result.IsRunning() || seconds > 3000)
                                        {
                                            MessageBox.Show("Anti-Anti-Cheat failed starting.");
                                            break;
                                        }
                                        Thread.Sleep(1);
                                        seconds++;
                                    }
                                }
                                if (file.Substring(file.Length - 4, 4).ToLower() == ".dll")
                                {
                                    // Inject anti-cheat dll
                                    // Check if process is still running
                                    if (CurrentTemplate.process.IsRunning())
                                    {
                                        // Inject
                                        bool ac_result = ghapi.InjectDLL(file, CurrentTemplate.process.ProcessName);
                                        Thread.Sleep(500);
                                        if (!ac_result) MessageBox.Show("Anti-Anti-Cheat failed injection.");
                                    }
                                }
                                if (file.Substring(file.Length - 4, 4).ToLower() == ".sys")
                                {
                                    // Inject anti-cheat sys
                                }
                            }
                            Progressbar_Inject.PerformStep();

                            // Check if TAS is injected
                            bool tas_injected = false;
                            string xnyu_tas_dll_path = dir_root + @"\bin\" + (CurrentTemplate.is64bit ? @"x64\" : @"x86\") + "xnyu-debug.dll";

                            for (int initAttempt = 0; initAttempt < initAttemptMax; initAttempt++)
                            {
                                if (initAttempt > 0) Thread.Sleep(500 + (initAttempt * 50));

                                for (int i_try = 0; i_try < injectionAttemptMax; i_try++)
                                {
                                    // Check if process is still running
                                    if (CurrentTemplate.process.IsRunning())
                                    {
                                        // Inject
                                        tas_injected = ghapi.InjectDLL(xnyu_tas_dll_path, CurrentTemplate.process.ProcessName);
                                        Thread.Sleep(350 + (i_try * 100));

                                        if (tas_injected)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            Thread.Sleep(350);
                                        }
                                    }
                                }

                                // Check injection worked fine
                                if (tas_injected)
                                {
                                    // Check if process is still running
                                    if (CurrentTemplate.process.IsRunning())
                                    {
                                        // Initialize shared functions
                                        sharedFunctions = new SharedFunctions(CurrentTemplate.process, xnyu_tas_dll_path);

                                        // Set parameter
                                        string initDebuggerParameter = "";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.root_dir + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + dir_config + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_script_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_working_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_log_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_debugmod_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_debugfunction_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_debugaddress_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_inputmapping_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_savefile_directory + "\\;";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_debugconfig_directory + "\\;";

                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_mousedriver_set + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_mousedriver_get + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_keyboarddriver_set + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_keyboarddriver_get + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_joystickdriver_set + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_joystickdriver_get + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_graphicdriver + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_d3d9_hook + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_rawinput_demand + ";";

                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_modname + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_processname + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_version + ";";

                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_tashook + ";";

                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_frame_skip + ";";
                                        initDebuggerParameter = initDebuggerParameter + CurrentTemplate.target_module.config_tas_delay + ";";

                                        // Call init in debugger
                                        SetForegroundWindow(CurrentTemplate.window);
                                        Thread.Sleep(450 + (100 * (initAttempt + 1)));
                                        int initResult = initResult = sharedFunctions.initDebugger(initDebuggerParameter);
                                        Thread.Sleep(600);

                                        if (initResult == 0)
                                        {
                                            Progressbar_Inject.PerformStep();

                                            ThreadResponseCheckerExit = false;
                                            ThreadResponseChecker = DebugResponseThread();
                                            ThreadResponseChecker.Start();

                                            // Init input buttons
                                            InputButtons = InitInputButtons(CurrentTemplate.target_module.config_inputmapping_directory);

                                            // Increase the Progressbar
                                            Progressbar_Inject.PerformStep();

                                            // Hide progressbar
                                            Progressbar_Inject.Visible = false;

                                            // Enable the lower Controls
                                            EnableDownControls();

                                            // Expand the form
                                            ExpandForm();

                                            // Change Inject Button
                                            Inject_Button.Tag = "Eject";
                                            Inject_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.eject_normal;
                                            break;
                                        }
                                        else
                                        {
                                            if (initAttempt + 1 >= initAttemptMax)
                                            {
                                                Thread.Sleep(100);
                                                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                                                if (initResult == 2) MessageBox.Show("Error occured: Graphic hook wasn't working was expected. Try to inject again and make sure the graphic engine declared in the template is correct.");
                                                else MessageBox.Show("Error occured: Unkown Error occurred in target application when initializing the debug. Try again to inject and check your template settings.");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (initAttempt + 1 >= initAttemptMax) MessageBox.Show("Error occured: Target process isn't running anymore! Probably an anti-cheat was triggered, you can program a bypass in .dll or .exe and add it to your template, so it get executed before the main injection process (Set module_type to 'anticheat)");
                                    }
                                }
                                else
                                {
                                    if (initAttempt + 1 >= initAttemptMax) MessageBox.Show("Error occured: The main injection failed! Probably an anti-cheat was triggered, you can program a bypass in .dll or .exe and add it to your template, so it get executed before the main injection process (Set module_type to 'anticheat)");
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Select a template to use first");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Select a template to use first");
                    }

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured:\n" + ex.Message + "\n" + ex.StackTrace);
            }

            // Hide Progrssbar
            Progressbar_Inject.Visible = false;
        }



        private void Box_Process_Fokus(object sender, System.EventArgs e)
        {
            ComboBox me = (sender as ComboBox);

            //Remove items
            if (me.Items.Count > 0) me.Items.Clear();

            List<string> template_files = Directory.GetFiles(dir_templates, "*.ntt").ToList<string>();
            template_files.Sort();
            foreach (string template_file in template_files) me.Items.Add(template_file.Split('\\')[template_file.Split('\\').Length - 1]);
        }

        private void Box_Script_Focus(object sender, System.EventArgs e)
        {
            ComboBox me = (sender as ComboBox);

            //Remove items
            if (me.Items.Count > 0) me.Items.Clear();

            List<string> script_files = Directory.GetFiles(CurrentTemplate.target_module.config_script_directory, "*.nts").ToList<string>();
            script_files.Sort();
            foreach (string script_file in script_files) me.Items.Add(script_file.Split('\\')[script_file.Split('\\').Length - 1]);
        }


        private void Form_FormClosing(Object sender, FormClosingEventArgs e)
        {
            if (ThreadResponseChecker != null)
            {
                if (ThreadResponseChecker.IsAlive)
                {
                    ThreadResponseChecker.Abort();
                    Thread.Sleep(250);
                }
            }

            if (FormResizer != null)
            {
                if (FormResizer.IsAlive)
                {
                    FormResizer.Abort();
                    Thread.Sleep(250);
                }
            }

            Environment.Exit(0);
        }

        // Send frame to TAS
        private void AddFrame(object sender, EventArgs e)
        {
            if (Record_Button.Tag.ToString() == "Stop")
            {
                SetForegroundWindow(CurrentTemplate.window);

                Thread.Sleep(int.Parse(CurrentTemplate.target_module.config_tas_delay));

                string TASFrameToSend = Frame_Textbox.Text;

                if (string.IsNullOrEmpty(TASFrameToSend) || TASFrameToSend.Length < 7)
                {
                    TASFrameToSend = "frame { }";
                }

                TASFrameToSend = TASFrameToSend.Replace(" ", "");

                string receiveFrameTASParams = TASFrameToSend + ";END;";
                sharedFunctions.receiveFrameTAS(receiveFrameTASParams);
            }
        }

        private void Frame_Checkbox_Checked(object sender, EventArgs e)
        {
            string enableFrameByFrameTASParams = Frame_Checkbox.Checked ? "1;" : "0;";
            sharedFunctions.enableFrameByFrameTAS(enableFrameByFrameTASParams);
        }

        private void PlayToStop_Checkbox_Checked(object sender, EventArgs e)
        {
            string playToRecordTASParams = PlayToStop_Checkbbox.Checked ? "1;" : "0;";
            sharedFunctions.playToRecordTAS(playToRecordTASParams);
        }

        private void Active_Checkbox_Checked(object sender, EventArgs e)
        {
            string windowStayActiveParams = checkbox_active.Checked ? "1;" : "0;";
            sharedFunctions.windowStayActive(windowStayActiveParams);
        }

        private void Console_Checkbox_Checked(object sender, EventArgs e)
        {
            string toggleDevConsoleParams = checkbox_console.Checked ? "1;" : "0;";
            sharedFunctions.toggleDevConsole(toggleDevConsoleParams);
        }

        private void Dev_Checkbox_Checked(object sender, EventArgs e)
        {
            string toggleDevModeParams = checkbox_devmode.Checked ? "1;" : "0;";
            sharedFunctions.toggleDevMode(toggleDevModeParams);
        }

        private void Overclock_Checkbox_Checked(object sender, EventArgs e)
        {
            string toggleOverclockParams = checkbox_devmode.Checked ? "1;" : "0;";
            sharedFunctions.toggleOverclock(toggleOverclockParams);
        }

        private void checkbox_togglemouse_Click(object sender, EventArgs e)
        {
            string toggleTASIgnoreMouseModeParams = checkbox_devmode.Checked ? "1;" : "0;";
            sharedFunctions.toggleTASIgnoreMouse(toggleTASIgnoreMouseModeParams);
        }

        public void UpdateFrameTextbox()
        {
            // Frame construction
            string frame = "frame{ ";

            // Detect selected buttons
            for (int i = 0; i < InputButtons.Count; i++)
            {
                // Get InputButtons iterative
                InputButton button = InputButtons.ElementAt(i).Value;

                // Add button command to the current frame
                if (button.selected && (button.id < 84 || button.id > 87)) frame = frame + button.command + "(); ";
            }

            // LAxis special case
            if (InputButtons["JOYLAXISX"].selected) {
                int L_axis_x = (Joystick_LAxis.Left - int.Parse(Joystick_LAxis.Tag.ToString().Split(';')[0])) * 100;
                int L_axis_y = (Joystick_LAxis.Top - int.Parse(Joystick_LAxis.Tag.ToString().Split(';')[1])) * 100;
                frame = frame + InputButtons["JOYLAXISX"].command + "(" + L_axis_x.ToString() + "); ";
                frame = frame + InputButtons["JOYLAXISY"].command + "(" + L_axis_y.ToString() + "); ";
            }

            // RAxis special case
            if (InputButtons["JOYRAXISX"].selected)
            {
                int R_axis_x = (Joystick_RAxis.Left - int.Parse(Joystick_RAxis.Tag.ToString().Split(';')[0])) * 100;
                int R_axis_y = (Joystick_RAxis.Top - int.Parse(Joystick_RAxis.Tag.ToString().Split(';')[1])) * 100;
                frame = frame + InputButtons["JOYRAXISX"].command + "(" + R_axis_x.ToString() + "); ";
                frame = frame + InputButtons["JOYRAXISY"].command + "(" + R_axis_y.ToString() + "); ";
            }

            // Finish frame construction
            frame = frame + "}";

            Frame_Textbox.Text = frame;
        }

        private void Input_Button_Click(object sender, System.EventArgs e)
        {
            PictureBox me = (PictureBox)(sender as PictureBox);

            string resource_name = me.Name;

            if (!resource_name.Contains("Axis"))
            {
                // Unset Focus
                UnsetFocus();

                InputButtons[resource_name].selected = !InputButtons[resource_name].selected;
                me.BackgroundImage = (Bitmap)xnyu_debug_studio.Properties.Resources.ResourceManager.GetObject(resource_name + (InputButtons[resource_name].selected ? "_Selected" : ""));

                UpdateFrameTextbox();
            }

        }

        public Dictionary<string, InputButton> InitInputButtons(string mapping_path)
        {
            // Load InputButton mapping and set their selection bool to false
            string inputMappingFile = mapping_path + @"\InputMapping.ini";

            Dictionary<string, InputButton> InputButtonsTmp = new Dictionary<string, InputButton>();

            List<string> inputMapping = File.ReadAllLines(inputMappingFile).ToList<string>();
            InputButtonsTmp.Add("ESC", new InputButton(1, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("ESC"))).Replace(" ", "").Replace(";", "").Replace("ESC=", "")));
            InputButtonsTmp.Add("D1", new InputButton(2, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D1"))).Replace(" ", "").Replace(";", "").Replace("D1=", "")));
            InputButtonsTmp.Add("D2", new InputButton(3, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D2"))).Replace(" ", "").Replace(";", "").Replace("D2=", "")));
            InputButtonsTmp.Add("D3", new InputButton(4, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D3"))).Replace(" ", "").Replace(";", "").Replace("D3=", "")));
            InputButtonsTmp.Add("D4", new InputButton(5, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D4"))).Replace(" ", "").Replace(";", "").Replace("D4=", "")));
            InputButtonsTmp.Add("D5", new InputButton(6, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D5"))).Replace(" ", "").Replace(";", "").Replace("D5=", "")));
            InputButtonsTmp.Add("D6", new InputButton(7, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D6"))).Replace(" ", "").Replace(";", "").Replace("D6=", "")));
            InputButtonsTmp.Add("D7", new InputButton(8, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D7"))).Replace(" ", "").Replace(";", "").Replace("D7=", "")));
            InputButtonsTmp.Add("D8", new InputButton(9, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D8"))).Replace(" ", "").Replace(";", "").Replace("D8=", "")));
            InputButtonsTmp.Add("D9", new InputButton(10, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D9"))).Replace(" ", "").Replace(";", "").Replace("D9=", "")));
            InputButtonsTmp.Add("D0", new InputButton(11, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D0"))).Replace(" ", "").Replace(";", "").Replace("D0=", "")));
            InputButtonsTmp.Add("BACK", new InputButton(14, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("BACK"))).Replace(" ", "").Replace(";", "").Replace("BACK=", "")));
            InputButtonsTmp.Add("TAB", new InputButton(15, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("TAB"))).Replace(" ", "").Replace(";", "").Replace("TAB=", "")));
            InputButtonsTmp.Add("Q", new InputButton(16, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("Q"))).Replace(" ", "").Replace(";", "").Replace("Q=", "")));
            InputButtonsTmp.Add("W", new InputButton(17, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("W"))).Replace(" ", "").Replace(";", "").Replace("W=", "")));
            InputButtonsTmp.Add("E", new InputButton(18, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("E"))).Replace(" ", "").Replace(";", "").Replace("E=", "")));
            InputButtonsTmp.Add("R", new InputButton(19, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("R"))).Replace(" ", "").Replace(";", "").Replace("R=", "")));
            InputButtonsTmp.Add("T", new InputButton(20, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("T"))).Replace(" ", "").Replace(";", "").Replace("T=", "")));
            InputButtonsTmp.Add("Y", new InputButton(21, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("Y"))).Replace(" ", "").Replace(";", "").Replace("Y=", "")));
            InputButtonsTmp.Add("U", new InputButton(22, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("U"))).Replace(" ", "").Replace(";", "").Replace("U=", "")));
            InputButtonsTmp.Add("I", new InputButton(23, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("I"))).Replace(" ", "").Replace(";", "").Replace("I=", "")));
            InputButtonsTmp.Add("O", new InputButton(24, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("O"))).Replace(" ", "").Replace(";", "").Replace("O=", "")));
            InputButtonsTmp.Add("P", new InputButton(25, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("P"))).Replace(" ", "").Replace(";", "").Replace("P=", "")));
            InputButtonsTmp.Add("RETURN", new InputButton(28, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("RETURN"))).Replace(" ", "").Replace(";", "").Replace("RETURN=", "")));
            InputButtonsTmp.Add("CTRL", new InputButton(29, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("CTRL"))).Replace(" ", "").Replace(";", "").Replace("CTRL=", "")));
            InputButtonsTmp.Add("ALT", new InputButton(29, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("ALT"))).Replace(" ", "").Replace(";", "").Replace("ALT=", "")));
            InputButtonsTmp.Add("A", new InputButton(30, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("A"))).Replace(" ", "").Replace(";", "").Replace("A=", "")));
            InputButtonsTmp.Add("S", new InputButton(31, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("S"))).Replace(" ", "").Replace(";", "").Replace("S=", "")));
            InputButtonsTmp.Add("D", new InputButton(32, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("D"))).Replace(" ", "").Replace(";", "").Replace("D=", "")));
            InputButtonsTmp.Add("F", new InputButton(33, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F"))).Replace(" ", "").Replace(";", "").Replace("F=", "")));
            InputButtonsTmp.Add("G", new InputButton(34, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("G"))).Replace(" ", "").Replace(";", "").Replace("G=", "")));
            InputButtonsTmp.Add("H", new InputButton(35, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("H"))).Replace(" ", "").Replace(";", "").Replace("H=", "")));
            InputButtonsTmp.Add("J", new InputButton(36, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("J"))).Replace(" ", "").Replace(";", "").Replace("J=", "")));
            InputButtonsTmp.Add("K", new InputButton(37, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("K"))).Replace(" ", "").Replace(";", "").Replace("K=", "")));
            InputButtonsTmp.Add("L", new InputButton(38, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("L"))).Replace(" ", "").Replace(";", "").Replace("L=", "")));
            InputButtonsTmp.Add("LSHIFT", new InputButton(42, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("LSHIFT"))).Replace(" ", "").Replace(";", "").Replace("LSHIFT=", "")));
            InputButtonsTmp.Add("RSHIFT", new InputButton(43, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("RSHIFT"))).Replace(" ", "").Replace(";", "").Replace("RSHIFT=", "")));
            InputButtonsTmp.Add("Z", new InputButton(44, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("Z"))).Replace(" ", "").Replace(";", "").Replace("Z=", "")));
            InputButtonsTmp.Add("X", new InputButton(45, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("X"))).Replace(" ", "").Replace(";", "").Replace("X=", "")));
            InputButtonsTmp.Add("C", new InputButton(46, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("C"))).Replace(" ", "").Replace(";", "").Replace("C=", "")));
            InputButtonsTmp.Add("V", new InputButton(47, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("V"))).Replace(" ", "").Replace(";", "").Replace("V=", "")));
            InputButtonsTmp.Add("B", new InputButton(48, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("B"))).Replace(" ", "").Replace(";", "").Replace("B=", "")));
            InputButtonsTmp.Add("N", new InputButton(49, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("N"))).Replace(" ", "").Replace(";", "").Replace("N=", "")));
            InputButtonsTmp.Add("M", new InputButton(50, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("M"))).Replace(" ", "").Replace(";", "").Replace("M=", "")));
            InputButtonsTmp.Add("SPACE", new InputButton(57, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("SPACE"))).Replace(" ", "").Replace(";", "").Replace("SPACE=", "")));
            InputButtonsTmp.Add("AUP", new InputButton(58, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("AUP"))).Replace(" ", "").Replace(";", "").Replace("AUP=", "")));
            InputButtonsTmp.Add("ALEFT", new InputButton(59, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("ALEFT"))).Replace(" ", "").Replace(";", "").Replace("ALEFT=", "")));
            InputButtonsTmp.Add("ARIGHT", new InputButton(60, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("ARIGHT"))).Replace(" ", "").Replace(";", "").Replace("ARIGHT=", "")));
            InputButtonsTmp.Add("ADOWN", new InputButton(61, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("ADOWN"))).Replace(" ", "").Replace(";", "").Replace("ADOWN=", "")));

            InputButtonsTmp.Add("NUM0", new InputButton(62, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM0"))).Replace(" ", "").Replace(";", "").Replace("NUM0=", "")));
            InputButtonsTmp.Add("NUM1", new InputButton(63, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM1"))).Replace(" ", "").Replace(";", "").Replace("NUM1=", "")));
            InputButtonsTmp.Add("NUM2", new InputButton(64, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM2"))).Replace(" ", "").Replace(";", "").Replace("NUM2=", "")));
            InputButtonsTmp.Add("NUM3", new InputButton(65, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM3"))).Replace(" ", "").Replace(";", "").Replace("NUM3=", "")));
            InputButtonsTmp.Add("NUM4", new InputButton(66, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM4"))).Replace(" ", "").Replace(";", "").Replace("NUM4=", "")));
            InputButtonsTmp.Add("NUM5", new InputButton(67, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM5"))).Replace(" ", "").Replace(";", "").Replace("NUM5=", "")));
            InputButtonsTmp.Add("NUM6", new InputButton(68, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM6"))).Replace(" ", "").Replace(";", "").Replace("NUM6=", "")));
            InputButtonsTmp.Add("NUM7", new InputButton(69, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM7"))).Replace(" ", "").Replace(";", "").Replace("NUM7=", "")));
            InputButtonsTmp.Add("NUM8", new InputButton(70, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM8"))).Replace(" ", "").Replace(";", "").Replace("NUM8=", "")));
            InputButtonsTmp.Add("NUM9", new InputButton(71, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("NUM9"))).Replace(" ", "").Replace(";", "").Replace("NUM9=", "")));

            InputButtonsTmp.Add("F1", new InputButton(72, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F1"))).Replace(" ", "").Replace(";", "").Replace("F1=", "")));
            InputButtonsTmp.Add("F2", new InputButton(73, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F2"))).Replace(" ", "").Replace(";", "").Replace("F2=", "")));
            InputButtonsTmp.Add("F3", new InputButton(74, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F3"))).Replace(" ", "").Replace(";", "").Replace("F3=", "")));
            InputButtonsTmp.Add("F4", new InputButton(75, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F4"))).Replace(" ", "").Replace(";", "").Replace("F4=", "")));
            InputButtonsTmp.Add("F5", new InputButton(76, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F5"))).Replace(" ", "").Replace(";", "").Replace("F5=", "")));
            InputButtonsTmp.Add("F6", new InputButton(77, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F6"))).Replace(" ", "").Replace(";", "").Replace("F6=", "")));
            InputButtonsTmp.Add("F7", new InputButton(78, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F7"))).Replace(" ", "").Replace(";", "").Replace("F7=", "")));
            InputButtonsTmp.Add("F8", new InputButton(79, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F8"))).Replace(" ", "").Replace(";", "").Replace("F8=", "")));
            InputButtonsTmp.Add("F9", new InputButton(80, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F9"))).Replace(" ", "").Replace(";", "").Replace("F9=", "")));
            InputButtonsTmp.Add("F10", new InputButton(81, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F10"))).Replace(" ", "").Replace(";", "").Replace("F10=", "")));
            InputButtonsTmp.Add("F11", new InputButton(82, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F11"))).Replace(" ", "").Replace(";", "").Replace("F11=", "")));
            InputButtonsTmp.Add("F12", new InputButton(83, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("F12"))).Replace(" ", "").Replace(";", "").Replace("F12=", "")));

            InputButtonsTmp.Add("JOYLAXISX", new InputButton(84, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYLAXISX"))).Replace(" ", "").Replace(";", "").Replace("JOYLAXISX=", "")));
            InputButtonsTmp.Add("JOYLAXISY", new InputButton(85, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYLAXISY"))).Replace(" ", "").Replace(";", "").Replace("JOYLAXISY=", "")));
            InputButtonsTmp.Add("JOYRAXISX", new InputButton(86, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYRAXISX"))).Replace(" ", "").Replace(";", "").Replace("JOYRAXISX=", "")));
            InputButtonsTmp.Add("JOYRAXISY", new InputButton(87, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYRAXISY"))).Replace(" ", "").Replace(";", "").Replace("JOYRAXISY=", "")));
            InputButtonsTmp.Add("JOYLT", new InputButton(88, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYLT"))).Replace(" ", "").Replace(";", "").Replace("JOYLT=", "")));
            InputButtonsTmp.Add("JOYRT", new InputButton(89, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYRT"))).Replace(" ", "").Replace(";", "").Replace("JOYRT=", "")));
            InputButtonsTmp.Add("JOYUP", new InputButton(90, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYUP"))).Replace(" ", "").Replace(";", "").Replace("JOYUP=", "")));
            InputButtonsTmp.Add("JOYRIGHT", new InputButton(91, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYRIGHT"))).Replace(" ", "").Replace(";", "").Replace("JOYRIGHT=", "")));
            InputButtonsTmp.Add("JOYDOWN", new InputButton(92, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYDOWN"))).Replace(" ", "").Replace(";", "").Replace("JOYDOWN=", "")));
            InputButtonsTmp.Add("JOYLEFT", new InputButton(93, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYLEFT"))).Replace(" ", "").Replace(";", "").Replace("JOYLEFT=", "")));
            InputButtonsTmp.Add("JOYA", new InputButton(94, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYA"))).Replace(" ", "").Replace(";", "").Replace("JOYA=", "")));
            InputButtonsTmp.Add("JOYB", new InputButton(95, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYB"))).Replace(" ", "").Replace(";", "").Replace("JOYB=", "")));
            InputButtonsTmp.Add("JOYX", new InputButton(96, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYX"))).Replace(" ", "").Replace(";", "").Replace("JOYX=", "")));
            InputButtonsTmp.Add("JOYY", new InputButton(97, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYY"))).Replace(" ", "").Replace(";", "").Replace("JOYY=", "")));
            InputButtonsTmp.Add("JOYLB", new InputButton(98, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYLB"))).Replace(" ", "").Replace(";", "").Replace("JOYLB=", "")));
            InputButtonsTmp.Add("JOYRB", new InputButton(99, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYRB"))).Replace(" ", "").Replace(";", "").Replace("JOYRB=", "")));
            InputButtonsTmp.Add("JOYSELECT", new InputButton(100, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYSELECT"))).Replace(" ", "").Replace(";", "").Replace("JOYSELECT=", "")));
            InputButtonsTmp.Add("JOYSTART", new InputButton(101, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("JOYSTART"))).Replace(" ", "").Replace(";", "").Replace("JOYSTART=", "")));

            InputButtonsTmp.Add("LMB", new InputButton(102, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("LMB"))).Replace(" ", "").Replace(";", "").Replace("LMB=", "")));
            InputButtonsTmp.Add("MB", new InputButton(103, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("MB"))).Replace(" ", "").Replace(";", "").Replace("MB=", "")));
            InputButtonsTmp.Add("RMB", new InputButton(104, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("RMB"))).Replace(" ", "").Replace(";", "").Replace("RMB=", "")));
            InputButtonsTmp.Add("ME1", new InputButton(105, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("ME1"))).Replace(" ", "").Replace(";", "").Replace("ME1=", "")));
            InputButtonsTmp.Add("ME2", new InputButton(106, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("ME2"))).Replace(" ", "").Replace(";", "").Replace("ME2=", "")));
            InputButtonsTmp.Add("WHEEL", new InputButton(107, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("WHEEL"))).Replace(" ", "").Replace(";", "").Replace("WHEEL=", "")));
            InputButtonsTmp.Add("MOUSEX", new InputButton(108, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("MOUSEX"))).Replace(" ", "").Replace(";", "").Replace("MOUSEX=", "")));
            InputButtonsTmp.Add("MOUSEY", new InputButton(109, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("MOUSEY"))).Replace(" ", "").Replace(";", "").Replace("MOUSEY=", "")));

            InputButtonsTmp.Add("COMMA", new InputButton(110, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("COMMA"))).Replace(" ", "").Replace(";", "").Replace("COMMA=", "")));
            InputButtonsTmp.Add("DOT", new InputButton(111, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("DOT"))).Replace(" ", "").Replace(";", "").Replace("DOT=", "")));
            InputButtonsTmp.Add("PLUS", new InputButton(112, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("PLUS"))).Replace(" ", "").Replace(";", "").Replace("PLUS=", "")));
            InputButtonsTmp.Add("MINUS", new InputButton(113, inputMapping.Find(s => (s.Substring(0, s.IndexOf("=")).Equals("MINUS"))).Replace(" ", "").Replace(";", "").Replace("MINUS=", "")));

            return InputButtonsTmp;
        }

        public void ResetInputButtonSelection()
        {
            foreach (Control c in this.Controls)
            {
                PictureBox me = null;
                try
                {
                    KeyValuePair<string, InputButton> key = InputButtons.First(k => k.Key == c.Name);
                    string validator = key.Key;
                    me = (PictureBox)c;
                }
                catch (Exception e) { }

                if(me != null)
                {
                    if (!me.Name.Contains("axis", StringComparison.OrdinalIgnoreCase))
                    {
                        if (me.Name == "Joystick_LAxis")
                        {
                            InputButtons["JOYLAXISX"].selected = false;
                            InputButtons["JOYLAXISY"].selected = false;
                            me.BackgroundImage = (Bitmap)xnyu_debug_studio.Properties.Resources.ResourceManager.GetObject("JOYLAXIS");
                        }
                        if (me.Name == "Joystick_RAxis")
                        {
                            InputButtons["JOYRAXISX"].selected = false;
                            InputButtons["JOYRAXISY"].selected = false;
                            me.BackgroundImage = (Bitmap)xnyu_debug_studio.Properties.Resources.ResourceManager.GetObject("JOYLAXIS");
                        }
                    }
                }
            }
        }

        //Mouse location
        private Point CursorPosition;

        private void Joystick_Axis_MouseDown(object sender, MouseEventArgs e)
        {
            AxisOnDrag = true;
            if (e.Button == MouseButtons.Left)
            {
                CursorPosition = new Point(e.Location.X, e.Location.Y);
            }
        }

        private void Joystick_Axis_MouseUp(object sender, MouseEventArgs e)
        {
            // Get the axis element
            PictureBox me = (PictureBox)(sender as PictureBox);

            AxisOnDrag = false;

            if (e.Button == MouseButtons.Left)
            {
                if (int.Parse(me.Tag.ToString().Split(';')[2]) == me.Left && int.Parse(me.Tag.ToString().Split(';')[3]) == me.Top)
                {
                    // Change current selection
                    bool isSelected = false;
                    if (me.Name == "Joystick_LAxis")
                    {
                        InputButtons["JOYLAXISX"].selected = !InputButtons["JOYLAXISX"].selected;
                        InputButtons["JOYLAXISY"].selected = !InputButtons["JOYLAXISY"].selected;
                        isSelected = InputButtons["JOYLAXISX"].selected;
                        me.BackgroundImage = (Bitmap)xnyu_debug_studio.Properties.Resources.ResourceManager.GetObject("JOYLAXIS" + (isSelected ? "_Selected" : ""));
                    }
                    if (me.Name == "Joystick_RAxis")
                    {
                        InputButtons["JOYRAXISX"].selected = !InputButtons["JOYRAXISX"].selected;
                        InputButtons["JOYRAXISY"].selected = !InputButtons["JOYRAXISY"].selected;
                        isSelected = InputButtons["JOYRAXISX"].selected;
                        me.BackgroundImage = (Bitmap)xnyu_debug_studio.Properties.Resources.ResourceManager.GetObject("JOYRAXIS" + (isSelected ? "_Selected" : ""));
                    }
                }
                else
                {
                    me.Tag = me.Tag.ToString().Split(';')[0] + ";" + me.Tag.ToString().Split(';')[1] + ";" + me.Left.ToString() + ";" + me.Top.ToString();
                }
            }
            if (e.Button == MouseButtons.Right)
            {
                me.Left = int.Parse(me.Tag.ToString().Split(';')[0]);
                me.Top = int.Parse(me.Tag.ToString().Split(';')[1]);
            }

            // Unset focus
            UnsetFocus();

            // Update current frame
            UpdateFrameTextbox();
        }

        private void Joystick_Axis_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the axis element
            PictureBox me = (PictureBox)(sender as PictureBox);

            // Update current frame
            UpdateFrameTextbox();

            // Unset focus
            UnsetFocus();

            if (e.Button == MouseButtons.Left)
            {
                int To_X = e.X + me.Left - CursorPosition.X;
                int To_Y = e.Y + me.Top - CursorPosition.Y;

                int StartPaddingX = int.Parse(me.Tag.ToString().Split(';')[0]);
                int StartPaddingY = int.Parse(me.Tag.ToString().Split(';')[1]);

                if (To_X >= StartPaddingX - 10 && To_X <= StartPaddingX + 10) me.Left = To_X;
                if (To_Y >= StartPaddingY - 10 && To_Y <= StartPaddingY + 10) me.Top = To_Y;
            }
        }




        public void ExpandForm()
        {
            // Set the triggers for the thread
            FormExpandTriggerInit = true;
            FormExpandTrigger = true;
        }

        public void ShrinkForm()
        {
            // Set the triggers for the thread
            FormShrinkTriggerInit = true;
            FormShrinkTrigger = true;
        }

        public void EnableDownControls()
        {
            // Disable the template Combobox
            Box_Templates.Enabled = false;

            // Enable the controls on the down side
            Box_Script.Enabled = true;
            Textbox_Record_Name.Enabled = true;
            AddFrame_Button.Enabled = true;
            Frame_Textbox.Enabled = true;
            Frame_Checkbox.Enabled = true;
            PlayToStop_Checkbbox.Enabled = true;
            checkbox_console.Enabled = true;
        }

        public void DisableDownControls()
        {
            // Enable the template Combobox
            Box_Templates.Enabled = true;

            // Disable the controls on the down side
            Box_Script.Enabled = false;
            Textbox_Record_Name.Enabled = false;
            AddFrame_Button.Enabled = false;
            Frame_Textbox.Enabled = false;
            Frame_Checkbox.Enabled = false;
            PlayToStop_Checkbbox.Enabled = false;
            checkbox_console.Enabled = false;
            Play_Button.Tag = "Play";
            Record_Button.Tag = "Record";
        }

        public void ResetVariables()
        {

        }

        public void CreateFolderStructure()
        {
            //Create all essential directories
            foreach (string dir in FolderStructure)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        public void UnsetFocus()
        {
            Unfocus_Button.Focus();
        }

        public void DisableRecordButton()
        {
            Record_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.record_gray;
            Record_Button.Tag = "Record";
            Record_Button.Enabled = false;
            Textbox_Record_Name.Enabled = false;
        }

        public void EnableRecordButton()
        {
            Record_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.record_normal;
            Record_Button.Tag = "Record";
            Record_Button.Enabled = true;
            Textbox_Record_Name.Enabled = true;
        }

        public void DisablePlayButton()
        {
            Play_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.play_gray;
            Play_Button.Tag = "Play";
            Play_Button.Enabled = false;
            Box_Script.Enabled = false;
        }

        public void EnablePlayButton()
        {
            Play_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.play_normal;
            Play_Button.Tag = "Play";
            Play_Button.Enabled = true;
            Box_Script.Enabled = true;
        }

        public void EnableInjectButton()
        {
            Inject_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.eject_normal;
            Inject_Button.Tag = "Eject";
            Inject_Button.Enabled = true;
        }

        public void DisableInjectButton()
        {
            Inject_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.eject_gray;
            Inject_Button.Tag = "Eject";
            Inject_Button.Enabled = false;
        }



        Thread DebugResponseThread()
        {
            return new Thread(() =>
            {

                while(!ThreadResponseCheckerExit)
                {
                    try
                    {
                        bool checkMore = true;

                        int playChecker = 0;
                        int recordChecker = 0;

                        if (checkMore)
                        {
                            playChecker = sharedFunctions.checkIfPlayScriptIsDoneTAS("");
                            if (playChecker == 666)
                            {
                                // Recording script is done
                                this.BeginInvoke(new MethodInvoker(delegate ()
                                {
                                    EnableRecordButton();
                                    EnablePlayButton();
                                    EnableInjectButton();
                                }));
                                checkMore = false;
                            }
                            else if (playChecker == 13)
                            {
                                // Recording script is done
                                this.BeginInvoke(new MethodInvoker(delegate ()
                                {
                                    // Disable play button
                                    DisableRecordButton();

                                    // Disable Eject Button
                                    DisableInjectButton();

                                    // Change to pause icon
                                    Play_Button.Tag = "Pause";
                                    Play_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_normal;
                                }));
                                checkMore = false;
                            }
                        }

                        if (checkMore)
                        {
                            recordChecker = sharedFunctions.checkIfRecordScriptIsDoneTAS("");
                            if (recordChecker == 666)
                            {
                                // Recording script is done
                                this.BeginInvoke(new MethodInvoker(delegate ()
                                {
                                    EnableRecordButton();
                                    EnablePlayButton();
                                    EnableInjectButton();
                                }));
                                checkMore = false;
                            }
                            else if (recordChecker == 13)
                            {
                                this.BeginInvoke(new MethodInvoker(delegate ()
                                {
                                    // Disable play button
                                    DisablePlayButton();

                                    // Disable Eject Button
                                    DisableInjectButton();

                                    // Change to pause icon
                                    Record_Button.Tag = "Stop";
                                    Record_Button.BackgroundImage = xnyu_debug_studio.Properties.Resources.pause_normal;
                                }));
                                checkMore = false;
                            }
                        }

                        Thread.Sleep(300);
                    }
                    catch(Exception e)
                    {
                        // Doing nothing here
                    }
                }

            });
        }



        private void Workspace_Load(object sender, EventArgs e)
        {

        }

    }

}
