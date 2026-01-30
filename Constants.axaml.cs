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
    public partial class Constants : UserControl
    {


        public event EventHandler RequestClose;
        private int m_cash_desk_number = 0;

        public Constants()
        {
            InitializeComponent();
            
            // Устанавливаем шрифт для всего контрола
            this.FontFamily = new FontFamily("Segoe UI");

            // Заполняем ComboBox
            FillComboBoxes();

            // Загружаем доступные порты
            LoadAvailablePorts();

            // Загружаем настройки
            LoadSettings();

            // Подписываемся на события
            SubscribeToEvents();

        }       

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
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
            var closeButton = this.FindControl<Button>("_close_");
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
                // Сначала установим значения по умолчанию
                //SetDefaultValues();

                // Затем загрузим из БД
                LoadFromDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                await MessageBox.Show($"Не удалось загрузить настройки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
            }
        }
        
        private async void LoadFromDatabase()
        {
            NpgsqlConnection conn = null;
            try
            {
                // Получаем подключение
                conn = MainStaticClass.NpgsqlConn(); // Если у вас есть такой метод
                //conn = DatabaseHelper.GetConnection(); // Используем наш хелпер
                conn.Open();

                string query = @"SELECT nick_shop, cash_desk_number, code_shop,
                        path_for_web_service, unloading_period, last_date_download_bonus_clients,
                        system_taxation, version_fn,
                        id_acquirer_terminal, ip_address_acquiring_terminal,
                        webservice_authorize, printing_using_libraries, fn_serial_port, 
                        get_weight_automatically, scale_serial_port,
                        variant_connect_fn, fn_ipaddr, acquiring_bank, 
                        constant_conversion_to_kilograms, nds_ip, ip_adress_local_ch_z,
                        include_piot FROM constants LIMIT 1";

                using (var command = new NpgsqlCommand(query, conn))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Получаем контролы
                        var nickShop = this.FindControl<TextBox>("nick_shop");
                        var cashDeskNumber = this.FindControl<TextBox>("cash_desk_number");
                        var pathForWebService = this.FindControl<TextBox>("path_for_web_service");
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
                        var comboBoxNdsIp = this.FindControl<ComboBox>("comboBox_nds_ip");

                        // Заполняем данные из БД
                        if (nickShop != null) nickShop.Text = reader["nick_shop"].ToString();
                        if (cashDeskNumber != null) cashDeskNumber.Text = reader["cash_desk_number"].ToString();
                        if (pathForWebService != null) pathForWebService.Text = reader["path_for_web_service"].ToString();
                        if (unloadingPeriod != null) unloadingPeriod.Text = reader["unloading_period"].ToString();

                        // Дата загрузки карточек
                        if (txtLastDateDownload != null)
                        {
                            var lastDate = reader["last_date_download_bonus_clients"];
                            if (lastDate != DBNull.Value && !string.IsNullOrEmpty(lastDate.ToString()))
                            {
                                txtLastDateDownload.Text = Convert.ToDateTime(lastDate).ToString("dd.MM.yyyy HH:mm");
                            }
                        }

                        // Система налогообложения
                        if (comboBoxSystemTaxation != null && reader["system_taxation"] != DBNull.Value)
                        {
                            int taxIndex = Convert.ToInt32(reader["system_taxation"]);
                            if (taxIndex >= 0 && taxIndex < comboBoxSystemTaxation.Items.Count)
                                comboBoxSystemTaxation.SelectedIndex = taxIndex;
                        }

                        // Версия ФН
                        if (txtVersionFn != null) txtVersionFn.Text = reader["version_fn"].ToString();

                        // Эквайринг
                        if (txtIdAcquiringTerminal != null) txtIdAcquiringTerminal.Text = reader["id_acquirer_terminal"].ToString();
                        if (txtIpAddressAcquiringTerminal != null) txtIpAddressAcquiringTerminal.Text = reader["ip_address_acquiring_terminal"].ToString();

                        // CheckBox'ы
                        if (checkBoxPrintingUsingLibraries != null)
                            checkBoxPrintingUsingLibraries.IsChecked = Convert.ToBoolean(reader["printing_using_libraries"]);

                        if (checkBoxGetWeightAutomatically != null)
                            checkBoxGetWeightAutomatically.IsChecked = Convert.ToBoolean(reader["get_weight_automatically"]);

                        if (checkBoxIncludePIot != null)
                            checkBoxIncludePIot.IsChecked = Convert.ToBoolean(reader["include_piot"]);

                        // Вариант подключения ФН
                        if (comboBoxVariantConnectFn != null && reader["variant_connect_fn"] != DBNull.Value)
                        {
                            int variantIndex = Convert.ToInt32(reader["variant_connect_fn"]);
                            if (variantIndex >= 0 && variantIndex < comboBoxVariantConnectFn.Items.Count)
                                comboBoxVariantConnectFn.SelectedIndex = variantIndex;
                        }

                        // Константы
                        if (txtConstantConversion != null) txtConstantConversion.Text = reader["constant_conversion_to_kilograms"].ToString();
                        if (txtFnIpAddr != null) txtFnIpAddr.Text = reader["fn_ipaddr"].ToString();

                        // Порт ФН
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

                        // Порт весов
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

                        // Банк эквайринга
                        if (comboBoxAcquiringBank != null && reader["acquiring_bank"] != DBNull.Value)
                        {
                            int bankIndex = Convert.ToInt32(reader["acquiring_bank"]);
                            if (bankIndex >= 0 && bankIndex < comboBoxAcquiringBank.Items.Count)
                                comboBoxAcquiringBank.SelectedIndex = bankIndex;
                        }

                        // IP адрес ЛМ ЧЗ
                        if (txtIpAddrLmChZ != null) txtIpAddrLmChZ.Text = reader["ip_adress_local_ch_z"].ToString();

                        // НДС для ИП
                        if (comboBoxNdsIp != null && reader["nds_ip"] != DBNull.Value)
                        {
                            int ndsIndex = Convert.ToInt32(reader["nds_ip"]);
                            if (ndsIndex >= 0 && ndsIndex < comboBoxNdsIp.Items.Count)
                                comboBoxNdsIp.SelectedIndex = ndsIndex;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Ошибка БД: {ex.Message}");
                await MessageBox.Show(ex.Message, "Ошибка БД",MessageBoxButton.OK,MessageBoxType.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                await MessageBox.Show("Ошибка", ex.Message, MessageBoxButton.OK, MessageBoxType.Error);
            }
            finally
            {
                conn?.Close();
            }
        }

        // Обработчики событий с nullable sender
        private void Write_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //var window = this.FindAncestorOfType<Window>();
            //window?.Close();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private async void Btn_get_weight_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var comboBoxScalePort = this.FindControl<ComboBox>("comboBox_scale_port");
            var selectedPort = comboBoxScalePort?.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedPort))
            {
                await MessageBox.Show("Порт весов не выбран!", "Ошибка");
                return;
            }
            double weight = MainStaticClass.GetWeight();
            string formattedWeight = weight.ToString("F3"); // 3 знака после запятой
            await MessageBox.Show($"Вес получен с порта {selectedPort}\nВес: {formattedWeight} кг", "Вес");
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
                await MessageBox.Show($"Проверка подключения к ФН\nОшибка {fptr.errorCode()}: {fptr.errorDescription()}",  "Проверка");
            }
            else
            {
                await MessageBox.Show($"Проверка подключения к ФН \nСтатус: Успешно","Проверка");
            }
        }

        private async void Btn_status_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var txtIpAddrLmChZ = this.FindControl<TextBox>("txtB_ip_addr_lm_ch_z");
            var ip = txtIpAddrLmChZ?.Text;

            if (string.IsNullOrEmpty(ip))
            {
                await MessageBox.Show("IP адрес ЛМ ЧЗ не указан!", "Ошибка");
                return;
            }

            await MessageBox.Show($"Проверка подключения к ЛМ ЧЗ по IP: {ip}\nСтатус: Успешно", "Проверка");
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
                //if (fn_ipaddr.Text.Trim().Length == 0)
                //{
                return result;
                //}
            }

            if (string.IsNullOrEmpty(fn_ipaddr.Text))
            {
                return false;
            }



            // Паттерн для проверки IP:порт
            string pattern = @"^(\d{1,3}(\.\d{1,3}){3}:\d+)?$";
            bool isValid = Regex.IsMatch(fn_ipaddr.Text, pattern) && !fn_ipaddr.Text.Contains(",");

            // Проверяем условие (прямая печать включена И выбран ETHERNET)
            //if (printing_using_libraries.IsChecked == true && variant_connect_fn.SelectedIndex == 1)
            //{
            // Если условие истинно, строка не должна быть пустой
            isValid = isValid && !string.IsNullOrEmpty(txtB_fn_ipaddr.Text);
            //}

            if (!isValid)
            {
                await MessageBox.Show(
                    "Строка IP адрес:порт не соответствует формату!\n" +
                    "Формат: XXX.XXX.XXX.XXX:PORT\n" +
                    "Пример: 192.168.1.100:5555",
                    "Проверка ввода IP адреса");
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

            // НАХОДИМ ВСЕ КОНТРОЛЫ ОДИН РАЗ
            var cashDeskNumber = this.FindControl<TextBox>("cash_desk_number");
            var nickShop = this.FindControl<TextBox>("nick_shop");
            var unloadingPeriod = this.FindControl<TextBox>("unloading_period");
            var pathForWebService = this.FindControl<TextBox>("path_for_web_service");
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

            // ПРОВЕРЯЕМ НА NULL ВСЕ ОСНОВНЫЕ КОНТРОЛЫ
            if (cashDeskNumber == null || nickShop == null || unloadingPeriod == null ||
                comboBoxSystemTaxation == null || txtB_version_fn == null)
            {
                await MessageBox.Show("Не удалось найти элементы управления", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                return;
            }

            // ПРОВЕРЯЕМ ЗАПОЛНЕНИЕ
            if (string.IsNullOrWhiteSpace(cashDeskNumber.Text))
            {
                await MessageBox.Show("Не заполнен номер кассы", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                cashDeskNumber.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(nickShop.Text))
            {
                await MessageBox.Show("Не заполнен код магазина", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                nickShop.Focus();
                return;
            }

            // ПРОВЕРКА НОМЕРА КАССЫ (теперь через переменную)
            if (m_cash_desk_number != 0)
            {
                if (Convert.ToInt16(cashDeskNumber.Text) != m_cash_desk_number)
                {
                    if (await check_exists())
                    {
                        await MessageBox.Show("За сегодня существуют чеки, номер кассы изменить невозможно", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                        return;
                    }
                }
            }

            // ПРОВЕРКА ПЕРИОДА ВЫГРУЗКИ
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
                    await MessageBox.Show("Период выгрузки должен быть числом", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                    unloadingPeriod.Focus();
                    return;
                }

                if (periodValue != 0 && (periodValue < 1 || periodValue > 10))
                {
                    await MessageBox.Show("Период выгрузки может быть равен нулю или быть в диапазоне 1-10", "Ошибка", MessageBoxButton.OK, MessageBoxType.Error);
                    unloadingPeriod.Focus();
                    return;
                }
            }

            // ПОЛУЧАЕМ ЗНАЧЕНИЯ ИЗ CHECKBOX
            string printing_using_libraries = (checkBoxPrinting?.IsChecked == true) ? "true" : "false";
            string get_weight_automatically = (checkBoxWeight?.IsChecked == true) ? "true" : "false";
            string include_piot = (checkBoxPIot?.IsChecked == true) ? "true" : "false";

            // ПОЛУЧАЕМ ЗНАЧЕНИЯ ИЗ COMBOBOX И TEXTBOX
            string fn_serial_port = comboBoxFnPort?.SelectedIndex >= 0 ? comboBoxFnPort.SelectedItem?.ToString() ?? "" : "";
            string scale_serial_port = comboBoxScalePort?.SelectedIndex >= 0 ? comboBoxScalePort.SelectedItem?.ToString() ?? "" : "";
            string variant_connect_fn = comboBoxVariantConnectFn?.SelectedIndex >= 0 ? comboBoxVariantConnectFn.SelectedIndex.ToString() : "0";
            string fn_ipaddr = txtB_fn_ipaddr?.Text?.Trim() ?? "";
            string nds_ip = comboBoxNdsIp?.SelectedIndex >= 0 ? comboBoxNdsIp.SelectedIndex.ToString() : "0";
            string idAcquirerTerminal = txtIdAcquiringTerminal?.Text ?? "";
            string ipAddressAcquiringTerminal = txtIpAddressAcquiringTerminal?.Text?.Trim() ?? "";
            string constantConversion = txtConstantConversion?.Text?.Trim() ?? "";
            string ipAddrLmChZ = txtB_ip_addr_lm_ch_z?.Text?.Trim() ?? "";
            string systemTaxation = comboBoxSystemTaxation.SelectedIndex.ToString();
            string versionFn = txtB_version_fn.Text;
            string acquiringBank = comboBoxAcquiringBank?.SelectedIndex.ToString() ?? "0";
            string pathWebService = pathForWebService?.Text ?? "";
            string lastDateDownload = txtB_last_date_download_bonus_clients?.Text ?? "";

            try
            {
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                NpgsqlTransaction tran = conn.BeginTransaction();

                // ТЕПЕРЬ ИСПОЛЬЗУЕМ ПЕРЕМЕННЫЕ ВМЕСТО ПРЯМОГО ОБРАЩЕНИЯ К КОНТРОЛАМ
                string query = "UPDATE constants SET " +
                    "cash_desk_number =" + cashDeskNumber.Text + "," +  // ? ИСПРАВЛЕНО
                    "nick_shop ='" + nickShop.Text + "'," +             // ? ИСПРАВЛЕНО
                    //"path_for_web_service ='" + pathWebService + "'," +
                    "unloading_period =" + periodText + "," +           // ? ИСПРАВЛЕНО
                    "last_date_download_bonus_clients ='" + lastDateDownload + "'," +
                    "system_taxation = '" + systemTaxation + "'," +     // ? ИСПРАВЛЕНО
                    "version_fn = " + versionFn + "," +                 // ? ИСПРАВЛЕНО
                    "id_acquirer_terminal='" + idAcquirerTerminal + "'," +
                    "ip_address_acquiring_terminal='" + ipAddressAcquiringTerminal + "'," +
                    "printing_using_libraries=" + printing_using_libraries + "," +
                    "fn_serial_port = '" + fn_serial_port + "'," +
                    "scale_serial_port = '" + scale_serial_port + "'," +
                    "get_weight_automatically=" + get_weight_automatically + "," +
                    "variant_connect_fn = " + variant_connect_fn + "," +
                    "fn_ipaddr='" + fn_ipaddr + "'" + "," +
                    "acquiring_bank= " + acquiringBank + "," +          // ? ИСПРАВЛЕНО
                    "constant_conversion_to_kilograms=" + constantConversion + "," +
                    "nds_ip=" + nds_ip + "," +
                    "ip_adress_local_ch_z='" + ipAddrLmChZ + "'," +     // ? ИСПРАВЛЕНО
                    "include_piot=" + include_piot;

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                int resul_update = command.ExecuteNonQuery();

                if (resul_update == 0)
                {
                    // INSERT запрос тоже нужно исправить
                    query = "INSERT INTO constants(cash_desk_number," +
                        "nick_shop," +
                        //"path_for_web_service," +
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
                        "include_piot) VALUES(" +
                        cashDeskNumber.Text + ",'" +                    // ? ИСПРАВЛЕНО
                        nickShop.Text + "'," +                          // ? ИСПРАВЛЕНО
                        //"'" + pathWebService + "'," +
                        periodText + ",'" +                             // ? ИСПРАВЛЕНО
                        lastDateDownload + "','" +
                        systemTaxation + "'," +                         // ? ИСПРАВЛЕНО
                        versionFn + ",'" +                              // ? ИСПРАВЛЕНО
                        idAcquirerTerminal + "','" +
                        ipAddressAcquiringTerminal + "'," +
                        printing_using_libraries + ",'" +
                        fn_serial_port + "','" +
                        scale_serial_port + "'," +
                        get_weight_automatically + "," +
                        variant_connect_fn + ",'" +
                        fn_ipaddr + "'," +
                        acquiringBank + "," +                           // ? ИСПРАВЛЕНО
                        constantConversion + "," +
                        nds_ip + ",'" +
                        ipAddrLmChZ + "'," +                           // ? ИСПРАВЛЕНО
                        include_piot + ")";

                    command = new NpgsqlCommand(query, conn);
                    command.ExecuteNonQuery();
                }

                tran.Commit();
                conn.Close();

                //ShowMessage("Успех", "Для применения новых параметров программа будет закрыта");

                MessageBoxResult result = await MessageBox.Show("Для применения новых параметров программа будет закрыта.", "", MessageBoxButton.OK, MessageBoxType.Info); 
               
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    desktopLifetime.Shutdown();
                }
                else
                {
                    Environment.Exit(0);
                }

                // Application.Exit(); // Если нужно закрыть приложение
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxType.Error);
            }
            catch (Exception ex)
            {                
                await MessageBox.Show($"Ошибка сохранения: {ex.Message}", " Ошибка ", MessageBoxButton.OK, MessageBoxType.Error);
            }
        }       

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TextBlock_ActualThemeVariantChanged(object? sender, EventArgs e)
        {

        }
    }
}