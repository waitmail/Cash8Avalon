using Atol.Drivers10.Fptr;
using Cash8Avalon;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//using System.Windows.Forms;
using AtolConstants = Atol.Drivers10.Fptr.Constants;


namespace Cash8Avalon
{
    class PrintingUsingLibraries
    {

        //private  Cash_check check;

        //public bool print_sale(ListView listView)
        //{
        //    bool result = true;


        //    return result;
        //}


        //private void setConnectSetting(IFptr fptr)
        //{
        //    fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_MODEL, AtolConstants.LIBFPTR_MODEL_ATOL_AUTO.ToString());
        //    fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_PORT, AtolConstants.LIBFPTR_PORT_COM.ToString());
        //    fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_COM_FILE, MainStaticClass.FnSreialPort);
        //    fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_BAUDRATE, AtolConstants.LIBFPTR_PORT_BR_115200.ToString());
        //    fptr.applySingleSettings();
        //}

        public void print_last_document()
        {
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(1021, MainStaticClass.Cash_Operator);
            fptr.setParam(1203, MainStaticClass.CashOperatorInn);
            fptr.operatorLogin();
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_REPORT_TYPE, AtolConstants.LIBFPTR_RT_LAST_DOCUMENT);
            fptr.report();
            //if (MainStaticClass.GetVariantConnectFN == 1)
            //{
            //    fptr.close();
            //}
        }

        //public void get_register_data()
        //{
        //    IFptr fptr = MainStaticClass.FPTR;
        //    if (!fptr.isOpened())
        //    {
        //        fptr.open();
        //    }

        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_SERIAL_NUMBER);
        //    fptr.queryData();

        //    string serialNumber = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_SERIAL_NUMBER);



        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_REG_INFO);
        //    fptr.fnQueryData();

        //    string organizationName = fptr.getParamString(1048);//Наименование организации
        //    string organizationAddress = fptr.getParamString(1009);//адрес организации
        //    string paymentsAddress = fptr.getParamString(1187);//место расчета магазин 
        //    string registrationNumber = fptr.getParamString(1037);//Регистрационный номер ККТ: каждый раз разный при перерегистрации  
        //    string machineNumber = fptr.getParamString(1036);
        //    string organizationVATIN = fptr.getParamString(1018);

        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_FN_INFO);
        //    fptr.fnQueryData();

        //    String fnSerial = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_SERIAL_NUMBER);
        //    String fnVersion = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_FN_VERSION);
        //    String fnExecution = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_FN_EXECUTION);


        //}

        public string get_version()
        {
            string result = "";
            IFptr fptr = MainStaticClass.FPTR;
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            result = fptr.version();
            return result;
        }

        public bool validate_date_time_with_fn(int minutes)
        {
            bool result = true;

            IFptr fptr = MainStaticClass.FPTR;
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_DATE_TIME);
            fptr.queryData();
            if (fptr.errorCode() != 0)
            {
                MessageBox.Show(" При проверке даты и времени в ФН произошла ошибка \r\n" + fptr.errorDescription(), " Проверка даты и времени в фн ");
                result = false;
            }
            else
            {
                DateTime dateTime = fptr.getParamDateTime(AtolConstants.LIBFPTR_PARAM_DATE_TIME);
                if (Math.Abs((dateTime - DateTime.Now).Minutes) > minutes)//Поскольку может быть как больше так и меньше 
                {
                    MessageBox.Show(" У ВАС ОТЛИЧАЕТСЯ ВРЕМЯ МЕЖДУ КОМПЬЮТЕРОМ И ФИСКАЛЬНЫМ РЕГИСТРАТОРОМ БОЛЬШЕ ЧЕМ НА" + minutes.ToString() + " МИНУТ ОТПРАВЬТЕ ЗАЯВКУ В ИТ ОТДЕЛ " +
                        "\r\nПосле отправки заявки вы можете продолжить работу в обычном режиме.    ", " Проверка даты и времени в фн ");
                    MainStaticClass.write_event_in_log(" Не схождение даты и времени между ФР и компьютером больше чем на" + minutes.ToString() + " минут ", "Документ", "0");

                    result = false;
                }
            }
            //if (MainStaticClass.GetVariantConnectFN == 1)
            //{
            //    fptr.close();
            //}

            return result;
        }


        public void getShiftStatus()
        {
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_SHIFT_STATE);
            fptr.queryData();
            uint state = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_SHIFT_STATE);
            //uint number = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_SHIFT_NUMBER);
            //DateTime dateTime = fptr.getParamDateTime(AtolConstants.LIBFPTR_PARAM_DATE_TIME);

            if (state == AtolConstants.LIBFPTR_SS_EXPIRED)
            {
                //if ((DateTime.Now - dateTime).TotalHours > 0)
                //{
                //MessageBox.Show(" Период открытой смены превысил 24 часа !!!\r\n СНИМИТЕ Z-ОТЧЁТ. ЕСЛИ СОМНЕВАЕТЕСЬ В ЧЁМ-ТО, ТО ВСЁ РАВНО СНИМИТЕ Z-ОТЧЁТ");
                MessageBox.Show(" Период открытой смены превысил 24 часа!\r\nСмена будет закрыта автоматически!\r\n" +
                    "В ИТ отдел звонить не надо, если хотите кому нибудь позвонить, звоните в бухгалтерию");
                reportZ();
                //}
            }
            //else
            //{
            //    //if (MainStaticClass.GetVariantConnectFN == 1)
            //    //{
            //    //    fptr.close();
            //    //}
            //}
            //fptr.close();
        }

        public string getFiscallInfo()
        {
            string fn_info = "";

            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }



            //var parts = "1 2 3 a b c".Split();
            //int inx = 0;

            //var dict = parts.GroupBy(x => inx++ % (parts.Length / 2))
            //                .ToDictionary(x => x.First(), x => x.Last());

            //string settings = fptr.getSettings();
            //bool isOpened = fptr.isOpened();
            //if (!isOpened)
            //{
            //    MessageBox.Show(fptr.errorCode() + " " + fptr.errorDescription());
            //}

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_VALIDITY_DAYS);
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATE_TIME, DateTime.Now);
            fptr.fnQueryData();
            uint daysRemain = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_FN_DAYS_REMAIN);
            fn_info = "Ресурс ФН " + daysRemain.ToString() + " дней ";

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_FN_INFO);
            fptr.fnQueryData();

            if (fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_FN_NEED_REPLACEMENT))
            {
                fn_info += "\r\n" + "Требуется срочная замена ФН !!!";
            }
            if (fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_FN_RESOURCE_EXHAUSTED))
            {
                fn_info += "\r\n" + "Исчерпан ресурс ФН !!!";
            }
            if (fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_FN_MEMORY_OVERFLOW))
            {
                fn_info += "\r\n" + "Память ФН переполнена !!!";
            }
            if (fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_FN_OFD_TIMEOUT))
            {
                fn_info += "\r\n" + "Превышено время ожидания ответа от ОФД !!!";
            }
            if (fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_FN_CRITICAL_ERROR))
            {
                fn_info += "\r\n" + "Критическая ошибка ФН !!!";
            }

            //fptr.close();
            //if (MainStaticClass.GetVariantConnectFN == 1)
            //{
            //    fptr.close();
            //}

            return fn_info;
        }

        public string ofdExchangeStatus()
        {
            string result = "0";

            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            //fptr.isOpened

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_OFD_EXCHANGE_STATUS);
            fptr.fnQueryData();

            uint exchangeStatus = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_OFD_EXCHANGE_STATUS);
            uint unsentCount = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_DOCUMENTS_COUNT);
            if (unsentCount > 0)
            {
                DateTime dateTime = fptr.getParamDateTime(AtolConstants.LIBFPTR_PARAM_DATE_TIME);
                result = "В ОФД Не отправлено документов " + unsentCount.ToString() + "\r\n" +
                               " начиная с даты " + dateTime.ToString("yyyy-MM-dd HH:mm:ss");

            }           
            return result;
        }

        public string getCasheSumm()
        {
            string result = "";
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(1021, MainStaticClass.Cash_Operator);
            fptr.setParam(1203, MainStaticClass.CashOperatorInn);
            fptr.operatorLogin();

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_CASH_SUM);
            fptr.queryData();
            double cashSum = fptr.getParamDouble(AtolConstants.LIBFPTR_PARAM_SUM);
            if (fptr.errorCode() != 0)
            {
                MessageBox.Show("Ошибка при получении суммы наличных в кассе  " + fptr.errorDescription());
            }           

            result = cashSum.ToString().Replace(",", ".");
            return result;
        }

        public void cashOutcome(double sumCashOut)
        {
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(1021, MainStaticClass.Cash_Operator);
            fptr.setParam(1203, MainStaticClass.CashOperatorInn);
            fptr.operatorLogin();
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_SUM, sumCashOut);

            if (fptr.cashOutcome() != 0)
            {
                MessageBox.Show("Ошибка при инкасации  " + fptr.errorDescription());//.GetAwaiter().GetResult();
            }
            //fptr.close();
            //if (MainStaticClass.GetVariantConnectFN == 1)
            //{
            //    fptr.close();
            //}
        }

        public void cashIncome(double sumCashIn)
        {
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(1021, MainStaticClass.Cash_Operator);
            fptr.setParam(1203, MainStaticClass.CashOperatorInn);
            fptr.operatorLogin();
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_SUM, sumCashIn);
            if (fptr.cashIncome() != 0)
            {
                MessageBox.Show("Ошибка при внесении  " + fptr.errorDescription());
            }           
        }

        #pragma warning disable CA1822
        public async void reportX()
        {
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(1021, MainStaticClass.Cash_Operator);
            fptr.setParam(1203, MainStaticClass.CashOperatorInn);
            fptr.operatorLogin();
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_REPORT_TYPE, AtolConstants.LIBFPTR_RT_X);
            if (fptr.report() != 0)
            {
                await MessageBox.Show(string.Format("Ошибка при X отчете.\nОшибка {0}: {1}", fptr.errorCode(), fptr.errorDescription()),
                    "Ошибка при X отчете", MessageBoxButton.OK, MessageBoxType.Error);
            }
            //fptr.close();
            //if (MainStaticClass.GetVariantConnectFN == 1)
            //{
            //    fptr.close();
            //}
        }

        public async void reportZ()
        {
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(1021, MainStaticClass.Cash_Operator);
            fptr.setParam(1203, MainStaticClass.CashOperatorInn);
            fptr.operatorLogin();
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_REPORT_TYPE, AtolConstants.LIBFPTR_RT_CLOSE_SHIFT);
            if (fptr.report() != 0)
            {
                await MessageBox.Show(string.Format("Ошибка при закрытии смены.\nОшибка {0}: {1}", fptr.errorCode(), fptr.errorDescription()),
                    "Ошибка закрытия смены", MessageBoxButton.OK, MessageBoxType.Error);
            }
            //if (MainStaticClass.GetVariantConnectFN == 1)
            //{
            //    fptr.close();
            //}
            //fptr.close();
        }

        private void print_terminal_check(IFptr fptr, Cash_check check)
        {
            if (check.recharge_note != "")
            {
                fptr.beginNonfiscalDocument();
                string s = check.recharge_note.Replace("0xDF^^", "");
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
                fptr.printText();
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_PRINT_FOOTER, false);
                fptr.endNonfiscalDocument();
                check.recharge_note = "";
            }
            //if (recharge_note != "")
            //{
            //    FiscallPrintJason2.NonFiscallDocument nonFiscallDocument = new FiscallPrintJason2.NonFiscallDocument();
            //    nonFiscallDocument.type = "nonFiscal";
            //    nonFiscallDocument.printFooter = false;

            //    FiscallPrintJason2.ItemNonFiscal itemNonFiscal = new FiscallPrintJason2.ItemNonFiscal();
            //    nonFiscallDocument.items = new List<FiscallPrintJason2.ItemNonFiscal>();

            //    itemNonFiscal.type = "text";
            //    itemNonFiscal.text = recharge_note.Replace("0xDF^^", "");
            //    itemNonFiscal.alignment = "center";
            //    nonFiscallDocument.items.Add(itemNonFiscal);
            //    FiscallPrintJason2.print_not_fiscal_document(nonFiscallDocument);
            //    recharge_note = "";
            //}
        }



        //private void check_validation_error_422(uint validationError)
        //{
        //    if (validationError == 422)
        //    {
        //        IFptr fptr = MainStaticClass.FPTR;
        //        //setConnectSetting(fptr);
        //        if (!fptr.isOpened())
        //        {
        //            fptr.open();
        //        }

        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_SHIFT_STATE);
        //        fptr.queryData();
        //        if (AtolConstants.LIBFPTR_SS_CLOSED == fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_SHIFT_STATE))
        //        {
        //            MessageBox.Show("У вас закрыта смена вы не сможете продавать маркированный товар, будете получать ошибку 422.Необходимо сделать внесение наличных в кассу. ", "Проверка состояния смены");
        //        }
        //    }
        //}

        private void print_fiscal_advertisement(IFptr fptr)
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            string s = ""; //int length = 0;
            string hex_string = "";
            bool first_string = true;
            bool text_available = false;
            try
            {
                conn.Open();
                string query = "SELECT advertisement_text, picture,num_str  FROM advertisement order by num_str";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    s = reader["advertisement_text"].ToString();
                    hex_string = reader["picture"].ToString();
                    if (s != "")
                    {
                        text_available = true;
                        if (first_string)
                        {
                            first_string = false;
                            s = " * -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*";
                            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
                            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);//печатать в конце чека
                            fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
                            fptr.printText();
                        }

                        fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
                        fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
                        fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
                        fptr.printText();
                    }
                    else if (hex_string != "")
                    {
                        byte[] byteArray = Convert.FromBase64String(hex_string);

                        // Запись массива байтов в новый файл            
                        //string outputFilePath = Application.StartupPath + "\\Pictures2\\B" + reader["num_str"].ToString() + "_picture.png";//Префикс A  у файла значит что это акционный документ
                        string outputFilePath = Path.Combine(Environment.CurrentDirectory, "Pictures2", "B" + reader["num_str"].ToString() + "_picture.png");

                        if (!System.IO.File.Exists(outputFilePath))
                        {
                            System.IO.File.WriteAllBytes(outputFilePath, byteArray);
                        }

                        fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
                        fptr.setParam(AtolConstants.LIBFPTR_PARAM_FILENAME, outputFilePath);
                        fptr.printPicture();
                    }
                }
                if ((!first_string) && (text_available))
                {
                    s = " * -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*";
                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
                    fptr.printText();
                }
                reader.Close();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибки при выводе рекламного текста " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибки при выводе рекламного текста " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

        }

        //public void print_sell_2_or_return_sell(Cash_check check)
        //{
        //    bool error = false;

        //    IFptr fptr = MainStaticClass.FPTR;
        //    if (!fptr.isOpened())
        //    {
        //        fptr.open();
        //    }

        //    //if (!check.print_promo_picture)
        //    //{
        //    //    get_actions_num_doc(check);//Печать акционных картинок
        //    //    check.print_promo_picture = true;
        //    //}


        //    if (check.check_type.SelectedIndex == 1 || check.check_type.SelectedIndex == 2 || check.reopened)//для возвратов и красных чеков старая схема
        //    {
        //        //здесь необходимо выполнить запрос на cdn
        //        ProductData productData = new ProductData(0, "", 0, ProductFlags.None);

        //        fptr.clearMarkingCodeValidationResult();
        //        check.cdn_markers_result_check.Clear();//если мы здесь предыдущие проверки очищаем
        //        foreach (ListViewItem lvi in check.listView1.Items)
        //        {
        //            if (lvi.SubItems[14].Text.Trim().Length > 13)
        //            {

        //                string mark = lvi.SubItems[14].Text.Trim().Replace("vasya2021", "'");

        //                if (InventoryManager.completeDictionaryProductData)
        //                {
        //                    productData = InventoryManager.GetItem(Convert.ToInt64(lvi.SubItems[0].Text.Trim()));
        //                }
        //                else
        //                {
        //                    productData = check.GetProductDataInDB(lvi.SubItems[0].Text.Trim());
        //                }

        //                if (productData.IsCDNCheck())//&& check.check_type.SelectedIndex == 0 
        //                {
        //                    if (!MainStaticClass.cdn_check(productData, mark, lvi, check))
        //                    {
        //                        error = true;
        //                    }
        //                }

        //                bool result_check_mark = check_marking_code(mark, check.numdoc.ToString(), ref check.cdn_markers_result_check, check.check_type.SelectedIndex);
        //                if (!result_check_mark)
        //                {
        //                    error = true;
        //                }
        //            }
        //        }
        //    }

        //    if (error)
        //    {
        //        return;
        //    }


        //    fptr.setParam(1021, MainStaticClass.Cash_Operator);
        //    fptr.setParam(1203, MainStaticClass.CashOperatorInn);
        //    fptr.operatorLogin();

        //    print_terminal_check(fptr, check);

        //    // Открытие чека (с передачей телефона получателя)
        //    if (check.check_type.SelectedIndex == 0)
        //    {
        //        get_actions_num_doc(check);//Печать акционных картинок
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL);
        //    }
        //    else if (check.check_type.SelectedIndex == 1)
        //    {
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL_RETURN);
        //    }
        //    else if (check.check_type.SelectedIndex == 2)
        //    {

        //        //fptr.setParam(1177, "Служебная записка");
        //        fptr.setParam(1178, check.sale_date);//Дата продажи
        //        //if (check.tax_order.Trim().Length != 0)
        //        //{
        //        //fptr.setParam(1179, check.tax_order);//Номер предписания
        //        fptr.setParam(1179, "0");//Номер предписания
        //        //}

        //        //fptr.setParam(1179, "2.15-15/00373 от 12.01.2024");//Номер предписания

        //        //fptr.setParam(1179, "2.15-15/00373 от 12.01.2024");//Номер предписания
        //        fptr.utilFormTlv();

        //        byte[] correctionInfo = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //        fptr.setParam(1174, correctionInfo);

        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL_CORRECTION);



        //        if (check.tax_order == "")
        //        {
        //            fptr.setParam(1173, 0);
        //        }
        //        else
        //        {
        //            fptr.setParam(1173, 1);
        //        }
        //    }

        //    if (check.txtB_email_telephone.Text.Trim().Length > 0)
        //    {
        //        fptr.setParam(1008, check.txtB_email_telephone.Text);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_ELECTRONICALLY, true);
        //    }

        //    if ((check.txtB_inn.Text.Trim().Length > 0) && (check.txtB_name.Text.Trim().Length > 0))
        //    {
        //        fptr.setParam(1228, check.txtB_inn.Text);
        //        fptr.setParam(1227, check.txtB_name.Text);
        //    }

        //    if (MainStaticClass.SystemTaxation == 1)
        //    {
        //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_OSN);
        //        MainStaticClass.write_event_in_log("SNO SystemTaxation == 1 LIBFPTR_TT_OSN", "print_sell_2_or_return_sell", check.numdoc.ToString());
        //    }
        //    else if (MainStaticClass.SystemTaxation == 2)
        //    {
        //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME_OUTCOME);
        //        MainStaticClass.write_event_in_log("SNO SystemTaxation == 2 LIBFPTR_TT_USN_INCOME_OUTCOME", "print_sell_2_or_return_sell", check.numdoc.ToString());
        //    }
        //    else if (MainStaticClass.SystemTaxation == 4)
        //    {
        //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME);
        //        MainStaticClass.write_event_in_log("SNO SystemTaxation == 4 LIBFPTR_TT_USN_INCOME", "print_sell_2_or_return_sell", check.numdoc.ToString());
        //    }

        //    if (fptr.openReceipt() != 0)
        //    {
        //        MessageBox.Show(string.Format("Ошибка при открытии чека.\nОшибка {0}: {1}", fptr.errorCode(), fptr.errorDescription()),
        //                "Ошибка откртия чека", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        MainStaticClass.WriteRecordErrorLog($"Ошибка при открытии чека.\nОшибка {fptr.errorCode()}: {fptr.errorDescription()}", "print_sell_2_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //        //fptr.close();
        //        if (fptr.errorCode() == 82)
        //        {
        //            fptr.cancelReceipt();
        //            MessageBox.Show("Попробуйте распечатать чек еще раз", "Ошибка при печати чека");
        //        }
        //        return;
        //    }

        //    foreach (ListViewItem lvi in check.listView1.Items)
        //    {
        //        if (MainStaticClass.its_excise(lvi.SubItems[0].Text.Trim()) == 0)
        //        {
        //            if (lvi.SubItems[14].Text.Trim().Length <= 13)//код маркировки не заполнен
        //            {
        //                fptr.setParam(1212, 32);
        //            }
        //            else
        //            {
        //                string mark = lvi.SubItems[14].Text.Trim().Replace("vasya2021", "'");

        //                Cash_check.Requisite1260 requisite1260;

        //                if (check.verifyCDN.TryGetValue(mark, out requisite1260))
        //                {
        //                    fptr.setParam(1262, requisite1260.req1262);
        //                    fptr.setParam(1263, requisite1260.req1263);
        //                    fptr.setParam(1264, requisite1260.req1264);
        //                    fptr.setParam(1265, requisite1260.req1265);
        //                    fptr.utilFormTlv();
        //                    byte[] industryInfo = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //                    fptr.setParam(1260, industryInfo);
        //                    //fptr.setParam(1011, AtolConstants.LIBFPTR_TIME_ZONE_2);
        //                }

        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE, mark);
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_TYPE, AtolConstants.LIBFPTR_MCT12_AUTO);
        //                if ((check.check_type.SelectedIndex == 0) || (check.check_type.SelectedIndex == 2))
        //                {
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_SOLD);
        //                }
        //                else if (check.check_type.SelectedIndex == 1)
        //                {
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_RETURN);
        //                }
        //                uint result_check = 0;
        //                if (check.cdn_markers_result_check.ContainsKey(mark))
        //                {
        //                    result_check = check.cdn_markers_result_check[mark];
        //                }
        //                else
        //                {
        //                    MessageBox.Show("Код маркировки " + mark + " не найден в проверенных");
        //                }

        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_COMMODITY_NAME, lvi.SubItems[0].Text.Trim() + " " + lvi.SubItems[1].Text.Trim());
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_ONLINE_VALIDATION_RESULT, result_check);
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_PROCESSING_MODE, 0);
        //            }
        //            //}
        //        }
        //        else
        //        {
        //            fptr.setParam(1212, 2);//подакцизеый товар
        //        }

        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_COMMODITY_NAME, lvi.SubItems[0].Text.Trim() + " " + lvi.SubItems[1].Text.Trim());
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PRICE, lvi.SubItems[5].Text.Replace(",", "."));
        //        if (MainStaticClass.check_fractional_tovar(lvi.SubItems[0].Text.Trim()) == "piece")
        //        {
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MEASUREMENT_UNIT, AtolConstants.LIBFPTR_IU_PIECE);
        //        }
        //        else
        //        {
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MEASUREMENT_UNIT, AtolConstants.LIBFPTR_IU_KILOGRAM);
        //        }
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_QUANTITY, lvi.SubItems[3].Text.Replace(",", "."));
        //        int stavka_nds = MainStaticClass.get_tovar_nds(lvi.SubItems[0].Text.Trim());
        //        //nomer_naloga = 0;
        //        //MainStaticClass.use
        //        if (MainStaticClass.SystemTaxation == 1)
        //        {
        //            if (stavka_nds == 0)
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT0);
        //            }
        //            else if (stavka_nds == 10)
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT10);
        //            }
        //            else if (stavka_nds == 18)
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT20);
        //            }
        //            else if (stavka_nds == 20)
        //            {
        //                if (DateTime.Now.Year >= 2026)
        //                {
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT22);
        //                }
        //                else
        //                {
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT20);
        //                }
        //            }
        //            else if (stavka_nds == 22)
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT22);
        //            }
        //            else
        //            {
        //                MessageBox.Show("Неизвестная ставка ндс");
        //                error = true;
        //            }
        //            if (MainStaticClass.its_certificate(lvi.SubItems[0].Text.Trim()))
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT120);
        //            }
        //        }
        //        else
        //        {
        //            if (MainStaticClass.GetNdsIp == 0)
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_NO);
        //            }
        //            else if (MainStaticClass.GetNdsIp == 5)
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT5);
        //            }
        //            else if (MainStaticClass.GetNdsIp == 7)
        //            {
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT7);
        //            }
        //        }

        //        //Проверка на сертификат
        //        if (check.check_type.SelectedIndex == 0)
        //        {
        //            if (MainStaticClass.its_certificate(lvi.SubItems[0].Text.Trim()))
        //            {
        //                fptr.setParam(1214, 3);
        //            }
        //            else
        //            {
        //                fptr.setParam(1214, 4);
        //            }
        //        }

        //        fptr.registration();
        //        if (fptr.errorCode() > 0)
        //        {
        //            MessageBox.Show("При печати позиции " + lvi.SubItems[0].Text.Trim() + " " + lvi.SubItems[1].Text.Trim() + " произошли ошибки \r\n Код ошибки " + fptr.errorCode().ToString() +
        //                "\r\n " + fptr.errorDescription().ToString());
        //            MainStaticClass.WriteRecordErrorLog("При печати позиции " + lvi.SubItems[0].Text.Trim() + " " + lvi.SubItems[1].Text.Trim() + " произошли ошибки \r\n Код ошибки " + fptr.errorCode().ToString() + "\r\n " + fptr.errorDescription().ToString(), "print_sell_2_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //            error = true;
        //            fptr.cancelReceipt();
        //            break;
        //        }
        //    }

        //    if (error)
        //    {
        //        return;
        //    }

        //    // Регистрация итога (отбрасываем копейки)
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_SUM, (double)check.calculation_of_the_sum_of_the_document());
        //    fptr.receiptTotal();

        //    double[] get_result_payment = MainStaticClass.get_cash_on_type_payment(check.numdoc.ToString());
        //    if (get_result_payment[0] != 0)//Наличные
        //    {
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_CASH);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[0]);
        //        fptr.payment();
        //    }
        //    if (get_result_payment[1] != 0)
        //    {
        //        //так было раньше
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_ELECTRONICALLY);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[1]);
        //        //fptr.payment();

        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_ELECTRONICALLY);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[1]);
        //        fptr.payment();
        //        //Это новое добавлено для безналичной оплаты
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_ADD_INFO);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[1]);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ELECTRONICALLY_PAYMENT_METHOD, 0);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ELECTRONICALLY_ID, "123");
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ELECTRONICALLY_ADD_INFO, "Сведения");
        //        //fptr.payment();

        //    }
        //    if (get_result_payment[2] != 0)
        //    {
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_PREPAID);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[2]);
        //        fptr.payment();
        //    }
        //    string s = "";
        //    if (check.check_type.SelectedIndex == 0)//это продажа
        //    {
        //        if (check.Discount != 0)
        //        {
        //            //fptr.beginNonfiscalDocument();
        //            //s = "Вами получена скидка " + check.calculation_of_the_discount_of_the_document().ToString().Replace(",", ".") + " " + MainStaticClass.get_currency();
        //            s = "Вами получена скидка " + check.calculation_of_the_discount_of_the_document().ToString().Replace(",", ".") + "руб. ";

        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
        //            fptr.printText();

        //            if (check.client.Tag != null)
        //            {
        //                if (check.client.Tag == check.user.Tag)
        //                {
        //                    s = "ДК: стандартная скидка";
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
        //                    fptr.printText();
        //                }
        //                else
        //                {
        //                    s = "ДК: " + check.client.Tag.ToString();
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
        //                    fptr.printText();
        //                }
        //            }
        //        }
        //    }

        //    s = MainStaticClass.Nick_Shop + "-" + MainStaticClass.CashDeskNumber.ToString() + "-" + check.numdoc.ToString();// +" кассир " + this.cashier;
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
        //    fptr.printText();

        //    print_fiscal_advertisement(fptr);

        //    //MessageBox.Show(fptr.errorCode().ToString());
        //    fptr.setParam(1085, "NumCheckShop");
        //    fptr.setParam(1086, s);
        //    fptr.utilFormTlv();
        //    byte[] userAttribute = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //    fptr.setNonPrintableParam(1084, userAttribute);
        //    //MessageBox.Show(fptr.errorCode().ToString());

        //    // Закрытие чека
        //    if (fptr.errorCode() > 0)
        //    {
        //        MessageBox.Show("При печати чека произошли ошибки \r\n Код ошибки " + fptr.errorCode().ToString() +
        //            "\r\n " + fptr.errorDescription().ToString());
        //        error = true;
        //        fptr.cancelReceipt();
        //        return;
        //    }
        //    else
        //    {
        //        //get_actions_num_doc(check);//Печать акционных картинок
        //        fptr.closeReceipt();
        //    }

        //    while (fptr.checkDocumentClosed() < 0)
        //    {
        //        // Не удалось проверить состояние документа. Вывести пользователю текст ошибки, попросить устранить неполадку и повторить запрос
        //        MessageBox.Show(fptr.errorCode().ToString() + " " + fptr.errorDescription(), " Ошибка при печати чека ");
        //        MainStaticClass.WriteRecordErrorLog(fptr.errorCode().ToString() + fptr.errorDescription().ToString(), "print_sell_2_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //        if (MessageBox.Show(" Продолжать попытки печати чека ", "Ошибка при печати чека", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
        //        {
        //            continue;
        //        }
        //        else
        //        {
        //            error = true;
        //            break;
        //        }
        //    }

        //    if ((!fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_DOCUMENT_CLOSED)) || (error))
        //    {
        //        // Документ не закрылся. Требуется его отменить (если это чек) и сформировать заново
        //        fptr.cancelReceipt();
        //        MessageBox.Show(String.Format("Не удалось напечатать документ (Ошибка \"{0}\"). Устраните неполадку и повторите.", fptr.errorDescription()));
        //        MainStaticClass.WriteRecordErrorLog(String.Format("Не удалось напечатать документ (Ошибка \"{0}\"). Устраните неполадку и повторите.", fptr.errorDescription()), "print_sell_2_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //        error = true;
        //        //fptr.close();
        //        return;
        //    }

        //    if (!fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_DOCUMENT_PRINTED))
        //    {
        //        // Можно сразу вызвать метод допечатывания документа, он завершится с ошибкой, если это невозможно
        //        while (fptr.continuePrint() < 0)
        //        {
        //            // Если не удалось допечатать документ - показать пользователю ошибку и попробовать еще раз.
        //            MessageBox.Show(String.Format("Не удалось напечатать документ (Ошибка \"{0}\"). Устраните неполадку и повторите.", fptr.errorDescription()));
        //            MainStaticClass.WriteRecordErrorLog(String.Format("Не удалось напечатать документ (Ошибка \"{0}\"). Устраните неполадку и повторите.", fptr.errorDescription()), "print_sell_2_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //            //*********************************
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_SHORT_STATUS);
        //            fptr.queryData();
        //            bool isPaperPresent = fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_RECEIPT_PAPER_PRESENT);
        //            if (!isPaperPresent)
        //            {
        //                MessageBox.Show("В ФР закончилась бумага.");
        //            }
        //            //*********************************
        //            continue;
        //        }
        //    }
        //    //s = MainStaticClass.Nick_Shop + "-" + MainStaticClass.CashDeskNumber.ToString() + "-" + check.numdoc.ToString();// +" кассир " + this.cashier;
        //    //fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //    //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
        //    //fptr.printText();
        //    //print_fiscal_advertisement(fptr);

        //    // Завершение работы
        //    //fptr.close();
        //    if (!error)
        //    {
        //        MainStaticClass.its_print(check.numdoc.ToString());
        //        check.closing = false;
        //        check.Close();
        //    }
        //    else
        //    {
        //        MessageBox.Show("При печати чека произошли ошибки,печать чека будет отменена", "Печать чека");
        //        MainStaticClass.WriteRecordErrorLog(String.Format("Не удалось напечатать документ (Ошибка \"{0}\"). Устраните неполадку и повторите.", fptr.errorDescription()), "print_sell_2_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //        fptr.cancelReceipt();
        //    }



        //    //if (MainStaticClass.GetVariantConnectFN == 1)
        //    //{
        //    //    fptr.close();
        //    //}
        //    //print_promo();
        //}

        //public void print_sell_2_3_or_return_sell(Cash_check check, int variant)
        //{

        //    bool error = false;

        //    IFptr fptr = MainStaticClass.FPTR;
        //    //setConnectSetting(fptr);
        //    if (!fptr.isOpened())
        //    {
        //        fptr.open();
        //    }

        //    if (!check.print_promo_picture)
        //    {
        //        get_actions_num_doc(check);//Печать акционных картинок
        //        check.print_promo_picture = true;
        //    }

        //    //fptr.setParam(1021, MainStaticClass.Cash_Operator);
        //    //fptr.setParam(1203, MainStaticClass.CashOperatorInn);
        //    //fptr.operatorLogin();


        //    //print_terminal_check(fptr, check);
        //    //if (variant == 0)
        //    //{
        //    //    fptr.setParam(1055, AtolConstants.LIBFPTR_TT_PATENT);
        //    //    MainStaticClass.write_event_in_log("SNO SystemTaxation == LIBFPTR_TT_PATENT variant = 0 не маркировка", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //    //}
        //    //else if (variant == 1)
        //    //{
        //    //    if (MainStaticClass.SystemTaxation == 3)
        //    //    {
        //    //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME_OUTCOME);
        //    //        MainStaticClass.write_event_in_log("SNO SystemTaxation == 3 variant = 1 маркировка LIBFPTR_TT_USN_INCOME_OUTCOME", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //    //    }
        //    //    else if (MainStaticClass.SystemTaxation == 5)
        //    //    {
        //    //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME);
        //    //        MainStaticClass.write_event_in_log("SNO SystemTaxation == 5 variant = 1 маркировка LIBFPTR_TT_USN_INCOME", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //    //    }                
        //    //}
        //    //else
        //    //{
        //    //    MessageBox.Show("В печать не передан или передан не верный вариант, печать невозможна");
        //    //    return;
        //    //}


        //    if (variant == 0)
        //    {
        //        //if (check.to_print_certainly == 1)
        //        //{
        //        //    MainStaticClass.delete_document_wil_be_printed(check.numdoc.ToString(), variant);
        //        //}

        //        //if (MainStaticClass.get_document_wil_be_printed(check.numdoc.ToString()) != 0)
        //        //{
        //        //    MessageBox.Show("Этот чек уже был успешно отправлен на печать");
        //        //    return;
        //        //}
        //        //здесь проверяем есть ли строки
        //        int count_string = 0;
        //        foreach (ListViewItem lvi in check.listView1.Items)
        //        {
        //            if (lvi.SubItems[14].Text.Trim().Length <= 13)
        //            {
        //                count_string++;
        //            }
        //        }
        //        if (count_string == 0)
        //        {
        //            //здесь необходимо поставить флаг распечатан
        //            check.its_print_p(variant);
        //            return;
        //        }

        //    }
        //    else if (variant == 1)
        //    {
        //        fptr.openDrawer();
        //        //здесь проверяем есть ли строки

        //        //if (check.to_print_certainly_p == 1)
        //        //{
        //        //    MainStaticClass.delete_document_wil_be_printed(check.numdoc.ToString(), variant);
        //        //}

        //        //if (MainStaticClass.get_document_wil_be_printed(check.numdoc.ToString(), variant) != 0)
        //        //{
        //        //    MessageBox.Show("Этот чек уже был успешно отправлен на печать");
        //        //    return;
        //        //}
        //        int count_string = 0;
        //        foreach (ListViewItem lvi in check.listView1.Items)
        //        {
        //            if (lvi.SubItems[14].Text.Trim().Length > 13)
        //            {
        //                count_string++;
        //            }
        //        }
        //        if (count_string == 0)
        //        {
        //            //здесь необходимо поставить флаг распечатан
        //            check.its_print_p(variant);
        //            //if (MainStaticClass.GetVariantConnectFN == 1)
        //            //{
        //            //    fptr.close();
        //            //}
        //            return;
        //        }

        //        if (check.check_type.SelectedIndex == 1 || check.check_type.SelectedIndex == 2 || check.reopened)//для возвратов и красных чеков старая схема
        //        {
        //            //здесь необходимо выполнить запрос на cdn
        //            ProductData productData = new ProductData(0, "", 0, ProductFlags.None);

        //            fptr.clearMarkingCodeValidationResult();
        //            check.cdn_markers_result_check.Clear();//если мы здесь предыдущие проверки очищаем
        //            foreach (ListViewItem lvi in check.listView1.Items)
        //            {
        //                if (lvi.SubItems[14].Text.Trim().Length > 13)
        //                {
        //                    //if (cdn_check(lvi.SubItems[0].Text.Trim(), check.numdoc.ToString()))
        //                    //{
        //                    //    continue;
        //                    //}

        //                    string mark = lvi.SubItems[14].Text.Trim().Replace("vasya2021", "'");

        //                    if (InventoryManager.completeDictionaryProductData)
        //                    {
        //                        productData = InventoryManager.GetItem(Convert.ToInt64(lvi.SubItems[0].Text.Trim()));
        //                    }
        //                    else
        //                    {
        //                        productData = check.GetProductDataInDB(lvi.SubItems[0].Text.Trim());
        //                    }

        //                    if (productData.IsCDNCheck())// && check.check_type.SelectedIndex == 0)
        //                    {
        //                        if (!MainStaticClass.cdn_check(productData, mark, lvi, check))
        //                        {
        //                            error = true;
        //                        }
        //                    }

        //                    //string mark = lvi.SubItems[14].Text.Trim().Replace("vasya2021", "'");
        //                    bool result_check_mark = check_marking_code(mark, check.numdoc.ToString(), ref check.cdn_markers_result_check, check.check_type.SelectedIndex);
        //                    if (!result_check_mark)
        //                    {
        //                        error = true;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (error)
        //    {
        //        return;
        //    }


        //    fptr.setParam(1021, MainStaticClass.Cash_Operator);
        //    fptr.setParam(1203, MainStaticClass.CashOperatorInn);
        //    fptr.operatorLogin();

        //    print_terminal_check(fptr, check);
        //    //if (variant == 0)
        //    //{
        //    //    fptr.setParam(1055, AtolConstants.LIBFPTR_TT_PATENT);
        //    //    MainStaticClass.write_event_in_log("SNO SystemTaxation == LIBFPTR_TT_PATENT variant = 0 не маркировка", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //    //}
        //    //else if (variant == 1)
        //    //{
        //    //    if (MainStaticClass.SystemTaxation == 3)
        //    //    {
        //    //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME_OUTCOME);
        //    //        MainStaticClass.write_event_in_log("SNO SystemTaxation == 3 variant = 1 маркировка LIBFPTR_TT_USN_INCOME_OUTCOME", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //    //    }
        //    //    else if (MainStaticClass.SystemTaxation == 5)
        //    //    {
        //    //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME);
        //    //        MainStaticClass.write_event_in_log("SNO SystemTaxation == 5 variant = 1 маркировка LIBFPTR_TT_USN_INCOME", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    MessageBox.Show("В печать не передан или передан не верный вариант, печать невозможна");
        //    //    return;
        //    //}


        //    if (check.check_type.SelectedIndex == 0)// || (check.check_type.SelectedIndex == 2))
        //    {
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL);
        //    }
        //    else if (check.check_type.SelectedIndex == 1)
        //    {
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL_RETURN);
        //    }
        //    else if (check.check_type.SelectedIndex == 2)
        //    {
        //        ////fptr.setParam(1177, "Служебная записка");
        //        //fptr.setParam(1178, check.sale_date);//Дата продажи
        //        //if (check.tax_order.Trim().Length != 0)
        //        //{
        //        //    fptr.setParam(1179, check.tax_order);//Номер предписания
        //        //}

        //        //fptr.utilFormTlv();
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL_CORRECTION);

        //        //byte[] correctionInfo = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //        //if (check.tax_order == "")
        //        //{
        //        //    fptr.setParam(1173, 0);
        //        //}
        //        //else
        //        //{
        //        //    fptr.setParam(1173, 1);
        //        //}
        //        ////fptr.setParam(1174, correctionInfo);

        //        //fptr.setParam(1177, "Служебная записка");
        //        fptr.setParam(1178, check.sale_date);//Дата продажи
        //        //if (check.tax_order.Trim().Length != 0)
        //        //{
        //        //fptr.setParam(1179, check.tax_order);//Номер предписания
        //        fptr.setParam(1179, "0");//Номер предписания
        //        //}

        //        //fptr.setParam(1179, "2.15-15/00373 от 12.01.2024");//Номер предписания

        //        //fptr.setParam(1179, "2.15-15/00373 от 12.01.2024");//Номер предписания
        //        fptr.utilFormTlv();

        //        byte[] correctionInfo = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //        fptr.setParam(1174, correctionInfo);

        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL_CORRECTION);

        //        if (check.tax_order == "")
        //        {
        //            fptr.setParam(1173, 0);
        //        }
        //        else
        //        {
        //            fptr.setParam(1173, 1);
        //        }

        //    }

        //    // bool closing = false;

        //    // Открытие чека (с передачей телефона получателя)
        //    //if (check.check_type.SelectedIndex == 0)
        //    //{
        //    //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL);
        //    //}
        //    //else
        //    //{
        //    //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_TYPE, AtolConstants.LIBFPTR_RT_SELL_RETURN);
        //    //}

        //    //fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_ELECTRONICALLY, true);


        //    if (check.txtB_email_telephone.Text.Trim().Length > 0)
        //    {
        //        fptr.setParam(1008, check.txtB_email_telephone.Text);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_RECEIPT_ELECTRONICALLY, true);
        //    }

        //    if ((check.txtB_inn.Text.Trim().Length > 0) && (check.txtB_name.Text.Trim().Length > 0))
        //    {
        //        fptr.setParam(1228, check.txtB_inn.Text);
        //        fptr.setParam(1227, check.txtB_name.Text);
        //    }

        //    if (variant == 0)
        //    {
        //        fptr.setParam(1055, AtolConstants.LIBFPTR_TT_PATENT);
        //        MainStaticClass.write_event_in_log("SNO SystemTaxation == LIBFPTR_TT_PATENT variant = 0 не маркировка", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //    }
        //    else if (variant == 1)
        //    {
        //        if (MainStaticClass.SystemTaxation == 3)
        //        {
        //            fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME_OUTCOME);
        //            MainStaticClass.write_event_in_log("SNO SystemTaxation == 3 variant = 1 маркировка LIBFPTR_TT_USN_INCOME_OUTCOME", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //        }
        //        else if (MainStaticClass.SystemTaxation == 5)
        //        {
        //            fptr.setParam(1055, AtolConstants.LIBFPTR_TT_USN_INCOME);
        //            MainStaticClass.write_event_in_log("SNO SystemTaxation == 5 variant = 1 маркировка LIBFPTR_TT_USN_INCOME", "print_sell_2_3_or_return_sell", check.numdoc.ToString());
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("В печать не передан или передан не верный вариант, печать невозможна");
        //        return;
        //    }


        //    //fptr.closeReceipt();//.cancelReceipt();//
        //    if (fptr.openReceipt() != 0)
        //    {
        //        MessageBox.Show(string.Format("Ошибка при открытии чека.\nОшибка {0}: {1}", fptr.errorCode(), fptr.errorDescription()),
        //                "Ошибка откртия чека", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        MainStaticClass.WriteRecordErrorLog($"Ошибка при открытии чека.\nОшибка {fptr.errorCode()}: {fptr.errorDescription()}", "print_sell_2_3_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //        //fptr.close();
        //        if (fptr.errorCode() == 82)
        //        {
        //            fptr.cancelReceipt();
        //            MessageBox.Show("Попробуйте распечатать чек еще раз", "Ошибка при печати чека");
        //        }
        //        return;
        //    }



        //    foreach (ListViewItem lvi in check.listView1.Items)
        //    {

        //        if (variant == 0)
        //        {
        //            if (lvi.SubItems[14].Text.Trim().Length > 13)
        //            {
        //                continue;
        //            }
        //        }
        //        else if (variant == 1)
        //        {
        //            if (lvi.SubItems[14].Text.Trim().Length < 14)
        //            {
        //                continue;
        //            }
        //        }

        //        if (MainStaticClass.its_excise(lvi.SubItems[0].Text.Trim()) == 0)
        //        {
        //            if (lvi.SubItems[14].Text.Trim().Length <= 13)//код маркировки не заполнен
        //            {
        //                fptr.setParam(1212, 32);
        //            }
        //            else
        //            {

        //                //if (!cdn_check(lvi.SubItems[0].Text.Trim(), check.numdoc.ToString()))
        //                //{
        //                //string marker_code = lvi.SubItems[14].Text.Trim().Replace("vasya2021", "'");
        //                //byte[] textAsBytes = System.Text.Encoding.Default.GetBytes(marker_code);
        //                //string mark = Convert.ToBase64String(textAsBytes);
        //                string mark = lvi.SubItems[14].Text.Trim().Replace("vasya2021", "'");
        //                Cash_check.Requisite1260 requisite1260;

        //                if (check.verifyCDN.TryGetValue(mark, out requisite1260))
        //                {
        //                    fptr.setParam(1262, requisite1260.req1262);
        //                    fptr.setParam(1263, requisite1260.req1263);
        //                    fptr.setParam(1264, requisite1260.req1264);
        //                    fptr.setParam(1265, requisite1260.req1265);
        //                    fptr.utilFormTlv();
        //                    byte[] industryInfo = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //                    fptr.setParam(1260, industryInfo);
        //                    //fptr.setParam(1011, AtolConstants.LIBFPTR_TIME_ZONE_2);
        //                }

        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE, mark);
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_TYPE, AtolConstants.LIBFPTR_MCT12_AUTO);
        //                //fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_SOLD);
        //                if ((check.check_type.SelectedIndex == 0) || (check.check_type.SelectedIndex == 2))
        //                {
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_SOLD);
        //                }
        //                else if (check.check_type.SelectedIndex == 1)
        //                {
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_RETURN);
        //                }
        //                uint result_check = 0;
        //                if (check.cdn_markers_result_check.ContainsKey(mark))
        //                {
        //                    result_check = check.cdn_markers_result_check[mark];
        //                }
        //                else
        //                {
        //                    MessageBox.Show("Код маркировки " + mark + " не найден в проверенных");
        //                }

        //                //Cash_check.Requisite1260 requisite1260;

        //                //if (check.verifyCDN.TryGetValue(mark, out requisite1260))
        //                //{
        //                //    fptr.setParam(1262, requisite1260.req1262);
        //                //    fptr.setParam(1263, requisite1260.req1263);
        //                //    fptr.setParam(1264, requisite1260.req1264);
        //                //    fptr.setParam(1265, requisite1260.req1265);
        //                //    fptr.utilFormTlv();
        //                //    byte[] industryInfo = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //                //    fptr.setParam(1260, industryInfo);
        //                //}

        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_ONLINE_VALIDATION_RESULT, result_check);
        //                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_PROCESSING_MODE, 0);
        //                //}
        //            }
        //        }
        //        else
        //        {
        //            fptr.setParam(1212, 2);//подакцизеый товар
        //        }

        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_COMMODITY_NAME, lvi.SubItems[0].Text.Trim() + " " + lvi.SubItems[1].Text.Trim());
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PRICE, lvi.SubItems[5].Text.Replace(",", "."));
        //        if (MainStaticClass.check_fractional_tovar(lvi.SubItems[0].Text.Trim()) == "piece")
        //        {
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MEASUREMENT_UNIT, AtolConstants.LIBFPTR_IU_PIECE);
        //        }
        //        else
        //        {
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MEASUREMENT_UNIT, AtolConstants.LIBFPTR_IU_KILOGRAM);
        //        }
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_QUANTITY, lvi.SubItems[3].Text.Replace(",", "."));

        //        if (MainStaticClass.GetNdsIp == 0)
        //        {
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_NO);
        //        }
        //        else if (MainStaticClass.GetNdsIp == 1)
        //        {
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT5);
        //        }
        //        else if (MainStaticClass.GetNdsIp == 2)
        //        {
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TAX_TYPE, AtolConstants.LIBFPTR_TAX_VAT7);
        //        }

        //        //Проверка на сертификат
        //        if (check.check_type.SelectedIndex == 0)
        //        {
        //            if (MainStaticClass.its_certificate(lvi.SubItems[0].Text.Trim()))
        //            {
        //                fptr.setParam(1214, 3);
        //            }
        //            else
        //            {
        //                fptr.setParam(1214, 4);
        //            }
        //        }

        //        fptr.registration();
        //        if (fptr.errorCode() > 0)
        //        {
        //            MessageBox.Show("При печати позиции " + lvi.SubItems[0].Text.Trim() + " " + lvi.SubItems[1].Text.Trim() + " произошли ошибки \r\n Код ошибки " + fptr.errorCode().ToString() +
        //                "\r\n " + fptr.errorDescription().ToString());
        //            error = true;
        //            fptr.cancelReceipt();
        //            MainStaticClass.WriteRecordErrorLog("При печати позиции " + lvi.SubItems[0].Text.Trim() + " " + lvi.SubItems[1].Text.Trim() + " произошли ошибки \r\n Код ошибки " + fptr.errorCode().ToString() +
        //                "\r\n " + fptr.errorDescription().ToString(), "print_sell_2_3_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //            //fptr.resetError();
        //            break;
        //        }
        //    }

        //    // Регистрация итога (отбрасываем копейки)
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_SUM, (double)check.calculation_of_the_sum_of_the_document());
        //    fptr.receiptTotal();

        //    double[] get_result_payment = check.get_summ1_systemtaxation3(variant);//MainStaticClass.get_cash_on_type_payment(check.numdoc.ToString());
        //    if (get_result_payment[0] != 0)//Наличные
        //    {
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_CASH);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[0]);
        //        fptr.payment();
        //    }
        //    if (get_result_payment[1] != 0)
        //    {
        //        //так было раньше
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_ELECTRONICALLY);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[1]);
        //        //fptr.payment();

        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_ELECTRONICALLY);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[1]);
        //        fptr.payment();

        //        ////Это новое добавлено для безналичной оплаты
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_ADD_INFO);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[1]);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ELECTRONICALLY_PAYMENT_METHOD, 0);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ELECTRONICALLY_ID, "123");
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ELECTRONICALLY_ADD_INFO, "Сведения");
        //        //fptr.payment();
        //    }
        //    if (get_result_payment[2] != 0)
        //    {
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_TYPE, AtolConstants.LIBFPTR_PT_PREPAID);
        //        fptr.setParam(AtolConstants.LIBFPTR_PARAM_PAYMENT_SUM, get_result_payment[2]);
        //        fptr.payment();
        //    }
        //    string s = "";
        //    if (check.check_type.SelectedIndex == 0)//это продажа
        //    {
        //        if (check.Discount != 0)
        //        {
        //            //s = "Вами получена скидка " + check.calculation_of_the_discount_of_the_document().ToString().Replace(",", ".") + " руб. " + MainStaticClass.get_currency();
        //            s = "Вами получена скидка " + check.calculation_of_the_discount_of_the_document().ToString().Replace(",", ".") + " руб. ";
        //            fptr.beginNonfiscalDocument();

        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //            fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
        //            fptr.printText();

        //            if (check.client.Tag != null)
        //            {
        //                if (check.client.Tag == check.user.Tag)
        //                {
        //                    s = "ДК: стандартная скидка";
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
        //                    fptr.printText();
        //                }
        //                else
        //                {
        //                    s = "ДК: " + check.client.Tag.ToString();
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
        //                    fptr.printText();
        //                }
        //            }
        //        }
        //    }

        //    s = MainStaticClass.Nick_Shop + "-" + MainStaticClass.CashDeskNumber.ToString() + "-" + check.numdoc.ToString();// +" кассир " + this.cashier;
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_TEXT, s);
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DEFER, AtolConstants.LIBFPTR_DEFER_POST);
        //    fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_LEFT);
        //    fptr.printText();
        //    print_fiscal_advertisement(fptr);

        //    fptr.setParam(1085, "NumCheckShop");
        //    fptr.setParam(1086, s);
        //    fptr.utilFormTlv();
        //    byte[] userAttribute = fptr.getParamByteArray(AtolConstants.LIBFPTR_PARAM_TAG_VALUE);
        //    fptr.setNonPrintableParam(1084, userAttribute);




        //    // Закрытие чека
        //    if (fptr.errorCode() > 0)
        //    {
        //        MessageBox.Show("При печати чека произошли ошибки \r\n Код ошибки " + fptr.errorCode().ToString() +
        //            "\r\n " + fptr.errorDescription().ToString());
        //        MainStaticClass.WriteRecordErrorLog($"Ошибка при закрытии чека.\nОшибка {fptr.errorCode()}: {fptr.errorDescription()}", "print_sell_2_3_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //        error = true;
        //        fptr.cancelReceipt();
        //        return;
        //    }
        //    else
        //    {
        //        //print_promo();
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_SCALE_PERCENT, 150);

        //        //fptr.setParam(AtolConstants.LIBFPTR_PARAM_FILENAME, "C:\\2025-05-13_10-30.png");
        //        //fptr.printPicture();
        //        //if (!check.print_promo_picture)
        //        //{
        //        //    get_actions_num_doc(check);//Печать акционных картинок
        //        //    check.print_promo_picture = true;
        //        //}

        //        fptr.closeReceipt();
        //    }

        //    while (fptr.checkDocumentClosed() < 0)
        //    {
        //        // Не удалось проверить состояние документа. Вывести пользователю текст ошибки, попросить устранить неполадку и повторить запрос
        //        MessageBox.Show(fptr.errorCode().ToString() + " " + fptr.errorDescription(), " Ошибка при печати чека ");
        //        MainStaticClass.WriteRecordErrorLog($"Ошибка при закрытии чека.\nОшибка {fptr.errorCode()}: {fptr.errorDescription()}", "print_sell_2_3_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //        if (MessageBox.Show(" Продолжать попытки печати чека ", "Ошибка при печати чека", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
        //        {
        //            continue;
        //        }
        //        else
        //        {
        //            error = true;
        //            break;
        //        }
        //    }

        //    if ((!fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_DOCUMENT_CLOSED)) || (error))
        //    {
        //        // Документ не закрылся. Требуется его отменить (если это чек) и сформировать заново
        //        fptr.cancelReceipt();
        //        //fptr.close();
        //        return;
        //    }

        //    if (!fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_DOCUMENT_PRINTED))
        //    {
        //        // Можно сразу вызвать метод допечатывания документа, он завершится с ошибкой, если это невозможно
        //        while (fptr.continuePrint() < 0)
        //        {
        //            // Если не удалось допечатать документ - показать пользователю ошибку и попробовать еще раз.
        //            MessageBox.Show(String.Format("Не удалось напечатать документ (Ошибка \"{0}\"). Устраните неполадку и повторите.", fptr.errorDescription()));
        //            MainStaticClass.WriteRecordErrorLog(String.Format("Не удалось напечатать документ (Ошибка \"{0}\"). Устраните неполадку и повторите.", fptr.errorDescription()), "print_sell_2_3_or_return_sell", check.numdoc, MainStaticClass.CashDeskNumber, "Ошибка при открытии чека");
        //            continue;
        //        }
        //    }

        //    if (!error)
        //    {

        //        check.its_print_p(variant);
        //        if (variant == 1)
        //        {
        //            check.Close();
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("При печати чека произошли ошибки,печать чека будет отменена", "Печать чека");
        //        fptr.cancelReceipt();
        //    }
        //}

        /// <summary>
        /// Преобразует шестнадцатеричную строку в массив байтов.
        /// </summary>
        /// <param name="hexString">Шестнадцатеричная строка.</param>
        /// <returns>Массив байтов.</returns>
        static byte[] HexStringToByteArray(string hexString)
        {
            int length = hexString.Length;
            byte[] byteArray = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                // Преобразуем каждую пару символов в байт
                byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return byteArray;
        }


        /// <summary>
        /// Добавляет новую запись в таблицу picture_number_in_f_r.
        /// </summary>
        /// <param name="idFR">Значение id_f_r</param>
        /// <param name="objectType">Значение object_type</param>
        /// <param name="idObject">Значение id_object</param>
        /// <returns>
        /// 1, если запись успешно добавлена,
        /// -1 в случае ошибки.
        /// </returns>
        public int InsertRecordPictureNumbe(int idFR, short objectType, int idObject)
        {
            // SQL-запрос для вставки новой записи
            string query = @"
        INSERT INTO picture_number_in_f_r (id_f_r, object_type, id_object)
        VALUES (@idFR, @objectType, @idObject);";

            try
            {
                // Открываем соединение с базой данных
                using (var connection = MainStaticClass.NpgsqlConn())
                {
                    connection.Open();

                    // Создаем команду с параметрами
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Добавляем параметры
                        command.Parameters.AddWithValue("idFR", idFR);
                        command.Parameters.AddWithValue("objectType", objectType);
                        command.Parameters.AddWithValue("idObject", idObject);

                        // Выполняем запрос
                        int rowsAffected = command.ExecuteNonQuery();

                        // Если хотя бы одна строка была добавлена, возвращаем 1
                        return rowsAffected > 0 ? 1 : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                MessageBox.Show($"Ошибка при добавлении записи в таблицу: {ex.Message}");
                return -1; // Возвращаем -1 в случае ошибки
            }
        }


        //// <summary>
        /// Проверяет, существует ли запись в таблице picture_number_in_f_r по заданным id_object и object_type.
        /// </summary>
        /// <param name="idObject">Значение id_object</param>
        /// <param name="objectType">Значение object_type</param>
        /// <returns>
        /// id_f_r, если запись существует,
        /// 0, если записи нет,
        /// -1 в случае ошибки.
        /// </returns>
        public int CheckRecordExists(int idObject, short objectType)
        {
            // SQL-запрос для получения id_f_r
            string query = @"
        SELECT id_f_r
        FROM picture_number_in_f_r
        WHERE id_object = @idObject AND object_type = @objectType;";

            try
            {
                // Открываем соединение с базой данных
                using (var connection = MainStaticClass.NpgsqlConn())
                {
                    connection.Open();

                    // Создаем команду с параметрами
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Добавляем параметры
                        command.Parameters.AddWithValue("idObject", idObject);
                        command.Parameters.AddWithValue("objectType", objectType);

                        // Выполняем запрос и получаем результат
                        var result = command.ExecuteScalar();

                        // Если результат не null, значит запись существует
                        if (result != null && result != DBNull.Value)
                        {
                            // Преобразуем результат в int (id_f_r)
                            return Convert.ToInt32(result);
                        }

                        // Если результат null, значит записи нет
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                MessageBox.Show($"Ошибка при проверке соответствия записи в бд: {ex.Message}");
                return -1; // Возвращаем -1 в случае ошибки
            }
        }

        private void print_picture(string hex_string, string action_num_doc)
        {

            IFptr fptr = MainStaticClass.FPTR;
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.beginNonfiscalDocument();

            // Преобразование шестнадцатеричной строки в массив байтов            
            byte[] byteArray = Convert.FromBase64String(hex_string);

            // Запись массива байтов в новый файл            
            string outputFilePath = Path.Combine(    AppDomain.CurrentDomain.BaseDirectory,    "Pictures2",    $"A{action_num_doc}_picture.png");

            if (!System.IO.File.Exists(outputFilePath))
            {
                System.IO.File.WriteAllBytes(outputFilePath, byteArray);
            }

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_ALIGNMENT, AtolConstants.LIBFPTR_ALIGNMENT_CENTER);
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_FILENAME, outputFilePath);

            fptr.printPicture();

            if (fptr.errorCode() != 0)
            {
                MainStaticClass.WriteRecordErrorLog("Номер акции " + action_num_doc + "\r\n" + fptr.errorDescription(), "print_picture", 0, MainStaticClass.CashDeskNumber, "Ошибка при печати акционной картинки ");
                //MessageBox.Show("Номер акции " + action_num_doc + "\r\n" + fptr.errorDescription(), "Ошибка при печати акционного купона", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            fptr.endNonfiscalDocument();
        }

        private void get_actions_num_doc(Cash_check check)
        {
            List<string> numDocsAction = new List<string>();

            //foreach (ListViewItem lvi in check.listView1.Items)
            //{
            //    string action_num_doc = lvi.SubItems[10].Text.Trim();

            //    if (action_num_doc.Length > 1)
            //    {
            //        if (numDocsAction.IndexOf(action_num_doc) == -1)
            //        {
            //            numDocsAction.Add(action_num_doc);
            //        }
            //    }
            //}
            foreach (int action_num_doc in check.action_num_doc)//номера акционных документов акций 10 типа 
            {
                numDocsAction.Add(action_num_doc.ToString());
            }

            if (numDocsAction.Count > 0)
            {
                //MessageBox.Show("Акция с картинкой есть");
                print_pictures(numDocsAction);
            }
            else
            {
                //MessageBox.Show("Нет акций с картинками");
            }
        }

        public void print_pictures(List<string> numDocsAction)
        {
            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    conn.Open();
                    foreach (var item in numDocsAction)
                    {
                        string query = "SELECT picture	FROM public.action_header WHERE num_doc=" + item;
                        NpgsqlCommand command = new NpgsqlCommand(query, conn);
                        string hex_string = command.ExecuteScalar().ToString();
                        //hex_string = hex_string.Substring(2, hex_string.Length - 2);
                        if (hex_string.Trim().Length > 0)
                        {
                            print_picture(hex_string, item);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show($"Ошибка базы данных: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Общие ошибки
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                }
            }
        }

        private void start_and_restart_beginMarkingCodeValidation(string mark, int check_type)
        {
            IFptr fptr = MainStaticClass.FPTR;

            if (!fptr.isOpened())
            {
                fptr.open();
            }

            // Запускаем проверку КМ
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_TYPE, AtolConstants.LIBFPTR_MCT12_AUTO);
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE, mark);
            //fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_FOR_SALE);
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_SOLD);

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_PROCESSING_MODE, 0);

            if (check_type == 0)
            {
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_SOLD);//LIBFPTR_MES_PIECE_SOLD
            }
            else if (check_type == 1)
            {
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_RETURN);
            }

            fptr.beginMarkingCodeValidation();
            if (fptr.errorCode() == 401)//процедура проверки кода уже запущена 
            {
                fptr.cancelMarkingCodeValidation();//прерываем ее и пробуем снова 
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_TYPE, AtolConstants.LIBFPTR_MCT12_AUTO);
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE, mark);
                //fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_FOR_SALE);                
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_STATUS, AtolConstants.LIBFPTR_MES_PIECE_SOLD);
                fptr.setParam(AtolConstants.LIBFPTR_PARAM_MARKING_PROCESSING_MODE, 0);

                fptr.beginMarkingCodeValidation();
            }
        }

        public void CheckTaxationTypes()
        {
            uint taxationTypes = 0;
            try
            {
                IFptr fptr = MainStaticClass.FPTR;

                if (!fptr.isOpened())
                {
                    fptr.open();
                }

                fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_REG_INFO);
                fptr.fnQueryData();

                if (fptr.errorCode() != 0)
                {
                    throw new Exception(fptr.errorDescription().ToString());
                }

                taxationTypes = fptr.getParamInt(1062);

                var taxationMapping = new Dictionary<uint, int>
        {
            { 1, 1 },   // ОСН
            { 4, 2 },   // УСН Доходы - Расходы
            { 36, 3 },  // (УСНДоходы - Расходы) + Патент
            { 2, 4 },   // УСН Доходы
            { 34, 5 }   // УСНДоходы + Патент            
        };

                if (taxationMapping.TryGetValue(taxationTypes, out int expectedSystemTaxation) || (taxationTypes == 32))
                {
                    if (MainStaticClass.SystemTaxation != expectedSystemTaxation)
                    {
                        ShowTaxationMismatchMessage(taxationTypes, MainStaticClass.SystemTaxation);
                    }
                }
                else
                {
                    MessageBox.Show("Неизвестная система налогообложения в фискальном регистраторе. Свяжитесь с бухгалтерией.", "Проверка системы налогообложения");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при проверке системы налогообложения. Свяжитесь с поддержкой. " + ex.Message, "Проверка системы налогообложения");
            }
        }

        private async void ShowTaxationMismatchMessage(uint taxationTypes, int systemTaxationInProgram)
        {
            // Описание систем налогообложения в ФР
            var taxationDescriptionsFR = new Dictionary<uint, string>
    {
        { 1, "ОСН" },
        { 4, "УСН Доходы - Расходы" },
        { 36, "УСН Доходы - Расходы + Патент" },
        { 2, "УСН Доходы" },
        { 34, "УСН Доходы + Патент" },
        { 32, "Патент" }
    };

            // Описание систем налогообложения в программе
            var taxationDescriptionsProgram = new Dictionary<int, string>
    {
        { 1, "ОСН" },
        { 2, "УСН Доходы - Расходы" },
        { 3, "УСН Доходы - Расходы + Патент" },
        { 4, "УСН Доходы" },
        { 5, "УСН Доходы + Патент" }
    };

            // Получаем описание системы налогообложения в ФР
            string frTaxationDescription = taxationDescriptionsFR.TryGetValue(taxationTypes, out var frDescription)
                ? frDescription
                : "Неизвестная система";

            // Получаем описание системы налогообложения в программе
            string programTaxationDescription = taxationDescriptionsProgram.TryGetValue(systemTaxationInProgram, out var programDescription)
                ? programDescription
                : "Неизвестная система";

            // Формируем сообщение
            string message = $"Система налогообложения в фискальном регистраторе: {frTaxationDescription}.\n" +
                             $"Система налогообложения в программе: {programTaxationDescription}.\n" +
                             "Вам необходимо связаться с бухгалтерией для устранения расхождения.";

            await MessageBox.Show(message, "Проверка системы налогообложения");
        }


        public bool check_marking_code(string mark, string num_doc, ref Dictionary<string, uint> cdn_markers_result_check, int check_type)        
        {
            bool result = true;
            IFptr fptr = MainStaticClass.FPTR;

            if (!fptr.isOpened())
            {
                fptr.open();
            }

            mark = mark.Replace("vasya2021", "'");//На всякий случай

            fptr.setParam(1021, MainStaticClass.Cash_Operator);
            fptr.setParam(1203, MainStaticClass.CashOperatorInn);
            fptr.operatorLogin();

            start_and_restart_beginMarkingCodeValidation(mark, check_type);

            uint validationError = Convert.ToUInt16(fptr.errorCode());

            if (validationError == 0)
            {
                while (true)
                {
                    fptr.getMarkingCodeValidationStatus();
                    if (fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_VALIDATION_READY))
                    {
                        if (fptr.errorCode() != 0)
                        {
                            validationError = Convert.ToUInt16(fptr.errorCode());
                            MainStaticClass.write_cdn_log("PrintingUsingLibraries check_marking_code LIBFPTR_PARAM_MARKING_CODE_VALIDATION_READY 1", num_doc, mark, "4");
                        }
                        break;
                    }
                    if (fptr.errorCode() != 0)
                    {
                        validationError = Convert.ToUInt16(fptr.errorCode());
                        MainStaticClass.write_cdn_log("PrintingUsingLibraries check_marking_code LIBFPTR_PARAM_MARKING_CODE_VALIDATION_READY 2", num_doc, mark, "4");
                        break;
                    }
                }
            }
            if (validationError == 0)
            {
                validationError = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_ONLINE_VALIDATION_ERROR);
            }
            if ((validationError != 0) && (validationError != 402) && (validationError != 421))
            {
                result = false;
                string error_decription = "check_marking_code Код ошибки = " + validationError + ";\r\nОписание ошибки " + fptr.errorDescription() + ";\r\n" + fptr.getParamString(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_ONLINE_VALIDATION_ERROR_DESCRIPTION);
                MessageBox.Show(error_decription, "Проверка кода маркировки");
                MainStaticClass.write_event_in_log(error_decription, "Документ", num_doc);
                fptr.declineMarkingCode();
                //check_validation_error_422(validationError);
            }
            else
            {
                // Подтверждаем реализацию товара с указанным КМ
                uint validationResult = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_MARKING_CODE_ONLINE_VALIDATION_RESULT);
                cdn_markers_result_check[mark] = validationResult;
                fptr.acceptMarkingCode();
            }

            return result;
        }
    }
}
