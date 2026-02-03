using System;
using Avalonia;
using System.IO;

namespace Cash8Avalon
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Проверяем наличие файла Setting.gaa
            //string configPath = Path.Combine(AppContext.BaseDirectory, "Setting.gaa");

            //if (!File.Exists(configPath))
            //{
            //    Console.WriteLine($"Не обнаружен файл Setting.gaa в папке:\n{AppContext.BaseDirectory}");
            //    Console.WriteLine("Дальнейшая работа невозможна.");
            //    Console.WriteLine("Нажмите любую клавишу для выхода...");
            //    Console.ReadKey();
            //    return;
            //}

            try
            {
                // Загружаем конфигурацию
                //LoadConfig(configPath);

                // Запускаем Avalonia приложение
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске приложения: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        }

        //private static void LoadConfig(string configPath)
        //{
        //    // Здесь ваш код загрузки конфигурации
        //    // Пример:
        //    // Cash8.MainStaticClass.loadConfig(configPath);

        //    Console.WriteLine($"Загружаем конфигурацию из: {configPath}");
        //    if (!ConfigHelper.CheckConfigFile())
        //    {
        //        return; // Приложение закроется в ShowErrorMessage
        //    }
        //    // TODO: Раскомментировать когда перенесете код
        //    // Cash8.MainStaticClass.loadConfig(configPath);
        //}

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}