using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public class CheckItem
    {
        public decimal ItsDeleted { get; set; }
        public DateTime DateTimeWrite { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public decimal Cash { get; set; }
        public decimal Remainder { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string CheckType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public bool ItsPrint { get; set; }
        public bool ItsPrintP { get; set; }
    }

    public partial class Cash_checks : UserControl
    {
        // СОБЫТИЕ ДЛЯ ЗАКРЫТИЯ
        public event EventHandler RequestClose;

        // Элементы управления таблицей
        private ScrollViewer _scrollViewer;
        private Grid _tableGrid;
        private List<CheckItem> _checkItems = new List<CheckItem>();
        private int _currentRow = 1; // 0 - заголовки
        private int _selectedRowIndex = -1;
        private Border _selectedRowBorder;

        // ДЛЯ ТАЙМЕРА И СТАТУСА
        private System.Timers.Timer _statusTimer;
        private DateTime _timerExecute = DateTime.Now;
        private TextBox _txtStatusBox; // Ссылка на TextBox со статусом

        // Константы для цветов
        private static readonly IBrush SELECTED_ROW_BACKGROUND = Brushes.LightSkyBlue;
        private static readonly IBrush SELECTED_ROW_BORDER = Brushes.DodgerBlue;
        private static readonly IBrush EVEN_ROW_BACKGROUND = Brushes.White;
        private static readonly IBrush ODD_ROW_BACKGROUND = Brushes.AliceBlue;

        private bool newDocument = false;

        private DateTime _currentDate = DateTime.Today;
        private TextBox _txtSelectedDate = null;

        public Cash_checks()
        {
            Console.WriteLine("=== Конструктор Cash_checks начат ===");

            try
            {
                // 1. Загружаем XAML
                InitializeComponent();
                Console.WriteLine("✓ XAML загружен");

                // 2. Создаем таблицу из кода
                CreateTableFromCode();

                // 3. Подписываемся на события
                InitializeEvents();
                InitializeCloseButton();

                // 4. Подписываемся на глобальные события клавиатуры
                this.AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);

                // 5. ИНИЦИАЛИЗИРУЕМ ТАЙМЕР ДЛЯ СТАТУСА
                InitializeStatusTimer();

                // 6. ✅ ИНИЦИАЛИЗАЦИЯ ЗНАЧЕНИЙ И ЗАГРУЗКА ДАННЫХ СРАЗУ
                InitializeAndLoadAsync();

                UpdateDateDisplay();

                Console.WriteLine("✓ Конструктор завершен успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ОШИБКА в конструкторе: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("=== Конструктор Cash_checks завершен ===");
        }

        private void UpdateDateDisplay()
        {
            _txtSelectedDate = this.FindControl<TextBox>("txtSelectedDate");
            _txtSelectedDate.Text = _currentDate.ToString("dd.MM.yyyy");
        }

        private async void BtnSelectDate_Click(object sender, RoutedEventArgs e)
        {
            // Создаем простой DatePicker для выбора даты
            var datePicker = new DatePicker();
            datePicker.SelectedDate = new DateTimeOffset(_currentDate);

            // Кнопка OK
            var okButton = new Button
            {
                Content = "OK",
                Width = 80
            };

            // Кнопка Отмена
            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 80
            };

            // Панель для кнопок
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            // Основная панель
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20,
                Children =
                {
                    datePicker,
                    buttonPanel
                }
            };

            // Окно диалога
            var dialog = new Window
            {
                Title = "Выберите дату",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = mainPanel,
                CanResize=false
            };

            // Подписываемся на события кнопок
            okButton.Click += (s, args) =>
            {
                if (datePicker.SelectedDate.HasValue)
                {
                    // Конвертируем DateTimeOffset в DateTime
                    _currentDate = datePicker.SelectedDate.Value.DateTime;
                    UpdateDateDisplay();
                }
                dialog.Close();
            };

            cancelButton.Click += (s, args) =>
            {
                dialog.Close();
            };

            // Показываем диалог
            var parentWindow = this.VisualRoot as Window;
            if (parentWindow != null)
            {
                await dialog.ShowDialog(parentWindow);
            }
        }


        // Получить выбранную дату
        public DateTime GetSelectedDate()
        {
            return _currentDate;
        }

        // Получить дату в строковом формате
        public string GetFormattedDate()
        {
            return _currentDate.ToString("dd.MM.yyyy");
        }

        // Установить дату
        public void SetDate(DateTime date)
        {
            _currentDate = date;
            UpdateDateDisplay();
        }


        /// <summary>
        /// ✅ Асинхронная инициализация и загрузка данных
        /// </summary>
        private async void InitializeAndLoadAsync()
        {
            try
            {
                // Инициализируем контролы сразу в UI потоке
                InitializeControls();

                Console.WriteLine("✓ Контролы инициализированы");

                // ЗАГРУЖАЕМ ДАННЫЕ АСИНХРОННО, НЕ БЛОКИРУЯ ПОКАЗ ФОРМЫ
                await LoadDocumentsAsync();

                Console.WriteLine("✓ Данные загружены асинхронно");

                // Первоначальное обновление статуса в фоне
                _ = Task.Run(() => GetStatusSendDocument());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в InitializeAndLoadAsync: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Инициализация кнопки закрытия
        /// </summary>
        private void InitializeCloseButton()
        {
            try
            {
                // Находим кнопку закрытия
                var closeButton = this.FindControl<Button>("btnClose");
                if (closeButton != null)
                {
                    Console.WriteLine("✓ Кнопка закрытия найдена и инициализирована");

                    // Добавляем эффект при наведении
                    closeButton.PointerEntered += (s, e) =>
                    {
                        closeButton.Foreground = Brushes.Red;
                    };

                    closeButton.PointerExited += (s, e) =>
                    {
                        closeButton.Foreground = Brushes.Gray;
                    };
                }
                else
                {
                    Console.WriteLine("⚠ Кнопка закрытия не найдена");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при инициализации кнопки закрытия: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализация значений контролов
        /// </summary>
        private void InitializeControls()
        {
            try
            {
                var numCash = this.FindControl<TextBlock>("num_cash");
                if (numCash != null)
                {
                    numCash.Text = "КАССА № " + MainStaticClass.CashDeskNumber.ToString();
                    Console.WriteLine($"Номер кассы установлен: {numCash.Text}");
                }

                var txtCashier = this.FindControl<TextBox>("txtB_cashier");
                if (txtCashier != null)
                {
                    txtCashier.Text = MainStaticClass.Cash_Operator;
                    Console.WriteLine($"Кассир установлен: {txtCashier.Text}");
                }

                var datePicker = this.FindControl<DatePicker>("dateTimePicker1");
                if (datePicker != null)
                {
                    datePicker.SelectedDate = DateTime.Today;
                    Console.WriteLine($"Дата установлена: {DateTime.Today:dd.MM.yyyy}");
                }

                // ИНИЦИАЛИЗАЦИЯ TextBox СО СТАТУСОМ
                _txtStatusBox = this.FindControl<TextBox>("txtB_not_unloaded_docs");
                if (_txtStatusBox != null)
                {
                    _txtStatusBox.Text = "Загрузка...";
                    Console.WriteLine("✓ TextBox со статусом найден");
                }
                else
                {
                    Console.WriteLine("⚠ TextBox 'txtB_not_unloaded_docs' не найден!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при инициализации контролов: {ex.Message}");
            }
        }

        /// <summary>
        /// Создание таблицы полностью из кода
        /// </summary>
        private void CreateTableFromCode()
        {
            try
            {
                Console.WriteLine("Создание таблицы из кода...");

                // Находим Border для таблицы
                var tableBorder = this.FindControl<Border>("TableBorder");
                if (tableBorder == null)
                {
                    Console.WriteLine("✗ TableBorder не найден!");
                    return;
                }

                // Создаем ScrollViewer
                _scrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White,
                    Focusable = true
                };

                // Подписываемся на события ScrollViewer
                _scrollViewer.PointerPressed += OnScrollViewerPointerPressed;

                // Создаем Grid для таблицы
                _tableGrid = new Grid
                {
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // Подписываемся на события Grid
                _tableGrid.PointerPressed += OnTableGridPointerPressed;

                // Добавляем колонки с пропорциональными ширинами
                var columnDefinitions = new[]
                {
                    new ColumnDefinition(0.8, GridUnitType.Star),   // Статус
                    new ColumnDefinition(0.8, GridUnitType.Star),   // Дата
                    new ColumnDefinition(2.2, GridUnitType.Star),   // Клиент
                    new ColumnDefinition(0.8, GridUnitType.Star),   // Сумма
                    new ColumnDefinition(0.8, GridUnitType.Star),   // Сдача
                    new ColumnDefinition(2.2, GridUnitType.Star),   // Комментарий
                    new ColumnDefinition(0.7, GridUnitType.Star),   // Тип
                    new ColumnDefinition(0.8, GridUnitType.Star),   // Номер
                    new ColumnDefinition(0.6, GridUnitType.Star),   // Напечатан
                    new ColumnDefinition(0.5, GridUnitType.Star)    // ПечатьП
                };

                foreach (var colDef in columnDefinitions)
                {
                    _tableGrid.ColumnDefinitions.Add(colDef);
                }

                // Создаем строку заголовков (строка 0)
                _tableGrid.RowDefinitions.Add(new RowDefinition(35, GridUnitType.Pixel));
                CreateHeaderRow();

                // ✅ ДОБАВЛЯЕМ СТРОКУ "ЗАГРУЗКА..." СРАЗУ
                AddLoadingRow();

                // Добавляем Grid в ScrollViewer
                _scrollViewer.Content = _tableGrid;

                // Добавляем ScrollViewer в Border
                tableBorder.Child = _scrollViewer;

                Console.WriteLine("✓ Таблица создана из кода");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании таблицы: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Добавление строки "Загрузка..."
        /// </summary>
        private void AddLoadingRow()
        {
            try
            {
                _tableGrid.RowDefinitions.Add(new RowDefinition(90, GridUnitType.Pixel));

                var mainContainer = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Orientation = Orientation.Vertical,
                    Spacing = 8
                };

                // Заголовок
                var titleText = new TextBlock
                {
                    Text = "ЗАГРУЗКА БАЗЫ ДАННЫХ",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#0D47A1")),
                    TextAlignment = TextAlignment.Center
                };

                // ProgressBar с настройками
                var progressBar = new ProgressBar
                {
                    Width = 400,
                    Height = 8,
                    IsIndeterminate = true,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.Parse("#2196F3")),
                    Background = new SolidColorBrush(Color.Parse("#E3F2FD")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#90CAF9")),
                    BorderThickness = new Thickness(1)
                };

                // Анимированные точки
                var dotsContainer = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                // Создаем 3 анимированные точки
                for (int i = 0; i < 3; i++)
                {
                    var dot = new Border
                    {
                        Width = 12,
                        Height = 12,
                        Background = new SolidColorBrush(Color.Parse("#42A5F5")),
                        CornerRadius = new CornerRadius(6),
                        Opacity = 0.4,
                        Margin = new Thickness(2)
                    };

                    // Простая анимация (можно заменить на настоящую анимацию)
                    DispatcherTimer timer = null;
                    timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(300 + (i * 200))
                    };
                    timer.Tick += (s, e) =>
                    {
                        dot.Opacity = dot.Opacity == 0.4 ? 1.0 : 0.4;
                    };
                    timer.Start();

                    dotsContainer.Children.Add(dot);
                }

                // Информационный текст
                var infoText = new TextBlock
                {
                    Text = "Получение данных из PostgreSQL...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#757575")),
                    FontStyle = FontStyle.Italic
                };

                mainContainer.Children.Add(titleText);
                mainContainer.Children.Add(progressBar);
                mainContainer.Children.Add(dotsContainer);
                mainContainer.Children.Add(infoText);

                Grid.SetColumnSpan(mainContainer, 10);
                Grid.SetRow(mainContainer, _currentRow);
                _tableGrid.Children.Add(mainContainer);

                _currentRow++;
                Console.WriteLine("✓ Добавлена строка загрузки с анимированными точками");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении строки загрузки: {ex.Message}");
            }
        }

        // Создание анимированного спиннера
        private Avalonia.Media.Imaging.Bitmap CreateLoadingSpinner()
        {
            // Простая реализация - можно заменить на настоящую картинку
            var renderTarget = new RenderTargetBitmap(new PixelSize(100, 100));
            using (var ctx = renderTarget.CreateDrawingContext())
            {
                var pen = new Pen(new SolidColorBrush(Color.Parse("#2196F3")), 4);
                ctx.DrawEllipse(null, pen, new Point(50, 50), 40, 40);
            }
            return renderTarget;
        }

        /// <summary>
        /// Создание строки заголовков
        /// </summary>
        private void CreateHeaderRow()
        {
            try
            {
                var headers = new[] { "Статус", "Дата", "Клиент", "Сумма", "Сдача", "Комментарий", "Тип", "Номер", "Напечатан", "ПечатьП" };

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
                            Foreground = Brushes.DarkBlue
                        }
                    };

                    Grid.SetColumn(headerBorder, i);
                    Grid.SetRow(headerBorder, 0);
                    _tableGrid.Children.Add(headerBorder);
                }

                Console.WriteLine("✓ Заголовки созданы");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании заголовков: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавление строки данных в таблицу (с учетом спец-стилей)
        /// </summary>
        private void AddRowToTable(CheckItem item, int dataRowIndex)
        {
            try
            {
                // Добавляем новую строку в Grid
                int gridRowIndex = _currentRow;
                _tableGrid.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));

                // БАЗОВЫЙ ЦВЕТ по умолчанию (белые и AliceBlue строки поочередно)
                IBrush rowBackground = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;
                FontWeight fontWeight = FontWeight.Normal;
                FontStyle fontStyle = FontStyle.Normal;
                double fontSize = 12;
                IBrush foreground = Brushes.Black;
                TextDecorationCollection textDecorations = null;

                // === ЛОГИКА ВЫДЕЛЕНИЯ СПЕЦИАЛЬНЫХ СЛУЧАЕВ ===

                // 1. Удаленный чек (ItsDeleted == 1)
                if (item.ItsDeleted == 1)
                {
                    // Прозрачный фон и зачеркнутый текст
                    rowBackground = Brushes.Transparent;
                    fontSize = 18;
                    fontStyle = FontStyle.Italic;
                    foreground = Brushes.Gray;
                    textDecorations = TextDecorations.Strikethrough;
                }
                // 2. Нераспечатанный чек (только для активных чеков)
                else if (item.ItsDeleted == 0 && MainStaticClass.Use_Fiscall_Print)
                {
                    // Проверяем по логике из WinForms
                    bool needHighlight = false;

                    // Проверяем оба флага печати (или один, если другой не установлен)
                    if (item.ItsPrint && item.ItsPrintP)
                    {
                        // Оба флага установлены - все распечатано
                        needHighlight = false;
                    }
                    else
                    {
                        // Хотя бы один флаг не установлен - нужно выделение
                        needHighlight = true;
                    }

                    if (needHighlight)
                    {
                        // Розовый фон (аналог Color.Pink) и подчеркнутый жирный текст
                        rowBackground = new SolidColorBrush(Color.Parse("#FFFFC0CB")); // Розовый цвет
                        fontSize = 18;
                        fontWeight = FontWeight.Bold;
                        textDecorations = TextDecorations.Underline;
                    }
                }

                // Создаем Border для всей строки
                var rowBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = rowBackground,
                    Tag = dataRowIndex // Сохраняем индекс данных
                };

                // Подписываемся на события клика
                rowBorder.PointerPressed += OnRowPointerPressed;

                Grid.SetColumnSpan(rowBorder, 10);
                Grid.SetRow(rowBorder, gridRowIndex);
                _tableGrid.Children.Add(rowBorder);

                // === ДОБАВЛЯЕМ ЯЧЕЙКИ С УЧЕТОМ СТИЛЕЙ ===

                // Колонка 1: Статус (Удален/Активен)
                AddStyledCell(0, gridRowIndex,
                    item.ItsDeleted == 1 ? "Удален" : "Активен",
                    HorizontalAlignment.Center,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 2: Дата
                AddStyledCell(1, gridRowIndex,
                    item.DateTimeWrite.ToString("dd.MM.yyyy HH:mm:ss"),
                    HorizontalAlignment.Left,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 3: Клиент
                AddStyledCell(2, gridRowIndex,
                    item.ClientName,
                    HorizontalAlignment.Left,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 4: Сумма
                AddStyledCell(3, gridRowIndex,
                    item.Cash.ToString("N2"),
                    HorizontalAlignment.Right,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 5: Сдача
                AddStyledCell(4, gridRowIndex,
                    item.Remainder.ToString("N2"),
                    HorizontalAlignment.Right,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 6: Комментарий
                AddStyledCell(5, gridRowIndex,
                    item.Comment,
                    HorizontalAlignment.Left,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 7: Тип чека
                AddStyledCell(6, gridRowIndex,
                    item.CheckType,
                    HorizontalAlignment.Center,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 8: Номер документа
                AddStyledCell(7, gridRowIndex,
                    item.DocumentNumber,
                    HorizontalAlignment.Right,
                    fontSize, fontWeight, fontStyle, foreground, textDecorations);

                // Колонка 9: Напечатан (CheckBox)
                AddCheckBoxCell(8, gridRowIndex, item.ItsPrint);

                // Колонка 10: ПечатьП (CheckBox)
                AddCheckBoxCell(9, gridRowIndex, item.ItsPrintP);

                _currentRow++;

                Console.WriteLine($"✓ Добавлена строка {dataRowIndex}: {item.DocumentNumber}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении строки: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавление ячейки со стилями
        /// </summary>
        private void AddStyledCell(int column, int row, string text,
                                  HorizontalAlignment alignment,
                                  double fontSize, FontWeight fontWeight, FontStyle fontStyle,
                                  IBrush foreground, TextDecorationCollection textDecorations)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment,
                FontSize = fontSize,
                FontWeight = fontWeight,
                FontStyle = fontStyle,
                Foreground = foreground
            };

            if (textDecorations != null)
            {
                textBlock.TextDecorations = textDecorations;
            }

            Grid.SetColumn(textBlock, column);
            Grid.SetRow(textBlock, row);
            _tableGrid.Children.Add(textBlock);
        }

        /// <summary>
        /// Добавление ячейки с CheckBox
        /// </summary>
        private void AddCheckBoxCell(int column, int row, bool isChecked)
        {
            var checkBox = new CheckBox
            {
                IsEnabled = false,
                IsChecked = isChecked,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(checkBox, column);
            Grid.SetRow(checkBox, row);
            _tableGrid.Children.Add(checkBox);
        }

        /// <summary>
        /// Очистка таблицы (кроме заголовков)
        /// </summary>
        private void ClearTable()
        {
            try
            {
                // Удаляем выделение
                ClearSelection();

                // Удаляем все строки кроме заголовков
                while (_tableGrid.RowDefinitions.Count > 1)
                {
                    _tableGrid.RowDefinitions.RemoveAt(_tableGrid.RowDefinitions.Count - 1);
                }

                // Удаляем все элементы кроме заголовков
                var elementsToRemove = new List<Control>();
                foreach (Control child in _tableGrid.Children)
                {
                    if (Grid.GetRow(child) > 0)
                    {
                        elementsToRemove.Add(child);
                    }
                }

                foreach (var element in elementsToRemove)
                {
                    _tableGrid.Children.Remove(element);
                }

                _currentRow = 1;
                _checkItems.Clear();
                _selectedRowIndex = -1;

                Console.WriteLine("✓ Таблица очищена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при очистке таблицы: {ex.Message}");
            }
        }

        /// <summary>
        /// Выделение строки по индексу
        /// </summary>
        private void SelectRow(int rowIndex, bool scrollToRow = true)  // Добавляем параметр scrollToRow
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _checkItems.Count)
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

                foreach (Control child in _tableGrid.Children)
                {
                    if (child is Border border && Grid.GetRow(border) == gridRowIndex)
                    {
                        // Меняем стиль выделенной строки
                        border.Background = SELECTED_ROW_BACKGROUND;
                        border.BorderBrush = SELECTED_ROW_BORDER;
                        border.BorderThickness = new Thickness(2);

                        _selectedRowBorder = border;
                        break;
                    }
                }

                // Прокручиваем к выделенной строке ТОЛЬКО если нужно
                if (scrollToRow)
                {
                    ScrollToRow(gridRowIndex);
                }

                Console.WriteLine($"✓ Выделена строка {rowIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при выделении строки: {ex.Message}");
            }
        }


        /// <summary>
        /// Снятие выделения
        /// </summary>
        private void ClearSelection()
        {
            try
            {
                if (_selectedRowBorder != null)
                {
                    // Восстанавливаем оригинальный фон строки
                    // Получаем индекс данных
                    int dataRowIndex = (int)_selectedRowBorder.Tag;

                    // Находим CheckItem для этой строки
                    if (dataRowIndex >= 0 && dataRowIndex < _checkItems.Count)
                    {
                        var checkItem = _checkItems[dataRowIndex];

                        // Проверяем, каким был оригинальный стиль
                        IBrush originalBackground = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;

                        // Если это удаленный чек
                        if (checkItem.ItsDeleted == 1)
                        {
                            originalBackground = Brushes.Transparent;
                        }
                        // Если это нераспечатанный чек
                        else if (checkItem.ItsDeleted == 0 && MainStaticClass.Use_Fiscall_Print)
                        {
                            bool needHighlight = false;
                            if (!checkItem.ItsPrint || !checkItem.ItsPrintP)
                            {
                                needHighlight = true;
                            }

                            if (needHighlight)
                            {
                                originalBackground = new SolidColorBrush(Color.Parse("#FFFFC0CB")); // Розовый
                            }
                        }

                        _selectedRowBorder.Background = originalBackground;
                    }
                    else
                    {
                        // Если не нашли, используем стандартную логику
                        var originalBackground = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;
                        _selectedRowBorder.Background = originalBackground;
                    }

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

        /// <summary>
        /// Прокрутка к указанной строке с проверкой видимости
        /// </summary>
        private void ScrollToRow(int gridRowIndex)
        {
            try
            {
                if (_scrollViewer == null || _tableGrid == null) return;

                // ПРОВЕРЯЕМ, ВИДИМА ЛИ УЖЕ СТРОКА В ОКНЕ ПРОСМОТРА
                // Вычисляем позицию строки
                double rowPosition = 0;
                for (int i = 0; i < gridRowIndex; i++)
                {
                    if (i < _tableGrid.RowDefinitions.Count)
                    {
                        rowPosition += _tableGrid.RowDefinitions[i].Height.Value;
                    }
                }

                double rowBottom = rowPosition + _tableGrid.RowDefinitions[gridRowIndex].Height.Value;

                // Проверяем, полностью ли видна строка в текущей области просмотра
                bool isFullyVisible = rowPosition >= _scrollViewer.Offset.Y &&
                                       rowBottom <= _scrollViewer.Offset.Y + _scrollViewer.Viewport.Height;

                // Проверяем, частично видна ли строка сверху или снизу
                bool isPartiallyVisibleTop = rowPosition >= _scrollViewer.Offset.Y &&
                                             rowPosition <= _scrollViewer.Offset.Y + _scrollViewer.Viewport.Height;

                bool isPartiallyVisibleBottom = rowBottom >= _scrollViewer.Offset.Y &&
                                                rowBottom <= _scrollViewer.Offset.Y + _scrollViewer.Viewport.Height;

                // Если строка уже полностью видна, НЕ прокручиваем
                if (isFullyVisible)
                {
                    Console.WriteLine($"✓ Строка {gridRowIndex} уже видна, прокрутка не требуется");
                    return;
                }

                // Если строка частично видна, можно немного подкорректировать позицию
                // но не прыгать резко. Давайте сделаем так:

                if (isPartiallyVisibleTop)
                {
                    // Строка видна сверху, но не полностью
                    // Прокручиваем так, чтобы она была сверху видимой области
                    _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, rowPosition - 20); // Небольшой отступ сверху
                    Console.WriteLine($"✓ Строка {gridRowIndex} частично видна сверху, корректируем позицию");
                }
                else if (isPartiallyVisibleBottom)
                {
                    // Строка видна снизу, но не полностью
                    // Прокручиваем так, чтобы она была снизу видимой области
                    double targetPosition = rowBottom - _scrollViewer.Viewport.Height + 20;
                    _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetPosition);
                    Console.WriteLine($"✓ Строка {gridRowIndex} частично видна снизу, корректируем позицию");
                }
                else
                {
                    // Строка не видна совсем - центрируем ее
                    double centerPosition = rowPosition - (_scrollViewer.Viewport.Height / 2) +
                                           (_tableGrid.RowDefinitions[gridRowIndex].Height.Value / 2);
                    _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, centerPosition);
                    Console.WriteLine($"✓ Строка {gridRowIndex} не видна, центрируем");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при прокрутке: {ex.Message}");
            }
        }

        /// <summary>
        /// Перемещение выделения вверх (без циклического перехода)
        /// </summary>
        private void MoveSelectionUp()
        {
            if (_checkItems.Count == 0) return;

            int newIndex = _selectedRowIndex - 1;

            // Если достигли начала списка - останавливаемся на первой строке
            if (newIndex < 0)
            {
                // Можно остаться на первой строке или снять выделение
                // В данном случае оставляем на первой строке
                if (_selectedRowIndex != 0)
                {
                    SelectRow(0);
                }
                return;
            }

            SelectRow(newIndex);
        }

        /// <summary>
        /// Перемещение выделения вниз (без циклического перехода)
        /// </summary>
        private void MoveSelectionDown()
        {
            if (_checkItems.Count == 0) return;

            int newIndex = _selectedRowIndex + 1;

            // Если достигли конца списка - останавливаемся на последней строке
            if (newIndex >= _checkItems.Count)
            {
                // Можно остаться на последней строке или снять выделение
                // В данном случае оставляем на последней строке
                if (_selectedRowIndex != _checkItems.Count - 1)
                {
                    SelectRow(_checkItems.Count - 1);
                }
                return;
            }

            SelectRow(newIndex);
        }

        /// <summary>
        /// Перемещение на страницу вверх
        /// </summary>
        private void MovePageUp()
        {
            if (_checkItems.Count == 0 || _selectedRowIndex <= 0) return;

            // Количество видимых строк
            int visibleRows = GetVisibleRowCount();
            int newIndex = Math.Max(0, _selectedRowIndex - visibleRows);
            SelectRow(newIndex);
        }

        /// <summary>
        /// Перемещение на страницу вниз
        /// </summary>
        private void MovePageDown()
        {
            if (_checkItems.Count == 0) return;

            // Количество видимых строк
            int visibleRows = GetVisibleRowCount();
            int newIndex = Math.Min(_checkItems.Count - 1, _selectedRowIndex + visibleRows);
            SelectRow(newIndex);
        }

        /// <summary>
        /// Получение количества видимых строк
        /// </summary>
        private int GetVisibleRowCount()
        {
            if (_scrollViewer == null || _scrollViewer.Viewport.Height <= 0)
                return 10; // значение по умолчанию

            // Высота строки (30 пикселей)
            double rowHeight = 30;

            // Количество строк, помещающихся в видимой области
            int visibleRows = (int)(_scrollViewer.Viewport.Height / rowHeight);

            // Минимум 1 строка, минус 1 чтобы оставить часть строки для контекста
            return Math.Max(1, visibleRows - 1);
        }

        /// <summary>
        /// Обработка выбранного элемента
        /// </summary>
        private async void ProcessSelectedItem()
        {
            if (_selectedRowIndex >= 0 && _selectedRowIndex < _checkItems.Count)
            {
                var selectedItem = _checkItems[_selectedRowIndex];
                Console.WriteLine($"Выбран чек: {selectedItem.DocumentNumber}, Клиент: {selectedItem.ClientName}, Сумма: {selectedItem.Cash}");

                // Открываем форму с деталями чека
                await OpenCheckDetails(selectedItem.DateTimeWrite.ToString("dd-MM-yyyy HH:mm:ss"));
            }
        }

        /// <summary>
        /// Открытие деталей чека
        /// </summary>
        private async Task OpenCheckDetails(string dateTimeWrite)
        {
            try
            {
                Console.WriteLine($"Открытие чека от {dateTimeWrite}");

                // Создаем окно чека
                var checkWindow = new Cash_check();

                // Передаем параметры
                checkWindow.date_time_write = dateTimeWrite;
                checkWindow.IsNewCheck = false;

                checkWindow.OnFormLoaded();

                // Находим активное окно
                Window parentWindow = null;

                // Вариант 1: Через TopLevel
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window currentWindow)
                {
                    parentWindow = currentWindow;
                }

                // Вариант 2: Через Application
                if (parentWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    parentWindow = desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
                }

                // Настройка размеров окна чека
                if (parentWindow != null)
                {
                    // Получаем размеры главного окна
                    double mainWidth = parentWindow.Bounds.Width;
                    double mainHeight = parentWindow.Bounds.Height;

                    // Проверяем, есть ли у родительского окна системные декорации
                    bool parentHasDecorations = parentWindow.SystemDecorations != SystemDecorations.None;
                    bool checkHasDecorations = checkWindow.SystemDecorations != SystemDecorations.None;

                    // Примерная высота заголовка Windows
                    const double titleBarHeight = 35;

                    if (parentHasDecorations && !checkHasDecorations)
                    {
                        // Компенсируем разницу в высоте
                        checkWindow.Width = mainWidth;
                        checkWindow.Height = mainHeight + titleBarHeight;

                        Console.WriteLine($"Компенсируем разницу в высоте: +{titleBarHeight}px");
                    }
                    else
                    {
                        // Окна имеют одинаковый тип декораций
                        checkWindow.Width = mainWidth;
                        checkWindow.Height = mainHeight;
                    }

                    // Позиционируем по центру главного окна
                    checkWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // Стандартные размеры если нет родительского окна
                    checkWindow.Width = 1200;
                    checkWindow.Height = 800;
                    checkWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // Настройка свойств окна
                checkWindow.Title = $"Чек № {_checkItems[_selectedRowIndex].DocumentNumber} от {_checkItems[_selectedRowIndex].DateTimeWrite:dd.MM.yyyy HH:mm:ss}";
                checkWindow.CanResize = false;
                checkWindow.CanMaximize = false;
                checkWindow.CanMinimize = false;

                // Устанавливаем позиционирование и показываем
                if (parentWindow != null)
                {
                    checkWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    await checkWindow.ShowDialog(parentWindow);
                }
                else
                {
                    checkWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    checkWindow.Show();
                }

                // Дополнительно: можно проверить результат после закрытия
                bool? dialogResult = checkWindow.Tag as bool?;
                if (dialogResult == true)
                {
                    Console.WriteLine("Чек успешно обработан");                    
                }
                LoadDocuments(); // Обновляем список после создания чека
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при открытии чека: {ex.Message}");
                await MessageBox.Show($"Ошибка при открытии чека: {ex.Message}");
            }
        }

        #region Обработчики событий мыши и клавиатуры

        /// <summary>
        /// Получение статуса отправки документов
        /// </summary>
        private void GetStatusSendDocument()
        {
            try
            {
                // ВЫПОЛНЯЕМ ВСЕ ТЯЖЕЛЫЕ ОПЕРАЦИИ В ПОТОКЕ ТАЙМЕРА, А НЕ В UI
                int documentsNotOut = MainStaticClass.get_documents_not_out();
                string documents_not_out = documentsNotOut.ToString();

                int documents_out_of_the_range_of_dates = MainStaticClass.get_documents_out_of_the_range_of_dates();

                string result = "";

                if (documents_not_out == "-1")
                {
                    result = " Произошли ошибки при получении кол-ва неотправленных документов, ";
                }
                else
                {
                    result = " Не отправлено документов " + documents_not_out + ",";
                }

                if (documents_out_of_the_range_of_dates == -1)
                {
                    result += " Не удалось получить дату с сервера ";
                }
                else if (documents_out_of_the_range_of_dates == -2)
                {
                    result += " Не удалось получить количество документов вне диапазона ";
                }
                else if (documents_out_of_the_range_of_dates > 0)
                {
                    result += " За диапазоном находится " + documents_out_of_the_range_of_dates.ToString();
                }

                result += "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

                // ТОЛЬКО ОБНОВЛЕНИЕ UI - В ДИСПЕТЧЕРЕ
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        if (_txtStatusBox != null)
                        {
                            _txtStatusBox.Text = result;
                        }
                        else
                        {
                            _txtStatusBox = this.FindControl<TextBox>("txtB_not_unloaded_docs");
                            if (_txtStatusBox != null)
                            {
                                _txtStatusBox.Text = result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка обновления UI: {ex.Message}");
                    }
                });

                // Проверка версии - тоже в фоне
                bool hasNewVersion = MainStaticClass.CheckNewVersionProgramm();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        var pictureBox = this.FindControl<Image>("pictureBox_get_update_program");
                        if (pictureBox != null)
                        {
                            pictureBox.IsVisible = hasNewVersion;
                            if (hasNewVersion)
                            {
                                Console.WriteLine("✓ Обнаружена новая версия программы");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при проверке версии: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в GetStatusSendDocument: {ex.Message}");

                // Показываем ошибку в UI
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        if (_txtStatusBox != null)
                        {
                            _txtStatusBox.Text = $"⚠ Ошибка связи с БД {DateTime.Now:dd-MM-yyyy HH:mm:ss}";
                        }
                    }
                    catch { }
                });
            }
        }

        /// <summary>
        /// Проверка наличия новой версии программы
        /// </summary>
        private void CheckNewVersion()
        {
            try
            {
                bool hasNewVersion = MainStaticClass.CheckNewVersionProgramm();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        var pictureBox = this.FindControl<Image>("pictureBox_get_update_program");
                        if (pictureBox != null)
                        {
                            pictureBox.IsVisible = hasNewVersion;

                            if (hasNewVersion)
                            {
                                Console.WriteLine("✓ Обнаружена новая версия программы");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при обновлении UI: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при проверке версии: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализация таймера для обновления статуса
        /// </summary>
        private async Task InitializeStatusTimer()
        {
            try
            {
                int unloadInterval = await MainStaticClass.GetUnloadingInterval();

                if (unloadInterval > 0)
                {
                    _statusTimer = new System.Timers.Timer(unloadInterval * 60 * 1000);
                    _statusTimer.Elapsed += StatusTimer_Elapsed;
                    _statusTimer.AutoReset = true;
                    _statusTimer.Enabled = true;

                    Console.WriteLine($"✓ Таймер статуса инициализирован с интервалом {unloadInterval} мин.");

                    // Первоначальное обновление статуса - в фоне
                    Task.Run(() => GetStatusSendDocument());
                }
                else
                {
                    Console.WriteLine("⚠ Интервал выгрузки не настроен, таймер не запущен");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при инициализации таймера: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события таймера
        /// </summary>
        private void StatusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // Проверяем, прошла ли минута с последнего выполнения
                if (DateTime.Now > _timerExecute.AddSeconds(59))
                {
                    // Обновляем статус отправки документов
                    GetStatusSendDocument();

                    // Обновляем время последнего выполнения
                    _timerExecute = DateTime.Now;

                    Console.WriteLine($"✓ Статус отправки обновлен в {DateTime.Now:HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в обработчике таймера: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка клика по строке
        /// </summary>
        private void OnRowPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is int dataRowIndex)
                {
                    // При клике мышкой прокручивать НЕ нужно
                    SelectRow(dataRowIndex, scrollToRow: false);  // false - не прокручивать
                    e.Handled = true;

                    // Устанавливаем фокус на ScrollViewer для обработки клавиатуры
                    _scrollViewer?.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в обработчике клика строки: {ex.Message}");
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
        /// Обработка клика по ScrollViewer
        /// </summary>
        private void OnScrollViewerPointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Устанавливаем фокус на ScrollViewer
            _scrollViewer?.Focus();
        }

        private async void ProcessInsertKey()
        {
            Console.WriteLine("Insert нажат - создание нового чека");

            try
            {
                // Проверка даты на компьютере
                if (DateTime.Now <= MainStaticClass.GetMinDateWork)
                {
                    await MessageBox.Show(
                        " У ВАС УСТАНОВЛЕНА НЕПРАВИЛЬНАЯ ДАТА НА КОМПЬЮТЕРЕ !!! ДАЛЬНЕЙШАЯ РАБОТА С ЧЕКАМИ НЕВОЗМОЖНА !!!",
                        "Проверка даты на компьютере",
                        MessageBoxButton.OK,
                        MessageBoxType.Error
                    );
                    return;
                }

                // Проверка версии ФН
                if (MainStaticClass.CashDeskNumber != 9)
                {
                    bool restart = false;
                    bool errors = false;
                    MainStaticClass.check_version_fn(ref restart, ref errors);

                    if (errors)
                    {
                        return;
                    }

                    if (restart)
                    {
                        await MessageBox.Show(
                            "У вас неверно была установлена версия ФН, НЕОБХОДИМ ПЕРЕЗАПУСК КАССОВОЙ ПРОГРАММЫ !!!",
                            "Проверка настройки ФН",
                            MessageBoxButton.OK,
                            MessageBoxType.Error
                        );
                        return;
                    }
                }

                // Проверка системы налогообложения
                if (MainStaticClass.SystemTaxation == 0)
                {
                    await MessageBox.Show(
                        "У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!"
                    );
                    return;
                }

                // Проверка на заполненность обязательных реквизитов
                if (!await AllIsFilled())
                {
                    return;
                }

                // Проверка кассира
                var txtCashier = this.FindControl<TextBox>("txtB_cashier");
                if (txtCashier == null || string.IsNullOrWhiteSpace(txtCashier.Text))
                {
                    await MessageBox.Show("Не заполнен кассир");
                    return;
                }

                // Проверка времени с ФН
                MainStaticClass.validate_date_time_with_fn(15);

                // Создаем окно для нового чека
                var checkWindow = new Cash_check();

                // Настраиваем для нового чека
                checkWindow.IsNewCheck = true;
                checkWindow.cashier = txtCashier.Text;

                // Дополнительная инициализация для нового чека
                checkWindow.OnFormLoaded();

                // Находим активное окно
                Window parentWindow = null;

                // Вариант 1: Через TopLevel
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window currentWindow)
                {
                    parentWindow = currentWindow;
                }

                // Вариант 2: Через Application
                if (parentWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    parentWindow = desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
                }

                // Настройка размеров окна чека
                if (parentWindow != null)
                {
                    // Получаем размеры главного окна
                    double mainWidth = parentWindow.Bounds.Width;
                    double mainHeight = parentWindow.Bounds.Height;

                    // Проверяем, есть ли у родительского окна системные декорации
                    bool parentHasDecorations = parentWindow.SystemDecorations != SystemDecorations.None;
                    bool checkHasDecorations = checkWindow.SystemDecorations != SystemDecorations.None;

                    // Примерная высота заголовка Windows
                    const double titleBarHeight = 35;

                    if (parentHasDecorations && !checkHasDecorations)
                    {
                        // Компенсируем разницу в высоте
                        checkWindow.Width = mainWidth;
                        checkWindow.Height = mainHeight + titleBarHeight;

                        Console.WriteLine($"Компенсируем разницу в высоте: +{titleBarHeight}px");
                    }
                    else
                    {
                        // Окна имеют одинаковый тип декораций
                        checkWindow.Width = mainWidth;
                        checkWindow.Height = mainHeight;
                    }

                    // Позиционируем по центру главного окна
                    checkWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // Стандартные размеры если нет родительского окна
                    checkWindow.Width = 1200;
                    checkWindow.Height = 800;
                    checkWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // Настройка свойств окна
                checkWindow.Title = "Новый чек";
                checkWindow.CanResize = false;
                checkWindow.CanMaximize = false;
                checkWindow.CanMinimize = false;

                // Подписываемся на событие закрытия окна
                checkWindow.Closed += (s, e) =>
                {
                    // Проверяем результат через Tag
                    bool? dialogResult = checkWindow.Tag as bool?;
                    if (dialogResult == true) // Чек успешно создан
                    {
                        LoadDocuments(); // Обновляем список после создания чека
                    }
                };

                // ВАЖНО: Добавляем обработчик события загрузки окна
                checkWindow.Loaded += (s, e) =>
                {
                    Console.WriteLine("Окно чека загружено и отображается");
                };

                // Показываем окно
                if (parentWindow != null)
                {
                    // Показываем как диалог (модальное окно)
                    await checkWindow.ShowDialog(parentWindow);
                }
                else
                {
                    // Показываем как немодальное окно
                    checkWindow.Show();

                    // Если нужно ждать закрытия, можно использовать TaskCompletionSource
                    var tcs = new TaskCompletionSource<bool>();
                    checkWindow.Closed += (s, e) => tcs.TrySetResult(true);
                    await tcs.Task;
                }

                // Обновляем список документов
                LoadDocuments();

                Console.WriteLine("Окно чека закрыто, обновляем список документов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании нового чека: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                await MessageBox.Show($"Ошибка при создании нового чека: {ex.Message}");
            }
        }

        private async Task<bool> AllIsFilled()
        {
            bool result = true;
            try
            {
                if (MainStaticClass.Nick_Shop.Trim().Length == 0)
                {
                    await MessageBox.Show("Не заполнен код магазина", "Проверка заполнения", MessageBoxButton.OK, MessageBoxType.Error);
                    return false;
                }
                if (MainStaticClass.CashDeskNumber == 0)
                {
                    await MessageBox.Show("Номер кассы не может быть ноль", "Проверка заполнения", MessageBoxButton.OK, MessageBoxType.Error);
                    return false;
                }
                if (MainStaticClass.Cash_Operator.Trim().Length == 0)
                {
                    await MessageBox.Show("Не заполнен Кассир", "Проверка заполнения", MessageBoxButton.OK, MessageBoxType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show(" AllIsFilled " + ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Глобальная обработка клавиатуры
        /// </summary>
        private void OnGlobalKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"Key pressed: {e.Key}");

            // Обработка Insert - работает ВЕЗДЕ на форме
            if (e.Key == Key.Insert)
            {
                ProcessInsertKey();
                e.Handled = true;
                return;
            }

            // Проверяем, есть ли фокус в таблице
            bool isTableFocused = _scrollViewer?.IsFocused == true ||
                                 _tableGrid?.IsFocused == true ||
                                 IsChildFocused(_scrollViewer);

            if (!isTableFocused) return;

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

                case Key.PageUp:
                    MovePageUp();
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    MovePageDown();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    ProcessSelectedItem();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    ClearSelection();
                    e.Handled = true;
                    break;

                case Key.Home:
                    if (_checkItems.Count > 0)
                    {
                        // При Home прокручиваем к первой строке
                        SelectRow(0);
                    }
                    e.Handled = true;
                    break;

                case Key.End:
                    if (_checkItems.Count > 0)
                    {
                        // При End прокручиваем к последней строке
                        SelectRow(_checkItems.Count - 1);
                    }
                    e.Handled = true;
                    break;

                case Key.F5: // Добавьте F5 для обновления
                    LoadDocuments();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Проверка, имеет ли дочерний элемент фокус
        /// </summary>
        private bool IsChildFocused(Control parent)
        {
            if (parent == null) return false;

            foreach (var child in parent.GetVisualChildren())
            {
                if (child is Control control && control.IsFocused)
                    return true;

                if (IsChildFocused(child as Control))
                    return true;
            }

            return false;
        }

        #endregion

        /// <summary>
        /// ✅ Асинхронная загрузка документов
        /// </summary>
        private async Task LoadDocumentsAsync()
        {
            try
            {
                Console.WriteLine("=== Асинхронная загрузка документов ===");

                var checkBox = this.FindControl<CheckBox>("checkBox_show_3_last_checks");
                //var datePicker = this.FindControl<DatePicker>("dateTimePicker1");

                if (checkBox == null)// || datePicker == null)
                {
                    Console.WriteLine("✗ Контрол не найдены!");
                    return;
                }

                // Получаем параметры
                bool showLast3 = checkBox.IsChecked ?? false;
                DateTime selectedDate = _currentDate;//datePicker.SelectedDate?.DateTime ?? DateTime.Today;

                Console.WriteLine($"Параметры: showLast3={showLast3}, date={selectedDate:yyyy-MM-dd}");

                // ✅ ЗАГРУЖАЕМ ДАННЫЕ В ФОНОВОМ ПОТОКЕ
                var checkItems = await Task.Run(() =>
                {
                    var items = new List<CheckItem>();

                    try
                    {
                        Console.WriteLine("Загрузка данных из БД...");
                        using (var conn = MainStaticClass.NpgsqlConn())
                        {
                            conn.Open();
                            Console.WriteLine("✓ Соединение с БД установлено");

                            string myQuery = @"
                        SELECT checks_header.its_deleted,
                               checks_header.date_time_write,
                               clients.name,
                               checks_header.cash,
                               checks_header.remainder,
                               checks_header.comment,
                               checks_header.its_print,
                               checks_header.check_type,
                               checks_header.document_number,
                               checks_header.its_print_p  
                        FROM checks_header 
                        LEFT JOIN clients ON checks_header.client = clients.code 
                        WHERE checks_header.date_time_write BETWEEN @startDate AND @endDate 
                          AND its_deleted < 2 
                        ORDER BY checks_header.date_time_write";

                            if (showLast3)
                            {
                                myQuery += " DESC LIMIT 3";
                            }

                            Console.WriteLine($"SQL запрос: {myQuery}");

                            using (var command = new NpgsqlCommand(myQuery, conn))
                            {
                                command.Parameters.AddWithValue("@startDate", selectedDate);
                                command.Parameters.AddWithValue("@endDate", selectedDate.AddDays(1));

                                using (var reader = command.ExecuteReader())
                                {
                                    int count = 0;

                                    while (reader.Read())
                                    {
                                        count++;
                                        Console.WriteLine($"Чтение строки {count}...");

                                        // Создаем CheckItem
                                        var checkItem = new CheckItem();

                                        // 0. its_deleted - numeric/decimal
                                        checkItem.ItsDeleted = reader.IsDBNull(0) ? 0 : Convert.ToDecimal(reader.GetValue(0));

                                        // 1. date_time_write - timestamp
                                        checkItem.DateTimeWrite = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);

                                        // 2. clients.name - text/varchar (может быть NULL)
                                        checkItem.ClientName = reader.IsDBNull(2) ? "" : reader.GetString(2);

                                        // 3. cash - numeric/decimal
                                        checkItem.Cash = reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3));

                                        // 4. remainder - numeric/decimal
                                        checkItem.Remainder = reader.IsDBNull(4) ? 0 : Convert.ToDecimal(reader.GetValue(4));

                                        // 5. comment - text/varchar
                                        checkItem.Comment = reader.IsDBNull(5) ? "" : reader.GetString(5);

                                        // 6. its_print - boolean
                                        checkItem.ItsPrint = reader.IsDBNull(6) ? false : Convert.ToBoolean(reader.GetValue(6));

                                        // 7. check_type - smallint (0,1,2)
                                        if (!reader.IsDBNull(7))
                                        {
                                            object checkTypeObj = reader.GetValue(7);
                                            if (checkTypeObj is short shortValue)
                                            {
                                                checkItem.CheckType = shortValue switch
                                                {
                                                    0 => "Продажа",
                                                    1 => "Возврат",
                                                    2 => "Коррекция",
                                                    _ => $"Неизвестно ({shortValue})"
                                                };
                                            }
                                            else if (checkTypeObj is int intValue)
                                            {
                                                checkItem.CheckType = intValue switch
                                                {
                                                    0 => "Продажа",
                                                    1 => "Возврат",
                                                    2 => "Коррекция",
                                                    _ => $"Неизвестно ({intValue})"
                                                };
                                            }
                                            else
                                            {
                                                checkItem.CheckType = "Неизвестно";
                                            }
                                        }
                                        else
                                        {
                                            checkItem.CheckType = "Неизвестно";
                                        }

                                        // 8. document_number - bigint (int64)
                                        if (!reader.IsDBNull(8))
                                        {
                                            object docNumObj = reader.GetValue(8);
                                            if (docNumObj is long longValue)
                                            {
                                                checkItem.DocumentNumber = longValue.ToString();
                                            }
                                            else if (docNumObj is int intValue)
                                            {
                                                checkItem.DocumentNumber = intValue.ToString();
                                            }
                                            else if (docNumObj is decimal decimalValue)
                                            {
                                                checkItem.DocumentNumber = decimalValue.ToString("F0");
                                            }
                                            else
                                            {
                                                checkItem.DocumentNumber = docNumObj.ToString();
                                            }
                                        }
                                        else
                                        {
                                            checkItem.DocumentNumber = "";
                                        }

                                        // 9. its_print_p - boolean
                                        checkItem.ItsPrintP = reader.IsDBNull(9) ? false : Convert.ToBoolean(reader.GetValue(9));

                                        items.Add(checkItem);
                                        Console.WriteLine($"  - Чек #{checkItem.DocumentNumber}: {checkItem.ClientName}, сумма: {checkItem.Cash}");
                                    }
                                    Console.WriteLine($"✓ Прочитано {count} строк из БД");
                                }
                            }
                        }
                        Console.WriteLine($"✓ Загружено {items.Count} записей из БД");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при загрузке из БД: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }

                    return items;
                });

                // ✅ ОБНОВЛЯЕМ UI В UI ПОТОКЕ
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        Console.WriteLine($"Обновление таблицы с {checkItems.Count} записями");

                        if (checkItems.Count == 0)
                        {
                            Console.WriteLine("⚠ Нет данных для отображения");
                            ClearTable();
                            AddMessageRow("Нет данных за выбранную дату");
                            return;
                        }

                        ClearTable();

                        for (int i = 0; i < checkItems.Count; i++)
                        {
                            _checkItems.Add(checkItems[i]);
                            AddRowToTable(checkItems[i], i);
                        }

                        Console.WriteLine($"✓ Таблица обновлена: {_checkItems.Count} записей");

                        // Автоматически выделяем первую строку
                        if (_checkItems.Count > 0)
                        {
                            SelectRow(0);
                        }

                        // Прокручиваем к началу
                        if (_scrollViewer != null)
                        {
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                _scrollViewer.ScrollToHome();
                            }, DispatcherPriority.Background);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при обновлении таблицы: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }
                });

                Console.WriteLine($"✓ Асинхронная загрузка завершена: {checkItems.Count} записей");
                RestoreFocusAfterLoad();                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в LoadDocumentsAsync: {ex.Message}");
                await MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает текущий элемент с фокусом в Avalonia
        /// </summary>
        private IInputElement GetFocusedElement()
        {
            try
            {
                // В Avalonia можно получить TopLevel и через него фокус
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    return topLevel.FocusManager?.GetFocusedElement();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении элемента с фокусом: {ex.Message}");
            }

            return null;
        }

        ///// <summary>
        ///// Получает текущий элемент с фокусом как Control
        ///// </summary>
        //private Control GetFocusedControl()
        //{
        //    return GetFocusedElement() as Control;
        //}

        /// <summary>
        /// Восстановление фокуса после загрузки данных
        /// </summary>
        private void RestoreFocusAfterLoad()
        {
            try
            {
                Console.WriteLine("Восстановление фокуса после загрузки данных...");

                // Даем немного времени для завершения обновления UI
                DispatcherTimer focusTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };

                focusTimer.Tick += (s, e) =>
                {
                    focusTimer.Stop();

                    try
                    {
                        // Просто устанавливаем фокус на UserControl
                        this.Focus();
                        Console.WriteLine("✓ Фокус установлен на UserControl");

                        // Дополнительно: через небольшой промежуток пробуем установить на таблицу
                        DispatcherTimer tableFocusTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(50)
                        };

                        tableFocusTimer.Tick += (s2, e2) =>
                        {
                            tableFocusTimer.Stop();

                            if (_scrollViewer != null)
                            {
                                _scrollViewer.Focus();
                                Console.WriteLine("✓ Дополнительная попытка фокуса на ScrollViewer");
                            }
                        };

                        tableFocusTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при восстановлении фокуса: {ex.Message}");
                    }
                };

                focusTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Критическая ошибка при восстановлении фокуса: {ex.Message}");
            }
        }

        /// <summary>
        /// Упрощенная принудительная установка фокуса
        /// </summary>
        public void ForceFocusToTable()
        {
            try
            {
                Console.WriteLine("Принудительная установка фокуса на таблицу...");

                // Простой подход: сначала на UserControl, потом на ScrollViewer
                this.Focus();

                if (_scrollViewer != null)
                {
                    _scrollViewer.Focus();
                }

                Console.WriteLine("✓ Фокус установлен");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при установке фокуса: {ex.Message}");
            }
        }



        /// <summary>
        /// Добавление строки с сообщением (если нет данных)
        /// </summary>
        private void AddMessageRow(string message)
        {
            try
            {
                // Добавляем строку для сообщения
                _tableGrid.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));

                // Создаем TextBlock с сообщением
                var messageText = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontStyle = FontStyle.Italic,
                    Foreground = Brushes.Gray
                };

                Grid.SetColumnSpan(messageText, 10);
                Grid.SetRow(messageText, _currentRow);
                _tableGrid.Children.Add(messageText);

                _currentRow++;
                Console.WriteLine($"✓ Добавлено сообщение: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении сообщения: {ex.Message}");
            }
        }

        #region Обработчики событий кнопок

        private void InitializeEvents()
        {
            try
            {
                var button1 = this.FindControl<Button>("button1");
                if (button1 != null)
                {
                    button1.Click += Button1_Click;
                }

                var fillButton = this.FindControl<Button>("fill");
                if (fillButton != null)
                {
                    fillButton.Click += Fill_Click;
                }

                var checkBox = this.FindControl<CheckBox>("checkBox_show_3_last_checks");
                if (checkBox != null)
                {
                    checkBox.Click += CheckBox_show_3_last_checks_Click;
                }

                var updateButton = this.FindControl<Button>("btn_update_status_send");
                if (updateButton != null)
                {
                    updateButton.Click += Btn_update_status_send_Click;
                }

                var checkActionsButton = this.FindControl<Button>("btn_check_actions");
                if (checkActionsButton != null)
                {
                    checkActionsButton.Click += Btn_check_actions_Click;
                }

                var image = this.FindControl<Image>("pictureBox_get_update_program");
                if (image != null)
                {
                    image.DoubleTapped += PictureBox_get_update_program_DoubleTapped;
                }

                // Кнопка закрытия в верхнем углу
                var closeButton = this.FindControl<Button>("btnClose");
                if (closeButton != null)
                {
                    closeButton.Click += CloseButton_Click;
                }

                Console.WriteLine("✓ События инициализированы");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при инициализации событий: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик для кнопки "✕" в правом верхнем углу
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Кнопка закрытия (✕) нажата");

            // Вызываем событие закрытия
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Кнопка 'Закрыть' (нижняя) нажата");

            // Вызываем событие закрытия
            RequestClose?.Invoke(this, EventArgs.Empty);

            // Также можно закрыть родительское окно (если открыто отдельно)
            var parentWindow = this.FindAncestorOfType<Window>();
            parentWindow?.Close();
        }

        private void Fill_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Кнопка 'Заполнить' нажата");
            LoadDocuments();
        }

        private void CheckBox_show_3_last_checks_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Чекбокс '3' нажат");
            LoadDocuments();
        }

        private void Btn_update_status_send_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Кнопка 'Обновить' нажата");

            try
            {
                // Обновляем время последней записи чека
                MainStaticClass.Last_Write_Check = DateTime.Now;

                // Запускаем обновление статуса
                Task.Run(() => GetStatusSendDocument());

                Console.WriteLine("✓ Обновление статуса запущено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при обновлении статуса: {ex.Message}");
            }
        }

        private void Btn_check_actions_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Кнопка 'ПРОВЕРКА АКЦИЙ' нажата");

            try
            {
                // Создаем и показываем форму проверки акций
                var verifyActionsForm = new VerifyActions();

                // Находим родительское окно для правильного позиционирования
                Window parentWindow = null;

                // Вариант 1: Через TopLevel
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window currentWindow)
                {
                    parentWindow = currentWindow;
                }

                // Вариант 2: Через Application
                if (parentWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    parentWindow = desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
                }

                // Настройка окна
                verifyActionsForm.Title = "Проверка акций";
                verifyActionsForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Управление поведением окна:
                verifyActionsForm.CanResize = true;
                verifyActionsForm.CanMaximize = true;
                verifyActionsForm.CanMinimize = true;

                // Можно установить минимальный размер окна
                verifyActionsForm.MinWidth = 400;
                verifyActionsForm.MinHeight = 300;

                // Размер окна по умолчанию
                verifyActionsForm.Width = 800;
                verifyActionsForm.Height = 600;

                // Подписываемся на события закрытия окна
                verifyActionsForm.Closed += (s, args) =>
                {
                    Console.WriteLine("Окно проверки акций закрыто");
                };

                // Показываем окно
                if (parentWindow != null)
                {
                    verifyActionsForm.Show();
                }
                else
                {
                    verifyActionsForm.Show();
                }

                Console.WriteLine("✓ Форма проверки акций открыта");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при открытии формы проверки акций: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void PictureBox_get_update_program_DoubleTapped(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Картинка двойной клик");
        }

        #endregion

        /// <summary>
        /// Загрузка документов (синхронный метод для обратной совместимости)
        /// </summary>
        public async void LoadDocuments()
        {
            await LoadDocumentsAsync();
        }
    }
}