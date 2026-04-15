using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Cash8Avalon
{
    /// <summary>
    /// Результат выполнения команды терминала
    /// </summary>
    public class TerminalResult
    {
        public bool IsSuccess { get; set; }
        public string CodeResponse { get; set; } // Поле 39
        public string CodeResponse15 { get; set; } // Поле 15 (для СБП статусов)
        public string AuthorizationCode { get; set; } = string.Empty; // Поле 13
        public string ReferenceNumber { get; set; } = string.Empty; // Поле 14
        public string RechargeNote { get; set; } = string.Empty; // Поле 90
        public string ErrorMessage { get; set; }

        // Информация о попытках
        public int AttemptsCount { get; set; } = 1;
        public List<string> AttemptErrors { get; set; } = new List<string>();

        public static TerminalResult CreateError(string message)
        {
            return new TerminalResult { IsSuccess = false, ErrorMessage = message };
        }
    }

    /// <summary>
    /// Окно ожидания ответа от эквайрингового терминала
    /// </summary>
    public partial class WaitNonCashPay : Window
    {
        // Настройки повторных попыток
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 1000;

        // Поля для управления состоянием окна
        private CancellationTokenSource _cts;
        private int _secondsRemaining;
        private bool _isClosed = false;
        private readonly TaskCompletionSource<TerminalResult> _tcs = new();
        private readonly int _totalSeconds;
        private int _currentAttempt = 1;

        // Публичные свойства для передачи данных
        public string Url { get; set; }
        public string Data { get; set; }

        // Для обратной совместимости
        internal Cash_check cc { get; set; }
        public event Action<bool, Pay.AnswerTerminal> CommandCompleted;
        public event EventHandler<bool> PaymentCompleted;
        internal CommandResult commandResult = null;
        private bool _commandCompletedInvoked = false;
        private readonly TaskCompletionSource<bool> _windowClosedTcs = new();

        // НОВОЕ: Свойство для кастомной операции (например, для Сбера)
        // Принимает токен отмены, возвращает результат терминала
        public Func<CancellationToken, Task<TerminalResult>> CustomOperation { get; set; }

        public WaitNonCashPay() : this(80) { }

        public WaitNonCashPay(int timeoutSeconds)
        {
            InitializeComponent();
            _totalSeconds = timeoutSeconds;
            _secondsRemaining = timeoutSeconds;

            if (ProgressBarNonCashPay != null)
            {
                ProgressBarNonCashPay.Maximum = timeoutSeconds;
                ProgressBarNonCashPay.Value = timeoutSeconds;
            }
            if (LabelTimer != null) LabelTimer.Text = timeoutSeconds.ToString();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this.ShowInTaskbar = false;
            this.Opened += OnOpened;
        }

        #region Инициализация окна

        private async void OnOpened(object sender, EventArgs e) => _ = SafeInitializeAsync();

        private async Task SafeInitializeAsync()
        {
            try
            {
                _ = ActivateWindowSafely();
                await RunBackgroundTasksAsync();
            }
            catch (Exception ex)
            {
                if (!_isClosed) CloseWithResult(TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Ошибка инициализации")));
            }
        }

        private async Task ActivateWindowSafely()
        {
            try { await MessageBoxHelper.ActivateWindow(this); } catch { }
        }

        #endregion

        #region Фоновые задачи

        private async Task RunBackgroundTasksAsync()
        {
            try
            {
                var timerTask = RunTimerAsync();
                var commandTask = SendCommandAsync();
                await Task.WhenAll(timerTask, commandTask);
            }
            catch (Exception ex)
            {
                if (!_isClosed)
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Сбой операции"))));
            }
        }

        private async Task RunTimerAsync()
        {
            _cts = new CancellationTokenSource();
            try
            {
                while (_secondsRemaining > 0 && !_cts.Token.IsCancellationRequested)
                {
                    if (_isClosed) return;
                    UpdateTimerDisplay();
                    await Task.Delay(1000, _cts.Token);
                    _secondsRemaining--;
                }

                if (_secondsRemaining <= 0 && !_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (StatusLabel != null) { StatusLabel.Text = "Время ожидания истекло"; StatusLabel.Foreground = Brushes.Red; }
                        if (CancelButton != null) CancelButton.Content = "Закрыть";
                    });
                    _cts.Cancel();
                    CloseWithResult(TerminalResult.CreateError("Терминал не ответил вовремя.\n\nПроверьте:\n• Терминал включен\n• Сетевой кабель подключен"));
                }
            }
            catch (OperationCanceledException) { }
        }

        public static async Task<TerminalResult> ShowCustomAndWaitAsync(Window owner, int timeoutSeconds, Func<CancellationToken, Task<TerminalResult>> operation, Cash_check cashCheck)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var dialog = new WaitNonCashPay(timeoutSeconds)
            {
                CustomOperation = operation, // Передаем нашу операцию
                cc = cashCheck
            };
            await dialog.ShowDialog(owner);
            return await dialog._tcs.Task;
        }

        private async Task<TerminalResult> SendCommandAsync()
        {
            try
            {
                TerminalResult result;

                // Если передана кастомная операция (Сбер), выполняем её
                if (CustomOperation != null)
                {
                    // Обновляем статус
                    Dispatcher.UIThread.Post(() => { if (StatusLabel != null) StatusLabel.Text = "Выполнение команды на терминале..."; });

                    result = await CustomOperation(_cts.Token).ConfigureAwait(false);
                }
                else
                {
                    // Иначе стандартная логика РНКБ (HTTP с ретраями)
                    result = await SendRequestWithRetryAsync(_cts.Token).ConfigureAwait(false);
                }

                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                    await WaitForWindowCloseAsync();
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                var result = TerminalResult.CreateError("Операция отменена");
                if (!_isClosed) { await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result)); await WaitForWindowCloseAsync(); }
                return result;
            }
            catch (Exception ex)
            {
                var result = TerminalResult.CreateError($"Ошибка: {ex.Message}");
                if (!_isClosed) { await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result)); await WaitForWindowCloseAsync(); }
                return result;
            }
        }

        #endregion

        #region HTTP запрос (Retry Logic)

        private async Task<TerminalResult> SendRequestWithRetryAsync(CancellationToken cancellationToken)
        {
            TerminalResult lastResult = null;
            var attemptErrors = new List<string>();

            // ИСПРАВЛЕНО: Минимум 20 секунд на запрос
            int singleRequestTimeout = Math.Max(20, (_totalSeconds - (MaxRetryAttempts - 1) * (RetryDelayMs / 1000)) / MaxRetryAttempts);

            using var overallTimeoutCts = new CancellationTokenSource(_totalSeconds * 1000);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, overallTimeoutCts.Token);

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                if (linkedCts.Token.IsCancellationRequested || _isClosed)
                    return TerminalResult.CreateError("Операция отменена");

                _currentAttempt = attempt;
                UpdateAttemptDisplay(attempt, MaxRetryAttempts);

                // Логирование попытки
                MainStaticClass.write_event_in_log($"Попытка {attempt}/{MaxRetryAttempts} отправки на {Url}", "Terminal", cc?.numdoc.ToString() ?? "0");

                try
                {
                    lastResult = await SendRequestAsync(Url, Data, singleRequestTimeout).ConfigureAwait(false);
                    lastResult.AttemptErrors = attemptErrors;
                    lastResult.AttemptsCount = attempt;

                    if (lastResult.IsSuccess)
                    {
                        InvokeCommandCompleted(lastResult);
                        return lastResult;
                    }

                    attemptErrors.Add($"Попытка {attempt}: {lastResult.ErrorMessage}");

                    if (!IsRetryableError(lastResult))
                    {
                        InvokeCommandCompleted(lastResult);
                        return lastResult;
                    }

                    // Экспоненциальная задержка (1с, 2с, 3с)
                    if (attempt < MaxRetryAttempts)
                    {
                        try { await Task.Delay(RetryDelayMs * attempt, linkedCts.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) { return TerminalResult.CreateError("Превышено время ожидания"); }
                    }
                }
                catch (OperationCanceledException) when (overallTimeoutCts.Token.IsCancellationRequested)
                {
                    return TerminalResult.CreateError($"Превышено общее время ожидания ({_totalSeconds} сек).");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    attemptErrors.Add($"Попытка {attempt}: {ex.Message}");
                    lastResult = TerminalResult.CreateError(ex.Message);
                    if (attempt < MaxRetryAttempts)
                    {
                        try { await Task.Delay(RetryDelayMs * attempt, linkedCts.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) { return TerminalResult.CreateError("Превышено время ожидания"); }
                    }
                }
            }

            if (lastResult != null)
            {
                InvokeCommandCompleted(lastResult);
                if (attemptErrors.Count > 1) lastResult.ErrorMessage = $"После {MaxRetryAttempts} попыток:\n\n{lastResult.ErrorMessage}";
            }
            else lastResult = TerminalResult.CreateError("Все попытки исчерпаны");

            return lastResult;
        }

        /// <summary>
        /// Отправляет POST-запрос к терминалу и парсит XML-ответ.
        /// Не показывает окно, не управляет таймером — только сетевая логика.
        /// </summary>
        public static async Task<TerminalResult> SendRequestAsync(string url, string data, int timeoutSeconds = 80)
        {
            System.Diagnostics.Debugger.Break();

            //string mockXmlResponse = @"<?xml version=""1.0"" encoding=""windows-1251"" standalone=""no""?>
            //<response>
            //    <field id=""0"">25800</field>
            //    <field id=""4"">643</field>
            //    <field id=""6"">20260414155022</field>
            //    <field id=""9"">0</field>
            //    <field id=""10"">************6199</field>
            //    <field id=""13"">244171</field>
            //    <field id=""14"">114658723171</field>
            //    <field id=""15"">001</field>
            //    <field id=""19"">ОДОБРЕНО</field>
            //    <field id=""21"">20260414155020</field>
            //    <field id=""23"">0</field>
            //    <field id=""25"">1</field>
            //    <field id=""26"">0</field>
            //    <field id=""27"">W0260144</field>
            //    <field id=""28"">00000000</field>
            //    <field id=""39"">1</field>
            //    <field hex=""true"" id=""86"">EE38D10436313939D22845423638344138413137323334454344383238413344443230314136384242433230354442454237D306323230303033</field>
            //    <field id=""90"">0x4F^^A0000006581010~0x95^^95058080008000~0xDD^^ /~0xDE^^МИР КРЕДИТ PIX 1010~</field>
            //</response>";

            //MainStaticClass.write_event_in_log("ВНИМАНИЕ: Используется ЗАГЛУШКА терминала (MOCK)", "Terminal", "0");
            //return ParseResponse(mockXmlResponse);


            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                var content = new StringContent(data, Encoding.GetEncoding("Windows-1251"), "text/xml");
                var response = await client.PostAsync(url, content).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // ИСПРАВЛЕНО: Вернул понятные сообщения
                    return TerminalResult.CreateError(GetHttpErrorMessage(response.StatusCode, response.ReasonPhrase));
                }

                return ParseResponse(responseContent);
            }
            catch (TaskCanceledException)
            {
                return TerminalResult.CreateError("Терминал не ответил вовремя.\n\nПроверьте:\n• Терминал включен\n• Сетевой кабель подключен");
            }
            catch (HttpRequestException ex)
            {
                // ИСПРАВЛЕНО: Вернул понятные сообщения
                return TerminalResult.CreateError(GetNetworkErrorMessage(ex));
            }
            catch (Exception ex)
            {
                return TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Неожиданная ошибка"));
            }
        }

        /// <summary>
        /// Определяет, стоит ли повторять запрос при данной ошибке.
        /// Логика настроена по документации РНКБ.
        /// </summary>
        private bool IsRetryableError(TerminalResult result)
        {
            if (result.IsSuccess) return false;

            string error = result.ErrorMessage?.ToLower() ?? "";
            string code = result.CodeResponse ?? "";

            // 1. Коды, при которых точно НЕЛЬЗЯ повторять автоматически
            // 16 - Отказано (банк отклонил)
            // 53 - Прервана (действие пользователя)
            // 2 - Частичное одобрение (требует внимания кассира)
            if (code == "16" || code == "53" || code == "2")
                return false;

            // 2. Коды, при которых НУЖНО повторять (сетевые/временные проблемы)
            // 34 - Нет соединения
            // 0 - Неопределенный статус (может быть временный сбой)
            if (code == "34" || code == "0")
                return true;

            // 3. Анализ текста ошибки (если код пустой или нестандартный)
            // Сетевые ошибки - повторяем
            var retryablePhrases = new[] {
                "нет соединения", "timeout", "не отвечает", "connection refused",
                "сетевое подключение", "временно недоступен", "превышено время",
                "нет соединен" // частичный вариант
            };
                    foreach (var p in retryablePhrases)
                        if (error.Contains(p)) return true;

                    // Ошибки карты/банка (по тексту) - не повторяем
                    var nonRetryablePhrases = new[] {
                "отказано", "недостаточно средств", "неверный pin",
                "срок действия", "карта заблокирована", "операция прервана",
                "одобрена не на полную сумму"
            };
            foreach (var p in nonRetryablePhrases)
                if (error.Contains(p)) return false;

            // По умолчанию: если код ответа не "1" (успех), считаем, что это сетевой сбой и пробуем еще раз
            return !string.IsNullOrEmpty(code) && code != "1";
        }

        private void InvokeCommandCompleted(TerminalResult result)
        {
            if (_commandCompletedInvoked) return;
            _commandCompletedInvoked = true;
            if (commandResult != null) CommandCompleted?.Invoke(result.IsSuccess, commandResult.AnswerTerminal);
        }

        private void UpdateAttemptDisplay(int current, int max)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (StatusLabel != null) StatusLabel.Text = $"Подключение... (попытка {current}/{max})";
            });
        }

        #endregion

        #region Парсинг ответа (Универсальный)

        [XmlRoot(ElementName = "field")]
        public class Field
        {
            [XmlAttribute(AttributeName = "id")] public string Id { get; set; }
            [XmlText] public string Text { get; set; }
        }

        [XmlRoot(ElementName = "response")]
        public class Response
        {
            [XmlElement(ElementName = "field")] public List<Field> Field { get; set; }
        }

        public class CommandResult
        {
            public bool Status { get; set; }
            public Pay.AnswerTerminal AnswerTerminal { get; set; } = new Pay.AnswerTerminal();
        }

        public static TerminalResult ParseResponse(string xml)
        {
            var result = new TerminalResult();
            try
            {
                // ==========================================
                // ПАТЧ: Логируем сырой ответ от банка
                // ==========================================
                MainStaticClass.write_event_in_log(
                    $"Сырой ответ терминала: {(xml ?? "NULL")}",
                    "TerminalResponse",
                    "0"
                );
                // ==========================================

                if (string.IsNullOrWhiteSpace(xml) || xml.Trim().Length < 10)
                    return TerminalResult.CreateError("Терминал вернул пустой ответ.");

                var serializer = new XmlSerializer(typeof(Response));
                using var reader = new StringReader(xml);
                var response = (Response)serializer.Deserialize(reader);

                if (response?.Field == null)
                    return TerminalResult.CreateError("Некорректный формат ответа.");

                foreach (var field in response.Field)
                {
                    // Безопасное получение текста. Если Text == null, вернется null или пустая строка
                    string textValue = field.Text?.Trim();

                    switch (field.Id)
                    {
                        case "39":
                            result.CodeResponse = textValue;
                            // Успех только если код "1"
                            result.IsSuccess = (textValue == "1");
                            break;
                        case "13":
                            // Безопасное присвоение. Если поле пустое, будет null, а не краш.
                            //result.AuthorizationCode = field.Text?.Trim();
                            result.AuthorizationCode = field.Text?.Trim() ?? string.Empty; // Не null
                            break;
                        case "14":
                            result.ReferenceNumber = textValue;
                            break;
                        case "15":
                            result.CodeResponse15 = textValue;
                            break;
                        case "90":
                        result.RechargeNote = CleanRechargeNote(textValue);                                                 
                            break;
                    }
                }

                if (string.IsNullOrEmpty(result.CodeResponse))
                    return TerminalResult.CreateError("Терминал не вернул код результата (поле 39).");

                if (!result.IsSuccess)
                    result.ErrorMessage = GetTerminalErrorMessage(result.CodeResponse);
            }
            catch (Exception ex)
            {
                // Сюда вы должны попасть, если XML кривой
                return TerminalResult.CreateError($"Ошибка разбора XML: {ex.Message}");
            }
            return result;
        }        

        private void UpdateLocalCommandResult(TerminalResult result)
        {
            commandResult = new CommandResult
            {
                Status = result.IsSuccess,
                AnswerTerminal = new Pay.AnswerTerminal
                {
                    сode_response_in_39_field = result.CodeResponse,
                    сode_response_in_15_field = result.CodeResponse15,
                    code_authorization = result.AuthorizationCode,
                    number_reference = result.ReferenceNumber,
                    error = !result.IsSuccess
                }
            };
            if (cc != null && !string.IsNullOrEmpty(result.RechargeNote)) cc.recharge_note = result.RechargeNote;
        }

        private static string CleanRechargeNote(string note)
        {
            // Если null или пусто — возвращаем string.Empty (как в конструкторе TerminalResult)
            if (string.IsNullOrEmpty(note)) return string.Empty;

            int pos = note.IndexOf("(КАССИР)");
            // Если нашли "КАССИР" - обрезаем, если нет - возвращаем как есть (но не null!)
            return pos > 0 ? note.Substring(0, pos + 8) : note;
        }

        /// <summary>
        /// Возвращает понятное сообщение для кодов ошибок терминала (поле 39).
        /// Сообщения соответствуют документации РНКБ. В конце всегда добавляется код ответа.
        /// </summary>
        private static string GetTerminalErrorMessage(string code39)
        {
            string message = code39 switch
            {
                "0" or "00" => "Неопределенный статус. Транзакция не выполнена.\n\nПроверьте терминал и повторите попытку.",

                "1" => "Операция одобрена.", // Обычно не выводится как ошибка

                "2" => "Внимание! Операция «Оплата» одобрена НЕ на полную сумму.\n\n" +
                       "При использовании СБП сверка итогов успешна только на хосте банка.\n" +
                       "Проверьте сумму на терминале.",

                "16" => "Отказано.\n\nТранзакция проведена, но ее одобрение не получено.\n" +
                        "Возможные причины:\n" +
                        "• Недостаточно средств\n" +
                        "• Неверный PIN-код\n" +
                        "• Карта заблокирована\n" +
                        "• Операция запрещена банком",

                "34" => "Нет соединения.\n\nПроверьте:\n" +
                        "• Сетевой кабель подключен\n" +
                        "• Связь с банком стабильна\n" +
                        "• Термопринтер закрыт",

                "53" => "Операция прервана.\n\nВозможно, отменено пользователем или сбой на терминале.",

                _ => $"Операция отклонена (неизвестный код).\n\nОбратитесь в техподдержку."
            };

            // ✅ ВСЕГДА добавляем код ответа в конце для отладки
            return $"{message}\n\nКод ответа: {code39}";
        }

        #endregion

        #region Вспомогательные сообщения (Static)

        private static string GetHttpErrorMessage(HttpStatusCode statusCode, string reasonPhrase)
        {
            return statusCode switch
            {
                HttpStatusCode.NotFound => "Терминал не найден в сети.\n\nПроверьте:\n• IP-адрес терминала в настройках\n• Терминал подключен к той же сети",
                HttpStatusCode.BadRequest => "Терминал не понял запрос.\n\nВозможно:\n• Неверный формат данных\n• Терминал требует обновления ПО",
                HttpStatusCode.InternalServerError => "Ошибка на стороне терминала.\n\nПопробуйте:\n• Перезагрузить терминал\n• Проверить наличие бумаги",
                _ => $"Ошибка связи с терминалом ({(int)statusCode} {reasonPhrase}).\n\nПроверьте сетевое подключение."
            };
        }

        private static string GetNetworkErrorMessage(HttpRequestException ex)
        {
            string message = ex.Message?.ToLower() ?? "";
            if (message.Contains("connection refused") || message.Contains("не удалось подключиться"))
                return "Не удалось подключиться к терминалу.\n\nПроверьте:\n• Терминал включен\n• IP-адрес указан верно\n• Сетевой кабель подключен";

            if (message.Contains("timeout") || message.Contains("превышено время"))
                return "Терминал не отвечает.\n\nВозможные причины:\n• Терминал завис\n• Сетевое соединение нестабильно";

            return $"Ошибка сетевого подключения.\n\nДетали: {ex.Message}";
        }

        private static string GetUserFriendlyMessage(Exception ex, string defaultPrefix)
        {
            string message = ex.Message?.ToLower() ?? "";
            if (message.Contains("connection") || message.Contains("connect"))
                return $"{defaultPrefix}\n\nНе удалось подключиться к терминалу.\nПроверьте сетевой кабель.";

            return $"{defaultPrefix}\n\n{ex.Message}";
        }

        #endregion

        #region UI и Закрытие

        private void UpdateTimerDisplay()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ProgressBarNonCashPay != null) ProgressBarNonCashPay.Value = _secondsRemaining;
                if (LabelTimer != null) LabelTimer.Text = "Осталось " + _secondsRemaining + " сек.";
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (CancelButton != null) { CancelButton.IsEnabled = false; CancelButton.Content = "Отмена..."; }
            _cts?.Cancel();
            CloseWithResult(TerminalResult.CreateError("Операция отменена пользователем."));
        }

        private void CloseWithResult(TerminalResult result)
        {
            if (_isClosed) return;
            _isClosed = true;

            UpdateLocalCommandResult(result);

            _tcs.TrySetResult(result);
            PaymentCompleted?.Invoke(this, result.IsSuccess);

            Dispatcher.UIThread.Post(() =>
            {
                this.Tag = result.IsSuccess;
                this.Close();
                _windowClosedTcs.TrySetResult(true);
            });
        }

        public async Task WaitForWindowCloseAsync() => await _windowClosedTcs.Task;

        #endregion

        #region Публичный API

        public static async Task<TerminalResult> ShowAndWaitAsync(Window owner, int timeoutSeconds, string url, string data, Cash_check cashCheck)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var dialog = new WaitNonCashPay(timeoutSeconds) { Url = url, Data = data, cc = cashCheck };
            await dialog.ShowDialog(owner);
            return await dialog._tcs.Task;
        }

        #endregion
    }
}