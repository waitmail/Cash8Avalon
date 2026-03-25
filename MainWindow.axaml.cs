//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Controls.ApplicationLifetimes;
//using Avalonia.Input;
//using Avalonia.Layout;
//using Avalonia.Markup.Xaml;
//using Avalonia.Media;
//using Avalonia.Threading;
//using Avalonia.VisualTree;
//using Cash8Avalon.ViewModels;
//using Newtonsoft.Json;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;


//namespace Cash8Avalon
//{
//    public partial class MainWindow : Window
//    {
//        private DispatcherTimer _unloadingTimer;
//        private MainViewModel _viewModel;
//        private bool _isReallyClosing = false;

//        // ✅ Пункт 1: Флаг для защиты от доступа к уничтоженному UI
//        private bool _isDisposed = false;

//        private CancellationTokenSource _lifetimeCts;

//        public MainWindow()
//        {
//            InitializeComponent();
//            _lifetimeCts = new CancellationTokenSource();
//            InitializeUnloadingTimer();

//            _viewModel = new MainViewModel();
//            DataContext = _viewModel;

//            this.Closing += MainWindow_Closing;
//            // ✅ Пункт 2: Подписываемся на событие закрытия для финальной очистки
//            this.Closed += MainWindow_Closed;
//        }

//        // ✅ Пункт 2: Реализация метода финальной очистки
//        private void MainWindow_Closed(object? sender, EventArgs e)
//        {
//            Console.WriteLine("[MainWindow] Финальная очистка (Closed)");

//            // Отписываемся, чтобы избежать повторных вызовов
//            this.Closed -= MainWindow_Closed;

//            // Обнуляем ссылки для GC
//            _viewModel = null;
//            _lifetimeCts = null;
//            _unloadingTimer = null;

//            Console.WriteLine("[MainWindow] Все ресурсы освобождены");
//        }

//        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
//        {
//            if (_isReallyClosing) return;

//            // ❌ НЕ устанавливаем _isDisposed здесь!
//            // ❌ НЕ отменяем _lifetimeCts здесь!
//            // ❌ НЕ обнуляем MainWindow здесь!

//            e.Cancel = true;
//            _unloadingTimer?.Stop();

//            // 1. СНАЧАЛА создаем окно ожидания
//            var waitWindow = new Window
//            {
//                Title = "Завершение работы",
//                SizeToContent = SizeToContent.WidthAndHeight,
//                WindowStartupLocation = WindowStartupLocation.CenterScreen,
//                CanResize = false,
//                SystemDecorations = SystemDecorations.BorderOnly,
//                Topmost = true
//            };

//            var stackPanel = new StackPanel { Margin = new Thickness(30), Spacing = 15, HorizontalAlignment = HorizontalAlignment.Center };
//            var titleText = new TextBlock { Text = "Завершение работы...", FontSize = 18, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center };
//            var messageText = new TextBlock { Text = "Идёт отправка данных на сервер.", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 };
//            var timerText = new TextBlock { Text = "⏱ 0 сек", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
//            var progressBar = new ProgressBar { Width = 300, Height = 8, IsIndeterminate = true, Foreground = new SolidColorBrush(Color.Parse("#2196F3")), Background = new SolidColorBrush(Color.Parse("#E3F2FD")), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Center };
//            var dotsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 5 };
//            for (int i = 0; i < 3; i++)
//            {
//                dotsPanel.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse("#2196F3")), Opacity = 0.3 });
//            }
//            stackPanel.Children.Add(titleText);
//            stackPanel.Children.Add(messageText);
//            stackPanel.Children.Add(timerText);
//            stackPanel.Children.Add(progressBar);
//            stackPanel.Children.Add(dotsPanel);
//            waitWindow.Content = stackPanel;
//            waitWindow.Show();

//            var stopwatch = new Stopwatch();
//            stopwatch.Start();

//            var uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
//            int dotAnimationStep = 0;
//            uiTimer.Tick += (s, ev) =>
//            {
//                if (stopwatch.IsRunning)
//                {
//                    var elapsed = stopwatch.Elapsed;
//                    timerText.Text = $"⏱ {elapsed.Seconds}.{elapsed.Milliseconds / 100} сек";
//                    dotAnimationStep++;
//                    for (int i = 0; i < dotsPanel.Children.Count; i++)
//                    {
//                        if (dotsPanel.Children[i] is Border dot)
//                        {
//                            double opacity = 0.3 + 0.7 * Math.Sin(dotAnimationStep * 0.1 + i * 2);
//                            dot.Opacity = Math.Max(0.2, Math.Min(1.0, opacity));
//                        }
//                    }
//                }
//            };
//            uiTimer.Start();

//            await Dispatcher.UIThread.InvokeAsync(() =>
//            {
//                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
//                {
//                    var windowsToClose = desktopLifetime.Windows.Where(w => w != this && w != waitWindow && w.IsVisible).ToList();
//                    foreach (var win in windowsToClose) { try { win.Close(); } catch { } }
//                }
//            }, DispatcherPriority.Background);

//            await Task.Delay(100);

//            // 2. ВЫГРУЗКА ДАННЫХ (пока все ресурсы ещё активны!)
//            try
//            {
//                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Начало выгрузки...");
//                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
//                await PerformUnloadAsync(cts.Token);
//                Console.WriteLine($"✓ Выгрузка завершена за {stopwatch.Elapsed.TotalSeconds:F1} сек");
//            }
//            catch (OperationCanceledException)
//            {
//                MainStaticClass.WriteRecordErrorLog("Таймаут выгрузки при закрытии", "MainWindow_Closing", 0, MainStaticClass.CashDeskNumber, "CancellationToken");
//                Console.WriteLine($"⚠ Таймаут выгрузки через {stopwatch.Elapsed.TotalSeconds:F1} сек");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"✗ Ошибка выгрузки: {ex.Message}");
//                Console.WriteLine($"✗ StackTrace: {ex.StackTrace}");
//                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка выгрузки при закрытии приложения");
//            }

//            // 3. ТОЛЬКО ПОСЛЕ ВЫГРУЗКИ - очистка ресурсов
//            finally
//            {
//                uiTimer.Stop();
//                stopwatch.Stop();
//                if (waitWindow.IsVisible) waitWindow.Close();

//                if (_unloadingTimer != null)
//                {
//                    _unloadingTimer.Tick -= UnloadingTimer_Tick;
//                }

//                // ✅ ТЕПЕРЬ отменяем токен
//                if (_lifetimeCts != null && !_lifetimeCts.IsCancellationRequested)
//                {
//                    _lifetimeCts.Cancel();
//                }

//                // ✅ ТЕПЕРЬ обнуляем ссылку
//                if (MainStaticClass.MainWindow == this)
//                {
//                    MainStaticClass.MainWindow = null;
//                }

//                // ✅ ТЕПЕРЬ устанавливаем флаг
//                _isDisposed = true;

//                _lifetimeCts?.Dispose();
//            }

//            _isReallyClosing = true;
//            this.Closing -= MainWindow_Closing;
//            this.Close();
//        }

//        private void InitializeUnloadingTimer()
//        {
//            _unloadingTimer = new DispatcherTimer
//            {
//                Interval = TimeSpan.FromMinutes(5)
//            };
//            _unloadingTimer.Tick += UnloadingTimer_Tick;
//        }

//        private void CreateDefaultSettingsFile(string filePath)
//        {
//            string defaultSettings = @"[ip адрес сервера]
//                127.0.0.1
//                [имя базы данных]
//                Cash_Place
//                [сервисный пароль]
//                1
//                [порт сервера]
//                5432
//                [пароль postgres]
//                a123456789
//                [пользователь postgres]
//                postgres";

//            MainStaticClass.EncryptData(filePath, defaultSettings);
//        }

//        /// <summary>
//        /// Показывает окно обновления и возвращает результат
//        /// </summary>
//        /// <returns>True — обновление успешно, False — отменено/ошибка</returns>
//        private async Task<bool> ShowUpdateWindowModalAsync(bool show_phone)
//        {
//            try
//            {
//                var updateWindow = new LoadProgramFromInternet
//                {
//                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
//                    show_phone = show_phone
//                };

//                // ✅ Всегда используем ShowDialog — окно уже показано
//                bool result = await updateWindow.ShowDialog(this);
//                Console.WriteLine($"[Update] Результат: {result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"✗ Ошибка при показе окна обновления: {ex.Message}");
//                return false;
//            }
//        }

//        protected override async void OnOpened(EventArgs e)
//        {
//            MainStaticClass.MainWindow = this;
//            // ✅ 1. Даем окну время на первичную отрисовку
//            await Task.Delay(50);

//            // ✅ 2. Настройка размеров (особенно для Linux)
//            if (OperatingSystem.IsLinux())
//            {
//                this.WindowState = WindowState.Maximized;
//                var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
//                if (screen != null)
//                {
//                    this.Width = screen.WorkingArea.Width;
//                    this.Height = screen.WorkingArea.Height;
//                    this.Position = new PixelPoint(0, 0);
//                }
//                this.Topmost = true;

//                // ✅ Пауза, чтобы Linux WM успел обработать Topmost
//                await Task.Delay(50);
//                this.Topmost = false;
//            }
//            else
//            {
//                this.WindowState = WindowState.Maximized;
//            }

//            // ✅ 3. Проверка конфигурации
//            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Setting.gaa");
//            if (!File.Exists(configPath))
//            {
//                CreateDefaultSettingsFile(configPath);
//                if (_isDisposed) return;

//                await MessageBoxHelper.Show($"Не обнаружен файл Setting.gaa в {AppDomain.CurrentDomain.BaseDirectory}\r\nБудет создан новый с настройками по умолчанию.",
//                    "Проверка файлов настроек", MessageBoxButton.OK, MessageBoxType.Error, this);

//                // ✅ Пауза после закрытия MessageBox
//                await Task.Delay(100);
//            }

//            Console.WriteLine($"Загружаем конфигурацию из: {configPath}");
//            MainStaticClass.loadConfig(configPath);
//            base.OnOpened(e);
//            UpdateMenuVisibility(0);
//            _ = Task.Run(() => GetUsers(_lifetimeCts.Token));

//            await Task.Delay(50);

//            // ✅ 4. Проверка обновлений
//            bool hasUpdate = await Task.Run(() => MainStaticClass.CheckNewVersionProgramm());
//            if (_isDisposed) return;

//            if (hasUpdate)
//            {
//                bool updateSuccess = await ShowUpdateWindowModalAsync(false);
//                if (updateSuccess)
//                {
//                    Console.WriteLine("✓ Обновление успешно, программа будет перезапущена");
//                    return;
//                }
//                // ✅ Пауза после окна обновления
//                await Task.Delay(100);
//            }

//            // ✅ 5. Окно авторизации
//            var loginWindow = new Interface_switching();
//            bool loginSuccess = false;
//            loginWindow.AuthorizationSuccess += (s, password) => { loginSuccess = true; loginWindow.Close(); };
//            loginWindow.AuthorizationCancel += (s, args) => { loginSuccess = false; loginWindow.Close(); };
//            await loginWindow.ShowDialog(this);

//            if (_isDisposed) return;

//            if (loginSuccess)
//            {
//                try
//                {
//                    UpdateMenuVisibility(MainStaticClass.Code_right_of_user);
//                    Console.WriteLine("=== ВЫПОЛНЕНИЕ ПРОВЕРОК ПРИ СТАРТЕ ===");
//                    MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
//                    MainStaticClass.Last_Write_Check = DateTime.Now.AddSeconds(1);


//                    string version_program = await MainStaticClass.GetAtolDriverVersion();
//                    if (_isDisposed) return;

//                    this.Title = "Касса   " + MainStaticClass.CashDeskNumber;
//                    this.Title += " | " + MainStaticClass.Nick_Shop;
//                    this.Title += " | " + MainStaticClass.version();
//                    this.Title += " | " + LoadDataWebService.last_date_download_tovars().ToString("yyyy-MM-dd hh:mm:ss");
//                    this.Title += " | " + version_program;

//                    MainStaticClass.SystemTaxation = await check_system_taxation();

//                    if (await MainStaticClass.exist_table_name("constants"))
//                    {
//                        _ = InventoryManager.FillDictionaryProductDataAsync(this);
//                        _ = Task.Run(() => InventoryManager.DictionaryPriceGiftAction);
//                        await UpdateUnloadingPeriod();

//                        int intervalMinutes = await MainStaticClass.GetUnloadingInterval();
//                        if (intervalMinutes > 0)
//                        {
//                            _unloadingTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
//                            _unloadingTimer.Start();
//                            Console.WriteLine($"✓ Таймер выгрузки запущен с интервалом {intervalMinutes} минут");
//                        }

//                        if (MainStaticClass.CashDeskNumber != 9)
//                        {
//                            PrintingUsingLibraries printing = new PrintingUsingLibraries();
//                            if (MainStaticClass.Use_Fiscall_Print)
//                            {
//                                printing = new PrintingUsingLibraries();
//                                await printing.getShiftStatus(this);
//                            }
//                            MainStaticClass.validate_date_time_with_fn(10,this);

//                            if (MainStaticClass.SystemTaxation == 0)
//                            {
//                                if (_isDisposed) return;
//                                // ✅ Пауза перед важным предупреждением
//                                await Task.Delay(150);
//                                await MessageBoxHelper.Show("У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!", "Проверка системы налогообложения", MessageBoxButton.OK, MessageBoxType.Error, this);
//                            }

//                            bool restart = false, error = false;
//                            MainStaticClass.check_version_fn(ref restart, ref error);
//                            if (!error && restart)
//                            {
//                                if (_isDisposed) return;
//                                await Task.Delay(150);
//                                await MessageBoxHelper.Show("У вас неверно была установлена версия ФН, необходим перезапуск программы", "Проверка версии ФН", MessageBoxButton.OK, MessageBoxType.Error, this);
//                                this.Close();
//                                return;
//                            }
//                        }

//                        if (MainStaticClass.CashDeskNumber != 9)
//                        {
//                            _ = loadBonusClients();
//                            if (string.IsNullOrEmpty(MainStaticClass.CDN_Token))
//                            {
//                                if (_isDisposed) return;
//                                await Task.Delay(150);
//                                await MessageBoxHelper.Show("В этой кассе не заполнен CDN токен!\r\nПРОДАЖА МАРКИРОВАННОГО ТОВАРА ОГРАНИЧЕНА!", "Проверка cdn токена", MessageBoxButton.OK, MessageBoxType.Error, this);
//                            }
//                            else
//                            {
//                                _ = LoadCdnWithStartAsync(_lifetimeCts.Token);
//                            }

//                            if (await MainStaticClass.PrintingUsingLibraries(this) == 1)
//                            {
//                                PrintingUsingLibraries printingUsingLibraries = new PrintingUsingLibraries();
//                                await printingUsingLibraries.CheckTaxationTypes(this);
//                            }
//                        }

//                        _ = CheckFilesAndFolders();
//                        Console.WriteLine("✓ ВСЕ ПРОВЕРКИ УСПЕШНО ВЫПОЛНЕНЫ");
//                    }
//                    else
//                    {
//                        if (_isDisposed) return;
//                        await Task.Delay(150);
//                        await MessageBoxHelper.Show("В этой бд нет таблицы constatnts, необходимо создать таблицы бд", "Проверка наличия таблицы", MessageBoxButton.OK, MessageBoxType.Error, this);
//                    }

//                    _viewModel.OpenCashChecks();
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"✗ Критическая ошибка: {ex.Message}");
//                    if (!_isDisposed)
//                    {
//                        await Task.Delay(150);
//                        await MessageBoxHelper.Show($"✗ Критическая ошибка: {ex.Message}", "Старт программы", MessageBoxButton.OK, MessageBoxType.Error, this);
//                        this.Close();
//                    }
//                }
//            }
//            else
//            {
//                this.Close();
//            }

//            if (await MainStaticClass.GetUnloadingInterval() != 0)
//            {
//                _ = InitializeTimeSyncAsync(_lifetimeCts.Token).ContinueWith(t =>
//                {
//                    if (t.IsFaulted) Console.WriteLine($"[TimeSync] Критическая ошибка: {t.Exception?.Message}");
//                });
//            }
//        }

//        private async Task InitializeTimeSyncAsync(CancellationToken token, int maxAttempts = 100, int timeoutSeconds = 15, int maxDelaySeconds = 600)
//        {
//            Console.WriteLine($"[TimeSync] Запуск инициализации (попыток: {maxAttempts}, таймаут: {timeoutSeconds}с)");

//            for (int attempt = 1; attempt <= maxAttempts; attempt++)
//            {
//                if (token.IsCancellationRequested)
//                {
//                    Console.WriteLine("[TimeSync] Отменено при закрытии окна.");
//                    return;
//                }

//                try
//                {
//                    Console.WriteLine($"[TimeSync] Попытка {attempt} из {maxAttempts}...");
//                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
//                    linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
//                    DateTime serverTime = await GetServerTimeOnStartupAsync(linkedCts.Token);
//                    Console.WriteLine($"[TimeSync] ✅ УСПЕХ! Попытка {attempt}: {serverTime:HH:mm:ss}");
//                    TimeSync.SetInitialTime(serverTime);
//                    return;
//                }
//                catch (OperationCanceledException)
//                {
//                    if (token.IsCancellationRequested)
//                    {
//                        Console.WriteLine("[TimeSync] Отменено пользователем.");
//                        return;
//                    }
//                    Console.WriteLine($"[TimeSync] Попытка {attempt}: таймаут ({timeoutSeconds}с)");
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"[TimeSync] Попытка {attempt}: ошибка - {ex.Message}");
//                }

//                if (attempt < maxAttempts)
//                {
//                    int delay = 1000 * Math.Min(attempt, maxDelaySeconds);
//                    try { await Task.Delay(delay, token); }
//                    catch (OperationCanceledException) { return; }
//                }
//            }
//            Console.WriteLine($"[TimeSync] ⚠ Не удалось инициализировать после {maxAttempts} попыток");
//        }

//        private static async Task<DateTime> GetServerTimeOnStartupAsync(CancellationToken token)
//        {
//            return await Task.Run(() =>
//            {
//                DS ds = MainStaticClass.get_ds();
//                ds.Timeout = 60000;
//                token.ThrowIfCancellationRequested();
//                var result = ds.GetDateTimeServer();
//                token.ThrowIfCancellationRequested();
//                return result;
//            }, token);
//        }

//        public class Users { public List<User> list_users { get; set; } }
//        public class User
//        {
//            public string shop { get; set; }
//            public string user_id { get; set; }
//            public string name { get; set; }
//            public string rights { get; set; }
//            public string password_m { get; set; }
//            public string password_b { get; set; }
//            public string fiscals_forbidden { get; set; }
//        }

//        private async Task GetUsers(CancellationToken token)
//        {
//            try
//            {
//                token.ThrowIfCancellationRequested();
//                DS ds = MainStaticClass.get_ds();
//                ds.Timeout = 10000;
//                string nick_shop = MainStaticClass.Nick_Shop.Trim();

//                if (nick_shop.Length == 0)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show(" Не удалось получить название магазина ", "Проверка названия магазина", this));
//                    return;
//                }

//                string code_shop = MainStaticClass.Code_Shop.Trim();
//                if (code_shop.Length == 0)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show(" Не удалось получить код магазина ", "Проверка кода магазина", this));
//                    return;
//                }

//                string count_day = CryptorEngine.get_count_day();
//                string key = nick_shop + count_day + code_shop;
//                string encrypt_string = CryptorEngine.Encrypt(nick_shop + "|" + code_shop, true, key);

//                string answer = "";
//                try
//                {
//                    token.ThrowIfCancellationRequested();
//                    answer = ds.GetUsers(MainStaticClass.Nick_Shop, encrypt_string, "4");
//                }
//                catch (Exception ex)
//                {
//                    if (token.IsCancellationRequested) return;
//                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли ошибки при получении пользователей от веб сервиса " + ex.Message + ".", "Синхронизация пользователей", this));
//                    return;
//                }

//                if (string.IsNullOrEmpty(answer)) return;
//                token.ThrowIfCancellationRequested();

//                string decrypt_string = CryptorEngine.Decrypt(answer, true, key);
//                Users users = JsonConvert.DeserializeObject<Users>(decrypt_string);

//                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
//                {
//                    NpgsqlTransaction? trans = null;
//                    try
//                    {
//                        conn.Open();
//                        trans = conn.BeginTransaction();
//                        string query = "UPDATE users SET rights=13";
//                        using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
//                        {
//                            command.Transaction = trans;
//                            command.ExecuteNonQuery();
//                        }

//                        foreach (User user in users.list_users)
//                        {
//                            if (token.IsCancellationRequested) { trans.Rollback(); return; }

//                            string safeName = user.name.Replace("'", "''");
//                            query = "DELETE FROM public.users WHERE inn='" + user.user_id + "';";
//                            query += "INSERT INTO users(code, name, rights, shop, password_m, password_b, inn, fiscals_forbidden)VALUES ('" +
//                                user.user_id + "','" + safeName + "'," + user.rights + ",'" + user.shop + "','" +
//                                user.password_m + "','" + user.password_b + "','" + user.user_id + "','" + user.fiscals_forbidden + "')";

//                            using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
//                            {
//                                command.Transaction = trans;
//                                command.ExecuteNonQuery();
//                            }
//                        }
//                        trans.Commit();
//                    }
//                    catch (NpgsqlException ex)
//                    {
//                        if (trans != null) trans.Rollback();
//                        if (!token.IsCancellationRequested)
//                            await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли ошибки sql при обновлении пользователей " + ex.Message, "Ошибки при обновлении пользователей", this));
//                    }
//                    catch (Exception ex)
//                    {
//                        if (trans != null) trans.Rollback();
//                        if (!token.IsCancellationRequested)
//                            await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли общие ошибки при обновлении пользователей " + ex.Message, "Ошибки при обновлении пользователей", this));
//                    }
//                }
//            }
//            catch (OperationCanceledException) { Console.WriteLine("GetUsers: операция отменена."); }
//            catch (Exception ex) { Console.WriteLine($"Критическая ошибка в GetUsers: {ex.Message}"); }
//        }

//        protected override void OnKeyDown(KeyEventArgs e)
//        {
//            base.OnKeyDown(e);
//            if (e.Key == Key.F12)
//            {
//                e.Handled = true;
//                _ = ShowAuthorizationWindow();
//            }
//        }

//        private async Task ShowAuthorizationWindow()
//        {
//            try
//            {
//                var loginWindow = new Interface_switching();
//                bool loginSuccess = false;

//                loginWindow.AuthorizationSuccess += (s, password) => { loginSuccess = true; loginWindow.Close(); };
//                loginWindow.AuthorizationCancel += (s, args) => { loginSuccess = false; loginWindow.Close(); };

//                await loginWindow.ShowDialog(this);

//                if (loginSuccess)
//                {
//                    UpdateMenuVisibility(MainStaticClass.Code_right_of_user);
//                    _viewModel.OpenCashChecks();
//                }
//                else { this.Close(); }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка при показе окна авторизации: {ex.Message}");
//                await MessageBoxHelper.Show($"Ошибка: {ex.Message}", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxType.Error);
//            }
//        }

//        private void UpdateMenuVisibility(int userRights)
//        {
//            //var menu = MainMenu ?? this.FindControl<Menu>("MainMenu");
//            //if (menu != null) menu.IsVisible = userRights != 2;
//            var menu = this.FindControl<Menu>("MainMenu");
//            if (menu != null)
//            {
//                // Логика:
//                // 1. Если userRights == 0 (при старте) -> Скрываем (false)
//                // 2. Если userRights == 2 (ограниченные права) -> Скрываем (false)
//                // 3. Во всех остальных случаях -> Показываем (true)

//                menu.IsVisible = userRights > 0 && userRights != 2;
//            }
//        }

//        private async Task UpdateUnloadingPeriod()
//        {
//            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
//            try
//            {
//                await conn.OpenAsync();
//                string query = "UPDATE constants SET unloading_period = 4 WHERE unloading_period > 0";
//                NpgsqlCommand command = new NpgsqlCommand(query, conn);
//                await command.ExecuteNonQueryAsync();
//                Console.WriteLine("✓ Период выгрузки обновлен в БД");
//            }
//            catch (Exception ex)
//            {
//                // ✅ Проверка перед UI
//                if (!_isDisposed)
//                {
//                    await MessageBoxHelper.Show($"Ошибка при проверке/установке значения периода выгрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error,this);
//                }
//                Console.WriteLine($"✗ Общая ошибка в UpdateUnloadingPeriod: {ex.Message}");
//            }
//            finally
//            {
//                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
//            }
//        }

//        private async void UnloadingTimer_Tick(object? sender, EventArgs e)
//        {
//            _ = PerformUnloadAsync(_lifetimeCts.Token).ContinueWith(t =>
//            {
//                if (t.Exception != null)
//                {
//                    MainStaticClass.WriteRecordErrorLog(t.Exception, 0, MainStaticClass.CashDeskNumber, "Ошибка периодической выгрузки");
//                    Console.WriteLine($"✗ Ошибка в таймере: {t.Exception.Message}");
//                }
//            }, TaskScheduler.Default);
//        }

//        private async Task PerformUnloadAsync(CancellationToken ct)
//        {
//            await Task.Run(async () =>
//            {
//                try
//                {
//                    Console.WriteLine($"=== Запуск выгрузки данных ({DateTime.Now:HH:mm:ss}) ===");
//                    MainStaticClass.SendOnlineStatus();
//                    ct.ThrowIfCancellationRequested();

//                    if (MainStaticClass.Last_Write_Check > MainStaticClass.Last_Send_Last_Successful_Sending)
//                    {
//                        try { ct.ThrowIfCancellationRequested(); var sdsp = new SendDataOnSalesPortions(); sdsp.send_sales_data_Click(null, null); Console.WriteLine("✓ Данные о продажах отправлены"); }
//                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки продаж"); Console.WriteLine($"✗ Продажи: {ex.Message}"); }

//                        try { ct.ThrowIfCancellationRequested(); UploadDeletedItems(); Console.WriteLine("✓ Удаленные элементы отправлены"); }
//                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки удаленных элементов"); Console.WriteLine($"✗ Удаленные: {ex.Message}"); }

//                        try { ct.ThrowIfCancellationRequested(); send_cdn_logs(); Console.WriteLine("✓ CDN логи отправлены"); }
//                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки CDN логов"); Console.WriteLine($"✗ CDN: {ex.Message}"); }

//                        try { ct.ThrowIfCancellationRequested(); UploadErrorsLog(); Console.WriteLine("✓ Логи ошибок отправлены"); }
//                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки логов ошибок"); Console.WriteLine($"✗ Логи: {ex.Message}"); }

//                        try { ct.ThrowIfCancellationRequested(); sent_open_close_shop(); Console.WriteLine("✓ Данные о сменах отправлены"); }
//                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки данных о сменах"); Console.WriteLine($"✗ Смены: {ex.Message}"); }

//                        MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
//                        Console.WriteLine("✓ Выгрузка завершена");
//                    }
//                    else { Console.WriteLine("⚠ Нет новых данных для выгрузки"); }
//                }
//                catch (OperationCanceledException)
//                {
//                    Console.WriteLine("Выгрузка прервана по таймауту");
//                    MainStaticClass.WriteRecordErrorLog("Выгрузка прервана по таймауту", "PerformUnloadAsync", 0, MainStaticClass.CashDeskNumber, "CancellationToken");
//                    throw;
//                }
//                catch (Exception ex)
//                {
//                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Непредвиденная ошибка в PerformUnloadAsync");
//                    Console.WriteLine($"✗ Критическая ошибка: {ex}");
//                    throw;
//                }
//            }, ct);
//        }

//        // Вспомогательные классы OpenCloseShop, CdnLogs, DeletedItem, RecordsErrorLog оставляем без изменений
//        // ...
//        class OpenCloseShop { public DateTime? Open { get; set; } public DateTime? Close { get; set; } public DateTime Date { get; set; } public bool ItsSent { get; set; } }

//        private async void sent_open_close_shop()
//        {
//            List<OpenCloseShop> closeShops = await get_open_close_shop();
//            if (closeShops.Count > 0)
//            {
//                DS ds = MainStaticClass.get_ds();
//                string nick_shop = MainStaticClass.Nick_Shop.Trim();
//                if (nick_shop.Trim().Length == 0) return;
//                string code_shop = MainStaticClass.Code_Shop.Trim();
//                if (code_shop.Trim().Length == 0) return;
//                string count_day = CryptorEngine.get_count_day();
//                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
//                string data = JsonConvert.SerializeObject(closeShops, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
//                string data_crypt = CryptorEngine.Encrypt(data, true, key);
//                try
//                {
//                    bool result = ds.UploadOpeningClosingShops(MainStaticClass.Nick_Shop, data_crypt, "4");
//                    if (result) MarkShopsAsSent(closeShops);
//                }
//                catch { }
//            }
//        }

//        private void MarkShopsAsSent(List<OpenCloseShop> shops)
//        {
//            if (shops == null || shops.Count == 0) return;
//            using (var conn = MainStaticClass.NpgsqlConn())
//            {
//                conn.Open();
//                using (var transaction = conn.BeginTransaction())
//                {
//                    try
//                    {
//                        foreach (var shop in shops)
//                        {
//                            string updateQuery = "UPDATE public.open_close_shop SET its_sent = true WHERE date = @date";
//                            using (var cmd = new NpgsqlCommand(updateQuery, conn, transaction))
//                            {
//                                cmd.Parameters.AddWithValue("@date", shop.Date.Date);
//                                cmd.ExecuteNonQuery();
//                            }
//                        }
//                        transaction.Commit();
//                    }
//                    catch (Exception ex)
//                    {
//                        transaction.Rollback();
//                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка при обновлении its_sent");
//                    }
//                }
//            }
//        }

//        private async Task<List<OpenCloseShop>> get_open_close_shop()
//        {
//            List<OpenCloseShop> openCloseShops = new List<OpenCloseShop>();
//            using (var conn = MainStaticClass.NpgsqlConn())
//            {
//                try
//                {
//                    conn.Open();
//                    string query = "SELECT open, close, date, its_sent FROM public.open_close_shop WHERE its_sent = false;";
//                    using (var command = new NpgsqlCommand(query, conn))
//                    using (var reader = command.ExecuteReader())
//                    {
//                        int openOrdinal = reader.GetOrdinal("open");
//                        int closeOrdinal = reader.GetOrdinal("close");
//                        int dateOrdinal = reader.GetOrdinal("date");
//                        int itsSentOrdinal = reader.GetOrdinal("its_sent");
//                        while (reader.Read())
//                        {
//                            var openCloseShop = new OpenCloseShop
//                            {
//                                Open = reader.IsDBNull(openOrdinal) ? (DateTime?)null : reader.GetDateTime(openOrdinal),
//                                Close = reader.IsDBNull(closeOrdinal) ? (DateTime?)null : reader.GetDateTime(closeOrdinal),
//                                Date = reader.GetDateTime(dateOrdinal),
//                                ItsSent = reader.GetBoolean(itsSentOrdinal)
//                            };
//                            openCloseShops.Add(openCloseShop);
//                        }
//                    }
//                }
//                catch (NpgsqlException ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Отправка даты открытия/закрытия магазина"); }
//                catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Отправка даты открытия/закрытия магазина"); }
//            }
//            return openCloseShops;
//        }

//        public class CdnLogs { public List<CdnLog> ListCdnLog { get; set; } }
//        public class CdnLog
//        {
//            public string NumCash { get; set; }
//            public string CdnAnswer { get; set; }
//            public string DateShop { get; set; }
//            public string NumDoc { get; set; }
//            public string Mark { get; set; }
//            public string Status { get; set; }
//        }

//        private void send_cdn_logs()
//        {
//            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
//            try
//            {
//                string query = "SELECT num_cash, date, cdn_answer, numdoc, is_sent, mark,status FROM cdn_log WHERE is_sent=0;";
//                conn.Open();
//                NpgsqlCommand command = new NpgsqlCommand(query, conn);
//                NpgsqlDataReader reader = command.ExecuteReader();
//                CdnLogs logs = new CdnLogs();
//                logs.ListCdnLog = new List<CdnLog>();
//                while (reader.Read())
//                {
//                    CdnLog log = new CdnLog();
//                    log.CdnAnswer = reader["cdn_answer"].ToString();
//                    log.Mark = reader["mark"].ToString();
//                    log.NumCash = MainStaticClass.CashDeskNumber.ToString();
//                    log.NumDoc = reader["numdoc"].ToString();
//                    log.DateShop = Convert.ToDateTime(reader["date"]).ToString("dd-MM-yyyy HH:mm:ss");
//                    log.Status = reader["status"].ToString();
//                    logs.ListCdnLog.Add(log);
//                }
//                if (logs.ListCdnLog.Count > 0)
//                {
//                    DS ds = MainStaticClass.get_ds();
//                    ds.Timeout = 180000;
//                    string nick_shop = MainStaticClass.Nick_Shop.Trim();
//                    if (nick_shop.Trim().Length == 0) return;
//                    string code_shop = MainStaticClass.Code_Shop.Trim();
//                    if (code_shop.Trim().Length == 0) return;
//                    string count_day = CryptorEngine.get_count_day();
//                    string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
//                    bool result_web_quey = false;
//                    string data = JsonConvert.SerializeObject(logs, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
//                    string data_crypt = CryptorEngine.Encrypt(data, true, key);
//                    result_web_quey = ds.UploadCDNLogsPortionJason(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString());
//                    if (result_web_quey)
//                    {
//                        foreach (CdnLog log in logs.ListCdnLog)
//                        {
//                            query = "UPDATE cdn_log SET is_sent = 1 WHERE date='" + log.DateShop + "';";
//                            command = new NpgsqlCommand(query, conn);
//                            command.ExecuteNonQuery();
//                        }
//                    }
//                }
//            }
//            catch (NpgsqlException) { }
//            catch (Exception) { }
//            finally { if (conn.State == ConnectionState.Open) conn.Close(); }
//        }

//        public class DeletedItem
//        {
//            public string num_doc { get; set; }
//            public string num_cash { get; set; }
//            public string date_time_start { get; set; }
//            public string date_time_action { get; set; }
//            public string tovar { get; set; }
//            public string quantity { get; set; }
//            public string type_of_operation { get; set; }
//            public string guid { get; set; }
//            public string autor { get; set; }
//            public string reason { get; set; }
//        }

//        public class DeletedItems : IDisposable
//        {
//            public string Version { get; set; }
//            public string NickShop { get; set; }
//            public string CodeShop { get; set; }
//            public List<DeletedItem> ListDeletedItem { get; set; }
//            void IDisposable.Dispose() { }
//        }

//        private void UploadDeletedItems()
//        {
//            DeletedItems deletedItems = new DeletedItems();
//            deletedItems.CodeShop = MainStaticClass.Code_Shop;
//            deletedItems.NickShop = MainStaticClass.Nick_Shop;
//            deletedItems.ListDeletedItem = new List<DeletedItem>();
//            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

//            try
//            {
//                conn.Open();
//                string query = "SELECT num_doc, num_cash, date_time_start, date_time_action, tovar, quantity, type_of_operation,guid,reason FROM deleted_items;";
//                NpgsqlCommand command = new NpgsqlCommand(query, conn);
//                NpgsqlDataReader reader = command.ExecuteReader();
//                while (reader.Read())
//                {
//                    DeletedItem deletedItem = new DeletedItem();
//                    deletedItem.num_doc = reader["num_doc"].ToString();
//                    deletedItem.num_cash = reader["num_cash"].ToString();
//                    deletedItem.date_time_start = reader["date_time_start"].ToString();
//                    deletedItem.date_time_action = reader["date_time_action"].ToString();
//                    deletedItem.tovar = reader["tovar"].ToString();
//                    deletedItem.quantity = reader["quantity"].ToString();
//                    deletedItem.type_of_operation = reader["type_of_operation"].ToString();
//                    deletedItem.guid = reader["guid"].ToString();
//                    deletedItem.autor = MainStaticClass.CashOperatorInn;
//                    deletedItem.reason = reader["reason"].ToString();
//                    deletedItems.ListDeletedItem.Add(deletedItem);
//                }
//                reader.Close();
//                reader.Dispose();

//                if (deletedItems.ListDeletedItem.Count == 0) return;

//                if (!MainStaticClass.service_is_worker()) return;

//                DS ds = MainStaticClass.get_ds();
//                ds.Timeout = 20000;

//                string nick_shop = MainStaticClass.Nick_Shop.Trim();
//                if (nick_shop.Trim().Length == 0) { Console.WriteLine("Не удалось получить название магазина (UploadDeletedItems)"); return; }

//                string code_shop = MainStaticClass.Code_Shop.Trim();
//                if (code_shop.Trim().Length == 0) { Console.WriteLine("Не удалось получить код магазина (UploadDeletedItems)"); return; }

//                string count_day = CryptorEngine.get_count_day();
//                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
//                string data = JsonConvert.SerializeObject(deletedItems, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
//                string encrypt_string = CryptorEngine.Encrypt(data, true, key);
//                string answer = ds.UploadDeletedItems(nick_shop, encrypt_string, MainStaticClass.GetWorkSchema.ToString());

//                if (answer == "1")
//                {
//                    query = "DELETE FROM deleted_items";
//                    command = new NpgsqlCommand(query, conn);
//                    command.ExecuteNonQuery();
//                }
//                else { MainStaticClass.WriteRecordErrorLog("Произошли ошибки при передаче удаленных строк", "UploadDeletedItems", 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию об удаленных строках"); }
//                command.Dispose();
//                conn.Close();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Произошли ошибки при передаче удаленных строк " + ex.Message);
//                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Не удалось передать информацию об удаленных строках");
//            }
//            finally { if (conn.State == ConnectionState.Open) conn.Close(); }
//        }

//        private void UploadErrorsLog()
//        {
//            try
//            {
//                var recordsErrorLog = ReadErrorLogsFromDatabase();
//                if (recordsErrorLog.ErrorLogs.Count > 0)
//                {
//                    bool uploadResult = UploadErrorLogsToServer(recordsErrorLog);
//                    if (uploadResult) DeleteErrorLogsFromDatabase(recordsErrorLog);
//                }
//            }
//            catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Произошла ошибка при загрузке логов ошибок"); }
//        }

//        public class RecordsErrorLog { public string Shop { get; set; } public short CashDeskNumber { get; set; } public List<RecordErrorLog> ErrorLogs { get; set; } = new List<RecordErrorLog>(); }
//        public class RecordErrorLog { public string ErrorMessage { get; set; } public string MethodName { get; set; } public long NumDoc { get; set; } public string Description { get; set; } public DateTime DateTimeRecord { get; set; } }

//        private RecordsErrorLog ReadErrorLogsFromDatabase()
//        {
//            RecordsErrorLog recordsErrorLog = new RecordsErrorLog();
//            recordsErrorLog.Shop = MainStaticClass.Nick_Shop;
//            recordsErrorLog.CashDeskNumber = Convert.ToInt16(MainStaticClass.CashDeskNumber);

//            using (var connection = MainStaticClass.NpgsqlConn())
//            {
//                connection.Open();
//                string query = "SELECT error_message, date_time_record, num_doc, method_name, description FROM public.errors_log";
//                using (var command = new NpgsqlCommand(query, connection))
//                using (var reader = command.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        var logError = new RecordErrorLog
//                        {
//                            ErrorMessage = reader["error_message"].ToString().Trim(),
//                            DateTimeRecord = reader.GetDateTime(reader.GetOrdinal("date_time_record")),
//                            NumDoc = reader.GetInt64(reader.GetOrdinal("num_doc")),
//                            MethodName = reader["method_name"].ToString().Trim(),
//                            Description = reader["description"].ToString().Trim()
//                        };
//                        recordsErrorLog.ErrorLogs.Add(logError);
//                    }
//                }
//            }
//            return recordsErrorLog;
//        }

//        private bool UploadErrorLogsToServer(RecordsErrorLog recordsErrorLog)
//        {
//            string nick_shop = MainStaticClass.Nick_Shop.Trim();
//            string code_shop = MainStaticClass.Code_Shop.Trim();
//            if (string.IsNullOrEmpty(nick_shop) || string.IsNullOrEmpty(code_shop)) return false;

//            string count_day = CryptorEngine.get_count_day();
//            string key = nick_shop + count_day + code_shop;
//            string data = JsonConvert.SerializeObject(recordsErrorLog, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
//            string data_crypt = CryptorEngine.Encrypt(data, true, key);

//            DS ds = MainStaticClass.get_ds();
//            ds.Timeout = 18000;
//            try { return ds.UploadErrorLogPortionJson(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString()); }
//            catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию об ошибках в программе"); return false; }
//        }

//        private void DeleteErrorLogsFromDatabase(RecordsErrorLog recordsErrorLog)
//        {
//            using (var connection = MainStaticClass.NpgsqlConn())
//            {
//                connection.Open();
//                foreach (var recordErrorLog in recordsErrorLog.ErrorLogs)
//                {
//                    string query = "DELETE FROM public.errors_log WHERE date_time_record = @DateTimeRecord";
//                    using (var command = new NpgsqlCommand(query, connection))
//                    {
//                        command.Parameters.AddWithValue("@DateTimeRecord", recordErrorLog.DateTimeRecord);
//                        command.ExecuteNonQuery();
//                    }
//                }
//            }
//        }

//        private void get_cdn_with_start() { CDN.CDN_List list = MainStaticClass.CDN_List; }

//        private async Task LoadCdnWithStartAsync(CancellationToken externalToken)
//        {
//            try
//            {
//                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
//                linkedCts.CancelAfter(TimeSpan.FromSeconds(60));
//                await Task.Run(() => get_cdn_with_start(), linkedCts.Token);
//            }
//            catch (OperationCanceledException) { Console.WriteLine("Загрузка CDN отменена (таймаут или закрытие окна)."); }
//            catch (Exception ex) { await MessageBoxHelper.Show($"При загрузке CDN произошла ошибка: {ex.Message}"); }
//        }

//        private async Task CheckFilesAndFolders()
//        {
//            try
//            {
//                string startupPath = AppContext.BaseDirectory;
//                string folderPathPictures = Path.Combine(startupPath, "Pictures2");
//                await Task.Run(() =>
//                {
//                    if (!Directory.Exists(folderPathPictures))
//                    {
//                        Directory.CreateDirectory(folderPathPictures);
//                        Console.WriteLine($"Папка создана: {folderPathPictures}");
//                    }
//                    else { _ = ClearFolder(folderPathPictures); Console.WriteLine($"Папка очищена: {folderPathPictures}"); }
//                });
//            }
//            catch (Exception ex)
//            {
//                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Проверка/создание файлов и папок");
//                await Dispatcher.UIThread.InvokeAsync(async () => { await MessageBoxHelper.Show($"Ошибка при работе с папкой Pictures2: {ex.Message}", "Ошибка",this); });
//            }
//        }

//        private async Task ClearFolder(string folderPath)
//        {
//            try
//            {
//                foreach (string file in Directory.GetFiles(folderPath)) { try { File.Delete(file); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить файл {file}: {ex.Message}"); } }
//                foreach (string subFolder in Directory.GetDirectories(folderPath)) { try { Directory.Delete(subFolder, true); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить папку {subFolder}: {ex.Message}"); } }
//            }
//            catch (Exception ex) { throw new Exception($"Ошибка при очистке папки {folderPath}: {ex.Message}", ex); }
//        }

//        private async Task loadBonusClients()
//        {
//            LoadDataWebService ld = new LoadDataWebService();
//            await Task.Run(() => ld.load_bonus_clients(false));
//        }

//        // ✅ Пункт 3 и 4: Исправленный метод
//        private async Task<int> check_system_taxation()
//        {
//            int result = 0;
//            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
//            try
//            {
//                conn.Open();
//                string query = "SELECT system_taxation FROM constants";
//                NpgsqlCommand command = new NpgsqlCommand(query, conn);
//                result = Convert.ToInt16(command.ExecuteScalar());
//            }
//            catch (NpgsqlException ex)
//            {
//                // ✅ Пункт 4: Dispatcher и передача this
//                await Dispatcher.UIThread.InvokeAsync(async () =>
//                {
//                    await MessageBoxHelper.Show("Ошибка sql check_system_taxation " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
//                });
//            }
//            catch (Exception ex)
//            {
//                // ✅ Пункт 4: Dispatcher и передача this
//                await Dispatcher.UIThread.InvokeAsync(async () =>
//                {
//                    await MessageBoxHelper.Show("Общая ошибка check_system_taxation " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
//                });
//            }
//            finally
//            {
//                // ✅ Пункт 3: Убран дубликат проверки
//                if (conn.State == ConnectionState.Open)
//                {
//                    await conn.CloseAsync();
//                }
//            }
//            return result;
//        }

//        private void InitializeComponent()
//        {
//            AvaloniaXamlLoader.Load(this);
//        }
//    }
//}

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

        // ✅ Пункт 2: Реализация метода финальной очистки
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            Console.WriteLine("[MainWindow] Финальная очистка (Closed)");

            // Отписываемся, чтобы избежать повторных вызовов
            this.Closed -= MainWindow_Closed;

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
            var messageText = new TextBlock { Text = "Идёт отправка данных на сервер.\nПожалуйста, подождите...", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 };

            var timerText = new TextBlock { Text = "⏱ 0 сек", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            var progressBar = new ProgressBar { Width = 300, Height = 8, IsIndeterminate = true, Foreground = new SolidColorBrush(Color.Parse("#2196F3")), Background = new SolidColorBrush(Color.Parse("#E3F2FD")), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Center };
            var dotsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 5 };
            for (int i = 0; i < 3; i++)
            {
                dotsPanel.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse("#2196F3")), Opacity = 0.3 });
            }
            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(messageText);
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
                var unloadTask = PerformUnloadAsync(_lifetimeCts.Token);

                // ✅ Запускаем таймер "нетерпения" (25 секунд)
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(25));

                // ✅ Ждем: кто закончится первым? Задача или Таймер?
                var completedTask = await Task.WhenAny(unloadTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Сработал таймер! Прошло 25 секунд.
                    Console.WriteLine($"⚠ Таймаут! Выгрузка не успела завершиться за 25 сек. Принудительное закрытие.");
                    MainStaticClass.WriteRecordErrorLog("Таймаут выгрузки (25 сек). Приложение закрыто принудительно.", "MainWindow_Closing", 0, MainStaticClass.CashDeskNumber, "Timeout");

                    // Просим задачу остановиться (если она умеет слушать токен)
                    if (_lifetimeCts != null && !_lifetimeCts.IsCancellationRequested)
                    {
                        _lifetimeCts.Cancel();
                    }

                    // ВАЖНО: Мы НЕ пишем await unloadTask здесь!
                    // Мы просто идем дальше на закрытие окна. 
                    // "Брошенная" задача в фоне умрет при закрытии процесса.
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

        protected override async void OnOpened(EventArgs e)
        {
            MainStaticClass.MainWindow = this;
            // ✅ 1. Даем окну время на первичную отрисовку
            await Task.Delay(50);

            // ✅ 2. Настройка размеров (особенно для Linux)
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

                // ✅ Пауза, чтобы Linux WM успел обработать Topmost
                await Task.Delay(50);
                this.Topmost = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }

            // ✅ 3. Проверка конфигурации
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Setting.gaa");
            if (!File.Exists(configPath))
            {
                CreateDefaultSettingsFile(configPath);
                if (_isDisposed) return;

                await MessageBoxHelper.Show($"Не обнаружен файл Setting.gaa в {AppDomain.CurrentDomain.BaseDirectory}\r\nБудет создан новый с настройками по умолчанию.",
                    "Проверка файлов настроек", MessageBoxButton.OK, MessageBoxType.Error, this);

                // ✅ Пауза после закрытия MessageBox
                await Task.Delay(100);
            }

            Console.WriteLine($"Загружаем конфигурацию из: {configPath}");
            MainStaticClass.loadConfig(configPath);
            base.OnOpened(e);
            UpdateMenuVisibility(0);
            _ = Task.Run(() => GetUsers(_lifetimeCts.Token));

            await Task.Delay(50);

            // ✅ 4. Проверка обновлений с диалогом ожидания
            // Создаем окно ожидания
            var checkUpdateWindow = new Window
            {
                Title = "Проверка обновлений",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                SystemDecorations = SystemDecorations.BorderOnly,
                Topmost = true
            };

            var stackPanel = new StackPanel { Margin = new Thickness(30), Spacing = 15, HorizontalAlignment = HorizontalAlignment.Center };
            var titleText = new TextBlock { Text = "Проверка обновлений", FontSize = 18, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center };
            var messageText = new TextBlock { Text = "Идёт проверка наличия обновлений на сервере.", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 };
            var timerText = new TextBlock { Text = "⏱ 0 сек", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#4CAF50")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            var progressBar = new ProgressBar { Width = 300, Height = 8, IsIndeterminate = true, Foreground = new SolidColorBrush(Color.Parse("#4CAF50")), Background = new SolidColorBrush(Color.Parse("#E8F5E9")), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Center };

            var dotsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 5 };
            for (int i = 0; i < 3; i++)
            {
                dotsPanel.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse("#4CAF50")), Opacity = 0.3 });
            }

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(timerText);
            stackPanel.Children.Add(progressBar);
            stackPanel.Children.Add(dotsPanel);

            checkUpdateWindow.Content = stackPanel;
            checkUpdateWindow.Show();

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

            // Запуск проверки обновлений в фоновом потоке
            bool hasUpdate = false;
            try
            {
                hasUpdate = await Task.Run(() => MainStaticClass.CheckNewVersionProgramm());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке обновлений: {ex.Message}");
            }
            finally
            {
                // Всегда закрываем окно ожидания и останавливаем таймеры
                uiTimer.Stop();
                stopwatch.Stop();
                if (checkUpdateWindow.IsVisible) checkUpdateWindow.Close();
            }

            if (_isDisposed) return;

            if (hasUpdate)
            {
                bool updateSuccess = await ShowUpdateWindowModalAsync(false);
                if (updateSuccess)
                {
                    Console.WriteLine("✓ Обновление успешно, программа будет перезапущена");
                    return;
                }
                // ✅ Пауза после окна обновления
                await Task.Delay(100);
            }

            // ✅ 5. Окно авторизации
            var loginWindow = new Interface_switching();
            bool loginSuccess = false;
            loginWindow.AuthorizationSuccess += (s, password) => { loginSuccess = true; loginWindow.Close(); };
            loginWindow.AuthorizationCancel += (s, args) => { loginSuccess = false; loginWindow.Close(); };
            await loginWindow.ShowDialog(this);

            if (_isDisposed) return;

            if (loginSuccess)
            {
                try
                {
                    UpdateMenuVisibility(MainStaticClass.Code_right_of_user);
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
                            MainStaticClass.validate_date_time_with_fn(10, this);

                            if (MainStaticClass.SystemTaxation == 0)
                            {
                                if (_isDisposed) return;
                                // ✅ Пауза перед важным предупреждением
                                await Task.Delay(150);
                                await MessageBoxHelper.Show("У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!", "Проверка системы налогообложения", MessageBoxButton.OK, MessageBoxType.Error, this);
                            }

                            bool restart = false, error = false;
                            MainStaticClass.check_version_fn(ref restart, ref error);
                            if (!error && restart)
                            {
                                if (_isDisposed) return;
                                await Task.Delay(150);
                                await MessageBoxHelper.Show("У вас неверно была установлена версия ФН, необходим перезапуск программы", "Проверка версии ФН", MessageBoxButton.OK, MessageBoxType.Error, this);
                                this.Close();
                                return;
                            }
                        }

                        if (MainStaticClass.CashDeskNumber != 9)
                        {
                            _ = loadBonusClients();
                            if (string.IsNullOrEmpty(MainStaticClass.CDN_Token))
                            {
                                if (_isDisposed) return;
                                await Task.Delay(150);
                                await MessageBoxHelper.Show("В этой кассе не заполнен CDN токен!\r\nПРОДАЖА МАРКИРОВАННОГО ТОВАРА ОГРАНИЧЕНА!", "Проверка cdn токена", MessageBoxButton.OK, MessageBoxType.Error, this);
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
                        await MessageBoxHelper.Show("В этой бд нет таблицы constatnts, необходимо создать таблицы бд", "Проверка наличия таблицы", MessageBoxButton.OK, MessageBoxType.Error, this);
                    }
                    await check_add_field();

                    _viewModel.OpenCashChecks();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Критическая ошибка: {ex.Message}");
                    if (!_isDisposed)
                    {
                        await Task.Delay(150);
                        await MessageBoxHelper.Show($"✗ Критическая ошибка: {ex.Message}", "Старт программы", MessageBoxButton.OK, MessageBoxType.Error, this);
                        this.Close();
                    }
                }
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

        /// <summary>
        /// Исправление старого типа колонки 'action_num_doc'
        /// </summary>
        private async Task<bool> check_correct_type_column()
        {
            bool update = false;

            // 1. Используем using для гарантированного закрытия соединения
            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    await conn.OpenAsync();
                }
                catch (InvalidOperationException) { /* Игнорируем, если уже открыто */ }

                try
                {
                    // 2. Транзакция убрана, так как это только чтение (SELECT)
                    string query = "SELECT data_type FROM information_schema.columns WHERE table_name = 'checks_header' AND column_name = 'action_num_doc'";

                    using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                    {
                        // 3. ExecuteScalar быстрее и проще, если нужно получить одно значение (тип данных)
                        // Если вернется null, значит колонки нет (но по логике проверяем тип)
                        var result = await command.ExecuteScalarAsync();

                        if (result != null && result.ToString() != "ARRAY")
                        {
                            update = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении типа колонки: {ex.Message}");
                    return false;
                }
            }
            // Соединение закрыто здесь

            // 4. Обновление запускаем ТОЛЬКО если чтение завершено и соединение освобождено
            if (update)
            {
                SettingConnect sc = new SettingConnect();
                await sc.AddField_Click(this);
                this.Close();
                return true;
            }

            return false;
        }

        private async Task<bool> check_exists_column()
        {
            using (var conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    await conn.OpenAsync();
                    string query = "SELECT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'constants' AND column_name = 'piot_url');";

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
        ///// Исправление старого типа автор
        ///// в колонке
        ///// </summary>
        private async Task check_add_field()
        {
            // Если метод вернул true (нужно обновление/закрытие), прерываем выполнение
            if (await check_correct_type_column()) return;
            if (await check_exists_table()) return;
            await check_exists_column();
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

        private async Task GetUsers(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 10000;
                string nick_shop = MainStaticClass.Nick_Shop.Trim();

                if (nick_shop.Length == 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show(" Не удалось получить название магазина ", "Проверка названия магазина", this));
                    return;
                }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Length == 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show(" Не удалось получить код магазина ", "Проверка кода магазина", this));
                    return;
                }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop + count_day + code_shop;
                string encrypt_string = CryptorEngine.Encrypt(nick_shop + "|" + code_shop, true, key);

                string answer = "";
                try
                {
                    token.ThrowIfCancellationRequested();
                    answer = ds.GetUsers(MainStaticClass.Nick_Shop, encrypt_string, "4");
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested) return;
                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли ошибки при получении пользователей от веб сервиса " + ex.Message + ".", "Синхронизация пользователей", this));
                    return;
                }

                if (string.IsNullOrEmpty(answer)) return;
                token.ThrowIfCancellationRequested();

                string decrypt_string = CryptorEngine.Decrypt(answer, true, key);
                Users users = JsonConvert.DeserializeObject<Users>(decrypt_string);

                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                {
                    NpgsqlTransaction? trans = null;
                    try
                    {
                        conn.Open();
                        trans = conn.BeginTransaction();
                        string query = "UPDATE users SET rights=13";
                        using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                        {
                            command.Transaction = trans;
                            command.ExecuteNonQuery();
                        }

                        foreach (User user in users.list_users)
                        {
                            if (token.IsCancellationRequested) { trans.Rollback(); return; }

                            string safeName = user.name.Replace("'", "''");
                            query = "DELETE FROM public.users WHERE inn='" + user.user_id + "';";
                            query += "INSERT INTO users(code, name, rights, shop, password_m, password_b, inn, fiscals_forbidden)VALUES ('" +
                                user.user_id + "','" + safeName + "'," + user.rights + ",'" + user.shop + "','" +
                                user.password_m + "','" + user.password_b + "','" + user.user_id + "','" + user.fiscals_forbidden + "')";

                            using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                            {
                                command.Transaction = trans;
                                command.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                    catch (NpgsqlException ex)
                    {
                        if (trans != null) trans.Rollback();
                        if (!token.IsCancellationRequested)
                            await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли ошибки sql при обновлении пользователей " + ex.Message, "Ошибки при обновлении пользователей", this));
                    }
                    catch (Exception ex)
                    {
                        if (trans != null) trans.Rollback();
                        if (!token.IsCancellationRequested)
                            await Dispatcher.UIThread.InvokeAsync(() => MessageBoxHelper.Show("Произошли общие ошибки при обновлении пользователей " + ex.Message, "Ошибки при обновлении пользователей", this));
                    }
                }
            }
            catch (OperationCanceledException) { Console.WriteLine("GetUsers: операция отменена."); }
            catch (Exception ex) { Console.WriteLine($"Критическая ошибка в GetUsers: {ex.Message}"); }
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
                await MessageBoxHelper.Show($"Ошибка: {ex.Message}", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxType.Error);
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
            _ = PerformUnloadAsync(_lifetimeCts.Token).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    MainStaticClass.WriteRecordErrorLog(t.Exception, 0, MainStaticClass.CashDeskNumber, "Ошибка периодической выгрузки");
                    Console.WriteLine($"✗ Ошибка в таймере: {t.Exception.Message}");
                }
            }, TaskScheduler.Default);
        }

        private async Task PerformUnloadAsync(CancellationToken ct)
        {
            await Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"=== Запуск выгрузки данных ({DateTime.Now:HH:mm:ss}) ===");
                    MainStaticClass.SendOnlineStatus();
                    ct.ThrowIfCancellationRequested();

                    if (MainStaticClass.Last_Write_Check > MainStaticClass.Last_Send_Last_Successful_Sending)
                    {
                        try { ct.ThrowIfCancellationRequested(); var sdsp = new SendDataOnSalesPortions(); sdsp.send_sales_data_Click(null, null); Console.WriteLine("✓ Данные о продажах отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки продаж"); Console.WriteLine($"✗ Продажи: {ex.Message}"); }

                        try { ct.ThrowIfCancellationRequested(); UploadDeletedItems(); Console.WriteLine("✓ Удаленные элементы отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки удаленных элементов"); Console.WriteLine($"✗ Удаленные: {ex.Message}"); }

                        try { ct.ThrowIfCancellationRequested(); send_cdn_logs(); Console.WriteLine("✓ CDN логи отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки CDN логов"); Console.WriteLine($"✗ CDN: {ex.Message}"); }

                        try { ct.ThrowIfCancellationRequested(); UploadErrorsLog(); Console.WriteLine("✓ Логи ошибок отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки логов ошибок"); Console.WriteLine($"✗ Логи: {ex.Message}"); }

                        try { ct.ThrowIfCancellationRequested(); sent_open_close_shop(); Console.WriteLine("✓ Данные о сменах отправлены"); }
                        catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка отправки данных о сменах"); Console.WriteLine($"✗ Смены: {ex.Message}"); }

                        MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
                        Console.WriteLine("✓ Выгрузка завершена");
                    }
                    else { Console.WriteLine("⚠ Нет новых данных для выгрузки"); }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Выгрузка прервана по таймауту");
                    MainStaticClass.WriteRecordErrorLog("Выгрузка прервана по таймауту", "PerformUnloadAsync", 0, MainStaticClass.CashDeskNumber, "CancellationToken");
                    throw;
                }
                catch (Exception ex)
                {
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Непредвиденная ошибка в PerformUnloadAsync");
                    Console.WriteLine($"✗ Критическая ошибка: {ex}");
                    throw;
                }
            }, ct);
        }

        // Вспомогательные классы OpenCloseShop, CdnLogs, DeletedItem, RecordsErrorLog оставляем без изменений
        // ...
        class OpenCloseShop { public DateTime? Open { get; set; } public DateTime? Close { get; set; } public DateTime Date { get; set; } public bool ItsSent { get; set; } }

        private async void sent_open_close_shop()
        {
            List<OpenCloseShop> closeShops = await get_open_close_shop();
            if (closeShops.Count > 0)
            {
                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 20000;
                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0) return;
                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0) return;
                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
                string data = JsonConvert.SerializeObject(closeShops, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string data_crypt = CryptorEngine.Encrypt(data, true, key);
                try
                {
                    bool result = ds.UploadOpeningClosingShops(MainStaticClass.Nick_Shop, data_crypt, "4");
                    if (result) MarkShopsAsSent(closeShops);
                }
                catch { }
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

        private void send_cdn_logs()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                string query = "SELECT num_cash, date, cdn_answer, numdoc, is_sent, mark,status FROM cdn_log WHERE is_sent=0;";
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
                if (logs.ListCdnLog.Count > 0)
                {
                    DS ds = MainStaticClass.get_ds();
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
                            query = "UPDATE cdn_log SET is_sent = 1 WHERE date='" + log.DateShop + "';";
                            command = new NpgsqlCommand(query, conn);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (NpgsqlException) { }
            catch (Exception) { }
            finally { if (conn.State == ConnectionState.Open) conn.Close(); }
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

        private void UploadDeletedItems()
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

                DS ds = MainStaticClass.get_ds();
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
            catch (Exception ex)
            {
                Console.WriteLine("Произошли ошибки при передаче удаленных строк " + ex.Message);
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Не удалось передать информацию об удаленных строках");
            }
            finally { if (conn.State == ConnectionState.Open) conn.Close(); }
        }

        private void UploadErrorsLog()
        {
            try
            {
                var recordsErrorLog = ReadErrorLogsFromDatabase();
                if (recordsErrorLog.ErrorLogs.Count > 0)
                {
                    bool uploadResult = UploadErrorLogsToServer(recordsErrorLog);
                    if (uploadResult) DeleteErrorLogsFromDatabase(recordsErrorLog);
                }
            }
            catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Произошла ошибка при загрузке логов ошибок"); }
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

        private bool UploadErrorLogsToServer(RecordsErrorLog recordsErrorLog)
        {
            string nick_shop = MainStaticClass.Nick_Shop.Trim();
            string code_shop = MainStaticClass.Code_Shop.Trim();
            if (string.IsNullOrEmpty(nick_shop) || string.IsNullOrEmpty(code_shop)) return false;

            string count_day = CryptorEngine.get_count_day();
            string key = nick_shop + count_day + code_shop;
            string data = JsonConvert.SerializeObject(recordsErrorLog, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            string data_crypt = CryptorEngine.Encrypt(data, true, key);

            DS ds = MainStaticClass.get_ds();
            ds.Timeout = 20000;
            try { return ds.UploadErrorLogPortionJson(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString()); }
            catch (Exception ex) { MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию об ошибках в программе"); return false; }
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
            catch (Exception ex) { await MessageBoxHelper.Show($"При загрузке CDN произошла ошибка: {ex.Message}"); }
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