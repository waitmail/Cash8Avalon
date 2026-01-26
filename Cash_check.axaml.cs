using Atol.Drivers10.Fptr;
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
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Cash8Avalon.Cash_check;
using static Cash8Avalon.LoadDataWebService;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AtolConstants = Atol.Drivers10.Fptr.Constants;


namespace Cash8Avalon
{
    public partial class Cash_check : Window
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
        private Pay pay_form = new Pay();
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

        public event EventHandler Loaded;

        public bool reopened = false;
        public bool print_promo_picture = false;


        List<int> qr_code_lenght = new List<int>();

        // Событие для закрытия формы
        //public event EventHandler Closed;

        // Контролы
        public ComboBox CheckType { get; private set; }
        public TextBox NumCash { get; private set; }
        public TextBox User { get; private set; }
        public TextBox Client { get; private set; }
        public TextBox ClientBarcodeOrPhone { get; private set; }
        public TextBox NumSales { get; private set; }
        public TextBox InputSearchProduct { get; private set; }
        public Button Pay { get; private set; }
        public TextBox Comment { get; private set; }
        
        

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
        public List<ProductItem> _productsData = new List<ProductItem>();
        private List<ProductItem> _productsDataBackup = new List<ProductItem>(); // Резервная копия

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

                this.Closed += Cash_check_Closed;

                Console.WriteLine("✓ Конструктор завершен успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ОШИБКА в конструкторе: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("=== Конструктор Cash_check завершен ===");
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            this.num_cash.Text = "КАССА № " + MainStaticClass.CashDeskNumber.ToString();
            this.num_cash.Tag = MainStaticClass.CashDeskNumber;

            //Создание таблицы для перераспределения акций
            DataColumn dc = new DataColumn("Code", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Tovar", System.Type.GetType("System.String"));
            table.Columns.Add(dc);
            dc = new DataColumn("Quantity", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Price", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("PriceAtDiscount", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("Sum", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("SumAtDiscount", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("Action", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Gift", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Action2", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);

            //this.inputbarcode.Focus();
            this.txtB_search_product.Focus();

            if (MainStaticClass.GetVersionFn == 1)
            {
                checkBox_print_check.IsVisible = false;
            }
            checkBox_print_check.IsChecked = true;

            if (IsNewCheck)
            {
                guid = Guid.NewGuid().ToString();

                checkBox_to_print_repeatedly.IsVisible = false;
                //label9.Visible = false;
                //label10.Visible = false;
                //label11.Visible = false;
                //label13.Visible = false;
                txtB_non_cash_money.IsVisible = false;
                txtB_sertificate_money.IsVisible = false;
                txtB_cash_money.IsVisible = false;
                txtB_bonus_money.IsVisible = false;

                //inputbarcode.Focus();
                this.txtB_search_product.Focus();

                this.date_time_start.Text = "Чек   " + DateTime.Now.ToString("yyy-MM-dd HH:mm:ss");
                this.Discount = 0;
                this.user.Text = MainStaticClass.Cash_Operator;
                this.user.Tag = MainStaticClass.Cash_Operator_Client_Code;//gaa поменять на инн
                numdoc = get_new_number_document();
                if (numdoc == 0)
                {
                    await MessageBox.Show("Ошибка при получении номера документа.", "Проверка при получении номер документа");
                    MainStaticClass.WriteRecordErrorLog("Ошибка при получении номера документа", "Cash_check_Load", 0, MainStaticClass.CashDeskNumber, "При вводе нового документа получен нулевой номер");
                    this.Close();
                }
                this.txtB_num_doc.Text = this.numdoc.ToString();
                MainStaticClass.write_event_in_log(" Ввод нового документа ", "Документ чек", numdoc.ToString());
                this.check_type.SelectedIndex = 0;
                this.check_type.IsEnabled = true;
                set_sale_disburse_button();
            }
            else
            {
                reopened = true;
                SetFormReadOnly(true);
                //checkBox_print_check.IsEnabled = false;
                ////Документ не новый поэтому запретим в нем ввод и изменение                
                //last_tovar.IsEnabled = false;
                //txtB_email_telephone.IsEnabled = false;
                //txtB_inn.IsEnabled = false;
                //btn_get_name.IsEnabled = false;
                ////txtB_client_phone.Enabled = false;
                //txtB_name.IsEnabled = false;
                //comment.IsEnabled = false;
                

                int status = get_its_deleted_document();
                if ((status == 0) || (status == 1))
                {
                    
                    //this.label4.Enabled = false;
                    this.check_type.IsEnabled = false;
                    //this.inputbarcode.Enabled = false;
                    this.txtB_search_product.IsEnabled = false;
                    this.client_barcode.IsEnabled = false;
                    //this.sale_cancellation.Enabled = false;
                    //this.inventory.Enabled = false;
                    this.comment.IsEnabled = false;                    
                    ToOpenTheWrittenDownDocument();
                    enable_print();
                    if (MainStaticClass.Code_right_of_user != 1)
                    {
                        this.pay.IsEnabled = false;
                    }
                    //IsNewCheck = false;
                }
                //else if (status == 2)
                //{
                //    return;
                //    //IsNewCheck = true;
                //    //Discount = 0;
                //    ////this.label4.Enabled = true;
                //    //this.check_type.IsEnabled = true;
                //    ////this.inputbarcode.Enabled = true;
                //    //this.txtB_search_product.IsEnabled = true;
                //    //this.client_barcode.IsEnabled = false;
                //    //ToOpenTheWrittenDownDocument();
                //    //get_old_document_Discount();
                //    //check_type.IsEnabled = false;
                //    //IsNewCheck = false;
                //}
            }        


            if (IsNewCheck)
            {
                //first_start_com_barcode_scaner();
                selection_goods = true;
                //inputbarcode.Focus();
                this.txtB_search_product.Focus();
                //список допустимых длин qr кодов                
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

                if (MainStaticClass.PrintingUsingLibraries == 1)
                {
                    IFptr fptr = MainStaticClass.FPTR;

                    if (!fptr.isOpened())
                    {
                        fptr.open();
                    }

                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_SHIFT_STATE);
                    fptr.queryData();
                    if (AtolConstants.LIBFPTR_SS_CLOSED == fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_SHIFT_STATE))
                    {
                        await MessageBox.Show("У вас закрыта смена вы не сможете продавать маркированный товар, будете получать ошибку 422.Необходимо сделать внесение наличных в кассу. ", "Проверка состояния смены");
                    }
                }
            }
            else
            {
                if (MainStaticClass.Use_Fiscall_Print)
                {
                    if ((MainStaticClass.SystemTaxation != 3) && (MainStaticClass.SystemTaxation != 5))
                    {
                        if (await ItcPrinted())
                        {
                            this.pay.IsEnabled = false;
                            this.checkBox_to_print_repeatedly.IsEnabled = false;
                        }
                    }
                    else if ((MainStaticClass.SystemTaxation == 3) || (MainStaticClass.SystemTaxation == 5))
                    {
                        if (await ItcPrinted())
                        {
                            this.checkBox_to_print_repeatedly.IsEnabled = false;
                        }
                        if (await ItcPrintedP())
                        {
                            this.checkBox_to_print_repeatedly_p.IsEnabled = false;
                        }
                        if (await ItcPrinted() && await this.ItcPrintedP())
                        {
                            this.pay.IsEnabled = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Устанавливает режим "только для чтения" для всей формы
        /// </summary>
        private void SetFormReadOnly(bool readOnly)
        {
            try
            {
                Console.WriteLine($"Установка режима только для чтения: {readOnly}");

                // 1. Делаем недоступными основные элементы управления
                if (txtB_search_product != null)
                    txtB_search_product.IsEnabled = !readOnly;

                if (client_barcode != null)
                    client_barcode.IsEnabled = !readOnly;

                if (pay != null)
                    pay.IsEnabled = !readOnly && MainStaticClass.Code_right_of_user == 1;

                if (check_type != null)
                    check_type.IsEnabled = !readOnly;

                if (comment != null)
                    comment.IsEnabled = !readOnly;

                if (txtB_inn != null)
                    txtB_inn.IsEnabled = !readOnly;

                if (txtB_name != null)
                    txtB_name.IsEnabled = !readOnly;

                if (btn_get_name != null)
                    btn_get_name.IsEnabled = !readOnly;

                // 2. Делаем Grid товаров недоступным
                if (_productsScrollViewer != null)
                {
                    _productsScrollViewer.IsEnabled = !readOnly;
                    _productsScrollViewer.Focusable = !readOnly;
                }

                if (_productsTableGrid != null)
                {
                    _productsTableGrid.IsEnabled = !readOnly;
                    _productsTableGrid.Focusable = !readOnly;
                }

                // 3. Добавляем визуальный индикатор только для чтения
                if (readOnly)
                {
                    // Меняем цвет фона у элементов
                    if (_productsScrollViewer != null)
                        _productsScrollViewer.Background = Brushes.LightGray;

                    if (_productsTableGrid != null)
                        _productsTableGrid.Background = Brushes.LightGray;

                    // Добавляем водяной знак "ТОЛЬКО ДЛЯ ЧТЕНИЯ"
                    AddReadOnlyWatermark();
                }
                else
                {
                    // Возвращаем нормальные цвета
                    if (_productsScrollViewer != null)
                        _productsScrollViewer.Background = Brushes.White;

                    if (_productsTableGrid != null)
                        _productsTableGrid.Background = Brushes.White;

                    // Убираем водяной знак
                    RemoveReadOnlyWatermark();
                }

                // 4. Делаем все элементы управления недоступными для клавиатуры
                if (readOnly)
                {
                    // Отписываемся от событий клавиатуры для Grid товаров
                    this.RemoveHandler(KeyDownEvent, OnGlobalKeyDownForProducts);
                }
                else
                {
                    // Подписываемся на события клавиатуры для Grid товаров
                    this.AddHandler(KeyDownEvent, OnGlobalKeyDownForProducts, RoutingStrategies.Tunnel);
                }

                Console.WriteLine($"✓ Режим только для чтения установлен: {readOnly}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при установке режима только для чтения: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавляет водяной знак "ТОЛЬКО ДЛЯ ЧТЕНИЯ" на Grid
        /// </summary>
        private void AddReadOnlyWatermark()
        {
            try
            {
                if (_productsTableGrid == null) return;

                // Создаем TextBlock с водяным знаком
                var watermark = new TextBlock
                {
                    Text = "ТОЛЬКО ДЛЯ ЧТЕНИЯ",
                    FontSize = 48,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.LightGray,
                    Opacity = 0.3,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransform = new RotateTransform(-30),
                    IsHitTestVisible = false
                };

                // Добавляем как последний элемент Grid
                Grid.SetColumnSpan(watermark, 11);
                Grid.SetRowSpan(watermark, _productsTableGrid.RowDefinitions.Count);
                _productsTableGrid.Children.Add(watermark);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при добавлении водяного знака: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаляет водяной знак "ТОЛЬКО ДЛЯ ЧТЕНИЯ" с Grid
        /// </summary>
        private void RemoveReadOnlyWatermark()
        {
            try
            {
                if (_productsTableGrid == null) return;

                // Ищем и удаляем водяной знак
                var watermark = _productsTableGrid.Children
                    .FirstOrDefault(c => c is TextBlock textBlock && textBlock.Text == "ТОЛЬКО ДЛЯ ЧТЕНИЯ");

                if (watermark != null)
                {
                    _productsTableGrid.Children.Remove(watermark);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при удалении водяного знака: {ex.Message}");
            }
        }



        private async void Cash_check_Loaded(object? sender, EventArgs e)
        {
            this.num_cash.Text = "КАССА № " + MainStaticClass.CashDeskNumber.ToString();
            this.num_cash.Tag = MainStaticClass.CashDeskNumber;

            //Создание таблицы для перераспределения акций
            DataColumn dc = new DataColumn("Code", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Tovar", System.Type.GetType("System.String"));
            table.Columns.Add(dc);
            dc = new DataColumn("Quantity", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Price", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("PriceAtDiscount", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("Sum", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("SumAtDiscount", System.Type.GetType("System.Decimal"));
            table.Columns.Add(dc);
            dc = new DataColumn("Action", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Gift", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);
            dc = new DataColumn("Action2", System.Type.GetType("System.Int32"));
            table.Columns.Add(dc);

            //this.inputbarcode.Focus();
            this.txtB_search_product.Focus();

            if (MainStaticClass.GetVersionFn == 1)
            {
                checkBox_print_check.IsVisible = false;
            }
            checkBox_print_check.IsChecked = true;

            if (IsNewCheck)
            {
                guid = Guid.NewGuid().ToString();


                checkBox_to_print_repeatedly.IsVisible = false;
                //label9.Visible = false;
                //label10.Visible = false;
                //label11.Visible = false;
                //label13.Visible = false;
                txtB_non_cash_money.IsVisible = false;
                txtB_sertificate_money.IsVisible = false;
                txtB_cash_money.IsVisible = false;
                txtB_bonus_money.IsVisible = false;

                //inputbarcode.Focus();
                this.txtB_search_product.Focus();


                this.date_time_start.Text = "Чек   " + DateTime.Now.ToString("yyy-MM-dd HH:mm:ss");
                this.Discount = 0;
                this.user.Text = MainStaticClass.Cash_Operator;
                this.user.Tag = MainStaticClass.Cash_Operator_Client_Code;//gaa поменять на инн
                numdoc = get_new_number_document();
                if (numdoc == 0)
                {
                    MessageBox.Show("Ошибка при получении номера документа.", "Проверка при получении номер документа");
                    MainStaticClass.WriteRecordErrorLog("Ошибка при получении номера документа", "Cash_check_Load", 0, MainStaticClass.CashDeskNumber, "При вводе нового документа получен нулевой номер");
                    this.Close();
                }
                this.txtB_num_doc.Text = this.numdoc.ToString();
                MainStaticClass.write_event_in_log(" Ввод нового документа ", "Документ чек", numdoc.ToString());
                this.check_type.SelectedIndex = 0;
                this.check_type.IsEnabled = true;
                set_sale_disburse_button();
            }
            else
            {
                reopened = true;
                checkBox_print_check.IsEnabled = false;
                //Документ не новый поэтому запретим в нем ввод и изменение                
                last_tovar.IsEnabled = false;
                //txtB_email_telephone.Enabled = false;
                txtB_inn.IsEnabled = false;
                btn_get_name.IsEnabled = false;
                //txtB_client_phone.Enabled = false;
                txtB_name.IsEnabled = false;
                comment.IsEnabled = false;

                int status = get_its_deleted_document();
                if ((status == 0) || (status == 1))
                {
                    //this.type_pay.Enabled = false;
                    //this.label4.Enabled = false;
                    this.check_type.IsEnabled = false;
                    //this.inputbarcode.Enabled = false;
                    this.txtB_search_product.IsEnabled = false;
                    this.client_barcode.IsEnabled = false;
                    //this.sale_cancellation.Enabled = false;
                    //this.inventory.Enabled = false;
                    //this.comment.Enabled = false;
                    //to_open_the_written_down_document();
                    enable_print();
                    if (MainStaticClass.Code_right_of_user != 1)
                    {
                        this.pay.IsEnabled = false;
                    }
                    //itsnew = true;
                }
                else if (status == 2)
                {
                    IsNewCheck = true;
                    Discount = 0;
                    //this.label4.Enabled = true;
                    this.check_type.IsEnabled = true;
                    //this.inputbarcode.Enabled = true;
                    this.txtB_search_product.IsEnabled = true;
                    this.client_barcode.IsEnabled = false;
                    ToOpenTheWrittenDownDocument();
                    get_old_document_Discount();
                    check_type.IsEnabled = false;
                    IsNewCheck = true;
                }
            }

            //this.Top = 0;
            //this.Left = 0;
            //this.Size = new System.Drawing.Size(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);
            //this.panel2.Left = 0;
            //this.listView2.Left = 20;

            //this.panel2.Size = new System.Drawing.Size(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height / 2);
            //this.listView2.Size = new System.Drawing.Size(SystemInformation.PrimaryMonitorSize.Width - 50, SystemInformation.PrimaryMonitorSize.Height / 2 - 50);


            if (IsNewCheck)
            {
                //first_start_com_barcode_scaner();
                selection_goods = true;
                //inputbarcode.Focus();
                this.txtB_search_product.Focus();
                //список допустимых длин qr кодов                
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

                if (MainStaticClass.PrintingUsingLibraries == 1)
                {
                    IFptr fptr = MainStaticClass.FPTR;

                    if (!fptr.isOpened())
                    {
                        fptr.open();
                    }

                    fptr.setParam(AtolConstants.LIBFPTR_PARAM_DATA_TYPE, AtolConstants.LIBFPTR_DT_SHIFT_STATE);
                    fptr.queryData();
                    if (AtolConstants.LIBFPTR_SS_CLOSED == fptr.getParamInt(AtolConstants.LIBFPTR_PARAM_SHIFT_STATE))
                    {
                        MessageBox.Show("У вас закрыта смена вы не сможете продавать маркированный товар, будете получать ошибку 422.Необходимо сделать внесение наличных в кассу. ", "Проверка состояния смены");
                    }
                }
            }
            else
            {
                if (MainStaticClass.Use_Fiscall_Print)
                {
                    if ((MainStaticClass.SystemTaxation != 3) && (MainStaticClass.SystemTaxation != 5))
                    {
                        if (await ItcPrinted())
                        {
                            this.pay.IsEnabled = false;
                            this.checkBox_to_print_repeatedly.IsEnabled = false;
                        }
                    }
                    else if ((MainStaticClass.SystemTaxation == 3) || (MainStaticClass.SystemTaxation == 5))
                    {
                        if (await ItcPrinted())
                        {
                            this.checkBox_to_print_repeatedly.IsEnabled = false;
                        }
                        if (await ItcPrintedP())
                        {
                            this.checkBox_to_print_repeatedly_p.IsEnabled = false;
                        }
                        if (await ItcPrinted() && await this.ItcPrintedP())
                        {
                            this.pay.IsEnabled = false;
                        }
                    }
                }
            }
        }

        private void Cash_check_Closed(object? sender, EventArgs e)
        {
            
        }

        public double Discount
        {
            get
            {
                return discount;
            }
            set
            {
                discount = value;
            }
        }


        public void OnFormLoaded()
        {
            Console.WriteLine("Форма чека загружена и данные инициализированы");
            
            // 3. Инициализируем данные формы
            InitializeFormData();

            // 4. Отладочная информация
            //DebugGridInfo();
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

                    case Key.Escape:
                        e.Handled = true;
                        this.Close();                        
                        break;

                    case Key.F8:
                        this.Pay_Click(null, null);
                        e.Handled = true;                    
                        break;
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

                CheckType = this.FindControl<ComboBox>("check_type");

                if (CheckType != null)
                {
                    CheckType.SelectionChanged += CheckType_SelectionChanged;
                }

                Client = this.FindControl<TextBox>("client");
                NumCash = this.FindControl<TextBox>("num_cash");
                User = this.FindControl<TextBox>("user");
                Comment= this.FindControl<TextBox>("comment");

                ClientBarcodeOrPhone = this.FindControl<TextBox>("client_barcode");
                if (ClientBarcodeOrPhone != null)
                {
                    ClientBarcodeOrPhone.KeyDown += ClientBarcodeOrPhone_KeyDown;
                }
                //client_barcode
                NumSales = this.FindControl<TextBox>("txtB_num_sales");
                InputSearchProduct = this.FindControl<TextBox>("txtB_search_product");
                InputSearchProduct.Focus();

                // Подписка на события поиска товара
                if (InputSearchProduct != null)
                {
                    InputSearchProduct.KeyDown += InputSearchProduct_KeyDown;
                    // Или: InputSearchProduct.KeyDown += OnSearchProductKeyDown;
                }

                Pay = this.FindControl<Button>("pay");

                Pay.Click += Pay_Click;



                _tabProducts = this.FindControl<TabItem>("tabProducts");
                _tabCertificates = this.FindControl<TabItem>("tabCertificates");

                Console.WriteLine("✓ Все основные контролы проверены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке контролов: {ex.Message}");
            }
        }

        /// <summary>
        /// Создать резервную копию данных товаров (аналог клонирования ListView)
        /// </summary>
        private void BackupProductsData()
        {
            MainStaticClass.write_event_in_log(" Копируем табличную часть в резервную копию ", "Документ чек", numdoc.ToString());

            // Очищаем старую резервную копию
            _productsDataBackup.Clear();

            // Глубокое копирование каждого элемента
            foreach (var product in _productsData)
            {
                var backupItem = new ProductItem
                {
                    Code = product.Code,
                    Tovar = product.Tovar,
                    Quantity = product.Quantity,
                    Price = product.Price,
                    PriceAtDiscount = product.PriceAtDiscount,
                    Sum = product.Sum,
                    SumAtDiscount = product.SumAtDiscount,
                    Action = product.Action,
                    Gift = product.Gift,
                    Action2 = product.Action2,
                    Mark = product.Mark
                };

                // Копируем дополнительные данные, если они есть
                // Например, если у вас есть Tag или другие поля:
                // backupItem.Tag = product.Tag;

                _productsDataBackup.Add(backupItem);
            }

            Console.WriteLine($"✓ Создана резервная копия: {_productsDataBackup.Count} записей");
        }

        /// <summary>
        /// Восстановить данные из резервной копии
        /// </summary>
        private void RestoreProductsData()
        {
            MainStaticClass.write_event_in_log(" Восстанавливаем табличную часть из резервной копии ", "Документ чек", numdoc.ToString());

            // Очищаем текущие данные
            _productsData.Clear();

            // Копируем из резервной копии
            foreach (var backupItem in _productsDataBackup)
            {
                var restoredItem = new ProductItem
                {
                    Code = backupItem.Code,
                    Tovar = backupItem.Tovar,
                    Quantity = backupItem.Quantity,
                    Price = backupItem.Price,
                    PriceAtDiscount = backupItem.PriceAtDiscount,
                    Sum = backupItem.Sum,
                    SumAtDiscount = backupItem.SumAtDiscount,
                    Action = backupItem.Action,
                    Gift = backupItem.Gift,
                    Action2 = backupItem.Action2,
                    Mark = backupItem.Mark
                };

                _productsData.Add(restoredItem);
            }

            // Обновляем Grid
            RefreshProductsGrid();

            // Выделяем первую строку если есть данные
            if (_productsData.Count > 0)
            {
                SelectProductRow(0);
            }

            Console.WriteLine($"✓ Восстановлено из резервной копии: {_productsData.Count} записей");
        }

        /// <summary>
        /// Очистить резервную копию
        /// </summary>
        private void ClearBackup()
        {
            _productsDataBackup.Clear();
            Console.WriteLine("✓ Резервная копия очищена");
        }

        private async void Pay_Click(object? sender, RoutedEventArgs e)
        {
            if (MainStaticClass.PrintingUsingLibraries == 1)
            {
                if (MainStaticClass.GetFiscalsForbidden)
                {
                    await MessageBox.Show("Вам запрещена печать на фискальном регистраторе", "Проверки при печати", MessageBoxButton.OK, MessageBoxType.Error);
                    return;
                }
            }


            if (IsNewCheck)
            {
                // ПРОВЕРКА 1: Количество товаров в чеке (новый чек)
                if (CheckType.SelectedIndex == 0) // Только для чека "Продажа"
                {
                    int productCount = _productsData.Count; // Используем коллекцию данных

                    if (productCount < 3)
                    {
                        await MessageBox.Show("В чеке менее 3 строк, предложить покупателю доп.товар.",
                                             "В чеке менее 3 строк",
                                             MessageBoxButton.OK,
                                             MessageBoxType.Info);
                        //return; // Прерываем дальнейшее выполнение
                    }
                }
            }

            recharge_note = "";
            print_to_button = 1;
            // Дополнительные проверки из вашего оригинального кода
            if (_productsData.Count == 0)
            {
                await MessageBox.Show("Нет строк", "Проверки перед записью документа");
                return;
            }


            if (await GetItsDeletedDocument() == 1)
            {
                await MessageBox.Show("Удаленный чек не может быть распечатан", "Проверка при печати", MessageBoxButton.OK, MessageBoxType.Error);
                return;
            }

            TextBox InnText = this.FindControl<TextBox>("txtB_inn");
            TextBox NameOrgText = this.FindControl<TextBox>("txtB_name");

            // Проверка на null контролов
            if (InnText == null || NameOrgText == null) return;

            // Безопасное получение текста
            string inn = (InnText.Text ?? "").Trim();
            string name = (NameOrgText.Text ?? "").Trim();

            // Проверка заполнения полей (только одно из двух заполнено)
            bool onlyInnFilled = !string.IsNullOrEmpty(inn) && string.IsNullOrEmpty(name);
            bool onlyNameFilled = string.IsNullOrEmpty(inn) && !string.IsNullOrEmpty(name);

            if (onlyInnFilled || onlyNameFilled)
            {
                await MessageBox.Show("Если заполнен ИНН, то должно быть заполнено и наименование, и наоборот",
                                     "Проверка при печати",
                                     MessageBoxButton.OK,
                                     MessageBoxType.Error);
                return;
            }



            if (IsNewCheck)
            {
                //kitchen_print(this);
                show_pay_form();
            }
            else
            {
                if (MainStaticClass.Use_Fiscall_Print)
                {
                    if ((MainStaticClass.SystemTaxation != 3) && (MainStaticClass.SystemTaxation != 5))
                    {
                        if (!await ItcPrinted())
                        {
                            if (this.check_type.SelectedIndex == 0)
                            {
                                fiscall_print_pay(this.p_sum_doc);
                            }
                            else
                            {
                                //fiscall_print_disburse(txtB_cash_money.Text, txtB_non_cash_money.Text);
                            }
                        }
                    }
                    else if ((MainStaticClass.SystemTaxation == 3) || (MainStaticClass.SystemTaxation == 5))
                    {
                        if (!await ItcPrinted() || !await ItcPrintedP())
                        {
                            if (this.check_type.SelectedIndex == 0)
                            {
                                fiscall_print_pay(this.p_sum_doc);
                            }
                            else
                            {
                                //                fiscall_print_disburse(txtB_cash_money.Text, txtB_non_cash_money.Text);
                            }
                        }

                        //    }
                    }


                    this.Close();
                }
            }

        }

        // Метод для показа Avalonia диалога из WinForms
        //private async Task<bool?> ShowAvaloniaDialog(Pay payForm)
        //{
        //    try
        //    {
        //        // Создаем окно Avalonia для размещения UserControl
        //        var dialogWindow = new Window
        //        {
        //            Title = "Оплата",
        //            Width = 800,
        //            Height = 600,
        //            Content = payForm,
        //            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        //            CanResize = false, // Запрещаем изменение размера если нужно
        //            SizeToContent = SizeToContent.Manual
        //        };

        //        // Добавляем кнопки в окно если нужно
        //        // Или используем кнопки из вашего UserControl

        //        // Показываем как диалоговое окно
        //        return await dialogWindow.ShowDialog<bool?>(GetAvaloniaMainWindow());
        //    }
        //    catch (Exception ex)
        //    {
        //        MainStaticClass.write_event_in_log($"Ошибка показа формы Avalonia: {ex.Message}", "Ошибка", numdoc.ToString());
        //        return null;
        //    }
        //}

        private async void show_pay_form()
        {

            MainStaticClass.write_event_in_log("Попытка перейти в окно оплаты", "Документ чек", numdoc.ToString());
            pay_form.InitializeComponent();
            pay_form.pay_bonus.Text = "0";
            pay_form.pay_bonus.IsVisible = false;
            pay_form.pay_bonus_many.Text = "0";
            pay_form.pay_bonus.IsEnabled = false;

            //listView_sertificates.Items.Clear();
            //pay_form.listView_sertificates.Items.Clear();
            pay_form.cc = this;
            //DialogResult dr;

            if (this.check_type.SelectedIndex == 0)
            {

                /*Копируем табличную часть один ListView в другой
                 *  чтобы если оплата отменится и с чеком будут дальше работать
                 *  отменить все расчитанные акции одним махом
                 */
                MainStaticClass.write_event_in_log(" Копируем табличную часть один ListView в другой ", "Документ чек", numdoc.ToString());
                //listview_original.Items.Clear();
                //for (int x = 0; x < listView1.Items.Count; x++)
                //{
                //    ListViewItem lvi = (ListViewItem)listView1.Items[x].Clone();
                //    lvi.SubItems[2].Tag = listView1.Items[x].SubItems[2].Tag;
                //    listview_original.Items.Add(lvi);

                //}
                BackupProductsData();

                MainStaticClass.write_event_in_log(" Попытка обработать акции по штрихкодам ", "Документ чек", numdoc.ToString());

                //DataTable dataTable = to_define_the_action_dt(true);//Обработка на дисконтные акции с использованием datatable 
                //checkSumOnDocument(dataTable);Пока закомментирую проблема если есть карта клиента, надо отлаживать  
                //

                //рассчитанные данные в памяти по акциям теперь помещаем в листвью 
                //listView1.Items.Clear();
                //foreach (DataRow row in dataTable.Rows)
                //{
                //    ListViewItem lvi = new ListViewItem(row["tovar_code"].ToString());
                //    lvi.Tag = row["tovar_code"].ToString();
                //    lvi.SubItems.Add(row["tovar_name"].ToString());//Наименование                        
                //    lvi.SubItems.Add(row["characteristic_name"].ToString());//Характеристика
                //    lvi.SubItems[2].Tag = row["characteristic_code"].ToString();
                //    lvi.SubItems.Add(row["quantity"].ToString());//Количество
                //    //lvi.SubItems.Add(row["price"].ToString());//Цена без скидки
                //    //lvi.SubItems.Add(row["price_at_discount"].ToString("F2"));//Цена Со скидкой
                //    //lvi.SubItems.Add(row["sum_full"].ToString());//Сумма без скидки
                //    //lvi.SubItems.Add(row["sum_at_discount"].ToString());//Сумма со скидкой
                //    lvi.SubItems.Add(Convert.ToDecimal(row["price"]).ToString("F2"));//Цена без скидки
                //    lvi.SubItems.Add(Convert.ToDecimal(row["price_at_discount"]).ToString("F2"));//Цена Со скидкой
                //    lvi.SubItems.Add(Convert.ToDecimal(row["sum_full"]).ToString("F2"));//Сумма без скидки
                //    lvi.SubItems.Add(Convert.ToDecimal(row["sum_at_discount"]).ToString("F2"));//Сумма со скидкой
                //    lvi.SubItems.Add(row["action"].ToString());//Акционный документ
                //    lvi.SubItems.Add(row["gift"].ToString());//Акционный документ
                //    lvi.SubItems.Add(row["action2"].ToString());//Акционный документ
                //    lvi.SubItems.Add(row["bonus_reg"].ToString());//Бонус
                //    lvi.SubItems.Add(row["bonus_action"].ToString());//Бонус
                //    lvi.SubItems.Add(row["bonus_action_b"].ToString());//Бонус
                //    lvi.SubItems.Add(row["marking"].ToString());//Маркировка

                //    listView1.Items.Add(lvi);
                //}

                selection_goods = false;

                MainStaticClass.write_event_in_log(" Попытка пересчитать чек ", "Документ чек", numdoc.ToString());
                //recalculate_all();
                // КОНЕЦ ПРОВЕРОЧНЫЙ ПЕРЕСЧЕТ ПО АКЦИЯМ               

                //MessageBox.Show(calculation_of_the_sum_of_the_document().ToString());
                //MessageBox.Show(calculation_of_the_sum_of_the_document().ToString("F", System.Globalization.CultureInfo.CurrentCulture));

                pay_form.pay_sum.Text = calculation_of_the_sum_of_the_document().ToString("F", System.Globalization.CultureInfo.CurrentCulture);

                write_new_document("0", calculation_of_the_sum_of_the_document().ToString(), "0", "0", false, "0", "0", "0", "0", false);//нужно для того чтобы в окне оплаты взять сумму из БД
            }
            else//Это возврат
            {
                pay_form.pay_sum.Text = calculation_of_the_sum_of_the_document().ToString("F", System.Globalization.CultureInfo.CurrentCulture);
            }

            pay_form.txtB_cash_sum.Focus();

            //При переходе в окно оплаты цены должны быть отрисованы
            SendDataToCustomerScreen(1, 1, 1);

            // Создаем окно оплаты
            //var payWindow = new Pay();
            //payWindow.InitializeComponent();

            // Настраиваем данные в окне
            pay_form.Title = "Оплата";
            pay_form.Width = 800;  // Размеры как в XAML
            pay_form.Height = 600;
            pay_form.CanResize = false;  // Скорее всего это уже в XAML

            // Устанавливаем сумму чека и другие данные
            //payWindow.pay_sum.Text = calculation_of_the_discount_of_the_document().ToString("N2");  // Или через публичное свойство

            // Находим активное окно
            Window parentWindow = null;

            // Вариант 1: Через TopLevel
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window currentWindow)
            {
                parentWindow = currentWindow;
            }

            // Вариант 2: Через Application
            if (parentWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                parentWindow = desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
            }

            // Подписываемся на событие закрытия если нужно
            pay_form.Closed += (s, e) =>
            {
                // Проверяем результат через Tag или другое свойство
                bool? paymentSuccess = pay_form.Tag as bool?;
                if (paymentSuccess == true)
                {
                    // Обработка успешной оплаты
                    Console.WriteLine("Оплата прошла успешно");
                }
            };

            // Устанавливаем позиционирование
            if (parentWindow != null)
            {
                pay_form.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // Показываем как диалог
                await pay_form.ShowDialog(parentWindow);
            }
            else
            {
                pay_form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                pay_form.Show();
            }

            //dr = pay_form.ShowDialog();

            //if (dr == DialogResult.OK)
            //{
            //    
            //}
            if (Convert.ToBoolean(pay_form.Tag)==true)
            {
                this.Close();
            }

            this.txtB_search_product.Focus();
            pay_form = new Pay();
        }

        public bool ValidateCheckSumAtDiscount()
        {
            bool result = true;
            foreach (var product in _productsData)
            {
                if (product.SumAtDiscount <= 0)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }


        /// <summary>
        /// variant это форма оплаты
        /// 0 наличные        
        /// 1 карта
        /// 2 сертификат
        /// </summary>
        /// <param name="variant"></param>
        /// <returns></returns>
        public double[] get_summ1_systemtaxation3(int variant)
        {
            double[] result = new double[3];

            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "";
                if (variant == 0)
                {
                    query = "SELECT SUM(cash_money - cash_money1),SUM(non_cash_money - non_cash_money1),SUM(sertificate_money - sertificate_money1) FROM checks_header WHERE document_number=" + numdoc;
                }
                else
                {
                    query = "SELECT cash_money1,non_cash_money1,sertificate_money1 FROM checks_header WHERE document_number=" + numdoc.ToString();
                }

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result[0] = Convert.ToDouble(reader[0]);
                    result[1] = Convert.ToDouble(reader[1]);
                    result[2] = Convert.ToDouble(reader[2]);
                }
                reader.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(" Ошибки при чтении 2 суммы " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Ошибки при чтении 2 суммы " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            return result;
        }



        /// <summary>
        /// печть возвратной накладной
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void sale_cancellation_Click(string cash_money, string non_cash_money)
        {
            if (_productsData.Count == 0)
            {
                await MessageBox.Show(" Нет строк ", " Проверки перед записью документа ");
                return;
            }

            if (MainStaticClass.Use_Fiscall_Print)
            {
                //fiscall_print_disburse(cash_money, non_cash_money);
            }
        }


        public async Task<bool> it_is_paid(string pay, string sum_doc, string remainder, string pay_bonus_many, bool last_rewrite, string cash_money, string non_cash_money, string sertificate_money)
        {
            //Здесь необходимо добавить проверку на то что документ уже не новый
            bool result = true;

            if (IsNewCheck)
            {
                MainStaticClass.write_event_in_log(" Финальная запись документа ", "Документ чек", numdoc.ToString());
                result = await write_new_document(pay, sum_doc, remainder, pay_bonus_many, last_rewrite, cash_money, non_cash_money, sertificate_money, "0");
                //if (result)
                //{
                //    if (MainStaticClass.Use_Usb_to_Com_Barcode_Scaner)
                //    {
                //        if (workerThread != null)//При нажатии клавиши ESC уже могло все завершится
                //        {
                //            stop_com_barcode_scaner();
                //            this.timer.Stop();
                //            this.timer = null;
                //            workerThread = null;
                //            rd = null;
                //            GC.Collect();
                //        }
                //    }
                //}
            }

            if (result)
            {                
                if (MainStaticClass.Use_Fiscall_Print)
                {
                    MainStaticClass.write_event_in_log("Попытка распечатать чек ", "Документ чек", numdoc.ToString());
                    fiscall_print_pay(pay);
                }
            }

            return result;
           
        }

        public decimal calculation_of_the_sum_of_the_document()
        {
            // В вашем коде есть вызовы этого метода, например:
            // pay_form.pay_sum.Text = calculation_of_the_sum_of_the_document().ToString("F", System.Globalization.CultureInfo.CurrentCulture);

            decimal total = 0;
            foreach (var product in _productsData)
            {
                total += product.SumAtDiscount;
            }

            return total;
        }

        /// <summary>
        /// Получение сумм по типам оплаты для 3 типа налогообложения
        /// расчет сумм идет перед записью и храниться в базе 
        /// возвращаются суммы по формам оплаты только для 2 чека
        /// </summary>
        private async Task<double[]> get_cash_on_type_payment_3_new(double sum_cash, double sum_non_cashe, double sum_sertificate)
        {
            double[] result = new double[3];
            result[0] = sum_cash;
            result[1] = sum_non_cashe;
            result[2] = sum_sertificate;

            double[] result_variant_1 = new double[3];
            result_variant_1[0] = sum_cash;
            result_variant_1[1] = sum_non_cashe;
            result_variant_1[2] = sum_sertificate;

            try
            {
                // Часть 1: Товары без маркировки (длина маркировки < 14 символов)
                double sum_print = 0;

                // Заменяем: foreach (ListViewItem lvi in listView1.Items)
                // На: foreach (var product in _productsData)
                foreach (var product in _productsData)
                {
                    // lvi.SubItems[14].Text = продукт.Mark (колонка 14 = маркировка)
                    // lvi.SubItems[7].Text = продукт.Sum (колонка 7 = сумма)
                    if (product.Mark?.Trim().Length < 14)
                    {
                        sum_print += (double)product.Sum;
                    }
                }

                if (result[0] > 0)
                {
                    if (result[0] >= sum_print)
                    {
                        result[0] = sum_print;
                        sum_print = 0; // Вся сумма распределена
                        result[1] = 0;
                        result[2] = 0;
                    }

                    if (result[0] < sum_print)
                    {
                        sum_print = Math.Round(sum_print - result[0], 2, MidpointRounding.AwayFromZero);
                    }
                }

                if (result[1] > 0)
                {
                    if (result[1] >= sum_print)
                    {
                        result[1] = sum_print;
                        sum_print = 0; // Вся сумма распределена
                        result[2] = 0;
                    }

                    if (result[1] < sum_print)
                    {
                        sum_print = Math.Round(sum_print - result[1], 2, MidpointRounding.AwayFromZero);
                    }
                }

                if (result[2] > 0)
                {
                    if (result[2] >= sum_print)
                    {
                        result[2] = sum_print;
                        sum_print = 0; // Вся сумма распределена
                    }

                    if (result[2] < sum_print)
                    {
                        sum_print = Math.Round(sum_print - result[2], 2, MidpointRounding.AwayFromZero);
                    }
                }

                result_variant_1[0] = Math.Round(result_variant_1[0] - result[0], 2, MidpointRounding.AwayFromZero);
                result_variant_1[1] = Math.Round(result_variant_1[1] - result[1], 2, MidpointRounding.AwayFromZero);
                result_variant_1[2] = Math.Round(result_variant_1[2] - result[2], 2, MidpointRounding.AwayFromZero);

                result[0] = result_variant_1[0];
                result[1] = result_variant_1[1];
                result[2] = result_variant_1[2];

                // Часть 2: Товары с маркировкой (длина маркировки > 13 символов)
                sum_print = 0;

                // Вторая итерация по товарам с маркировкой
                foreach (var product in _productsData)
                {
                    // Проверка длины маркировки (> 13 символов)
                    if (!string.IsNullOrEmpty(product.Mark) && product.Mark.Trim().Length > 13)
                    {
                        sum_print += (double)product.Sum;
                    }
                }

                if (result[0] > 0)
                {
                    if (result[0] >= sum_print)
                    {
                        result[0] = sum_print;
                        sum_print = 0; // Вся сумма распределена
                        result[1] = 0;
                        result[2] = 0;
                    }

                    if (result[0] < sum_print)
                    {
                        sum_print = Math.Round(sum_print - result[0], 2, MidpointRounding.AwayFromZero);
                    }
                }

                if (result[1] > 0)
                {
                    if (result[1] >= sum_print)
                    {
                        result[1] = sum_print;
                        sum_print = 0; // Вся сумма распределена
                        result[2] = 0;
                    }

                    if (result[1] < sum_print)
                    {
                        sum_print = Math.Round(sum_print - result[1], 2, MidpointRounding.AwayFromZero);
                    }
                }

                if (result[2] > 0)
                {
                    if (result[2] >= sum_print)
                    {
                        result[2] = sum_print;
                        sum_print = 0; // Вся сумма распределена
                    }

                    if (result[2] < sum_print)
                    {
                        sum_print = Math.Round(sum_print - result[2], 2, MidpointRounding.AwayFromZero);
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show("Произошли ошибки при получении сумм по типам оплаты: " + ex.Message,
                                    "Ошибка БД",
                                    MessageBoxButton.OK,
                                    MessageBoxType.Error);
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Произошли ошибки при получении сумм по типам оплаты: " + ex.Message,
                                    "Ошибка",
                                    MessageBoxButton.OK,
                                    MessageBoxType.Error);
            }

            return result;
        }

        public decimal calculation_of_the_discount_of_the_document()
        {
            // Этот метод, вероятно, рассчитывает общую сумму скидки
            decimal totalDiscount = 0;
            foreach (var product in _productsData)
            {
                decimal itemDiscount = (product.Sum - product.SumAtDiscount);
                totalDiscount += itemDiscount;
            }

            return totalDiscount;
        }

        /// <summary>
        /// процедура для записи обычного документа
        /// </summary>
        public async Task<bool> write_new_document(string pay, string sum_doc, string remainder, string pay_bonus_many,
                                                  bool last_rewrite, string cash_money, string non_cash_money,
                                                  string sertificate_money, string its_deleted, bool sendToScreen = true)
        {
            if ((sum_doc == "") || (sum_doc == "0"))
            {
                sum_doc = calculation_of_the_sum_of_the_document().ToString();
            }
            bonuses_it_is_written_off = Convert.ToDecimal(pay_bonus_many);
            bool result = false;

            // Проверяем общее количество строк (товары + сертификаты)
            if (_productsData.Count == 0 && _certificatesData.Count == 0)
            {
                return result;
            }

            double[] sum1 = new double[3];
            sum1[0] = 0;
            sum1[1] = 0;
            sum1[2] = 0;

            if ((MainStaticClass.SystemTaxation == 3) || (MainStaticClass.SystemTaxation == 5))
            {
                if (last_rewrite)
                {
                    sum1 = await get_cash_on_type_payment_3_new(
                        Convert.ToDouble(cash_money.Replace(".", ",")),
                        Convert.ToDouble(non_cash_money.Replace(".", ",")),
                        Convert.ToDouble(sertificate_money.Replace(".", ",")));
                }
            }

            NpgsqlConnection conn = null;
            NpgsqlTransaction tran = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                tran = conn.BeginTransaction();
                NpgsqlCommand command = new NpgsqlCommand(
                    "DELETE FROM checks_table WHERE document_number=@num_doc;" +
                    "DELETE FROM checks_header WHERE document_number=@num_doc;",
                    conn);

                command.Parameters.AddWithValue("num_doc", numdoc);
                command.Transaction = tran;
                command.ExecuteNonQuery();

                date_time_write = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                command = new NpgsqlCommand("INSERT INTO checks_header(" +
                                        "document_number," +
                                        "date_time_start," +
                                        "client," +
                                        "cash_desk_number," +
                                        "comment," +
                                        "cash," +
                                        "remainder," +
                                        "date_time_write," +
                                        "discount," +
                                        "autor," +
                                        "its_deleted," +
                                        "action_num_doc," +
                                        "check_type," +
                                        "have_action," +
                                        "bonuses_it_is_written_off," +
                                        "is_sent," +
                                        "cash_money," +
                                        "non_cash_money," +
                                        "sertificate_money," +
                                        "id_transaction," +
                                        //"bonus_is_on," +
                                        "its_print," +
                                        "id_transaction_sale," +
                                        "clientInfo_vatin," +
                                        "clientInfo_name," +
                                        "id_sale," +
                                        "sent_to_processing_center," +
                                        "requisite," +
                                        "bonuses_it_is_counted," +
                                        //"viza_d,"+
                                        "id_transaction_terminal," +
                                        "system_taxation," +
                                        "code_authorization_terminal," +
                                        "cash_money1," +
                                        "non_cash_money1," +
                                        "sertificate_money1," +
                                        "guid," +
                                        //"guid1," +
                                        "payment_by_sbp) VALUES(" +

                                        "@document_number," +
                                        "@date_time_start," +
                                        "@client," +
                                        "@cash_desk_number," +
                                        "@comment," +
                                        "@cash," +
                                        "@remainder," +
                                        "@date_time_write," +
                                        "@discount," +
                                        "@autor," +
                                        "@its_deleted," +
                                        "@action_num_doc," +
                                        "@check_type," +
                                        "@have_action," +
                                        "@bonuses_it_is_written_off," +
                                        "@is_sent," +
                                        "@cash_money," +
                                        "@non_cash_money," +
                                        "@sertificate_money," +
                                        "@id_transaction," +
                                        //"@bonus_is_on," +
                                        "@its_print," +
                                        "@id_transaction_sale," +
                                        "@clientInfo_vatin," +
                                        "@clientInfo_name," +
                                        "@id_sale," +
                                        "@sent_to_processing_center," +
                                        "@requisite," +
                                        "@bonuses_it_is_counted," +
                                        //"@checkBox_viza_d,"+
                                        "@id_transaction_terminal," +
                                        "@system_taxation," +
                                        "@code_authorization_terminal," +
                                        "@cash_money1," +
                                        "@non_cash_money1," +
                                        "@sertificate_money1," +
                                        "@guid," +
                                        //"@guid1," +
                                        "@payment_by_sbp)", conn);

                // Заполнение параметров заголовка (оставить как было)
                command.Parameters.AddWithValue("document_number", numdoc);
                command.Parameters.AddWithValue("date_time_start", Convert.ToDateTime(date_time_start.Text.Replace("Чек", "")));
                command.Parameters.AddWithValue("client", Client.Tag?.ToString() ?? string.Empty);
                command.Parameters.AddWithValue("cash_desk_number", Convert.ToInt16(num_cash.Tag.ToString()));
                string commentValue = string.Empty;
                if (!string.IsNullOrEmpty(Comment.Text))
                {
                    commentValue = Comment.Text.Trim().Replace("'", "");
                }
                command.Parameters.AddWithValue("comment", commentValue);
                //command.Parameters.AddWithValue("cash", Convert.ToDecimal(sum_doc.Replace(",", ".")));
                command.Parameters.AddWithValue("cash", Convert.ToDecimal(sum_doc.Replace(".",",")));
                command.Parameters.AddWithValue("remainder", Convert.ToDecimal(remainder.Replace(".",",")));
                command.Parameters.AddWithValue("date_time_write", Convert.ToDateTime(date_time_write));
                command.Parameters.AddWithValue("discount", calculation_of_the_discount_of_the_document());
                command.Parameters.AddWithValue("autor", User.Tag.ToString());

                //if (its_deleted == "0")
                //{
                //    command.Parameters.AddWithValue("its_deleted", (last_rewrite ? 0 : 2).ToString());
                //}
                //else
                //{
                    command.Parameters.AddWithValue("its_deleted", Convert.ToDecimal(its_deleted));
                //}

                command.Parameters.AddWithValue("action_num_doc", action_num_doc.ToArray());
                command.Parameters.AddWithValue("check_type", CheckType.SelectedIndex);
                command.Parameters.AddWithValue("have_action", have_action);
                command.Parameters.AddWithValue("bonuses_it_is_written_off",
                    (CheckType.SelectedIndex == 1 ? Convert.ToDecimal(return_bonus) : Convert.ToDecimal(pay_bonus_many)));
                command.Parameters.AddWithValue("is_sent", 0);
                command.Parameters.AddWithValue("cash_money", Convert.ToDecimal(cash_money.Replace(".", ",")));
                command.Parameters.AddWithValue("non_cash_money", Convert.ToDecimal(non_cash_money.Replace(".", ",")));
                command.Parameters.AddWithValue("sertificate_money", Convert.ToDecimal(sertificate_money.Replace(".", ",")));
                command.Parameters.AddWithValue("id_transaction", id_transaction);
                command.Parameters.AddWithValue("its_print", false);
                command.Parameters.AddWithValue("id_transaction_sale", id_transaction_sale);
                command.Parameters.AddWithValue("clientInfo_vatin", txtB_inn.Text?.Trim() ?? string.Empty);
                command.Parameters.AddWithValue("clientInfo_name", txtB_name.Text?.Trim() ?? string.Empty);
                command.Parameters.AddWithValue("id_sale", id_sale.ToString());
                command.Parameters.AddWithValue("requisite", 0);
                command.Parameters.AddWithValue("bonuses_it_is_counted", Convert.ToDecimal(bonuses_it_is_counted));
                command.Parameters.AddWithValue("id_transaction_terminal", id_transaction_terminal);
                command.Parameters.AddWithValue("system_taxation", MainStaticClass.SystemTaxation);
                command.Parameters.AddWithValue("code_authorization_terminal", code_authorization_terminal);
                command.Parameters.AddWithValue("cash_money1", Convert.ToDecimal(sum1[0]));
                command.Parameters.AddWithValue("non_cash_money1", Convert.ToDecimal(sum1[1]));
                command.Parameters.AddWithValue("sertificate_money1", Convert.ToDecimal(sum1[2]));
                command.Parameters.AddWithValue("guid", guid);
                //command.Parameters.AddWithValue("guid1", guid1);
                command.Parameters.AddWithValue("payment_by_sbp", payment_by_sbp);
                command.Parameters.AddWithValue("sent_to_processing_center", 0);

                command.Transaction = tran;
                command.ExecuteNonQuery();

                // ЗАМЕНА 1: Записываем товары из _productsData
                int numstr = 0;
                foreach (var product in _productsData)
                {
                    command = new NpgsqlCommand("INSERT INTO checks_table(" +
                        "document_number, tovar_code, quantity, price, " +
                        "price_at_a_discount, sum, sum_at_a_discount, numstr, action_num_doc, " +
                        "action_num_doc1, action_num_doc2, bonus_standard, bonus_promotion, " +
                        "promotion_b_mover, item_marker, guid) VALUES(" +
                        "@document_number, @tovar_code, @quantity, @price, " +
                        "@price_at_a_discount, @sum, @sum_at_a_discount, @numstr, @action_num_doc, " +
                        "@action_num_doc1, @action_num_doc2, @bonus_standard, @bonus_promotion, " +
                        "@promotion_b_mover, @item_marker, @guid)", conn);                   

                    command.Parameters.AddWithValue("document_number", numdoc);
                    command.Parameters.AddWithValue("tovar_code", product.Code);                    
                    command.Parameters.AddWithValue("quantity", product.Quantity);
                    command.Parameters.AddWithValue("price", product.Price);
                    command.Parameters.AddWithValue("price_at_a_discount", product.PriceAtDiscount);
                    command.Parameters.AddWithValue("sum", product.Sum);
                    command.Parameters.AddWithValue("sum_at_a_discount", product.SumAtDiscount);
                    command.Parameters.AddWithValue("numstr", numstr);
                    command.Parameters.AddWithValue("action_num_doc", product.Action);
                    command.Parameters.AddWithValue("action_num_doc1", product.Gift);
                    command.Parameters.AddWithValue("action_num_doc2", product.Action2);
                    command.Parameters.AddWithValue("bonus_standard", 0); // Нужно добавить поле в ProductItem
                    command.Parameters.AddWithValue("bonus_promotion", 0); // Нужно добавить поле в ProductItem
                    command.Parameters.AddWithValue("promotion_b_mover", 0); // Нужно добавить поле в ProductItem
                    command.Parameters.AddWithValue("item_marker", (product.Mark ?? "0").Replace("'", "vasya2021"));
                    command.Parameters.AddWithValue("guid", guid);

                    command.Transaction = tran;
                    command.ExecuteNonQuery();
                    numstr++;
                }

                // ЗАМЕНА 2: Записываем сертификаты из _certificatesData
                foreach (var certificate in _certificatesData)
                {
                    command = new NpgsqlCommand("INSERT INTO checks_table(" +
                        "document_number, tovar_code, characteristic, quantity, price, " +
                        "price_at_a_discount, sum, sum_at_a_discount, numstr, action_num_doc, " +
                        "action_num_doc1, action_num_doc2, item_marker, guid) VALUES(" +
                        "@document_number, @tovar_code, @characteristic, @quantity, @price, " +
                        "@price_at_a_discount, @sum, @sum_at_a_discount, @numstr, @action_num_doc, " +
                        "@action_num_doc1, @action_num_doc2, @item_marker, @guid)", conn);

                    command.Parameters.AddWithValue("document_number", numdoc);
                    command.Parameters.AddWithValue("tovar_code", certificate.Code);
                    command.Parameters.AddWithValue("characteristic", "");
                    command.Parameters.AddWithValue("quantity", "1");
                    command.Parameters.AddWithValue("price", "-" + certificate.Nominal.ToString("F2").Replace(",", "."));
                    command.Parameters.AddWithValue("price_at_a_discount", "-" + certificate.Nominal.ToString("F2").Replace(",", "."));
                    command.Parameters.AddWithValue("sum", "-" + certificate.Nominal.ToString("F2").Replace(",", "."));
                    command.Parameters.AddWithValue("sum_at_a_discount", "-" + certificate.Nominal.ToString("F2").Replace(",", "."));
                    command.Parameters.AddWithValue("numstr", numstr.ToString());
                    command.Parameters.AddWithValue("action_num_doc", "0");
                    command.Parameters.AddWithValue("action_num_doc1", "0");
                    command.Parameters.AddWithValue("action_num_doc2", "0");
                    command.Parameters.AddWithValue("item_marker", certificate.Barcode);
                    command.Parameters.AddWithValue("guid", guid);

                    command.Transaction = tran;
                    command.ExecuteNonQuery();
                    numstr++;

                    // Обновляем статус сертификата в локальной базе
                    command = new NpgsqlCommand("UPDATE sertificates SET is_active = 0 WHERE code_tovar = @tovar_code", conn);
                    command.Parameters.AddWithValue("tovar_code", certificate.Code);
                    command.Transaction = tran;
                    command.ExecuteNonQuery();
                }

                tran.Commit();
                conn.Close();

                if (sendToScreen)
                {
                    if (last_rewrite)
                    {
                        IsNewCheck = false;
                        if (this.check_type.SelectedIndex == 0)
                        {
                            SendDataToCustomerScreen(0, 0, 0);
                        }
                    }
                    else
                    {
                        IsNewCheck = true;
                        if (this.check_type.SelectedIndex == 0)
                        {
                            SendDataToCustomerScreen(1, 0, 1);
                        }
                    }
                    if (its_deleted == "1")
                    {
                        if (this.check_type.SelectedIndex == 0)
                        {
                            SendDataToCustomerScreen(0, 0, 0);
                        }
                    }
                }
                result = true;
            }
            catch (NpgsqlException ex)
            {
                if (tran != null)
                {
                    tran.Rollback();
                }

                await MessageBox.Show("Ошибка при записи документа " + ex.Message);
                result = false;
            }
            catch (Exception ex)
            {
                if (tran != null)
                {
                    tran.Rollback();
                }
                await MessageBox.Show("Ошибка при записи документа " + ex.Message);
                result = false;
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            if (result)
            {
                MainStaticClass.Last_Write_Check = DateTime.Now;
            }
            return result;
        }

        public class CustomerScreen
        {
            public List<CheckPosition> ListCheckPositions { get; set; }
            public int show_price { get; set; }
        }

        public class CheckPosition
        {
            public string NamePosition { get; set; }
            public string Quantity { get; set; }
            public string Price { get; set; }
        }

        /// <summary>
        /// Если mode == 0 то тогда товары не передаются чек закрыт
        /// mode == 1 Это отрисовка товаров 
        /// Если show_price == 0 тогда цены не отображаются
        /// отображаются номенклатура и количество
        /// Если show_price == 1 тогда цены отображаются, пока 
        /// этот режим будет доступен после перехода в окно оплаты
        /// </summary>
        /// <param name="mode"></param>
        public async void SendDataToCustomerScreen(int mode, int show_price, int calculate_actionc)
        {
            try
            {
                if (_productsData.Count == 0)
                {
                    return;
                }
                //if ((MainStaticClass.UseOldProcessiingActions) || (!itsnew))
                if ((mode == 1 && show_price == 1) || (mode == 0 && show_price == 0))
                {
                    CustomerScreen customerScreen = new CustomerScreen();
                    customerScreen.show_price = show_price;
                    customerScreen.ListCheckPositions = new List<CheckPosition>();
                    if (mode == 1)
                    {                       
                        foreach (var product in _productsData)
                        {
                            CheckPosition checkPosition = new CheckPosition();
                            checkPosition.NamePosition = product.Tovar;                // Наименование товара
                            checkPosition.Quantity = product.Quantity.ToString();      // Количество
                            checkPosition.Price = product.PriceAtDiscount.ToString();  // Цена со скидкой
                            customerScreen.ListCheckPositions.Add(checkPosition);
                        }
                    }
                    string message = JsonConvert.SerializeObject(customerScreen, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    SendUDPMessage(message);
                }
                else
                {
                    CustomerScreen customerScreen = new CustomerScreen();
                    customerScreen.show_price = 1;
                    customerScreen.ListCheckPositions = new List<CheckPosition>();
                    DataTable dataTable = to_define_the_action_dt(false);
                    //if (dataTable.Rows.Count > 0)
                    //{
                    this.txtB_total_sum.Text = "0";// calculation_of_the_sum_of_the_document().ToString() + " / " + Math.Round(Convert.ToDouble(dataTable.Compute("Sum(sum_at_discount)", (string)null)), 2).ToString("F2");//calculation_of_the_sum_of_the_document().ToString() +" / "+Convert.ToDouble(dataTable.Compute("Sum(sum_at_discount)", (string)null)).ToString("F2");
                    //}
                    //foreach (DataRow row in dataTable.Rows)
                    //{
                    //    //CheckPosition checkPosition = new CheckPosition();
                    //    //checkPosition.NamePosition = row["tovar_name"].ToString();
                    //    //checkPosition.Quantity = row["quantity"].ToString();
                    //    //checkPosition.Price = row["price_at_discount"].ToString();
                    //    //customerScreen.ListCheckPositions.Add(checkPosition);
                    //}
                    string message = JsonConvert.SerializeObject(customerScreen, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    SendUDPMessage(message);
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show("SendDataToCustomerScreen " + ex.Message);
            }
        }

        /// <summary>
        /// Здесь происходит обработка по всем 
        /// регулярным акциям течто со штрихкодом
        /// и те что без штрихкода        
        /// </summary>
        /// <param name="show_messages"></param>
        /// <returns></returns>
        private DataTable to_define_the_action_dt(bool show_messages)
        {
            DataTable dataTable = null;
            //if (!itsnew)
            //{
            //    return dataTable;
            //}
            //if (this.check_type.SelectedIndex > 0)
            //{
            //    return dataTable;
            //}
            //ProcessingOfActions processingOfActions = new ProcessingOfActions();
            //processingOfActions.cc = this;
            //action_num_doc = new List<int>();//При какждом пересчете список предвариетльно обнуляется


            //processingOfActions.dt = processingOfActions.create_dt(listView1);
            //processingOfActions.show_messages = show_messages;
            //MainStaticClass.write_event_in_log(" Попытка обработать акции по штрихкодам ", "Документ чек", numdoc.ToString());
            //foreach (string barcode in action_barcode_list)
            //{
            //    processingOfActions.to_define_the_action_dt(barcode);
            //}

            //if (client.Tag != null)
            //{
            //    processingOfActions.to_define_the_action_personal_dt(this.client.Tag.ToString());
            //}

            //processingOfActions.to_define_the_action_dt();

            //dataTable = processingOfActions.dt;

            //if (show_messages)//если с показом сообщений, то это уже боевой режим 
            //{
            //    have_action = processingOfActions.have_action;
            //}

            return dataTable;
        }

        private async static void SendUDPMessage(string message)
        {
            UdpClient sender = new UdpClient(); // создаем UdpClient для отправки сообщений
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                sender.Send(data, data.Length, "127.0.0.1", 12345); // отправка
            }
            catch (Exception ex)
            {
                await MessageBox.Show("SendDataToCustomerScreen " + ex.Message);
            }
            finally
            {
                sender.Close();
            }
        }

        private void ClientBarcodeOrPhone_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                MainStaticClass.write_event_in_log(" Перед началом поиска клиента ", "Документ чек", numdoc.ToString());
                ProcessClientDiscount(ClientBarcodeOrPhone.Text.Trim());
            }
            else
            {
                // Разрешаем только цифры и Backspace
                if (!IsDigitKey(e.Key) && e.Key != Key.Back && e.Key != Key.Delete)
                {
                    e.Handled = true;
                }
            }
        }       

        // Метод для проверки, является ли клавиша цифрой
        private bool IsDigitKey(Key key)
        {
            // Проверяем цифры на основной клавиатуре
            if (key >= Key.D0 && key <= Key.D9)
                return true;

            // Проверяем цифры на NumPad
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return true;

            return false;
        }

        /// <summary>
        /// Обработка ввода дисконтной карты клиента
        /// </summary>
        /// <param name="barcode"></param>
        private async void ProcessClientDiscount(string barcode)
        {
            Discount = 0;           
           
            if ((barcode.Trim().Length == 10) || (barcode.Trim().Length == 13))
            {
                MainStaticClass.write_event_in_log(" Код клиента имеет нормальную длину " + barcode, " Документ ", numdoc.ToString());
                //if (MainStaticClass.PassPromo == "")
                //{
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;
                //command.CommandText = " SELECT discount_types.discount_percent,clients.code,clients.name,clients.phone AS clients_phone," +
                //    " temp_phone_clients.phone AS temp_phone_clients_phone,attribute,clients.its_work,COALESCE(clients.bonus_is_on,0) AS bonus_is_on  FROM clients " +
                //    " left join discount_types ON clients.discount_types_code= discount_types.code " +
                //    " left join temp_phone_clients ON clients.code = temp_phone_clients.barcode " +
                //    " WHERE clients.code='" + barcode + "' OR right(clients.phone,10)='" + barcode + "' AND clients.its_work = 1 ";

                if (barcode.Substring(0, 1) == "9")
                {
                    check_and_verify_phone_number(barcode);//возможно что это новый клиент необходимо провести проверку 

                    command.CommandText = " SELECT 5.00,clients.code,clients.name,clients.phone AS clients_phone," +
                     " temp_phone_clients.phone AS temp_phone_clients_phone,attribute,clients.its_work,COALESCE(clients.bonus_is_on,0) AS bonus_is_on  FROM clients " +
                     " left join temp_phone_clients ON clients.code = temp_phone_clients.barcode " +
                     " WHERE clients.phone='" + barcode + "' AND clients.its_work = 1 ";
                }
                else
                {
                    command.CommandText = " SELECT 5.00,clients.code,clients.name,clients.phone AS clients_phone," +
                        " temp_phone_clients.phone AS temp_phone_clients_phone,attribute,clients.its_work,COALESCE(clients.bonus_is_on,0) AS bonus_is_on  FROM clients " +
                        " left join temp_phone_clients ON clients.code = temp_phone_clients.barcode " +
                        " WHERE clients.code='" + barcode + "' AND clients.its_work = 1 ";
                }

                MainStaticClass.write_event_in_log("Старт поиска клиента", "Документ чек", numdoc.ToString());

                NpgsqlDataReader reader = command.ExecuteReader();
                //bool client_find = false;
                while (reader.Read())
                {
                    //bool client_find = true;

                    if (reader["its_work"].ToString() != "1")
                    {
                        await MessageBox.Show(" Эта карточка клиента заблокирована !!!","Проверка ввода клиента",MessageBoxButton.OK,MessageBoxType.Error);
                        break;
                    }

                    //bonus_is_on = Convert.ToInt16(reader["bonus_is_on"]);

                    client_barcode_scanned = 1;

                    //if (bonus_is_on == 0)
                    //{
                    Discount = Convert.ToDouble(reader.GetDecimal(0)) / 100;
                    //}

                    client_barcode.IsEnabled = false;//дисконтная карта определена, сделаем недоступным окно ввода кода  
                    //txtB_client_phone.IsEnabled = false;//дисконтная карта определена, сделаем недоступным окно ввода телефона  
                    //btn_inpute_phone_client.IsEnabled = false;
                    //this.btn_inpute_phone_client.IsEnabled = false;
                    //it_is_possible_to_write_off_bonuses = true;

                    if (reader["attribute"].ToString().Trim() == "1")
                    {
                        client.Background = Brushes.LightGreen;// System.Drawing.ColorTranslator.FromHtml("#22FF99");
                    }
                    MainStaticClass.write_event_in_log(" Клиент найден ", "Документ чек", numdoc.ToString());
                    MainStaticClass.write_event_in_log(" Присвоение значения реквизиту на форме ", " Документ ", numdoc.ToString());
                    //this.client.Tag = reader["code"].ToString();
                    this.client.Tag = reader["code"].ToString();
                    this.client.Text = reader["name"].ToString();


                    //if ((reader["clients_phone"].ToString().Trim().Length < 10) && (reader["temp_phone_clients_phone"].ToString().Trim().Length < 10))//будем считать, что номера телефона нет
                    //{
                    //    InputePhoneClient ipc = new InputePhoneClient();
                    //    ipc.barcode = barcode;
                    //    DialogResult dialogResult = ipc.ShowDialog();
                    //    //if (bonus_is_on == 1)//Проверка для новой бонусной карты, при сканировании если нет привязанного номера телефона, то он обязательно должен быть введен 
                    //    //{
                    //    //    if (dialogResult != DialogResult.OK)
                    //    //    {
                    //    //        MessageBox.Show(" Для бонусной карты недопустимо отсутствие номера телефона ");
                    //    //        MessageBox.Show(" БОНУСНУЮ КАРТУ НЕ ВЫДАВАТЬ НИ В КОЕМ СЛУЧАЕ !!! ");
                    //    //        this.client.Tag = null;
                    //    //        this.client.Text = "";
                    //    //        this.client_barcode.Text = "";
                    //    //    }
                    //    //}
                    //    //this.inputbarcode.Focus();
                    //    this.txtB_search_product.Focus();
                    //    btn_inpute_phone_client.Enabled = true;
                    //}
                    //else//думалось заполнить телефонный номер в форму , но это наверное лишнее
                    //{
                    //    btn_inpute_phone_client.Enabled = true;
                    //}
                }
                reader.Close();
                conn.Close();


                if (this.client.Tag == null)//По каким то причинам клиент или не найден или не прошел проверки 
                {
                    MainStaticClass.write_event_in_log(" Клиент не найден ", "Документ чек", numdoc.ToString());
                    await MessageBox.Show("Клиент не найден");
                    return;
                }


                if (check_type.SelectedIndex == 1)//При возврате теперь можно использовать карту клиента 
                {
                    Discount = 0;
                }

                //Discount = Discount / 100;

                if (Discount != 0)//Пересчитать цены 
                {
                    MainStaticClass.write_event_in_log(" Начало пересчета ТЧ " + barcode, " Документ ", numdoc.ToString());                   
                    RecalculateAllProducts();
                    MainStaticClass.write_event_in_log(" Окончание пересчета ТЧ " + barcode, " Документ ", numdoc.ToString());
                }

                //Проверить на день рождения и вывести предупреждение
                //MainStaticClass.write_event_in_log(" Проверка на день рождения начало " + barcode, " Документ ", numdoc.ToString());
                //if (actions_birthday())
                //{
                //    MessageBox.Show(" ДР " + get_date_birthday());
                //    MainStaticClass.write_event_in_log(" Сегодня днюха у " + barcode, " Документ ", numdoc.ToString());
                //}
                //MainStaticClass.write_event_in_log(" Проверка на день рождения окончание " + barcode, " Документ ", numdoc.ToString());
                ////this.inputbarcode.Focus();
                //this.txtB_search_product.Focus();
                //this.client_barcode.Text = "";
                //this.btn_inpute_phone_client.Enabled = false;
            }
            else
            {
                await MessageBox.Show("Введено неверное количество символов");
            }
            MainStaticClass.write_event_in_log(" Выход из процедуры поиска клиента " + barcode, " Документ ", numdoc.ToString());
        }

        /// <summary>
        /// Пересчитать все товары в чеке (аналог перебора listView1.Items)
        /// </summary>
        private async void RecalculateAllProducts()
        {
            MainStaticClass.write_event_in_log(" Начало пересчета ТЧ ", "Документ", numdoc.ToString());

            // Перебираем все товары в коллекции
            foreach (var product in _productsData)
            {
                RecalculateProductSums(product);
            }

            // Обновляем Grid с новыми данными
            RefreshProductsGrid();
            UpdateTotalSum();
            await write_new_document("0", calculation_of_the_sum_of_the_document().ToString(),
                               "0", "0", false, "0", "0", "0", "0");

            MainStaticClass.write_event_in_log(" Окончание пересчета ТЧ ", "Документ", numdoc.ToString());
        }

        /// <summary>
        /// При вводе номера телефона
        /// сначала происходит проверка 
        /// наличия клиента с таким номером телефона
        /// если клиент не найден он создается и работа 
        /// происходит с новой виртуальной картой 
        /// равной номеру телефона, если клиент 
        /// найден тогда действия с ним
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void check_and_verify_phone_number(string phone_number)
        {
            MainStaticClass.write_event_in_log(" Проверка наличия телефона старт ", " Документ ", numdoc.ToString());
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT code,name FROM clients where phone='" + phone_number + "'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                bool client_exist = false;
                while (reader.Read())
                {
                    client_exist = true;
                    MainStaticClass.write_event_in_log(" Присвоение значения реквизиту на форме ", " Документ ", numdoc.ToString());
                    client.Tag = reader["code"].ToString();
                    client.Text = reader["name"].ToString();
                    Discount = Convert.ToDouble(0.05);
                    //txtB_client_phone.IsEnabled = false;
                    client_barcode.IsEnabled = false;

                }
                if (!client_exist)
                {
                    MainStaticClass.write_event_in_log(" Проверка наличия телефона это новый телефон  ", " Документ ", numdoc.ToString());
                    query = "DELETE FROM temp_phone_clients WHERE phone='" + phone_number + "'";
                    command = new NpgsqlCommand(query, conn);
                    command.ExecuteNonQuery();
                    query = "INSERT INTO temp_phone_clients(barcode, phone)VALUES ('" + phone_number + "','" + phone_number + "')";
                    command = new NpgsqlCommand(query, conn);
                    command.ExecuteNonQuery();
                    conn.Close();
                    if (client.Tag == null)
                    {
                        MainStaticClass.write_event_in_log(" Присвоение значения реквизиту на форме ", " Документ ", numdoc.ToString());
                        client.Tag = phone_number;
                        client.Text = phone_number;
                        Discount = Convert.ToDouble(0.05);
                        //txtB_client_phone.IsEnabled = false;
                        client_barcode.IsEnabled = false;
                    }
                }
                //Пересчет ТЧ в любом случае
                MainStaticClass.write_event_in_log(" Начало пересчета ТЧ " + phone_number, " Документ ", numdoc.ToString());
                RecalculateAllProducts();
                MainStaticClass.write_event_in_log(" Окончание пересчета ТЧ " + phone_number, " Документ ", numdoc.ToString());
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(" Ошибки при записи номера телефона " + ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(" Ошибки при записи номера телефона " + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
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

                // Устанавливаем фокус на ScrollViewer для обработки клавиатуры
                _productsScrollViewer?.Focus();

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

            await write_new_document("0", calculation_of_the_sum_of_the_document().ToString(), "0", "0", false, "0", "0", "0", "0");//нужно для того чтобы в окне оплаты взять сумму из БД

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

                    // Устанавливаем фокус на ScrollViewer
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _productsScrollViewer?.Focus();
                    }, DispatcherPriority.Background);
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

                // УСТАНАВЛИВАЕМ ФОКУС НА SCROLLVIEWER
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _productsScrollViewer?.Focus();
                }, DispatcherPriority.Background);

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
        private async void IncreaseProductQuantity(int dataIndex)
        {
            try
            {
                if (dataIndex >= 0 && dataIndex < _productsData.Count)
                {
                    var product = _productsData[dataIndex];

                    // ПРОВЕРКА: нельзя увеличивать количество для весового, маркированного товара или сертификата
                    if (!CanIncreaseQuantity(product))
                    {
                        // Показываем сообщение о запрете увеличения
                        //ShowCannotIncreaseMessage(product);
                        //// Эффект ошибки/предупреждения
                        //ShowWarningEffect(dataIndex);
                        return;
                    }

                    product.Quantity++;

                    RecalculateProductSums(product);
                    UpdateProductRowInGrid(dataIndex);
                    UpdateTotalSum();

                    ShowQuantityEffect(dataIndex, true); // true = увеличение (зеленый)
                    await write_new_document("0", calculation_of_the_sum_of_the_document().ToString(),
                                   "0", "0", false, "0", "0", "0", "0");

                    // ВОССТАНАВЛИВАЕМ ВЫДЕЛЕНИЕ И ФОКУС
                    SelectProductRow(dataIndex);
                    _productsScrollViewer?.Focus();

                    Console.WriteLine($"✓ Увеличено количество товара '{product.Tovar}' до {product.Quantity}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при увеличении количества: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверка, можно ли увеличивать количество для данного товара
        /// Возвращает false для весовых, маркированных товаров и сертификатов
        /// </summary>
        private bool CanIncreaseQuantity(ProductItem product)
        {
            try
            {
                // Получаем данные товара из БД для проверки флагов
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    string query = "SELECT its_certificate, its_marked, fractional FROM tovar WHERE code = @code";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", product.Code);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool isCertificate = Convert.ToBoolean(reader["its_certificate"]);
                                bool isMarked = Convert.ToBoolean(reader["its_marked"]);
                                bool isFractional = Convert.ToBoolean(reader["fractional"]);

                                // Проверяем все ограничения:
                                // 1. Сертификат - нельзя увеличивать количество
                                // 2. Маркированный товар - нельзя увеличивать количество
                                // 3. Весовой товар (fractional) - нельзя увеличивать количество

                                if (isCertificate)
                                {
                                    Console.WriteLine($"⚠ Товар {product.Tovar} - сертификат, увеличение количества запрещено");
                                    return false;
                                }

                                if (isMarked)
                                {
                                    Console.WriteLine($"⚠ Товар {product.Tovar} - маркированный, увеличение количества запрещено");
                                    return false;
                                }

                                if (isFractional)
                                {
                                    Console.WriteLine($"⚠ Товар {product.Tovar} - весовой, увеличение количества запрещено");
                                    return false;
                                }

                                // Все проверки пройдены - можно увеличивать
                                return true;
                            }
                            else
                            {
                                // Товар не найден в БД - разрешаем увеличение по умолчанию
                                Console.WriteLine($"⚠ Товар {product.Code} не найден в БД, разрешаем увеличение");
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при проверке возможности увеличения количества: {ex.Message}");
                // В случае ошибки разрешаем увеличение, чтобы не блокировать пользователя
                return true;
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
        private async void DecreaseProductQuantity(int dataIndex)
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
                        await write_new_document("0", calculation_of_the_sum_of_the_document().ToString(),
                                      "0", "0", false, "0", "0", "0", "0");
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
            await write_new_document("0", calculation_of_the_sum_of_the_document().ToString(),
                               "0", "0", false, "0", "0", "0", "0");

            // Выделяем следующую строку или снимаем выделение
            if (_productsData.Count > 0)
            {
                _productsScrollViewer?.Focus();
            }
            else
            {
                ClearProductSelection();
            }
        }

        // Метод для пересчета сумм товара с учетом скидки
        private void RecalculateProductSums(ProductItem product)
        {
            // ПЕРЕСЧЕТ ЦЕНЫ СО СКИДКОЙ (аналог WinForms calculate_on_string)
            if (Discount > 0) // Если есть скидка у клиента
            {
                // Проверка: не участвует в акции и не подарок (аналог SubItems[8] и SubItems[9])
                if (product.Action == 0 && product.Gift == 0)
                {
                    // Проверка на сертификат
                    if (!IsCertificate(product.Code.ToString()))
                    {
                        // Проверяем тип чека (0 = Продажа)
                        if (CheckType != null && CheckType.SelectedIndex == 0)
                        {
                            decimal originalPrice = product.Price;
                            decimal discountedPrice = originalPrice - originalPrice * (decimal)Discount;
                            decimal roundedUpPrice = Math.Round(discountedPrice, 2, MidpointRounding.AwayFromZero);
                            product.PriceAtDiscount = roundedUpPrice;
                        }
                        else
                        {
                            // Для возвратов и корректировок - цена без скидки
                            product.PriceAtDiscount = product.Price;
                        }
                    }
                    else // Сертификат
                    {
                        // Для сертификата цена со скидкой равна номиналу
                        product.PriceAtDiscount = Math.Round(product.Price, 2, MidpointRounding.AwayFromZero);
                    }
                }
                else // Если товар участвует в акции или это подарок
                {
                    // Для акционных товаров и подарков - цена без скидки
                    product.PriceAtDiscount = product.Price;

                    // Если это подарок с ценой 0.01
                    if (product.Gift != 0 && Math.Abs((double)product.Price - 0.01) < 0.001)
                    {
                        // Особый расчет для подарков по цене 0.01
                        product.PriceAtDiscount = product.Price;
                    }
                }
            }
            else // Если скидки нет
            {
                product.PriceAtDiscount = product.Price;
            }

            // Расчет сумм (это уже есть)
            product.Sum = product.Quantity * product.Price;
            product.SumAtDiscount = product.Quantity * product.PriceAtDiscount;
        }

        /// <summary>
        /// Проверка является ли товар сертификатом
        /// </summary>
        private bool IsCertificate(string productCode)
        {
            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM tovar WHERE code = @code AND its_certificate = '1'";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", Convert.ToInt64(productCode));
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке сертификата: {ex.Message}");
                return false;
            }
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
        private async void ShowQuantityEditDialog(int dataIndex)
        {
            if (dataIndex >= 0 && dataIndex < _productsData.Count)
            {
                var product = _productsData[dataIndex];

                // Создаем окно с увеличенными размерами
                var dialog = new Window
                {
                    Title = "Изменение количества",
                    Width = 400,
                    Height = 280,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    CanResize = false,
                    ShowInTaskbar = false,
                    SystemDecorations = SystemDecorations.BorderOnly
                };

                // Внешний Border с выраженной рамкой
                var outerBorder = new Border
                {
                    BorderBrush = Brushes.DarkSlateGray,
                    BorderThickness = new Thickness(3),
                    Background = Brushes.WhiteSmoke,
                    Child = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.White,
                        Padding = new Thickness(20, 15, 20, 15), // УМЕНЬШИЛИ ОТСТУП СНИЗУ С 20 ДО 15
                        Child = CreateDialogContent(product, dataIndex, dialog)
                    }
                };

                dialog.Content = outerBorder;

                // Позиционируем окно
                dialog.Loaded += (s, e) =>
                {
                    PositionDialogCorrectly(dialog);
                };

                // Автовыделение текста при загрузке окна
                dialog.Opened += (s, e) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Находим TextBox в содержимом
                        if (outerBorder.Child is Border innerBorder &&
                            innerBorder.Child is StackPanel stackPanel)
                        {
                            foreach (var child in stackPanel.Children)
                            {
                                if (child is Border textBoxBorder &&
                                    textBoxBorder.Child is TextBox textBox)
                                {
                                    textBox.Focus();
                                    textBox.SelectAll();
                                    break;
                                }
                            }
                        }
                    }, DispatcherPriority.Background);
                };

                // Показываем окно
                // Показываем окно
                var parentWindow = this is Window window ? window : this.FindAncestorOfType<Window>();
                await dialog.ShowDialog(parentWindow);
            }
        }

        /// <summary>
        /// Создает содержимое диалога
        /// </summary>
        private StackPanel CreateDialogContent(ProductItem product, int dataIndex, Window dialog)
        {
            var stackPanel = new StackPanel
            {
                Spacing = 20 // УМЕНЬШИЛИ С 25 ДО 20
            };

            // Информация о товаре
            var productTextBlock = new TextBlock
            {
                Text = $"Товар: {product.Tovar}",
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 340,
                MaxHeight = 40,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = FontWeight.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(productTextBlock);

            // TextBox с БОЛЬШИМ ШРИФТОМ
            var textBox = new TextBox
            {
                Text = product.Quantity.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 160,
                MaxLength = 5,
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                Name = "quantityTextBox",
                TextAlignment = TextAlignment.Center,
                CaretBrush = Brushes.Blue,
                SelectionBrush = Brushes.LightBlue,
                Foreground = Brushes.Black
            };

            var textBoxBorder = new Border
            {
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 15, 18, 15),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.AliceBlue,
                Width = 200,
                Height = 75,
                Margin = new Thickness(0, 10, 0, 15), // УМЕНЬШИЛИ ОТСТУП СНИЗУ С 20 ДО 15
                Child = textBox
            };

            stackPanel.Children.Add(textBoxBorder);

            // КОНТЕЙНЕР ДЛЯ КНОПОК - ПОДНИМАЕМ ВЫШЕ
            var buttonContainer = new Border
            {
                Height = 55, // НЕМНОГО УМЕНЬШИЛИ ВЫСОТУ
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 10, 0, 0), // УМЕНЬШИЛИ ОТСТУП СВЕРХУ С 25 ДО 10
                Child = CreateButtonPanel(product, textBox, dataIndex, dialog)
            };

            stackPanel.Children.Add(buttonContainer);

            return stackPanel;
        }

        /// <summary>
        /// Создает панель с кнопками
        /// </summary>
        private StackPanel CreateButtonPanel(ProductItem product, TextBox textBox, int dataIndex, Window dialog)
        {
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 25 // УМЕНЬШИЛИ С 30 ДО 25
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 105, // НЕМНОГО УМЕНЬШИЛИ
                Height = 40, // НЕМНОГО УМЕНЬШИЛИ
                IsDefault = true,
                Background = Brushes.LightGreen,
                BorderBrush = Brushes.Green,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(15, 8), // УМЕНЬШИЛИ ОТСТУПЫ
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 0)
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 105,
                Height = 40,
                IsCancel = true,
                Background = Brushes.LightCoral,
                BorderBrush = Brushes.DarkRed,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(15, 8),
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 0)
            };

            // Обработчики событий
            okButton.Click += (s, e) =>
            {
                ApplyQuantityChanges(product, textBox.Text, dataIndex);
                dialog.Close();
            };

            cancelButton.Click += (s, e) => dialog.Close();

            // Обработка клавиш
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    dialog.Close();
                    e.Handled = true;
                }
            };

            // Выделяем текст при получении фокуса
            textBox.GotFocus += (s, e) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    textBox.SelectAll();
                }, DispatcherPriority.Background);
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            return buttonPanel;
        }

        /// <summary>
        /// Применяет изменения количества
        /// </summary>
        private async void ApplyQuantityChanges(ProductItem product, string quantityText, int dataIndex)
        {
            if (int.TryParse(quantityText, out int newQuantity) && newQuantity >= 1 && newQuantity <= 9999)
            {
                product.Quantity = newQuantity;
                RecalculateProductSums(product);
                UpdateProductRowInGrid(dataIndex);
                UpdateTotalSum();
                Console.WriteLine($"✓ Количество товара '{product.Tovar}' изменено на {product.Quantity}");
                await write_new_document("0", calculation_of_the_sum_of_the_document().ToString(),
                               "0", "0", false, "0", "0", "0", "0");
            }
            else
            {
                Console.WriteLine("⚠ Неверное количество товара");
            }
        }

        /// <summary>
        /// Корректно позиционирует диалоговое окно
        /// </summary>
        private void PositionDialogCorrectly(Window dialog)
        {
            try
            {
                // Получаем главное окно
                var mainWindow = this.FindAncestorOfType<Window>();
                if (mainWindow == null) return;

                // Получаем экран
                var screen = mainWindow.Screens.ScreenFromPoint(mainWindow.Position);
                if (screen == null) return;

                // Рабочая область экрана
                var workingArea = screen.WorkingArea;

                // Позиция в центре родительского окна
                double targetX = mainWindow.Position.X + (mainWindow.Bounds.Width / 2) - (dialog.Bounds.Width / 2);
                double targetY = mainWindow.Position.Y + (mainWindow.Bounds.Height / 2) - (dialog.Bounds.Height / 2);

                // Корректируем позицию
                if (targetX < workingArea.X) targetX = workingArea.X + 10;
                if (targetY < workingArea.Y) targetY = workingArea.Y + 10;
                if (targetX + dialog.Bounds.Width > workingArea.Right) targetX = workingArea.Right - dialog.Bounds.Width - 10;
                if (targetY + dialog.Bounds.Height > workingArea.Bottom) targetY = workingArea.Bottom - dialog.Bounds.Height - 50;

                dialog.Position = new PixelPoint((int)targetX, (int)targetY);
                dialog.Topmost = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при позиционировании диалога: {ex.Message}");
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

                    if (MainStaticClass.Code_right_of_user == 1 && MainStaticClass.PrintingUsingLibraries == 1)
                    {
                        CheckType.Items.Add("КоррекцияПродажи");
                    }
                    CheckType.SelectedIndex = 0;
                    Console.WriteLine("✓ CheckType инициализирован");
                }


                if (NumCash != null)
                {
                    NumCash.Text = $"КАССА № {MainStaticClass.CashDeskNumber}";
                    NumCash.Tag = MainStaticClass.CashDeskNumber;
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
                    User.Tag = MainStaticClass.Cash_Operator_Client_Code;
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
        
        /// <summary>
        /// Фискальная Печать
        /// регистрация продажного чека
        /// </summary>
        /// <param name="pay"></param>
        private async void fiscall_print_pay(string pay)
        {
            if (MainStaticClass.SystemTaxation == 0)
            {
                await MessageBox.Show("В константах не определена система налогообложения, печать чеков невозможна");
                return;
            }

            if ((MainStaticClass.SystemTaxation != 3) && (MainStaticClass.SystemTaxation != 5))
            {
                PrintingUsingLibraries printingUsingLibraries = new PrintingUsingLibraries();
                {
                    if (MainStaticClass.GetKithenPrint != "")
                    {
                        //Еще надо проверить форму оплаты, что только наличные 
                        double[] type_payment = await get_cash_on_type_payment();
                        if ((type_payment[1] == 0) && (type_payment[2] == 0))
                        {
                            //kitchen_print(this);
                        }
                    }
                    else
                    {
                        printingUsingLibraries.print_sell_2_or_return_sell(this);
                    }
                }
            }
            else
            {
                if (print_to_button == 0)
                {

                    PrintingUsingLibraries printingUsingLibraries = new PrintingUsingLibraries();
                    printingUsingLibraries.print_sell_2_3_or_return_sell(this, 1);//Если первый печатать без маркировки то очищается буфер в проверенных
                    printingUsingLibraries.print_sell_2_3_or_return_sell(this, 0);
                }
                else if (print_to_button == 1)
                {
                    if (this.checkBox_to_print_repeatedly.IsChecked==true)
                    {
                        new PrintingUsingLibraries().print_sell_2_3_or_return_sell(this, 0);
                    }
                    if (this.checkBox_to_print_repeatedly_p.IsChecked==true)
                    {
                        new PrintingUsingLibraries().print_sell_2_3_or_return_sell(this, 1);
                    }
                }
                closing = false;
                //this.Close();
            }
        }

        /// <summary>
        /// Получение сумм по типам оплаты
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<double[]> get_cash_on_type_payment()
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
                await MessageBox.Show("Произошли ошибки при получении сумм по типам оплаты" + ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Произошли ошибки при получении сумм по типам оплаты" + ex.Message);
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
        /// Устанавливает флаг 
        /// распечатан для налогообложения
        /// по схеме патент
        /// </summary>
        public async void its_print_p(int variant)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            NpgsqlTransaction trans = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                trans = conn.BeginTransaction();

                string query = "";
                if (variant == 0)
                {
                    query = " UPDATE checks_header   SET its_print=true WHERE document_number=" + numdoc.ToString() + ";" + "UPDATE checks_header SET is_sent = 0 WHERE document_number = " + numdoc.ToString();
                }
                else
                {
                    query = " UPDATE checks_header   SET its_print_p=true WHERE document_number=" + numdoc.ToString() + ";" + "UPDATE checks_header SET is_sent = 0 WHERE document_number = " + numdoc.ToString();
                }

                command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                command.ExecuteNonQuery();

                query = " DELETE FROM document_wil_be_printed WHERE document_number=" + numdoc.ToString() + " AND tax_type =" + (MainStaticClass.SystemTaxation + variant).ToString();
                command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                command.ExecuteNonQuery();
                trans.Commit();
                command.Dispose();
                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
                await MessageBox.Show("Ошибка при установке флага распечатан " + ex.Message);
            }
            catch (Exception ex)
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
                await MessageBox.Show("Ошибка при установке флага распечатан " + ex.Message);
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
        /// Функция возвращает значение флага напечатан для чека,
        /// при ошибке получения вернется истина
        /// </summary>
        /// <returns></returns>
        private async Task<bool>  ItcPrinted()
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            bool result = true;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT its_print  FROM checks_header WHERE date_time_write = '" + this.date_time_write + "'";
                command = new NpgsqlCommand(query, conn);
                object result_query = command.ExecuteScalar();

                if (result_query != DBNull.Value)
                {
                    result = Convert.ToBoolean(result_query);
                }
                else
                {
                    result = false;
                }

                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show("Ошибка при получении флага распечатан " + ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Ошибка при получении флага распечатан " + ex.Message);
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
        /// Функция возвращает значение флага напечатан для чека,
        /// при ошибке получения вернется истина
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ItcPrintedP()
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            bool result = true;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT its_print_p  FROM checks_header WHERE date_time_write = '" + this.date_time_write + "'";
                command = new NpgsqlCommand(query, conn);
                object result_query = command.ExecuteScalar();

                if (result_query != DBNull.Value)
                {
                    result = Convert.ToBoolean(result_query);
                }
                else
                {
                    result = false;
                }

                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show("Ошибка при получении флага распечатан по патенту " + ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Ошибка при получении флага распечатан по патенту " + ex.Message);
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

        private async Task<int> GetItsDeletedDocument()
        {
            int result = 0;

            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT checks_header.its_deleted FROM  checks_header where checks_header.date_time_write='"
                    + date_time_write + "'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show("Ошибки при получении признака удаленности документа " + ex.Message);
                result = 1;
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Ошибки при получении признака удаленности документа " + ex.Message);
                result = 1;
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

        //private void DebugGridInfo()
        //{
        //    Console.WriteLine("\n=== Отладочная информация Grid ===");

        //    Console.WriteLine($"Grid товаров: {_productsTableGrid != null}");
        //    if (_productsTableGrid != null)
        //    {
        //        Console.WriteLine($"  - Колонок: {_productsTableGrid.ColumnDefinitions.Count}");
        //        Console.WriteLine($"  - Строк: {_productsTableGrid.RowDefinitions.Count}");
        //        Console.WriteLine($"  - Записей: {_productsData.Count}");
        //    }

        //    Console.WriteLine($"\nGrid сертификатов: {_certificatesTableGrid != null}");
        //    if (_certificatesTableGrid != null)
        //    {
        //        Console.WriteLine($"  - Колонок: {_certificatesTableGrid.ColumnDefinitions.Count}");
        //        Console.WriteLine($"  - Строк: {_certificatesTableGrid.RowDefinitions.Count}");
        //        Console.WriteLine($"  - Записей: {_certificatesData.Count}");
        //    }

        //    Console.WriteLine("=== Конец отладочной информации ===\n");
        //}

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

        //public void CloseForm()
        //{
        //    Closed?.Invoke(this, EventArgs.Empty);
        //}

        #endregion

        


        private Int64 get_new_number_document()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            conn.Open();
            NpgsqlCommand command = new NpgsqlCommand();
            command.Connection = conn;
            //            command.CommandText = "SELECT COUNT(*) FROM checks_header";
            command.CommandText = "SELECT nextval('checks_header_document_number_seq'::regclass);";
            Int64 result = Convert.ToInt64(command.ExecuteScalar());
            conn.Close();
            MainStaticClass.write_event_in_log(" Получение номера для нового документа ", "Документ чек", result.ToString());
            return result;
        }


        private void set_sale_disburse_button()
        {
            //if ((!itsnew) && (itc_printed()))
            if (!IsNewCheck)//Если документ не новый он для чтения и там ничего менять нельзя
            {
                return;
            }
            //if (MainStaticClass.SelfServiceKiosk == 1)
            //{
            //    this.client.Text = "";
            //    this.client.Tag = "";
            //}
            //else
            //{ 
            if (this.check_type.SelectedIndex == 1)
            {
                if (client.Tag != null)
                {
                    if (client.Tag.ToString().Trim() != "")//Выбрана дисконтная карта, тип документа изенен быть не может
                    {
                        MessageBox.Show(" Выбрана дисконтная карта, тип документа изменен быть не может ");
                        this.check_type.SelectedIndex = 0;
                        return;
                    }
                }
            }
            //}
        }

        private async void check_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsNewCheck)
            {
                this.check_type.IsEnabled = false;
                if (check_type.SelectedIndex > 0)
                {
                    if (_productsData.Count > 0)
                    {
                        await MessageBox.Show("Тип чека необходимо выбирать перед добавлением строк");
                        return;
                    }
                    btn_fill_on_sales.IsVisible = true;
                    txtB_num_sales.IsVisible = true;
                    //if (MainStaticClass.Code_right_of_user != 1)
                    //{
                    //inputbarcode.Enabled = false;
                    txtB_search_product.IsEnabled = false;
                    client_barcode.IsEnabled = false;
                    //txtB_client_phone.Enabled = false;
                    //}
                }
            }
            set_sale_disburse_button();
        }

        protected virtual async Task OnLoaded()
        {
            //this.num_cash.Text = "КАССА № " + MainStaticClass.CashDeskNumber.ToString();
            //this.num_cash.Tag = MainStaticClass.CashDeskNumber;       
         
            ////Создание таблицы для перераспределения акций
            //DataColumn dc = new DataColumn("Code", System.Type.GetType("System.Int32"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("Tovar", System.Type.GetType("System.String"));
            //table.Columns.Add(dc);           
            //dc = new DataColumn("Quantity", System.Type.GetType("System.Int32"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("Price", System.Type.GetType("System.Decimal"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("PriceAtDiscount", System.Type.GetType("System.Decimal"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("Sum", System.Type.GetType("System.Decimal"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("SumAtDiscount", System.Type.GetType("System.Decimal"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("Action", System.Type.GetType("System.Int32"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("Gift", System.Type.GetType("System.Int32"));
            //table.Columns.Add(dc);
            //dc = new DataColumn("Action2", System.Type.GetType("System.Int32"));
            //table.Columns.Add(dc);

            ////this.inputbarcode.Focus();
            //this.txtB_search_product.Focus();

            //if (MainStaticClass.GetVersionFn == 1)
            //{
            //    checkBox_print_check.IsVisible = false;
            //}
            //checkBox_print_check.IsChecked = true;

            //if (IsNewCheck)
            //{
            //    guid = Guid.NewGuid().ToString();
                

            //    checkBox_to_print_repeatedly.IsVisible = false;
            //    //label9.Visible = false;
            //    //label10.Visible = false;
            //    //label11.Visible = false;
            //    //label13.Visible = false;
            //    txtB_non_cash_money.IsVisible = false;
            //    txtB_sertificate_money.IsVisible = false;
            //    txtB_cash_money.IsVisible = false;
            //    txtB_bonus_money.IsVisible = false;

            //    //inputbarcode.Focus();
            //    this.txtB_search_product.Focus();


            //    this.date_time_start.Text = "Чек   " + DateTime.Now.ToString("yyy-MM-dd HH:mm:ss");
            //    this.Discount = 0;
            //    this.user.Text = MainStaticClass.Cash_Operator;
            //    this.user.Tag = MainStaticClass.Cash_Operator_Client_Code;//gaa поменять на инн
            //    numdoc = get_new_number_document();
            //    if (numdoc == 0)
            //    {
            //        MessageBox.Show("Ошибка при получении номера документа.", "Проверка при получении номер документа");
            //        MainStaticClass.WriteRecordErrorLog("Ошибка при получении номера документа", "Cash_check_Load", 0, MainStaticClass.CashDeskNumber, "При вводе нового документа получен нулевой номер");
            //        this.Close();
            //    }
            //    this.txtB_num_doc.Text = this.numdoc.ToString();
            //    MainStaticClass.write_event_in_log(" Ввод нового документа ", "Документ чек", numdoc.ToString());
            //    this.check_type.SelectedIndex = 0;
            //    this.check_type.IsEnabled = true;
            //    set_sale_disburse_button();
            //}
            //else
            //{
            //    reopened = true;
            //    checkBox_print_check.IsEnabled = false;
            //    //Документ не новый поэтому запретим в нем ввод и изменение                
            //    last_tovar.IsEnabled = false;
            //    //txtB_email_telephone.Enabled = false;
            //    txtB_inn.IsEnabled = false;
            //    btn_get_name.IsEnabled = false;
            //    //txtB_client_phone.Enabled = false;
            //    txtB_name.IsEnabled = false;
            //    comment.IsEnabled = false;
                
            //    int status = get_its_deleted_document();
            //    if ((status == 0) || (status == 1))
            //    {
            //        //this.type_pay.Enabled = false;
            //        //this.label4.Enabled = false;
            //        this.check_type.IsEnabled = false;
            //        //this.inputbarcode.Enabled = false;
            //        this.txtB_search_product.IsEnabled = false;
            //        this.client_barcode.IsEnabled = false;
            //        //this.sale_cancellation.Enabled = false;
            //        //this.inventory.Enabled = false;
            //        //this.comment.Enabled = false;
            //        //to_open_the_written_down_document();
            //        enable_print();
            //        if (MainStaticClass.Code_right_of_user != 1)
            //        {
            //            this.pay.IsEnabled = false;
            //        }
            //        //itsnew = true;
            //    }
            //    else if (status == 2)
            //    {
            //        IsNewCheck = true;
            //        Discount = 0;
            //        //this.label4.Enabled = true;
            //        this.check_type.IsEnabled = true;
            //        //this.inputbarcode.Enabled = true;
            //        this.txtB_search_product.IsEnabled = true;
            //        this.client_barcode.IsEnabled = false;
            //        ToOpenTheWrittenDownDocument();
            //        get_old_document_Discount();
            //        check_type.IsEnabled = false;
            //        IsNewCheck = true;
            //    }
            //}

            ////this.Top = 0;
            ////this.Left = 0;
            ////this.Size = new System.Drawing.Size(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);
            ////this.panel2.Left = 0;
            ////this.listView2.Left = 20;

            ////this.panel2.Size = new System.Drawing.Size(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height / 2);
            ////this.listView2.Size = new System.Drawing.Size(SystemInformation.PrimaryMonitorSize.Width - 50, SystemInformation.PrimaryMonitorSize.Height / 2 - 50);


            //if (IsNewCheck)
            //{
            //    //first_start_com_barcode_scaner();
            //    selection_goods = true;
            //    //inputbarcode.Focus();
            //    this.txtB_search_product.Focus();
            //    //список допустимых длин qr кодов                
            //    qr_code_lenght.Add(29);
            //    qr_code_lenght.Add(30);
            //    qr_code_lenght.Add(31);
            //    qr_code_lenght.Add(32);
            //    qr_code_lenght.Add(37);
            //    qr_code_lenght.Add(40);
            //    qr_code_lenght.Add(41);
            //    qr_code_lenght.Add(76);
            //    qr_code_lenght.Add(83);
            //    qr_code_lenght.Add(115);
            //    qr_code_lenght.Add(127);

            //    if (MainStaticClass.PrintingUsingLibraries == 1)
            //    {
            //        IFptr fptr = MainStaticClass.FPTR;

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
            //else
            //{
            //    if (MainStaticClass.Use_Fiscall_Print)
            //    {
            //        if ((MainStaticClass.SystemTaxation != 3) && (MainStaticClass.SystemTaxation != 5))
            //        {
            //            if (await ItcPrinted())
            //            {
            //                this.pay.IsEnabled = false;
            //                this.checkBox_to_print_repeatedly.IsEnabled = false;
            //            }
            //        }
            //        else if ((MainStaticClass.SystemTaxation == 3) || (MainStaticClass.SystemTaxation == 5))
            //        {
            //            if (await ItcPrinted())
            //            {
            //                this.checkBox_to_print_repeatedly.IsEnabled = false;
            //            }
            //            if (await ItcPrintedP())
            //            {
            //                this.checkBox_to_print_repeatedly_p.IsEnabled = false;
            //            }
            //            if (await ItcPrinted() && await this.ItcPrintedP())
            //            {
            //                this.pay.IsEnabled = false;
            //            }
            //        }
            //    }
            //}          
        }

        private void enable_print()
        {
            if (MainStaticClass.SystemTaxation < 3)
            {
                checkBox_to_print_repeatedly.IsEnabled = true;
                checkBox_to_print_repeatedly_p.IsEnabled = false;
            }
            else
            {
                int _checkBox_to_print_repeatedly_ = 0;
                int _checkBox_to_print_repeatedly_p_ = 0;

                foreach (ProductItem productItem in _productsData)
                {
                    if (productItem.Mark.Trim().Length > 13)
                    {
                        _checkBox_to_print_repeatedly_p_ = 1;//Здесь путаница, печатать не маркировку 
                    }
                    else
                    {
                        _checkBox_to_print_repeatedly_ = 1; //Здесь путаница, печатать маркировку 
                    }
                    if ((_checkBox_to_print_repeatedly_ == 1) && (_checkBox_to_print_repeatedly_p_ == 1))
                    {
                        break;
                    }
                }
                if (_checkBox_to_print_repeatedly_ == 1)
                {
                    checkBox_to_print_repeatedly.IsEnabled = true;
                }
                if (_checkBox_to_print_repeatedly_p_ == 1)
                {
                    checkBox_to_print_repeatedly_p.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void get_old_document_Discount()
        {

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;
                Discount = 0;
                command.CommandText = "SELECT discount_types.discount_percent,clients.code,clients.name  FROM clients left join discount_types ON clients.discount_types_code= discount_types.code WHERE clients.code='" + client_barcode.Tag.ToString() + "'";
                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Discount = Convert.ToDouble(reader.GetDecimal(0));
                    Discount = Discount / 100;
                }
                reader.Close();
                conn.Close();
            }
            catch (NpgsqlException)
            {

            }
            catch (Exception)
            {

            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private int get_its_deleted_document()
        {
            int result = 0;

            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT checks_header.its_deleted FROM  checks_header where checks_header.date_time_write='"
                    + date_time_write + "'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                result = Convert.ToInt16(command.ExecuteScalar());
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибки при получении признака удаленности документа " + ex.Message);
                result = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибки при получении признака удаленности документа " + ex.Message);
                result = 1;
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
}