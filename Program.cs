//using System;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Data;              // Для ConnectionState и ADO.NET
//using Npgsql;                   // Для работы с PostgreSQL
//using System.Diagnostics;       // Для Process
//using Avalonia;                 // Для AppBuilder

//namespace Cash8Avalon
//{
//    internal class Program
//    {
//        private static Mutex _mutex;
//        private static readonly string MutexName = "Cash8Avalon_Global_Mutex";

//        [STAThread]
//        public static void Main(string[] args)
//        {
//            if (!TryAcquireLock())
//            {
//                NotifyUser("Программа уже запущена!");
//                return;
//            }

//            // Ловим краши из фоновых потоков
//            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
//            {
//                if (e.ExceptionObject is Exception ex)
//                {
//                    LogCrashToDb(ex);
//                }
//            };

//            // Ловим краши от незавершенных Task (async void)
//            TaskScheduler.UnobservedTaskException += (sender, e) =>
//            {
//                LogCrashToDb(e.Exception);
//                e.SetObserved();
//            };

//            try
//            {
//                BuildAvaloniaApp()
//                    .StartWithClassicDesktopLifetime(args);
//            }
//            catch (Exception ex)
//            {
//                LogCrashToDb(ex);
//                NotifyUser($"Критическая ошибка: {ex.Message}");
//            }
//        }

//        private static void LogCrashToDb(Exception ex)
//        {
//            try
//            {
//                // Упаковываем данные об исключении в параметры вашего метода
//                string errorMessage = ex.ToString(); // Содержит Type, Message и StackTrace
//                long numdoc = 0;       // При глобальном краше документа нет
//                string metodName = "APP_CRASH";          // Маркер, что это краш приложения
//                string status = "3";                // 3 - Ошибка (по вашей документации)
//                short cashDeskNumber = MainStaticClass.CashDeskNumber;

//                // Пытаемся записать в PostgreSQL
//                MainStaticClass.WriteRecordErrorLog(errorMessage, metodName, numdoc, cashDeskNumber, "LogCrashToDb");
//            }
//            catch
//            {
//                // ФАЛЛБЭК в текстовый файл, если Postgres недоступен (например, нет сети, 
//                // сервер упал, или Crash произошел ДО инициализации MainStaticClass)
//                try
//                {
//                    string fallbackPath = Path.Combine(AppContext.BaseDirectory, "crash_fallback.log");
//                    string logText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType()}: {ex.Message}\n{ex.StackTrace}\n\n";
//                    File.AppendAllText(fallbackPath, logText);
//                }
//                catch { /* Промолчим */ }
//            }
//        }


//        private static void NotifyUser(string message)
//        {
//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                ShowWindowsMessageBox(message);
//            }
//            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//            {
//                ShowLinuxNotification(message);
//            }
//        }

//        private static void ShowWindowsMessageBox(string message)
//        {
//            [DllImport("user32.dll", CharSet = CharSet.Auto)]
//            static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

//            MessageBox(IntPtr.Zero, message, "Cash8Avalon", 0x00000040);
//        }

//        private static void ShowLinuxNotification(string message)
//        {
//            bool shown = false;

//            try
//            {
//                using (Process p = new Process())
//                {
//                    p.StartInfo.FileName = "zenity";
//                    p.StartInfo.Arguments = $"--warning --text=\"{message}\" --title=\"Cash8Avalon\" --timeout=20";
//                    p.StartInfo.UseShellExecute = false;
//                    p.Start();
//                    p.WaitForExit(500);
//                    shown = true;
//                }
//            }
//            catch { }

//            if (!shown)
//            {
//                try
//                {
//                    using (Process p = new Process())
//                    {
//                        p.StartInfo.FileName = "notify-send";
//                        p.StartInfo.Arguments = $"--urgency=normal --expire-time=20000 \"Cash8Avalon\" \"{message}\"";
//                        p.StartInfo.UseShellExecute = false;
//                        p.Start();
//                        shown = true;
//                    }
//                }
//                catch { }
//            }

//            if (!shown)
//            {
//                Console.ForegroundColor = ConsoleColor.Red;
//                Console.WriteLine($"[ERROR] {message}");
//                Console.ResetColor();
//            }
//        }

//        private static bool TryAcquireLock()
//        {
//            try
//            {
//                _mutex = new Mutex(true, MutexName, out bool createdNew);
//                return createdNew;
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }

//        public static AppBuilder BuildAvaloniaApp()
//            => AppBuilder.Configure<App>()
//                .UsePlatformDetect()
//                .WithInterFont()
//                .LogToTrace();
//    }
//}

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using Npgsql;
using System.Diagnostics;
using Avalonia;

namespace Cash8Avalon
{
    internal class Program
    {
        private static Mutex _mutex;
        private static bool _mutexOwned = false; // ← Флаг владения мьютексом
        private static readonly string MutexName = "Cash8Avalon_Global_Mutex";

        [STAThread]
        public static void Main(string[] args)
        {
            if (!TryAcquireLock())
            {
                NotifyUser("Программа уже запущена!");
                return;
            }

            // 1. Ловим краши из фоновых потоков (Domain level)
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    LogCrashToDb(ex, "AppDomain_UnhandledException");
                }
            };

            // 2. Ловим краши от незавершенных Task
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogCrashToDb(e.Exception, "UnobservedTaskException");
                e.SetObserved();
            };

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                LogCrashToDb(ex, "Main_TryCatch");
                NotifyUser($"Критическая ошибка при запуске: {ex.Message}");
            }
            finally
            {
                ReleaseLock();
            }
        }

        // Изменено на internal, чтобы было доступно из App.axaml.cs в рамках сборки
        internal static void LogCrashToDb(Exception ex, string source = "APP_CRASH")
        {
            try
            {
                if (ex is AggregateException aggEx)
                {
                    ex = aggEx.Flatten();
                }

                string errorMessage = ex.ToString();

                long numdoc = 0;
                string metodName = source;
                string status = "3";
                short cashDeskNumber = MainStaticClass.CashDeskNumber;

                MainStaticClass.WriteRecordErrorLog(errorMessage, metodName, numdoc, cashDeskNumber, "LogCrashToDb");
            }
            catch (Exception dbEx)
            {
                try
                {
                    string fallbackPath = Path.Combine(Path.GetTempPath(), "Cash8Avalon_crash_fallback.log");
                    string logText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SOURCE: {source}\n{ex.GetType()}: {ex.Message}\n{ex.StackTrace}\n\n";
                    File.AppendAllText(fallbackPath, logText);
                }
                catch (Exception fallbackEx)
                {
                    Console.Error.WriteLine($"[FATAL] Failed to write crash log. DB Error: {dbEx.Message}. File Error: {fallbackEx.Message}");
                }
            }
        }

        private static void NotifyUser(string message)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ShowLinuxNotification(message);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {message}");
                Console.ResetColor();
            }
        }

        private static void ShowLinuxNotification(string message)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "notify-send";
                    p.StartInfo.UseShellExecute = false;

                    p.StartInfo.ArgumentList.Add("--urgency=critical");
                    p.StartInfo.ArgumentList.Add("--expire-time=10000");
                    p.StartInfo.ArgumentList.Add("Cash8Avalon Ошибка");
                    p.StartInfo.ArgumentList.Add(message);

                    p.Start();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to show notify-send: {ex.Message}");
            }
        }

        private static bool TryAcquireLock()
        {
            try
            {
                _mutex = new Mutex(true, MutexName, out bool createdNew);
                _mutexOwned = createdNew; // ← Запоминаем факт владения
                return createdNew;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ReleaseLock()
        {
            try
            {
                // ✅ Освобождаем только если действительно владеем
                if (_mutexOwned && _mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                    _mutex = null;
                    _mutexOwned = false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to release mutex: {ex.Message}");
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}