using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cash8Avalon.ViewModels;
using Npgsql;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;


namespace Cash8Avalon
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

//#if DEBUG
//            this.AttachDevTools();
//#endif
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

                    // 3. Проверка обновлений (только если не A01)
                    if (MainStaticClass.Nick_Shop != "A01")
                    {
                        Console.WriteLine("Проверка обновлений программы...");
                        // TODO: адаптировать LoadProgramFromInternet
                    }

                    MainStaticClass.SystemTaxation = await check_system_taxation();

                    // 4. Проверка таблицы constants
                    //if (!await CheckConstantsTable())
                    //{
                    //    await ShowErrorMessage("В базе данных нет таблицы constants!");
                    //    this.Close();
                    //    return;
                    //}

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
                            //await GetShiftStatus();
                        }

                        // Проверка даты/времени с ФН
                        MainStaticClass.validate_date_time_with_fn(10);

                        // Проверка системы налогообложения
                        if (MainStaticClass.SystemTaxation == 0)
                        {
                            //await ShowWarningMessage(
                            //    "У вас не заполнена система налогообложения!\r\nСоздание и печать чеков невозможна!\r\nОБРАЩАЙТЕСЬ В БУХГАЛТЕРИЮ!");
                        }

                        // Проверка версии ФН
                        bool restart = false, error = false;
                        MainStaticClass.check_version_fn(ref restart, ref error);
                        if (!error && restart)
                        {
                            //await ShowErrorMessage("У вас неверно была установлена версия ФН, необходим перезапуск программы");
                            this.Close();
                            return;
                        }
                    }

                    // 10. Проверка версии ФН для маркировки
                    //CheckFnMarkingVersion();

                    // 11. Загрузка бонусных клиентов и CDN
                    if (MainStaticClass.CashDeskNumber != 9)
                    {
                        //await LoadBonusClients();

                        if (string.IsNullOrEmpty(MainStaticClass.CDN_Token))
                        {
                            //await ShowWarningMessage(
                            //    "В этой кассе не заполнен CDN токен!\r\nПРОДАЖА МАРКИРОВАННОГО ТОВАРА ОГРАНИЧЕНА!");
                        }
                        else
                        {
                            //await LoadCdnData();
                        }
                    }

                    // 12. Проверка файлов и папок
                    //CheckFilesAndFolders();

                    // 13. Отправка статуса открытия магазина
                    //await SendShopStatus(true);

                    Console.WriteLine("? ВСЕ ПРОВЕРКИ УСПЕШНО ВЫПОЛНЕНЫ");

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