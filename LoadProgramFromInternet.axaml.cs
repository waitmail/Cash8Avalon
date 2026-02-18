using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Npgsql;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Cash8Avalon
{
    public partial class LoadProgramFromInternet : Window
    {
        private string version = "";
        public bool new_version_of_the_program = false;
        public bool show_phone = false;
        Button btnClose = null;
        Button btnDownload = null;

        public LoadProgramFromInternet()
        {
            InitializeComponent();
            this.Opened += LoadProgramFromInternet_Opened;
            // Находим кнопку и подписываемся на событие
            btnClose = this.FindControl<Button>("BtnClose");
            if (btnClose != null)
            {
                btnClose.Click += BtnClose_Click;
            }
            btnDownload = this.FindControl<Button>("BtnDownload");
            if (btnDownload != null)
            {
                btnDownload.Click += BtnDownload_Click;
            }
        }

        

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void LoadProgramFromInternet_Opened(object? sender, EventArgs e)
        {
            if (!show_phone)
            {
                await check_new_version_programm();
            }
        }

        public async Task check_new_version_programm()
        {
            if (!MainStaticClass.service_is_worker())
            {
                return;
            }

            DS ds = MainStaticClass.get_ds();
            ds.Timeout = 100000;

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
            string version = MainStaticClass.version();
            string data = code_shop.Trim() + "|" + version + "|" + code_shop.Trim();
            string result_web_query = "";

            try
            {
                result_web_query = await Task.Run(() =>
                    ds.ExistsUpdateProrgamAvalon(nick_shop, CryptorEngine.Encrypt(data, true, key), MainStaticClass.GetWorkSchema.ToString())
                );
            }
            catch (Exception ex)
            {
                await ShowMessage("Ошибка при получении версии программы на сервере " + ex.Message);
                return;
            }

            var labelUpdate = this.FindControl<Label>("LabelUpdate");

            if (result_web_query == "")
            {
                if (labelUpdate != null)
                    labelUpdate.Content = "Не удалось проверить версию программы на сервере";
            }
            else
            {
                result_web_query = CryptorEngine.Decrypt(result_web_query, true, key);

                if (MainStaticClass.version() == result_web_query)
                {
                    if (labelUpdate != null)
                        labelUpdate.Content = " У вас установлена самая последняя версия программы ";
                }
                else
                {
                    version = result_web_query;

                    Int64 local_version = Convert.ToInt64(MainStaticClass.version().Replace(".", ""));
                    Int64 remote_version = Convert.ToInt64(result_web_query.Replace(".", ""));

                    if (remote_version > local_version)
                    {
                        if (labelUpdate != null)
                            labelUpdate.Content = "Есть обновление программы " + result_web_query;

                        EnableDownloadButton(true);
                        new_version_of_the_program = true;

                        //Принудительно вызываем обновление версии программы                        
                        if (!show_phone)
                        {
                            await BtnDownload_ClickAsync();
                        }
                    }
                }
            }
        }

        //private async Task check_and_update_npgsql()
        //{
        //    string startupPath = AppContext.BaseDirectory;
        //    FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(startupPath, "Npgsql.dll"));

        //    string fileVersion = myFileVersionInfo.FileVersion?.Replace(".", "") ?? "0";
        //    int cash_version = int.Parse(fileVersion);

        //    if (cash_version == 20100)//Старая версия Npgsql 
        //    {
        //        string previousPath = Path.Combine(startupPath, "PreviousNpgsql");
        //        string updatePath = Path.Combine(startupPath, "UpdateNpgsql");

        //        if (!Directory.Exists(previousPath))
        //        {
        //            Directory.CreateDirectory(previousPath);
        //        }

        //        if (!Directory.Exists(updatePath))
        //        {
        //            Directory.CreateDirectory(updatePath);
        //        }

        //        if (!MainStaticClass.service_is_worker())
        //        {
        //            return;
        //        }

        //        DS ds = MainStaticClass.get_ds();
        //        ds.Timeout = 50000;

        //        string nick_shop = MainStaticClass.Nick_Shop.Trim();
        //        if (nick_shop.Trim().Length == 0)
        //        {
        //            return;
        //        }

        //        string code_shop = MainStaticClass.Code_Shop.Trim();
        //        if (code_shop.Trim().Length == 0)
        //        {
        //            return;
        //        }

        //        string count_day = CryptorEngine.get_count_day();
        //        string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
        //        string data = code_shop.Trim() + "|" + code_shop.Trim();

        //        byte[] result_web_query = new byte[0];

        //        try
        //        {
        //            result_web_query = await Task.Run(() =>
        //                ds.GetNpgsqlNew(nick_shop, CryptorEngine.Encrypt(data, true, key), MainStaticClass.GetWorkSchema.ToString())
        //            );
        //        }
        //        catch (Exception ex)
        //        {
        //            await ShowMessage(ex.Message);
        //            return;
        //        }

        //        string npgsqlPath = Path.Combine(updatePath, "Npgsql.dll");
        //        await File.WriteAllBytesAsync(npgsqlPath, result_web_query);

        //        try
        //        {
        //            File.Copy(Path.Combine(startupPath, "Npgsql.dll"),
        //                     Path.Combine(previousPath, "Npgsql.dll"), true);

        //            if ((await File.ReadAllBytesAsync(npgsqlPath)).Length > 0)
        //            {
        //                File.Copy(npgsqlPath, Path.Combine(startupPath, "Npgsql.dll"), true);
        //                await ShowMessage("Библиотека Npgsql.dll успешно обновлена");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            await ShowMessage(ex.Message);
        //        }
        //    }
        //}

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            await BtnDownload_ClickAsync();
        }

        //private async Task BtnDownload_ClickAsync()
        //{
        //    //await check_and_update_npgsql();

        //    var btnClose = this.FindControl<Button>("BtnClose");
        //    if (btnClose != null)
        //        btnClose.IsEnabled = false;

        //    if (!MainStaticClass.service_is_worker())
        //    {
        //        return;
        //    }

        //    DS ds = MainStaticClass.get_ds();
        //    ds.Timeout = 10000;

        //    string nick_shop = MainStaticClass.Nick_Shop.Trim();
        //    if (nick_shop.Trim().Length == 0)
        //    {
        //        return;
        //    }

        //    string code_shop = MainStaticClass.Code_Shop.Trim();
        //    if (code_shop.Trim().Length == 0)
        //    {
        //        return;
        //    }

        //    string count_day = CryptorEngine.get_count_day();
        //    string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
        //    string my_version = version;
        //    string data = code_shop.Trim() + "|" + version + "|" + code_shop.Trim();

        //    byte[] result_web_query = new byte[0];

        //    try
        //    {
        //        result_web_query = await Task.Run(() =>
        //            ds.GetUpdateProgramAvalon(nick_shop, CryptorEngine.Encrypt(data, true, key), MainStaticClass.GetWorkSchema.ToString())
        //        );
        //    }
        //    catch
        //    {
        //        return;
        //    }

        //    if (result_web_query.Length > 10)
        //    {
        //        try
        //        {
        //            string startupPath = AppContext.BaseDirectory;
        //            string updatePath = Path.Combine(startupPath, "Update");
        //            string previousPath = Path.Combine(startupPath, "Previous");

        //            if (!Directory.Exists(updatePath))
        //            {
        //                Directory.CreateDirectory(updatePath);
        //            }

        //            string exePath = Path.Combine(updatePath, "Cash.exe");
        //            await File.WriteAllBytesAsync(exePath, result_web_query);

        //            if (!Directory.Exists(previousPath))
        //            {
        //                Directory.CreateDirectory(previousPath);
        //            }

        //            File.Copy(Path.Combine(startupPath, "Cash.exe"),
        //                     Path.Combine(previousPath, "Cash.exe"), true);

        //            await ShowMessage("Обновление успешно загружено, теперь необходимо перезапустить программу");

        //            this.Close(DialogResult.Yes);
        //        }
        //        catch (Exception ex)
        //        {
        //            await ShowMessage("При загрузке произошли ошибки " + ex.Message);
        //        }
        //    }

        //    if (btnClose != null)
        //        btnClose.IsEnabled = true;
        //}
        //
        private async Task BtnDownload_ClickAsync()
        {
            //btnClose = this.FindControl<Button>("BtnClose");
            //if (btnClose != null)
            //    btnClose.IsEnabled = false;

            if (!MainStaticClass.service_is_worker())
            {
                if (btnClose != null)
                    btnClose.IsEnabled = true;
                await ShowMessage("Веб сервис недоступен");
                return;
            }

            DS ds = MainStaticClass.get_ds();
            ds.Timeout = 100000;

            string nick_shop = MainStaticClass.Nick_Shop.Trim();
            if (nick_shop.Trim().Length == 0)
            {
                if (btnClose != null)
                    btnClose.IsEnabled = true;
                return;
            }

            string code_shop = MainStaticClass.Code_Shop.Trim();
            if (code_shop.Trim().Length == 0)
            {
                if (btnClose != null)
                    btnClose.IsEnabled = true;
                return;
            }

            string count_day = CryptorEngine.get_count_day();
            string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
            string local_version = MainStaticClass.version();
            string data = code_shop.Trim() + "|" + local_version + "|" + code_shop.Trim();

            try
            {
                string encryptedData = CryptorEngine.Encrypt(data, true, key);

                // Получаем зашифрованную строку от сервера
                string encryptedResponse = await Task.Run(() =>
                    ds.GetUpdateProgramAvalon(nick_shop, encryptedData, MainStaticClass.GetWorkSchema.ToString())
                );

                Console.WriteLine($"Получен ответ от сервера длиной: {encryptedResponse?.Length ?? 0}");

                // Расшифровываем
                string decryptedResponse = CryptorEngine.Decrypt(encryptedResponse, true, key);

                // Разделяем версию и Base64 файл
                string[] parts = decryptedResponse.Split('|');

                if (parts.Length < 2)
                {
                    await ShowMessage("Ошибка формата ответа от сервера");
                    if (btnClose != null)
                        btnClose.IsEnabled = true;
                    return;
                }

                string serverVersion = parts[0];
                string base64File = parts[1];

                // Сравниваем версии
                long localVerNum = Convert.ToInt64(local_version.Replace(".", ""));
                long serverVerNum = Convert.ToInt64(serverVersion.Replace(".", ""));

                if (serverVerNum <= localVerNum)
                {
                    await ShowMessage("С сервера получена версия программы меньшая или равная текущей, обновление выполнено не будет");
                    if (btnClose != null)
                        btnClose.IsEnabled = true;
                    return;
                }

                Console.WriteLine($"Версия с сервера: {serverVersion}");
                Console.WriteLine($"Base64 длина: {base64File.Length}");

                // Конвертируем Base64 обратно в байты
                byte[] fileBytes = Convert.FromBase64String(base64File);

                Console.WriteLine($"Сконвертировано в байты: {fileBytes.Length}");

                // Сохраняем файл
                string startupPath = AppContext.BaseDirectory;
                string currentExePath = Path.Combine(startupPath, "Cash8Avalon.dll");

                // Создаем backup
                string backupPath = Path.Combine(startupPath, "Cash8Avalon.dll_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                if (File.Exists(currentExePath))
                {
                    File.Move(currentExePath, backupPath);
                    Console.WriteLine($"✓ Текущий файл переименован в: {Path.GetFileName(backupPath)}");
                }

                await File.WriteAllBytesAsync(currentExePath, fileBytes);
                Console.WriteLine($"✓ Новый файл сохранен: Cash8Avalon.dll, размер: {fileBytes.Length} байт");

                await ShowMessage("Обновление успешно загружено, теперь необходимо перезапустить программу");

                // Закрываем текущее окно с результатом
                this.Close(DialogResult.Yes);

                // Закрываем главное окно (это может привести к завершению приложения)
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // Попробуем вызвать Close для MainWindow
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow != null)
                    {
                        // ВАЖНО: Убедитесь, что логика в MainWindow_Closing / OnMainWindowClosed
                        // позволяет приложению завершиться после закрытия MainWindow.
                        mainWindow.Close();
                    }
                    // Даже если MainWindow не был null, но не завершило приложение,
                    // следующий шаг гарантирует завершение.
                }

                // Принудительно завершаем процесс ПОСЛЕ закрытия окон.
                // Это гарантирует завершение в любом случае, включая Linux.
                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await ShowMessage($"Ошибка при загрузке обновления: {ex.Message}");

                if (btnClose != null)
                    btnClose.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }

        private void EnableDownloadButton(bool enable)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var btnDownload = this.FindControl<Button>("BtnDownload");
                if (btnDownload != null)
                {
                    btnDownload.IsEnabled = enable;
                }
            });
        }

        private async Task ShowMessage(string message)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBoxHelper.Show(message, "Обновление программы",
                    MessageBoxButton.OK, MessageBoxType.Info, this);
            });
        }

        public void Close(MessageBoxResult result)
        {
            this.Tag = result;
            this.Close();
        }

        public new async Task<MessageBoxResult> ShowDialog(Window owner)
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>();

            this.Closed += (s, e) =>
            {
                tcs.TrySetResult(this.Tag as MessageBoxResult? ?? MessageBoxResult.None);
            };

            await base.ShowDialog(owner);

            return await tcs.Task;
        }
    }
}