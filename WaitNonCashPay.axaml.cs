using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string CodeResponse { get; set; }
        public string CodeResponse15 { get; set; }
        public string AuthorizationCode { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string RechargeNote { get; set; } = string.Empty;
        public string ErrorMessage { get; set; }

        public int AttemptsCount { get; set; } = 1;
        public List<string> AttemptErrors { get; set; } = new List<string>();

        // ★ ДОБАВЛЕНО: Храним оригинальное исключение для JSON-логирования
        public Exception Exception { get; set; }

        public static TerminalResult CreateError(string message) => new TerminalResult { IsSuccess = false, ErrorMessage = message };
    }

    /// <summary>
    /// Окно ожидания ответа от эквайрингового терминала
    /// </summary>
    public partial class WaitNonCashPay : Window
    {
        // Настройки повторных попыток
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 3000;

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

        // private async Task<TerminalResult> SendCommandAsync()
        // {
        //     try
        //     {
        //         TerminalResult result;
        //
        //         // Если передана кастомная операция (Сбер), выполняем её
        //         if (CustomOperation != null)
        //         {
        //             // Обновляем статус
        //             Dispatcher.UIThread.Post(() => { if (StatusLabel != null) StatusLabel.Text = "Выполнение команды на терминале..."; });
        //
        //             result = await CustomOperation(_cts.Token).ConfigureAwait(false);
        //             
        //             // ★ ДОБАВИТЬ: Логирование результата кастомной операции (Сбер) ★
        //             MainStaticClass.write_event_in_log(
        //                 $"[Сбер] Результат кастомной операции: IsSuccess={result.IsSuccess}, " +
        //                 $"Code39='{result.CodeResponse}', Code15='{result.CodeResponse15}', " +
        //                 $"AuthCode='{result.AuthorizationCode}', RefNum='{result.ReferenceNumber}', " +
        //                 $"Error='{result.ErrorMessage}'",
        //                 "TerminalResponse",
        //                 cc?.numdoc.ToString() ?? "0"
        //             );
        //             
        //         }
        //         else
        //         {
        //             // Иначе стандартная логика РНКБ (HTTP с ретраями)
        //             result = await SendRequestWithRetryAsync(_cts.Token).ConfigureAwait(false);
        //         }
        //
        //         if (!_isClosed)
        //         {
        //             await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
        //             await WaitForWindowCloseAsync();
        //         }
        //         return result;
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         var result = TerminalResult.CreateError("Операция отменена");
        //         if (!_isClosed) { await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result)); await WaitForWindowCloseAsync(); }
        //         return result;
        //     }
        //     catch (Exception ex)
        //     {
        //         var result = TerminalResult.CreateError($"Ошибка: {ex.Message}");
        //         if (!_isClosed) { await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result)); await WaitForWindowCloseAsync(); }
        //         return result;
        //     }
        // }

        private async Task<TerminalResult> SendCommandAsync()
        {
            try
            {
                TerminalResult result;

                // Если передана кастомная операция (Сбер), выполняем её
                if (CustomOperation != null)
                {
                    // Обновляем статус
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (StatusLabel != null) StatusLabel.Text = "Выполнение команды на терминале...";
                    });

                    result = await CustomOperation(_cts.Token).ConfigureAwait(false);

                    // ★ ДОБАВЛЕНО РАНЕЕ: Логирование результата в текстовый лог ★
                    MainStaticClass.write_event_in_log(
                        $"[Сбер] Результат кастомной операции: IsSuccess={result.IsSuccess}, " +
                        $"Code39='{result.CodeResponse}', Code15='{result.CodeResponse15}', " +
                        $"AuthCode='{result.AuthorizationCode}', RefNum='{result.ReferenceNumber}', " +
                        $"Error='{result.ErrorMessage}'",
                        "TerminalResponse",
                        cc?.numdoc.ToString() ?? "0"
                    );

                    // ★ НОВОЕ: Логирование ошибки в БД (Error Log) для Сбера ★
                    if (!result.IsSuccess)
                    {
                        LogTerminalErrorToDb(result, "Sber_CustomError");
                    }
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
                // ★ НОВОЕ: Логируем отмену от Сбера в БД ★
                LogTerminalErrorToDb(result, "Sber_OperationCanceled");

                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                    await WaitForWindowCloseAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                var result = TerminalResult.CreateError($"Ошибка: {ex.Message}");

                // ★ НОВОЕ: Сохраняем исключение и логируем критический сбой Сбера в БД ★
                result.Exception = ex; // Чтобы в БД записался полный StackTrace
                LogTerminalErrorToDb(result, "Sber_FatalException");

                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                    await WaitForWindowCloseAsync();
                }

                return result;
            }
        }

        #endregion

        #region HTTP запрос (Retry Logic)


        //private async Task<TerminalResult> SendRequestWithRetryAsync(CancellationToken cancellationToken)
        //{
        //    TerminalResult lastResult = null;
        //    var attemptErrors = new List<string>();

        //    int singleRequestTimeout = Math.Max(20, (_totalSeconds - (MaxRetryAttempts - 1) * (RetryDelayMs / 1000)) / MaxRetryAttempts);

        //    using var overallTimeoutCts = new CancellationTokenSource(_totalSeconds * 1000);
        //    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, overallTimeoutCts.Token);

        //    for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        //    {
        //        if (linkedCts.Token.IsCancellationRequested || _isClosed)
        //        {
        //            var cancelResult = TerminalResult.CreateError("Операция отменена");
        //            LogTerminalErrorToDb(cancelResult, "Retry_Cancel");
        //            return cancelResult;
        //        }

        //        _currentAttempt = attempt;
        //        UpdateAttemptDisplay(attempt, MaxRetryAttempts);

        //        MainStaticClass.write_event_in_log($"Попытка {attempt}/{MaxRetryAttempts} отправки на {Url}", "Terminal", cc?.numdoc.ToString() ?? "0");

        //        try
        //        {
        //            // ★ РАСКОММЕНТИРОВАНА И ИСПРАВЛЕНА ЛОГИКА ДИНАМИЧЕСКОГО ТАЙМАУТА ★
        //            int currentRequestTimeout = singleRequestTimeout;

        //            // Если это не первая попытка И предыдущая ошибка была "мгновенной" (сеть мертва) — 
        //            // жестко ставим 5 секунд на ВЕСЬ запрос. Нет смысла ждать 20 сек, если хост выключен.
        //            if (attempt > 1 && lastResult != null && IsImmediateNetworkError(lastResult.ErrorMessage))
        //            {
        //                currentRequestTimeout = 5;
        //                MainStaticClass.write_event_in_log(
        //                    $"[Retry] Предыдущая ошибка мгновенная, сокращаю общий таймаут до {currentRequestTimeout} сек",
        //                    "Terminal", cc?.numdoc.ToString() ?? "0");
        //            }

        //            lastResult = await SendRequestAsync(Url, Data, currentRequestTimeout).ConfigureAwait(false);
        //            lastResult.AttemptErrors = attemptErrors;
        //            lastResult.AttemptsCount = attempt;

        //            if (lastResult.IsSuccess)
        //            {
        //                InvokeCommandCompleted(lastResult);
        //                return lastResult;
        //            }

        //            attemptErrors.Add($"Попытка {attempt}: {lastResult.ErrorMessage}");

        //            if (!IsRetryableError(lastResult))
        //            {
        //                LogTerminalErrorToDb(lastResult, "Retry_BusinessError");
        //                InvokeCommandCompleted(lastResult);
        //                return lastResult;
        //            }

        //            if (attempt < MaxRetryAttempts)
        //            {
        //                int delayMs = RetryDelayMs; // Фиксированная пауза 3 секунды
        //                int delaySec = delayMs / 1000;

        //                string shortError = lastResult.ErrorMessage?.Split('\n').FirstOrDefault() ?? "Ошибка связи";
        //                if (!string.IsNullOrEmpty(shortError) && shortError.Length > 60)
        //                    shortError = shortError.Substring(0, 57) + "...";

        //                Dispatcher.UIThread.Post(() =>
        //                {
        //                    if (StatusLabel != null)
        //                    {
        //                        StatusLabel.Text = $"Попытка {attempt} из {MaxRetryAttempts}: {shortError}\nПовтор через {delaySec} сек...";
        //                        StatusLabel.Foreground = Brushes.Orange;
        //                    }
        //                });

        //                try { await Task.Delay(delayMs, linkedCts.Token).ConfigureAwait(false); }
        //                catch (OperationCanceledException)
        //                {
        //                    var delayCancelResult = TerminalResult.CreateError("Превышено время ожидания при задержке");
        //                    LogTerminalErrorToDb(delayCancelResult, "Retry_DelayCancel");
        //                    return delayCancelResult;
        //                }
        //            }
        //        }
        //        catch (OperationCanceledException) when (overallTimeoutCts.Token.IsCancellationRequested)
        //        {
        //            var overallTimeoutResult = TerminalResult.CreateError($"Превышено общее время ожидания ({_totalSeconds} сек).");
        //            LogTerminalErrorToDb(overallTimeoutResult, "Retry_OverallTimeout");
        //            return overallTimeoutResult;
        //        }
        //        catch (OperationCanceledException) { throw; }
        //        catch (Exception ex)
        //        {
        //            attemptErrors.Add($"Попытка {attempt}: {ex.Message}");
        //            lastResult = TerminalResult.CreateError(ex.Message);
        //            lastResult.Exception = ex;

        //            if (attempt < MaxRetryAttempts)
        //            {
        //                int delayMs = RetryDelayMs; // ★ ИСПРАВЛЕНО: Фиксированная пауза 3 секунды (было RetryDelayMs * attempt)
        //                int delaySec = delayMs / 1000;

        //                string shortError = ex.Message?.Split('\n').FirstOrDefault() ?? "Критическая ошибка";
        //                if (!string.IsNullOrEmpty(shortError) && shortError.Length > 60)
        //                    shortError = shortError.Substring(0, 57) + "...";

        //                Dispatcher.UIThread.Post(() =>
        //                {
        //                    if (StatusLabel != null)
        //                    {
        //                        StatusLabel.Text = $"Попытка {attempt} из {MaxRetryAttempts}: {shortError}\nПовтор через {delaySec} сек...";
        //                        StatusLabel.Foreground = Brushes.Orange;
        //                    }
        //                });

        //                try { await Task.Delay(delayMs, linkedCts.Token).ConfigureAwait(false); }
        //                catch (OperationCanceledException)
        //                {
        //                    var exceptionDelayResult = TerminalResult.CreateError("Превышено время ожидания");
        //                    LogTerminalErrorToDb(exceptionDelayResult, "Retry_ExceptionDelayCancel");
        //                    return exceptionDelayResult;
        //                }
        //            }
        //        }
        //    }

        //    if (lastResult != null)
        //    {
        //        LogTerminalErrorToDb(lastResult, "Retry_AllRetriesFailed");

        //        InvokeCommandCompleted(lastResult);

        //        if (attemptErrors.Count > 1)
        //        {
        //            var errorSummary = string.Join("\n", attemptErrors.Select((e, i) =>
        //            {
        //                string msg = e.Split('\n').FirstOrDefault() ?? e;
        //                if (msg.Length > 80) msg = msg.Substring(0, 77) + "...";
        //                return $"[{i + 1}] {msg}";
        //            }));

        //            lastResult.ErrorMessage = $"❌ Не удалось выполнить операцию после {MaxRetryAttempts} попыток.\n\n" +
        //                                      $"Детали:\n{errorSummary}\n\n" +
        //                                      $"Последняя ошибка:\n{lastResult.ErrorMessage}";
        //        }
        //    }
        //    else
        //    {
        //        lastResult = TerminalResult.CreateError("Все попытки исчерпаны");
        //        LogTerminalErrorToDb(lastResult, "Retry_UnknownFail");
        //    }

        //    return lastResult;
        //}

        private async Task<TerminalResult> SendRequestWithRetryAsync(CancellationToken cancellationToken)
        {
            TerminalResult lastResult = null;
            var attemptErrors = new List<string>();

            int singleRequestTimeout = Math.Max(20, (_totalSeconds - (MaxRetryAttempts - 1) * (RetryDelayMs / 1000)) / MaxRetryAttempts);

            using var overallTimeoutCts = new CancellationTokenSource(_totalSeconds * 1000);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, overallTimeoutCts.Token);

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                if (linkedCts.Token.IsCancellationRequested || _isClosed)
                {
                    var cancelResult = TerminalResult.CreateError("Операция отменена");
                    LogTerminalErrorToDb(cancelResult, "Retry_Cancel");
                    return cancelResult;
                }

                _currentAttempt = attempt;
                UpdateAttemptDisplay(attempt, MaxRetryAttempts);

                MainStaticClass.write_event_in_log($"Попытка {attempt}/{MaxRetryAttempts} отправки на {Url}", "Terminal", cc?.numdoc.ToString() ?? "0");

                try
                {
                    // ★ ИСПРАВЛЕНО: Убрана логика сокращения таймаута до 5 секунд. ★
                    // SocketsHttpHandler.ConnectTimeout (3 сек) уже защищает от долгого ожидания мертвого хоста.
                    // Урезание client.Timeout до 5 сек лишало терминал времени на связь с банком (особенно при коде 34).
                    // Теперь каждый запрос (включая повторные попытки) получает полный таймаут (20+ секунд).
                    int currentRequestTimeout = singleRequestTimeout;

                    lastResult = await SendRequestAsync(Url, Data, currentRequestTimeout).ConfigureAwait(false);
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
                        LogTerminalErrorToDb(lastResult, "Retry_BusinessError");
                        InvokeCommandCompleted(lastResult);
                        return lastResult;
                    }

                    if (attempt < MaxRetryAttempts)
                    {
                        int delayMs = RetryDelayMs; // Фиксированная пауза 3 секунды
                        int delaySec = delayMs / 1000;

                        string shortError = lastResult.ErrorMessage?.Split('\n').FirstOrDefault() ?? "Ошибка связи";
                        if (!string.IsNullOrEmpty(shortError) && shortError.Length > 60)
                            shortError = shortError.Substring(0, 57) + "...";

                        Dispatcher.UIThread.Post(() =>
                        {
                            if (StatusLabel != null)
                            {
                                StatusLabel.Text = $"Попытка {attempt} из {MaxRetryAttempts}: {shortError}\nПовтор через {delaySec} сек...";
                                StatusLabel.Foreground = Brushes.Orange;
                            }
                        });

                        try { await Task.Delay(delayMs, linkedCts.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException)
                        {
                            var delayCancelResult = TerminalResult.CreateError("Превышено время ожидания при задержке");
                            LogTerminalErrorToDb(delayCancelResult, "Retry_DelayCancel");
                            return delayCancelResult;
                        }
                    }
                }
                catch (OperationCanceledException) when (overallTimeoutCts.Token.IsCancellationRequested)
                {
                    var overallTimeoutResult = TerminalResult.CreateError($"Превышено общее время ожидания ({_totalSeconds} сек).");
                    LogTerminalErrorToDb(overallTimeoutResult, "Retry_OverallTimeout");
                    return overallTimeoutResult;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    attemptErrors.Add($"Попытка {attempt}: {ex.Message}");
                    lastResult = TerminalResult.CreateError(ex.Message);
                    lastResult.Exception = ex;

                    if (attempt < MaxRetryAttempts)
                    {
                        int delayMs = RetryDelayMs;
                        int delaySec = delayMs / 1000;

                        string shortError = ex.Message?.Split('\n').FirstOrDefault() ?? "Критическая ошибка";
                        if (!string.IsNullOrEmpty(shortError) && shortError.Length > 60)
                            shortError = shortError.Substring(0, 57) + "...";

                        Dispatcher.UIThread.Post(() =>
                        {
                            if (StatusLabel != null)
                            {
                                StatusLabel.Text = $"Попытка {attempt} из {MaxRetryAttempts}: {shortError}\nПовтор через {delaySec} сек...";
                                StatusLabel.Foreground = Brushes.Orange;
                            }
                        });

                        try { await Task.Delay(delayMs, linkedCts.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException)
                        {
                            var exceptionDelayResult = TerminalResult.CreateError("Превышено время ожидания");
                            LogTerminalErrorToDb(exceptionDelayResult, "Retry_ExceptionDelayCancel");
                            return exceptionDelayResult;
                        }
                    }
                }
            }

            if (lastResult != null)
            {
                LogTerminalErrorToDb(lastResult, "Retry_AllRetriesFailed");

                InvokeCommandCompleted(lastResult);

                if (attemptErrors.Count > 1)
                {
                    var errorSummary = string.Join("\n", attemptErrors.Select((e, i) =>
                    {
                        string msg = e.Split('\n').FirstOrDefault() ?? e;
                        if (msg.Length > 80) msg = msg.Substring(0, 77) + "...";
                        return $"[{i + 1}] {msg}";
                    }));

                    lastResult.ErrorMessage = $"❌ Не удалось выполнить операцию после {MaxRetryAttempts} попыток.\n\n" +
                                              $"Детали:\n{errorSummary}\n\n" +
                                              $"Последняя ошибка:\n{lastResult.ErrorMessage}";
                }
            }
            else
            {
                lastResult = TerminalResult.CreateError("Все попытки исчерпаны");
                LogTerminalErrorToDb(lastResult, "Retry_UnknownFail");
            }

            return lastResult;
        }

        /// <summary>
        /// Протоколирует ошибку терминала в БД с максимальной детализацией для диагностики
        /// </summary>
        private void LogTerminalErrorToDb(TerminalResult result, string contextMethod)
        {
            if (result == null || result.IsSuccess) return;

            try
            {
                // ВНИМАНИЕ: Замените MainStaticClass.CashDeskNumber на ваше реальное свойство!
                short cashDesk = MainStaticClass.CashDeskNumber;

                long numDoc = 0;
                if (cc != null)
                {
                    numDoc = cc.numdoc;
                }

                // ★ Формируем описание, в которое упаковываем ВСЮ диагностическую информацию ★
                // Включаем имя метода, коды ответа терминала и количество попыток.
                string description = $"[{contextMethod}] Code39: '{result.CodeResponse ?? "null"}', Code15: '{result.CodeResponse15 ?? "null"}', Attempts: {result.AttemptsCount}";

                // Обрезаем, чтобы влезло в 255 символов в БД
                if (description.Length > 255) description = description.Substring(0, 252) + "...";

                if (result.Exception != null)
                {
                    // Это технический сбой (сеть, таймаут сокета). 
                    // Вызываем ваш оригинальный метод с Exception (без изменения его сигнатуры).
                    // JSON-сериализатор внутри него упакует StackTrace и InnerException.
                    MainStaticClass.WriteRecordErrorLog(result.Exception, numDoc, cashDesk, description);
                }
                else
                {
                    // Это бизнес-ошибка (терминал ответил, но отказал).
                    // Формируем строку для первого метода логирования
                    string errorMessage = result.ErrorMessage?.Split('\n').FirstOrDefault() ?? "Неизвестная ошибка терминала";

                    // Приклеиваем код ответа к сообщению, чтобы оно 100% попало в error_message
                    errorMessage += $" [Code39: {result.CodeResponse ?? "null"}]";

                    if (errorMessage.Length > 255) errorMessage = errorMessage.Substring(0, 252) + "...";

                    // Вызываем ваш оригинальный строковый метод логирования
                    MainStaticClass.WriteRecordErrorLog(errorMessage, contextMethod, numDoc, cashDesk, description);
                }
            }
            catch (Exception ex)
            {
                // Если сама БД недоступна, пишем хотя бы в текстовый лог
                MainStaticClass.write_event_in_log($"[LogTerminalErrorToDb] Ошибка записи в БД: {ex.Message}", "TerminalLog", cc?.numdoc.ToString() ?? "0");
            }
        }

        /// <summary>
        /// Отправляет POST-запрос к терминалу и парсит XML-ответ.
        /// Не показывает окно, не управляет таймером — только сетевая логика.
        /// </summary>
        //public static async Task<TerminalResult> SendRequestAsync(string url, string data, int timeoutSeconds = 80)
        //{
        //    try
        //    {
        //        // ★ НОВОЕ: Ограничиваем таймаут ПОДКЛЮЧЕНИЯ 5 секундами.
        //        // Если терминал выключен или IP неверный, мы получим отлуп за 5 секунд, 
        //        // а не будем висеть 20-30 секунд, ожидая ответа от маршрутизатора.
        //        var handler = new SocketsHttpHandler
        //        {
        //            ConnectTimeout = TimeSpan.FromSeconds(5)
        //        };

        //        using var client = new HttpClient(handler);
        //        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds); // Общий таймаут на весь запрос

        //        var content = new StringContent(data, Encoding.GetEncoding("Windows-1251"), "text/xml");
        //        var response = await client.PostAsync(url, content).ConfigureAwait(false);
        //        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return TerminalResult.CreateError(GetHttpErrorMessage(response.StatusCode, response.ReasonPhrase));
        //        }

        //        return ParseResponse(responseContent);
        //    }
        //    catch (TaskCanceledException ex)
        //    {
        //        var res = TerminalResult.CreateError("Терминал не ответил вовремя.\n\nПроверьте:\n• Терминал включен\n• Сетевой кабель подключен");
        //        res.Exception = ex;
        //        return res;
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        var res = TerminalResult.CreateError(GetNetworkErrorMessage(ex));
        //        res.Exception = ex;
        //        return res;
        //    }
        //    catch (Exception ex)
        //    {
        //        var res = TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Неожиданная ошибка"));
        //        res.Exception = ex;
        //        return res;
        //    }
        //}

        /// <summary>
        /// Отправляет POST-запрос к терминалу и парсит XML-ответ.
        /// Не показывает окно, не управляет таймером — только сетевая логика.
        /// </summary>
        /// <summary>
        /// Отправляет POST-запрос к терминалу и парсит XML-ответ.
        /// </summary>
        public static async Task<TerminalResult> SendRequestAsync(string url, string data, int timeoutSeconds = 80)
        {
            try
            {
                // ★ НОВОЕ: Полный контроль над таймаутами через SocketsHttpHandler ★
                var handler = new SocketsHttpHandler
                {
                    // Таймаут на установку TCP-соединения. Если терминал выключен, отвалится за 3 секунды.
                    ConnectTimeout = TimeSpan.FromSeconds(3),

                    // Отключаем пулинг для терминалов — каждый запрос "с нуля"
                    PooledConnectionLifetime = TimeSpan.Zero,
                };

                using var client = new HttpClient(handler);

                // Общий таймаут на ВСЁ (DNS + connect + send + wait for PIN + read)
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                var content = new StringContent(data, Encoding.GetEncoding("Windows-1251"), "text/xml");
                var response = await client.PostAsync(url, content).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return TerminalResult.CreateError(GetHttpErrorMessage(response.StatusCode, response.ReasonPhrase));
                }

                return ParseResponse(responseContent);
            }
            // ★ УЛУЧШЕННАЯ ЛОВУШКА ТАЙМАУТОВ ★
            // SocketsHttpHandler при срабатывании ConnectTimeout выбрасывает именно TaskCanceledException 
            // с InnerException типа TimeoutException.
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                // Это точно таймаут подключения (терминал не найден в сети за 3 секунды)
                var res = TerminalResult.CreateError("Терминал не найден в сети.\n\nПроверьте:\n• Терминал включен\n• Сетевой кабель подключен\n• IP-адрес указан верно");
                res.Exception = ex;
                return res;
            }
            catch (TaskCanceledException ex)
            {
                // Это общий таймаут (client.Timeout) или отмена через CancellationToken
                var res = TerminalResult.CreateError("Терминал не ответил вовремя (завис или долго обрабатывает).\n\nПроверьте терминал.");
                res.Exception = ex;
                return res;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused") || ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.ConnectionRefused })
            {
                // Мгновенный отлуп — порт закрыт (терминал работает, но сервис банка не запущен)
                var res = TerminalResult.CreateError("Не удалось подключиться к терминалу (соединение отклонено).\n\nПроверьте:\n• Терминал включен\n• Перезагрузите терминал");
                res.Exception = ex;
                return res;
            }
            catch (HttpRequestException ex)
            {
                // Другие сетевые ошибки (DNS не найден и т.д.)
                var res = TerminalResult.CreateError(GetNetworkErrorMessage(ex));
                res.Exception = ex;
                return res;
            }
            catch (Exception ex)
            {
                var res = TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Неожиданная ошибка"));
                res.Exception = ex;
                return res;
            }
        }

        ///// <summary>
        ///// Определяет, является ли ошибка сетевой и «мгновенной» (хост не доступен, соединение отклонено и т.п.)
        ///// Такие ошибки не требуют долгого ожидания ответа — можно сразу пробовать снова с коротким таймаутом.
        ///// </summary>
        //private static bool IsImmediateNetworkError(string errorMessage)
        //{
        //    if (string.IsNullOrEmpty(errorMessage)) return false;

        //    var error = errorMessage.ToLower();

        //    var immediatePhrases = new[] {
        //        "connection refused", "не удалось подключиться", "нет соединения",
        //        "не найден", "not found", "404", "name resolution failure",
        //        "no such host", "host unreachable", "network unreachable",
        //        "socketexception", "connectionreset", "forcibly closed"
        //        // "терминал не отвечает" - УБРАНО! Если терминал найден, но тупит, даем ему 20 сек
        //    };

        //    return immediatePhrases.Any(p => error.Contains(p));
        //}

        ///// <summary>
        ///// Определяет, стоит ли повторять запрос при данной ошибке.
        ///// Логика настроена по документации РНКБ.
        ///// </summary>
        //private bool IsRetryableError(TerminalResult result)
        //{
        //    if (result.IsSuccess) return false;

        //    string error = result.ErrorMessage?.ToLower() ?? "";
        //    string code = result.CodeResponse ?? "";

        //    // 1. Коды, при которых точно НЕЛЬЗЯ повторять автоматически
        //    // 16 - Отказано (банк отклонил)
        //    // 53 - Прервана (действие пользователя)
        //    // 2 - Частичное одобрение (требует внимания кассира)
        //    if (code == "16" || code == "53" || code == "2")
        //        return false;

        //    // 2. Коды, при которых НУЖНО повторять (сетевые/временные проблемы)
        //    // 34 - Нет соединения
        //    // 0 - Неопределенный статус (может быть временный сбой)
        //    if (code == "34" || code == "0")
        //        return true;

        //    // 3. Анализ текста ошибки (если код пустой или нестандартный)
        //    // Сетевые ошибки - повторяем
        //    var retryablePhrases = new[] {
        //        "нет соединения", "timeout", "не отвечает", "connection refused",
        //        "сетевое подключение", "временно недоступен", "превышено время",
        //        "нет соединен" // частичный вариант
        //    };
        //            foreach (var p in retryablePhrases)
        //                if (error.Contains(p)) return true;

        //            // Ошибки карты/банка (по тексту) - не повторяем
        //            var nonRetryablePhrases = new[] {
        //        "отказано", "недостаточно средств", "неверный pin",
        //        "срок действия", "карта заблокирована", "операция прервана",
        //        "одобрена не на полную сумму"
        //    };
        //    foreach (var p in nonRetryablePhrases)
        //        if (error.Contains(p)) return false;

        //    // По умолчанию: если код ответа не "1" (успех), считаем, что это сетевой сбой и пробуем еще раз
        //    return !string.IsNullOrEmpty(code) && code != "1";
        //}

        /// <summary>
        /// Определяет, стоит ли повторять запрос при данной ошибке.
        /// </summary>
        private bool IsRetryableError(TerminalResult result)
        {
            if (result.IsSuccess) return false;

            string error = (result.ErrorMessage ?? "").ToLower();
            string code = result.CodeResponse ?? "";

            MainStaticClass.write_event_in_log(
                $"[RetryCheck] Code: '{code}', Error: '{result.ErrorMessage}'",
                "Terminal", cc?.numdoc.ToString() ?? "0");

            // 1. Коды, при которых точно НЕЛЬЗЯ повторять (бизнес-ошибки)
            if (code == "16" || code == "53")
            {
                MainStaticClass.write_event_in_log("[RetryCheck] Non-retryable code", "Terminal", "0");
                return false;
            }

            // 2. Коды, при которых НУЖНО повторять (сетевые/временные проблемы)
            if (code == "34" || code == "0" || string.IsNullOrEmpty(code))
            {
                MainStaticClass.write_event_in_log("[RetryCheck] Retryable code or empty", "Terminal", "0");
                return true;
            }

            // 3. Анализ текста ошибки
            var retryablePhrases = new[] {
        "нет соединения", "timeout", "не отвечает", "connection refused",
        "сетевое подключение", "временно недоступен", "превышено время",
        "нет соединен", "не найден", "name resolution failure",
        "no such host", "http 404", "http 502", "http 503", "http 504"
    };

            foreach (var p in retryablePhrases)
            {
                if (error.Contains(p))
                {
                    MainStaticClass.write_event_in_log($"[RetryCheck] Retryable phrase found: '{p}'", "Terminal", "0");
                    return true;
                }
            }

            var nonRetryablePhrases = new[] {
        "отказано", "недостаточно средств", "неверный pin",
        "срок действия", "карта заблокирована", "операция прервана",
        "одобрена не на полную сумму"
    };

            foreach (var p in nonRetryablePhrases)
            {
                if (error.Contains(p))
                {
                    MainStaticClass.write_event_in_log($"[RetryCheck] Non-retryable phrase found: '{p}'", "Terminal", "0");
                    return false;
                }
            }

            // 4. ИСПРАВЛЕННЫЙ Default: если код пустой ИЛИ это не явный успех "1" — пробуем еще раз
            // Это ключевое изменение для обработки сетевых ошибок
            MainStaticClass.write_event_in_log($"[RetryCheck] Default fallback -> Retry", "Terminal", "0");
            return string.IsNullOrEmpty(code) || code != "1";
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

                //"2" => "Внимание! Операция «Оплата» одобрена НЕ на полную сумму.\n\n" +
                //       "При использовании СБП сверка итогов успешна только на хосте банка.\n" +
                //       "Проверьте сумму на терминале.",

                "16" => "Отказано.\n\n" +
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

        // private void CloseWithResult(TerminalResult result)
        // {
        //     if (_isClosed) return;
        //     _isClosed = true;
        //
        //     UpdateLocalCommandResult(result);
        //
        //     _tcs.TrySetResult(result);
        //     PaymentCompleted?.Invoke(this, result.IsSuccess);
        //
        //     Dispatcher.UIThread.Post(() =>
        //     {
        //         this.Tag = result.IsSuccess;
        //         this.Close();
        //         _windowClosedTcs.TrySetResult(true);
        //     });
        // }

        private void CloseWithResult(TerminalResult result)
        {
            if (_isClosed) return;
            _isClosed = true;

            // ★ НОВОЕ: Разблокировка чека при БИЗНЕС-ошибках терминала ★
            if (cc != null && !result.IsSuccess)
            {
                string code = result.CodeResponse ?? "";
                string errorText = (result.ErrorMessage ?? "").ToLower(); // Добавили чтение текста

                bool isRefused = false; // Общий флаг отказа

                // 1. Проверяем по коду (Это для ВТБ)
                if (code == "16" || // Отклонено
                    code == "53" || // Операция прервана
                    code == "55" || // Неверный ПИН-код
                    code == "61" || // Превышен лимит
                    code == "54" || // Срок действия карты истек
                    code == "58" || // Терминал не поддерживает операцию
                    code == "96" || // Системная ошибка банка
                    // На будущее, если добавите конвертацию кодов Сбера:
                    code == "4451" || code == "4452" || code == "2000" || code == "7400")
                {
                    isRefused = true;
                }
                // 2. Фоллбэк по тексту (Это спасет Сбера прямо сейчас!)
                else if (string.IsNullOrEmpty(code) &&
                         (errorText.Contains("отклонено") ||
                          errorText.Contains("недостаточно средств") ||
                          errorText.Contains("операция не прошла") ||
                          errorText.Contains("заблокирована") ||
                          errorText.Contains("пинпад не подключен") ||
                          errorText.Contains("отказано")))
                {
                    isRefused = true;
                }

                // Если любой из проверок сработал — разблокируем чек
                if (isRefused)
                {
                    cc.PaymentAttempted = false;

                    MainStaticClass.write_event_in_log(
                        $"[Terminal] Получен четкий отказ (Code:{code}), чек разблокирован для изменений",
                        "Terminal", cc.numdoc.ToString() ?? "0");
                }
            }

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