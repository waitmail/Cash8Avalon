using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Cash8Avalon.Cash_check;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Cash8Avalon
{
    public partial class Cash_check : ControlBase
    {
        // Класс Requisite1260 теперь внутри класса Cash_check
        public class Requisite1260
        {
            public string? req1262 { get; set; }
            public string? req1263 { get; set; }
            public string? req1264 { get; set; }
            public string? req1265 { get; set; }
        }

        // Модели данных
        public Dictionary<string, uint> cdn_markers_result_check = new Dictionary<string, uint>();
        public Dictionary<string, Requisite1260> verifyCDN = new Dictionary<string, Requisite1260>();
        public bool enable_delete = false;
        public string recharge_note = "";
        public List<int> action_num_doc = new List<int>();

        private bool selection_goods = false;
        public bool have_action = false;
        private StringBuilder print_string = new StringBuilder();
        public int to_print_certainly = 0;
        public int to_print_certainly_p = 0;
        public bool closing = true;
        public bool inpun_action_barcode = false;
        public ArrayList action_barcode_list = new ArrayList();
        public ArrayList action_barcode_bonus_list = new ArrayList();
        private double discount = 0;
        public Int64 numdoc = 0;
        private bool inpun_client_barcode = false;
        public bool IsNewCheck = true;
        public string date_time_write = "";
        public string p_sum_pay = "";
        public string p_sum_doc = "";
        public string p_remainder = "";
        public string p_discount = "0";
        Thread workerThread = null;
        private DateTime start_action = DateTime.Now;

        private DataTable table = new DataTable();

        public string cashier = "";
        public int added_bonus_when_replacing_card = 0;
        public decimal bonuses_it_is_written_off = 0;
        public int bonus_total_centr = 0;
        public int return_bonus = 0;
        public int client_barcode_scanned = 0;
        public bool it_is_possible_to_write_off_bonuses = false;
        public string id_transaction = "";
        private string id_transaction_sale = "";
        public int bonuses_it_is_counted = 0;
        //public string qr_code = "";
        public string id_sale = "";
        public string phone_client = "";
        private string code_bonus_card = "";
        public string spendAllowed = "";
        public string message_processing = "";
        public bool change_bonus_card = false;
        public string id_transaction_terminal = "";
        public string code_authorization_terminal = "";
        public string sale_id_transaction_terminal = "";
        public string sale_code_authorization_terminal = "";
        public DateTime sale_date;
        public int print_to_button = 0;
        public string guid = "";
        private string guid1 = "";
        public string guid_sales = "";
        public string tax_order = "";
        public bool external_fix = false;
        public double sale_non_cash_money = 0;
        public bool payment_by_sbp = false;
        public bool payment_by_sbp_sales = false;

        List<int> qr_code_lenght = new List<int>();

        // Событие для закрытия формы
        public event EventHandler Closed;

        // Контролы
        public ComboBox CheckType { get; private set; }
        public TextBox NumCash { get; private set; }
        public TextBox User { get; private set; }
        public TextBox Client { get; private set; }
        public TextBox ClientBarcodeOrPhone { get; private set; }
        public TextBox NumSales { get; private set; }
        public TextBox InputSearchProduct { get; private set; }

        // Контролы TabItems
        private TabItem _tabProducts;
        private TabItem _tabCertificates;

        // Поля для Grid товаров
        private ScrollViewer _productsScrollViewer;
        private Grid _productsTableGrid;
        private int _productsCurrentRow = 1;

        // Поля для Grid сертификатов
        private ScrollViewer _certificatesScrollViewer;
        private Grid _certificatesTableGrid;
        private int _certificatesCurrentRow = 1;

        // Константы для визуальных эффектов
        private static readonly IBrush QUANTITY_INCREASE_COLOR = Brushes.LimeGreen;
        private static readonly IBrush QUANTITY_DECREASE_COLOR = Brushes.Gold; // Желтый для уменьшения
        private static readonly IBrush QUANTITY_MINIMUM_COLOR = Brushes.Red; // Красный для мин. количества
        private static readonly IBrush QUANTITY_ERROR_COLOR = Brushes.OrangeRed;

        // Классы данных для товаров
        public class ProductItem
        {
            public int Code { get; set; }
            public string Tovar { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal PriceAtDiscount { get; set; }
            public decimal Sum { get; set; }
            public decimal SumAtDiscount { get; set; }
            public int Action { get; set; }
            public int Gift { get; set; }
            public int Action2 { get; set; }
            public string Mark { get; set; } = "0";
        }

        // Классы данных для сертификатов
        public class CertificateItem
        {
            public string Code { get; set; } = string.Empty;
            public string Certificate { get; set; } = string.Empty;
            public decimal Nominal { get; set; }
            public string Barcode { get; set; } = string.Empty;
        }

        // Коллекции данных для товаров
        private List<ProductItem> _productsData = new List<ProductItem>();

        // Коллекции данных для сертификатов
        private List<CertificateItem> _certificatesData = new List<CertificateItem>();

        // Поля для работы с выделением товаров
        private Border _selectedProductRowBorder;
        private int _selectedProductRowIndex = -1;
        private static readonly IBrush PRODUCT_SELECTED_BACKGROUND = Brushes.LightSkyBlue;
        private static readonly IBrush PRODUCT_SELECTED_BORDER = Brushes.DodgerBlue;

        public Cash_check()
        {
            Console.WriteLine("=== Конструктор Cash_check начат ===");

            try
            {
                // 1. Загружаем XAML
                InitializeComponent();

                // 2. Проверяем контролы из XAML
                CheckControls();

                // 3. Создаем Grid программно
                CreateAllGridsProgrammatically();

                // 4. Подписываемся на глобальные события клавиатуры
                this.AddHandler(KeyDownEvent, OnGlobalKeyDownForProducts, RoutingStrategies.Tunnel);

                // 5. Дополнительный глобальный обработчик для F7
                this.AddHandler(KeyDownEvent, OnGlobalKeyDownForForm, RoutingStrategies.Tunnel);

                Console.WriteLine("✓ Конструктор завершен успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ОШИБКА в конструкторе: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("=== Конструктор Cash_check завершен ===");
        }



        public void OnFormLoaded()
        {
            Console.WriteLine("Форма чека загружена и данные инициализированы");
            
            // 3. Инициализируем данные формы
            InitializeFormData();

            // 4. Отладочная информация
            DebugGridInfo();
        }
        
        // Новый глобальный обработчик для всей формы
        private void OnGlobalKeyDownForForm(object sender, KeyEventArgs e)
        {
            try
            {
                // Можно добавить и другие глобальные горячие клавиши
                switch (e.Key)
                {
                    case Key.F7:
                        // Обновить данные
                        e.Handled = true;
                        InputSearchProduct.Focus();
                        break;

                    //case Key.F5:
                    //    // Обновить данные
                    //    e.Handled = true;
                    //    RefreshData();
                    //    break;

                    //case Key.F12:
                    //    // Открыть справку
                    //    e.Handled = true;
                    //    OpenHelp();
                    //    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в OnGlobalKeyDownForForm: {ex.Message}");
            }
        }
              

        private void CheckControls()
        {
            try
            {
                Console.WriteLine("=== Проверка и заполнение контролов ===");

                CheckType = GetRequiredControl<ComboBox>("check_type");

                if (CheckType != null)
                {
                    CheckType.SelectionChanged += CheckType_SelectionChanged;
                }

                Client = GetRequiredControl<TextBox>("client");
                NumCash = GetRequiredControl<TextBox>("num_cash");
                User = GetRequiredControl<TextBox>("user");
                ClientBarcodeOrPhone = GetRequiredControl<TextBox>("client_barcode");
                NumSales = GetRequiredControl<TextBox>("txtB_num_sales");
                InputSearchProduct = GetRequiredControl<TextBox>("txtB_search_product");

                // Подписка на события поиска товара
                if (InputSearchProduct != null)
                {
                    InputSearchProduct.KeyDown += InputSearchProduct_KeyDown;
                    // Или: InputSearchProduct.KeyDown += OnSearchProductKeyDown;
                }

                _tabProducts = GetRequiredControl<TabItem>("tabProducts");
                _tabCertificates = GetRequiredControl<TabItem>("tabCertificates");

                Console.WriteLine("✓ Все основные контролы проверены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке контролов: {ex.Message}");
            }
        }

        private void InputSearchProduct_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Console.WriteLine("Enter нажат в поле поиска товара");
                    e.Handled = true;
                    find_product();
                    break;
            }
        }

        private async void find_product()
        {
            string search_param = InputSearchProduct.Text.Trim();
            InputSearchProduct.Text = string.Empty;
            int Length = search_param.Length;
            string gtin = string.Empty;
            if (Length == 0)
            {
                return;
            }

            if (Length > 13)//попробуем получить gtin
            {
                search_param = CleanQrCodeString(search_param);
                if (!await ValidateQrCodeAsync(search_param))//выполнить все проверки по длине и структуре кода маркировки
                {
                    return;
                }

                if (Length > 18)
                {
                    if ((search_param.Substring(0, 2) == "01") && (search_param.Substring(16, 2) == "21"))
                    {
                        gtin = search_param.Substring(3, 13);
                        find_barcode_or_code_in_tovar_new(gtin, search_param);
                    }
                    else
                    {
                        gtin = search_param.Substring(1, 13);
                        find_barcode_or_code_in_tovar_new(gtin, search_param);
                    }
                }
            }
            else //Length <= 13)
            {
                find_barcode_or_code_in_tovar_new(search_param, "");
            }
        }

        private async Task ShowTovarNotFoundWindow()
        {
            var t_n_f = new Tovar_Not_Found();

            // Получаем текущее окно через TopLevel
            var topLevel = TopLevel.GetTopLevel(this);

            if (topLevel is Window ownerWindow)
            {
                await t_n_f.ShowDialog(ownerWindow);
            }
            else
            {
                // Если TopLevel не вернул окно, ищем через Application
                var app = Application.Current;
                if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var owner = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                    await t_n_f.ShowDialog(owner);
                }
            }
        }

        public async void find_barcode_or_code_in_tovar_new(string barcode, string marking_code)
        {

            //DateTime start = DateTime.Now;
            //Повторная проверка если документ не новый или уже вызвано окно оплаты подбор товара не работает
            if (!IsNewCheck)
            {
                return;
            }

            if (this.check_type.SelectedIndex > 0)
            {
                if (barcode.Trim().Length > 6)
                {
                    await MessageBox.Show("Поиск товара прерван ! Длина кода превышает 6 символов ");
                    return;
                }
            }
            else if (this.check_type.SelectedIndex < 0)
            {
                await MessageBox.Show(" Произошла ошибка при получении типа чека, чек будет закрыт попробуйте создать его заново.", "Проверки при получении типа чека.");
                MainStaticClass.WriteRecordErrorLog("Произошла ошибка при получении типа чека", "find_barcode_or_code_in_tovar_new", numdoc, MainStaticClass.CashDeskNumber, "Произошла ошибка при получении типа чека, чек будет закрыт попробуйте создать его заново");
                var window = this.FindAncestorOfType<Window>();
                if (window != null)
                {
                    window.Close();
                }

            }

            MainStaticClass.write_event_in_log("Попытка добавить новый товар в чек " + barcode, "Документ чек", numdoc.ToString());

            //Здесь проверка штрихкода на весовой товар с весов ****************************************
            bool ProductFromScales = false;
            double WeightFromScales = 0;
            if (barcode.Length == 13)
            {
                if (barcode.Substring(0, 2) == "23")//Это штрихкод с весов 
                {
                    WeightFromScales = Math.Round(double.Parse(barcode.Substring(8, 4)) / 1000, 3, MidpointRounding.AwayFromZero);//Получить вес в кг с весов
                    barcode = Convert.ToInt32(barcode.Substring(2, 6)).ToString();//Здесь переопределяем штрихкод для дальнейшего стандартного поведения 
                    ProductFromScales = true;
                }
            }
            //****************************************

            ProductData productData = new ProductData(0, "", 0, ProductFlags.None);

            if (InventoryManager.completeDictionaryProductData)
            {
                productData = InventoryManager.GetItem(Convert.ToInt64(barcode));
            }
            else
            {
                productData = GetProductDataInDB(barcode);
            }


            if (productData.IsEmpty())
            {
                last_tovar.Text = barcode;
                await ShowTovarNotFoundWindow();
                return;
            }

            // Проверяем маркированный товар
            if (productData.IsMarked())
            {
                bool error = false;
                if (marking_code == "")
                {                    
                    var inputActionBarcode = new InputActionBarcode();
                    inputActionBarcode.call_type = 6;
                    inputActionBarcode.caller = this;

                    // Получаем родительское окно
                    var owner = TopLevel.GetTopLevel(this) as Window;

                    // Проверяем, что получили валидное окно
                    if (owner == null)
                    {
                        // Если не удалось получить окно, обрабатываем как ошибку
                        error = true;
                        return; // или continue, в зависимости от контекста
                    }

                    var result = await inputActionBarcode.ShowDialog<bool?>(owner);

                    if (result == null || result == false)
                    {
                        if (!productData.IsRefusalMarking())//Не пропускать проверку 
                        {
                            error = true;
                        }
                    }
                    else
                    {
                        // В Avalonia свойство EnteredBarcode может быть пустым после закрытия окна
                        // Проверяем, что получили валидный код
                        marking_code = inputActionBarcode.EnteredBarcode ?? string.Empty;
                        
                        if (!qr_code_lenght.Contains(marking_code.Length))
                        {
                            // Используем то же окно owner для MessageBox
                            await MessageBox.Show(
                                marking_code + "\r\n Ваш код маркировки имеет длину " +
                                marking_code.Length.ToString() +
                                " символов при этом он не входит в допустимый диапазон ",
                                "Проверка qr-кода",
                                MessageBoxButton.OK,
                                MessageBoxType.Error
                            );
                            error = true;
                        }
                        else if (string.IsNullOrEmpty(marking_code))
                        {
                            error = true;
                        }
                    }
                }
                
                if (error)
                {
                    last_tovar.Text = barcode;
                    await ShowTovarNotFoundWindow();
                    return;
                }
                //если все ок тогда проверяем код маркировки в ФР, пока без пиот или сдн, позже добавлю
                marking_code = add_gs1(marking_code);//Обязательно добавляем разделитель групп 
                if (!string.IsNullOrEmpty(marking_code))
                {
                    bool markingExists = CheckMarkingExists(marking_code);
                    if (markingExists)
                    {
                        await MessageBox.Show("Маркировка этого товара уже добавлена в чек. Нельзя добавить одну и ту же маркировку дважды.", "Проверка маркировки",MessageBoxButton.OK,MessageBoxType.Error);
                        return;
                    }
                }

                //ПОЗЖЕ будет понятно ЧТО включать 

                //if (productData.IsCDNCheck())
                //{
                //    if (MainStaticClass.IncludedPiot)
                //    {
                //        //if (ValidatePiotAgainstFiscalData())
                //        //{
                //        if (!MainStaticClass.piot_cdn_check(productData, mark_str, lvi, this))
                //        {
                //            last_tovar.Text = barcode;
                //            Tovar_Not_Found t_n_f = new Tovar_Not_Found();
                //            t_n_f.textBox1.Text = "Код маркировки не прошел проверку в ПИот";
                //            t_n_f.textBox1.Font = new Font("Microsoft Sans Serif", 22);
                //            //t_n_f.label1.Text = " Возможно, что проблемы с доступом к CDN серверам.";
                //            t_n_f.ShowDialog();
                //            t_n_f.Dispose();
                //            return;
                //        }
                //        else
                //        {
                //            cdn_vrifyed = true;
                //        }
                //        //}
                //    }
                //    else
                //    {
                //        if (!MainStaticClass.cdn_check(productData, mark_str, lvi, this))
                //        {
                //            last_tovar.Text = barcode;
                //            Tovar_Not_Found t_n_f = new Tovar_Not_Found();
                //            t_n_f.textBox1.Text = "Код маркировки не прошел проверку на CDN";
                //            t_n_f.textBox1.Font = new Font("Microsoft Sans Serif", 22);
                //            t_n_f.label1.Text = " Возможно, что проблемы с доступом к CDN серверам.";
                //            t_n_f.ShowDialog();
                //            t_n_f.Dispose();
                //            return;
                //        }
                //        else
                //        {
                //            cdn_vrifyed = true;
                //        }
                //    }
                //}


                byte[] textAsBytes = Encoding.Default.GetBytes(marking_code);
                string imc = Convert.ToBase64String(textAsBytes);

                PrintingUsingLibraries printingUsingLibraries = new PrintingUsingLibraries();
                if (!printingUsingLibraries.check_marking_code(marking_code, this.numdoc.ToString(), ref this.cdn_markers_result_check, this.check_type.SelectedIndex))
                {
                    error = true;
                    last_tovar.Text = barcode;
                    await ShowTovarNotFoundWindow();
                    return;
                }

            }

            if (this._productsTableGrid.RowDefinitions.Count - 1 > 70)//Превышен предел строк
            {

                await MessageBox.Show("В одном чеке может быть максимум 70 строк.\r\n Tсли у покупателя еще есть тоовары продавайте их в другом чеке.", "Проверка количества строк",MessageBoxButton.OK,MessageBoxType.Error);

                last_tovar.Text = barcode;
                await ShowTovarNotFoundWindow();
                return;                
            }
            
            //Надо проверить может уже сертификат есть в чеке      
            if (productData.isCertificate())
            {
                bool find_sertificate = CheckCertificateExists(barcode);

                if (find_sertificate)
                {
                    await MessageBox.Show("Этот сертификат уже добавлен в чек","Проверка сертификата",MessageBoxButton.OK,MessageBoxType.Error);
                    return;
                }
            }           

            //КОНЕЦ Надо проверить может уже сертификат есть в чеке                                    

            if (!productData.IsFractional())
            {
                if (WeightFromScales != 0)
                {
                    await MessageBox.Show("Товар с кодом/штрихкодком " + barcode + " не является весовым и в чек добавлен не будет ","Проверка ввода товара",MessageBoxButton.OK,MessageBoxType.Error);
                    return;
                }
            }  

            //Проверка по сертификату
            if (productData.isCertificate())
            {
                if (!check_sertificate_for_sales(barcode))
                {
                    return;
                }
                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 60000;
                //Получить параметр для запроса на сервер 
                string nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0)
                {
                    await MessageBox.Show(" Не удалось получить название магазина ");
                    return;
                }
                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0)
                {
                    await MessageBox.Show(" Не удалось получить код магазина ");
                    return;
                }
                string count_day = CryptorEngine.get_count_day();
                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();

                string sertificate_code = barcode;
                string encrypt_data = CryptorEngine.Encrypt(sertificate_code, true, key);
                string status = "";
                try
                {
                    status = ds.GetStatusSertificat(MainStaticClass.Nick_Shop, encrypt_data, MainStaticClass.GetWorkSchema.ToString());
                }
                catch (Exception ex)
                {
                    await MessageBox.Show(" Отсутствует доступ в интернет с кассы или же на сервере, который обрабатывает сертификаты.", "Проверка сертификата", MessageBoxButton.OK, MessageBoxType.Error);
                    MainStaticClass.WriteRecordErrorLog(ex, numdoc, MainStaticClass.CashDeskNumber, "Провекрка активации сертификата при продаже");
                    return;
                }
                if (status == "-1")
                {
                    await MessageBox.Show("Произошли ошибки на сервере при работе с сертификатами","Проверка сертификата", MessageBoxButton.OK, MessageBoxType.Error);
                    MainStaticClass.WriteRecordErrorLog("Произошли ошибки на сервере при работе с сертификатами", "find_barcode_or_code_in_tovar_new", numdoc, MainStaticClass.CashDeskNumber, "Провекрка активации сертификата при продаже");
                    return;
                }
                else
                {
                    string decrypt_data = CryptorEngine.Decrypt(status, true, key);
                    if (decrypt_data == "1")
                    {
                        await MessageBox.Show("Сертификат уже активирован","Проверка сертификата",MessageBoxButton.OK,MessageBoxType.Error);
                        return;
                    }
                }

            }

            ProductItem existingProduct = null;

            if ((!productData.IsMarked()) && (!productData.isCertificate()) && (!productData.IsFractional())) // && ((MainStaticClass.GetWorkSchema == 1) || (MainStaticClass.GetWorkSchema == 3)))
            {                
                existingProduct = _productsData.FirstOrDefault(p => p.Code == productData.Code);
            }

            if (existingProduct != null)
            {
                // Если товар уже есть в чеке, увеличиваем количество
                existingProduct.Quantity++;
                RecalculateProductSums(existingProduct);

                // Получаем индекс товара в коллекции
                int productIndex = _productsData.IndexOf(existingProduct);

                // Обновляем строку в Grid
                UpdateProductRowInGrid(productIndex);
                UpdateTotalSum();

                // Показываем эффект увеличения
                ShowQuantityEffect(productIndex, true);

                // ВЫДЕЛЯЕМ СТРОКУ С УВЕЛИЧЕННЫМ ТОВАРОМ
                SelectProductRow(productIndex);

                return;
            }

            // Создаем новый товар для добавления
            var productItem = new ProductItem
            {
                Code = (int)productData.Code,
                Tovar = productData.GetName(),
                Quantity = 1,
                Price = productData.Price,
                PriceAtDiscount = productData.Price, // По умолчанию без скидки
                Action = 0,
                Gift = 0,
                Action2 = 0,
                Mark = !string.IsNullOrEmpty(marking_code) ? marking_code : "0"
            };

            // Пересчитываем суммы
            RecalculateProductSums(productItem);
            
            last_tovar.Text = productData.GetName();

            // Добавляем в коллекцию данных
            _productsData.Add(productItem);

            // Обновляем Grid (оптимизированная версия - добавляем только одну строку)
            await AddSingleProductToGrid(productItem);

            // Обновляем общую сумму
            UpdateTotalSum();

            // Выделяем добавленную строку
            SelectProductRow(_productsData.Count - 1);

        }


        ///// <summary>
        ///// Добавить разделитель групп
        ///// </summary>
        ///// <param name="mark_str"></param>
        ///// <returns></returns>
        private string add_gs1(string mark_str)
        {
            string GS1 = Char.ConvertFromUtf32(29);
            int length = mark_str.Length;

            if (mark_str.Contains(GS1))
            {
                return mark_str;
            }

            switch (length)
            {
                case 30:
                    mark_str = mark_str.Insert(24, GS1);
                    break;

                case 31:
                    mark_str = mark_str.Insert(25, GS1);
                    break;

                case 32:
                    mark_str = mark_str.Insert(26, GS1);
                    break;

                //case 36:
                //    mark_str = mark_str.Insert(30, GS1);
                //    break;

                case 37 when mark_str.Substring(16, 2) == "21":
                    mark_str = mark_str.Insert(31, GS1);
                    break;

                case 40:
                    mark_str = mark_str.Insert(24, GS1);
                    mark_str = mark_str.Insert(31, GS1);
                    break;

                case 41:
                    mark_str = mark_str.Insert(25, GS1);
                    mark_str = mark_str.Insert(36, GS1);
                    break;

                case 76:
                    mark_str = mark_str.Insert(24, GS1);
                    mark_str = mark_str.Insert(31, GS1);
                    break;

                case 83:
                case 115:
                case 127:
                    mark_str = mark_str.Insert(31, GS1);
                    mark_str = mark_str.Insert(38, GS1);
                    break;
            }

            return mark_str;
        }

        // Метод проверки наличия маркировки в чеке
        private bool CheckMarkingExists(string markingCode)
        {
            try
            {
                // Ищем в данных товаров по полю Mark
                return _productsData.Any(p => p.Mark == markingCode);

                // Или более строгая проверка:
                // return _productsData.Any(p => 
                //     !string.IsNullOrEmpty(p.Mark) && 
                //     p.Mark.Trim() == markingCode.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке маркировки: {ex.Message}");
                return false;
            }
        }


        private bool CheckCertificateExists(string barcode)
        {
            try
            {
                // Ищем в данных товаров
                var existingCertificate = _productsData.FirstOrDefault(p =>
                    p.Mark == barcode || p.Mark.Contains(barcode));

                if (existingCertificate != null)
                {
                    return true;
                }

                // Также проверяем в Grid (на всякий случай)
                foreach (Control child in _productsTableGrid.Children)
                {
                    if (child is TextBlock textBlock &&
                        Grid.GetColumn(textBlock) == 10) // Колонка "Марк" (индекс 10)
                    {
                        if (textBlock.Text == barcode || textBlock.Text.Contains(barcode))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке сертификата: {ex.Message}");
                return false;
            }
        }

        private void ScrollToProductRow(int gridRowIndex)
        {
            try
            {
                if (_productsScrollViewer == null)
                    return;

                // Проверяем, это последняя строка?
                bool isLastRow = (gridRowIndex == _productsCurrentRow - 1);

                if (isLastRow)
                {
                    // Для последней строки - прокрутка до конца
                    double maxScroll = _productsScrollViewer.Extent.Height - _productsScrollViewer.Viewport.Height;
                    if (maxScroll > 0)
                    {
                        _productsScrollViewer.Offset = new Vector(0, maxScroll);
                    }
                }
                else
                {
                    // Для остальных строк - обычный расчет
                    double rowHeight = 40;
                    double targetPosition = (gridRowIndex - 1) * rowHeight;
                    _productsScrollViewer.Offset = new Vector(0, targetPosition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при прокрутке: {ex.Message}");
            }
        }

        private async Task AddSingleProductToGrid(ProductItem productItem)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // Добавляем новую строку в Grid
                    int gridRowIndex = _productsCurrentRow;

                    // Добавляем RowDefinition
                    _productsTableGrid.RowDefinitions.Add(new RowDefinition(40, GridUnitType.Pixel));

                    // Создаем фон строки
                    var rowBackground = (_productsCurrentRow % 2 == 0) ? Brushes.White : Brushes.AliceBlue;

                    var rowBorder = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Background = rowBackground,
                        Tag = _productsData.IndexOf(productItem) // Сохраняем индекс данных
                    };

                    // Подписываемся на события
                    rowBorder.PointerPressed += OnProductRowPointerPressed;

                    // Устанавливаем позицию
                    Grid.SetColumnSpan(rowBorder, 11);
                    Grid.SetRow(rowBorder, gridRowIndex);
                    _productsTableGrid.Children.Add(rowBorder);

                    // Добавляем ячейки с данными
                    AddCell(_productsTableGrid, 0, gridRowIndex, productItem.Code.ToString(), HorizontalAlignment.Right);
                    AddCellWithWrap(_productsTableGrid, 1, gridRowIndex, productItem.Tovar, HorizontalAlignment.Left);
                    AddCell(_productsTableGrid, 2, gridRowIndex, productItem.Quantity.ToString(), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 3, gridRowIndex, productItem.Price.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 4, gridRowIndex, productItem.PriceAtDiscount.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 5, gridRowIndex, productItem.Sum.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 6, gridRowIndex, productItem.SumAtDiscount.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 7, gridRowIndex, productItem.Action.ToString(), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 8, gridRowIndex, productItem.Gift.ToString(), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 9, gridRowIndex, productItem.Action2.ToString(), HorizontalAlignment.Right);
                    AddCell(_productsTableGrid, 10, gridRowIndex, productItem.Mark, HorizontalAlignment.Center);

                    // Увеличиваем счетчик строк
                    _productsCurrentRow++;

                    // Прокручиваем к добавленной строке
                    ScrollToProductRow(gridRowIndex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при добавлении строки в Grid: {ex.Message}");
                }
            });
        }



        /// <summary>
        /// Проверка по продаваемому сертификату на 
        /// то что он только сегодня поступил в магазин 
        /// в качестве оплаты 
        /// </summary>
        /// <param name="tovar_code"></param>
        /// <returns></returns>
        private bool check_sertificate_for_sales(string barcode)
        {

            bool result = true;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlParameter parameter = null;
            try
            {
                conn.Open();             
                string query = " SELECT checks_header.guid,checks_table.item_marker " +
                               " FROM public.checks_header " +
                               " LEFT JOIN public.checks_table ON checks_header.guid = checks_table.guid " +
                               " WHERE checks_table.item_marker = @barcode AND sum_at_a_discount< 0  " +
                               " AND date_time_start between @date_start AND @date_finish;";

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                parameter = new NpgsqlParameter("@date_start", DateTime.Now.Date.ToString("yyyy-MM-dd"));
                command.Parameters.Add(parameter);
                parameter = new NpgsqlParameter("@date_finish", DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd"));
                command.Parameters.Add(parameter);
                parameter = new NpgsqlParameter("@barcode", barcode);
                command.Parameters.Add(parameter);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    MessageBox.Show(" Вы пытаетесь продать сертификат который был сегодня получен в качестве оплаты на этой кассе. ", " Проверка сертификатов ", MessageBoxButton.OK, MessageBoxType.Error);
                    MainStaticClass.write_event_in_log(" Вы пытаетесь продать сертификат который был сегодня получен в качестве оплаты на этой кассе. ", "Документ", numdoc.ToString());
                    result = false;
                }
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при проверке сертификата" + ex.Message, " Проверка сертификатов ",MessageBoxButton.OK,MessageBoxType.Error);
                result = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при проверке сертификата" + ex.Message, " Проверка сертификатов ", MessageBoxButton.OK, MessageBoxType.Error);
                result = false;
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



        public ProductData GetProductDataInDB(string barcode)
        {
            NpgsqlConnection conn = null;
            ProductData productData = new ProductData(0, "", 0, ProductFlags.None);

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;
                if (barcode.Length > 6)
                {
                    command.CommandText = "select tovar.code,tovar.name,tovar.retail_price,tovar.its_certificate,tovar.its_marked,tovar.cdn_check,tovar.fractional,tovar.refusal_of_marking,tovar.rr_not_control_owner " +
                        " from  barcode left join tovar ON barcode.tovar_code=tovar.code " +
                    //" left join characteristic ON tovar.code = characteristic.tovar_code " +
                    //" where barcode='" + barcode + "' AND its_deleted=0  AND (retail_price<>0 OR characteristic.retail_price_characteristic<>0)";
                    " where barcode='" + barcode + "' AND its_deleted=0  AND retail_price<>0";

                }
                else
                {
                    command.CommandText = "select tovar.code,tovar.name,tovar.retail_price,tovar.its_certificate,tovar.its_marked,tovar.cdn_check,tovar.fractional,tovar.refusal_of_marking,tovar.rr_not_control_owner " +
                        " FROM tovar left join characteristic  ON tovar.code = characteristic.tovar_code where tovar.its_deleted=0 AND tovar.its_certificate=0 " +
                        //"AND  (retail_price<>0 OR characteristic.retail_price_characteristic<>0) " +
                        "AND  retail_price<>0 " +
                        " AND tovar.code='" + barcode + "'";
                }
                NpgsqlDataReader reader = command.ExecuteReader();
                //bool find = false;
                while (reader.Read())
                {
                    //find = true;
                    Int64 code = Convert.ToInt64(reader["code"].ToString());
                    string name = reader["name"].ToString();
                    decimal price = Convert.ToDecimal(reader["retail_price"]);

                    ProductFlags flags = ProductFlags.None;
                    if (Convert.ToBoolean(reader["its_certificate"])) flags |= ProductFlags.Certificate;
                    if (Convert.ToBoolean(reader["its_marked"])) flags |= ProductFlags.Marked;
                    if (Convert.ToBoolean(reader["refusal_of_marking"])) flags |= ProductFlags.RefusalMarking;
                    if (Convert.ToBoolean(reader["rr_not_control_owner"])) flags |= ProductFlags.RrNotControlOwner;
                    //if (Convert.ToBoolean(reader["refusal_of_marking"]))
                    //{
                    //    // Сбрасываем флаг Marked, если он был установлен ранее
                    //    flags &= ~ProductFlags.Marked;
                    //}
                    //else if (Convert.ToBoolean(reader["its_marked"]))
                    //{
                    //    // Устанавливаем флаг Marked, если refusal_of_marking == false и its_marked == true
                    //    flags |= ProductFlags.Marked;
                    //}


                    if (Convert.ToBoolean(reader["cdn_check"])) flags |= ProductFlags.CDNCheck;
                    if (Convert.ToBoolean(reader["fractional"])) flags |= ProductFlags.Fractional;
                    productData = new ProductData(code, name, price, flags);
                }
                //if (!find)
                //{
                //    last_tovar.Text = barcode;
                //    Tovar_Not_Found t_n_f = new Tovar_Not_Found();
                //    t_n_f.ShowDialog();
                //    t_n_f.Dispose();
                //}
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(@"Произошла ошибка при получении товара по коду\штрихкоду " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Произошла ошибка при получении товара по коду\штрихкоду " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }

            }

            return productData;
        }


        private async Task<bool> ValidateQrCodeAsync(string cleanedCode)
        {
            // Проверка HTTP в очищенной строке
            if (cleanedCode.Length >= 4 &&
                cleanedCode.Substring(0, 4).IndexOf("HTTP", StringComparison.OrdinalIgnoreCase) != -1)
            {
                await MessageBox.Show("Содержит HTTP", "Ошибка");
                return false;
            }

            // Проверка длины очищенной строки
            if (!qr_code_lenght.Contains(cleanedCode.Length))
            {
                await MessageBox.Show($"Длина {cleanedCode.Length} недопустима", "Ошибка");
                return false;
            }

            return true;
        }

        private string CleanQrCodeString(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                    return input;

                string result = input;
                int numPos = result.IndexOf("\\");

                // Пока находим \u001d - удаляем
                while (numPos > 0 && result.Length > numPos + 5)
                {
                    if (result.Substring(numPos + 1, 5) == "u001d")
                    {
                        // Удаляем \u001d
                        result = result.Substring(0, numPos) + result.Substring(numPos + 6);
                        numPos = result.IndexOf("\\");
                    }
                    else
                    {
                        break;
                    }
                }

                Console.WriteLine($"Очистка QR-кода: '{input}' -> '{result}'");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при очистке QR-кода: {ex.Message}");
                return input; // Возвращаем оригинал при ошибке
            }
        }

        private void CheckType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Console.WriteLine($"CheckType_SelectionChanged: SelectedIndex={CheckType.SelectedIndex}");

                if (CheckType == null || NumSales == null)
                {
                    Console.WriteLine("⚠ CheckType или NumSales не инициализированы");
                    return;
                }

                // Если индекс больше 0 (не "Продажа")
                if (CheckType.SelectedIndex > 0)
                {
                    // Делаем CheckType недоступным
                    CheckType.IsEnabled = false;
                    Console.WriteLine("✓ CheckType отключен (IsEnabled = false)");

                    // Делаем NumSales видимым
                    NumSales.IsVisible = true;
                    Console.WriteLine("✓ NumSales включен (IsVisible = true)");

                    btn_fill_on_sales.IsVisible = true;
                    Console.WriteLine("✓ btn_fill_on_sales включен (IsVisible = true)");
                }
                else
                {
                    // Если индекс 0 ("Продажа") - возвращаем исходное состояние
                    CheckType.IsEnabled = true;
                    NumSales.IsVisible = false;
                    Console.WriteLine("✓ CheckType включен, NumSales скрыт");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в CheckType_SelectionChanged: {ex.Message}");
            }
        }

        public void OpenCheck(string dateTimeWrite)
        {
            this.date_time_write = dateTimeWrite;
            Console.WriteLine($"Открытие чека: {dateTimeWrite}");

            // Если форма уже создана, загружаем данные из БД
            if (!string.IsNullOrEmpty(dateTimeWrite))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        ToOpenTheWrittenDownDocument();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при загрузке данных чека: {ex.Message}");
                    }
                });
            }
        }

        private void CreateAllGridsProgrammatically()
        {
            try
            {
                Console.WriteLine("=== Создание всех Grid программно ===");

                // 1. Создаем Grid для товаров
                CreateProductsGrid();

                // 2. Создаем Grid для сертификатов
                CreateCertificatesGrid();

                Console.WriteLine("✓ Все Grid созданы программно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании Grid: {ex.Message}");
            }
        }

        #region Grid товаров

        private void CreateProductsGrid()
        {
            try
            {
                Console.WriteLine("Создание Grid для товаров...");

                // Создаем ScrollViewer
                _productsScrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White,
                    Focusable = true
                };

                // Подписываемся на события ScrollViewer
                _productsScrollViewer.PointerPressed += OnProductsScrollViewerPointerPressed;

                // Создаем Grid для таблицы
                _productsTableGrid = new Grid
                {
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // Подписываемся на события Grid
                _productsTableGrid.PointerPressed += OnProductsTableGridPointerPressed;

                // Для процентного распределения (Star)
                var columnDefinitions = new[]
                {
                    new ColumnDefinition(1, GridUnitType.Star),      // Код (10%)
                    new ColumnDefinition(4, GridUnitType.Star),      // Наименование (40%)
                    new ColumnDefinition(1, GridUnitType.Star),      // Кол-во (10%)
                    new ColumnDefinition(1.2, GridUnitType.Star),    // Цена (12%)
                    new ColumnDefinition(1.2, GridUnitType.Star),    // Цена со ск. (12%)
                    new ColumnDefinition(1.2, GridUnitType.Star),    // Сумма (12%)
                    new ColumnDefinition(1.2, GridUnitType.Star),    // Сумма со ск. (12%)
                    new ColumnDefinition(0.9, GridUnitType.Star),      // Акция (9%)
                    new ColumnDefinition(0.9, GridUnitType.Star),      // Подарок (9%)
                    new ColumnDefinition(0.9, GridUnitType.Star),       // Акция2 (9%)
                    new ColumnDefinition(0.3, GridUnitType.Star)       // Марк (3%)
                };

                var columnHeaders = new[] { "Код", "Наименование", "Кол-во", "Цена", "Цена ск.", "Сумма", "Сумма ск.", "Акция", "Подарок", "Акция2", "Марк" };

                foreach (var colDef in columnDefinitions)
                {
                    _productsTableGrid.ColumnDefinitions.Add(colDef);
                }

                // Создаем строку заголовков (строка 0)
                _productsTableGrid.RowDefinitions.Add(new RowDefinition(35, GridUnitType.Pixel));
                CreateHeaderRow(_productsTableGrid, columnHeaders, Brushes.LightBlue);

                // Добавляем тестовые данные
                AddProductsGridRows(_productsTableGrid, ref _productsCurrentRow, _productsData);

                // Добавляем Grid в ScrollViewer
                _productsScrollViewer.Content = _productsTableGrid;

                // Устанавливаем ScrollViewer как содержимое TabItem
                _tabProducts.Content = _productsScrollViewer;

                Console.WriteLine($"✓ Grid для товаров создан. Записей: {_productsData.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании Grid товаров: {ex.Message}");
            }
        }

        #endregion

        #region Grid сертификатов

        private void CreateCertificatesGrid()
        {
            try
            {
                Console.WriteLine("Создание Grid для сертификатов...");

                // Создаем ScrollViewer
                _certificatesScrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White,
                    Focusable = true
                };

                // Создаем Grid для таблицы
                _certificatesTableGrid = new Grid
                {
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // Определяем колонки Grid (4 колонки)
                var columnWidths = new[] { 100, 400, 100, 100 };
                var columnHeaders = new[] { "Код", "Сертификат", "Номинал", "Штрихкод" };

                foreach (var width in columnWidths)
                {
                    _certificatesTableGrid.ColumnDefinitions.Add(new ColumnDefinition(width, GridUnitType.Pixel));
                }

                // Создаем строку заголовков (строка 0)
                _certificatesTableGrid.RowDefinitions.Add(new RowDefinition(35, GridUnitType.Pixel));
                CreateHeaderRow(_certificatesTableGrid, columnHeaders, Brushes.LightBlue);

                // Добавляем тестовые данные
                AddCertificatesGridRows(_certificatesTableGrid, ref _certificatesCurrentRow, _certificatesData);

                // Добавляем Grid в ScrollViewer
                _certificatesScrollViewer.Content = _certificatesTableGrid;

                // Устанавливаем ScrollViewer как содержимое TabItem
                _tabCertificates.Content = _certificatesScrollViewer;

                Console.WriteLine($"✓ Grid для сертификатов создан. Записей: {_certificatesData.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании Grid сертификатов: {ex.Message}");
            }
        }

        #endregion

        #region Общие методы для всех Grid

        // Универсальный метод для создания заголовков
        private void CreateHeaderRow(Grid grid, string[] headers, IBrush headerBackground)
        {
            try
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    var headerBorder = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 1, 2),
                        Background = headerBackground,
                        Child = new TextBlock
                        {
                            Text = headers[i],
                            FontWeight = FontWeight.Bold,
                            FontSize = 12,
                            Margin = new Thickness(5, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.DarkBlue
                        }
                    };

                    Grid.SetColumn(headerBorder, i);
                    Grid.SetRow(headerBorder, 0);
                    grid.Children.Add(headerBorder);
                }

                Console.WriteLine($"✓ Заголовки созданы: {headers.Length} колонок");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при создании заголовков: {ex.Message}");
            }
        }

        private void AddProductsGridRows(Grid grid, ref int currentRow, List<ProductItem> data)
        {
            try
            {
                Console.WriteLine($"Добавление данных в Grid товаров: {data.Count} записей");

                for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
                {
                    var product = data[rowIndex];
                    var gridRowIndex = currentRow;

                    // Увеличиваем высоту строки для переноса текста, но не слишком много
                    int rowHeight = 40;
                    grid.RowDefinitions.Add(new RowDefinition(rowHeight, GridUnitType.Pixel));

                    var rowBackground = (currentRow % 2 == 0) ? Brushes.White : Brushes.AliceBlue;

                    var rowBorder = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Background = rowBackground,
                        Tag = rowIndex // Сохраняем индекс данных для выделения
                    };

                    // Подписываемся на события клика
                    rowBorder.PointerPressed += OnProductRowPointerPressed;

                    // ИЗМЕНЕНИЕ: Устанавливаем Span на 11 колонок вместо 10
                    Grid.SetColumnSpan(rowBorder, 11);
                    Grid.SetRow(rowBorder, gridRowIndex);
                    grid.Children.Add(rowBorder);

                    // Добавляем ячейки с данными
                    AddCell(grid, 0, gridRowIndex, product.Code.ToString(), HorizontalAlignment.Right);

                    // Колонка с наименованием товара - с переносом
                    AddCellWithWrap(grid, 1, gridRowIndex, product.Tovar, HorizontalAlignment.Left);

                    // Остальные колонки без переноса
                    AddCell(grid, 2, gridRowIndex, product.Quantity.ToString(), HorizontalAlignment.Right);
                    AddCell(grid, 3, gridRowIndex, product.Price.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(grid, 4, gridRowIndex, product.PriceAtDiscount.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(grid, 5, gridRowIndex, product.Sum.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(grid, 6, gridRowIndex, product.SumAtDiscount.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(grid, 7, gridRowIndex, product.Action.ToString(), HorizontalAlignment.Right);
                    AddCell(grid, 8, gridRowIndex, product.Gift.ToString(), HorizontalAlignment.Right);
                    AddCell(grid, 9, gridRowIndex, product.Action2.ToString(), HorizontalAlignment.Right);                    
                    AddCell(grid, 10, gridRowIndex, product.Mark, HorizontalAlignment.Center); // Пока пустая строка

                    currentRow++;
                }

                Console.WriteLine($"✓ Добавлено {data.Count} записей в Grid товаров");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении данных в Grid товаров: {ex.Message}");
            }
        }

        // Универсальный метод для добавления ячейки (с гарантиями стиля)
        private void AddCell(Grid grid, int column, int row, string text, HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 12,
                FontWeight = FontWeight.Normal, // Явно указываем обычный вес
                Foreground = Brushes.Black,     // Явно указываем цвет
                Background = Brushes.Transparent,
                IsHitTestVisible = false
            };

            Grid.SetColumn(textBlock, column);
            Grid.SetRow(textBlock, row);
            grid.Children.Add(textBlock);
        }

        // Отдельный метод для ячеек с переносом текста (с гарантиями стиля)
        private void AddCellWithWrap(Grid grid, int column, int row, string text,
                                     HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            string cleanText = text?.Trim() ?? string.Empty;

            var textBlock = new TextBlock
            {
                Text = cleanText,
                Margin = new Thickness(5, 2, 5, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                FontWeight = FontWeight.Normal, // Явно указываем обычный вес
                Foreground = Brushes.Black,     // Явно указываем цвет
                Background = Brushes.Transparent,
                MaxHeight = 50,
                IsHitTestVisible = false
            };

            Grid.SetColumn(textBlock, column);
            Grid.SetRow(textBlock, row);
            grid.Children.Add(textBlock);
        }

        // Метод для добавления строк в Grid сертификатов
        private void AddCertificatesGridRows(Grid grid, ref int currentRow, List<CertificateItem> data)
        {
            try
            {
                Console.WriteLine($"Добавление данных в Grid сертификатов: {data.Count} записей");

                foreach (var item in data)
                {
                    grid.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel));

                    var rowBackground = (currentRow % 2 == 0) ? Brushes.White : Brushes.Honeydew;

                    var rowBorder = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Background = rowBackground
                    };

                    Grid.SetColumnSpan(rowBorder, 4);
                    Grid.SetRow(rowBorder, currentRow);
                    grid.Children.Add(rowBorder);

                    // Добавляем ячейки с данными
                    AddCell(grid, 0, currentRow, item.Code, HorizontalAlignment.Left);
                    AddCell(grid, 1, currentRow, item.Certificate, HorizontalAlignment.Left);
                    AddCell(grid, 2, currentRow, item.Nominal.ToString("N2"), HorizontalAlignment.Right);
                    AddCell(grid, 3, currentRow, item.Barcode, HorizontalAlignment.Left);

                    currentRow++;
                }

                Console.WriteLine($"✓ Добавлено {data.Count} записей в Grid сертификатов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении данных в Grid сертификатов: {ex.Message}");
            }
        }

        #endregion

        #region Обработчики событий и выделение для Grid товаров

        // Метод для выделения строки в Grid товаров
        //private void SelectProductRow(int rowIndex)
        //{
        //    try
        //    {
        //        if (rowIndex < 0 || rowIndex >= _productsData.Count)
        //        {
        //            ClearProductSelection();
        //            return;
        //        }

        //        // Снимаем предыдущее выделение
        //        ClearProductSelection();

        //        // Устанавливаем новое выделение
        //        _selectedProductRowIndex = rowIndex;

        //        // Находим Border строки (Grid.Row = rowIndex + 1, так как строка 0 - заголовки)
        //        int gridRowIndex = rowIndex + 1;

        //        foreach (Control child in _productsTableGrid.Children)
        //        {
        //            if (child is Border border && Grid.GetRow(border) == gridRowIndex)
        //            {
        //                // Меняем стиль выделенной строки
        //                border.Background = PRODUCT_SELECTED_BACKGROUND;
        //                border.BorderBrush = PRODUCT_SELECTED_BORDER;
        //                border.BorderThickness = new Thickness(2);

        //                _selectedProductRowBorder = border;
        //                break;
        //            }
        //        }

        //        Console.WriteLine($"✓ Выделена строка товаров {rowIndex}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"✗ Ошибка при выделении строки товаров: {ex.Message}");
        //    }
        //}

        private void SelectProductRow(int dataIndex)
        {
            try
            {
                if (dataIndex < 0 || dataIndex >= _productsData.Count)
                {
                    ClearProductSelection();
                    return;
                }

                // Снимаем предыдущее выделение
                ClearProductSelection();

                // Устанавливаем новое выделение
                _selectedProductRowIndex = dataIndex;

                // Находим Border строки (Grid.Row = dataIndex + 1, так как строка 0 - заголовки)
                int gridRowIndex = dataIndex + 1;

                foreach (Control child in _productsTableGrid.Children)
                {
                    if (child is Border border && Grid.GetRow(border) == gridRowIndex)
                    {
                        // Меняем стиль выделенной строки
                        border.Background = PRODUCT_SELECTED_BACKGROUND;
                        border.BorderBrush = PRODUCT_SELECTED_BORDER;
                        border.BorderThickness = new Thickness(2);

                        _selectedProductRowBorder = border;
                        break;
                    }
                }

                // ПРОКРУЧИВАЕМ К ВЫДЕЛЕННОЙ СТРОКЕ
                ScrollToSelectedRow(gridRowIndex);

                Console.WriteLine($"✓ Выделена строка товаров {dataIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при выделении строки товаров: {ex.Message}");
            }
        }

        private void ScrollToSelectedRow(int gridRowIndex)
        {
            try
            {
                if (_productsScrollViewer == null)
                {
                    Console.WriteLine("⚠ ScrollViewer не инициализирован");
                    return;
                }

                // Вычисляем примерную позицию строки
                double rowHeight = 40; // Высота строки (должно совпадать с RowDefinition)
                double rowTopPosition = (gridRowIndex - 1) * rowHeight; // -1 потому что строка 0 - заголовки
                double rowBottomPosition = rowTopPosition + rowHeight;

                // Получаем текущие видимые границы
                double viewportTop = _productsScrollViewer.Offset.Y;
                double viewportHeight = _productsScrollViewer.Viewport.Height;
                double viewportBottom = viewportTop + viewportHeight;

                // Проверяем, видна ли строка
                bool isRowVisible = rowTopPosition >= viewportTop && rowBottomPosition <= viewportBottom;

                if (!isRowVisible)
                {
                    // Прокручиваем так, чтобы строка была в центре видимой области
                    double targetOffset = rowTopPosition - (viewportHeight / 2) + (rowHeight / 2);

                    // Ограничиваем минимальное и максимальное смещение
                    double maxOffset = Math.Max(0, _productsScrollViewer.Extent.Height - viewportHeight);
                    targetOffset = Math.Max(0, Math.Min(targetOffset, maxOffset));

                    // Прокручиваем с анимацией
                    _productsScrollViewer.Offset = new Vector(_productsScrollViewer.Offset.X, targetOffset);

                    Console.WriteLine($"✓ Прокрутка к строке {gridRowIndex}: offset={targetOffset:F1}");
                }
                else
                {
                    Console.WriteLine($"✓ Строка {gridRowIndex} уже видна");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при прокрутке: {ex.Message}");
            }
        }

        // Метод для снятия выделения
        private void ClearProductSelection()
        {
            try
            {
                if (_selectedProductRowBorder != null)
                {
                    // Восстанавливаем оригинальный стиль строки
                    int dataRowIndex = _selectedProductRowIndex;
                    var originalBackground = (dataRowIndex % 2 == 0) ? Brushes.White : Brushes.AliceBlue;

                    _selectedProductRowBorder.Background = originalBackground;
                    _selectedProductRowBorder.BorderBrush = Brushes.LightGray;
                    _selectedProductRowBorder.BorderThickness = new Thickness(0, 0, 0, 1);

                    _selectedProductRowBorder = null;
                }

                _selectedProductRowIndex = -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при снятии выделения товаров: {ex.Message}");
            }
        }

        // Обработчик клика по строке товаров
        private void OnProductRowPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is int dataRowIndex)
                {
                    SelectProductRow(dataRowIndex);
                    e.Handled = true;

                    // Устанавливаем фокус на ScrollViewer для обработки клавиатуры
                    _productsScrollViewer?.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка в обработчике клика строки товаров: {ex.Message}");
            }
        }

        // Обработчик клика по таблице товаров
        private void OnProductsTableGridPointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Если кликнули не по строке, НЕ снимаем выделение
            var source = e.Source as Control;
            if (source is Border border && border.Tag is int)
            {
                // Это строка, обработка будет в OnProductRowPointerPressed
                return;
            }

            e.Handled = true;
        }

        // Обработчик клика по ScrollViewer товаров
        private void OnProductsScrollViewerPointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Устанавливаем фокус на ScrollViewer
            _productsScrollViewer?.Focus();
        }

        // Проверка, имеет ли дочерний элемент фокус
        private bool IsChildFocused(Control parent)
        {
            if (parent == null) return false;

            foreach (var child in parent.GetVisualChildren())
            {
                if (child is Control control && control.IsFocused)
                    return true;

                if (IsChildFocused(child as Control))
                    return true;
            }

            return false;
        }

        #endregion

        #region Обработка клавиатуры и управление количеством товаров

        // Глобальная обработка клавиатуры для товаров
        private void OnGlobalKeyDownForProducts(object sender, KeyEventArgs e)
        {
            // Проверяем, есть ли фокус в таблице товаров
            bool isProductsTableFocused = _productsScrollViewer?.IsFocused == true ||
                                         _productsTableGrid?.IsFocused == true ||
                                         IsChildFocused(_productsScrollViewer);

            if (!isProductsTableFocused) return;

            switch (e.Key)
            {
                case Key.Up:
                    MoveProductSelectionUp();
                    e.Handled = true;
                    break;

                case Key.Down:
                    MoveProductSelectionDown();
                    e.Handled = true;
                    break;

                case Key.Home:
                    if (_productsData.Count > 0) SelectProductRow(0);
                    e.Handled = true;
                    break;

                case Key.End:
                    if (_productsData.Count > 0) SelectProductRow(_productsData.Count - 1);
                    e.Handled = true;
                    break;

                // ОБРАБОТКА КЛАВИШ + И - ДЛЯ ИЗМЕНЕНИЯ КОЛИЧЕСТВА
                case Key.Add:
                case Key.OemPlus:
                    if (_selectedProductRowIndex >= 0)
                        IncreaseProductQuantity(_selectedProductRowIndex);
                    e.Handled = true;
                    break;

                case Key.Subtract:
                case Key.OemMinus:
                    if (_selectedProductRowIndex >= 0)
                        DecreaseProductQuantity(_selectedProductRowIndex);
                    e.Handled = true;
                    break;

                // Дополнительно: клавиша Delete для удаления товара
                case Key.Delete:
                    if (_selectedProductRowIndex >= 0)
                        DeleteSelectedProduct();
                    e.Handled = true;
                    break;

                // Клавиша Enter для быстрого редактирования количества
                case Key.Enter:
                    if (_selectedProductRowIndex >= 0)
                        ShowQuantityEditDialog(_selectedProductRowIndex);
                    e.Handled = true;
                    break;
            }
        }

        // Перемещение выделения вверх в товарах
        //private void MoveProductSelectionUp()
        //{
        //    if (_productsData.Count == 0) return;

        //    int newIndex = _selectedProductRowIndex - 1;
        //    if (newIndex < 0) newIndex = 0; // Остаемся на первой строке

        //    SelectProductRow(newIndex);
        //}

        private void MoveProductSelectionUp()
        {
            if (_productsData.Count == 0) return;

            int newIndex = _selectedProductRowIndex - 1;
            if (newIndex < 0) newIndex = 0;

            SelectProductRow(newIndex);
        }

        // Перемещение выделения вниз в товарах
        //private void MoveProductSelectionDown()
        //{
        //    if (_productsData.Count == 0) return;

        //    int newIndex = _selectedProductRowIndex + 1;
        //    if (newIndex >= _productsData.Count) newIndex = _productsData.Count - 1; // Остаемся на последней строке

        //    SelectProductRow(newIndex);
        //}
        private void MoveProductSelectionDown()
        {
            if (_productsData.Count == 0) return;

            int newIndex = _selectedProductRowIndex + 1;
            if (newIndex >= _productsData.Count) newIndex = _productsData.Count - 1;

            SelectProductRow(newIndex);
        }

        private void MoveProductSelectionHome()
        {
            if (_productsData.Count > 0) SelectProductRow(0);
        }

        private void MoveProductSelectionEnd()
        {
            if (_productsData.Count > 0) SelectProductRow(_productsData.Count - 1);
        }

        // Метод для увеличения количества (минимальное изменение)
        private void IncreaseProductQuantity(int dataIndex)
        {
            try
            {
                if (dataIndex >= 0 && dataIndex < _productsData.Count)
                {
                    var product = _productsData[dataIndex];
                    product.Quantity++;

                    RecalculateProductSums(product);
                    UpdateProductRowInGrid(dataIndex);
                    UpdateTotalSum();

                    // Замените эту строку:
                    // ShowSimpleQuantityEffect(dataIndex, true);

                    // На эту:
                    ShowQuantityEffect(dataIndex, true); // true = увеличение (зеленый)

                    Console.WriteLine($"✓ Увеличено количество товара '{product.Tovar}' до {product.Quantity}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при увеличении количества: {ex.Message}");
            }
        }

        // Улучшенный метод для эффекта изменения количества (бордеры ячеек)
        private void ShowQuantityEffect(int dataIndex, bool isIncrease, bool isMinimum = false)
        {
            try
            {
                int gridRowIndex = dataIndex + 1;

                if (isMinimum)
                {
                    // Эффект для минимального количества (красный, только количество)
                    FlashCellBorder(gridRowIndex, 2, QUANTITY_MINIMUM_COLOR, 3); // колонка 2 - количество
                    return;
                }

                // Определяем цвет в зависимости от типа изменения
                var effectColor = isIncrease ? QUANTITY_INCREASE_COLOR : QUANTITY_DECREASE_COLOR;

                // 1. Мигаем бордером ячейки количества (колонка 2)
                FlashCellBorder(gridRowIndex, 2, effectColor, 1);

                // 2. Мигаем бордером ячейки суммы (колонка 5)
                FlashCellBorder(gridRowIndex, 5, effectColor, 1);

                // 3. Мигаем бордером ячейки суммы со скидкой (колонка 6)
                FlashCellBorder(gridRowIndex, 6, effectColor, 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка в эффекте: {ex.Message}");
            }
        }

        // Метод для мигания бордера конкретной ячейки
        private void FlashCellBorder(int gridRow, int column, IBrush color, int flashCount)
        {
            try
            {
                // Создаем временный Border для ячейки
                var cellBorder = new Border
                {
                    BorderBrush = color,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(3),
                    IsHitTestVisible = false,
                    Opacity = 0.8
                };

                Grid.SetColumn(cellBorder, column);
                Grid.SetRow(cellBorder, gridRow);
                _productsTableGrid.Children.Add(cellBorder);

                int flashCounter = 0;
                DispatcherTimer flashTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };

                flashTimer.Tick += (s, e) =>
                {
                    if (flashCounter % 2 == 0)
                    {
                        // Включение эффекта
                        cellBorder.Opacity = 0.8;
                    }
                    else
                    {
                        // Выключение эффекта
                        cellBorder.Opacity = 0.3;
                    }

                    flashCounter++;

                    if (flashCounter >= flashCount * 2) // 2 такта на каждую вспышку
                    {
                        flashTimer.Stop();
                        // Удаляем временный Border
                        _productsTableGrid.Children.Remove(cellBorder);
                    }
                };

                flashTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка при мигании бордера ячейки: {ex.Message}");
            }
        }
                       
        
        // Метод для уменьшения количества (минимальное изменение)
        private void DecreaseProductQuantity(int dataIndex)
        {
            try
            {
                if (dataIndex >= 0 && dataIndex < _productsData.Count)
                {
                    var product = _productsData[dataIndex];

                    if (product.Quantity > 1)
                    {
                        product.Quantity--;

                        RecalculateProductSums(product);
                        UpdateProductRowInGrid(dataIndex);
                        UpdateTotalSum();

                        // Замените эту строку (если есть):
                        // ShowSimpleQuantityEffect(dataIndex, false);

                        // На эту:
                        ShowQuantityEffect(dataIndex, false); // false = уменьшение (желтый)

                        Console.WriteLine($"✓ Уменьшено количество товара '{product.Tovar}' до {product.Quantity}");
                    }
                    else
                    {
                        Console.WriteLine("⚠ Невозможно уменьшить количество меньше 1");
                        // Красный эффект минимального количества
                        ShowQuantityEffect(dataIndex, false, true); // true = минимальное количество
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при уменьшении количества: {ex.Message}");
            }
        }

        // Метод для эффекта ошибки/предупреждения
        private void ShowWarningEffect(int dataIndex)
        {
            try
            {
                int gridRowIndex = dataIndex + 1;

                // Ищем TextBlock в ячейке количества (колонка 2)
                foreach (Control child in _productsTableGrid.Children)
                {
                    if (child is TextBlock textBlock &&
                        Grid.GetRow(textBlock) == gridRowIndex &&
                        Grid.GetColumn(textBlock) == 2)
                    {
                        // Сохраняем оригинальные значения
                        var originalForeground = textBlock.Foreground;
                        var originalBackground = textBlock.Background;

                        // Устанавливаем эффект ошибки (оранжевый)
                        textBlock.Foreground = Brushes.OrangeRed;
                        textBlock.Background = Brushes.LightYellow;
                        textBlock.FontWeight = FontWeight.Bold;

                        // Возвращаем оригинальный вид через 0.5 секунды
                        DispatcherTimer timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(500)
                        };

                        timer.Tick += (s, e) =>
                        {
                            textBlock.Foreground = originalForeground;
                            textBlock.Background = originalBackground;
                            textBlock.FontWeight = FontWeight.Normal;
                            timer.Stop();
                        };

                        timer.Start();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка в эффекте предупреждения: {ex.Message}");
            }
        }

        // Метод для удаления выбранного товара
        private void DeleteSelectedProduct()
        {
            try
            {
                if (_selectedProductRowIndex >= 0 && _selectedProductRowIndex < _productsData.Count)
                {
                    var product = _productsData[_selectedProductRowIndex];

                    // Простое подтверждение удаления без MessageBox
                    // Вместо MessageBox будем использовать простой механизм
                    DeleteProductWithConfirmation(product);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при удалении товара: {ex.Message}");
            }
        }

        // Простой метод удаления с подтверждением
        private async void DeleteProductWithConfirmation(ProductItem product)
        {
            // Можно реализовать простой диалог или сразу удалять
            // Для простоты сразу удаляем
            // Удаляем товар из данных
            _productsData.RemoveAt(_selectedProductRowIndex);

            // Обновляем Grid
            RefreshProductsGrid();

            // Обновляем общую сумму
            UpdateTotalSum();

            Console.WriteLine($"✓ Товар '{product.Tovar}' удален");

            // Выделяем следующую строку или снимаем выделение
            if (_productsData.Count > 0)
            {
                if (_selectedProductRowIndex >= _productsData.Count)
                    _selectedProductRowIndex = _productsData.Count - 1;
                SelectProductRow(_selectedProductRowIndex);
            }
            else
            {
                ClearProductSelection();
            }
        }

        // Метод для пересчета сумм товара
        private void RecalculateProductSums(ProductItem product)
        {
            product.Sum = product.Quantity * product.Price;
            product.SumAtDiscount = product.Quantity * product.PriceAtDiscount;
        }

        // Метод для обновления всей строки товара в Grid (обновление текста без пересоздания)
        private void UpdateProductRowInGrid(int dataIndex)
        {
            try
            {
                if (dataIndex < 0 || dataIndex >= _productsData.Count)
                    return;

                var product = _productsData[dataIndex];
                int gridRowIndex = dataIndex + 1;

                Console.WriteLine($"Обновление строки {gridRowIndex} в Grid (данные: {dataIndex})...");

                // Находим все TextBlock'и в строке и обновляем их текст
                var textBlocksByColumn = new Dictionary<int, TextBlock>();

                foreach (Control child in _productsTableGrid.Children)
                {
                    if (child is TextBlock textBlock && Grid.GetRow(textBlock) == gridRowIndex)
                    {
                        int column = Grid.GetColumn(textBlock);
                        textBlocksByColumn[column] = textBlock;
                    }
                }

                // Обновляем текст в существующих TextBlock'ах
                UpdateTextBlockText(textBlocksByColumn, 0, product.Code.ToString());
                UpdateTextBlockText(textBlocksByColumn, 1, product.Tovar);
                UpdateTextBlockText(textBlocksByColumn, 2, product.Quantity.ToString());
                UpdateTextBlockText(textBlocksByColumn, 3, product.Price.ToString("N2"));
                UpdateTextBlockText(textBlocksByColumn, 4, product.PriceAtDiscount.ToString("N2"));
                UpdateTextBlockText(textBlocksByColumn, 5, product.Sum.ToString("N2"));
                UpdateTextBlockText(textBlocksByColumn, 6, product.SumAtDiscount.ToString("N2"));
                UpdateTextBlockText(textBlocksByColumn, 7, product.Action.ToString());
                UpdateTextBlockText(textBlocksByColumn, 8, product.Gift.ToString());
                UpdateTextBlockText(textBlocksByColumn, 9, product.Action2.ToString());
                UpdateTextBlockText(textBlocksByColumn, 10, product.Mark.ToString());

                // Если TextBlock'и не найдены (странная ситуация), создаем новые
                if (textBlocksByColumn.Count == 0)
                {
                    Console.WriteLine("⚠ TextBlock'и не найдены, создаем новые...");
                    CreateNewCellsForRow(gridRowIndex, product);
                }

                Console.WriteLine($"✓ Строка {gridRowIndex} обновлена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при обновлении строки в Grid: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        // Метод для обновления текста в TextBlock
        private void UpdateTextBlockText(Dictionary<int, TextBlock> textBlocksByColumn, int column, string text)
        {
            if (textBlocksByColumn.TryGetValue(column, out var textBlock))
            {
                // Сбрасываем стиль перед обновлением текста
                textBlock.FontWeight = FontWeight.Normal;
                textBlock.Foreground = Brushes.Black;
                textBlock.FontSize = 12;
                textBlock.Text = text;
            }
        }

        // Метод для создания новых ячеек, если их нет
        private void CreateNewCellsForRow(int gridRowIndex, ProductItem product)
        {
            AddCell(_productsTableGrid, 0, gridRowIndex, product.Code.ToString(), HorizontalAlignment.Right);
            AddCellWithWrap(_productsTableGrid, 1, gridRowIndex, product.Tovar, HorizontalAlignment.Left);
            AddCell(_productsTableGrid, 2, gridRowIndex, product.Quantity.ToString(), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 3, gridRowIndex, product.Price.ToString("N2"), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 4, gridRowIndex, product.PriceAtDiscount.ToString("N2"), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 5, gridRowIndex, product.Sum.ToString("N2"), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 6, gridRowIndex, product.SumAtDiscount.ToString("N2"), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 7, gridRowIndex, product.Action.ToString(), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 8, gridRowIndex, product.Gift.ToString(), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 9, gridRowIndex, product.Action2.ToString(), HorizontalAlignment.Right);
            AddCell(_productsTableGrid, 10, gridRowIndex, product.Mark.ToString(), HorizontalAlignment.Right);
        }

        // Метод для обновления всего Grid (после удаления товара)
        private void RefreshProductsGrid()
        {
            try
            {
                Console.WriteLine("Обновление всего Grid товаров...");

                // Очищаем старые строки данных (кроме заголовков)
                while (_productsTableGrid.RowDefinitions.Count > 1)
                {
                    _productsTableGrid.RowDefinitions.RemoveAt(_productsTableGrid.RowDefinitions.Count - 1);
                }

                // Удаляем все элементы кроме заголовков
                var elementsToRemove = new List<Control>();
                foreach (Control child in _productsTableGrid.Children)
                {
                    if (Grid.GetRow(child) > 0) // Все что ниже строки 0 (заголовки)
                    {
                        elementsToRemove.Add(child);
                    }
                }

                foreach (var element in elementsToRemove)
                {
                    _productsTableGrid.Children.Remove(element);
                }

                // Сбрасываем счетчик строк
                _productsCurrentRow = 1;

                // Добавляем обновленные данные
                AddProductsGridRows(_productsTableGrid, ref _productsCurrentRow, _productsData);

                Console.WriteLine($"✓ Grid товаров обновлен. Записей: {_productsData.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при обновлении Grid: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        // Метод для показа диалога редактирования количества (по нажатию Enter)
        // Альтернативный метод для показа диалога редактирования количества
        private void ShowQuantityEditDialog(int dataIndex)
        {
            if (dataIndex >= 0 && dataIndex < _productsData.Count)
            {
                var product = _productsData[dataIndex];

                // Создаем простое окно
                var dialog = new Window
                {
                    Title = "Изменение количества",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    CanResize = false,
                    ShowInTaskbar = false,
                    SystemDecorations = SystemDecorations.BorderOnly // Только рамка и кнопка закрытия
                };

                var stackPanel = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 10
                };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"Товар: {product.Tovar}",
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 250
                });

                var textBox = new TextBox
                {
                    Text = product.Quantity.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 120,
                    MaxLength = 5
                };

                // Устанавливаем фокус и выделяем весь текст при загрузке окна
                dialog.Opened += (s, e) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        textBox.Focus();
                        textBox.SelectAll(); // Выделяем весь текст
                    }, DispatcherPriority.Background);
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var okButton = new Button
                {
                    Content = "OK",
                    Width = 80,
                    IsDefault = true
                };

                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 80,
                    IsCancel = true
                };

                bool dialogResult = false;

                void ApplyQuantityChanges()
                {
                    if (int.TryParse(textBox.Text, out int newQuantity) && newQuantity >= 1 && newQuantity <= 9999)
                    {
                        product.Quantity = newQuantity;
                        RecalculateProductSums(product);
                        UpdateProductRowInGrid(dataIndex);
                        UpdateTotalSum();
                        dialogResult = true;
                        Console.WriteLine($"✓ Количество товара '{product.Tovar}' изменено на {product.Quantity}");
                    }
                    else
                    {
                        Console.WriteLine("⚠ Неверное количество товара");
                    }
                    dialog.Close();
                }

                okButton.Click += (s, e) => ApplyQuantityChanges();

                cancelButton.Click += (s, e) =>
                {
                    dialogResult = false;
                    dialog.Close();
                };

                // Обработка нажатия Enter в TextBox
                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        ApplyQuantityChanges();
                        e.Handled = true;
                    }
                };

                // Обработка нажатия Escape в окне
                dialog.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Escape)
                    {
                        dialog.Close();
                        e.Handled = true;
                    }
                };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);

                stackPanel.Children.Add(textBox);
                stackPanel.Children.Add(buttonPanel);

                dialog.Content = stackPanel;

                // Показываем окно
                dialog.ShowDialog(this.FindAncestorOfType<Window>());
            }
        }

        #endregion

        private void InitializeFormData()
        {
            try
            {
                Console.WriteLine("=== Инициализация данных формы ===");

                // Инициализация ComboBox с типами чеков
                if (CheckType != null)
                {
                    CheckType.Items.Clear();
                    CheckType.Items.Add("Продажа");
                    CheckType.Items.Add("Возврат");
                    CheckType.Items.Add("Коррекция");
                    CheckType.SelectedIndex = 0;
                    Console.WriteLine("✓ CheckType инициализирован");
                }


                if (NumCash != null)
                {
                    NumCash.Text = $"КАССА № {MainStaticClass.CashDeskNumber}";
                    Console.WriteLine("✓ NumCash установлен");
                }

                if (date_time_start != null)
                {
                    date_time_start.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Console.WriteLine("✓ date_time_start установлен");
                }

                if (User != null)
                {
                    User.Text = MainStaticClass.Cash_Operator;
                    Console.WriteLine("✓ User установлен");
                }

                // Обновление общей суммы
                UpdateTotalSum();

                // Если поле date_time_write заполнено, загружаем данные из БД
                if (!string.IsNullOrEmpty(date_time_write))
                {
                    Console.WriteLine($"✓ Поле date_time_write заполнено: {date_time_write}");
                    Console.WriteLine("Вызываем метод загрузки данных из БД...");

                    // Вызываем асинхронно, чтобы не блокировать конструктор
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            ToOpenTheWrittenDownDocument();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Ошибка при загрузке данных из БД: {ex.Message}");
                        }
                    });
                }

                InitializeQrCodeLengths();

                Console.WriteLine("✓ Данные формы инициализированы");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при инициализации данных формы: {ex.Message}");
            }
        }

        private void InitializeQrCodeLengths()
        {
            // Объявите qr_code_lenght как поле класса:
            // private List<int> qr_code_lenght = new List<int>();

            qr_code_lenght.Clear();
            qr_code_lenght.Add(29);
            qr_code_lenght.Add(30);
            qr_code_lenght.Add(31);
            qr_code_lenght.Add(32);
            qr_code_lenght.Add(37);
            qr_code_lenght.Add(40);
            qr_code_lenght.Add(41);
            qr_code_lenght.Add(76);
            qr_code_lenght.Add(83);
            qr_code_lenght.Add(115);
            qr_code_lenght.Add(127);

            Console.WriteLine($"✓ Инициализирован qr_code_lenght: {qr_code_lenght.Count} значений");
        }

        private void UpdateTotalSum()
        {
            try
            {
                decimal totalProducts = _productsData.Sum(p => p.SumAtDiscount);
                decimal totalCertificates = _certificatesData.Sum(c => c.Nominal);
                decimal total = totalProducts + totalCertificates;

                if (txtB_total_sum != null)
                {
                    txtB_total_sum.Text = total.ToString("N2");
                    Console.WriteLine($"✓ Общая сумма обновлена: Товары={totalProducts:N2}, Сертификаты={totalCertificates:N2}, Итого={total:N2}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при обновлении общей суммы: {ex.Message}");
            }
        }

        private void DebugGridInfo()
        {
            Console.WriteLine("\n=== Отладочная информация Grid ===");

            Console.WriteLine($"Grid товаров: {_productsTableGrid != null}");
            if (_productsTableGrid != null)
            {
                Console.WriteLine($"  - Колонок: {_productsTableGrid.ColumnDefinitions.Count}");
                Console.WriteLine($"  - Строк: {_productsTableGrid.RowDefinitions.Count}");
                Console.WriteLine($"  - Записей: {_productsData.Count}");
            }

            Console.WriteLine($"\nGrid сертификатов: {_certificatesTableGrid != null}");
            if (_certificatesTableGrid != null)
            {
                Console.WriteLine($"  - Колонок: {_certificatesTableGrid.ColumnDefinitions.Count}");
                Console.WriteLine($"  - Строк: {_certificatesTableGrid.RowDefinitions.Count}");
                Console.WriteLine($"  - Записей: {_certificatesData.Count}");
            }

            Console.WriteLine("=== Конец отладочной информации ===\n");
        }

        // Метод для открытия записанного документа (аналог старого метода)
        private void ToOpenTheWrittenDownDocument()
        {
            Console.WriteLine($"=== ToOpenTheWrittenDownDocument начат (date_time_write='{date_time_write}') ===");

            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = "SELECT checks_header.client, checks_header.cash_desk_number, checks_header.comment, checks_header.cash, " +
                               " checks_header.remainder,checks_header.date_time_start,checks_header.discount,clients.name AS clients_name ,users.name AS users_name  " +
                               ",tovar.name AS tovar_name ,checks_table.tovar_code, checks_table.quantity,checks_table.price, checks_table.price_at_a_discount,checks_table.sum, " +
                               " checks_table.sum_at_a_discount,checks_table.action_num_doc,checks_table.action_num_doc1,checks_table.action_num_doc2," +
                               " checks_header.check_type " +
                               " ,characteristic.name AS characteristic_name,checks_header.document_number,checks_header.autor,characteristic.guid,clients.code AS clients_code ," +
                               " checks_header.sertificate_money,checks_header.non_cash_money,checks_header.cash_money,checks_header.bonuses_it_is_counted, " +
                               " checks_header.bonuses_it_is_written_off, " +
                               " checks_table.bonus_standard,checks_table.bonus_promotion,checks_table.promotion_b_mover,checks_table.item_marker,checks_header.requisite," +
                               "checks_header.its_deleted,checks_header.system_taxation,checks_header.guid AS checks_header_guid,checks_header.guid1 AS checks_header_guid,payment_by_sbp,checks_header.action_num_doc " +
                               " FROM checks_header left join checks_table ON checks_header.document_number=checks_table.document_number " +
                               " left join clients ON checks_header.client  = clients.code " +
                               " left join tovar ON checks_table.tovar_code = tovar.code " +
                               " left join users ON  checks_header.autor = users.code " +
                               " left join characteristic ON  checks_table.characteristic = characteristic.guid " +
                               " where checks_header.date_time_write='" + date_time_write + "' order by checks_table.numstr;";

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                bool header_fill = false;

                // Очищаем существующие данные перед загрузкой новых
                _productsData.Clear();
                _productsCurrentRow = 1; // Сбрасываем счетчик строк

                Console.WriteLine("Чтение данных из БД...");

                while (reader.Read())
                {
                    if (!header_fill)
                    {
                        // Заполнение заголовка документа
                        this.guid = reader["checks_header_guid"].ToString();
                        this.guid1 = reader["checks_header_guid"].ToString();
                        this.Client.Tag = reader["client"].ToString();
                        this.Client.Text = reader["clients_name"].ToString();
                        this.User.Text = reader["users_name"].ToString();
                        this.txtB_cash_money.Text = reader["cash_money"].ToString();
                        this.txtB_non_cash_money.Text = reader["non_cash_money"].ToString();
                        this.txtB_sertificate_money.Text = reader["sertificate_money"].ToString();
                        this.txtB_bonus_money.Text = reader["bonuses_it_is_written_off"].ToString();
                        this.numdoc = Convert.ToInt64(reader["document_number"]);
                        this.txtB_num_doc.Text = this.numdoc.ToString();
                        this.checkBox_payment_by_sbp.IsChecked = Convert.ToBoolean(reader["payment_by_sbp"]);

                        if (CheckType != null)
                        {
                            this.CheckType.SelectedIndex = Convert.ToInt16(reader["check_type"]);
                        }

                        header_fill = true;
                        Console.WriteLine("✓ Заголовок документа заполнен");
                    }

                    // Добавляем данные в Grid товаров
                    var productItem = new ProductItem
                    {
                        Code = Convert.ToInt32(reader["tovar_code"]),
                        Tovar = reader["tovar_name"].ToString().Trim(),
                        Quantity = Convert.ToInt32(reader["quantity"]),
                        Price = Convert.ToDecimal(reader["price"]),
                        PriceAtDiscount = Convert.ToDecimal(reader["price_at_a_discount"]),
                        Sum = Convert.ToDecimal(reader["sum"]),
                        SumAtDiscount = Convert.ToDecimal(reader["sum_at_a_discount"]),
                        Action = Convert.ToInt32(reader["action_num_doc"]),
                        Gift = Convert.ToInt32(reader["action_num_doc1"]),
                        Action2 = Convert.ToInt32(reader["action_num_doc2"]),
                        Mark = reader["item_marker"].ToString().Replace("vasya2021", "'").Trim()
                    };

                    _productsData.Add(productItem);
                    Console.WriteLine($"Добавлена запись в товары: {productItem.Tovar} (Код: {productItem.Code})");
                }

                reader.Close();
                Console.WriteLine($"✓ Прочитано {_productsData.Count} записей из БД");

                // УБИРАЕМ ПЕРЕСОЗДАНИЕ GRID - просто обновляем данные
                if (_productsTableGrid != null && _productsScrollViewer != null && _tabProducts != null)
                {
                    Console.WriteLine("Обновление данных Grid товаров...");

                    // НЕ очищаем Grid и не пересоздаем колонки!
                    // Просто очищаем старые строки с данными (начиная со строки 1, так как строка 0 - заголовки)

                    // 1. Удаляем все строки данных (кроме заголовков)
                    while (_productsTableGrid.RowDefinitions.Count > 1)
                    {
                        _productsTableGrid.RowDefinitions.RemoveAt(_productsTableGrid.RowDefinitions.Count - 1);
                    }

                    // 2. Удаляем все элементы кроме заголовков (строки с данными и Border строк)
                    var elementsToRemove = new List<Control>();
                    foreach (Control child in _productsTableGrid.Children)
                    {
                        if (Grid.GetRow(child) > 0) // Все что ниже строки 0 (заголовки)
                        {
                            elementsToRemove.Add(child);
                        }
                    }

                    foreach (var element in elementsToRemove)
                    {
                        _productsTableGrid.Children.Remove(element);
                    }

                    // 3. Сбрасываем счетчик строк
                    _productsCurrentRow = 1;

                    // 4. Добавляем новые данные в существующий Grid с новыми параметрами
                    AddProductsGridRows(_productsTableGrid, ref _productsCurrentRow, _productsData);

                    Console.WriteLine($"✓ Данные Grid товаров обновлены: {_productsData.Count} записей");

                    // 5. Автоматически выделяем первую строку после загрузки данных
                    if (_productsData.Count > 0)
                    {
                        SelectProductRow(0);

                        // Устанавливаем фокус на таблицу товаров
                        Dispatcher.UIThread.Post(() =>
                        {
                            _productsScrollViewer?.Focus();
                        }, DispatcherPriority.Background);
                    }
                }
                else
                {
                    Console.WriteLine("⚠ Grid товаров не инициализирован, создаем новый...");
                    // Если Grid еще не создан (маловероятно), создаем его
                    CreateProductsGrid();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"✗ Npgsql ошибка в ToOpenTheWrittenDownDocument: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Общая ошибка в ToOpenTheWrittenDownDocument: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    Console.WriteLine("✓ Соединение с БД закрыто");
                }
            }

            Console.WriteLine("=== ToOpenTheWrittenDownDocument завершен ===");
        }

        #region Публичные методы для совместимости

        public void CloseForm()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public event EventHandler Loaded;

        protected virtual void OnLoaded()
        {
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        public void LoadForm()
        {
            OnLoaded();
            InitializeFormData();
        }
    }
}