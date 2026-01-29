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

                // ПРИНУДИТЕЛЬНО УСТАНАВЛИВАЕМ РАЗМЕРЫ
                Dispatcher.UIThread.Post(() =>
                {
                    _gridTable.MinHeight = 200;
                    _gridTable.MinWidth = 600;
                }, DispatcherPriority.Background);

                Console.WriteLine("✓ Grid для сертификатов создан программно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании Grid: {ex.Message}");
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
                        CommitInput();
                        e.Handled = true;
                    }
                    break;

                case Key.F5:
                    // button_cancel_Click(null, null); // если нужна кнопка отмены
                    e.Handled = true;
                    break;

                case Key.Escape:
                    this.Close();
                    e.Handled = true;
                    break;

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

        // Остальные методы остаются без изменений (SelectRow, ClearSelection, ScrollToRow, MoveSelectionUp/Down и т.д.)

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

        private async void CommitInput()
        {
            if (_certificates.Count == 0)
            {
                await MessageBox.Show("Нет добавленных сертификатов", "Информация",
                    MessageBoxButton.OK, MessageBoxType.Info);
                return;
            }

            // Вычисляем общую сумму
            decimal totalAmount = _certificates.Sum(c => c.Amount);

            // Устанавливаем результат через Tag
            this.Tag = _sertificatesData;

            // Закрываем окно
            this.Close();
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

        private void ButtonCommit_Click(object sender, RoutedEventArgs e)
        {
            CommitInput();
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
    }
}