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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class InputSertificates : Window
    {

       

        // Элементы управления
        private TextBox _inputSertificate;
        private Button _buttonCommit;
        private Border _gridContainer;
        private ScrollViewer _scrollViewer;
        private Grid _gridTable;
        private int _currentGridRow = 1;

        // Флаг для корректного закрытия окна
        private bool _closedNormally = false;

        // Для передачи данных в окно оплаты
        public Pay PayForm { get; set; }

        // Константы для стилей
        private static readonly IBrush HEADER_BACKGROUND = Brushes.LightBlue;
        private static readonly IBrush ROW_EVEN_BACKGROUND = Brushes.White;
        private static readonly IBrush ROW_ODD_BACKGROUND = Brushes.AliceBlue;
        private static readonly IBrush SELECTED_BACKGROUND = Brushes.LightSkyBlue;
        private static readonly IBrush SELECTED_BORDER = Brushes.DodgerBlue;

        // Выделение строк
        private Border _selectedRowBorder;
        private int _selectedRowIndex = -1;

        // Коллекция данных сертификатов
        private List<CertificateItem> _certificates = new List<CertificateItem>();

        // DataTable для совместимости
        private DataTable _sertificatesData;

        // Модель данных для сертификата
        public class CertificateItem
        {
            public int Number { get; set; }
            public string Code { get; set; }              // Код товара (tovar_code)
            public string Name { get; set; }              // Наименование (tovar_name)
            public decimal Amount { get; set; }           // Номинал (retail_price)
            public string Barcode { get; set; }           // Штрихкод
            public string Status { get; set; }            // Статус

            // Для клонирования (аналог Clone() в ListViewItem)
            public CertificateItem Clone()
            {
                return new CertificateItem
                {
                    Number = this.Number,
                    Code = this.Code,
                    Name = this.Name,
                    Amount = this.Amount,
                    Barcode = this.Barcode,
                    Status = this.Status
                };
            }
        }

        public InputSertificates()
        {
            InitializeComponent();

            // Находим элементы управления из XAML
            FindControls();

            // Создаем Grid программно
            CreateGridProgrammatically();

            // Инициализируем обработчики событий
            InitializeEventHandlers();

            // Инициализируем DataTable для совместимости
            InitializeDataTable();

            // Устанавливаем фокус на поле ввода
            this.AttachedToVisualTree += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _inputSertificate?.Focus();
                }, DispatcherPriority.Input);
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void FindControls()
        {
            try
            {
                _inputSertificate = this.FindControl<TextBox>("input_sertificate");
                _buttonCommit = this.FindControl<Button>("button_commit");
                _gridContainer = this.FindControl<Border>("gridContainer");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске контролов: {ex.Message}");
            }
        }

        private void CreateGridProgrammatically()
        {
            try
            {
                // Создаем ScrollViewer
                _scrollViewer = new ScrollViewer
                {
                    Name = "scrollViewer_sertificates",
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White,
                    Focusable = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // Создаем Grid для таблицы
                _gridTable = new Grid
                {
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // Определяем колонки (5 колонок как в старом ListView)
                var columnDefinitions = new[]
                {
                    new ColumnDefinition(new GridLength(80, GridUnitType.Pixel)),     // №
                    new ColumnDefinition(new GridLength(100, GridUnitType.Pixel)),    // Код товара
                    new ColumnDefinition(new GridLength(2, GridUnitType.Star)),       // Наименование
                    new ColumnDefinition(new GridLength(120, GridUnitType.Pixel)),    // Сумма
                    new ColumnDefinition(new GridLength(150, GridUnitType.Pixel))     // Штрихкод
                };

                foreach (var colDef in columnDefinitions)
                {
                    _gridTable.ColumnDefinitions.Add(colDef);
                }

                // Создаем строку заголовков (строка 0)
                _gridTable.RowDefinitions.Add(new RowDefinition(35, GridUnitType.Pixel));
                CreateHeaderRow();

                // Добавляем Grid в ScrollViewer
                _scrollViewer.Content = _gridTable;

                // Добавляем ScrollViewer в контейнер
                _gridContainer.Child = _scrollViewer;

                Console.WriteLine("✓ Grid для сертификатов создан программно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании Grid: {ex.Message}");
            }
        }

        public void LoadExistingCertificates(List<CertificateItem> existingCertificates)
        {
            if (existingCertificates == null || existingCertificates.Count == 0)
                return;

            // Очищаем текущие данные
            _certificates.Clear();
            _sertificatesData?.Clear();

            // Добавляем существующие сертификаты
            foreach (var cert in existingCertificates)
            {
                var certificate = cert.Clone();
                certificate.Number = _certificates.Count + 1;
                _certificates.Add(certificate);

                // Добавляем в DataTable
                DataRow newRow = _sertificatesData.NewRow();
                newRow["Number"] = certificate.Number;
                newRow["Code"] = certificate.Code;
                newRow["Name"] = certificate.Name;
                newRow["Amount"] = certificate.Amount;
                newRow["Barcode"] = certificate.Barcode;
                newRow["Status"] = certificate.Status;
                _sertificatesData.Rows.Add(newRow);

                // Добавляем строку в Grid
                AddRowToGrid(certificate);
            }

            // Выделяем последнюю строку
            if (_certificates.Count > 0)
            {
                SelectRow(_certificates.Count - 1);
            }
        }

        private void CreateHeaderRow()
        {
            try
            {
                string[] headers = { "№", "Код товара", "Наименование", "Номинал", "Штрихкод" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var headerBorder = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 1, 2),
                        Background = HEADER_BACKGROUND,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Child = new TextBlock
                        {
                            Text = headers[i],
                            FontWeight = FontWeight.Bold,
                            FontSize = 12,
                            Margin = new Thickness(5, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.DarkBlue
                        }
                    };

                    Grid.SetColumn(headerBorder, i);
                    Grid.SetRow(headerBorder, 0);
                    _gridTable.Children.Add(headerBorder);
                }

                Console.WriteLine($"✓ Заголовки созданы: {headers.Length} колонок");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании заголовков: {ex.Message}");
            }
        }

        private void InitializeDataTable()
        {
            _sertificatesData = new DataTable("Sertificates");
            _sertificatesData.Columns.Add("Number", typeof(int));
            _sertificatesData.Columns.Add("Code", typeof(string));
            _sertificatesData.Columns.Add("Name", typeof(string));
            _sertificatesData.Columns.Add("Amount", typeof(decimal));
            _sertificatesData.Columns.Add("Barcode", typeof(string));
            _sertificatesData.Columns.Add("Status", typeof(string));
        }

        private void InitializeEventHandlers()
        {
            // Обработка нажатия кнопки
            if (_buttonCommit != null)
            {
                _buttonCommit.Click += ButtonCommit_Click;
            }

            // Обработка клавиатуры для всей формы
            this.AddHandler(KeyDownEvent, InputSertificates_KeyDown, RoutingStrategies.Tunnel);

            // Обработка Enter в поле ввода
            if (_inputSertificate != null)
            {
                _inputSertificate.KeyDown += InputSertificate_KeyDown;
            }

            // Подписываемся на клики по Grid
            if (_gridTable != null)
            {
                _gridTable.PointerPressed += GridTable_PointerPressed;
            }

            // Обработка двойного клика для удаления
            if (_scrollViewer != null)
            {
                _scrollViewer.DoubleTapped += ScrollViewer_DoubleTapped;
            }
        }

        // АДАПТИРОВАННЫЙ МЕТОД ДОБАВЛЕНИЯ СЕРТИФИКАТА
        private async void FindSertificateOnCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                await MessageBox.Show("Введите код сертификата", "Ошибка",
                    MessageBoxButton.OK, MessageBoxType.Error);
                return;
            }

            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                await conn.OpenAsync();

                string query = "";
                if (code.Length > 6)
                {
                    query = @"SELECT tovar.code AS tovar_code, 
                                     tovar.name AS tovar_name, 
                                     tovar.retail_price AS retail_price 
                              FROM barcode 
                              LEFT JOIN tovar ON barcode.tovar_code = tovar.code 
                              WHERE barcode.barcode = @code 
                              AND tovar.its_deleted = 0  
                              AND tovar.retail_price <> 0 
                              AND tovar.its_certificate = 1";
                }
                else
                {
                    query = @"SELECT tovar.code AS tovar_code, 
                                     tovar.name AS tovar_name, 
                                     tovar.retail_price AS retail_price 
                              FROM tovar 
                              WHERE tovar.its_deleted = 0 
                              AND tovar.retail_price <> 0 
                              AND tovar.code = @code 
                              AND tovar.its_certificate = 1";
                }

                // Очищаем поле ввода сразу, как в старом коде
                _inputSertificate.Text = "";

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                command.Parameters.AddWithValue("@code", code);

                NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                bool have = false;

                while (await reader.ReadAsync())
                {
                    have = true;

                    // Проверка есть ли такой уже сертификат (по штрихкоду)
                    string tovarCode = reader["tovar_code"].ToString();
                    string tovarName = reader["tovar_name"].ToString();
                    decimal retailPrice = Convert.ToDecimal(reader["retail_price"]);

                    bool exist = _certificates.Any(c => c.Barcode == code);

                    if (exist)
                    {
                        await MessageBox.Show($"Сертификат с номером {code} уже выбран в строках",
                            "Предупреждение",
                            MessageBoxButton.OK,
                            MessageBoxType.Warning);
                        break;
                    }

                    // Добавляем сертификат
                    var certificate = new CertificateItem
                    {
                        Number = _certificates.Count + 1,
                        Code = tovarCode,
                        Name = tovarName,
                        Amount = retailPrice,
                        Barcode = code,
                        Status = "Активен"
                    };

                    // Добавляем в коллекцию
                    _certificates.Add(certificate);

                    // Добавляем в DataTable для совместимости
                    DataRow newRow = _sertificatesData.NewRow();
                    newRow["Number"] = certificate.Number;
                    newRow["Code"] = certificate.Code;
                    newRow["Name"] = certificate.Name;
                    newRow["Amount"] = certificate.Amount;
                    newRow["Barcode"] = certificate.Barcode;
                    newRow["Status"] = certificate.Status;
                    _sertificatesData.Rows.Add(newRow);

                    // Добавляем строку в Grid
                    AddRowToGrid(certificate);

                    // Выделяем добавленную строку
                    SelectRow(_certificates.Count - 1);

                    Console.WriteLine($"✓ Сертификат добавлен: код={tovarCode}, штрихкод={code}, номинал={retailPrice}");
                }

                reader.Close();
                command.Dispose();

                if (!have)
                {
                    await MessageBox.Show($"Сертификат с номером {code} не найден",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxType.Error);
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show($"Ошибка базы данных: {ex.Message}",
                    "Ошибка БД",
                    MessageBoxButton.OK,
                    MessageBoxType.Error);
                Console.WriteLine($"Npgsql ошибка: {ex.Message}");
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxType.Error);
                Console.WriteLine($"Общая ошибка: {ex.Message}");
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
                }

                // Возвращаем фокус на поле ввода
                Dispatcher.UIThread.Post(() =>
                {
                    _inputSertificate?.Focus();
                }, DispatcherPriority.Background);
            }
        }

        private void AddRowToGrid(CertificateItem certificate)
        {
            try
            {
                // Добавляем строку в Grid
                int gridRowIndex = _currentGridRow;
                _gridTable.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));

                // Создаем фон строки
                var rowBackground = (gridRowIndex % 2 == 0) ? ROW_EVEN_BACKGROUND : ROW_ODD_BACKGROUND;

                var rowBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = rowBackground,
                    Tag = _certificates.Count - 1, // Сохраняем индекс для выделения
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // Подписываемся на события клика
                rowBorder.PointerPressed += (s, e) =>
                {
                    if (rowBorder.Tag is int index)
                    {
                        SelectRow(index);
                        e.Handled = true;
                    }
                };

                Grid.SetColumnSpan(rowBorder, 5);
                Grid.SetRow(rowBorder, gridRowIndex);
                _gridTable.Children.Add(rowBorder);

                // Добавляем ячейки с данными (5 колонок)
                AddCell(_gridTable, 0, gridRowIndex, certificate.Number.ToString(), HorizontalAlignment.Center);
                AddCell(_gridTable, 1, gridRowIndex, certificate.Code, HorizontalAlignment.Left);
                AddCell(_gridTable, 2, gridRowIndex, certificate.Name, HorizontalAlignment.Left);
                AddCell(_gridTable, 3, gridRowIndex, certificate.Amount.ToString("N2"), HorizontalAlignment.Right);
                AddCell(_gridTable, 4, gridRowIndex, certificate.Barcode, HorizontalAlignment.Left);

                // Увеличиваем счетчик строк
                _currentGridRow++;

                // Прокручиваем к добавленной строке
                ScrollToRow(gridRowIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении строки в Grid: {ex.Message}");
            }
        }

        private void AddCell(Grid grid, int column, int row, string text, HorizontalAlignment alignment)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 12,
                FontWeight = FontWeight.Normal,
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                IsHitTestVisible = false
            };

            Grid.SetColumn(textBlock, column);
            Grid.SetRow(textBlock, row);
            grid.Children.Add(textBlock);
        }

        private void InputSertificate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindSertificateOnCode(_inputSertificate.Text);
                e.Handled = true;
            }
        }

        private void InputSertificates_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F7:
                    _inputSertificate?.Focus();
                    e.Handled = true;
                    break;

                case Key.F12:
                    if (_buttonCommit.IsEnabled)
                    {
                        ButtonCommit_Click(null, null);
                        e.Handled = true;
                    }
                    break;

                case Key.F5:
                    // button_cancel_Click(null, null); // если нужна кнопка отмены
                    e.Handled = true;
                    break;

                //case Key.Escape:
                //    this.Close();
                //    e.Handled = true;
                //    break;

                case Key.Delete:
                    if (_selectedRowIndex >= 0)
                    {
                        RemoveSelectedSertificate();
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    MoveSelectionUp();
                    e.Handled = true;
                    break;

                case Key.Down:
                    MoveSelectionDown();
                    e.Handled = true;
                    break;

                case Key.Home:
                    if (_certificates.Count > 0)
                        SelectRow(0);
                    e.Handled = true;
                    break;

                case Key.End:
                    if (_certificates.Count > 0)
                        SelectRow(_certificates.Count - 1);
                    e.Handled = true;
                    break;
            }
        }

        private async void ButtonCommit_Click(object sender, RoutedEventArgs e)
        {
            await CommitSertificates();
        }

        /// <summary>
        /// Проверить все сертификаты на то, что они активированы
        /// Аналог button_commit_Click из WinForms
        /// </summary>
        private async Task CommitSertificates()
        {
            if (_certificates.Count > 0)
            {
                bool resultCheck = true;

                // Проверяем все сертификаты на активность
                foreach (var certificate in _certificates)
                {
                    if (!await CheckSertificateActive(certificate.Barcode))
                    {
                        resultCheck = false;
                        break;
                    }
                }

                if (!resultCheck)
                {
                    return;
                }
            }

            _closedNormally = true;

            // Вычисляем общую сумму сертификатов (даже если 0)
            decimal totalAmount = _certificates.Sum(c => c.Amount);

            // Устанавливаем результат через Tag для передачи в вызывающую форму
            this.Tag = new
            {
                Success = true,
                Sertificates = _certificates.Select(c => c.Clone()).ToList(), // Клонируем
                TotalAmount = totalAmount, // Добавляем общую сумму
                DataTable = _sertificatesData
            };

            this.Close();
        }

        /// <summary>
        /// Проверка активности сертификата с таймаутом
        /// Аналог check_sertificate_active из WinForms
        /// </summary>
        /// <param name="sertificateCode">Штрихкод сертификата</param>
        /// <returns>True если сертификат активен</returns>
        private async Task<bool> CheckSertificateActive(string sertificateCode)
        {
            bool result = true;
            var cts = new CancellationTokenSource();

            try
            {
                // Запускаем проверку с таймаутом
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                var checkTask = CheckSertificateActiveAsync(sertificateCode, cts.Token);

                // Ожидаем завершения первой задачи
                var completedTask = await Task.WhenAny(checkTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Таймаут
                    MainStaticClass.write_event_in_log($"Таймаут при проверке сертификата {sertificateCode}",
                        "Документ чек", "0");

                    await MessageBox.Show("Внешний таймаут при проверке активности сертификата",
                        "Проверка сертификата",
                        MessageBoxButton.OK,
                        MessageBoxType.Error);

                    cts.Cancel();
                    result = false;
                }
                else
                {
                    // Проверка завершена успешно
                    result = await checkTask;

                    if (result)
                    {
                        MainStaticClass.write_event_in_log($"Успешная проверка сертификата {sertificateCode}",
                            "Документ чек", "0");
                    }
                    else
                    {
                        MainStaticClass.write_event_in_log($"Сертификат {sertificateCode} не активен",
                            "Документ чек", "0");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Задача была отменена
                result = false;
                MainStaticClass.write_event_in_log($"Проверка сертификата {sertificateCode} отменена",
                    "Документ чек", "0");
            }
            catch (Exception ex)
            {
                // Обработка других исключений
                result = false;
                MainStaticClass.write_event_in_log($"Ошибка при проверке сертификата {sertificateCode}: {ex.Message}",
                    "Документ чек", "0");

                await MessageBox.Show($"Ошибка при проверке сертификата: {ex.Message}",
                    "Ошибка проверки",
                    MessageBoxButton.OK,
                    MessageBoxType.Error);
            }
            finally
            {
                cts.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Асинхронная проверка активности сертификата
        /// Аналог check_sertificate_active1 из WinForms
        /// </summary>
        private async Task<bool> CheckSertificateActiveAsync(string sertificateCode, CancellationToken cancellationToken)
        {
            try
            {
                // Проверка на отмену
                cancellationToken.ThrowIfCancellationRequested();

                // Получаем параметры для запроса
                string nickShop = MainStaticClass.Nick_Shop?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(nickShop))
                {
                    await MessageBox.Show("Не удалось получить название магазина",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxType.Error);
                    return false;
                }

                string codeShop = MainStaticClass.Code_Shop?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(codeShop))
                {
                    await MessageBox.Show("Не удалось получить код магазина",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxType.Error);
                    return false;
                }

                string countDay = CryptorEngine.get_count_day();
                string key = nickShop.Trim() + countDay.Trim() + codeShop.Trim();

                string encryptData = CryptorEngine.Encrypt(sertificateCode, true, key);

                // Используем веб-сервис для проверки
                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 60000;

                string status;
                try
                {
                    // Асинхронный вызов веб-сервиса
                    status = await Task.Run(() =>
                        ds.GetStatusSertificat(MainStaticClass.Nick_Shop, encryptData, MainStaticClass.GetWorkSchema.ToString()),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    await MessageBox.Show($"Отсутствует доступ в интернет или ошибка на сервере: {ex.Message}",
                        "Проверка сертификата",
                        MessageBoxButton.OK,
                        MessageBoxType.Error);
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber,
                        "Проверка активации сертификата");
                    return false;
                }

                if (status == "-1")
                {
                    await MessageBox.Show("Произошли ошибки на сервере при работе с сертификатами",
                        "Проверка сертификата",
                        MessageBoxButton.OK,
                        MessageBoxType.Error);
                    MainStaticClass.WriteRecordErrorLog("Ошибки на сервере при работе с сертификатами",
                        "CheckSertificateActiveAsync", 0, MainStaticClass.CashDeskNumber,
                        "Проверка активации сертификата");
                    return false;
                }
                else
                {
                    string decryptData = CryptorEngine.Decrypt(status, true, key);

                    // 1 - уже активирован, 0 - не активирован
                    if (decryptData == "1")
                    {
                        await MessageBox.Show($"Сертификат {sertificateCode} уже активирован",
                            "Проверка сертификата",
                            MessageBoxButton.OK,
                            MessageBoxType.Error);
                        return false;
                    }
                    else if (decryptData == "0")
                    {
                        return true; // Сертификат не активирован, можно использовать
                    }
                    else
                    {
                        await MessageBox.Show($"Неизвестный статус сертификата: {decryptData}",
                            "Проверка сертификата",
                            MessageBoxButton.OK,
                            MessageBoxType.Error);
                        return false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Пробрасываем дальше
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber,
                    "Проверка активации сертификата");
                throw;
            }
        }

        private void SelectRow(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _certificates.Count)
                {
                    ClearSelection();
                    return;
                }

                // Снимаем предыдущее выделение
                ClearSelection();

                // Устанавливаем новое выделение
                _selectedRowIndex = rowIndex;

                // Находим Border строки (Grid.Row = rowIndex + 1, так как строка 0 - заголовки)
                int gridRowIndex = rowIndex + 1;

                foreach (Control child in _gridTable.Children)
                {
                    if (child is Border border && Grid.GetRow(border) == gridRowIndex)
                    {
                        // Меняем стиль выделенной строки
                        border.Background = SELECTED_BACKGROUND;
                        border.BorderBrush = SELECTED_BORDER;
                        border.BorderThickness = new Thickness(2);

                        _selectedRowBorder = border;
                        break;
                    }
                }

                // Прокручиваем к выделенной строке
                ScrollToRow(gridRowIndex);

                // Устанавливаем фокус на ScrollViewer
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _scrollViewer?.Focus();
                }, DispatcherPriority.Background);

                Console.WriteLine($"✓ Выделена строка сертификатов {rowIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при выделении строки: {ex.Message}");
            }
        }

        private void ClearSelection()
        {
            try
            {
                if (_selectedRowBorder != null)
                {
                    // Восстанавливаем оригинальный стиль строки
                    int dataRowIndex = _selectedRowIndex;
                    var originalBackground = (dataRowIndex % 2 == 0) ? ROW_EVEN_BACKGROUND : ROW_ODD_BACKGROUND;

                    _selectedRowBorder.Background = originalBackground;
                    _selectedRowBorder.BorderBrush = Brushes.LightGray;
                    _selectedRowBorder.BorderThickness = new Thickness(0, 0, 0, 1);

                    _selectedRowBorder = null;
                }

                _selectedRowIndex = -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при снятии выделения: {ex.Message}");
            }
        }

        private void ScrollToRow(int gridRowIndex)
        {
            try
            {
                if (_scrollViewer == null) return;

                // Вычисляем примерную позицию строки
                double rowHeight = 30;
                double rowTopPosition = (gridRowIndex - 1) * rowHeight;
                double rowBottomPosition = rowTopPosition + rowHeight;

                // Получаем текущие видимые границы
                double viewportTop = _scrollViewer.Offset.Y;
                double viewportHeight = _scrollViewer.Viewport.Height;
                double viewportBottom = viewportTop + viewportHeight;

                // Проверяем, видна ли строка
                bool isRowVisible = rowTopPosition >= viewportTop && rowBottomPosition <= viewportBottom;

                if (!isRowVisible)
                {
                    // Прокручиваем так, чтобы строка была в центре видимой области
                    double targetOffset = rowTopPosition - (viewportHeight / 2) + (rowHeight / 2);

                    // Ограничиваем минимальное и максимальное смещение
                    double maxOffset = Math.Max(0, _scrollViewer.Extent.Height - viewportHeight);
                    targetOffset = Math.Max(0, Math.Min(targetOffset, maxOffset));

                    _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetOffset);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при прокрутке: {ex.Message}");
            }
        }

        private void MoveSelectionUp()
        {
            if (_certificates.Count == 0) return;

            int newIndex = _selectedRowIndex - 1;
            if (newIndex < 0) newIndex = 0;

            SelectRow(newIndex);
        }

        private void MoveSelectionDown()
        {
            if (_certificates.Count == 0) return;

            int newIndex = _selectedRowIndex + 1;
            if (newIndex >= _certificates.Count) newIndex = _certificates.Count - 1;

            SelectRow(newIndex);
        }

        private void RemoveSelectedSertificate()
        {
            try
            {
                if (_selectedRowIndex >= 0 && _selectedRowIndex < _certificates.Count)
                {
                    // Удаляем из коллекции
                    _certificates.RemoveAt(_selectedRowIndex);

                    // Удаляем из DataTable
                    _sertificatesData.Rows[_selectedRowIndex].Delete();

                    // Обновляем Grid
                    RefreshGrid();

                    // Обновляем номера строк
                    for (int i = 0; i < _certificates.Count; i++)
                    {
                        _certificates[i].Number = i + 1;
                    }

                    // Выделяем следующую строку или снимаем выделение
                    if (_certificates.Count > 0)
                    {
                        if (_selectedRowIndex >= _certificates.Count)
                            _selectedRowIndex = _certificates.Count - 1;
                        SelectRow(_selectedRowIndex);
                    }
                    else
                    {
                        ClearSelection();
                    }

                    // Устанавливаем фокус
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _inputSertificate?.Focus();
                    }, DispatcherPriority.Background);

                    Console.WriteLine($"✓ Сертификат удален из позиции {_selectedRowIndex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при удалении сертификата: {ex.Message}");
            }
        }

        private void RefreshGrid()
        {
            try
            {
                Console.WriteLine("Обновление Grid сертификатов...");

                // Очищаем старые строки данных (кроме заголовков)
                while (_gridTable.RowDefinitions.Count > 1)
                {
                    _gridTable.RowDefinitions.RemoveAt(_gridTable.RowDefinitions.Count - 1);
                }

                // Удаляем все элементы кроме заголовков
                var elementsToRemove = new List<Control>();
                foreach (Control child in _gridTable.Children)
                {
                    if (Grid.GetRow(child) > 0) // Все что ниже строки 0 (заголовки)
                    {
                        elementsToRemove.Add(child);
                    }
                }

                foreach (var element in elementsToRemove)
                {
                    _gridTable.Children.Remove(element);
                }

                // Сбрасываем счетчик строк
                _currentGridRow = 1;

                // Добавляем обновленные данные
                foreach (var certificate in _certificates)
                {
                    AddRowToGrid(certificate);
                }

                Console.WriteLine($"✓ Grid сертификатов обновлен. Записей: {_certificates.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при обновлении Grid: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        // Переопределяем закрытие окна для корректной работы с флагом
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Если окно закрыто не через кнопку подтверждения,
            // устанавливаем Tag в null или false
            if (!_closedNormally)
            {
                this.Tag = null;
            }
        }

        private void GridTable_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var source = e.Source as Control;
                if (source == null) return;

                // Ищем родительский Border строки
                var border = FindParentBorder(source);
                if (border != null && border.Tag is int rowIndex)
                {
                    SelectRow(rowIndex);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при клике по Grid: {ex.Message}");
            }
        }

        private void ScrollViewer_DoubleTapped(object sender, RoutedEventArgs e)
        {
            RemoveSelectedSertificate();
        }

        private Border FindParentBorder(Control control)
        {
            while (control != null)
            {
                if (control is Border border && border.Tag is int)
                    return border;
                control = control.Parent as Control;
            }
            return null;
        }

        // Методы для получения данных (совместимость со старым кодом)
        public DataTable GetSertificatesData()
        {
            return _sertificatesData;
        }

        public List<string> GetSertificateCodes()
        {
            return _certificates.Select(c => c.Code).ToList();
        }

        public List<string> GetSertificateBarcodes()
        {
            return _certificates.Select(c => c.Barcode).ToList();
        }

        public decimal GetTotalAmount()
        {
            return _certificates.Sum(c => c.Amount);
        }


        // Метод для получения списка объектов сертификатов
        public List<CertificateItem> GetCertificates()
        {
            return new List<CertificateItem>(_certificates);
        }
    }
}