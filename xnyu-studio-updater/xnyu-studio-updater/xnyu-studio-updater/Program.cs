using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Policy;

namespace xnyu_studio_updater
{
    internal class Program
    {
        public static void DownloadZipFile(string url, string dst)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, dst);
            }
        }


        public static void ExtractZipFile(string zipPath, string extractPath)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.Combine(extractPath, entry.FullName);
                    var destinationDirectory = Path.GetDirectoryName(destinationPath);

                    if (!entry.FullName.Contains("xnyu-studio-updater"))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                        entry.ExtractToFile(destinationDirectory, overwrite: true);
                    }
                }
            }
        }

        public static string GetHtmlFromUrlRaw(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
                return client.GetStringAsync(url).GetAwaiter().GetResult();
            }
        }

        public static void InstallNewVersion(string sourceDirName, string destDirName, bool copySubDirs = true)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    InstallNewVersion(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        static void Main(string[] args)
        {
            string bitFlag = IntPtr.Size == 8 ? "x64" : "x86";

            int timeout = 0;
            while(timeout < 100)
            {
                Process[] targetProcs = Process.GetProcessesByName("xnyu-debug-studio");
                if (targetProcs.Length > 0)
                {
                    try
                    {
                        targetProcs[0].Close();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    break;
                }
                timeout++;
                Thread.Sleep(100);
            }

            if (timeout >= 100)
            {
                Console.WriteLine("An error occured, update could not start :(");
                Console.WriteLine("Press return to close this dialogue...");
                Console.ReadLine();
                Environment.Exit(0);
            }

            Thread.Sleep(5000);

            string currentVersion = "http://raw.githubusercontent.com/MovEaxEax/xnyu-debug-studio/main/version.txt";
            string version = GetHtmlFromUrlRaw(currentVersion);
            string release = "http://raw.githubusercontent.com/MovEaxEax/xnyu-debug-studio/main/builds/xnyu-debug-studio_v" + version + ".zip";

            const string chars = "0123456789";
            var random = new Random();
            string tmpDir = Directory.GetCurrentDirectory() + @"\tmp_" + new string(Enumerable.Repeat(chars, 1).SelectMany(s => s).OrderBy(_ => random.Next()).ToArray());
            string zipTarget = tmpDir + @"\update.zip";

            // Create tmp directory and save the update zip in there
            Directory.CreateDirectory(tmpDir);
            DownloadZipFile(release, zipTarget);

            // Update the files
            //ExtractZipFile(zipTarget, Directory.GetCurrentDirectory());
            ZipFile.ExtractToDirectory(zipTarget, tmpDir);

            File.Delete(zipTarget);

            string oldUpdater = tmpDir + @"\updater\" + bitFlag + @"\xnyu-studio-updater.exe";
            string newUpdater = tmpDir + @"\updater\" + bitFlag + @"\xnyu-studio-updater_new.exe";
            File.Move(oldUpdater, newUpdater);

            //string targetDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
            string targetDirectory = Directory.GetCurrentDirectory();
            InstallNewVersion(tmpDir, targetDirectory);

            // Delete tmp files and directory again
            Directory.Delete(tmpDir, true);

            Process.Start(targetDirectory + @"\xnyu-debug-studio-" + bitFlag + ".exe");
            Thread.Sleep(1000);
            Environment.Exit(0);
        }
    }
}
