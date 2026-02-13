//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Interactivity;
//using Avalonia.Markup.Xaml;
//using Newtonsoft.Json;
//using System;
//using System.Text;

//namespace Cash8Avalon
//{
//    public partial class ReasonsDeletionCheck : Window
//    {
//        public string Reason = "";
//        ComboBox _comboBoxReasons = null;
//        private TextBlock _txtSelectedReason = null;
//        private Button _btn_ok = null;

//        public ReasonsDeletionCheck()
//        {
//            InitializeComponent();
//            CheckControl();            
//        }

//        private void CheckControl()
//        {
//            _txtSelectedReason = this.FindControl<TextBlock>("txt_selected_reason");

//            _btn_ok = this.FindControl<Button>("btn_ok");
//            _btn_ok.IsEnabled = false;

//            _comboBoxReasons = this.FindControl<ComboBox>("comboBox_reasons");
//            _comboBoxReasons.SelectionChanged += _comboBoxReasons_SelectionChanged;
//        }        

//        private void _comboBoxReasons_SelectionChanged(object? sender, SelectionChangedEventArgs e)
//        {
//            if (_comboBoxReasons != null)
//            {
//                if (_comboBoxReasons.SelectedItem is ComboBoxItem selectedItem)
//                {
//                    Reason = selectedItem.Content?.ToString() ?? string.Empty;
//                    UpdateSelectedReasonText();
//                    _btn_ok.IsEnabled = true;
//                }                
//            }
//        }

//        private void UpdateSelectedReasonText()
//        {
//            if (_comboBoxReasons != null && _txtSelectedReason != null)
//            {
//                if (_comboBoxReasons.SelectedItem is ComboBoxItem selectedItem)
//                {
//                    Reason = selectedItem.Content?.ToString() ?? string.Empty;
//                    _txtSelectedReason.Text = $"Причина: {Reason}";
//                    _txtSelectedReason.Foreground = Avalonia.Media.Brushes.Green;
//                    //_txtSelectedReason.FontStyle = FontStyle.Normal;
//                    //_txtSelectedReason.FontWeight = FontWeight.Bold;
//                }
//                else
//                {
//                    _txtSelectedReason.Text = "Причина не выбрана";
//                    _txtSelectedReason.Foreground = Avalonia.Media.Brushes.Gray;
//                    //_txtSelectedReason.FontStyle = FontStyle.Italic;
//                    //_txtSelectedReason.FontWeight = FontWeight.Normal;
//                }
//            }
//        }



//        private void InitializeComponent()
//        {
//            AvaloniaXamlLoader.Load(this);
//        }

//        private void Btn_ok_Click(object sender, RoutedEventArgs e)
//        {
//            // Получаем выбранную причину
//            var selectedItem = _comboBoxReasons.SelectedItem as ListBoxItem;
//            string selectedReason = selectedItem?.Content?.ToString() ?? string.Empty;

//            // Здесь ваша логика обработки выбора
//            // Например, возвращаем результат

//            this.Close(true);
//        }

//        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
//        {
//            // Закрываем окно без выбора
//            this.Close(false);
//        }      
//    }
//}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;

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

            this.Opened += (s, e) =>
            {
                _comboBoxReasons.Focus();
            };

            UpdateSelectedReasonText();
        }

        private void CheckControl()
        {
            _txtSelectedReason = this.FindControl<TextBlock>("txt_selected_reason");
            _btn_ok = this.FindControl<Button>("btn_ok");
            _comboBoxReasons = this.FindControl<ComboBox>("comboBox_reasons");

            _btn_ok.IsEnabled = false;

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
            }
        }

        private void ComboBoxReasons_DropDownClosed(object? sender, EventArgs e)
        {
            if (_comboBoxReasons.SelectedItem != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
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
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
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