using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class ReasonsDeletionCheck : Window
    {
        public string Reason = "";
        private ComboBox _comboBoxReasons = null;
        private TextBlock _txtSelectedReason = null;
        private Button _btn_ok = null;

        public ReasonsDeletionCheck()
        {
            InitializeComponent();
            CheckControl();

            // Для всех ОС используем Opened
            this.Opened += OnOpened;
        }

        private async void OnOpened(object sender, EventArgs e)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Для Linux нужна особая последовательность
                await Task.Delay(100);
                this.Activate();
                await Task.Delay(50);
                _comboBoxReasons?.Focus();
                _comboBoxReasons.IsDropDownOpen = true;
                await Task.Delay(50);
                _comboBoxReasons?.Focus(); // двойная попытка фокуса
            }
            else
            {
                // Для Windows - как вы проверили
                _comboBoxReasons.IsDropDownOpen = true;
                _comboBoxReasons?.Focus();
            }
        }

        private void CheckControl()
        {
            _txtSelectedReason = this.FindControl<TextBlock>("txt_selected_reason");
            _btn_ok = this.FindControl<Button>("btn_ok");
            _comboBoxReasons = this.FindControl<ComboBox>("comboBox_reasons");

            _btn_ok.IsEnabled = false;

            _comboBoxReasons.SelectionChanged += _comboBoxReasons_SelectionChanged;
            _comboBoxReasons.KeyDown += ComboBoxReasons_KeyDown;
            _comboBoxReasons.DropDownClosed += ComboBoxReasons_DropDownClosed;
            this.KeyDown += Window_KeyDown;
        }

        private void _comboBoxReasons_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_comboBoxReasons.SelectedItem is ComboBoxItem selectedItem)
            {
                Reason = selectedItem.Content?.ToString() ?? string.Empty;
                UpdateSelectedReasonText();
                _btn_ok.IsEnabled = true;

                // Когда пользователь выбрал элемент, закрываем выпадающий список
                _comboBoxReasons.IsDropDownOpen = false;
            }
        }

        private void ComboBoxReasons_DropDownClosed(object? sender, EventArgs e)
        {
            if (_comboBoxReasons.SelectedItem != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _btn_ok.Focus();
                });
            }
        }

        private void ComboBoxReasons_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_comboBoxReasons.SelectedItem != null)
                {
                    _btn_ok.Focus();
                    e.Handled = true;
                }
                else
                {
                    _comboBoxReasons.IsDropDownOpen = true;
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Down && !_comboBoxReasons.IsDropDownOpen)
            {
                _comboBoxReasons.IsDropDownOpen = true;
                e.Handled = true;
            }

            if (e.Key == Key.Enter && _comboBoxReasons.IsDropDownOpen)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _comboBoxReasons.IsDropDownOpen = false;
                    if (_comboBoxReasons.SelectedItem != null)
                    {
                        _btn_ok.Focus();
                    }
                });
                e.Handled = true;
            }
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _btn_ok.IsFocused && _btn_ok.IsEnabled)
            {
                Btn_ok_Click(sender, e);
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                Btn_cancel_Click(sender, e);
                e.Handled = true;
            }
        }

        private void UpdateSelectedReasonText()
        {
            if (_txtSelectedReason == null) return;

            if (_comboBoxReasons?.SelectedItem is ComboBoxItem selectedItem)
            {
                Reason = selectedItem.Content?.ToString() ?? string.Empty;
                _txtSelectedReason.Text = $"Причина: {Reason}";
                _txtSelectedReason.Foreground = Avalonia.Media.Brushes.Green;
            }
            else
            {
                _txtSelectedReason.Text = "Причина не выбрана";
                _txtSelectedReason.Foreground = Avalonia.Media.Brushes.Gray;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
            if (_comboBoxReasons.SelectedItem is ComboBoxItem selectedItem)
            {
                Reason = selectedItem.Content?.ToString() ?? string.Empty;
                this.Close(true);
            }
        }

        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            Reason = "";
            this.Close(false);
        }
    }
}