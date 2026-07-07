//using Avalonia;
//using Avalonia.Controls.ApplicationLifetimes;
//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Cash8Avalon
//{
//    public static class UpdateManager
//    {
//        public static string UpdateFolderPath => Path.Combine(AppContext.BaseDirectory, "Update");
//        public static string NewDllInUpdatePath => Path.Combine(AppContext.BaseDirectory, "Update", "Cash8Avalon.dll");

//        /// <summary>
//        /// Скачивает и сохраняет обновление.
//        /// </summary>
//        /// <param name="progress">Для отчета о статусе (можно передать null для фонового режима)</param>
//        /// <param name="cancellationToken">Токен отмены</param>
//        /// <returns>True, если файл успешно скачан и сохранен</returns>
//        public static async Task<bool> DownloadAndSaveUpdateAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                progress?.Report("Проверка веб-сервиса...");
//                if (!MainStaticClass.service_is_worker())
//                {
//                    progress?.Report("Веб-сервис недоступен");
//                    return false;
//                }

//                var ds = MainStaticClass.get_ds();
//                ds.Timeout = 100000;

//                string nick_shop = MainStaticClass.Nick_Shop.Trim();
//                if (string.IsNullOrEmpty(nick_shop)) { progress?.Report("Не указан ник магазина"); return false; }

//                string code_shop = MainStaticClass.Code_Shop.Trim();
//                if (string.IsNullOrEmpty(code_shop)) { progress?.Report("Не указан код магазина"); return false; }

//                string count_day = CryptorEngine.get_count_day();
//                string key = nick_shop + count_day + code_shop;
//                string local_version = MainStaticClass.version();
//                string data = code_shop + "|" + local_version + "|" + code_shop;

//                string encrypted_data = CryptorEngine.Encrypt(data, true, key);

//                progress?.Report("Запрос файла обновления с сервера...");
//                string encrypted_response = await Task.Run(() =>
//                    ds.GetUpdateProgramAvalon(nick_shop, encrypted_data, MainStaticClass.GetWorkSchema.ToString()),
//                    cancellationToken
//                );

//                if (string.IsNullOrEmpty(encrypted_response)) { progress?.Report("Пустой ответ от сервера"); return false; }

//                string decrypted_response = CryptorEngine.Decrypt(encrypted_response, true, key);
//                string[] parts = decrypted_response.Split('|', 2);

//                if (parts.Length < 2) { progress?.Report("Ошибка формата ответа от сервера"); return false; }

//                string server_version = parts[0];
//                string base64_file = parts[1];

//                if (!long.TryParse(local_version, out var local_ver) || !long.TryParse(server_version, out var server_ver))
//                {
//                    progress?.Report("Неверный формат версии"); return false;
//                }

//                if (server_ver <= local_ver)
//                {
//                    progress?.Report("Уже установлена последняя версия"); return false;
//                }

//                byte[] file_bytes;
//                try
//                {
//                    file_bytes = Convert.FromBase64String(base64_file);
//                }
//                catch (FormatException)
//                {
//                    progress?.Report("Повреждённые данные обновления"); return false;
//                }

//                if (file_bytes.Length < 1024) { progress?.Report("Файл обновления слишком мал"); return false; }

//                progress?.Report("Сохранение файла...");
//                bool save_success = await SaveUpdateToFolderAsync(file_bytes, cancellationToken);
//                if (!save_success) { progress?.Report("Не удалось сохранить обновление"); return false; }

//                progress?.Report("Обновление успешно загружено.");
//                return true;
//            }
//            catch (OperationCanceledException)
//            {
//                Debug.WriteLine("[UpdateManager] Operation canceled");
//                progress?.Report("Операция отменена");
//                return false;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[UpdateManager] Error: {ex}");
//                progress?.Report($"Ошибка: {ex.Message}");
//                return false;
//            }
//        }

//        private static async Task<bool> SaveUpdateToFolderAsync(byte[] file_bytes, CancellationToken cancellationToken)
//        {
//            try
//            {
//                if (!Directory.Exists(UpdateFolderPath))
//                    Directory.CreateDirectory(UpdateFolderPath);

//                // Очистка старых файлов
//                try
//                {
//                    foreach (var file in Directory.GetFiles(UpdateFolderPath))
//                        File.Delete(file);
//                }
//                catch { /* Игнорируем ошибки удаления старых файлов */ }

//                await File.WriteAllBytesAsync(NewDllInUpdatePath, file_bytes, cancellationToken);

//                var written_file = new FileInfo(NewDllInUpdatePath);
//                return written_file.Length == file_bytes.Length;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[UpdateManager] Save error: {ex.Message}");
//                return false;
//            }
//        }

//        /// <summary>
//        /// Закрывает приложение (для применения обновления)
//        /// </summary>
//        public static async Task RestartApplicationAsync()
//        {
//            await Task.Delay(300);
//            try
//            {
//                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
//                    desktop.Shutdown();
//                else
//                    Environment.Exit(0);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[UpdateManager] Restart error: {ex.Message}");
//                Environment.Exit(0);
//            }
//        }
//    }
//}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public static class UpdateManager
    {
        public static string UpdateFolderPath => Path.Combine(AppContext.BaseDirectory, "Update");
        public static string NewDllInUpdatePath => Path.Combine(AppContext.BaseDirectory, "Update", "Cash8Avalon.dll");

        // ✅ ДОБАВЛЕНО: Путь к файлу с версией скачанного обновления
        public static string VersionFilePath => Path.Combine(AppContext.BaseDirectory, "Update", "version.txt");

        /// <summary>
        /// ✅ ДОБАВЛЕНО: Возвращает версию скачанного файла обновления (или пустую строку, если файла нет)
        /// </summary>
        public static string GetDownloadedVersion()
        {
            try
            {
                if (File.Exists(VersionFilePath))
                    return File.ReadAllText(VersionFilePath).Trim();
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Скачивает и сохраняет обновление.
        /// </summary>
        /// <param name="progress">Для отчета о статусе (можно передать null для фонового режима)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>True - скачано новое или уже скачано ожидает перезапуска, False - ошибки/нет обновления</returns>
        public static async Task<bool> DownloadAndSaveUpdateAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                progress?.Report("Проверка веб-сервиса...");
                if (!MainStaticClass.service_is_worker())
                {
                    progress?.Report("Веб-сервис недоступен");
                    return false;
                }

                var ds = MainStaticClass.get_ds();
                ds.Timeout = 100000;

                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (string.IsNullOrEmpty(nick_shop)) { progress?.Report("Не указан ник магазина"); return false; }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (string.IsNullOrEmpty(code_shop)) { progress?.Report("Не указан код магазина"); return false; }

                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop + count_day + code_shop;
                string local_version = MainStaticClass.version();

                // ✅ ДОБАВЛЕНО: Читаем версию уже скачанного файла (если есть)
                string cached_version = GetDownloadedVersion();

                // ✅ ВАЖНО: Если в папке Update уже есть файл, отправляем серверу ЕГО версию.
                // Это позволит серверу понять, что у нас уже есть этот баг, и не присылать файл повторно.
                string version_to_send = !string.IsNullOrEmpty(cached_version) ? cached_version : local_version;
                string data = code_shop + "|" + version_to_send + "|" + code_shop;

                string encrypted_data = CryptorEngine.Encrypt(data, true, key);

                progress?.Report("Запрос файла обновления с сервера...");
                string encrypted_response = await Task.Run(() =>
                    ds.GetUpdateProgramAvalon(nick_shop, encrypted_data, MainStaticClass.GetWorkSchema.ToString()),
                    cancellationToken
                );

                if (string.IsNullOrEmpty(encrypted_response)) { progress?.Report("Пустой ответ от сервера"); return false; }

                string decrypted_response = CryptorEngine.Decrypt(encrypted_response, true, key);
                string[] parts = decrypted_response.Split('|', 2);

                if (parts.Length < 2) { progress?.Report("Ошибка формата ответа от сервера"); return false; }

                string server_version = parts[0];
                string base64_file = parts[1];

                if (!long.TryParse(local_version, out var local_ver) || !long.TryParse(server_version, out var server_ver))
                {
                    progress?.Report("Неверный формат версии"); return false;
                }

                // ✅ ДОБАВЛЕНО: Если скачанная версия равна серверной (или новее), не качаем повторно!
                if (!string.IsNullOrEmpty(cached_version) && long.TryParse(cached_version, out var cached_ver))
                {
                    if (server_ver <= cached_ver)
                    {
                        progress?.Report("Эта версия уже скачана и ожидает перезапуска.");
                        // Возвращаем true! Это скажет таймеру: "Всё хорошо, обновление у нас есть, сбрось таймер на 30 минут"
                        return true;
                    }
                }

                if (server_ver <= local_ver)
                {
                    progress?.Report("Уже установлена последняя версия");
                    return false;
                }

                byte[] file_bytes;
                try
                {
                    file_bytes = Convert.FromBase64String(base64_file);
                }
                catch (FormatException)
                {
                    progress?.Report("Повреждённые данные обновления"); return false;
                }

                if (file_bytes.Length < 1024) { progress?.Report("Файл обновления слишком мал"); return false; }

                progress?.Report("Сохранение файла...");
                bool save_success = await SaveUpdateToFolderAsync(file_bytes, server_version, cancellationToken);
                if (!save_success) { progress?.Report("Не удалось сохранить обновление"); return false; }

                progress?.Report("Обновление успешно загружено.");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[UpdateManager] Operation canceled");
                progress?.Report("Операция отменена");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateManager] Error: {ex}");
                progress?.Report($"Ошибка: {ex.Message}");
                return false;
            }
        }

        // ✅ ИЗМЕНЕНО: Добавлен параметр server_version
        private static async Task<bool> SaveUpdateToFolderAsync(byte[] file_bytes, string server_version, CancellationToken cancellationToken)
        {
            try
            {
                if (!Directory.Exists(UpdateFolderPath))
                    Directory.CreateDirectory(UpdateFolderPath);

                // Очистка старых файлов
                try
                {
                    foreach (var file in Directory.GetFiles(UpdateFolderPath))
                        File.Delete(file);
                }
                catch { /* Игнорируем ошибки удаления старых файлов */ }

                await File.WriteAllBytesAsync(NewDllInUpdatePath, file_bytes, cancellationToken);

                // ✅ ДОБАВЛЕНО: Сохраняем версию в текстовый файл для будущих проверок
                await File.WriteAllTextAsync(VersionFilePath, server_version, cancellationToken);

                var written_file = new FileInfo(NewDllInUpdatePath);
                return written_file.Length == file_bytes.Length;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateManager] Save error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Закрывает приложение (для применения обновления)
        /// </summary>
        public static async Task RestartApplicationAsync()
        {
            await Task.Delay(300);
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.Shutdown();
                else
                    Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateManager] Restart error: {ex.Message}");
                Environment.Exit(0);
            }
        }
    }
}