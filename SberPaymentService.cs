using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public class SberPaymentService
    {
        private readonly string _sberPath;
        private readonly string _exeName;

        public SberPaymentService()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _sberPath = Path.Combine(baseDir, "Sber", "Windows");
                _exeName = "sb_pilot.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _sberPath = Path.Combine(baseDir, "Sber", "Linux");
                _exeName = "sb_pilot";
            }
            else
            {
                throw new PlatformNotSupportedException("Данная операционная система не поддерживается.");
            }
        }

        /// <summary>
        /// Запуск оплаты или возврата через sb_pilot
        /// </summary>
        /// <param name="amountInKopecks">Сумма в копейках</param>
        /// <param name="command">1=Оплата, 3=Возврат</param>
        /// <param name="rrn">RRN исходной транзакции (для возврата)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns></returns>
        public async Task<PaymentResult> PayAsync(int amountInKopecks, int command = 1, string rrn = null, CancellationToken cancellationToken = default)
        {
            var result = new PaymentResult();

            string exeFullPath = Path.Combine(_sberPath, _exeName);
            string fileE = Path.Combine(_sberPath, "e");
            string fileP = Path.Combine(_sberPath, "p");

            if (!File.Exists(exeFullPath))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Файл пилота не найден по пути: " + exeFullPath;
                return result;
            }

            try
            {
                if (File.Exists(fileE)) File.Delete(fileE);
                if (File.Exists(fileP)) File.Delete(fileP);
            }
            catch (Exception ex)
            {
                MainStaticClass.write_event_in_log($"Предупреждение: не удалось удалить старые файлы: {ex.Message}", "Terminal", "0");
            }

            Process process = null;
            try
            {
                // ======================================================
                // ФОРМИРОВАНИЕ АРГУМЕНТОВ
                // ======================================================
                string args = $"{command} {amountInKopecks} 0";
                if (!string.IsNullOrEmpty(rrn))
                {
                    args += $" QSELECT {rrn.Trim()}";
                }

                // ======================================================
                // НАСТРОЙКА ПРОЦЕССА
                // ======================================================
                var startInfo = new ProcessStartInfo
                {
                    WorkingDirectory = _sberPath,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // === СПЕЦИАЛЬНАЯ ЛОГИКА ДЛЯ LINUX ===
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Запуск через x-terminal-emulator
                    // --minimize пытается свернуть окно, -e выполняет команду
                    startInfo.FileName = "x-terminal-emulator";
                    startInfo.Arguments = $"--minimize -e \"{exeFullPath} {args}\"";

                    // НЕ перенаправляем потоки, так как процесс запущен в отдельном терминале
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.RedirectStandardError = false;

                    //MainStaticClass.write_event_in_log($"Linux Run (Terminal): {startInfo.FileName} {startInfo.Arguments}", "Terminal", "0");
                }
                else // WINDOWS
                {
                    startInfo.FileName = exeFullPath;
                    startInfo.Arguments = args;
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;

                    MainStaticClass.write_event_in_log($"Windows Run: {exeFullPath} {args}", "Terminal", "0");
                }

                process = new Process { StartInfo = startInfo };
                process.Start();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // В Linux терминал может работать в отдельном процессе.
                    // Мы ждем появления файла результата, так как сам процесс x-terminal-emulator может завершиться быстро
                    // или работать независимо.

                    // Таймаут ожидания (например, 2 минуты)
                    int timeoutMs = 120000;
                    int elapsed = 0;

                    while (!File.Exists(fileE) && elapsed < timeoutMs)
                    {
                        // Проверяем отмену операции
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        await Task.Delay(500, cancellationToken);
                        elapsed += 500;
                    }
                }
                else
                {
                    // Windows стандартное ожидание
                    var stdOutTask = process.StandardOutput.ReadToEndAsync();
                    var stdErrTask = process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(cancellationToken);

                    string stdOut = await stdOutTask;
                    string stdErr = await stdErrTask;

                    if (!string.IsNullOrEmpty(stdErr))
                        MainStaticClass.write_event_in_log($"sb_pilot ERROR: {stdErr}", "Terminal", "0");

                    result.ExitCode = process.ExitCode;
                }

                // ======================================================
                // ОБРАБОТКА РЕЗУЛЬТАТА
                // ======================================================

                // Небольшая пауза перед проверкой файла (на случай задержки записи)
                if (!File.Exists(fileE))
                {
                    await Task.Delay(200);
                }

                if (File.Exists(fileE))
                {
                    string eContent = File.ReadAllText(fileE, System.Text.Encoding.GetEncoding(866));
                    ParseFileE(eContent, result);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Файл результата 'e' не был создан. Возможно, терминал был закрыт или таймаут.";
                }

                if (result.IsSuccess && File.Exists(fileP))
                {
                    result.SlipContent = File.ReadAllText(fileP, System.Text.Encoding.GetEncoding(866));
                }
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Операция отменена пользователем.";
                if (process != null && !process.HasExited)
                {
                    try { process.Kill(); }
                    catch { }
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Ошибка запуска x-terminal-emulator: {ex.Message}. Проверьте, установлен ли эмулятор терминала.";
                MainStaticClass.write_event_in_log($"Win32Exception: {ex.Message}", "Terminal", "0");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Ошибка при выполнении: {ex.Message}";
                MainStaticClass.write_event_in_log($"Exception: {ex.Message}", "Terminal", "0");
            }
            finally
            {
                process?.Dispose();
            }

            return result;
        }

        private void ParseFileE(string content, PaymentResult result)
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            if (lines.Length == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Файл результата 'e' пуст.";
                return;
            }

            string firstLine = lines[0];
            var parts = firstLine.Split(',');

            if (parts.Length >= 1)
            {
                string codeStr = parts[0].Trim();

                if (int.TryParse(codeStr, out int errorCode))
                {
                    if (errorCode == 0)
                    {
                        result.IsSuccess = true;
                        result.ErrorMessage = parts.Length > 1 ? parts[1].Trim() : "Успешно";

                        // Парсинг данных согласно спецификации файла 'e'
                        if (lines.Length > 3) result.AuthorizationCode = lines[3].Trim();
                        if (lines.Length > 7) result.TerminalId = lines[7].Trim();
                        if (lines.Length > 9) result.ReferenceNumber = lines[9].Trim(); // RRN
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorCode = errorCode;
                        string fileMessage = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                        result.ErrorMessage = !string.IsNullOrEmpty(fileMessage) ? fileMessage : GetErrorDescription(errorCode);
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Некорректный формат ответа терминала: {firstLine}";
                }
            }
        }

        private string GetErrorDescription(int code)
        {
            return code switch
            {
                99 => "Пинпад не подключен",
                5 => "Подождать с ответом. Скорее всего терминал выполняет перезагрузку",
                2000 => "Повторите операцию, возможно на пинпаде нажата отмена",
                7400 => "Операция заблокирована для пользователя",
                4451 => "Недостаточно средств",
                4452 => "Операция не прошла",
                _ => $"Код ошибки {code}"
            };
        }
    }

    // Класс-модель для возврата результата
    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public int ExitCode { get; set; }
        public string SlipContent { get; set; } // Содержимое файла 'p'

        // Данные из файла 'e'
        public string AuthorizationCode { get; set; } // Строка 4 (индекс 3)
        public string TerminalId { get; set; }        // Строка 8 (индекс 7)
        public string ReferenceNumber { get; set; }   // Строка 10 (индекс 9) -> Это RRN для возврата
    }
}