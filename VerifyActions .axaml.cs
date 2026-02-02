using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class VerifyActions : Window
    {
        // Элементы управления из XAML
        private TextBox InputCodeOrBarcode;
        private TextBox Client;
        private TextBox ClientCode;
        private TextBox SummTextBox;

        // Контейнеры для таблиц (созданные в XAML)
        private ScrollViewer _scrollViewerTovar;
        private Grid _gridTovar;
        private ScrollViewer _scrollViewerTovarExecute;
        private Grid _gridTovarExecute;
        private ScrollViewer _scrollViewerParticipation;
        private Grid _gridParticipation;

        // Текущие строки в таблицах
        private int _currentRowTovar = 1;
        private int _currentRowTovarExecute = 1;
        private int _currentRowParticipation = 1;

        // DataTable для хранения данных
        private DataTable dt1 = new DataTable();
        private DataTable dt2 = new DataTable();
        private DataTable dt3 = new DataTable();

        // Список акционных штрихкодов
        public ArrayList action_barcode_list = new ArrayList();

        // Статическое поле для Singleton
        private static VerifyActions _instance;
        public bool IsClosed { get; private set; }

        // Для выделения строк
        private int _selectedRowIndexTovar = -1;
        private int _selectedRowIndexTovarExecute = -1;
        private Border _selectedRowBorderTovar;
        private Border _selectedRowBorderTovarExecute;
        private Grid _activeGrid;

        // Константы для цветов
        private static readonly IBrush SELECTED_ROW_BACKGROUND = Brushes.LightSkyBlue;
        private static readonly IBrush SELECTED_ROW_BORDER = Brushes.DodgerBlue;
        private static readonly IBrush EVEN_ROW_BACKGROUND = Brushes.White;
        private static readonly IBrush ODD_ROW_BACKGROUND = Brushes.AliceBlue;

        public VerifyActions()
        {
            InitializeComponent();
            LoadControls();

            // Создаем таблицы программно
            CreateTovarTableProgrammatically();
            CreateTovarExecuteTableProgrammatically();
            CreateParticipationTableProgrammatically();

            // Инициализируем DataTable
            InitializeDataTables();

            SubscribeToEvents();

            _instance = this;

            // Подписываемся на глобальные события клавиатуры
            this.AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);

            // Обработка закрытия окна
            this.Closed += (s, e) =>
            {
                IsClosed = true;
                _instance = null;
                ClearAll();
            };

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

        // Статический метод для показа окна (Singleton)
        public static void ShowWindow()
        {
            try
            {
                if (_instance == null || _instance.IsClosed)
                {
                    _instance = new VerifyActions();
                    _instance.Show();
                }
                else
                {
                    // Активируем существующее окно
                    _instance.Activate();
                    _instance.Topmost = true;
                    _instance.Topmost = false;
                    _instance.WindowState = WindowState.Normal;
                    _instance.Focus();

                    // Обновляем данные
                    _instance.RefreshWindowData();

                    Dispatcher.UIThread.Post(() =>
                    {
                        _instance.InputCodeOrBarcode?.Focus();
                        _instance.InputCodeOrBarcode?.SelectAll();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии окна VerifyActions: {ex.Message}");
            }
        }

        // Метод для обновления данных при повторном открытии
        public void RefreshWindowData()
        {
            try
            {
                Console.WriteLine("Обновление данных окна VerifyActions...");

                // Очищаем и фокусируем
                ClearAll();

                Dispatcher.UIThread.Post(() =>
                {
                    InputCodeOrBarcode?.Focus();
                    InputCodeOrBarcode?.SelectAll();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении данных: {ex.Message}");
            }
        }

        private void LoadControls()
        {
            try
            {
                Console.WriteLine("=== Загрузка контролов ===");

                // Получаем ссылки на элементы управления
                InputCodeOrBarcode = this.FindControl<TextBox>("txtB_input_code_or_barcode");
                Console.WriteLine($"InputCodeOrBarcode: {InputCodeOrBarcode != null}");

                Client = this.FindControl<TextBox>("txtB_client");
                Console.WriteLine($"Client: {Client != null}");

                ClientCode = this.FindControl<TextBox>("txtB_client_code");
                Console.WriteLine($"ClientCode: {ClientCode != null}");

                SummTextBox = this.FindControl<TextBox>("txtxB_summ");
                Console.WriteLine($"SummTextBox: {SummTextBox != null}");

                Console.WriteLine("✓ Все элементы управления загружены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при загрузке контролов: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreateTovarTableProgrammatically()
        {
            try
            {
                Console.WriteLine("Создание таблицы товаров из кода...");

                var tovarTableContainer = this.FindControl<Border>("TovarTableContainer");
                if (tovarTableContainer == null)
                {
                    Console.WriteLine("✗ TovarTableContainer не найден");
                    return;
                }

                // Создаем ScrollViewer
                _scrollViewerTovar = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White,
                    Focusable = true
                };

                // Подписываемся на события ScrollViewer
                _scrollViewerTovar.PointerPressed += OnScrollViewerTovarPointerPressed;

                // Создаем Grid для таблицы
                _gridTovar = new Grid
                {
                    Background = Brushes.White
                };

                // Подписываемся на события Grid
                _gridTovar.PointerPressed += OnTableGridPointerPressed;

                // Определяем колонки Grid для первого грида: Код, Наименование, К-во, Цена, Сумма
                var columnWidths = new[] { 80, 300, 60, 80, 80 }; // 5 колонок

                foreach (var width in columnWidths)
                {
                    _gridTovar.ColumnDefinitions.Add(new ColumnDefinition(width, GridUnitType.Pixel));
                }

                // Создаем строку заголовков (строка 0)
                _gridTovar.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));
                CreateHeaderRowTovar();

                // Добавляем Grid в ScrollViewer
                _scrollViewerTovar.Content = _gridTovar;

                // Добавляем ScrollViewer в контейнер
                tovarTableContainer.Child = _scrollViewerTovar;

                Console.WriteLine("✓ Таблица товаров создана из кода");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании таблицы товаров: {ex.Message}");
            }
        }

        private void CreateHeaderRowTovar()
        {
            try
            {
                var headers = new[] { "Код", "Наименование", "К-во", "Цена", "Сумма" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var headerBorder = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 1, 2),
                        Background = Brushes.LightBlue,
                        Child = new TextBlock
                        {
                            Text = headers[i],
                            FontWeight = FontWeight.Bold,
                            FontSize = 12,
                            Margin = new Thickness(5, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.DarkBlue,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };

                    Grid.SetColumn(headerBorder, i);
                    Grid.SetRow(headerBorder, 0);
                    _gridTovar.Children.Add(headerBorder);
                }

                Console.WriteLine("✓ Заголовки товаров созданы");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании заголовков: {ex.Message}");
            }
        }

        private void CreateTovarExecuteTableProgrammatically()
        {
            try
            {
                Console.WriteLine("Создание таблицы выполненных товаров из кода...");

                var tovarExecuteTableContainer = this.FindControl<Border>("TovarExecuteTableContainer");
                if (tovarExecuteTableContainer == null)
                {
                    Console.WriteLine("✗ TovarExecuteTableContainer не найден");
                    return;
                }

                // Создаем ScrollViewer
                _scrollViewerTovarExecute = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White,
                    Focusable = true
                };

                // Подписываемся на события ScrollViewer
                _scrollViewerTovarExecute.PointerPressed += OnScrollViewerTovarExecutePointerPressed;

                // Создаем Grid для таблицы
                _gridTovarExecute = new Grid
                {
                    Background = Brushes.White
                };

                // Подписываемся на события Grid
                _gridTovarExecute.PointerPressed += OnTableGridPointerPressed;

                // Определяем колонки Grid для второго грида: Код, Наименование, К-во, Цена, Сумма, Уч.в акции, Акция
                var columnWidths = new[] { 80, 250, 50, 70, 70, 70, 120 }; // 7 колонок

                foreach (var width in columnWidths)
                {
                    _gridTovarExecute.ColumnDefinitions.Add(new ColumnDefinition(width, GridUnitType.Pixel));
                }

                // Создаем строку заголовков (строка 0)
                _gridTovarExecute.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));
                CreateHeaderRowTovarExecute();

                // Добавляем Grid в ScrollViewer
                _scrollViewerTovarExecute.Content = _gridTovarExecute;

                // Добавляем ScrollViewer в контейнер
                tovarExecuteTableContainer.Child = _scrollViewerTovarExecute;

                Console.WriteLine("✓ Таблица выполненных товаров создана из кода");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании таблицы выполненных товаров: {ex.Message}");
            }
        }

        private void CreateHeaderRowTovarExecute()
        {
            try
            {
                var headers = new[] { "Код", "Наименование", "К-во", "Цена", "Сумма", "Уч.в акции", "Акция" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var headerBorder = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 1, 2),
                        Background = Brushes.LightGreen,
                        Child = new TextBlock
                        {
                            Text = headers[i],
                            FontWeight = FontWeight.Bold,
                            FontSize = 12,
                            Margin = new Thickness(5, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.DarkGreen,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };

                    Grid.SetColumn(headerBorder, i);
                    Grid.SetRow(headerBorder, 0);
                    _gridTovarExecute.Children.Add(headerBorder);
                }

                Console.WriteLine("✓ Заголовки выполненных товаров созданы");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании заголовков: {ex.Message}");
            }
        }

        private void CreateParticipationTableProgrammatically()
        {
            try
            {
                Console.WriteLine("Создание таблицы участия в акциях из кода...");

                var participationTableContainer = this.FindControl<Border>("ParticipationTableContainer");
                if (participationTableContainer == null)
                {
                    Console.WriteLine("✗ ParticipationTableContainer не найден");
                    return;
                }

                // Создаем ScrollViewer
                _scrollViewerParticipation = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White,
                    Focusable = true
                };

                // Создаем Grid для таблицы
                _gridParticipation = new Grid
                {
                    Background = Brushes.White
                };

                // Определяем колонки Grid
                var columnWidths = new[] { 80, 60, 300, 100 };

                foreach (var width in columnWidths)
                {
                    _gridParticipation.ColumnDefinitions.Add(new ColumnDefinition(width, GridUnitType.Pixel));
                }

                // Создаем строку заголовков (строка 0)
                _gridParticipation.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));
                CreateHeaderRowParticipation();

                // Добавляем Grid в ScrollViewer
                _scrollViewerParticipation.Content = _gridParticipation;

                // Добавляем ScrollViewer в контейнер
                participationTableContainer.Child = _scrollViewerParticipation;

                Console.WriteLine("✓ Таблица участия в акциях создана из кода");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании таблицы участия в акциях: {ex.Message}");
            }
        }

        private void CreateHeaderRowParticipation()
        {
            try
            {
                var headers = new[] { "Номер док.", "Тип", "Комментарий", "Порядок" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var headerBorder = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 1, 2),
                        Background = Brushes.LightPink,
                        Child = new TextBlock
                        {
                            Text = headers[i],
                            FontWeight = FontWeight.Bold,
                            FontSize = 12,
                            Margin = new Thickness(5, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.DarkMagenta,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };

                    Grid.SetColumn(headerBorder, i);
                    Grid.SetRow(headerBorder, 0);
                    _gridParticipation.Children.Add(headerBorder);
                }

                Console.WriteLine("✓ Заголовки участия в акциях созданы");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании заголовков: {ex.Message}");
            }
        }

        private void InitializeDataTables()
        {
            dt1 = CreateDataTable(1);
            dt2 = CreateDataTable(2);
            dt3 = CreateDataTableParticipationMechanicalAction();

            Console.WriteLine($"✓ Таблицы созданы: dt1={dt1 != null}, dt2={dt2 != null}, dt3={dt3 != null}");
        }

        // Метод SubscribeToEvents не нужно делать асинхронным, так как это просто подписка на события
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
        }

        // Обработка нажатия клавиш в поле ввода штрихкода
        private async void TxtB_input_code_or_barcode_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем состояние окна и активируем его если нужно
            //CheckAndActivateWindow();

            // Очищаем переводы строк
            if (InputCodeOrBarcode != null)
            {
                InputCodeOrBarcode.Text = InputCodeOrBarcode.Text?.Replace("\r\n", "") ?? "";
            }

            if (e.Key == Key.Enter)
            {
                if (string.IsNullOrEmpty(InputCodeOrBarcode?.Text))
                {
                    Console.WriteLine("Штрихкод не найден");
                    return;
                }

                // Используем await для асинхронного вызова
                await FindBarcodeOrCodeInTovar(InputCodeOrBarcode.Text);
                InputCodeOrBarcode.Text = "";
                e.Handled = true;
                CheckAndActivateWindow();
                return;
            }

            // Разрешаем только цифры и Backspace
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) &&
                !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) &&
                e.Key != Key.Back && e.Key != Key.Delete &&
                e.Key != Key.Left && e.Key != Key.Right &&
                e.Key != Key.Home && e.Key != Key.End)
            {
                e.Handled = true;
            }
        }       
        

        // Вспомогательный метод для проверки и активации окна
        private void CheckAndActivateWindow()
        {
            try
            {
                // Получаем текущее окно
                var window = this.VisualRoot as Window;

                if (window == null)
                {
                    // Пытаемся найти окно через TopLevel
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel is Window w)
                    {
                        window = w;
                    }
                }

                if (window != null)
                {
                    // Проверяем состояние окна
                    if (window.WindowState == WindowState.Minimized)
                    {
                        // Восстанавливаем свернутое окно
                        window.WindowState = WindowState.Normal;
                        Console.WriteLine("Окно восстановлено из свернутого состояния");
                    }

                    // Активируем окно (делаем активным)
                    window.Activate();

                    // Фокусируемся на поле ввода
                    if (InputCodeOrBarcode != null && InputCodeOrBarcode.IsVisible)
                    {
                        InputCodeOrBarcode.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при активации окна: {ex.Message}");
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

        /* Поиск товара по штрихкоду и добавление его в табличную часть */
        public async Task FindBarcodeOrCodeInTovar(string barcode)
        {
            NpgsqlConnection conn = null;

            try
            {
                Console.WriteLine($"Поиск товара по штрихкоду: {barcode}");

                // Проверяем, является ли штрихкод акционным
                if (CheckAction(barcode))
                {
                    action_barcode_list.Add(barcode);
                    GetParticipationMechanicalAction(barcode);
                    Console.WriteLine($"Добавлен акционный штрихкод: {barcode}");
                    BtnCheckActionsClick();
                    return;
                }

                // Поиск товара в БД
                bool tovarFound = false;

                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                Console.WriteLine("Подключение к БД открыто");

                using (var command = new NpgsqlCommand())
                {
                    command.Connection = conn;

                    if (barcode.Length > 6)
                    {
                        command.CommandText = @"SELECT tovar.code, tovar.name, tovar.retail_price, 
                                      characteristic.name as characteristic_name, 
                                      characteristic.guid, characteristic.retail_price_characteristic,
                                      tovar.its_certificate, tovar.its_marked, tovar.cdn_check, tovar.fractional 
                                      FROM barcode 
                                      LEFT JOIN tovar ON barcode.tovar_code = tovar.code 
                                      LEFT JOIN characteristic ON tovar.code = characteristic.tovar_code 
                                      WHERE barcode = @barcode 
                                      AND its_deleted = 0  
                                      AND (retail_price <> 0 OR characteristic.retail_price_characteristic <> 0)";
                        command.Parameters.AddWithValue("@barcode", barcode);
                    }
                    else
                    {
                        command.CommandText = @"SELECT tovar.code, tovar.name, tovar.retail_price, 
                                      characteristic.name as characteristic_name, 
                                      characteristic.guid, characteristic.retail_price_characteristic,
                                      tovar.its_certificate, tovar.its_marked, tovar.cdn_check, tovar.fractional 
                                      FROM tovar 
                                      LEFT JOIN characteristic ON tovar.code = characteristic.tovar_code 
                                      WHERE tovar.its_deleted = 0 
                                      AND tovar.its_certificate = 0 
                                      AND (retail_price <> 0 OR characteristic.retail_price_characteristic <> 0) 
                                      AND tovar.code = @barcode";
                        command.Parameters.AddWithValue("@barcode", Convert.ToInt64(barcode));
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tovarFound = true;

                            string tovarCode = reader["code"].ToString().Trim();
                            string tovarName = reader["name"].ToString().Trim();
                            string characteristicName = reader["characteristic_name"]?.ToString()?.Trim() ?? "";

                            Console.WriteLine($"Найден товар: {tovarCode} - {tovarName} ({characteristicName})");

                            // Получаем информацию об акциях для этого товара
                            GetParticipationMechanicalAction(tovarCode);

                            // Ищем товар в DataTable
                            DataRow row = null;
                            DataRow[] найденныеСтроки = dt1.Select($"tovar_code = '{tovarCode}'");

                            decimal price = 0;
                            if (reader["retail_price_characteristic"] != DBNull.Value && Convert.ToDecimal(reader["retail_price_characteristic"]) > 0)
                            {
                                price = Convert.ToDecimal(reader["retail_price_characteristic"]);
                            }
                            else
                            {
                                price = Convert.ToDecimal(reader["retail_price"]);
                            }

                            if (найденныеСтроки.Length > 0)
                            {
                                // Товар уже есть в таблице - увеличиваем количество
                                найденныеСтроки[0]["quantity"] = Convert.ToInt32(найденныеСтроки[0]["quantity"]) + 1;
                                row = найденныеСтроки[0];
                                row["price_at_discount"] = GetPriceWithDiscount(price);
                                row["sum_at_discount"] = Convert.ToDecimal(row["price_at_discount"]) * Convert.ToDecimal(row["quantity"]);
                                row["sum_full"] = price * Convert.ToDecimal(row["quantity"]);

                                // Обновляем строку в таблице
                                UpdateRowInTovarTable(row, найденныеСтроки[0]);
                            }
                            else
                            {
                                // Новый товар - создаем строку
                                row = dt1.NewRow();
                                row["tovar_code"] = tovarCode;
                                row["tovar_name"] = tovarName;
                                row["characteristic_name"] = characteristicName;
                                row["characteristic_code"] = reader["guid"]?.ToString()?.Trim() ?? "";
                                row["quantity"] = 1;
                                row["price"] = price;
                                row["price_at_discount"] = GetPriceWithDiscount(price);
                                row["sum_full"] = price * Convert.ToDecimal(row["quantity"]);
                                row["sum_at_discount"] = Convert.ToDecimal(row["price_at_discount"]) * Convert.ToDecimal(row["quantity"]);
                                row["action"] = 0;
                                row["gift"] = 0;
                                row["action2"] = 0;
                                row["bonus_reg"] = 0;
                                row["bonus_action"] = 0;
                                row["bonus_action_b"] = 0;
                                row["marking"] = "0";
                                row["promo_description"] = "";

                                dt1.Rows.Add(row);

                                // Добавляем новую строку в таблицу
                                AddRowToTovarTable(row);
                            }

                            BtnCheckActionsClick();
                        }
                    }
                }

                if (!tovarFound)
                {
                    Console.WriteLine("Товар не найден");
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine("Ошибка БД: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            Calculate();
            //CheckAndActivateWindow();
        }

        // Получение цены со скидкой
        private decimal GetPriceWithDiscount(decimal basePrice)
        {
            if (Client?.Tag != null && !string.IsNullOrEmpty(Client.Tag.ToString()))
            {
                return Math.Round(basePrice - basePrice * 0.05m, 2);
            }
            return basePrice;
        }

        // Обновление строки в таблице товаров
        private void UpdateRowInTovarTable(DataRow dataRow, DataRow originalRow)
        {
            try
            {
                int rowIndex = dt1.Rows.IndexOf(originalRow);
                if (rowIndex >= 0 && rowIndex < _gridTovar.RowDefinitions.Count - 1)
                {
                    int gridRowIndex = rowIndex + 1; // +1 потому что строка 0 - заголовки

                    // Обновляем ячейки в Grid
                    UpdateCellInGrid(_gridTovar, 0, gridRowIndex, dataRow["tovar_code"].ToString());
                    UpdateCellInGrid(_gridTovar, 1, gridRowIndex, dataRow["tovar_name"].ToString());
                    UpdateCellInGrid(_gridTovar, 2, gridRowIndex, dataRow["quantity"].ToString(), HorizontalAlignment.Right);
                    UpdateCellInGrid(_gridTovar, 3, gridRowIndex, Convert.ToDecimal(dataRow["price_at_discount"]).ToString("N2"), HorizontalAlignment.Right);
                    UpdateCellInGrid(_gridTovar, 4, gridRowIndex, Convert.ToDecimal(dataRow["sum_at_discount"]).ToString("N2"), HorizontalAlignment.Right);

                    Console.WriteLine($"✓ Обновлена строка {gridRowIndex} в таблице товаров");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при обновлении строки в таблице товаров: {ex.Message}");
            }
        }

        // Обновление ячейки в Grid
        private void UpdateCellInGrid(Grid grid, int column, int row, string text, HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            try
            {
                // Ищем существующий TextBlock в этой ячейке
                foreach (var child in grid.Children)
                {
                    if (child is TextBlock textBlock && Grid.GetColumn(textBlock) == column && Grid.GetRow(textBlock) == row)
                    {
                        textBlock.Text = text;
                        textBlock.HorizontalAlignment = alignment;
                        return;
                    }
                }

                // Если не нашли, создаем новую ячейку
                AddCellToGrid(grid, column, row, text, alignment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении ячейки: {ex.Message}");
            }
        }

        // Проверка акции по штрихкоду
        public bool CheckAction(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return false;
            }

            NpgsqlConnection conn = null;
            int count_action = 0;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = "";
                if (barcode.Trim().Length > 4)
                {
                    query = "SELECT COUNT(*) FROM action_header WHERE @date between date_started AND date_end AND barcode = @barcode";
                }
                else
                {
                    query = "SELECT COUNT(*) FROM action_header WHERE @date between date_started AND date_end AND promo_code = @barcode";
                }

                using (var command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@date", DateTime.Now.Date);
                    command.Parameters.AddWithValue("@barcode", barcode.Trim());
                    count_action = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Ошибка при работе с базой данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return count_action > 0;
        }

        private async void FindClientByCode()
        {
            if (ClientCode == null) return;

            string clientCode = ClientCode.Text?.Trim() ?? string.Empty;

            if (clientCode.Length != 10 && clientCode.Length != 13)
            {
                Console.WriteLine("Код клиента имеет неправильную длину");
                return;
            }

            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = "";
                if (clientCode.Length == 10)
                {
                    if (clientCode.Substring(0, 1) == "9")
                    {
                        query = "SELECT code, name FROM clients WHERE phone = @code";
                    }
                    else
                    {
                        query = "SELECT code, name FROM clients WHERE code = @code";
                    }
                }
                else
                {
                    query = "SELECT code, name FROM clients WHERE code = @code";
                }

                using (var command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@code", clientCode);
                    using (var reader = command.ExecuteReader())
                    {
                        bool findCard = false;
                        while (reader.Read())
                        {
                            findCard = true;
                            Client.Tag = reader["code"].ToString();
                            Client.Text = reader["name"].ToString();
                            ClientCode.IsEnabled = false;
                            break;
                        }

                        if (!findCard)
                        {
                            Console.WriteLine($"Клиент {clientCode} не найден");
                            // Обновляем цены без скидки
                            foreach (DataRow row in dt1.Rows)
                            {
                                decimal price = Convert.ToDecimal(row["price"]);
                                row["price_at_discount"] = price;
                                row["sum_at_discount"] = price * Convert.ToDecimal(row["quantity"]);
                            }
                            RefreshTovarTable();
                        }
                        else
                        {
                            // Обновляем цены со скидкой
                            foreach (DataRow row in dt1.Rows)
                            {
                                decimal price = Convert.ToDecimal(row["price"]);
                                row["price_at_discount"] = GetPriceWithDiscount(price);
                                row["sum_at_discount"] = Convert.ToDecimal(row["price_at_discount"]) * Convert.ToDecimal(row["quantity"]);
                            }
                            RefreshTovarTable();
                        }
                    }
                }

                ClientCode.Text = "";
                BtnCheckActionsClick();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine("Произошли ошибки при поиске клиента: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошли ошибки при поиске клиента: " + ex.Message);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            Calculate();
        }

        // Обновление отображения таблицы товаров
        private void RefreshTovarTable()
        {
            try
            {
                // Очищаем таблицу
                ClearTableTovar();
                _currentRowTovar = 1;

                // Перерисовываем все строки
                foreach (DataRow row in dt1.Rows)
                {
                    AddRowToTovarTable(row);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении таблицы товаров: {ex.Message}");
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
            NpgsqlConnection conn = null;

            try
            {
                Console.WriteLine($"\n=== GetParticipationMechanicalAction для: '{tovarCode}' ===");

                // Очищаем таблицу
                if (dt3 == null)
                {
                    Console.WriteLine("ERROR: dt3 is null!");
                    return;
                }

                dt3.Rows.Clear();
                ClearTableParticipation();

                // Ищем акции для товара
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                // Исправленный запрос - код товара сравнивается как текст
                string query = @"
            SELECT action_header.num_doc, action_header.comment, action_header.tip,
                   action_header.execution_order 
            FROM action_table 
            LEFT JOIN action_header ON action_table.num_doc = action_header.num_doc 
            WHERE @date_time between date_started and date_end
            AND
            (action_table.code_tovar::text = @tovar_code 
            OR 
            action_table.code_tovar IN 
            (SELECT tovar_code FROM public.barcode WHERE barcode = @tovar_code)) 
            ORDER BY execution_order";

                using (var command = new NpgsqlCommand(query, conn))
                {
                    // ПЕРЕДАЕМ КАК СТРОКУ
                    command.Parameters.AddWithValue("@tovar_code", tovarCode);

                    // Дата должна быть передана как date
                    command.Parameters.AddWithValue("@date_time", DateTime.Now.Date);

                    using (var reader = command.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            count++;
                            DataRow row = dt3.NewRow();

                            // Получаем данные
                            row["num_doc"] = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader["num_doc"]);
                            row["tip"] = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader["tip"]);
                            row["comment"] = reader.IsDBNull(1) ? "" : reader["comment"].ToString().Trim();
                            row["execution_order"] = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader["execution_order"]);

                            dt3.Rows.Add(row);
                            AddRowToParticipationTable(row);

                            Console.WriteLine($"  - Акция: {row["num_doc"]}, Комментарий: {row["comment"]}");
                        }

                        Console.WriteLine($"Найдено {count} акций для товара {tovarCode}");

                        if (count == 0)
                        {
                            //AddNoParticipationMessage();
                        }
                    }
                }

                Console.WriteLine($"=== GetParticipationMechanicalAction завершен ===");
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Ошибка БД при поиске акций: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        // Методы добавления строк в таблицы
        private void AddRowToTovarTable(DataRow dataRow)
        {
            try
            {
                // Добавляем новую строку в Grid
                _gridTovar.RowDefinitions.Add(new RowDefinition(25, GridUnitType.Pixel));

                // Определяем цвет фона строки (чередование)
                var rowBackground = ((_currentRowTovar - 1) % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;

                // Создаем Border для всей строки
                var rowBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = rowBackground,
                    Tag = _currentRowTovar - 1 // Сохраняем индекс данных
                };

                // Подписываемся на события клика
                rowBorder.PointerPressed += OnTovarRowPointerPressed;

                Grid.SetColumnSpan(rowBorder, 5); // 5 колонок в первом гриде
                Grid.SetRow(rowBorder, _currentRowTovar);
                _gridTovar.Children.Add(rowBorder);

                // Добавляем ячейки для первого грида: Код, Наименование, К-во, Цена, Сумма
                AddCellToGrid(_gridTovar, 0, _currentRowTovar, dataRow["tovar_code"].ToString());
                AddCellToGrid(_gridTovar, 1, _currentRowTovar, dataRow["tovar_name"].ToString());
                AddCellToGrid(_gridTovar, 2, _currentRowTovar, dataRow["quantity"].ToString(), HorizontalAlignment.Right);
                AddCellToGrid(_gridTovar, 3, _currentRowTovar, Convert.ToDecimal(dataRow["price_at_discount"]).ToString("N2"), HorizontalAlignment.Right);
                AddCellToGrid(_gridTovar, 4, _currentRowTovar, Convert.ToDecimal(dataRow["sum_at_discount"]).ToString("N2"), HorizontalAlignment.Right);

                _currentRowTovar++;

                Console.WriteLine($"✓ Добавлена строка в таблицу товаров {_currentRowTovar - 1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении строки в таблицу товаров: {ex.Message}");
            }
        }

        private void AddRowToTovarExecuteTable(DataRow dataRow)
        {
            try
            {
                // Добавляем новую строку в Grid
                _gridTovarExecute.RowDefinitions.Add(new RowDefinition(25, GridUnitType.Pixel));

                // Определяем цвет фона строки (чередование)
                var rowBackground = ((_currentRowTovarExecute - 1) % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;

                // Создаем Border для всей строки
                var rowBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = rowBackground,
                    Tag = _currentRowTovarExecute - 1
                };

                // Подписываемся на события клика
                rowBorder.PointerPressed += OnTovarExecuteRowPointerPressed;

                Grid.SetColumnSpan(rowBorder, 7); // 7 колонок во втором гриде
                Grid.SetRow(rowBorder, _currentRowTovarExecute);
                _gridTovarExecute.Children.Add(rowBorder);

                // Добавляем ячейки для второго грида: Код, Наименование, К-во, Цена, Сумма, Уч.в акции, Акция
                AddCellToGrid(_gridTovarExecute, 0, _currentRowTovarExecute, dataRow["tovar_code"].ToString());
                AddCellToGrid(_gridTovarExecute, 1, _currentRowTovarExecute, dataRow["tovar_name"].ToString());
                AddCellToGrid(_gridTovarExecute, 2, _currentRowTovarExecute, dataRow["quantity"].ToString(), HorizontalAlignment.Right);
                AddCellToGrid(_gridTovarExecute, 3, _currentRowTovarExecute, Convert.ToDecimal(dataRow["price_at_discount"]).ToString("N2"), HorizontalAlignment.Right);
                AddCellToGrid(_gridTovarExecute, 4, _currentRowTovarExecute, Convert.ToDecimal(dataRow["sum_at_discount"]).ToString("N2"), HorizontalAlignment.Right);
                AddCellToGrid(_gridTovarExecute, 5, _currentRowTovarExecute, dataRow["action2"].ToString(), HorizontalAlignment.Center);

                // Для колонки "Акция" добавляем TextBlock с обрезкой текста
                var promoText = dataRow["promo_description"].ToString();
                var promoTextBlock = new TextBlock
                {
                    Text = promoText,
                    Margin = new Thickness(5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontSize = 10,
                    TextWrapping = TextWrapping.NoWrap,
                    TextTrimming = TextTrimming.CharacterEllipsis // Обрезка текста с многоточием
                };

                Grid.SetColumn(promoTextBlock, 6);
                Grid.SetRow(promoTextBlock, _currentRowTovarExecute);
                _gridTovarExecute.Children.Add(promoTextBlock);

                _currentRowTovarExecute++;

                Console.WriteLine($"✓ Добавлена строка в таблицу выполненных товаров {_currentRowTovarExecute - 1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении строки в таблицу выполненных товаров: {ex.Message}");
            }
        }

        private void AddRowToParticipationTable(DataRow dataRow)
        {
            try
            {
                // Добавляем новую строку в Grid
                _gridParticipation.RowDefinitions.Add(new RowDefinition(25, GridUnitType.Pixel));

                // Определяем цвет фона строки (чередование)
                var rowBackground = (_currentRowParticipation % 2 == 0) ? Brushes.White : Brushes.AliceBlue;

                // Создаем Border для всей строки
                var rowBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = rowBackground,
                    Tag = _currentRowParticipation - 1
                };

                Grid.SetColumnSpan(rowBorder, 4);
                Grid.SetRow(rowBorder, _currentRowParticipation);
                _gridParticipation.Children.Add(rowBorder);

                // Добавляем ячейки как в старом проекте
                AddCellToGrid(_gridParticipation, 0, _currentRowParticipation,
                             dataRow["num_doc"].ToString(), HorizontalAlignment.Right);

                // Преобразуем тип акции в читаемый текст
                string tipText = GetTipDescription(Convert.ToInt32(dataRow["tip"]));
                AddCellToGrid(_gridParticipation, 1, _currentRowParticipation, tipText, HorizontalAlignment.Center);

                AddCellToGrid(_gridParticipation, 2, _currentRowParticipation,
                             dataRow["comment"].ToString());

                AddCellToGrid(_gridParticipation, 3, _currentRowParticipation,
                             dataRow["execution_order"].ToString(), HorizontalAlignment.Right);

                _currentRowParticipation++;

                Console.WriteLine($"✓ Добавлена строка в таблицу участия {_currentRowParticipation - 1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении строки в таблицу участия: {ex.Message}");
            }
        }

        // Метод для преобразования числового типа акции в текст
        private string GetTipDescription(int tip)
        {
            return tip switch
            {
                1 => "Подарок",
                2 => "Скидка",
                3 => "Бонус",
                4 => "Умножение",
                _ => tip.ToString()
            };
        }

        private void AddCellToGrid(Grid grid, int column, int row, string text, HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(textBlock, column);
            Grid.SetRow(textBlock, row);
            grid.Children.Add(textBlock);
        }

        // Методы очистки таблиц
        private void ClearTableTovar()
        {
            ClearGrid(_gridTovar, ref _currentRowTovar);
        }

        private void ClearTableTovarExecute()
        {
            ClearGrid(_gridTovarExecute, ref _currentRowTovarExecute);
        }

        private void ClearTableParticipation()
        {
            ClearGrid(_gridParticipation, ref _currentRowParticipation);
        }

        private void ClearGrid(Grid grid, ref int currentRow)
        {
            try
            {
                // Удаляем выделение
                ClearSelection();

                // Удаляем все строки кроме заголовков
                while (grid.RowDefinitions.Count > 1)
                {
                    grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);
                }

                // Удаляем все элементы кроме заголовков
                var elementsToRemove = new List<Control>();
                foreach (Control child in grid.Children)
                {
                    if (Grid.GetRow(child) > 0)
                    {
                        elementsToRemove.Add(child);
                    }
                }

                foreach (var element in elementsToRemove)
                {
                    grid.Children.Remove(element);
                }

                currentRow = 1;
                Console.WriteLine($"✓ Таблица очищена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при очистке таблицы: {ex.Message}");
            }
        }

        // Аналог to_define_the_action_dt из WinForms
        public void BtnCheckActionsClick()
        {
            ToDefineTheActionDt(true);
        }

        /// <summary>
        /// Здесь происходит обработка по всем 
        /// регулярным акциям те что со штрихкодом
        /// и те что без штрихкода        
        /// </summary>
        private async void ToDefineTheActionDt(bool showMessages)
        {
            try
            {
                Console.WriteLine("Обработка акций через ProcessingOfActions...");

                // Создаем экземпляр ProcessingOfActions
                var processingOfActions = new ProcessingOfActions();

                // Копируем данные из dt1 (как в старом коде)
                processingOfActions.dt = dt1.Copy();
                processingOfActions.show_messages = showMessages;

                Console.WriteLine($"Количество строк для обработки: {processingOfActions.dt.Rows.Count}");
                Console.WriteLine($"Количество акционных штрихкодов: {action_barcode_list.Count}");

                // Обработка акционных штрихкодов
                foreach (string barcode in action_barcode_list)
                {
                    Console.WriteLine($"Обработка акционного штрихкода: {barcode}");
                    processingOfActions.to_define_the_action_dt(barcode);
                }

                // Обработка персональных акций
                if (Client?.Tag != null)
                {
                    Console.WriteLine($"Обработка персональных акций для клиента: {Client.Tag}");
                    await processingOfActions.to_define_the_action_personal_dt(Client.Tag.ToString());
                }

                // Основная обработка акций
                await processingOfActions.to_define_the_action_dt();

                // Получаем обработанные данные
                dt2 = processingOfActions.dt.Copy();

                // Очищаем таблицу выполненных товаров
                ClearTableTovarExecute();
                _currentRowTovarExecute = 1; // Сбрасываем счетчик строк

                // Добавляем строки из dt2 в таблицу выполненных товаров
                foreach (DataRow row in dt2.Rows)
                {
                    AddRowToTovarExecuteTable(row);
                }

                // Подсвечиваем строки с action2 != 0 (как в старом коде)
                ApplyRowHighlighting();

                Calculate();

                Console.WriteLine($"✓ Акции обработаны через ProcessingOfActions. Всего выполненных товаров: {dt2.Rows.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке акций: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            CheckAndActivateWindow();
        }

        // Метод для подсветки строк с action2 != 0
        private void ApplyRowHighlighting()
        {
            try
            {
                Console.WriteLine("Применение подсветки строк...");

                // Получаем все строки Border из таблицы выполненных товаров
                var rowBorders = new List<Border>();
                foreach (var child in _gridTovarExecute.Children)
                {
                    if (child is Border border && Grid.GetRow(border) > 0)
                    {
                        rowBorders.Add(border);
                    }
                }

                // Сортируем по строке
                rowBorders.Sort((a, b) => Grid.GetRow(a).CompareTo(Grid.GetRow(b)));

                // Подсвечиваем строки
                for (int i = 0; i < rowBorders.Count; i++)
                {
                    if (i < dt2.Rows.Count)
                    {
                        var row = dt2.Rows[i];
                        var action2 = Convert.ToInt32(row["action2"]);

                        if (action2 != 0)
                        {
                            rowBorders[i].Background = Brushes.LightYellow;
                            Console.WriteLine($"Строка {i + 1} подсвечена (action2 = {action2})");
                        }
                        else
                        {
                            // Чередование цветов как раньше
                            int dataRowIndex = (int)rowBorders[i].Tag;
                            rowBorders[i].Background = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при подсветке строк: {ex.Message}");
            }
        }

        private async void ShowQueryWindowBarcode(int callType, int count, int numDoc)
        {
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
                Console.WriteLine("Акционный штрихкод добавлен");
                BtnCheckActionsClick();
            }
        }

        private void ClearAll()
        {
            try
            {
                // Очищаем все таблицы
                dt1.Rows.Clear();
                dt2.Rows.Clear();
                dt3.Rows.Clear();

                // Очищаем визуальные таблицы
                ClearTableTovar();
                ClearTableTovarExecute();
                ClearTableParticipation();

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

                // Снимаем выделение
                ClearSelection();

                // Устанавливаем фокус
                Dispatcher.UIThread.Post(() => InputCodeOrBarcode?.Focus());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при очистке: {ex.Message}");
            }
        }

        public void OpenVerifyActions()
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    InputCodeOrBarcode?.Focus();
                    InputCodeOrBarcode?.SelectAll();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии формы: {ex.Message}");
            }
        }

        #region Обработчики событий мыши и клавиатуры для выделения строк

        /// <summary>
        /// Обработка клика по строке в таблице товаров
        /// </summary>
        private void OnTovarRowPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is int dataRowIndex)
                {
                    SelectTovarRow(dataRowIndex);
                    _activeGrid = _gridTovar;
                    e.Handled = true;

                    // Устанавливаем фокус на ScrollViewer для обработки клавиатуры
                    _scrollViewerTovar?.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в обработчике клика строки товаров: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка клика по строке в таблице выполненных товаров
        /// </summary>
        private void OnTovarExecuteRowPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is int dataRowIndex)
                {
                    SelectTovarExecuteRow(dataRowIndex);
                    _activeGrid = _gridTovarExecute;
                    e.Handled = true;

                    // Устанавливаем фокус на ScrollViewer для обработки клавиатуры
                    _scrollViewerTovarExecute?.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в обработчике клика строки выполненных товаров: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка клика по таблице (для снятия выделения)
        /// </summary>
        private void OnTableGridPointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Если кликнули не по строке, снимаем выделение
            var source = e.Source as Control;
            if (source is Border border && border.Tag is int)
            {
                // Это строка, обработка будет в OnRowPointerPressed
                return;
            }

            ClearSelection();
            e.Handled = true;
        }

        /// <summary>
        /// Обработка клика по ScrollViewer таблицы товаров
        /// </summary>
        private void OnScrollViewerTovarPointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Устанавливаем фокус на ScrollViewer
            _scrollViewerTovar?.Focus();
            _activeGrid = _gridTovar;
        }

        /// <summary>
        /// Обработка клика по ScrollViewer таблицы выполненных товаров
        /// </summary>
        private void OnScrollViewerTovarExecutePointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Устанавливаем фокус на ScrollViewer
            _scrollViewerTovarExecute?.Focus();
            _activeGrid = _gridTovarExecute;
        }

        /// <summary>
        /// Выделение строки в таблице товаров
        /// </summary>
        private void SelectTovarRow(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= dt1.Rows.Count)
                {
                    ClearSelection();
                    return;
                }

                // Снимаем предыдущее выделение
                ClearSelection();

                // Устанавливаем новое выделение
                _selectedRowIndexTovar = rowIndex;

                // Находим Border строки (Grid.Row = rowIndex + 1, так как строка 0 - заголовки)
                int gridRowIndex = rowIndex + 1;

                foreach (Control child in _gridTovar.Children)
                {
                    if (child is Border border && Grid.GetRow(border) == gridRowIndex)
                    {
                        // Меняем стиль выделенной строки
                        border.Background = SELECTED_ROW_BACKGROUND;
                        border.BorderBrush = SELECTED_ROW_BORDER;
                        border.BorderThickness = new Thickness(2);

                        _selectedRowBorderTovar = border;
                        break;
                    }
                }

                // Прокручиваем к выделенной строке
                ScrollToRow(_scrollViewerTovar, _gridTovar, gridRowIndex);

                Console.WriteLine($"✓ Выделена строка товаров {rowIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при выделении строки товаров: {ex.Message}");
            }
        }

        /// <summary>
        /// Выделение строки в таблице выполненных товаров
        /// </summary>
        private void SelectTovarExecuteRow(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= dt2.Rows.Count)
                {
                    ClearSelection();
                    return;
                }

                // Снимаем предыдущее выделение
                ClearSelection();

                // Устанавливаем новое выделение
                _selectedRowIndexTovarExecute = rowIndex;

                // Находим Border строки (Grid.Row = rowIndex + 1, так как строка 0 - заголовки)
                int gridRowIndex = rowIndex + 1;

                foreach (Control child in _gridTovarExecute.Children)
                {
                    if (child is Border border && Grid.GetRow(border) == gridRowIndex)
                    {
                        // Меняем стиль выделенной строки
                        border.Background = SELECTED_ROW_BACKGROUND;
                        border.BorderBrush = SELECTED_ROW_BORDER;
                        border.BorderThickness = new Thickness(2);

                        _selectedRowBorderTovarExecute = border;
                        break;
                    }
                }

                // Прокручиваем к выделенной строке
                ScrollToRow(_scrollViewerTovarExecute, _gridTovarExecute, gridRowIndex);

                Console.WriteLine($"✓ Выделена строка выполненных товаров {rowIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при выделении строки выполненных товаров: {ex.Message}");
            }
        }

        /// <summary>
        /// Снятие выделения
        /// </summary>
        private void ClearSelection()
        {
            try
            {
                // Снимаем выделение с таблицы товаров
                if (_selectedRowBorderTovar != null)
                {
                    // Восстанавливаем оригинальный стиль строки
                    int dataRowIndex = (int)_selectedRowBorderTovar.Tag;
                    var originalBackground = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;

                    _selectedRowBorderTovar.Background = originalBackground;
                    _selectedRowBorderTovar.BorderBrush = Brushes.LightGray;
                    _selectedRowBorderTovar.BorderThickness = new Thickness(0, 0, 0, 1);

                    _selectedRowBorderTovar = null;
                }

                // Снимаем выделение с таблицы выполненных товаров
                if (_selectedRowBorderTovarExecute != null)
                {
                    // Восстанавливаем оригинальный стиль строки
                    int dataRowIndex = (int)_selectedRowBorderTovarExecute.Tag;
                    var originalBackground = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;

                    _selectedRowBorderTovarExecute.Background = originalBackground;
                    _selectedRowBorderTovarExecute.BorderBrush = Brushes.LightGray;
                    _selectedRowBorderTovarExecute.BorderThickness = new Thickness(0, 0, 0, 1);

                    _selectedRowBorderTovarExecute = null;
                }

                _selectedRowIndexTovar = -1;
                _selectedRowIndexTovarExecute = -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при снятии выделения: {ex.Message}");
            }
        }

        /// <summary>
        /// Прокрутка к указанной строке
        /// </summary>
        private void ScrollToRow(ScrollViewer scrollViewer, Grid grid, int gridRowIndex)
        {
            try
            {
                if (scrollViewer == null || grid == null) return;

                // Вычисляем позицию строки
                double rowPosition = 0;
                for (int i = 0; i < gridRowIndex; i++)
                {
                    if (i < grid.RowDefinitions.Count)
                    {
                        rowPosition += grid.RowDefinitions[i].Height.Value;
                    }
                }

                // Прокручиваем
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, rowPosition - 50); // -50 для небольшого отступа сверху
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при прокрутке: {ex.Message}");
            }
        }

        /// <summary>
        /// Перемещение выделения вверх в активной таблице
        /// </summary>
        private void MoveSelectionUp()
        {
            if (_activeGrid == _gridTovar)
            {
                if (dt1.Rows.Count == 0) return;

                int newIndex = _selectedRowIndexTovar - 1;
                if (newIndex < 0) newIndex = dt1.Rows.Count - 1; // Циклическое перемещение

                SelectTovarRow(newIndex);
            }
            else if (_activeGrid == _gridTovarExecute)
            {
                if (dt2.Rows.Count == 0) return;

                int newIndex = _selectedRowIndexTovarExecute - 1;
                if (newIndex < 0) newIndex = dt2.Rows.Count - 1; // Циклическое перемещение

                SelectTovarExecuteRow(newIndex);
            }
        }

        /// <summary>
        /// Перемещение выделения вниз в активной таблице
        /// </summary>
        private void MoveSelectionDown()
        {
            if (_activeGrid == _gridTovar)
            {
                if (dt1.Rows.Count == 0) return;

                int newIndex = _selectedRowIndexTovar + 1;
                if (newIndex >= dt1.Rows.Count) newIndex = 0; // Циклическое перемещение

                SelectTovarRow(newIndex);
            }
            else if (_activeGrid == _gridTovarExecute)
            {
                if (dt2.Rows.Count == 0) return;

                int newIndex = _selectedRowIndexTovarExecute + 1;
                if (newIndex >= dt2.Rows.Count) newIndex = 0; // Циклическое перемещение

                SelectTovarExecuteRow(newIndex);
            }
        }

        /// <summary>
        /// Перемещение выделения в начало активной таблицы
        /// </summary>
        private void MoveSelectionHome()
        {
            if (_activeGrid == _gridTovar)
            {
                if (dt1.Rows.Count > 0) SelectTovarRow(0);
            }
            else if (_activeGrid == _gridTovarExecute)
            {
                if (dt2.Rows.Count > 0) SelectTovarExecuteRow(0);
            }
        }

        /// <summary>
        /// Перемещение выделения в конец активной таблицы
        /// </summary>
        private void MoveSelectionEnd()
        {
            if (_activeGrid == _gridTovar)
            {
                if (dt1.Rows.Count > 0) SelectTovarRow(dt1.Rows.Count - 1);
            }
            else if (_activeGrid == _gridTovarExecute)
            {
                if (dt2.Rows.Count > 0) SelectTovarExecuteRow(dt2.Rows.Count - 1);
            }
        }

        /// <summary>
        /// Глобальная обработка клавиатуры
        /// </summary>
        private void OnGlobalKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"Key pressed: {e.Key}");

            // Проверяем, какая таблица активна
            bool isTovarTableFocused = _scrollViewerTovar?.IsFocused == true ||
                                      _gridTovar?.IsFocused == true;

            bool isTovarExecuteTableFocused = _scrollViewerTovarExecute?.IsFocused == true ||
                                             _gridTovarExecute?.IsFocused == true;

            if (!isTovarTableFocused && !isTovarExecuteTableFocused)
            {
                // Если ни одна таблица не в фокусе, проверяем поле ввода
                if (InputCodeOrBarcode?.IsFocused == true || ClientCode?.IsFocused == true)
                {
                    // Пропускаем обработку для полей ввода
                    return;
                }
            }

            // Устанавливаем активную таблицу
            if (isTovarTableFocused)
            {
                _activeGrid = _gridTovar;
            }
            else if (isTovarExecuteTableFocused)
            {
                _activeGrid = _gridTovarExecute;
            }

            // Обработка комбинаций с Ctrl
            if ((e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                if (e.Key == Key.Escape)
                {
                    this.Close();
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.F2)
                {
                    ClearAll();
                    e.Handled = true;
                    return;
                }
            }

            // Обработка клавиш без модификаторов
            switch (e.Key)
            {
                case Key.Up:
                    MoveSelectionUp();
                    e.Handled = true;
                    break;

                case Key.Down:
                    MoveSelectionDown();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    ClearSelection();
                    e.Handled = true;
                    break;

                case Key.Home:
                    MoveSelectionHome();
                    e.Handled = true;
                    break;

                case Key.End:
                    MoveSelectionEnd();
                    e.Handled = true;
                    break;

                case Key.F5:
                    ShowQueryWindowBarcode(1, 0, 0);
                    e.Handled = true;
                    break;

                case Key.Delete:
                    if (_activeGrid == _gridTovar && _selectedRowIndexTovar >= 0)
                    {
                        DeleteSelectedTovarRow();
                        e.Handled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Удаление выделенной строки из таблицы товаров
        /// </summary>
        private void DeleteSelectedTovarRow()
        {
            try
            {
                if (_selectedRowIndexTovar >= 0 && _selectedRowIndexTovar < dt1.Rows.Count)
                {
                    // Удаляем строку из DataTable
                    dt1.Rows.RemoveAt(_selectedRowIndexTovar);

                    // Перестраиваем таблицу
                    ClearTableTovar();
                    _currentRowTovar = 1;

                    // Перерисовываем все строки
                    for (int i = 0; i < dt1.Rows.Count; i++)
                    {
                        AddRowToTovarTable(dt1.Rows[i]);
                    }

                    // Если есть строки, выделяем следующую
                    if (dt1.Rows.Count > 0)
                    {
                        if (_selectedRowIndexTovar >= dt1.Rows.Count)
                        {
                            _selectedRowIndexTovar = dt1.Rows.Count - 1;
                        }
                        SelectTovarRow(_selectedRowIndexTovar);
                    }
                    else
                    {
                        ClearSelection();
                    }

                    // Пересчитываем акции
                    BtnCheckActionsClick();

                    Console.WriteLine($"✓ Удалена строка {_selectedRowIndexTovar}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при удалении строки: {ex.Message}");
            }
        }

        #endregion
    }   
}