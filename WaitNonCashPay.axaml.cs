//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Interactivity;
//using Avalonia.Media;
//using Avalonia.Threading;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Xml.Serialization;

//namespace Cash8Avalon
//{
//    /// <summary>
//    /// Результат выполнения команды терминала
//    /// </summary>
//    public class TerminalResult
//    {
//        public bool IsSuccess { get; set; }
//        public string CodeResponse { get; set; }
//        public string AuthorizationCode { get; set; }
//        public string ReferenceNumber { get; set; }
//        public string RechargeNote { get; set; }
//        public string ErrorMessage { get; set; }

//        public static TerminalResult CreateError(string message)
//        {
//            return new TerminalResult
//            {
//                IsSuccess = false,
//                ErrorMessage = message
//            };
//        }
//    }

//    /// <summary>
//    /// Окно ожидания ответа от эквайрингового терминала
//    /// </summary>
//    public partial class WaitNonCashPay : Window
//    {
//        // Поля для управления состоянием
//        private CancellationTokenSource _cts;
//        private int _secondsRemaining;
//        private bool _isClosed = false;
//        private readonly TaskCompletionSource<TerminalResult> _tcs = new();
//        private readonly int _totalSeconds;

//        // Публичные свойства для передачи данных
//        public string Url { get; set; }
//        public string Data { get; set; }

//        // Для обратной совместимости
//        internal Cash_check cc { get; set; }

//        // События для обратной совместимости
//        public event Action<bool, Pay.AnswerTerminal> CommandCompleted;
//        public event EventHandler<bool> PaymentCompleted;

//        // Для обратной совместимости
//        internal CommandResult commandResult = null;

//        private readonly TaskCompletionSource<bool> _windowClosedTcs = new();

//        public WaitNonCashPay() : this(80)
//        {
//        }

//        public WaitNonCashPay(int timeoutSeconds)
//        {
//            InitializeComponent();

//            _totalSeconds = timeoutSeconds;
//            _secondsRemaining = timeoutSeconds;

//            if (ProgressBarNonCashPay != null)
//            {
//                ProgressBarNonCashPay.Maximum = timeoutSeconds;
//                ProgressBarNonCashPay.Value = timeoutSeconds;
//            }

//            if (LabelTimer != null)
//                LabelTimer.Text = timeoutSeconds.ToString();

//            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
//            this.ShowInTaskbar = false;
//            this.Opened += OnOpened;
//        }

//        #region Инициализация и запуск

//        private async void OnOpened(object sender, EventArgs e)
//        {
//            _ = SafeInitializeAsync();
//        }

//        private async Task SafeInitializeAsync()
//        {
//            try
//            {
//                _ = ActivateWindowSafely();
//                await RunBackgroundTasksAsync();
//            }
//            catch (Exception ex)
//            {
//                if (!_isClosed)
//                    CloseWithResult(TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Ошибка инициализации")));
//            }
//        }

//        private async Task ActivateWindowSafely()
//        {
//            try
//            {
//                await MessageBoxHelper.ActivateWindow(this);
//            }
//            catch
//            {
//                // Игнорируем ошибки активации - не критично
//            }
//        }

//        #endregion

//        #region Фоновые задачи

//        private async Task RunBackgroundTasksAsync()
//        {
//            try
//            {
//                var timerTask = RunTimerAsync();
//                var commandTask = SendCommandAsync();
//                await Task.WhenAll(timerTask, commandTask);
//            }
//            catch (Exception ex)
//            {
//                if (!_isClosed)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() =>
//                        CloseWithResult(TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Сбой операции"))));
//                }
//            }
//        }

//        private async Task RunTimerAsync()
//        {
//            _cts = new CancellationTokenSource();

//            try
//            {
//                while (_secondsRemaining > 0 && !_cts.Token.IsCancellationRequested)
//                {
//                    if (_isClosed) return;
//                    UpdateTimerDisplay();
//                    await Task.Delay(1000, _cts.Token);
//                    _secondsRemaining--;
//                }

//                if (_secondsRemaining <= 0 && !_isClosed)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() =>
//                    {
//                        if (StatusLabel != null)
//                        {
//                            StatusLabel.Text = "Время ожидания истекло";
//                            StatusLabel.Foreground = Brushes.Red;
//                        }
//                        if (CancelButton != null)
//                            CancelButton.Content = "Закрыть";
//                    });

//                    _cts.Cancel();
//                    CloseWithResult(TerminalResult.CreateError(
//                        "Терминал не ответил вовремя.\n\nПроверьте:\n• Терминал включен и готов к работе\n• Сетевой кабель подключен\n• Нет ошибок на экране терминала"));
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                // Нормальная отмена
//            }
//        }

//        private async Task<TerminalResult> SendCommandAsync()
//        {
//            try
//            {
//                var result = await SendRequestInternal(_cts.Token).ConfigureAwait(false);

//                if (!_isClosed)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));

//                    // ✅ ЖДЁМ ФАКТИЧЕСКОГО ЗАКРЫТИЯ ОКНА
//                    await WaitForWindowCloseAsync();
//                }

//                return result;
//            }
//            catch (OperationCanceledException)
//            {
//                var result = TerminalResult.CreateError("Операция отменена");
//                if (!_isClosed)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
//                    await WaitForWindowCloseAsync();
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                var result = TerminalResult.CreateError($"Ошибка сети: {ex.Message}");
//                if (!_isClosed)
//                {
//                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));
//                    await WaitForWindowCloseAsync();
//                }
//                return result;
//            }
//        }

//        #endregion

//        //#region HTTP запрос с понятными ошибками

//        //private async Task<TerminalResult> SendRequestInternal(CancellationToken cancellationToken)
//        //{
//        //    using var client = new HttpClient();
//        //    client.Timeout = TimeSpan.FromSeconds(_totalSeconds);

//        //    var content = new StringContent(Data, Encoding.GetEncoding("Windows-1251"), "text/xml");

//        //    try
//        //    {
//        //        var response = await client.PostAsync(Url, content, cancellationToken);
//        //        var responseContent = await response.Content.ReadAsStringAsync();

//        //        if (!response.IsSuccessStatusCode)
//        //        {
//        //            return TerminalResult.CreateError(GetHttpErrorMessage(response.StatusCode, response.ReasonPhrase));
//        //        }

//        //        return ParseResponse(responseContent);
//        //    }
//        //    catch (HttpRequestException ex)
//        //    {
//        //        return TerminalResult.CreateError(GetNetworkErrorMessage(ex));
//        //    }
//        //    catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
//        //    {
//        //        throw; // Пробрасываем для корректной обработки
//        //    }
//        //    catch (TaskCanceledException)
//        //    {
//        //        return TerminalResult.CreateError(
//        //            "Запрос к терминалу превысил время ожидания.\n\nВозможные причины:\n• Терминал завис или перезагружается\n• Проблемы с сетевым подключением\n• Терминал занят другой операцией");
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        return TerminalResult.CreateError(GetUserFriendlyMessage(ex, "Неожиданная ошибка"));
//        //    }
//        //}

//        #region HTTP запрос с понятными ошибками (LEGACY - HttpWebRequest)
//        private async Task<TerminalResult> SendRequestInternal(CancellationToken cancellationToken)
//        {
//            try
//            {
//                var request = (HttpWebRequest)WebRequest.Create(Url);
//                request.Method = "POST";
//                request.ContentType = "text/xml; charset=windows-1251";
//                request.KeepAlive = false;
//                request.ProtocolVersion = HttpVersion.Version10;
//                request.Timeout = _totalSeconds * 1000;
//                request.ReadWriteTimeout = _totalSeconds * 1000;

//                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
//                byte[] byteArray = Encoding.GetEncoding("Windows-1251").GetBytes(Data);
//                request.ContentLength = byteArray.Length;

//                using (var dataStream = await request.GetRequestStreamAsync())
//                {
//                    await dataStream.WriteAsync(byteArray, 0, byteArray.Length, cancellationToken);
//                }

//                using (var response = (HttpWebResponse)await request.GetResponseAsync())
//                using (var responseStream = response.GetResponseStream())
//                using (var reader = new StreamReader(responseStream, Encoding.GetEncoding("Windows-1251")))
//                {
//                    var responseContent = await reader.ReadToEndAsync();
//                    return ParseResponse(responseContent);
//                }
//            }
//            catch (WebException ex) when (ex.Status == WebExceptionStatus.ConnectFailure)
//            {
//                return TerminalResult.CreateError(
//                    "Не удалось подключиться к терминалу.\n\n" +
//                    "Проверьте:\n" +
//                    "• Терминал включен и работает\n" +
//                    "• IP-адрес терминала указан верно\n" +
//                    "• Сетевой кабель подключен\n" +
//                    $"Детали: {ex.Message}");
//            }
//            catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
//            {
//                return TerminalResult.CreateError(
//                    "Запрос к терминалу превысил время ожидания.\n\n" +
//                    "Возможные причины:\n" +
//                    "• Терминал завис или перезагружается\n" +
//                    "• Проблемы с сетевым подключением\n" +
//                    "• Терминал занят другой операцией");
//            }
//            catch (WebException ex)
//            {
//                // Универсальная обработка остальных WebException
//                string detail = ex.Response is HttpWebResponse httpResp
//                    ? $"\nСтатус: {(int)httpResp.StatusCode} {httpResp.StatusDescription}"
//                    : "";

//                return TerminalResult.CreateError(
//                    $"Ошибка связи с терминалом.{detail}\n\n" +
//                    $"Детали: {ex.Message}\n\n" +
//                    "Проверьте сетевое подключение и состояние терминала.");
//            }
//            catch (OperationCanceledException)
//            {
//                throw; // Пробрасываем для корректной обработки отмены
//            }
//            catch (Exception ex)
//            {
//                return TerminalResult.CreateError(
//                    $"Неожиданная ошибка: {ex.Message}\n\n" +
//                    "Если ошибка повторяется — обратитесь в техническую поддержку.");
//            }
//        }

//        /// <summary>
//        /// Возвращает понятное сообщение для HTTP-ошибок
//        /// </summary>
//        private string GetHttpErrorMessage(System.Net.HttpStatusCode statusCode, string reasonPhrase)
//        {
//            return statusCode switch
//            {
//                System.Net.HttpStatusCode.NotFound =>
//                    "Терминал не найден в сети.\n\nПроверьте:\n• IP-адрес терминала в настройках\n• Терминал подключен к той же сети\n• Сетевой кабель не повреждён",

//                System.Net.HttpStatusCode.BadRequest =>
//                    "Терминал не понял запрос.\n\nВозможно:\n• Неверный формат данных\n• Терминал требует обновления ПО\n• Обратитесь в техническую поддержку",

//                System.Net.HttpStatusCode.InternalServerError =>
//                    "Ошибка на стороне терминала.\n\nПопробуйте:\n• Перезагрузить терминал\n• Проверить наличие бумаги и чеков\n• Если ошибка повторяется — вызовите специалиста",

//                System.Net.HttpStatusCode.ServiceUnavailable =>
//                    "Терминал временно недоступен.\n\nВозможно:\n• Терминал выполняет другую операцию\n• Идёт перезагрузка терминала\n• Подождите 30 секунд и повторите попытку",

//                _ => $"Ошибка связи с терминалом ({(int)statusCode} {reasonPhrase}).\n\nПроверьте сетевое подключение и состояние терминала."
//            };
//        }

//        /// <summary>
//        /// Возвращает понятное сообщение для сетевых ошибок
//        /// </summary>
//        private string GetNetworkErrorMessage(HttpRequestException ex)
//        {
//            string message = ex.Message?.ToLower() ?? "";

//            if (message.Contains("connection refused") || message.Contains("не удалось подключиться"))
//            {
//                return "Не удалось подключиться к терминалу.\n\nПроверьте:\n• Терминал включен и работает\n• IP-адрес терминала указан верно\n• Сетевой кабель подключен к терминалу и кассе\n• Нет конфликтов IP-адресов в сети";
//            }

//            if (message.Contains("timeout") || message.Contains("превышено время"))
//            {
//                return "Терминал не отвечает.\n\nВозможные причины:\n• Терминал завис или перезагружается\n• Сетевое соединение нестабильно\n• Терминал занят другой операцией\n\nПодождите 30 секунд и повторите попытку.";
//            }

//            if (message.Contains("name resolution") || message.Contains("host") || message.Contains("dns"))
//            {
//                return "Не удалось найти терминал в сети.\n\nПроверьте:\n• IP-адрес терминала в настройках кассы\n• Терминал и касса в одной подсети\n• Сетевые настройки (шлюз, маска)";
//            }

//            if (message.Contains("ssl") || message.Contains("certificate") || message.Contains("secure"))
//            {
//                return "Ошибка защищённого соединения с терминалом.\n\nОбратитесь в техническую поддержку для проверки настроек безопасности.";
//            }

//            return $"Ошибка сетевого подключения к терминалу.\n\nДетали: {ex.Message}\n\nПроверьте сетевое подключение и состояние терминала.";
//        }

//        #endregion

//        #region Парсинг ответа с понятными ошибками

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

//        public class CommandResult
//        {
//            public bool Status { get; set; }
//            public Pay.AnswerTerminal AnswerTerminal { get; set; } = new Pay.AnswerTerminal();
//        }

//        private TerminalResult ParseResponse(string xml)
//        {
//            var result = new TerminalResult();
//            var commandResult = new CommandResult();

//            try
//            {
//                if (string.IsNullOrWhiteSpace(xml) || xml.Trim().Length < 10)
//                {
//                    return TerminalResult.CreateError(
//                        "Терминал вернул пустой ответ.\n\nВозможно:\n• Терминал не завершил операцию\n• Произошёл сбой в работе терминала\n• Попробуйте повторить оплату");
//                }

//                var serializer = new XmlSerializer(typeof(Response));
//                using var reader = new StringReader(xml);
//                var response = (Response)serializer.Deserialize(reader);

//                if (response?.Field == null)
//                {
//                    return TerminalResult.CreateError(
//                        "Некорректный формат ответа от терминала.\n\nВозможно:\n• Терминал требует обновления ПО\n• Произошёл сбой при передаче данных\n• Обратитесь в техническую поддержку");
//                }

//                bool hasCode39 = false;

//                foreach (var field in response.Field)
//                {
//                    switch (field.Id)
//                    {
//                        case "39":
//                            hasCode39 = true;
//                            result.CodeResponse = field.Text.Trim();
//                            result.IsSuccess = field.Text.Trim() == "1";
//                            commandResult.Status = result.IsSuccess;
//                            commandResult.AnswerTerminal.сode_response_in_39_field = field.Text.Trim();
//                            break;
//                        case "13":
//                            result.AuthorizationCode = field.Text.Trim();
//                            commandResult.AnswerTerminal.code_authorization = field.Text.Trim();
//                            break;
//                        case "14":
//                            result.ReferenceNumber = field.Text.Trim();
//                            commandResult.AnswerTerminal.number_reference = field.Text.Trim();
//                            break;
//                        case "15":
//                            commandResult.AnswerTerminal.сode_response_in_15_field = field.Text.Trim();
//                            break;
//                        case "90":
//                            result.RechargeNote = CleanRechargeNote(field.Text.Trim());
//                            if (cc != null)
//                                cc.recharge_note = result.RechargeNote;
//                            break;
//                    }
//                }

//                if (!hasCode39)
//                {
//                    return TerminalResult.CreateError(
//                        "Терминал не вернул код результата операции.\n\nВозможно:\n• Ошибка в протоколе обмена\n• Терминал требует диагностики\n• Обратитесь в техническую поддержку");
//                }

//                commandResult.AnswerTerminal.error = !result.IsSuccess;
//                this.commandResult = commandResult;
//                CommandCompleted?.Invoke(result.IsSuccess, commandResult.AnswerTerminal);

//                // Если операция не успешна — добавляем понятное сообщение
//                if (!result.IsSuccess && !string.IsNullOrEmpty(result.CodeResponse))
//                {
//                    result.ErrorMessage = GetTerminalErrorMessage(result.CodeResponse);
//                }
//            }
//            catch (InvalidOperationException ex) when (ex.InnerException != null)
//            {
//                return TerminalResult.CreateError(
//                    $"Не удалось обработать ответ терминала.\n\nВозможно:\n• Ответ повреждён при передаче\n• Терминал вернул некорректные данные\n• Попробуйте повторить операцию");
//            }
//            catch (Exception ex)
//            {
//                return TerminalResult.CreateError(
//                    $"Ошибка при обработке ответа терминала.\n\nЕсли ошибка повторяется — обратитесь в техническую поддержку.");
//            }

//            return result;
//        }

//        /// <summary>
//        /// Возвращает понятное сообщение для кодов ошибок терминала (поле 39)
//        /// </summary>
//        private string GetTerminalErrorMessage(string code39)
//        {
//            return code39 switch
//            {
//                "0" or "00" => "Операция отклонена банком.\n\nВозможные причины:\n• Недостаточно средств на карте\n• Карта заблокирована\n• Неверный PIN-код\n• Обратитесь в банк-эмитент карты",

//                "05" => "Операция отклонена.\n\nВозможные причины:\n• Карта не активирована\n• Превышен лимит операций\n• Обратитесь в банк-эмитент карты",

//                "14" => "Неверные реквизиты карты.\n\nПроверьте:\n• Карта вставлена правильно\n• Чип карты не повреждён\n• Попробуйте провести карту ещё раз",

//                "41" or "43" => "Карта в списке исключений.\n\nКарта заблокирована или утеряна.\nПопросите покупателя использовать другую карту.",

//                "51" => "Недостаточно средств.\n\nНа карте покупателя недостаточно средств для оплаты.\nПопросите использовать другую карту или наличные.",

//                "54" => "Срок действия карты истёк.\n\nПопросите покупателя использовать другую карту.",

//                "55" => "Неверный PIN-код.\n\nПопросите покупателя ввести PIN-код ещё раз.\nПри трёх неудачных попытках карта может быть заблокирована.",

//                "57" => "Операция запрещена для этой карты.\n\nВозможно:\n• Карта не поддерживает данный тип операций\n• Ограничения со стороны банка\n• Попросите использовать другую карту",

//                "61" => "Превышена сумма операции.\n\nСумма покупки превышает лимит для одной операции.\nРазбейте оплату на несколько частей или используйте другую карту.",

//                "62" or "63" => "Нарушение безопасности.\n\nОперация отклонена системой безопасности.\nПопросите покупателя обратиться в банк.",

//                "91" or "96" => "Ошибка связи с банком.\n\nВременные проблемы со связью с процессинговым центром.\nПодождите 1-2 минуты и повторите попытку.",

//                "94" => "Дубликат операции.\n\nЭта операция уже была проведена.\nПроверьте историю операций на терминале.",

//                "E0" or "E1" or "E2" => "Ошибка терминала.\n\nВозможно:\n• Терминал требует перезагрузки\n• Проблема с фискальным накопителем\n• Обратитесь в техническую поддержку",

//                _ => $"Операция отклонена (код {code39}).\n\nПопросите покупателя:\n• Использовать другую карту\n• Проверить баланс\n• Обратиться в банк-эмитент при повторении ошибки"
//            };
//        }

//        private string CleanRechargeNote(string note)
//        {
//            if (string.IsNullOrEmpty(note)) return note;
//            int pos = note.IndexOf("(КАССИР)");
//            return pos > 0 ? note.Substring(0, pos + 8) : note;
//        }

//        #endregion

//        #region Обновление UI

//        private void UpdateTimerDisplay()
//        {
//            Dispatcher.UIThread.InvokeAsync(() =>
//            {
//                if (ProgressBarNonCashPay != null)
//                    ProgressBarNonCashPay.Value = _secondsRemaining;

//                if (LabelTimer != null)
//                    LabelTimer.Text = "Осталось "+_secondsRemaining.ToString() + " сек.";

//                if (StatusLabel != null)
//                {
//                    double percent = ((double)_secondsRemaining / _totalSeconds) * 100;
//                    StatusLabel.Text = $"Оплата еще не подтверждена ... {percent:F0}%";
//                }
//            });
//        }

//        #endregion

//        #region Обработка действий пользователя

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            if (CancelButton != null)
//            {
//                CancelButton.IsEnabled = false;
//                CancelButton.Content = "Отмена...";
//            }

//            _cts?.Cancel();
//            CloseWithResult(TerminalResult.CreateError(
//                "Операция отменена пользователем.\n\nОплата не была проведена.\nПопробуйте повторить или выберите другой способ оплаты."));
//        }

//        #endregion

//        #region Завершение работы

//        private void CloseWithResult(TerminalResult result)
//        {
//            if (_isClosed) return;
//            _isClosed = true;

//            _tcs.TrySetResult(result);
//            PaymentCompleted?.Invoke(this, result.IsSuccess);

//            // ✅ ВАЖНО: Закрываем окно и ЖДЁМ завершения
//            Dispatcher.UIThread.Post(() =>
//            {
//                this.Tag = result.IsSuccess;
//                this.Close();
//                // Сигнализируем что окно действительно закрылось
//                _windowClosedTcs.TrySetResult(true);
//            });
//        }

//        // ✅ НОВЫЙ МЕТОД: Ждать фактического закрытия окна
//        public async Task WaitForWindowCloseAsync()
//        {
//            await _windowClosedTcs.Task;
//        }

//        #endregion

//        #region Публичный API

//        public static async Task<TerminalResult> ShowAndWaitAsync(
//            Window owner,
//            int timeoutSeconds,
//            string url,
//            string data)
//        {
//            if (owner == null)
//                throw new ArgumentNullException(nameof(owner), "Владелец окна обязателен");

//            var dialog = new WaitNonCashPay(timeoutSeconds)
//            {
//                Url = url,
//                Data = data
//            };

//            await dialog.ShowDialog(owner);
//            return await dialog._tcs.Task;
//        }

//        #endregion

//        #region API для обратной совместимости

//        [Obsolete("Используйте ShowAndWaitAsync")]
//        public static async Task<bool> ShowDialogAsync(
//            Window owner,
//            int timeoutSeconds = 80,
//            string url = null,
//            string data = null,
//            Cash_check cashCheck = null)
//        {
//            if (owner == null)
//                throw new ArgumentNullException(nameof(owner));

//            var dialog = new WaitNonCashPay(timeoutSeconds)
//            {
//                Url = url,
//                Data = data,
//                cc = cashCheck
//            };

//            await dialog.ShowDialog(owner);
//            var result = await dialog._tcs.Task;
//            return result.IsSuccess;
//        }

//        [Obsolete("Используйте ShowAndWaitAsync")]
//        public async Task<CommandResult> SendCommandWithTimeout(string url, string data, Cash_check cashCheck)
//        {
//            var owner = this.VisualRoot as Window;
//            if (owner == null)
//                throw new InvalidOperationException("Не удалось определить владельца окна");

//            this.Url = url;
//            this.Data = data;
//            this.cc = cashCheck;

//            await ShowDialog(owner);
//            var terminalResult = await _tcs.Task;

//            return new CommandResult
//            {
//                Status = terminalResult.IsSuccess,
//                AnswerTerminal = new Pay.AnswerTerminal
//                {
//                    сode_response_in_39_field = terminalResult.CodeResponse,
//                    code_authorization = terminalResult.AuthorizationCode,
//                    number_reference = terminalResult.ReferenceNumber,
//                    error = !terminalResult.IsSuccess
//                }
//            };
//        }

//        #endregion

//        #region Свойства

//        public bool IsTimeout => _secondsRemaining <= 0;
//        public int SecondsRemaining => _secondsRemaining;

//        #endregion

//        #region Вспомогательные методы

//        /// <summary>
//        /// Возвращает понятное сообщение об ошибке для пользователя
//        /// </summary>
//        private string GetUserFriendlyMessage(Exception ex, string defaultPrefix)
//        {
//            string message = ex.Message?.ToLower() ?? "";

//            // Сетевые ошибки
//            if (message.Contains("connection") || message.Contains("connect") || message.Contains("подключ"))
//            {
//                return $"{defaultPrefix}\n\nНе удалось подключиться к терминалу.\nПроверьте:\n• Терминал включен\n• Сетевой кабель подключен\n• IP-адрес указан верно";
//            }

//            // Таймауты
//            if (message.Contains("timeout") || message.Contains("время") || message.Contains("wait"))
//            {
//                return $"{defaultPrefix}\n\nТерминал не ответил вовремя.\nПопробуйте:\n• Подождать 30 секунд\n• Перезагрузить терминал\n• Проверить сетевое подключение";
//            }

//            // Ошибки авторизации/безопасности
//            if (message.Contains("auth") || message.Contains("access") || message.Contains("forbidden") || message.Contains("unauthorized"))
//            {
//                return $"{defaultPrefix}\n\nДоступ к терминалу запрещён.\nВозможно:\n• Неверные настройки доступа\n• Требуется авторизация\n• Обратитесь к администратору";
//            }

//            // Ошибки парсинга/формата
//            if (message.Contains("parse") || message.Contains("format") || message.Contains("xml") || message.Contains("deserialize"))
//            {
//                return $"{defaultPrefix}\n\nНекорректный ответ от терминала.\nВозможно:\n• Терминал требует обновления\n• Ошибка в протоколе обмена\n• Обратитесь в техническую поддержку";
//            }

//            // Ошибки ввода-вывода
//            if (message.Contains("io") || message.Contains("stream") || message.Contains("read") || message.Contains("write"))
//            {
//                return $"{defaultPrefix}\n\nОшибка передачи данных.\nПопробуйте:\n• Проверить сетевое подключение\n• Перезагрузить терминал\n• Повторить операцию";
//            }

//            // Ошибки аргументов/настроек
//            if (message.Contains("argument") || message.Contains("parameter") || message.Contains("null") || message.Contains("empty"))
//            {
//                return $"{defaultPrefix}\n\nОшибка в настройках подключения.\nПроверьте:\n• IP-адрес терминала\n• Порт подключения\n• Формат данных запроса";
//            }

//            // Дефолтное сообщение
//            return $"{defaultPrefix}\n\n{ex.Message}\n\nЕсли ошибка повторяется — обратитесь в техническую поддержку.";
//        }

//        #endregion
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

        // Информация о попытках
        public int AttemptsCount { get; set; } = 1;
        public List<string> AttemptErrors { get; set; } = new List<string>();

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
        // Настройки повторных попыток
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 1000; // 1 секунда

        // Поля для управления состоянием
        private CancellationTokenSource _cts;
        private int _secondsRemaining;
        private bool _isClosed = false;
        private readonly TaskCompletionSource<TerminalResult> _tcs = new();
        private readonly int _totalSeconds;

        // Текущая попытка (для будущего отображения)
        private int _currentAttempt = 1;

        // ИСПРАВЛЕНО: Флаг для предотвращения многократного вызова CommandCompleted
        private bool _commandCompletedInvoked = false;

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
                // Используем метод с повторными попытками
                var result = await SendRequestWithRetryAsync(_cts.Token).ConfigureAwait(false);

                if (!_isClosed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result));

                    // ЖДЁМ ФАКТИЧЕСКОГО ЗАКРЫТИЯ ОКНА
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

        #region HTTP запрос с механизмом повторных попыток (RETRY)

        /// <summary>
        /// Выполняет запрос с автоматическими повторными попытками
        /// </summary>
        private async Task<TerminalResult> SendRequestWithRetryAsync(CancellationToken cancellationToken)
        {
            TerminalResult lastResult = null;
            var attemptErrors = new List<string>();

            // ИСПРАВЛЕНО: Рассчитываем таймаут для каждого запроса с учётом повторных попыток
            // Делим общее время на количество попыток + запас на задержки между попытками
            int singleRequestTimeout = Math.Max(10, (_totalSeconds - (MaxRetryAttempts - 1) * (RetryDelayMs / 1000)) / MaxRetryAttempts);

            // Создаём связанный CancellationToken с общим таймаутом
            using var overallTimeoutCts = new CancellationTokenSource(_totalSeconds * 1000);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, overallTimeoutCts.Token);

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                if (linkedCts.Token.IsCancellationRequested || _isClosed)
                {
                    return TerminalResult.CreateError("Операция отменена");
                }

                _currentAttempt = attempt;

                // Обновляем UI с информацией о текущей попытке (для будущего использования)
                UpdateAttemptDisplay(attempt, MaxRetryAttempts);

                try
                {
                    // ИСПРАВЛЕНО: Передаём рассчитанный таймаут для отдельного запроса
                    lastResult = await SendRequestInternal(linkedCts.Token, singleRequestTimeout).ConfigureAwait(false);

                    // ИСПРАВЛЕНО: Defensive coding - проверяем инициализацию списка
                    lastResult.AttemptErrors = attemptErrors ?? new List<string>();
                    lastResult.AttemptsCount = attempt;

                    // Если успешно - возвращаем результат немедленно
                    if (lastResult.IsSuccess)
                    {
                        // ИСПРАВЛЕНО: Вызываем CommandCompleted только при финальном успехе
                        InvokeCommandCompleted(lastResult);
                        return lastResult;
                    }

                    // Сохраняем ошибку для истории
                    if (attemptErrors != null)
                    {
                        attemptErrors.Add($"Попытка {attempt}: {lastResult.ErrorMessage}");
                    }

                    // Если это НЕ сетевая ошибка подключения - не повторяем
                    if (!IsRetryableError(lastResult))
                    {
                        // ИСПРАВЛЕНО: Вызываем CommandCompleted только при финальной ошибке (не повторяемой)
                        InvokeCommandCompleted(lastResult);
                        return lastResult;
                    }

                    // Если это не последняя попытка - ждём и пробуем снова
                    if (attempt < MaxRetryAttempts)
                    {
                        try
                        {
                            await Task.Delay(RetryDelayMs, linkedCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Общий таймаут истёк во время ожидания
                            return TerminalResult.CreateError("Превышено общее время ожидания");
                        }
                    }
                }
                catch (OperationCanceledException) when (overallTimeoutCts.Token.IsCancellationRequested)
                {
                    // Общий таймаут истёк
                    var timeoutResult = TerminalResult.CreateError(
                        $"Превышено общее время ожидания ({_totalSeconds} сек).\n\n" +
                        "Проверьте:\n• Терминал включен\n• Сетевое подключение стабильно\n• Терминал не занят другой операцией");
                    timeoutResult.AttemptsCount = attempt;
                    timeoutResult.AttemptErrors = attemptErrors ?? new List<string>();
                    return timeoutResult;
                }
                catch (OperationCanceledException)
                {
                    // Отмена пользователем
                    throw;
                }
                catch (Exception ex)
                {
                    // ИСПРАВЛЕНО: Defensive coding
                    attemptErrors ??= new List<string>();
                    attemptErrors.Add($"Попытка {attempt}: {ex.Message}");

                    // Создаем результат с ошибкой
                    lastResult = TerminalResult.CreateError(ex.Message);
                    lastResult.AttemptsCount = attempt;
                    lastResult.AttemptErrors = attemptErrors;

                    // Если это не последняя попытка - ждём и пробуем снова
                    if (attempt < MaxRetryAttempts)
                    {
                        try
                        {
                            await Task.Delay(RetryDelayMs, linkedCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            return TerminalResult.CreateError("Превышено общее время ожидания");
                        }
                    }
                }
            }

            // Все попытки исчерпаны - возвращаем последний результат
            if (lastResult != null)
            {
                // ИСПРАВЛЕНО: Вызываем CommandCompleted только после всех попыток
                InvokeCommandCompleted(lastResult);

                // Добавляем информацию о том, что были повторные попытки
                if (attemptErrors != null && attemptErrors.Count > 1)
                {
                    lastResult.ErrorMessage = $"После {MaxRetryAttempts} попыток:\n\n{lastResult.ErrorMessage}";
                }
            }
            else
            {
                lastResult = TerminalResult.CreateError("Все попытки подключения исчерпаны");
                lastResult.AttemptsCount = MaxRetryAttempts;
                lastResult.AttemptErrors = attemptErrors ?? new List<string>();
            }

            return lastResult;
        }

        /// <summary>
        /// ИСПРАВЛЕНО: Безопасный вызов CommandCompleted (только один раз)
        /// </summary>
        private void InvokeCommandCompleted(TerminalResult result)
        {
            if (_commandCompletedInvoked) return;
            _commandCompletedInvoked = true;

            if (commandResult != null)
            {
                CommandCompleted?.Invoke(result.IsSuccess, commandResult.AnswerTerminal);
            }
        }

        /// <summary>
        /// Определяет, стоит ли повторять запрос при данной ошибке
        /// </summary>
        private bool IsRetryableError(TerminalResult result)
        {
            if (result.IsSuccess) return false;

            string error = result.ErrorMessage?.ToLower() ?? "";
            string code = result.CodeResponse ?? "";

            // Ошибки, при которых НЕ стоит повторять (ошибки карты/банка)
            var nonRetryableCodes = new[] { "51", "54", "55", "14", "41", "43", "57", "61", "62", "63", "94" };
            foreach (var nonRetryableCode in nonRetryableCodes)
            {
                if (code == nonRetryableCode) return false;
            }

            var nonRetryablePhrases = new[]
            {
                "недостаточно средств",
                "неверный pin",
                "срок действия",
                "карта заблокирована",
                "карта в списке исключений",
                "дубликат операции"
            };

            foreach (var phrase in nonRetryablePhrases)
            {
                if (error.Contains(phrase)) return false;
            }

            // Сетевые ошибки и ошибки подключения - повторяем
            var retryablePhrases = new[]
            {
                "не удалось подключиться",
                "timeout",
                "превысил время",
                "не отвечает",
                "connection refused",
                "сетевое подключение",
                "связь с терминалом",
                "временно недоступен"
            };

            foreach (var phrase in retryablePhrases)
            {
                if (error.Contains(phrase)) return true;
            }

            // По умолчанию - повторяем при ошибке терминала (код ответа не "1")
            return !string.IsNullOrEmpty(code) && code != "1";
        }

        /// <summary>
        /// Обновляет отображение текущей попытки (для будущего использования)
        /// </summary>
        private void UpdateAttemptDisplay(int current, int max)
        {
            // Сейчас не показываем пользователю - это прозрачно
            // В будущем можно добавить отображение в UI:
            // Dispatcher.UIThread.Post(() =>
            // {
            //     if (StatusLabel != null)
            //     {
            //         StatusLabel.Text = $"Подключение к терминалу... (попытка {current}/{max})";
            //     }
            // });
        }

        #endregion

        #region HTTP запрос с понятными ошибками (LEGACY - HttpWebRequest)

        /// <summary>
        /// ИСПРАВЛЕНО: Добавлен параметр timeout для контроля времени отдельного запроса
        /// </summary>
        private async Task<TerminalResult> SendRequestInternal(CancellationToken cancellationToken, int timeoutSeconds)
        {
            HttpWebRequest request = null;
            WebResponse response = null;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.ContentType = "text/xml; charset=windows-1251";
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version10;

                // ИСПРАВЛЕНО: Используем переданный таймаут для отдельного запроса
                request.Timeout = timeoutSeconds * 1000;
                request.ReadWriteTimeout = timeoutSeconds * 1000;

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] byteArray = Encoding.GetEncoding("Windows-1251").GetBytes(Data);
                request.ContentLength = byteArray.Length;

                // ИСПРАВЛЕНО: Регистрируем отмену для асинхронного прерывания запроса
                var registration = cancellationToken.Register(() =>
                {
                    request?.Abort();
                });

                try
                {
                    using (var dataStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                    {
                        await dataStream.WriteAsync(byteArray, 0, byteArray.Length, cancellationToken).ConfigureAwait(false);
                    }

                    response = await request.GetResponseAsync().ConfigureAwait(false);

                    using var responseStream = response.GetResponseStream();
                    using var reader = new StreamReader(responseStream, Encoding.GetEncoding("Windows-1251"));
                    var responseContent = await reader.ReadToEndAsync().ConfigureAwait(false);

                    return ParseResponse(responseContent);
                }
                finally
                {
                    registration.Dispose();
                    response?.Dispose();
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
            catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return TerminalResult.CreateError("Запрос был отменён");
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

                // ИСПРАВЛЕНО: Не вызываем CommandCompleted здесь - будет вызван из SendRequestWithRetryAsync
                this.commandResult = commandResult;

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
                    LabelTimer.Text = "Осталось " + _secondsRemaining.ToString() + " сек.";

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

            // ВАЖНО: Закрываем окно и ЖДЁМ завершения
            Dispatcher.UIThread.Post(() =>
            {
                this.Tag = result.IsSuccess;
                this.Close();
                // Сигнализируем что окно действительно закрылось
                _windowClosedTcs.TrySetResult(true);
            });
        }

        // МЕТОД: Ждать фактического закрытия окна
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
        // Текущая попытка
        public int CurrentAttempt => _currentAttempt;

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
