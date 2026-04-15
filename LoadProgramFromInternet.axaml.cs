using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class LoadProgramFromInternet : Window, IDisposable
    {
        private bool _isUpdating = false;
        private bool _isClosed = false;
        private CancellationTokenSource? _cts;

        public bool new_version_of_the_program = false;
        public bool show_phone = false;

        private Button? _btnClose;
        private Button? _btnDownload;
        private Label? _labelUpdate;

        private string UpdateFolderPath => Path.Combine(AppContext.BaseDirectory, "Update");
        private string NewDllInUpdatePath => Path.Combine(AppContext.BaseDirectory, "Update", "Cash8Avalon.dll");

        public LoadProgramFromInternet()
        {
            InitializeComponent();
            this.Opened += OnOpened;
            this.Closed += OnClosed;

            _btnClose = this.FindControl<Button>("BtnClose");
            if (_btnClose != null)
                _btnClose.Click += BtnClose_Click;

            _btnDownload = this.FindControl<Button>("BtnDownload");
            if (_btnDownload != null)
            {
                _btnDownload.Click += BtnDownload_Click;
                _btnDownload.IsEnabled = false;  // ✅ Отключена по умолчанию
            }

            _labelUpdate = this.FindControl<Label>("LabelUpdate");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region Инициализация

        private async void OnOpened(object? sender, EventArgs e)
        {
            await Task.Delay(50);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                this.Activate();
                this.Focus();
                this.Topmost = false;
                this.Topmost = true;
                await Task.Delay(100);
                this.Activate();
            }, DispatcherPriority.Render);

            _ = SafeInitializeAsync();
        }

        private async Task SafeInitializeAsync()
        {
            try
            {
                if (!show_phone)
                    await CheckNewVersionProgramm();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Critical error: {ex}");
                if (!_isClosed)
                    SetStatusLabel($"Ошибка инициализации: {ex.Message}");
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _isClosed = true;
            _cts?.Cancel();
            Dispose();
        }

        #endregion

        #region Проверка версии

        public async Task CheckNewVersionProgramm()
        {
            if (_isUpdating || _isClosed) return;

            if (!MainStaticClass.service_is_worker())
            {
                SetStatusLabel("Веб-сервис недоступен");
                SetDownloadButtonEnabled(false);  // ✅ Синхронно
                return;
            }

            var ds = MainStaticClass.get_ds();
            ds.Timeout = 10000;

            string nick_shop = MainStaticClass.Nick_Shop.Trim();
            if (string.IsNullOrEmpty(nick_shop))
            {
                SetDownloadButtonEnabled(false);
                return;
            }

            string code_shop = MainStaticClass.Code_Shop.Trim();
            if (string.IsNullOrEmpty(code_shop))
            {
                SetDownloadButtonEnabled(false);
                return;
            }

            string count_day = CryptorEngine.get_count_day();
            string key = nick_shop + count_day + code_shop;
            string local_version = MainStaticClass.version();
            string data = code_shop + "|" + local_version + "|" + code_shop;

            try
            {
                string encrypted_data = CryptorEngine.Encrypt(data, true, key);

                string encrypted_response = await Task.Run(() =>
                    ds.ExistsUpdateProrgamAvalon(nick_shop, encrypted_data, MainStaticClass.GetWorkSchema.ToString())
                );

                if (string.IsNullOrEmpty(encrypted_response))
                {
                    SetStatusLabel("Не удалось проверить версию программы на сервере");
                    SetDownloadButtonEnabled(false);
                    return;
                }

                string server_version = CryptorEngine.Decrypt(encrypted_response, true, key);

                if (local_version == server_version)
                {
                    SetStatusLabel("У вас установлена самая последняя версия программы");
                    SetDownloadButtonEnabled(false);
                    return;
                }

                if (long.TryParse(local_version, out var local_ver) &&
                    long.TryParse(server_version, out var server_ver))
                {
                    if (server_ver > local_ver)
                    {
                        string formatted_date = FormatVersionDate(server_version);
                        SetStatusLabel($"Есть обновление программы от {formatted_date}");
                        SetDownloadButtonEnabled(true);  // ✅ Только здесь true
                        new_version_of_the_program = true;

                        if (!show_phone)
                        {
                            await BtnDownload_ClickAsync();
                        }
                    }
                    else
                    {
                        string local_date = FormatVersionDate(local_version);
                        string server_date = FormatVersionDate(server_version);
                        SetStatusLabel($"Ваша версия ({local_date}) новее версии на сервере ({server_date})");
                        SetDownloadButtonEnabled(false);
                    }
                }
                else
                {
                    SetStatusLabel("Ошибка формата версии");
                    SetDownloadButtonEnabled(false);
                    Debug.WriteLine($"[Update] Version parse error: local={local_version}, server={server_version}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Version check error: {ex}");
                SetStatusLabel($"Ошибка при проверке версии: {ex.Message}");
                SetDownloadButtonEnabled(false);
            }
        }

        private string FormatVersionDate(string timestamp)
        {
            if (long.TryParse(timestamp, out var ts))
            {
                try
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
                    return $"{date:dd.MM.yyyy HH:mm}";
                }
                catch
                {
                    return timestamp;
                }
            }
            return timestamp;
        }

        private void SetStatusLabel(string text)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                if (_labelUpdate != null)
                    _labelUpdate.Content = text;
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_labelUpdate != null)
                        _labelUpdate.Content = text;
                });
            }
        }

        /// <summary>
        /// Синхронная установка состояния кнопки скачивания
        /// </summary>
        private void SetDownloadButtonEnabled(bool enabled)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                if (_btnDownload != null)
                    _btnDownload.IsEnabled = enabled;
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_btnDownload != null)
                        _btnDownload.IsEnabled = enabled;
                });
            }
        }

        #endregion

        #region Загрузка обновления

        private async void BtnDownload_Click(object? sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;
            await BtnDownload_ClickAsync();
        }

        private async Task BtnDownload_ClickAsync()
        {
            if (_isUpdating || _isClosed) return;

            _isUpdating = true;
            _cts = new CancellationTokenSource();

            bool shouldEnableDownloadButton = false;  // ✅ По умолчанию отключена

            try
            {
                SetUiStateLoading(true);

                if (!MainStaticClass.service_is_worker())
                {
                    SetStatusLabel("Веб-сервис недоступен");
                    return;
                }

                var ds = MainStaticClass.get_ds();
                ds.Timeout = 100000;

                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (string.IsNullOrEmpty(nick_shop))
                {
                    SetStatusLabel("Не указан ник магазина");
                    return;
                }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (string.IsNullOrEmpty(code_shop))
                {
                    SetStatusLabel("Не указан код магазина");
                    return;
                }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop + count_day + code_shop;
                string local_version = MainStaticClass.version();
                string data = code_shop + "|" + local_version + "|" + code_shop;

                string encrypted_data = CryptorEngine.Encrypt(data, true, key);

                string encrypted_response = await Task.Run(() =>
                    ds.GetUpdateProgramAvalon(nick_shop, encrypted_data, MainStaticClass.GetWorkSchema.ToString()),
                    _cts.Token
                );

                if (string.IsNullOrEmpty(encrypted_response))
                {
                    SetStatusLabel("Пустой ответ от сервера");
                    return;
                }

                Debug.WriteLine($"[Update] Response length: {encrypted_response.Length}");

                string decrypted_response = CryptorEngine.Decrypt(encrypted_response, true, key);
                string[] parts = decrypted_response.Split('|', 2);

                if (parts.Length < 2)
                {
                    SetStatusLabel("Ошибка формата ответа от сервера");
                    return;
                }

                string server_version = parts[0];
                string base64_file = parts[1];

                if (!long.TryParse(local_version, out var local_ver) ||
                    !long.TryParse(server_version, out var server_ver))
                {
                    SetStatusLabel("Неверный формат версии");
                    return;
                }

                if (server_ver < local_ver)
                {
                    string local_date = FormatVersionDate(local_version);
                    string server_date = FormatVersionDate(server_version);
                    SetStatusLabel($"Ваша версия ({local_date}) новее версии на сервере ({server_date})");
                    return;
                }

                if (server_ver == local_ver)
                {
                    SetStatusLabel("У вас установлена самая последняя версия программы");
                    return;
                }

                byte[] file_bytes;
                try
                {
                    file_bytes = Convert.FromBase64String(base64_file);
                }
                catch (FormatException)
                {
                    SetStatusLabel("Повреждённые данные обновления");
                    return;
                }

                Debug.WriteLine($"[Update] File size: {file_bytes.Length} bytes");

                if (file_bytes.Length < 1024)
                {
                    SetStatusLabel("Файл обновления слишком мал");
                    return;
                }

                bool save_success = await SaveUpdateToFolderAsync(file_bytes);
                if (!save_success)
                {
                    SetStatusLabel("Не удалось сохранить обновление");
                    return;
                }

                await ShowMessage("Обновление успешно загружено в папку Update.\nПрограмма будет закрыта.");

                CloseWithResult(true);
                await RestartApplicationAsync();
                return;  // Успешное завершение
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[Update] Operation canceled");
                SetStatusLabel("Операция отменена");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Error: {ex}");
                SetStatusLabel($"Ошибка: {ex.Message}");
            }
            finally
            {
                _isUpdating = false;
                // ✅ Кнопка скачивания остаётся ОТКЛЮЧЕННОЙ после завершения
                SetUiStateLoading(false);
            }
        }

        private async Task<bool> SaveUpdateToFolderAsync(byte[] file_bytes)
        {
            try
            {
                if (!Directory.Exists(UpdateFolderPath))
                {
                    Directory.CreateDirectory(UpdateFolderPath);
                    Debug.WriteLine($"[Update] Created folder: {UpdateFolderPath}");
                }

                try
                {
                    foreach (var file in Directory.GetFiles(UpdateFolderPath))
                    {
                        File.Delete(file);
                        Debug.WriteLine($"[Update] Deleted old file: {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Update] Warning - could not clear Update folder: {ex.Message}");
                }

                await File.WriteAllBytesAsync(NewDllInUpdatePath, file_bytes, _cts?.Token ?? CancellationToken.None);

                var written_file = new FileInfo(NewDllInUpdatePath);
                Debug.WriteLine($"[Update] Saved: {written_file.FullName}, size: {written_file.Length}");

                if (written_file.Length != file_bytes.Length)
                {
                    Debug.WriteLine($"[Update] Size mismatch! Expected: {file_bytes.Length}, Got: {written_file.Length}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Save error: {ex.Message}");
                return false;
            }
        }

        private async Task RestartApplicationAsync()
        {
            await Task.Delay(300);

            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Restart error: {ex.Message}");
                Environment.Exit(0);
            }
        }

        #endregion

        #region UI Helpers

        /// <summary>
        /// Устанавливает состояние загрузки (только текст кнопки и состояние кнопки закрытия)
        /// </summary>
        private void SetUiStateLoading(bool isLoading)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                if (_btnDownload != null)
                {
                    _btnDownload.Content = isLoading ? "Загрузка..." : "Скачать обновление";
                    _btnDownload.IsEnabled = false;  // ✅ Всегда false после завершения
                }

                if (_btnClose != null)
                    _btnClose.IsEnabled = !isLoading;
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_btnDownload != null)
                    {
                        _btnDownload.Content = isLoading ? "Загрузка..." : "Скачать обновление";
                        _btnDownload.IsEnabled = false;  // ✅ Всегда false после завершения
                    }

                    if (_btnClose != null)
                        _btnClose.IsEnabled = !isLoading;
                });
            }
        }

        private async Task ShowMessage(string message)
        {
            if (_isClosed) return;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBoxHelper.Show(message, "Обновление программы",
                    MessageBoxButton.OK, MessageBoxType.Info, this);
            });
        }

        #endregion

        #region Закрытие окна

        private void BtnClose_Click(object? sender, RoutedEventArgs e)
        {
            CloseWithResult(false);
        }

        private void CloseWithResult(bool result)
        {
            if (_isClosed) return;
            _isClosed = true;

            if (Dispatcher.UIThread.CheckAccess())
            {
                this.Tag = result;
                this.Close();
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    this.Tag = result;
                    this.Close();
                });
            }
        }

        public new async Task<bool> ShowDialog(Window owner)
        {
            var tcs = new TaskCompletionSource<bool>();
            this.Closed += (s, e) => tcs.TrySetResult(this.Tag as bool? ?? false);
            await base.ShowDialog(owner);
            return await tcs.Task;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        #endregion
    }
}