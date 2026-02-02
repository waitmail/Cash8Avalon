using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Npgsql;
using System.Collections;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class CheckActions : Window
    {
        // DataTable для хранения данных
        private DataTable dt1 = null;
        private DataTable dt2 = null;
        private DataTable dt3 = null;

        public ArrayList action_barcode_list = new ArrayList();

        // Элементы управления
        private TextBox InputCodeOrBarcode;
        private TextBox Client;
        private TextBox ClientCode;
        private TextBox SummTextBox; // Изменил название
        private DataGrid DataGridViewTovar;
        private DataGrid DataGridViewTovarExecute; // Исправил имя
        private DataGrid DataGridViewParticipationMechanicalAction; // Исправил имя

        public CheckActions()
        {
            InitializeComponent();
            LoadControls(); // Сначала загружаем контролы
            InitializeDataTables(); // Затем инициализируем таблицы

            // Теперь можно добавлять тестовые данные
            AddTestData();

            // После этого инициализируем DataGrid с данными
            InitializeDataGrids();
            SubscribeToEvents();

            // Устанавливаем фокус на поле ввода
            this.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    InputCodeOrBarcode?.Focus();
                    InputCodeOrBarcode?.SelectAll();
                });
            };
        }

        private void LoadControls()
        {
            try
            {
                // Получаем ссылки на элементы управления
                InputCodeOrBarcode = this.FindControl<TextBox>("txtB_input_code_or_barcode");
                Client = this.FindControl<TextBox>("txtB_client");
                ClientCode = this.FindControl<TextBox>("txtB_client_code");
                SummTextBox = this.FindControl<TextBox>("txtxB_summ"); // Изменил
                DataGridViewTovar = this.FindControl<DataGrid>("dataGridView_tovar");
                DataGridViewTovarExecute = this.FindControl<DataGrid>("dataGridView_tovar_execute"); // Изменил
                DataGridViewParticipationMechanicalAction = this.FindControl<DataGrid>("dataGridView_participation_mechanical_action"); // Изменил

                Console.WriteLine("✓ Все элементы управления загружены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при загрузке контролов: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeDataTables()
        {
            dt1 = CreateDataTable(1);
            dt2 = CreateDataTable(2);
            dt3 = CreateDataTableParticipationMechanicalAction();

            Console.WriteLine($"✓ Таблицы созданы: dt1={dt1 != null}, dt2={dt2 != null}, dt3={dt3 != null}");
        }

        private void InitializeDataGrids()
        {
            try
            {
                Console.WriteLine($"Инициализация DataGrid: dt1.Rows.Count={dt1?.Rows.Count ?? 0}");

                // Устанавливаем данные
                if (DataGridViewTovar != null && dt1 != null)
                {
                    DataGridViewTovar.AutoGenerateColumns = true; // Изменил на true для автогенерации
                    DataGridViewTovar.ItemsSource = dt1.DefaultView;
                    Console.WriteLine($"✓ TovarDataGrid инициализирован");
                }

                if (DataGridViewTovarExecute != null && dt2 != null)
                {
                    DataGridViewTovarExecute.AutoGenerateColumns = true; // Изменил
                    DataGridViewTovarExecute.ItemsSource = dt2.DefaultView;
                    Console.WriteLine($"✓ TovarExecuteDataGrid инициализирован");
                }

                if (DataGridViewParticipationMechanicalAction != null && dt3 != null)
                {
                    DataGridViewParticipationMechanicalAction.AutoGenerateColumns = true; // Изменил
                    DataGridViewParticipationMechanicalAction.ItemsSource = dt3.DefaultView;
                    Console.WriteLine($"✓ ParticipationDataGrid инициализирован");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при инициализации DataGrid: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            // Обработка клавиш
            if (InputCodeOrBarcode != null)
            {
                InputCodeOrBarcode.KeyDown += TxtB_input_code_or_barcode_KeyDown;
            }

            if (ClientCode != null)
            {
                ClientCode.KeyDown += TxtB_client_code_KeyDown;
            }

            this.KeyDown += CheckActions_KeyDown;
        }

        private void TxtB_input_code_or_barcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessBarcodeInput();
                e.Handled = true;
            }
            else if (e.Key != Key.Back && e.Key != Key.Tab &&
                     e.Key != Key.Left && e.Key != Key.Right &&
                     e.Key != Key.Home && e.Key != Key.End &&
                     e.Key != Key.Delete && !(e.Key >= Key.D0 && e.Key <= Key.D9) &&
                     !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                e.Handled = true;
            }
        }

        private void TxtB_client_code_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindClientByCode();
                e.Handled = true;
            }
        }

        private void CheckActions_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                ShowQueryWindowBarcode(1, 0, 0);
                e.Handled = true;
            }
            else if (e.Key == Key.F2 && (e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                ClearAll();
                e.Handled = true;
            }
        }

        private void ProcessBarcodeInput()
        {
            if (InputCodeOrBarcode == null) return;

            string barcode = InputCodeOrBarcode.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(barcode))
            {
                ShowMessage("Введите штрихкод или код товара");
                return;
            }

            FindBarcodeOrCodeInTovar(barcode);
            InputCodeOrBarcode.Text = string.Empty;
            Dispatcher.UIThread.Post(() => InputCodeOrBarcode.Focus());
        }

        /* Поиск товара по штрихкоду и добавление его в табличную часть */
        public async void FindBarcodeOrCodeInTovar(string barcode)
        {
            try
            {
                Console.WriteLine($"Поиск товара по штрихкоду: {barcode}");

                GetParticipationMechanicalAction(barcode);

                string tovarCode = barcode;
                DataRow[] найденныеСтроки = dt1.Select($"tovar_code = '{tovarCode}'");

                if (найденныеСтроки.Length > 0)
                {
                    // Увеличиваем количество существующего товара
                    найденныеСтроки[0]["quantity"] = Convert.ToInt32(найденныеСтроки[0]["quantity"]) + 1;
                    var row = найденныеСтроки[0];
                    row["sum_full"] = Convert.ToDecimal(row["price"]) * Convert.ToDecimal(row["quantity"]);
                    row["sum_at_discount"] = Convert.ToDecimal(row["price_at_discount"]) * Convert.ToDecimal(row["quantity"]);

                    Console.WriteLine($"Товар найден, количество увеличено");
                }
                else
                {
                    // Добавляем новый товар
                    DataRow newRow = dt1.NewRow();
                    newRow["tovar_code"] = tovarCode;
                    newRow["tovar_name"] = $"Товар {barcode}";
                    newRow["quantity"] = 1;
                    newRow["price"] = 100.50m;

                    // Применяем скидку 5% если клиент указан
                    if (Client?.Tag != null && !string.IsNullOrEmpty(Client.Tag.ToString()))
                    {
                        newRow["price_at_discount"] = Math.Round(
                            100.50m - 100.50m * 0.05m, 2);
                    }
                    else
                    {
                        newRow["price_at_discount"] = 100.50m;
                    }

                    newRow["sum_full"] = Convert.ToDecimal(newRow["price"]) * Convert.ToDecimal(newRow["quantity"]);
                    newRow["sum_at_discount"] = Convert.ToDecimal(newRow["price_at_discount"]) * Convert.ToDecimal(newRow["quantity"]);
                    newRow["action"] = 0;
                    newRow["gift"] = 0;
                    newRow["action2"] = 0;
                    newRow["bonus_reg"] = 0;
                    newRow["bonus_action"] = 0;
                    newRow["bonus_action_b"] = 0;
                    newRow["marking"] = "0";
                    newRow["promo_description"] = "";
                    newRow["characteristic_code"] = "";
                    newRow["characteristic_name"] = "";

                    dt1.Rows.Add(newRow);
                    Console.WriteLine($"Новый товар добавлен: {tovarCode}");
                }

                // Обновляем DataGrid
                UpdateDataGridTovar();
                BtnCheckActionsClick();
                Calculate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                await MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
            }
        }

        private async void FindClientByCode()
        {
            if (ClientCode == null) return;

            string clientCode = ClientCode.Text?.Trim() ?? string.Empty;

            if (clientCode.Length != 10 && clientCode.Length != 13)
            {
                await MessageBox.Show("Код клиента имеет неправильную длину", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                return;
            }

            try
            {
                bool findCard = false;

                if (clientCode == "1234567890" || clientCode == "9876543210987")
                {
                    findCard = true;
                    Client.Tag = clientCode;
                    Client.Text = $"Клиент {clientCode}";
                    ClientCode.IsEnabled = false;
                }

                if (!findCard)
                {
                    await MessageBox.Show($"Клиент {clientCode} не найден", "Информация", MessageBoxButton.OK, MessageBoxType.Info);
                    foreach (DataRow row in dt1.Rows)
                    {
                        row["price_at_discount"] = Convert.ToDecimal(row["price"]);
                    }
                }
                else
                {
                    // Применяем скидку 5% для всех товаров
                    foreach (DataRow row in dt1.Rows)
                    {
                        row["price_at_discount"] = Math.Round(
                            Convert.ToDecimal(row["price"]) -
                            Convert.ToDecimal(row["price"]) * 0.05m, 2);
                        row["sum_full"] = Convert.ToDecimal(row["price"]) * Convert.ToDecimal(row["quantity"]);
                        row["sum_at_discount"] = Convert.ToDecimal(row["price_at_discount"]) * Convert.ToDecimal(row["quantity"]);
                    }

                    // Обновляем DataGrid
                    UpdateDataGridTovar();
                }

                ClientCode.Text = "";
                BtnCheckActionsClick();
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка при поиске клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
            }
        }

        private void Calculate()
        {
            decimal total = 0;
            if (dt2 != null)
            {
                foreach (DataRow row in dt2.Rows)
                {
                    total += Convert.ToDecimal(row["sum_at_discount"]);
                }
            }

            if (SummTextBox != null)
            {
                SummTextBox.Text = total.ToString("N2");
            }
        }

        public DataTable CreateDataTable(int variant)
        {
            DataTable dt = new DataTable();

            // Добавляем колонки
            dt.Columns.Add("tovar_code", typeof(string));
            dt.Columns.Add("tovar_name", typeof(string));
            dt.Columns.Add("characteristic_code", typeof(string));
            dt.Columns.Add("characteristic_name", typeof(string));
            dt.Columns.Add("quantity", typeof(int));
            dt.Columns.Add("price", typeof(decimal));
            dt.Columns.Add("price_at_discount", typeof(decimal));
            dt.Columns.Add("sum_full", typeof(decimal));
            dt.Columns.Add("sum_at_discount", typeof(decimal));
            dt.Columns.Add("action", typeof(int));
            dt.Columns.Add("gift", typeof(int));
            dt.Columns.Add("action2", typeof(int));
            dt.Columns.Add("bonus_reg", typeof(int));
            dt.Columns.Add("bonus_action", typeof(int));
            dt.Columns.Add("bonus_action_b", typeof(int));
            dt.Columns.Add("marking", typeof(string));
            dt.Columns.Add("promo_description", typeof(string));

            return dt;
        }

        private DataTable CreateDataTableParticipationMechanicalAction()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("num_doc", typeof(int));
            dt.Columns.Add("tip", typeof(int));
            dt.Columns.Add("comment", typeof(string));
            dt.Columns.Add("execution_order", typeof(int));

            return dt;
        }

        private void GetParticipationMechanicalAction(string tovarCode)
        {
            try
            {
                if (dt3 == null) return;

                dt3.Rows.Clear();

                // Тестовые данные для акций
                DataRow row1 = dt3.NewRow();
                row1["num_doc"] = 1;
                row1["tip"] = 1;
                row1["comment"] = $"Акция для товара {tovarCode}";
                row1["execution_order"] = 1;
                dt3.Rows.Add(row1);

                DataRow row2 = dt3.NewRow();
                row2["num_doc"] = 2;
                row2["tip"] = 2;
                row2["comment"] = $"Скидка на товар {tovarCode}";
                row2["execution_order"] = 2;
                dt3.Rows.Add(row2);

                // Обновляем DataGrid
                UpdateDataGridParticipation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении акций: {ex.Message}");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBox.Show($"Ошибка при получении акций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                });
            }
        }

        public void BtnCheckActionsClick()
        {
            ToDefineTheActionDt(true);
        }

        private void ToDefineTheActionDt(bool showMessages)
        {
            try
            {
                // Копируем данные из dt1 в dt2 с обработкой акций
                if (dt2 == null) dt2 = CreateDataTable(2);
                dt2.Rows.Clear();

                if (dt1 != null)
                {
                    foreach (DataRow sourceRow in dt1.Rows)
                    {
                        DataRow newRow = dt2.NewRow();

                        // Копируем все поля
                        foreach (DataColumn column in dt1.Columns)
                        {
                            newRow[column.ColumnName] = sourceRow[column.ColumnName];
                        }

                        // Здесь будет логика обработки акций
                        // Пока просто копируем данные
                        dt2.Rows.Add(newRow);
                    }
                }

                // Обновляем DataGrid
                UpdateDataGridTovarExecute();
                Calculate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке акций: {ex.Message}");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBox.Show($"Ошибка при обработке акций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                });
            }
        }

        private async void ShowQueryWindowBarcode(int callType, int count, int numDoc)
        {
            // Создаем диалоговое окно для ввода акционного штрихкода
            var dialog = new Window
            {
                Title = "Ввод акционного штрихкода",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var textBox = new TextBox
            {
                Watermark = "Введите акционный штрихкод",
                Margin = new Thickness(20),
                FontSize = 16
            };

            var button = new Button
            {
                Content = "OK",
                Width = 100,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var panel = new StackPanel
            {
                Children = { textBox, button }
            };

            dialog.Content = panel;

            bool result = false;
            button.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    action_barcode_list.Add(textBox.Text);
                    result = true;
                }
                dialog.Close();
            };

            await dialog.ShowDialog(this);

            if (result)
            {
                await MessageBox.Show("Акционный штрихкод добавлен", "Информация", MessageBoxButton.OK, MessageBoxType.Info);
                BtnCheckActionsClick();
            }
        }

        public bool CheckAction(string barcode)
        {
            // Тестовая логика - всегда возвращаем true для демонстрации
            return !string.IsNullOrWhiteSpace(barcode);
        }

        private void ClearAll()
        {
            try
            {
                // Очищаем все таблицы
                if (dt1 != null) dt1.Rows.Clear();
                if (dt2 != null) dt2.Rows.Clear();
                if (dt3 != null) dt3.Rows.Clear();

                // Обновляем DataGrid
                UpdateDataGridTovar();
                UpdateDataGridTovarExecute();
                UpdateDataGridParticipation();

                // Очищаем поля ввода
                if (InputCodeOrBarcode != null) InputCodeOrBarcode.Text = "";
                if (ClientCode != null) ClientCode.Text = "";
                if (Client != null)
                {
                    Client.Text = "";
                    Client.Tag = null;
                }
                if (ClientCode != null) ClientCode.IsEnabled = true;
                if (SummTextBox != null) SummTextBox.Text = "0.00";

                // Очищаем список акционных штрихкодов
                action_barcode_list.Clear();

                // Устанавливаем фокус
                Dispatcher.UIThread.Post(() => InputCodeOrBarcode?.Focus());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при очистке: {ex.Message}");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBox.Show($"Ошибка при очистке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                });
            }
        }

        // Методы обновления DataGrid - ИСПРАВЛЕННЫЕ
        private void UpdateDataGridTovar()
        {
            if (DataGridViewTovar != null && dt1 != null)
            {
                DataGridViewTovar.ItemsSource = null;
                DataGridViewTovar.ItemsSource = dt1.DefaultView;
                Console.WriteLine($"DataGrid товаров обновлен. Строк: {dt1.Rows.Count}");
            }
        }

        private void UpdateDataGridTovarExecute()
        {
            if (DataGridViewTovarExecute != null && dt2 != null)
            {
                DataGridViewTovarExecute.ItemsSource = null;
                DataGridViewTovarExecute.ItemsSource = dt2.DefaultView;
                Console.WriteLine($"DataGrid выполненных товаров обновлен. Строк: {dt2.Rows.Count}");
            }
        }

        private void UpdateDataGridParticipation()
        {
            if (DataGridViewParticipationMechanicalAction != null && dt3 != null)
            {
                DataGridViewParticipationMechanicalAction.ItemsSource = null;
                DataGridViewParticipationMechanicalAction.ItemsSource = dt3.DefaultView;
                Console.WriteLine($"DataGrid участия в акциях обновлен. Строк: {dt3.Rows.Count}");
            }
        }

        private void AddTestData()
        {
            try
            {
                if (dt1 == null)
                {
                    Console.WriteLine("⚠ dt1 не инициализирована");
                    return;
                }

                // Добавляем тестовые данные для визуализации
                DataRow testRow = dt1.NewRow();
                testRow["tovar_code"] = "1234567890123";
                testRow["tovar_name"] = "Тестовый товар 1";
                testRow["quantity"] = 2;
                testRow["price"] = 100.50m;
                testRow["price_at_discount"] = 100.50m;
                testRow["sum_full"] = 201.00m;
                testRow["sum_at_discount"] = 201.00m;
                testRow["action"] = 0;
                testRow["gift"] = 0;
                testRow["action2"] = 0;
                testRow["bonus_reg"] = 0;
                testRow["bonus_action"] = 0;
                testRow["bonus_action_b"] = 0;
                testRow["marking"] = "0";
                testRow["promo_description"] = "";
                testRow["characteristic_code"] = "";
                testRow["characteristic_name"] = "";

                dt1.Rows.Add(testRow);

                Console.WriteLine($"✓ Тестовые данные добавлены. Всего строк в dt1: {dt1.Rows.Count}");

                // Обновляем DataGrid
                UpdateDataGridTovar();
                Calculate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении тестовых данных: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async void ShowMessage(string message)
        {
            await MessageBox.Show(message, "Сообщение", MessageBoxButton.OK, MessageBoxType.Info);
        }

        // Обработка кнопки проверки акций из формы Cash_checks
        public void OpenCheckActions()
        {
            try
            {
                // Устанавливаем фокус на поле ввода
                Dispatcher.UIThread.Post(() =>
                {
                    InputCodeOrBarcode?.Focus();
                    InputCodeOrBarcode?.SelectAll();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBox.Show($"Ошибка при открытии формы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                });
            }
        }
    }
}