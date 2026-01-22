using Atol.Drivers10.Fptr;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AtolConstants = Atol.Drivers10.Fptr.Constants;

namespace Cash8Avalon;

public partial class FPTK22 : ControlBase
{
    private bool complete = false;
    private string recharge_note = "";
    private DateTime dateTimeEnd;    

    public FPTK22()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Этот метод будет вызван после полной загрузки формы
        // Удаляем подписку, чтобы метод не вызывался повторно
        Loaded -= OnLoaded;

        // Выполняем необходимые методы
        get_fiscall_info();
        btn_date_mark_Click(null, null);
        load_status_open_shop();
        load_status_close_shop();
    }

    private void get_fiscall_info()
    {       
        PrintingUsingLibraries printing = new PrintingUsingLibraries();
        var fnInfo = GetRequiredControl<TextBox>("txtB_fn_info");
        fnInfo.Text = printing.getFiscallInfo();     
    }

    public event EventHandler? CloseRequested;

    protected virtual void OnCloseRequested()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        OnCloseRequested();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void get_summ_in_cashe_Click(object sender, RoutedEventArgs e)
    {       
        if (MainStaticClass.GetFiscalsForbidden)
        {
            await MessageBox.Show("Вам запрещена печать на фискаольном регистраторое", "Проверки при печати");
            return;
        }
        else
        {
            PrintingUsingLibraries printing = new PrintingUsingLibraries();
            var sumIncassTextBox = this.FindControl<TextBox>("sum_incass");
            if (sumIncassTextBox == null)
            {
                await MessageBox.Show("Произошла ошибка при поиске контрола", "Проверки при печати");
                return;
            }
            sumIncassTextBox.Text = printing.getCasheSumm().ToString();            
        }       
    }

    private async void incass_Click(object sender, RoutedEventArgs e)
    {
        var incassButton = this.FindControl<Button>("incass");
        var sumIncassTextBox = this.FindControl<TextBox>("sum_incass");

        if (incassButton == null || sumIncassTextBox == null)
        {
            await MessageBox.Show("Не найден необходимый контрол", "Ошибка");
            return;
        }

        incassButton.IsEnabled = false;

        PrintingUsingLibraries printing = new PrintingUsingLibraries();
        printing.cashOutcome(Convert.ToDouble(sumIncassTextBox.Text.Replace(".", ",")));
        get_summ_in_cashe_Click(null, null);
        incassButton.IsEnabled = true;
    }

    private void avans_Click(object sender, RoutedEventArgs e)
    {     

        var avansButton = this.FindControl<Button>("avans");
        var sumAvansTextBox = this.FindControl<TextBox>("sum_avans");
        var getSummButton = this.FindControl<Button>("get_summ_in_cashe");

        if (avansButton == null || sumAvansTextBox == null)
            return;

        // Проверка continue_process
        if (!MainStaticClass.continue_process(dateTimeEnd, 1))
        {
            return;
        }

        // Валидация даты/времени с ФН
        MainStaticClass.validate_date_time_with_fn(13);

        // Отключаем кнопку
        avansButton.IsEnabled = false;

        if (MainStaticClass.GetFiscalsForbidden)
        {
            MessageBox.Show("Вам запрещена печать на фискаольном регистраторое", "Проверки при печати");
            return;
        }
        else
        {
            PrintingUsingLibraries printing = new PrintingUsingLibraries();
            printing.cashIncome(Convert.ToDouble(sumAvansTextBox.Text.Replace(".", ",")));
            get_summ_in_cashe_Click(null, null);
        }
        //}
        //sum_avans.Text = "0";
        avansButton.IsEnabled = true;
        dateTimeEnd = DateTime.Now;
    }

    private void x_Report_Click(object sender, RoutedEventArgs e)
    {       
        if (MainStaticClass.GetFiscalsForbidden)
        {
             MessageBox.Show("Вам запрещена печать на фискаольном регистраторое", "Проверки при печати");
            return;
        }
        else
        {
            PrintingUsingLibraries printing = new PrintingUsingLibraries();
            printing.reportX();
        }        
    }

    public void z_Report_Click(object sender, RoutedEventArgs e)
    {       
        if (MainStaticClass.GetFiscalsForbidden)
        {
             MessageBox.Show("Вам запрещена печать на фискаольном регистраторое", "Проверки при печати");
            return;
        }
        else
        {
            PrintingUsingLibraries printing = new PrintingUsingLibraries();
            printing.reportZ();
        }        
    }

    private void print_last_check_Click(object sender, RoutedEventArgs e)
    {
       
        if (MainStaticClass.GetFiscalsForbidden)
        {
            MessageBox.Show("Вам запрещена печать на фискаольном регистраторое", "Проверки при печати");
            return;
        }
        else
        {
            PrintingUsingLibraries printing = new PrintingUsingLibraries();
            printing.print_last_document();
        }
        //}
    }

    public class AnswerTerminal
    {
        public string number_reference { get; set; }
        public string code_authorization { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

        public AnswerTerminal()
        {
            number_reference = "";
            code_authorization = "";
            IsSuccess = true;
            ErrorCode = "";
            ErrorMessage = "";
        }

        // Добавляем статические методы для создания объектов

        public static AnswerTerminal CreateSuccess(string numberRef = "", string codeAuth = "")
        {
            return new AnswerTerminal
            {
                IsSuccess = true,
                number_reference = numberRef,
                code_authorization = codeAuth
            };
        }

        public static AnswerTerminal CreateError(string errorCode, string errorMessage, Exception ex = null)
        {
            return new AnswerTerminal
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                Exception = ex
            };
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
    public AnswerTerminal send_command_acquiring_terminal(string Url, string Data)
    {
        try
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create(Url);
            req.Method = "POST";
            req.Timeout = 120000;
            req.ContentType = "text/xml;charset = windows-1251";

            byte[] sentData = Encoding.GetEncoding("Windows-1251").GetBytes(Data);
            req.ContentLength = sentData.Length;

            System.IO.Stream sendStream = req.GetRequestStream();
            sendStream.Write(sentData, 0, sentData.Length);
            sendStream.Close();

            using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)req.GetResponse())
            {
                if (myHttpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var streamReader = new StreamReader(myHttpWebResponse.GetResponseStream(),
                           Encoding.GetEncoding("Windows-1251")))
                    {
                        var responseContent = streamReader.ReadToEnd();

                        XmlSerializer serializer = new XmlSerializer(typeof(Response));
                        using (StringReader reader = new StringReader(responseContent))
                        {
                            var test = (Response)serializer.Deserialize(reader);
                            var answer = new AnswerTerminal();

                            foreach (Field field in test.Field)
                            {
                                if (field.Id == "39")
                                {
                                    answer.IsSuccess = field.Text.Trim() == "1";
                                    if (!answer.IsSuccess)
                                    {
                                        answer.ErrorCode = "TERMINAL_REJECTED";
                                        answer.ErrorMessage = "Терминал отклонил операцию";
                                    }
                                }
                                else if (field.Id == "90")
                                {
                                    recharge_note = field.Text.Trim();
                                }
                            }

                            return answer;
                        }
                    }
                }
                else
                {
                    // Используем новый статический метод
                    return AnswerTerminal.CreateError(
                        $"HTTP_{(int)myHttpWebResponse.StatusCode}",
                        $"Ответ от терминала: {myHttpWebResponse.StatusCode}"
                    );
                }
            }
        }
        catch (WebException ex)
        {
            if (ex.Response is HttpWebResponse httpResponse)
            {
                return AnswerTerminal.CreateError(
                    $"HTTP_{(int)httpResponse.StatusCode}",
                    $"Ошибка соединения: {httpResponse.StatusDescription}",
                    ex
                );
            }

            return AnswerTerminal.CreateError(
                "NETWORK_ERROR",
                $"Ошибка сети: {ex.Message}",
                ex
            );
        }
        catch (XmlException ex)
        {
            return AnswerTerminal.CreateError(
                "XML_PARSE_ERROR",
                $"Ошибка разбора XML ответа: {ex.Message}",
                ex
            );
        }
        catch (Exception ex)
        {
            return AnswerTerminal.CreateError(
                "UNEXPECTED_ERROR",
                $"Непредвиденная ошибка: {ex.Message}",
                ex
            );
        }
    }

    private void btn_query_summary_report_Click(object sender, RoutedEventArgs e)
    {
        string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;
        string _str_command_ = @"<?xml version=""1.0"" encoding=""utf-8""?><request><field id=""25"">63</field><field id=""27"">id_terminal</field><field id=""65"">20</field></request>";
        _str_command_ = _str_command_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
        recharge_note = "";
        AnswerTerminal answerTerminal = new AnswerTerminal();
        answerTerminal = send_command_acquiring_terminal(url, _str_command_);
        if (answerTerminal.IsSuccess)//ответ от терминала не удовлетворительный
        {
            if (recharge_note != "")
            {
                IFptr fptr = MainStaticClass.FPTR;
                if (!fptr.isOpened())
                {
                    fptr.open();
                }
                fptr.beginNonfiscalDocument();
                string s = recharge_note.Replace("0xDF^^", "");
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
                fptr.printText();
                fptr.endNonfiscalDocument();
                recharge_note = "";
            }
        }
        else
        {
            // Проверяем, есть ли ErrorMessage
            string errorMsg = !string.IsNullOrEmpty(answerTerminal.ErrorMessage)
                ? answerTerminal.ErrorMessage
                : "Произошла неизвестная ошибка";

            MessageBox.Show($"Ошибка: {errorMsg}", "Ошибка операции");
        }
    }


    private void btn_query_full_report_Click(object sender, RoutedEventArgs e)
    {
        if (MainStaticClass.GetAcquiringBank == 1) //РНКБ
        {
            string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;
            string _str_command_ = @"<?xml version=""1.0"" encoding=""utf-8""?><request><field id=""25"">63</field><field id=""27"">id_terminal</field><field id=""65"">21</field></request>";
            _str_command_ = _str_command_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
            recharge_note = "";
            AnswerTerminal answerTerminal = new AnswerTerminal();
            answerTerminal = send_command_acquiring_terminal(url, _str_command_);
            if (answerTerminal.IsSuccess)//ответ от терминала удовлетворительный
            {
                if (recharge_note != "")
                {
                    IFptr fptr = MainStaticClass.FPTR;
                    if (!fptr.isOpened())
                    {
                        fptr.open();
                    }
                    fptr.beginNonfiscalDocument();
                    string s = recharge_note.Replace("0xDF^^", "");
                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
                    fptr.printText();
                    fptr.endNonfiscalDocument();
                    recharge_note = "";
                    //}
                }
            }
            else
            {
                // Проверяем, есть ли ErrorMessage
                string errorMsg = !string.IsNullOrEmpty(answerTerminal.ErrorMessage)
                    ? answerTerminal.ErrorMessage
                    : "Произошла неизвестная ошибка";

                MessageBox.Show($"Ошибка: {errorMsg}", "Ошибка операции");
            }
        }
        else if (MainStaticClass.GetAcquiringBank == 2) //СБЕР
        {
            MessageBox.Show("Сбербанк пока что не работает ");
            //try
            //{
            //    string s = Regex.Replace(CommandWrapper.GetFullReport(), @"~S\u0001", "").Trim();
            //    IFptr fptr = MainStaticClass.FPTR;
            //    if (!fptr.isOpened())
            //    {
            //        fptr.open();
            //    }
            //    fptr.beginNonfiscalDocument();
            //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
            //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
            //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
            //    fptr.printText();
            //    fptr.endNonfiscalDocument();
            //    //if (MainStaticClass.GetVariantConnectFN == 1)
            //    //{
            //    //    fptr.close();
            //    //}
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Ошибки при полном отчете  " + ex.Message);
            //}
        }
        else
        {
            MessageBox.Show(" У вас в константах не выбран банк эквайринга");
        }
    }

    private void btn_reconciliation_of_totals_Click(object sender, RoutedEventArgs e)
    {
        if (MainStaticClass.GetAcquiringBank == 1) //РНКБ
        {
            string url = "http://" + MainStaticClass.IpAddressAcquiringTerminal;
            string _str_command_ = @"<?xml version=""1.0"" encoding=""utf-8""?><request><field id=""25"">59</field><field id=""27"">id_terminal</field></request>";
            _str_command_ = _str_command_.Replace("id_terminal", MainStaticClass.IdAcquirerTerminal);
            recharge_note = "";
            AnswerTerminal answerTerminal = new AnswerTerminal();
            answerTerminal = send_command_acquiring_terminal(url, _str_command_);
            if (answerTerminal.IsSuccess)//ответ от терминала не удовлетворительный
            {
                if (recharge_note != "")
                {                   
                    IFptr fptr = MainStaticClass.FPTR;
                    if (!fptr.isOpened())
                    {
                        fptr.open();
                    }
                    fptr.beginNonfiscalDocument();
                    string s = recharge_note.Replace("0xDF^^", "");
                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
                    fptr.printText();
                    fptr.endNonfiscalDocument();
                    recharge_note = "";

                    //}
                }
            }
            else
            {
                // Проверяем, есть ли ErrorMessage
                string errorMsg = !string.IsNullOrEmpty(answerTerminal.ErrorMessage)
                    ? answerTerminal.ErrorMessage
                    : "Произошла неизвестная ошибка";

                MessageBox.Show($"Ошибка: {errorMsg}", "Ошибка операции");
            }
        }
        else if (MainStaticClass.GetAcquiringBank == 2) //СБЕР
        {
            MessageBox.Show("Сбербанк пока что не работает ");
            //try
            //{
            //    string s = Regex.Replace(CommandWrapper.CloseDay(), @"~S\u0001", "").Trim();
            //    IFptr fptr = MainStaticClass.FPTR;
            //    if (!fptr.isOpened())
            //    {
            //        fptr.open();
            //    }

            //    fptr.beginNonfiscalDocument();

            //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
            //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
            //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
            //    fptr.printText();
            //    fptr.endNonfiscalDocument();
            //    //if (MainStaticClass.GetVariantConnectFN == 1)
            //    //{
            //    //    fptr.close();
            //    //}
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Ошибки при сверке итогов " + ex.Message);
            //}
        }
        else
        {
            MessageBox.Show(" У вас в константах не выбран банк эквайринга");
        }
    }


    private void btn_open_shop_Click(object sender, RoutedEventArgs e)
    {
        using (var conn = MainStaticClass.NpgsqlConn())
        {
            try
            {
                conn.Open();

                // Проверяем, открыт ли магазин сегодня
                string checkQuery = "SELECT COUNT(1) FROM open_close_shop WHERE date = @date";
                using (var command = new NpgsqlCommand(checkQuery, conn))
                {
                    command.Parameters.AddWithValue("@date", DateTime.Now.Date);

                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                    {
                        // Открываем магазин
                        string insertQuery = "INSERT INTO open_close_shop(open, date,its_sent) VALUES(@open, @date,@its_sent)";
                        using (var insertCommand = new NpgsqlCommand(insertQuery, conn))
                        {
                            insertCommand.Parameters.AddWithValue("@open", DateTime.Now);
                            insertCommand.Parameters.AddWithValue("@date", DateTime.Now.Date);
                            insertCommand.Parameters.AddWithValue("@its_sent", false);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Сегодня магазин уже был открыт ранее", "Открытие магазина");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при открытии магазина");
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Открытие магазина");
            }
        }
        load_status_open_shop();
    }

    private void load_status_open_shop()
    {
        using (var conn = MainStaticClass.NpgsqlConn())
        {
            try
            {
                conn.Open();

                // Проверяем, открыт ли магазин сегодня
                string checkQuery = "SELECT open FROM open_close_shop WHERE date = @date";
                using (var command = new NpgsqlCommand(checkQuery, conn))
                {
                    command.Parameters.AddWithValue("@date", DateTime.Now.Date);
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        string status_open = "Не открыт";
                        while (reader.Read())
                        {
                            var openValue = reader["open"];
                            if (openValue != DBNull.Value)
                            {
                                status_open = Convert.ToDateTime(openValue).ToString("dd:MM:yyyy HH:mm:ss");
                            }
                        }
                        var labelOpenShopText = GetRequiredControl<TextBlock>("label_open_shop_text");
                        if (labelOpenShopText == null)
                        {
                            MessageBox.Show("не найден котрол label_open_shop", "Ошибка при чтении статуса открытия магазина");
                        }
                        else
                        {
                            labelOpenShopText.Text = status_open;                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при чтении статуса открытия магазина");
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Чтение статуса открытия магазина");
            }
        }
    }

    private void btn_close_shop_Click(object sender, RoutedEventArgs e)
    {
        using (var conn = MainStaticClass.NpgsqlConn())
        {
            try
            {
                conn.Open();

                // Проверяем, открыт ли магазин сегодня
                string checkQuery = "SELECT COUNT(1) FROM open_close_shop WHERE date = @date";
                using (var command = new NpgsqlCommand(checkQuery, conn))
                {
                    command.Parameters.AddWithValue("@date", DateTime.Now.Date);

                    if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                    {
                        // Закрываем магазин
                        string insertQuery = "UPDATE open_close_shop  	SET close=@close,its_sent = @its_sent  WHERE date = @date";
                        using (var insertCommand = new NpgsqlCommand(insertQuery, conn))
                        {
                            insertCommand.Parameters.AddWithValue("@close", DateTime.Now);
                            insertCommand.Parameters.AddWithValue("@date", DateTime.Now.Date);
                            insertCommand.Parameters.AddWithValue("@its_sent", false);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Сегодня магазин еще не был открыт");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при закрытии магазина");
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Закрытие магазина");
            }
        }
        load_status_close_shop();
    }

    private void load_status_close_shop()
    {
        using (var conn = MainStaticClass.NpgsqlConn())
        {
            try
            {
                conn.Open();

                // Проверяем, открыт ли магазин сегодня
                string checkQuery = "SELECT close FROM open_close_shop WHERE date = @date";
                using (var command = new NpgsqlCommand(checkQuery, conn))
                {
                    command.Parameters.AddWithValue("@date", DateTime.Now.Date);
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        string status_close = "Не закрыт";
                        while (reader.Read())
                        {
                            var closeValue = reader["close"];
                            if (closeValue != DBNull.Value)
                            {
                                status_close = Convert.ToDateTime(closeValue).ToString("dd:MM:yyyy HH:mm:ss");
                            }
                        }
                        //label_close_shop.Text = status_close;

                        var labelOpenShopText = this.FindControl<TextBlock>("label_open_shop_text");
                        if (labelOpenShopText == null)
                        {
                            MessageBox.Show("не найден котрол label_open_shop", "Ошибка при чтении статуса открытия магазина");
                        }
                        else
                        {
                            labelOpenShopText.Text = status_close;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при чтении статуса закрытия магазина");
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Чтение статуса закрытия магазина");
            }
        }
    }

    private void btn_have_internet_Click(object sender, RoutedEventArgs e)
    {
        var txtBHaveInternet = this.FindControl<TextBox>("txtB_have_internet");
        if (txtBHaveInternet == null) return;

        // Меняем цвет фона и текст
        txtBHaveInternet.Background = Avalonia.Media.Brushes.White;
        txtBHaveInternet.Text = "Проверка ...";

        // Ждем асинхронно
        Task.Delay(1000);

        // Проверяем интернет
        bool hasInternet = MainStaticClass.get_exists_internet();

        // Устанавливаем результат
        if (hasInternet)
        {
            txtBHaveInternet.Text = "Работает";
            txtBHaveInternet.Background = Avalonia.Media.Brushes.Green;
        }
        else
        {
            txtBHaveInternet.Text = "Не работает";
            txtBHaveInternet.Background = Avalonia.Media.Brushes.Gold;
        }
    }

    private void btn_ofd_exchange_status_Click(object sender, RoutedEventArgs e)
    {
        var txtBOfdExchangeStatus = this.FindControl<TextBox>("txtB_ofd_exchange_status");
        if (txtBOfdExchangeStatus == null)
        {
            MessageBox.Show("не найден контрол txtB_ofd_exchange_status", "Проверка наличия контрола");
            return;
        }

        // Показываем процесс проверки
        txtBOfdExchangeStatus.Background = Avalonia.Media.Brushes.White;
        txtBOfdExchangeStatus.Text = "Проверка ...";

        // Ждем асинхронно
        Task.Delay(1000);

        // Получаем статус
        PrintingUsingLibraries printing = new PrintingUsingLibraries();
        string status = printing.ofdExchangeStatus();

        // Обновляем UI
        txtBOfdExchangeStatus.Text = status;
    }

    private void btn_send_fiscal_Click(object sender, RoutedEventArgs e)
    {
        var txtBOfdUtilityStatus = this.FindControl<TextBox>("txtB_ofd_utility_status");
        if (txtBOfdUtilityStatus == null) return;

        Process[] ethOverUsbProcesses = Process.GetProcessesByName("EthOverUsb");

        if (ethOverUsbProcesses.Length > 0)
        {
            txtBOfdUtilityStatus.Text = "Запущена";
            txtBOfdUtilityStatus.Background = Avalonia.Media.Brushes.Green;
        }
        else
        {
            txtBOfdUtilityStatus.Text = "Не запущена";
            txtBOfdUtilityStatus.Background = Avalonia.Media.Brushes.Gold;
        }
    }

    private void btn_date_mark_Click(object sender, RoutedEventArgs e)
    {
        var txtBLastSendMark = this.FindControl<TextBox>("txtB_last_send_mark");
        var btnDateMark = this.FindControl<Button>("btn_date_mark");

        if (txtBLastSendMark == null || btnDateMark == null) return;

        // Показываем проверку
        txtBLastSendMark.Background = Avalonia.Media.Brushes.White;
        txtBLastSendMark.Text = "Проверка ...";

        // Ждем асинхронно
        Task.Delay(1000);

        if (MainStaticClass.PrintingUsingLibraries == 1)
        {
            try
            {
                IFptr fptr = MainStaticClass.FPTR;
                if (!fptr.isOpened())
                {
                    fptr.open();
                }

                fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_LAST_SENT_ISM_NOTICE_DATE_TIME);
                fptr.queryData();

                DateTime dateTime = fptr.getParamDateTime(AtolConstants.LIBFPTR_PARAM_DATE_TIME);
                txtBLastSendMark.Text = dateTime.ToString("dd-MM-yyyy HH:mm:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибки при получении даты о последней успешной отправке маркировки в ИСМ: " + ex.Message, "Ошибка");
            }
        }
        else
        {
            btnDateMark.IsVisible = false;
            txtBLastSendMark.IsVisible = false;
        }
    }

    private void btn_openDrawer_Click(object sender, RoutedEventArgs e)
    {
        if (MainStaticClass.PrintingUsingLibraries == 1)
        {
            try
            {
                IFptr fptr = MainStaticClass.FPTR;
                if (!fptr.isOpened())
                {
                    fptr.open();
                }
                fptr.openDrawer();              
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибки при попытке открыть денежный ящик  " + ex.Message);
            }
        }
    }

}
