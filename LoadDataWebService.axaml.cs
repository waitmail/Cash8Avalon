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
using System.Linq;
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
        private TextBlock _lastUpdateText;

        // Состояние загрузки
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isLoading = false;
        private readonly TimeSpan _loadTimeout = TimeSpan.FromMinutes(30);
        private Timer _timer;
        private Stopwatch _stopwatch;
        private bool _userCancelled = false;
        private TextBlock _workHintText;
        //private const bool USE_OPTIMIZED_SYNC = true;
        private CheckBox _useOptimizedSyncCheckBox;

        private bool _useOptimizedSync = true;

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

            _lastUpdateText = this.FindControl<TextBlock>("lastUpdateText");
            _workHintText = this.FindControl<TextBlock>("workHintText");

            // ⬇⬇⬇ ДОБАВИТЬ ЭТУ СТРОКУ ⬇⬇⬇
            _useOptimizedSyncCheckBox = this.FindControl<CheckBox>("useOptimizedSyncCheckBox");

            UpdateLastSyncDate();

            if (_btn_new_load != null)
                _btn_new_load.Click += Btn_new_load_Click;

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

            public void Dispose() { }
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

            // ⬇⬇⬇ ДОБАВИТЬ ЭТИ ДВЕ СТРОКИ ⬇⬇⬇
            if (_useOptimizedSyncCheckBox != null)
                _useOptimizedSync = _useOptimizedSyncCheckBox.IsChecked ?? true;

            _userCancelled = false;
            _cancellationTokenSource = new CancellationTokenSource();
            _stopwatch = Stopwatch.StartNew();

            try
            {
                await SetLoadingStateAsync(true);
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
                await SetLoadingStateAsync(false);
                StopTimer();
                _stopwatch?.Stop();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task SetLoadingStateAsync(bool isLoading)
        {
            _isLoading = isLoading;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_btn_new_load != null)
                {
                    if (isLoading)
                    {
                        _btn_new_load.Background = new SolidColorBrush(Color.Parse("#4CAF50"));
                        _btn_new_load.Content = "Идет загрузка...";
                        _btn_new_load.Cursor = new Cursor(StandardCursorType.Wait);
                        _btn_new_load.Click -= Btn_new_load_Click;
                    }
                    else
                    {
                        _btn_new_load.Background = new SolidColorBrush(Color.Parse("#2196F3"));
                        _btn_new_load.Content = "Начать загрузку данных";
                        _btn_new_load.Cursor = new Cursor(StandardCursorType.Hand);
                        _btn_new_load.Click += Btn_new_load_Click;
                    }
                }

                if (_progressPanel != null)
                    _progressPanel.IsVisible = isLoading;

                if (_workHintText != null)
                    _workHintText.IsVisible = isLoading;

                // ⬇⬇⬇ ДОБАВИТЬ ЭТИ ДВЕ СТРОКИ ⬇⬇⬇
                if (_useOptimizedSyncCheckBox != null)
                    _useOptimizedSyncCheckBox.IsEnabled = !isLoading;

                if (_timeInfoText != null)
                {
                    if (isLoading)
                    {
                        _timeInfoText.Text = "Время загрузки: 00:00";
                        _timeInfoText.IsVisible = true;
                    }
                }

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
            });
        }

        private async Task<(bool success, string errorMessage)> PerformFullLoadAsync(CancellationToken cancellationToken)
        {
            string errorMessage = "";

            try
            {
                await UpdateProgressAsync("Подготовка к загрузке...", 0);
                await PrepareForLoadAsync(cancellationToken, skipClearMemory: true);

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                await UpdateProgressAsync("Проверка соединения с веб-сервисом...", 5);
                if (!await CheckServiceAvailabilityAsync(cancellationToken))
                {
                    return (false, "Веб-сервис недоступен");
                }

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                await UpdateProgressAsync("Подготовка временных таблиц...", 10);
                await CreateTempTablesAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

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

                await UpdateProgressAsync("Подготовка к сохранению данных...", 20);

                var saveResult = await SaveDataToDatabaseAsync(serverData.data, cancellationToken, 20, 80);
                if (!saveResult.success)
                {
                    return (false, saveResult.errorMessage);
                }

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                await UpdateProgressAsync("Завершение операций с базой данных...", 85);
                await FinalizeLoadAsync(cancellationToken);

                if (MainStaticClass.CashDeskNumber != 9)
                {
                    if (!await MainStaticClass.SendResultGetData(MainStaticClass.MainWindow))
                    {
                        MainStaticClass.write_event_in_log("Не удалось отправить информацию об успешной загрузке ",
                            "Загрузка данных", "0");
                    }
                }

                await UpdateProgressAsync("Обновление данных в памяти...", 90);
                var memoryResult = await RefreshMemoryDataAsync(cancellationToken);
                if (!memoryResult.success)
                {
                    errorMessage = memoryResult.errorMessage;
                    Console.WriteLine($"Предупреждение при обновлении памяти: {errorMessage}");
                }

                if (cancellationToken.IsCancellationRequested)
                    return (false, "Операция отменена");

                await UpdateProgressAsync("Готово", 100);
                UpdateLastSyncDate();

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
                InventoryManager.SetOwnerWindow(this);
                await UpdateProgressAsync("Очистка кэша в памяти...", 85);
                InventoryManager.ClearDictionaryProductData();

                await Task.Delay(100, cancellationToken);

                await UpdateProgressAsync("Загрузка товаров в память...", 90);
                try
                {
                    await InventoryManager.FillDictionaryProductDataAsync(this);
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка при загрузке товаров в память: {ex.Message}");
                }

                await UpdateProgressAsync("Загрузка цен подарков...", 93);
                try
                {
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
                    Console.WriteLine($"Предупреждение при фоновой загрузке цен: {ex.Message}");
                }

                await Task.Delay(200, cancellationToken);
                if (!InventoryManager.IsDictionaryValid)
                {
                    return (false, "Кэш товаров не был успешно загружен");
                }

                await UpdateProgressAsync("Загрузка условий акций...", 96);
                try
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            LoadActionDataInMemory.StartRefresh();
                            _ = LoadActionDataInMemory.AllActionData1;
                            _ = LoadActionDataInMemory.AllActionData2;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Предупреждение: не удалось загрузить условия акций: {ex.Message}");
                        }
                        finally
                        {
                            LoadActionDataInMemory.FinishRefresh();
                        }
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Предупреждение при фоновой загрузке условий акций: {ex.Message}");
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
                if (!skipClearMemory)
                {
                    try
                    {
                        InventoryManager.ClearDictionaryProductData();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при очистке кэша: {ex.Message}");
                    }
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

            if (_stopwatch != null)
            {
                _stopwatch.Stop();
                var elapsed = _stopwatch.Elapsed;

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_timeInfoText != null)
                    {
                        _timeInfoText.Text = $"Общее время загрузки: {elapsed:mm\\:ss}";
                        _timeInfoText.IsVisible = true;
                    }
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

                    var loadPacketData = await getLoadPacketDataFullAsync(nick_shop, data_encrypt, key);

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
            catch (OperationCanceledException)
            {
                return (false, null, "Операция отменена пользователем");
            }
            catch (Exception ex)
            {
                return (false, null, $"Ошибка при получении данных с сервера: {ex.Message}");
            }
        }

        private async Task<(bool success, string errorMessage)> SaveDataToDatabaseAsync(
            LoadPacketData loadPacketData,
            CancellationToken cancellationToken,
            int startProgress,
            int endProgress)
        {
            NpgsqlConnection conn = null;
            NpgsqlTransaction tran = null;
            string queryActual = "";

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                await conn.OpenAsync(cancellationToken);
                tran = await conn.BeginTransactionAsync(cancellationToken);

                var queries = new List<string>();
                PrepareDatabaseQueries(loadPacketData, queries);

                int progressRange = endProgress - startProgress;

                int tovarEndProgress;
                int queryStartProgress, queryEndProgress;
                int actionTableStartProgress, actionTableEndProgress;
                int sertStartProgress, sertEndProgress;
                int barcodeStartProgress, barcodeEndProgress;

                if (_useOptimizedSync)
                {
                    tovarEndProgress = startProgress + (int)(progressRange * 0.10);
                    queryStartProgress = tovarEndProgress;
                    queryEndProgress = startProgress + (int)(progressRange * 0.25);
                    actionTableStartProgress = queryEndProgress;
                    actionTableEndProgress = startProgress + (int)(progressRange * 0.35);
                    sertStartProgress = actionTableEndProgress;
                    sertEndProgress = startProgress + (int)(progressRange * 0.60);
                    barcodeStartProgress = sertEndProgress;
                    barcodeEndProgress = endProgress;
                }
                else
                {
                    tovarEndProgress = startProgress;
                    queryStartProgress = startProgress;
                    queryEndProgress = endProgress;
                    actionTableStartProgress = actionTableEndProgress = endProgress;
                    sertStartProgress = sertEndProgress = endProgress;
                    barcodeStartProgress = barcodeEndProgress = endProgress;
                }

                int queryProgressRange = queryEndProgress - queryStartProgress;

                if (_useOptimizedSync)
                {
                    await SyncTovarsAsync(
                        loadPacketData.ListTovar,
                        conn,
                        tran,
                        cancellationToken,
                        startProgress,
                        tovarEndProgress);
                }

                int totalQueries = queries.Count;
                int completedQueries = 0;

                foreach (string query in queries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    queryActual = query;

                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        command.Transaction = tran;
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }

                    completedQueries++;
                    double progressPercentage = (double)completedQueries / totalQueries;
                    int currentProgress = queryStartProgress + (int)(progressPercentage * queryProgressRange);
                    await UpdateProgressAsync($"Выполнение запросов ({completedQueries}/{totalQueries})...", currentProgress);
                }

                if (_useOptimizedSync)
                {
                    await SyncActionTableAsync(
                        loadPacketData.ListActionTable,
                        conn,
                        tran,
                        cancellationToken,
                        actionTableStartProgress,
                        actionTableEndProgress);
                }

                if (_useOptimizedSync)
                {
                    await SyncSertificatesAsync(
                        loadPacketData.ListSertificate,
                        conn,
                        tran,
                        cancellationToken,
                        sertStartProgress,
                        sertEndProgress);
                }

                if (_useOptimizedSync)
                {
                    await SyncBarcodesAsync(
                        loadPacketData.ListBarcode,
                        conn,
                        tran,
                        cancellationToken,
                        barcodeStartProgress,
                        barcodeEndProgress);
                }

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

                return (true, "");
            }
            catch (NpgsqlException ex)
            {
                string errorMsg = $"Ошибка базы данных: {ex.Message}";
                Console.WriteLine($"Ошибка Npgsql: {ex.Message}");
                Console.WriteLine($"Query: {queryActual}");

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBox.Show($"Ошибка базы данных: {ex.Message}\n\nQuery: {queryActual}", 
                        "Ошибка при загрузке", MessageBoxButton.OK, MessageBoxType.Error, MainStaticClass.MainWindow);
                });

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

        private void PrepareDatabaseQueries(LoadPacketData loadPacketData, List<string> queries)
        {
            queries.Add("DELETE FROM action_table");
            queries.Add("DELETE FROM action_header");
            queries.Add("DELETE FROM advertisement");

            if (!string.IsNullOrEmpty(loadPacketData.TokenMark))
            {
                queries.Add($"UPDATE constants SET cdn_token='{EscapeSql(loadPacketData.TokenMark)}'");
            }

            if (_useOptimizedSync)
            {
                queries.Add("UPDATE tovar SET its_deleted=1, retail_price=0");
                queries.Add(GetInsertQuery());
                queries.Add(GetUpdateQuery());
                queries.Add("DELETE FROM tovar2");
            }
            else
            {
                queries.Add("DELETE FROM tovar2");

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

                queries.Add("UPDATE tovar SET its_deleted=1, retail_price=0");
                queries.Add(GetInsertQuery());
                queries.Add(GetUpdateQuery());
                queries.Add("DELETE FROM tovar2");
            }

            if (_useOptimizedSync)
            {
                // Обрабатывается в SaveDataToDatabaseAsync
            }
            else
            {
                queries.Add("DELETE FROM barcode");

                if (loadPacketData.ListBarcode?.Count > 0)
                {
                    foreach (var barcode in loadPacketData.ListBarcode)
                    {
                        queries.Add($"INSERT INTO barcode(tovar_code,barcode) VALUES({barcode.TovarCode},'{EscapeSql(barcode.BarCode)}')");
                    }
                }
            }

            if (_useOptimizedSync)
            {
                // Обрабатывается в SaveDataToDatabaseAsync
            }
            else
            {
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
            }

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
                _ = Task.Run(async () =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
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
                });
            }

            if (_useOptimizedSync)
            {
                // Обрабатывается в SaveDataToDatabaseAsync
            }
            else
            {
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
            }

            if (loadPacketData.ListPromoText?.Count > 0)
            {
                foreach (var promoText in loadPacketData.ListPromoText)
                {
                    queries.Add($@"
                        INSERT INTO advertisement(advertisement_text,num_str,picture)
                        VALUES('{EscapeSql(promoText.AdvertisementText)}',{promoText.NumStr},'{EscapeSql(promoText.Picture)}')");
                }
            }

            queries.Add("DELETE FROM action_clients");
            // if (loadPacketData.ListActionClients?.Count > 0)
            // {
            //     foreach (var actionClients in loadPacketData.ListActionClients)
            //     {
            //         queries.Add($@"
            //             INSERT INTO action_clients(num_doc, code_client)
            //             VALUES({actionClients.NumDoc},{actionClients.CodeClient})");
            //     }
            // }
            if (loadPacketData.ListActionClients?.Count > 0)
            {
                foreach (var actionClients in loadPacketData.ListActionClients)
                {
                    // ✅ ИСПРАВЛЕНИЕ: Добавлены одинарные кавычки вокруг кода клиента и экранирование
                    queries.Add($@"
                        INSERT INTO action_clients(num_doc, code_client)
                        VALUES({actionClients.NumDoc},'{EscapeSql(actionClients.CodeClient)}')");
                }
            }
        }

        private async Task SyncTovarsAsync(
            List<Tovar> packetTovars,
            NpgsqlConnection conn,
            NpgsqlTransaction tran,
            CancellationToken cancellationToken,
            int startProgress,
            int endProgress)
        {
            int progressRange = endProgress - startProgress;

            if (packetTovars == null || packetTovars.Count == 0)
            {
                Console.WriteLine("[TOVAR SYNC] Пакет товаров пуст");
                return;
            }

            await UpdateProgressAsync(
                $"Товары: загрузка {packetTovars.Count} шт. во временную таблицу...",
                startProgress + progressRange * 5 / 100);

            using (var writer = await conn.BeginBinaryImportAsync(
                "COPY tovar2 (code, name, retail_price, its_deleted, nds, its_certificate, percent_bonus, tnved, its_marked, its_excise, cdn_check, fractional, refusal_of_marking, rr_not_control_owner) FROM STDIN (FORMAT BINARY)", cancellationToken))
            {
                foreach (var tovar in packetTovars)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.StartRow();

                    writer.Write(long.TryParse(tovar.Code, out long code) ? code : 0L, NpgsqlTypes.NpgsqlDbType.Bigint);
                    writer.Write(tovar.Name ?? string.Empty, NpgsqlTypes.NpgsqlDbType.Varchar);
                    writer.Write(decimal.TryParse(tovar.RetailPrice, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal rp) ? rp : 0m, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(decimal.TryParse(tovar.ItsDeleted, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal del) ? del : 0m, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(int.TryParse(tovar.Nds, out int nds) ? nds : 0, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(short.TryParse(tovar.ItsCertificate, out short cert) ? cert : (short)0, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(decimal.TryParse(tovar.PercentBonus, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal pb) ? pb : 0m, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(tovar.TnVed ?? string.Empty, NpgsqlTypes.NpgsqlDbType.Varchar);
                    writer.Write(short.TryParse(tovar.ItsMarked, out short marked) ? marked : (short)0, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(short.TryParse(tovar.ItsExcise, out short excise) ? excise : (short)0, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(tovar.CdnCheck == "1" || tovar.CdnCheck?.ToLower() == "true", NpgsqlTypes.NpgsqlDbType.Boolean);
                    writer.Write(tovar.Fractional == "1" || tovar.Fractional?.ToLower() == "true", NpgsqlTypes.NpgsqlDbType.Boolean);
                    writer.Write(tovar.RefusalOfMarking == "1" || tovar.RefusalOfMarking?.ToLower() == "true", NpgsqlTypes.NpgsqlDbType.Boolean);
                    writer.Write(tovar.RrNotControlOwner == "1" || tovar.RrNotControlOwner?.ToLower() == "true", NpgsqlTypes.NpgsqlDbType.Boolean);
                }

                await writer.CompleteAsync(cancellationToken);
            }

            Console.WriteLine($"[TOVAR SYNC] Загружено товаров через COPY: {packetTovars.Count}");
        }

        private async Task SyncActionTableAsync(
            List<ActionTable> packetActionTables,
            NpgsqlConnection conn,
            NpgsqlTransaction tran,
            CancellationToken cancellationToken,
            int startProgress,
            int endProgress)
        {
            int progressRange = endProgress - startProgress;

            if (packetActionTables == null || packetActionTables.Count == 0)
            {
                Console.WriteLine("[ACTION_TABLE SYNC] Пакет пуст");
                return;
            }

            await UpdateProgressAsync(
                $"Условия акций: загрузка {packetActionTables.Count} шт...",
                startProgress + progressRange * 5 / 100);

            using (var writer = await conn.BeginBinaryImportAsync(
                "COPY action_table (num_doc, num_list, code_tovar, price) FROM STDIN (FORMAT BINARY)", cancellationToken))
            {
                foreach (var at in packetActionTables)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.StartRow();

                    if (int.TryParse(at.NumDoc, out int numDoc))
                        writer.Write(numDoc, NpgsqlTypes.NpgsqlDbType.Integer);
                    else
                        writer.WriteNull();

                    if (int.TryParse(at.NumList, out int numList))
                        writer.Write(numList, NpgsqlTypes.NpgsqlDbType.Integer);
                    else
                        writer.WriteNull();

                    if (long.TryParse(at.CodeTovar, out long codeTovar))
                        writer.Write(codeTovar, NpgsqlTypes.NpgsqlDbType.Bigint);
                    else
                        writer.WriteNull();

                    if (decimal.TryParse(at.Price, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                        writer.Write(price, NpgsqlTypes.NpgsqlDbType.Numeric);
                    else
                        writer.Write(0m, NpgsqlTypes.NpgsqlDbType.Numeric);
                }

                await writer.CompleteAsync(cancellationToken);
            }

            Console.WriteLine($"[ACTION_TABLE SYNC] Загружено условий акций через COPY: {packetActionTables.Count}");
        }

        private async Task SyncSertificatesAsync(
            List<Sertificate> packetSertificates,
            NpgsqlConnection conn,
            NpgsqlTransaction tran,
            CancellationToken cancellationToken,
            int startProgress,
            int endProgress)
        {
            int progressRange = endProgress - startProgress;

            if (packetSertificates == null || packetSertificates.Count == 0)
            {
                using var cmdDeleteAll = new NpgsqlCommand("DELETE FROM sertificates", conn, tran);
                await cmdDeleteAll.ExecuteNonQueryAsync(cancellationToken);
                Console.WriteLine("[SERT SYNC] Пакет пуст — все сертификаты удалены");
                return;
            }

            await UpdateProgressAsync("Сертификаты: дедупликация пакета...", startProgress);

            var uniqueSertificates = packetSertificates
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && !string.IsNullOrWhiteSpace(s.CodeTovar))
                .GroupBy(s => new { s.Code, s.CodeTovar })
                .Select(g => g.First())
                .ToList();

            if (uniqueSertificates.Count == 0)
            {
                using var cmdDeleteAll2 = new NpgsqlCommand("DELETE FROM sertificates", conn, tran);
                await cmdDeleteAll2.ExecuteNonQueryAsync(cancellationToken);
                Console.WriteLine("[SERT SYNC] После фильтрации пакет пуст — все сертификаты удалены");
                return;
            }

            await UpdateProgressAsync("Сертификаты: подготовка временной таблицы...",
                startProgress + progressRange * 2 / 100);

            using (var cmdCreateTemp = new NpgsqlCommand(@"
        CREATE TEMP TABLE sertificates_temp (
            code bigint,
            code_tovar bigint,
            rating numeric,
            is_active smallint
        ) ON COMMIT DROP", conn, tran))
            {
                await cmdCreateTemp.ExecuteNonQueryAsync(cancellationToken);
            }

            await UpdateProgressAsync(
                $"Сертификаты: загрузка {uniqueSertificates.Count} шт. во временную таблицу...",
                startProgress + progressRange * 5 / 100);

            using (var writer = await conn.BeginBinaryImportAsync(
                "COPY sertificates_temp (code, code_tovar, rating, is_active) FROM STDIN (FORMAT BINARY)", cancellationToken))
            {
                foreach (var sert in uniqueSertificates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.StartRow();

                    if (long.TryParse(sert.Code, out long certCode))
                        writer.Write(certCode, NpgsqlTypes.NpgsqlDbType.Bigint);
                    else
                        writer.WriteNull();

                    if (long.TryParse(sert.CodeTovar, out long tovarCode))
                        writer.Write(tovarCode, NpgsqlTypes.NpgsqlDbType.Bigint);
                    else
                        writer.WriteNull();

                    if (decimal.TryParse(sert.Rating, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal rating))
                        writer.Write(rating, NpgsqlTypes.NpgsqlDbType.Numeric);
                    else
                        writer.Write(0m, NpgsqlTypes.NpgsqlDbType.Numeric);

                    writer.Write(ParseSmallint(sert.IsActive), NpgsqlTypes.NpgsqlDbType.Smallint);
                }

                await writer.CompleteAsync(cancellationToken);
            }

            await UpdateProgressAsync("Сертификаты: индексация временной таблицы...",
                startProgress + progressRange * 55 / 100);

            using (var cmdIndex = new NpgsqlCommand(
                "CREATE INDEX idx_sertificates_temp ON sertificates_temp (code, code_tovar)", conn, tran))
            {
                await cmdIndex.ExecuteNonQueryAsync(cancellationToken);
            }

            await UpdateProgressAsync("Сертификаты: удаление устаревших...",
                startProgress + progressRange * 65 / 100);

            int deleted;
            using (var cmdDelete = new NpgsqlCommand(@"
        DELETE FROM sertificates 
        WHERE NOT EXISTS (
            SELECT 1 FROM sertificates_temp st 
            WHERE st.code = sertificates.code 
              AND st.code_tovar = sertificates.code_tovar
        )", conn, tran))
            {
                deleted = await cmdDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            await UpdateProgressAsync("Сертификаты: добавление новых...",
                startProgress + progressRange * 80 / 100);

            int inserted;
            using (var cmdInsert = new NpgsqlCommand(@"
        INSERT INTO sertificates (code, code_tovar, rating, is_active)
        SELECT st.code, st.code_tovar, st.rating, st.is_active 
        FROM sertificates_temp st
        WHERE NOT EXISTS (
            SELECT 1 FROM sertificates s 
            WHERE s.code = st.code 
              AND s.code_tovar = st.code_tovar
        )", conn, tran))
            {
                inserted = await cmdInsert.ExecuteNonQueryAsync(cancellationToken);
            }

            Console.WriteLine($"[SERT SYNC] Итого: в пакете {uniqueSertificates.Count}, удалено {deleted}, добавлено {inserted}");
        }

        private async Task SyncBarcodesAsync(
            List<Barcode> packetBarcodes,
            NpgsqlConnection conn,
            NpgsqlTransaction tran,
            CancellationToken cancellationToken,
            int startProgress,
            int endProgress)
        {
            int progressRange = endProgress - startProgress;

            await UpdateProgressAsync("Штрихкоды: очистка старых данных...", startProgress);

            using (var cmdDeleteAll = new NpgsqlCommand("DELETE FROM barcode", conn, tran))
            {
                await cmdDeleteAll.ExecuteNonQueryAsync(cancellationToken);
            }

            if (packetBarcodes == null || packetBarcodes.Count == 0)
            {
                Console.WriteLine("[BARCODE SYNC] Пакет пуст — таблица barcode очищена");
                return;
            }

            await UpdateProgressAsync("Штрихкоды: дедупликация пакета...",
                startProgress + progressRange * 5 / 100);

            var uniqueBarcodes = packetBarcodes
                .Where(b => !string.IsNullOrWhiteSpace(b.TovarCode)
                          && !string.IsNullOrWhiteSpace(b.BarCode))
                .GroupBy(b => new { b.TovarCode, b.BarCode })
                .Select(g => g.First())
                .ToList();

            if (uniqueBarcodes.Count == 0)
            {
                Console.WriteLine("[BARCODE SYNC] После фильтрации пакет пуст");
                return;
            }

            await UpdateProgressAsync(
                $"Штрихкоды: загрузка {uniqueBarcodes.Count} шт...",
                startProgress + progressRange * 20 / 100);

            using (var writer = await conn.BeginBinaryImportAsync(
                       "COPY barcode (tovar_code, barcode) FROM STDIN (FORMAT BINARY)", cancellationToken))
            {
                foreach (var barcode in uniqueBarcodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.StartRow();

                    if (long.TryParse(barcode.TovarCode, out long tovarCode))
                        writer.Write(tovarCode, NpgsqlTypes.NpgsqlDbType.Bigint);
                    else
                        writer.WriteNull();

                    writer.Write(barcode.BarCode, NpgsqlTypes.NpgsqlDbType.Text);
                }

                await writer.CompleteAsync(cancellationToken);
            }

            Console.WriteLine($"[BARCODE SYNC] Загружено штрихкодов через COPY: {uniqueBarcodes.Count}");
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
            await load_bonus_clients_internal(show_message);
        }

        private DateTime last_date_reset_bonus_clients()
        {
            DateTime result = DateTime.MinValue;
            try
            {
                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    string query = "SELECT last_date_reset_bonus_clients FROM constants LIMIT 1";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        object obj = cmd.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value)
                        {
                            result = Convert.ToDateTime(obj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка получения last_date_reset_bonus_clients");
            }
            return result;
        }

        private async Task load_bonus_clients_internal(bool show_message)
        {
            try
            {
                DS ds = await ServiceLocator.DsAsync();
                ds.Timeout = 60000;

                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (string.IsNullOrWhiteSpace(nick_shop))
                {
                    if (show_message) await MessageBox.Show("Не удалось получить название магазина", "Ошибка", owner: this);
                    return;
                }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (string.IsNullOrWhiteSpace(code_shop))
                {
                    if (show_message) await MessageBox.Show("Не удалось получить код магазина", "Ошибка", owner: this);
                    return;
                }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop + count_day + code_shop;

                bool needToLoadMore = true;
                int portionNumber = 1;
                
                DateTime resetDate = last_date_reset_bonus_clients();
                DateTime threeDaysAgo = DateTime.Today.AddDays(-2);
                bool isFullSyncRequested = (resetDate >= threeDaysAgo);

                while (needToLoadMore)
                {
                    Console.WriteLine($"--- Запрос порции № {portionNumber} ---");

                    DateTime dt = last_date_download_bonus_clients();
                    string data = CryptorEngine.Encrypt($"{nick_shop}|{dt.Ticks}|{code_shop}", true, key);

                    string result_query = "-1";
                    try
                    {
                        result_query = ds.GetDiscountClientsV8DateTime_NEW(nick_shop, data, "4");
                    }
                    catch (Exception ex)
                    {
                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "load_bonus_clients (веб-сервис)");
                        if (show_message) await MessageBox.Show(ex.Message, "Ошибка", owner: this);
                        break;
                    }

                    if (result_query == "-1")
                    {
                        Console.WriteLine("Веб-сервис вернул -1. Остановка.");
                        if (show_message) await MessageBox.Show("При обработке запроса на сервере произошли ошибки", "Ошибка", owner: this);
                        break;
                    }

                    string result_query_decrypt = CryptorEngine.Decrypt(result_query, true, key);
                    Clients clients = JsonConvert.DeserializeObject<Clients>(result_query_decrypt);

                    if (clients?.list_clients == null || clients.list_clients.Count == 0)
                    {
                        Console.WriteLine("Список клиентов пуст. Загрузка завершена.");
                        break;
                    }

                    Console.WriteLine($"Получено клиентов от сервера: {clients.list_clients.Count}");

                    DateTime? maxDateInPortion = clients.list_clients
                        .Where(c => !string.IsNullOrWhiteSpace(c.datetime_update))
                        .Select(c => DateTime.TryParse(c.datetime_update, out var parsedDate) ? parsedDate : (DateTime?)null)
                        .Max();

                    NpgsqlConnection conn = null;
                    NpgsqlTransaction tran = null;
                    Client failedClient = null;

                    try
                    {
                        conn = MainStaticClass.NpgsqlConn();
                        await conn.OpenAsync();
                        tran = await conn.BeginTransactionAsync();

                        const string upsertQuery = @"
                    INSERT INTO clients (code, phone, name, date_of_birth, its_work, reason_for_blocking, notify_security, last_server_sync)
                    VALUES (@code, @phone, @name, @date_of_birth, @its_work, @reason_for_blocking, @notify_security, NOW())
                    ON CONFLICT (code) DO UPDATE SET 
                        phone = EXCLUDED.phone,
                        name = EXCLUDED.name,
                        date_of_birth = EXCLUDED.date_of_birth,
                        its_work = EXCLUDED.its_work,
                        reason_for_blocking = EXCLUDED.reason_for_blocking,
                        notify_security = EXCLUDED.notify_security,
                        last_server_sync = NOW()";

                        using var cmdUpsert = new NpgsqlCommand(upsertQuery, conn, tran);
                        AddClientParameters(cmdUpsert);
                        cmdUpsert.Prepare();

                        var processedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        int successCount = 0;
                        int duplicateSourceCount = 0;

                        foreach (Client client in clients.list_clients)
                        {
                            if (!string.IsNullOrWhiteSpace(client.code))
                            {
                                if (!processedCodes.Add(client.code))
                                {
                                    duplicateSourceCount++;
                                    continue;
                                }
                            }

                            failedClient = client;

                            try
                            {
                                SetClientParameters(cmdUpsert, client);
                                await cmdUpsert.ExecuteNonQueryAsync();
                                successCount++;
                            }
                            catch (NpgsqlException ex)
                            {
                                if (ex.IsTransient) throw;
                                Console.WriteLine($"[ОШИБКА БД] Не загружен: Code={client.code}, Phone={client.phone} | Причина: {ex.Message}");
                                continue;
                            }
                        }

                        Console.WriteLine($"--- ИТОГО ПОРЦИИ: Обработано: {successCount} | Дубли в JSON: {duplicateSourceCount} ---");

                        const string updateConstantsQuery = @"
                    UPDATE constants SET last_date_download_bonus_clients = @last_date;
                    UPDATE date_sync SET client = @last_date";

                        using var cmdConstants = new NpgsqlCommand(updateConstantsQuery, conn, tran);
                        cmdConstants.Parameters.Add("@last_date", NpgsqlTypes.NpgsqlDbType.Timestamp);
                        cmdConstants.Parameters["@last_date"].Value = (object)maxDateInPortion ?? DBNull.Value;
                        await cmdConstants.ExecuteNonQueryAsync();

                        await tran.CommitAsync();
                        Console.WriteLine("Транзакция успешно закоммичена.");

                        failedClient = null;

                        if (clients.list_clients.Count < 50000)
                        {
                            Console.WriteLine("Порция меньше 50000. Это были последние данные.");
                            needToLoadMore = false;
                        }
                        else
                        {
                            portionNumber++;
                        }
                    }
                    catch (NpgsqlException ex)
                    {
                        string errorType = "Критическая ошибка БД";
                        string detailedMessage = FormatExceptionMessage(ex, failedClient, errorType);
                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, $"load_bonus_clients | {detailedMessage}");
                        Console.WriteLine($"!!! {errorType}: {detailedMessage}");
                        if (show_message) await MessageBox.Show(detailedMessage, "Ошибка при импорте данных", owner: this);
                        if (tran != null) await tran.RollbackAsync();
                        break;
                    }
                    catch (System.Net.WebException ex)
                    {
                        if (ex.Status == System.Net.WebExceptionStatus.Timeout || ex.Status == System.Net.WebExceptionStatus.ConnectFailure)
                        {
                            await ServiceLocator.ResetDsCacheAsync();
                        }
                        else
                        {
                            MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, $"load_bonus_clients (WebException: {ex.Status})");
                        }
                        if (show_message) await MessageBox.Show("Ошибка связи с сервером при загрузке клиентов.", "Ошибка сети", owner: this);
                        if (tran != null) await tran.RollbackAsync();
                        break;
                    }
                    catch (Exception ex)
                    {
                        string errorType = "Критическая ошибка логики";
                        string detailedMessage = FormatExceptionMessage(ex, failedClient, errorType);
                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, $"load_bonus_clients | {detailedMessage}");
                        Console.WriteLine($"!!! {errorType}: {detailedMessage}");
                        if (show_message) await MessageBox.Show(detailedMessage, "Ошибка при импорте данных", owner: this);
                        if (tran != null) await tran.RollbackAsync();
                        break;
                    }
                    finally
                    {
                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            conn.Close();
                        }
                        conn?.Dispose();
                        tran?.Dispose();
                    }
                }

                if (show_message)
                    await MessageBox.Show("Загрузка клиентов полностью завершена", "Успех", owner: this);

                if (!needToLoadMore && isFullSyncRequested)
                {
                    await CleanUpOrphanClients(resetDate);
                }
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "load_bonus_clients (Критическая ошибка)");
                Console.WriteLine($"Критическая ошибка в load_bonus_clients: {ex}");
            }
        }

        private string FormatExceptionMessage(Exception ex, Client failedClient, string errorType)
        {
            string clientInfo = failedClient != null
                ? $"Код: '{failedClient.code}', Телефон: '{failedClient.phone}', Имя: '{failedClient.name}'"
                : "Неизвестно (ошибка до начала цикла)";

            string stackTrace = string.IsNullOrEmpty(ex.StackTrace) ? "Стек недоступен" : ex.StackTrace;

            return $"[{errorType}] Клиент: {clientInfo}\n" +
                   $"Сообщение: {ex.Message}\n" +
                   $"Стек: {stackTrace}";
        }

        private async Task CleanUpOrphanClients(DateTime syncThresholdTime)
        {
            try
            {
                if (syncThresholdTime > DateTime.Now.AddMinutes(1))
                {
                    string errorMsg = $"[ОЧИСТКА МУСОРА] ОТМЕНА! Пороговое время синхронизации ({syncThresholdTime}) находится в будущем.";
                    Console.WriteLine(errorMsg);
                    MainStaticClass.WriteRecordErrorLog(errorMsg, "CleanUpOrphanClients", 0, MainStaticClass.CashDeskNumber, "ОШИБКА БЕЗОПАСНОСТИ");
                    return;
                }

                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                {
                    await conn.OpenAsync();

                    string countTotalQuery = "SELECT COUNT(1) FROM clients";
                    using (NpgsqlCommand cmdTotal = new NpgsqlCommand(countTotalQuery, conn))
                    {
                        long totalClients = (long)await cmdTotal.ExecuteScalarAsync();

                        if (totalClients == 0) return;

                        string countToDeleteQuery = @"
                    SELECT COUNT(1) FROM clients 
                    WHERE last_server_sync IS NULL 
                       OR last_server_sync < @syncThresholdTime";

                        using (NpgsqlCommand cmdCountDelete = new NpgsqlCommand(countToDeleteQuery, conn))
                        {
                            cmdCountDelete.Parameters.AddWithValue("@syncThresholdTime", syncThresholdTime);
                            long clientsToDelete = (long)await cmdCountDelete.ExecuteScalarAsync();

                            if (clientsToDelete == 0) return;

                            decimal deletePercentage = (decimal)clientsToDelete / totalClients;

                            const decimal MAX_ALLOWED_PERCENTAGE = 0.30m; 
                            const int MAX_ALLOWED_ABSOLUTE = 300000;       

                            if (deletePercentage > MAX_ALLOWED_PERCENTAGE || clientsToDelete > MAX_ALLOWED_ABSOLUTE)
                            {
                                string errorMsg = $"[ОЧИСТКА МУСОРА] ОТМЕНА! Попытка удалить аномальное количество данных. " +
                                                  $"Хотим удалить: {clientsToDelete} из {totalClients} ({deletePercentage:P0}). " +
                                                  $"Лимиты: не более {MAX_ALLOWED_PERCENTAGE:P0} или {MAX_ALLOWED_ABSOLUTE} записей.";

                                Console.WriteLine(errorMsg);
                                MainStaticClass.WriteRecordErrorLog(errorMsg, "CleanUpOrphanClients", 0, MainStaticClass.CashDeskNumber, "ОШИБКА БЕЗОПАСНОСТИ");
                                return;
                            }

                            string deleteQuery = @"
                        DELETE FROM clients 
                        WHERE last_server_sync IS NULL 
                           OR last_server_sync < @syncThresholdTime";

                            using (NpgsqlCommand cmdDelete = new NpgsqlCommand(deleteQuery, conn))
                            {
                                cmdDelete.Parameters.AddWithValue("@syncThresholdTime", syncThresholdTime);
                                int deletedRows = await cmdDelete.ExecuteNonQueryAsync();

                                if (deletedRows > 0)
                                {
                                    Console.WriteLine($"[ОЧИСТКА МУСОРА] Безопасно удалено осиротевших клиентов: {deletedRows}");
                                    MainStaticClass.WriteRecordErrorLog($"Удалено осиротевших клиентов: {deletedRows}", "CleanUpOrphanClients", 0, MainStaticClass.CashDeskNumber, "ИНФО");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка при очистке мусора клиентов");
            }
        }

        private static void AddClientParameters(NpgsqlCommand cmd)
        {
            cmd.Parameters.Add("@code", NpgsqlTypes.NpgsqlDbType.Varchar, 50);
            cmd.Parameters.Add("@phone", NpgsqlTypes.NpgsqlDbType.Varchar, 20);
            cmd.Parameters.Add("@name", NpgsqlTypes.NpgsqlDbType.Varchar, 100);
            cmd.Parameters.Add("@date_of_birth", NpgsqlTypes.NpgsqlDbType.Date);
            cmd.Parameters.Add("@its_work", NpgsqlTypes.NpgsqlDbType.Smallint);
            cmd.Parameters.Add("@reason_for_blocking", NpgsqlTypes.NpgsqlDbType.Varchar, 255);
            cmd.Parameters.Add("@notify_security", NpgsqlTypes.NpgsqlDbType.Smallint);
        }

        private static void SetClientParameters(NpgsqlCommand cmd, Client client)
        {
            cmd.Parameters["@code"].Value = (object)client.code ?? DBNull.Value;
            cmd.Parameters["@phone"].Value = string.IsNullOrWhiteSpace(client.phone) ? DBNull.Value : client.phone;
            cmd.Parameters["@name"].Value = string.IsNullOrWhiteSpace(client.name) ? client.phone : client.name;
            cmd.Parameters["@date_of_birth"].Value = ParseDateForDb(client.holiday);
            cmd.Parameters["@its_work"].Value = ParseSmallint(client.use_blocked);
            cmd.Parameters["@reason_for_blocking"].Value = string.IsNullOrWhiteSpace(client.reason_for_blocking) ? DBNull.Value : client.reason_for_blocking;
            cmd.Parameters["@notify_security"].Value = ParseSmallint(client.notify_security);
        }

        private static object ParseDateForDb(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return DBNull.Value;
            if (DateTime.TryParse(dateStr, out DateTime result)) return result.Date;
            return DBNull.Value;
        }

        private static object ParseSmallint(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return (short)0;

            string val = value.Trim().ToLowerInvariant();
            if (val is "1" or "true" or "да" or "y") return (short)1;
            if (val is "0" or "false" or "нет" or "n") return (short)0;

            if (short.TryParse(val, out short parsedShort)) return parsedShort;

            return (short)0;
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

        private async Task<LoadPacketData> getLoadPacketDataFullAsync(string nick_shop, string data_encrypt, string key)
        {
            LoadPacketData loadPacketData = new LoadPacketData();
            loadPacketData.PacketIsFull = false;

            string result_query = "";
            string decrypt_data = "";

            try
            {
                DS ds = await ServiceLocator.DsAsync();
                ds.Timeout = 60000;

                byte[] result_query_byte = await ds.GetDataForCasheV8JasonAvalonAsync(
                    nick_shop,
                    data_encrypt,
                    MainStaticClass.GetWorkSchema.ToString());

                result_query = DecompressString(result_query_byte);
                decrypt_data = CryptorEngine.Decrypt(result_query, true, key);
                loadPacketData = JsonConvert.DeserializeObject<LoadPacketData>(decrypt_data);

            }
            catch (Exception ex)
            {
                await ServiceLocator.ResetDsCacheAsync();
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

                using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                {
                    object query_result = command.ExecuteScalar();

                    if (query_result != null && query_result != DBNull.Value)
                    {
                        if (query_result is DateOnly dateOnly)
                        {
                            result = dateOnly.ToDateTime(TimeOnly.MinValue);
                            Console.WriteLine($"[DEBUG] Конвертировано из DateOnly: {result}");
                        }
                        else if (query_result is DateTime dateTime)
                        {
                            result = dateTime;
                            Console.WriteLine($"[DEBUG] Получено DateTime: {result}");
                        }
                        else
                        {
                            result = Convert.ToDateTime(query_result);
                            Console.WriteLine($"[DEBUG] Конвертировано через Convert: {result}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception: {ex.Message}");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
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

        private void UpdateLastSyncDate()
        {
            try
            {
                DateTime lastDate = last_date_download_tovars();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_lastUpdateText != null)
                    {
                        if (lastDate > new DateTime(2001, 1, 1))
                        {
                            _lastUpdateText.Text = $"Последняя успешная загрузка: {lastDate:dd.MM.yyyy HH:mm}";
                        }
                        else
                        {
                            _lastUpdateText.Text = "Последняя загрузка: данные еще не загружались";
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения даты последней загрузки: {ex.Message}");
            }
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

                await Task.Delay(100);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #endregion // Закрывает "Основная логика загрузки"
    }
}