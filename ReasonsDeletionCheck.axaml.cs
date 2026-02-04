using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace Cash8Avalon
{
    public partial class ReasonsDeletionCheck : Window
    {
        public string Reason = "";
        ComboBox _comboBoxReasons = null;

        public ReasonsDeletionCheck()
        {
            InitializeComponent();            
            FillComboboxReasons();
        }

        private void FillComboboxReasons()
        {
            _comboBoxReasons = this.FindControl<ComboBox>("comboBox_reasons");

            //_comboBoxReasons.Items.Add("Проверка акции.");
            //_comboBoxReasons.Items.Add("Несрабатывание акции, переоценки, уценки.");
            //_comboBoxReasons.Items.Add("Ошибка магазина.");
            //_comboBoxReasons.Items.Add("Техническая ошибка.");
            //_comboBoxReasons.Items.Add("Клиента не устроила цена.");
            //_comboBoxReasons.Items.Add("Клиенту не хватило денег.");
            //_comboBoxReasons.Items.Add("IT-отдел.");
            //_comboBoxReasons.Items.Add("Иные причины.");

            // Если нужно установить начальное значение
            //_comboBoxReasons.SelectedIndex = 0;
            _comboBoxReasons.SelectionChanged += _comboBoxReasons_SelectionChanged;

        }

        private void _comboBoxReasons_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_comboBoxReasons != null)
            {
                if (_comboBoxReasons.SelectedItem is ComboBoxItem selectedItem)
                {
                    Reason = selectedItem.Content?.ToString() ?? string.Empty;                    
                }                
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранную причину
            var selectedItem = _comboBoxReasons.SelectedItem as ListBoxItem;
            string selectedReason = selectedItem?.Content?.ToString() ?? string.Empty;

            // Здесь ваша логика обработки выбора
            // Например, возвращаем результат
            
            this.Close(true);
        }

        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем окно без выбора
            this.Close(false);
        }      
    }
}