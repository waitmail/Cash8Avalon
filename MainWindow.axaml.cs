using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cash8Avalon.ViewModels;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Cash8Avalon
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _unloadingTimer;
        private MainViewModel _viewModel;
        private bool _isReallyClosing = false;

        // ✅ Пункт 1: Флаг для защиты от доступа к уничтоженному UI
        private bool _isDisposed = false;

        private CancellationTokenSource _lifetimeCts;
        // ✅ ДОБАВЬТЕ ЭТО ПОЛЕ для защиты от повторного запуска
        private bool _isClosingInProgress = false;

        private bool _isGetUsersStarted = false;

        public MainWindow()
        {
            InitializeComponent();
            _lifetimeCts = new CancellationTokenSource();
            InitializeUnloadingTimer();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            this.Closing += MainWindow_Closing;
            // ✅ Пункт 2: Подписываемся на событие закрытия для финальной очистки
            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Безопасный показ сообщения. Защита от гонки состояний при закрытии окна.
        /// </summary>
        private async Task ShowSafeMessage(string message, string title,
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxType type = MessageBoxType.Info)
        {
            // 1. Проверка, что окно живое
            if (_isDisposed || !this.IsVisible) return;

            try
            {
                // 2. Передаем this как владельца (решает проблему фокуса)
                await MessageBoxHelper.Show(message, title, buttons, type, this);
            }
            catch (ObjectDisposedException)
            {
                // Игнорируем, если окно уничтожилось в момент показа
                Console.WriteLine("[UI] Попытка показать MessageBox на уничтоженном окне.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UI] Ошибка при показе MessageBox: {ex.Message}");
            }
        }

        // ✅ Пункт 2: Реализация метода финальной очистки        
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            Console.WriteLine("[MainWindow] Финальная очистка (Closed)");

            // Отписываемся, чтобы избежать повторных вызовов
            this.Closed -= MainWindow_Closed;

            // ✅ Очищаем DataContext для помощи GC
            this.DataContext = null;

            // Обнуляем ссылки для GC
            _viewModel = null;
            _lifetimeCts = null;
            _unloadingTimer = null;

            Console.WriteLine("[MainWindow] Все ресурсы освобождены");
        }

        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            // ✅ 1. Защита от повторного нажатия (Решает проблему "цикла")
            if (_isClosingInProgress)
            {
                e.Cancel = true; // Отменяем попытку закрыть повторно, пока идет процесс
                return;
            }

            if (_isReallyClosing) return;

            // ✅ 2. Устанавливаем флаг и блокируем интерфейс сразу
            _isClosingInProgress = true;
            e.Cancel = true;
            this.IsEnabled = false; // Блокируем главное окно, чтобы пользователь не кликал по нему

            _unloadingTimer?.Stop();

            // 3. Создаем окно ожидания
            var waitWindow = new Window
            {
                Title = "Завершение работы",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                SystemDecorations = SystemDecorations.BorderOnly,
                Topmost = true
            };

            var stackPanel = new StackPanel { Margin = new Thickness(30), Spacing = 15, HorizontalAlignment = HorizontalAlignment.Center };
            var titleText = new TextBlock { Text = "Завершение работы...", FontSize = 18, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center };

            // ✅ Изменил текст, чтобы не пугать пользователя, если сеть отвалится
            var unloadingMessageText = new TextBlock { Text = "Идёт отправка данных на сервер.\nПожалуйста, подождите...", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 };
            
            var timerText = new TextBlock { Text = "⏱ 0 сек", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            var progressBar = new ProgressBar { Width = 300, Height = 8, IsIndeterminate = true, Foreground = new SolidColorBrush(Color.Parse("#2196F3")), Background = new SolidColorBrush(Color.Parse("#E3F2FD")), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Center };
            var dotsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 5 };
            for (int i = 0; i < 3; i++)
            {
                dotsPanel.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse("#2196F3")), Opacity = 0.3 });
            }
            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(unloadingMessageText);
            stackPanel.Children.Add(timerText);
            stackPanel.Children.Add(progressBar);
            stackPanel.Children.Add(dotsPanel);
            waitWindow.Content = stackPanel;

            // ✅ Показываем окно ожидания поверх заблокированного главного окна
            waitWindow.Show(this);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            int dotAnimationStep = 0;
            uiTimer.Tick += (s, ev) =>
            {
                if (stopwatch.IsRunning)
                {
                    var elapsed = stopwatch.Elapsed;
                    timerText.Text = $"⏱ {elapsed.Seconds}.{elapsed.Milliseconds / 100} сек";
                    dotAnimationStep++;
                    for (int i = 0; i < dotsPanel.Children.Count; i++)
                    {
                        if (dotsPanel.Children[i] is Border dot)
                        {
                            double opacity = 0.3 + 0.7 * Math.Sin(dotAnimationStep * 0.1 + i * 2);
                            dot.Opacity = Math.Max(0.2, Math.Min(1.0, opacity));
                        }
                    }
                }
            };
            uiTimer.Start();

            // ✅ Закрываем остальные окна (Cash_check и т.д.)
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    // Закрываем все окна, кроме главного и окна ожидания
                    var windowsToClose = desktopLifetime.Windows.Where(w => w != this && w != waitWindow && w.IsVisible).ToList();
                    foreach (var win in windowsToClose) { try { win.Close(); } catch { } }
                }
            }, DispatcherPriority.Background);

            // Даем время на закрытие дочерних окон
            await Task.Delay(100);

            // 4. ВЫГРУЗКА ДАННЫХ (с жестким таймаутом)
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Начало выгрузки...");

                // ✅ Запускаем задачу выгрузки, НЕ ожидая её (нет await).
                // Используем _lifetimeCts.Token, чтобы можно было послать сигнал отмены.
                // 1. Создаем объект для передачи прогресса (сразу перед запуском задачи)
                var progress = new Progress<string>(message =>
                {
                    unloadingMessageText.Text = message; // обновляем текст в спинере
                });

                // 2. Передаем progress вторым аргументом!
                var unloadTask = PerformUnloadAsync(_lifetimeCts.Token, progress);

                // ✅ Запускаем таймер "нетерпения" (25 секунд)
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(25));

                // ✅ Ждем: кто закончится первым? Задача или Таймер?
                var completedTask = await Task.WhenAny(unloadTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Сработал таймер! Прошло 25 секунд.
                    Console.WriteLine($"⚠ Таймаут! Выгрузка не успела завершиться за 25 сек. Принудительное закрытие.");
                    MainStaticClass.WriteRecordErrorLog("Таймаут выгрузки (25 сек). Приложение закрыто принудительно.", "MainWindow_Closing", 0, MainStaticClass.CashDeskNumber, "Timeout");

                    // Просим задачу остановиться
                    if (_lifetimeCts != null && !_lifetimeCts.IsCancellationRequested)
                    {
                        _lifetimeCts.Cancel();
                    }

                    // ✅ ИСПРАВЛЕНИЕ: "Наблюдаем" за брошенной задачей, чтобы не потерять ошибку
                    _ = unloadTask.ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception?.InnerException is not OperationCanceledException)
                        {
                            Console.WriteLine($"[Background] Ошибка выгрузки: {t.Exception?.InnerException?.Message}");
                            MainStaticClass.WriteRecordErrorLog(t.Exception?.InnerException, 0, MainStaticClass.CashDeskNumber, "UnloadAsync (background)");
                        }
                    }, TaskScheduler.Default);
                }
                else
                {
                    // Задача завершилась РАНЬШЕ таймера (успех или ошибка внутри задачи)
                    // Теперь проверяем, не упала ли она с ошибкой
                    try
                    {
                        await unloadTask; // Получаем результат (или исключение)
                        Console.WriteLine($"✓ Выгрузка завершена успешно за {stopwatch.Elapsed.TotalSeconds:F1} сек");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка выгрузки: {ex.Message}");
                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка выгрузки при закрытии приложения");
                    }
                }
            }
            catch (Exception ex)
            {
                // Общая ошибка (например, при создании задач)
                Console.WriteLine($"✗ Критическая ошибка в блоке закрытия: {ex.Message}");
            }
            finally
            {
                // 5. Финальная очистка
                uiTimer.Stop();
                stopwatch.Stop();

                if (waitWindow.IsVisible) waitWindow.Close();

                if (_unloadingTimer != null)
                {
                    _unloadingTimer.Tick -= UnloadingTimer_Tick;
                }

                if (_lifetimeCts != null && !_lifetimeCts.IsCancellationRequested)
                {
                    _lifetimeCts.Cancel();
                }

                if (MainStaticClass.MainWindow == this)
                {
                    MainStaticClass.MainWindow = null;
                }

                _isDisposed = true;

                _lifetimeCts?.Dispose();
            }

            // ✅ Снимаем обработчик и закрываем окно
            _isReallyClosing = true;
            this.Closing -= MainWindow_Closing;
            this.Close();
        }

        private void InitializeUnloadingTimer()
        {
            _unloadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _unloadingTimer.Tick += UnloadingTimer_Tick;
        }

        private void CreateDefaultSettingsFile(string filePath)
        {
            string defaultSettings = @"[ip адрес сервера]
                127.0.0.1
                [имя базы данных]
                Cash_Place
                [сервисный пароль]
                1
                [порт сервера]
                5432
                [пароль postgres]
                a123456789
                [пользователь postgres]
                postgres";

            MainStaticClass.EncryptData(filePath, defaultSettings);
        }

        /// <summary>
        /// Показывает окно обновления и возвращает результат
        /// </summary>
        /// <returns>True — обновление успешно, False — отменено/ошибка</returns>
        private async Task<bool> ShowUpdateWindowModalAsync(bool show_phone)
        {
            try
            {
                var updateWindow = new LoadProgramFromInternet
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    show_phone = show_phone
                };

                // ✅ Всегда используем ShowDialog — окно уже показано
                bool result = await updateWindow.ShowDialog(this);
                Console.WriteLine($"[Update] Результат: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при показе окна обновления: {ex.Message}");
                return false;
            }
        }

        // Хелпер для привязки жизненного цикла (чтобы фантомы не висели в панели задач)
        private static void SetWindowOwner(Window target, Window owner)
        {
            typeof(WindowBase).GetProperty(nameof(WindowBase.Owner))?.SetValue(target, owner);
        }

        public async Task UpdateChecksIfDateValidAsync()
        {
            // 1. Проверяем условие: если СЕГОДНЯ меньше 01.07.2026
            DateTime limitDate = new DateTime(2026, 7, 7);

            if (DateTime.Today >= limitDate)
            {
                Console.WriteLine("Текущая дата больше или равна 07.07.2026. Запрос не выполняется.");
                return;
            }

            // 2. Выполняем запрос, если условие выполнено
            string sqlQuery = @"
            UPDATE public.checks_header 
            SET extra = false, is_sent = 0 
            WHERE its_deleted = 0 
              AND non_cash_money <> 0 
              AND date_time_write < @targetDate
              AND extra = true";

            try
            {
                using (var connection = MainStaticClass.NpgsqlConn())
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(sqlQuery, connection))
                    {
                        // Передаем дату как параметр (формат YYYY-MM-DD безопасно передается в Npgsql)
                        // Если вам нужно строго '04.07.2026', передаем именно эту дату
                        command.Parameters.AddWithValue("@targetDate", limitDate);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"Запрос успешно выполнен. Обновлено строк: {rowsAffected}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
                // Здесь можно добавить логирование ошибки
            }
        }


        protected override async void OnOpened(EventArgs e)
        {
            MainStaticClass.MainWindow = this;
            await Task.Delay(50);

            if (OperatingSystem.IsLinux())
            {
                this.WindowState = WindowState.Maximized;
                var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
                if (screen != null)
                {
                    this.Width = screen.WorkingArea.Width;
                    this.Height = screen.WorkingArea.Height;
                    this.Position = new PixelPoint(0, 0);
                }
                this.Topmost = true;
                await Task.Delay(50);
                this.Topmost = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }

            // ==========================================
            // ПРОВЕРКА СВОБОДНОГО МЕСТА (УНИВЕРСАЛЬНО: WINDOWS + LINUX)
            // ==========================================
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                // В Windows вернет "C:\", в Linux вернет "/"
                string rootPath = Path.GetPathRoot(basePath) ?? Path.DirectorySeparatorChar.ToString();

                var drive = new DriveInfo(rootPath);

                // 5 ГБ в байтах
                long minRequiredBytes = 5L * 1024 * 1024 * 1024;
                long freeBytes = drive.AvailableFreeSpace;

                if (freeBytes < minRequiredBytes)
                {
                    double freeGb = freeBytes / (1024.0 * 1024.0 * 1024.0);
                    string warningMsg = $"ВНИМАНИЕ!\n" +
                                        $"НАПИШИТЕ ЗАЯВКУ В ИТ ОТДЕЛ!\n" +
                                        $"На диске, где установлена программа ({rootPath}), критически мало свободного места.\n" +
                                        $"Осталось: {freeGb:F2} ГБ (Рекомендуемый минимум: 5 ГБ).\n\n" +
                                        $"Нехватка места может привести к повреждению базы данных, ошибкам записи логов и сбоям синхронизации!\n" +
                                        $"Пожалуйста, освободите место на диске.";

                    if (_isDisposed) return;
                    await Task.Delay(50);
                    await ShowSafeMessage(warningMsg, "Критически мало свободного места", MessageBoxButton.OK, MessageBoxType.Warning);
                }
                else
                {
                    double freeGb = freeBytes / (1024.0 * 1024.0 * 1024.0);
                    Console.WriteLine($"✓ Проверка диска пройдена. Свободно на {rootPath}: {freeGb:F2} ГБ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Не удалось проверить свободное место на диске: {ex.Message}");
            }


            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Setting.gaa");
            if (!File.Exists(configPath))
            {
                CreateDefaultSettingsFile(configPath);
                if (_isDisposed) return;
                await ShowSafeMessage($"Не обнаружен файл Setting.gaa...", "Проверка файлов настроек", MessageBoxButton.OK, MessageBoxType.Error);
                await Task.Delay(100);
            }

            Console.WriteLine($"Загружаем конфигурацию из: {configPath}");
            MainStaticClass.loadConfig(configPath);
            base.OnOpened(e);
            UpdateMenuVisibility(0);

            await Task.Delay(50);

            string usersSyncStatus = "Статус синхронизации неизвестен";
            bool hasUpdate = false;

            // ==========================================
            // БЛОК 1: ПРОВЕРКА ОБНОВЛЕНИЙ
            // ==========================================
            var checkUpdateWindow = new Window
            {
                Title = "Проверка обновлений",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                SystemDecorations = SystemDecorations.None,
                Topmost = true,
                Background = null
            };

            var shadowWrapper1 = new Border
            {
                Background = Brushes.White,
                Margin = new Thickness(8),
                CornerRadius = new CornerRadius(4),
                BoxShadow = new BoxShadows(new BoxShadow { Blur = 25.0, OffsetY = 5.0, Spread = 0, Color = Color.FromArgb(150, 33, 150, 243) }),
                BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
                BorderThickness = new Thickness(1)
            };

            var stackPanel1 = new StackPanel { Margin = new Thickness(30), Spacing = 15, HorizontalAlignment = HorizontalAlignment.Center };
            stackPanel1.Children.Add(new TextBlock { Text = "Проверка обновлений", FontSize = 18, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            var updateMessageText = new TextBlock { Text = "Идёт проверка наличия обновлений на сервере.", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 };
            var timerText1 = new TextBlock { Text = "⏱ 0 сек", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#4CAF50")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            var progressBar1 = new ProgressBar { Width = 300, Height = 8, IsIndeterminate = true, Foreground = new SolidColorBrush(Color.Parse("#4CAF50")), Background = new SolidColorBrush(Color.Parse("#E8F5E9")), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Center };
            var dotsPanel1 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 5 };
            for (int i = 0; i < 3; i++) dotsPanel1.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse("#4CAF50")), Opacity = 0.3 });

            stackPanel1.Children.Add(updateMessageText);
            stackPanel1.Children.Add(timerText1);
            stackPanel1.Children.Add(progressBar1);
            stackPanel1.Children.Add(dotsPanel1);

            // СВЯЗЫВАЕМ ВСЁ ВМЕСТЕ:
            shadowWrapper1.Child = stackPanel1;       // 1. Кладем точки внутрь рамки
            checkUpdateWindow.Content = shadowWrapper1; // 2. Кладем рамку в окно
            SetWindowOwner(checkUpdateWindow, this);   // 3. Привязываем жизненный цикл

            var stopwatch1 = Stopwatch.StartNew();
            DispatcherTimer uiTimer1 = null;
            int dotAnimationStep1 = 0;

            try
            {
                uiTimer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                uiTimer1.Tick += (s, ev) =>
                {
                    if (stopwatch1.IsRunning)
                    {
                        var elapsed = stopwatch1.Elapsed;
                        timerText1.Text = $"⏱ {elapsed.Seconds}.{elapsed.Milliseconds / 100} сек";
                        dotAnimationStep1++;
                        for (int i = 0; i < dotsPanel1.Children.Count; i++)
                        {
                            if (dotsPanel1.Children[i] is Border dot)
                                dot.Opacity = Math.Max(0.2, Math.Min(1.0, 0.3 + 0.7 * Math.Sin(dotAnimationStep1 * 0.1 + i * 2)));
                        }
                    }
                };

                checkUpdateWindow.Show();
                uiTimer1.Start();

                try
                {
                    hasUpdate = await Task.Run(() => MainStaticClass.CheckNewVersionProgrammAsync());
                    updateMessageText.Text = hasUpdate ? "Доступно обновление!" : "Для вашей версии обновлений нет.";
                }
                catch
                {
                    updateMessageText.Text = "Не удалось проверить обновления.";
                }
                finally
                {
                    progressBar1.IsIndeterminate = false;
                    timerText1.Text = "Готово";
                    uiTimer1.Stop();
                    stopwatch1.Stop();
                    await Task.Delay(2000); // Даем прочитать результат
                    checkUpdateWindow.Close();
                    await Task.Delay(150);   // Даем панели задач Windows обновиться
                }
            }
            catch (Exception ex) { Console.WriteLine($"Ошибка обновлений: {ex.Message}"); }

            if (_isDisposed) return;
            if (hasUpdate)
            {
                bool updateSuccess = await ShowUpdateWindowModalAsync(false);
                if (updateSuccess) { this.Close(); return; }
            }

            // ==========================================
            // БЛОК 2: ЗАГРУЗКА ПОЛЬЗОВАТЕЛЕЙ
            // ==========================================
            var loadUsersWindow = new Window
            {
                Title = "Синхронизация",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                SystemDecorations = SystemDecorations.None,
                Topmost = true,
                Background = null
            };

            var shadowWrapper2 = new Border
            {
                Background = Brushes.White,
                Margin = new Thickness(8),
                CornerRadius = new CornerRadius(4),
                BoxShadow = new BoxShadows(new BoxShadow { Blur = 25.0, OffsetY = 5.0, Spread = 0, Color = Color.FromArgb(150, 33, 150, 243) }),
                BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
                BorderThickness = new Thickness(1)
            };

            var stackPanel2 = new StackPanel { Margin = new Thickness(30), Spacing = 15, HorizontalAlignment = HorizontalAlignment.Center };
            stackPanel2.Children.Add(new TextBlock { Text = "Синхронизация", FontSize = 18, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            var usersMessageText = new TextBlock { Text = "Идёт загрузка списка пользователей...", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 };
            var timerText2 = new TextBlock { Text = "⏱ 0 сек", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#4CAF50")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            var progressBar2 = new ProgressBar { Width = 300, Height = 8, IsIndeterminate = true, Foreground = new SolidColorBrush(Color.Parse("#4CAF50")), Background = new SolidColorBrush(Color.Parse("#E8F5E9")), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Center };
            var dotsPanel2 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 5 };
            for (int i = 0; i < 3; i++) dotsPanel2.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse("#4CAF50")), Opacity = 0.3 });

            stackPanel2.Children.Add(usersMessageText);
            stackPanel2.Children.Add(timerText2);
            stackPanel2.Children.Add(progressBar2);
            stackPanel2.Children.Add(dotsPanel2);

            // СВЯЗЫВАЕМ ВСЁ ВМЕСТЕ:
            shadowWrapper2.Child = stackPanel2;        // 1. Кладем точки внутрь рамки
            loadUsersWindow.Content = shadowWrapper2;  // 2. Кладем рамку в окно
            SetWindowOwner(loadUsersWindow, this);    // 3. Привязываем жизненный цикл

            var stopwatch2 = Stopwatch.StartNew();
            DispatcherTimer uiTimer2 = null;
            int dotAnimationStep2 = 0;

            try
            {
                uiTimer2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                uiTimer2.Tick += (s, ev) =>
                {
                    if (stopwatch2.IsRunning)
                    {
                        var elapsed = stopwatch2.Elapsed;
                        timerText2.Text = $"⏱ {elapsed.Seconds}.{elapsed.Milliseconds / 100} сек";
                        dotAnimationStep2++;
                        for (int i = 0; i < dotsPanel2.Children.Count; i++)
                        {
                            if (dotsPanel2.Children[i] is Border dot)
                                dot.Opacity = Math.Max(0.2, Math.Min(1.0, 0.3 + 0.7 * Math.Sin(dotAnimationStep2 * 0.1 + i * 2)));
                        }
                    }
                };

                loadUsersWindow.Show();
                uiTimer2.Start();

                try
                {
                    // Ждем результат
                    bool success = await Task.Run(() => GetUsers(_lifetimeCts.Token));

                    if (success)
                    {
                        usersSyncStatus = "Пользователи синхронизированы";
                        usersMessageText.Text = "Список пользователей успешно загружен!";

                        // ✅ Успех: можно явно вернуть стандартный цвет (хотя он и так черный)
                        usersMessageText.Foreground = Brushes.Black;
                    }
                    else
                    {
                        usersSyncStatus = "Ошибка обновления пользователей";
                        usersMessageText.Text = "Не удалось загрузить пользователей. Подробности в логах.";

                        // ❌ Ошибка: красим шрифт в красный!
                        usersMessageText.Foreground = new SolidColorBrush(Color.Parse("#D32F2F")); // Красный цвет
                    }
                    await UpdateChecksIfDateValidAsync();
                }
                catch (Exception ex)
                {
                    usersMessageText.Text = "Критическая ошибка загрузки.";
                    // ❌ Критическая ошибка: тоже красим в красный
                    usersMessageText.Foreground = new SolidColorBrush(Color.Parse("#D32F2F"));
                }
                finally
                {
                    // ... ваш код остановки таймера и закрытия окна ...
                    progressBar2.IsIndeterminate = false;
                    timerText2.Text = "Готово";
                    uiTimer2.Stop();
                    stopwatch2.Stop();
                    await Task.Delay(2000);
                    loadUsersWindow.Close();
                    await Task.Delay(150);
                }
            }
            catch (Exception ex) { Console.WriteLine($"Ошибка пользователей: {ex.Message}"); }

            if (_isDisposed) return;

            // ==========================================
            // ЭТАП 2: АВТОРИЗАЦИЯ
            // ==========================================
            var loginWindow = new Interface_switching();

            // Статус из Блока 2 попадает в заголовок окна логина
            loginWindow.Title = usersSyncStatus;

            bool loginSuccess = false;
            loginWindow.AuthorizationSuccess += (s, password) => { loginSuccess = true; loginWindow.Close(); };
            loginWindow.AuthorizationCancel += (s, args) => { loginSuccess = false; loginWindow.Close(); };

            await loginWindow.ShowDialog(this); // Теперь фокус перехватится корректно!

            if (_isDisposed) return;            

                if (loginSuccess)
            {
                try
                {
                    
                    //UpdateMenuVisibility(MainStaticClass.Code_right_of_user);
                    Console.WriteLine("=== ВЫПОЛНЕНИЕ ПРОВЕРОК ПРИ СТАРТЕ ===");
                    MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
                    MainStaticClass.Last_Write_Check = DateTime.Now.AddSeconds(1);

                    string version_program = await MainStaticClass.GetAtolDriverVersion();
                    if (_isDisposed) return;

                    this.Title = "Касса   " + MainStaticClass.CashDeskNumber;
                    this.Title += " | " + MainStaticClass.Nick_Shop;
                    this.Title += " | " + MainStaticClass.version();
                    this.Title += " | " + LoadDataWebService.last_date_download_tovars().ToString("yyyy-MM-dd hh:mm:ss");
                    this.Title += " | " + version_program;

                    MainStaticClass.SystemTaxation = await check_system_taxation();

                    if (await MainStaticClass.exist_table_name("constants"))
                    {
                        await check_add_field();
                        _ = InventoryManager.FillDictionaryProductDataAsync(this);
                        _ = Task.Run(() => InventoryManager.DictionaryPriceGiftAction);
                        await UpdateUnloadingPeriod();

                        int intervalMinutes = await MainStaticClass.GetUnloadingInterval();
                        if (intervalMinutes > 0)
                        {
                            _unloadingTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
                            _unloadingTimer.Start();
                            Console.WriteLine($"✓ Таймер выгрузки запущен с интервалом {intervalMinutes} минут");
                        }

                        if (MainStaticClass.CashDeskNumber != 9)
                        {
                            PrintingUsingLibraries printing = new PrintingUsingLibraries();
                            if (MainStaticClass.Use_Fiscall_Print)
                            {
                                printing = new PrintingUsingLibraries();
                                await printing.getShiftStatus(this);
                            }
                            MainStaticClass.validate_date_time_with_fn(3, this);

                            //if (MainStaticClass.SystemTaxation == 0)
                            //{
                            //    if (_isDisposed) return;
                            //    await Task.Delay(250);
                            //    await ShowSafeMessage("У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!", "Проверка системы налогообложения", MessageBoxButton.OK, MessageBoxType.Error);
                            //}

                            if (MainStaticClass.SystemTaxation == 0)
                            {
                                if (_isDisposed) return;

                                // Даем UI потоку закончить отрисовку после окна логина
                                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                                await ShowSafeMessage(
                                    "У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!",
                                    "Проверка системы налогообложения",
                                    MessageBoxButton.OK,
                                    MessageBoxType.Error);

                                // ⚠️ ОШИБКА ФАТАЛЬНАЯ - дальше идти нельзя, закрываем кассу!
                                this.Close();
                                return;
                            }

                            bool restart = false, error = false;
                            MainStaticClass.check_version_fn(ref restart, ref error);
                            if (!error && restart)
                            {
                                if (_isDisposed) return;
                                await Task.Delay(250);
                                await ShowSafeMessage("У вас неверно была установлена версия ФН, необходим перезапуск программы", "Проверка версии ФН", MessageBoxButton.OK, MessageBoxType.Error);
                                this.Close();
                                return;
                            }
                        //}

                        //if (MainStaticClass.CashDeskNumber != 9)
                        //{
                            _= UploadPhoneClients();
                            //await CheckCorectClients();
                            //_= CheckCorectClients();
                            _ = loadBonusClients();
                            if (string.IsNullOrEmpty(MainStaticClass.CDN_Token))
                            {
                                if (_isDisposed) return;
                                await Task.Delay(150);
                                await ShowSafeMessage("В этой кассе не заполнен CDN токен!\r\nПРОДАЖА МАРКИРОВАННОГО ТОВАРА ОГРАНИЧЕНА!", "Проверка cdn токена", MessageBoxButton.OK, MessageBoxType.Error);
                            }
                            else
                            {
                                _ = LoadCdnWithStartAsync(_lifetimeCts.Token);
                            }

                            if (await MainStaticClass.PrintingUsingLibraries(this) == 1)
                            {
                                PrintingUsingLibraries printingUsingLibraries = new PrintingUsingLibraries();
                                await printingUsingLibraries.CheckTaxationTypes(this);
                            }
                        }

                        _ = CheckFilesAndFolders();
                        Console.WriteLine("✓ ВСЕ ПРОВЕРКИ УСПЕШНО ВЫПОЛНЕНЫ");
                    }
                    else
                    {
                        if (_isDisposed) return;
                        await Task.Delay(150);
                        await ShowSafeMessage("В этой бд нет таблицы constatnts, необходимо создать таблицы бд", "Проверка наличия таблицы", MessageBoxButton.OK, MessageBoxType.Error);
                    }
                    
                    UpdateMenuVisibility(MainStaticClass.Code_right_of_user);
                    _viewModel.OpenCashChecks();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Критическая ошибка: {ex.Message}");
                    if (!_isDisposed)
                    {
                        await Task.Delay(150);
                        await ShowSafeMessage($"✗ Критическая ошибка: {ex.Message}", "Старт программы", MessageBoxButton.OK, MessageBoxType.Error);
                        this.Close();
                    }
                }

                MainStaticClass.delete_all_events_in_log(MainStaticClass.GetMinDateWorkLogs);
            }
            else
            {
                this.Close();
            }

            if (await MainStaticClass.GetUnloadingInterval() != 0)
            {
                _ = InitializeTimeSyncAsync(_lifetimeCts.Token).ContinueWith(t =>
                {
                    if (t.IsFaulted) Console.WriteLine($"[TimeSync] Критическая ошибка: {t.Exception?.Message}");
                });
            }
        }
        

//        /// <summary>
//        /// Исправление старого типа колонки 'action_num_doc'
//        /// </summary>
//        private async Task<bool> check_correct_type_column()
//        {
//            bool update = false;
//#if DEBUG
//            if (System.Diagnostics.Debugger.IsAttached)
//            {
//                System.Diagnostics.Debugger.Break();
//            }
//#endif

//            // 1. Используем using для гарантированного закрытия соединения
//            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
//            {
//                try
//                {
//                    await conn.OpenAsync();
//                }
//                catch (InvalidOperationException) { /* Игнорируем, если уже открыто */ }

//                try
//                {
//                    // 2. Транзакция убрана, так как это только чтение (SELECT)
//                    string query = "SELECT data_type FROM information_schema.columns WHERE table_name = 'checks_header' AND column_name = 'comment'";

//                    using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
//                    {
//                        // 3. ExecuteScalar быстрее и проще, если нужно получить одно значение (тип данных)
//                        // Если вернется null, значит колонки нет (но по логике проверяем тип)
//                        var result = await command.ExecuteScalarAsync();

//                        if (result != null && result.ToString() != "varchar(100)")
//                        {
//                            update = true;
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Ошибка при чтении типа колонки: {ex.Message}");
//                    return false;
//                }
//            }
//            // Соединение закрыто здесь

//            // 4. Обновление запускаем ТОЛЬКО если чтение завершено и соединение освобождено
//            if (update)
//            {
//                SettingConnect sc = new SettingConnect();
//                await sc.AddField_Click(this);
//                this.Close();
//                return true;
//            }

//            return false;
//        }


               /// <summary>
        /// Проверяет, что колонка "comment" в таблице "checks_header" имеет тип varchar(50).
        /// Возвращает true, если тип некорректен/отсутствует (и окно было закрыто). Возвращает false, если всё нормально.
        /// </summary>
        private async Task<bool> check_correct_type_column()
        {
            const string targetSchema = "public";          
            const string targetTable = "checks_header";
            const string targetColumn = "comment";
            const string expectedType = "character varying";
            const int expectedMaxLength = 50;

            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
            {
                await conn.OpenAsync();   

                string query = @"
            SELECT data_type, character_maximum_length
            FROM information_schema.columns
            WHERE table_schema = @schema
              AND table_name = @table
              AND column_name = @column";

                using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@schema", targetSchema);
                    command.Parameters.AddWithValue("@table", targetTable);
                    command.Parameters.AddWithValue("@column", targetColumn);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            // Колонка не найдена
                            Console.WriteLine($"Колонка {targetSchema}.{targetTable}.{targetColumn} не существует.");
                            SettingConnect sc = new SettingConnect();
                            await sc.AddField_Click(this);
                            this.Close();
                            return true; // ✅ Была ошибка, вызвали Close, возвращаем true
                        }

                        await reader.ReadAsync();
                        string currentType = reader.GetString(0);
                        int? currentLength = reader.IsDBNull(1) ? null : reader.GetInt32(1);

                        bool isCorrect = (currentType == expectedType && currentLength == expectedMaxLength);
                        
                        if (!isCorrect)
                        {
                            SettingConnect sc = new SettingConnect();
                            await sc.AddField_Click(this);
                            this.Close();
                            return true; // ✅ Была ошибка, вызвали Close, возвращаем true
                        }
                        
                        return false; // ✅ Все нормально, возвращаем false
                    }
                }
            }
        }


        private async Task<bool> check_exists_column()
        {
            using (var conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    await conn.OpenAsync();
                    string query = "SELECT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'checks_header' AND column_name = 'extra');";

                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || !Convert.ToBoolean(result))
                        {
                            SettingConnect sc = new SettingConnect();
                            await sc.AddField_Click(this);
                            this.Close();
                            return true; // ✅ Вызвали Close, возвращаем true
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        MessageBox.Show(ex.Message, "check_exists_column", this));
                }
            }
            return false; // ✅ Все нормально, возвращаем false
        }
        private async Task<bool> check_exists_table()
        {
            using (var conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    await conn.OpenAsync();
                    string query = "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'open_close_shop');";

                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || !Convert.ToBoolean(result))
                        {
                            SettingConnect sc = new SettingConnect();
                            await sc.AddField_Click(this);
                            this.Close();
                            return true; // ✅ Вызвали Close, возвращаем true
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        MessageBox.Show(ex.Message, "check_exists_table", this));
                }
            }
            return false; // ✅ Все нормально, возвращаем false
        }
        
        ///// <summary>
        ///// Исправление старого типа автор в колонке и проверка наличия таблиц/колонок
        ///// </summary>
        private async Task check_add_field()
        {
            // Если метод вернул true (была проблема, окно закрывается), 
            // прерываем выполнение остальных проверок!
            if (await check_correct_type_column()) return;
            if (await check_exists_table()) return;
            if (await check_exists_column()) return;
        }

        private async Task InitializeTimeSyncAsync(CancellationToken token, int maxAttempts = 100, int timeoutSeconds = 15, int maxDelaySeconds = 600)
        {
            Console.WriteLine($"[TimeSync] Запуск инициализации (попыток: {maxAttempts}, таймаут: {timeoutSeconds}с)");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("[TimeSync] Отменено при закрытии окна.");
                    return;
                }

                try
                {
                    Console.WriteLine($"[TimeSync] Попытка {attempt} из {maxAttempts}...");
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                    DateTime serverTime = await GetServerTimeOnStartupAsync(linkedCts.Token);
                    Console.WriteLine($"[TimeSync] ✅ УСПЕХ! Попытка {attempt}: {serverTime:HH:mm:ss}");
                    TimeSync.SetInitialTime(serverTime);
                    return;
                }
                catch (OperationCanceledException)
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("[TimeSync] Отменено пользователем.");
                        return;
                    }
                    Console.WriteLine($"[TimeSync] Попытка {attempt}: таймаут ({timeoutSeconds}с)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TimeSync] Попытка {attempt}: ошибка - {ex.Message}");
                }

                if (attempt < maxAttempts)
                {
                    int delay = 1000 * Math.Min(attempt, maxDelaySeconds);
                    try { await Task.Delay(delay, token); }
                    catch (OperationCanceledException) { return; }
                }
            }
            Console.WriteLine($"[TimeSync] ⚠ Не удалось инициализировать после {maxAttempts} попыток");
        }

        private static async Task<DateTime> GetServerTimeOnStartupAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 60000;
                token.ThrowIfCancellationRequested();
                var result = ds.GetDateTimeServer();
                token.ThrowIfCancellationRequested();
                return result;
            }, token);
        }

        public class Users { public List<User> list_users { get; set; } }
        public class User
        {
            public string shop { get; set; }
            public string user_id { get; set; }
            public string name { get; set; }
            public string rights { get; set; }
            public string password_m { get; set; }
            public string password_b { get; set; }
            public string fiscals_forbidden { get; set; }
        }

        //private async Task GetUsers(CancellationToken token)
        //{
        //    try
        //    {
        //        //System.Diagnostics.Debugger.Break();
        //        token.ThrowIfCancellationRequested();
        //        //DS ds = MainStaticClass.get_ds();
        //        DS ds = await ServiceLocator.DsAsync();

        //        ds.Timeout = 20000;
        //        string nick_shop = MainStaticClass.Nick_Shop.Trim();

        //        if (nick_shop.Length == 0)
        //        {
        //            await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show(" Не удалось получить название магазина ", "Проверка названия магазина", this));
        //            return;
        //        }

        //        string code_shop = MainStaticClass.Code_Shop.Trim();
        //        if (code_shop.Length == 0)
        //        {
        //            await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show(" Не удалось получить код магазина ", "Проверка кода магазина", this));
        //            return;
        //        }

        //        string count_day = CryptorEngine.get_count_day();
        //        string key = nick_shop + count_day + code_shop;
        //        string encrypt_string = CryptorEngine.Encrypt(nick_shop + "|" + code_shop, true, key);

        //        string answer = "";
        //        try
        //        {
        //            token.ThrowIfCancellationRequested();
        //            answer = ds.GetUsers(MainStaticClass.Nick_Shop, encrypt_string, "4");
        //        }                
        //        catch (System.Net.WebException ex)
        //        {
        //            HandleWebException(ex, "GetUsers");
        //            return;
        //        }
        //        catch (Exception ex)
        //        {
        //            if (token.IsCancellationRequested) return;
        //            await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли ошибки при получении пользователей от веб сервиса " + ex.Message + ".", "Синхронизация пользователей", this));
        //            return;
        //        }

        //        if (string.IsNullOrEmpty(answer)) return;
        //        token.ThrowIfCancellationRequested();

        //        string decrypt_string = CryptorEngine.Decrypt(answer, true, key);
        //        Users users = JsonConvert.DeserializeObject<Users>(decrypt_string);

        //        using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
        //        {
        //            NpgsqlTransaction? trans = null;
        //            try
        //            {
        //                conn.Open();
        //                trans = conn.BeginTransaction();

        //                // 1. Сброс прав (параметры не нужны, нет внешних данных)
        //                string query = "UPDATE users SET rights=13";
        //                using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
        //                {
        //                    command.Transaction = trans;
        //                    command.ExecuteNonQuery();
        //                }

        //                // 2. Цикл обновления пользователей
        //                foreach (User user in users.list_users)
        //                {
        //                    if (token.IsCancellationRequested) { trans.Rollback(); return; }

        //                    // ✅ ИСПРАВЛЕНО: Используем параметризованные запросы

        //                    // A. Удаление старой записи
        //                    string deleteQuery = "DELETE FROM public.users WHERE inn = @inn";
        //                    using (NpgsqlCommand cmdDelete = new NpgsqlCommand(deleteQuery, conn))
        //                    {
        //                        cmdDelete.Transaction = trans;
        //                        cmdDelete.Parameters.AddWithValue("@inn", user.user_id);
        //                        cmdDelete.ExecuteNonQuery();
        //                    }

        //                    // B. Вставка новой записи
        //                    string insertQuery = @"INSERT INTO users 
        //                        (code, name, rights, shop, password_m, password_b, inn, fiscals_forbidden) 
        //                        VALUES 
        //                        (@code, @name, @rights, @shop, @password_m, @password_b, @inn, @fiscals_forbidden)";

        //                    using (NpgsqlCommand cmdInsert = new NpgsqlCommand(insertQuery, conn))
        //                    {
        //                        cmdInsert.Transaction = trans;
        //                        cmdInsert.Parameters.AddWithValue("@code", user.user_id);
        //                        cmdInsert.Parameters.AddWithValue("@name", user.name); // ✅ Кавычки обрабатываются драйвером автоматически
        //                        cmdInsert.Parameters.AddWithValue("@rights", Convert.ToInt32(user.rights));
        //                        cmdInsert.Parameters.AddWithValue("@shop", user.shop);
        //                        cmdInsert.Parameters.AddWithValue("@password_m", user.password_m);
        //                        cmdInsert.Parameters.AddWithValue("@password_b", user.password_b);
        //                        cmdInsert.Parameters.AddWithValue("@inn", user.user_id);
        //                        cmdInsert.Parameters.AddWithValue("@fiscals_forbidden", Convert.ToBoolean(user.fiscals_forbidden));

        //                        cmdInsert.ExecuteNonQuery();
        //                    }
        //                }
        //                trans.Commit();
        //                Console.WriteLine("Пользователи успешно обновлены.");
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                if (trans != null) trans.Rollback();
        //                if (!token.IsCancellationRequested)
        //                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли ошибки sql при обновлении пользователей " + ex.Message, "Ошибки при обновлении пользователей", this));
        //            }
        //            catch (Exception ex)
        //            {
        //                if (trans != null) trans.Rollback();
        //                if (!token.IsCancellationRequested)
        //                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли общие ошибки при обновлении пользователей " + ex.Message, "Ошибки при обновлении пользователей", this));
        //            }
        //        }
        //    }
        //    catch (OperationCanceledException) { Console.WriteLine("GetUsers: операция отменена."); }
        //    catch (Exception ex) { Console.WriteLine($"Критическая ошибка в GetUsers: {ex.Message}"); }
        //}

        // ✅ 1. Меняем возвращаемый тип на bool (true - успех, false - ошибка)
        private async Task<bool> GetUsers(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                DS ds = await ServiceLocator.DsAsync();
                ds.Timeout = 20000;

                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Length == 0)
                    throw new InvalidOperationException("Не удалось получить название магазина");

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Length == 0)
                    throw new InvalidOperationException("Не удалось получить код магазина");

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop + count_day + code_shop;
                string encrypt_string = CryptorEngine.Encrypt(nick_shop + "|" + code_shop, true, key);

                string answer = "";
                try
                {
                    token.ThrowIfCancellationRequested();
                    answer = ds.GetUsers(MainStaticClass.Nick_Shop, encrypt_string, "4");
                }
                catch (System.Net.WebException ex)
                {
                    HandleWebException(ex, "GetUsers");
                    // ✅ 2. Не показываем MessageBox, а просто выбрасываем понятное исключение
                    throw new InvalidOperationException("Ошибка сети при получении пользователей", ex);
                }

                if (string.IsNullOrEmpty(answer))
                    throw new InvalidOperationException("Сервер вернул пустой ответ");

                token.ThrowIfCancellationRequested();

                string decrypt_string = CryptorEngine.Decrypt(answer, true, key);
                Users users = JsonConvert.DeserializeObject<Users>(decrypt_string);

                if (users == null || users.list_users == null)
                    throw new InvalidOperationException("Не удалось расшифровать список пользователей");

                // ✅ 3. Убираем вложенный try-catch для БД. Пусть ошибка летит в общий catch
                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                {
                    await conn.OpenAsync();
                    using (NpgsqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            string query = "UPDATE users SET rights=13";
                            using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                            {
                                command.Transaction = trans;
                                await command.ExecuteNonQueryAsync();
                            }

                            foreach (User user in users.list_users)
                            {
                                token.ThrowIfCancellationRequested();

                                string deleteQuery = "DELETE FROM public.users WHERE inn = @inn";
                                using (NpgsqlCommand cmdDelete = new NpgsqlCommand(deleteQuery, conn))
                                {
                                    cmdDelete.Transaction = trans;
                                    cmdDelete.Parameters.AddWithValue("@inn", user.user_id);
                                    await cmdDelete.ExecuteNonQueryAsync();
                                }

                                string insertQuery = @"INSERT INTO users 
                            (code, name, rights, shop, password_m, password_b, inn, fiscals_forbidden) 
                            VALUES (@code, @name, @rights, @shop, @password_m, @password_b, @inn, @fiscals_forbidden)";

                                using (NpgsqlCommand cmdInsert = new NpgsqlCommand(insertQuery, conn))
                                {
                                    cmdInsert.Transaction = trans;
                                    cmdInsert.Parameters.AddWithValue("@code", user.user_id);
                                    cmdInsert.Parameters.AddWithValue("@name", user.name);
                                    cmdInsert.Parameters.AddWithValue("@rights", Convert.ToInt32(user.rights));
                                    cmdInsert.Parameters.AddWithValue("@shop", user.shop);
                                    cmdInsert.Parameters.AddWithValue("@password_m", user.password_m);
                                    cmdInsert.Parameters.AddWithValue("@password_b", user.password_b);
                                    cmdInsert.Parameters.AddWithValue("@inn", user.user_id);
                                    cmdInsert.Parameters.AddWithValue("@fiscals_forbidden", Convert.ToBoolean(user.fiscals_forbidden));
                                    await cmdInsert.ExecuteNonQueryAsync();
                                }
                            }

                            await trans.CommitAsync();
                            Console.WriteLine("Пользователи успешно обновлены.");
                            return true; // ✅ Явный сигнал об успехе
                        }
                        catch
                        {
                            await trans.RollbackAsync();
                            throw; // ✅ Пробрасываем ошибку БД дальше
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                MainStaticClass.WriteRecordErrorLog("GetUsers: операция отменена.", "GetUsers", 0, MainStaticClass.CashDeskNumber,"Синхрон пользователей");
                Console.WriteLine("GetUsers: операция отменена.");
                return false; // Отмена - это не ошибка, но и не успех
            }
            catch (Exception ex)
            {
                // ✅ 4. Единая точка сбора ошибок. Логируем, НО НЕ ПОКАЗЫВАЕМ MessageBox!
                Console.WriteLine($"Ошибка в GetUsers: {ex.Message}");
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "GetUsers");
                return false; // ✅ Явный сигнал о провале
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.F12)
            {
                e.Handled = true;
                _ = ShowAuthorizationWindow();
            }
        }

        private async Task ShowAuthorizationWindow()
        {
            try
            {
                var loginWindow = new Interface_switching();
                bool loginSuccess = false;

                loginWindow.AuthorizationSuccess += (s, password) => { loginSuccess = true; loginWindow.Close(); };
                loginWindow.AuthorizationCancel += (s, args) => { loginSuccess = false; loginWindow.Close(); };

                await loginWindow.ShowDialog(this);

                if (loginSuccess)
                {
                    UpdateMenuVisibility(MainStaticClass.Code_right_of_user);
                    _viewModel.OpenCashChecks();
                }
                else { this.Close(); }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при показе окна авторизации: {ex.Message}");
                // ✅ ИСПРАВЛЕНО: Передаем this
                await MessageBoxHelper.Show($"Ошибка: {ex.Message}", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxType.Error, this);
            }
        }

        private void UpdateMenuVisibility(int userRights)
        {
            //var menu = MainMenu ?? this.FindControl<Menu>("MainMenu");
            //if (menu != null) menu.IsVisible = userRights != 2;
            var menu = this.FindControl<Menu>("MainMenu");
            if (menu != null)
            {
                // Логика:
                // 1. Если userRights == 0 (при старте) -> Скрываем (false)
                // 2. Если userRights == 2 (ограниченные права) -> Скрываем (false)
                // 3. Во всех остальных случаях -> Показываем (true)

                menu.IsVisible = userRights > 0 && userRights != 2;
            }
        }

        private async Task UpdateUnloadingPeriod()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                await conn.OpenAsync();
                string query = "UPDATE constants SET unloading_period = 4 WHERE unloading_period > 0";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Период выгрузки обновлен в БД");
            }
            catch (Exception ex)
            {
                // ✅ Проверка перед UI
                if (!_isDisposed)
                {
                    await MessageBoxHelper.Show($"Ошибка при проверке/установке значения периода выгрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                }
                Console.WriteLine($"✗ Общая ошибка в UpdateUnloadingPeriod: {ex.Message}");
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        private async void UnloadingTimer_Tick(object? sender, EventArgs e)
        {
            // ✅ Добавили null вторым аргументом
            _ = PerformUnloadAsync(_lifetimeCts.Token, null).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    MainStaticClass.WriteRecordErrorLog(t.Exception, 0, MainStaticClass.CashDeskNumber, "Ошибка периодической выгрузки");
                    Console.WriteLine($"✗ Ошибка в таймере: {t.Exception.Message}");
                }
            }, TaskScheduler.Default);
        }


        //private async Task PerformUnloadAsync(CancellationToken ct)
        //{
        //    await Task.Run(async () =>
        //    {
        //        try
        //        {
        //            Console.WriteLine($"=== Запуск выгрузки данных ({DateTime.Now:HH:mm:ss}) ===");
        //            MainStaticClass.SendOnlineStatus();
        //            ct.ThrowIfCancellationRequested();

        //            if (MainStaticClass.Last_Write_Check > MainStaticClass.Last_Send_Last_Successful_Sending)
        //            {
        //                await MainStaticClass.SendOnlineStatus();

        //                try { ct.ThrowIfCancellationRequested(); var sdsp = new SendDataOnSalesPortions(); sdsp.send_sales_data_Click(null, null); Console.WriteLine("✓ Данные о продажах отправлены"); }
        //                catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки продаж"); Console.WriteLine($"✗ Продажи: {ex.Message}"); }

        //                try { ct.ThrowIfCancellationRequested(); UploadDeletedItems(); Console.WriteLine("✓ Удаленные элементы отправлены"); }
        //                catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки удаленных элементов"); Console.WriteLine($"✗ Удаленные: {ex.Message}"); }

        //                try { ct.ThrowIfCancellationRequested(); send_cdn_logs(); Console.WriteLine("✓ CDN логи отправлены"); }
        //                catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки CDN логов"); Console.WriteLine($"✗ CDN: {ex.Message}"); }

        //                try { ct.ThrowIfCancellationRequested(); UploadErrorsLog(); Console.WriteLine("✓ Логи ошибок отправлены"); }
        //                catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки логов ошибок"); Console.WriteLine($"✗ Логи: {ex.Message}"); }

        //                try { ct.ThrowIfCancellationRequested(); sent_open_close_shop(); Console.WriteLine("✓ Данные о сменах отправлены"); }
        //                catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки данных о сменах"); Console.WriteLine($"✗ Смены: {ex.Message}"); }

        //                MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
        //                Console.WriteLine("✓ Выгрузка завершена");
        //            }
        //            else { Console.WriteLine("⚠ Нет новых данных для выгрузки"); }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            Console.WriteLine("Выгрузка прервана по таймауту");
        //            MainStaticClass.WriteRecordErrorLog("Выгрузка прервана по таймауту", "PerformUnloadAsync", 0, MainStaticClass.CashDeskNumber, "CancellationToken");
        //            throw;
        //        }
        //        catch (Exception ex)
        //        {
        //            MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Непредвиденная ошибка в PerformUnloadAsync");
        //            Console.WriteLine($"✗ Критическая ошибка: {ex}");
        //            throw;
        //        }
        //    }, ct);
        //}

        // ✅ Добавили параметр IProgress<string> (стандартный паттерн C# для отчета о прогрессе)
        private async Task PerformUnloadAsync(CancellationToken ct, IProgress<string> progress)
        {
            await Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"=== Запуск выгрузки данных ({DateTime.Now:HH:mm:ss}) ===");
                    progress?.Report("Отправка статуса кассы..."); // 0-й этап

                    await MainStaticClass.SendOnlineStatus();
                    ct.ThrowIfCancellationRequested();

                    if (MainStaticClass.Last_Write_Check > MainStaticClass.Last_Send_Last_Successful_Sending)
                    {
                        // Обновляем общий счетчик этапов
                        progress?.Report("Этап 1 из 5: Отправка чеков...");
                        try
                        {
                            ct.ThrowIfCancellationRequested();
                            var sdsp = new SendDataOnSalesPortions();
                            await sdsp.send_sales_data_Click(null, null);
                            Console.WriteLine("✓ Данные о продажах отправлены");
                        }
                        catch (Exception ex)
                        {
                            MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки продаж");
                            Console.WriteLine($"✗ Продажи: {ex.Message}");
                            // Можно добавить пометку об ошибке прямо в текст
                            progress?.Report("Этап 1 из 5: Ошибка отправки чеков");
                        }
// #if DEBUG
//                         if (System.Diagnostics.Debugger.IsAttached)
//                         {
//                             System.Diagnostics.Debugger.Break();
//                         }
// #endif
                        progress?.Report("Этап 2 из 5: Отправка удалений...");
                        try { ct.ThrowIfCancellationRequested(); await UploadDeletedItems(); Console.WriteLine("✓ Удаленные элементы отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки удаленных элементов"); Console.WriteLine($"✗ Удаленные: {ex.Message}"); progress?.Report("Этап 2 из 5: Ошибка удалений"); }
// #if DEBUG
//                         if (System.Diagnostics.Debugger.IsAttached)
//                         {
//                             System.Diagnostics.Debugger.Break();
//                         }
// #endif
                        progress?.Report("Этап 3 из 5: Отправка марок (CDN)...");
                        //try { ct.ThrowIfCancellationRequested(); await send_cdn_logs(); Console.WriteLine("✓ CDN логи отправлены"); }
                        //catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки CDN логов"); Console.WriteLine($"✗ CDN: {ex.Message}"); progress?.Report("Этап 3 из 5: Ошибка CDN"); }
// #if DEBUG
//                         if (System.Diagnostics.Debugger.IsAttached)
//                         {
//                             System.Diagnostics.Debugger.Break();
//                         }
// #endif
                        progress?.Report("Этап 4 из 5: Отправка логов ошибок...");
                        try { ct.ThrowIfCancellationRequested(); await UploadErrorsLog(); Console.WriteLine("✓ Логи ошибок отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки логов ошибок"); Console.WriteLine($"✗ Логи: {ex.Message}"); progress?.Report("Этап 4 из 5: Ошибка логов"); }
// #if DEBUG
//                         if (System.Diagnostics.Debugger.IsAttached)
//                         {
//                             System.Diagnostics.Debugger.Break();
//                         }
// #endif
                        progress?.Report("Этап 5 из 5: Отправка данных о сменах...");
                        try { ct.ThrowIfCancellationRequested(); await sent_open_close_shop(); Console.WriteLine("✓ Данные о сменах отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки данных о сменах"); Console.WriteLine($"✗ Смены: {ex.Message}"); progress?.Report("Этап 5 из 5: Ошибка смен"); }

                        progress?.Report("✅ Все данные успешно отправлены!");
                        MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
                        Console.WriteLine("✓ Выгрузка завершена");
                    }
                    else
                    {
                        progress?.Report("✅ Нет новых данных для выгрузки");
                        Console.WriteLine("⚠ Нет новых данных для выгрузки");
                    }
                }
                catch (OperationCanceledException)
                {
                    progress?.Report("⏱ Время ожидания истекло. Закрытие...");
                    Console.WriteLine("Выгрузка прервана по таймауту");
                    MainStaticClass.WriteRecordErrorLog("Выгрузка прервана по таймауту", "PerformUnloadAsync", 0, MainStaticClass.CashDeskNumber, "CancellationToken");
                    throw;
                }
                catch (Exception ex)
                {
                    progress?.Report("❌ Критическая ошибка при выгрузке");
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Непредвиденная ошибка в PerformUnloadAsync");
                    Console.WriteLine($"✗ Критическая ошибка: {ex}");
                    throw;
                }
            }, ct);
        }

        public class PhoneClient
        {
            public string NumPhone { get; set; }
            public string ClientCode { get; set; }
        }

        public class PhonesClients : IDisposable
        {
            public string Version { get; set; }
            public string NickShop { get; set; }
            public string CodeShop { get; set; }
            public List<PhoneClient> ListPhoneClient { get; set; }

            void IDisposable.Dispose()
            {

            }
        }


        private async Task CheckCorectClients()
        {
//#if DEBUG
//            if (System.Diagnostics.Debugger.IsAttached)
//            {
//                System.Diagnostics.Debugger.Break();
//            }
//#endif

            ClientsCompareResult compareResult = await CheckClientsCountSync();

            switch (compareResult)
            {
                case ClientsCompareResult.Equals:
                    // Всё идеально, работаем спокойно
                    Console.WriteLine("Количество клиентов совпадает.");
                    break;

                case ClientsCompareResult.MinorDifference:
                    // Разница есть, но меньше 5000. Не паникуем, фоновую выгрузку не сбрасываем.
                    Console.WriteLine("Небольшая разница в клиентах, допустимо. Синхронизация продолжится в обычном режиме.");
                    break;

                case ClientsCompareResult.MajorDifference:
                    // Разница 5000 и более! Явный обрыв синхронизации.
                    Console.WriteLine("КРИТИЧЕСКАЯ РАЗНИЦА! Проверяем, был ли сброс за последние 3 суток...");

                    try
                    {
                        using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                        {
                            await conn.OpenAsync();

                            // Атомарный запрос: сбросит дату выгрузки на 2000 год ТОЛЬКО если 
                            // последний сброс был БОЛЕЕ 3-х суток назад (или его вообще никогда не было).
                            string query = @"
                        UPDATE public.constants 
                        SET last_date_download_bonus_clients = @resetDate,
                            last_date_reset_bonus_clients = @nowDate
                        WHERE last_date_reset_bonus_clients IS NULL 
                           OR last_date_reset_bonus_clients < (CURRENT_TIMESTAMP - INTERVAL '3 days')";

                            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@resetDate", new DateTime(2000, 1, 1));
                                cmd.Parameters.AddWithValue("@nowDate", DateTime.Now);

                                // ExecuteNonQuery вернет количество измененных строк
                                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                    // Строка обновилась, значит прошло больше 3-х суток. Мы сделали сброс!
                                    Console.WriteLine("Дата синхронизации УСПЕШНО сброшена на 01.01.2000. При следующем цикле пойдет полная выгрузка.");

                                    // Если нужно, чтобы полная выгрузка началась прямо сейчас же, раскомментируйте:
                                    // await load_bonus_clients_internal(false); 
                                }
                                else
                                {
                                    // Строка НЕ обновилась (rowsAffected == 0). 
                                    // Это значит, что в колонке last_date_reset_bonus_clients стоит дата свежее, чем 3 дня назад.
                                    Console.WriteLine("Сброс даты синхронизации УЖЕ ВЫПОЛНЯЛСЯ за последние 3 суток. Повторный сброс отменен.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка при сбросе даты синхронизации клиентов");
                    }
                    break;

                case ClientsCompareResult.Error:
                    // Сеть отвалилась или БД упала. Ничего не делаем, попробуем в следующий раз.
                    Console.WriteLine("Не удалось сверить количество. Пропуск.");
                    break;
            }
        }

        public class CountResult
        {
            public int count { get; set; }
        }

        private async Task<ClientsCompareResult> CheckClientsCountSync()
        {
            int localCount = 0;
            int serverCount = -1;

            // === ШАГ 1: Получаем количество из ЛОКАЛЬНОЙ базы ===
            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    await conn.OpenAsync();
                    string query = "SELECT COUNT(1) FROM clients WHERE code <> ''";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                    {
                        object result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            localCount = Convert.ToInt32(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "CheckClientsCountSync (Локальная БД)");
                    return ClientsCompareResult.Error;
                }
            }

            if (!MainStaticClass.service_is_worker()) return ClientsCompareResult.Error;

            string nick_shop = MainStaticClass.Nick_Shop.Trim();
            string code_shop = MainStaticClass.Code_Shop.Trim();
            if (string.IsNullOrWhiteSpace(nick_shop) || string.IsNullOrWhiteSpace(code_shop)) return ClientsCompareResult.Error;

            // === ШАГ 2: Запрос к серверу ===
            try
            {
                DS ds = await ServiceLocator.DsAsync();
                ds.Timeout = 20000;

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop + count_day + code_shop;
                string payload = $"{nick_shop}|{DateTime.Now.Ticks}|{code_shop}";
                string encrypt_string = CryptorEngine.Encrypt(payload, true, key);

                string answer = ds.GetDiscountClientsCount(nick_shop, encrypt_string, "4");

                if (answer == "-1")
                {
                    MainStaticClass.WriteRecordErrorLog("Сервер вернул -1", "CheckClientsCountSync", 0, MainStaticClass.CashDeskNumber, "");
                    return ClientsCompareResult.Error;
                }

                string decrypt_answer = CryptorEngine.Decrypt(answer, true, key);
                dynamic serverData = JsonConvert.DeserializeObject(decrypt_answer);

                if (serverData != null && serverData.count != null)
                {
                    serverCount = (int)serverData.count;
                }
                else
                {
                    return ClientsCompareResult.Error;
                }

                // === ШАГ 3: СРАВНЕНИЕ С УЧЕТОМ ПОРОГА ===

                // Допустимая разница. Если расхождение больше этого значения - это проблема.
                // Настройте это число под ваши реалии (например, 100 или 500)
                const int CRITICAL_THRESHOLD = 5000;

                // Считаем разницу по модулю (неважно, локальных больше или серверных)
                int difference = Math.Abs(localCount - serverCount);

                Console.WriteLine($"[СВЕРКА КЛИЕНТОВ] Локально: {localCount} | Сервер: {serverCount} | Разница: {difference}");

                if (difference == 0)
                {
                    return ClientsCompareResult.Equals;
                }
                else if (difference <= CRITICAL_THRESHOLD)
                {
                    // Разница есть, но она в пределах нормы (например, 20 человек)
                    MainStaticClass.WriteRecordErrorLog($"Незначительная рассинхронизация клиентов. Локально: {localCount}, Сервер: {serverCount}", "CheckClientsCountSync", 0, MainStaticClass.CashDeskNumber, "ВНИМАНИЕ");
                    return ClientsCompareResult.MinorDifference;
                }
                else
                {
                    // Разница критическая (например, 1000 человек)
                    MainStaticClass.WriteRecordErrorLog($"КРИТИЧЕСКАЯ рассинхронизация клиентов! Локально: {localCount}, Сервер: {serverCount}", "CheckClientsCountSync", 0, MainStaticClass.CashDeskNumber, "ОШИБКА СИНХРОНИЗАЦИИ");
                    return ClientsCompareResult.MajorDifference;
                }
            }
            catch (System.Net.WebException ex)
            {
                HandleWebException(ex, "CheckClientsCountSync");
                return ClientsCompareResult.Error;
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "CheckClientsCountSync (Ошибка при сверке)");
                return ClientsCompareResult.Error;
            }
        }

        public enum ClientsCompareResult
        {
            Equals,          // Идеальное совпадение (или разница = 0)
            MinorDifference, // Небольшая разница (в пределах допуска, не страшно)
            MajorDifference, // Критическая разница (явная проблема, нужен перезалив)
            Error            // Техническая ошибка (упала БД, нет сети и т.д.)
        }

        private async Task UploadPhoneClients()
        {
            //StringBuilder sb = new StringBuilder();
            PhonesClients phonesClients = new PhonesClients();
            phonesClients.CodeShop = MainStaticClass.Code_Shop;
            phonesClients.NickShop = MainStaticClass.Nick_Shop;
            phonesClients.ListPhoneClient = new List<PhoneClient>();

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = " SELECT barcode, phone  FROM temp_phone_clients; ";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    PhoneClient phoneClient = new PhoneClient();
                    phoneClient.NumPhone = reader["phone"].ToString().Trim();
                    phoneClient.ClientCode = reader["barcode"].ToString();
                    phonesClients.ListPhoneClient.Add(phoneClient);
                }
                reader.Close();
                reader.Dispose();

                if (phonesClients.ListPhoneClient.Count == 0)
                {
                    return;
                }

                if (!MainStaticClass.service_is_worker())
                {
                    //MessageBox.Show("Веб сервис недоступен");
                    return;
                }
                
                DS ds = await ServiceLocator.DsAsync();
                ds.Timeout = 20000;

                //Получить параметра для запроса на сервер 
                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0)
                {
                    //MessageBox.Show(" Не удалось получить название магазина ");
                    return;
                }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0)
                {
                    //MessageBox.Show(" Не удалось получить код магазина ");
                    return;
                }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
                string data = JsonConvert.SerializeObject(phonesClients, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string encrypt_string = CryptorEngine.Encrypt(data, true, key);
                //string answer = ds.UploadPhoneClients(nick_shop, encrypt_string,MainStaticClass.GetWorkSchema.ToString());
                string answer = ds.UploadPhoneClients(nick_shop, encrypt_string, "4");
                if (answer == "1")
                {
                    query = "DELETE FROM temp_phone_clients";
                    command = new NpgsqlCommand(query, conn);
                    command.ExecuteNonQuery();
                    command.Dispose();
                }
                else
                {
                    //MessageBox.Show("Произошли ошибки на сервере при передаче телефонов клиентов");
                    MainStaticClass.WriteRecordErrorLog("Произошли ошибки на сервере при передаче телефонов клиентов", "UploadPhoneClients", 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию о телефонах клиентов");
                }
                conn.Close();
            }
            catch (System.Net.WebException ex)
            {
                HandleWebException(ex, "UploadPhoneClients");
            }
            catch (Exception ex)
            {                
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию о телефонах клиентов");                
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        // Вспомогательные классы OpenCloseShop, CdnLogs, DeletedItem, RecordsErrorLog оставляем без изменений
        // ...
        class OpenCloseShop { public DateTime? Open { get; set; } public DateTime? Close { get; set; } public DateTime Date { get; set; } public bool ItsSent { get; set; } }

        private async Task sent_open_close_shop()
        {
            // Получаем данные из БД
            List<OpenCloseShop> closeShops = await get_open_close_shop();

            if (closeShops.Count == 0) return;

            try
            {
                //DS ds = MainStaticClass.get_ds();
                DS ds = await ServiceLocator.DsAsync();
                ds.Timeout = 20000;

                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (string.IsNullOrEmpty(nick_shop)) return;

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (string.IsNullOrEmpty(code_shop)) return;

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop + count_day + code_shop;

                string data = JsonConvert.SerializeObject(closeShops, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string data_crypt = CryptorEngine.Encrypt(data, true, key);

                // Вызов веб-метода
                bool result = ds.UploadOpeningClosingShops(nick_shop, data_crypt, "4");

                if (result)
                {
                    MarkShopsAsSent(closeShops);
                }
            }
            catch (System.Net.WebException ex)
            {
                HandleWebException(ex, "sent_open_close_shop");
            }
            // 2. Ловим остальные ошибки (JSON, БД при записи и т.д.)
            catch (Exception ex)
            {
                // Кэш веб-сервиса НЕ сбрасываем, так как проблема скорее всего не в сети
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "sent_open_close_shop");
            }
        }

        private void MarkShopsAsSent(List<OpenCloseShop> shops)
        {
            if (shops == null || shops.Count == 0) return;
            using (var conn = MainStaticClass.NpgsqlConn())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var shop in shops)
                        {
                            string updateQuery = "UPDATE public.open_close_shop SET its_sent = true WHERE date = @date";
                            using (var cmd = new NpgsqlCommand(updateQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@date", shop.Date.Date);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка при обновлении its_sent");
                    }
                }
            }
        }

        private async Task<List<OpenCloseShop>> get_open_close_shop()
        {
            List<OpenCloseShop> openCloseShops = new List<OpenCloseShop>();
            using (var conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    conn.Open();
                    string query = "SELECT open, close, date, its_sent FROM public.open_close_shop WHERE its_sent = false;";
                    using (var command = new NpgsqlCommand(query, conn))
                    using (var reader = command.ExecuteReader())
                    {
                        int openOrdinal = reader.GetOrdinal("open");
                        int closeOrdinal = reader.GetOrdinal("close");
                        int dateOrdinal = reader.GetOrdinal("date");
                        int itsSentOrdinal = reader.GetOrdinal("its_sent");
                        while (reader.Read())
                        {
                            var openCloseShop = new OpenCloseShop
                            {
                                Open = reader.IsDBNull(openOrdinal) ? (DateTime?)null : reader.GetDateTime(openOrdinal),
                                Close = reader.IsDBNull(closeOrdinal) ? (DateTime?)null : reader.GetDateTime(closeOrdinal),
                                Date = reader.GetDateTime(dateOrdinal),
                                ItsSent = reader.GetBoolean(itsSentOrdinal)
                            };
                            openCloseShops.Add(openCloseShop);
                        }
                    }
                }
                catch (NpgsqlException ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Отправка даты открытия/закрытия магазина"); }
                catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Отправка даты открытия/закрытия магазина"); }
            }
            return openCloseShops;
        }

        public class CdnLogs { public List<CdnLog> ListCdnLog { get; set; } }
        public class CdnLog
        {
            public string NumCash { get; set; }
            public string CdnAnswer { get; set; }
            public string DateShop { get; set; }
            public string NumDoc { get; set; }
            public string Mark { get; set; }
            public string Status { get; set; }
        }

        private async Task send_cdn_logs()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                string query = "SELECT num_cash, date, cdn_answer, numdoc, is_sent, mark, status FROM cdn_log WHERE is_sent=0;";
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                CdnLogs logs = new CdnLogs();
                logs.ListCdnLog = new List<CdnLog>();
                while (reader.Read())
                {
                    CdnLog log = new CdnLog();
                    log.CdnAnswer = reader["cdn_answer"].ToString();
                    log.Mark = reader["mark"].ToString();
                    log.NumCash = MainStaticClass.CashDeskNumber.ToString();
                    log.NumDoc = reader["numdoc"].ToString();
                    log.DateShop = Convert.ToDateTime(reader["date"]).ToString("dd-MM-yyyy HH:mm:ss");
                    log.Status = reader["status"].ToString();
                    logs.ListCdnLog.Add(log);
                }
                reader.Close(); // Важно закрыть ридер перед выполнением следующих команд (Update)

                if (logs.ListCdnLog.Count > 0)
                {
                    //DS ds = MainStaticClass.get_ds();
                    DS ds = await ServiceLocator.DsAsync();
                    ds.Timeout = 20000;
                    string nick_shop = MainStaticClass.Nick_Shop.Trim();
                    if (nick_shop.Trim().Length == 0) return;
                    string code_shop = MainStaticClass.Code_Shop.Trim();
                    if (code_shop.Trim().Length == 0) return;
                    string count_day = CryptorEngine.get_count_day();
                    string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();

                    bool result_web_quey = false;
                    string data = JsonConvert.SerializeObject(logs, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    string data_crypt = CryptorEngine.Encrypt(data, true, key);

                    result_web_quey = ds.UploadCDNLogsPortionJason(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString());

                    if (result_web_quey)
                    {
                        foreach (CdnLog log in logs.ListCdnLog)
                        {
                            // ✅ ИСПРАВЛЕНО: Параметризованный запрос (безопасно и надежно)
                            query = "UPDATE cdn_log SET is_sent = 1 WHERE date = @date";
                            using (NpgsqlCommand updateCmd = new NpgsqlCommand(query, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@date", DateTime.Parse(log.DateShop));
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (System.Net.WebException ex)
            {
                HandleWebException(ex, "send_cdn_logs");                
            }
            // 2. Ошибки PostgreSQL - кэш веб-сервиса НЕ трогаем
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"[DB] Ошибка БД: {ex.Message}");
            }
            // 3. Прочие ошибки (сериализация и т.д.) - кэш НЕ трогаем
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Общая ошибка: {ex.Message}");
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        /// <summary>
        /// Централизованная обработка ошибок сети.
        /// Сбрасывает кэш DNS при таймауте или недоступности сервера.
        /// </summary>
        private void HandleWebException(System.Net.WebException ex, string context)
        {
            if (ex.Status == System.Net.WebExceptionStatus.Timeout ||
                ex.Status == System.Net.WebExceptionStatus.ConnectFailure)
            {
                Console.WriteLine($"[WebService] {context}: Ошибка сети ({ex.Status}). Сброс кэша DNS...");
                MainStaticClass.ResetDsCache();               
            }
            else
            {
                // Для ошибок протокола (404, 500) кэш не сбрасываем, просто пишем лог
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, $"{context} (WebException: {ex.Status})");
            }
        }

        public class DeletedItem
        {
            public string num_doc { get; set; }
            public string num_cash { get; set; }
            public string date_time_start { get; set; }
            public string date_time_action { get; set; }
            public string tovar { get; set; }
            public string quantity { get; set; }
            public string type_of_operation { get; set; }
            public string guid { get; set; }
            public string autor { get; set; }
            public string reason { get; set; }
        }

        public class DeletedItems : IDisposable
        {
            public string Version { get; set; }
            public string NickShop { get; set; }
            public string CodeShop { get; set; }
            public List<DeletedItem> ListDeletedItem { get; set; }
            void IDisposable.Dispose() { }
        }

        private async Task UploadDeletedItems()
        {
            DeletedItems deletedItems = new DeletedItems();
            deletedItems.CodeShop = MainStaticClass.Code_Shop;
            deletedItems.NickShop = MainStaticClass.Nick_Shop;
            deletedItems.ListDeletedItem = new List<DeletedItem>();
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = "SELECT num_doc, num_cash, date_time_start, date_time_action, tovar, quantity, type_of_operation,guid,reason FROM deleted_items;";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DeletedItem deletedItem = new DeletedItem();
                    deletedItem.num_doc = reader["num_doc"].ToString();
                    deletedItem.num_cash = reader["num_cash"].ToString();
                    deletedItem.date_time_start = reader["date_time_start"].ToString();
                    deletedItem.date_time_action = reader["date_time_action"].ToString();
                    deletedItem.tovar = reader["tovar"].ToString();
                    deletedItem.quantity = reader["quantity"].ToString();
                    deletedItem.type_of_operation = reader["type_of_operation"].ToString();
                    deletedItem.guid = reader["guid"].ToString();
                    deletedItem.autor = MainStaticClass.CashOperatorInn;
                    deletedItem.reason = reader["reason"].ToString();
                    deletedItems.ListDeletedItem.Add(deletedItem);
                }
                reader.Close();
                reader.Dispose();

                if (deletedItems.ListDeletedItem.Count == 0) return;

                if (!MainStaticClass.service_is_worker()) return;

                //DS ds = MainStaticClass.get_ds();
                DS ds = await ServiceLocator.DsAsync();
                ds.Timeout = 20000;

                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0) { Console.WriteLine("Не удалось получить название магазина (UploadDeletedItems)"); return; }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0) { Console.WriteLine("Не удалось получить код магазина (UploadDeletedItems)"); return; }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
                string data = JsonConvert.SerializeObject(deletedItems, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string encrypt_string = CryptorEngine.Encrypt(data, true, key);
                string answer = ds.UploadDeletedItems(nick_shop, encrypt_string, MainStaticClass.GetWorkSchema.ToString());

                if (answer == "1")
                {
                    query = "DELETE FROM deleted_items";
                    command = new NpgsqlCommand(query, conn);
                    command.ExecuteNonQuery();
                }
                else { MainStaticClass.WriteRecordErrorLog("Произошли ошибки при передаче удаленных строк", "UploadDeletedItems", 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию об удаленных строках"); }
                command.Dispose();
                conn.Close();
            }
            catch (System.Net.WebException ex)
            {                
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "UploadDeletedItems");
                HandleWebException(ex, "UploadDeletedItems");
            }
            // 2. Ловим остальные ошибки (JSON, БД при записи и т.д.)
            catch (Exception ex)
            {
                // Кэш веб-сервиса НЕ сбрасываем, так как проблема скорее всего не в сети
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "UploadDeletedItems");
            }
            finally { if (conn.State == ConnectionState.Open) conn.Close(); }
        }

        private async Task UploadErrorsLog()
        {
            try
            {
                var recordsErrorLog = ReadErrorLogsFromDatabase();
                if (recordsErrorLog.ErrorLogs.Count > 0)
                {
                    bool uploadResult = await UploadErrorLogsToServer(recordsErrorLog);
                    if (uploadResult) DeleteErrorLogsFromDatabase(recordsErrorLog);
                }
            }
            catch (System.Net.WebException ex)
            {
                HandleWebException(ex, "UploadErrorsLog");
            }
            // 2. Остальные ошибки (БД, JSON)
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Произошла ошибка при загрузке логов ошибок");
            }
        }

        public class RecordsErrorLog { public string Shop { get; set; } public short CashDeskNumber { get; set; } public List<RecordErrorLog> ErrorLogs { get; set; } = new List<RecordErrorLog>(); }
        public class RecordErrorLog { public string ErrorMessage { get; set; } public string MethodName { get; set; } public long NumDoc { get; set; } public string Description { get; set; } public DateTime DateTimeRecord { get; set; } }

        private RecordsErrorLog ReadErrorLogsFromDatabase()
        {
            RecordsErrorLog recordsErrorLog = new RecordsErrorLog();
            recordsErrorLog.Shop = MainStaticClass.Nick_Shop;
            recordsErrorLog.CashDeskNumber = Convert.ToInt16(MainStaticClass.CashDeskNumber);

            using (var connection = MainStaticClass.NpgsqlConn())
            {
                connection.Open();
                string query = "SELECT error_message, date_time_record, num_doc, method_name, description FROM public.errors_log";
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var logError = new RecordErrorLog
                        {
                            ErrorMessage = reader["error_message"].ToString().Trim(),
                            DateTimeRecord = reader.GetDateTime(reader.GetOrdinal("date_time_record")),
                            NumDoc = reader.GetInt64(reader.GetOrdinal("num_doc")),
                            MethodName = reader["method_name"].ToString().Trim(),
                            Description = reader["description"].ToString().Trim()
                        };
                        recordsErrorLog.ErrorLogs.Add(logError);
                    }
                }
            }
            return recordsErrorLog;
        }

        private async Task<bool> UploadErrorLogsToServer(RecordsErrorLog recordsErrorLog)
        {
            string nick_shop = MainStaticClass.Nick_Shop.Trim();
            string code_shop = MainStaticClass.Code_Shop.Trim();
            if (string.IsNullOrEmpty(nick_shop) || string.IsNullOrEmpty(code_shop)) return false;

            string count_day = CryptorEngine.get_count_day();
            string key = nick_shop + count_day + code_shop;
            string data = JsonConvert.SerializeObject(recordsErrorLog, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            string data_crypt = CryptorEngine.Encrypt(data, true, key);

            //DS ds = MainStaticClass.get_ds();
            DS ds = await ServiceLocator.DsAsync();
            ds.Timeout = 20000;

            // ✅ УБРАЛИ try-catch. Теперь, если сервер упадет (Timeout), исключение полетит в UploadErrorsLog
            return ds.UploadErrorLogPortionJson(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString());
        }

        private void DeleteErrorLogsFromDatabase(RecordsErrorLog recordsErrorLog)
        {
            using (var connection = MainStaticClass.NpgsqlConn())
            {
                connection.Open();
                foreach (var recordErrorLog in recordsErrorLog.ErrorLogs)
                {
                    string query = "DELETE FROM public.errors_log WHERE date_time_record = @DateTimeRecord";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DateTimeRecord", recordErrorLog.DateTimeRecord);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void get_cdn_with_start() { CDN.CDN_List list = MainStaticClass.CDN_List; }

        private async Task LoadCdnWithStartAsync(CancellationToken externalToken)
        {
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(60));
                await Task.Run(() => get_cdn_with_start(), linkedCts.Token);
            }
            catch (OperationCanceledException) { Console.WriteLine("Загрузка CDN отменена (таймаут или закрытие окна)."); }
            catch (Exception ex) { await MessageBoxHelper.Show($"При загрузке CDN произошла ошибка: {ex.Message}","Загрузка CDN",this); }
        }

        private async Task CheckFilesAndFolders()
        {
            try
            {
                string startupPath = AppContext.BaseDirectory;
                string folderPathPictures = Path.Combine(startupPath, "Pictures2");
                await Task.Run(() =>
                {
                    if (!Directory.Exists(folderPathPictures))
                    {
                        Directory.CreateDirectory(folderPathPictures);
                        Console.WriteLine($"Папка создана: {folderPathPictures}");
                    }
                    else { _ = ClearFolder(folderPathPictures); Console.WriteLine($"Папка очищена: {folderPathPictures}"); }
                });
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Проверка/создание файлов и папок");
                await Dispatcher.UIThread.InvokeAsync(async () => { await MessageBoxHelper.Show($"Ошибка при работе с папкой Pictures2: {ex.Message}", "Ошибка", this); });
            }
        }

        private async Task ClearFolder(string folderPath)
        {
            try
            {
                foreach (string file in Directory.GetFiles(folderPath)) { try { File.Delete(file); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить файл {file}: {ex.Message}"); } }
                foreach (string subFolder in Directory.GetDirectories(folderPath)) { try { Directory.Delete(subFolder, true); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить папку {subFolder}: {ex.Message}"); } }
            }
            catch (Exception ex) { throw new Exception($"Ошибка при очистке папки {folderPath}: {ex.Message}", ex); }
        }

        private async Task loadBonusClients()
        {
            await CheckCorectClients();
            LoadDataWebService ld = new LoadDataWebService();
            await Task.Run(() => ld.load_bonus_clients(false));
        }

        // ✅ Пункт 3 и 4: Исправленный метод
        private async Task<int> check_system_taxation()
        {
            int result = 0;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT system_taxation FROM constants";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
            }
            catch (NpgsqlException ex)
            {
                // ✅ Пункт 4: Dispatcher и передача this
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBoxHelper.Show("Ошибка sql check_system_taxation " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                });
            }
            catch (Exception ex)
            {
                // ✅ Пункт 4: Dispatcher и передача this
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBoxHelper.Show("Общая ошибка check_system_taxation " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                });
            }
            finally
            {
                // ✅ Пункт 3: Убран дубликат проверки
                if (conn.State == ConnectionState.Open)
                {
                    await conn.CloseAsync();
                }
            }
            return result;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}