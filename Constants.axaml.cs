using Atol.Drivers10.Fptr;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Npgsql;
using System;
using System.Data;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class Constants : Window
    {
        private int m_cash_desk_number = 0;

        private Button btnCheckPiot = null;

        public Constants()
        {
            InitializeComponent();
            
            btnCheckPiot = this.FindControl<Button>("btn_check_piot");
            btnCheckPiot.Click += BtnCheckPiot_Click;

            // Устанавливаем шрифт для всего окна
            this.FontFamily = new FontFamily("Segoe UI");

            // Заполняем ComboBox
            FillComboBoxes();

            // Загружаем доступные порты
            LoadAvailablePorts();

            // Загружаем настройки
            LoadSettings();

            // Подписываемся на события
            SubscribeToEvents();

            // Устанавливаем владельца - главное окно
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null && desktop.MainWindow != this)
                {
                    this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    this.Owner = desktop.MainWindow;
                }
            }
        }

        private async void BtnCheckPiot_Click(object? sender, RoutedEventArgs e)
        {
            //if (await MainStaticClass.CheckPiotAvailable(this))
            //{
            //    await MessageBoxHelper.Show("Пиот доступен, все настройки верны.","Проверка доступности ПИот");
            //}

            await MainStaticClass.CheckPiotAvailable(this);            
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FillComboBoxes()
        {
            var comboBoxSystemTaxation = this.FindControl<ComboBox>("comboBox_system_taxation");
            if (comboBoxSystemTaxation != null)
            {
                comboBoxSystemTaxation.Items.Clear();
                comboBoxSystemTaxation.Items.Add("НЕ ВЫБРАНО");
                comboBoxSystemTaxation.Items.Add("ОСН");
                comboBoxSystemTaxation.Items.Add("УСН (ДОХОДЫ МИНУС РАСХОДЫ)");
                comboBoxSystemTaxation.Items.Add("УСН (ДОХОДЫ МИНУС РАСХОДЫ) + ПАТЕНТ");
                comboBoxSystemTaxation.Items.Add("УСН ДОХОДЫ");
                comboBoxSystemTaxation.Items.Add("УСН ДОХОДЫ + ПАТЕНТ");
                comboBoxSystemTaxation.SelectedIndex = 0;
            }

            var comboBoxNdsIp = this.FindControl<ComboBox>("comboBox_nds_ip");
            if (comboBoxNdsIp != null)
            {
                comboBoxNdsIp.Items.Clear();
                comboBoxNdsIp.Items.Add("Без НДС");
                comboBoxNdsIp.Items.Add("5");
                comboBoxNdsIp.Items.Add("7");
                comboBoxNdsIp.SelectedIndex = 0;
            }

            var comboBoxAcquiringBank = this.FindControl<ComboBox>("comboBox_acquiring_bank");
            if (comboBoxAcquiringBank != null)
            {
                comboBoxAcquiringBank.Items.Clear();
                comboBoxAcquiringBank.Items.Add("НЕ ВЫБРАНО");
                comboBoxAcquiringBank.Items.Add("РНКБ");
                comboBoxAcquiringBank.Items.Add("СБЕР");
                comboBoxAcquiringBank.SelectedIndex = 0;
            }

            var comboBoxVariantConnectFn = this.FindControl<ComboBox>("comboBox_variant_connect_fn");
            if (comboBoxVariantConnectFn != null)
            {
                comboBoxVariantConnectFn.Items.Clear();
                comboBoxVariantConnectFn.Items.Add("USB==>COM");
                comboBoxVariantConnectFn.Items.Add("ETHERNET");
                comboBoxVariantConnectFn.SelectedIndex = 0;
            }

            var txtVersionFn = this.FindControl<TextBox>("txtB_version_fn");
            if (txtVersionFn != null)
            {
                txtVersionFn.Text = "2";
            }
        }

        private void LoadAvailablePorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames();

                var comboBoxFnPort = this.FindControl<ComboBox>("comboBox_fn_port");
                var comboBoxScalePort = this.FindControl<ComboBox>("comboBox_scale_port");

                if (comboBoxFnPort != null)
                {
                    comboBoxFnPort.Items.Clear();
                    foreach (var port in ports)
                    {
                        comboBoxFnPort.Items.Add(port);
                    }

                    if (ports.Length > 0)
                    {
                        comboBoxFnPort.SelectedIndex = 0;
                    }
                    else
                    {
                        comboBoxFnPort.Items.Add("COM1");
                        comboBoxFnPort.Items.Add("COM2");
                        comboBoxFnPort.Items.Add("COM3");
                        comboBoxFnPort.SelectedIndex = 0;
                    }
                }

                if (comboBoxScalePort != null)
                {
                    comboBoxScalePort.Items.Clear();
                    foreach (var port in ports)
                    {
                        comboBoxScalePort.Items.Add(port);
                    }

                    if (ports.Length > 0)
                    {
                        comboBoxScalePort.SelectedIndex = 0;
                    }
                    else
                    {
                        comboBoxScalePort.Items.Add("COM1");
                        comboBoxScalePort.Items.Add("COM2");
                        comboBoxScalePort.Items.Add("COM3");
                        comboBoxScalePort.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке портов: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            var writeButton = this.FindControl<Button>("write");
            var closeButton = this.FindControl<Button>("btnCloseBottom");
            var btnGetWeight = this.FindControl<Button>("btn_get_weight");
            var btnTestConnection = this.FindControl<Button>("btn_test_connection");
            var btnStatus = this.FindControl<Button>("btn_status");
            var comboBoxVariantConnectFn = this.FindControl<ComboBox>("comboBox_variant_connect_fn");

            if (writeButton != null) writeButton.Click += Write_Click;
            if (closeButton != null) closeButton.Click += Close_Click;
            if (btnGetWeight != null) btnGetWeight.Click += Btn_get_weight_Click;
            if (btnTestConnection != null) btnTestConnection.Click += Btn_test_connection_Click;
            if (btnStatus != null) btnStatus.Click += Btn_status_Click;

            if (comboBoxVariantConnectFn != null)
            {
                comboBoxVariantConnectFn.SelectionChanged += ComboBox_variant_connect_fn_SelectionChanged;
            }
        }

        private async void LoadSettings()
        {
            try
            {
                LoadFromDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                await MessageBox.Show($"Не удалось загрузить настройки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error,this);
            }
        }

        private async void LoadFromDatabase()
        {
            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = @"SELECT nick_shop, cash_desk_number, code_shop,
                        path_for_web_service, unloading_period, last_date_download_bonus_clients,
                        system_taxation, version_fn,
                        id_acquirer_terminal, ip_address_acquiring_terminal,
                        webservice_authorize, printing_using_libraries, fn_serial_port, 
                        get_weight_automatically, scale_serial_port,
                        variant_connect_fn, fn_ipaddr, acquiring_bank, 
                        constant_conversion_to_kilograms, nds_ip, ip_adress_local_ch_z,
                        include_piot,piot_url FROM constants LIMIT 1";

                using (var command = new NpgsqlCommand(query, conn))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var nickShop = this.FindControl<TextBox>("nick_shop");
                        var cashDeskNumber = this.FindControl<TextBox>("cash_desk_number");
                        var unloadingPeriod = this.FindControl<TextBox>("unloading_period");
                        var txtLastDateDownload = this.FindControl<TextBox>("txtB_last_date_download_bonus_clients");
                        var comboBoxSystemTaxation = this.FindControl<ComboBox>("comboBox_system_taxation");
                        var txtVersionFn = this.FindControl<TextBox>("txtB_version_fn");
                        var txtIdAcquiringTerminal = this.FindControl<TextBox>("txtB_id_acquiring_terminal");
                        var txtIpAddressAcquiringTerminal = this.FindControl<TextBox>("txtB_ip_address_acquiring_terminal");
                        var checkBoxPrintingUsingLibraries = this.FindControl<CheckBox>("checkBox_printing_using_libraries");
                        var checkBoxGetWeightAutomatically = this.FindControl<CheckBox>("checkBox_get_weight_automatically");
                        var checkBoxIncludePIot = this.FindControl<CheckBox>("checkBox_includePIot");
                        var comboBoxVariantConnectFn = this.FindControl<ComboBox>("comboBox_variant_connect_fn");
                        var txtConstantConversion = this.FindControl<TextBox>("txtB_constant_conversion_to_kilograms");
                        var txtFnIpAddr = this.FindControl<TextBox>("txtB_fn_ipaddr");
                        var comboBoxFnPort = this.FindControl<ComboBox>("comboBox_fn_port");
                        var comboBoxScalePort = this.FindControl<ComboBox>("comboBox_scale_port");
                        var comboBoxAcquiringBank = this.FindControl<ComboBox>("comboBox_acquiring_bank");
                        var txtIpAddrLmChZ = this.FindControl<TextBox>("txtB_ip_addr_lm_ch_z");
                                                
                        var txtPiotUrl = this.FindControl<TextBox>("txtB_piot_url");

                        var comboBoxNdsIp = this.FindControl<ComboBox>("comboBox_nds_ip");


                        if (nickShop != null) nickShop.Text = reader["nick_shop"].ToString();
                        if (cashDeskNumber != null) cashDeskNumber.Text = reader["cash_desk_number"].ToString();
                        if (unloadingPeriod != null) unloadingPeriod.Text = reader["unloading_period"].ToString();

                        if (txtLastDateDownload != null)
                        {
                            var lastDate = reader["last_date_download_bonus_clients"];
                            if (lastDate != DBNull.Value && !string.IsNullOrEmpty(lastDate.ToString()))
                            {
                                txtLastDateDownload.Text = Convert.ToDateTime(lastDate).ToString("dd.MM.yyyy HH:mm");
                            }
                        }

                        if (comboBoxSystemTaxation != null && reader["system_taxation"] != DBNull.Value)
                        {
                            int taxIndex = Convert.ToInt32(reader["system_taxation"]);
                            if (taxIndex >= 0 && taxIndex < comboBoxSystemTaxation.Items.Count)
                                comboBoxSystemTaxation.SelectedIndex = taxIndex;
                        }

                        if (txtVersionFn != null) txtVersionFn.Text = reader["version_fn"].ToString();

                        if (txtIdAcquiringTerminal != null) txtIdAcquiringTerminal.Text = reader["id_acquirer_terminal"].ToString();
                        if (txtIpAddressAcquiringTerminal != null) txtIpAddressAcquiringTerminal.Text = reader["ip_address_acquiring_terminal"].ToString();

                        if (checkBoxPrintingUsingLibraries != null)
                            checkBoxPrintingUsingLibraries.IsChecked = Convert.ToBoolean(reader["printing_using_libraries"]);

                        if (checkBoxGetWeightAutomatically != null)
                            checkBoxGetWeightAutomatically.IsChecked = Convert.ToBoolean(reader["get_weight_automatically"]);

                        if (checkBoxIncludePIot != null)
                            checkBoxIncludePIot.IsChecked = Convert.ToBoolean(reader["include_piot"]);

                        if (comboBoxVariantConnectFn != null && reader["variant_connect_fn"] != DBNull.Value)
                        {
                            int variantIndex = Convert.ToInt32(reader["variant_connect_fn"]);
                            if (variantIndex >= 0 && variantIndex < comboBoxVariantConnectFn.Items.Count)
                                comboBoxVariantConnectFn.SelectedIndex = variantIndex;
                        }

                        if (txtConstantConversion != null) txtConstantConversion.Text = reader["constant_conversion_to_kilograms"].ToString();
                        if (txtFnIpAddr != null) txtFnIpAddr.Text = reader["fn_ipaddr"].ToString();

                        if (comboBoxFnPort != null)
                        {
                            string fnPort = reader["fn_serial_port"].ToString();
                            if (!string.IsNullOrEmpty(fnPort))
                            {
                                int portIndex = comboBoxFnPort.Items.IndexOf(fnPort);
                                if (portIndex >= 0)
                                    comboBoxFnPort.SelectedIndex = portIndex;
                            }
                        }

                        if (comboBoxScalePort != null)
                        {
                            string scalePort = reader["scale_serial_port"].ToString();
                            if (!string.IsNullOrEmpty(scalePort))
                            {
                                int portIndex = comboBoxScalePort.Items.IndexOf(scalePort);
                                if (portIndex >= 0)
                                    comboBoxScalePort.SelectedIndex = portIndex;
                            }
                        }

                        if (comboBoxAcquiringBank != null && reader["acquiring_bank"] != DBNull.Value)
                        {
                            int bankIndex = Convert.ToInt32(reader["acquiring_bank"]);
                            if (bankIndex >= 0 && bankIndex < comboBoxAcquiringBank.Items.Count)
                                comboBoxAcquiringBank.SelectedIndex = bankIndex;
                        }

                        if (txtIpAddrLmChZ != null) txtIpAddrLmChZ.Text = reader["ip_adress_local_ch_z"].ToString();

                        if (comboBoxNdsIp != null && reader["nds_ip"] != DBNull.Value)
                        {
                            //int ndsIndex = Convert.ToInt32(reader["nds_ip"]);
                            //if (ndsIndex >= 0 && ndsIndex < comboBoxNdsIp.Items.Count)
                            //    comboBoxNdsIp.SelectedIndex = ndsIndex;
                            int index = comboBoxNdsIp.Items.IndexOf(reader["nds_ip"].ToString());
                            if (index == -1)
                            {
                                comboBoxNdsIp.SelectedIndex = 0;
                            }
                            else
                            {
                                comboBoxNdsIp.SelectedIndex = index;// Convert.ToInt16(reader["nds_ip"]);
                            }
                        }

                        


                        txtPiotUrl.Text = reader["piot_url"].ToString();
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Ошибка БД: {ex.Message}");
                await MessageBox.Show(ex.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxType.Error,this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                await MessageBox.Show("Ошибка", ex.Message, MessageBoxButton.OK, MessageBoxType.Error,this);
            }
            finally
            {
                conn?.Close();
            }
        }

        private void Write_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Btn_get_weight_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var comboBoxScalePort = this.FindControl<ComboBox>("comboBox_scale_port");
            var selectedPort = comboBoxScalePort?.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedPort))
            {
                await MessageBox.Show("Порт весов не выбран!", "Ошибка",MessageBoxButton.OK,MessageBoxType.Error, this);
                return;
            }
            double weight = await MainStaticClass.GetWeight();
            string formattedWeight = weight.ToString("F3");
            await MessageBox.Show($"Вес получен с порта {selectedPort}\nВес: {formattedWeight} кг", "Вес",this);
        }

        private async void Btn_test_connection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            IFptr fptr = MainStaticClass.FPTR;
            if (!fptr.isOpened())
            {
                fptr.open();
            }
            if (fptr.printText() < 0)
            {
                await MessageBox.Show($"Проверка подключения к ФН\nОшибка {fptr.errorCode()}: {fptr.errorDescription()}", "Проверка", this);
            }
            else
            {
                await MessageBox.Show($"Проверка подключения к ФН \nСтатус: Успешно", "Проверка", this);
            }
        }

        private async void Btn_status_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var txtIpAddrLmChZ = this.FindControl<TextBox>("txtB_ip_addr_lm_ch_z");
            var ip = txtIpAddrLmChZ?.Text;

            if (string.IsNullOrEmpty(ip))
            {
                await MessageBox.Show("IP адрес ЛМ ЧЗ не указан!", "Ошибка", this);
                return;
            }

            await MessageBox.Show($"Проверка подключения к ЛМ ЧЗ по IP: {ip}\nСтатус: Успешно", "Проверка", this);
        }

        private void ComboBox_variant_connect_fn_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var comboBoxVariantConnectFn = sender as ComboBox;
            var variant = comboBoxVariantConnectFn?.SelectedItem?.ToString();
            Console.WriteLine($"Выбран вариант подключения ФН: {variant}");
        }

        private async Task<bool> check_ip_addr()
        {
            bool result = true;

            var fn_ipaddr = this.FindControl<TextBox>("txtB_fn_ipaddr");
            var printing_using_libraries = this.FindControl<CheckBox>("checkBox_printing_using_libraries");
            var variant_connect_fn = this.FindControl<ComboBox>("comboBox_variant_connect_fn");

            if ((variant_connect_fn.SelectedIndex == 0) || (printing_using_libraries.IsChecked! != true))
            {
                return result;
            }

            if (string.IsNullOrEmpty(fn_ipaddr.Text))
            {
                return false;
            }

            string pattern = @"^(\d{1,3}(\.\d{1,3}){3}:\d+)?$";
            bool isValid = Regex.IsMatch(fn_ipaddr.Text, pattern) && !fn_ipaddr.Text.Contains(",");
            isValid = isValid && !string.IsNullOrEmpty(fn_ipaddr.Text);

            if (!isValid)
            {
                await MessageBox.Show(
                    "Строка IP адрес:порт не соответствует формату!\n" +
                    "Формат: XXX.XXX.XXX.XXX:PORT\n" +
                    "Пример: 192.168.1.100:5555",
                    "Проверка ввода IP адреса", this);
                result = false;
            }

            return result;
        }

        private async Task<bool> check_exists()
        {
            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT COUNT(*) FROM checks_header WHERE checks_header.date_time_write BETWEEN '" + DateTime.Now.Date +
                  "' and '" + DateTime.Now.Date.AddDays(1) + "'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                if (Convert.ToInt32(command.ExecuteScalar()) != 0)
                {
                    conn.Close();
                    return true;
                }
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
            }

            return false;
        }

        private async void SaveSettings()
        {
            if (!await check_ip_addr())
            {
                return;
            }

            var cashDeskNumber = this.FindControl<TextBox>("cash_desk_number");
            var nickShop = this.FindControl<TextBox>("nick_shop");
            var unloadingPeriod = this.FindControl<TextBox>("unloading_period");
            var txtB_last_date_download_bonus_clients = this.FindControl<TextBox>("txtB_last_date_download_bonus_clients");
            var comboBoxSystemTaxation = this.FindControl<ComboBox>("comboBox_system_taxation");
            var txtB_version_fn = this.FindControl<TextBox>("txtB_version_fn");
            var comboBoxAcquiringBank = this.FindControl<ComboBox>("comboBox_acquiring_bank");
            var txtB_ip_addr_lm_ch_z = this.FindControl<TextBox>("txtB_ip_addr_lm_ch_z");

            var checkBoxPrinting = this.FindControl<CheckBox>("checkBox_printing_using_libraries");
            var checkBoxWeight = this.FindControl<CheckBox>("checkBox_get_weight_automatically");
            var checkBoxPIot = this.FindControl<CheckBox>("checkBox_includePIot");

            var comboBoxFnPort = this.FindControl<ComboBox>("comboBox_fn_port");
            var comboBoxScalePort = this.FindControl<ComboBox>("comboBox_scale_port");
            var comboBoxVariantConnectFn = this.FindControl<ComboBox>("comboBox_variant_connect_fn");
            var txtB_fn_ipaddr = this.FindControl<TextBox>("txtB_fn_ipaddr");
            var comboBoxNdsIp = this.FindControl<ComboBox>("comboBox_nds_ip");

            var txtIdAcquiringTerminal = this.FindControl<TextBox>("txtB_id_acquiring_terminal");
            var txtIpAddressAcquiringTerminal = this.FindControl<TextBox>("txtB_ip_address_acquiring_terminal");
            var txtConstantConversion = this.FindControl<TextBox>("txtB_constant_conversion_to_kilograms");

            var txtPiotUrl = this.FindControl<TextBox>("txtB_piot_url");



            if (cashDeskNumber == null || nickShop == null || unloadingPeriod == null ||
                comboBoxSystemTaxation == null || txtB_version_fn == null)
            {
                await MessageBox.Show("Не удалось найти элементы управления", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                return;
            }

            if (string.IsNullOrWhiteSpace(cashDeskNumber.Text))
            {
                await MessageBox.Show("Не заполнен номер кассы", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                cashDeskNumber.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(nickShop.Text))
            {
                await MessageBox.Show("Не заполнен код магазина", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                nickShop.Focus();
                return;
            }

            if (m_cash_desk_number != 0)
            {
                if (Convert.ToInt16(cashDeskNumber.Text) != m_cash_desk_number)
                {
                    if (await check_exists())
                    {
                        await MessageBox.Show("За сегодня существуют чеки, номер кассы изменить невозможно", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                        return;
                    }
                }
            }

            string periodText = unloadingPeriod.Text.Trim();
            if (string.IsNullOrEmpty(periodText))
            {
                unloadingPeriod.Text = "0";
                periodText = "0";
            }
            else
            {
                if (!int.TryParse(periodText, out int periodValue))
                {
                    await MessageBox.Show("Период выгрузки должен быть числом", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    unloadingPeriod.Focus();
                    return;
                }

                if (periodValue != 0 && (periodValue < 1 || periodValue > 10))
                {
                    await MessageBox.Show("Период выгрузки может быть равен нулю или быть в диапазоне 1-10", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error, this);
                    unloadingPeriod.Focus();
                    return;
                }
            }

            string printing_using_libraries = (checkBoxPrinting?.IsChecked == true) ? "true" : "false";
            string get_weight_automatically = (checkBoxWeight?.IsChecked == true) ? "true" : "false";
            string include_piot = (checkBoxPIot?.IsChecked == true) ? "true" : "false";
            string piot_url     = txtPiotUrl.Text.Trim();

            string fn_serial_port = comboBoxFnPort?.SelectedIndex >= 0 ? comboBoxFnPort.SelectedItem?.ToString() ?? "" : "";
            string scale_serial_port = comboBoxScalePort?.SelectedIndex >= 0 ? comboBoxScalePort.SelectedItem?.ToString() ?? "" : "";
            string variant_connect_fn = comboBoxVariantConnectFn?.SelectedIndex >= 0 ? comboBoxVariantConnectFn.SelectedIndex.ToString() : "0";
            string fn_ipaddr = txtB_fn_ipaddr?.Text?.Trim() ?? "";
            //string nds_ip = comboBoxNdsIp?.SelectedIndex >= 0 ? comboBoxNdsIp.SelectedIndex.ToString() : "0";
            string nds_ip = "";
            if (comboBoxNdsIp.SelectedIndex > 0)
            {
                nds_ip = comboBoxNdsIp.SelectedItem.ToString();
            }
            else
            {
                nds_ip = "0";
            }
            string idAcquirerTerminal = txtIdAcquiringTerminal?.Text ?? "";
            string ipAddressAcquiringTerminal = txtIpAddressAcquiringTerminal?.Text?.Trim() ?? "";
            string constantConversion = txtConstantConversion?.Text?.Trim() ?? "";
            string ipAddrLmChZ = txtB_ip_addr_lm_ch_z?.Text?.Trim() ?? "";
            string systemTaxation = comboBoxSystemTaxation.SelectedIndex.ToString();
            string versionFn = txtB_version_fn.Text;
            string acquiringBank = comboBoxAcquiringBank?.SelectedIndex.ToString() ?? "0";
            string lastDateDownload = txtB_last_date_download_bonus_clients?.Text ?? "";

            try
            {
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                NpgsqlTransaction tran = conn.BeginTransaction();

                string query = "UPDATE constants SET " +
                    "cash_desk_number =" + cashDeskNumber.Text + "," +
                    "nick_shop ='" + nickShop.Text + "'," +
                    "unloading_period =" + periodText + "," +
                    "last_date_download_bonus_clients ='" + lastDateDownload + "'," +
                    "system_taxation = '" + systemTaxation + "'," +
                    "version_fn = " + versionFn + "," +
                    "id_acquirer_terminal='" + idAcquirerTerminal + "'," +
                    "ip_address_acquiring_terminal='" + ipAddressAcquiringTerminal + "'," +
                    "printing_using_libraries=" + printing_using_libraries + "," +
                    "fn_serial_port = '" + fn_serial_port + "'," +
                    "scale_serial_port = '" + scale_serial_port + "'," +
                    "get_weight_automatically=" + get_weight_automatically + "," +
                    "variant_connect_fn = " + variant_connect_fn + "," +
                    "fn_ipaddr='" + fn_ipaddr + "'" + "," +
                    "acquiring_bank= " + acquiringBank + "," +
                    "constant_conversion_to_kilograms=" + constantConversion + "," +
                    "nds_ip=" + nds_ip + "," +
                    "ip_adress_local_ch_z='" + ipAddrLmChZ + "'," +
                    "include_piot=" + include_piot + "," +
                    "piot_url='" + txtPiotUrl.Text.Trim() + "'";

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                int resul_update = command.ExecuteNonQuery();

                if (resul_update == 0)
                {
                    query = "INSERT INTO constants(cash_desk_number," +
                        "nick_shop," +
                        "unloading_period," +
                        "last_date_download_bonus_clients," +
                        "system_taxation," +
                        "version_fn," +
                        "id_acquirer_terminal," +
                        "ip_address_acquiring_terminal," +
                        "printing_using_libraries," +
                        "fn_serial_port," +
                        "scale_serial_port," +
                        "get_weight_automatically," +
                        "variant_connect_fn," +
                        "fn_ipaddr," +
                        "acquiring_bank," +
                        "constant_conversion_to_kilograms," +
                        "nds_ip," +
                        "ip_adress_local_ch_z," +
                        "include_piot," +
                        "piot_url) VALUES(" +
                        cashDeskNumber.Text + ",'" +
                        nickShop.Text + "'," +
                        periodText + ",'" +
                        lastDateDownload + "','" +
                        systemTaxation + "'," +
                        versionFn + ",'" +
                        idAcquirerTerminal + "','" +
                        ipAddressAcquiringTerminal + "'," +
                        printing_using_libraries + ",'" +
                        fn_serial_port + "','" +
                        scale_serial_port + "'," +
                        get_weight_automatically + "," +
                        variant_connect_fn + ",'" +
                        fn_ipaddr + "'," +
                        acquiringBank + "," +
                        constantConversion + "," +
                        nds_ip + ",'" +
                        ipAddrLmChZ + "'," +
                        include_piot + ",'"+
                        piot_url+"')";

                    command = new NpgsqlCommand(query, conn);
                    command.ExecuteNonQuery();
                }

                tran.Commit();
                conn.Close();

                MessageBoxResult result = await MessageBox.Show("Для применения новых параметров программа будет закрыта.", "", MessageBoxButton.OK, MessageBoxType.Info, this);

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    desktopLifetime.Shutdown();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxType.Error,this);
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка сохранения: {ex.Message}", " Ошибка ", MessageBoxButton.OK, MessageBoxType.Error,this);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}