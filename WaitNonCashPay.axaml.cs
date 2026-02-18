using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Cash8Avalon
{
    public partial class WaitNonCashPay : Window
    {
        private CancellationTokenSource _cts;
        private int _secondsRemaining = 80;
        private bool _isSuccessful = false;
        public event Action<bool, Pay.AnswerTerminal> CommandCompleted;
        public Cash_check cc;
        public string Url;
        public string Data;
        private CancellationTokenSource cts;
        private int timeout = 80;
        public CommandResult commandResult = null;

        // Событие для уведомления о завершении
        public event EventHandler<bool> PaymentCompleted;

        public WaitNonCashPay()
        {
            InitializeComponent(); // Теперь это будет работать
            StartTimer();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this.ShowInTaskbar = false;
            this.Opened += WaitNonCashPay_Opened;
        }

        private async void WaitNonCashPay_Opened(object? sender, EventArgs e)
        {
            await ActivateWindow(this);
        }

        private async Task ActivateWindow(Window window)
        {
            if (window == null) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (window.IsVisible)
                {
                    // Попытка активировать окно
                    window.Activate();
                    window.Focus();

                    // Для Linux - трюк с Topmost
                    if (OperatingSystem.IsLinux())
                    {
                        window.Topmost = true;
                        window.Topmost = false;
                        window.Topmost = true;
                    }
                }
            }, DispatcherPriority.Render);

            // Дайте оконному менеджеру время отреагировать
            if (OperatingSystem.IsLinux())
            {
                await Task.Delay(100); // 100 мс для надежности
            }
            else
            {
                await Task.Delay(10); // Для Windows достаточно
            }
        }

        public WaitNonCashPay(int timeoutSeconds) : this()
        {
            _secondsRemaining = timeoutSeconds;
            ProgressBarNonCashPay.Maximum = timeoutSeconds;
            ProgressBarNonCashPay.Value = timeoutSeconds;
            LabelTimer.Text = timeoutSeconds.ToString();
            timeout = timeoutSeconds;
        }

        public class CommandResult
        {
            public bool Status { get; set; }
            public Pay.AnswerTerminal AnswerTerminal { get; set; } = new Pay.AnswerTerminal();
        }

        /// <summary>
        /// Отправляет команду в эквайринг
        /// терминал и возвращает результат
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="Data"></param>        
        public async Task<CommandResult> send_command_acquiring_terminal(string Url, string Data, CancellationToken cancellationToken)
        {
            CommandResult result = new CommandResult();

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.ContentType = "text/xml; charset=windows-1251";
                byte[] byteArray = Encoding.GetEncoding("Windows-1251").GetBytes(Data);
                request.ContentLength = byteArray.Length;

                using (Stream dataStream = await request.GetRequestStreamAsync())
                {
                    await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                }

                using (WebResponse response = await request.GetResponseAsync())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("Windows-1251")))
                        {
                            string responseContent = await reader.ReadToEndAsync();

                            XmlSerializer serializer = new XmlSerializer(typeof(Response));
                            using (StringReader stringReader = new StringReader(responseContent))
                            {
                                var test = (Response)serializer.Deserialize(stringReader);
                                foreach (Field field in test.Field)
                                {
                                    if (field.Id == "39")
                                    {
                                        result.AnswerTerminal.сode_response_in_39_field = field.Text;
                                        result.Status = field.Text.Trim() == "1";
                                    }
                                    else if (field.Id == "13")
                                    {
                                        result.AnswerTerminal.code_authorization = field.Text.Trim();
                                    }
                                    else if (field.Id == "14")
                                    {
                                        result.AnswerTerminal.number_reference = field.Text.Trim();
                                    }
                                    else if (field.Id == "15")
                                    {
                                        result.AnswerTerminal.сode_response_in_15_field = field.Text.Trim();
                                    }
                                    else if (field.Id == "90")
                                    {
                                        cc.recharge_note = field.Text.Trim();
                                        int num_pos = cc.recharge_note.IndexOf("(КАССИР)");
                                        if (num_pos > 0)
                                        {
                                            cc.recharge_note = cc.recharge_note.Substring(0, num_pos + 8);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // ИСПРАВЛЕНИЕ 1: Вызываем событие перед закрытием окна
                CommandCompleted?.Invoke(result.Status, result.AnswerTerminal);

                // ИСПРАВЛЕНИЕ 2: Закрываем окно после вызова события
                await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(result.Status));
            }
            catch (WebException ex)
            {
                result.Status = false;
                result.AnswerTerminal.error = true;

                // ИСПРАВЛЕНИЕ 3: Сначала показываем сообщение
                await MessageBoxHelper.Show($"Ошибка при оплате по карте: {ex.Message}",
                    "Оплата по терминалу",
                    MessageBoxButton.OK,
                    MessageBoxType.Error,
                    this);

                // ИСПРАВЛЕНИЕ 4: Потом вызываем событие
                CommandCompleted?.Invoke(false, result.AnswerTerminal);

                // ИСПРАВЛЕНИЕ 5: Потом закрываем окно
                await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(false));
            }
            catch (Exception ex)
            {
                result.Status = false;
                result.AnswerTerminal.error = true;

                await MessageBoxHelper.Show($"Ошибка при оплате по карте: {ex.Message}",
                    "Оплата по терминалу",
                    MessageBoxButton.OK,
                    MessageBoxType.Error,
                    this);

                // ИСПРАВЛЕНИЕ 6: Вызываем событие
                CommandCompleted?.Invoke(false, result.AnswerTerminal);

                // ИСПРАВЛЕНИЕ 7: Закрываем окно
                await Dispatcher.UIThread.InvokeAsync(() => CloseWithResult(false));
            }

            return result; // Теперь это возвращается ПОСЛЕ всех операций
        }

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

        private async void StartTimer()
        {
            _cts = new CancellationTokenSource();

            try
            {
                while (_secondsRemaining >= 0)
                {
                    // Обновляем UI
                    UpdateTimerDisplay();

                    if (_cts.Token.IsCancellationRequested)
                        break;

                    // Ждем 1 секунду
                    await Task.Delay(1000, _cts.Token);
                    _secondsRemaining--;
                }

                // Если время вышло
                if (_secondsRemaining < 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusLabel.Text = "Время ожидания истекло!";
                        StatusLabel.Foreground = Brushes.Red;
                        CancelButton.Content = "Закрыть";
                    });

                    // ИСПРАВЛЕНИЕ 8: Вызываем событие при таймауте
                    CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });

                    PaymentCompleted?.Invoke(this, false);
                    CloseWithResult(false);
                }
            }
            catch (TaskCanceledException)
            {
                // Отменено пользователем
                CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });
                CloseWithResult(false);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusLabel.Text = $"Ошибка: {ex.Message}";
                    StatusLabel.Foreground = Brushes.Red;
                });

                CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });
                CloseWithResult(false);
            }
        }

        private void UpdateTimerDisplay()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressBarNonCashPay.Value = _secondsRemaining;
                LabelTimer.Text = _secondsRemaining.ToString();
                MessageLabel.Text = $"Ожидание ответа от терминала\nОсталось: {_secondsRemaining} секунд";
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();

            // ИСПРАВЛЕНИЕ 9: Вызываем событие при отмене
            CommandCompleted?.Invoke(false, new Pay.AnswerTerminal { error = true });

            CloseWithResult(false);
        }

        // Метод для закрытия с результатом (аналог DialogResult)
        private void CloseWithResult(bool? result)
        {
            // Устанавливаем результат в Tag окна
            this.Tag = result;

            // Закрываем окно
            this.Close();

            // Вызываем событие
            PaymentCompleted?.Invoke(this, result == true);
        }

        // Свойства для доступа извне
        public bool IsTimeout => _secondsRemaining <= 0;
        public int SecondsRemaining => _secondsRemaining;
        public bool IsSuccessful => _isSuccessful;

        // Статический метод для показа диалога
        public static async Task<bool> ShowDialogAsync(Window owner = null, int timeoutSeconds = 80)
        {
            var dialog = new WaitNonCashPay(timeoutSeconds);
            var tcs = new TaskCompletionSource<bool>();

            dialog.PaymentCompleted += (s, result) => tcs.TrySetResult(result);

            if (owner != null)
                await dialog.ShowDialog(owner);
            else
                dialog.Show();

            return await tcs.Task;
        }

        // Метод для запуска команды с ожиданием
        public async Task<CommandResult> SendCommandWithTimeout(string url, string data, Cash_check cashCheck)
        {
            this.Url = url;
            this.Data = data;
            this.cc = cashCheck;

            // Запускаем отправку команды в фоне
            var sendTask = send_command_acquiring_terminal(url, data, _cts?.Token ?? CancellationToken.None);

            // Создаем TaskCompletionSource для ожидания завершения
            var tcs = new TaskCompletionSource<CommandResult>();

            // Подписываемся на событие завершения
            this.CommandCompleted += (success, answer) =>
            {
                var result = new CommandResult
                {
                    Status = success,
                    AnswerTerminal = answer ?? new Pay.AnswerTerminal()
                };
                tcs.TrySetResult(result);
            };

            // Показываем окно
            this.Show();

            // Ждем завершения
            return await tcs.Task;
        }
    }
}