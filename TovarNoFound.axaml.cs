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
using System.Timers;

namespace Cash8Avalon
{
    public partial class TovarNotFound : Window
    {
        private Timer _timer;
        private Grid _mainGrid;

        public TovarNotFound()
        {
            InitializeComponent();
            SetupControls();
            SetupTimer();

            // Подписка на события жизненного цикла
            this.Opened += OnWindowOpened;

            // ГЛАВНОЕ ИСПРАВЛЕНИЕ: Подписка на клавиши через AddHandler с Tunnel стратегией
            // Это гарантирует, что ESC будет обработан ДО любой другой логики
            this.AddHandler(KeyDownEvent, OnKeyDownHandler, RoutingStrategies.Tunnel);
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
            _timer = new Timer(1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer.Start();
        }

        // ===== КЛЮЧЕВОЙ МЕТОД ДЛЯ LINUX =====
        private async void OnWindowOpened(object sender, EventArgs e)
        {
            await Task.Delay(50);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                this.Activate();
                this.Focus();

                // Трюк с Topmost для Linux
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    this.Topmost = false;
                    this.Topmost = true;
                }

                await Task.Delay(100);
                this.Focus();
                this.Activate();
            }, DispatcherPriority.Render);
        }

        // ГЛАВНОЕ ИСПРАВЛЕНИЕ: Обработчик клавиш через AddHandler
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"[TovarNotFound] KeyDown: {e.Key}, RoutedEvent: {e.RoutedEvent}");

            if (e.Key == Key.Escape)
            {
                Console.WriteLine("[TovarNotFound] ESC pressed - closing window");
                e.Handled = true; // ВАЖНО: помечаем как обработанное
                _timer?.Stop();
                _timer?.Dispose();
                this.Close();
                return;
            }

            if (e.Key == Key.Enter)
            {
                Console.WriteLine("[TovarNotFound] Enter pressed - closing window");
                e.Handled = true;
                _timer?.Stop();
                _timer?.Dispose();
                this.Close();
                return;
            }
        }

        // УДАЛИТЬ или закомментировать старый OnKeyDown - он больше не нужен
        // protected override async void OnKeyDown(KeyEventArgs e)
        // {
        //     ...
        // }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                SetShowTovarNotFound();
            });
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
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }
}