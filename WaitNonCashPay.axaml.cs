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

        // Публичные свойства для передачи данных
        public string Url { get; set; }
        public string Data { get; set; }

        // Для обратной совместимости
        internal Cash_check cc { get; set; }

        // События для обратной совместимости
        public event Action<bool, Pay.AnswerTerminal> CommandCompleted;
        public event EventHandler<bool> PaymentCompleted;

        // Для обратной совместимости
        internal CommandResult commandResult = null;

        private readonly TaskCompletionSource<bool> _windowClosedTcs = new();

        public WaitNonCashPay() : this(80)
        {
        }

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
            try
            {
                _ = ActivateWindowSafely();
                await RunBackgroundTasksAsync();
            }
            catch (Exception ex)
            {
                if (!_isClosed)
                    CloseWithResult(TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Ошибка инициализации")));
            }
        }

        private async Task ActivateWindowSafely()
        {
            try
            {
                await MessageBoxHelper.ActivateWindow(this);
            }
            catch
            {
                // Игнорируем ошибки активации - не критично
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
                await Task.WhenAll(timerTask, commandTask);
            }
            catch (Exception ex)
            {
                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        CloseWithResult(TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Сбой операции"))));
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
                    if (_isClosed) return;
                    UpdateTimerDisplay();
                    await Task.Delay(1000, _cts.Token);
                    _secondsRemaining--;
                }

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

                    _cts.Cancel();
                    CloseWithResult(TerminalResult.CreateError(
                        "Терминал не ответил вовремя.\n\nПроверьте:\n• Терминал включен и готов к работе\n• Сетевой кабель подключен\n• Нет ошибок на экране терминала"));
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная отмена
            }
        }

        private async Task<TerminalResult> SendCommandAsync()
        {
            try
            {
                var result = await SendRequestInternal(_cts.Token).ConfigureAwait(false);

                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));

                    // ✅ ЖДЁМ ФАКТИЧЕСКОГО ЗАКРЫТИЯ ОКНА
                    await WaitForWindowCloseAsync();
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                var result = TerminalResult.CreateError("Операция отменена");
                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                    await WaitForWindowCloseAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                var result = TerminalResult.CreateError($"Ошибка сети: {ex.Message}");
                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
                    await WaitForWindowCloseAsync();
                }
                return result;
            }
        }

        #endregion

        //#region HTTP запрос с понятными ошибками

        //private async Task<TerminalResult> SendRequestInternal(CancellationToken cancellationToken)
        //{
        //    using var client = new HttpClient();
        //    client.Timeout = TimeSpan.FromSeconds(_totalSeconds);

        //    var content = new StringContent(Data, Encoding.GetEncoding("Windows-1251"), "text/xml");

        //    try
        //    {
        //        var response = await client.PostAsync(Url, content, cancellationToken);
        //        var responseContent = await response.Content.ReadAsStringAsync();

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return TerminalResult.CreateError(GetHttpErrorMessage(response.StatusCode, response.ReasonPhrase));
        //        }

        //        return ParseResponse(responseContent);
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        return TerminalResult.CreateError(GetNetworkErrorMessage(ex));
        //    }
        //    catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        //    {
        //        throw; // Пробрасываем для корректной обработки
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return TerminalResult.CreateError(
        //            "Запрос к терминалу превысил время ожидания.\n\nВозможные причины:\n• Терминал завис или перезагружается\n• Проблемы с сетевым подключением\n• Терминал занят другой операцией");
        //    }
        //    catch (Exception ex)
        //    {
        //        return TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Неожиданная ошибка"));
        //    }
        //}

        #region HTTP запрос с понятными ошибками (LEGACY - HttpWebRequest)
        private async Task<TerminalResult> SendRequestInternal(CancellationToken cancellationToken)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.ContentType = "text/xml; charset=windows-1251";
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Timeout = _totalSeconds * 1000;
                request.ReadWriteTimeout = _totalSeconds * 1000;

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] byteArray = Encoding.GetEncoding("Windows-1251").GetBytes(Data);
                request.ContentLength = byteArray.Length;

                using (var dataStream = await request.GetRequestStreamAsync())
                {
                    await dataStream.WriteAsync(byteArray, 0, byteArray.Length, cancellationToken);
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream, Encoding.GetEncoding("Windows-1251")))
                {
                    var responseContent = await reader.ReadToEndAsync();
                    return ParseResponse(responseContent);
                }
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.ConnectFailure)
            {
                return TerminalResult.CreateError(
                    "Не удалось подключиться к терминалу.\n\n" +
                    "Проверьте:\n" +
                    "• Терминал включен и работает\n" +
                    "• IP-адрес терминала указан верно\n" +
                    "• Сетевой кабель подключен\n" +
                    $"Детали: {ex.Message}");
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
            {
                return TerminalResult.CreateError(
                    "Запрос к терминалу превысил время ожидания.\n\n" +
                    "Возможные причины:\n" +
                    "• Терминал завис или перезагружается\n" +
                    "• Проблемы с сетевым подключением\n" +
                    "• Терминал занят другой операцией");
            }
            catch (WebException ex)
            {
                // Универсальная обработка остальных WebException
                string detail = ex.Response is HttpWebResponse httpResp
                    ? $"\nСтатус: {(int)httpResp.StatusCode} {httpResp.StatusDescription}"
                    : "";

                return TerminalResult.CreateError(
                    $"Ошибка связи с терминалом.{detail}\n\n" +
                    $"Детали: {ex.Message}\n\n" +
                    "Проверьте сетевое подключение и состояние терминала.");
            }
            catch (OperationCanceledException)
            {
                throw; // Пробрасываем для корректной обработки отмены
            }
            catch (Exception ex)
            {
                return TerminalResult.CreateError(
                    $"Неожиданная ошибка: {ex.Message}\n\n" +
                    "Если ошибка повторяется — обратитесь в техническую поддержку.");
            }
        }

        /// <summary>
        /// Возвращает понятное сообщение для HTTP-ошибок
        /// </summary>
        private string GetHttpErrorMessage(System.Net.HttpStatusCode statusCode, string reasonPhrase)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.NotFound =>
                    "Терминал не найден в сети.\n\nПроверьте:\n• IP-адрес терминала в настройках\n• Терминал подключен к той же сети\n• Сетевой кабель не повреждён",

                System.Net.HttpStatusCode.BadRequest =>
                    "Терминал не понял запрос.\n\nВозможно:\n• Неверный формат данных\n• Терминал требует обновления ПО\n• Обратитесь в техническую поддержку",

                System.Net.HttpStatusCode.InternalServerError =>
                    "Ошибка на стороне терминала.\n\nПопробуйте:\n• Перезагрузить терминал\n• Проверить наличие бумаги и чеков\n• Если ошибка повторяется — вызовите специалиста",

                System.Net.HttpStatusCode.ServiceUnavailable =>
                    "Терминал временно недоступен.\n\nВозможно:\n• Терминал выполняет другую операцию\n• Идёт перезагрузка терминала\n• Подождите 30 секунд и повторите попытку",

                _ => $"Ошибка связи с терминалом ({(int)statusCode} {reasonPhrase}).\n\nПроверьте сетевое подключение и состояние терминала."
            };
        }

        /// <summary>
        /// Возвращает понятное сообщение для сетевых ошибок
        /// </summary>
        private string GetNetworkErrorMessage(HttpRequestException ex)
        {
            string message = ex.Message?.ToLower() ?? "";

            if (message.Contains("connection refused") || message.Contains("не удалось подключиться"))
            {
                return "Не удалось подключиться к терминалу.\n\nПроверьте:\n• Терминал включен и работает\n• IP-адрес терминала указан верно\n• Сетевой кабель подключен к терминалу и кассе\n• Нет конфликтов IP-адресов в сети";
            }

            if (message.Contains("timeout") || message.Contains("превышено время"))
            {
                return "Терминал не отвечает.\n\nВозможные причины:\n• Терминал завис или перезагружается\n• Сетевое соединение нестабильно\n• Терминал занят другой операцией\n\nПодождите 30 секунд и повторите попытку.";
            }

            if (message.Contains("name resolution") || message.Contains("host") || message.Contains("dns"))
            {
                return "Не удалось найти терминал в сети.\n\nПроверьте:\n• IP-адрес терминала в настройках кассы\n• Терминал и касса в одной подсети\n• Сетевые настройки (шлюз, маска)";
            }

            if (message.Contains("ssl") || message.Contains("certificate") || message.Contains("secure"))
            {
                return "Ошибка защищённого соединения с терминалом.\n\nОбратитесь в техническую поддержку для проверки настроек безопасности.";
            }

            return $"Ошибка сетевого подключения к терминалу.\n\nДетали: {ex.Message}\n\nПроверьте сетевое подключение и состояние терминала.";
        }

        #endregion

        #region Парсинг ответа с понятными ошибками

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
                if (string.IsNullOrWhiteSpace(xml) || xml.Trim().Length < 10)
                {
                    return TerminalResult.CreateError(
                        "Терминал вернул пустой ответ.\n\nВозможно:\n• Терминал не завершил операцию\n• Произошёл сбой в работе терминала\n• Попробуйте повторить оплату");
                }

                var serializer = new XmlSerializer(typeof(Response));
                using var reader = new StringReader(xml);
                var response = (Response)serializer.Deserialize(reader);

                if (response?.Field == null)
                {
                    return TerminalResult.CreateError(
                        "Некорректный формат ответа от терминала.\n\nВозможно:\n• Терминал требует обновления ПО\n• Произошёл сбой при передаче данных\n• Обратитесь в техническую поддержку");
                }

                bool hasCode39 = false;

                foreach (var field in response.Field)
                {
                    switch (field.Id)
                    {
                        case "39":
                            hasCode39 = true;
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
                            if (cc != null)
                                cc.recharge_note = result.RechargeNote;
                            break;
                    }
                }

                if (!hasCode39)
                {
                    return TerminalResult.CreateError(
                        "Терминал не вернул код результата операции.\n\nВозможно:\n• Ошибка в протоколе обмена\n• Терминал требует диагностики\n• Обратитесь в техническую поддержку");
                }

                commandResult.AnswerTerminal.error = !result.IsSuccess;
                this.commandResult = commandResult;
                CommandCompleted?.Invoke(result.IsSuccess, commandResult.AnswerTerminal);

                // Если операция не успешна — добавляем понятное сообщение
                if (!result.IsSuccess && !string.IsNullOrEmpty(result.CodeResponse))
                {
                    result.ErrorMessage = GetTerminalErrorMessage(result.CodeResponse);
                }
            }
            catch (InvalidOperationException ex) when (ex.InnerException != null)
            {
                return TerminalResult.CreateError(
                    $"Не удалось обработать ответ терминала.\n\nВозможно:\n• Ответ повреждён при передаче\n• Терминал вернул некорректные данные\n• Попробуйте повторить операцию");
            }
            catch (Exception ex)
            {
                return TerminalResult.CreateError(
                    $"Ошибка при обработке ответа терминала.\n\nЕсли ошибка повторяется — обратитесь в техническую поддержку.");
            }

            return result;
        }

        /// <summary>
        /// Возвращает понятное сообщение для кодов ошибок терминала (поле 39)
        /// </summary>
        private string GetTerminalErrorMessage(string code39)
        {
            return code39 switch
            {
                "0" or "00" => "Операция отклонена банком.\n\nВозможные причины:\n• Недостаточно средств на карте\n• Карта заблокирована\n• Неверный PIN-код\n• Обратитесь в банк-эмитент карты",

                "05" => "Операция отклонена.\n\nВозможные причины:\n• Карта не активирована\n• Превышен лимит операций\n• Обратитесь в банк-эмитент карты",

                "14" => "Неверные реквизиты карты.\n\nПроверьте:\n• Карта вставлена правильно\n• Чип карты не повреждён\n• Попробуйте провести карту ещё раз",

                "41" or "43" => "Карта в списке исключений.\n\nКарта заблокирована или утеряна.\nПопросите покупателя использовать другую карту.",

                "51" => "Недостаточно средств.\n\nНа карте покупателя недостаточно средств для оплаты.\nПопросите использовать другую карту или наличные.",

                "54" => "Срок действия карты истёк.\n\nПопросите покупателя использовать другую карту.",

                "55" => "Неверный PIN-код.\n\nПопросите покупателя ввести PIN-код ещё раз.\nПри трёх неудачных попытках карта может быть заблокирована.",

                "57" => "Операция запрещена для этой карты.\n\nВозможно:\n• Карта не поддерживает данный тип операций\n• Ограничения со стороны банка\n• Попросите использовать другую карту",

                "61" => "Превышена сумма операции.\n\nСумма покупки превышает лимит для одной операции.\nРазбейте оплату на несколько частей или используйте другую карту.",

                "62" or "63" => "Нарушение безопасности.\n\nОперация отклонена системой безопасности.\nПопросите покупателя обратиться в банк.",

                "91" or "96" => "Ошибка связи с банком.\n\nВременные проблемы со связью с процессинговым центром.\nПодождите 1-2 минуты и повторите попытку.",

                "94" => "Дубликат операции.\n\nЭта операция уже была проведена.\nПроверьте историю операций на терминале.",

                "E0" or "E1" or "E2" => "Ошибка терминала.\n\nВозможно:\n• Терминал требует перезагрузки\n• Проблема с фискальным накопителем\n• Обратитесь в техническую поддержку",

                _ => $"Операция отклонена (код {code39}).\n\nПопросите покупателя:\n• Использовать другую карту\n• Проверить баланс\n• Обратиться в банк-эмитент при повторении ошибки"
            };
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
                    LabelTimer.Text = "Осталось "+_secondsRemaining.ToString() + " сек.";

                if (StatusLabel != null)
                {
                    double percent = ((double)_secondsRemaining / _totalSeconds) * 100;
                    StatusLabel.Text = $"Оплата еще не подтверждена ... {percent:F0}%";
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
            CloseWithResult(TerminalResult.CreateError(
                "Операция отменена пользователем.\n\nОплата не была проведена.\nПопробуйте повторить или выберите другой способ оплаты."));
        }

        #endregion

        #region Завершение работы

        private void CloseWithResult(TerminalResult result)
        {
            if (_isClosed) return;
            _isClosed = true;

            _tcs.TrySetResult(result);
            PaymentCompleted?.Invoke(this, result.IsSuccess);

            // ✅ ВАЖНО: Закрываем окно и ЖДЁМ завершения
            Dispatcher.UIThread.Post(() =>
            {
                this.Tag = result.IsSuccess;
                this.Close();
                // Сигнализируем что окно действительно закрылось
                _windowClosedTcs.TrySetResult(true);
            });
        }

        // ✅ НОВЫЙ МЕТОД: Ждать фактического закрытия окна
        public async Task WaitForWindowCloseAsync()
        {
            await _windowClosedTcs.Task;
        }

        #endregion

        #region Публичный API

        public static async Task<TerminalResult> ShowAndWaitAsync(
            Window owner,
            int timeoutSeconds,
            string url,
            string data)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner), "Владелец окна обязателен");

            var dialog = new WaitNonCashPay(timeoutSeconds)
            {
                Url = url,
                Data = data
            };

            await dialog.ShowDialog(owner);
            return await dialog._tcs.Task;
        }

        #endregion

        #region API для обратной совместимости

        [Obsolete("Используйте ShowAndWaitAsync")]
        public static async Task<bool> ShowDialogAsync(
            Window owner,
            int timeoutSeconds = 80,
            string url = null,
            string data = null,
            Cash_check cashCheck = null)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            var dialog = new WaitNonCashPay(timeoutSeconds)
            {
                Url = url,
                Data = data,
                cc = cashCheck
            };

            await dialog.ShowDialog(owner);
            var result = await dialog._tcs.Task;
            return result.IsSuccess;
        }

        [Obsolete("Используйте ShowAndWaitAsync")]
        public async Task<CommandResult> SendCommandWithTimeout(string url, string data, Cash_check cashCheck)
        {
            var owner = this.VisualRoot as Window;
            if (owner == null)
                throw new InvalidOperationException("Не удалось определить владельца окна");

            this.Url = url;
            this.Data = data;
            this.cc = cashCheck;

            await ShowDialog(owner);
            var terminalResult = await _tcs.Task;

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

        #region Свойства

        public bool IsTimeout => _secondsRemaining <= 0;
        public int SecondsRemaining => _secondsRemaining;

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Возвращает понятное сообщение об ошибке для пользователя
        /// </summary>
        private string GetUserFriendlyMessage(Exception ex, string defaultPrefix)
        {
            string message = ex.Message?.ToLower() ?? "";

            // Сетевые ошибки
            if (message.Contains("connection") || message.Contains("connect") || message.Contains("подключ"))
            {
                return $"{defaultPrefix}\n\nНе удалось подключиться к терминалу.\nПроверьте:\n• Терминал включен\n• Сетевой кабель подключен\n• IP-адрес указан верно";
            }

            // Таймауты
            if (message.Contains("timeout") || message.Contains("время") || message.Contains("wait"))
            {
                return $"{defaultPrefix}\n\nТерминал не ответил вовремя.\nПопробуйте:\n• Подождать 30 секунд\n• Перезагрузить терминал\n• Проверить сетевое подключение";
            }

            // Ошибки авторизации/безопасности
            if (message.Contains("auth") || message.Contains("access") || message.Contains("forbidden") || message.Contains("unauthorized"))
            {
                return $"{defaultPrefix}\n\nДоступ к терминалу запрещён.\nВозможно:\n• Неверные настройки доступа\n• Требуется авторизация\n• Обратитесь к администратору";
            }

            // Ошибки парсинга/формата
            if (message.Contains("parse") || message.Contains("format") || message.Contains("xml") || message.Contains("deserialize"))
            {
                return $"{defaultPrefix}\n\nНекорректный ответ от терминала.\nВозможно:\n• Терминал требует обновления\n• Ошибка в протоколе обмена\n• Обратитесь в техническую поддержку";
            }

            // Ошибки ввода-вывода
            if (message.Contains("io") || message.Contains("stream") || message.Contains("read") || message.Contains("write"))
            {
                return $"{defaultPrefix}\n\nОшибка передачи данных.\nПопробуйте:\n• Проверить сетевое подключение\n• Перезагрузить терминал\n• Повторить операцию";
            }

            // Ошибки аргументов/настроек
            if (message.Contains("argument") || message.Contains("parameter") || message.Contains("null") || message.Contains("empty"))
            {
                return $"{defaultPrefix}\n\nОшибка в настройках подключения.\nПроверьте:\n• IP-адрес терминала\n• Порт подключения\n• Формат данных запроса";
            }

            // Дефолтное сообщение
            return $"{defaultPrefix}\n\n{ex.Message}\n\nЕсли ошибка повторяется — обратитесь в техническую поддержку.";
        }

        #endregion
    }
}