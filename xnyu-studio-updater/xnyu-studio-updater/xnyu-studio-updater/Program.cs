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

namespace xnyu_studio_updater
{
    internal class Program
    {
        public static async Task<string> GetOnlineDataAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
            }
        }

        public static async Task<byte[]> GetOnlineDataBytesAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsByteArrayAsync();
                    return content;
                }
            }
        }

        public static string GetOnlineData(string url)
        {
            return GetOnlineDataAsync(url).GetAwaiter().GetResult();
        }

        public static byte[] DownloadFile(string url)
        {
            return GetOnlineDataBytesAsync(url).GetAwaiter().GetResult();
        }

        public static void ExtractZipFile(string zipPath, string extractPath)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.Combine(extractPath, entry.FullName);
                    var destinationDirectory = Path.GetDirectoryName(destinationPath);

                    if (!destinationPath.Contains("xnyu-studio-updater"))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            int timeout = 0;
            while(timeout < 50)
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

            if (timeout >= 50)
            {
                Console.WriteLine("An error occured, update could not start :(");
                Console.WriteLine("Press return to close this dialogue...");
                Console.ReadLine();
                Environment.Exit(0);
            }

            string currentVersion = "http://raw.githubusercontent.com/MovEaxEax/xnyu-debug-studio/main/version.txt";
            string version = GetOnlineData(currentVersion);
            string release = "http://raw.githubusercontent.com/MovEaxEax/xnyu-debug-studio/main/xnyu-debug-studio_" + version + ".zip";
            byte[] zipFileContent = DownloadFile(release);

            const string chars = "0123456789";
            var random = new Random();
            string tmpDir = Directory.GetCurrentDirectory() + @"\" + new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
            string zipTarget = tmpDir + @"\update.zip";

            // Create tmp directory and save the update zip in there
            Directory.CreateDirectory(tmpDir);
            File.WriteAllBytes(zipTarget, zipFileContent);

            // Update the files
            ExtractZipFile(Directory.GetCurrentDirectory(), zipTarget);

            // Delete tmp files and directory again
            File.Delete(zipTarget);
            Directory.Delete(tmpDir);

            string bitFlag = IntPtr.Size == 8 ? "x64" : "x86";

            Process.Start(Directory.GetCurrentDirectory() + @"\xnyu-debug-studio-" + bitFlag + ".exe");
            Thread.Sleep(1000);
            Environment.Exit(0);
        }
    }
}
