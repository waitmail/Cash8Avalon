//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Cash8Avalon
//{
//    public class SberPaymentService
//    {
//        private readonly string _sberPath;
//        private readonly string _exeName;

//        public SberPaymentService()
//        {
//            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                _sberPath = Path.Combine(baseDir, "Sber", "Windows");
//                _exeName = "sb_pilot.exe";
//            }
//            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//            {
//                _sberPath = Path.Combine(baseDir, "Sber", "Linux");
//                _exeName = "sb_pilot";
//            }
//            else
//            {
//                throw new PlatformNotSupportedException("Данная операционная система не поддерживается.");
//            }
//        }

//        /// <summary>
//        /// Запуск оплаты или возврата через sb_pilot
//        /// </summary>
//        public async Task<PaymentResult> PayAsync(int amountInKopecks, int command = 1, string rrn = null, CancellationToken cancellationToken = default)
//        {
//            string args = $"{command} {amountInKopecks} 0";
//            if (!string.IsNullOrEmpty(rrn))
//            {
//                args += $" QSELECT {rrn.Trim()}";
//            }

//            return await ExecuteCommandAsync(args, cancellationToken);
//        }

//        /// <summary>
//        /// Печать краткого отчета (Контрольная лента). Команда 9, тип 0 (краткий)
//        /// </summary>
//        public async Task<PaymentResult> GetShortReportAsync(CancellationToken cancellationToken = default)
//        {
//            // По документации: 9 0 0 (9 - команда, 0 - обязательный параметр, 0 - краткий отчет)
//            string args = "9 0 0";

//            MainStaticClass.write_event_in_log($"Запуск краткого отчета: {args}", "Terminal", "0");

//            return await ExecuteCommandAsync(args, cancellationToken);
//        }

//        /// <summary>
//        /// Печать полного отчета (Контрольная лента). Команда 9, тип 1 (полный)
//        /// </summary>
//        public async Task<PaymentResult> GetFullReportAsync(CancellationToken cancellationToken = default)
//        {
//            // По документации: 9 0 1 (9 - команда, 0 - обязательный параметр, 1 - полный отчет)
//            string args = "9 0 1";

//            MainStaticClass.write_event_in_log($"Запуск полного отчета: {args}", "Terminal", "0");

//            return await ExecuteCommandAsync(args, cancellationToken);
//        }

//        /// <summary>
//        /// Сверка итогов / Закрытие дня (Аналог старого CloseDay). Команда 7 без параметров.
//        /// </summary>
//        public async Task<PaymentResult> CloseShiftAsync(CancellationToken cancellationToken = default)
//        {
//            // Строго как было в старой программе - просто "7"
//            string args = "7";

//            MainStaticClass.write_event_in_log($"Запуск сверки итогов (CloseDay): {args}", "Terminal", "0");

//            return await ExecuteCommandAsync(args, cancellationToken);
//        }

//        /// <summary>
//        /// Общий метод для выполнения любой команды sb_pilot
//        /// </summary>
//        private async Task<PaymentResult> ExecuteCommandAsync(string args, CancellationToken cancellationToken)
//        {
//            var result = new PaymentResult();

//            string exeFullPath = Path.Combine(_sberPath, _exeName);
//            string fileE = Path.Combine(_sberPath, "e");
//            string fileP = Path.Combine(_sberPath, "p");

//            if (!File.Exists(exeFullPath))
//            {
//                result.IsSuccess = false;
//                result.ErrorMessage = "Файл пилота не найден по пути: " + exeFullPath;
//                return result;
//            }

//            try
//            {
//                if (File.Exists(fileE)) File.Delete(fileE);
//                if (File.Exists(fileP)) File.Delete(fileP);
//            }
//            catch (Exception ex)
//            {
//                MainStaticClass.write_event_in_log($"Предупреждение: не удалось удалить старые файлы: {ex.Message}", "Terminal", "0");
//            }

//            Process process = null;
//            try
//            {
//                var startInfo = new ProcessStartInfo
//                {
//                    WorkingDirectory = _sberPath,
//                    UseShellExecute = false,
//                    WindowStyle = ProcessWindowStyle.Hidden
//                };

//                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                {
//                    startInfo.FileName = "x-terminal-emulator";
//                    startInfo.Arguments = $"--minimize -e \"{exeFullPath} {args}\"";
//                    startInfo.CreateNoWindow = true;
//                    startInfo.RedirectStandardOutput = false;
//                    startInfo.RedirectStandardError = false;
//                }
//                else // WINDOWS
//                {
//                    startInfo.FileName = exeFullPath;
//                    startInfo.Arguments = args;
//                    startInfo.CreateNoWindow = true;
//                    startInfo.RedirectStandardOutput = true;
//                    startInfo.RedirectStandardError = true;

//                    MainStaticClass.write_event_in_log($"Windows Run: {exeFullPath} {args}", "Terminal", "0");
//                }

//                process = new Process { StartInfo = startInfo };
//                process.Start();

//                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                {
//                    int timeoutMs = 120000;
//                    int elapsed = 0;

//                    while (!File.Exists(fileE) && elapsed < timeoutMs)
//                    {
//                        if (cancellationToken.IsCancellationRequested) break;
//                        await Task.Delay(500, cancellationToken);
//                        elapsed += 500;
//                    }
//                }
//                else
//                {
//                    var stdOutTask = process.StandardOutput.ReadToEndAsync();
//                    var stdErrTask = process.StandardError.ReadToEndAsync();
//                    await process.WaitForExitAsync(cancellationToken);

//                    string stdOut = await stdOutTask;
//                    string stdErr = await stdErrTask;

//                    if (!string.IsNullOrEmpty(stdErr))
//                        MainStaticClass.write_event_in_log($"sb_pilot ERROR: {stdErr}", "Terminal", "0");

//                    result.ExitCode = process.ExitCode;
//                }

//                // Пауза перед чтением файла
//                if (!File.Exists(fileE))
//                {
//                    await Task.Delay(200);
//                }

//                if (File.Exists(fileE))
//                {
//                    string eContent = File.ReadAllText(fileE, System.Text.Encoding.GetEncoding(866));
//                    ParseFileE(eContent, result);
//                }
//                else
//                {
//                    result.IsSuccess = false;
//                    result.ErrorMessage = "Файл результата 'e' не был создан. Возможно, терминал был закрыт или таймаут.";
//                }

//                if (result.IsSuccess && File.Exists(fileP))
//                {
//                    result.SlipContent = File.ReadAllText(fileP, System.Text.Encoding.GetEncoding(866));
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                result.IsSuccess = false;
//                result.ErrorMessage = "Операция отменена пользователем.";
//                if (process != null && !process.HasExited)
//                {
//                    try { process.Kill(); }
//                    catch { }
//                }
//            }
//            catch (System.ComponentModel.Win32Exception ex)
//            {
//                result.IsSuccess = false;
//                result.ErrorMessage = $"Ошибка запуска x-terminal-emulator: {ex.Message}. Проверьте, установлен ли эмулятор терминала.";
//                MainStaticClass.write_event_in_log($"Win32Exception: {ex.Message}", "Terminal", "0");
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccess = false;
//                result.ErrorMessage = $"Ошибка при выполнении: {ex.Message}";
//                MainStaticClass.write_event_in_log($"Exception: {ex.Message}", "Terminal", "0");
//            }
//            finally
//            {
//                process?.Dispose();
//            }

//            return result;
//        }

//        private void ParseFileE(string content, PaymentResult result)
//        {
//            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

//            if (lines.Length == 0)
//            {
//                result.IsSuccess = false;
//                result.ErrorMessage = "Файл результата 'e' пуст.";
//                return;
//            }

//            string firstLine = lines[0];
//            var parts = firstLine.Split(',');

//            if (parts.Length >= 1)
//            {
//                string codeStr = parts[0].Trim();

//                if (int.TryParse(codeStr, out int errorCode))
//                {
//                    if (errorCode == 0)
//                    {
//                        result.IsSuccess = true;
//                        result.ErrorMessage = parts.Length > 1 ? parts[1].Trim() : "Успешно";

//                        if (lines.Length > 3) result.AuthorizationCode = lines[3].Trim();
//                        if (lines.Length > 7) result.TerminalId = lines[7].Trim();
//                        if (lines.Length > 9) result.ReferenceNumber = lines[9].Trim();
//                    }
//                    else
//                    {
//                        result.IsSuccess = false;
//                        result.ErrorCode = errorCode;
//                        string fileMessage = parts.Length > 1 ? parts[1].Trim() : string.Empty;
//                        result.ErrorMessage = !string.IsNullOrEmpty(fileMessage) ? fileMessage : GetErrorDescription(errorCode);
//                    }
//                }
//                else
//                {
//                    result.IsSuccess = false;
//                    result.ErrorMessage = $"Некорректный формат ответа терминала: {firstLine}";
//                }
//            }
//        }

//        private string GetErrorDescription(int code)
//        {
//            return code switch
//            {
//                99 => "Пинпад не подключен",
//                5 => "Подождать с ответом. Скорее всего терминал выполняет перезагрузку",
//                2000 => "Повторите операцию, возможно на пинпаде нажата отмена",
//                7400 => "Операция заблокирована для пользователя",
//                4451 => "Недостаточно средств",
//                4452 => "Операция не прошла",
//                _ => $"Код ошибки {code}"
//            };
//        }
//    }

//    public class PaymentResult
//    {
//        public bool IsSuccess { get; set; }
//        public string ErrorMessage { get; set; }
//        public int ErrorCode { get; set; }
//        public int ExitCode { get; set; }

//        // При сверке итогов здесь будет текст Z-отчета (контрольной ленты)
//        public string SlipContent { get; set; }

//        public string AuthorizationCode { get; set; }
//        public string TerminalId { get; set; }
//        public string ReferenceNumber { get; set; }
//    }
//}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
        public async Task<PaymentResult> PayAsync(int amountInKopecks, int command = 1, string rrn = null, CancellationToken cancellationToken = default)
        {
            string args = $"{command} {amountInKopecks} 0";
            if (!string.IsNullOrEmpty(rrn))
            {
                args += $" QSELECT {rrn.Trim()}";
            }

            return await ExecuteCommandAsync(args, cancellationToken);
        }

        /// <summary>
        /// Печать краткого отчета (Контрольная лента). Команда 9, тип 0 (краткий)
        /// </summary>
        public async Task<PaymentResult> GetShortReportAsync(CancellationToken cancellationToken = default)
        {
            string args = "9 0 0";
            MainStaticClass.write_event_in_log($"Запуск краткого отчета: {args}", "Terminal", "0");
            return await ExecuteCommandAsync(args, cancellationToken);
        }

        /// <summary>
        /// Печать полного отчета (Контрольная лента). Команда 9, тип 1 (полный)
        /// </summary>
        public async Task<PaymentResult> GetFullReportAsync(CancellationToken cancellationToken = default)
        {
            string args = "9 0 1";
            MainStaticClass.write_event_in_log($"Запуск полного отчета: {args}", "Terminal", "0");
            return await ExecuteCommandAsync(args, cancellationToken);
        }

        /// <summary>
        /// Сверка итогов / Закрытие дня (Аналог старого CloseDay). Команда 7 без параметров.
        /// </summary>
        public async Task<PaymentResult> CloseShiftAsync(CancellationToken cancellationToken = default)
        {
            string args = "7";
            MainStaticClass.write_event_in_log($"Запуск сверки итогов (CloseDay): {args}", "Terminal", "0");
            return await ExecuteCommandAsync(args, cancellationToken);
        }

        /// <summary>
        /// Общий метод для выполнения любой команды sb_pilot
        /// </summary>
        private async Task<PaymentResult> ExecuteCommandAsync(string args, CancellationToken cancellationToken)
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
                var startInfo = new ProcessStartInfo
                {
                    WorkingDirectory = _sberPath,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    startInfo.FileName = "x-terminal-emulator";
                    startInfo.Arguments = $"--minimize -e \"{exeFullPath} {args}\"";
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.RedirectStandardError = false;
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
                    int timeoutMs = 120000;
                    int elapsed = 0;

                    while (!File.Exists(fileE) && elapsed < timeoutMs)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        await Task.Delay(500, cancellationToken);
                        elapsed += 500;
                    }
                }
                else
                {
                    var stdOutTask = process.StandardOutput.ReadToEndAsync();
                    var stdErrTask = process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(cancellationToken);

                    string stdOut = await stdOutTask;
                    string stdErr = await stdErrTask;

                    if (!string.IsNullOrEmpty(stdErr))
                        MainStaticClass.write_event_in_log($"sb_pilot ERROR: {stdErr}", "Terminal", "0");

                    result.ExitCode = process.ExitCode;
                }

                // Пауза перед чтением файла
                if (!File.Exists(fileE))
                {
                    await Task.Delay(200);
                }

                // ИЗМЕНЕНО ЗДЕСЬ: Читаем файл 'e' с умным определением кодировки (KOI8-R, UTF-8, 866)
                if (File.Exists(fileE))
                {
                    string eContent = ReadTextFileSafe(File.ReadAllBytes(fileE));
                    ParseFileE(eContent, result);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Файл результата 'e' не был создан. Возможно, терминал был закрыт или таймаут.";
                }

                // ИЗМЕНЕНО ЗДЕСЬ: Читаем файл 'p' (сам чек) с умным определением кодировки
                if (result.IsSuccess && File.Exists(fileP))
                {
                    result.SlipContent = ReadTextFileSafe(File.ReadAllBytes(fileP));
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

        // =====================================================================
        // ДОБАВЛЕННЫЙ МЕТОД ДЛЯ ПРАВИЛЬНОГО ЧТЕНИЯ КОДИРОВКИ
        // =====================================================================

        /// <summary>
        /// Умное чтение текстового файла от сберовского пилота.
        /// Сначала пробует KOI8-R (так как вы подтвердили, что файл в ней), 
        /// затем UTF-8 и старую добрую DOS-866.
        /// </summary>
        private string ReadTextFileSafe(byte[] fileBytes)
        {
            // 1. Пробуем KOI8-R (приоритет для Linux пилота)
            string koi8Text = Encoding.GetEncoding("KOI8-R").GetString(fileBytes);
            if (koi8Text.Any(c => c >= 'А' && c <= 'я'))
            {
                return koi8Text;
            }

            // 2. Пробуем UTF-8 (современные системы)
            string utf8Text = Encoding.UTF8.GetString(fileBytes);
            if (!utf8Text.Contains("\uFFFD") && utf8Text.Any(c => c >= 'А' && c <= 'я'))
            {
                return utf8Text;
            }

            // 3. Пробуем CP866 (старые Windows-версии пилота)
            string cp866Text = Encoding.GetEncoding(866).GetString(fileBytes);
            if (cp866Text.Any(c => c >= 'А' && c <= 'я'))
            {
                return cp866Text;
            }

            // 4. Если русских букв нет вообще (только цифры, латиница, спецсимволы), 
            // возвращаем как UTF-8, чтобы не сломать переводами строк
            return utf8Text;
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

                        if (lines.Length > 3) result.AuthorizationCode = lines[3].Trim();
                        if (lines.Length > 7) result.TerminalId = lines[7].Trim();
                        if (lines.Length > 9) result.ReferenceNumber = lines[9].Trim();
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

    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public int ExitCode { get; set; }

        // При сверке итогов здесь будет текст Z-отчета (контрольной ленты)
        public string SlipContent { get; set; }

        public string AuthorizationCode { get; set; }
        public string TerminalId { get; set; }
        public string ReferenceNumber { get; set; }
    }
}