using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Cash8Avalon
{
    public partial class ReasonsDeletionCheck : Window
    {
        public ReasonsDeletionCheck()
        {
            InitializeComponent();
            // Если нужно установить начальное значение
            comboBox_reasons.SelectedIndex = 0;
        }

        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранную причину
            var selectedItem = comboBox_reasons.SelectedItem as ListBoxItem;
            string selectedReason = selectedItem?.Content?.ToString() ?? string.Empty;

            // Здесь ваша логика обработки выбора
            // Например, возвращаем результат
            this.Close();
        }

        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем окно без выбора
            this.Close();
        }

        private void comboBox_reasons_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработка изменения выбора в комбобоксе
            if (comboBox_reasons.SelectedItem is ListBoxItem selectedItem)
            {
                // Ваша логика при изменении выбора
            }
        }
    }
}