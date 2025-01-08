using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Security.Principal;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        // Проверка прав администратора
        if (!IsAdministrator())
        {
            RestartAsAdministrator();
            return;
        }

        int version = 2;
        string fileName = "disk.file";
        Console.Title = "CheckStartupAllDrives";

        if (args.Length > 0 && (args[0] == "-r" || args[0] == "--reset"))
        {
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                    Console.WriteLine("Файл disk.file успешно удален.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Ошибка при удалении файла: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("Файл disk.file не найден.");
            }
            return;
        }
        else if (args.Length > 0 && (args[0] == "-v" || args[0] == "--version"))
        {
            Console.WriteLine($"Версия {version}");
            Console.Read();
            return;
        }

        // Добавление в автозагрузку
        AddToStartup();

        string[] diskLetters;
        var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).ToArray();

        if (File.Exists(fileName))
        {
            diskLetters = File.ReadAllLines(fileName).Select(d => d.ToLower()).ToArray();
        }
        else
        {
            diskLetters = drives.Select(d => d.Name.Substring(0, 1).ToLower()).ToArray();
            File.WriteAllLines(fileName, diskLetters);
        }

        var currentDrives = drives.Select(d => d.Name.Substring(0, 1).ToLower()).ToArray();
        bool allDisksPassed = true;

        foreach (var disk in diskLetters)
        {
            string driveLetter = disk.ToUpper() + ":\\";
            var drive = drives.FirstOrDefault(d => d.Name.StartsWith(disk, StringComparison.OrdinalIgnoreCase));

            if (drive == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Диск {disk.ToUpper()} -- гб |-------------------| -- гб - не найден");
                allDisksPassed = false;
                continue;
            }

            string testFilePath = Path.Combine(drive.Name, "testdisk.file");
            bool writeSuccess = true;

            try
            {
                using (FileStream fs = new FileStream(testFilePath, FileMode.Create, FileAccess.Write))
                {
                    fs.SetLength(100 * 1024 * 1024); // 100мб
                }
                File.Delete(testFilePath);
            }
            catch
            {
                writeSuccess = false;
            }

            string bar = new string('█', (int)(drive.TotalSize > 0 ? 10 * (1 - (double)drive.AvailableFreeSpace / drive.TotalSize) : 0))
                .PadRight(10, ' ');

            Console.ForegroundColor = writeSuccess ? ConsoleColor.Green : ConsoleColor.DarkYellow;
            Console.WriteLine(
                $"Диск {drive.Name.Substring(0, 1)} {(drive.TotalSize - drive.AvailableFreeSpace) / (1024 * 1024 * 1024)} гб {bar} | {drive.TotalSize / (1024 * 1024 * 1024)} гб - {(writeSuccess ? "рабочий" : "не прошел проверку")}");

            if (!writeSuccess)
            {
                allDisksPassed = false;
            }
        }

        foreach (var newDisk in currentDrives.Except(diskLetters))
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Диск {newDisk.ToUpper()} найден, но не указан в disk.file");
        }

        if (!allDisksPassed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Обнаружены ошибки на дисках. Перезагрузка через 5 секунд...");
            Thread.Sleep(5000);
            RestartComputer();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Все диски успешно проверены. Нажмите ENTER для выхода.");
            Console.ResetColor();
            Console.ReadLine();
        }
    }

    static void AddToStartup()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (key != null)
        {
            key.SetValue("CheckStartupAllDrives", exePath);
            key.Close();
        }
    }

    static void RestartComputer()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown",
            Arguments = "/r /t 0",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    static bool IsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    static void RestartAsAdministrator()
    {
        var exePath = Process.GetCurrentProcess().MainModule.FileName;
        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        });
    }
}