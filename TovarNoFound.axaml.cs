using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Timers;

namespace Cash8Avalon
{
    public partial class Tovar_Not_Found : Window
    {
        private Timer _timer;
        private Grid _mainGrid;

        public Tovar_Not_Found()
        {
            InitializeComponent();
            SetupControls();
            SetupTimer();

            // ВАЖНО: Фокус должен быть на окне для получения событий клавиш
            this.Activated += OnWindowActivated;
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

        private void OnWindowActivated(object sender, EventArgs e)
        {
            // Устанавливаем фокус на окно для получения событий клавиш
            this.Focus();
        }

        // Обработка нажатия клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                _timer?.Stop();
                this.Close();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
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

        // Свойства для доступа к элементам
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

        // Очистка ресурсов
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Отписываемся от событий
            this.Activated -= OnWindowActivated;

            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}