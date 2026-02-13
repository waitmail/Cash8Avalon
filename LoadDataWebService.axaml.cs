//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Interactivity;
//using Avalonia.Markup.Xaml;
//using Avalonia.Threading;
//using Newtonsoft.Json;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.IO;
//using System.IO.Compression;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Cash8Avalon
//{
//    public partial class LoadDataWebService : Window
//    {
//        // Элементы управления
//        private Button _btn_new_load;
//        private ProgressBar _progressBar1;
//        private TextBlock _statusText;
//        private TextBlock _progressPercent;
//        private TextBlock _timeInfoText;
//        private StackPanel _progressPanel;

//        // Состояние загрузки
//        private CancellationTokenSource _cancellationTokenSource;
//        private bool _isLoading = false;
//        private readonly TimeSpan _loadTimeout = TimeSpan.FromMinutes(30);
//        private Timer _timer;
//        private Stopwatch _stopwatch;
//        private bool _userCancelled = false;

//        public event EventHandler? RequestClose;

//        public LoadDataWebService()
//        {
//            InitializeComponent();
//            InitializeControls();
//        }

//        private void InitializeComponent()
//        {
//            AvaloniaXamlLoader.Load(this);
//        }

//        private void InitializeControls()
//        {
//            _btn_new_load = this.FindControl<Button>("btn_new_load");
//            _progressBar1 = this.FindControl<ProgressBar>("progressBar1");
//            _statusText = this.FindControl<TextBlock>("statusText");
//            _progressPercent = this.FindControl<TextBlock>("progressPercent");
//            _timeInfoText = this.FindControl<TextBlock>("timeInfoText");
//            _progressPanel = this.FindControl<StackPanel>("progressPanel");

//            if (_btn_new_load != null)
//                _btn_new_load.Click += Btn_new_load_Click;

//            // Скрываем панель прогресса при старте
//            if (_progressPanel != null)
//                _progressPanel.IsVisible = false;

//            if (_timeInfoText != null)
//                _timeInfoText.IsVisible = false;

//            if (_progressBar1 != null)
//                _progressBar1.Value = 0;
//        }

//        #region Классы данных

//        public class LoadPacketData : IDisposable
//        {
//            public int Threshold { get; set; }
//            public List<Tovar> ListTovar { get; set; }
//            public List<Barcode> ListBarcode { get; set; }
//            public List<ActionHeader> ListActionHeader { get; set; }
//            public List<ActionTable> ListActionTable { get; set; }
//            public List<Characteristic> ListCharacteristic { get; set; }
//            public List<Sertificate> ListSertificate { get; set; }
//            public List<PromoText> ListPromoText { get; set; }
//            public List<ActionClients> ListActionClients { get; set; }
//            public bool PacketIsFull { get; set; }
//            public bool Exchange { get; set; }
//            public string Exception { get; set; }
//            public string TokenMark { get; set; }

//            public void Dispose()
//            {
//                ListTovar?.Clear();
//                ListBarcode?.Clear();
//                ListActionHeader?.Clear();
//                ListActionTable?.Clear();
//                ListCharacteristic?.Clear();
//                ListSertificate?.Clear();
//                ListPromoText?.Clear();
//                ListActionClients?.Clear();

//                ListTovar = null;
//                ListBarcode = null;
//                ListActionHeader = null;
//                ListActionTable = null;
//                ListCharacteristic = null;
//                ListSertificate = null;
//                ListPromoText = null;
//                ListActionClients = null;
//            }
//        }

//        public class Tovar
//        {
//            public string Code { get; set; }
//            public string Name { get; set; }
//            public string RetailPrice { get; set; }
//            public string ItsDeleted { get; set; }
//            public string Nds { get; set; }
//            public string ItsCertificate { get; set; }
//            public string PercentBonus { get; set; }
//            public string TnVed { get; set; }
//            public string ItsMarked { get; set; }
//            public string ItsExcise { get; set; }
//            public string CdnCheck { get; set; }
//            public string Fractional { get; set; }
//            public string RefusalOfMarking { get; set; }
//            public string RrNotControlOwner { get; set; }
//        }

//        public class Barcode
//        {
//            public string BarCode { get; set; }
//            public string TovarCode { get; set; }
//        }

//        public class ActionHeader
//        {
//            public string DateStarted { get; set; }
//            public string DateEnd { get; set; }
//            public string NumDoc { get; set; }
//            public string Tip { get; set; }
//            public string Barcode { get; set; }
//            public string Persent { get; set; }
//            public string sum { get; set; }
//            public string sum1 { get; set; }
//            public string Comment { get; set; }
//            public string Marker { get; set; }
//            public string ActionByDiscount { get; set; }
//            public string TimeStart { get; set; }
//            public string TimeEnd { get; set; }
//            public string BonusPromotion { get; set; }
//            public string WithOldPromotion { get; set; }
//            public string Monday { get; set; }
//            public string Tuesday { get; set; }
//            public string Wednesday { get; set; }
//            public string Thursday { get; set; }
//            public string Friday { get; set; }
//            public string Saturday { get; set; }
//            public string Sunday { get; set; }
//            public string PromoCode { get; set; }
//            public string SumBonus { get; set; }
//            public string ExecutionOrder { get; set; }
//            public string GiftPrice { get; set; }
//            public string Kind { get; set; }
//            public string Picture { get; set; }
//        }

//        public class ActionTable
//        {
//            public string NumDoc { get; set; }
//            public string NumList { get; set; }
//            public string CodeTovar { get; set; }
//            public string Price { get; set; }
//        }

//        public class Characteristic
//        {
//            public string CodeTovar { get; set; }
//            public string Name { get; set; }
//            public string Guid { get; set; }
//            public string RetailPrice { get; set; }
//        }

//        public class Sertificate
//        {
//            public string Code { get; set; }
//            public string CodeTovar { get; set; }
//            public string Rating { get; set; }
//            public string IsActive { get; set; }
//        }

//        public class PromoText
//        {
//            public string AdvertisementText { get; set; }
//            public string NumStr { get; set; }
//            public string Picture { get; set; }
//        }

//        public class ActionClients
//        {
//            public string NumDoc { get; set; }
//            public string CodeClient { get; set; }
//        }

//        public class Client
//        {
//            public string code { get; set; }
//            public string phone { get; set; }
//            public string name { get; set; }
//            public string holiday { get; set; }
//            public string use_blocked { get; set; }
//            public string reason_for_blocking { get; set; }
//            public string notify_security { get; set; }
//            public string datetime_update { get; set; }
//        }

//        public class Clients
//        {
//            public List<Client> list_clients { get; set; }
//        }

//        public class QueryPacketData : IDisposable
//        {
//            public string Version { get; set; }
//            public string NickShop { get; set; }
//            public string CodeShop { get; set; }
//            public string LastDateDownloadTovar { get; set; }
//            public string NumCash { get; set; }

//            public void Dispose()
//            {
//                // Освобождение ресурсов
//            }
//        }

//        #endregion

//        #region Обработчики событий UI

//        private async void Btn_new_load_Click(object sender, RoutedEventArgs e)
//        {
//            await StartAsyncLoad();
//        }

//        #endregion

//        #region Основная логика загрузки

//        private async Task StartAsyncLoad()
//        {
//            if (_isLoading)
//            {
//                await MessageBox.Show("Загрузка уже выполняется", "Информация", owner: this);
//                return;
//            }

//            var result = await MessageBox.Show(
//                "Выполнить загрузку данных из системы?",
//                "Подтверждение",
//                MessageBoxButton.YesNo,
//                MessageBoxType.Question,
//                this);

//            if (result != MessageBoxResult.Yes)
//                return;

//            _userCancelled = false;
//            _cancellationTokenSource = new CancellationTokenSource();
//            _stopwatch = Stopwatch.StartNew();

//            try
//            {
//                SetLoadingState(true);

//                // Запускаем таймер для отображения времени
//                StartTimer();

//                // Запускаем загрузку в отдельном потоке
//                var loadTask = Task.Run(async () =>
//                {
//                    try
//                    {
//                        return await PerformFullLoadAsync(_cancellationTokenSource.Token);
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        return (false, "Операция отменена пользователем");
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Ошибка в задаче загрузки: {ex.Message}");
//                        return (false, $"Ошибка при выполнении загрузки: {ex.Message}");
//                    }
//                }, _cancellationTokenSource.Token);

//                // Таймаут загрузки
//                var timeoutTask = Task.Delay(_loadTimeout, _cancellationTokenSource.Token);

//                var completedTask = await Task.WhenAny(loadTask, timeoutTask);

//                if (completedTask == timeoutTask && !_userCancelled)
//                {
//                    await HandleTimeoutAsync();
//                    return;
//                }

//                var (success, errorMessage) = await loadTask;

//                // Показываем результат только если не была отмена пользователем
//                if (!_userCancelled)
//                {
//                    await HandleLoadResultAsync(success, errorMessage);
//                }
//            }
//            catch (Exception ex)
//            {
//                if (!_userCancelled)
//                {
//                    await MessageBox.Show($"Ошибка при запуске загрузки: {ex.Message}", "Ошибка", owner: this);
//                }
//            }
//            finally
//            {
//                SetLoadingState(false);
//                StopTimer();
//                _stopwatch?.Stop();
//                _cancellationTokenSource?.Dispose();
//                _cancellationTokenSource = null;
//            }
//        }

//        private async Task<(bool success, string errorMessage)> PerformFullLoadAsync(CancellationToken cancellationToken)
//        {
//            string errorMessage = "";

//            try
//            {
//                // Этап 1: Подготовка (без очистки памяти!)
//                await UpdateProgressAsync("Подготовка к загрузке...", 0);
//                await PrepareForLoadAsync(cancellationToken, skipClearMemory: true);

//                if (cancellationToken.IsCancellationRequested)
//                    return (false, "Операция отменена");

//                // Этап 2: Проверка сервиса
//                await UpdateProgressAsync("Проверка соединения с веб-сервисом...", 5);
//                if (!await CheckServiceAvailabilityAsync(cancellationToken))
//                {
//                    return (false, "Веб-сервис недоступен");
//                }

//                if (cancellationToken.IsCancellationRequested)
//                    return (false, "Операция отменена");

//                // Этап 3: Создание временных таблиц
//                await UpdateProgressAsync("Подготовка временных таблиц...", 10);
//                await CreateTempTablesAsync(cancellationToken);

//                if (cancellationToken.IsCancellationRequested)
//                    return (false, "Операция отменена");

//                // Этап 4: Получение данных с сервера
//                await UpdateProgressAsync("Получение данных с сервера...", 15);
//                var serverData = await GetDataFromServerAsync(cancellationToken);
//                if (!serverData.success)
//                {
//                    errorMessage = "Не удалось получить данные с сервера";
//                    if (!string.IsNullOrEmpty(serverData.errorMessage))
//                        errorMessage = serverData.errorMessage;
//                    return (false, errorMessage);
//                }

//                if (cancellationToken.IsCancellationRequested)
//                    return (false, "Операция отменена");

//                // Теперь прогресс 20%, готовимся к вставке данных в БД
//                await UpdateProgressAsync("Подготовка к сохранению данных...", 20);

//                // Этап 5: Сохранение данных в БД - здесь прогресс будет идти от 20% до 80%
//                var saveResult = await SaveDataToDatabaseAsync(serverData.data, cancellationToken, 20, 80);
//                if (!saveResult.success)
//                {
//                    return (false, saveResult.errorMessage);
//                }

//                if (cancellationToken.IsCancellationRequested)
//                    return (false, "Операция отменена");

//                // Этап 6: Финализация операций с БД
//                await UpdateProgressAsync("Завершение операций с базой данных...", 85);
//                await FinalizeLoadAsync(cancellationToken);

//                // ТОЛЬКО ПОСЛЕ УСПЕШНОЙ ЗАГРУЗКИ В БД:
//                // Этап 7: Очистка и перезаполнение памяти
//                await UpdateProgressAsync("Обновление данных в памяти...", 90);
//                var memoryResult = await RefreshMemoryDataAsync(cancellationToken);
//                if (!memoryResult.success)
//                {
//                    // Это предупреждение, но не критическая ошибка
//                    errorMessage = memoryResult.errorMessage;
//                    Console.WriteLine($"Предупреждение при обновлении памяти: {errorMessage}");
//                }

//                if (cancellationToken.IsCancellationRequested)
//                    return (false, "Операция отменена");

//                await UpdateProgressAsync("Готово", 100);

//                return (true, "");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"ERROR in PerformFullLoadAsync: {ex.Message}");
//                return (false, $"Ошибка при выполнении загрузки: {ex.Message}");
//            }
//        }

//        private async Task<(bool success, string errorMessage)> RefreshMemoryDataAsync(CancellationToken cancellationToken)
//        {
//            try
//            {
//                // 0. Устанавливаем владельца для InventoryManager
//                // Это простое присваивание статического поля, не требует UI потока
//                InventoryManager.SetOwnerWindow(this);

//                // 1. Очищаем кэш в памяти
//                await UpdateProgressAsync("Очистка кэша в памяти...", 85);

//                // ClearDictionaryProductData тоже не требует UI потока (простая работа с коллекциями)
//                InventoryManager.ClearDictionaryProductData();

//                // 2. Ждем немного для стабилизации
//                await Task.Delay(100, cancellationToken);

//                // 3. Заполняем товары
//                await UpdateProgressAsync("Загрузка товаров в память...", 90);
//                try
//                {
//                    // Передаем текущее окно как владельца для MessageBox
//                    await InventoryManager.FillDictionaryProductDataAsync(this);
//                }
//                catch (Exception ex)
//                {
//                    return (false, $"Ошибка при загрузке товаров в память: {ex.Message}");
//                }

//                // 4. Загружаем акции (в фоне)
//                await UpdateProgressAsync("Загрузка данных об акциях...", 95);
//                try
//                {
//                    // DictionaryPriceGiftAction - это свойство, вызываем его для инициализации
//                    _ = Task.Run(() =>
//                    {
//                        try
//                        {
//                            var _ = InventoryManager.DictionaryPriceGiftAction;
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine($"Предупреждение: не удалось загрузить цены подарков: {ex.Message}");
//                        }
//                    }, cancellationToken);
//                }
//                catch (Exception ex)
//                {
//                    // Это не критическая ошибка, только логируем
//                    Console.WriteLine($"Предупреждение при загрузке акций: {ex.Message}");
//                }

//                // 5. Проверяем, что кэш загружен
//                await Task.Delay(200, cancellationToken);

//                if (!InventoryManager.completeDictionaryProductData)
//                {
//                    return (false, "Кэш товаров не был успешно загружен");
//                }

//                await UpdateProgressAsync("Кэш памяти обновлен", 100);
//                return (true, "");
//            }
//            catch (Exception ex)
//            {
//                return (false, $"Ошибка при обновлении данных в памяти: {ex.Message}");
//            }
//        }

//        private async Task PrepareForLoadAsync(CancellationToken cancellationToken, bool skipClearMemory = false)
//        {
//            try
//            {
//                // Очищаем память только если явно не указано пропустить
//                if (!skipClearMemory)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() =>
//                    {
//                        try
//                        {
//                            if (InventoryManager.ClearDictionaryProductData != null)
//                                InventoryManager.ClearDictionaryProductData();
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine($"Ошибка при очистке кэша: {ex.Message}");
//                        }
//                    });
//                }

//                // Сборка мусора
//                await Task.Run(() =>
//                {
//                    try
//                    {
//                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
//                        GC.WaitForPendingFinalizers();
//                    }
//                    catch { }
//                }, cancellationToken);

//                await Task.Delay(100, cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка при подготовке: {ex.Message}");
//            }
//        }

//        #region Таймер для отображения времени

//        private void StartTimer()
//        {
//            _timer = new Timer(async _ =>
//            {
//                await Dispatcher.UIThread.InvokeAsync(() =>
//                {
//                    if (_stopwatch != null && _stopwatch.IsRunning)
//                    {
//                        var elapsed = _stopwatch.Elapsed;
//                        if (_timeInfoText != null)
//                            _timeInfoText.Text = $"Время загрузки: {elapsed:mm\\:ss}";
//                    }
//                });
//            }, null, 0, 1000);
//        }

//        private void StopTimer()
//        {
//            _timer?.Dispose();
//            _timer = null;

//            if (_stopwatch != null && _stopwatch.IsRunning)
//            {
//                var elapsed = _stopwatch.Elapsed;

//                Dispatcher.UIThread.InvokeAsync(() =>
//                {
//                    if (_timeInfoText != null)
//                        _timeInfoText.Text = $"Общее время загрузки: {elapsed:mm\\:ss}";
//                });
//            }
//        }

//        #endregion

//        #region Методы загрузки

//        private async Task<bool> CheckServiceAvailabilityAsync(CancellationToken cancellationToken)
//        {
//            try
//            {
//                return await Task.Run(() => MainStaticClass.service_is_worker(), cancellationToken);
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private async Task CreateTempTablesAsync(CancellationToken cancellationToken)
//        {
//            try
//            {
//                await Task.Run(() => check_temp_tables(), cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка при создании временных таблиц: {ex.Message}");
//            }
//        }

//        private async Task<(bool success, LoadPacketData data, string errorMessage)> GetDataFromServerAsync(CancellationToken cancellationToken)
//        {
//            return await Task.Run(() =>
//            {
//                try
//                {
//                    string nick_shop = MainStaticClass.Nick_Shop?.Trim();
//                    if (string.IsNullOrEmpty(nick_shop))
//                    {
//                        return (false, null, "Не удалось получить название магазина");
//                    }

//                    string code_shop = MainStaticClass.Code_Shop?.Trim();
//                    if (string.IsNullOrEmpty(code_shop))
//                    {
//                        return (false, null, "Не удалось получить код магазина");
//                    }

//                    string count_day = CryptorEngine.get_count_day();
//                    string key = nick_shop + count_day + code_shop;

//                    using (var queryPacketData = new QueryPacketData())
//                    {
//                        queryPacketData.NickShop = nick_shop;
//                        queryPacketData.CodeShop = code_shop;
//                        queryPacketData.LastDateDownloadTovar = last_date_download_tovars().ToString("dd-MM-yyyy");
//                        queryPacketData.NumCash = MainStaticClass.CashDeskNumber.ToString();
//                        queryPacketData.Version = MainStaticClass.version().Replace(".", "");

//                        string data = JsonConvert.SerializeObject(queryPacketData,
//                            Formatting.Indented,
//                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

//                        string data_encrypt = CryptorEngine.Encrypt(data, true, key);

//                        cancellationToken.ThrowIfCancellationRequested();

//                        var loadPacketData = getLoadPacketDataFull(nick_shop, data_encrypt, key);

//                        if (loadPacketData == null)
//                        {
//                            return (false, null, "Не удалось получить данные с сервера (null результат)");
//                        }

//                        if (!loadPacketData.PacketIsFull)
//                        {
//                            string errorMsg = "Пакет данных не полный";
//                            if (!string.IsNullOrEmpty(loadPacketData.Exception))
//                                errorMsg += $": {loadPacketData.Exception}";
//                            return (false, null, errorMsg);
//                        }

//                        if (loadPacketData.Exchange)
//                        {
//                            return (false, null, "Пакет данных получен во время обновления данных на сервере");
//                        }

//                        return (true, loadPacketData, "");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    return (false, null, $"Ошибка при получении данных с сервера: {ex.Message}");
//                }
//            }, cancellationToken);
//        }

//        // Новая перегрузка метода с поддержкой прогресса
//        private async Task<(bool success, string errorMessage)> SaveDataToDatabaseAsync(
//            LoadPacketData loadPacketData,
//            CancellationToken cancellationToken,
//            int startProgress,
//            int endProgress)
//        {
//            NpgsqlConnection conn = null;
//            NpgsqlTransaction tran = null;

//            try
//            {
//                conn = MainStaticClass.NpgsqlConn();
//                await conn.OpenAsync(cancellationToken);
//                tran = await conn.BeginTransactionAsync(cancellationToken);

//                var queries = new List<string>();
//                PrepareDatabaseQueries(loadPacketData, queries);

//                int totalQueries = queries.Count;
//                int completedQueries = 0;

//                // Рассчитываем прогресс для каждого запроса
//                int progressRange = endProgress - startProgress;

//                foreach (string query in queries)
//                {
//                    cancellationToken.ThrowIfCancellationRequested();

//                    using (var command = new NpgsqlCommand(query, conn))
//                    {
//                        command.Transaction = tran;
//                        await command.ExecuteNonQueryAsync(cancellationToken);
//                    }

//                    completedQueries++;

//                    // Рассчитываем текущий прогресс
//                    double progressPercentage = (double)completedQueries / totalQueries;
//                    int currentProgress = startProgress + (int)(progressPercentage * progressRange);

//                    await UpdateProgressAsync($"Выполнение запросов ({completedQueries}/{totalQueries})...", currentProgress);
//                }

//                // Обновление даты последнего обновления
//                string updateQuery = "UPDATE date_sync SET tovar = @date";
//                using (var command = new NpgsqlCommand(updateQuery, conn))
//                {
//                    command.Transaction = tran;
//                    command.Parameters.AddWithValue("@date", DateTime.Now);
//                    int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

//                    if (rowsAffected == 0)
//                    {
//                        updateQuery = "INSERT INTO date_sync(tovar) VALUES(@date)";
//                        command.CommandText = updateQuery;
//                        await command.ExecuteNonQueryAsync(cancellationToken);
//                    }
//                }

//                await tran.CommitAsync(cancellationToken);

//                // Отправка подтверждения
//                try
//                {
//                    if (!await MainStaticClass.SendResultGetData())
//                    {
//                        Console.WriteLine("WARNING: Не удалось отправить информацию об успешной загрузке");
//                    }
//                }
//                catch { }

//                return (true, "");
//            }
//            catch (NpgsqlException ex)
//            {
//                string errorMsg = $"Ошибка базы данных: {ex.Message}";
//                Console.WriteLine($"Ошибка Npgsql: {ex.Message}");

//                if (tran != null)
//                {
//                    try { await tran.RollbackAsync(cancellationToken); } catch { }
//                }

//                return (false, errorMsg);
//            }
//            catch (Exception ex)
//            {
//                string errorMsg = $"Ошибка при сохранении данных: {ex.Message}";
//                Console.WriteLine($"Ошибка: {ex.Message}");

//                if (tran != null)
//                {
//                    try { await tran.RollbackAsync(cancellationToken); } catch { }
//                }

//                return (false, errorMsg);
//            }
//            finally
//            {
//                if (conn != null && conn.State == ConnectionState.Open)
//                {
//                    try { await conn.CloseAsync(); } catch { }
//                }

//                conn?.Dispose();
//                tran?.Dispose();
//            }
//        }

//        // Оригинальный метод для обратной совместимости
//        private async Task<(bool success, string errorMessage)> SaveDataToDatabaseAsync(
//            LoadPacketData loadPacketData,
//            CancellationToken cancellationToken)
//        {
//            // Вызываем новую перегрузку со значениями по умолчанию
//            return await SaveDataToDatabaseAsync(loadPacketData, cancellationToken, 0, 100);
//        }

//        private void PrepareDatabaseQueries(LoadPacketData loadPacketData, List<string> queries)
//        {
//            // Очистка таблиц
//            queries.Add("DELETE FROM action_table");
//            queries.Add("DELETE FROM action_header");
//            queries.Add("DELETE FROM advertisement");

//            // Обновление токена
//            if (!string.IsNullOrEmpty(loadPacketData.TokenMark))
//            {
//                queries.Add($"UPDATE constants SET cdn_token='{EscapeSql(loadPacketData.TokenMark)}'");
//            }

//            // Создание временной таблицы для товаров
//            queries.Add("DELETE FROM tovar2");

//            // Вставка товаров во временную таблицу
//            if (loadPacketData.ListTovar?.Count > 0)
//            {
//                foreach (var tovar in loadPacketData.ListTovar)
//                {
//                    queries.Add($@"
//                        INSERT INTO tovar2(code,name,retail_price,its_deleted,nds,its_certificate,
//                        percent_bonus,tnved,its_marked,its_excise,cdn_check,fractional,
//                        refusal_of_marking,rr_not_control_owner) 
//                        VALUES({tovar.Code},'{EscapeSql(tovar.Name)}',{tovar.RetailPrice},{tovar.ItsDeleted},
//                        {tovar.Nds},{tovar.ItsCertificate},{tovar.PercentBonus},'{EscapeSql(tovar.TnVed)}',
//                        {tovar.ItsMarked},{tovar.ItsExcise},{tovar.CdnCheck},{tovar.Fractional},
//                        {tovar.RefusalOfMarking},{tovar.RrNotControlOwner})");
//                }
//            }

//            // Обновление основной таблицы товаров
//            queries.Add("UPDATE tovar SET its_deleted=1, retail_price=0");
//            queries.Add(GetInsertQuery());
//            queries.Add(GetUpdateQuery());
//            queries.Add("DELETE FROM tovar2");
//            queries.Add("DELETE FROM barcode");

//            // Вставка штрихкодов
//            if (loadPacketData.ListBarcode?.Count > 0)
//            {
//                foreach (var barcode in loadPacketData.ListBarcode)
//                {
//                    queries.Add($"INSERT INTO barcode(tovar_code,barcode) VALUES({barcode.TovarCode},'{EscapeSql(barcode.BarCode)}')");
//                }
//            }

//            // Вставка характеристик
//            if (loadPacketData.ListCharacteristic?.Count > 0)
//            {
//                queries.Add("DELETE FROM characteristic");
//                foreach (var characteristic in loadPacketData.ListCharacteristic)
//                {
//                    queries.Add($@"
//                        INSERT INTO characteristic(tovar_code, guid, name, retail_price_characteristic) 
//                        VALUES({characteristic.CodeTovar},'{EscapeSql(characteristic.Guid)}','{EscapeSql(characteristic.Name)}',
//                        {characteristic.RetailPrice})");
//                }
//            }

//            // Вставка сертификатов
//            queries.Add("DELETE FROM sertificates");
//            if (loadPacketData.ListSertificate?.Count > 0)
//            {
//                foreach (var sertificate in loadPacketData.ListSertificate)
//                {
//                    queries.Add($@"
//                        INSERT INTO sertificates(code, code_tovar, rating, is_active)
//                        VALUES({sertificate.Code},{sertificate.CodeTovar},{sertificate.Rating},
//                        {sertificate.IsActive})");
//                }
//            }

//            // Вставка акций
//            if (loadPacketData.ListActionHeader?.Count > 0)
//            {
//                foreach (var actionHeader in loadPacketData.ListActionHeader)
//                {
//                    queries.Add($@"
//                        INSERT INTO action_header(date_started,date_end,num_doc,tip,barcode,persent,sum,
//                        comment,marker,action_by_discount,time_start,time_end,bonus_promotion,
//                        with_old_promotion,monday,tuesday,wednesday,thursday,friday,saturday,sunday,
//                        promo_code,sum_bonus,execution_order,gift_price,kind,sum1,picture)
//                        VALUES('{actionHeader.DateStarted}','{actionHeader.DateEnd}',{actionHeader.NumDoc},
//                        {actionHeader.Tip},'{EscapeSql(actionHeader.Barcode)}',{actionHeader.Persent},{actionHeader.sum},
//                        '{EscapeSql(actionHeader.Comment)}',{actionHeader.Marker},{actionHeader.ActionByDiscount},
//                        {actionHeader.TimeStart},{actionHeader.TimeEnd},{actionHeader.BonusPromotion},
//                        {actionHeader.WithOldPromotion},{actionHeader.Monday},{actionHeader.Tuesday},
//                        {actionHeader.Wednesday},{actionHeader.Thursday},{actionHeader.Friday},
//                        {actionHeader.Saturday},{actionHeader.Sunday},{actionHeader.PromoCode},
//                        {actionHeader.SumBonus},{actionHeader.ExecutionOrder},{actionHeader.GiftPrice},
//                        {actionHeader.Kind},{actionHeader.sum1},'{EscapeSql(actionHeader.Picture)}')");
//                }
//            }
//            else
//            {
//                // Уведомление показывается через MessageBox.Show в основном потоке
//                Dispatcher.UIThread.InvokeAsync(async () =>
//                {
//                    await MessageBox.Show("Нет данных по акциям", "Проверка наличия акций", MessageBoxButton.OK, MessageBoxType.Info);
//                });
//            }

//            // Вставка табличных данных акций
//            if (loadPacketData.ListActionTable?.Count > 0)
//            {
//                foreach (var actionTable in loadPacketData.ListActionTable)
//                {
//                    queries.Add($@"
//                        INSERT INTO action_table(num_doc, num_list, code_tovar, price)
//                        VALUES({actionTable.NumDoc},{actionTable.NumList},{actionTable.CodeTovar},
//                        {actionTable.Price})");
//                }
//            }

//            // Вставка рекламных текстов
//            if (loadPacketData.ListPromoText?.Count > 0)
//            {
//                foreach (var promoText in loadPacketData.ListPromoText)
//                {
//                    queries.Add($@"
//                        INSERT INTO advertisement(advertisement_text,num_str,picture)
//                        VALUES('{EscapeSql(promoText.AdvertisementText)}',{promoText.NumStr},'{EscapeSql(promoText.Picture)}')");
//                }
//            }

//            // Вставка клиентов акций
//            queries.Add("DELETE FROM action_clients");
//            if (loadPacketData.ListActionClients?.Count > 0)
//            {
//                foreach (var actionClients in loadPacketData.ListActionClients)
//                {
//                    queries.Add($@"
//                        INSERT INTO action_clients(num_doc, code_client)
//                        VALUES({actionClients.NumDoc},{actionClients.CodeClient})");
//                }
//            }
//        }

//        private string EscapeSql(string input)
//        {
//            if (string.IsNullOrEmpty(input))
//                return string.Empty;

//            return input.Replace("'", "''");
//        }

//        private async Task FinalizeLoadAsync(CancellationToken cancellationToken)
//        {
//            try
//            {
//                await Task.Delay(200, cancellationToken);

//                if (CheckFirstLoadData())
//                {
//                    await Dispatcher.UIThread.InvokeAsync(async () =>
//                    {
//                        await MessageBox.Show(
//                            "Это была первая загрузка данных. Для применения новых параметров программа будет перезапущена.",
//                            "Первая загрузка",
//                            MessageBoxButton.OK,
//                            MessageBoxType.Info,
//                            this);
//                    });
//                }

//                await Task.Run(() =>
//                {
//                    try
//                    {
//                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
//                        GC.WaitForPendingFinalizers();
//                    }
//                    catch { }
//                }, cancellationToken);
//            }
//            catch { }
//        }

//        #endregion

//        #region Метод load_bonus_clients

//        public async Task load_bonus_clients(bool show_message)
//        {
//            await Task.Run(() => load_bonus_clients_internal(show_message));
//        }

//        private async Task load_bonus_clients_internal(bool show_message)
//        {
//            try
//            {
//                if (!MainStaticClass.service_is_worker())
//                {
//                    if (show_message)
//                    {
//                        await Dispatcher.UIThread.InvokeAsync(async () =>
//                        {
//                            await MessageBox.Show("Веб сервис недоступен", "Ошибка", owner: this);
//                        });
//                    }
//                    return;
//                }

//                DS ds = MainStaticClass.get_ds();
//                ds.Timeout = 60000;

//                string nick_shop = MainStaticClass.Nick_Shop.Trim();
//                if (nick_shop.Trim().Length == 0)
//                {
//                    if (show_message)
//                    {
//                        await Dispatcher.UIThread.InvokeAsync(async () =>
//                        {
//                            await MessageBox.Show("Не удалось получить название магазина", "Ошибка", owner: this);
//                        });
//                    }
//                    return;
//                }

//                string code_shop = MainStaticClass.Code_Shop.Trim();
//                if (code_shop.Trim().Length == 0)
//                {
//                    if (show_message)
//                    {
//                        await Dispatcher.UIThread.InvokeAsync(async () =>
//                        {
//                            await MessageBox.Show("Не удалось получить код магазина", "Ошибка", owner: this);
//                        });
//                    }
//                    return;
//                }

//                string count_day = CryptorEngine.get_count_day();
//                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
//                DateTime dt = last_date_download_bonus_clients();

//                string data = CryptorEngine.Encrypt(nick_shop + "|" + dt.Ticks.ToString() + "|" + code_shop, true, key);

//                string result_query = "-1";
//                try
//                {
//                    result_query = ds.GetDiscountClientsV8DateTime_NEW(nick_shop, data, "4");
//                }
//                catch (Exception ex)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(async () =>
//                    {
//                        await MessageBox.Show(ex.Message, "Ошибка", owner: this);
//                    });
//                }

//                if (result_query == "-1")
//                {
//                    if (show_message)
//                    {
//                        await Dispatcher.UIThread.InvokeAsync(async () =>
//                        {
//                            await MessageBox.Show("При обработке запроса на сервере произошли ошибки", "Ошибка", owner: this);
//                        });
//                    }
//                    return;
//                }

//                string result_query_decrypt = CryptorEngine.Decrypt(result_query, true, key);
//                Clients clients = JsonConvert.DeserializeObject<Clients>(result_query_decrypt);

//                if (clients.list_clients.Count == 0)
//                {
//                    return;
//                }

//                NpgsqlConnection conn = null;
//                NpgsqlTransaction tran = null;
//                string query = "";

//                try
//                {
//                    conn = MainStaticClass.NpgsqlConn();
//                    conn.Open();
//                    tran = conn.BeginTransaction();
//                    NpgsqlCommand command = null;
//                    string local_last_date_download_bonus_clients = "";

//                    foreach (Client client in clients.list_clients)
//                    {
//                        query = "UPDATE clients SET " +
//                            " phone='" + client.phone + "'," +
//                            " name='" + client.name + "'," +
//                            " date_of_birth='" + client.holiday + "'," +
//                            " its_work='" + client.use_blocked + "'," +
//                            " reason_for_blocking='" + client.reason_for_blocking + "'," +
//                            " notify_security='" + client.notify_security + "' " +
//                            " WHERE code='" + client.code + "';";

//                        local_last_date_download_bonus_clients = client.datetime_update;

//                        command = new NpgsqlCommand(query, conn);
//                        command.Transaction = tran;
//                        int rowsaffected = command.ExecuteNonQuery();
//                        if (rowsaffected == 0)
//                        {
//                            query = "INSERT INTO clients(code,phone,name, date_of_birth,its_work,reason_for_blocking,notify_security)VALUES('" +
//                                client.code + "','" +
//                                client.phone + "','" +
//                                client.name + "','" +
//                                client.holiday + "','" +
//                                client.use_blocked + "','" +
//                                client.reason_for_blocking + "','" +
//                                client.notify_security + "')";
//                            command = new NpgsqlCommand(query, conn);
//                            command.Transaction = tran;
//                            command.ExecuteNonQuery();
//                        }
//                    }

//                    query = "UPDATE constants SET last_date_download_bonus_clients='" + local_last_date_download_bonus_clients + "'";
//                    command = new NpgsqlCommand(query, conn);
//                    command.Transaction = tran;
//                    command.ExecuteNonQuery();

//                    tran.Commit();
//                    conn.Close();

//                    if (show_message)
//                    {
//                        await Dispatcher.UIThread.InvokeAsync(async () =>
//                        {
//                            await MessageBox.Show("Клиенты успешно загружены", "Успех", owner: this);
//                        });
//                    }
//                }
//                catch (NpgsqlException ex)
//                {
//                    if (show_message)
//                    {
//                        await Dispatcher.UIThread.InvokeAsync(async () =>
//                        {
//                            await MessageBox.Show(query + "\n" + ex.Message, "Ошибка при импорте данных", owner: this);
//                        });
//                    }
//                    if (tran != null)
//                    {
//                        tran.Rollback();
//                    }
//                }
//                catch (Exception ex)
//                {
//                    if (show_message)
//                    {
//                        await Dispatcher.UIThread.InvokeAsync(async () =>
//                        {
//                            await MessageBox.Show(query + "\n" + ex.Message, "Ошибка при импорте данных", owner: this);
//                        });
//                    }
//                    if (tran != null)
//                    {
//                        tran.Rollback();
//                    }
//                }
//                finally
//                {
//                    if (conn != null && conn.State == ConnectionState.Open)
//                    {
//                        conn.Close();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка в load_bonus_clients: {ex.Message}");
//            }
//        }

//        private DateTime last_date_download_bonus_clients()
//        {
//            DateTime result = new DateTime(2000, 1, 1);

//            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

//            try
//            {
//                conn.Open();
//                string query = "SELECT last_date_download_bonus_clients FROM constants";
//                NpgsqlCommand command = new NpgsqlCommand(query, conn);
//                object query_result = command.ExecuteScalar();
//                if (query_result != null)
//                {
//                    result = Convert.ToDateTime(query_result);
//                }
//                conn.Close();
//            }
//            catch (NpgsqlException) { }
//            catch (Exception) { }
//            finally
//            {
//                if (conn.State == ConnectionState.Open)
//                {
//                    conn.Close();
//                }
//            }

//            return result;
//        }

//        #endregion

//        #region Вспомогательные методы из оригинального кода

//        private void check_temp_tables()
//        {
//            try
//            {
//                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
//                {
//                    conn.Open();

//                    string sql = @"
//                    DROP TABLE IF EXISTS tovar2;
//                    CREATE TABLE tovar2(
//                        code bigint NOT NULL, name character(100) NOT NULL,
//                        retail_price numeric(10,2), purchase_price numeric(10,2),
//                        its_deleted numeric(1), nds integer, its_certificate smallint,
//                        percent_bonus numeric(8,2), tnved character varying(10),
//                        its_marked smallint, its_excise smallint, cdn_check boolean,
//                        fractional boolean NOT NULL DEFAULT false,
//                        refusal_of_marking boolean NOT NULL DEFAULT false,
//                        rr_not_control_owner boolean NOT NULL DEFAULT false
//                    ) WITH (OIDS=FALSE);
//                    ALTER TABLE tovar2 OWNER TO postgres;
//                    CREATE UNIQUE INDEX _tovar2_code_ ON tovar2 USING btree (code);";

//                    using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
//                    {
//                        command.ExecuteNonQuery();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка при создании таблицы tovar2: {ex.Message}");
//            }
//        }

//        private LoadPacketData getLoadPacketDataFull(string nick_shop, string data_encrypt, string key)
//        {
//            LoadPacketData loadPacketData = new LoadPacketData();
//            loadPacketData.PacketIsFull = false;

//            string result_query = "";
//            string decrypt_data = "";
//            try
//            {
//                using (DS ds = MainStaticClass.get_ds())
//                {
//                    ds.Timeout = 60000;
//                    byte[] result_query_byte = ds.GetDataForCasheV8Jason(nick_shop, data_encrypt, MainStaticClass.GetWorkSchema.ToString());
//                    result_query = DecompressString(result_query_byte);
//                    decrypt_data = CryptorEngine.Decrypt(result_query, true, key);
//                    loadPacketData = JsonConvert.DeserializeObject<LoadPacketData>(decrypt_data);
//                }
//            }
//            catch (Exception ex)
//            {
//                loadPacketData.Exception = ex.Message;
//                loadPacketData.PacketIsFull = false;
//            }
//            return loadPacketData;
//        }

//        private string DecompressString(byte[] value)
//        {
//            string resultString = string.Empty;
//            if (value != null && value.Length > 0)
//            {
//                using (MemoryStream stream = new MemoryStream(value))
//                using (GZipStream zip = new GZipStream(stream, CompressionMode.Decompress))
//                using (StreamReader reader = new StreamReader(zip))
//                {
//                    resultString = reader.ReadToEnd();
//                }
//            }
//            return resultString;
//        }

//        private string GetInsertQuery()
//        {
//            return @"
//            INSERT INTO tovar 
//            SELECT F.code, F.name, F.retail_price, F.its_deleted, F.nds, 
//                   F.its_certificate, F.percent_bonus, F.tnved, F.its_marked,
//                   F.its_excise, F.cdn_check, F.fractional, F.refusal_of_marking,
//                   F.rr_not_control_owner
//            FROM (
//                SELECT t2.code, t.code AS code2, t2.name, t2.retail_price, 
//                       t2.its_deleted, t2.nds, t2.its_certificate, t2.percent_bonus, 
//                       t2.tnved, t2.its_marked, t2.its_excise, t2.cdn_check, 
//                       t2.fractional, t2.refusal_of_marking,t2.rr_not_control_owner
//                FROM tovar2 t2 
//                LEFT JOIN tovar t ON t2.code = t.code
//            ) AS F 
//            WHERE code2 IS NULL;";
//        }

//        private string GetUpdateQuery()
//        {
//            return @"
//            UPDATE tovar 
//            SET name = t2.name,
//                retail_price = t2.retail_price,
//                its_deleted = t2.its_deleted,
//                nds = t2.nds,
//                its_certificate = t2.its_certificate,
//                percent_bonus = t2.percent_bonus,
//                tnved = t2.tnved,
//                its_marked = t2.its_marked,
//                its_excise = t2.its_excise,
//                cdn_check = t2.cdn_check,
//                fractional = t2.fractional,
//                refusal_of_marking = t2.refusal_of_marking,
//                rr_not_control_owner = t2.rr_not_control_owner
//            FROM tovar2 t2 
//            WHERE tovar.code = t2.code;";
//        }

//        public static DateTime last_date_download_tovars()
//        {
//            DateTime result = new DateTime(2000, 1, 1);

//            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

//            try
//            {
//                conn.Open();
//                string query = "SELECT tovar FROM date_sync";
//                NpgsqlCommand command = new NpgsqlCommand(query, conn);
//                object query_result = command.ExecuteScalar();
//                if (query_result != null)
//                {
//                    result = Convert.ToDateTime(query_result);
//                }
//                conn.Close();
//            }
//            catch (NpgsqlException)
//            {

//            }
//            catch (Exception)
//            {

//            }
//            finally
//            {
//                if (conn.State == ConnectionState.Open)
//                {
//                    conn.Close();

//                }
//            }

//            return result;
//        }

//        private bool CheckFirstLoadData()
//        {
//            bool result = false;

//            try
//            {
//                using (var conn = MainStaticClass.NpgsqlConn())
//                {
//                    conn.Open();
//                    string query = "SELECT tovar FROM public.date_sync";
//                    using (var command = new NpgsqlCommand(query, conn))
//                    {
//                        object resultQuery = command.ExecuteScalar();
//                        if (resultQuery != null && DateTime.TryParse(resultQuery.ToString(), out DateTime date))
//                        {
//                            if (date < new DateTime(2001, 1, 1))
//                            {
//                                result = true;
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка при проверке первой загрузки: {ex.Message}");
//            }

//            return result;
//        }

//        #endregion

//        #region Методы для работы с UI

//        private void SetLoadingState(bool isLoading)
//        {
//            _isLoading = isLoading;

//            Dispatcher.UIThread.InvokeAsync(() =>
//            {
//                _btn_new_load.IsEnabled = !isLoading;

//                if (_progressPanel != null)
//                    _progressPanel.IsVisible = isLoading;

//                if (_timeInfoText != null)
//                    _timeInfoText.IsVisible = isLoading;

//                if (isLoading)
//                {
//                    this.CanResize = false;
//                }
//                else
//                {
//                    this.CanResize = true;
//                    if (_progressBar1 != null)
//                    {
//                        _progressBar1.Value = 0;
//                        _progressBar1.IsIndeterminate = false;
//                    }
//                    if (_statusText != null)
//                        _statusText.Text = "";
//                }
//            }).Wait();
//        }

//        private async Task UpdateProgressAsync(string message, int progress)
//        {
//            await Dispatcher.UIThread.InvokeAsync(() =>
//            {
//                if (_statusText != null)
//                    _statusText.Text = message;

//                if (_progressBar1 != null)
//                {
//                    _progressBar1.IsIndeterminate = false;
//                    _progressBar1.Value = progress;
//                }

//                if (_progressPercent != null)
//                    _progressPercent.Text = $"{progress}%";
//            });
//        }

//        private async Task HandleTimeoutAsync()
//        {
//            _cancellationTokenSource?.Cancel();

//            await Dispatcher.UIThread.InvokeAsync(async () =>
//            {
//                await MessageBox.Show(
//                    $"Загрузка превысила лимит времени ({_loadTimeout.TotalMinutes} минут)",
//                    "Таймаут",
//                    owner: this);
//            });
//        }

//        private async Task HandleLoadResultAsync(bool success, string errorMessage)
//        {
//            await Dispatcher.UIThread.InvokeAsync(async () =>
//            {
//                if (success)
//                {
//                    await MessageBox.Show(
//                        "Загрузка данных успешно завершена",
//                        "Успех",
//                        owner: this);
//                }
//                else
//                {
//                    string message = "Не удалось выполнить загрузку данных";
//                    if (!string.IsNullOrEmpty(errorMessage))
//                        message += $"\n\nПричина: {errorMessage}";

//                    await MessageBox.Show(
//                        message,
//                        "Ошибка",
//                        owner: this);
//                }
//            });
//        }

//        protected override void OnClosing(WindowClosingEventArgs e)
//        {
//            if (_isLoading)
//            {
//                e.Cancel = true;
//                ShowCancelDialog();
//            }

//            base.OnClosing(e);
//        }

//        private async void ShowCancelDialog()
//        {
//            var result = await MessageBox.Show(
//                "Идет загрузка данных. Вы уверены, что хотите отменить?",
//                "Подтверждение",
//                MessageBoxButton.YesNo,
//                MessageBoxType.Warning,
//                this);

//            if (result == MessageBoxResult.Yes)
//            {
//                _userCancelled = true;
//                _cancellationTokenSource?.Cancel();

//                // Закрываем окно после небольшой задержки для корректной отмены
//                await Task.Delay(100);
//                RequestClose?.Invoke(this, EventArgs.Empty);
//            }
//        }

//        #endregion

//        #endregion
//    }
//}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class LoadDataWebService : Window
    {
        // Элементы управления
        private Button _btn_new_load;
        private ProgressBar _progressBar1;
        private TextBlock _statusText;
        private TextBlock _progressPercent;
        private TextBlock _timeInfoText;
        private StackPanel _progressPanel;

        // Состояние загрузки
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isLoading = false;
        private readonly TimeSpan _loadTimeout = TimeSpan.FromMinutes(30);
        private Timer _timer;
        private Stopwatch _stopwatch;
        private bool _userCancelled = false;

        public event EventHandler? RequestClose;

        public LoadDataWebService()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _btn_new_load = this.FindControl<Button>("btn_new_load");
            _progressBar1 = this.FindControl<ProgressBar>("progressBar1");
            _statusText = this.FindControl<TextBlock>("statusText");
            _progressPercent = this.FindControl<TextBlock>("progressPercent");
            _timeInfoText = this.FindControl<TextBlock>("timeInfoText");
            _progressPanel = this.FindControl<StackPanel>("progressPanel");

            if (_btn_new_load != null)
                _btn_new_load.Click += Btn_new_load_Click;

            // Скрываем панель прогресса при старте
            if (_progressPanel != null)
                _progressPanel.IsVisible = false;

            if (_timeInfoText != null)
                _timeInfoText.IsVisible = false;

            if (_progressBar1 != null)
                _progressBar1.Value = 0;
        }

        #region Классы данных

        public class LoadPacketData : IDisposable
        {
            public int Threshold { get; set; }
            public List<Tovar> ListTovar { get; set; }
            public List<Barcode> ListBarcode { get; set; }
            public List<ActionHeader> ListActionHeader { get; set; }
            public List<ActionTable> ListActionTable { get; set; }
            public List<Characteristic> ListCharacteristic { get; set; }
            public List<Sertificate> ListSertificate { get; set; }
            public List<PromoText> ListPromoText { get; set; }
            public List<ActionClients> ListActionClients { get; set; }
            public bool PacketIsFull { get; set; }
            public bool Exchange { get; set; }
            public string Exception { get; set; }
            public string TokenMark { get; set; }

            public void Dispose()
            {
                ListTovar?.Clear();
                ListBarcode?.Clear();
                ListActionHeader?.Clear();
                ListActionTable?.Clear();
                ListCharacteristic?.Clear();
                ListSertificate?.Clear();
                ListPromoText?.Clear();
                ListActionClients?.Clear();

                ListTovar = null;
                ListBarcode = null;
                ListActionHeader = null;
                ListActionTable = null;
                ListCharacteristic = null;
                ListSertificate = null;
                ListPromoText = null;
                ListActionClients = null;
            }
        }

        public class Tovar
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string RetailPrice { get; set; }
            public string ItsDeleted { get; set; }
            public string Nds { get; set; }
            public string ItsCertificate { get; set; }
            public string PercentBonus { get; set; }
            public string TnVed { get; set; }
            public string ItsMarked { get; set; }
            public string ItsExcise { get; set; }
            public string CdnCheck { get; set; }
            public string Fractional { get; set; }
            public string RefusalOfMarking { get; set; }
            public string RrNotControlOwner { get; set; }
        }

        public class Barcode
        {
            public string BarCode { get; set; }
            public string TovarCode { get; set; }
        }

        public class ActionHeader
        {
            public string DateStarted { get; set; }
            public string DateEnd { get; set; }
            public string NumDoc { get; set; }
            public string Tip { get; set; }
            public string Barcode { get; set; }
            public string Persent { get; set; }
            public string sum { get; set; }
            public string sum1 { get; set; }
            public string Comment { get; set; }
            public string Marker { get; set; }
            public string ActionByDiscount { get; set; }
            public string TimeStart { get; set; }
            public string TimeEnd { get; set; }
            public string BonusPromotion { get; set; }
            public string WithOldPromotion { get; set; }
            public string Monday { get; set; }
            public string Tuesday { get; set; }
            public string Wednesday { get; set; }
            public string Thursday { get; set; }
            public string Friday { get; set; }
            public string Saturday { get; set; }
            public string Sunday { get; set; }
            public string PromoCode { get; set; }
            public string SumBonus { get; set; }
            public string ExecutionOrder { get; set; }
            public string GiftPrice { get; set; }
            public string Kind { get; set; }
            public string Picture { get; set; }
        }

        public class ActionTable
        {
            public string NumDoc { get; set; }
            public string NumList { get; set; }
            public string CodeTovar { get; set; }
            public string Price { get; set; }
        }

        public class Characteristic
        {
            public string CodeTovar { get; set; }
            public string Name { get; set; }
            public string Guid { get; set; }
            public string RetailPrice { get; set; }
        }

        public class Sertificate
        {
            public string Code { get; set; }
            public string CodeTovar { get; set; }
            public string Rating { get; set; }
            public string IsActive { get; set; }
        }

        public class PromoText
        {
            public string AdvertisementText { get; set; }
            public string NumStr { get; set; }
            public string Picture { get; set; }
        }

        public class ActionClients
        {
            public string NumDoc { get; set; }
            public string CodeClient { get; set; }
        }

        public class Client
        {
            public string code { get; set; }
            public string phone { get; set; }
            public string name { get; set; }
            public string holiday { get; set; }
            public string use_blocked { get; set; }
            public string reason_for_blocking { get; set; }
            public string notify_security { get; set; }
            public string datetime_update { get; set; }
        }

        public class Clients
        {
            public List<Client> list_clients { get; set; }
        }

        public class QueryPacketData : IDisposable
        {
            public string Version { get; set; }
            public string NickShop { get; set; }
            public string CodeShop { get; set; }
            public string LastDateDownloadTovar { get; set; }
            public string NumCash { get; set; }

            public void Dispose()
            {
                // Освобождение ресурсов
            }
        }

        #endregion

        #region Обработчики событий UI

        private async void Btn_new_load_Click(object sender, RoutedEventArgs e)
        {
            await StartAsyncLoad();
        }

        #endregion

        #region Основная логика загрузки

        private async Task StartAsyncLoad()
        {
            if (_isLoading)
            {
                await MessageBox.Show("Загрузка уже выполняется", "Информация", owner: this);
                return;
            }

            var result = await MessageBox.Show(
                "Выполнить загрузку данных из системы?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxType.Question,
                this);

            if (result != MessageBoxResult.Yes)
                return;

            _userCancelled = false;
            _cancellationTokenSource = new CancellationTokenSource();
            _stopwatch = Stopwatch.StartNew();

            try
            {
                SetLoadingState(true); // Кнопка станет зеленой

                // Убираем временно обработчик клика
                if (_btn_new_load != null)
                    _btn_new_load.Click -= Btn_new_load_Click;

                StartTimer();

                var loadTask = Task.Run(async () =>
                {
                    try
                    {
                        return await PerformFullLoadAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return (false, "Операция отменена пользователем");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка в задаче загрузки: {ex.Message}");
                        return (false, $"Ошибка при выполнении загрузки: {ex.Message}");
                    }
                }, _cancellationTokenSource.Token);

                var timeoutTask = Task.Delay(_loadTimeout, _cancellationTokenSource.Token);
                var completedTask = await Task.WhenAny(loadTask, timeoutTask);

                if (completedTask == timeoutTask && !_userCancelled)
                {
                    await HandleTimeoutAsync();
                    return;
                }

                var (success, errorMessage) = await loadTask;

                if (!_userCancelled)
                {
                    await HandleLoadResultAsync(success, errorMessage);
                }
            }
            catch (Exception ex)
            {
                if (!_userCancelled)
                {
                    await MessageBox.Show($"Ошибка при запуске загрузки: {ex.Message}", "Ошибка", owner: this);
                }
            }
            finally
            {
                SetLoadingState(false); // Кнопка станет синей

                // Возвращаем обработчик клика
                if (_btn_new_load != null)
                    _btn_new_load.Click += Btn_new_load_Click;

                StopTimer();
                _stopwatch?.Stop();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task<(bool success, string errorMessage)> PerformFullLoadAsync(CancellationToken cancellationToken)
        {
            string errorMessage = "";

            try
            {
                // Этап 1: Подготовка (без очистки памяти!)
                await UpdateProgressAsync("Подготовка к загрузке...", 0);
                await PrepareForLoadAsync(cancellationToken, skipClearMemory: true);

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                // Этап 2: Проверка сервиса
                await UpdateProgressAsync("Проверка соединения с веб-сервисом...", 5);
                if (!await CheckServiceAvailabilityAsync(cancellationToken))
                {
                    return (false, "Веб-сервис недоступен");
                }

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                // Этап 3: Создание временных таблиц
                await UpdateProgressAsync("Подготовка временных таблиц...", 10);
                await CreateTempTablesAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                // Этап 4: Получение данных с сервера
                await UpdateProgressAsync("Получение данных с сервера...", 15);
                var serverData = await GetDataFromServerAsync(cancellationToken);
                if (!serverData.success)
                {
                    errorMessage = "Не удалось получить данные с сервера";
                    if (!string.IsNullOrEmpty(serverData.errorMessage))
                        errorMessage = serverData.errorMessage;
                    return (false, errorMessage);
                }

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                // Теперь прогресс 20%, готовимся к вставке данных в БД
                await UpdateProgressAsync("Подготовка к сохранению данных...", 20);

                // Этап 5: Сохранение данных в БД - здесь прогресс будет идти от 20% до 80%
                var saveResult = await SaveDataToDatabaseAsync(serverData.data, cancellationToken, 20, 80);
                if (!saveResult.success)
                {
                    return (false, saveResult.errorMessage);
                }

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                // Этап 6: Финализация операций с БД
                await UpdateProgressAsync("Завершение операций с базой данных...", 85);
                await FinalizeLoadAsync(cancellationToken);

                // ТОЛЬКО ПОСЛЕ УСПЕШНОЙ ЗАГРУЗКИ В БД:
                // Этап 7: Очистка и перезаполнение памяти
                await UpdateProgressAsync("Обновление данных в памяти...", 90);
                var memoryResult = await RefreshMemoryDataAsync(cancellationToken);
                if (!memoryResult.success)
                {
                    // Это предупреждение, но не критическая ошибка
                    errorMessage = memoryResult.errorMessage;
                    Console.WriteLine($"Предупреждение при обновлении памяти: {errorMessage}");
                }

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                await UpdateProgressAsync("Готово", 100);

                return (true, "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in PerformFullLoadAsync: {ex.Message}");
                return (false, $"Ошибка при выполнении загрузки: {ex.Message}");
            }
        }

        private async Task<(bool success, string errorMessage)> RefreshMemoryDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 0. Устанавливаем владельца для InventoryManager
                // Это простое присваивание статического поля, не требует UI потока
                InventoryManager.SetOwnerWindow(this);

                // 1. Очищаем кэш в памяти
                await UpdateProgressAsync("Очистка кэша в памяти...", 85);

                // ClearDictionaryProductData тоже не требует UI потока (простая работа с коллекциями)
                InventoryManager.ClearDictionaryProductData();

                // 2. Ждем немного для стабилизации
                await Task.Delay(100, cancellationToken);

                // 3. Заполняем товары
                await UpdateProgressAsync("Загрузка товаров в память...", 90);
                try
                {
                    // Передаем текущее окно как владельца для MessageBox
                    await InventoryManager.FillDictionaryProductDataAsync(this);
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка при загрузке товаров в память: {ex.Message}");
                }

                // 4. Загружаем акции (в фоне)
                await UpdateProgressAsync("Загрузка данных об акциях...", 95);
                try
                {
                    // DictionaryPriceGiftAction - это свойство, вызываем его для инициализации
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            var _ = InventoryManager.DictionaryPriceGiftAction;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Предупреждение: не удалось загрузить цены подарков: {ex.Message}");
                        }
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Это не критическая ошибка, только логируем
                    Console.WriteLine($"Предупреждение при загрузке акций: {ex.Message}");
                }

                // 5. Проверяем, что кэш загружен
                await Task.Delay(200, cancellationToken);

                // Сначала проверяем валидность словаря
                if (!InventoryManager.IsDictionaryValid)
                {
                    return (false, "Кэш товаров не был успешно загружен");
                }

                await UpdateProgressAsync("Кэш памяти обновлен", 100);
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при обновлении данных в памяти: {ex.Message}");
            }
        }

        private async Task PrepareForLoadAsync(CancellationToken cancellationToken, bool skipClearMemory = false)
        {
            try
            {
                // Очищаем память только если явно не указано пропустить
                if (!skipClearMemory)
                {
                    try
                    {
                        // Убрал Dispatcher - это не UI операция
                        InventoryManager.ClearDictionaryProductData();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при очистке кэша: {ex.Message}");
                    }
                }

                // Сборка мусора
                await Task.Run(() =>
                {
                    try
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        GC.WaitForPendingFinalizers();
                    }
                    catch { }
                }, cancellationToken);

                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при подготовке: {ex.Message}");
            }
        }

        #region Таймер для отображения времени

        private void StartTimer()
        {
            _timer = new Timer(async _ =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_stopwatch != null && _stopwatch.IsRunning)
                    {
                        var elapsed = _stopwatch.Elapsed;
                        if (_timeInfoText != null)
                            _timeInfoText.Text = $"Время загрузки: {elapsed:mm\\:ss}";
                    }
                });
            }, null, 0, 1000);
        }

        private void StopTimer()
        {
            _timer?.Dispose();
            _timer = null;

            if (_stopwatch != null && _stopwatch.IsRunning)
            {
                var elapsed = _stopwatch.Elapsed;

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_timeInfoText != null)
                        _timeInfoText.Text = $"Общее время загрузки: {elapsed:mm\\:ss}";
                });
            }
        }

        #endregion

        #region Методы загрузки

        private async Task<bool> CheckServiceAvailabilityAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() => MainStaticClass.service_is_worker(), cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        private async Task CreateTempTablesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() => check_temp_tables(), cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании временных таблиц: {ex.Message}");
            }
        }

        private async Task<(bool success, LoadPacketData data, string errorMessage)> GetDataFromServerAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string nick_shop = MainStaticClass.Nick_Shop?.Trim();
                    if (string.IsNullOrEmpty(nick_shop))
                    {
                        return (false, null, "Не удалось получить название магазина");
                    }

                    string code_shop = MainStaticClass.Code_Shop?.Trim();
                    if (string.IsNullOrEmpty(code_shop))
                    {
                        return (false, null, "Не удалось получить код магазина");
                    }

                    string count_day = CryptorEngine.get_count_day();
                    string key = nick_shop + count_day + code_shop;

                    using (var queryPacketData = new QueryPacketData())
                    {
                        queryPacketData.NickShop = nick_shop;
                        queryPacketData.CodeShop = code_shop;
                        queryPacketData.LastDateDownloadTovar = last_date_download_tovars().ToString("dd-MM-yyyy");
                        queryPacketData.NumCash = MainStaticClass.CashDeskNumber.ToString();
                        queryPacketData.Version = MainStaticClass.version().Replace(".", "");

                        string data = JsonConvert.SerializeObject(queryPacketData,
                            Formatting.Indented,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        string data_encrypt = CryptorEngine.Encrypt(data, true, key);

                        cancellationToken.ThrowIfCancellationRequested();

                        var loadPacketData = getLoadPacketDataFull(nick_shop, data_encrypt, key);

                        if (loadPacketData == null)
                        {
                            return (false, null, "Не удалось получить данные с сервера (null результат)");
                        }

                        if (!loadPacketData.PacketIsFull)
                        {
                            string errorMsg = "Пакет данных не полный";
                            if (!string.IsNullOrEmpty(loadPacketData.Exception))
                                errorMsg += $": {loadPacketData.Exception}";
                            return (false, null, errorMsg);
                        }

                        if (loadPacketData.Exchange)
                        {
                            return (false, null, "Пакет данных получен во время обновления данных на сервере");
                        }

                        return (true, loadPacketData, "");
                    }
                }
                catch (Exception ex)
                {
                    return (false, null, $"Ошибка при получении данных с сервера: {ex.Message}");
                }
            }, cancellationToken);
        }

        // Новая перегрузка метода с поддержкой прогресса
        private async Task<(bool success, string errorMessage)> SaveDataToDatabaseAsync(
            LoadPacketData loadPacketData,
            CancellationToken cancellationToken,
            int startProgress,
            int endProgress)
        {
            NpgsqlConnection conn = null;
            NpgsqlTransaction tran = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                await conn.OpenAsync(cancellationToken);
                tran = await conn.BeginTransactionAsync(cancellationToken);

                var queries = new List<string>();
                PrepareDatabaseQueries(loadPacketData, queries);

                int totalQueries = queries.Count;
                int completedQueries = 0;

                // Рассчитываем прогресс для каждого запроса
                int progressRange = endProgress - startProgress;

                foreach (string query in queries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        command.Transaction = tran;
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }

                    completedQueries++;

                    // Рассчитываем текущий прогресс
                    double progressPercentage = (double)completedQueries / totalQueries;
                    int currentProgress = startProgress + (int)(progressPercentage * progressRange);

                    await UpdateProgressAsync($"Выполнение запросов ({completedQueries}/{totalQueries})...", currentProgress);
                }

                // Обновление даты последнего обновления
                string updateQuery = "UPDATE date_sync SET tovar = @date";
                using (var command = new NpgsqlCommand(updateQuery, conn))
                {
                    command.Transaction = tran;
                    command.Parameters.AddWithValue("@date", DateTime.Now);
                    int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

                    if (rowsAffected == 0)
                    {
                        updateQuery = "INSERT INTO date_sync(tovar) VALUES(@date)";
                        command.CommandText = updateQuery;
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }

                await tran.CommitAsync(cancellationToken);

                // Отправка подтверждения
                try
                {
                    if (!await MainStaticClass.SendResultGetData())
                    {
                        Console.WriteLine("WARNING: Не удалось отправить информацию об успешной загрузке");
                    }
                }
                catch { }

                return (true, "");
            }
            catch (NpgsqlException ex)
            {
                string errorMsg = $"Ошибка базы данных: {ex.Message}";
                Console.WriteLine($"Ошибка Npgsql: {ex.Message}");

                if (tran != null)
                {
                    try { await tran.RollbackAsync(cancellationToken); } catch { }
                }

                return (false, errorMsg);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка при сохранении данных: {ex.Message}";
                Console.WriteLine($"Ошибка: {ex.Message}");

                if (tran != null)
                {
                    try { await tran.RollbackAsync(cancellationToken); } catch { }
                }

                return (false, errorMsg);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    try { await conn.CloseAsync(); } catch { }
                }

                conn?.Dispose();
                tran?.Dispose();
            }
        }

        // Оригинальный метод для обратной совместимости
        private async Task<(bool success, string errorMessage)> SaveDataToDatabaseAsync(
            LoadPacketData loadPacketData,
            CancellationToken cancellationToken)
        {
            // Вызываем новую перегрузку со значениями по умолчанию
            return await SaveDataToDatabaseAsync(loadPacketData, cancellationToken, 0, 100);
        }

        private void PrepareDatabaseQueries(LoadPacketData loadPacketData, List<string> queries)
        {
            // Очистка таблиц
            queries.Add("DELETE FROM action_table");
            queries.Add("DELETE FROM action_header");
            queries.Add("DELETE FROM advertisement");

            // Обновление токена
            if (!string.IsNullOrEmpty(loadPacketData.TokenMark))
            {
                queries.Add($"UPDATE constants SET cdn_token='{EscapeSql(loadPacketData.TokenMark)}'");
            }

            // Создание временной таблицы для товаров
            queries.Add("DELETE FROM tovar2");

            // Вставка товаров во временную таблицу
            if (loadPacketData.ListTovar?.Count > 0)
            {
                foreach (var tovar in loadPacketData.ListTovar)
                {
                    queries.Add($@"
                        INSERT INTO tovar2(code,name,retail_price,its_deleted,nds,its_certificate,
                        percent_bonus,tnved,its_marked,its_excise,cdn_check,fractional,
                        refusal_of_marking,rr_not_control_owner) 
                        VALUES({tovar.Code},'{EscapeSql(tovar.Name)}',{tovar.RetailPrice},{tovar.ItsDeleted},
                        {tovar.Nds},{tovar.ItsCertificate},{tovar.PercentBonus},'{EscapeSql(tovar.TnVed)}',
                        {tovar.ItsMarked},{tovar.ItsExcise},{tovar.CdnCheck},{tovar.Fractional},
                        {tovar.RefusalOfMarking},{tovar.RrNotControlOwner})");
                }
            }

            // Обновление основной таблицы товаров
            queries.Add("UPDATE tovar SET its_deleted=1, retail_price=0");
            queries.Add(GetInsertQuery());
            queries.Add(GetUpdateQuery());
            queries.Add("DELETE FROM tovar2");
            queries.Add("DELETE FROM barcode");

            // Вставка штрихкодов
            if (loadPacketData.ListBarcode?.Count > 0)
            {
                foreach (var barcode in loadPacketData.ListBarcode)
                {
                    queries.Add($"INSERT INTO barcode(tovar_code,barcode) VALUES({barcode.TovarCode},'{EscapeSql(barcode.BarCode)}')");
                }
            }

            // Вставка характеристик
            if (loadPacketData.ListCharacteristic?.Count > 0)
            {
                queries.Add("DELETE FROM characteristic");
                foreach (var characteristic in loadPacketData.ListCharacteristic)
                {
                    queries.Add($@"
                        INSERT INTO characteristic(tovar_code, guid, name, retail_price_characteristic) 
                        VALUES({characteristic.CodeTovar},'{EscapeSql(characteristic.Guid)}','{EscapeSql(characteristic.Name)}',
                        {characteristic.RetailPrice})");
                }
            }

            // Вставка сертификатов
            queries.Add("DELETE FROM sertificates");
            if (loadPacketData.ListSertificate?.Count > 0)
            {
                foreach (var sertificate in loadPacketData.ListSertificate)
                {
                    queries.Add($@"
                        INSERT INTO sertificates(code, code_tovar, rating, is_active)
                        VALUES({sertificate.Code},{sertificate.CodeTovar},{sertificate.Rating},
                        {sertificate.IsActive})");
                }
            }

            // Вставка акций
            if (loadPacketData.ListActionHeader?.Count > 0)
            {
                foreach (var actionHeader in loadPacketData.ListActionHeader)
                {
                    queries.Add($@"
                        INSERT INTO action_header(date_started,date_end,num_doc,tip,barcode,persent,sum,
                        comment,marker,action_by_discount,time_start,time_end,bonus_promotion,
                        with_old_promotion,monday,tuesday,wednesday,thursday,friday,saturday,sunday,
                        promo_code,sum_bonus,execution_order,gift_price,kind,sum1,picture)
                        VALUES('{actionHeader.DateStarted}','{actionHeader.DateEnd}',{actionHeader.NumDoc},
                        {actionHeader.Tip},'{EscapeSql(actionHeader.Barcode)}',{actionHeader.Persent},{actionHeader.sum},
                        '{EscapeSql(actionHeader.Comment)}',{actionHeader.Marker},{actionHeader.ActionByDiscount},
                        {actionHeader.TimeStart},{actionHeader.TimeEnd},{actionHeader.BonusPromotion},
                        {actionHeader.WithOldPromotion},{actionHeader.Monday},{actionHeader.Tuesday},
                        {actionHeader.Wednesday},{actionHeader.Thursday},{actionHeader.Friday},
                        {actionHeader.Saturday},{actionHeader.Sunday},{actionHeader.PromoCode},
                        {actionHeader.SumBonus},{actionHeader.ExecutionOrder},{actionHeader.GiftPrice},
                        {actionHeader.Kind},{actionHeader.sum1},'{EscapeSql(actionHeader.Picture)}')");
                }
            }
            else
            {
                // Уведомление показывается через MessageBox.Show
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await MessageBox.Show(
                            "Нет данных по акциям",
                            "Проверка наличия акций",
                            MessageBoxButton.OK,
                            MessageBoxType.Info,
                            this);
                    }
                    catch { }
                });
            }

            // Вставка табличных данных акций
            if (loadPacketData.ListActionTable?.Count > 0)
            {
                foreach (var actionTable in loadPacketData.ListActionTable)
                {
                    queries.Add($@"
                        INSERT INTO action_table(num_doc, num_list, code_tovar, price)
                        VALUES({actionTable.NumDoc},{actionTable.NumList},{actionTable.CodeTovar},
                        {actionTable.Price})");
                }
            }

            // Вставка рекламных текстов
            if (loadPacketData.ListPromoText?.Count > 0)
            {
                foreach (var promoText in loadPacketData.ListPromoText)
                {
                    queries.Add($@"
                        INSERT INTO advertisement(advertisement_text,num_str,picture)
                        VALUES('{EscapeSql(promoText.AdvertisementText)}',{promoText.NumStr},'{EscapeSql(promoText.Picture)}')");
                }
            }

            // Вставка клиентов акций
            queries.Add("DELETE FROM action_clients");
            if (loadPacketData.ListActionClients?.Count > 0)
            {
                foreach (var actionClients in loadPacketData.ListActionClients)
                {
                    queries.Add($@"
                        INSERT INTO action_clients(num_doc, code_client)
                        VALUES({actionClients.NumDoc},{actionClients.CodeClient})");
                }
            }
        }

        private string EscapeSql(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Replace("'", "''");
        }

        private async Task FinalizeLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(200, cancellationToken);

                if (CheckFirstLoadData())
                {
                    // Убрал Dispatcher - MessageBox.Show сам обрабатывает UI поток
                    await MessageBox.Show(
                        "Это была первая загрузка данных. Для применения новых параметров программа будет перезапущена.",
                        "Первая загрузка",
                        MessageBoxButton.OK,
                        MessageBoxType.Info,
                        this);
                }

                await Task.Run(() =>
                {
                    try
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        GC.WaitForPendingFinalizers();
                    }
                    catch { }
                }, cancellationToken);
            }
            catch { }
        }

        #endregion

        #region Метод load_bonus_clients

        public async Task load_bonus_clients(bool show_message)
        {
            await Task.Run(() => load_bonus_clients_internal(show_message));
        }

        private async Task load_bonus_clients_internal(bool show_message)
        {
            try
            {
                if (!MainStaticClass.service_is_worker())
                {
                    if (show_message)
                    {
                        await MessageBox.Show("Веб сервис недоступен", "Ошибка", owner: this);
                    }
                    return;
                }

                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 60000;

                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0)
                {
                    if (show_message)
                    {
                        await MessageBox.Show("Не удалось получить название магазина", "Ошибка", owner: this);
                    }
                    return;
                }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0)
                {
                    if (show_message)
                    {
                        await MessageBox.Show("Не удалось получить код магазина", "Ошибка", owner: this);
                    }
                    return;
                }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
                DateTime dt = last_date_download_bonus_clients();

                string data = CryptorEngine.Encrypt(nick_shop + "|" + dt.Ticks.ToString() + "|" + code_shop, true, key);

                string result_query = "-1";
                try
                {
                    result_query = ds.GetDiscountClientsV8DateTime_NEW(nick_shop, data, "4");
                }
                catch (Exception ex)
                {
                    await MessageBox.Show(ex.Message, "Ошибка", owner: this);
                }

                if (result_query == "-1")
                {
                    if (show_message)
                    {
                        await MessageBox.Show("При обработке запроса на сервере произошли ошибки", "Ошибка", owner: this);
                    }
                    return;
                }

                string result_query_decrypt = CryptorEngine.Decrypt(result_query, true, key);
                Clients clients = JsonConvert.DeserializeObject<Clients>(result_query_decrypt);

                if (clients.list_clients.Count == 0)
                {
                    return;
                }

                NpgsqlConnection conn = null;
                NpgsqlTransaction tran = null;
                string query = "";

                try
                {
                    conn = MainStaticClass.NpgsqlConn();
                    conn.Open();
                    tran = conn.BeginTransaction();
                    NpgsqlCommand command = null;
                    string local_last_date_download_bonus_clients = "";

                    foreach (Client client in clients.list_clients)
                    {
                        query = "UPDATE clients SET " +
                            " phone='" + client.phone + "'," +
                            " name='" + client.name + "'," +
                            " date_of_birth='" + client.holiday + "'," +
                            " its_work='" + client.use_blocked + "'," +
                            " reason_for_blocking='" + client.reason_for_blocking + "'," +
                            " notify_security='" + client.notify_security + "' " +
                            " WHERE code='" + client.code + "';";

                        local_last_date_download_bonus_clients = client.datetime_update;

                        command = new NpgsqlCommand(query, conn);
                        command.Transaction = tran;
                        int rowsaffected = command.ExecuteNonQuery();
                        if (rowsaffected == 0)
                        {
                            query = "INSERT INTO clients(code,phone,name, date_of_birth,its_work,reason_for_blocking,notify_security)VALUES('" +
                                client.code + "','" +
                                client.phone + "','" +
                                client.name + "','" +
                                client.holiday + "','" +
                                client.use_blocked + "','" +
                                client.reason_for_blocking + "','" +
                                client.notify_security + "')";
                            command = new NpgsqlCommand(query, conn);
                            command.Transaction = tran;
                            command.ExecuteNonQuery();
                        }
                    }

                    query = "UPDATE constants SET last_date_download_bonus_clients='" + local_last_date_download_bonus_clients + "'";
                    command = new NpgsqlCommand(query, conn);
                    command.Transaction = tran;
                    command.ExecuteNonQuery();

                    tran.Commit();
                    conn.Close();

                    if (show_message)
                    {
                        await MessageBox.Show("Клиенты успешно загружены", "Успех", owner: this);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (show_message)
                    {
                        await MessageBox.Show(query + "\n" + ex.Message, "Ошибка при импорте данных", owner: this);
                    }
                    if (tran != null)
                    {
                        tran.Rollback();
                    }
                }
                catch (Exception ex)
                {
                    if (show_message)
                    {
                        await MessageBox.Show(query + "\n" + ex.Message, "Ошибка при импорте данных", owner: this);
                    }
                    if (tran != null)
                    {
                        tran.Rollback();
                    }
                }
                finally
                {
                    if (conn != null && conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в load_bonus_clients: {ex.Message}");
            }
        }

        private DateTime last_date_download_bonus_clients()
        {
            DateTime result = new DateTime(2000, 1, 1);

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = "SELECT last_date_download_bonus_clients FROM constants";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                object query_result = command.ExecuteScalar();
                if (query_result != null)
                {
                    result = Convert.ToDateTime(query_result);
                }
                conn.Close();
            }
            catch (NpgsqlException) { }
            catch (Exception) { }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }

        #endregion

        #region Вспомогательные методы из оригинального кода

        private void check_temp_tables()
        {
            try
            {
                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    string sql = @"
                    DROP TABLE IF EXISTS tovar2;
                    CREATE TABLE tovar2(
                        code bigint NOT NULL, name character(100) NOT NULL,
                        retail_price numeric(10,2), purchase_price numeric(10,2),
                        its_deleted numeric(1), nds integer, its_certificate smallint,
                        percent_bonus numeric(8,2), tnved character varying(10),
                        its_marked smallint, its_excise smallint, cdn_check boolean,
                        fractional boolean NOT NULL DEFAULT false,
                        refusal_of_marking boolean NOT NULL DEFAULT false,
                        rr_not_control_owner boolean NOT NULL DEFAULT false
                    ) WITH (OIDS=FALSE);
                    ALTER TABLE tovar2 OWNER TO postgres;
                    CREATE UNIQUE INDEX _tovar2_code_ ON tovar2 USING btree (code);";

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании таблицы tovar2: {ex.Message}");
            }
        }

        private LoadPacketData getLoadPacketDataFull(string nick_shop, string data_encrypt, string key)
        {
            LoadPacketData loadPacketData = new LoadPacketData();
            loadPacketData.PacketIsFull = false;

            string result_query = "";
            string decrypt_data = "";
            try
            {
                using (DS ds = MainStaticClass.get_ds())
                {
                    ds.Timeout = 60000;
                    byte[] result_query_byte = ds.GetDataForCasheV8Jason(nick_shop, data_encrypt, MainStaticClass.GetWorkSchema.ToString());
                    result_query = DecompressString(result_query_byte);
                    decrypt_data = CryptorEngine.Decrypt(result_query, true, key);
                    loadPacketData = JsonConvert.DeserializeObject<LoadPacketData>(decrypt_data);
                }
            }
            catch (Exception ex)
            {
                loadPacketData.Exception = ex.Message;
                loadPacketData.PacketIsFull = false;
            }
            return loadPacketData;
        }

        private string DecompressString(byte[] value)
        {
            string resultString = string.Empty;
            if (value != null && value.Length > 0)
            {
                using (MemoryStream stream = new MemoryStream(value))
                using (GZipStream zip = new GZipStream(stream, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(zip))
                {
                    resultString = reader.ReadToEnd();
                }
            }
            return resultString;
        }

        private string GetInsertQuery()
        {
            return @"
            INSERT INTO tovar 
            SELECT F.code, F.name, F.retail_price, F.its_deleted, F.nds, 
                   F.its_certificate, F.percent_bonus, F.tnved, F.its_marked,
                   F.its_excise, F.cdn_check, F.fractional, F.refusal_of_marking,
                   F.rr_not_control_owner
            FROM (
                SELECT t2.code, t.code AS code2, t2.name, t2.retail_price, 
                       t2.its_deleted, t2.nds, t2.its_certificate, t2.percent_bonus, 
                       t2.tnved, t2.its_marked, t2.its_excise, t2.cdn_check, 
                       t2.fractional, t2.refusal_of_marking,t2.rr_not_control_owner
                FROM tovar2 t2 
                LEFT JOIN tovar t ON t2.code = t.code
            ) AS F 
            WHERE code2 IS NULL;";
        }

        private string GetUpdateQuery()
        {
            return @"
            UPDATE tovar 
            SET name = t2.name,
                retail_price = t2.retail_price,
                its_deleted = t2.its_deleted,
                nds = t2.nds,
                its_certificate = t2.its_certificate,
                percent_bonus = t2.percent_bonus,
                tnved = t2.tnved,
                its_marked = t2.its_marked,
                its_excise = t2.its_excise,
                cdn_check = t2.cdn_check,
                fractional = t2.fractional,
                refusal_of_marking = t2.refusal_of_marking,
                rr_not_control_owner = t2.rr_not_control_owner
            FROM tovar2 t2 
            WHERE tovar.code = t2.code;";
        }

        public static DateTime last_date_download_tovars()
        {
            DateTime result = new DateTime(2000, 1, 1);

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = "SELECT tovar FROM date_sync";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                object query_result = command.ExecuteScalar();
                if (query_result != null)
                {
                    result = Convert.ToDateTime(query_result);
                }
                conn.Close();
            }
            catch (NpgsqlException)
            {

            }
            catch (Exception)
            {

            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();

                }
            }

            return result;
        }

        private bool CheckFirstLoadData()
        {
            bool result = false;

            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    string query = "SELECT tovar FROM public.date_sync";
                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        object resultQuery = command.ExecuteScalar();
                        if (resultQuery != null && DateTime.TryParse(resultQuery.ToString(), out DateTime date))
                        {
                            if (date < new DateTime(2001, 1, 1))
                            {
                                result = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке первой загрузки: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Методы для работы с UI

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_btn_new_load != null)
                {
                    if (isLoading)
                    {
                        _btn_new_load.Background = new SolidColorBrush(Color.Parse("#4CAF50"));
                        _btn_new_load.Content = "Идет загрузка...";
                        _btn_new_load.Cursor = new Cursor(StandardCursorType.Wait); // Курсор-часики
                        _btn_new_load.Click -= Btn_new_load_Click;
                    }
                    else
                    {
                        _btn_new_load.Background = new SolidColorBrush(Color.Parse("#2196F3"));
                        _btn_new_load.Content = "Начать загрузку данных";
                        _btn_new_load.Cursor = new Cursor(StandardCursorType.Hand); // Обычная рука
                        _btn_new_load.Click += Btn_new_load_Click;
                    }
                }

                if (_progressPanel != null)
                    _progressPanel.IsVisible = isLoading;

                if (_timeInfoText != null)
                    _timeInfoText.IsVisible = isLoading;

                if (isLoading)
                {
                    this.CanResize = false;
                }
                else
                {
                    this.CanResize = true;
                    if (_progressBar1 != null)
                    {
                        _progressBar1.Value = 0;
                        _progressBar1.IsIndeterminate = false;
                    }
                    if (_statusText != null)
                        _statusText.Text = "";
                    if (_progressPercent != null)
                        _progressPercent.Text = "0%";
                }
            }).Wait();
        }

        private async Task UpdateProgressAsync(string message, int progress)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_statusText != null)
                    _statusText.Text = message;

                if (_progressBar1 != null)
                {
                    _progressBar1.IsIndeterminate = false;
                    _progressBar1.Value = progress;
                }

                if (_progressPercent != null)
                    _progressPercent.Text = $"{progress}%";
            });
        }

        private async Task HandleTimeoutAsync()
        {
            _cancellationTokenSource?.Cancel();

            await MessageBox.Show(
                $"Загрузка превысила лимит времени ({_loadTimeout.TotalMinutes} минут)",
                "Таймаут",
                owner: this);
        }

        private async Task HandleLoadResultAsync(bool success, string errorMessage)
        {
            if (success)
            {
                await MessageBox.Show(
                    "Загрузка данных успешно завершена",
                    "Успех",
                    owner: this);
            }
            else
            {
                string message = "Не удалось выполнить загрузку данных";
                if (!string.IsNullOrEmpty(errorMessage))
                    message += $"\n\nПричина: {errorMessage}";

                await MessageBox.Show(
                    message,
                    "Ошибка",
                    owner: this);
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (_isLoading)
            {
                e.Cancel = true;
                ShowCancelDialog();
            }

            base.OnClosing(e);
        }



        private async void ShowCancelDialog()
        {
            var result = await MessageBox.Show(
                "Идет загрузка данных. Вы уверены, что хотите отменить?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxType.Warning,
                this);

            if (result == MessageBoxResult.Yes)
            {
                _userCancelled = true;
                _cancellationTokenSource?.Cancel();

                // Закрываем окно после небольшой задержки для корректной отмены
                await Task.Delay(100);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #endregion
    }
}