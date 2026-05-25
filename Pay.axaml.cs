using Atol.Drivers10.Fptr;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AtolConstants = Atol.Drivers10.Fptr.Constants;

namespace Cash8Avalon
{
    public partial class Pay : Window
    {
        private DataTable _certificatesData = null;
        private List<InputSertificates.CertificateItem> _certificatesList = new List<InputSertificates.CertificateItem>();

        public event EventHandler ReturnToDocumentRequested;
        public event EventHandler PaymentConfirmed;
        public event EventHandler<bool> SbpPaymentChanged;

        private bool _firstInput = true;
        private bool firs_input_non_cash = true;

        public bool code_it_is_confirmed = false;
        private bool complete = false;
        private bool _isShowingCertificatesDialog = false;

        // Строки шаблонов XML
        private string str_command_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field></request>";
        string str_return_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">number_reference</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field></request>";
        string str_cancel_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""01"">sale_non_cash_money</field><field id=""04"">643</field><field id = ""25"">4</field><field id=""27"">id_terminal</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">number_reference</field></request>";
        string str_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""14"">guid</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field><field id=""53"">115</field></request>";
        string str_payment_status_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field><field id=""53"">117</field></request>";
        string str_return_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">guid</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field><field id=""53"">118</field></request>";
        string str_payment_status_return_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">guid</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field><field id=""53"">119</field></request>";

        public Cash_check cc = null;
        //TextBox cashSumTextBox = null;

        // ═══════════════════════════════════════════════
        //  КЭШИРОВАННЫЕ ССЫЛКИ НА КОНТРОЛЫ
        // ═══════════════════════════════════════════════
        private TextBox _paySumTextBox;
        private TextBox _cashSumTextBox;
        private TextBox _nonCashSumTextBox;        
        private TextBox _nonCashSumKopTextBox;
        private TextBox _sertificatesSumTextBox;
        private TextBox _bonusSumTextBox;       // pay_bonus
        private TextBox _bonusManyTextBox;      // pay_bonus_many
        private TextBox _remainderTextBox;
        private CheckBox _checkBoxPaymentBySbp;
        private CheckBox _checkBoxDoNotSendPaymentToTheTerminal;
        private Button _buttonPay;
        private Button _button1;


        public Pay()
        {
            InitializeComponent();
            LoadControls();
            this.ShowInTaskbar = false;
            this.Loaded += Pay_Loaded;
            this.Opened += Pay_Opened;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public void  LoadControls()
        {            
            _paySumTextBox = this.FindControl<TextBox>("pay_sum");
            _cashSumTextBox = this.FindControl<TextBox>("txtB_cash_sum");
            _nonCashSumTextBox = this.FindControl<TextBox>("non_cash_sum");
            _nonCashSumKopTextBox = this.FindControl<TextBox>("non_cash_sum_kop");
            _sertificatesSumTextBox = this.FindControl<TextBox>("sertificates_sum");
            _bonusSumTextBox = this.FindControl<TextBox>("pay_bonus");
            _bonusManyTextBox = this.FindControl<TextBox>("pay_bonus_many");
            _remainderTextBox = this.FindControl<TextBox>("remainder");
            _checkBoxPaymentBySbp = this.FindControl<CheckBox>("checkBox_payment_by_sbp");
            _checkBoxDoNotSendPaymentToTheTerminal = this.FindControl<CheckBox>("checkBox_do_not_send_payment_to_the_terminal");
            _buttonPay = this.FindControl<Button>("button_pay");
            _button1 = this.FindControl<Button>("button1");
        }

        private void Pay_Loaded(object? sender, RoutedEventArgs e)
        {
            this._bonusManyTextBox.Text = "0";
            this._bonusSumTextBox.Text = "0";
            this._sertificatesSumTextBox.Text = "0";
            this._nonCashSumTextBox.Text = "0";
            this._nonCashSumKopTextBox.Text = "0";

            InitializeEventHandlers();

            if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != ""))
            {
                if (MainStaticClass.GetAcquiringBank == 1)//ВТБ
                {
                    if (_checkBoxPaymentBySbp != null)
                    {
                        _checkBoxPaymentBySbp.Opacity = 1;
                        _checkBoxPaymentBySbp.IsHitTestVisible = true;
                    }
                }
                _checkBoxDoNotSendPaymentToTheTerminal.Opacity = 1;
                _checkBoxDoNotSendPaymentToTheTerminal.IsHitTestVisible = true;
            }

            if (cc.payment_by_sbp_sales)
            {
                _checkBoxPaymentBySbp.IsChecked = true;
            }

            var toolTipContent = new StackPanel
            {
                Spacing = 5,
                Children =
                {
                    new TextBlock { Text = "Если оплата по терминалу для этого чека уже прошла", FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap, MaxWidth = 250 },
                    new TextBlock { Text = "Не отправлять запрос об оплате на терминал", TextWrapping = TextWrapping.Wrap, MaxWidth = 250 }
                }
            };
            ToolTip.SetTip(_checkBoxDoNotSendPaymentToTheTerminal, toolTipContent);
            CalculateChange();
        }

        private async void Pay_Opened(object? sender, EventArgs e)
        {
            this.Topmost = true;
            await Task.Delay(100);
            this.Topmost = false;
            this.Activate();

            if (_cashSumTextBox != null)
            {
                _cashSumTextBox.Focus();
                if (_cashSumTextBox is TextBox tb)
                {
                    string text = tb.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        int dotIndex = text.IndexOf('.');
                        int commaIndex = text.IndexOf(',');
                        int separatorIndex = -1;
                        if (dotIndex != -1 && commaIndex != -1) separatorIndex = Math.Min(dotIndex, commaIndex);
                        else if (dotIndex != -1) separatorIndex = dotIndex;
                        else separatorIndex = commaIndex;

                        if (separatorIndex > 0)
                        {
                            tb.SelectionStart = 0;
                            tb.SelectionEnd = separatorIndex;
                        }
                        else tb.SelectAll();
                    }
                }
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void InitializeEventHandlers()
        {
            this.KeyDown += Pay_KeyDown;

            //var checkBoxPaymentBySbp = this.FindControl<CheckBox>("checkBox_payment_by_sbp");
            if (_checkBoxPaymentBySbp != null) _checkBoxPaymentBySbp.IsCheckedChanged += CheckBox_payment_by_sbp_CheckedChanged;

            this._buttonPay.Click += Button_pay_Click;
            this._button1.Click += Button1_Click;

            //cashSumTextBox = this.FindControl<TextBox>("txtB_cash_sum");
            if (_cashSumTextBox != null)
            {
                _cashSumTextBox.TextChanged += CashSumTextBox_TextChanged;
                _cashSumTextBox.GotFocus += OnCashSumGotFocus;
                _cashSumTextBox.LostFocus += OnCashSumLostFocus;
                _cashSumTextBox.TextInput += OnCashSumTextInput;
                _cashSumTextBox.KeyDown += OnCashSumKeyDown;
                _cashSumTextBox.KeyUp += CashSumTextBox_KeyUp;
                _cashSumTextBox.Text = "0,00";
            }

            //var nonCashSumTextBox = this.FindControl<TextBox>("non_cash_sum");
            if (_nonCashSumTextBox != null)
            {
                _nonCashSumTextBox.KeyDown += NonCashSumTextBox_KeyDown;
                _nonCashSumTextBox.LostFocus += OnNonCashSumLostFocus;
                _nonCashSumTextBox.TextChanged += NonCashSumTextBox_TextChanged;
                _nonCashSumTextBox.Text = "0";
            }
        }

        #region Обработчики ввода

        private void NonCashSumTextBox_TextChanged(object? sender, TextChangedEventArgs e) => CalculateChange();
        private void OnNonCashSumLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;
            if (string.IsNullOrWhiteSpace(textBox.Text)) textBox.Text = "0";
            else if (!int.TryParse(textBox.Text, out _)) textBox.Text = "0";
            CalculateChange();
        }
        private void NonCashSumTextBox_KeyUp(object? sender, KeyEventArgs e) => CalculateChange();
        private void CashSumTextBox_KeyUp(object? sender, KeyEventArgs e) => CalculateChange();

        private void OnNonCashSumKopLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;
            if (string.IsNullOrWhiteSpace(textBox.Text)) textBox.Text = "00";
            Dispatcher.UIThread.Post(() => CalculateChange(), DispatcherPriority.Background);
        }
        private void NonCashSumKopTextBox_KeyUp(object? sender, KeyEventArgs e) => Dispatcher.UIThread.Post(() => CalculateChange(), DispatcherPriority.Background);

        private void NonCashSumTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            Dispatcher.UIThread.Post(() => CalculateChange(), DispatcherPriority.Background);
            var textBox = sender as TextBox;
            if (textBox == null) return;
            if (e.Key == Key.Y || e.Key == Key.R || e.Key == Key.F5 || e.Key == Key.F12 || e.Key == Key.F8) return;

            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isControl = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.Escape;
            bool isSeparator = e.Key == Key.OemComma || e.Key == Key.OemPeriod;

            if (!isNumeric && !isControl && !isSeparator) { e.Handled = true; return; }

            if (isNumeric)
            {
                e.Handled = true;
                var currentText = textBox.Text ?? "";
                var selectionStart = textBox.CaretIndex;
                char digit = GetDigitFromKey(e.Key);
                if (currentText == "0" || string.IsNullOrEmpty(currentText)) { textBox.Text = digit.ToString(); textBox.CaretIndex = 1; }
                else { textBox.Text = currentText.Insert(selectionStart, digit.ToString()); textBox.CaretIndex = selectionStart + 1; }
            }
            if (isSeparator) { e.Handled = true; return; }
            Dispatcher.UIThread.Post(() => CalculateChange(), DispatcherPriority.Background);
        }
        private char GetDigitFromKey(Key key)
        {
            // ИСПРАВЛЕНО: key.D9 != 0 заменено на правильное сравнение
            if (key >= Key.D0 && key <= Key.D9) return (char)('0' + (key - Key.D0));
            else if (key >= Key.NumPad0 && key <= Key.NumPad9) return (char)('0' + (key - Key.NumPad0));
            return '0';
        }
        private async void Button1_Click(object? sender, RoutedEventArgs e)
        {
            if (cc.check_type.SelectedIndex == 0) await MessageBoxHelper.Show("Список введённых подарков будет очищен...", "Уведомление по акциям", MessageBoxButton.OK, MessageBoxType.Info, this);
            ClearCertificates();
            cc.cancel_action();
            this.Tag = false;
            this.Close();
        }
        private void Button_pay_Click(object? sender, RoutedEventArgs e) => button2_Click(null, null);
        private void OnCashSumGotFocus(object sender, GotFocusEventArgs e) { if (_cashSumTextBox?.Text == "0,00") _firstInput = true; }
        private void OnCashSumLostFocus(object sender, RoutedEventArgs e)
        {
            if (_cashSumTextBox == null) return;
            if (string.IsNullOrWhiteSpace(_cashSumTextBox.Text)) { _cashSumTextBox.Text = "0,00"; _firstInput = true; }
            else { if (decimal.TryParse(_cashSumTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) _cashSumTextBox.Text = result.ToString("F2"); else _cashSumTextBox.Text = "0,00"; _firstInput = true; }
            CalculateChange();
        }
        private void OnCashSumTextInput(object sender, TextInputEventArgs e)
        {
            if (_cashSumTextBox == null) return;
            if (string.IsNullOrEmpty(e.Text)) { e.Handled = true; return; }
            char inputChar = e.Text[0];
            bool isDigit = char.IsDigit(inputChar);
            bool isSeparator = inputChar == ',' || inputChar == '.';
            if (!isDigit && !isSeparator && !char.IsControl(inputChar)) { e.Handled = true; return; }

            var selectionStart = _cashSumTextBox.CaretIndex;
            var currentText = _cashSumTextBox.Text ?? "";

            if (isDigit)
            {
                if (_firstInput) { _firstInput = false; _cashSumTextBox.Text = inputChar + currentText.Substring(1); e.Handled = true; _cashSumTextBox.CaretIndex = 1; }
                else { _cashSumTextBox.Text = currentText.Insert(selectionStart, inputChar.ToString()); e.Handled = true; _cashSumTextBox.CaretIndex = selectionStart + 1; }
            }
            else if (isSeparator)
            {
                string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                if (!currentText.Contains(separator)) { _cashSumTextBox.Text = currentText.Insert(selectionStart, separator); e.Handled = true; _cashSumTextBox.CaretIndex = selectionStart + 1; }
                else { _cashSumTextBox.CaretIndex = currentText.IndexOf(separator) + 1; e.Handled = true; }
            }

            var separatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (_cashSumTextBox.Text.Contains(separatorChar))
            {
                int decimalIndex = _cashSumTextBox.Text.IndexOf(separatorChar);
                if (_cashSumTextBox.Text.Length - decimalIndex - 1 < 2) { _cashSumTextBox.Text = _cashSumTextBox.Text.Substring(0, decimalIndex + 1) + _cashSumTextBox.Text.Substring(decimalIndex + 1).PadRight(2, '0'); 
                    _cashSumTextBox.CaretIndex = decimalIndex + 1; }
            }
            if (_cashSumTextBox.CaretIndex == 0) _cashSumTextBox.CaretIndex = _cashSumTextBox.Text.Length;
            CalculateChange();
        }
        private void OnCashSumKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete) { _firstInput = false; Task.Delay(10).ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => { if (_cashSumTextBox != null) FormatCashSumText(); })); }
            CalculateChange();
        }
        private void CashSumTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (_cashSumTextBox == null) return; FormatCashSumText(); CalculateChange(); }
        private void FormatCashSumText()
        {
            if (_cashSumTextBox == null) return;
            var currentText = _cashSumTextBox.Text;
            if (!string.IsNullOrEmpty(currentText))
            {
                var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                var cleanedText = new string(currentText.Where(c => char.IsDigit(c) || c == separator[0]).ToArray());
                int separatorCount = cleanedText.Count(c => c == separator[0]);
                if (separatorCount > 1) { int firstIndex = cleanedText.IndexOf(separator[0]); cleanedText = cleanedText.Substring(0, firstIndex + 1) + new string(cleanedText.Substring(firstIndex + 1).Where(char.IsDigit).ToArray()); }
                if (cleanedText != currentText) { _cashSumTextBox.Text = cleanedText; _cashSumTextBox.CaretIndex = cleanedText.Length; }
            }
            var separatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (_cashSumTextBox.Text.Contains(separatorChar))
            {
                int decimalIndex = _cashSumTextBox.Text.IndexOf(separatorChar); string text = _cashSumTextBox.Text;
                if (text.Length - decimalIndex - 1 < 2) _cashSumTextBox.Text = text.Substring(0, decimalIndex + 1) + text.Substring(decimalIndex + 1).PadRight(2, '0');
                else if (text.Length - decimalIndex - 1 > 2) _cashSumTextBox.Text = text.Substring(0, decimalIndex + 3);
            }
        }
        #endregion

        private async void Pay_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5: e.Handled = true; Button1_Click(sender, e); break;
                case Key.F12: e.Handled = true; button2_Click(null, null); break;
                case Key.Y: e.Handled = true; this.CashSum = this.PaySum; ClearNonCash(); _cashSumTextBox?.Focus(); break;
                //case Key.R: e.Handled = true; FillNonCashFromPaySum(); ClearCash(); this.FindControl<TextBox>("non_cash_sum")?.Focus(); break;
                case Key.R: e.Handled = true; FillNonCashFromPaySum(); ClearCash(); _nonCashSumTextBox?.Focus(); break;
                case Key.F8: e.Handled = true; await ShowCertificatesDialog(); break;
            }
        }

        private async Task ShowCertificatesDialog()
        {
            // 1. Защита от двойного клика
            if (_isShowingCertificatesDialog)
            {
                Console.WriteLine("⚠ Диалог сертификатов уже открыт, пропуск повторного вызова.");
                return;
            }

            _isShowingCertificatesDialog = true; // Взводим флаг

            try
            {
                var inputSertificates = new InputSertificates();
                if (_certificatesList.Count > 0) inputSertificates.LoadExistingCertificates(_certificatesList);
                inputSertificates.Topmost = true;

                await inputSertificates.ShowDialog<List<InputSertificates.CertificateItem>>(this);

                var updatedCertificates = inputSertificates.Tag as List<InputSertificates.CertificateItem>;
                if (updatedCertificates != null)
                {
                    await ProcessCertificatesData(updatedCertificates);
                }
            }
            catch (Exception ex)
            {
                await MessageBoxHelper.Show($"Ошибка открытия формы сертификатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
            }
            finally
            {
                // 2. Гарантированно сбрасываем флаг и восстанавливаем фокус
                _isShowingCertificatesDialog = false;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Focus();
                    this.Activate();
                    _cashSumTextBox?.Focus();
                    this.Topmost = true;
                }, DispatcherPriority.Render);
            }
        }

        private async Task ProcessCertificatesData(object certificateData)
        {
            if (certificateData == null) return;
            try
            {
                if (certificateData is List<InputSertificates.CertificateItem> certificates)
                {
                    if (certificates.Count > 0)
                    {
                        _certificatesList = certificates;
                        decimal totalAmount = certificates.Sum(c => c.Amount);
                        this.CertificatesSum = totalAmount.ToString("F2");
                        CalculateChange();
                        MainStaticClass.write_event_in_log($"Добавлено {certificates.Count} сертификатов на сумму {totalAmount:F2}", "Сертификаты", cc?.numdoc.ToString() ?? "0");
                    }
                    else ClearCertificates();
                }
                else ClearCertificates();
            }
            catch (Exception ex)
            {
                await MessageBoxHelper.Show($"Ошибка обработки сертификатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                ClearCertificates();
            }
        }

        private void ClearCertificates() { _certificatesList.Clear(); _certificatesData?.Clear(); this.CertificatesSum = "0,00"; CalculateChange(); }
        public decimal GetCertificatesTotal() => _certificatesList.Sum(c => c.Amount);
        public int GetCertificatesCount() => _certificatesList.Count;

        private void FillNonCashFromPaySum()
        {
            if (decimal.TryParse(this.PaySum.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal paySum))
            {
                paySum = Math.Round(paySum, 2, MidpointRounding.AwayFromZero);
                int rubles = (int)Math.Floor(paySum);
                int kopecks = (int)((paySum - rubles) * 100);
                this.NonCashSum = rubles.ToString();
                this.NonCashSumKop = kopecks.ToString("00");
            }
            CalculateChange();
        }
        private void ClearNonCash() { this.NonCashSum = "0"; this.NonCashSumKop = "00"; CalculateChange(); }
        private void ClearCash() { this.CashSum = "0,00"; CalculateChange(); }

        private async Task<bool> copFilledCorrectly()
        {
            if (string.IsNullOrWhiteSpace(_nonCashSumTextBox.Text))
            {
                await MessageBoxHelper.Show("У вас пустое поле оплата по карте. Сделайте фото и создайте заявку в ит отдел.", "Проверки при оплате картой", MessageBoxButton.OK, MessageBoxType.Error, this);
                return false;
            }
            if (_nonCashSumTextBox.Text.Trim().Length > 0)
            {
                if (int.TryParse(_nonCashSumTextBox.Text.Trim(), out int rubles) && rubles == 0)
                {
                    if (short.TryParse(_nonCashSumKopTextBox.Text.Trim(), out short kopecks) && kopecks > 0)
                    {
                        MessageBoxResult dialogResult = await MessageBoxHelper.Show("У вас заполнены копейки для оплаты по карте, но не заполнена целая часть суммы оплаты по карте.\n\nЕсли вы выберете ДА, тогда копейки будут оплачены по карте.\nЕсли вы выберете НЕТ, то копейки обнулятся.", "Проверки при оплате картой", MessageBoxButton.YesNo, MessageBoxType.Question, this);
                        if (dialogResult == MessageBoxResult.No) { _nonCashSumKopTextBox.Text = "0"; return false; }
                    }
                }
            }
            return true;
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainStaticClass.write_event_in_log($"[Pay.button2_Click] Start. ButtonEnabled: {this._buttonPay.IsEnabled}", "PayWindow", cc?.numdoc.ToString() ?? "0");

                if (!this._buttonPay.IsEnabled)
                {
                    MainStaticClass.write_event_in_log("[Pay.button2_Click] Button disabled, exiting.", "PayWindow", cc?.numdoc.ToString() ?? "0");
                    return;
                }

                // Проверка ввода копеек
                if (!await copFilledCorrectly()) { CalculateChange(); return; }

                // Проверка ссылки на чек
                if (cc == null)
                {
                    MainStaticClass.write_event_in_log($"[Pay.button2_Click] CRITICAL: cc (Cash_check) is NULL!", "PayWindow", "0");
                    await MessageBoxHelper.Show("Внутренняя ошибка: не передана ссылка на чек.", "Ошибка данных", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return;
                }

                // 2. Проверки бизнес-логики
                if (!await ValidateInputs()) return;

                // 3. Подготовка данных
                cc.SetCertificatesFromPay(_certificatesList);
                MainStaticClass.write_event_in_log("Окно оплаты: переход к оплате", "Документ чек", cc.numdoc.ToString());

                // 4. Запуск процесса оплаты
                await it_is_paid();
            }
            catch (Exception ex)
            {
                //MainStaticClass.write_event_in_log($"CRITICAL ERROR button2_Click: {ex.Message}\nStackTrace: {ex.StackTrace}", "PayWindow", cc?.numdoc.ToString() ?? "0");
                //await MessageBoxHelper.Show($"Произошла ошибка при попытке оплаты:\n{ex.Message}\n\nПопробуйте отменить операцию на терминале.", "Сбой программы", MessageBoxButton.OK, MessageBoxType.Error, this);
                //CalculateChange();
                // ✅ Используем перегрузку с Exception — она запишет в БД полный JSON
                // с типом исключения (например, NullReferenceException), стек-трейсом и InnerException.
                // В description передаём контекст: номер чека (сколько смогли достать)
                MainStaticClass.WriteRecordErrorLog(
                    ex,
                    cc?.numdoc ?? 0,
                    MainStaticClass.CashDeskNumber,
                    "Pay.button2_Click");

                // ✅ Простое и понятное сообщение кассиру, без домыслов про терминал
                await MessageBoxHelper.Show(
                    $"Произошла ошибка при попытке оплаты:\n{ex.Message}",
                    "Сбой программы", MessageBoxButton.OK, MessageBoxType.Error, this);

                CalculateChange();
            }
        }

        private async Task<bool> ValidateInputs()
        {
            // Вспомогательная функция безопасного парсинга
            double Parse(string text)
            {
                if (string.IsNullOrWhiteSpace(text)) return 0.0;
                if (double.TryParse(text, out double res)) return res;
                if (double.TryParse(text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out res)) return res;
                return 0.0;
            }

            if (cc == null)
            {
                MainStaticClass.write_event_in_log($"[Pay.ValidateInputs] Error: cc is null", "PayWindow", "0");
                return false;
            }

            // ИСПОЛЬЗУЕМ СВОЙСТВА (Properties)
            string paySumStr = this.PaySum;
            string changeStr = this.Remainder;
            string certSumStr = this.CertificatesSum;
            string bonusManyStr = this.BonusMany;
            string cashSumStr = this.CashSum;

            // Парсим значения
            double cash_money = Math.Round(Parse(cashSumStr), 2);
            double non_cash_money = Math.Round(get_non_cash_sum(), 2);
            double sertificate_money = Math.Round(Parse(certSumStr), 2);
            double bonus_money = Math.Round(Parse(bonusManyStr), 2);
            double sum_on_document = Math.Round(Parse(paySumStr), 2);
            double remainderVal = Parse(changeStr);

            double total_paid = cash_money + non_cash_money + sertificate_money + bonus_money;

            if (total_paid < sum_on_document)
            {
                await MessageBoxHelper.Show("Проверьте сумму внесенной оплаты\r\n оплат внесено"+ total_paid.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                return false;
            }

            if (remainderVal > 0 && cc.check_type.SelectedIndex != 0)
            {
                await MessageBoxHelper.Show(" Сумма возврата должна быть равна сумме оплаты ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                return false;
            }

            if (bonus_money > 0)
            {
                if (!string.IsNullOrEmpty(this.BonusSum)) this.BonusSum = "0";
                if (non_cash_money + sertificate_money + bonus_money > sum_on_document)
                {
                    await MessageBoxHelper.Show("Сумма сертификатов + карта + бонусы превышает сумму чека ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return false;
                }
            }
            else
            {
                if (non_cash_money + sertificate_money > sum_on_document)
                {
                    await MessageBoxHelper.Show(" Сумма сертификатов + карта превышает сумму чека ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return false;
                }
            }

            if ((MainStaticClass.GetWorkSchema == 1) || (MainStaticClass.GetWorkSchema == 3) || (MainStaticClass.GetWorkSchema == 4))
            {
                double cash_final = cash_money - remainderVal;
                double sum_doc_calc = Convert.ToDouble(cc.calculation_of_the_sum_of_the_document());

                if (Math.Round(sum_doc_calc, 2) != Math.Round((cash_final + non_cash_money + sertificate_money + bonus_money), 2))
                {
                    await MessageBoxHelper.Show(" Повторно внесите суммы оплаты, обнаружено не схождение в окне оплаты ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return false;
                }
            }

            if (cc.check_type.SelectedIndex == 1)
            {
                double cash_final = cash_money - remainderVal;
                if (!MainStaticClass.validate_cash_sum_non_cash_sum_on_return(cc.id_sale, cash_final, non_cash_money))
                {
                    return false;
                }
            }

            return true;
        }

        private string CalculateMoneyInKopecks(string rublesText, string kopecksText)
        {
            if (!long.TryParse(rublesText?.Trim(), out long rubles) || rubles < 0) rubles = 0;
            string kopRaw = kopecksText?.Trim() ?? "";
            if (!int.TryParse(new string(kopRaw.Where(char.IsDigit).Take(2).ToArray()), out int kopecks)) kopecks = 0;
            kopecks = Math.Max(0, Math.Min(99, kopecks));
            return (rubles * 100 + kopecks).ToString();
        }

        //private async Task it_is_paid()
        //{
        //    if (cc == null)
        //    {
        //        MainStaticClass.write_event_in_log($"[Pay.it_is_paid] CRITICAL: cc is null", "PayWindow", "0");
        //        return;
        //    }            

        //    if (cc.check_type.SelectedIndex == 0) // ОПЛАТА
        //    {
        //        decimal cashSumVal = 0, remainderVal = 0, paySumVal = 0;
        //        decimal.TryParse(this.CashSum, out cashSumVal);
        //        decimal.TryParse(this.Remainder, out remainderVal);
        //        decimal.TryParse(this.PaySum, out paySumVal);

        //        //if ((cashSumVal - remainderVal) < 0)
        //        //{
        //        //    await MessageBoxHelper.Show("Ошибка при определении суммы наличных", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
        //        //    return;
        //        //}

        //        decimal sertSum = 0, bonusSum = 0, nonCashSum = 0;
        //        decimal.TryParse(this.CertificatesSum, out sertSum);
        //        decimal.TryParse(this.BonusMany, out bonusSum);
        //        nonCashSum = Convert.ToDecimal(get_non_cash_sum());

        //        if (remainderVal < 0)
        //        {
        //            await MessageBoxHelper.Show(
        //                $"ОШИБКА РАСЧЁТА!\n\n" +
        //                $"Сумма чека: {paySumVal:F2} ₽\n" +
        //                $"Внесено наличными: {cashSumVal:F2} ₽\n" +
        //                $"Внесено по карте: {nonCashSum:F2} ₽\n" +
        //                $"Сертификатами: {sertSum:F2} ₽\n" +
        //                $"Бонусами: {bonusSum:F2} ₽\n" +
        //                $"Итого внесено: {(cashSumVal + nonCashSum + sertSum + bonusSum):F2} ₽\n\n" +
        //                $"НЕХВАТАЕТ: {Math.Abs(remainderVal):F2} ₽\n\n" +
        //                "Проверьте введённые суммы и попробуйте снова.",
        //                "Ошибка расчёта оплаты",
        //                MessageBoxButton.OK,
        //                MessageBoxType.Error,
        //                this);
        //            CalculateChange();
        //            return;
        //        }

        //        if (paySumVal - (cashSumVal - remainderVal + sertSum + bonusSum + nonCashSum) > 1)
        //        {
        //            await MessageBoxHelper.Show(" Неверно внесенные суммы ", "Проверка оплаты", MessageBoxButton.OK, MessageBoxType.Error, this);
        //            return;
        //        }

        //        if (!cc.ValidateCheckSumAtDiscount())
        //        {
        //            await MessageBoxHelper.Show(" При распределении расчетов получилась нулевая/отрицательная сумма в строке...", "Проверка суммы со скидкой", MessageBoxButton.OK, MessageBoxType.Error, this);
        //            return;
        //        }

        //        // === ПОДГОТОВКА ДАННЫХ ДЛЯ ЗАПИСИ ===
        //        string sum_cash_pay = (cashSumVal - remainderVal).ToString().Replace(",", ".");
        //        string non_sum_cash_pay = get_non_cash_sum().ToString().Replace(",", ".");
        //        string sertificate_money_str = sertSum.ToString().Replace(",", ".");
        //        string bonus_money_str = string.IsNullOrEmpty(this.BonusMany.Trim()) ? "0" : this.BonusMany.Trim();
        //        string sum_doc_str = cc.calculation_of_the_sum_of_the_document().ToString().Replace(",", ".");
        //        string remainder_str = this.Remainder.Replace(",", ".");


        //        cc.LastPaymentSnapshot = new PaymentSnapshot
        //        {
        //            CashMoney = cashSumVal - remainderVal,
        //            NonCashMoney = nonCashSum,
        //            CertificateMoney = sertSum,                    
        //            TotalSumAtDiscount = paySumVal,
        //            CreatedAt = DateTime.Now
        //        };
        //        // Записываем лог для отладки
        //        MainStaticClass.write_event_in_log($"[Pay] Snapshot created: {cc.LastPaymentSnapshot}", "PaymentSnapshot", cc.numdoc.ToString());

        //        ////=== ШАГ 1: Предварительная запись в БД ===
        //        //bool writeResult = await cc.write_new_document(
        //        //    this.CashSum, // Используем свойство
        //        //    sum_doc_str,
        //        //    remainder_str,
        //        //    bonus_money_str,
        //        //    false,
        //        //    sum_cash_pay,
        //        //    non_sum_cash_pay,
        //        //    sertificate_money_str,
        //        //    "0",
        //        //    false
        //        //);

        //        //if (!writeResult)
        //        //{
        //        //    await MessageBoxHelper.Show("Не удалось сохранить документ. Оплата отменена.", "Ошибка записи", MessageBoxButton.OK, MessageBoxType.Error, this);
        //        //    return;
        //        //}

        //        double notCashSum = get_non_cash_sum();

        //        if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != "") && notCashSum > 0)
        //        {
        //            //if (checkBox_do_not_send_payment_to_the_terminal.IsChecked != true)                    
        //                if (_checkBoxDoNotSendPaymentToTheTerminal.IsChecked != true)
        //                {
        //                // ИСПОЛЬЗУЕМ СВОЙСТВА
        //                string money = CalculateMoneyInKopecks(this.NonCashSum, this.NonCashSumKop);

        //                if (MainStaticClass.GetAcquiringBank == 1) //ВТБ
        //                {
        //                    string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;

        //                    //if (checkBox_payment_by_sbp.IsChecked != true)
        //                        if (_checkBoxPaymentBySbp?.IsChecked != true)

        //                        {
        //                        #region Обычная оплата картой
        //                        string _str_command_sale_ = str_command_sale.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
        //                        MainStaticClass.write_event_in_log($"Оплата картой: {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
        //                        var terminalResult = await WaitNonCashPay.ShowAndWaitAsync(this, 80, url, _str_command_sale_, this.cc);

        //                        if (!terminalResult.IsSuccess)
        //                        {
        //                            CalculateChange(); cc.recharge_note = "";
        //                            await MessageBoxHelper.Show(terminalResult.ErrorMessage, "Оплата по терминалу", MessageBoxButton.OK, MessageBoxType.Error, this);
        //                            return;
        //                        }
        //                        cc.code_authorization_terminal = terminalResult.AuthorizationCode;
        //                        cc.id_transaction_terminal = terminalResult.ReferenceNumber;
        //                        cc.recharge_note = terminalResult.RechargeNote;
        //                        #endregion
        //                    }
        //                    else
        //                    {
        //                        #region Оплата СБП
        //                        string _str_sale_sbp = str_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("guid", cc.guid);
        //                        MainStaticClass.write_event_in_log($"Оплата СБП (Init): {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
        //                        var resultSbp = await WaitNonCashPay.SendRequestAsync(url, _str_sale_sbp);
        //                        TerminalResult finalResult = resultSbp;

        //                        if (!resultSbp.IsSuccess)
        //                        {
        //                            string _str_payment_status = str_payment_status_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.guid);
        //                            var (success, pollResult) = await PollSbpStatusAsync(url, _str_payment_status, "Оплата СБП");
        //                            if (!success) { CalculateChange(); cc.recharge_note = ""; return; }
        //                            finalResult = pollResult;
        //                        }
        //                        cc.code_authorization_terminal = finalResult.AuthorizationCode;
        //                        cc.id_transaction_terminal = finalResult.ReferenceNumber;
        //                        cc.payment_by_sbp = true;
        //                        #endregion
        //                    }
        //                }
        //                else if (MainStaticClass.GetAcquiringBank == 2) // СБЕР
        //                {
        //                    var sberService = new SberPaymentService();
        //                    if (int.TryParse(money, out int amountKopecks))
        //                    {
        //                        Func<CancellationToken, Task<TerminalResult>> sberp = async (ct) =>
        //                        {
        //                            var res = await sberService.PayAsync(amountKopecks, 1, null, ct);
        //                            return new TerminalResult
        //                            {
        //                                IsSuccess = res.IsSuccess,
        //                                ErrorMessage = res.ErrorMessage,
        //                                AuthorizationCode = res.AuthorizationCode,
        //                                ReferenceNumber = res.ReferenceNumber,
        //                                RechargeNote = res.SlipContent,
        //                                CodeResponse = res.IsSuccess ? "1" : "0"
        //                            };
        //                        };
        //                        var result = await WaitNonCashPay.ShowCustomAndWaitAsync(this, 80, sberp, this.cc);
        //                        if (!result.IsSuccess) { CalculateChange(); await MessageBoxHelper.Show(result.ErrorMessage, "Ошибка оплаты Сбер", MessageBoxButton.OK, MessageBoxType.Error, this); return; }
        //                        cc.code_authorization_terminal = result.AuthorizationCode;
        //                        cc.id_transaction_terminal = result.ReferenceNumber;
        //                        if (!string.IsNullOrEmpty(result.RechargeNote)) cc.recharge_note = result.RechargeNote;
        //                    }
        //                }
        //            }
        //        }

        //        cc.print_to_button = 0;
        //        if (await cc.it_is_paid(this.CashSum, sum_doc_str, remainder_str, bonus_money_str, true, sum_cash_pay, non_sum_cash_pay, sertSum.ToString().Replace(",", ".")))
        //        {
        //            cc.closing = false; this.Tag = true; this.Close();
        //        }
        //    }
        //    else // ВОЗВРАТ
        //    {

        //        // 1. СНАЧАЛА парсим и проверяем
        //        decimal returnCashSum = Convert.ToDecimal(this.CashSum);
        //        decimal returnRemainder = Convert.ToDecimal(this.Remainder);

        //        if (returnRemainder < 0)
        //        {
        //            await MessageBoxHelper.Show(
        //                "Ошибка: сумма возврата превышает внесённую сумму.",
        //                "Ошибка расчёта возврата",
        //                MessageBoxButton.OK,
        //                MessageBoxType.Error, this);
        //            return;
        //        }


        //        string sum_cash_pay = (Convert.ToDecimal(this.CashSum) - Convert.ToDecimal(this.Remainder)).ToString().Replace(",", ".");
        //        string non_sum_cash_pay = get_non_cash_sum().ToString().Replace(",", ".");
        //        string sertificate_money_str = Convert.ToDecimal(this.CertificatesSum).ToString().Replace(",", ".");
        //        string bonus_money_str = (string.IsNullOrEmpty(this.BonusMany.Trim()) ? "0" : this.BonusMany.Trim());
        //        string sum_doc_str = cc.calculation_of_the_sum_of_the_document().ToString().Replace(",", ".");
        //        string remainder_str = this.Remainder.Replace(",", ".");


        //        //bool writeResult = await cc.write_new_document(
        //        //    this.CashSum, sum_doc_str, remainder_str, bonus_money_str,
        //        //    false, sum_cash_pay, non_sum_cash_pay, sertificate_money_str, "0", false
        //        //);

        //        //if (!writeResult)
        //        //{
        //        //    await MessageBoxHelper.Show("Не удалось сохранить документ. Оплата отменена.", "Ошибка записи", MessageBoxButton.OK, MessageBoxType.Error, this);
        //        //    return;
        //        //}

        //        if (cc.check_type.SelectedIndex == 1 && get_non_cash_sum() < 1)
        //        {
        //            sum_cash_pay = (Convert.ToDecimal(this.CashSum) - Convert.ToDecimal(this.Remainder) + Convert.ToDecimal(get_non_cash_sum())).ToString().Replace(",", ".");
        //            non_sum_cash_pay = "0";
        //        }

        //        if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != "") && (get_non_cash_sum() > 0))
        //        {
        //            //if (checkBox_do_not_send_payment_to_the_terminal.IsChecked != true)
        //                if (_checkBoxDoNotSendPaymentToTheTerminal?.IsChecked != true)
        //                {
        //                // ИСПОЛЬЗУЕМ СВОЙСТВА
        //                string money = CalculateMoneyInKopecks(this.NonCashSum, this.NonCashSumKop);

        //                if (MainStaticClass.GetAcquiringBank == 1)//РНКБ
        //                {
        //                    string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;
        //                    //if (checkBox_payment_by_sbp.IsChecked != true)
        //                        if (_checkBoxPaymentBySbp?.IsChecked != true)
        //                        {
        //                        string xmlData = "";
        //                        if (cc.sale_date.CompareTo(DateTime.Today) < 0)
        //                            xmlData = str_return_sale.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_code_authorization_terminal).Replace("number_reference", cc.sale_id_transaction_terminal);
        //                        else
        //                        {
        //                            xmlData = str_cancel_sale.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_code_authorization_terminal).Replace("number_reference", cc.sale_id_transaction_terminal);
        //                            if (money.Trim() != (cc.sale_non_cash_money * 100).ToString().Trim()) xmlData = xmlData.Replace("sale_non_cash_money", (cc.sale_non_cash_money * 100).ToString());
        //                            else xmlData = xmlData.Replace(@"<field id=""01"">sale_non_cash_money</field>", "");
        //                        }
        //                        MainStaticClass.write_event_in_log($"Возврат картой: {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
        //                        var resultReturn = await WaitNonCashPay.ShowAndWaitAsync(this, 60, url, xmlData, this.cc);
        //                        if (!resultReturn.IsSuccess) { CalculateChange(); await MessageBoxHelper.Show($"Неудачная попытка возврата: {resultReturn.ErrorMessage}", "Возврат по терминалу", MessageBoxButton.OK, MessageBoxType.Error, this); return; }

        //                        cc.code_authorization_terminal = resultReturn.AuthorizationCode ?? string.Empty;
        //                        cc.id_transaction_terminal = resultReturn.ReferenceNumber;
        //                        complete = true;
        //                    }
        //                    else
        //                    {
        //                        string _str_return_sale_sbp_ = str_return_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_id_transaction_terminal).Replace("guid", cc.guid_sales);
        //                        MainStaticClass.write_event_in_log($"Возврат СБП (Init): {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
        //                        var resultSbpReturn = await WaitNonCashPay.SendRequestAsync(url, _str_return_sale_sbp_);
        //                        TerminalResult finalResult = resultSbpReturn;
        //                        if (!resultSbpReturn.IsSuccess)
        //                        {
        //                            string _str_payment_status_return = str_payment_status_return_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_id_transaction_terminal).Replace("guid", cc.guid_sales);
        //                            var (success, pollResult) = await PollSbpStatusAsync(url, _str_payment_status_return, "Возврат СБП");
        //                            if (!success) { CalculateChange(); return; }
        //                            finalResult = pollResult;
        //                        }
        //                        cc.code_authorization_terminal = finalResult.AuthorizationCode;
        //                        cc.id_transaction_terminal = finalResult.ReferenceNumber;
        //                        cc.payment_by_sbp = true;
        //                    }
        //                }
        //                else if (MainStaticClass.GetAcquiringBank == 2) // СБЕР
        //                {
        //                    var sberService = new SberPaymentService();
        //                    if (int.TryParse(money, out int amountKopecks))
        //                    {
        //                        Func<CancellationToken, Task<TerminalResult>> sberOp = async (ct) =>
        //                        {
        //                            var res = await sberService.PayAsync(amountKopecks, 3, cc.sale_id_transaction_terminal, ct);
        //                            return new TerminalResult { IsSuccess = res.IsSuccess, ErrorMessage = res.ErrorMessage, AuthorizationCode = res.AuthorizationCode, ReferenceNumber = res.ReferenceNumber, RechargeNote = res.SlipContent, CodeResponse = res.IsSuccess ? "1" : "0" };
        //                        };
        //                        var result = await WaitNonCashPay.ShowCustomAndWaitAsync(this, 80, sberOp, this.cc);
        //                        if (!result.IsSuccess) { CalculateChange(); await MessageBoxHelper.Show(result.ErrorMessage, "Ошибка возврата Сбер", MessageBoxButton.OK, MessageBoxType.Error, this); return; }
        //                        cc.code_authorization_terminal = result.AuthorizationCode;
        //                        cc.id_transaction_terminal = result.ReferenceNumber;
        //                        if (!string.IsNullOrEmpty(result.RechargeNote)) cc.recharge_note = result.RechargeNote;
        //                        complete = true;
        //                    }
        //                }
        //            }
        //        }
        //        bool printSuccess = await cc.sale_cancellation_Click(sum_cash_pay, non_sum_cash_pay);
        //        if (printSuccess) { cc.closing = false; this.Close(); }
        //    }
        //}

        private async Task it_is_paid()
        {
            const string logCtx = "Pay.it_is_paid";
            string currentTrap = "0";
            var cashCheck = this.cc;

            try
            {
                MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Вход в it_is_paid", logCtx, cashCheck?.numdoc.ToString() ?? "0");

                if (cashCheck == null)
                {
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] cashCheck is null! Выход.", logCtx, "0");
                    return;
                }

                currentTrap = "0.1";
                MainStaticClass.write_event_in_log(
                    $"[TRAP {currentTrap}] Controls init: " +
                    $"_checkBoxDoNotSend={(_checkBoxDoNotSendPaymentToTheTerminal != null ? "OK" : "NULL")}, " +
                    $"_checkBoxSbp={(_checkBoxPaymentBySbp != null ? "OK" : "NULL")}, " +
                    $"_paySumTextBox={(_paySumTextBox != null ? "OK" : "NULL")}",
                    logCtx, cashCheck.numdoc.ToString());

                currentTrap = "0.2";
                MainStaticClass.write_event_in_log(
                    $"[TRAP {currentTrap}] Properties: " +
                    $"CashSum='{this.CashSum}', " +
                    $"NonCashSum='{this.NonCashSum}', " +
                    $"BonusMany='{this.BonusMany}', " +
                    $"NonCashSumKop='{this.NonCashSumKop}'",
                    logCtx, cashCheck.numdoc.ToString());

                currentTrap = "1";
                if (cashCheck.CheckType?.SelectedIndex == 0) // ОПЛАТА
                {
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Начало ОПЛАТЫ", logCtx, cashCheck.numdoc.ToString());

                    currentTrap = "2";
                    decimal cashSumVal = 0, remainderVal = 0, paySumVal = 0;
                    decimal.TryParse(this.CashSum, out cashSumVal);
                    decimal.TryParse(this.Remainder, out remainderVal);
                    decimal.TryParse(this.PaySum, out paySumVal);

                    decimal sertSum = 0, bonusSum = 0;
                    decimal.TryParse(this.CertificatesSum, out sertSum);
                    decimal.TryParse(this.BonusMany, out bonusSum);
                    decimal nonCashSum = Convert.ToDecimal(get_non_cash_sum());

                    if (remainderVal < 0)
                    {
                        await MessageBoxHelper.Show($"ОШИБКА РАСЧЁТА!...", "Ошибка расчёта оплаты", MessageBoxButton.OK, MessageBoxType.Error, this);
                        CalculateChange(); return;
                    }
                    if (paySumVal - (cashSumVal - remainderVal + sertSum + bonusSum + nonCashSum) > 1)
                    { await MessageBoxHelper.Show(" Неверно внесенные суммы ", "Проверка оплаты", MessageBoxButton.OK, MessageBoxType.Error, this); return; }
                    if (!cashCheck.ValidateCheckSumAtDiscount())
                    { await MessageBoxHelper.Show(" При распределении расчетов получилась нулевая/отрицательная сумма в строке...", "Проверка суммы со скидкой", MessageBoxButton.OK, MessageBoxType.Error, this); return; }

                    currentTrap = "3";
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Формирование строк...", logCtx, cashCheck.numdoc.ToString());
                    string sum_cash_pay = (cashSumVal - remainderVal).ToString().Replace(",", ".");
                    string non_sum_cash_pay = get_non_cash_sum().ToString().Replace(",", ".");
                    string sertificate_money_str = sertSum.ToString().Replace(",", ".");
                    string bonus_money_str = this.BonusMany?.Trim() ?? "0";

                    currentTrap = "4";
                    string sum_doc_str = cashCheck.calculation_of_the_sum_of_the_document().ToString().Replace(",", ".");
                    string remainder_str = this.Remainder?.Replace(",", ".") ?? "0.00";

                    currentTrap = "5";
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Создание PaymentSnapshot...", logCtx, cashCheck.numdoc.ToString());
                    cashCheck.LastPaymentSnapshot = new PaymentSnapshot
                    {
                        CashMoney = cashSumVal - remainderVal,
                        NonCashMoney = nonCashSum,
                        CertificateMoney = sertSum,
                        TotalSumAtDiscount = paySumVal,
                        CreatedAt = DateTime.Now
                    };
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}.1] Snapshot: {cashCheck.LastPaymentSnapshot}", logCtx, cashCheck.numdoc.ToString());

                    currentTrap = "6";
                    double notCashSum = get_non_cash_sum();

                    currentTrap = "6.1";
                    MainStaticClass.write_event_in_log(
                        $"[TRAP {currentTrap}] MainStaticClass: " +
                        $"Ip='{MainStaticClass.IpAddressAcquiringTerminal?.Length ?? 0}', " +
                        $"Id='{MainStaticClass.IdAcquirerTerminal?.Length ?? 0}'",
                        logCtx, cashCheck.numdoc.ToString());

                    string ipTerm = MainStaticClass.IpAddressAcquiringTerminal?.Trim() ?? "";
                    string idTerm = MainStaticClass.IdAcquirerTerminal?.Trim() ?? "";

                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}.2] Терминал: IP='{ipTerm}', ID='{idTerm}', Sum={notCashSum}", logCtx, cashCheck.numdoc.ToString());

                    if (ipTerm != "" && idTerm != "" && notCashSum > 0)
                    {
                        currentTrap = "7";
                        bool skipTerminal = _checkBoxDoNotSendPaymentToTheTerminal?.IsChecked == true;
                        MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Пропуск терминала: {skipTerminal}", logCtx, cashCheck.numdoc.ToString());

                        if (!skipTerminal)
                        {
                            currentTrap = "8";

                            if (string.IsNullOrEmpty(this.NonCashSum))
                                MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] ⚠ NonCashSum is NULL or Empty!", logCtx, cashCheck.numdoc.ToString());
                            if (string.IsNullOrEmpty(this.NonCashSumKop))
                                MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] ⚠ NonCashSumKop is NULL or Empty!", logCtx, cashCheck.numdoc.ToString());

                            string money = CalculateMoneyInKopecks(this.NonCashSum, this.NonCashSumKop);

                            if (MainStaticClass.GetAcquiringBank == 1) //ВТБ
                            {
                                string url = "http://" + ipTerm;
                                bool isSbp = _checkBoxPaymentBySbp?.IsChecked == true;
                                MainStaticClass.write_event_in_log($"[TRAP {currentTrap}.1] СБП чекбокс: {isSbp}", logCtx, cashCheck.numdoc.ToString());

                                if (!isSbp)
                                {
                                    string _str_command_sale_ = str_command_sale.Replace("sum", money).Replace("id_terminal", idTerm);

                                    // ✅ ЛОГ ЗАПРОСА
                                    MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=VTB, Type=CardSale | URL='{url}' | XML:\n{_str_command_sale_}", logCtx, cashCheck.numdoc.ToString());

                                    var terminalResult = await WaitNonCashPay.ShowAndWaitAsync(this, 80, url, _str_command_sale_, cashCheck);
                                    if (!terminalResult.IsSuccess) { CalculateChange(); cashCheck.recharge_note = ""; await MessageBoxHelper.Show(terminalResult.ErrorMessage, "Оплата по терминалу", MessageBoxButton.OK, MessageBoxType.Error, this); return; }

                                    // ✅ ЛОГИРОВАНИЕ РЕЗУЛЬТАТА ПАРСИНГА
                                    MainStaticClass.write_event_in_log($"[TERMINAL RESPONSE] Bank=VTB, Type=CardSale | AuthCode='{terminalResult.AuthorizationCode}' | RefNum='{terminalResult.ReferenceNumber}'", logCtx, cashCheck.numdoc.ToString());

                                    cashCheck.code_authorization_terminal = terminalResult.AuthorizationCode;
                                    cashCheck.id_transaction_terminal = terminalResult.ReferenceNumber;
                                    cashCheck.recharge_note = terminalResult.RechargeNote;

                                    MainStaticClass.write_event_in_log($"[TERMINAL SAVED] Bank=VTB, Type=CardSale | AuthCode='{cashCheck.code_authorization_terminal}' | RefNum='{cashCheck.id_transaction_terminal}'", logCtx, cashCheck.numdoc.ToString());
                                }
                                else
                                {
                                    string _str_sale_sbp = str_sale_sbp.Replace("sum", money).Replace("id_terminal", idTerm).Replace("guid", cashCheck.guid);

                                    // ✅ ЛОГ ЗАПРОСА
                                    MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=VTB, Type=SbpSale | URL='{url}' | XML:\n{_str_sale_sbp}", logCtx, cashCheck.numdoc.ToString());

                                    var resultSbp = await WaitNonCashPay.SendRequestAsync(url, _str_sale_sbp);
                                    TerminalResult finalResult = resultSbp;
                                    if (!resultSbp.IsSuccess)
                                    {
                                        string _str_payment_status = str_payment_status_sale_sbp.Replace("sum", money).Replace("id_terminal", idTerm).Replace("sale_code_authorization_terminal", cashCheck.guid);

                                        // ✅ ЛОГ ЗАПРОСА ПОЛЛИНГА
                                        MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=VTB, Type=SbpPolling | XML:\n{_str_payment_status}", logCtx, cashCheck.numdoc.ToString());

                                        var (success, pollResult) = await PollSbpStatusAsync(url, _str_payment_status, "Оплата СБП");
                                        if (!success) { CalculateChange(); cashCheck.recharge_note = ""; return; }
                                        finalResult = pollResult;
                                    }

                                    // ✅ ЛОГИРОВАНИЕ РЕЗУЛЬТАТА ПАРСИНГА
                                    MainStaticClass.write_event_in_log($"[TERMINAL RESPONSE] Bank=VTB, Type=SbpSale | AuthCode='{finalResult.AuthorizationCode}' | RefNum='{finalResult.ReferenceNumber}'", logCtx, cashCheck.numdoc.ToString());

                                    cashCheck.code_authorization_terminal = finalResult.AuthorizationCode;
                                    cashCheck.id_transaction_terminal = finalResult.ReferenceNumber;
                                    cashCheck.payment_by_sbp = true;

                                    MainStaticClass.write_event_in_log($"[TERMINAL SAVED] Bank=VTB, Type=SbpSale | AuthCode='{cashCheck.code_authorization_terminal}' | RefNum='{cashCheck.id_transaction_terminal}'", logCtx, cashCheck.numdoc.ToString());
                                }
                            }
                            else if (MainStaticClass.GetAcquiringBank == 2) // СБЕР
                            {
                                // ✅ ЛОГ ЗАПРОСА
                                MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=Sber, Type=Sale | AmountKopecks='{money}'", logCtx, cashCheck.numdoc.ToString());

                                var sberService = new SberPaymentService();
                                if (int.TryParse(money, out int amountKopecks))
                                {
                                    Func<CancellationToken, Task<TerminalResult>> sberp = async (ct) =>
                                    {
                                        var res = await sberService.PayAsync(amountKopecks, 1, null, ct);
                                        return new TerminalResult { IsSuccess = res.IsSuccess, ErrorMessage = res.ErrorMessage, AuthorizationCode = res.AuthorizationCode, ReferenceNumber = res.ReferenceNumber, RechargeNote = res.SlipContent, CodeResponse = res.IsSuccess ? "1" : "0" };
                                    };
                                    var result = await WaitNonCashPay.ShowCustomAndWaitAsync(this, 80, sberp, cashCheck);
                                    if (!result.IsSuccess) { CalculateChange(); await MessageBoxHelper.Show(result.ErrorMessage, "Ошибка оплаты Сбер", MessageBoxButton.OK, MessageBoxType.Error, this); return; }

                                    // ✅ ЛОГИРОВАНИЕ РЕЗУЛЬТАТА ПАРСИНГА
                                    MainStaticClass.write_event_in_log($"[TERMINAL RESPONSE] Bank=Sber, Type=Sale | AuthCode='{result.AuthorizationCode}' | RefNum='{result.ReferenceNumber}'", logCtx, cashCheck.numdoc.ToString());

                                    cashCheck.code_authorization_terminal = result.AuthorizationCode;
                                    cashCheck.id_transaction_terminal = result.ReferenceNumber;
                                    if (!string.IsNullOrEmpty(result.RechargeNote)) cashCheck.recharge_note = result.RechargeNote;

                                    MainStaticClass.write_event_in_log($"[TERMINAL SAVED] Bank=Sber, Type=Sale | AuthCode='{cashCheck.code_authorization_terminal}' | RefNum='{cashCheck.id_transaction_terminal}'", logCtx, cashCheck.numdoc.ToString());
                                }
                                else
                                {
                                    MainStaticClass.write_event_in_log($"[TRAP 8.2] ОШИБКА: Невозможно преобразовать сумму '{money}' в копейки для СБЕР!", logCtx, cashCheck.numdoc.ToString());
                                    await MessageBoxHelper.Show($"Сбой: неверный формат суммы для терминала ({money}). Оплата отменена.", "Ошибка формата", MessageBoxButton.OK, MessageBoxType.Error, this);
                                    CalculateChange();
                                    return;
                                }
                            }
                        }
                    }

                    currentTrap = "9";
                    cashCheck.print_to_button = 0;
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Вызов cc.it_is_paid...", logCtx, cashCheck.numdoc.ToString());
                    if (await cashCheck.it_is_paid(this.CashSum, sum_doc_str, remainder_str, bonus_money_str, true, sum_cash_pay, non_sum_cash_pay, sertSum.ToString().Replace(",", ".")))
                    {
                        MainStaticClass.write_event_in_log($"[TRAP {currentTrap}.1] Успех, закрываем окно.", logCtx, cashCheck.numdoc.ToString());
                        cashCheck.closing = false; this.Tag = true; this.Close();
                    }
                    else
                    {
                        MainStaticClass.write_event_in_log($"[TRAP {currentTrap}.2] cc.it_is_paid вернул false.", logCtx, cashCheck.numdoc.ToString());
                    }
                }
                else // ВОЗВРАТ
                {
                    currentTrap = "10";
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Начало оформления ВОЗВРАТА", logCtx, cashCheck.numdoc.ToString());

                    decimal returnCashSum = 0, returnRemainder = 0;
                    decimal.TryParse(this.CashSum, out returnCashSum);
                    decimal.TryParse(this.Remainder, out returnRemainder);

                    if (returnRemainder < 0)
                    {
                        await MessageBoxHelper.Show("Ошибка: сумма возврата превышает внесённую сумму.", "Ошибка расчёта возврата", MessageBoxButton.OK, MessageBoxType.Error, this);
                        return;
                    }

                    currentTrap = "11";
                    string sum_cash_pay = (returnCashSum - returnRemainder).ToString().Replace(",", ".");
                    string non_sum_cash_pay = get_non_cash_sum().ToString().Replace(",", ".");
                    string sertificate_money_str = Convert.ToDecimal(this.CertificatesSum).ToString().Replace(",", ".");
                    string bonus_money_str = this.BonusMany?.Trim() ?? "0";
                    string sum_doc_str = cashCheck.calculation_of_the_sum_of_the_document().ToString().Replace(",", ".");
                    string remainder_str = this.Remainder?.Replace(",", ".") ?? "0.00";

                    if (cashCheck.CheckType?.SelectedIndex == 1 && get_non_cash_sum() < 1)
                    {
                        sum_cash_pay = (returnCashSum - returnRemainder + Convert.ToDecimal(get_non_cash_sum())).ToString().Replace(",", ".");
                        non_sum_cash_pay = "0";
                    }

                    currentTrap = "12";
                    string ipTerm = MainStaticClass.IpAddressAcquiringTerminal?.Trim() ?? "";
                    string idTerm = MainStaticClass.IdAcquirerTerminal?.Trim() ?? "";
                    double notCashSum = get_non_cash_sum();

                    if (ipTerm != "" && idTerm != "" && notCashSum > 0)
                    {
                        bool skipTerminal = _checkBoxDoNotSendPaymentToTheTerminal?.IsChecked == true;
                        MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Возврат: Терминал IP='{ipTerm}', Пропуск={skipTerminal}", logCtx, cashCheck.numdoc.ToString());

                        if (!skipTerminal)
                        {
                            currentTrap = "12.1";
                            if (string.IsNullOrEmpty(this.NonCashSum))
                                MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] ⚠ NonCashSum is NULL or Empty!", logCtx, cashCheck.numdoc.ToString());
                            if (string.IsNullOrEmpty(this.NonCashSumKop))
                                MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] ⚠ NonCashSumKop is NULL or Empty!", logCtx, cashCheck.numdoc.ToString());

                            string money = CalculateMoneyInKopecks(this.NonCashSum, this.NonCashSumKop);

                            if (MainStaticClass.GetAcquiringBank == 1)//ВТБ/РНКБ
                            {
                                string url = "http://" + ipTerm;
                                bool isSbp = _checkBoxPaymentBySbp?.IsChecked == true;
                                if (!isSbp)
                                {
                                    string xmlData = "";
                                    if (cashCheck.sale_date.CompareTo(DateTime.Today) < 0) xmlData = str_return_sale.Replace("sum", money).Replace("id_terminal", idTerm).Replace("sale_code_authorization_terminal", cashCheck.sale_code_authorization_terminal).Replace("number_reference", cashCheck.sale_id_transaction_terminal);
                                    else
                                    {
                                        xmlData = str_cancel_sale.Replace("sum", money).Replace("id_terminal", idTerm).Replace("sale_code_authorization_terminal", cashCheck.sale_code_authorization_terminal).Replace("number_reference", cashCheck.sale_id_transaction_terminal);
                                        if (money.Trim() != (cashCheck.sale_non_cash_money * 100).ToString().Trim()) xmlData = xmlData.Replace("sale_non_cash_money", (cashCheck.sale_non_cash_money * 100).ToString());
                                        else xmlData = xmlData.Replace(@"<field id=""01"">sale_non_cash_money</field>", "");
                                    }

                                    // ✅ ЛОГ ЗАПРОСА
                                    MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=VTB, Type=CardReturn | URL='{url}' | XML:\n{xmlData}", logCtx, cashCheck.numdoc.ToString());

                                    var resultReturn = await WaitNonCashPay.ShowAndWaitAsync(this, 60, url, xmlData, cashCheck);
                                    if (!resultReturn.IsSuccess) { CalculateChange(); await MessageBoxHelper.Show($"Неудачная попытка возврата: {resultReturn.ErrorMessage}", "Возврат по терминалу", MessageBoxButton.OK, MessageBoxType.Error, this); return; }

                                    // ✅ ЛОГИРОВАНИЕ РЕЗУЛЬТАТА ПАРСИНГА
                                    MainStaticClass.write_event_in_log($"[TERMINAL RESPONSE] Bank=VTB, Type=CardReturn | AuthCode='{resultReturn.AuthorizationCode}' | RefNum='{resultReturn.ReferenceNumber}'", logCtx, cashCheck.numdoc.ToString());

                                    cashCheck.code_authorization_terminal = resultReturn.AuthorizationCode ?? string.Empty;
                                    cashCheck.id_transaction_terminal = resultReturn.ReferenceNumber;
                                    complete = true;

                                    MainStaticClass.write_event_in_log($"[TERMINAL SAVED] Bank=VTB, Type=CardReturn | AuthCode='{cashCheck.code_authorization_terminal}' | RefNum='{cashCheck.id_transaction_terminal}'", logCtx, cashCheck.numdoc.ToString());
                                }
                                else
                                {
                                    currentTrap = "13";
                                    string _str_return_sale_sbp_ = str_return_sale_sbp.Replace("sum", money).Replace("id_terminal", idTerm).Replace("sale_code_authorization_terminal", cashCheck.sale_id_transaction_terminal).Replace("guid", cashCheck.guid_sales);

                                    // ✅ ЛОГ ЗАПРОСА
                                    MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=VTB, Type=SbpReturn | URL='{url}' | XML:\n{_str_return_sale_sbp_}", logCtx, cashCheck.numdoc.ToString());

                                    var resultSbpReturn = await WaitNonCashPay.SendRequestAsync(url, _str_return_sale_sbp_);
                                    TerminalResult finalResult = resultSbpReturn;
                                    if (!resultSbpReturn.IsSuccess)
                                    {
                                        string _str_payment_status_return = str_payment_status_return_sale_sbp.Replace("sum", money).Replace("id_terminal", idTerm).Replace("sale_code_authorization_terminal", cashCheck.sale_id_transaction_terminal).Replace("guid", cashCheck.guid_sales);

                                        // ✅ ЛОГ ЗАПРОСА ПОЛЛИНГА
                                        MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=VTB, Type=SbpReturnPolling | XML:\n{_str_payment_status_return}", logCtx, cashCheck.numdoc.ToString());

                                        var (success, pollResult) = await PollSbpStatusAsync(url, _str_payment_status_return, "Возврат СБП");
                                        if (!success) { CalculateChange(); return; }
                                        finalResult = pollResult;
                                    }

                                    // ✅ ЛОГИРОВАНИЕ РЕЗУЛЬТАТА ПАРСИНГА
                                    MainStaticClass.write_event_in_log($"[TERMINAL RESPONSE] Bank=VTB, Type=SbpReturn | AuthCode='{finalResult.AuthorizationCode}' | RefNum='{finalResult.ReferenceNumber}'", logCtx, cashCheck.numdoc.ToString());

                                    cashCheck.code_authorization_terminal = finalResult.AuthorizationCode;
                                    cashCheck.id_transaction_terminal = finalResult.ReferenceNumber;
                                    cashCheck.payment_by_sbp = true;

                                    MainStaticClass.write_event_in_log($"[TERMINAL SAVED] Bank=VTB, Type=SbpReturn | AuthCode='{cashCheck.code_authorization_terminal}' | RefNum='{cashCheck.id_transaction_terminal}'", logCtx, cashCheck.numdoc.ToString());
                                }
                            }
                            else if (MainStaticClass.GetAcquiringBank == 2) // СБЕР (ВОЗВРАТ)
                            {
                                currentTrap = "14";

                                // ✅ ЛОГ ЗАПРОСА
                                MainStaticClass.write_event_in_log($"[TERMINAL REQUEST] Bank=Sber, Type=Return | AmountKopecks='{money}' | OriginalRRN='{cashCheck.sale_id_transaction_terminal}'", logCtx, cashCheck.numdoc.ToString());

                                var sberService = new SberPaymentService();
                                if (int.TryParse(money, out int amountKopecks))
                                {
                                    // ✅ ИСПРАВЛЕНО: Тип операции 3 (Возврат) и передача RRN оригинальной транзакции
                                    Func<CancellationToken, Task<TerminalResult>> sberOp = async (ct) =>
                                    {
                                        var res = await sberService.PayAsync(amountKopecks, 3, cashCheck.sale_id_transaction_terminal, ct);
                                        return new TerminalResult { IsSuccess = res.IsSuccess, ErrorMessage = res.ErrorMessage, AuthorizationCode = res.AuthorizationCode, ReferenceNumber = res.ReferenceNumber, RechargeNote = res.SlipContent, CodeResponse = res.IsSuccess ? "1" : "0" };
                                    };
                                    var result = await WaitNonCashPay.ShowCustomAndWaitAsync(this, 80, sberOp, cashCheck);
                                    if (!result.IsSuccess) { CalculateChange(); await MessageBoxHelper.Show(result.ErrorMessage, "Ошибка возврата Сбер", MessageBoxButton.OK, MessageBoxType.Error, this); return; }

                                    // ✅ ЛОГИРОВАНИЕ РЕЗУЛЬТАТА ПАРСИНГА
                                    MainStaticClass.write_event_in_log($"[TERMINAL RESPONSE] Bank=Sber, Type=Return | AuthCode='{result.AuthorizationCode}' | RefNum='{result.ReferenceNumber}'", logCtx, cashCheck.numdoc.ToString());

                                    cashCheck.code_authorization_terminal = result.AuthorizationCode;
                                    cashCheck.id_transaction_terminal = result.ReferenceNumber;
                                    if (!string.IsNullOrEmpty(result.RechargeNote)) cashCheck.recharge_note = result.RechargeNote;
                                    complete = true;

                                    MainStaticClass.write_event_in_log($"[TERMINAL SAVED] Bank=Sber, Type=Return | AuthCode='{cashCheck.code_authorization_terminal}' | RefNum='{cashCheck.id_transaction_terminal}'", logCtx, cashCheck.numdoc.ToString());
                                }
                                else
                                {
                                    // ⚠️ ВАЖНО: Если сумма не распарсилась — СТОП!
                                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] ОШИБКА: Невозможно преобразовать сумму '{money}' в копейки для СБЕР (Возврат)!", logCtx, cashCheck.numdoc.ToString());
                                    await MessageBoxHelper.Show($"Сбой: неверный формат суммы для терминала ({money}). Возврат отменён.", "Ошибка формата", MessageBoxButton.OK, MessageBoxType.Error, this);
                                    CalculateChange();
                                    return;
                                }
                            }
                        }
                    }

                    currentTrap = "15";
                    MainStaticClass.write_event_in_log($"[TRAP {currentTrap}] Вызов cc.sale_cancellation_Click...", logCtx, cashCheck.numdoc.ToString());
                    bool printSuccess = await cashCheck.sale_cancellation_Click(sum_cash_pay, non_sum_cash_pay);
                    if (printSuccess)
                    {
                        MainStaticClass.write_event_in_log($"[TRAP {currentTrap}.1] Возврат успешен, закрытие.", logCtx, cashCheck.numdoc.ToString());
                        cashCheck.closing = false; this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif

                string description = $"Pay.it_is_paid FAILED at TRAP {currentTrap} | " +
                                    $"Bank={MainStaticClass.GetAcquiringBank} | " +
                                    $"SbpCheckbox={_checkBoxPaymentBySbp?.IsChecked} | " +
                                    $"SkipTerminal={_checkBoxDoNotSendPaymentToTheTerminal?.IsChecked} | " +
                                    $"NonCashSum='{this.NonCashSum}' | NonCashSumKop='{this.NonCashSumKop}'";

                MainStaticClass.WriteRecordErrorLog(
                    ex,
                    cashCheck?.numdoc ?? 0,
                    MainStaticClass.CashDeskNumber,
                    description);

                await MessageBoxHelper.Show(
                    $"Ошибка при оплате (шаг {currentTrap}).\n{ex.Message}\n\n" +
                    $"Если ошибка повторяется — обратитесь в ИТ-отдел.",
                    "Сбой программы", MessageBoxButton.OK, MessageBoxType.Error, this);

                CalculateChange();
            }
        }

        //private double get_non_cash_sum()
        //{
        //    double result = 0;
        //    string rub = this.NonCashSum;
        //    string kop = this.NonCashSumKop;

        //    // Логируем, если свойства вернули null (хотя свойства этого не должны делать, но на всякий случай)
        //    if (rub == null) MainStaticClass.write_event_in_log($"[Pay.get_non_cash_sum] NonCashSum is null", "PayWindow", cc?.numdoc.ToString() ?? "0");
        //    if (kop == null) MainStaticClass.write_event_in_log($"[Pay.get_non_cash_sum] NonCashSumKop is null", "PayWindow", cc?.numdoc.ToString() ?? "0");

        //    if (double.TryParse(rub, out double rubVal)) result += rubVal;
        //    if (double.TryParse(kop, out double kopVal)) result += kopVal / 100;

        //    return result;
        //}

        /// <summary>
        /// Универсальный метод опроса статуса СБП (для оплаты и возврата)
        /// </summary>
        private async Task<(bool Success, TerminalResult Result)> PollSbpStatusAsync(string url, string xmlData, string contextLog)
        {
            int attempts = 0;
            int userPromptCount = 0; // Счетчик вопросов пользователю
            const int MaxAttempts = 30;
            const int MaxUserPrompts = 3; // Лимит вопросов

            while (attempts < MaxAttempts)
            {
                attempts++;

                if (attempts % 5 == 0 || attempts == 1)
                    MainStaticClass.write_event_in_log($"{contextLog}: попытка опроса {attempts}/{MaxAttempts}", "Terminal", cc?.numdoc.ToString() ?? "0");

                var result = await WaitNonCashPay.SendRequestAsync(url, xmlData, 20);

                if (result.IsSuccess)
                {
                    MainStaticClass.write_event_in_log($"{contextLog}: УСПЕХ (попытка {attempts})", "Terminal", cc?.numdoc.ToString() ?? "0");
                    return (true, result);
                }

                // Обработка кодов
                if (result.CodeResponse15 == "R10")
                {
                    await MessageBoxHelper.Show("Операция отклонена", contextLog, MessageBoxButton.OK, MessageBoxType.Error, this);
                    return (false, result);
                }
                if (result.CodeResponse15 == "R11")
                {
                    await MessageBoxHelper.Show("Операции по QR коду не существует.", contextLog, MessageBoxButton.OK, MessageBoxType.Error, this);
                    return (false, result);
                }
                if (result.CodeResponse15 == "R12")
                {
                    if (result.CodeResponse == "0") { await Task.Delay(3000); continue; }
                    await MessageBoxHelper.Show("Не получен ответ на запрос статуса/QR-кода", contextLog, MessageBoxButton.OK, MessageBoxType.Error, this);
                    return (false, result);
                }
                if (result.CodeResponse15 == "R13")
                {
                    await MessageBoxHelper.Show("Запрос статуса не отправлен", contextLog, MessageBoxButton.OK, MessageBoxType.Error, this);
                    return (false, result);
                }
                if (result.CodeResponse15 == "R14")
                {
                    await MessageBoxHelper.Show("Операция не добавлена в базу терминала", contextLog, MessageBoxButton.OK, MessageBoxType.Error, this);
                    return (false, result);
                }

                // R00 или пусто - "в процессе"
                if (result.CodeResponse15 == "R00" || string.IsNullOrEmpty(result.CodeResponse15))
                {
                    await Task.Delay(2000);
                    continue;
                }

                // Прочие ошибки - спрашиваем пользователя (с ограничением)
                if (userPromptCount >= MaxUserPrompts)
                {
                    MainStaticClass.write_event_in_log($"{contextLog}: Превышен лимит вопросов пользователю", "Terminal", cc?.numdoc.ToString() ?? "0");
                    return (false, result);
                }

                userPromptCount++;
                var choice = await MessageBoxHelper.Show(
                    $"Ошибка терминала ({result.CodeResponse}/{result.CodeResponse15}). Продолжать опрос?",
                    contextLog,
                    MessageBoxButton.YesNo,
                    MessageBoxType.Question,
                    this);

                if (choice == MessageBoxResult.No)
                    return (false, result);
            }

            MainStaticClass.write_event_in_log($"{contextLog}: Превышено количество попыток ({MaxAttempts})", "Terminal", cc?.numdoc.ToString() ?? "0");
            return (false, TerminalResult.CreateError("Превышено время ожидания статуса СБП"));
        }

        // Внутренние классы
        public class AnswerTerminal
        {
            public string code_authorization { get; set; }
            public string number_reference { get; set; }
            public string сode_response_in_15_field { get; set; }
            public string сode_response_in_39_field { get; set; }
            public bool error { get; set; }
            public int error_code { get; set; }
            public AnswerTerminal() { number_reference = ""; code_authorization = ""; }
        }

        [XmlRoot(ElementName = "field")] public class Field { [XmlAttribute(AttributeName = "id")] public string Id { get; set; } [XmlText] public string Text { get; set; } }
        [XmlRoot(ElementName = "response")] public class Response { [XmlElement(ElementName = "field")] public List<Field> Field { get; set; } }

        //private double get_non_cash_sum()
        //{
        //    double result = 0;
        //    result += double.Parse(non_cash_sum.Text) + double.Parse(non_cash_sum_kop.Text.Trim().Length == 0 ? "0" : non_cash_sum_kop.Text) / 100;
        //    return result;
        //}

        // ИСПРАВЛЕННЫЙ метод get_non_cash_sum с защитой от null
        private double get_non_cash_sum()
        {
            double result = 0;
            // Используем свойства NonCashSum и NonCashSumKop вместо полей
            string rub = this.NonCashSum;
            string kop = this.NonCashSumKop;

            // Логируем, если свойства вернули null (хотя свойства этого не должны делать, но на всякий случай)
            if (rub == null)
            {
                MainStaticClass.write_event_in_log($"[Pay.get_non_cash_sum] NonCashSum is null", "PayWindow", cc?.numdoc.ToString() ?? "0");
                rub = "0"; // Дополнительная защита, чтобы TryParse не сработал странно
            }
            if (kop == null)
            {
                MainStaticClass.write_event_in_log($"[Pay.get_non_cash_sum] NonCashSumKop is null", "PayWindow", cc?.numdoc.ToString() ?? "0");
                kop = "0"; // Дополнительная защита
            }

            if (double.TryParse(rub, out double rubVal)) result += rubVal;
            if (double.TryParse(kop, out double kopVal)) result += kopVal / 100;

            return result;
        }

        private void CheckBox_payment_by_sbp_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {                

                if (checkBox.IsChecked != true)
                {
                    if (_nonCashSumTextBox != null) { _nonCashSumTextBox.Text = "0"; _nonCashSumTextBox.IsEnabled = false; }
                    if (_nonCashSumKopTextBox != null) _nonCashSumKopTextBox.Text = "0";
                }
                else
                {
                    if (_nonCashSumTextBox != null) _nonCashSumTextBox.IsEnabled = true;
                }
                SbpPaymentChanged?.Invoke(this, checkBox.IsChecked ?? false);
            }
        }

        private void CalculateChange()
        {

            if (_paySumTextBox != null && _cashSumTextBox != null && _remainderTextBox != null)
            {
                try
                {
                    decimal ParseDecimal(string text) { if (string.IsNullOrWhiteSpace(text)) return 0m; text = text.Replace(",", "."); return decimal.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture); }
                    int ParseInt(string text) { if (string.IsNullOrWhiteSpace(text)) return 0; return int.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture); }

                    decimal paySum = ParseDecimal(_paySumTextBox.Text);
                    decimal cashSum = ParseDecimal(_cashSumTextBox.Text);
                    decimal nonCashSum = 0;
                    if (_nonCashSumTextBox != null) { nonCashSum = ParseDecimal(_nonCashSumTextBox.Text); if (_nonCashSumKopTextBox != null) nonCashSum += ParseInt(_nonCashSumKopTextBox.Text) / 100m; }
                    decimal certificatesSum = _sertificatesSumTextBox != null ? ParseDecimal(_sertificatesSumTextBox.Text) : 0;
                    decimal bonusSum = _bonusManyTextBox != null ? ParseDecimal(_bonusManyTextBox.Text) : 0;

                    decimal totalPaid = cashSum + nonCashSum + certificatesSum + bonusSum;
                    decimal remainder = totalPaid - paySum;
                    _remainderTextBox.Text = remainder.ToString("F2");

                    if (remainder < 0 || remainder > cashSum) _remainderTextBox.Foreground = Brushes.Red;
                    else _remainderTextBox.Foreground = Brushes.Green;

                    //var buttonPay = this.FindControl<Button>("button_pay");
                    if (_buttonPay != null) _buttonPay.IsEnabled = totalPaid >= paySum;
                }
                catch
                {
                    _remainderTextBox.Text = "0.00";
                    _remainderTextBox.Foreground = Brushes.Green;                    
                    if (_buttonPay != null)
                    {
                        _buttonPay.IsEnabled = false;
                    }
                }            
            }
        }

        //#region Свойства доступа к UI
        

        ///// <summary>
        ///// Управляет видимостью и доступностью элементов управления бонусами
        ///// </summary>
        //public void SetBonusControlsState(bool isVisible, bool isEnabled)
        //{
        //    if (_bonusSumTextBox != null)
        //    {
        //        _bonusSumTextBox.IsVisible = isVisible;
        //        _bonusSumTextBox.IsEnabled = isEnabled;
        //    }
        //    // Если нужно управлять видимостью поля "Списать бонусов", раскомментируйте:
        //    // if (_bonusManyTextBox != null)
        //    // {
        //    //     _bonusManyTextBox.IsVisible = isVisible;
        //    //     _bonusManyTextBox.IsEnabled = isEnabled;
        //    // }
        //}
        //#endregion

        #region Свойства доступа к UI (ТЕПЕРЬ РАБОТАЮТ ЧЕРЕЗ КЭШИРОВАННЫЕ ПОЛЯ)

        /// <summary>
        /// Управляет видимостью и доступностью элементов управления бонусами
        /// </summary>
        public void SetBonusControlsState(bool isVisible, bool isEnabled)
        {
            if (_bonusSumTextBox != null)
            {
                _bonusSumTextBox.IsVisible = isVisible;
                _bonusSumTextBox.IsEnabled = isEnabled;
            }
            // Если нужно управлять видимостью поля "Списать бонусов", раскомментируйте:
            // if (_bonusManyTextBox != null)
            // {
            //     _bonusManyTextBox.IsVisible = isVisible;
            //     _bonusManyTextBox.IsEnabled = isEnabled;
            // }
        }

        public string PaySum { get => _paySumTextBox?.Text ?? string.Empty; set { if (_paySumTextBox != null) { _paySumTextBox.Text = value; CalculateChange(); } } }
        public string CashSum { get => _cashSumTextBox?.Text ?? string.Empty; set { if (_cashSumTextBox != null) { _cashSumTextBox.Text = value; CalculateChange(); } } }
        public string NonCashSum { get => _nonCashSumTextBox?.Text ?? string.Empty; set { if (_nonCashSumTextBox != null) { _nonCashSumTextBox.Text = value; CalculateChange(); } } }
        public string NonCashSumKop { get => _nonCashSumKopTextBox?.Text ?? string.Empty; set { if (_nonCashSumKopTextBox != null) { _nonCashSumKopTextBox.Text = value; CalculateChange(); } } }
        public string CertificatesSum { get => _sertificatesSumTextBox?.Text ?? string.Empty; set { if (_sertificatesSumTextBox != null) _sertificatesSumTextBox.Text = value; } }
        public string BonusSum { get => _bonusSumTextBox?.Text ?? string.Empty; set { if (_bonusSumTextBox != null) _bonusSumTextBox.Text = value; } }
        public string BonusMany { get => _bonusManyTextBox?.Text ?? string.Empty; set { if (_bonusManyTextBox != null) _bonusManyTextBox.Text = value; } }
        public string Remainder { get => _remainderTextBox?.Text ?? string.Empty; set { if (_remainderTextBox != null) _remainderTextBox.Text = value; } }
        public bool IsSbpPayment { get => _checkBoxPaymentBySbp?.IsChecked ?? false; set { if (_checkBoxPaymentBySbp != null) _checkBoxPaymentBySbp.IsChecked = value; } }
        public void ShowSbpControls(bool show) { if (_checkBoxPaymentBySbp != null) _checkBoxPaymentBySbp.IsVisible = false; if (_checkBoxDoNotSendPaymentToTheTerminal != null) _checkBoxDoNotSendPaymentToTheTerminal.IsVisible = show; }

        #endregion
    }
}