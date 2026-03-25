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

        // Строки шаблонов XML
        private string str_command_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field></request>";
        string str_return_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">number_reference</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field></request>";
        string str_cancel_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""01"">sale_non_cash_money</field><field id=""04"">643</field><field id = ""25"">4</field><field id=""27"">id_terminal</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">number_reference</field></request>";
        string str_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""14"">guid</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field><field id=""53"">115</field></request>";
        string str_payment_status_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field><field id=""53"">117</field></request>";
        string str_return_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">guid</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field><field id=""53"">118</field></request>";
        string str_payment_status_return_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">guid</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field><field id=""53"">119</field></request>";

        public Cash_check cc = null;
        TextBox cashSumTextBox = null;

        public Pay()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.Loaded += Pay_Loaded;
            this.Opened += Pay_Opened;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private void Pay_Loaded(object? sender, RoutedEventArgs e)
        {
            this.pay_bonus_many.Text = "0";
            this.pay_bonus.Text = "0";
            this.sertificates_sum.Text = "0";
            this.non_cash_sum.Text = "0";
            this.non_cash_sum_kop.Text = "0";

            InitializeEventHandlers();

            if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != ""))
            {
                if (MainStaticClass.GetAcquiringBank == 1)//ВТБ
                {
                    checkBox_payment_by_sbp.Opacity = 1;
                    checkBox_payment_by_sbp.IsHitTestVisible = true;
                }
                checkBox_do_not_send_payment_to_the_terminal.Opacity = 1;
                checkBox_do_not_send_payment_to_the_terminal.IsHitTestVisible = true;
            }

            if (cc.payment_by_sbp_sales)
            {
                checkBox_payment_by_sbp.IsChecked = true;
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
            ToolTip.SetTip(checkBox_do_not_send_payment_to_the_terminal, toolTipContent);
            CalculateChange();
        }

        private async void Pay_Opened(object? sender, EventArgs e)
        {
            this.Topmost = true;
            await Task.Delay(100);
            this.Topmost = false;
            this.Activate();

            if (cashSumTextBox != null)
            {
                cashSumTextBox.Focus();
                if (cashSumTextBox is TextBox tb)
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

            var checkBoxPaymentBySbp = this.FindControl<CheckBox>("checkBox_payment_by_sbp");
            if (checkBoxPaymentBySbp != null) checkBoxPaymentBySbp.IsCheckedChanged += CheckBox_payment_by_sbp_CheckedChanged;

            this.button_pay.Click += Button_pay_Click;
            this.button1.Click += Button1_Click;

            cashSumTextBox = this.FindControl<TextBox>("txtB_cash_sum");
            if (cashSumTextBox != null)
            {
                cashSumTextBox.TextChanged += CashSumTextBox_TextChanged;
                cashSumTextBox.GotFocus += OnCashSumGotFocus;
                cashSumTextBox.LostFocus += OnCashSumLostFocus;
                cashSumTextBox.TextInput += OnCashSumTextInput;
                cashSumTextBox.KeyDown += OnCashSumKeyDown;
                cashSumTextBox.KeyUp += CashSumTextBox_KeyUp;
                cashSumTextBox.Text = "0,00";
            }

            var nonCashSumTextBox = this.FindControl<TextBox>("non_cash_sum");
            if (nonCashSumTextBox != null)
            {
                nonCashSumTextBox.KeyDown += NonCashSumTextBox_KeyDown;
                nonCashSumTextBox.LostFocus += OnNonCashSumLostFocus;
                nonCashSumTextBox.TextChanged += NonCashSumTextBox_TextChanged;
                nonCashSumTextBox.Text = "0";
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
        private void OnCashSumGotFocus(object sender, GotFocusEventArgs e) { if (cashSumTextBox?.Text == "0,00") _firstInput = true; }
        private void OnCashSumLostFocus(object sender, RoutedEventArgs e)
        {
            if (cashSumTextBox == null) return;
            if (string.IsNullOrWhiteSpace(cashSumTextBox.Text)) { cashSumTextBox.Text = "0,00"; _firstInput = true; }
            else { if (decimal.TryParse(cashSumTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) cashSumTextBox.Text = result.ToString("F2"); else cashSumTextBox.Text = "0,00"; _firstInput = true; }
            CalculateChange();
        }
        private void OnCashSumTextInput(object sender, TextInputEventArgs e)
        {
            if (cashSumTextBox == null) return;
            if (string.IsNullOrEmpty(e.Text)) { e.Handled = true; return; }
            char inputChar = e.Text[0];
            bool isDigit = char.IsDigit(inputChar);
            bool isSeparator = inputChar == ',' || inputChar == '.';
            if (!isDigit && !isSeparator && !char.IsControl(inputChar)) { e.Handled = true; return; }

            var selectionStart = cashSumTextBox.CaretIndex;
            var currentText = cashSumTextBox.Text ?? "";

            if (isDigit)
            {
                if (_firstInput) { _firstInput = false; cashSumTextBox.Text = inputChar + currentText.Substring(1); e.Handled = true; cashSumTextBox.CaretIndex = 1; }
                else { cashSumTextBox.Text = currentText.Insert(selectionStart, inputChar.ToString()); e.Handled = true; cashSumTextBox.CaretIndex = selectionStart + 1; }
            }
            else if (isSeparator)
            {
                string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                if (!currentText.Contains(separator)) { cashSumTextBox.Text = currentText.Insert(selectionStart, separator); e.Handled = true; cashSumTextBox.CaretIndex = selectionStart + 1; }
                else { cashSumTextBox.CaretIndex = currentText.IndexOf(separator) + 1; e.Handled = true; }
            }

            var separatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (cashSumTextBox.Text.Contains(separatorChar))
            {
                int decimalIndex = cashSumTextBox.Text.IndexOf(separatorChar);
                if (cashSumTextBox.Text.Length - decimalIndex - 1 < 2) { cashSumTextBox.Text = cashSumTextBox.Text.Substring(0, decimalIndex + 1) + cashSumTextBox.Text.Substring(decimalIndex + 1).PadRight(2, '0'); cashSumTextBox.CaretIndex = decimalIndex + 1; }
            }
            if (cashSumTextBox.CaretIndex == 0) cashSumTextBox.CaretIndex = cashSumTextBox.Text.Length;
            CalculateChange();
        }
        private void OnCashSumKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete) { _firstInput = false; Task.Delay(10).ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => { if (cashSumTextBox != null) FormatCashSumText(); })); }
            CalculateChange();
        }
        private void CashSumTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (cashSumTextBox == null) return; FormatCashSumText(); CalculateChange(); }
        private void FormatCashSumText()
        {
            if (cashSumTextBox == null) return;
            var currentText = cashSumTextBox.Text;
            if (!string.IsNullOrEmpty(currentText))
            {
                var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                var cleanedText = new string(currentText.Where(c => char.IsDigit(c) || c == separator[0]).ToArray());
                int separatorCount = cleanedText.Count(c => c == separator[0]);
                if (separatorCount > 1) { int firstIndex = cleanedText.IndexOf(separator[0]); cleanedText = cleanedText.Substring(0, firstIndex + 1) + new string(cleanedText.Substring(firstIndex + 1).Where(char.IsDigit).ToArray()); }
                if (cleanedText != currentText) { cashSumTextBox.Text = cleanedText; cashSumTextBox.CaretIndex = cleanedText.Length; }
            }
            var separatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (cashSumTextBox.Text.Contains(separatorChar))
            {
                int decimalIndex = cashSumTextBox.Text.IndexOf(separatorChar); string text = cashSumTextBox.Text;
                if (text.Length - decimalIndex - 1 < 2) cashSumTextBox.Text = text.Substring(0, decimalIndex + 1) + text.Substring(decimalIndex + 1).PadRight(2, '0');
                else if (text.Length - decimalIndex - 1 > 2) cashSumTextBox.Text = text.Substring(0, decimalIndex + 3);
            }
        }
        #endregion

        private async void Pay_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5: e.Handled = true; Button1_Click(sender, e); break;
                case Key.F12: e.Handled = true; button2_Click(null, null); break;
                case Key.Y: e.Handled = true; this.CashSum = this.PaySum; ClearNonCash(); cashSumTextBox?.Focus(); break;
                case Key.R: e.Handled = true; FillNonCashFromPaySum(); ClearCash(); this.FindControl<TextBox>("non_cash_sum")?.Focus(); break;
                case Key.F8: e.Handled = true; await ShowCertificatesDialog(); break;
            }
        }

        private async Task ShowCertificatesDialog()
        {
            try
            {
                var inputSertificates = new InputSertificates();
                if (_certificatesList.Count > 0) inputSertificates.LoadExistingCertificates(_certificatesList);
                inputSertificates.Topmost = true;
                await inputSertificates.ShowDialog<List<InputSertificates.CertificateItem>>(this);
                var updatedCertificates = inputSertificates.Tag as List<InputSertificates.CertificateItem>;
                if (updatedCertificates != null) await ProcessCertificatesData(updatedCertificates);
            }
            catch (Exception ex)
            {
                await MessageBoxHelper.Show($"Ошибка открытия формы сертификатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
            }
            await Dispatcher.UIThread.InvokeAsync(() => { this.Focus(); this.Activate(); cashSumTextBox?.Focus(); this.Topmost = true; }, DispatcherPriority.Render);
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
            if (string.IsNullOrWhiteSpace(non_cash_sum.Text))
            {
                await MessageBoxHelper.Show("У вас пустое поле оплата по карте. Сделайте фото и создайте заявку в ит отдел.", "Проверки при оплате картой", MessageBoxButton.OK, MessageBoxType.Error, this);
                return false;
            }
            if (non_cash_sum.Text.Trim().Length > 0)
            {
                if (int.TryParse(non_cash_sum.Text.Trim(), out int rubles) && rubles == 0)
                {
                    if (short.TryParse(non_cash_sum_kop.Text.Trim(), out short kopecks) && kopecks > 0)
                    {
                        MessageBoxResult dialogResult = await MessageBoxHelper.Show("У вас заполнены копейки для оплаты по карте, но не заполнена целая часть суммы оплаты по карте.\n\nЕсли вы выберете ДА, тогда копейки будут оплачены по карте.\nЕсли вы выберете НЕТ, то копейки обнулятся.", "Проверки при оплате картой", MessageBoxButton.YesNo, MessageBoxType.Question, this);
                        if (dialogResult == MessageBoxResult.No) { non_cash_sum_kop.Text = "0"; return false; }
                    }
                }
            }
            return true;
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            if (!this.button_pay.IsEnabled == true) return;
            if (!await copFilledCorrectly()) { CalculateChange(); return; }

            await Dispatcher.UIThread.InvokeAsync(() => { this.button_pay.IsEnabled = false; }, DispatcherPriority.Render);

            double cash_money = Math.Round(Convert.ToDouble(txtB_cash_sum.Text.Replace(".", ",")), 2);
            double non_cash_money = Math.Round(Convert.ToDouble(get_non_cash_sum()), 2);
            double sertificate_money = Math.Round(Convert.ToDouble(sertificates_sum.Text), 2);
            double bonus_money = Math.Round(Convert.ToDouble(pay_bonus_many.Text.Replace(".", ",")), 2);
            double sum_on_document = Math.Round(Convert.ToDouble(pay_sum.Text.Replace(".", ",")), 2);
            double all_cash_non_cash = cash_money + non_cash_money + sertificate_money + bonus_money;

            if (Math.Round(all_cash_non_cash, 2) - Math.Round(sum_on_document, 2) < 0)
            {
                await MessageBoxHelper.Show("Проверьте сумму внесенной оплаты", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                return;
            }

            TextBox Remainder = this.FindControl<TextBox>("remainder");
            if (Convert.ToDouble(Remainder.Text.Trim()) > 0 && cc.check_type.SelectedIndex != 0)
            {
                await MessageBoxHelper.Show(" Сумма возврата должна быть равно сумме оплаты ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                return;
            }

            if (Convert.ToDouble(pay_bonus_many.Text) != 0) bonus_on_document.Text = "0";

            if (Convert.ToDouble(pay_bonus_many.Text) > 0)
            {
                if (Convert.ToDouble(non_cash_sum.Text) + Convert.ToDouble(sertificates_sum.Text) + Convert.ToDouble(pay_bonus_many.Text) > Convert.ToDouble(pay_sum.Text))
                {
                    await MessageBoxHelper.Show("Сумма сертификатов + сумма по карте оплаты + сумма по бонусам превышает сумму чека ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return;
                }
            }
            else
            {
                if (Convert.ToDouble(non_cash_sum.Text) + Convert.ToDouble(sertificates_sum.Text) > Convert.ToDouble(pay_sum.Text))
                {
                    await MessageBoxHelper.Show(" Сумма сертификатов + сумма по карте оплаты превышает сумму чека ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return;
                }
            }

            cc.SetCertificatesFromPay(_certificatesList);
            MainStaticClass.write_event_in_log("Окно оплаты перед записью и закрытием документа ", "Документ чек", cc.numdoc.ToString());

            Double _cash_summ_ = Convert.ToDouble(txtB_cash_sum.Text) - Convert.ToDouble(remainder.Text);
            Double _non_cash_summ_ = Math.Round(Convert.ToDouble(get_non_cash_sum()), 2);
            Double _sertificates_sum_ = Convert.ToDouble(sertificates_sum.Text);
            Double _pay_bonus_many_ = Convert.ToDouble(pay_bonus_many.Text);
            Double sum_of_the_document = Convert.ToDouble(cc.calculation_of_the_sum_of_the_document());

            if ((MainStaticClass.GetWorkSchema == 1) || (MainStaticClass.GetWorkSchema == 3) || (MainStaticClass.GetWorkSchema == 4))
            {
                if (Math.Round(sum_of_the_document, 2) != Math.Round((_cash_summ_ + _non_cash_summ_ + _sertificates_sum_ + _pay_bonus_many_), 2))
                {
                    await MessageBoxHelper.Show(" Повторно внесите суммы оплаты, обнаружено не схождение в окне оплаты ", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return;
                }
            }

            if (cc.check_type.SelectedIndex == 1)
            {
                if (!MainStaticClass.validate_cash_sum_non_cash_sum_on_return(cc.id_sale, _cash_summ_, _non_cash_summ_)) return;
            }

            await it_is_paid();
        }

        private string CalculateMoneyInKopecks(string rublesText, string kopecksText)
        {
            if (!long.TryParse(rublesText?.Trim(), out long rubles) || rubles < 0) rubles = 0;
            string kopRaw = kopecksText?.Trim() ?? "";
            if (!int.TryParse(new string(kopRaw.Where(char.IsDigit).Take(2).ToArray()), out int kopecks)) kopecks = 0;
            kopecks = Math.Max(0, Math.Min(99, kopecks));
            return (rubles * 100 + kopecks).ToString();
        }

        private async Task it_is_paid()
        {
            if (cc.check_type.SelectedIndex == 0) // ОПЛАТА
            {
                if ((Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text)) < 0)
                {
                    await MessageBoxHelper.Show("Ошибка при определении суммы наличных", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return;
                }

                if (Convert.ToDecimal(pay_sum.Text) - (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text) + Convert.ToDecimal(sertificates_sum.Text) + Convert.ToDecimal(pay_bonus_many.Text) + Convert.ToDecimal(non_cash_sum.Text)) > 1)
                {
                    await MessageBoxHelper.Show(" Неверно внесенные суммы ", "Проверка оплаты", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return;
                }

                if (!cc.ValidateCheckSumAtDiscount())
                {
                    await MessageBoxHelper.Show(" При распределении расчетов получилась нулевая/отрицательная сумма в строке, попробуйте ввести суммы оплаты еще раз", "Проверка суммы со скидкой", MessageBoxButton.OK, MessageBoxType.Error, this);
                    return;
                }

                double notCashSum = Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100;

                if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != "") && notCashSum > 0)
                {
                    if (checkBox_do_not_send_payment_to_the_terminal.IsChecked != true)
                    {
                        string money = CalculateMoneyInKopecks(this.non_cash_sum.Text.Trim(), non_cash_sum_kop.Text.Trim());

                        if (MainStaticClass.GetAcquiringBank == 1) //ВТБ
                        {
                            string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;

                            if (checkBox_payment_by_sbp.IsChecked != true)
                            {
                                #region Обычная оплата картой (РНКБ)
                                string _str_command_sale_ = str_command_sale.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);

                                MainStaticClass.write_event_in_log($"Оплата картой: {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
                                var terminalResult = await WaitNonCashPay.ShowAndWaitAsync(this, 80, url, _str_command_sale_, this.cc);

                                if (!terminalResult.IsSuccess)
                                {
                                    CalculateChange(); cc.recharge_note = "";
                                    await MessageBoxHelper.Show(terminalResult.ErrorMessage, "Оплата по терминалу", MessageBoxButton.OK, MessageBoxType.Error, this);
                                    return;
                                }

                                cc.code_authorization_terminal = terminalResult.AuthorizationCode;
                                cc.id_transaction_terminal = terminalResult.ReferenceNumber;
                                cc.recharge_note = terminalResult.RechargeNote;
                                #endregion
                            }
                            else
                            {
                                #region Оплата СБП (ВТБ)
                                string _str_sale_sbp = str_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("guid", cc.guid);

                                MainStaticClass.write_event_in_log($"Оплата СБП (Init): {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
                                var resultSbp = await WaitNonCashPay.SendRequestAsync(url, _str_sale_sbp);

                                TerminalResult finalResult = resultSbp;

                                if (!resultSbp.IsSuccess)
                                {
                                    string _str_payment_status = str_payment_status_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.guid);

                                    var (success, pollResult) = await PollSbpStatusAsync(url, _str_payment_status, "Оплата СБП");

                                    if (!success)
                                    {
                                        CalculateChange(); cc.recharge_note = "";
                                        return;
                                    }
                                    finalResult = pollResult;
                                }

                                cc.code_authorization_terminal = finalResult.AuthorizationCode;
                                cc.id_transaction_terminal = finalResult.ReferenceNumber;
                                cc.payment_by_sbp = true;
                                #endregion
                            }
                        }
                        else if (MainStaticClass.GetAcquiringBank == 2) // СБЕР
                        {
                            var sberService = new SberPaymentService();
                            if (int.TryParse(money, out int amountKopecks))
                            {
                                // Определяем тип оплаты
                                //bool isSbpPayment = checkBox_payment_by_sbp.IsChecked == true;

                                Func<CancellationToken, Task<TerminalResult>> sberp = async (ct) =>
                                {
                                    // ВАЖНО: Здесь передаем все 5 параметров по порядку:
                                    // 1. Сумма
                                    // 2. Команда (1)
                                    // 3. RRN (null)
                                    // 4. isSbpPayment (bool) -> ЭТОТ ПАРАМЕТР БЫЛ ПРОПУЩЕН В ВАШЕМ КОДЕ
                                    // 5. ct (CancellationToken)

                                    var res = await sberService.PayAsync(amountKopecks, 1, null, ct);

                                    return new TerminalResult
                                    {
                                        IsSuccess = res.IsSuccess,
                                        ErrorMessage = res.ErrorMessage,
                                        AuthorizationCode = res.AuthorizationCode,
                                        ReferenceNumber = res.ReferenceNumber,
                                        RechargeNote = res.SlipContent,
                                        CodeResponse = res.IsSuccess ? "1" : "0"
                                    };
                                };

                                var result = await WaitNonCashPay.ShowCustomAndWaitAsync(this, 80, sberp, this.cc);

                                if (!result.IsSuccess)
                                {
                                    CalculateChange();
                                    await MessageBoxHelper.Show(result.ErrorMessage, "Ошибка оплаты Сбер", MessageBoxButton.OK, MessageBoxType.Error, this);
                                    return;
                                }

                                cc.code_authorization_terminal = result.AuthorizationCode;
                                cc.id_transaction_terminal = result.ReferenceNumber;
                                if (!string.IsNullOrEmpty(result.RechargeNote)) cc.recharge_note = result.RechargeNote;
                                
                            }
                        }

                    }
                }

                string sum_cash_pay = (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text)).ToString().Replace(",", ".");
                string non_sum_cash_pay = (get_non_cash_sum()).ToString().Replace(",", ".");
                cc.print_to_button = 0;

                if (await cc.it_is_paid(txtB_cash_sum.Text, cc.calculation_of_the_sum_of_the_document().ToString().Replace(",", "."), remainder.Text.Replace(",", "."), (pay_bonus_many.Text.Trim() == "" ? "0" : pay_bonus_many.Text.Trim()), true, sum_cash_pay, non_sum_cash_pay, Convert.ToDecimal(sertificates_sum.Text).ToString().Replace(",", ".")))
                {
                    cc.closing = false; this.Tag = true; this.Close();
                }
            }
            else // ВОЗВРАТ
            {
                string sum_cash_pay = (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text)).ToString().Replace(",", ".");
                string non_sum_cash_pay = (get_non_cash_sum()).ToString().Replace(",", ".");
                if (cc.check_type.SelectedIndex == 1 && get_non_cash_sum() < 1)
                {
                    sum_cash_pay = (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text) + Convert.ToDecimal(get_non_cash_sum())).ToString().Replace(",", ".");
                    non_sum_cash_pay = "0";
                }

                if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != "") && (Convert.ToDouble(non_cash_sum.Text) > 0))
                {
                    if (checkBox_do_not_send_payment_to_the_terminal.IsChecked != true)
                    {
                        string money = ((Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100) * 100).ToString();

                        if (MainStaticClass.GetAcquiringBank == 1)//РНКБ
                        {
                            string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;

                            if (checkBox_payment_by_sbp.IsChecked != true)
                            {
                                #region Возврат картой (РНКБ)
                                string xmlData = "";
                                if (cc.sale_date.CompareTo(DateTime.Today) < 0)
                                {
                                    xmlData = str_return_sale.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_code_authorization_terminal).Replace("number_reference", cc.sale_id_transaction_terminal);
                                }
                                else
                                {
                                    xmlData = str_cancel_sale.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_code_authorization_terminal).Replace("number_reference", cc.sale_id_transaction_terminal);
                                    if (money.Trim() != (cc.sale_non_cash_money * 100).ToString().Trim()) xmlData = xmlData.Replace("sale_non_cash_money", (cc.sale_non_cash_money * 100).ToString());
                                    else xmlData = xmlData.Replace(@"<field id=""01"">sale_non_cash_money</field>", "");
                                }

                                MainStaticClass.write_event_in_log($"Возврат картой: {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
                                var resultReturn = await WaitNonCashPay.ShowAndWaitAsync(this, 60, url, xmlData, this.cc);

                                if (!resultReturn.IsSuccess)
                                {
                                    CalculateChange();
                                    await MessageBoxHelper.Show($"Неудачная попытка возврата: {resultReturn.ErrorMessage}", "Возврат по терминалу", MessageBoxButton.OK, MessageBoxType.Error, this);
                                    return;
                                }

                                //cc.code_authorization_terminal = resultReturn.AuthorizationCode;
                                cc.code_authorization_terminal = resultReturn.AuthorizationCode ?? string.Empty;
                                cc.id_transaction_terminal = resultReturn.ReferenceNumber;
                                complete = true;
                                #endregion
                            }
                            else
                            {
                                #region Возврат СБП (РНКБ)
                                string _str_return_sale_sbp_ = str_return_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_id_transaction_terminal).Replace("guid", cc.guid_sales);

                                MainStaticClass.write_event_in_log($"Возврат СБП (Init): {money} коп.", "Terminal", cc?.numdoc.ToString() ?? "0");
                                var resultSbpReturn = await WaitNonCashPay.SendRequestAsync(url, _str_return_sale_sbp_);

                                TerminalResult finalResult = resultSbpReturn;

                                if (!resultSbpReturn.IsSuccess)
                                {
                                    string _str_payment_status_return = str_payment_status_return_sale_sbp.Replace("sum", money).Replace("id_terminal", MainStaticClass.IdAcquirerTerminal).Replace("sale_code_authorization_terminal", cc.sale_id_transaction_terminal).Replace("guid", cc.guid_sales);

                                    var (success, pollResult) = await PollSbpStatusAsync(url, _str_payment_status_return, "Возврат СБП");

                                    if (!success)
                                    {
                                        CalculateChange();
                                        return;
                                    }
                                    finalResult = pollResult;
                                }

                                cc.code_authorization_terminal = finalResult.AuthorizationCode;
                                cc.id_transaction_terminal = finalResult.ReferenceNumber;
                                cc.payment_by_sbp = true;
                                #endregion
                            }
                        }
                        else if (MainStaticClass.GetAcquiringBank == 2) // СБЕР (ВОЗВРАТ)
                        {
                            // Определяем, был ли исходный чек оплачен по СБП
                            bool isSbpReturn = checkBox_payment_by_sbp.IsChecked == true;

                            var sberService = new SberPaymentService();
                            if (int.TryParse(money, out int amountKopecks))
                            {
                                Func<CancellationToken, Task<TerminalResult>> sberOp = async (ct) =>
                                {
                                    // ИСПРАВЛЕНО: Добавлен параметр isSbpReturn перед ct
                                    var res = await sberService.PayAsync(amountKopecks, 3, cc.sale_id_transaction_terminal, ct);

                                    return new TerminalResult
                                    {
                                        IsSuccess = res.IsSuccess,
                                        ErrorMessage = res.ErrorMessage,
                                        AuthorizationCode = res.AuthorizationCode,
                                        ReferenceNumber = res.ReferenceNumber,
                                        RechargeNote = res.SlipContent,
                                        CodeResponse = res.IsSuccess ? "1" : "0"
                                    };
                                };

                                var result = await WaitNonCashPay.ShowCustomAndWaitAsync(this, 80, sberOp, this.cc);

                                if (!result.IsSuccess)
                                {
                                    CalculateChange();
                                    await MessageBoxHelper.Show(result.ErrorMessage, "Ошибка возврата Сбер", MessageBoxButton.OK, MessageBoxType.Error, this);
                                    return;
                                }

                                cc.code_authorization_terminal = result.AuthorizationCode;
                                cc.id_transaction_terminal = result.ReferenceNumber;
                                if (!string.IsNullOrEmpty(result.RechargeNote)) cc.recharge_note = result.RechargeNote;

                                complete = true;
                            }
                        }
                    }
                    //cc.sale_cancellation_Click(sum_cash_pay, non_sum_cash_pay);
                    //cc.closing = false;
                    //this.Close();
                }
                cc.sale_cancellation_Click(sum_cash_pay, non_sum_cash_pay);
                cc.closing = false;
                this.Close();
            }
        }

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

        private double get_non_cash_sum()
        {
            double result = 0;
            result += double.Parse(non_cash_sum.Text) + double.Parse(non_cash_sum_kop.Text.Trim().Length == 0 ? "0" : non_cash_sum_kop.Text) / 100;
            return result;
        }

        private void CheckBox_payment_by_sbp_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var nonCashSumTextBox = this.FindControl<TextBox>("non_cash_sum");
                var nonCashSumKopTextBox = this.FindControl<TextBox>("non_cash_sum_kop");

                if (checkBox.IsChecked != true)
                {
                    if (nonCashSumTextBox != null) { nonCashSumTextBox.Text = "0"; nonCashSumTextBox.IsEnabled = false; }
                    if (nonCashSumKopTextBox != null) nonCashSumKopTextBox.Text = "0";
                }
                else
                {
                    if (nonCashSumTextBox != null) nonCashSumTextBox.IsEnabled = true;
                }
                SbpPaymentChanged?.Invoke(this, checkBox.IsChecked ?? false);
            }
        }

        private void CalculateChange()
        {
            var paySumTextBox = this.FindControl<TextBox>("pay_sum");
            var cashSumTextBox = this.FindControl<TextBox>("txtB_cash_sum");
            var nonCashSumTextBox = this.FindControl<TextBox>("non_cash_sum");
            var nonCashSumKopTextBox = this.FindControl<TextBox>("non_cash_sum_kop");
            var sertificatesSumTextBox = this.FindControl<TextBox>("sertificates_sum");
            var bonusManyTextBox = this.FindControl<TextBox>("pay_bonus_many");
            var remainderTextBox = this.FindControl<TextBox>("remainder");

            if (paySumTextBox != null && cashSumTextBox != null && remainderTextBox != null)
            {
                try
                {
                    decimal ParseDecimal(string text) { if (string.IsNullOrWhiteSpace(text)) return 0m; text = text.Replace(",", "."); return decimal.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture); }
                    int ParseInt(string text) { if (string.IsNullOrWhiteSpace(text)) return 0; return int.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture); }

                    decimal paySum = ParseDecimal(paySumTextBox.Text);
                    decimal cashSum = ParseDecimal(cashSumTextBox.Text);
                    decimal nonCashSum = 0;
                    if (nonCashSumTextBox != null) { nonCashSum = ParseDecimal(nonCashSumTextBox.Text); if (nonCashSumKopTextBox != null) nonCashSum += ParseInt(nonCashSumKopTextBox.Text) / 100m; }
                    decimal certificatesSum = sertificatesSumTextBox != null ? ParseDecimal(sertificatesSumTextBox.Text) : 0;
                    decimal bonusSum = bonusManyTextBox != null ? ParseDecimal(bonusManyTextBox.Text) : 0;

                    decimal totalPaid = cashSum + nonCashSum + certificatesSum + bonusSum;
                    decimal remainder = totalPaid - paySum;
                    remainderTextBox.Text = remainder.ToString("F2");

                    if (remainder < 0 || remainder > cashSum) remainderTextBox.Foreground = Brushes.Red;
                    else remainderTextBox.Foreground = Brushes.Green;

                    var buttonPay = this.FindControl<Button>("button_pay");
                    if (buttonPay != null) buttonPay.IsEnabled = totalPaid >= paySum;
                }
                catch { remainderTextBox.Text = "0.00"; remainderTextBox.Foreground = Brushes.Green; var buttonPay = this.FindControl<Button>("button_pay"); if (buttonPay != null) buttonPay.IsEnabled = false; }
            }
        }

        #region Свойства доступа к UI
        public string PaySum { get => this.FindControl<TextBox>("pay_sum")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("pay_sum"); if (textBox != null) { textBox.Text = value; CalculateChange(); } } }
        public string CashSum { get => this.FindControl<TextBox>("txtB_cash_sum")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("txtB_cash_sum"); if (textBox != null) { textBox.Text = value; CalculateChange(); } } }
        public string NonCashSum { get => this.FindControl<TextBox>("non_cash_sum")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("non_cash_sum"); if (textBox != null) { textBox.Text = value; CalculateChange(); } } }
        public string NonCashSumKop { get => this.FindControl<TextBox>("non_cash_sum_kop")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("non_cash_sum_kop"); if (textBox != null) { textBox.Text = value; CalculateChange(); } } }
        public string CertificatesSum { get => this.FindControl<TextBox>("sertificates_sum")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("sertificates_sum"); if (textBox != null) textBox.Text = value; } }
        public string BonusSum { get => this.FindControl<TextBox>("pay_bonus")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("pay_bonus"); if (textBox != null) textBox.Text = value; } }
        public string BonusMany { get => this.FindControl<TextBox>("pay_bonus_many")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("pay_bonus_many"); if (textBox != null) textBox.Text = value; } }
        public string Change { get => this.FindControl<TextBox>("remainder")?.Text ?? string.Empty; set { var textBox = this.FindControl<TextBox>("remainder"); if (textBox != null) textBox.Text = value; } }
        public bool IsSbpPayment { get => this.FindControl<CheckBox>("checkBox_payment_by_sbp")?.IsChecked ?? false; set { var checkBox = this.FindControl<CheckBox>("checkBox_payment_by_sbp"); if (checkBox != null) checkBox.IsChecked = value; } }
        public void ShowBonusControls(bool show) { /* ... реализация ... */ }
        public void ShowSbpControls(bool show) { if (this.FindControl<CheckBox>("checkBox_payment_by_sbp") is CheckBox ch) ch.IsVisible = false; if (this.FindControl<CheckBox>("checkBox_do_not_send_payment_to_the_terminal") is CheckBox ch2) ch2.IsVisible = show; }
        #endregion
    }
}