//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Interactivity;
//using Avalonia.Media;
//using Avalonia.Threading;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Xml.Serialization;

//namespace Cash8Avalon
//{
//    public partial class WaitNonCashPay : Window
//    {
//        private CancellationTokenSource _cts;
//        private int _secondsRemaining = 80;
//        private bool _isSuccessful = false;
//        public event Action<bool, Pay.AnswerTerminal> CommandCompleted;
//        public Cash_check cc;
//        public string Url;
//        public string Data;
//        private CancellationTokenSource cts;
//        private int timeout = 80;
//        public CommandResult commandResult = null;

//        // Событие для уведомления о завершении
//        public event EventHandler<bool> PaymentCompleted;

//        public WaitNonCashPay()
//        {
//            InitializeComponent(); // Теперь это будет работать
//            StartTimer();
//            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
//            this.ShowInTaskbar = false;
//            this.Opened += WaitNonCashPay_Opened;
//        }

//        private async void WaitNonCashPay_Opened(object? sender, EventArgs e)
//        {
//            await ActivateWindow(this);
//        }

//        private async Task ActivateWindow(Window window)
//        {
//            if (window == null) return;

//            await Dispatcher.UIThread.InvokeAsync(() =>
//            {
//                if (window.IsVisible)
//                {
//                    // Попытка активировать окно
//                    window.Activate();
//                    window.Focus();

//                    // Для Linux - трюк с Topmost
//                    if (OperatingSystem.IsLinux())
//                    {
//                        window.Topmost = true;
//                        window.Topmost = false;
//                        window.Topmost = true;
//                    }
//                }
//            }, DispatcherPriority.Render);

//            // Дайте оконному менеджеру время отреагировать
//            if (OperatingSystem.IsLinux())
//            {
//                await Task.Delay(100); // 100 мс для надежности
//            }
//            else
//            {
//                await Task.Delay(10); // Для Windows достаточно
//            }
//        }

//        public WaitNonCashPay(int timeoutSeconds) : this()
//        {
//            _secondsRemaining = timeoutSeconds;
//            ProgressBarNonCashPay.Maximum = timeoutSeconds;
//            ProgressBarNonCashPay.Value = timeoutSeconds;
//            LabelTimer.Text = timeoutSeconds.ToString();
//            timeout = timeoutSeconds;
//        }

//        public class CommandResult
//        {
//            public bool Status { get; set; }
//            public Pay.AnswerTerminal AnswerTerminal { get; set; } = new Pay.AnswerTerminal();
//        }

//        /// <summary>
//        /// Отправляет команду в эквайринг
//        /// терминал и возвращает результат
//        /// </summary>
//        /// <param name="Url"></param>
//        /// <param name="Data"></param>        
//        public async Task<CommandResult> send_command_acquiring_terminal(string Url, string Data, CancellationToken cancellationToken)
//        {
//            CommandResult result = new CommandResult();

//            try
//            {
//                var request = (HttpWebRequest)WebRequest.Create(Url);
//                request.Method = "POST";
//                request.ContentType = "text/xml; charset=windows-1251";
//                byte[] byteArray = Encoding.GetEncoding("Windows-1251").GetBytes(Data);
//                request.ContentLength = byteArray.Length;

//                using (Stream dataStream = await request.GetRequestStreamAsync())
//                {
//                    await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
//                }

//                using (WebResponse response = await request.GetResponseAsync())
//                {
//                    using (Stream responseStream = response.GetResponseStream())
//                    {
//                        using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("Windows-1251")))
//                        {
//                            string responseContent = await reader.ReadToEndAsync();

//                            XmlSerializer serializer = new XmlSerializer(typeof(Response));
//                            using (StringReader stringReader = new StringReader(responseContent))
//                            {
//                                var test = (Response)serializer.Deserialize(stringReader);
//                                foreach (Field field in test.Field)
//                                {
//                                    if (field.Id == "39")
//                                    {
//                                        result.AnswerTerminal.сode_response_in_39_field = field.Text;
//                                        result.Status = field.Text.Trim() == "1";
//                                    }
//                                    else if (field.Id == "13")
//                                    {
//                                        result.AnswerTerminal.code_authorization = field.Text.Trim();
//                                    }
//                                    else if (field.Id == "14")
//                                    {
//                                        result.AnswerTerminal.number_reference = field.Text.Trim();
//                                    }
//                                    else if (field.Id == "15")
//                                    {
//                                        result.AnswerTerminal.сode_response_in_15_field = field.Text.Trim();
//                                    }
//                                    else if (field.Id == "90")
//                                    {
//                                        cc.recharge_note = field.Text.Trim();
//                                        int num_pos = cc.recharge_note.IndexOf("(КАССИР)");
//                                        if (num_pos > 0)
//                                        {
//                                            cc.recharge_note = cc.recharge_note.Substring(0, num_pos + 8);
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }

//                // ИСПРАВЛЕНИЕ 1: Вызываем событие перед закрытием окна
//                CommandCompleted?.Invoke(result.Status, result.AnswerTerminal);

//                // ИСПРАВЛЕНИЕ 2: Закрываем окно после вызова события
//                await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result.Status));
//            }
//            catch (WebException ex)
//            {
//                result.Status = false;
//                result.AnswerTerminal.error = true;

//                // ИСПРАВЛЕНИЕ 3: Сначала показываем сообщение
//                await MessageBoxHelper.Show($"Ошибка при оплате по карте: {ex.Message}",
//                    "Оплата по терминалу",
//                    MessageBoxButton.OK,
//                    MessageBoxType.Error,
//                    this);

//                // ИСПРАВЛЕНИЕ 4: Потом вызываем событие
//                CommandCompleted?.Invoke(false, result.AnswerTerminal);

//                // ИСПРАВЛЕНИЕ 5: Потом закрываем окно
//                await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(false));
//            }
//            catch (Exception ex)
//            {
//                result.Status = false;
//                result.AnswerTerminal.error = true;

//                await MessageBoxHelper.Show($"Ошибка при оплате по карте: {ex.Message}",
//                    "Оплата по терминалу",
//                    MessageBoxButton.OK,
//                    MessageBoxType.Error,
//                    this);

//                // ИСПРАВЛЕНИЕ 6: Вызываем событие
//                CommandCompleted?.Invoke(false, result.AnswerTerminal);

//                // ИСПРАВЛЕНИЕ 7: Закрываем окно
//                await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(false));
//            }

//            return result; // Теперь это возвращается ПОСЛЕ всех операций
//        }

//        [XmlRoot(ElementName = "field")]
//        public class Field
//        {

//            [XmlAttribute(AttributeName = "id")]
//            public string Id { get; set; }

//            [XmlText]
//            public string Text { get; set; }
//        }

//        [XmlRoot(ElementName = "response")]
//        public class Response
//        {

//            [XmlElement(ElementName = "field")]
//            public List<Field> Field { get; set; }
//        }

//        private async void StartTimer()
//        {
//            _cts = new CancellationTokenSource();

//            try
//            {
//                while (_secondsRemaining >= 0)
//                {
//                    // Обновляем UI
//                    UpdateTimerDisplay();

//                    if (_cts.Token.IsCancellationRequested)
//                        break;

//                    // Ждем 1 секунду
//                    await Task.Delay(1000, _cts.Token);
//                    _secondsRemaining--;
//                }

//                // Если время вышло
//                if (_secondsRemaining < 0)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() =>
//                    {
//                        StatusLabel.Text = "Время ожидания истекло!";
//                        StatusLabel.Foreground = Brushes.Red;
//                        CancelButton.Content = "Закрыть";
//                    });

//                    // ИСПРАВЛЕНИЕ 8: Вызываем событие при таймауте
//                    CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });

//                    PaymentCompleted?.Invoke(this, false);
//                    CloseWithResult(false);
//                }
//            }
//            catch (TaskCanceledException)
//            {
//                // Отменено пользователем
//                CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });
//                CloseWithResult(false);
//            }
//            catch (Exception ex)
//            {
//                await Dispatcher.UIThread.InvokeAsync(() =>
//                {
//                    StatusLabel.Text = $"Ошибка: {ex.Message}";
//                    StatusLabel.Foreground = Brushes.Red;
//                });

//                CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });
//                CloseWithResult(false);
//            }
//        }

//        private void UpdateTimerDisplay()
//        {
//            Dispatcher.UIThread.InvokeAsync(() =>
//            {
//                ProgressBarNonCashPay.Value = _secondsRemaining;
//                LabelTimer.Text = _secondsRemaining.ToString();
//                MessageLabel.Text = $"Ожидание ответа от терминала\nОсталось: {_secondsRemaining} секунд";
//            });
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            _cts?.Cancel();

//            // ИСПРАВЛЕНИЕ 9: Вызываем событие при отмене
//            CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });

//            CloseWithResult(false);
//        }

//        // Метод для закрытия с результатом (аналог DialogResult)
//        private void CloseWithResult(bool? result)
//        {
//            // Устанавливаем результат в Tag окна
//            this.Tag = result;

//            // Закрываем окно
//            this.Close();

//            // Вызываем событие
//            PaymentCompleted?.Invoke(this, result == true);
//        }

//        // Свойства для доступа извне
//        public bool IsTimeout => _secondsRemaining <= 0;
//        public int SecondsRemaining => _secondsRemaining;
//        public bool IsSuccessful => _isSuccessful;

//        // Статический метод для показа диалога
//        public static async Task<bool> ShowDialogAsync(Window owner = null, int timeoutSeconds = 80)
//        {
//            var dialog = new WaitNonCashPay(timeoutSeconds);
//            var tcs = new TaskCompletionSource<bool>();

//            dialog.PaymentCompleted += (s, result) => tcs.TrySetResult(result);

//            if (owner != null)
//                await dialog.ShowDialog(owner);
//            else
//                dialog.Show();

//            return await tcs.Task;
//        }

//        // Метод для запуска команды с ожиданием
//        public async Task<CommandResult> SendCommandWithTimeout(string url, string data, Cash_check cashCheck)
//        {
//            this.Url = url;
//            this.Data = data;
//            this.cc = cashCheck;

//            // Запускаем отправку команды в фоне
//            var sendTask = send_command_acquiring_terminal(url, data, _cts?.Token ?? CancellationToken.None);

//            // Создаем TaskCompletionSource для ожидания завершения
//            var tcs = new TaskCompletionSource<CommandResult>();

//            // Подписываемся на событие завершения
//            this.CommandCompleted += (success, answer) =>
//            {
//                var result = new CommandResult
//                {
//                    Status = success,
//                    AnswerTerminal = answer ?? new Pay.AnswerTerminal()
//                };
//                tcs.TrySetResult(result);
//            };

//            // Показываем окно
//            this.Show();

//            // Ждем завершения
//            return await tcs.Task;
//        }
//    }
//}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
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
        public string AuthorizationCode { get; set; }
        public string ReferenceNumber { get; set; }
        public string RechargeNote { get; set; }
        public string ErrorMessage { get; set; }

        public static TerminalResult CreateError(string message)
        {
            return new TerminalResult
            {
                IsSuccess = false,
                ErrorMessage = message
            };
        }
    }

    /// <summary>
    /// Окно ожидания ответа от эквайрингового терминала
    /// </summary>
    public partial class WaitNonCashPay : Window
    {
        // Поля для управления состоянием
        private CancellationTokenSource _cts;
        private int _secondsRemaining;
        private bool _isClosed = false;
        private readonly TaskCompletionSource<TerminalResult> _tcs = new();
        private readonly int _totalSeconds;

        // Публичные свойства для передачи данных (с публичными сеттерами - они нужны для инициализации)
        public string Url { get; set; }
        public string Data { get; set; }

        // Для обратной совместимости - internal чтобы новый код не использовал
        internal Cash_check cc { get; set; }

        // События для обратной совместимости
        public event Action<bool, Pay.AnswerTerminal> CommandCompleted;
        public event EventHandler<bool> PaymentCompleted;

        // Для обратной совместимости
        internal CommandResult commandResult = null;

        public WaitNonCashPay() : this(80) // Конструктор по умолчанию
        {
        }

        public WaitNonCashPay(int timeoutSeconds)
        {
            InitializeComponent();

            _totalSeconds = timeoutSeconds;
            _secondsRemaining = timeoutSeconds;

            // Инициализация UI
            if (ProgressBarNonCashPay != null)
            {
                ProgressBarNonCashPay.Maximum = timeoutSeconds;
                ProgressBarNonCashPay.Value = timeoutSeconds;
            }

            if (LabelTimer != null)
                LabelTimer.Text = timeoutSeconds.ToString();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this.ShowInTaskbar = false;
            this.Opened += OnOpened;
        }

        #region Инициализация и запуск

        private async void OnOpened(object sender, EventArgs e)
        {
            _ = SafeInitializeAsync();
        }

        private async Task SafeInitializeAsync()
        {
            // Локальная функция для безопасной активации окна через единый MessageBoxHelper
            async Task ActivateSafely()
            {
                try
                {
                    // Используем общий метод активации из MessageBoxHelper
                    await MessageBoxHelper.ActivateWindow(this).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[WaitNonCashPay] Activation warning: {ex.Message}");
                }
            }

            try
            {
                _ = ActivateSafely(); // Fire-and-forget
                await RunBackgroundTasksAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_isClosed)
                    CloseWithResult(TerminalResult.CreateError($"Ошибка инициализации: {ex.Message}"));
            }
        }

        #endregion

        #region Фоновые задачи

        private async Task RunBackgroundTasksAsync()
        {
            try
            {
                var timerTask = RunTimerAsync();
                var commandTask = SendCommandAsync();

                await Task.WhenAll(timerTask, commandTask).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        CloseWithResult(TerminalResult.CreateError($"Сбой: {ex.Message}")));
                }
            }
        }

        private async Task RunTimerAsync()
        {
            _cts = new CancellationTokenSource();

            try
            {
                while (_secondsRemaining > 0 && !_cts.Token.IsCancellationRequested)
                {
                    if (_isClosed) return; // Быстрый выход если окно уже закрыто

                    UpdateTimerDisplay();
                    await Task.Delay(1000, _cts.Token).ConfigureAwait(false);
                    _secondsRemaining--;
                }

                // Таймаут - только если окно еще не закрыто
                if (_secondsRemaining <= 0 && !_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (StatusLabel != null)
                        {
                            StatusLabel.Text = "Время ожидания истекло";
                            StatusLabel.Foreground = Brushes.Red;
                        }
                        if (CancelButton != null)
                            CancelButton.Content = "Закрыть";
                    });

                    _cts.Cancel(); // Отменяем HTTP запрос
                    CloseWithResult(TerminalResult.CreateError("Таймаут операции"));
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная отмена - ничего не делаем
            }
        }

        private async Task<TerminalResult> SendCommandAsync()
        {
            try
            {
                var result = await SendRequestInternal(_cts.Token).ConfigureAwait(false);
                if (!_isClosed)
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                return result;
            }
            catch (OperationCanceledException)
            {
                var result = TerminalResult.CreateError("Операция отменена");
                if (!_isClosed)
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                return result;
            }
            catch (Exception ex)
            {
                var result = TerminalResult.CreateError($"Ошибка сети: {ex.Message}");
                if (!_isClosed)
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                return result;
            }
        }

        #endregion

        #region HTTP запрос

        private async Task<TerminalResult> SendRequestInternal(CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(_totalSeconds); // Синхронизируем с таймером

            var content = new StringContent(Data, Encoding.GetEncoding("Windows-1251"), "text/xml");

            try
            {
                var response = await client.PostAsync(Url, content, cancellationToken).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseResponse(responseContent);
            }
            catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Запрос отменен", ex, cancellationToken);
            }
        }

        #endregion

        #region Парсинг ответа

        [XmlRoot(ElementName = "field")]
        public class Field
        {
            [XmlAttribute(AttributeName = "id")]
            public string Id { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "response")]
        public class Response
        {
            [XmlElement(ElementName = "field")]
            public List<Field> Field { get; set; }
        }

        public class CommandResult
        {
            public bool Status { get; set; }
            public Pay.AnswerTerminal AnswerTerminal { get; set; } = new Pay.AnswerTerminal();
        }

        private TerminalResult ParseResponse(string xml)
        {
            var result = new TerminalResult();
            var commandResult = new CommandResult();

            try
            {
                var serializer = new XmlSerializer(typeof(Response));
                using var reader = new StringReader(xml);
                var response = (Response)serializer.Deserialize(reader);

                foreach (var field in response.Field)
                {
                    switch (field.Id)
                    {
                        case "39":
                            result.CodeResponse = field.Text.Trim();
                            result.IsSuccess = field.Text.Trim() == "1";
                            commandResult.Status = result.IsSuccess;
                            commandResult.AnswerTerminal.сode_response_in_39_field = field.Text.Trim();
                            break;
                        case "13":
                            result.AuthorizationCode = field.Text.Trim();
                            commandResult.AnswerTerminal.code_authorization = field.Text.Trim();
                            break;
                        case "14":
                            result.ReferenceNumber = field.Text.Trim();
                            commandResult.AnswerTerminal.number_reference = field.Text.Trim();
                            break;
                        case "15":
                            commandResult.AnswerTerminal.сode_response_in_15_field = field.Text.Trim();
                            break;
                        case "90":
                            result.RechargeNote = CleanRechargeNote(field.Text.Trim());
                            // Для обратной совместимости - обновляем cc
                            if (cc != null)
                            {
                                cc.recharge_note = result.RechargeNote;
                            }
                            break;
                    }
                }

                // Сохраняем для обратной совместимости
                commandResult.AnswerTerminal.error = !result.IsSuccess;
                this.commandResult = commandResult;

                // Вызываем события для обратной совместимости
                CommandCompleted?.Invoke(result.IsSuccess, commandResult.AnswerTerminal);
            }
            catch (Exception ex)
            {
                return TerminalResult.CreateError($"Ошибка парсинга: {ex.Message}");
            }

            return result;
        }

        private string CleanRechargeNote(string note)
        {
            if (string.IsNullOrEmpty(note)) return note;
            int pos = note.IndexOf("(КАССИР)");
            return pos > 0 ? note.Substring(0, pos + 8) : note;
        }

        #endregion

        #region Обновление UI

        private void UpdateTimerDisplay()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ProgressBarNonCashPay != null)
                    ProgressBarNonCashPay.Value = _secondsRemaining;

                if (LabelTimer != null)
                    LabelTimer.Text = _secondsRemaining.ToString();

                if (StatusLabel != null)
                {
                    double percent = ((double)_secondsRemaining / _totalSeconds) * 100;
                    StatusLabel.Text = $"Ожидание ответа от терминала... {percent:F0}%";
                }
            });
        }

        #endregion

        #region Обработка действий пользователя

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (CancelButton != null)
            {
                CancelButton.IsEnabled = false;
                CancelButton.Content = "Отмена...";
            }

            _cts?.Cancel();
            CloseWithResult(TerminalResult.CreateError("Отменено пользователем"));
        }

        #endregion

        #region Завершение работы

        private void CloseWithResult(TerminalResult result)
        {
            if (_isClosed) return;
            _isClosed = true;

            // Устанавливаем результат
            _tcs.TrySetResult(result);

            // Для обратной совместимости
            PaymentCompleted?.Invoke(this, result.IsSuccess);

            // Закрываем окно
            Dispatcher.UIThread.Post(() =>
            {
                this.Tag = result.IsSuccess;
                this.Close();
            });
        }

        #endregion

        #region Публичный API (НОВЫЙ, РЕКОМЕНДУЕМЫЙ)

        /// <summary>
        /// Показывает модальное окно ожидания и возвращает результат
        /// </summary>
        /// <param name="owner">Владелец окна (обязателен для модального показа)</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <param name="url">URL терминала</param>
        /// <param name="data">XML данные для отправки</param>
        /// <returns>Результат операции терминала</returns>
        public static async Task<TerminalResult> ShowAndWaitAsync(
            Window owner,
            int timeoutSeconds,
            string url,
            string data)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner), "Владелец окна обязателен для модального показа");

            var dialog = new WaitNonCashPay(timeoutSeconds)
            {
                Url = url,
                Data = data
            };

            await dialog.ShowDialog(owner).ConfigureAwait(false);
            return await dialog._tcs.Task.ConfigureAwait(false);
        }

        #endregion

        #region API для обратной совместимости (OLD, ПОМЕЧЕН КАК УСТАРЕВШИЙ)

        /// <summary>
        /// Для обратной совместимости со старым кодом.
        /// ВНИМАНИЕ: Этот метод устарел. Используйте ShowAndWaitAsync вместо него.
        /// </summary>
        [Obsolete("Используйте ShowAndWaitAsync вместо этого метода. ShowAndWaitAsync предоставляет более чистый API и не зависит от Cash_check. Этот метод будет удален в будущих версиях.")]
        public static async Task<bool> ShowDialogAsync(
            Window owner,
            int timeoutSeconds = 80,
            string url = null,
            string data = null,
            Cash_check cashCheck = null)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner), "Владелец окна обязателен для модального показа");

            var dialog = new WaitNonCashPay(timeoutSeconds)
            {
                Url = url,
                Data = data,
                cc = cashCheck
            };

            await dialog.ShowDialog(owner).ConfigureAwait(false);
            var result = await dialog._tcs.Task.ConfigureAwait(false);

            return result.IsSuccess;
        }

        /// <summary>
        /// Устаревший метод - используйте ShowAndWaitAsync.
        /// Этот метод будет удален в будущих версиях.
        /// </summary>
        [Obsolete("Используйте ShowAndWaitAsync вместо этого метода. ShowAndWaitAsync принимает все параметры явно и не требует создания экземпляра диалога.")]
        public async Task<CommandResult> SendCommandWithTimeout(string url, string data, Cash_check cashCheck)
        {
            var owner = this.VisualRoot as Window;
            if (owner == null)
            {
                throw new InvalidOperationException("Не удалось определить владельца окна. Используйте ShowAndWaitAsync с явным указанием owner.");
            }

            // Присваиваем данные (this. нужен чтобы отличить от параметров)
            this.Url = url;
            this.Data = data;
            this.cc = cashCheck;

            // Показываем окно модально
            await ShowDialog(owner).ConfigureAwait(false);

            // Получаем результат
            var terminalResult = await _tcs.Task.ConfigureAwait(false);

            // Конвертируем в старый формат для обратной совместимости
            return new CommandResult
            {
                Status = terminalResult.IsSuccess,
                AnswerTerminal = new Pay.AnswerTerminal
                {
                    сode_response_in_39_field = terminalResult.CodeResponse,
                    code_authorization = terminalResult.AuthorizationCode,
                    number_reference = terminalResult.ReferenceNumber,
                    error = !terminalResult.IsSuccess
                }
            };
        }

        #endregion

        #region Свойства для обратной совместимости

        public bool IsTimeout => _secondsRemaining <= 0;
        public int SecondsRemaining => _secondsRemaining;

        #endregion
    }
}