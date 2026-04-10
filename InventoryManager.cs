//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Controls.ApplicationLifetimes;
//using Avalonia.Markup.Xaml;
//using Cash8Avalon;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Cash8Avalon
//{
//    public static class InventoryManager
//    {
//        // ================ ПОТОКОБЕЗОПАСНАЯ РАБОТА СО СЛОВАРЕМ ТОВАРОВ ================

//        /// <summary>
//        /// Основной словарь товаров. Ключом может быть как код товара, так и штрихкод.
//        /// </summary>
//        private static Dictionary<long, ProductData> dictionaryProductData = new Dictionary<long, ProductData>();

//        /// <summary>
//        /// Словарь цен подарков по акциям.
//        /// </summary>
//        private static Dictionary<int, double> giftPriceAction = new Dictionary<int, double>();

//        /// <summary>
//        /// ReaderWriterLockSlim обеспечивает потокобезопасный доступ к словарю:
//        /// - Множество потоков могут одновременно читать (EnterReadLock/ExitReadLock)
//        /// - Только один поток может писать (EnterWriteLock/ExitWriteLock)
//        /// - Во время записи чтение блокируется и наоборот
//        /// </summary>
//        private static readonly ReaderWriterLockSlim _dictionaryLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

//        /// <summary>
//        /// Флаг валидности словаря. 
//        /// Используется для быстрой проверки, можно ли доверять данным в словаре.
//        /// Защищен блокировкой на чтение/запись.
//        /// </summary>
//        private static bool _dictionaryIsValid = false;

//        // Добавляем статическую переменную для хранения владельца MessageBox
//        private static Window _owner = null;

//        /// <summary>
//        /// Свойство для проверки валидности словаря.
//        /// Рекомендуется использовать его для проверки перед чтением данных.
//        /// </summary>
//        public static bool IsDictionaryValid
//        {
//            get
//            {
//                _dictionaryLock.EnterReadLock();
//                try
//                {
//                    return _dictionaryIsValid;
//                }
//                finally
//                {
//                    _dictionaryLock.ExitReadLock();
//                }
//            }
//        }

//        // Метод для установки владельца (вызывается из основного окна при инициализации)
//        public static void SetOwnerWindow(Window owner)
//        {
//            _owner = owner;
//        }

//        // Метод для получения владельца (всегда должен возвращать окно)
//        private static Window GetOwnerWindow()
//        {
//            if (_owner != null)
//                return _owner;

//            // Если владелец не установлен, пытаемся найти главное окно
//            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
//            {
//                return desktop.MainWindow;
//            }

//            return null;
//        }

//        /// <summary>
//        /// Очистка словаря товаров.
//        /// Безопасно выполняется под WriteLock, чтобы другие потоки не могли 
//        /// читать словарь во время его очистки.
//        /// </summary>
//        public static void ClearDictionaryProductData()
//        {
//            // WriteLock гарантирует, что никто не читает словарь в момент его очистки
//            _dictionaryLock.EnterWriteLock();
//            try
//            {
//                _dictionaryIsValid = false;
//                dictionaryProductData.Clear();
//                giftPriceAction.Clear();
//            }
//            finally
//            {
//                _dictionaryLock.ExitWriteLock();
//            }
//        }

//        public static async Task FillDictionaryProductDataAsync(Window parentWindow = null)
//        {
//            try
//            {
//                await Task.Run(() =>
//                {
//                    FillDictionaryProductData();
//                });
//            }
//            catch (Exception ex)
//            {
//                // Показываем MessageBox в UI потоке
//                ShowErrorDialogAsync(parentWindow, ex);
//            }
//        }

//        private static void ShowErrorDialogAsync(Window parentWindow, Exception ex)
//        {
//            // Используем Dispatcher для гарантированного выполнения в UI потоке
//            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
//            {
//                try
//                {
//                    // Определяем окно-владельца: сначала parentWindow, затем глобальный _owner
//                    Window ownerWindow = parentWindow ?? GetOwnerWindow();

//                    if (ownerWindow != null && ownerWindow.IsVisible)
//                    {
//                        // Показываем MessageBox с владельцем - порядок параметров: (message, title, button, type, owner)
//                        await MessageBox.Show(
//                            $"Произошла ошибка: {ex.Message}",
//                            "Работа с кешем товаров",
//                            MessageBoxButton.OK,
//                            MessageBoxType.Error,
//                            ownerWindow);
//                    }
//                    else
//                    {
//                        // Если не удалось получить окно-владельца, ищем главное окно
//                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
//                        {
//                            var mainWindow = desktop.MainWindow;
//                            if (mainWindow != null && mainWindow.IsVisible)
//                            {
//                                await MessageBox.Show(
//                                    $"Произошла ошибка: {ex.Message}",
//                                    "Работа с кешем товаров",
//                                    MessageBoxButton.OK,
//                                    MessageBoxType.Error,
//                                    mainWindow);
//                            }
//                            else
//                            {
//                                // В крайнем случае показываем без владельца
//                                await MessageBox.Show(
//                                    $"Произошла ошибка: {ex.Message}",
//                                    "Работа с кешем товаров",
//                                    MessageBoxButton.OK,
//                                    MessageBoxType.Error);
//                            }
//                        }
//                        else
//                        {
//                            // Fallback: без владельца
//                            await MessageBox.Show(
//                                $"Произошла ошибка: {ex.Message}",
//                                "Работа с кешем товаров",
//                                MessageBoxButton.OK,
//                                MessageBoxType.Error);
//                        }
//                    }
//                }
//                catch (Exception dialogEx)
//                {
//                    Console.WriteLine($"Ошибка при отображении диалога: {dialogEx.Message}");
//                }
//            });
//        }

//        public static Dictionary<int, double> DictionaryPriceGiftAction
//        {
//            get
//            {
//                // Ленивая инициализация
//                if ((giftPriceAction == null) || (giftPriceAction.Count == 0))
//                {
//                    giftPriceAction = GetPriceGiftAction();
//                }
//                return giftPriceAction;
//            }
//        }

//        private static Dictionary<int, double> GetPriceGiftAction()
//        {
//            var result = new Dictionary<int, double>();

//            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
//            {
//                try
//                {
//                    conn.Open();
//                    string query = "SELECT num_doc, gift_price FROM action_header";
//                    using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
//                    using (NpgsqlDataReader reader = command.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            int numDoc = reader.GetInt32(reader.GetOrdinal("num_doc"));
//                            double giftPriceValue = Convert.ToDouble(reader.GetDecimal(reader.GetOrdinal("gift_price")));
//                            result[numDoc] = giftPriceValue;
//                        }
//                    }
//                }
//                catch (NpgsqlException ex)
//                {
//                    // Используем MessageBox с владельцем для ошибок
//                    ShowLogErrorDialogAsync("Заполнение словаря ценами подарков из акций", ex);
//                    return new Dictionary<int, double>();
//                }
//                catch (Exception ex)
//                {
//                    // Используем MessageBox с владельцем для ошибок
//                    ShowLogErrorDialogAsync("Заполнение словаря ценами подарков из акций", ex);
//                    return new Dictionary<int, double>();
//                }
//            }

//            return result;
//        }

//        // Новый метод для показа ошибок логирования с владельцем
//        private static void ShowLogErrorDialogAsync(string context, Exception ex)
//        {
//            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
//            {
//                try
//                {
//                    Window ownerWindow = GetOwnerWindow();

//                    if (ownerWindow != null && ownerWindow.IsVisible)
//                    {
//                        await MessageBox.Show(
//                            $"Ошибка в {context}: {ex.Message}",
//                            "Ошибка логирования",
//                            MessageBoxButton.OK,
//                            MessageBoxType.Error,
//                            ownerWindow);
//                    }
//                    else if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
//                    {
//                        var mainWindow = desktop.MainWindow;
//                        if (mainWindow != null && mainWindow.IsVisible)
//                        {
//                            await MessageBox.Show(
//                                $"Ошибка в {context}: {ex.Message}",
//                                "Ошибка логирования",
//                                MessageBoxButton.OK,
//                                MessageBoxType.Error,
//                                mainWindow);
//                        }
//                        else
//                        {
//                            await MessageBox.Show(
//                                $"Ошибка в {context}: {ex.Message}",
//                                "Ошибка логирования",
//                                MessageBoxButton.OK,
//                                MessageBoxType.Error);
//                        }
//                    }
//                    else
//                    {
//                        await MessageBox.Show(
//                            $"Ошибка в {context}: {ex.Message}",
//                            "Ошибка логирования",
//                            MessageBoxButton.OK,
//                            MessageBoxType.Error);
//                    }
//                }
//                catch
//                {
//                    // Игнорируем ошибки при показе диалога ошибки
//                }
//            });
//        }

//        /// <summary>
//        /// Заполняет словарь товаров данными из базы.
//        /// Используется паттерн "Копирование на запись" (Copy-on-Write):
//        /// 1. Создаем временный словарь
//        /// 2. Заполняем его данными
//        /// 3. Под блокировкой заменяем ссылку на основной словарь
//        /// 4. Устанавливаем флаги валидности
//        /// 
//        /// Преимущества:
//        /// - Минимальное время блокировки (только на замену ссылки)
//        /// - Читающие потоки не блокируются на время заполнения
//        /// - Атомарная замена словаря
//        /// </summary>
//        public static bool FillDictionaryProductData()
//        {
//            // Создаем временный словарь для заполнения
//            var tempDictionary = new Dictionary<long, ProductData>();

//            using (var conn = MainStaticClass.NpgsqlConn())
//            {
//                try
//                {
//                    conn.Open();

//                    string query = @"
//                SELECT tovar.code, tovar.name, tovar.retail_price, tovar.its_certificate, 
//                       tovar.its_marked, tovar.cdn_check, tovar.fractional, barcode.barcode,tovar.refusal_of_marking,tovar.rr_not_control_owner 
//                FROM tovar 
//                LEFT JOIN barcode ON tovar.code = barcode.tovar_code 
//                WHERE tovar.its_deleted = 0 AND tovar.retail_price <> 0";

//                    using (var command = new NpgsqlCommand(query, conn))
//                    using (var reader = command.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            long code = Convert.ToInt64(reader["code"]);
//                            string name = reader["name"].ToString().Trim();
//                            decimal retailPrice = Convert.ToDecimal(reader["retail_price"]);
//                            string barcode = reader["barcode"]?.ToString().Trim();

//                            // Создаем флаги на основе значений из базы данных
//                            ProductFlags flags = ProductFlags.None;
//                            if (Convert.ToBoolean(reader["its_certificate"])) flags |= ProductFlags.Certificate;
//                            if (Convert.ToBoolean(reader["its_marked"])) flags |= ProductFlags.Marked;
//                            if (Convert.ToBoolean(reader["refusal_of_marking"])) flags |= ProductFlags.RefusalMarking;
//                            if (Convert.ToBoolean(reader["cdn_check"])) flags |= ProductFlags.CDNCheck;
//                            if (Convert.ToBoolean(reader["fractional"])) flags |= ProductFlags.Fractional;
//                            if (Convert.ToBoolean(reader["rr_not_control_owner"])) flags |= ProductFlags.RrNotControlOwner;

//                            var productData = new ProductData(code, name, retailPrice, flags);

//                            // Добавляем во временный словарь по коду товара
//                            tempDictionary[code] = productData;

//                            // Добавляем во временный словарь по штрихкоду
//                            if (!string.IsNullOrEmpty(barcode) && long.TryParse(barcode, out var barcodeValue))
//                            {
//                                tempDictionary[barcodeValue] = productData;
//                            }
//                        }
//                    }

//                    // ===== КРИТИЧЕСКАЯ СЕКЦИЯ - минимальное время блокировки =====
//                    // Блокируем словарь только для атомарной замены ссылки
//                    _dictionaryLock.EnterWriteLock();
//                    try
//                    {
//                        // Атомарно заменяем ссылку на словарь
//                        dictionaryProductData = tempDictionary;

//                        // Устанавливаем флаг валидности
//                        _dictionaryIsValid = true;
//                    }
//                    finally
//                    {
//                        _dictionaryLock.ExitWriteLock();
//                    }
//                    // ===== КОНЕЦ КРИТИЧЕСКОЙ СЕКЦИИ =====

//                    return true;
//                }
//                catch (NpgsqlException ex)
//                {
//                    // В случае ошибки сбрасываем флаг валидности под блокировкой
//                    _dictionaryLock.EnterWriteLock();
//                    try
//                    {
//                        _dictionaryIsValid = false;
//                    }
//                    finally
//                    {
//                        _dictionaryLock.ExitWriteLock();
//                    }

//                    // Показываем ошибку с владельцем
//                    ShowErrorDialogAsync(null, new Exception($"Ошибка при заполнении словаря данными о товарах: {ex.Message}", ex));

//                    throw new Exception($"Ошибка при заполнении словаря данными о товарах: {ex.Message}", ex);
//                }
//                catch (Exception ex)
//                {
//                    // В случае ошибки сбрасываем флаг валидности под блокировкой
//                    _dictionaryLock.EnterWriteLock();
//                    try
//                    {
//                        _dictionaryIsValid = false;
//                    }
//                    finally
//                    {
//                        _dictionaryLock.ExitWriteLock();
//                    }

//                    // Показываем ошибку с владельцем
//                    ShowErrorDialogAsync(null, ex);

//                    throw new Exception($"Ошибка при заполнении словаря данными о товарах: {ex.Message}", ex);
//                }
//            }
//        }

//        /// <summary>
//        /// Добавляет новый товар в словарь.
//        /// Выполняется под WriteLock для безопасности.
//        /// </summary>
//        public static void AddItem(long id, ProductData data)
//        {
//            _dictionaryLock.EnterWriteLock();
//            try
//            {
//                if (!dictionaryProductData.ContainsKey(id))
//                {
//                    dictionaryProductData.Add(id, data);
//                }
//                else
//                {
//                    throw new ArgumentException($"Товар с идентификатором {id} уже существует.");
//                }
//            }
//            finally
//            {
//                _dictionaryLock.ExitWriteLock();
//            }
//        }

//        /// <summary>
//        /// Получает товар из словаря по идентификатору (код или штрихкод).
//        /// Выполняется под ReadLock, что позволяет множеству потоков 
//        /// одновременно читать данные.
//        /// </summary>
//        public static ProductData GetItem(long id)
//        {
//            // EnterReadLock позволяет множеству потоков одновременно читать
//            _dictionaryLock.EnterReadLock();
//            try
//            {
//                // Проверяем валидность словаря
//                if (!_dictionaryIsValid)
//                {
//                    return new ProductData(0, string.Empty, 0, ProductFlags.None);
//                }

//                return dictionaryProductData.TryGetValue(id, out var data)
//                    ? data
//                    : new ProductData(0, string.Empty, 0, ProductFlags.None);
//            }
//            finally
//            {
//                // Всегда освобождаем блокировку
//                _dictionaryLock.ExitReadLock();
//            }
//        }

//        /// <summary>
//        /// Безопасно пытается получить товар из словаря.
//        /// Возвращает true, если товар найден и словарь валиден.
//        /// Рекомендуемый метод получения товаров.
//        /// </summary>
//        public static bool TryGetItem(long id, out ProductData data)
//        {
//            _dictionaryLock.EnterReadLock();
//            try
//            {
//                if (!_dictionaryIsValid)
//                {
//                    data = new ProductData(0, string.Empty, 0, ProductFlags.None);
//                    return false;
//                }

//                return dictionaryProductData.TryGetValue(id, out data);
//            }
//            finally
//            {
//                _dictionaryLock.ExitReadLock();
//            }
//        }

//        /// <summary>
//        /// Получает копию текущего словаря.
//        /// Полезно для операций, требующих консистентного снимка данных.
//        /// </summary>
//        public static Dictionary<long, ProductData> GetDictionarySnapshot()
//        {
//            _dictionaryLock.EnterReadLock();
//            try
//            {
//                // Создаем копию словаря для безопасной работы вне блокировки
//                return new Dictionary<long, ProductData>(dictionaryProductData);
//            }
//            finally
//            {
//                _dictionaryLock.ExitReadLock();
//            }
//        }

//        /// <summary>
//        /// Поиск товара по штрихкоду или коду с автоматическим определением источника
//        /// Для обратной совместимости возвращает ProductData (пустой товар с кодом 0, если не найден)
//        /// </summary>
//        public static async Task<ProductData> FindProductAsync(string barcode, Window ownerWindow = null)
//        {
//            try
//            {
//                // Проверка на пустой ввод
//                if (string.IsNullOrWhiteSpace(barcode))
//                {
//                    return new ProductData(0, "", 0, ProductFlags.None);
//                }

//                // Пробуем преобразовать в число
//                if (!long.TryParse(barcode, out long code))
//                {
//                    return new ProductData(0, "", 0, ProductFlags.None);
//                }

//                // Сначала ищем в кэше
//                if (TryGetItem(code, out ProductData cachedProduct))
//                {
//                    return cachedProduct;
//                }

//                // Если в кэше нет, ищем в БД
//                var dbProduct = await FindProductInDatabaseAsync(barcode, ownerWindow);

//                if (dbProduct != null && dbProduct.Code != 0)
//                {
//                    // Добавляем найденный товар в кэш для будущих запросов
//                    AddOrUpdateItem(code, dbProduct);

//                    // Также добавляем по штрихкоду, если он отличается от кода
//                    if (code.ToString() != barcode && long.TryParse(barcode, out long barcodeLong))
//                    {
//                        AddOrUpdateItem(barcodeLong, dbProduct);
//                    }

//                    return dbProduct;
//                }

//                // Товар не найден
//                return new ProductData(0, "", 0, ProductFlags.None);
//            }
//            catch (Exception ex)
//            {
//                // Логируем ошибку и показываем сообщение
//                await ShowSearchErrorAsync(ex, ownerWindow);
//                return new ProductData(0, "", 0, ProductFlags.None);
//            }
//        }

//        /// <summary>
//        /// Поиск товара напрямую в БД
//        /// </summary>
//        private static async Task<ProductData> FindProductInDatabaseAsync(string barcode, Window ownerWindow)
//        {
//            NpgsqlConnection conn = null;

//            try
//            {
//                conn = MainStaticClass.NpgsqlConn();
//                await conn.OpenAsync();

//                string query;

//                if (barcode.Length > 6)
//                {
//                    // Поиск по штрихкоду
//                    query = @"
//                        SELECT tovar.code, tovar.name, tovar.retail_price, 
//                               tovar.its_certificate, tovar.its_marked, tovar.cdn_check, 
//                               tovar.fractional, tovar.refusal_of_marking, tovar.rr_not_control_owner 
//                        FROM barcode 
//                        LEFT JOIN tovar ON barcode.tovar_code = tovar.code 
//                        WHERE barcode.barcode = @barcode 
//                          AND tovar.its_deleted = 0 
//                          AND tovar.retail_price <> 0";
//                }
//                else
//                {
//                    // Поиск по коду товара
//                    query = @"
//                        SELECT tovar.code, tovar.name, tovar.retail_price, 
//                               tovar.its_certificate, tovar.its_marked, tovar.cdn_check, 
//                               tovar.fractional, tovar.refusal_of_marking, tovar.rr_not_control_owner 
//                        FROM tovar 
//                        WHERE tovar.its_deleted = 0 
//                          AND tovar.retail_price <> 0 
//                          AND tovar.code = @barcode";
//                }

//                using (var command = new NpgsqlCommand(query, conn))
//                {
//                    if (barcode.Length > 6)
//                    {
//                        command.Parameters.AddWithValue("@barcode", barcode);
//                    }
//                    else
//                    {
//                        command.Parameters.AddWithValue("@barcode", Convert.ToInt64(barcode));
//                    }

//                    using (var reader = await command.ExecuteReaderAsync())
//                    {
//                        if (await reader.ReadAsync())
//                        {
//                            return CreateProductDataFromReader(reader);
//                        }
//                    }
//                }

//                // Товар не найден
//                return new ProductData(0, "", 0, ProductFlags.None);
//            }
//            catch (Exception ex)
//            {
//                throw new Exception($"Ошибка при поиске товара в БД: {ex.Message}", ex);
//            }
//            finally
//            {
//                if (conn?.State == System.Data.ConnectionState.Open)
//                {
//                    await conn.CloseAsync();
//                }
//            }
//        }

//        /// <summary>
//        /// Создание ProductData из DataReader
//        /// </summary>
//        private static ProductData CreateProductDataFromReader(NpgsqlDataReader reader)
//        {
//            long code = Convert.ToInt64(reader["code"]);
//            string name = reader["name"].ToString().Trim();
//            decimal price = Convert.ToDecimal(reader["retail_price"]);

//            ProductFlags flags = ProductFlags.None;
//            if (Convert.ToBoolean(reader["its_certificate"])) flags |= ProductFlags.Certificate;
//            if (Convert.ToBoolean(reader["its_marked"])) flags |= ProductFlags.Marked;
//            if (Convert.ToBoolean(reader["refusal_of_marking"])) flags |= ProductFlags.RefusalMarking;
//            if (Convert.ToBoolean(reader["rr_not_control_owner"])) flags |= ProductFlags.RrNotControlOwner;
//            if (Convert.ToBoolean(reader["cdn_check"])) flags |= ProductFlags.CDNCheck;
//            if (Convert.ToBoolean(reader["fractional"])) flags |= ProductFlags.Fractional;

//            return new ProductData(code, name, price, flags);
//        }

//        /// <summary>
//        /// Добавление или обновление товара в кэше
//        /// </summary>
//        private static void AddOrUpdateItem(long id, ProductData data)
//        {
//            _dictionaryLock.EnterWriteLock();
//            try
//            {
//                dictionaryProductData[id] = data;
//            }
//            finally
//            {
//                _dictionaryLock.ExitWriteLock();
//            }
//        }

//        /// <summary>
//        /// Показать ошибку поиска
//        /// </summary>
//        private static async Task ShowSearchErrorAsync(Exception ex, Window ownerWindow)
//        {
//            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
//            {
//                try
//                {
//                    var owner = ownerWindow ?? GetOwnerWindow();
//                    await MessageBox.Show(
//                        $"Ошибка при поиске товара: {ex.Message}",
//                        "Поиск товара",
//                        MessageBoxButton.OK,
//                        MessageBoxType.Error,
//                        owner);
//                }
//                catch
//                {
//                    // Игнорируем ошибки при показе диалога
//                }
//            });
//        }
//    }
//}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public static class InventoryManager
    {
        // ================ ПОТОКОБЕЗОПАСНАЯ РАБОТА СО СЛОВАРЯМИ ================

        /// <summary>
        /// Основной словарь товаров. Ключом может быть как код товара, так и штрихкод.
        /// </summary>
        private static Dictionary<long, ProductData> dictionaryProductData = new Dictionary<long, ProductData>();

        /// <summary>
        /// Словарь цен подарков по акциям. volatile для безопасной проверки null.
        /// </summary>
        private static volatile Dictionary<int, double> giftPriceAction = null;

        /// <summary>
        /// Флаг, указывающий что giftPriceAction уже загружен (даже если пустой из-за ошибки БД)
        /// </summary>
        private static volatile bool giftPriceActionInitialized = false;

        /// <summary>
        /// ReaderWriterLockSlim обеспечивает потокобезопасный доступ к словарю товаров.
        /// </summary>
        private static readonly ReaderWriterLockSlim _dictionaryLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Отдельная блокировка для словаря подарков, чтобы не блокировать товары при работе с подарками.
        /// </summary>
        private static readonly object _giftPriceLock = new object();

        /// <summary>
        /// Флаг валидности словаря товаров.
        /// </summary>
        private static bool _dictionaryIsValid = false;

        // Владелец MessageBox
        private static Window _owner = null;

        /// <summary>
        /// Свойство для проверки валидности словаря товаров.
        /// </summary>
        public static bool IsDictionaryValid
        {
            get
            {
                _dictionaryLock.EnterReadLock();
                try
                {
                    return _dictionaryIsValid;
                }
                finally
                {
                    _dictionaryLock.ExitReadLock();
                }
            }
        }

        public static void SetOwnerWindow(Window owner)
        {
            _owner = owner;
        }

        private static Window GetOwnerWindow()
        {
            if (_owner != null)
                return _owner;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }

            return null;
        }

        /// <summary>
        /// Очистка ВСЕХ словарей.
        /// </summary>
        public static void ClearDictionaryProductData()
        {
            // Очищаем основной словарь
            _dictionaryLock.EnterWriteLock();
            try
            {
                _dictionaryIsValid = false;
                dictionaryProductData.Clear();
            }
            finally
            {
                _dictionaryLock.ExitWriteLock();
            }

            // Очищаем словарь подарков под отдельной блокировкой
            lock (_giftPriceLock)
            {
                giftPriceAction = null;
                giftPriceActionInitialized = false;
            }
        }

        /// <summary>
        /// Получает цену подарка для указанной акции.
        /// Сначала ищет в кэше, при отсутствии — делает запрос к БД (fallback).
        /// </summary>
        /// <param name="numDoc">Номер документа акции</param>
        /// <returns>Строковое представление цены подарка</returns>
        public static string GetGiftPrice(int numDoc)
        {
            // 1. Быстрая проверка кэша
            if (TryGetGiftPrice(numDoc, out double cachedPrice))
            {
                return cachedPrice.ToString();
            }

            // 2. Если в кэше нет (например, при загрузке пропустили из-за фильтра > 0), 
            // идем напрямую в базу данных
            try
            {
                using (var connection = MainStaticClass.NpgsqlConn())
                {
                    const string query = @"
                SELECT gift_price 
                FROM action_header 
                WHERE num_doc = @numDoc 
                  AND tip IN (1,2,3,4,5,6,8)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@numDoc", numDoc);
                        connection.Open();
                        var result = command.ExecuteScalar();
                        return Convert.ToDecimal(result ?? 0m).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // ВАЖНО: Здесь мы ТОЛЬКО логируем ошибку. 
                // НЕТ MessageBox.Show! Иначе при сработке акции у кассира зависнет экран.
                MainStaticClass.WriteRecordErrorLog(
                    ex, numDoc, MainStaticClass.CashDeskNumber, "Получение цены подарка (Fallback в БД)");

                return "0"; // Возвращаем безопасное значение по умолчанию
            }
        }

        /// <summary>
        /// Асинхронная обёртка без дублирования ошибок
        /// </summary>
        public static async Task<bool> FillDictionaryProductDataAsync(Window parentWindow = null)
        {
            try
            {
                return await Task.Run(() => FillDictionaryProductData());
            }
            catch (Exception ex)
            {
                // Показываем ошибку только ОДИН раз - здесь
                ShowErrorDialog(parentWindow ?? GetOwnerWindow(), $"Ошибка при заполнении кэша товаров: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ПОТОКОБЕЗОПАСНОЕ получение словаря цен подарков (Double-check locking)
        /// </summary>
        public static Dictionary<int, double> DictionaryPriceGiftAction
        {
            get
            {
                // Быстрая проверка без блокировки
                if (giftPriceActionInitialized && giftPriceAction != null)
                {
                    return giftPriceAction;
                }

                // Двойная проверка с блокировкой
                lock (_giftPriceLock)
                {
                    if (giftPriceActionInitialized && giftPriceAction != null)
                    {
                        return giftPriceAction;
                    }

                    // Загружаем данные
                    giftPriceAction = LoadGiftPriceActionFromDatabase();
                    giftPriceActionInitialized = true;  // Отмечаем, что попытка загрузки была

                    return giftPriceAction;
                }
            }
        }

        /// <summary>
        /// Проверяет наличие цены подарка для конкретной акции (рекомендуемый метод)
        /// </summary>
        public static bool TryGetGiftPrice(int numDoc, out double price)
        {
            var dict = DictionaryPriceGiftAction; // Потокобезопасное получение
            return dict.TryGetValue(numDoc, out price);
        }

        /// <summary>
        /// Загрузка цен подарков из БД (внутренний метод)
        /// </summary>
        private static Dictionary<int, double> LoadGiftPriceActionFromDatabase()
        {
            var result = new Dictionary<int, double>();

            try
            {
                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    // Добавлена фильтрация, чтобы не грузить пустые цены
                    string query = "SELECT num_doc, gift_price FROM action_header WHERE gift_price IS NOT NULL AND gift_price > 0";

                    using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int numDoc = reader.GetInt32(reader.GetOrdinal("num_doc"));
                            double giftPriceValue = Convert.ToDouble(reader.GetDecimal(reader.GetOrdinal("gift_price")));
                            result[numDoc] = giftPriceValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем, но не бросаем исключение - возвращаем пустой словарь
                Console.WriteLine($"Ошибка загрузки цен подарков: {ex.Message}");
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Загрузка цен подарков");
            }

            return result;
        }

        /// <summary>
        /// Синхронное заполнение словаря товаров (БЕЗ показа ошибок - они обрабатываются в Async-обёртке)
        /// </summary>
        public static bool FillDictionaryProductData()
        {
            var tempDictionary = new Dictionary<long, ProductData>();

            using (var conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    conn.Open();

                    string query = @"
                        SELECT tovar.code, tovar.name, tovar.retail_price, tovar.its_certificate, 
                               tovar.its_marked, tovar.cdn_check, tovar.fractional, barcode.barcode,
                               tovar.refusal_of_marking, tovar.rr_not_control_owner 
                        FROM tovar 
                        LEFT JOIN barcode ON tovar.code = barcode.tovar_code 
                        WHERE tovar.its_deleted = 0 AND tovar.retail_price <> 0";

                    using (var command = new NpgsqlCommand(query, conn))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long code = Convert.ToInt64(reader["code"]);
                            string name = reader["name"].ToString().Trim();
                            decimal retailPrice = Convert.ToDecimal(reader["retail_price"]);
                            string barcode = reader["barcode"]?.ToString().Trim();

                            ProductFlags flags = ProductFlags.None;
                            if (Convert.ToBoolean(reader["its_certificate"])) flags |= ProductFlags.Certificate;
                            if (Convert.ToBoolean(reader["its_marked"])) flags |= ProductFlags.Marked;
                            if (Convert.ToBoolean(reader["refusal_of_marking"])) flags |= ProductFlags.RefusalMarking;
                            if (Convert.ToBoolean(reader["cdn_check"])) flags |= ProductFlags.CDNCheck;
                            if (Convert.ToBoolean(reader["fractional"])) flags |= ProductFlags.Fractional;
                            if (Convert.ToBoolean(reader["rr_not_control_owner"])) flags |= ProductFlags.RrNotControlOwner;

                            var productData = new ProductData(code, name, retailPrice, flags);

                            tempDictionary[code] = productData;

                            if (!string.IsNullOrEmpty(barcode) && long.TryParse(barcode, out var barcodeValue))
                            {
                                tempDictionary[barcodeValue] = productData;
                            }
                        }
                    }

                    // Атомарная замена под блокировкой
                    _dictionaryLock.EnterWriteLock();
                    try
                    {
                        dictionaryProductData = tempDictionary;
                        _dictionaryIsValid = true;
                    }
                    finally
                    {
                        _dictionaryLock.ExitWriteLock();
                    }

                    return true;
                }
                catch (Exception)
                {
                    // Сбрасываем флаг валидности
                    _dictionaryLock.EnterWriteLock();
                    try
                    {
                        _dictionaryIsValid = false;
                    }
                    finally
                    {
                        _dictionaryLock.ExitWriteLock();
                    }

                    // НЕ показываем ошибку здесь - она будет показана в FillDictionaryProductDataAsync
                    throw;
                }
            }
        }

        /// <summary>
        /// Добавляет новый товар в словарь.
        /// </summary>
        public static void AddItem(long id, ProductData data)
        {
            _dictionaryLock.EnterWriteLock();
            try
            {
                if (!dictionaryProductData.ContainsKey(id))
                {
                    dictionaryProductData.Add(id, data);
                }
                else
                {
                    throw new ArgumentException($"Товар с идентификатором {id} уже существует.");
                }
            }
            finally
            {
                _dictionaryLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Получает товар из словаря по идентификатору (код или штрихкод).
        /// </summary>
        public static ProductData GetItem(long id)
        {
            _dictionaryLock.EnterReadLock();
            try
            {
                if (!_dictionaryIsValid)
                {
                    return new ProductData(0, string.Empty, 0, ProductFlags.None);
                }

                return dictionaryProductData.TryGetValue(id, out var data)
                    ? data
                    : new ProductData(0, string.Empty, 0, ProductFlags.None);
            }
            finally
            {
                _dictionaryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Безопасно пытается получить товар из словаря.
        /// </summary>
        public static bool TryGetItem(long id, out ProductData data)
        {
            _dictionaryLock.EnterReadLock();
            try
            {
                if (!_dictionaryIsValid)
                {
                    data = new ProductData(0, string.Empty, 0, ProductFlags.None);
                    return false;
                }

                return dictionaryProductData.TryGetValue(id, out data);
            }
            finally
            {
                _dictionaryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Получает копию текущего словаря товаров.
        /// </summary>
        public static Dictionary<long, ProductData> GetDictionarySnapshot()
        {
            _dictionaryLock.EnterReadLock();
            try
            {
                return new Dictionary<long, ProductData>(dictionaryProductData);
            }
            finally
            {
                _dictionaryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Поиск товара по штрихкоду или коду с автоматическим определением источника
        /// </summary>
        public static async Task<ProductData> FindProductAsync(string barcode, Window ownerWindow = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    return new ProductData(0, "", 0, ProductFlags.None);
                }

                if (!long.TryParse(barcode, out long code))
                {
                    return new ProductData(0, "", 0, ProductFlags.None);
                }

                // Сначала ищем в кэше
                if (TryGetItem(code, out ProductData cachedProduct))
                {
                    return cachedProduct;
                }

                // Если в кэше нет, ищем в БД
                var dbProduct = await FindProductInDatabaseAsync(barcode, ownerWindow);

                if (dbProduct != null && dbProduct.Code != 0)
                {
                    // Добавляем найденный товар в кэш
                    AddOrUpdateItem(code, dbProduct);

                    // Также добавляем по штрихкоду, если он отличается от кода
                    if (code.ToString() != barcode && long.TryParse(barcode, out long barcodeLong))
                    {
                        AddOrUpdateItem(barcodeLong, dbProduct);
                    }

                    return dbProduct;
                }

                return new ProductData(0, "", 0, ProductFlags.None);
            }
            catch (Exception ex)
            {
                await ShowSearchErrorAsync(ex, ownerWindow);
                return new ProductData(0, "", 0, ProductFlags.None);
            }
        }

        /// <summary>
        /// Поиск товара напрямую в БД
        /// </summary>
        private static async Task<ProductData> FindProductInDatabaseAsync(string barcode, Window ownerWindow)
        {
            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                await conn.OpenAsync();

                string query;

                if (barcode.Length > 6)
                {
                    query = @"
                        SELECT tovar.code, tovar.name, tovar.retail_price, 
                               tovar.its_certificate, tovar.its_marked, tovar.cdn_check, 
                               tovar.fractional, tovar.refusal_of_marking, tovar.rr_not_control_owner 
                        FROM barcode 
                        LEFT JOIN tovar ON barcode.tovar_code = tovar.code 
                        WHERE barcode.barcode = @barcode 
                          AND tovar.its_deleted = 0 
                          AND tovar.retail_price <> 0";
                }
                else
                {
                    query = @"
                        SELECT tovar.code, tovar.name, tovar.retail_price, 
                               tovar.its_certificate, tovar.its_marked, tovar.cdn_check, 
                               tovar.fractional, tovar.refusal_of_marking, tovar.rr_not_control_owner 
                        FROM tovar 
                        WHERE tovar.its_deleted = 0 
                          AND tovar.retail_price <> 0 
                          AND tovar.code = @barcode";
                }

                using (var command = new NpgsqlCommand(query, conn))
                {
                    if (barcode.Length > 6)
                    {
                        command.Parameters.AddWithValue("@barcode", barcode);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@barcode", Convert.ToInt64(barcode));
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return CreateProductDataFromReader(reader);
                        }
                    }
                }

                return new ProductData(0, "", 0, ProductFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при поиске товара в БД: {ex.Message}", ex);
            }
            finally
            {
                if (conn?.State == System.Data.ConnectionState.Open)
                {
                    await conn.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Создание ProductData из DataReader
        /// </summary>
        private static ProductData CreateProductDataFromReader(NpgsqlDataReader reader)
        {
            long code = Convert.ToInt64(reader["code"]);
            string name = reader["name"].ToString().Trim();
            decimal price = Convert.ToDecimal(reader["retail_price"]);

            ProductFlags flags = ProductFlags.None;
            if (Convert.ToBoolean(reader["its_certificate"])) flags |= ProductFlags.Certificate;
            if (Convert.ToBoolean(reader["its_marked"])) flags |= ProductFlags.Marked;
            if (Convert.ToBoolean(reader["refusal_of_marking"])) flags |= ProductFlags.RefusalMarking;
            if (Convert.ToBoolean(reader["rr_not_control_owner"])) flags |= ProductFlags.RrNotControlOwner;
            if (Convert.ToBoolean(reader["cdn_check"])) flags |= ProductFlags.CDNCheck;
            if (Convert.ToBoolean(reader["fractional"])) flags |= ProductFlags.Fractional;

            return new ProductData(code, name, price, flags);
        }

        /// <summary>
        /// Добавление или обновление товара в кэше
        /// </summary>
        private static void AddOrUpdateItem(long id, ProductData data)
        {
            _dictionaryLock.EnterWriteLock();
            try
            {
                dictionaryProductData[id] = data;
            }
            finally
            {
                _dictionaryLock.ExitWriteLock();
            }
        }

        // ================ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ UI ================

        private static void ShowErrorDialog(Window ownerWindow, string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    var owner = ownerWindow ?? GetOwnerWindow();
                    await MessageBox.Show(
                        message,
                        "Работа с кешем товаров",
                        MessageBoxButton.OK,
                        MessageBoxType.Error,
                        owner);
                }
                catch
                {
                    // Игнорируем ошибки при показе диалога ошибки
                }
            });
        }

        private static async Task ShowSearchErrorAsync(Exception ex, Window ownerWindow)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    var owner = ownerWindow ?? GetOwnerWindow();
                    await MessageBox.Show(
                        $"Ошибка при поиске товара: {ex.Message}",
                        "Поиск товара",
                        MessageBoxButton.OK,
                        MessageBoxType.Error,
                        owner);
                }
                catch
                {
                    // Игнорируем ошибки при показе диалога
                }
            });
        }
    }
}