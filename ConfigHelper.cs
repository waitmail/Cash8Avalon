using Avalonia;
using Avalonia.Controls;
using System;
using System.IO;

namespace Cash8Avalon
{
    public static class ConfigHelper
    {
        public static string StartupPath => AppContext.BaseDirectory;

        public static bool CheckConfigFile()
        {
            string configPath = Path.Combine(StartupPath, "Setting.gaa");

            if (!File.Exists(configPath))
            {
                ShowErrorMessage($"Не обнаружен файл Setting.gaa в папке:\n{StartupPath}\nДальнейшая работа невозможна.");
                return false;
            }

            try
            {
                // Загружаем конфигурацию
                LoadConfig(configPath);
                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки конфигурации:\n{ex.Message}");
                return false;
            }
        }

        private static void LoadConfig(string configPath)
        {
            // Здесь должен быть ваш код загрузки конфигурации
            // Например: MainStaticClass.loadConfig(configPath);

            Console.WriteLine($"Загружаем конфигурацию из: {configPath}");
            MainStaticClass.loadConfig(configPath); // Раскомментируйте если есть такой класс
        }

        private static async void ShowErrorMessage(string message)
        {
            var dialog = new Window
            {
                Title = "Ошибка",
                Width = 500,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Margin = new Thickness(0, 20, 0, 0),
                            Width = 100
                        }
                    }
                }
            };

            // Находим кнопку и добавляем обработчик
            if (dialog.Content is StackPanel panel && panel.Children[1] is Button button)
            {
                button.Click += (s, e) =>
                {
                    dialog.Close();
                    Environment.Exit(0);
                };
            }

            // Показываем диалог
            dialog.Show();
        }
    }
}