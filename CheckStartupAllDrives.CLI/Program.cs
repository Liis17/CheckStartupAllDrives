using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        int version = 1;

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
        }
        else if (args.Length > 0 && (args[0] == "-v" || args[0] == "--version"))
        {
            Console.WriteLine($"Версия {version}");
            Console.Read();
            return;
        }
        else if (args.Length > 0 && (args[0] == "-h" || args[0] == "--help"))
        {
            Console.WriteLine($"-h | --help - этот текст\n-r | --reset - сбросить файл с сохраненными дисками\n-v | --version - версия программы\n\nПрограмма проверяет доступные диски на компьютере. Она читает буквы дисков из файла disk.file. Если файл отсутствует или пуст, программа запрашивает у пользователя ввод и сохраняет его. Затем программа выводит информацию о каждом диске (общий объем, занятое и свободное место), создает на дисках тестовый файл testdisk.file размером 100 МБ и удаляет его после проверки, сообщая о результатах каждой операции.");
            Console.Read();
            return;
        }
        else if(args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Программа запущена без аргументов");
            Console.ResetColor();
        }
        string[] diskLetters;

        if (File.Exists(fileName))
        {
            diskLetters = File.ReadAllLines(fileName);
            diskLetters = Array.FindAll(diskLetters, letter => !string.IsNullOrWhiteSpace(letter));
        }
        else
        {
            diskLetters = new string[0];
        }

        if (diskLetters.Length == 0)
        {
            Console.WriteLine("Введите буквы дисков (в нижнем регистре, через пробел):");
            bool validInput = false;

            while (!validInput)
            {
                string input = Console.ReadLine();
                diskLetters = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                validInput = true;
                foreach (var letter in diskLetters)
                {
                    if (letter.Length != 1 || !char.IsLetter(letter[0]) || !char.IsLower(letter[0]))
                    {
                        validInput = false;
                        Console.WriteLine("Некорректный ввод. Попробуйте еще раз:");
                        break;
                    }
                }
            }

            File.WriteAllLines(fileName, diskLetters);
        }

        foreach (var letter in diskLetters)
        {
            string driveLetter = letter.ToUpper() + ":\\";
            DriveInfo drive = null;

            try
            {
                drive = new DriveInfo(driveLetter);
                if (!drive.IsReady)
                    throw new Exception("Диск не готов.");
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine($"Диск {driveLetter} не найден или недоступен.");
                Console.ResetColor();
                continue;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Диск {driveLetter}:");
            Console.ResetColor();
            Console.WriteLine($"Всего места: {drive.TotalSize / (1024 * 1024)} МБ");
            Console.WriteLine($"Занято: {(drive.TotalSize - drive.AvailableFreeSpace) / (1024 * 1024)} МБ");
            Console.WriteLine($"Свободно: {drive.AvailableFreeSpace / (1024 * 1024)} МБ");

            string testFilePath = Path.Combine(driveLetter, "testdisk.file");
            try
            {
                using (FileStream fs = new FileStream(testFilePath, FileMode.Create, FileAccess.Write))
                {
                    fs.SetLength(100 * 1024 * 1024); // 100мб
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Файл testdisk.file успешно создан на диске {driveLetter}.");
                Console.ResetColor();
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Не удалось создать файл на диске {driveLetter}.");
                Console.ResetColor();
                continue;
            }

            try
            {
                File.Delete(testFilePath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Файл testdisk.file успешно удален с диска {driveLetter}.");
                Console.ResetColor();
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Не удалось удалить файл с диска {driveLetter}.");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Проверка завершена, нажмите ENTER для выхода");
        Console.ResetColor();
        Console.ReadKey();
    }
}
