using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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

        // Константы для цветов
        private static readonly IBrush SELECTED_ROW_BACKGROUND = Brushes.LightSkyBlue;
        private static readonly IBrush SELECTED_ROW_BORDER = Brushes.DodgerBlue;
        private static readonly IBrush EVEN_ROW_BACKGROUND = Brushes.White;
        private static readonly IBrush ODD_ROW_BACKGROUND = Brushes.AliceBlue;

        private bool newDocument = false;

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
                Loaded += OnLoaded;
                InitializeEvents();

                // 4. Инициализируем кнопку закрытия
                InitializeCloseButton();

                // 5. Подписываемся на глобальные события клавиатуры
                this.AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);

                Console.WriteLine("✓ Конструктор завершен успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ОШИБКА в конструкторе: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("=== Конструктор Cash_checks завершен ===");
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
                    Background = Brushes.White
                };

                // Подписываемся на события Grid
                _tableGrid.PointerPressed += OnTableGridPointerPressed;

                // Определяем колонки Grid
                var columnWidths = new[] { 80, 150, 200, 100, 100, 150, 100, 100, 100, 100 };

                foreach (var width in columnWidths)
                {
                    _tableGrid.ColumnDefinitions.Add(new ColumnDefinition(width, GridUnitType.Pixel));
                }

                // Создаем строку заголовков (строка 0)
                _tableGrid.RowDefinitions.Add(new RowDefinition(35, GridUnitType.Pixel));
                CreateHeaderRow();

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
        /// Создание строки заголовков
        /// </summary>
        private void CreateHeaderRow()
        {
            try
            {
                var headers = new[] { "Удален", "Дата", "Клиент", "Сумма", "Сдача", "Комментарий", "Тип", "Номер", "Напечатан", "ПечатьП" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var headerBorder = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 1, 2),
                        Background = Brushes.LightGray,
                        Child = new TextBlock
                        {
                            Text = headers[i],
                            FontWeight = FontWeight.Bold,
                            Margin = new Thickness(5, 0),
                            VerticalAlignment = VerticalAlignment.Center
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
        /// Добавление строки данных в таблицу
        /// </summary>
        private void AddRowToTable(CheckItem item, int dataRowIndex)
        {
            try
            {
                // Добавляем новую строку в Grid
                _tableGrid.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));

                // Определяем цвет фона строки (чередование)
                var rowBackground = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;

                // Создаем Border для всей строки (для выделения)
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
                Grid.SetRow(rowBorder, _currentRow);
                _tableGrid.Children.Add(rowBorder);

                // Колонка 1: Удален
                AddCellToRow(0, _currentRow, item.ItsDeleted.ToString());

                // Колонка 2: Дата
                AddCellToRow(1, _currentRow, item.DateTimeWrite.ToString("dd.MM.yyyy HH:mm:ss"));

                // Колонка 3: Клиент
                AddCellToRow(2, _currentRow, item.ClientName);

                // Колонка 4: Сумма
                AddCellToRow(3, _currentRow, item.Cash.ToString("N2"), HorizontalAlignment.Right);

                // Колонка 5: Сдача
                AddCellToRow(4, _currentRow, item.Remainder.ToString("N2"), HorizontalAlignment.Right);

                // Колонка 6: Комментарий
                AddCellToRow(5, _currentRow, item.Comment);

                // Колонка 7: Тип
                AddCellToRow(6, _currentRow, item.CheckType);

                // Колонка 8: Номер
                AddCellToRow(7, _currentRow, item.DocumentNumber);

                // Колонка 9: Напечатан
                AddCheckBoxCell(8, _currentRow, item.ItsPrint);

                // Колонка 10: ПечатьП
                AddCheckBoxCell(9, _currentRow, item.ItsPrintP);

                _currentRow++;

                Console.WriteLine($"✓ Добавлена строка {_currentRow - 1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении строки: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавление текстовой ячейки
        /// </summary>
        private void AddCellToRow(int column, int row, string text, HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment
            };

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
        private void SelectRow(int rowIndex)
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

                // Прокручиваем к выделенной строке
                ScrollToRow(gridRowIndex);

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
                    // Восстанавливаем оригинальный стиль строки
                    int dataRowIndex = (int)_selectedRowBorder.Tag;
                    var originalBackground = (dataRowIndex % 2 == 0) ? EVEN_ROW_BACKGROUND : ODD_ROW_BACKGROUND;

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

        /// <summary>
        /// Прокрутка к указанной строке
        /// </summary>
        private void ScrollToRow(int gridRowIndex)
        {
            try
            {
                if (_scrollViewer == null || _tableGrid == null) return;

                // Вычисляем позицию строки
                double rowPosition = 0;
                for (int i = 0; i < gridRowIndex; i++)
                {
                    if (i < _tableGrid.RowDefinitions.Count)
                    {
                        rowPosition += _tableGrid.RowDefinitions[i].Height.Value;
                    }
                }

                // Прокручиваем
                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, rowPosition - 50); // -50 для небольшого отступа сверху
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при прокрутке: {ex.Message}");
            }
        }

        /// <summary>
        /// Перемещение выделения вверх
        /// </summary>
        private void MoveSelectionUp()
        {
            if (_checkItems.Count == 0) return;

            int newIndex = _selectedRowIndex - 1;
            if (newIndex < 0) newIndex = _checkItems.Count - 1; // Циклическое перемещение

            SelectRow(newIndex);
        }

        /// <summary>
        /// Перемещение выделения вниз
        /// </summary>
        private void MoveSelectionDown()
        {
            if (_checkItems.Count == 0) return;

            int newIndex = _selectedRowIndex + 1;
            if (newIndex >= _checkItems.Count) newIndex = 0; // Циклическое перемещение

            SelectRow(newIndex);
        }

        /// <summary>
        /// Обработка выбранного элемента
        /// </summary>
        /// <summary>
        /// Обработка выбранного элемента
        /// </summary>
        private async void ProcessSelectedItem()
        {
            if (_selectedRowIndex >= 0 && _selectedRowIndex < _checkItems.Count)
            {
                var selectedItem = _checkItems[_selectedRowIndex];
                Console.WriteLine($"Выбран чек: {selectedItem.DocumentNumber}, Клиент: {selectedItem.ClientName}, Сумма: {selectedItem.Cash}");

                // Здесь открываем форму с деталями чека
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

                // ИЗМЕНЕНИЕ 1: Создаем CashCheckWindow вместо Cash_check
                var checkWindow = new Cash_check();

                // ИЗМЕНЕНИЕ 2: Передаем параметры (предполагая, что в CashCheckWindow есть аналогичные свойства)
                checkWindow.date_time_write = dateTimeWrite; // Если сохранили свойство
                checkWindow.IsNewCheck = false;

                checkWindow.OnFormLoaded(); // Если сохранили метод

                // Находим активное окно (без изменений)
                Window parentWindow = null;

                // Вариант 1: Через TopLevel (без изменений)
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window currentWindow)
                {
                    parentWindow = currentWindow;
                }

                // Вариант 2: Через Application (без изменений)
                if (parentWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    parentWindow = desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
                }

                // ИЗМЕНЕНИЕ 3: Теперь работаем напрямую с checkWindow, а не создаем новое окно
                checkWindow.Title = $"Чек № {_checkItems[_selectedRowIndex].DocumentNumber} от {_checkItems[_selectedRowIndex].DateTimeWrite:dd.MM.yyyy HH:mm:ss}";
                checkWindow.Width = 1200;
                checkWindow.Height = 800;

                // Дополнительные настройки окна (по желанию)
                checkWindow.CanResize = false;
                checkWindow.CanMaximize = false;
                checkWindow.CanMinimize = false;
                


                // Устанавливаем позиционирование и показываем
                if (parentWindow != null)
                {
                    checkWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // ИЗМЕНЕНИЕ 4: Показываем само checkWindow как диалог
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
                    // Можно обновить список чеков если нужно
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при открытии чека: {ex.Message}");
                await MessageBox.Show($"Ошибка при открытии чека: {ex.Message}");
            }
        }
        
        #region Обработчики событий мыши и клавиатуры

        /// <summary>
        /// Обработка клика по строке
        /// </summary>
        private void OnRowPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is int dataRowIndex)
                {
                    SelectRow(dataRowIndex);
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

        //private async void ProcessInsertKey()
        //{
        //    if (DateTime.Now <= MainStaticClass.GetMinDateWork)
        //    {
        //        await MessageBox.Show(" У ВАС УСТАНОВЛЕНА НЕПРАВИЛЬНАЯ ДАТА НА КОМПЬЮТЕРЕ !!! ДАЛЬНЕЙШАЯ РАБОТА С ЧЕКАМИ НЕВОЗМОЖНА !!!", "Проверка даты на компьютере", MessageBoxButton.OK, MessageBoxType.Error);
        //        return;
        //    }
        //    if (MainStaticClass.CashDeskNumber != 9)
        //    {
        //        bool restart = false; bool errors = false;
        //        MainStaticClass.check_version_fn(ref restart, ref errors);
        //        if (errors)
        //        {
        //            return;
        //        }
        //        if (restart)
        //        {
        //            await MessageBox.Show("У вас неверно была установлена версия ФН,НЕОБХОДИМ ПЕРЕЗАПУСК КАССОВОЙ ПРОГРАММЫ !!!", "Проверка настройки ФН", MessageBoxButton.OK, MessageBoxType.Error);
        //            //this.Close();
        //            return;
        //        }
        //    }
        //    if (MainStaticClass.SystemTaxation == 0)
        //    {
        //        await MessageBox.Show("У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!");
        //        return;
        //    }


        //    //Проверка на заполненность обяз реквизитов
        //    if (await AllIsFilled())
        //    {
        //        if (newDocument)
        //        {
        //            return;
        //        }

        //        if (txtB_cashier.Text.Trim().Length == 0)
        //        {
        //            await MessageBox.Show("Не заполнен кассир");
        //            return;
        //        }

        //        MainStaticClass.validate_date_time_with_fn(15);

        //        newDocument = true;
        //        Cash_check doc = new Cash_check();
        //        doc.cashier = txtB_cashier.Text;
        //        doc.ShowDialog();
        //        doc.Dispose();
        //        newDocument = false;
        //        LoadDocuments();             
        //    }
        //}

        private async void ProcessInsertKey()
        {
            Console.WriteLine("Insert нажат - создание нового чека");

            try
            {
                // Все проверки остаются без изменений...

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

                // Создаем контрол для нового чека (без изменений)
                var checkForm = new Cash_check();

                // Настраиваем для нового чека (пустой) (без изменений)
                checkForm.IsNewCheck = true; // Добавьте это свойство в класс Cash_check
                checkForm.cashier = txtCashier.Text; // Передаем кассира

                // Дополнительная инициализация для нового чека (без изменений)
                checkForm.OnFormLoaded();

                // Находим активное окно (без изменений)
                Window parentWindow = null;

                // Вариант 1: Через TopLevel (без изменений)
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window currentWindow)
                {
                    parentWindow = currentWindow;
                }

                // Вариант 2: Через Application (без изменений)
                if (parentWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    parentWindow = desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
                }

                // === ИЗМЕНЕНИЕ ТОЛЬКО ЗДЕСЬ ===
                // Теперь Cash_check уже является Window, поэтому:
                // 1. Не создаем newWindow
                // 2. Настраиваем само checkForm как окно
                checkForm.Title = "Новый чек";
                checkForm.Width = 1200;
                checkForm.Height = 800;

                // Убираем стандартные кнопки если нужно
                checkForm.CanResize = false;
                checkForm.CanMaximize = false;
                checkForm.CanMinimize = false;

                // Подписываемся на событие закрытия окна
                checkForm.Closed += (s, e) =>
                {
                    // Проверяем результат через Tag
                    bool? dialogResult = checkForm.Tag as bool?;
                    if (dialogResult == true) // Чек успешно создан
                    {
                        LoadDocuments(); // Обновляем список после создания чека
                    }
                };

                // Устанавливаем позиционирование
                if (parentWindow != null)
                {
                    checkForm.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Показываем как диалог
                    await checkForm.ShowDialog(parentWindow);
                }
                else
                {
                    checkForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    checkForm.Show();
                }
                LoadDocuments();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании нового чека: {ex.Message}");
                await MessageBox.Show($"Ошибка при создании нового чека: {ex.Message}");
            }
        }
             
        private async Task<bool> AllIsFilled()
        {
            bool result = true;
            //NpgsqlConnection conn = null;
            //try
            //{
            try
            {
                if (MainStaticClass.Nick_Shop.Trim().Length == 0)
                {
                    await MessageBox.Show("Не заполнен код магазина","Проверка заполнения",MessageBoxButton.OK,MessageBoxType.Error);
                    return false;
                }
                if (MainStaticClass.CashDeskNumber == 0)
                {
                    await MessageBox.Show("Номер кассы не может быть ноль","Проверка заполнения",MessageBoxButton.OK,MessageBoxType.Error);
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

                case Key.Enter:
                    ProcessSelectedItem();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    ClearSelection();
                    e.Handled = true;
                    break;

                case Key.Home:
                    if (_checkItems.Count > 0) SelectRow(0);
                    e.Handled = true;
                    break;

                case Key.End:
                    if (_checkItems.Count > 0) SelectRow(_checkItems.Count - 1);
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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("=== UserControl загружен ===");

            try
            {
                // Инициализация значений контролов
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

                Console.WriteLine("✓ Инициализация завершена");

                // ЗАГРУЖАЕМ ДАННЫЕ ИЗ БД ПРИ ЗАГРУЗКЕ!
                LoadDocuments();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в OnLoaded: {ex.Message}");
            }
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(100); // Небольшая задержка
                this.Focus();

                // Или попробуйте фокус на ScrollViewer
                _scrollViewer?.Focus();

                Console.WriteLine("✓ Фокус установлен после задержки");
            }, DispatcherPriority.Background);
        }

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
        /// Загрузка документов из БД
        /// </summary>
        public async void LoadDocuments()
        {
            try
            {
                Console.WriteLine("=== Загрузка документов из БД ===");

                var checkBox = this.FindControl<CheckBox>("checkBox_show_3_last_checks");
                var datePicker = this.FindControl<DatePicker>("dateTimePicker1");

                if (checkBox == null || datePicker == null)
                {
                    Console.WriteLine("✗ Контролы не найдены!");
                    return;
                }

                // Получаем параметры
                bool showLast3 = checkBox.IsChecked ?? false;
                DateTime selectedDate = datePicker.SelectedDate?.DateTime ?? DateTime.Today;

                Console.WriteLine($"Параметры: showLast3={showLast3}, date={selectedDate:yyyy-MM-dd}");

                var checkItems = new List<CheckItem>();

                // Загружаем данные из БД
                await Task.Run(() =>
                {
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
                                                checkItem.DocumentNumber = decimalValue.ToString("F0"); // Без дробной части
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

                                        checkItems.Add(checkItem);
                                        Console.WriteLine($"  - Чек #{checkItem.DocumentNumber}: {checkItem.ClientName}, сумма: {checkItem.Cash}");
                                    }
                                    Console.WriteLine($"✓ Прочитано {count} строк из БД");
                                }
                            }
                        }
                        Console.WriteLine($"✓ Загружено {checkItems.Count} записей из БД");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при загрузке из БД: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);

                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                        }
                    }
                });

                // Обновляем таблицу в UI потоке
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Общая ошибка в LoadDocuments: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
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
            LoadDocuments();
        }

        private void Btn_check_actions_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Кнопка 'ПРОВЕРКА АКЦИЙ' нажата");
            // Здесь можно добавить вашу логику
        }

        private void PictureBox_get_update_program_DoubleTapped(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Картинка двойной клик");
        }

        #endregion
    }
}