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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AtolConstants = Atol.Drivers10.Fptr.Constants;

namespace Cash8Avalon
{
    public partial class Pay : Window
    {

        private DataTable _certificatesData = null;
        private List<InputSertificates.CertificateItem> _certificatesList = new List<InputSertificates.CertificateItem>();

        // События для внешней подписки
        public event EventHandler ReturnToDocumentRequested;
        public event EventHandler PaymentConfirmed;
        public event EventHandler<bool> SbpPaymentChanged;

        private bool _firstInput = true;

        //private int curpos = 0;
        //private bool firs_input = true;
        //private int curpos_non_cash = 0;
        //private int curpos_pay_bonus = 0;
        //private bool firs_input_pay_bonus = true;

        private bool firs_input_non_cash = true;
        //public ListView listView_sertificates = null;
        public bool code_it_is_confirmed = false;//При списании бонусов, присланный код подтвержден клиентом  
        private bool complete = false;
        //private string reference_number = "";
        private string str_command_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field></request>";
        string str_return_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">number_reference</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field></request>";
        string str_cancel_sale = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""01"">sale_non_cash_money</field><field id=""04"">643</field><field id = ""25"">4</field><field id=""27"">id_terminal</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">number_reference</field></request>";
        //string str_command_cancel_sale    = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"" >sum</field><field id=""04"">643</field><field id=""14"">number_reference</field><field id = ""25"" >4</field><field id=""27"">id_terminal</field></request>";
        string str_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""14"">guid</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field><field id=""53"">115</field></request>";
        string str_payment_status_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id = ""25"" >1</field><field id=""27"">id_terminal</field><field id=""53"">117</field></request>";
        string str_return_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">guid</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field><field id=""53"">118</field></request>";
        string str_payment_status_return_sale_sbp = @"<?xml version=""1.0"" encoding=""UTF-8""?><request><field id = ""00"">sum</field><field id=""04"">643</field><field id=""13"">sale_code_authorization_terminal</field><field id=""14"">guid</field><field id = ""25"" >29</field><field id=""27"">id_terminal</field><field id=""53"">119</field></request>";
        public Cash_check cc = null;
        private ToolTip toolTip = new ToolTip();

        TextBox cashSumTextBox = null;
        
        public Pay()
        {            
            InitializeComponent();
            this.ShowInTaskbar = false;            
            this.Loaded += Pay_Loaded;            
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
                if (MainStaticClass.GetAcquiringBank == 1)//РНКБ
                {
                    checkBox_payment_by_sbp.Opacity = 1; // Изменить на Opacity!
                    checkBox_payment_by_sbp.IsHitTestVisible = true;
                }
                checkBox_do_not_send_payment_to_the_terminal.Opacity = 1; // Изменить на Opacity!
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
                        new TextBlock
                        {
                            Text = "Если оплата по терминалу для этого чека уже прошла",
                            FontWeight = FontWeight.Bold,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 250
                        },
                        new TextBlock
                        {
                            Text = "Не отправлять запрос об оплате на терминал",
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 250
                        }
                    }
            };

            ToolTip.SetTip(checkBox_do_not_send_payment_to_the_terminal, toolTipContent);
            calculate();
        }

        private void CashSumTextBox_KeyUp(object? sender, KeyEventArgs e)
        {
            calculate();
        }

        //private void TxtB_cash_sum_KeyDown(object? sender, KeyEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeEventHandlers()
        {
            // Обработчики для горячих клавиш
            this.KeyDown += Pay_KeyDown;

           
            

            // Связывание событий
            var checkBoxPaymentBySbp = this.FindControl<CheckBox>("checkBox_payment_by_sbp");
            if (checkBoxPaymentBySbp != null)
            {
                checkBoxPaymentBySbp.IsCheckedChanged += CheckBox_payment_by_sbp_CheckedChanged;
            }

            this.button_pay.Click += Button_pay_Click;

            this.button1.Click += Button1_Click;


            //this.txtB_cash_sum.KeyDown += TxtB_cash_sum_KeyDown;
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



            // Обработка изменения безналичной оплаты
            var nonCashSumTextBox = this.FindControl<TextBox>("non_cash_sum");
            if (nonCashSumTextBox != null)
            {                
                nonCashSumTextBox.KeyDown += NonCashSumTextBox_KeyDown;
                nonCashSumTextBox.Text = "0";
            }          
        }

        private void NonCashSumTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (e.Key == Key.Y || e.Key == Key.R ||
            e.Key == Key.F5 || e.Key == Key.F12 || e.Key == Key.F8)
            {
                return; // Пусть обрабатывает Pay_KeyDown
            }

            // Определяем тип клавиши
            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                             (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isControl = e.Key == Key.Back || e.Key == Key.Delete ||
                             e.Key == Key.Left || e.Key == Key.Right ||
                             e.Key == Key.Home || e.Key == Key.End ||
                             e.Key == Key.Tab || e.Key == Key.Enter ||
                             e.Key == Key.Escape;
            bool isSeparator = e.Key == Key.OemComma || e.Key == Key.OemPeriod;

            // Блокируем все, кроме цифр и управляющих клавиш
            // Для non_cash_sum не нужны разделители (это целые рубли)
            if (!isNumeric && !isControl && !isSeparator)
            {
                e.Handled = true;
                return;
            }

            // Обрабатываем цифровые клавиши
            if (isNumeric)
            {
                e.Handled = true; // Берем обработку на себя

                var currentText = textBox.Text ?? "";
                var selectionStart = textBox.CaretIndex;

                // Получаем цифру из клавиши
                char digit = GetDigitFromKey(e.Key);

                // Если текущий текст "0" или пустой
                if (currentText == "0" || string.IsNullOrEmpty(currentText))
                {
                    textBox.Text = digit.ToString();
                    textBox.CaretIndex = 1;
                }
                else
                {
                    // Вставляем цифру в текущую позицию
                    textBox.Text = currentText.Insert(selectionStart, digit.ToString());
                    textBox.CaretIndex = selectionStart + 1;
                }
            }

            // Блокируем разделители для этого поля (только целые рубли)
            if (isSeparator)
            {
                e.Handled = true;
                return;
            }

            // Вызываем пересчет сдачи после обработки клавиши
            Dispatcher.UIThread.Post(() => CalculateChange(), DispatcherPriority.Background);
        
        }

        private char GetDigitFromKey(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9)
            {
                return (char)('0' + (key - Key.D0));
            }
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return (char)('0' + (key - Key.NumPad0));
            }
            return '0';
        }

        private void Button1_Click(object? sender, RoutedEventArgs e)
        {
            ClearCertificates();
            this.Tag = false;
            this.Close();
        }

        private void Button_pay_Click(object? sender, RoutedEventArgs e)
        {
            button2_Click(null, null);
        }

        #region CashSumTextBox Handlers

        private void OnCashSumGotFocus(object sender, GotFocusEventArgs e)
        {
            if (cashSumTextBox == null) return;

            if (cashSumTextBox.Text == "0,00")
            {
                //cashSumTextBox.Text = "";
                _firstInput = true;
            }
        }

        private void OnCashSumLostFocus(object sender, RoutedEventArgs e)
        {
            if (cashSumTextBox == null) return;

            if (string.IsNullOrWhiteSpace(cashSumTextBox.Text))
            {
                cashSumTextBox.Text = "0,00";
                _firstInput = true;
            }
            else
            {
                if (decimal.TryParse(cashSumTextBox.Text.Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                {
                    cashSumTextBox.Text = result.ToString("F2");
                }
                else
                {
                    cashSumTextBox.Text = "0,00";
                }
                _firstInput = true;
            }
        }

        private void OnCashSumTextInput(object sender, TextInputEventArgs e)
        {
            if (cashSumTextBox == null) return;

            // Проверяем текст ввода
            if (string.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            // Проверяем первый символ
            char inputChar = e.Text[0];

            // Разрешаем только: цифры, разделитель, управляющие символы
            bool isDigit = char.IsDigit(inputChar);
            bool isSeparator = inputChar == ',' || inputChar == '.';
            bool isControlChar = char.IsControl(inputChar);

            // ЕСЛИ НЕ цифра, НЕ разделитель, НЕ управляющий символ -> БЛОКИРУЕМ
            if (!isDigit && !isSeparator && !isControlChar)
            {
                e.Handled = true; // БЛОКИРУЕМ ввод букв и других символов
                return;
            }

            var selectionStart = cashSumTextBox.CaretIndex;
            var currentText = cashSumTextBox.Text ?? "";

            if (isDigit)
            {
                if (_firstInput)
                {
                    _firstInput = false;
                    cashSumTextBox.Text = inputChar + currentText.Substring(1);
                    e.Handled = true;
                    cashSumTextBox.CaretIndex = 1;
                }
                else
                {
                    cashSumTextBox.Text = currentText.Insert(selectionStart, inputChar.ToString());
                    e.Handled = true;
                    cashSumTextBox.CaretIndex = selectionStart + 1;
                }
            }
            else if (isSeparator)
            {
                string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

                if (!currentText.Contains(separator))
                {
                    cashSumTextBox.Text = currentText.Insert(selectionStart, separator);
                    e.Handled = true;
                    cashSumTextBox.CaretIndex = selectionStart + 1;
                }
                else
                {
                    cashSumTextBox.CaretIndex = currentText.IndexOf(separator) + 1;
                    e.Handled = true;
                }
            }
            // Для управляющих символов (Backspace, Delete и т.д.) разрешаем стандартную обработку
            // Не устанавливаем e.Handled = true

            // Обеспечиваем 2 цифры после запятой
            var separatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (cashSumTextBox.Text.Contains(separatorChar))
            {
                int decimalIndex = cashSumTextBox.Text.IndexOf(separatorChar);
                if (cashSumTextBox.Text.Length - decimalIndex - 1 < 2)
                {
                    cashSumTextBox.Text = cashSumTextBox.Text.Substring(0, decimalIndex + 1) +
                                          cashSumTextBox.Text.Substring(decimalIndex + 1).PadRight(2, '0');
                    cashSumTextBox.CaretIndex = decimalIndex + 1;
                }
            }

            // Исправляем позицию курсора если он в начале
            if (cashSumTextBox.CaretIndex == 0)
            {
                cashSumTextBox.CaretIndex = cashSumTextBox.Text.Length;
            }
        }

        private void OnCashSumKeyDown(object sender, KeyEventArgs e)
        {
            // Обработка Backspace и Delete
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                _firstInput = false;

                // После удаления символа обновляем форматирование
                Task.Delay(10).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (cashSumTextBox != null)
                        {
                            FormatCashSumText();
                        }
                    });
                });
            }
            calculate();
        }

        private void CashSumTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cashSumTextBox == null) return;

            // Автоматическое форматирование при изменении текста
            FormatCashSumText();

            // Пересчет сдачи
            CalculateChange();
        }

        public decimal GetCashSumValue()
        {
            if (cashSumTextBox == null) return 0m;

            if (string.IsNullOrWhiteSpace(cashSumTextBox.Text) || cashSumTextBox.Text == "0,00")
                return 0m;

            return decimal.Parse(cashSumTextBox.Text.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public void SetCashSumValue(decimal value)
        {
            if (cashSumTextBox != null)
            {
                cashSumTextBox.Text = value.ToString("F2");
                _firstInput = true;
            }
        }

        private void ClearCashSumTextBox()
        {
            if (cashSumTextBox != null)
            {
                cashSumTextBox.Text = "0,00";
                _firstInput = true;
            }
        }

        private bool ValidateCashSumInput()
        {
            if (cashSumTextBox == null) return false;

            if (string.IsNullOrWhiteSpace(cashSumTextBox.Text))
            {
                cashSumTextBox.Text = "0,00";
                return true;
            }

            return decimal.TryParse(cashSumTextBox.Text.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        private void FormatCashSumText()
        {
            if (cashSumTextBox == null) return;

            var currentText = cashSumTextBox.Text;

            // Защита от вставки недопустимых символов
            if (!string.IsNullOrEmpty(currentText))
            {
                // Удаляем все нецифровые символы кроме разделителя
                var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                var cleanedText = new string(currentText
                    .Where(c => char.IsDigit(c) || c == separator[0])
                    .ToArray());

                // Убираем лишние разделители (оставляем только первый)
                int separatorCount = cleanedText.Count(c => c == separator[0]);
                if (separatorCount > 1)
                {
                    int firstIndex = cleanedText.IndexOf(separator[0]);
                    cleanedText = cleanedText.Substring(0, firstIndex + 1) +
                                 new string(cleanedText.Substring(firstIndex + 1)
                                 .Where(char.IsDigit).ToArray());
                }

                if (cleanedText != currentText)
                {
                    cashSumTextBox.Text = cleanedText;
                    cashSumTextBox.CaretIndex = cleanedText.Length;
                }
            }

            // Обеспечиваем 2 цифры после запятой
            var separatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (cashSumTextBox.Text.Contains(separatorChar))
            {
                int decimalIndex = cashSumTextBox.Text.IndexOf(separatorChar);
                string text = cashSumTextBox.Text;

                if (text.Length - decimalIndex - 1 < 2)
                {
                    cashSumTextBox.Text = text.Substring(0, decimalIndex + 1) +
                                         text.Substring(decimalIndex + 1).PadRight(2, '0');
                }
                else if (text.Length - decimalIndex - 1 > 2)
                {
                    // Ограничиваем до 2 знаков после запятой
                    cashSumTextBox.Text = text.Substring(0, decimalIndex + 3);
                }
            }
        }

        #endregion

        private async void Pay_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    e.Handled = true;
                    Button1_Click(sender, e);
                    break;
                case Key.F12:
                    e.Handled = true;
                    button2_Click(null, null);
                    break;
                case Key.Y:
                    e.Handled = true;
                    // Устанавливаем наличные = сумме чека, обнуляем безнал
                    this.CashSum = this.PaySum;
                    ClearNonCash();
                    break;
                case Key.R:
                    e.Handled = true;
                    // Устанавливаем безнал = сумме чека, обнуляем наличные
                    FillNonCashFromPaySum();
                    ClearCash();
                    break;
                case Key.F8:
                    e.Handled = true;
                    await ShowCertificatesDialog();
                    break;
            }
        }

        private async Task ShowCertificatesDialog()
        {
            try
            {
                var inputSertificates = new InputSertificates();

                // ПЕРЕДАЕМ СУЩЕСТВУЮЩИЕ СЕРТИФИКАТЫ
                if (_certificatesList.Count > 0)
                {
                    inputSertificates.LoadExistingCertificates(_certificatesList);
                }

                // Открываем как модальное окно
                await inputSertificates.ShowDialog<List<InputSertificates.CertificateItem>>(this);

                // Получаем обновленный список сертификатов
                var updatedCertificates = inputSertificates.Tag as List<InputSertificates.CertificateItem>;
                if (updatedCertificates != null)
                {
                    // Обрабатываем обновленные данные
                    await ProcessCertificatesData(updatedCertificates);
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка открытия формы сертификатов: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxType.Error);
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
                        // СОХРАНЯЕМ СЕРТИФИКАТЫ
                        _certificatesList = certificates;

                        // Считаем сумму сами
                        decimal totalAmount = certificates.Sum(c => c.Amount);

                        // Обновляем сумму в интерфейсе
                        this.CertificatesSum = totalAmount.ToString("F2");
                        CalculateChange();

                        // Логирование
                        MainStaticClass.write_event_in_log(
                            $"Добавлено {certificates.Count} сертификатов на сумму {totalAmount:F2}",
                            "Сертификаты",
                            cc?.numdoc.ToString() ?? "0"
                        );
                    }
                    else
                    {
                        // Пустой список - очищаем
                        ClearCertificates();
                    }
                }
                else
                {
                    // Неправильный формат данных
                    ClearCertificates();
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка обработки сертификатов: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxType.Error);
                ClearCertificates();
            }
        }

        private void ClearCertificates()
        {
            _certificatesList.Clear();
            _certificatesData?.Clear();
            this.CertificatesSum = "0,00";
            CalculateChange();
        }

        // Метод для получения суммы сертификатов
        public decimal GetCertificatesTotal()
        {
            return _certificatesList.Sum(c => c.Amount);
        }

        // Метод для получения количества сертификатов
        public int GetCertificatesCount()
        {
            return _certificatesList.Count;
        }

        private void FillNonCashFromPaySum()
        {
            // Получаем сумму чека и разделяем на рубли и копейки
            if (decimal.TryParse(this.PaySum.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal paySum))
            {
                // Округляем до 2 знаков после запятой
                paySum = Math.Round(paySum, 2, MidpointRounding.AwayFromZero);

                // Разделяем на рубли и копейки
                int rubles = (int)Math.Floor(paySum);
                int kopecks = (int)((paySum - rubles) * 100);

                // Устанавливаем значения
                this.NonCashSum = rubles.ToString();
                this.NonCashSumKop = kopecks.ToString("00");
            }
            calculate();
        }

        // Обнулить безналичную оплату
        private void ClearNonCash()
        {
            this.NonCashSum = "0";
            this.NonCashSumKop = "00";
            calculate();
        }

        // Обнулить наличные
        private void ClearCash()
        {
            this.CashSum = "0,00";
            calculate();
        }




        //private void button1_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Tag = false;
        //    this.Close();            
        //}


        private async Task<bool> copFilledCorrectly()
        {
            bool result = true;

            if (string.IsNullOrWhiteSpace(non_cash_sum.Text))
            {
                await MessageBox.Show(
                    "У вас пустое поле оплата по карте. Сделайте фото и создайте заявку в ит отдел.",
                    "Проверки при оплате картой",
                    MessageBoxButton.OK,
                    MessageBoxType.Error);
                return false;
            }

            // Проверяем, если целая часть суммы равна 0, но копейки заполнены
            if (non_cash_sum.Text.Trim().Length > 0)
            {
                // Пытаемся преобразовать в число с учетом культуры
                if (int.TryParse(non_cash_sum.Text.Trim(), out int rubles) && rubles == 0)
                {
                    if (short.TryParse(non_cash_sum_kop.Text.Trim(), out short kopecks) && kopecks > 0)
                    {
                        // Используем ваш MessageBox для Avalonia
                        MessageBoxResult dialogResult = await MessageBox.Show(
                            "У вас заполнены копейки для оплаты по карте, но не заполнена целая часть суммы оплаты по карте.\n\n" +
                            "Если вы выберете ДА, тогда копейки будут оплачены по карте.\n" +
                            "Если вы выберете НЕТ, то копейки обнулятся и вам будет необходимо снова выбрать сумму и форму оплаты.",
                            "Проверки при оплате картой",
                            MessageBoxButton.YesNo,
                            MessageBoxType.Question);

                        if (dialogResult == MessageBoxResult.No)
                        {
                            non_cash_sum_kop.Text = "0";
                            result = false;
                        }
                    }
                }
            }

            return result;
        }

        private async void calculate()
        {
            try
            {
                if (this.txtB_cash_sum.Text.Length == 0)
                {
                    this.txtB_cash_sum.Text = "0,00";
                }

                this.txtB_cash_sum.Text = Convert.ToDouble(this.txtB_cash_sum.Text).ToString("F2", System.Globalization.CultureInfo.CurrentCulture);

                this.remainder.Text = Math.Round(
                (double.Parse(txtB_cash_sum.Text) +
                double.Parse(pay_bonus_many.Text) +
                get_non_cash_sum() +
                double.Parse(sertificates_sum.Text) - double.Parse(pay_sum.Text)), 2).ToString("F", System.Globalization.CultureInfo.CurrentCulture);

                if (Math.Round(double.Parse(txtB_cash_sum.Text.Replace(".", ",")) + double.Parse(non_cash_sum.Text) +
                double.Parse(sertificates_sum.Text) + double.Parse(pay_bonus_many.Text) +
                Convert.ToDouble(double.Parse(non_cash_sum_kop.Text.Trim().Length == 0 ? "0" : non_cash_sum_kop.Text) / 100), 2, MidpointRounding.AwayFromZero) - double.Parse(pay_sum.Text.Replace(".", ",")) < 0)
                {
                    this.button_pay.IsEnabled = false;
                }
                else
                {
                    this.button_pay.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show("calculate " + ex.Message,"Ошибка при подсчете",MessageBoxButton.OK,MessageBoxType.Error);
            }           
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            this.button_pay.IsEnabled = false;

            if (!await copFilledCorrectly())
            {
                calculate();
                return;
            }

            //Проверить заполнены копейки или нет 

            double cash_money = Math.Round(Convert.ToDouble(txtB_cash_sum.Text.Replace(".", ",")), 2);
            double non_cash_money = Math.Round(Convert.ToDouble(get_non_cash_sum()), 2);
            double sertificate_money = Math.Round(Convert.ToDouble(sertificates_sum.Text), 2);
            double bonus_money = Math.Round(Convert.ToDouble(pay_bonus_many.Text.Replace(".", ",")), 2);

            double sum_on_document = Math.Round(Convert.ToDouble(pay_sum.Text.Replace(".", ",")), 2);

            double all_cash_non_cash = cash_money + non_cash_money + sertificate_money + bonus_money;

            //if (Math.Round(Convert.ToDouble(cash_sum.Text.Replace(".", ",")),2, MidpointRounding.AwayFromZero) + Math.Round(Convert.ToDouble(get_non_cash_sum(0)),2, MidpointRounding.AwayFromZero) + Math.Round(Convert.ToDouble(sertificates_sum.Text),2, MidpointRounding.AwayFromZero) + Math.Round(Convert.ToDouble(pay_bonus_many.Text.Replace(".", ",")),2, MidpointRounding.AwayFromZero) - Math.Round(Convert.ToDouble(pay_sum.Text.Replace(".", ",")),2, MidpointRounding.AwayFromZero) < 0)
            //if ((Math.Round(all_cash_non_cash, 2) - Math.Round(sum_on_document, 2)) < 0)
            //MessageBox.Show("Всего оплат " + all_cash_non_cash);
            //MessageBox.Show("all_cash_non_cash - sum_on_document=" + (Math.Round(all_cash_non_cash, 2) - Math.Round(sum_on_document, 2)));
            if (Math.Round(all_cash_non_cash, 2) - Math.Round(sum_on_document, 2) < 0)
            {
                //MessageBox.Show("Общая сумма оплат  " + (cash_money + non_cash_money + sertificate_money + bonus_money));
                //double minus = (cash_money + non_cash_money + sertificate_money + bonus_money) - sum_on_document;
                //MessageBox.Show(minus.ToString());
                await MessageBox.Show("Проверьте сумму внесенной оплаты");
                await MessageBox.Show("Наличные" + Math.Round(Convert.ToDouble(txtB_cash_sum.Text.Replace(".", ",")), 2).ToString());
                await MessageBox.Show("Карта " + Math.Round(Convert.ToDouble(get_non_cash_sum()), 2).ToString());
                await MessageBox.Show("Сертификаты " + Math.Round(Convert.ToDouble(sertificates_sum.Text), 2).ToString());
                await MessageBox.Show("Бонусы " + Math.Round(Convert.ToDouble(pay_bonus_many.Text.Replace(".", ",")), 2).ToString());
                await MessageBox.Show("Общая сумма  " + Math.Round(Convert.ToDouble(pay_sum.Text.Replace(".", ",")), 2));

                return;
            }

            TextBox Remainder = this.FindControl<TextBox>("remainder");
            if (Convert.ToDouble(Remainder.Text.Trim()) > 0)
            {
                if (cc.check_type.SelectedIndex != 0)
                {
                    await MessageBox.Show(" Сумма возврата должна быть равно сумме оплаты ");
                    return;
                }
            }

            if (Convert.ToDouble(pay_bonus_many.Text) != 0)//При оплате бонусами бонусы не начисляются
            {
                bonus_on_document.Text = "0";
            }

            if (Convert.ToDouble(pay_bonus_many.Text) > 0)
            {
                if (Convert.ToDouble(non_cash_sum.Text) + Convert.ToDouble(sertificates_sum.Text) + Convert.ToDouble(pay_bonus_many.Text) > Convert.ToDouble(pay_sum.Text))
                {
                    await MessageBox.Show("Сумма сертификатов + сумма по карте оплаты + сумма по бонусам превышает сумму чека ");
                    return;
                }
            }
            else
            {
                if (Convert.ToDouble(non_cash_sum.Text) + Convert.ToDouble(sertificates_sum.Text) > Convert.ToDouble(pay_sum.Text))
                {
                    await MessageBox.Show(" Сумма сертификатов + сумма по карте оплаты превышает сумму чека ");
                    return;
                }
            }



            //cc.listView_sertificates.Items.Clear();
            //foreach (ListViewItem lvi in listView_sertificates.Items)
            //{
            //    cc.listView_sertificates.Items.Add((ListViewItem)lvi.Clone());
            //}

            cc.SetCertificatesFromPay(_certificatesList);

            MainStaticClass.write_event_in_log("Окно оплаты перед записью и закрытием документа ", "Документ чек", cc.numdoc.ToString());

            //Необходимо проверка на сумму документа где сумма всех форм оплаты равно сумме документа
            //Получаем общу сумму по оплате 
            Double _cash_summ_ = Convert.ToDouble(txtB_cash_sum.Text) - Convert.ToDouble(remainder.Text);
            //MessageBox.Show("Наличные " + _cash_summ_.ToString());
            Double _non_cash_summ_ = Math.Round(Convert.ToDouble(get_non_cash_sum()), 2);
            //MessageBox.Show("Безнал " + _non_cash_summ_.ToString());
            Double _sertificates_sum_ = Convert.ToDouble(sertificates_sum.Text);
            //MessageBox.Show("Сертификаты " + _sertificates_sum_.ToString());
            //decimal _pay_bonus_many_ = Convert.ToDecimal((int)(Convert.ToInt32(pay_bonus_many.Text)/100));
            Double _pay_bonus_many_ = Convert.ToDouble(pay_bonus_many.Text);
            //MessageBox.Show("Бонусы " + _pay_bonus_many_.ToString());
            Double sum_of_the_document = Convert.ToDouble(cc.calculation_of_the_sum_of_the_document());
            //decimal sum_of_the_document = Math.Round(Convert.ToDecimal(pay_sum.Text.Replace(".", ",")), 2);
            //MessageBox.Show("Сумма документа " + sum_of_the_document.ToString());
            
            if ((MainStaticClass.GetWorkSchema == 1) || (MainStaticClass.GetWorkSchema == 3) || (MainStaticClass.GetWorkSchema == 4))
            {
                if (Math.Round(sum_of_the_document, 2) != Math.Round((_cash_summ_ + _non_cash_summ_ + _sertificates_sum_ + _pay_bonus_many_), 2))
                {

                    await MessageBox.Show(" Повторно внесите суммы оплаты, обнаружено не схождение в окне оплаты ");
                    await MessageBox.Show("Сумма документа = " + sum_of_the_document.ToString() + " а сумма оплат = " + (_cash_summ_ + _non_cash_summ_ + _sertificates_sum_ + _pay_bonus_many_).ToString());
                    await MessageBox.Show("Сумма наличные = " + _cash_summ_.ToString());
                    await MessageBox.Show("Сумма карта оплаты = " + _non_cash_summ_.ToString());
                    await MessageBox.Show("Сумма сертификатов = " + _sertificates_sum_.ToString());
                    await MessageBox.Show("Сумма бонусов = " + _pay_bonus_many_.ToString());

                    return;
                }
            }
            

            //Если это возврат то необходимо проверить сумму по каждой форме оплаты 
            if (cc.check_type.SelectedIndex == 1)
            {
                if (!MainStaticClass.validate_cash_sum_non_cash_sum_on_return(cc.id_sale, _cash_summ_, _non_cash_summ_))
                {
                    return;
                }
            }

            await it_is_paid();
        }

        /*Оплачено
        *Это процедура записи документа в базу данных AnswerTerminal
        */
        private async Task it_is_paid()
        {
            //if (cc.it_is_paid(cash_sum.Text,cash_sum.Text, remainder.Text))
            if (cc.check_type.SelectedIndex == 0)
            {
                if ((Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text)) < 0)
                {
                    await MessageBox.Show("Ошибка при определении суммы наличных");
                    return;
                }
                //Получаем копейки которые необходимо распределить
                double total = Convert.ToDouble(pay_sum.Text);


                if (Convert.ToDecimal(pay_sum.Text) - (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text) + Convert.ToDecimal(sertificates_sum.Text) + Convert.ToDecimal(pay_bonus_many.Text) + Convert.ToDecimal(non_cash_sum.Text)) > 1)
                {
                    await MessageBox.Show(" Неверно внесенные суммы ","Проверка оплаты",MessageBoxButton.OK,MessageBoxType.Error);
                    return;
                }

                if (!cc.ValidateCheckSumAtDiscount())
                {
                    await MessageBox.Show(" При распределении расчетов получилась нулевая/отрицательная сумма в строке, попробуйте ввести суммы оплаты еще раз",
                        "Проверка суммы со скидкой", MessageBoxButton.OK, MessageBoxType.Error);
                    return;
                }

                //параметры подключение терминала заполнены и сумма по карте к оплате заполнена
                double notCashSum = Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100;

                if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != "") && notCashSum > 0)
                {
                    if (checkBox_do_not_send_payment_to_the_terminal.IsChecked != true)
                    {

                        string money = ((Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100) * 100).ToString();

                        if (MainStaticClass.GetAcquiringBank == 1) //РНКБ
                        {
                            //if ((checkBox_payment_by_sbp.CheckState != CheckState.Checked) && (checkBox_do_not_send_payment_to_the_terminal.CheckState == CheckState.Unchecked))
                            if (checkBox_payment_by_sbp.IsChecked != true)
                            {
                                string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;
                                //string money = ((Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100) * 100).ToString();
                                string _str_command_sale_ = str_command_sale.Replace("sum", money);
                                _str_command_sale_ = _str_command_sale_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);

                                AnswerTerminal answerTerminal = new AnswerTerminal();

                                WaitNonCashPay waitNonCashPay = new WaitNonCashPay();
                                waitNonCashPay.Url = url;
                                waitNonCashPay.Data = _str_command_sale_;
                                waitNonCashPay.cc = this.cc;
                                var commandResult = await waitNonCashPay.SendCommandWithTimeout(url, _str_command_sale_, this.cc);
                                //if (waitNonCashPay.commandResult != null)
                                if (commandResult.AnswerTerminal != null)
                                {
                                    answerTerminal = waitNonCashPay.commandResult.AnswerTerminal;
                                    complete = waitNonCashPay.commandResult.Status;
                                }
                                else
                                {
                                    await MessageBox.Show("Результат команды не получен.\r\nНеудачная попытка оплаты", "Неудачная попытка оплаты");
                                    calculate();
                                    this.Focus();
                                    return;
                                }


                                if (!complete)//ответ от терминала не удовлетворительный
                                {
                                    calculate();
                                    cc.recharge_note = "";
                                    await MessageBox.Show(" Неудачная попытка получения оплаты ", "Оплата по терминалу");
                                    return;
                                }
                                else
                                {
                                    cc.code_authorization_terminal = answerTerminal.code_authorization;     //13 поле
                                    cc.id_transaction_terminal = answerTerminal.number_reference;  //14 поле                                    
                                }
                            }
                            else
                            {
                                //if(checkBox_do_not_send_payment_to_the_terminal.CheckState == CheckState.Unchecked)
                                string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;
                                //string money = ((Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100) * 100).ToString();
                                string _str_sale_sbp = str_sale_sbp.Replace("sum", money);
                                _str_sale_sbp = _str_sale_sbp.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
                                _str_sale_sbp = _str_sale_sbp.Replace("guid", cc.guid);
                                ////MessageBox.Show(_str_command_sale_);
                                AnswerTerminal answerTerminal = new AnswerTerminal();
                                send_command_acquiring_terminal(url, _str_sale_sbp, ref complete, ref answerTerminal);
                                if (!complete)//ответ от терминала не удовлетворительный, значит операция в обработке необходим дополнительный запрос
                                {
                                    string _str_payment_status_sale_sbp = str_payment_status_sale_sbp.Replace("sum", money);
                                    _str_payment_status_sale_sbp = _str_payment_status_sale_sbp.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
                                    _str_payment_status_sale_sbp = _str_payment_status_sale_sbp.Replace("sale_code_authorization_terminal", cc.guid);
                                    while (1 == 1)
                                    {
                                        answerTerminal = new AnswerTerminal();
                                        send_command_acquiring_terminal(url, _str_payment_status_sale_sbp, ref complete, ref answerTerminal);
                                        if (complete)//получен ответ об успешной оплате, прерываем цикл
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            if (answerTerminal.сode_response_in_15_field == "R00")//Операция в обработке 
                                            {
                                                if (answerTerminal.сode_response_in_39_field == "0")
                                                {
                                                    continue;
                                                }
                                            }

                                            if (answerTerminal.сode_response_in_15_field == "R10")
                                            {
                                                await MessageBox.Show(" Операция отклонена ", "Оплата по терминалу");
                                                break;
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R11")
                                            {
                                                await MessageBox.Show(" Операции по QR коду не существует. ", "Оплата по терминалу");
                                                break;
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R12")
                                            {
                                                if (answerTerminal.сode_response_in_39_field == "0")
                                                {
                                                    await MessageBox.Show(" Не получен ответ на запрос статуса ", "Оплата по терминалу");
                                                    break;
                                                }
                                                else if (answerTerminal.сode_response_in_39_field == "16")
                                                {
                                                    await MessageBox.Show(" Не получен ответ на запрос QR - кода ", "Оплата по терминалу");
                                                    break;
                                                }
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R13")
                                            {
                                                await MessageBox.Show(" Запрос статуса не отправлен ", "Оплата по терминалу");
                                                break;
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R14")
                                            {
                                                await MessageBox.Show(" Операция не добавлена в базу транзакций терминала ", "Оплата по терминалу");
                                                break;
                                            }
                                            if (answerTerminal.error)
                                            {
                                                if (answerTerminal.error_code != 404)
                                                {
                                                    // Используем ваш MessageBox с параметрами YesNo
                                                    MessageBoxResult result = await MessageBox.Show(
                                                        "Продолжать опрос об оплате клиента по СБП",
                                                        "Продолжать опрос об оплате клиента по СБП",
                                                        MessageBoxButton.YesNo,
                                                        MessageBoxType.Question);

                                                    if (result == MessageBoxResult.No)
                                                    {
                                                        // Пользователь отказался от дальнейшего ожидания информации об оплате
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    if (!complete)//если не удалось получить информацию об успешной оплате
                                    {
                                        calculate();
                                        cc.recharge_note = "";
                                        await MessageBox.Show(" Неудачная попытка получения оплаты ", "СБП");
                                        return;
                                    }
                                    else
                                    {
                                        cc.code_authorization_terminal = answerTerminal.code_authorization;     //13 поле
                                        cc.id_transaction_terminal = answerTerminal.number_reference;           //14 поле
                                        cc.payment_by_sbp = (checkBox_payment_by_sbp.IsChecked == true);
                                    }
                                }
                                else//был сразу получен успешный ответ по по оплате СБП
                                {
                                    cc.code_authorization_terminal = answerTerminal.code_authorization;     //13 поле
                                    cc.id_transaction_terminal = answerTerminal.number_reference;           //14 поле
                                    cc.payment_by_sbp = (checkBox_payment_by_sbp.IsChecked == true);
                                }
                            }
                        }
                        else if (MainStaticClass.GetAcquiringBank == 2)//СБЕР
                        {
                            //    try
                            //    {
                            //        CommandWrapper.return_slip = "";
                            //        AuthAnswer13 authAnswer = CommandWrapper.Authorization(Convert.ToInt32(money));
                            //        cc.id_transaction_terminal = authAnswer.RRN;
                            //        if (CommandWrapper.return_slip.Trim().Length != 0)
                            //        {
                            //            IFptr fptr = MainStaticClass.FPTR;
                            //            if (!fptr.isOpened())
                            //            {
                            //                fptr.open();
                            //            }

                            //            fptr.beginNonfiscalDocument();
                            //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, CommandWrapper.return_slip);
                            //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
                            //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
                            //            fptr.printText();
                            //            fptr.endNonfiscalDocument();
                            //            //if (MainStaticClass.GetVariantConnectFN == 1)
                            //            //{
                            //            //    fptr.close();
                            //            //}
                            //        }
                            //        else
                            //        {
                            //            MessageBox.Show(" Не удалось получить слип с терминала ", "Неудачная попытка оплаты по терминалу");
                            //            calculate();
                            //            return;
                            //        }
                            //        //authAnswer.
                            //        //Trace.WriteLine("Списание произвели. RRN:{authAnswer.RRN}. CardNumber:{authAnswer.CardID}");
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        MessageBox.Show("Произошла ошибка при попытке оплаты по терминалу \r\n" + ex.Message);
                            //        calculate();
                            //        return;
                            //    }
                            //}
                            //else
                            //{
                            //    MessageBox.Show(" У вас в константах не выбран банк эквайринга");
                            //    calculate();
                            //    return;
                            //}
                        }
                    }
                }


                //Получить сумму наличных
                //если это возврат и если сумма безнала меньше 1 тогда копейки прибавить к наличным
                string sum_cash_pay = (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text)).ToString().Replace(",", ".");
                string non_sum_cash_pay = (get_non_cash_sum()).ToString().Replace(",", ".");
                cc.print_to_button = 0;
                //cc.payment_by_sbp = (checkBox_payment_by_sbp.CheckState == CheckState.Checked ? true : false);//Перенес выше в секцию РНКБ, здесь было до появления сбера
                if (await cc.it_is_paid(txtB_cash_sum.Text, cc.calculation_of_the_sum_of_the_document().ToString().Replace(",", "."), remainder.Text.Replace(",", "."),
                (pay_bonus_many.Text.Trim() == "" ? "0" : pay_bonus_many.Text.Trim()),
                true,
            sum_cash_pay,
            non_sum_cash_pay,
            Convert.ToDecimal(sertificates_sum.Text).ToString().Replace(",", ".")))
                {
                    cc.closing = false;
                    // Закрываем с результатом OK
                    //(this.Parent as Window)?.Close();
                    // Или если это окно:
                    this.Tag = true;
                    this.Close();
                }
            }
            else//ЭТО ВОЗВРАТ
            {

                string sum_cash_pay = (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text)).ToString().Replace(",", ".");
                string non_sum_cash_pay = (get_non_cash_sum()).ToString().Replace(",", ".");
                if (cc.check_type.SelectedIndex == 1)
                {
                    if (get_non_cash_sum() < 1)
                    {
                        sum_cash_pay = (Convert.ToDecimal(txtB_cash_sum.Text) - Convert.ToDecimal(remainder.Text) + Convert.ToDecimal(get_non_cash_sum())).ToString().Replace(",", ".");
                        non_sum_cash_pay = "0";
                    }
                }

                //здесь надо понимать возврат сегодняшний или более ранний

                if ((MainStaticClass.IpAddressAcquiringTerminal.Trim() != "") && (MainStaticClass.IdAcquirerTerminal.Trim() != "") && (Convert.ToDouble(non_cash_sum.Text) > 0))
                {
                    if (checkBox_do_not_send_payment_to_the_terminal.IsChecked == true)
                    {
                        string money = ((Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100) * 100).ToString();

                        if (MainStaticClass.GetAcquiringBank == 1)//РНКБ
                        {
                            string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;
                            //string money = ((Convert.ToDouble(this.non_cash_sum.Text.Trim()) + Convert.ToDouble(non_cash_sum_kop.Text) / 100) * 100).ToString();
                            //Поскольку нет автоматической конвертации отмены в возврат, то необходимо 2 варианта печати для возвратов                     
                            DateTime today = DateTime.Today;
                            AnswerTerminal answerTerminal = new AnswerTerminal();
                            if (checkBox_payment_by_sbp.IsChecked == true)
                            {
                                if (cc.sale_date.CompareTo(today) < 0)
                                {
                                    string _str_return_sale_ = str_return_sale.Replace("sum", money);
                                    _str_return_sale_ = _str_return_sale_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
                                    _str_return_sale_ = _str_return_sale_.Replace("sale_code_authorization_terminal", cc.sale_code_authorization_terminal);
                                    _str_return_sale_ = _str_return_sale_.Replace("number_reference", cc.sale_id_transaction_terminal);
                                    send_command_acquiring_terminal(url, _str_return_sale_, ref complete, ref answerTerminal);
                                }
                                else
                                {
                                    string _str_return_sale_ = str_cancel_sale.Replace("sum", money);
                                    _str_return_sale_ = _str_return_sale_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
                                    _str_return_sale_ = _str_return_sale_.Replace("sale_code_authorization_terminal", cc.sale_code_authorization_terminal);
                                    _str_return_sale_ = _str_return_sale_.Replace("number_reference", cc.sale_id_transaction_terminal);
                                    if (money.Trim() != (cc.sale_non_cash_money * 100).ToString().Trim())//Это частичная отмена.
                                    {
                                        _str_return_sale_ = _str_return_sale_.Replace("sale_non_cash_money", (cc.sale_non_cash_money * 100).ToString());
                                    }
                                    else
                                    {
                                        _str_return_sale_ = _str_return_sale_.Replace(@"<field id=""01"">sale_non_cash_money</field>", "");
                                    }

                                    send_command_acquiring_terminal(url, _str_return_sale_, ref complete, ref answerTerminal);
                                }
                            }
                            else
                            {
                                string _str_return_sale_sbp_ = str_return_sale_sbp.Replace("sum", money);
                                _str_return_sale_sbp_ = _str_return_sale_sbp_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
                                _str_return_sale_sbp_ = _str_return_sale_sbp_.Replace("sale_code_authorization_terminal", cc.sale_id_transaction_terminal);// cc.sale_code_authorization_terminal);
                                _str_return_sale_sbp_ = _str_return_sale_sbp_.Replace("guid", cc.guid_sales);
                                send_command_acquiring_terminal(url, _str_return_sale_sbp_, ref complete, ref answerTerminal);
                                if (!complete)//ответ от терминала не удовлетворительный
                                {
                                    string _str_payment_status_return_sale_sbp_ = str_payment_status_return_sale_sbp.Replace("sum", money);
                                    _str_payment_status_return_sale_sbp_ = _str_payment_status_return_sale_sbp_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
                                    _str_payment_status_return_sale_sbp_ = _str_payment_status_return_sale_sbp_.Replace("sale_code_authorization_terminal", cc.sale_id_transaction_terminal);// cc.sale_code_authorization_terminal);
                                    _str_payment_status_return_sale_sbp_ = _str_payment_status_return_sale_sbp_.Replace("guid", cc.guid_sales);
                                    //send_command_acquiring_terminal(url, _str_payment_status_return_sale_sbp_, ref complete, ref answerTerminal);
                                    while (1 == 1)
                                    {
                                        answerTerminal = new AnswerTerminal();
                                        send_command_acquiring_terminal(url, _str_payment_status_return_sale_sbp_, ref complete, ref answerTerminal);
                                        if (complete)//получен ответ об успешной оплате, прерываем цикл
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            if (answerTerminal.сode_response_in_15_field == "R10")
                                            {
                                                await MessageBox.Show(" Операция отклонена ");
                                                break;
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R11")
                                            {
                                                await MessageBox.Show(" Операции по QR коду не существует. ");
                                                break;
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R12")
                                            {
                                                if (answerTerminal.сode_response_in_39_field == "0")
                                                {
                                                    await MessageBox.Show(" Не получен ответ на запрос статуса ");
                                                    break;
                                                }
                                                else if (answerTerminal.сode_response_in_39_field == "16")
                                                {
                                                    await MessageBox.Show(" Не получен ответ на запрос QR - кода ");
                                                    break;
                                                }
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R13")
                                            {
                                                await MessageBox.Show(" Запрос статуса не отправлен ");
                                                break;
                                            }
                                            else if (answerTerminal.сode_response_in_15_field == "R14")
                                            {
                                                await MessageBox.Show(" Операция не добавлена в базу транзакций терминала ");
                                                break;
                                            }
                                            if (answerTerminal.error)
                                            {
                                                MessageBoxResult result = await MessageBox.Show(
                                                    "Продолжать опрос по возврату оплаты по СБП",
                                                    "Опрос по возврату оплаты по СБП",
                                                    MessageBoxButton.YesNo,
                                                    MessageBoxType.Question);

                                                if (result == MessageBoxResult.No)
                                                {
                                                    // Пользователь отказался от дальнейшего ожидания информации об оплате
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (!complete)//ответ от терминала не удовлетворительный
                            {
                                calculate();
                                await MessageBox.Show(" Неудачная попытка возврата оплаты ", "СБП");
                                return;
                            }
                            else
                            {
                                cc.code_authorization_terminal = answerTerminal.code_authorization;//13 поле
                                cc.id_transaction_terminal = answerTerminal.number_reference;  //14 поле 
                                cc.payment_by_sbp = (checkBox_payment_by_sbp.IsChecked == true);
                            }
                        }
                        else if (MainStaticClass.GetAcquiringBank == 2)//СБЕР
                        {
                            //    try
                            //    {
                            //        //AuthAnswer13 authAnswer = CommandWrapper.Authorization(Convert.ToInt32(money));
                            //        //cc.id_transaction_terminal = authAnswer.RRN;
                            //        //Trace.WriteLine("Списание произвели. RRN:{authAnswer.RRN}. CardNumber:{authAnswer.CardID}");
                            //        CommandWrapper.return_slip = "";
                            //        AuthAnswer13 authAnswer = CommandWrapper.ReturnAmountToCard(Convert.ToInt32(money), cc.sale_id_transaction_terminal);
                            //        cc.id_transaction_terminal = authAnswer.RRN;

                            //        if (CommandWrapper.return_slip.Trim().Length != 0)
                            //        {
                            //            IFptr fptr = MainStaticClass.FPTR;
                            //            if (!fptr.isOpened())
                            //            {
                            //                fptr.open();
                            //            }

                            //            fptr.beginNonfiscalDocument();
                            //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, CommandWrapper.return_slip);
                            //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
                            //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
                            //            fptr.printText();
                            //            fptr.endNonfiscalDocument();
                            //            //if (MainStaticClass.GetVariantConnectFN == 1)
                            //            //{
                            //            //    fptr.close();
                            //            //}
                            //        }
                            //        else
                            //        {
                            //            MessageBox.Show(" Не удалось получить слип с терминала ", "Неудачная возврата средств по терминалу");
                            //            calculate();
                            //            return;
                            //        }

                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        MessageBox.Show("Произошла ошибка при попытке возврата средств по терминалу \r\n" + ex.Message);
                            //        calculate();
                            //        return;
                            //    }
                            //}
                            //else
                            //{
                            //    MessageBox.Show(" У вас в константах не выбран банк эквайринга");
                            //    calculate();
                            //    return;
                            //}
                        }
                    }

                    cc.sale_cancellation_Click(sum_cash_pay, non_sum_cash_pay);
                    cc.closing = false;
                    //this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
            
        

        public class AnswerTerminal
        {
            public string code_authorization { get; set; }
            public string number_reference { get; set; }
            public string сode_response_in_15_field { get; set; }
            public string сode_response_in_39_field { get; set; }
            public bool error { get; set; }
            public int error_code { get; set; }

            public AnswerTerminal()
            {
                number_reference = "";
                code_authorization = "";
            }
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


        /// <summary>
        /// Отправляет команду в эквайринг
        /// терминал и возвращает результат
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="Data"></param>
        /// <param name="status"></param>
        public void send_command_acquiring_terminal(string Url, string Data, ref bool status, ref AnswerTerminal answerTerminal)
        {
            //string Out = String.Empty;

            try
            {
                System.Net.WebRequest req = WebRequest.Create(Url);
                req.Method = "POST";
                req.Timeout = 80000;
                //req.Timeout = 0;
                req.ContentType = "text/xml;charset = windows-1251";
                //req.ContentType = "text/xml;charset = UTF-8";                
                byte[] sentData = Encoding.GetEncoding("Windows-1251").GetBytes(Data);
                //byte[] sentData = Encoding.UTF8.GetBytes(Data);
                req.ContentLength = sentData.Length;
                System.IO.Stream sendStream = req.GetRequestStream();
                sendStream.Write(sentData, 0, sentData.Length);
                sendStream.Close();
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)req.GetResponse();
                if (myHttpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    var streamReader = new StreamReader(myHttpWebResponse.GetResponseStream(), Encoding.GetEncoding("Windows-1251"));
                    var responseContent = streamReader.ReadToEnd();

                    XmlSerializer serializer = new XmlSerializer(typeof(Response));
                    using (StringReader reader = new StringReader(responseContent))
                    {
                        var test = (Response)serializer.Deserialize(reader);
                        foreach (Field field in test.Field)
                        {
                            if (field.Id == "39")
                            {
                                answerTerminal.сode_response_in_39_field = field.Text;
                                if (field.Text.Trim() == "1")
                                {
                                    status = true;
                                }
                                else
                                {
                                    status = false;
                                }
                            }
                            else if (field.Id == "13")
                            {
                                answerTerminal.code_authorization = field.Text.Trim();
                            }
                            else if (field.Id == "14")
                            {
                                answerTerminal.number_reference = field.Text.Trim();
                            }
                            else if (field.Id == "15")
                            {
                                answerTerminal.сode_response_in_15_field = field.Text.Trim();
                            }
                            else if (field.Id == "90")
                            {
                                cc.recharge_note = field.Text.Trim();
                                int num_pos = cc.recharge_note.IndexOf("(КАССИР)");
                                if (num_pos > 0)
                                {
                                    cc.recharge_note = cc.recharge_note.Substring(0, num_pos + 8);
                                    //if ((answerTerminal.code_authorization == "sbpnspk")&&(answerTerminal.number_reference==""))//Оплата по сбп и не вернулся номер транзакции
                                    //{
                                    //    int num_pos1 = cc.recharge_note.IndexOf("TRN:");
                                    //    int num_pos2 = cc.recharge_note.IndexOf("Статус:");
                                    //    answerTerminal.number_reference = cc.recharge_note.Substring(num_pos1 + 4, num_pos2 - (num_pos1 + 4)).Replace("\r\n", "").Trim();
                                    //}
                                }
                            }
                        }
                    }
                }
                else
                {
                    status = false;
                }

                req = null;
                sendStream = null;
                myHttpWebResponse.Close();// = null;
            }
            catch (WebException ex)
            {
                status = false;
                MessageBox.Show(" Ошибка при оплате по карте  " + ex.Message, "Оплата по терминалу");//Код ошибки  "+ ((System.Net.Sockets.SocketException)ex.InnerException).ErrorCode
                answerTerminal.error = true;
                if (ex.Message.IndexOf("404") != -1)
                {
                    answerTerminal.error_code = 404;
                }
            }
            catch (Exception ex)
            {
                status = false;
                MessageBox.Show(" Ошибка при оплате по карте  " + ex.Message, "Оплата по терминалу");
                answerTerminal.error = true;
            }
        }


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

                if (checkBox.IsChecked == true)
                {
                    // Логика для включенного СБП
                    if (nonCashSumTextBox != null)
                    {
                        nonCashSumTextBox.Text = "0";
                        nonCashSumTextBox.IsEnabled = false;
                    }

                    if (nonCashSumKopTextBox != null)
                    {
                        nonCashSumKopTextBox.Text = "0";
                    }
                }
                else
                {
                    // Логика для выключенного СБП
                    if (nonCashSumTextBox != null)
                    {
                        nonCashSumTextBox.IsEnabled = true;
                    }
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
            var remainderTextBox = this.FindControl<TextBox>("remainder");

            if (paySumTextBox != null && cashSumTextBox != null && remainderTextBox != null)
            {
                if (decimal.TryParse(paySumTextBox.Text, out decimal paySum) &&
                    decimal.TryParse(cashSumTextBox.Text, out decimal cashSum))
                {
                    decimal nonCashSum = 0;
                    if (nonCashSumTextBox != null && decimal.TryParse(nonCashSumTextBox.Text, out decimal nonCash))
                    {
                        nonCashSum = nonCash;

                        // Добавляем копейки если есть
                        if (nonCashSumKopTextBox != null && int.TryParse(nonCashSumKopTextBox.Text, out int kop))
                        {
                            nonCashSum += kop / 100m;
                        }
                    }

                    decimal totalPaid = cashSum + nonCashSum;

                    if (totalPaid >= paySum)
                    {
                        decimal remainder = totalPaid - paySum;
                        remainderTextBox.Text = remainder.ToString("F2");
                    }
                    else
                    {
                        remainderTextBox.Text = "0,00";
                    }
                }
            }
        }

        // Методы для доступа к элементам извне
        public string PaySum
        {
            get => this.FindControl<TextBox>("pay_sum")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("pay_sum");
                if (textBox != null)
                {
                    textBox.Text = value;
                    CalculateChange();
                }
            }
        }

        public string CashSum
        {
            get => this.FindControl<TextBox>("txtB_cash_sum")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("txtB_cash_sum");
                if (textBox != null)
                {
                    textBox.Text = value;
                    CalculateChange();
                }
            }
        }

        public string NonCashSum
        {
            get => this.FindControl<TextBox>("non_cash_sum")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("non_cash_sum");
                if (textBox != null)
                {
                    textBox.Text = value;
                    CalculateChange();
                }
            }
        }

        public string NonCashSumKop
        {
            get => this.FindControl<TextBox>("non_cash_sum_kop")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("non_cash_sum_kop");
                if (textBox != null)
                {
                    textBox.Text = value;
                    CalculateChange();
                }
            }
        }

        public string CertificatesSum
        {
            get => this.FindControl<TextBox>("sertificates_sum")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("sertificates_sum");
                if (textBox != null) textBox.Text = value;
            }
        }

        public string BonusSum
        {
            get => this.FindControl<TextBox>("pay_bonus")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("pay_bonus");
                if (textBox != null) textBox.Text = value;
            }
        }

        public string BonusMany
        {
            get => this.FindControl<TextBox>("pay_bonus_many")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("pay_bonus_many");
                if (textBox != null) textBox.Text = value;
            }
        }

        public string Change
        {
            get => this.FindControl<TextBox>("remainder")?.Text ?? string.Empty;
            set
            {
                var textBox = this.FindControl<TextBox>("remainder");
                if (textBox != null) textBox.Text = value;
            }
        }

        public bool IsSbpPayment
        {
            get => this.FindControl<CheckBox>("checkBox_payment_by_sbp")?.IsChecked ?? false;
            set
            {
                var checkBox = this.FindControl<CheckBox>("checkBox_payment_by_sbp");
                if (checkBox != null) checkBox.IsChecked = value;
            }
        }

        public void ShowBonusControls(bool show)
        {
            var label4 = this.FindControl<TextBlock>("label4");
            var bonusOnDocument = this.FindControl<TextBox>("bonus_on_document");
            var label5 = this.FindControl<TextBlock>("label5");
            var bonusTotal = this.FindControl<TextBox>("bonus_total_in_centr");
            var label6 = this.FindControl<TextBlock>("label6");
            var payBonus = this.FindControl<TextBox>("pay_bonus");
            var payBonusMany = this.FindControl<TextBox>("pay_bonus_many");

            if (label4 != null) label4.IsVisible = show;
            if (bonusOnDocument != null) bonusOnDocument.IsVisible = show;
            if (label5 != null) label5.IsVisible = show;
            if (bonusTotal != null) bonusTotal.IsVisible = show;
            if (label6 != null) label6.IsVisible = show;
            if (payBonus != null) payBonus.IsVisible = show;
            if (payBonusMany != null) payBonusMany.IsVisible = show;
        }

        public void ShowSbpControls(bool show)
        {
            var checkBoxSbp = this.FindControl<CheckBox>("checkBox_payment_by_sbp");
            var checkBoxTerminal = this.FindControl<CheckBox>("checkBox_do_not_send_payment_to_the_terminal");

            if (checkBoxSbp != null) checkBoxSbp.IsVisible = show;
            if (checkBoxTerminal != null) checkBoxTerminal.IsVisible = show;
        }
    }
}