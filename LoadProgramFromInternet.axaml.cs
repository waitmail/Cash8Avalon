using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Npgsql;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class LoadProgramFromInternet : Window
    {
        private string version = "";
        public bool new_version_of_the_program = false;
        public bool show_phone = false;

        public LoadProgramFromInternet()
        {
            InitializeComponent();
            this.Opened += LoadProgramFromInternet_Opened;
            // Находим кнопку и подписываемся на событие
            var btnClose = this.FindControl<Button>("BtnClose");
            if (btnClose != null)
            {
                btnClose.Click += BtnClose_Click;
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
            ds.Timeout = 1000;

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
            string data = code_shop.Trim() + "|" + MainStaticClass.version() + "|" + code_shop.Trim();
            string result_web_query = "";

            try
            {
                result_web_query = await Task.Run(() =>
                    ds.ExistsUpdateProrgam(nick_shop, CryptorEngine.Encrypt(data, true, key), MainStaticClass.GetWorkSchema.ToString())
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

        private async Task check_and_update_npgsql()
        {
            string startupPath = AppContext.BaseDirectory;
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(startupPath, "Npgsql.dll"));

            string fileVersion = myFileVersionInfo.FileVersion?.Replace(".", "") ?? "0";
            int cash_version = int.Parse(fileVersion);

            if (cash_version == 20100)//Старая версия Npgsql 
            {
                string previousPath = Path.Combine(startupPath, "PreviousNpgsql");
                string updatePath = Path.Combine(startupPath, "UpdateNpgsql");

                if (!Directory.Exists(previousPath))
                {
                    Directory.CreateDirectory(previousPath);
                }

                if (!Directory.Exists(updatePath))
                {
                    Directory.CreateDirectory(updatePath);
                }

                if (!MainStaticClass.service_is_worker())
                {
                    return;
                }

                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 50000;

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
                string data = code_shop.Trim() + "|" + code_shop.Trim();

                byte[] result_web_query = new byte[0];

                try
                {
                    result_web_query = await Task.Run(() =>
                        ds.GetNpgsqlNew(nick_shop, CryptorEngine.Encrypt(data, true, key), MainStaticClass.GetWorkSchema.ToString())
                    );
                }
                catch (Exception ex)
                {
                    await ShowMessage(ex.Message);
                    return;
                }

                string npgsqlPath = Path.Combine(updatePath, "Npgsql.dll");
                await File.WriteAllBytesAsync(npgsqlPath, result_web_query);

                try
                {
                    File.Copy(Path.Combine(startupPath, "Npgsql.dll"),
                             Path.Combine(previousPath, "Npgsql.dll"), true);

                    if ((await File.ReadAllBytesAsync(npgsqlPath)).Length > 0)
                    {
                        File.Copy(npgsqlPath, Path.Combine(startupPath, "Npgsql.dll"), true);
                        await ShowMessage("Библиотека Npgsql.dll успешно обновлена");
                    }
                }
                catch (Exception ex)
                {
                    await ShowMessage(ex.Message);
                }
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            await BtnDownload_ClickAsync();
        }

        private async Task BtnDownload_ClickAsync()
        {
            await check_and_update_npgsql();

            var btnClose = this.FindControl<Button>("BtnClose");
            if (btnClose != null)
                btnClose.IsEnabled = false;

            if (!MainStaticClass.service_is_worker())
            {
                return;
            }

            DS ds = MainStaticClass.get_ds();
            ds.Timeout = 10000;

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
            string my_version = version;
            string data = code_shop.Trim() + "|" + version + "|" + code_shop.Trim();

            byte[] result_web_query = new byte[0];

            try
            {
                result_web_query = await Task.Run(() =>
                    ds.GetUpdateProgram(nick_shop, CryptorEngine.Encrypt(data, true, key), MainStaticClass.GetWorkSchema.ToString())
                );
            }
            catch
            {
                return;
            }

            if (result_web_query.Length > 10)
            {
                try
                {
                    string startupPath = AppContext.BaseDirectory;
                    string updatePath = Path.Combine(startupPath, "Update");
                    string previousPath = Path.Combine(startupPath, "Previous");

                    if (!Directory.Exists(updatePath))
                    {
                        Directory.CreateDirectory(updatePath);
                    }

                    string exePath = Path.Combine(updatePath, "Cash.exe");
                    await File.WriteAllBytesAsync(exePath, result_web_query);

                    if (!Directory.Exists(previousPath))
                    {
                        Directory.CreateDirectory(previousPath);
                    }

                    File.Copy(Path.Combine(startupPath, "Cash.exe"),
                             Path.Combine(previousPath, "Cash.exe"), true);

                    await ShowMessage("Обновление успешно загружено, теперь необходимо перезапустить программу");

                    this.Close(DialogResult.Yes);
                }
                catch (Exception ex)
                {
                    await ShowMessage("При загрузке произошли ошибки " + ex.Message);
                }
            }

            if (btnClose != null)
                btnClose.IsEnabled = true;
        }        

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close(DialogResult.Cancel);
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
                await MessageBox.Show(message, "Обновление программы",
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