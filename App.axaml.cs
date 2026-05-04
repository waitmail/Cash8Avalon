//using Avalonia;
//using Avalonia.Controls.ApplicationLifetimes;
//using Avalonia.Data.Core.Plugins;
//using Avalonia.Markup.Xaml;

//namespace Cash8Avalon
//{
//    public class App : Application
//    {
//        public override void Initialize()
//        {
//            AvaloniaXamlLoader.Load(this);
//        }

//        public override void OnFrameworkInitializationCompleted()
//        {
//            // Убрать валидацию данных если она есть
//            DisableAvaloniaDataAnnotationValidation();

//            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
//            {
//                desktop.MainWindow = new MainWindow();
//            }

//            base.OnFrameworkInitializationCompleted();
//        }

//        private void DisableAvaloniaDataAnnotationValidation()
//        {
//            // Проверяем, есть ли валидаторы
//            if (BindingPlugins.DataValidators.Count > 0)
//            {
//                BindingPlugins.DataValidators.RemoveAt(0);
//            }
//        }
//    }
//}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Linq; // Обязательно для FirstOrDefault

namespace Cash8Avalon
{
    public class App : Application
    {
        // Флаг защиты от рекурсии (повторного входа в обработчик ошибок)
        private static bool _isShowingUiThreadError = false;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Подписываемся на ошибки UI-потока после инициализации фреймворка
            Dispatcher.UIThread.UnhandledException += OnUIThreadException;

            // Отключаем валидацию
            DisableAvaloniaDataAnnotationValidation();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async void OnUIThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 1. Обязательно помечаем как обработанное, чтобы приложение не упало
            e.Handled = true;

            // 2. Пишем в лог (используя метод из Program.cs)
            Program.LogCrashToDb(e.Exception, "Avalonia_UIThreadException");

            // 3. Защита от рекурсии: если мы уже показываем ошибку, вторую не показываем
            if (_isShowingUiThreadError) return;
            _isShowingUiThreadError = true;

            try
            {
                // 4. Безопасное получение владельца окна
                var owner = MainStaticClass.MainWindow;

                if (owner != null && owner.IsVisible)
                {
                    await MessageBox.Show(
                        $"Произошла непредвиденная ошибка в интерфейсе:\n\n{e.Exception.Message}",
                        "Ошибка приложения",
                        MessageBoxButton.OK,
                        MessageBoxType.Error,
                        owner // Передаем владельца для правильного модального поведения
                    );
                }
                else
                {
                    // Если MainWindow еще не создано или уже закрыто, показываем без владельца
                    await MessageBox.Show(
                        $"Произошла непредвиденная ошибка в интерфейсе:\n\n{e.Exception.Message}",
                        "Ошибка приложения",
                        MessageBoxButton.OK,
                        MessageBoxType.Error
                    );
                }
            }
            catch (Exception ex)
            {
                // Если сам MessageBox упал, пишем в консоль, чтобы не вызвать новый краш
                Console.Error.WriteLine($"[FATAL] Failed to show Error MessageBox: {ex.Message}");
            }
            finally
            {
                _isShowingUiThreadError = false;
            }
        }

        /// <summary>
        /// Отключает встроенную валидацию через DataAnnotations.
        /// POS-приложение использует собственную логику проверки данных,
        /// чтобы избежать конфликтов и лишних визуальных эффектов (красные рамки).
        /// </summary>
        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Находим и удаляем именно плагин валидации через DataAnnotations
            var dataAnnotationsValidator = BindingPlugins.DataValidators
                .FirstOrDefault(v => v.GetType().Name.Contains("DataAnnotations"));

            if (dataAnnotationsValidator != null)
            {
                BindingPlugins.DataValidators.Remove(dataAnnotationsValidator);
            }
        }
    }
}