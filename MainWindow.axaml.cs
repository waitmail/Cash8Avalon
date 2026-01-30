using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Cash8Avalon.ViewModels;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;


namespace Cash8Avalon
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _unloadingTimer; // Заменяем System.Timers.Timer на DispatcherTimer

        public MainWindow()
        {
            InitializeComponent();
            InitializeUnloadingTimer();

            //#if DEBUG
            //            this.AttachDevTools();
            //#endif
        }

        private void InitializeUnloadingTimer()
        {
            _unloadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // По умолчанию 5 минут
            };
            _unloadingTimer.Tick += UnloadingTimer_Tick;
        }



        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Ждем пока окно появится на экране
            await Task.Delay(50);

            // Создаем окно авторизации
            var loginWindow = new Interface_switching();

            bool loginSuccess = false;

            loginWindow.AuthorizationSuccess += (s, password) =>
            {
                loginSuccess = true;
                loginWindow.Close();
            };

            loginWindow.AuthorizationCancel += (s, args) =>
            {
                loginSuccess = false;
                loginWindow.Close();
            };
            //loginWindow.input_barcode.Focus();
            // Показываем как модальное окно
            await loginWindow.ShowDialog(this);

            if (loginSuccess)
            {
                try
                {
                    Console.WriteLine("=== ВЫПОЛНЕНИЕ ПРОВЕРОК ПРИ СТАРТЕ ===");

                    // ВОТ СЮДА ДОБАВЛЯЕМ ВСЕ ПРОВЕРКИ!

                    // 1. Устанавливаем статические значения
                    MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
                    MainStaticClass.Last_Write_Check = DateTime.Now.AddSeconds(1);
                    MainStaticClass.MainWindow = this;

                    // 2. Проверка файла конфигурации
                    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Setting.gaa");
                    if (!File.Exists(configPath))
                    {
                        await MessageBox.Show($"Не обнаружен файл Setting.gaa в {AppDomain.CurrentDomain.BaseDirectory}","Проверка файлов настроек ",MessageBoxButton.OK,MessageBoxType.Error);
                        this.Close();
                        return;
                    }
                    MainStaticClass.loadConfig(configPath);
                    Console.WriteLine($"? Конфиг загружен: {configPath}");

                    string version_program = await MainStaticClass.GetAtolDriverVersion();
                    this.Title = "Касса   " + MainStaticClass.CashDeskNumber;
                    this.Title += " | " + MainStaticClass.Nick_Shop;
                    this.Title += " | " + MainStaticClass.version();
                    this.Title += " | " + LoadDataWebService.last_date_download_tovars().ToString("yyyy-MM-dd hh:mm:ss");
                    PrintingUsingLibraries printing = new PrintingUsingLibraries();
                    this.Title += " | " + version_program;

                    // 1. Обновляем период выгрузки
                    //await UpdateUnloadingPeriod();

                    //// 2. Настраиваем и запускаем таймер
                    //int intervalMinutes = await MainStaticClass.GetUnloadingInterval();
                    //if (intervalMinutes > 0)
                    //{
                    //    _unloadingTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
                    //    _unloadingTimer.Start();
                    //    Console.WriteLine($"✓ Таймер выгрузки запущен с интервалом {intervalMinutes} минут");
                    //}


                    // 3. Проверка обновлений (только если не A01)
                    //if (MainStaticClass.Nick_Shop != "A01")
                    //{
                    //    Console.WriteLine("Проверка обновлений программы...");
                    //    // TODO: адаптировать LoadProgramFromInternet
                    //}

                    MainStaticClass.SystemTaxation = await check_system_taxation();

                    // 4. Проверка таблицы constants
                    if (await MainStaticClass.exist_table_name("constants"))
                    {
                        _ = InventoryManager.FillDictionaryProductDataAsync(this);
                        _ = Task.Run(() => InventoryManager.DictionaryPriceGiftAction);

                        //await ShowErrorMessage("В базе данных нет таблицы constants!");
                        //this.Close();
                        //return;


                        // 5. Установка заголовка окна
                        //SetWindowTitle();

                        // 6. Запуск фоновых задач
                        //StartBackgroundTasks();

                        // 7. Проверка системы налогообложения
                        //await CheckTaxation();

                        // 8. Очистка старых чеков и логов
                        //CleanOldData();

                        // 9. Проверки для реальных касс (не тестовой №9)
                        if (MainStaticClass.CashDeskNumber != 9)
                        {
                            if (MainStaticClass.Use_Fiscall_Print)
                            {
                                printing = new PrintingUsingLibraries();
                                await printing.getShiftStatus();
                            }

                            // Проверка даты/времени с ФН
                            MainStaticClass.validate_date_time_with_fn(10);

                            // Проверка системы налогообложения
                            if (MainStaticClass.SystemTaxation == 0)
                            {
                                await MessageBox.Show("У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!", "Проверка системы налогообложения", MessageBoxButton.OK, MessageBoxType.Error);
                            }

                            // Проверка версии ФН
                            bool restart = false, error = false;
                            MainStaticClass.check_version_fn(ref restart, ref error);
                            if (!error && restart)
                            {
                                await MessageBox.Show("У вас неверно была установлена версия ФН, необходим перезапуск программы", "Проверка версии ФН", MessageBoxButton.OK, MessageBoxType.Error);
                                this.Close();
                                return;
                            }
                        }

                        // 10. Проверка версии ФН для маркировки
                        //CheckFnMarkingVersion();

                        // 11. Загрузка бонусных клиентов и CDN
                        if (MainStaticClass.CashDeskNumber != 9)
                        {
                            _ = loadBonusClients();
                            if (string.IsNullOrEmpty(MainStaticClass.CDN_Token))
                            {
                                await MessageBox.Show("В этой кассе не заполнен CDN токен!\r\nПРОДАЖА МАРКИРОВАННОГО ТОВАРА ОГРАНИЧЕНА!", "Проверка cdn токена", MessageBoxButton.OK, MessageBoxType.Error);
                            }
                            else
                            {
                                _ = LoadCdnWithStartAsync();
                            }
                        }

                        // 12. Проверка файлов и папок
                        _ = CheckFilesAndFolders();

                        // 13. Отправка статуса открытия магазина
                        //await SendShopStatus(true);

                        // 2. Настраиваем и запускаем таймер
                        int intervalMinutes = await MainStaticClass.GetUnloadingInterval();
                        if (intervalMinutes > 0)
                        {
                            _unloadingTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
                            _unloadingTimer.Start();
                            Console.WriteLine($"✓ Таймер выгрузки запущен с интервалом {intervalMinutes} минут");
                        }

                        Console.WriteLine("? ВСЕ ПРОВЕРКИ УСПЕШНО ВЫПОЛНЕНЫ");
                    }
                    else
                    {
                        await MessageBox.Show("В этой бд нет таблицы constatnts,необходимо создать таблицы бд");
                    }


                    

                    // ТОЛЬКО ПОСЛЕ ВСЕХ ПРОВЕРОК СОЗДАЕМ ViewModel!
                    this.DataContext = new MainViewModel();



                   



                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Критическая ошибка: {ex.Message}");
                    //await ShowErrorMessage($"Ошибка при запуске: {ex.Message}");
                    this.Close();
                }
            }
            else
            {
                // Закрываем главное окно при отмене
                this.Close();
            }
        }

        /// <summary>
        /// Обновление периода выгрузки в БД
        /// </summary>
        private async Task UpdateUnloadingPeriod()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                await conn.OpenAsync();
                string query = "UPDATE constants SET unloading_period = 4 WHERE unloading_period > 0";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Период выгрузки обновлен в БД");
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show($"Ошибка при проверке/установке значения периода выгрузки: {ex.Message}",
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxType.Error);
                Console.WriteLine($"✗ Ошибка Npgsql в UpdateUnloadingPeriod: {ex.Message}");
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка при проверке/установке значения периода выгрузки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                Console.WriteLine($"✗ Общая ошибка в UpdateUnloadingPeriod: {ex.Message}");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    await conn.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Обработчик события таймера выгрузки
        /// </summary>
        private async void UnloadingTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                 Console.WriteLine($"=== Запуск выгрузки данных ({DateTime.Now:HH:mm:ss}) ===");

                // Отправка статуса онлайн
                await Task.Run(() => MainStaticClass.SendOnlineStatus());

                // Проверяем, нужно ли отправлять данные о продажах
                if (MainStaticClass.Last_Write_Check > MainStaticClass.Last_Send_Last_Successful_Sending)
                {
                    //await SendSalesData();
                    //await UploadDeletedItems();
                    //await SendCdnLogs();
                    //await UploadErrorsLog();
                    //await SentOpenCloseShop();


                    MainStaticClass.SendOnlineStatus();
                    if (MainStaticClass.Last_Write_Check > MainStaticClass.Last_Send_Last_Successful_Sending)
                    {
                        SendDataOnSalesPortions sdsp = new SendDataOnSalesPortions();
                        sdsp.send_sales_data_Click(null, null);                        
                        UploadDeletedItems();                        
                        send_cdn_logs();                        
                        UploadErrorsLog();
                        sent_open_close_shop();
                    }

                    //// Обновляем время последней успешной отправки
                    //MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
                    Console.WriteLine("✓ Выгрузка данных завершена успешно");
                }
                else
                {
                    Console.WriteLine("⚠ Нет новых данных для выгрузки");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в обработчике таймера выгрузки: {ex.Message}");
                await MessageBox.Show($"Ошибка при выгрузке данных: {ex.Message}",
                    "Ошибка выгрузки", MessageBoxButton.OK, MessageBoxType.Error);
            }
        }

        class OpenCloseShop
        {
            public DateTime? Open { get; set; }
            public DateTime? Close { get; set; }
            public DateTime Date { get; set; }
            public bool ItsSent { get; set; }
        }

        private void sent_open_close_shop()
        {
            List<OpenCloseShop> closeShops = get_open_close_shop();
            if (closeShops.Count > 0)
            {
                DS ds = MainStaticClass.get_ds();
                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0)
                {
                    return;
                }
                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0)
                {
                    return;
                }
                string count_day = CryptorEngine.get_count_day();

                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();

                string data = JsonConvert.SerializeObject(closeShops, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string data_crypt = CryptorEngine.Encrypt(data, true, key);
                try
                {
                    bool result = ds.UploadOpeningClosingShops(MainStaticClass.Nick_Shop, data_crypt, "4");
                    if (result)
                    {
                        MarkShopsAsSent(closeShops);
                    }
                }
                catch
                {

                }
            }
        }

        private void MarkShopsAsSent(List<OpenCloseShop> shops)
        {
            if (shops == null || shops.Count == 0)
                return;

            using (var conn = MainStaticClass.NpgsqlConn())
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var shop in shops)
                        {
                            string updateQuery = "UPDATE public.open_close_shop SET its_sent = true WHERE date = @date";

                            using (var cmd = new NpgsqlCommand(updateQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@date", shop.Date.Date);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        // Логирование ошибки (опционально)
                        MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Ошибка при обновлении its_sent");
                    }
                }
            }
        }

        private List<OpenCloseShop> get_open_close_shop()
        {
            List<OpenCloseShop> openCloseShops = new List<OpenCloseShop>();

            using (var conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    conn.Open();

                    string query = "SELECT open, close, date, its_sent FROM public.open_close_shop WHERE its_sent = false;";

                    using (var command = new NpgsqlCommand(query, conn))
                    using (var reader = command.ExecuteReader())
                    {
                        // Получаем индексы колонок
                        int openOrdinal = reader.GetOrdinal("open");
                        int closeOrdinal = reader.GetOrdinal("close");
                        int dateOrdinal = reader.GetOrdinal("date");
                        int itsSentOrdinal = reader.GetOrdinal("its_sent");

                        while (reader.Read())
                        {
                            var openCloseShop = new OpenCloseShop
                            {
                                Open = reader.IsDBNull(openOrdinal) ? (DateTime?)null : reader.GetDateTime(openOrdinal),
                                Close = reader.IsDBNull(closeOrdinal) ? (DateTime?)null : reader.GetDateTime(closeOrdinal),
                                Date = reader.GetDateTime(dateOrdinal),
                                ItsSent = reader.GetBoolean(itsSentOrdinal)
                            };

                            openCloseShops.Add(openCloseShop);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show("При отправке даты открытия и закрытия магазина произошли ошибки: " + ex.Message);
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Отправка даты открытия/закрытия магазина");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("При отправке даты открытия и закрытия магазина произошли ошибки: " + ex.Message);
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Отправка даты открытия/закрытия магазина");
                }
            }

            return openCloseShops;
        }

        public class CdnLogs
        {
            public List<CdnLog> ListCdnLog { get; set; }
        }

        public class CdnLog
        {
            //public string Shop { get; set; }
            public string NumCash { get; set; }
            public string CdnAnswer { get; set; }
            public string DateShop { get; set; }
            public string NumDoc { get; set; }
            public string Mark { get; set; }
            public string Status { get; set; }
        }

        private void send_cdn_logs()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                string query = "SELECT num_cash, date, cdn_answer, numdoc, is_sent, mark,status FROM cdn_log WHERE is_sent=0;";
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                CdnLogs logs = new CdnLogs();
                logs.ListCdnLog = new List<CdnLog>();
                while (reader.Read())
                {
                    CdnLog log = new CdnLog();
                    log.CdnAnswer = reader["cdn_answer"].ToString();
                    log.Mark = reader["mark"].ToString();
                    log.NumCash = MainStaticClass.CashDeskNumber.ToString();
                    log.NumDoc = reader["numdoc"].ToString();
                    log.DateShop = Convert.ToDateTime(reader["date"]).ToString("dd-MM-yyyy HH:mm:ss");
                    log.Status = reader["status"].ToString();
                    logs.ListCdnLog.Add(log);
                }
                if (logs.ListCdnLog.Count > 0)
                {
                    DS ds = MainStaticClass.get_ds();
                    ds.Timeout = 180000;

                    //Получить параметра для запроса на сервер 
                    string nick_shop = MainStaticClass.Nick_Shop.Trim();
                    if (nick_shop.Trim().Length == 0)
                    {
                        return;
                    }
                    string code_shop = MainStaticClass.Code_Shop.Trim();
                    if (code_shop.Trim().Length == 0)
                    {
                        return;
                    }
                    string count_day = CryptorEngine.get_count_day();
                    string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
                    bool result_web_quey = false;
                    string data = JsonConvert.SerializeObject(logs, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    string data_crypt = CryptorEngine.Encrypt(data, true, key);

                    result_web_quey = ds.UploadCDNLogsPortionJason(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString());

                    if (result_web_quey)
                    {
                        foreach (CdnLog log in logs.ListCdnLog)
                        {
                            query = "UPDATE cdn_log SET is_sent = 1 WHERE date='" + log.DateShop + "';";
                            command = new NpgsqlCommand(query, conn);
                            command.ExecuteNonQuery();
                        }
                    }
                }
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
        }

        /// <summary>
        /// Уменьшенное количестов в чеке
        /// или удаленная строка
        /// </summary>
        public class DeletedItem
        {
            public string num_doc { get; set; }
            public string num_cash { get; set; }
            public string date_time_start { get; set; }
            public string date_time_action { get; set; }
            public string tovar { get; set; }
            public string quantity { get; set; }
            public string type_of_operation { get; set; }
            public string guid { get; set; }
            public string autor { get; set; }
            public string reason { get; set; }
        }

        public class DeletedItems : IDisposable
        {
            public string Version { get; set; }
            public string NickShop { get; set; }
            public string CodeShop { get; set; }
            //public string Guid { get; set; }
            public List<DeletedItem> ListDeletedItem { get; set; }

            void IDisposable.Dispose()
            {

            }
        }

        private void UploadDeletedItems()
        {
            DeletedItems deletedItems = new DeletedItems();
            deletedItems.CodeShop = MainStaticClass.Code_Shop;
            deletedItems.NickShop = MainStaticClass.Nick_Shop;
            deletedItems.ListDeletedItem = new List<DeletedItem>();
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = "SELECT num_doc, num_cash, date_time_start, date_time_action, tovar, quantity, type_of_operation,guid,reason FROM deleted_items;";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DeletedItem deletedItem = new DeletedItem();
                    deletedItem.num_doc = reader["num_doc"].ToString();
                    deletedItem.num_cash = reader["num_cash"].ToString();
                    deletedItem.date_time_start = reader["date_time_start"].ToString();
                    deletedItem.date_time_action = reader["date_time_action"].ToString();
                    deletedItem.tovar = reader["tovar"].ToString();
                    deletedItem.quantity = reader["quantity"].ToString();
                    deletedItem.type_of_operation = reader["type_of_operation"].ToString();
                    deletedItem.guid = reader["guid"].ToString();
                    deletedItem.autor = MainStaticClass.CashOperatorInn;
                    deletedItem.reason = reader["reason"].ToString();
                    deletedItems.ListDeletedItem.Add(deletedItem);
                }
                reader.Close();
                reader.Dispose();

                if (deletedItems.ListDeletedItem.Count == 0)
                {
                    return;
                }

                if (!MainStaticClass.service_is_worker())
                {
                    //MessageBox.Show("Веб сервис недоступен");
                    return;
                }
                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 20000;

                //Получить параметра для запроса на сервер 
                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0)
                {
                    MessageBox.Show(" Не удалось получить название магазина ");
                    return;
                }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0)
                {
                    MessageBox.Show(" Не удалось получить код магазина ");
                    return;
                }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
                string data = JsonConvert.SerializeObject(deletedItems, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string encrypt_string = CryptorEngine.Encrypt(data, true, key);
                string answer = ds.UploadDeletedItems(nick_shop, encrypt_string, MainStaticClass.GetWorkSchema.ToString());
                if (answer == "1")
                {
                    query = "DELETE FROM deleted_items";
                    command = new NpgsqlCommand(query, conn);
                    command.ExecuteNonQuery();
                }
                else
                {
                    //MessageBox.Show("Произошли ошибки при передаче удаленных строк");
                    MainStaticClass.WriteRecordErrorLog("Произошли ошибки при передаче удаленных строк", "UploadDeletedItems", 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию об удаленных строках");
                }
                command.Dispose();
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошли ошибки при передаче удаленных строк " + ex.Message);
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Не удалось передать информацию об удаленных строках");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void UploadErrorsLog()
        {
            try
            {
                var recordsErrorLog = ReadErrorLogsFromDatabase();
                if (recordsErrorLog.ErrorLogs.Count > 0)
                {
                    bool uploadResult = UploadErrorLogsToServer(recordsErrorLog);
                    if (uploadResult)
                    {
                        DeleteErrorLogsFromDatabase(recordsErrorLog);
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку или предпринимаем другие действия по обработке исключения
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Произошла ошибка при загрузке логов ошибок");
            }
        }

        public class RecordsErrorLog
        {
            public string Shop { get; set; }
            public short CashDeskNumber { get; set; }
            public List<RecordErrorLog> ErrorLogs { get; set; } = new List<RecordErrorLog>();
        }

        public class RecordErrorLog
        {
            public string ErrorMessage { get; set; }
            public string MethodName { get; set; }
            public long NumDoc { get; set; }
            public string Description { get; set; }
            public DateTime DateTimeRecord { get; set; }
        }

        private RecordsErrorLog ReadErrorLogsFromDatabase()
        {
            RecordsErrorLog recordsErrorLog = new RecordsErrorLog();
            recordsErrorLog.Shop = MainStaticClass.Nick_Shop;
            recordsErrorLog.CashDeskNumber = Convert.ToInt16(MainStaticClass.CashDeskNumber);

            using (var connection = MainStaticClass.NpgsqlConn())
            {
                connection.Open();
                string query = "SELECT error_message, date_time_record, num_doc, method_name, description FROM public.errors_log";
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var logError = new RecordErrorLog
                        {
                            ErrorMessage = reader["error_message"].ToString().Trim(),
                            DateTimeRecord = reader.GetDateTime(reader.GetOrdinal("date_time_record")),
                            NumDoc = reader.GetInt64(reader.GetOrdinal("num_doc")),
                            MethodName = reader["method_name"].ToString().Trim(),
                            Description = reader["description"].ToString().Trim()
                        };
                        recordsErrorLog.ErrorLogs.Add(logError);
                    }
                }
            }
            return recordsErrorLog;
        }

        private bool UploadErrorLogsToServer(RecordsErrorLog recordsErrorLog)
        {
            string nick_shop = MainStaticClass.Nick_Shop.Trim();
            string code_shop = MainStaticClass.Code_Shop.Trim();
            if (string.IsNullOrEmpty(nick_shop) || string.IsNullOrEmpty(code_shop))
            {
                return false;
            }

            string count_day = CryptorEngine.get_count_day();
            string key = nick_shop + count_day + code_shop;
            string data = JsonConvert.SerializeObject(recordsErrorLog, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            string data_crypt = CryptorEngine.Encrypt(data, true, key);

            DS ds = MainStaticClass.get_ds();
            ds.Timeout = 18000;
            try
            {
                return ds.UploadErrorLogPortionJson(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString());
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "не удалось передать информацию об ошибках в программе");
                return false;
            }
        }

        private void DeleteErrorLogsFromDatabase(RecordsErrorLog recordsErrorLog)
        {
            using (var connection = MainStaticClass.NpgsqlConn())
            {
                connection.Open();
                foreach (var recordErrorLog in recordsErrorLog.ErrorLogs)
                {
                    string query = "DELETE FROM public.errors_log WHERE date_time_record = @DateTimeRecord";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DateTimeRecord", recordErrorLog.DateTimeRecord);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void get_cdn_with_start()
        {
            CDN.CDN_List list = MainStaticClass.CDN_List;
        }

        private async Task LoadCdnWithStartAsync()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            try
            {
                // Запуск функции с параметром в новом потоке
                Task task = Task.Run(() => get_cdn_with_start(), token);

                // Ожидание результата функции в течение 60 секунд
                bool isCompletedSuccessfully = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(60), token)) == task;

                if (!isCompletedSuccessfully)
                {
                    cts.Cancel();
                }
            }
            catch (Exception ex)
            {
                // Обработка исключений
                await MessageBox.Show($"При загрузке CDN произошла ошибка: {ex.Message}");
            }
        }

        private async Task CheckFilesAndFolders()
        {
            try
            {
                // Получаем путь к директории приложения
                string startupPath = AppContext.BaseDirectory;
                string folderPathPictures = Path.Combine(startupPath, "Pictures2");

                await Task.Run(() =>
                {
                    if (!Directory.Exists(folderPathPictures))
                    {
                        Directory.CreateDirectory(folderPathPictures);
                        Console.WriteLine($"Папка создана: {folderPathPictures}");
                    }
                    else
                    {
                        // Очистка папки
                        _ = ClearFolder(folderPathPictures);
                        Console.WriteLine($"Папка очищена: {folderPathPictures}");
                    }
                });
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Проверка/создание файлов и папок");

                // Асинхронный MessageBox
                await MessageBox.Show($"Ошибка при работе с папкой Pictures2: {ex.Message}", "Ошибка");
            }
        }

        private async Task ClearFolder(string folderPath)
        {
            try
            {
                // Удаляем все файлы
                foreach (string file in Directory.GetFiles(folderPath))
                {
                    try
                    {
                        File.Delete(file);
                        //Console.WriteLine($"Удален файл: {file}");
                    }
                    catch (Exception ex)
                    {
                        await MessageBox.Show($"Не удалось удалить файл {file}: {ex.Message}");
                    }
                }

                // Удаляем все подпапки
                foreach (string subFolder in Directory.GetDirectories(folderPath))
                {
                    try
                    {
                        Directory.Delete(subFolder, true); // true - рекурсивное удаление
                        //MessageBox.ShowriteLine($"Удалена папка: {subFolder}");
                    }
                    catch (Exception ex)
                    {
                        await MessageBox.Show($"Не удалось удалить папку {subFolder}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при очистке папки {folderPath}: {ex.Message}", ex);
            }
        }

        private async Task loadBonusClients()
        {
            LoadDataWebService ld = new LoadDataWebService();
            await Task.Run(() => ld.load_bonus_clients(false));            
        }

        private async Task<int> check_system_taxation()
        {
            int result = 0;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT system_taxation FROM constants";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show("Ошибка sql check_system_taxation " + ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Общая ошибка check_system_taxation " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }

            return result;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}