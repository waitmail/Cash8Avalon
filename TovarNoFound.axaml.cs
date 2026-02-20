//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Input;
//using Avalonia.Markup.Xaml;
//using Avalonia.Media;
//using System;
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

//            // ВАЖНО: Фокус должен быть на окне для получения событий клавиш
//            this.Activated += OnWindowActivated;
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

//        private void OnWindowActivated(object sender, EventArgs e)
//        {
//            // Устанавливаем фокус на окно для получения событий клавиш
//            this.Focus();
//        }

//        // Обработка нажатия клавиш
//        protected override void OnKeyDown(KeyEventArgs e)
//        {
//            base.OnKeyDown(e);

//            if (e.Key == Key.Escape)
//            {
//                _timer?.Stop();
//                this.Close();
//            }
//        }

//        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
//        {
//            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
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

//        // Свойства для доступа к элементам
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

//        // Очистка ресурсов
//        protected override void OnClosed(EventArgs e)
//        {
//            base.OnClosed(e);

//            // Отписываемся от событий
//            this.Activated -= OnWindowActivated;

//            _timer?.Stop();
//            _timer?.Dispose();
//        }
//    }
//}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
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

            // ГЛАВНОЕ: Opened для Linux
            this.Opened += OnWindowOpened;
            this.Deactivated += OnWindowDeactivated;
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
            await Task.Delay(50); // Даём время оконному менеджеру

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // 1. Активируем окно
                this.Activate();
                this.Focus();

                // 2. Трюк с Topmost для Linux
                this.Topmost = false;
                this.Topmost = true;

                // 3. Задержка для применения
                await Task.Delay(100);

                // 4. Ещё раз фокус
                this.Focus();

                // 5. Финальная активация
                this.Activate();

            }, DispatcherPriority.Render);
        }

        // Если окно потеряло фокус - возвращаем
        private async void OnWindowDeactivated(object sender, EventArgs e)
        {
            if (this.IsVisible)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.Delay(50);

                    this.Topmost = false;
                    this.Topmost = true;
                    this.Activate();
                    this.Focus();

                }, DispatcherPriority.Render);
            }
        }

        // Обработка нажатия клавиш
        protected override async void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {                
                e.Handled = true; // ВАЖНО: помечаем как обработанное
                _timer?.Stop();
                this.Close();
                return;
            }

            base.OnKeyDown(e);
        }

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

            this.Opened -= OnWindowOpened;
            this.Deactivated -= OnWindowDeactivated;

            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}