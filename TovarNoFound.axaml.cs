//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Input;
//using Avalonia.Markup.Xaml;
//using Avalonia.Media;
//using Avalonia.Threading;
//using System;
//using System.Threading.Tasks;
//using System.Timers;

//namespace Cash8Avalon
//{
//    public partial class TovarNotFound : Window
//    {
//        private Timer _timer;
//        private Grid _mainGrid;

//        public TovarNotFound()
//        {
//            InitializeComponent();
//            SetupControls();
//            SetupTimer();

//            // ГЛАВНОЕ: Opened для Linux
//            this.Opened += OnWindowOpened;
//            this.Deactivated += OnWindowDeactivated;
//        }

//        private void InitializeComponent()
//        {
//            AvaloniaXamlLoader.Load(this);
//        }

//        private void SetupControls()
//        {
//            _mainGrid = this.FindControl<Grid>("MainGrid");
//            if (_mainGrid != null)
//            {
//                _mainGrid.Background = new SolidColorBrush(Colors.Yellow);
//            }
//        }

//        private void SetupTimer()
//        {
//            _timer = new Timer(1000);
//            _timer.Elapsed += Timer_Elapsed;
//            _timer.AutoReset = true;
//            _timer.Enabled = true;
//            _timer.Start();
//        }

//        // ===== КЛЮЧЕВОЙ МЕТОД ДЛЯ LINUX =====
//        private async void OnWindowOpened(object sender, EventArgs e)
//        {
//            await Task.Delay(50); // Даём время оконному менеджеру

//            await Dispatcher.UIThread.InvokeAsync(async () =>
//            {
//                // 1. Активируем окно
//                this.Activate();
//                this.Focus();

//                // 2. Трюк с Topmost для Linux
//                this.Topmost = false;
//                this.Topmost = true;

//                // 3. Задержка для применения
//                await Task.Delay(100);

//                // 4. Ещё раз фокус
//                this.Focus();

//                // 5. Финальная активация
//                this.Activate();

//            }, DispatcherPriority.Render);
//        }

//        // Если окно потеряло фокус - возвращаем
//        private async void OnWindowDeactivated(object sender, EventArgs e)
//        {
//            if (this.IsVisible)
//            {
//                await Dispatcher.UIThread.InvokeAsync(async () =>
//                {
//                    await Task.Delay(50);

//                    this.Topmost = false;
//                    this.Topmost = true;
//                    this.Activate();
//                    this.Focus();

//                }, DispatcherPriority.Render);
//            }
//        }

//        // Обработка нажатия клавиш
//        protected override async void OnKeyDown(KeyEventArgs e)
//        {
//            if (e.Key == Key.Escape)
//            {                
//                e.Handled = true; // ВАЖНО: помечаем как обработанное
//                _timer?.Stop();
//                this.Close();
//                return;
//            }

//            base.OnKeyDown(e);
//        }

//        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
//        {
//            Dispatcher.UIThread.InvokeAsync(() =>
//            {
//                SetShowTovarNotFound();
//            });
//        }

//        private void SetShowTovarNotFound()
//        {
//            if (_mainGrid == null) return;

//            var currentColor = (_mainGrid.Background as SolidColorBrush)?.Color;

//            if (currentColor == Colors.Yellow)
//            {
//                _mainGrid.Background = new SolidColorBrush(Colors.Red);
//            }
//            else
//            {
//                _mainGrid.Background = new SolidColorBrush(Colors.Yellow);
//            }
//        }

//        public string TextBoxText
//        {
//            get
//            {
//                var textBox = this.FindControl<TextBox>("textBox1");
//                return textBox?.Text ?? string.Empty;
//            }
//            set
//            {
//                var textBox = this.FindControl<TextBox>("textBox1");
//                if (textBox != null)
//                    textBox.Text = value;
//            }
//        }

//        public string LabelText
//        {
//            get
//            {
//                var label = this.FindControl<TextBlock>("label1");
//                return label?.Text ?? string.Empty;
//            }
//            set
//            {
//                var label = this.FindControl<TextBlock>("label1");
//                if (label != null)
//                    label.Text = value;
//            }
//        }

//        protected override void OnClosed(EventArgs e)
//        {
//            base.OnClosed(e);

//            this.Opened -= OnWindowOpened;
//            this.Deactivated -= OnWindowDeactivated;

//            _timer?.Stop();
//            _timer?.Dispose();
//        }
//    }
//}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class TovarNotFound : Window
    {
        // Заменили System.Timers.Timer на DispatcherTimer (работает прямо в UI-потоке)
        private DispatcherTimer _dispatcherTimer;
        private Grid _mainGrid;

        // Флаг для предотвращения двойного закрытия (важно для Linux)
        private volatile bool _isClosing = false;

        public TovarNotFound()
        {
            InitializeComponent();
            SetupControls();
            SetupTimer();

            // Подписка на события жизненного цикла
            this.Opened += OnWindowOpened;

            // Подписка на клавиши через AddHandler с Tunnel стратегией
            this.AddHandler(KeyDownEvent, OnKeyDownHandler, RoutingStrategies.Tunnel);

            this.Loaded += Window_FitToScreen;
        }

        /// <summary>
        /// Универсальный метод, который заставляет окно влезть в экран 800x600.
        /// </summary>
        private void Window_FitToScreen(object? sender, RoutedEventArgs e)
        {
            try
            {
                var screen = this.Screens.ScreenFromVisual(this);
                if (screen == null) return;

                var workArea = screen.WorkingArea;

                double windowWidth = this.Bounds.Width + 10;
                double windowHeight = this.Bounds.Height + 10;

                if (windowWidth > workArea.Width || windowHeight > workArea.Height)
                {
                    double targetX = workArea.X + (workArea.Width - windowWidth) / 2;
                    double targetY = workArea.Y + (workArea.Height - windowHeight) / 2;

                    if (targetX < workArea.X) targetX = workArea.X + 5;
                    if (targetY < workArea.Y) targetY = workArea.Y + 5;

                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                    this.Position = new PixelPoint((int)targetX, (int)targetY);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подгонки окна под экран: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupControls()
        {
            _mainGrid = this.FindControl<Grid>("MainGrid");
            if (_mainGrid != null)
            {
                _mainGrid.Background = new SolidColorBrush(Colors.Yellow);
            }
        }

        private void SetupTimer()
        {
            // DispatcherTimer не требует Dispatcher.UIThread.InvokeAsync, 
            // так как сам выполняется в UI-потоке
            _dispatcherTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
            _dispatcherTimer.Start();
        }

        private async void OnWindowOpened(object sender, EventArgs e)
        {
            await Task.Delay(50);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_isClosing) return; // Проверка флага

                this.Activate();
                this.Focus();

                // Трюк для Linux: чтобы вытащить окно на передний план
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    this.Topmost = true;  // Выбиваем наверх
                    // this.Topmost = false; // Раскомментируйте, если не хотите, чтобы окно висело поверх ВСЕХ окон ОС
                }

                await Task.Delay(100);

                if (_isClosing) return;
                this.Focus();
                this.Activate();
            }, DispatcherPriority.Render);

            await Task.Delay(2000); // Ждем 2 секунды

            // Если за 2 секунды не нажали ESC/Enter - закрываем
            CloseWindow();
        }

        /// <summary>
        /// Единый и безопасный метод для закрытия окна
        /// </summary>
        private void CloseWindow()
        {
            // Атомарная проверка: если уже закрываем - выходим, предотвращая двойной Close()
            if (_isClosing) return;
            _isClosing = true;

            _dispatcherTimer?.Stop();

            try
            {
                // На Linux иногда Close() может упасть, если окно уже уничтожается оконным менеджером
                if (this.IsVisible)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при закрытии окна TovarNotFound: {ex.Message}");
            }
        }

        // Обработчик клавиш через AddHandler (Tunnel)
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"[TovarNotFound] KeyDown: {e.Key}, RoutedEvent: {e.RoutedEvent}");

            if (e.Key == Key.Escape)
            {
                Console.WriteLine("[TovarNotFound] ESC pressed - closing window");
                e.Handled = true;
                CloseWindow();
                return;
            }

            if (e.Key == Key.Enter)
            {
                Console.WriteLine("[TovarNotFound] Enter pressed - closing window");
                e.Handled = true;
                CloseWindow();
                return;
            }
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            // Мы уже в UI-потоке, можем безопасно менять цвета
            SetShowTovarNotFound();
        }

        private void SetShowTovarNotFound()
        {
            if (_mainGrid == null) return;

            var currentColor = (_mainGrid.Background as SolidColorBrush)?.Color;

            if (currentColor == Colors.Yellow)
            {
                _mainGrid.Background = new SolidColorBrush(Colors.Red);
            }
            else
            {
                _mainGrid.Background = new SolidColorBrush(Colors.Yellow);
            }
        }

        public string TextBoxText
        {
            get
            {
                var textBox = this.FindControl<TextBox>("textBox1");
                return textBox?.Text ?? string.Empty;
            }
            set
            {
                var textBox = this.FindControl<TextBox>("textBox1");
                if (textBox != null)
                    textBox.Text = value;
            }
        }

        public string LabelText
        {
            get
            {
                var label = this.FindControl<TextBlock>("label1");
                return label?.Text ?? string.Empty;
            }
            set
            {
                var label = this.FindControl<TextBlock>("label1");
                if (label != null)
                    label.Text = value;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Отписка от событий
            this.Opened -= OnWindowOpened;
            this.RemoveHandler(KeyDownEvent, OnKeyDownHandler);

            // Очистка таймера
            _dispatcherTimer?.Stop();
            _dispatcherTimer = null;
        }
    }
}