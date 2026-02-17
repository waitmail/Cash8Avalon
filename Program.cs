using System;
using Avalonia;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cash8Avalon
{
    internal class Program
    {
        private static FileStream _lockFileStream;
        private static readonly string LockFilePath = Path.Combine(
            Path.GetTempPath(), "Cash8Avalon.lock");

        [STAThread]
        public static void Main(string[] args)
        {
            if (!TryAcquireLock())
            {
                // Приложение уже запущено - показываем уведомление
                NotifyUser("Программа уже запущена!");
                return;
            }

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                NotifyUser($"Ошибка: {ex.Message}");
            }
        }

        private static void NotifyUser(string message)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ShowWindowsMessageBox(message);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ShowLinuxNotification(message);
            }
        }

        private static void ShowWindowsMessageBox(string message)
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

            MessageBox(IntPtr.Zero, message, "Cash8Avalon", 0x00000040);
        }

        private static void ShowLinuxNotification(string message)
        {
            // Способ 1: notify-send с авто-закрытием через 20 секунд
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "notify-send";
                    p.StartInfo.Arguments = $"--urgency=critical --expire-time=20000 \"Cash8Avalon\" \"{message}\"";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
                return;
            }
            catch { }

            // Способ 2: zenity с таймаутом 20 секунд
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "zenity";
                    p.StartInfo.Arguments = $"--warning --text=\"{message}\" --title=\"Cash8Avalon\" --timeout=20";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
                return;
            }
            catch { }

            // Способ 3: yad (более новая версия zenity) с таймаутом
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "yad";
                    p.StartInfo.Arguments = $"--center --text=\"{message}\" --title=\"Cash8Avalon\" --timeout=20 --button=OK:0";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
                return;
            }
            catch { }

            // Способ 4: kdialog для KDE с авто-закрытием
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "kdialog";
                    p.StartInfo.Arguments = $"--title \"Cash8Avalon\" --passivepopup \"{message}\" 20";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
                return;
            }
            catch { }

            // Способ 5: xmessage с таймаутом
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "xmessage";
                    p.StartInfo.Arguments = $"-center -timeout 20 \"{message}\"";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
                return;
            }
            catch { }
        }

        private static bool TryAcquireLock()
        {
            try
            {
                _lockFileStream = File.Open(
                    LockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);

                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}