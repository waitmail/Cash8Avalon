using Atol.Drivers10.Fptr;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AtolConstants = Atol.Drivers10.Fptr.Constants;


namespace Cash8Avalon
{

    class MainStaticClass
    {
        //public static Cash_check cc = null;
        //public static 

        //public static string url = "http://10.21.47.111:16732/requests";
        public static string url = "http://localhost:16732/requests";
        //public static string url = "http://127.0.0.1:16732/requests";
        //public static string url = "http://" + get_ip_adress() + ":16732/requests";
        //public static string url = "http://192.168.0.96:16732/requests";

        public static string shablon = "{uuid,\"request\": [body]}\"";

        private static bool fiscal_print;
        //private static int bonus_treshold = 0;
        //public static ListView listview_print;
        //public static double sum_print;

        private static byte[] EncryptedSymmetricKey = { 214, 46, 220, 83, 160, 73, 40, 39, 201, 155, 19, 202, 3, 11, 191, 178, 56, 74, 90, 36, 248, 103, 18, 144, 170, 163, 145, 87, 54, 61, 34, 220 };
        private static byte[] EncryptedSymmetricIV = { 207, 137, 149, 173, 14, 92, 120, 206, 222, 158, 28, 40, 24, 30, 16, 175 };
        private static string ipAdrServer = null;
        private static string dataBaseName = null;
        private static string portServer = null;
        private static string postgresUser = null;
        private static string passwordPostgres = null;
        static DESCryptoServiceProvider des = new DESCryptoServiceProvider();
        //private static string codebase = null;
        private static string codekey = null;
        //private static bool databaseIsCentral;
        private static Int16 cashDeskNumber = 0;
        private static Int16 code_right_of_user = 0;
        //private static Main main = null;
        private static string nick_shop = "";
        private static string code_shop = "";
        private static string cash_operator = "";
        //private static string cash_operator_nick = "";//Пока не используется Фио кассира 
        private static string cash_operator_inn { get; set; }

        private static string cash_operator_client_code = "";
        private static bool result_fiscal_print;

        private static DateTime last_answer_barcode_scaner;
        private static ArrayList forms = new ArrayList();

        //private static string pass_promo = "";
        //private static string login_promo = "";

        private static bool first_fogin_admin = false;

        //private static Int16 fiscal_num_port = -1;
        //private static string fiscal_type_port = "";
        private static string firma = "";
        private static string inn = "";
        //private static int use_trassir = -1;
        //private static string ip_addr_trassir = "";
        //private static int ip_port_trassir = -1;
        private static string path_for_web_service = "";
        //private static int show_before_payment_window = -1;
        //private static int start_sum_opt_price = -1;
        //private static bool use_envd = false;
        private static int system_taxation = 0;
        private static DateTime last_send_last_successful_sending;
        private static DateTime last_write_check;
        private static DateTime min_date_work = new DateTime(2023, 09, 01);
        private static DateTime min_date_work_logs = new DateTime(2025, 10, 01);
        //private static bool use_old_processiing_actions = true;
        //private static int work_schema = 0;
        private static int version_fn = 0;
        private static string version_fn_real = "";
        private static string fn_serial_port = "";
        private static int variant_connect_fn = -1;

        //private static bool use_text_print;
        //private static int width_of_symbols;
        //private static string barcode = "";
        //private static bool use_usb_to_com_barcode_scaner;

        //public enum TypeAction {ip,poi};
        //{
        //enum Types_of_actions { }
        //}

        //private static string start_url = "http://92.242.41.218/processing/v3";

        private static string barcode = "";

        public static bool continue_to_read_the_data_from_a_port = false;
        //private static int enable_stock_processing_in_memory=-1;
        //private static int self_service_kiosk = -1;
        private static string id_acquirer_terminal = "00000000";
        private static string ip_address_acquiring_terminal = "000000000000000";
        //private static int enable_cdn_markers = -1;
        private static int version2_marking = -1;
        private static int authorization_required = -1;
        private static int static_guid_in_print = -1;
        private static int printing_using_libraries = -1;
        private static IFptr _fptr = null;
        private static string cdn_token = "";
        private static int this_new_database = 0;
        private static CDN.CDN_List CDN_list = null;
        private static string fiscal_drive_number = "";//номер фискального регистратора 
        private static int get_weight_automatically = -1;
        private static string scale_serial_port = "";
        private static string fn_ipaddr = "";
        private static int acquiring_bank = -1;
        //private static int do_not_prompt_marking_code = -1;
        private static int nds_ip = -1;
        private static bool fiscals_forbidden = true;
        private static string ip_addr_lm_ch_z = "0";
        private static string kitchen_print = "0";
        private static int included_piot = -1;

        //private static Dictionary<int, Cash8.ProductData> dictionaryProductData = new Dictionary<int, Cash8.ProductData>();


        private static readonly Random random = new Random();

        public static string Generate10DigitNumber()
        {
            // Генерируем 10 случайных цифр
            string digits = new string(Enumerable.Repeat("0123456789", 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return digits;
        }

        //public static Cash8.PIOT.PIOTInfo PiotInfo { get; set; }


        //public static Dictionary DictionaryProductData
        //{
        //    get
        //    {

        //    }

        //}

        private static readonly Lazy<bool> _includedPiotLazy = new Lazy<bool>(() =>
        {
            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    string query = "SELECT include_piot FROM constants LIMIT 1";
                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        var result = command.ExecuteScalar();
                        return result != null && result != DBNull.Value
                            ? Convert.ToBoolean(result)
                            : false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении include_piot: {ex.Message}");
                return false; // значение по умолчанию
            }
        });

        public static Window? MainWindow { get; set; }

        public static void SetMainWindow(Window window)
        {
            MainWindow = window;
        }

        //public static void Show(string message, string title = "Сообщение")
        //{
        //    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        //    {
        //        try
        //        {
        //            var mainWindow = MainStaticClass.MainWindow;

        //            if (mainWindow != null && mainWindow.IsVisible)
        //            {
        //                // ИГНОРИРУЕМ РЕЗУЛЬТАТ с помощью discard (_)
        //                _ = await Avalonia.Controls.MessageBox.Show(mainWindow, message, title);
        //            }
        //            else
        //            {
        //                await ShowFallbackDialog(message, title);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // Если что-то пошло не так, показываем в консоли
        //            Console.WriteLine($"[{title}] {message}");
        //            Console.WriteLine($"Ошибка при показе сообщения: {ex.Message}");
        //        }
        //    });
        //}

        public static async Task<string> GetAtolDriverVersion()
        {
            try
            {
                // Получаем путь к исполняемому файлу
                string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string directory = Path.GetDirectoryName(executablePath);

                // Формируем полный путь к библиотеке
                string dllPath = Path.Combine(directory, "Atol.Drivers10.Fptr.dll");

                // Загружаем сборку и получаем версию
                var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                return assembly.GetName().Version.ToString();
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка при получении версии: {ex.Message}",
                                          "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);                

                return "Версия не определена";
            }
        }

        private static async Task ShowFallbackDialog(string message, string title)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                ShowInTaskbar = true
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10
            };

            stackPanel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                MaxWidth = 350
            });

            var button = new Button
            {
                Content = "OK",
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var tcs = new TaskCompletionSource();
            button.Click += (s, e) =>
            {
                window.Close();
                tcs.SetResult();
            };

            stackPanel.Children.Add(button);
            window.Content = stackPanel;

            window.Show();
            await tcs.Task;
        }

        //public static string IncludedPiot => _includedPiotLazy.Value ? "1" : "0";
        //public static bool IncludedPiot => _includedPiotLazy.Value;

        ///// <summary>
        ///// Возвращает ip адресс лм чз
        ///// в локальной сети магазина
        ///// </summary>
        //public static string IncludedPiot
        //{
        //    get
        //    {
        //        if (included_piot == -1)
        //        {
        //            NpgsqlConnection conn = null;
        //            NpgsqlCommand command = null;
        //            conn = MainStaticClass.NpgsqlConn();
        //            try
        //            {
        //                conn.Open();
        //                string query = "SELECT include_piot FROM constants";
        //                command = new NpgsqlCommand(query, conn);
        //                if (Convert.ToBoolean(command.ExecuteScalar()))
        //                {
        //                    included_piot = 1;
        //                }
        //                else
        //                {
        //                    included_piot = 0;
        //                }
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                MessageBox.Show("Ошибка при чтении include_piot" + ex.ToString());
        //                kitchen_print = "";
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show("Ошибка при чтении include_piot" + ex.ToString());
        //                kitchen_print = "";
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }
        //        }

        //        return included_piot.ToString();
        //    }
        //}


        /// <summary>
        /// Возвращает ip адресс лм чз
        /// в локальной сети магазина
        /// </summary>
        public static string GetKithenPrint
        {
            get
            {
                if (kitchen_print == "0")
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT kitchen_print FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        kitchen_print = command.ExecuteScalar().ToString();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении kitchen_print" + ex.ToString());
                        kitchen_print = "";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении kitchen_print" + ex.ToString());
                        kitchen_print = "";
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }

                return kitchen_print;
            }
        }


        /// <summary>
        /// Возвращает ip адресс лм чз
        /// в локальной сети магазина
        /// </summary>
        public static string GetIpAddrLmChZ
        {
            get
            {
                if (ip_addr_lm_ch_z == "0")
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT ip_adress_local_ch_z FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        ip_addr_lm_ch_z = command.ExecuteScalar().ToString();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении ip_adress_local_ch_z" + ex.ToString());
                        ip_addr_lm_ch_z = "";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении ip_adress_local_ch_z" + ex.ToString());
                        ip_addr_lm_ch_z = "";
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }

                return ip_addr_lm_ch_z;
            }
        }


        /// <summary>
        /// Возвращает истина если печать 
        /// на фискальном регистраторе запрещена
        /// </summary>
        public static bool GetFiscalsForbidden
        {
            get
            {
                if (nds_ip == -1)
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT fiscals_forbidden FROM users where code='" + MainStaticClass.CashOperatorInn + "'";
                        command = new NpgsqlCommand(query, conn);
                        fiscals_forbidden = Convert.ToBoolean(command.ExecuteScalar());
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении fiscals_forbidden" + ex.ToString());
                        fiscals_forbidden = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении fiscals_forbidden" + ex.ToString());
                        fiscals_forbidden = true;
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return fiscals_forbidden;
            }
        }



        public static void validate_date_time_with_fn(int minutes)
        {
            if (MainStaticClass.CashDeskNumber != 9)
            {
                //if (MainStaticClass.PrintingUsingLibraries == 1)
                //{
                PrintingUsingLibraries usingLibraries = new PrintingUsingLibraries();
                usingLibraries.validate_date_time_with_fn(minutes);
                //    }
                //    else
                //    {

                //        try
                //        {
                //            Cash8.FiscallPrintJason2.RootObject result = FiscallPrintJason2.execute_operator_type("getDeviceStatus");
                //            if (result != null)
                //            {
                //                if (result.results[0].status == "ready")//Задание выполнено успешно 
                //                {
                //                    DateTime dateTime = Convert.ToDateTime(result.results[0].result.deviceStatus.currentDateTime);

                //                    if (Math.Abs((dateTime - DateTime.Now).Minutes) > minutes)//Поскольку может быть как больше так и меньше 
                //                    {
                //                        MessageBox.Show(" У ВАС ОТЛИЧАЕТСЯ ВРЕМЯ МЕЖДУ КОМПЬЮТЕРОМ И ФИСКАЛЬНЫМ РЕГИСТРАТОРОМ БОЛЬШЕ ЧЕМ НА "+ minutes.ToString()+" МИНУТ ОТПРАВЬТЕ ЗАЯВКУ В ИТ ОТДЕЛ ", "Проверка даты и времени");
                //                        MainStaticClass.write_event_in_log(" Не схождение даты и времени между ФР и компьютером больше чем на  " + minutes.ToString() +" минут ", "Документ", "0");
                //                    }
                //                }
                //                else
                //                {
                //                    MessageBox.Show(" Ошибка !!! " + result.results[0].status + " | " + result.results[0].errorDescription);
                //                }
                //            }
                //            else
                //            {
                //                MessageBox.Show("Общая ошибка");
                //            }
                //        }
                //        catch (Exception ex)
                //        {
                //            MessageBox.Show(" OnKeyDown " + ex.Message);
                //        }
                //    }
            }
        }

        public static int GetNdsIp
        {
            get
            {
                if (nds_ip == -1)
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT nds_ip FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        nds_ip = Convert.ToInt16(command.ExecuteScalar());
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении nds_ip" + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении nds_ip" + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return nds_ip;
            }
        }



        public static int GetAcquiringBank
        {
            get
            {
                if (acquiring_bank == -1)
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT acquiring_bank FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        acquiring_bank = Convert.ToInt16(command.ExecuteScalar());
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении fn_ipaddr" + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении fn_ipaddr" + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return acquiring_bank;
            }
        }

        ///// <summary>
        ///// Возвращает флажок 
        ///// запрашивать ли код маркировки        
        ///// </summary>
        //public static int GetDoNotPromptMarkingCode
        //{
        //    get
        //    {
        //        if (do_not_prompt_marking_code == -1)
        //        {
        //            NpgsqlConnection conn = null;
        //            NpgsqlCommand command = null;
        //            conn = MainStaticClass.NpgsqlConn();
        //            try
        //            {
        //                conn.Open();
        //                string query = "SELECT do_not_prompt_marking_code FROM constants";
        //                command = new NpgsqlCommand(query, conn);
        //                do_not_prompt_marking_code = Convert.ToInt16(command.ExecuteScalar());
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                MessageBox.Show("Ошибка при чтении do_not_prompt_marking_code" + ex.ToString());
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show("Ошибка при чтении do_not_prompt_marking_code" + ex.ToString());
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }
        //        }
        //        return do_not_prompt_marking_code;
        //    }
        //}

        public static string GetFnIpaddr
        {
            get
            {
                if (fn_ipaddr == "")
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT fn_ipaddr FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        fn_ipaddr = command.ExecuteScalar().ToString();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении fn_ipaddr " + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении fn_ipaddr " + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return fn_ipaddr;
            }

        }

        //public static bool fractional_exists(ListView listView1)
        //{
        //    bool fractional = false;
        //    double sum_of_the_document = 0;
        //    foreach (ListViewItem lvi in listView1.Items)
        //    {
        //        if (lvi.SubItems[3].Text.IndexOf(".") != -1)
        //        {
        //            string[] parts = lvi.SubItems[3].Text.ToString().Split('.');
        //            sum_of_the_document = Double.Parse(parts[1]);
        //        }
        //        else if (lvi.SubItems[3].Text.IndexOf(",") != -1)
        //        {
        //            string[] parts = lvi.SubItems[3].Text.ToString().Split(',');
        //            sum_of_the_document = Double.Parse(parts[1]);
        //        }
        //        if (Convert.ToDouble(sum_of_the_document) != 0)
        //        {
        //            fractional = true;
        //        }
        //        if (fractional)
        //        {
        //            break;
        //        }
        //    }
        //    return fractional;
        //}

        public static int GetVariantConnectFN
        {
            get
            {
                if (variant_connect_fn == -1)
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT variant_connect_fn FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        //variant_connect_fn = (Convert.ToBoolean(command.ExecuteScalar()) ? 1 : 0);
                        variant_connect_fn = Convert.ToInt16(command.ExecuteScalar());
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении variant_connect_fn" + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении variant_connect_fn" + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return variant_connect_fn;

            }

        }


        public static int GetWeightAutomatically
        {
            get
            {
                if (get_weight_automatically == -1)
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT get_weight_automatically FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        get_weight_automatically = (Convert.ToBoolean(command.ExecuteScalar()) ? 1 : 0);
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении get_weight_automatically" + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении get_weight_automatically" + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return get_weight_automatically;
            }
        }

        public static string ScaleSerialPort
        {
            get
            {
                if (scale_serial_port == "")
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT scale_serial_port FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        scale_serial_port = command.ExecuteScalar().ToString();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении scale_serial_port" + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении scale_serial_port" + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return scale_serial_port;
            }
        }


        private static CDN.CDN_List DeepCopyCDN_List(CDN.CDN_List original)
        {
            CDN.CDN_List copy = new CDN.CDN_List
            {
                code = original.code,
                description = original.description,
                createDateTime = original.createDateTime,
                hosts = original.hosts.Select(h => new CDN.Host
                {
                    host = h.host,
                    avgTimeMs = h.avgTimeMs,
                    latensy = h.latensy,
                    dateTime = h.dateTime
                }).ToList()
            };
            return copy;
        }

        public static CDN.CDN_List CDN_List
        {
            get
            {
                if (CDN_list == null)
                {
                    CDN cdn = new CDN();
                    CDN_list = cdn.get_cdn_list();
                    //обновить кеш                     
                    //update_cash_cdn(CDN_list);
                }

                //return DeepCopyCDN_List(CDN_list);//Здесь отдаем копию там дальше будут отборы, а сохранять нужно весь оргинальный список 
                return CDN_list;
            }
            set
            {
                CDN_list = null;//Если CDN сервера недоступны, то таким образом мы обнуляем весь список 
            }
        }


        // Если при проверке продукции через CDN-площадку 3 раза подряд не удаётся получить ответ на
        //запрос в течение 1.5 секунд, то необходимо пометить в своей информационной системе эту
        //площадку на 15 минут как недоступную и переключиться на следующую по приоритету в списке
        //CDN-площадку
        //public static void UpdateHostDateTimeCdnHost(string hostName, DateTime newDateTime)
        //{
        //    // Найти хост с указанным именем
        //    CDN.Host hostToUpdate = CDN_list.hosts.FirstOrDefault(h => h.host == hostName);

        //    // Если хост найден, обновить его dateTime
        //    if (hostToUpdate != null)
        //    {
        //        hostToUpdate.dateTime = newDateTime;
        //    }
        //    else
        //    {
        //        MessageBox.Show("При попытке присвоить новое значение dateTime в классе   CDN.Host произошло исключение  Host not found.");
        //    }
        //}

        //private static void update_cash_cdn(CDN.CDN_List cdn_list)
        //{
        //    using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
        //    {
        //        NpgsqlCommand command = null;

        //        try
        //        {
        //            conn.Open();

        //            foreach (CDN.Host host in cdn_list.hosts)
        //            {
        //                using (command = new NpgsqlCommand())
        //                {
        //                    command.Connection = conn;

        //                    // Обновление
        //                    command.CommandText = "UPDATE cdn_cash SET latensy = @latensy, date = @date WHERE host = @host";
        //                    command.Parameters.AddWithValue("@date", DateTime.Now);
        //                    command.Parameters.AddWithValue("@latensy", host.latensy);
        //                    command.Parameters.AddWithValue("@host", host.host);

        //                    int rowsaffected = command.ExecuteNonQuery();

        //                    // Вставка, если обновление не затронуло ни одной строки
        //                    if (rowsaffected == 0)
        //                    {
        //                        command.CommandText = "INSERT INTO cdn_cash(host, latensy, date) VALUES (@host, @latensy, @date)";
        //                        command.Parameters.Clear();
        //                        command.Parameters.AddWithValue("@date", DateTime.Now);
        //                        command.Parameters.AddWithValue("@latensy", host.latensy);
        //                        command.Parameters.AddWithValue("@host", host.host);

        //                        command.ExecuteNonQuery();
        //                    }
        //                }
        //            }
        //        }
        //        catch (NpgsqlException ex)
        //        {
        //            MessageBox.Show("Ошибка при обновлении кеша cdn update_cash_cdn: " + ex.Message);
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Ошибка при обновлении кеша cdn update_cash_cdn: " + ex.Message);
        //        }
        //        finally
        //        {
        //            if (conn.State == System.Data.ConnectionState.Open)
        //            {
        //                conn.Close();
        //            }
        //            if (command != null)
        //            {
        //                command.Dispose();
        //            }
        //        }
        //    }
        //}



        public static string check_fractional_tovar(string tovar_code)
        {
            string result = "piece";

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT fractional FROM tovar WHERE code=" + tovar_code;
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                if (Convert.ToBoolean(command.ExecuteScalar()))
                {
                    result = "kilogram";
                }
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при получении признака весовой " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении признака весовой " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }


        public static double GetWeight()
        {
            Dictionary<double, int> frequencyMap = new Dictionary<double, int>();
            double weigt = 0;
            int num = 0;
            while (num < 7)
            {
                num++;
                weigt = MainStaticClass.TryGetWeight();
                if (frequencyMap.ContainsKey(weigt))
                {
                    frequencyMap[weigt]++;
                }
                else
                {
                    frequencyMap[weigt] = 1;
                }
            }
            weigt = frequencyMap.Where(pair => pair.Key > 0) // Фильтруем, оставляя только числа больше нуля
                .OrderByDescending(pair => pair.Value) // Сортируем по убыванию частоты
                .FirstOrDefault().Key; // Берем первый элемент или значение по умолчанию, если таких нет

            return weigt;
        }

        private static double get_constant_conversion_to_kilograms()
        {
            double result = 0;

            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT constant_conversion_to_kilograms FROM constants";
                command = new NpgsqlCommand(query, conn);
                result = Convert.ToDouble(command.ExecuteScalar());
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при чтении constant_conversion_to_kilograms" + ex.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении constant_conversion_to_kilograms" + ex.ToString());
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }


            return result;
        }

        private static double TryGetWeight()
        {
            //error = false;
            double result = 0;
            //string portName = MainStaticClass.ScaleSerialPort;
            //int baudRate = 9600;

            //using (SerialPort serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One))
            //{
            //    try
            //    {
            //        Thread.Sleep(100);
            //        serialPort.Open();
            //        //Console.WriteLine("Порт открыт успешно.");

            //        byte[] data = { 0x02, 0x05, 0x3A, 0x30, 0x30, 0x33, 0x30, 0x3C }; // команда весам
            //        serialPort.Write(data, 0, data.Length); // отправляем команду весам

            //        serialPort.ReadTimeout = 1000; // ждем 1 секунду для получения ответа

            //        byte[] buffer = new byte[15];
            //        int bytesRead = serialPort.Read(buffer, 0, buffer.Length); // читаем ответ

            //        if (bytesRead == 15)
            //        {
            //            // используем BitConverter для выделения нужных байт из ответного сообщения
            //            int b = BitConverter.ToInt32(buffer, 7);
            //            //result = b / 10000.0; // Перевод в килограммы
            //            double constant_conversion_to_kilograms = get_constant_conversion_to_kilograms();
            //            if (constant_conversion_to_kilograms == 0)
            //            {
            //                result = b / 10000.0; // Перевод в килограммы
            //            }
            //            else
            //            {
            //                result = b / constant_conversion_to_kilograms; // Перевод в килограммы
            //            }
            //        }
            //        //else
            //        //{
            //        //    error = true;
            //        //}
            //    }
            //    catch (TimeoutException)
            //    {
            //        MessageBox.Show("Время ожидания истекло.");
            //        //    Console.WriteLine("Время ожидания истекло.");
            //        result = -1;
            //        //error = true;
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("Ошибка " + ex.Message);
            //        //Console.WriteLine($"Ошибка: {ex.Message}");
            //        result = -1;
            //        //error = true;
            //    }
            //    finally
            //    {
            //        if (serialPort.IsOpen)
            //        {
            //            serialPort.Close();
            //            //Console.WriteLine("Порт закрыт.");
            //        }
            //    }
            //}
            return result;
        }

        /// <summary>
        /// Возвращает false если нажатие 
        /// было очень быстрым
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool continue_process(DateTime dateTime, int second)
        {
            bool result = true;
            if (dateTime > DateTime.Now.AddDays(-1))
            {
                if ((DateTime.Now - dateTime).TotalSeconds < 1)
                {
                    //MessageBox.Show((DateTime.Now - dateTime).TotalSeconds.ToString());
                    MainStaticClass.write_event_in_log("Слишком частое нажатие", "", "0");
                    return false;
                }
            }
            return result;
        }

        public static string FiscalDriveNumber
        {
            get
            {
                if (fiscal_drive_number == "")
                {
                    if (MainStaticClass.PrintingUsingLibraries == 1)
                    {
                        IFptr fptr = MainStaticClass.FPTR;
                        if (!fptr.isOpened())
                        {
                            fptr.open();
                        }

                        fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_REG_INFO);
                        fptr.fnQueryData();

                        fiscal_drive_number = fptr.getParamString(1037);
                    }
                    //else
                    //{
                    //    try
                    //    {
                    //        Cash8.FiscallPrintJason.RootObject result = FiscallPrintJason.execute_operator_type("getRegistrationInfo");
                    //        if (result != null)
                    //        {
                    //            if (result.results[0].status == "ready")//Задание выполнено успешно 
                    //            {                                    
                    //                fiscal_drive_number = result.results[0].result.device.registrationNumber;
                    //            }
                    //            else
                    //            {
                    //                MessageBox.Show(" Ошибка при получении номера фискального регистратора FiscalDriveNumber" + result.results[0].status + " | " + result.results[0].errorDescription);
                    //            }
                    //        }
                    //        else
                    //        {
                    //            MessageBox.Show("Общая ошибка");
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        MessageBox.Show("Ошибка при получении номера фискального регистратора FiscalDriveNumber" + ex.Message);
                    //    }

                    //}
                }
                return fiscal_drive_number;
            }
        }

        public static string CDN_Token
        {
            get
            {
                if (cdn_token == "")
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT cdn_token FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        cdn_token = command.ExecuteScalar().ToString().Trim();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении cdn_token" + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении cdn_token" + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }

                }
                return cdn_token;
            }
        }

        public static string FnSerialPort
        {
            get
            {
                if (fn_serial_port == "")
                {
                    NpgsqlConnection conn = null;
                    NpgsqlCommand command = null;
                    conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT fn_serial_port FROM constants";
                        command = new NpgsqlCommand(query, conn);
                        fn_serial_port = command.ExecuteScalar().ToString();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении fn_serial_port" + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении fn_serial_port" + ex.ToString());
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }

                }
                return fn_serial_port;
            }
        }


        public static void its_print(string num_doc)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            NpgsqlTransaction trans = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                trans = conn.BeginTransaction();
                string query = " UPDATE checks_header   SET its_print=true WHERE document_number=" + num_doc.ToString() + ";" + "UPDATE checks_header SET is_sent = 0 WHERE document_number = " + num_doc.ToString();//date_time_start='" + date_time_start.Text.Replace("Чек", "") + "'"; ;
                command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                command.ExecuteNonQuery();

                query = " UPDATE checks_header   SET its_print_p=true WHERE document_number=" + num_doc.ToString() + ";" + "UPDATE checks_header SET is_sent = 0 WHERE document_number = " + num_doc.ToString(); //date_time_start='" + date_time_start.Text.Replace("Чек", "") + "'"; ;
                command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                command.ExecuteNonQuery();

                query = " DELETE FROM document_wil_be_printed WHERE document_number=" + num_doc.ToString() + " AND tax_type=" + MainStaticClass.SystemTaxation.ToString();
                command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                command.ExecuteNonQuery();
                trans.Commit();
                conn.Close();
                command.Dispose();
                trans.Dispose();
            }
            catch (NpgsqlException ex)
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
                MessageBox.Show("Ошибка при установке флага распечатан " + ex.Message);
            }
            catch (Exception ex)
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
                MessageBox.Show("Ошибка при установке флага распечатан " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }


        /// <summary>
        /// Получение сумм по типам оплаты
        /// 
        /// </summary>
        /// <returns></returns>
        public static double[] get_cash_on_type_payment(string numdoc)
        {
            double[] result = new double[3];
            result[0] = 0;
            result[1] = 0;
            result[2] = 0;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT cash_money, non_cash_money, sertificate_money  FROM checks_header WHERE document_number=" + numdoc;
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result[0] = Convert.ToDouble(reader.GetDecimal(0));
                    result[1] = Convert.ToDouble(reader.GetDecimal(1));
                    result[2] = Convert.ToDouble(reader.GetDecimal(2));
                }
                reader.Close();
                command.Dispose();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Произошли ошибки при получении сумм по типам оплаты" + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошли ошибки при получении сумм по типам оплаты" + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }


        /// <summary>
        /// проверка на подакцизный товар
        /// </summary>
        /// <param name="code_tovar"></param>
        /// <returns></returns>
        public static int its_excise(string code_tovar)
        {
            int result = 0;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT its_excise FROM tovar WHERE code=" + code_tovar;
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при чтении свойства подакцизный товар " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении свойства подакцизный товар " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }

            return result;
        }


        public static bool its_certificate(string code)
        {
            bool result = false;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = "SELECT COUNT(*) AS qty FROM sertificates where code_tovar = " + code;
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                {
                    result = true;
                }
                command.Dispose();
                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при проверке на сертификат " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при проверке на сертификат " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }


            return result;
        }
        public static int get_tovar_nds(string code)
        {
            int result = 0;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT nds  FROM tovar WHERE code=" + code;
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt32(command.ExecuteScalar());
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при получении значения ставки ндс " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении значения ставки ндс " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }


        //private static int version_fn = 0;
        //private static int sno = -1;//это система налогообложения

        //public static int SNO
        //{
        //    get
        //    {
        //        if (sno == -1)
        //        {
        //            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //            try
        //            {
        //                conn.Open();
        //                string query = "SELECT sno FROM constants";
        //                NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //                sno =Convert.ToInt16(command.ExecuteScalar());                        
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                sno = 0;
        //                MessageBox.Show(" Ошибка при чтении системы налогообложения " + ex.Message);
        //            }
        //            catch (Exception ex)
        //            {
        //                sno = 0;                        
        //                MessageBox.Show("Ошибка при чтении системы налогообложения" + ex.Message);
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }

        //        }
        //        return sno;
        //    }
        //}

        public static int ThisNewDatabase
        {
            get
            {
                return this_new_database;
            }
            set
            {
                this_new_database = value;
            }

        }

        public static string CashOperatorInn
        {
            get
            {
                return cash_operator_inn;
            }
            set
            {
                cash_operator_inn = value;
            }

        }

        public static void set_basic_auth(System.Net.WebRequest req)
        {
            if (AuthorizationRequired == 1)
            {
                string authInfo = "admin:AdminRetail0123456789";
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                req.Headers["Authorization"] = "Basic " + authInfo;
            }
        }

        /// <summary>
        /// Экземпляр объекта печати 
        /// </summary>
        public static IFptr FPTR
        {
            get
            {
                if (_fptr == null)
                {
                    //_fptr = new Fptr(Application.StartupPath+ "/fptr10.dll");
                    _fptr = new Fptr();
                    setConnectSetting(_fptr);
                    _fptr.open();
                }

                return _fptr;
            }
            //set
            //{
            //    _fptr = value;
            //}
        }

        public static int PrintingUsingLibraries
        {
            get
            {
                if (printing_using_libraries == -1)
                {

                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT printing_using_libraries FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(query, conn);
                        object result_query = command.ExecuteScalar();
                        if (Convert.ToBoolean(result_query) == false)
                        {
                            printing_using_libraries = 0;
                        }
                        else
                        {
                            printing_using_libraries = 1;
                        }
                    }
                    catch (NpgsqlException ex)
                    {
                        printing_using_libraries = 0;
                        MessageBox.Show(" Ошибка при чтении флага печати с помощью библиотек " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        printing_using_libraries = 0;
                        MessageBox.Show(" Ошибка при чтении флага печати с помощью библиотек " + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return printing_using_libraries;
            }
        }

        public static int AuthorizationRequired
        {
            get
            {
                if (authorization_required == -1)
                {

                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT webservice_authorize FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(query, conn);
                        object result_query = command.ExecuteScalar();
                        if (Convert.ToBoolean(result_query) == false)
                        {
                            authorization_required = 0;
                        }
                        else
                        {
                            authorization_required = 1;
                        }
                    }
                    catch (NpgsqlException ex)
                    {
                        authorization_required = 0;
                        MessageBox.Show(" Ошибка при чтении флага по работе веб сервиса с авторизацией " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        authorization_required = 0;
                        MessageBox.Show(" Ошибка при чтении флага по работе веб сервиса с авторизацией " + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return authorization_required;
            }
        }

        public async static Task<bool> exist_table_name(string table_name)
        {
            bool exists = true;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            int conn_open = 0;
            try
            {
                conn.Open();
                conn_open = 1;
                string query = "select case when exists((select * from information_schema.tables where table_name = '" + table_name + "')) then 1 else 0 end";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                exists = (int)command.ExecuteScalar() == 1;
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show("Ошибка при чтении наличия таблицы в текущей бд " + ex.Message);
                if (conn_open == 1)
                {
                    exists = false;
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Ошибка при чтении наличия таблицы в текущей бд " + ex.Message);
                if (conn_open == 1)
                {
                    exists = false;
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            return exists;
        }

        public static int Version2Marking
        {
            get
            {
                if (version2_marking == -1)
                {

                    //NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    //try
                    //{
                    //    conn.Open();
                    //    string query = "SELECT version2_marking FROM constants";
                    //    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                    //    object result_query = command.ExecuteScalar();
                    //    if (Convert.ToBoolean(result_query) == false)
                    //    {
                    //        version2_marking = 0;
                    //    }
                    //    else
                    //    {
                    //        version2_marking = 1;
                    //    }
                    //}
                    //catch (NpgsqlException ex)
                    //{
                    //    version2_marking = 0;
                    //    MessageBox.Show("Ошибка при чтении флага по работе с маркировкой по 2 схеме" + ex.Message);
                    //}
                    //catch (Exception ex)
                    //{
                    //    version2_marking = 0;
                    //    MessageBox.Show("Ошибка при чтении флага по работе с маркировкой по 2 схеме" + ex.Message);
                    //}
                    //finally
                    //{
                    //    if (conn.State == ConnectionState.Open)
                    //    {
                    //        conn.Close();
                    //    }
                    //}
                    version2_marking = 1;
                }
                return version2_marking;
            }
            set
            {
                version2_marking = value;
            }
        }

        public static int StaticGuidInPrint
        {
            get
            {
                if (static_guid_in_print == -1)
                {

                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT static_guid_in_print FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(query, conn);
                        object result_query = command.ExecuteScalar();
                        if (Convert.ToBoolean(result_query) == false)
                        {
                            static_guid_in_print = 0;
                        }
                        else
                        {
                            static_guid_in_print = 1;
                        }
                    }
                    catch (NpgsqlException ex)
                    {
                        static_guid_in_print = 0;
                        MessageBox.Show("Ошибка при чтении флага по печати старая/новая схема " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        static_guid_in_print = 0;
                        MessageBox.Show("Ошибка при чтении флага по печати старая/новая схема " + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return static_guid_in_print;
            }
        }


        //public static int EnableCdnMarkers
        //{
        //    get
        //    {
        //        if (enable_cdn_markers == -1)
        //        {

        //            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //            try
        //            {
        //                conn.Open();
        //                string query = "SELECT enable_cdn_markers FROM constants";
        //                NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //                object result_query = command.ExecuteScalar();
        //                if (Convert.ToBoolean(result_query) == false)
        //                {
        //                    enable_cdn_markers = 0;
        //                }
        //                else
        //                {
        //                    enable_cdn_markers = 1;
        //                }
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                enable_cdn_markers = 0;
        //                MessageBox.Show("Ошибка при чтении флага о том что разрешена работа с CDN серверами " + ex.Message);
        //            }
        //            catch (Exception ex)
        //            {
        //                enable_cdn_markers = 0;
        //                MessageBox.Show("Ошибка при чтении флага о том что разрешена работа с CDN серверами " + ex.Message);
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }
        //        }                
        //        return enable_cdn_markers;
        //    }
        //    set
        //    {
        //        enable_cdn_markers = value;
        //    }
        //}


        /// <summary>
        /// Возвращает ип адрес эквайриного
        /// терминала если такой установлен
        /// </summary>
        public static string IpAddressAcquiringTerminal
        {
            get
            {
                if (ip_address_acquiring_terminal == "000000000000000")
                {
                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT ip_address_acquiring_terminal FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(query, conn);
                        ip_address_acquiring_terminal = command.ExecuteScalar().ToString();
                    }
                    catch (NpgsqlException ex)
                    {
                        ip_address_acquiring_terminal = "";
                        MessageBox.Show("Ошибка при чтении ид терминала эквайринга" + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        ip_address_acquiring_terminal = "";
                        MessageBox.Show("Ошибка при чтении ид терминала эквайринга" + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }

                }
                return ip_address_acquiring_terminal;
            }
        }




        /// <summary>
        /// Возвращает ид эквайриного
        /// терминала если он указан
        /// в константах
        /// </summary>
        public static string IdAcquirerTerminal
        {
            get
            {
                if (id_acquirer_terminal == "00000000")
                {
                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT id_acquirer_terminal FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(query, conn);
                        id_acquirer_terminal = command.ExecuteScalar().ToString();
                    }
                    catch (NpgsqlException ex)
                    {
                        id_acquirer_terminal = "";
                        MessageBox.Show("Ошибка при чтении ид терминала эквайринга" + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        id_acquirer_terminal = "";
                        MessageBox.Show("Ошибка при чтении ид терминала эквайринга" + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }

                }

                return id_acquirer_terminal;
            }

        }




        ///// <summary>
        ///// Возвращает флаг 1 если это киоск
        ///// саммобслуживания иначе 0
        ///// </summary>
        //public static int SelfServiceKiosk
        //{
        //    get
        //    {
        //        if (self_service_kiosk == -1)
        //        {
        //            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //            try
        //            {
        //                conn.Open();
        //                string query = "SELECT self_service_kiosk FROM constants";
        //                NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //                object result_query = command.ExecuteScalar();
        //                if (Convert.ToBoolean(result_query) == false)
        //                {
        //                    self_service_kiosk = 0;
        //                }
        //                else
        //                {
        //                    self_service_kiosk = 1;
        //                }
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                self_service_kiosk = 0;
        //                MessageBox.Show("Ошибка при чтении флага это киоск самообслуживания" + ex.Message);
        //            }
        //            catch (Exception ex)
        //            {
        //                self_service_kiosk = 0;
        //                MessageBox.Show("Ошибка при чтении флага это киоск самообслуживания" + ex.Message);
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }
        //        }                
        //            return self_service_kiosk;
        //    }
        //}




        ///// <summary>
        ///// Флаг возвращает истина если 
        ///// действует старый алгоритм по обработке акций 
        ///// и ложь если уже включен новый 
        ///// алгоритм с использованием DataTable
        ///// </summary>
        //public static int EnableStockProcessingInMemory
        //{
        //    get
        //    {
        //        if (enable_stock_processing_in_memory == -1)
        //        {
        //            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //            try
        //            {
        //                conn.Open();
        //                string query = "SELECT enable_stock_processing_in_memory FROM constants";
        //                NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //                object result_query = command.ExecuteScalar();
        //                if (Convert.ToBoolean(result_query) == false)
        //                {
        //                    enable_stock_processing_in_memory = 0;
        //                }
        //                else
        //                {
        //                    enable_stock_processing_in_memory = 1;
        //                }
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                enable_stock_processing_in_memory = 0;
        //                MessageBox.Show("Ошибка при чтении версии обработки акций" + ex.Message);
        //            }
        //            catch (Exception ex)
        //            {
        //                enable_stock_processing_in_memory = 0;
        //                MessageBox.Show("Ошибка при чтении версии обработки акций" + ex.Message);
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }
        //        }

        //        return enable_stock_processing_in_memory;
        //    }
        //}



        public static int GetVersionFn
        {
            get
            {
                if (version_fn == 0)
                {
                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string query = "SELECT version_fn	FROM public.constants";
                        NpgsqlCommand command = new NpgsqlCommand(query, conn);
                        version_fn = Convert.ToInt16(command.ExecuteScalar());
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при чтении версии протокола" + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении версии протокола" + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }

                return version_fn;
            }
        }


        //public static string GetAuthStringProcessing
        //{
        //    get
        //    {
        //        string result = "";
        //        if (MainStaticClass.GetWorkSchema == 1)
        //        {
        //            string shop_request = "";
        //            if (MainStaticClass.Nick_Shop.Substring(0, 1).ToUpper() == "A")
        //            {
        //                shop_request = MainStaticClass.Nick_Shop + MainStaticClass.CashDeskNumber;
        //            }
        //            else
        //            {
        //                shop_request = "1" + Convert.ToInt16(MainStaticClass.Nick_Shop.Substring(1, 2)).ToString() + MainStaticClass.CashDeskNumber;
        //            }

        //            result = Convert.ToBase64String(Encoding.Default.GetBytes(shop_request + ":" + MainStaticClass.PassPromo));
        //        }
        //        else if (MainStaticClass.GetWorkSchema == 2)
        //        {
        //            result = Convert.ToBase64String(Encoding.Default.GetBytes(MainStaticClass.LoginPromo + ":" + MainStaticClass.PassPromo));
        //        }
        //        return result;
        //    }
        //}


        /// <summary>
        /// Возвращает начальный адрес 
        /// процессингового центра
        /// </summary>
        public static string GetStartUrl
        {
            get
            {
                string result = "";
                if (MainStaticClass.GetWorkSchema == 1)
                {
                    result = "http://92.242.41.218/processing/v3";
                }
                //else if (MainStaticClass.GetWorkSchema == 2)
                //{
                //    //это боевой процессинг "https://evaviza1.cardnonstop.com/processing";
                //    result = "https://evaviza1.cardnonstop.com/processing";
                //    //result = "https://evaviza1.cardnonstop.com/test";//"http://5.188.118.39/test";
                //}

                return result;
            }
        }

        /// <summary>
        /// Флаг возвращает истина если 
        /// действует старый алгоритм по обработке акций 
        /// и ложь если уже включен новый 
        /// алгоритм с использованием DataTable
        /// </summary>
        //public static bool UseOldProcessiingActions
        //{
        //    get
        //    {
        //        return use_old_processiing_actions;
        //    }
        //    set
        //    {
        //        use_old_processiing_actions = value;
        //    }
        //}

        public static int GetWorkSchema
        {
            get
            {
                //if (work_schema == 0)
                //{
                //    work_schema = get_work_schema();
                //}                
                //return work_schema;
                return 1;
            }
        }

        private static int get_work_schema()
        {
            int result = 0;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT  work_schema	FROM public.constants";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar().ToString());
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при получении схемы работы программы " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении схемы работы программы" + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }

        //private static int BonusTreshold
        //{
        //    get
        //    {
        //        if (bonus_treshold == 0)
        //        {
        //            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //            try
        //            {
        //                conn.Open();
        //                string query = "SELECT threshold  FROM constants";
        //                NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //                bonus_treshold = Convert.ToInt32(command.ExecuteScalar());
        //                conn.Close();
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                MessageBox.Show(ex.Message);
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show(ex.Message);
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }
        //        }
        //        return bonus_treshold;
        //    }
        //}

        public static bool validate_cash_sum_non_cash_sum_on_return(string id_sale, Double cash_summ, Double non_cash_sum)
        {
            bool result = true;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                //string query = " SELECT SUM(d_c.cash_money)AS cash_money,SUM(d_c.non_cash_money)AS non_cash_money FROM" +
                //              " (SELECT cash_money, non_cash_money FROM checks_header where guid = '"+ id_sale.ToString()+"'"+
                //              "  AND checks_header.check_type = 0 AND checks_header.its_deleted = 0 "+
                //              " AND checks_header.date_time_write BETWEEN '" + 
                //              DateTime.Now.AddDays(-14).Date.ToString("dd-MM-yyyy") + "' AND  '" + DateTime.Now.AddDays(1).ToString("dd-MM-yyyy") + "'" +
                //              " UNION ALL " +
                //              " SELECT - coalesce(SUM(cash_money),0), - coalesce(SUM(non_cash_money),0) FROM checks_header where id_sale = '" + id_sale.ToString()+"'"+
                //              " AND checks_header.check_type = 1 AND checks_header.its_deleted = 0 "+
                //              " AND checks_header.date_time_write BETWEEN '" + 
                //              DateTime.Now.AddDays(-14).Date.ToString("dd-MM-yyyy") + "' AND  '" + DateTime.Now.AddDays(1).ToString("dd-MM-yyyy") + "') AS d_c ";//--delta_calculations

                string query = " SELECT SUM(d_c.cash_money)AS cash_money,SUM(d_c.non_cash_money)AS non_cash_money FROM" +
                              " (SELECT (cash_money+sertificate_money) AS cash_money, non_cash_money FROM checks_header where guid = '" + id_sale.ToString() + "'" +
                              "  AND checks_header.check_type = 0 AND checks_header.its_deleted = 0 " +
                              " AND checks_header.date_time_write BETWEEN '" +
                              DateTime.Now.AddDays(-14).Date.ToString("dd-MM-yyyy") + "' AND  '" + DateTime.Now.AddDays(1).ToString("dd-MM-yyyy") + "'" +
                              " UNION ALL " +
                              " SELECT - coalesce(SUM(cash_money),0), - coalesce(SUM(non_cash_money),0) FROM checks_header where id_sale = '" + id_sale.ToString() + "'" +
                              " AND checks_header.check_type = 1 AND checks_header.its_deleted = 0 " +
                              " AND checks_header.date_time_write BETWEEN '" +
                              DateTime.Now.AddDays(-14).Date.ToString("dd-MM-yyyy") + "' AND  '" + DateTime.Now.AddDays(1).ToString("dd-MM-yyyy") + "') AS d_c ";//--delta_calculations


                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (Convert.ToDouble(reader["cash_money"]) < cash_summ)
                    {
                        result = false;
                        MessageBox.Show("Вы можете вернуть наличными не более " + Convert.ToDouble(reader["cash_money"]).ToString());
                    }
                    if (Convert.ToDouble(reader["non_cash_money"]) < non_cash_sum)
                    {
                        result = false;
                        MessageBox.Show("Вы можете вернуть по безналу не более " + Convert.ToDouble(reader["non_cash_money"]).ToString());
                        MessageBox.Show("Преобразованное левая часть " + Convert.ToDouble(reader["non_cash_money"]).ToString());
                        MessageBox.Show("Правая часть  " + non_cash_sum.ToString());
                    }
                }

            }
            catch (NpgsqlException ex)
            {
                result = false;
                MessageBox.Show("Ошибка при определении корректности суммы возврата по видам оплаты " + ex.Message);
            }
            catch (Exception ex)
            {
                result = false;
                MessageBox.Show("Ошибка при определении корректности суммы возврата по видам оплаты " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }


        public static DateTime GetMinDateWork
        {
            get
            {
                return min_date_work;
            }
        }

        public static DateTime GetMinDateWorkLogs
        {
            get
            {
                return min_date_work_logs;
            }
        }

        //public static bool check_amount_exceeds_threshold(Decimal check_amount)
        //{
        //    bool result = false;

        //    if (MainStaticClass.BonusTreshold > 0)
        //    {
        //        if (check_amount >= MainStaticClass.BonusTreshold)
        //        {
        //            result = true;
        //        }
        //    }

        //    return result;
        //}

        public static int ckeck_failed_input_phone_on_client(string client_code)
        {
            int result = 0;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM failed_input_phone where client_code='" + client_code + "'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при определении количества попыток ввода неправильного номера телефона");
                result = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при определении количества попыток ввода неправильного номера телефона");
                result = -1;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }


        public static DateTime Last_Write_Check
        {
            get
            {
                return last_write_check;
            }
            set
            {
                last_write_check = value;
            }
        }

        public static DateTime Last_Send_Last_Successful_Sending
        {
            get
            {
                return last_send_last_successful_sending;
            }
            set
            {
                last_send_last_successful_sending = value;
            }
        }

        public static bool get_exists_internet()
        {
            //bool pingable = false;
            //Ping pinger = new Ping();//под wine не работает
            //try
            //{
            //    PingReply reply = pinger.Send("8.8.8.8");
            //    pingable = reply.Status == IPStatus.Success;
            //}
            //catch (PingException)
            //{

            //}

            //return pingable;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://ya.ru/");
                request.Timeout = 5000; // Таймаут в миллисекундах (5 секунд)
                request.ReadWriteTimeout = 5000; // Таймаут на чтение/запись

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }



        //public static Cash8.FiscallPrintJason.RootObject get_ofd_exchange_status()
        //{

        //    Cash8.FiscallPrintJason.RootObject result = null;

        //    try
        //    {
        //        result = FiscallPrintJason.execute_operator_type("ofdExchangeStatus");
        //        //if (result != null)
        //        //{
        //        //    if (result.results[0].status == "ready")//Задание выполнено успешно 
        //        //    {
        //        //        //string s = result.results[0].result.status.notSentFirstDocDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        //        //        //result.results[0].result.status
        //        //        //Invoke(new set_message_on_ofd_exchange_status(set_txtB_ofd_exchange_status), new object[] { s });
        //        //    }
        //        //    else
        //        //    {
        //        //        //string s = result.results[0].status + " | " + result.results[0].errorDescription;
        //        //        //Invoke(new set_message_on_ofd_exchange_status(set_txtB_ofd_exchange_status), new object[] { s });
        //        //    }
        //        //}
        //        //else
        //        //{
        //        //    //Invoke(new set_message_on_ofd_exchange_status(set_txtB_ofd_exchange_status), new object[] { "Общая ошибка" });
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }

        //    return result;
        //}



        //public static string get_ip_adress()
        //{
        //    // Получение имени компьютера.
        //    String host = System.Net.Dns.GetHostName();
        //    // Получение ip-адреса.
        //    System.Net.IPAddress ip = System.Net.Dns.GetHostByName(host).AddressList[0];
        //    return ip.ToString();
        //}


        //public static void delete_old_checks(DateTime date)
        //{
        //    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //    try
        //    {
        //        conn.Open();
        //        string query = "SELECT Max(document_number)  FROM checks_header where date_time_start<'" + date.ToString("yyyy.MM.dd") + "'";
        //        NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //        object result_query = command.ExecuteScalar();
        //        if (result_query.ToString() != "")
        //        {
        //            query = "DELETE FROM checks_header where document_number<" + Convert.ToInt64(result_query).ToString()+ " AND is_sent=1";
        //            command = new NpgsqlCommand(query, conn);
        //            command.ExecuteNonQuery();
        //            //query = "DELETE FROM checks_table LEFT JOIN checks_header ON checks_table.document_number = checks_header.document_number  where document_number<=" + Convert.ToInt64(result_query).ToString()+ " AND is_sent = 1";
        //            //query = "DELETE FROM checks_table ct  USING checks_header ch Where ct.document_number = ch.document_number  AND ct.document_number <=" + Convert.ToInt64(result_query).ToString() + " AND ch.is_sent = 1";
        //            query = "DELETE FROM checks_table Where document_number <" + Convert.ToInt64(result_query).ToString();
        //            command = new NpgsqlCommand(query, conn);
        //            command.ExecuteNonQuery();
        //        }
        //        command.Dispose();
        //        conn.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(" Ошибка при удалении документов с датой до " + date.ToString("yyyy.MM.dd") + " " + ex.Message);
        //    }
        //    finally
        //    {
        //        if (conn.State == ConnectionState.Open)
        //        {
        //            conn.Close();
        //        }
        //    }
        //}

        public static void delete_old_checks(DateTime date)
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlTransaction transaction = null;
            try
            {
                conn.Open();
                transaction = conn.BeginTransaction();
                string query = "DELETE FROM checks_table WHERE guid in(SELECT guid FROM checks_header where date_time_write < '" + date.ToString("yyyy.MM.dd") + "' AND is_sent=1)";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                command.Transaction = transaction;
                command.ExecuteNonQuery();
                query = "DELETE FROM checks_header where date_time_write < '" + date.ToString("yyyy.MM.dd") + "' AND is_sent=1";
                command = new NpgsqlCommand(query, conn);
                command.Transaction = transaction;
                command.ExecuteNonQuery();
                transaction.Commit();
                command.Dispose();
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Ошибка при удалении документов с датой до " + date.ToString("yyyy.MM.dd") + " " + ex.Message);
                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        static object GetErrorInfo(Exception ex)
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrame(0); // Получаем первый кадр стека

            if (frame == null)
                return new { Error = "Не удалось получить информацию о стеке вызовов." };

            // Получаем метод, в котором произошло исключение
            var method = frame.GetMethod();

            // Формируем объект с информацией об ошибке
            return new
            {
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                FileName = frame.GetFileName(),
                LineNumber = frame.GetFileLineNumber(),
                MethodName = method?.Name,
            };
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="methodName"></param>
        /// <param name="numDoc"></param>
        /// <param name="cashDeskNumber"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static void WriteRecordErrorLog(
          //string errorMessage,
          Exception exception,
         long numDoc,
         short cashDeskNumber,
         string description)
        {

            string errorMessage = JsonConvert.SerializeObject(GetErrorInfo(exception), Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            // Валидация числовых параметров
            if (numDoc < 0)
                throw new ArgumentOutOfRangeException(nameof(numDoc), "Номер документа должен быть положительным числом");

            if (cashDeskNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(cashDeskNumber), "Номер кассы должен быть положительным числом");
            string methodName = "methodName";
            // Обработка строковых параметров
            string truncatedErrorMessage = errorMessage.Trim();
            string truncatedMethodName = TruncateString(methodName, 255);
            string truncatedDescription = TruncateString(description, 255);

            const string sql = @"
        INSERT INTO errors_log(
            error_message,
            date_time_record,
            method_name,
            num_doc,            
            description
        )
        VALUES(
            @errorMessage,
            @dateTimeRecord,
            @methodName,
            @numDoc,            
            @description
        )";

            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    using (var command = new NpgsqlCommand(sql, conn))
                    {
                        command.Parameters.Add("@errorMessage", NpgsqlTypes.NpgsqlDbType.Text).Value = (object)truncatedErrorMessage ?? DBNull.Value;
                        command.Parameters.Add("@dateTimeRecord", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now;
                        command.Parameters.Add("@methodName", NpgsqlTypes.NpgsqlDbType.Text).Value = (object)truncatedMethodName ?? DBNull.Value;
                        command.Parameters.Add("@numDoc", NpgsqlTypes.NpgsqlDbType.Bigint).Value = numDoc;
                        command.Parameters.Add("@cashDeskNumber", NpgsqlTypes.NpgsqlDbType.Smallint).Value = cashDeskNumber;
                        command.Parameters.Add("@description", NpgsqlTypes.NpgsqlDbType.Text).Value = (object)truncatedDescription ?? DBNull.Value;

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем оригинальные значения (до обрезки)
                var logMessage = new StringBuilder()
                    .AppendLine($"Дата: {DateTime.Now}")
                    .AppendLine($"Ошибка: {ex.Message}")
                    .AppendLine($"StackTrace: {ex.StackTrace}")
                    .AppendLine("Параметры:")
                    .AppendLine($"- Сообщение: {exception.Message}")
                    .AppendLine($"- Метод: {methodName}")
                    .AppendLine($"- Документ: {numDoc}")
                    .AppendLine($"- Касса: {cashDeskNumber}")
                    .AppendLine($"- Описание: {description}")
                    .AppendLine(new string('-', 50))
                    .ToString();
                                

                // Получаем путь к исполняемому файлу
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "ErrorsLog.txt");

                try
                {
                    File.AppendAllText(logPath, logMessage);
                }
                catch (Exception fileEx)
                {
                    MessageBox.Show($"Ошибка записи в лог: {fileEx.Message}");

                }
            }
        }

        public static void WriteRecordErrorLog(
      string errorMessage,
     string methodName,
     long numDoc,
     short cashDeskNumber,
     string description)
        {
            // Валидация числовых параметров
            if (numDoc < 0)
                throw new ArgumentOutOfRangeException(nameof(numDoc), "Номер документа должен быть положительным числом");

            if (cashDeskNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(cashDeskNumber), "Номер кассы должен быть положительным числом");

            // Обработка строковых параметров
            string truncatedErrorMessage = TruncateString(errorMessage, 255);
            string truncatedMethodName = TruncateString(methodName, 255);
            string truncatedDescription = TruncateString(description, 255);

            const string sql = @"
        INSERT INTO errors_log(
            error_message,
            date_time_record,
            method_name,
            num_doc,            
            description
        )
        VALUES(
            @errorMessage,
            @dateTimeRecord,
            @methodName,
            @numDoc,            
            @description
        )";

            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    using (var command = new NpgsqlCommand(sql, conn))
                    {
                        command.Parameters.Add("@errorMessage", NpgsqlTypes.NpgsqlDbType.Text).Value = (object)truncatedErrorMessage ?? DBNull.Value;
                        command.Parameters.Add("@dateTimeRecord", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now;
                        command.Parameters.Add("@methodName", NpgsqlTypes.NpgsqlDbType.Text).Value = (object)truncatedMethodName ?? DBNull.Value;
                        command.Parameters.Add("@numDoc", NpgsqlTypes.NpgsqlDbType.Bigint).Value = numDoc;
                        command.Parameters.Add("@cashDeskNumber", NpgsqlTypes.NpgsqlDbType.Smallint).Value = cashDeskNumber;
                        command.Parameters.Add("@description", NpgsqlTypes.NpgsqlDbType.Text).Value = (object)truncatedDescription ?? DBNull.Value;

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем оригинальные значения (до обрезки)
                var logMessage = new StringBuilder()
                    .AppendLine($"Дата: {DateTime.Now}")
                    .AppendLine($"Ошибка: {ex.Message}")
                    .AppendLine($"StackTrace: {ex.StackTrace}")
                    .AppendLine("Параметры:")
                    .AppendLine($"- Сообщение: {errorMessage}")
                    .AppendLine($"- Метод: {methodName}")
                    .AppendLine($"- Документ: {numDoc}")
                    .AppendLine($"- Касса: {cashDeskNumber}")
                    .AppendLine($"- Описание: {description}")
                    .AppendLine(new string('-', 50))
                    .ToString();

                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "ErrorsLog.txt");

                try
                {
                    File.AppendAllText(logPath, logMessage);
                }
                catch (Exception fileEx)
                {
                    MessageBox.Show($"Ошибка записи в лог: {fileEx.Message}");

                }
            }
        }

        // Вспомогательный метод для обрезки строк
        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength);
        }

        public static int SystemTaxation
        {
            get
            {
                return system_taxation;
            }
            set
            {
                system_taxation = value;
            }
        }


        public static bool First_Login_Admin
        {
            get
            {
                return first_fogin_admin;
            }
            set
            {
                first_fogin_admin = value;
            }
        }


        //public static bool piot_cdn_check(ProductData productData, string mark_str, ListViewItem lvi, Cash_check check)
        //{
        //    bool result = true;
        //    string mark_str_cdn = "";

        //    if (productData.IsCDNCheck())
        //    {
        //        if (MainStaticClass.CashDeskNumber != 9)// && MainStaticClass.EnableCdnMarkers == 1
        //        {
        //            if (MainStaticClass.CDN_Token == "")
        //            {
        //                MessageBox.Show("В этой кассе не заполнен CDN токен, \r\n ПРОДАЖА ДАННОГО ТОВАРА НЕВОЗМОЖНА ! ", "Проверка CDN");
        //                result = false;
        //            }
        //            else
        //            {
        //                //CDN cdn = new CDN();
        //                PIOT piot = new PIOT();
        //                List<string> codes = new List<string>();
        //                mark_str_cdn = mark_str.Replace("\u001d", @"\u001d");
        //                codes.Add(mark_str_cdn);
        //                mark_str_cdn = mark_str_cdn.Replace("'", "\'");
        //                Dictionary<string, string> d_tovar = new Dictionary<string, string>();
        //                d_tovar[lvi.SubItems[1].Text] = lvi.SubItems[0].Text;
        //                //result = cdn.cdn_check_marker_code(codes, mark_str, check.numdoc, ref check.request, mark_str_cdn, d_tovar, check, productData);
        //                result = piot.cdn_check_marker_code(codes, mark_str, check.numdoc, ref check.request, mark_str_cdn, d_tovar, check, productData);
        //            }

        //        }
        //    }

        //    return result;
        //}


        //public static bool cdn_check(ProductData productData, string mark_str, ListViewItem lvi, Cash_check check)
        //{
        //    bool result = true;
        //    string mark_str_cdn = "";

        //    if (productData.IsCDNCheck())
        //    {
        //        if (MainStaticClass.CashDeskNumber != 9)// && MainStaticClass.EnableCdnMarkers == 1
        //        {
        //            if (MainStaticClass.CDN_Token == "")
        //            {
        //                MessageBox.Show("В этой кассе не заполнен CDN токен, \r\n ПРОДАЖА ДАННОГО ТОВАРА НЕВОЗМОЖНА ! ", "Проверка CDN");
        //                result = false;
        //            }
        //            else
        //            {
        //                CDN cdn = new CDN();
        //                List<string> codes = new List<string>();
        //                mark_str_cdn = mark_str.Replace("\u001d", @"\u001d");
        //                codes.Add(mark_str_cdn);
        //                mark_str_cdn = mark_str_cdn.Replace("'", "\'");
        //                Dictionary<string, string> d_tovar = new Dictionary<string, string>();
        //                d_tovar[lvi.SubItems[1].Text] = lvi.SubItems[0].Text;
        //                result = cdn.cdn_check_marker_code(codes, mark_str, check.numdoc, ref check.request, mark_str_cdn, d_tovar, check, productData);
        //            }
        //        }
        //    }

        //    return result;
        //}


        /// <summary>
        /// получает строкове 
        /// представление текущей валюты
        /// </summary>
        /// <returns></returns>
        //public static string get_currency()
        //{

        //    string result = "";

        //    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //    try
        //    {
        //        conn.Open();
        //        string query = "SELECT currency  FROM constants;";
        //        NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //        result = Convert.ToString(command.ExecuteScalar());
        //        conn.Close();

        //    }
        //    catch (NpgsqlException)
        //    {
        //        MyMessageBox mmb = new MyMessageBox("Ошибка при получении валюты", "Ошибка при получении валюты");
        //        mmb.ShowDialog();
        //    }
        //    catch (Exception)
        //    {
        //        MyMessageBox mmb = new MyMessageBox("Ошибка при получении валюты", "Ошибка при получении валюты");
        //        mmb.ShowDialog();
        //    }
        //    finally
        //    {
        //        if (conn.State == ConnectionState.Open)
        //        {
        //            conn.Close();
        //        }
        //    }

        //    return result;

        //}

        //public static string version()
        //{
        //    var assembly = Assembly.GetEntryAssembly();
        //    var version = assembly?.GetName().Version;
        //    return version?.ToString() ?? "1.0.0";
        //}

        public static string version()
        {
            var versionStr = GetProductVersion();

            //// Пробуем распарсить как Unix timestamp
            //if (long.TryParse(versionStr, out long timestamp))
            //{
            //    var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            //    // Возвращаем в старом формате "1.0.0.0" для совместимости
            //    //return "1.0.0.0";
            //    return date;
            //}

            return "1"+versionStr;//костыль для веб сервиса предыдущей версии
        }

        private static string GetProductVersion()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly();
                var location = assembly?.Location;

                if (string.IsNullOrEmpty(location) || !File.Exists(location))
                    return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                var versionInfo = FileVersionInfo.GetVersionInfo(location);
                return versionInfo.ProductVersion ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }
            catch
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }
        }



        private static string get_device_info()
        {
            string string_get_device_info = string.Empty;

            //if (MainStaticClass.printing_using_libraries == 0)
            //{
            //    try
            //    {
            //        Cash8.FiscallPrintJason.RootObject result = FiscallPrintJason.execute_operator_type("getDeviceInfo");
            //        if (result != null)
            //        {
            //            if (result.results[0].status == "ready")//Задание выполнено успешно 
            //            {
            //                string_get_device_info = JsonConvert.SerializeObject(result.results[0].result.deviceInfo, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            //                //string_get_device_info = result.results[0].result.deviceInfo.ToString();
            //            }
            //            else
            //            {
            //                MessageBox.Show(" Ошибка !!! " + result.results[0].status + " | " + result.results[0].errorDescription);
            //            }
            //        }
            //        else
            //        {
            //            MessageBox.Show("Общая ошибка");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("getDeviceInfo" + ex.Message);
            //    }
            //}
            //else
            //{                
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_CACHE_REQUISITES);
            fptr.queryData();
            string_get_device_info = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_FFD_VERSION).ToString();
            //fptr.close();
            //}

            return string_get_device_info;

            //}
        }

        private static void update_version_fn(string _version_fn)
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "UPDATE constants SET version_fn=" + _version_fn;
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(" Ошибка при обновлении версии фн " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Ошибка при обновлении версии фн " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }


        /// <summary>
        /// Проверяет и корректирует установленную версию ФН в программе
        /// </summary>
        public static void check_version_fn(ref bool restart, ref bool error)//,
        {
            string _version_fn_real = MainStaticClass.GetVersionFnReal;
            double _version_fn = Convert.ToDouble(MainStaticClass.GetVersionFn);

            //MessageBox.Show("_version_fn_real "+_version_fn_real);
            //MessageBox.Show("_version_fn "+ _version_fn.ToString());
            if ((_version_fn_real == "0") || (_version_fn_real == ""))
            {
                error = true;
                MessageBox.Show("Не удалось проверить версию ФН, ПРИ ВКЛЮЧЧЕНИИ КАССОВОЙ ПРОГРАММЫ ФИСКАЛЬНЫЙ РЕГИСТРАТОР ДОЛЖЕН БЫТЬ ВКЛЮЧЕН !!!");
                return;
            }
            if ((_version_fn_real == "1.2") && (_version_fn != 2))
            {
                MessageBox.Show(" Версия ФН прочитанная из ФР " + _version_fn_real);
                update_version_fn("2");
                restart = true;
            }
            else if ((_version_fn_real != "1.2") && (_version_fn != 1))
            {
                MessageBox.Show(" Версия ФН прочитанная из ФР " + _version_fn_real);
                update_version_fn("1");
                restart = true;
            }
        }

        /// <summary>
        /// Получение реальной версии фн
        /// </summary>
        /// <returns></returns>
        public static string GetVersionFnReal
        {
            get
            {
                //if (MainStaticClass.printing_using_libraries == 0)
                //{
                //    if (version_fn_real == "")
                //    {
                //        try
                //        {
                //            Cash8.FiscallPrintJason.RootObject result = FiscallPrintJason.execute_operator_type("getRegistrationInfo");
                //            if (result != null)
                //            {
                //                if (result.results[0].status == "ready")//Задание выполнено успешно 
                //                {
                //                    version_fn_real = result.results[0].result.device.ffdVersion;
                //                }
                //                else
                //                {
                //                    MessageBox.Show(" Ошибка !!! " + result.results[0].status + " | " + result.results[0].errorDescription);
                //                }
                //            }
                //            else
                //            {
                //                MessageBox.Show("Общая ошибка");
                //            }
                //        }
                //        catch (Exception ex)
                //        {
                //            MessageBox.Show("get_registration_info" + ex.Message);
                //        }
                //    }
                //}
                //else
                //{
                IFptr fptr = MainStaticClass.FPTR;
                //setConnectSetting(fptr);
                if (!fptr.isOpened())
                {
                    fptr.open();
                }

                fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_FFD_VERSIONS);
                fptr.fnQueryData();
                version_fn_real = (Convert.ToDouble(fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_DEVICE_FFD_VERSION)) / 100).ToString().Replace(",", ".");
                //fptr.close();
                //}

                return version_fn_real;
            }
        }

        private static void setConnectSetting(IFptr fptr)
        {
            if (MainStaticClass.GetVariantConnectFN == 0)
            {
                //fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_MODEL, AtolConstants.LIBFPTR_MODEL_ATOL_AUTO.ToString());
                //fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_PORT, AtolConstants.LIBFPTR_PORT_COM.ToString());
                ////fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_PORT, AtolConstants.LIBFPTR_PORT_TCPIP.ToString());
                //fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_COM_FILE, MainStaticClass.FnSerialPort);
                ////fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_IPADDRESS, "10.21.200.46");
                ////fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_IPPORT, "5555");            
                //fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_BAUDRATE, AtolConstants.LIBFPTR_PORT_BR_115200.ToString());
                fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_PORT, (AtolConstants.LIBFPTR_PORT_USB).ToString());
            }
            else if (MainStaticClass.GetVariantConnectFN == 1)
            {
                fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_MODEL, AtolConstants.LIBFPTR_MODEL_ATOL_AUTO.ToString());
                //fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_PORT, AtolConstants.LIBFPTR_PORT_COM.ToString());
                fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_PORT, AtolConstants.LIBFPTR_PORT_TCPIP.ToString());
                //fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_COM_FILE, MainStaticClass.FnSerialPort);
                string[] ip_adress = GetFnIpaddr.Split(':');
                fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_IPADDRESS, ip_adress[0]);
                fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_IPPORT, ip_adress[1]);
                //fptr.setSingleSetting(AtolConstants.LIBFPTR_SETTING_BAUDRATE, AtolConstants.LIBFPTR_PORT_BR_115200.ToString());
            }
            fptr.applySingleSettings();
        }


        private static string get_registration_info()
        {
            string string_get_registration_info = string.Empty;
            //if (MainStaticClass.printing_using_libraries == 0)
            //{
            //    try
            //    {
            //        Cash8.FiscallPrintJason.RootObject result = FiscallPrintJason.execute_operator_type("getRegistrationInfo");
            //        if (result != null)
            //        {
            //            if (result.results[0].status == "ready")//Задание выполнено успешно 
            //            {
            //                string_get_registration_info = JsonConvert.SerializeObject(result.results[0].result, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            //                //string_get_registration_info = result.results[0].result.organization.vatin;
            //                //string_get_device_info = result.results[0].result.deviceInfo.ToString();
            //            }
            //            else
            //            {
            //                MessageBox.Show(" Ошибка !!! " + result.results[0].status + " | " + result.results[0].errorDescription);
            //            }
            //        }
            //        else
            //        {
            //            MessageBox.Show("Общая ошибка");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("get_registration_info" + ex.Message);
            //    }
            //}
            //else
            //{
            IFptr fptr = MainStaticClass.FPTR;
            //setConnectSetting(fptr);
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_CACHE_REQUISITES);
            fptr.queryData();
            string_get_registration_info = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_FFD_VERSION).ToString();

            //}

            return string_get_registration_info;
        }

        public class ResultGetData
        {
            public string Successfully { get; set; }
            public string Shop { get; set; }
            public string NumCash { get; set; }
            public string Version { get; set; }
            public string OSVersion { get; set; }
            public string DeviceInfo { get; set; }
            public string PrintingLibrary { get; set; }
            public string VersionPrintingLibrary { get; set; }
            public string VariantUsePrintingLibrary { get; set; }

        }

        #region DeviceInfo
        public class Device
        {
            public string registrationNumber { get; set; }
            public string defaultTaxationType { get; set; }
            public string ofdChannel { get; set; }
            public string ffdVersion { get; set; }
            public bool marking { get; set; }
        }

        public class Organization
        {
            public string name { get; set; }
            public string vatin { get; set; }
            public List<string> taxationTypes { get; set; }
            public string address { get; set; }
        }

        public class DeviseInfo
        {
            public string configurationVersion { get; set; }
            public string fnFfdVersion { get; set; }
            public string serial { get; set; }
            public string firmwareVersion { get; set; }
            public string ffdVersion { get; set; }
            public Organization organization { get; set; }
            public Device device { get; set; }
            public string modelName { get; set; }
        }

        private static string get_device_info_printing_libraries()
        {
            string result = "";

            IFptr fptr = MainStaticClass.FPTR;
            if (!fptr.isOpened())
            {
                fptr.open();
            }


            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_UNIT_VERSION);
            fptr.setParam(AtolConstants.LIBFPTR_PARAM_UNIT_TYPE, AtolConstants.LIBFPTR_UT_CONFIGURATION);
            fptr.queryData();

            string configurationVersion = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_UNIT_VERSION);
            //string releaseVersion = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_UNIT_RELEASE_VERSION);


            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_SERIAL_NUMBER);
            fptr.queryData();

            string serialNumber = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_SERIAL_NUMBER);

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_STATUS);
            fptr.queryData();
            string modelName = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_MODEL_NAME);


            fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_MODEL_INFO);
            fptr.queryData();

            string firmwareVersion = fptr.getParamString(AtolConstants.LIBFPTR_PARAM_UNIT_VERSION);

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_REG_INFO);
            fptr.fnQueryData();

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_FFD_VERSIONS);
            fptr.fnQueryData();

            uint deviceFfdVersion = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_DEVICE_FFD_VERSION);
            uint fnFfdVersion = fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_FN_FFD_VERSION);

            fptr.setParam(AtolConstants.LIBFPTR_PARAM_FN_DATA_TYPE, AtolConstants.LIBFPTR_FNDT_REG_INFO);
            fptr.fnQueryData();

            string organizationAddress = fptr.getParamString(1009);
            string organizationVATIN = fptr.getParamString(1018);
            string organizationName = fptr.getParamString(1048);
            uint taxationTypes = fptr.getParamInt(1062);
            uint ffdVersion = fptr.getParamInt(1209);
            string registrationNumber = fptr.getParamString(1037);

            bool marking = fptr.getParamBool(AtolConstants.LIBFPTR_PARAM_TRADE_MARKED_PRODUCTS);

            DeviseInfo deviseInfo = new DeviseInfo();
            deviseInfo.configurationVersion = configurationVersion;
            deviseInfo.fnFfdVersion = deviceFfdVersion.ToString();
            deviseInfo.serial = serialNumber;
            deviseInfo.firmwareVersion = firmwareVersion;
            deviseInfo.ffdVersion = (Convert.ToDouble(ffdVersion) / 100).ToString().Replace(",", ".");
            deviseInfo.modelName = modelName;

            Organization organization = new Organization();
            organization.name = organizationName;
            organization.address = organizationAddress;
            organization.vatin = organizationVATIN;
            //MessageBox.Show(taxationTypes.ToString());
            List<string> tT = new List<string>();
            tT.Add(taxationTypes.ToString());
            organization.taxationTypes = tT;

            deviseInfo.organization = organization;

            Device device = new Device();
            device.defaultTaxationType = "";
            device.ffdVersion = (Convert.ToDouble(ffdVersion) / 100).ToString().Replace(",", ".");
            device.marking = marking;
            device.ofdChannel = fptr.getSingleSetting(AtolConstants.LIBFPTR_SETTING_OFD_CHANNEL);
            device.registrationNumber = registrationNumber;

            deviseInfo.device = device;

            result = JsonConvert.SerializeObject(deviseInfo, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            return result;
        }
        #endregion


        //private static string GetAtolDriverVersion()
        //{
        //    try
        //    {
        //        // Получаем путь к исполняемому файлу
        //        string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        //        string directory = Path.GetDirectoryName(executablePath);

        //        // Формируем полный путь к библиотеке
        //        string dllPath = Path.Combine(directory, "Atol.Drivers10.Fptr.dll");

        //        // Загружаем сборку и получаем версию
        //        var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
        //        return assembly.GetName().Version.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Обработка ошибок
        //        MessageBox.Show($"Ошибка при получении версии: {ex.Message}");
        //        return "Версия не определена";
        //    }
        //}

        public async static Task<bool> SendResultGetData()
        {
            bool result = true;

            string count_day = CryptorEngine.get_count_day();
            string key = nick_shop.Trim() + count_day.Trim() + nick_shop.Trim();
            string data_encrypt = "";
            ResultGetData resultGetData = new ResultGetData();
            resultGetData.Successfully = "Successfully";
            resultGetData.Version = MainStaticClass.version().Replace(".", "");
            resultGetData.NumCash = MainStaticClass.CashDeskNumber.ToString();
            resultGetData.OSVersion = Environment.OSVersion.VersionString;
            resultGetData.VariantUsePrintingLibrary = MainStaticClass.variant_connect_fn.ToString();
            resultGetData.VersionPrintingLibrary = await GetAtolDriverVersion();
            //Запросим информацию про фискальный регистратор
            if (MainStaticClass.printing_using_libraries == 0)
            {
                resultGetData.DeviceInfo = "[" + get_device_info() + "," + get_registration_info() + "]";
            }
            else
            {
                resultGetData.DeviceInfo = get_device_info_printing_libraries();
            }
            resultGetData.PrintingLibrary = MainStaticClass.PrintingUsingLibraries.ToString();
            //string vatin = get_registration_info();
            //if (vatin.Trim() != "")
            //{
            //    vatin = "vatin=" + vatin;
            //}
            //resultGetData.DeviceInfo += vatin;


            string data = JsonConvert.SerializeObject(resultGetData, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            data_encrypt = CryptorEngine.Encrypt(data, true, key);
            //using (var ds = MainStaticClass.get_ds())
            //{
            //    ds.Timeout = 60000;
            //    try
            //    {
            //        ds.GetDataForCasheV8Successfully(nick_shop, data_encrypt, MainStaticClass.GetWorkSchema.ToString());
            //    }
            //    catch
            //    {
            //        result = false;
            //    }
            //}

            return result;
        }


        /// <summary>
        /// Отправить информацию что касса включена
        /// и на нейт такая то версия программы
        /// </summary>
        /// <returns></returns>
        public async static Task<bool> SendOnlineStatus()
        {
            bool result = true;

            string count_day = CryptorEngine.get_count_day();
            string key = nick_shop.Trim() + count_day.Trim() + nick_shop.Trim();
            string data_encrypt = "";
            ResultGetData resultGetData = new ResultGetData();
            resultGetData.Successfully = "Successfully";
            resultGetData.Version = MainStaticClass.version().Replace(".", "");
            resultGetData.NumCash = MainStaticClass.CashDeskNumber.ToString();
            resultGetData.PrintingLibrary = MainStaticClass.PrintingUsingLibraries.ToString();
            resultGetData.VariantUsePrintingLibrary = MainStaticClass.variant_connect_fn.ToString();
            resultGetData.VersionPrintingLibrary = await GetAtolDriverVersion();
            string data = JsonConvert.SerializeObject(resultGetData, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            data_encrypt = CryptorEngine.Encrypt(data, true, key);
            //using (var ds = MainStaticClass.get_ds())
            //{
            //    ds.Timeout = 60000;
            //    try
            //    {
            //        ds.OnlineCasheV8Successfully(nick_shop, data_encrypt, MainStaticClass.GetWorkSchema.ToString());
            //    }
            //    catch
            //    {
            //        result = false;
            //    }
            //}

            return result;
        }

        public static string getMd5Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public static int check_new_shema_autenticate()
        {
            int result = 0;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT COUNT(*)FROM information_schema.columns where table_name='users' and column_name='rights'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(" Ошика при определении схемы " + ex.Message);
                result = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Ошика при определении схемы " + ex.Message);
                result = -1;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;

        }



        /// <summary>
        /// получение признака ведения учета в 2 валютах
        /// </summary>
        /// <returns></returns>
        public static bool get_account_two_currencies()
        {
            bool result = false;


            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT two_currencies  FROM constants;";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToBoolean(command.ExecuteScalar());
                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                //MyMessageBox mmb = new MyMessageBox("Ошибка при получении валюты", "Ошибка при получении валюты");
                //mmb.ShowDialog();
            }
            catch (Exception ex)
            {
                //MyMessageBox mmb = new MyMessageBox("Ошибка при получении валюты", "Ошибка при получении валюты");
                //mmb.ShowDialog();
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;

        }


        public static IWebProxy CreateWebProxyWithCredentials(String sUrl, string ProxyUserName, string ProxyUserPassword, string sAuthType, string ProxyUserDomain)
        {
            if (String.IsNullOrEmpty(ProxyUserName) || String.IsNullOrEmpty(ProxyUserPassword))
            {
                return null;
            }
            // get default proxy and assign it to the WebService. Alternatively, you can replace this with manual WebProxy creation.
            IWebProxy iDefaultWebProxy = WebRequest.DefaultWebProxy;
            Uri uriProxy = iDefaultWebProxy.GetProxy(new Uri(sUrl));
            string sProxyUrl = uriProxy.AbsoluteUri;
            if (sProxyUrl == sUrl)
            {//no proxy specified
                return null;
            }
            IWebProxy proxyObject = new WebProxy(sProxyUrl, true);
            // assign the credentials to the Proxy
            //todo do we need to add credentials to  WebService too??
            if ((!String.IsNullOrEmpty(sAuthType)) && (sAuthType.ToLower() != "basic"))
            {
                //from http://www.mcse.ms/archive105-2004-10-1165271.html
                // create credentials cache - it will hold both, the WebProxy credentials (??and the WebService credentials too??)
                System.Net.CredentialCache cache = new System.Net.CredentialCache();
                // add default credentials for Proxy (notice the authType = 'Kerberos' !) Other types are 'Basic', 'Digest', 'Negotiate', 'NTLM'
                cache.Add(new Uri(sProxyUrl), sAuthType, new System.Net.NetworkCredential(ProxyUserName, ProxyUserPassword, ProxyUserDomain));
                proxyObject.Credentials = cache;
            }
            else//special case for Basic (from http://www.xmlwebservices.cc/index_FAQ.htm )
            {
                proxyObject.Credentials = new System.Net.NetworkCredential(ProxyUserName, ProxyUserPassword);
            }
            return proxyObject;
        }


        public static DS get_ds()
        {
            DS ds = null;
            ds = new DS();
            try
            {
                ds.Url = MainStaticClass.PathForWebService;//.get_path_for_web_service();
            }
            catch
            {
                ds.Url = "http://8.8.8.8/DiscountSystem/Ds.asmx";//.get_path_for_web_service();
            }

            return ds;
        }



        private static DateTime get_datetime_on_server()
        {
            DateTime result = new DateTime(1, 1, 1);

            //if (!MainStaticClass.service_is_worker())
            //{
            //    return result;
            //}

            try
            {
                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 15000;
                result = ds.GetDateTimeServer();
            }
            catch (Exception)
            {

            }

            return result;

        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int get_documents_not_out()
        {
            int result = 0;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM checks_header WHERE is_sent = 0";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt32(command.ExecuteScalar());
                conn.Close();
            }
            catch (NpgsqlException)
            {
                result = -1;
            }
            catch (Exception)
            {
                result = -1;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;

        }



        /// <summary>
        /// Количество документов которые находятся
        /// за диапазоном разрешенных дат
        /// </summary>
        /// <returns></returns>
        public static int get_documents_out_of_the_range_of_dates()
        {
            int result = 0;

            DateTime result_query_datetime_on_server = get_datetime_on_server();
            if (result_query_datetime_on_server == new DateTime(1, 1, 1))
            {
                result = -1;
            }
            else
            {
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                try
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM checks_header WHERE (date_time_write<@start_data OR date_time_write>@current_data) AND is_sent = 0";
                    NpgsqlParameter start_data = new NpgsqlParameter("start_data", result_query_datetime_on_server.AddDays(-31));
                    NpgsqlParameter current_data = new NpgsqlParameter("current_data", result_query_datetime_on_server.AddHours(2));
                    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                    command.Parameters.Add(start_data);
                    command.Parameters.Add(current_data);
                    result = Convert.ToInt32(command.ExecuteScalar());
                    conn.Close();
                }
                catch (NpgsqlException)
                {
                    result = -2;
                }
                catch (Exception)
                {
                    result = -2;
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }

                }
            }

            return result;
        }

        public async static Task<int> GetUnloadingInterval()
        {
            int result = 0;

            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT  unloading_period  FROM constants;";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message);
            }


            return result;
        }

        public static bool service_is_worker()
        {
            bool result = true;

            DS ds = MainStaticClass.get_ds();
            ds.Timeout = 3000;
            try
            {
                result = ds.ServiceIsWorker();
            }
            catch
            {
                result = false;
            }


            return result;
        }
        
        public static string PathForWebService
        {
            get
            {

                if (path_for_web_service == "")
                {
                    path_for_web_service = get_path_for_web_service();
                }
                return path_for_web_service;
            }

        }

        /// <summary>
        /// Возвращает путь к веб сервису дисконта
        /// </summary>
        /// <returns></returns>
        private static string get_path_for_web_service()
        {
            string result = "";

            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = " SELECT path_for_web_service  FROM constants ";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = reader[0].ToString();
                }
                reader.Close();
                reader.Dispose();
                command.Dispose();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                //MyMessageBox mmb = new MyMessageBox(ex.Message, "Получение пути веб сервиса дисконта");
                //mmb.ShowDialog();
            }
            catch (Exception ex)
            {
                //MyMessageBox mmb = new MyMessageBox(ex.Message, "Получение пути веб сервиса дисконта");
                //mmb.ShowDialog();
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }


            return result;
        }


        public static bool two_currencies()
        {
            bool result = false;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                string query = "SELECT two_currencies  FROM constants;";
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToBoolean(command.ExecuteScalar());
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;

        }



        //public static string LoginPromo
        //{
        //    get
        //    {

        //        if (login_promo == "")
        //        {


        //            NpgsqlConnection conn = null;

        //            try
        //            {
        //                conn = MainStaticClass.NpgsqlConn();
        //                conn.Open();
        //                string query = "SELECT login_promo  FROM constants;";
        //                NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //                login_promo = command.ExecuteScalar().ToString().Trim();
        //                conn.Close();
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                MessageBox.Show(ex.Message, " Ошибка при определении включения бонусов  получение пароля");
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show(ex.Message, " Ошибка при определении включения бонусов получение пароля");
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }

        //            return login_promo;

        //        }
        //        else
        //        {
        //            return login_promo;
        //        }
        //    }
        //}


        /// <summary>
        /// Если пароль заполнен, значит бонусная программа для этой кассы включена
        /// </summary>
        //public static string PassPromo
        //{
        //    get
        //    {

        //        if (pass_promo == "")
        //        {


        //            NpgsqlConnection conn = null;

        //            try
        //            {
        //                conn = MainStaticClass.NpgsqlConn();
        //                conn.Open();
        //                string query = "SELECT pass_promo  FROM constants;";
        //                NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //                pass_promo = command.ExecuteScalar().ToString().Trim();
        //                conn.Close();
        //            }
        //            catch (NpgsqlException ex)
        //            {
        //                MessageBox.Show(ex.Message, " Ошибка при определении включения бонусов  получение пароля");
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show(ex.Message, " Ошибка при определении включения бонусов получение пароля");
        //            }
        //            finally
        //            {
        //                if (conn.State == ConnectionState.Open)
        //                {
        //                    conn.Close();
        //                }
        //            }

        //            return pass_promo;

        //        }
        //        else
        //        {
        //            return pass_promo;
        //        }               
        //    }


        //}

        /// <summary>
        /// Возвращает путь к папке обмена с главным компом
        /// 
        /// </summary>
        /// <returns></returns>
        public static string get_change_path_for_main_computer()
        {
            string result = "";

            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT change_path_for_main_computer  FROM constants";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = command.ExecuteScalar().ToString().Trim();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message, " Получение номера принтера ");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, " Получение номера принтера ");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }


        public static decimal get_rate()
        {
            decimal result = 0;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = " SELECT rate  FROM constants;";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                object result_query = command.ExecuteScalar();
                if (result_query != null)
                {
                    result = Convert.ToDecimal(result_query);
                }
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                //MyMessageBox mmb = new MyMessageBox(ex.Message, "Получение курса");
                //mmb.ShowDialog();
            }
            catch (Exception ex)
            {
                //MyMessageBox mmb = new MyMessageBox(ex.Message, "Получение курса");
                //mmb.ShowDialog();
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }

        //public static float Font_list_view()
        //{
        //    float result = 0;
        //    NpgsqlConnection conn = null;
        //    try
        //    {
        //        conn = MainStaticClass.NpgsqlConn();
        //        conn.Open();
        //        string select_query = "SELECT size_font_listview FROM constants";
        //        NpgsqlCommand command = new NpgsqlCommand(select_query, conn);
        //        result = Convert.ToSingle(command.ExecuteScalar());
        //        conn.Close();
        //    }
        //    catch
        //    {

        //    }
        //    finally
        //    {
        //        if (conn.State == ConnectionState.Open)
        //        {
        //            conn.Close();
        //        }
        //    }
        //    if (result == 0)
        //    {
        //        result = 12;
        //    }
        //    return result;
        //}

        public static void add_window(object form)
        {
            if (!forms.Contains(form))
            {
                forms.Add(form);
            }
        }

        public static bool Result_Fiscal_Print
        {
            get
            {
                return result_fiscal_print;
            }
            set
            {
                result_fiscal_print = value;
            }
        }



        /// <summary>
        /// Получает индекс принтера
        /// в любом случае возвращает 0
        /// </summary>
        /// <returns></returns>
        public static int get_num_text_pinter()
        {
            int result = 0;

            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT num_text_printer  FROM constants";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                string result_query = command.ExecuteScalar().ToString().Trim();
                conn.Close();
                if (result_query.Length > 0)
                {
                    result = Convert.ToInt16(result_query);
                }
                //if (result > PrinterSettings.InstalledPrinters.Count - 1)
                //{
                //    result = PrinterSettings.InstalledPrinters.Count - 1;
                //    MessageBox.Show("В константах неверно указан принтер", "Получение номера принтера");
                //}
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message, " Получение номера принтера ");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, " Получение номера принтера ");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }


        //

        public static void delete_all_events_in_log(DateTime date)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "DELETE FROM logs WHERE time_event < '" + date.ToString("yyyy.MM.dd") + "'";
                command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        public static void delete_events_in_log(string document_number)
        {
            //NpgsqlConnection conn = null;
            //NpgsqlCommand command = null;
            //try
            //{
            //    conn = MainStaticClass.NpgsqlConn();
            //    conn.Open();
            //    string query = "DELETE FROM logs WHERE document_number = '" + document_number + "'";
            //    command = new NpgsqlCommand(query, conn);
            //    command.ExecuteNonQuery();
            //    conn.Close();

            //}
            //catch (NpgsqlException ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
            //finally
            //{
            //    if (conn.State == ConnectionState.Open)
            //    {
            //        conn.Close();
            //    }
            //}
        }

        //public static string read_last_sell_guid()
        //{
        //    NpgsqlConnection conn = null;
        //    NpgsqlCommand command = null;
        //    string result = "";
        //    try
        //    {
        //        conn = MainStaticClass.NpgsqlConn();
        //        conn.Open();                
        //        string query = "SELECT guid FROM last_guid;";
        //        command = new NpgsqlCommand(query, conn);                
        //        object result_query=command.ExecuteScalar();
        //        if(result_query!=null)
        //        {
        //            result = result_query.ToString();                
        //        }
        //        conn.Close();
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        MessageBox.Show(ex.Message + " | " + ex.Detail);
        //    }
        //    finally
        //    {
        //        if (conn.State == ConnectionState.Open)
        //        {
        //            conn.Close();
        //        }
        //    }

        //    return result;
        //}


        //public static void write_last_sell_guid(string guid,string num_doc)
        //{
        //    NpgsqlConnection conn = null;
        //    NpgsqlCommand command = null;
        //    NpgsqlTransaction trans = null;
        //    try
        //    {
        //        conn = MainStaticClass.NpgsqlConn();
        //        conn.Open();
        //        trans = conn.BeginTransaction();
        //        string query = "DELETE FROM last_guid;";
        //        command = new NpgsqlCommand(query, conn);
        //        command.Transaction = trans;
        //        command.ExecuteNonQuery();
        //        query = "INSERT INTO last_guid(guid,num_doc)VALUES ('" + guid + "',"+num_doc+");";
        //        command = new NpgsqlCommand(query, conn);
        //        command.Transaction = trans;
        //        command.ExecuteNonQuery();
        //        trans.Commit();
        //        conn.Close();
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        MessageBox.Show(ex.Message + " | " + ex.Detail);
        //    }
        //    finally
        //    {
        //        if (conn.State == ConnectionState.Open)
        //        {
        //            conn.Close();
        //        }
        //    }
        //}

        //write_last_sell_guid

        public static void write_document_wil_be_printed(string document_number)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "INSERT INTO document_wil_be_printed(document_number,tax_type)VALUES (" + document_number + "," + MainStaticClass.system_taxation.ToString() + ");";
                command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                command.Dispose();
                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }

        public static void write_document_wil_be_printed(string document_number, int variant)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "INSERT INTO document_wil_be_printed(document_number,tax_type)VALUES (" + document_number + "," + (MainStaticClass.system_taxation + variant).ToString() + ");";
                command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                command.Dispose();
                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }


        public static void delete_document_wil_be_printed(string document_number)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "DELETE FROM document_wil_be_printed WHERE document_number=" + document_number + " AND tax_type=" + MainStaticClass.system_taxation.ToString();
                command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }

        public static void delete_document_wil_be_printed(string document_number, int variant)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "DELETE FROM document_wil_be_printed WHERE document_number=" + document_number + " AND tax_type=" + (MainStaticClass.system_taxation + variant).ToString();
                command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }

        /// <summary>
        /// Получаем признак печатать ли букву m при печати 
        /// маркированного товара
        /// </summary>
        /// <returns></returns>
        public static bool get_print_m()
        {
            bool result = true;

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT  print_m	FROM constants;";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToBoolean(command.ExecuteScalar());
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(" Ошибка при получении флага print_m" + ex.Message);
                result = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Ошибка при получении флага print_m" + ex.Message);
                result = false;
            }

            return result;

        }

        public static int get_document_wil_be_printed(string document_number)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            int result = 0;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT COUNT(document_number) FROM document_wil_be_printed WHERE document_number=" + document_number + " AND tax_type =" + MainStaticClass.system_taxation.ToString();
                command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
            return result;
        }

        public static int get_document_wil_be_printed(string document_number, int variant)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            int result = 0;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT COUNT(document_number) FROM document_wil_be_printed WHERE document_number=" + document_number + " AND tax_type =" + (MainStaticClass.system_taxation + variant).ToString();
                command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="description"></param>
        /// <param name="numdoc"></param>
        /// <param name="mark"></param>
        /// <param name="guid"></param>
        /// <param name="status">1 - Ответ от cdn 2 - Отладочная информация 3 - Ошибка при работе с CDN</param> 
        public async static Task write_cdn_log(string description, string numdoc, string mark, string status)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "INSERT INTO cdn_log(date,cdn_answer,numdoc,num_cash,mark,status) VALUES(@date,@cdn_answer,@numdoc,@num_cash,@mark,@status)";
                command = new NpgsqlCommand(query, conn);

                //NpgsqlParameter parameter = new NpgsqlParameter("date", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"));
                //NpgsqlParameter parameter = new NpgsqlParameter("date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                NpgsqlParameter parameter = new NpgsqlParameter("date", DateTime.Now);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("cdn_answer", description);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("numdoc", numdoc);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("num_cash", MainStaticClass.CashDeskNumber);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("mark", mark);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("status", Convert.ToInt16(status));
                command.Parameters.Add(parameter);

                command.ExecuteNonQuery();
                command.Dispose();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }



        public async static void write_event_in_log(string description, string metadata, string document_number)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {

                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                //string query = "INSERT INTO logs(time_event,description,metadata,document_number) VALUES('" + DateTime.Now.ToString("yyy-MM-dd HH:mm:ss") + "','" + description + "','" + metadata + "','" + document_number + "')";
                string query = "INSERT INTO logs(time_event,description,metadata,document_number) VALUES(@time_event,@description,@metadata,@document_number)";
                command = new NpgsqlCommand(query, conn);

                //NpgsqlParameter parameter = new NpgsqlParameter("time_event", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"));
                NpgsqlParameter parameter = new NpgsqlParameter("time_event", DateTime.Now);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("description", description);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("metadata", metadata);
                command.Parameters.Add(parameter);

                parameter = new NpgsqlParameter("document_number", Convert.ToInt64(document_number));
                command.Parameters.Add(parameter);

                command.ExecuteNonQuery();
                command.Dispose();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }




        public static void remove_window(object form)
        {
            forms.Remove(form);
        }

        /*Эта функция возвращает true если такая форма уже открыта
         * иначе ложь 
         */
        public static bool exist_form(object form)
        {
            return forms.Contains(form);
        }





        public static bool Fiscal_Print
        {
            get
            {
                return fiscal_print;
            }
            set
            {
                fiscal_print = value;
            }
        }

        public static DateTime Last_Answer_Barcode_Scaner
        {
            get
            {
                return last_answer_barcode_scaner;
            }
            set
            {
                last_answer_barcode_scaner = value;
            }
        }

        public static string NumberDecimalSeparator()
        {
            return System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        }

        public static string Barcode
        {
            get
            {
                return barcode;
            }
            set
            {
                lock (barcode)
                {
                    barcode = value;
                }
                //if (cc != null)
                //{
                //    cc.find_barcode_or_code_in_tovar(value);
                //}
            }
        }

        public static string Name_Com_Port
        {
            get
            {
                string result = "";
                NpgsqlConnection conn = null;
                try
                {
                    conn = MainStaticClass.NpgsqlConn();
                    conn.Open();
                    string query = "select name_com_port FROM constants";
                    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                    result = Convert.ToString(command.ExecuteScalar());
                    conn.Close();
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show(ex.Message, " Ошибка при работе с базой данных");
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
                return result;
            }
        }

        public static bool Use_Usb_to_Com_Barcode_Scaner
        {
            get
            {
                bool result = false;
                //NpgsqlConnection conn = null;
                //try
                //{
                //    conn = MainStaticClass.NpgsqlConn();
                //    conn.Open();
                //    string query = "select use_usb_to_com_barcode_scaner FROM constants";
                //    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                //    result = Convert.ToBoolean(command.ExecuteScalar());
                //    conn.Close();
                //}
                //catch (NpgsqlException ex)
                //{
                //    MessageBox.Show(ex.Message + " | " + ex.Detail, " Ошибка при работе с базой данных");
                //}
                //finally
                //{
                //    if (conn.State == ConnectionState.Open)
                //    {
                //        conn.Close();
                //    }
                //}
                return result;
            }

        }


        //public static string Barcode
        //{
        //    get
        //    {
        //        return barcode;
        //    }
        //    set
        //    {
        //        barcode = value;
        //    }
        //}


        public static bool Use_Fiscall_Print
        {
            get
            {
                bool result = true;
                //NpgsqlConnection conn = null;
                //try
                //{
                //    conn = MainStaticClass.NpgsqlConn();
                //    conn.Open();
                //    string query = "select use_fiscal_print FROM constants";
                //    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                //    result = Convert.ToBoolean(command.ExecuteScalar());
                //    conn.Close();
                //}
                //catch (NpgsqlException ex)
                //{
                //    MessageBox.Show(ex.Message + " | " + ex.Detail, "Ошибка при работе с базой данных");
                //}
                //finally
                //{
                //    if (conn.State == ConnectionState.Open)
                //    {
                //        conn.Close();
                //    }
                //}
                return result;
            }
        }




        public static bool Use_Text_Print
        {
            get
            {
                bool result = false;
                //NpgsqlConnection conn = null;
                //try
                //{
                //    conn = MainStaticClass.NpgsqlConn();
                //    conn.Open();
                //    string query = "select use_text_print FROM constants";
                //    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                //    result = Convert.ToBoolean(command.ExecuteScalar());
                //    conn.Close();
                //}
                //catch (NpgsqlException ex)
                //{
                //    MessageBox.Show(ex.Message + " | " + ex.Detail, "Ошибка при работе с базой данных");
                //}
                //finally
                //{
                //    if (conn.State == ConnectionState.Open)
                //    {
                //        conn.Close();
                //    }
                //}
                return result;
            }
        }

        public static int Width_Of_Symbols
        {
            get
            {
                int result = 0;
                NpgsqlConnection conn = null;
                try
                {
                    conn = MainStaticClass.NpgsqlConn();
                    conn.Open();
                    string query = "select width_of_symbols FROM constants";
                    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                    result = Convert.ToInt16(command.ExecuteScalar());
                    conn.Close();
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при работе с базой данных");
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
                return result;
            }
        }



        public static string Cash_Operator_Client_Code
        {
            get
            {
                return cash_operator_client_code;
            }
            set
            {
                cash_operator_client_code = value;
            }
        }

        public static string Cash_Operator
        {
            get
            {
                return cash_operator;
            }
            set
            {
                cash_operator = value;
            }
        }



        public static string Code_Shop
        {
            get
            {
                if (code_shop == "")
                {
                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string queryString = "SELECT code_shop FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(queryString, conn);
                        code_shop = command.ExecuteScalar().ToString().Trim();
                        conn.Close();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при получении названия магазина" + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }

                return code_shop;
            }
        }




        public static string Nick_Shop
        {
            get
            {
                if (nick_shop == "")
                {
                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string queryString = "SELECT nick_shop FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(queryString, conn);
                        nick_shop = (command.ExecuteScalar()).ToString().Trim();
                        conn.Close();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при получении названия магазина" + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                return nick_shop;
            }
        }


        //public static Main Main
        //{
        //    get
        //    {
        //        return main;
        //    }
        //    set
        //    {
        //        main = value;
        //    }
        //}

        //private static NpgsqlConnection npgsqlconn = null;

        public static NpgsqlConnection NpgsqlConn()
        {
            //return new NpgsqlConnection("Server=" + Cash8.MainStaticClass.ipAdrServer + ";Port=" + Cash8.MainStaticClass.portServer + ";User Id=" + Cash8.MainStaticClass.postgresUser + ";Password=" + Cash8.MainStaticClass.PasswordPostgres + ";Database=" + Cash8.MainStaticClass.DataBaseName + ";CommandTimeout=300;Pooling=false");

            //IPAddress ip = IPAddress.Parse("192.234.58.78.9");
            return new NpgsqlConnection("Server=" + MainStaticClass.ipAdrServer + ";Port=" + MainStaticClass.portServer + ";User Id=postgres" + ";Password=" + MainStaticClass.PasswordPostgres + ";Database=" + MainStaticClass.DataBaseName + ";CommandTimeout=300;Pooling=false;Timeout=10");
            //return new NpgsqlConnection("Host=" + MainStaticClass.ipAdrServer + ";Port=" + MainStaticClass.portServer + ";User Id=postgres" + ";Password=" + MainStaticClass.PasswordPostgres + ";Database=" + MainStaticClass.DataBaseName + ";CommandTimeout=300;Pooling=false;Timeout=10");

            //return new NpgsqlConnection("Server="+ IPAddress.Parse("192.168.0.107").Address.ToString() + ";Port=" + Cash8.MainStaticClass.portServer + ";User Id=postgres" + ";Password=" + Cash8.MainStaticClass.PasswordPostgres + ";Database=" + Cash8.MainStaticClass.DataBaseName + ";CommandTimeout=300;Pooling=false");
            //return new NpgsqlConnection("Server=" + Cash8.MainStaticClass.ipAdrServer + ";Port=" + Cash8.MainStaticClass.portServer + ";User Id=postgres" + ";Password=" + Cash8.MainStaticClass.PasswordPostgres + ";Database=Cash_Place_CH" + ";CommandTimeout=300;Pooling=false");
            //return new NpgsqlConnection("Server=127.0.0.1;Port=5432;User Id=postgres;Password=a123456789;Database=Cash_Place;CommandTimeout=60;");
            //return new NpgsqlConnection("Server=127.0.0.1;Port=5432;User Id=postgres;Password=1;Database=Cash_Place;CommandTimeout=60;");

        }
        public static Int16 Code_right_of_user
        {
            get
            {
                return code_right_of_user;
            }
            set
            {
                code_right_of_user = value;
            }

        }
        public static Int16 CashDeskNumber
        {
            get
            {
                if (cashDeskNumber == 0)
                {
                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    try
                    {
                        conn.Open();
                        string queryString = "SELECT cash_desk_number FROM constants";
                        NpgsqlCommand command = new NpgsqlCommand(queryString, conn);
                        cashDeskNumber = Convert.ToInt16(command.ExecuteScalar());
                        conn.Close();
                    }
                    catch (NpgsqlException ex)
                    {
                        MessageBox.Show("Ошибка при получении номера кассы" + ex.Message);
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }

                return cashDeskNumber;
            }
        }


        public static string Firma
        {
            get
            {
                //if (cashDeskNumber == 0)
                //{
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string queryString = "SELECT firma FROM constants";
                NpgsqlCommand command = new NpgsqlCommand(queryString, conn);
                //Int16 rez = 0;
                try
                {
                    firma = Convert.ToString(command.ExecuteScalar());
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show(" Ошибка при получении фирмы " + ex.Message);
                }
                conn.Close();
                //if (rez != 0)
                //{
                //    cashDeskNumber = rez;
                //}
                //}
                return firma;
            }
        }

        public static string INN
        {
            get
            {
                //if (cashDeskNumber == 0)
                //{
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string queryString = "SELECT inn FROM constants";
                NpgsqlCommand command = new NpgsqlCommand(queryString, conn);
                //Int16 rez = 0;
                try
                {
                    inn = Convert.ToString(command.ExecuteScalar());
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show(" Ошибка при получении инн " + ex.Message);
                }
                conn.Close();
                //if (rez != 0)
                //{
                //    cashDeskNumber = rez;
                //}
                //}
                return inn;
            }
        }


        public static string CodeKey
        {
            get
            {
                if (codekey == null)
                {
                    NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                    conn.Open();
                    object rez = null;
                    string queryString = "SELECT guidhash FROM urbd";
                    NpgsqlCommand command = new NpgsqlCommand(queryString, conn);
                    try
                    {
                        rez = command.ExecuteScalar();
                    }
                    catch
                    { }
                    conn.Close();
                    if (rez != null)
                    {
                        codekey = rez.ToString();
                    }
                }
                return codekey;
            }
        }

        //public static string CodeBase
        //{
        //    get
        //    {
        //        if (codebase == null)
        //        {
        //            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
        //            conn.Open();
        //            object rez = null;
        //            string queryString = "SELECT numbase FROM urbd";
        //            NpgsqlCommand command = new NpgsqlCommand(queryString, conn);
        //            try
        //            {
        //                rez = command.ExecuteScalar();
        //            }
        //            catch
        //            { }
        //            conn.Close();
        //            if (rez != null)
        //            {
        //                codebase = rez.ToString();
        //            }
        //        }
        //        return codebase;
        //    }
        //}

        public static string IPAdrServer
        {
            get
            {
                return ipAdrServer;
            }
            set
            {
                ipAdrServer = value;
            }
        }
        public static string DataBaseName
        {
            get
            {
                return dataBaseName;
            }
            set
            {
                dataBaseName = value;
            }
        }
        public static string PortServer
        {
            get
            {
                return portServer;
            }
            set
            {
                portServer = value;
            }
        }
        public static string PostgresUser
        {
            get
            {
                return postgresUser;
            }
            set
            {
                postgresUser = value;
            }
        }
        public static string PasswordPostgres
        {
            get
            {
                return passwordPostgres;
            }
            set
            {
                passwordPostgres = value;
            }
        }
        public static void EncryptData(string outName, string data)
        {
            using (FileStream fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fout.SetLength(0);

                // Используем нашу кодировку
                byte[] bin = System.Text.Encoding.UTF8.GetBytes(data);
                int totlen = bin.Length;

                using (RijndaelManaged rijndael = new RijndaelManaged())
                {
                    rijndael.Key = EncryptedSymmetricKey;
                    rijndael.IV = EncryptedSymmetricIV;

                    using (CryptoStream encStream = new CryptoStream(
                        fout,
                        rijndael.CreateEncryptor(),
                        CryptoStreamMode.Write))
                    {
                        encStream.Write(bin, 0, totlen);
                        encStream.FlushFinalBlock();
                    }
                }
            }
        }

        public static StringReader DecryptData(string inName)
        {
            using (FileStream fin = new FileStream(inName, FileMode.Open, FileAccess.Read))
            {
                using (RijndaelManaged rijndael = new RijndaelManaged())
                {
                    rijndael.Key = EncryptedSymmetricKey;
                    rijndael.IV = EncryptedSymmetricIV;

                    using (CryptoStream encStream = new CryptoStream(
                        fin,
                        rijndael.CreateDecryptor(),
                        CryptoStreamMode.Read))
                    {
                        // Читаем все байты
                        using (MemoryStream ms = new MemoryStream())
                        {
                            encStream.CopyTo(ms);
                            byte[] decryptedBytes = ms.ToArray();

                            // Преобразуем байты в строку с правильной кодировкой
                            string roundtrip = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                            return new StringReader(roundtrip);
                        }
                    }
                }
            }
        }


        public static void loadConfig(string fileConfig)
        {
            StringReader sr = MainStaticClass.DecryptData(fileConfig);
            string line = ""; int etap = 0;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == "[ip адрес сервера]")
                {
                    etap = 1;
                    continue;
                }
                if (line == "[имя базы данных]")
                {
                    etap = 2;
                    continue;
                }
                if (line == "[порт сервера]")
                {
                    etap = 3;
                    continue;
                }
                if (line == "[пароль postgres]")
                {
                    etap = 4;
                    continue;
                }
                if (line == "[пользователь postgres]")
                {
                    etap = 5;
                    continue;
                }

                if (etap == 1)
                {
                    MainStaticClass.IPAdrServer = line;
                    //this.Text += " | Server = " + line;
                    etap = 0;
                }
                if (etap == 2)
                {
                    MainStaticClass.DataBaseName = line;
                    //this.Text += " | DataBase = " + line;
                    etap = 0;
                }
                if (etap == 3)
                {
                    MainStaticClass.PortServer = line;
                    //this.Text += " | DataBase = " + line;
                    etap = 0;
                }
                if (etap == 4)
                {
                    MainStaticClass.PasswordPostgres = line;
                    //this.Text += " | DataBase = " + line;
                    etap = 0;
                }
                if (etap == 5)
                {
                    MainStaticClass.PostgresUser = line;
                    //this.Text += " | DataBase = " + line;
                    etap = 0;
                }
            }
        }
        //public static StringReader DecryptData(string inName)
        //{
        //    string roundtrip = "";

        //    using (FileStream fin = new FileStream(inName, FileMode.Open, FileAccess.Read))
        //    {
        //        // Для Rijndael в .NET Core/6+
        //        using (RijndaelManaged rijndael = new RijndaelManaged())
        //        {
        //            rijndael.Key = EncryptedSymmetricKey;
        //            rijndael.IV = EncryptedSymmetricIV;

        //            using (CryptoStream encStream = new CryptoStream(
        //                fin,
        //                rijndael.CreateDecryptor(),
        //                CryptoStreamMode.Read))
        //            {
        //                Encoding ascii = Encoding.UTF8;
        //                byte[] bin = new byte[100];
        //                int len;

        //                while ((len = encStream.Read(bin, 0, 100)) > 0)
        //                {
        //                    roundtrip += ascii.GetString(bin, 0, len);
        //                }
        //            }
        //        }
        //    }

        //    return new StringReader(roundtrip);
        //}

        public static bool exists_update_prorgam()
        {
            bool result = false;



            return result;
        }

        public static DataTable CreateDataTableForActions(int variant = 0)
        {
            DataTable dt = new DataTable();

            // Добавление столбцов с указанием типов данных
            dt.Columns.Add(new DataColumn("tovar_code", typeof(double)));
            dt.Columns.Add(new DataColumn("tovar_name", typeof(string)));
            dt.Columns.Add(new DataColumn("characteristic_code", typeof(string)));
            dt.Columns.Add(new DataColumn("characteristic_name", typeof(string)));
            dt.Columns.Add(new DataColumn("quantity", typeof(double)));
            dt.Columns.Add(new DataColumn("price", typeof(decimal)));
            dt.Columns.Add(new DataColumn("price_at_discount", typeof(decimal)));
            dt.Columns.Add(new DataColumn("sum_full", typeof(decimal)));
            dt.Columns.Add(new DataColumn("sum_at_discount", typeof(decimal)));
            dt.Columns.Add(new DataColumn("action", typeof(int)));
            dt.Columns.Add(new DataColumn("gift", typeof(int)));
            dt.Columns.Add(new DataColumn("action2", typeof(int)));
            dt.Columns.Add(new DataColumn("bonus_reg", typeof(int)));
            dt.Columns.Add(new DataColumn("bonus_action", typeof(int)));
            dt.Columns.Add(new DataColumn("bonus_action_b", typeof(int)));
            dt.Columns.Add(new DataColumn("marking", typeof(string)));
            dt.Columns.Add(new DataColumn("promo_description", typeof(string)));

            // Настройка видимости столбцов
            switch (variant)
            {
                case 0:
                case 2:
                    // Скрываем только изначально скрытые столбцы
                    dt.Columns["characteristic_code"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["characteristic_name"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["price"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["sum_full"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["action"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["gift"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["bonus_reg"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["bonus_action"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["bonus_action_b"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["marking"].ColumnMapping = MappingType.Hidden;
                    break;

                case 1:
                    // Скрываем все изначально скрытые столбцы + дополнительные
                    dt.Columns["characteristic_code"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["characteristic_name"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["price"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["sum_full"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["action"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["gift"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["bonus_reg"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["bonus_action"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["bonus_action_b"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["marking"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["promo_description"].ColumnMapping = MappingType.Hidden;
                    dt.Columns["action2"].ColumnMapping = MappingType.Hidden; // Дополнительно скрываем action2
                    break;
            }

            return dt;
        }

        public async static Task<bool> cdn_check(ProductData productData, string mark_str, Cash_check check)
        {
            bool result = true;
            string mark_str_cdn = "";

            if (productData.IsCDNCheck())
            {
                if (MainStaticClass.CashDeskNumber != 9)// && MainStaticClass.EnableCdnMarkers == 1
                {
                    if (MainStaticClass.CDN_Token == "")
                    {
                        await MessageBox.Show("В этой кассе не заполнен CDN токен, \r\n ПРОДАЖА ДАННОГО ТОВАРА НЕВОЗМОЖНА ! ", "Проверка CDN");
                        result = false;
                    }
                    else
                    {
                        CDN cdn = new CDN();
                        List<string> codes = new List<string>();
                        mark_str_cdn = mark_str.Replace("\u001d", @"\u001d");
                        codes.Add(mark_str_cdn);
                        mark_str_cdn = mark_str_cdn.Replace("'", "\'");
                        Dictionary<string, string> d_tovar = new Dictionary<string, string>();
                        //d_tovar[lvi.SubItems[1].Text] = lvi.SubItems[0].Text;
                        d_tovar[productData.Name] = productData.Code.ToString();                        
                        result = await cdn.cdn_check_marker_code(codes, mark_str, check.numdoc, check.request, mark_str_cdn, d_tovar, check, productData);
                    }
                }
            }

            return result;
        }


        public static bool CheckNewVersionProgramm()
        {
            bool result = false;
            //if (!MainStaticClass.service_is_worker())
            //{
            //    return result;
            //}

            //Cash8.DS.DS ds = MainStaticClass.get_ds();
            //ds.Timeout = 1000;

            ////Получить параметра для запроса на сервер 
            //string nick_shop = MainStaticClass.Nick_Shop.Trim();
            //if (nick_shop.Trim().Length == 0)
            //{
            //    return result;
            //}

            //string code_shop = MainStaticClass.Code_Shop.Trim();
            //if (code_shop.Trim().Length == 0)
            //{
            //    return result;
            //}

            //string count_day = CryptorEngine.get_count_day();
            //string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
            //string data = code_shop.Trim() + "|" + MainStaticClass.version() + "|" + code_shop.Trim();
            //string result_web_query = "";

            //try
            //{
            //    result_web_query = ds.ExistsUpdateProrgam(nick_shop, CryptorEngine.Encrypt(data, true, key), MainStaticClass.GetWorkSchema.ToString());
            //}
            //catch (Exception ex)
            //{
            //    //MessageBox.Show("Ошибка при получении версии программы на сервере " + ex.Message);
            //    return result;
            //}

            //if (result_web_query != "")
            //{
            //    result_web_query = CryptorEngine.Decrypt(result_web_query, true, key);

            //    if (MainStaticClass.version() == result_web_query)
            //    {
            //        //label_update.Text = " У вас установлена самая последняя версия программы ";
            //        return result;
            //    }
            //    else
            //    {
            //        //это старое решение по контролю версий
            //        string version = result_web_query;
            //        //это новое решение по контролю версий
            //        //здесь наверное надо установить проверку на больше меньше по версиям 
            //        Int64 local_version = Convert.ToInt64(MainStaticClass.version().Replace(".", ""));
            //        Int64 remote_version = Convert.ToInt64(result_web_query.Replace(".", ""));
            //        if (remote_version > local_version)
            //        {
            //            result = true;
            //        }
            //    }
            //}
            return result;
        }

        //public static void fiscall_print()
        //{
        //    Mini_FP_6 mini = new Mini_FP_6();            
        //    System.Threading.Thread t = new System.Threading.Thread(delegate() { mini.fiscall_print(MainStaticClass.listview_print,MainStaticClass.sum_print); });
        //    t.Start();
        //    t.Join();
        //}

        //public static void test_print()
        //{
        //    //Test_Mini_FP_6 myPrint = new Test_Mini_FP_6();
        //    //Thread t = new Thread(new ThreadStart(myPrint.Test));
        //    //t.Start();
        //    //t.Join();
        //}


    }
}
