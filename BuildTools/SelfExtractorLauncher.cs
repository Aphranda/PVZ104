using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

namespace PVZ104.ReleaseTools
{
    internal static class SelfExtractorLauncher
    {
        private const string ResourceName = "PayloadZip";
        private const string AppFolderName = "PVZ104_RZDemoWpf";
        private const string AppExeName = "RZDemoWpf.exe";

        [STAThread]
        private static int Main()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string targetDirectory = Path.Combine(baseDirectory, AppFolderName);
                string appExePath = Path.Combine(targetDirectory, AppExeName);

                Directory.CreateDirectory(targetDirectory);
                ExtractPayload(targetDirectory);
                Launch(appExePath, targetDirectory);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.GetType().Name + ": " + ex.Message,
                    "PVZ104 自解压失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }
        }

        private static void ExtractPayload(string targetDirectory)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream resource = assembly.GetManifestResourceStream(ResourceName))
            {
                if (resource == null)
                {
                    throw new InvalidOperationException("自解压包缺少内置资源：" + ResourceName);
                }

                using (ZipArchive archive = new ZipArchive(resource, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.GetFullPath(Path.Combine(targetDirectory, entry.FullName));
                        if (!destinationPath.StartsWith(Path.GetFullPath(targetDirectory), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException("压缩包包含非法路径：" + entry.FullName);
                        }

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        if (File.Exists(destinationPath) &&
                            string.Equals(entry.Name, "MotionModule.json", StringComparison.OrdinalIgnoreCase))
                        {
                            string templatePath = Path.Combine(
                                Path.GetDirectoryName(destinationPath),
                                "MotionModule.default.json");
                            entry.ExtractToFile(templatePath, true);
                            continue;
                        }

                        entry.ExtractToFile(destinationPath, true);
                    }
                }
            }
        }

        private static void Launch(string appExePath, string workingDirectory)
        {
            if (!File.Exists(appExePath))
            {
                throw new FileNotFoundException("未找到应用程序入口。", appExePath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = appExePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            });
        }
    }
}
