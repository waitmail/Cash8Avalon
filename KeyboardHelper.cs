using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Cash8Avalon
{
    public static class KeyboardHelper
    {
        // ==================== WINDOWS API ====================
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        // ==================== LINUX (X11) API ====================
        private const string LibX11 = "libX11.so.6";

        [DllImport(LibX11)]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport(LibX11)]
        private static extern int XCloseDisplay(IntPtr display);

        // Используем XkbGetIndicatorState, это проще, чем XkbGetState
        [DllImport(LibX11)]
        private static extern int XkbGetIndicatorState(IntPtr display, uint deviceSpec, out uint state);

        // ==================== ОБЩИЙ МЕТОД ====================

        /// <summary>
        /// Проверяет, включен ли CapsLock. Работает на Windows и Linux (X11/Wayland).
        /// </summary>
        public static bool IsCapsLockOn()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return CheckWindows();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // 1. Сначала пробуем самый надежный способ для Linux (файловая система)
                    if (CheckLinuxFileSystem())
                    {
                        return true;
                    }

                    // 2. Если не вышло (нет доступа к файлам), пробуем X11
                    return CheckLinuxX11();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка определения CapsLock: {ex.Message}");
            }

            return false;
        }

        [SupportedOSPlatform("windows")]
        private static bool CheckWindows()
        {
            return (GetKeyState(0x14) & 1) == 1;
        }

        // ==================== LINUX СПОСОБ 1: Файлы (Лучший для Wayland/X11) ====================
        /// <summary>
        /// Проверяет состояние CapsLock через /sys/class/leds (работает везде на Linux)
        /// </summary>
        private static bool CheckLinuxFileSystem()
        {
            try
            {
                // Путь к светодиоду CapsLock в ядре Linux
                // Обычно это inputX::capslock, но имя может отличаться.
                // Мы ищем папку, содержащую 'capslock' в названии
                var ledsPath = "/sys/class/leds";

                if (!Directory.Exists(ledsPath))
                    return false;

                var dirs = Directory.GetDirectories(ledsPath);
                foreach (var dir in dirs)
                {
                    // Ищем папку, связанную с capslock
                    // Пример имени: input0::capslock
                    if (dir.Contains("capslock", StringComparison.OrdinalIgnoreCase))
                    {
                        var brightnessFile = Path.Combine(dir, "brightness");
                        if (File.Exists(brightnessFile))
                        {
                            var text = File.ReadAllText(brightnessFile).Trim();
                            if (int.TryParse(text, out int val))
                            {
                                return val > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Часто бывает доступ запрещен, если не запущен от root, но обычно для чтения доступ есть
                Console.WriteLine($"FileSystem check error: {ex.Message}");
            }

            return false;
        }

        // ==================== LINUX СПОСОБ 2: X11 (Запасной) ====================
        [SupportedOSPlatform("linux")]
        private static bool CheckLinuxX11()
        {
            IntPtr display = IntPtr.Zero;
            try
            {
                display = XOpenDisplay(IntPtr.Zero);
                if (display == IntPtr.Zero) return false;

                // Получаем состояние индикаторов
                // deviceSpec 0x0100 = UseCoreKbd
                if (XkbGetIndicatorState(display, 0x0100, out uint state) == 0) // 0 = Success
                {
                    // Бит 0 (маска 1) обычно соответствует CapsLock
                    return (state & 1) == 1;
                }
            }
            catch (DllNotFoundException)
            {
                Console.WriteLine("libX11 не найдена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"X11 check error: {ex.Message}");
            }
            finally
            {
                if (display != IntPtr.Zero)
                {
                    try { XCloseDisplay(display); } catch { }
                }
            }

            return false;
        }
    }
}