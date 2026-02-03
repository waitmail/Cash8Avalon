using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class SettingConnect : Window
    {

        private Button? _button4;
        private Button? _button2;
        private Button? _button1;
        private TextBlock? _label1;
        private TextBlock? _label2;
        private TextBlock? _label5;
        private TextBlock? _label6;
        private TextBlock? _portPostgres;
        private TextBlock? _userPostrgres;
        private TextBox? _ipaddrServer;
        private TextBox? _nameDataBase;
        private TextBox? _textBox2;
        private TextBox? _servicePassword;
        private TextBox? _repeatPassword;
        private TextBox? _portServer;
        private TextBox? _passwordPostgres;
        private TextBox? _repeatPasswordPostgres;
        private TextBox? _postgresUser;
        private CheckBox? _checkBox1;
        private CheckBox? _checkBox2;
        private Border? _groupBox1;
        private Border? _groupBox2;

        private bool fileSettinConnect = false;

        public SettingConnect()
        {
            InitializeComponent();
            //#if DEBUG
            //            this.AttachDevTools();
            //#endif
            this.Opened += SettingConnect_Opened;
            InitializeControls();
        }

        private void InitializeControls()
        {
            _button4 = this.FindControl<Button>("button4");
            _button2 = this.FindControl<Button>("button2");
            _button1 = this.FindControl<Button>("button1");
            _label1 = this.FindControl<TextBlock>("label1");
            _label2 = this.FindControl<TextBlock>("label2");
            _label5 = this.FindControl<TextBlock>("label5");
            _label6 = this.FindControl<TextBlock>("label6");
            _portPostgres = this.FindControl<TextBlock>("portPostgres");
            _userPostrgres = this.FindControl<TextBlock>("userPostrgres");
            _ipaddrServer = this.FindControl<TextBox>("ipaddrServer");
            _nameDataBase = this.FindControl<TextBox>("nameDataBase");
            _textBox2 = this.FindControl<TextBox>("textBox2");
            _servicePassword = this.FindControl<TextBox>("servicePassword");
            _repeatPassword = this.FindControl<TextBox>("repeatPassword");
            _portServer = this.FindControl<TextBox>("portServer");
            _passwordPostgres = this.FindControl<TextBox>("passwordPostgres");
            _repeatPasswordPostgres = this.FindControl<TextBox>("repeatPasswordPostgres");
            _postgresUser = this.FindControl<TextBox>("postgresUser");
            _checkBox1 = this.FindControl<CheckBox>("checkBox1");
            _checkBox2 = this.FindControl<CheckBox>("checkBox2");
            _groupBox1 = this.FindControl<Border>("groupBox1");
            _groupBox2 = this.FindControl<Border>("groupBox2");
        }

        private void SettingConnect_Opened(object? sender, EventArgs e)
        {
            // Получение пути к исполняемому файлу
            string startupPath = AppContext.BaseDirectory;

            // Или для получения директории приложения:
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

            fileSettinConnect = File.Exists(Path.Combine(startupPath, "Setting.gaa"));

            if (fileSettinConnect == true)//файл с параметрами есть заполнить реквизиты
            {
                StringReader stReader = MainStaticClass.DecryptData(Path.Combine(startupPath, "Setting.gaa"));
                fillDialog(stReader);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Можно также инициализировать элементы здесь, если нужно
            // или оставить пустым, если все уже в XAML
        }

        private bool IsValidServicePassword(string enteredPassword)
        {
            // Убираем пробелы и приводим к верхнему регистру для сравнения
            string cleanPassword = enteredPassword.Trim();
            string upperPassword = cleanPassword.ToUpper();

            // Если файла настроек еще нет (первый запуск)
            if (!fileSettinConnect)
            {
                // Допустимые пароли при первом запуске:
                // 1. "1" (простой пароль по умолчанию)
                // 2. "SERVICEMODE" (специальный режим сервиса)
                return upperPassword == "1" || upperPassword == "SERVICEMODE";
            }

            // Если файл настроек уже существует
            if (_servicePassword != null)
            {
                // Допустимые пароли:
                // 1. Совпадение с сохраненным паролем (точное сравнение)
                // 2. Специальный режим SERVICEMODE (в любом регистре)
                return cleanPassword == _servicePassword.Text.Trim() ||
                       upperPassword == "SERVICEMODE";
            }

            // Если почему-то нет контрола servicePassword, 
            // но файл настроек есть - разрешаем только SERVICEMODE
            return upperPassword == "SERVICEMODE";
        }

        private async Task ProcessPasswordCheck()
        {
            try
            {
                if (_textBox2 == null)
                {
                    await MessageBox.Show("Ошибка: поле ввода пароля не найдено");
                    return;
                }

                string enteredPassword = _textBox2.Text;
                if (enteredPassword == null)
                {
                    return;
                }

                if (IsValidServicePassword(enteredPassword))
                {
                    // Пароль верный - показываем настройки
                    ChangeVisibleBeforeWrite();

                    // Можно добавить логирование успешного входа
                    Console.WriteLine($"Успешная авторизация: {DateTime.Now}");
                }
                else
                {
                    // Пароль неверный
                    await MessageBox.Show("Неправильный пароль");

                    // Можно добавить счетчик неудачных попыток
                    // или блокировку после нескольких попыток
                }

                // Очищаем поле ввода пароля
                _textBox2.Text = "";
                _textBox2.Focus(); // Возвращаем фокус на поле ввода
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка проверки пароля: {ex.Message}");
            }
        }

        // Обработчик нажатия Enter в поле ввода пароля
        private async void TextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ProcessPasswordCheck();
                e.Handled = true; // Предотвращаем дальнейшую обработку
            }
        }

        // Обработчик нажатия кнопки "Ввести"
        private async void Button1_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPasswordCheck();
        }

        private async Task<bool> GetValidateDialogControl()
        {
            bool result = true;

            // Проверка ipaddrServer
            if (_ipaddrServer == null || _ipaddrServer.Text.Trim().Length == 0)
            {
                await MessageBox.Show("ip адрес сервера пустой");
                return false;
            }

            // Проверка nameDataBase
            if (_nameDataBase == null || _nameDataBase.Text.Trim().Length == 0)
            {
                await MessageBox.Show("имя базы данных пустое");
                return false;
            }

            // Проверка servicePassword
            if (_servicePassword == null || _servicePassword.Text.Trim().Length == 0)
            {
                await MessageBox.Show("сервисный пароль не заполнен");
                return false;
            }

            // Проверка repeatPassword
            if (_repeatPassword == null || _repeatPassword.Text.Trim().Length == 0)
            {
                await MessageBox.Show("подтверждение сервисного пароля не заполнено");
                return false;
            }

            // Сравнение сервисных паролей
            if (_servicePassword.Text.Trim() != _repeatPassword.Text.Trim())
            {
                await MessageBox.Show("сервисный пароль не совпадает с подтверждением");
                return false;
            }

            // Проверка postgresUser
            if (_postgresUser == null || _postgresUser.Text.Trim().Length == 0)
            {
                await MessageBox.Show("пользователь postgres пустой");
                return false;
            }

            // Проверка portServer
            if (_portServer == null || _portServer.Text.Trim().Length == 0)
            {
                await MessageBox.Show("порт сервера пустой");
                return false;
            }

            // Проверка passwordPostgres
            if (_passwordPostgres == null || _passwordPostgres.Text.Trim().Length == 0)
            {
                await MessageBox.Show("пароль к серверу пустой");
                return false;
            }

            // Проверка repeatPasswordPostgres
            if (_repeatPasswordPostgres == null || _repeatPasswordPostgres.Text.Trim().Length == 0)
            {
                await MessageBox.Show("подтверждение пароля к серверу пустое");
                return false;
            }

            // Сравнение паролей postgres
            if (_passwordPostgres.Text.Trim() != _repeatPasswordPostgres.Text.Trim())
            {
                await MessageBox.Show("пароль к серверу не совпадает с подтверждением");
                return false;
            }

            return result;
        }

        private void CheckBox1_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                bool showPassword = checkBox.IsChecked ?? false;

                // Получаем TextBox элементы
                var servicePassword = this.FindControl<TextBox>("servicePassword");
                var repeatPassword = this.FindControl<TextBox>("repeatPassword");

                if (servicePassword != null)
                {
                    servicePassword.PasswordChar = showPassword ? '\0' : '*';
                }

                if (repeatPassword != null)
                {
                    repeatPassword.PasswordChar = showPassword ? '\0' : '*';
                }
            }
        }

        private void CheckBox2_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                bool showPassword = checkBox.IsChecked ?? false;

                // Получаем TextBox элементы
                var passwordPostgres = this.FindControl<TextBox>("passwordPostgres");
                var repeatPasswordPostgres = this.FindControl<TextBox>("repeatPasswordPostgres");

                if (passwordPostgres != null)
                {
                    passwordPostgres.PasswordChar = showPassword ? '\0' : '*';
                }

                if (repeatPasswordPostgres != null)
                {
                    repeatPasswordPostgres.PasswordChar = showPassword ? '\0' : '*';
                }
            }
        }

        private async void Button2_Click(object sender, RoutedEventArgs e)
        {
            if (!await GetValidateDialogControl())
            {
                return;
            }

            string f = "";
            f += "[ip адрес сервера]";
            f += "\r\n";
            f += _ipaddrServer.Text.Trim();
            f += "\r\n";
            f += "[имя базы данных]";
            f += "\r\n";
            f += _nameDataBase.Text.Trim();
            f += "\r\n";
            f += "[сервисный пароль]";
            f += "\r\n";
            f += _servicePassword.Text.Trim();
            f += "\r\n";
            f += "[порт сервера]";
            f += "\r\n";
            f += _portServer.Text.Trim();
            f += "\r\n";
            f += "[пароль postgres]";
            f += "\r\n";
            f += _passwordPostgres.Text.Trim();
            f += "\r\n";
            f += "[пользователь postgres]";
            f += "\r\n";
            f += _postgresUser.Text.Trim();

            // Получение пути к исполняемому файлу
            string startupPath = AppContext.BaseDirectory;
            string settingsPath = Path.Combine(startupPath, "Setting.gaa");

            // Используем ваш метод шифрования вместо StreamWriter
            MainStaticClass.EncryptData(settingsPath, f);

            ChangeVisibleAfterWrite();

            // Загружаем зашифрованные настройки для отображения
            if (fileSettinConnect)
            {
                StringReader stReader = MainStaticClass.DecryptData(settingsPath);
                fillDialog(stReader);
            }
        }

        private void fillDialog(StringReader sr)
        {
            string line;
            int etap = 0;

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
                if (line == "[сервисный пароль]")
                {
                    etap = 3;
                    continue;
                }
                if (line == "[порт сервера]")
                {
                    etap = 4;
                    continue;
                }
                if (line == "[пароль postgres]")
                {
                    etap = 5;
                    continue;
                }
                if (line == "[пользователь postgres]")
                {
                    etap = 6;
                    continue;
                }

                if (etap == 1 && _ipaddrServer != null)
                {
                    _ipaddrServer.Text = line;                    
                    etap = 0;
                }
                if (etap == 2 && _nameDataBase != null)
                {
                    _nameDataBase.Text = line;
                    etap = 0;
                }
                if (etap == 3 && _servicePassword != null && _repeatPassword != null)
                {
                    _servicePassword.Text = line;
                    _repeatPassword.Text = line;
                    etap = 0;
                }
                if (etap == 4 && _portServer != null)
                {
                    _portServer.Text = line;
                    etap = 0;
                }
                if (etap == 5 && _servicePassword != null && _repeatPassword != null)
                {
                    _passwordPostgres.Text = line;
                    _repeatPasswordPostgres.Text = line;
                    etap = 0;
                }
                if (etap == 6 && _postgresUser != null)
                {
                    _postgresUser.Text = line;
                    etap = 0;
                }
            }
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Button4_Click(object sender, RoutedEventArgs e)
        {
            await CreateDataTables();
            AddField_Click(null, null);
        }

        private async Task CreateDataTables()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlCommand command = null;
            NpgsqlTransaction trans = null;

            List<string> queries = new List<string>();
            try
            {
                string[] query = new string[25];
                queries.Add("CREATE TABLE public.tovar(code bigint NOT NULL,name character(100) COLLATE pg_catalog.default NOT NULL,    retail_price numeric(10, 2) NOT NULL,    its_deleted numeric(1, 0) NOT NULL,    nds integer,    its_certificate smallint,    percent_bonus numeric(8, 2) DEFAULT 0,    tnved character varying(10) COLLATE pg_catalog.default,    its_marked smallint,    its_excise smallint NOT NULL DEFAULT 0,CONSTRAINT tovar_pkey PRIMARY KEY (code))WITH(    OIDS = FALSE)TABLESPACE pg_default;                ALTER TABLE public.tovar                    OWNER to postgres;        COMMENT ON COLUMN public.tovar.its_excise            IS '0 - обычный товар 1 - подакцизный товар';        CREATE UNIQUE INDEX _tovar_code_    ON public.tovar USING btree    (code ASC NULLS LAST)    TABLESPACE pg_default;");
                //queries.Add("CREATE TABLE public.tovar(    code bigint NOT NULL,    name character(100) COLLATE pg_catalog.default NOT NULL,    retail_price numeric(10, 2) NOT NULL,    its_deleted numeric(1, 0) NOT NULL,    nds integer,    its_certificate smallint,    percent_bonus numeric(8, 2) DEFAULT 0,    tnved character varying(10) COLLATE pg_catalog.default,    its_marked smallint,    its_excise smallint NOT NULL DEFAULT 0)WITH(    OIDS = FALSE)TABLESPACE pg_default;                ALTER TABLE public.tovar                    OWNER to postgres;        COMMENT ON COLUMN public.tovar.its_excise            IS '0 - обычный товар 1 - подакцизный товар';        CREATE UNIQUE INDEX _tovar_code_    ON public.tovar USING btree    (code ASC NULLS LAST, code ASC NULLS LAST, code ASC NULLS LAST, code ASC NULLS LAST)    TABLESPACE pg_default;");
                //query[0]  = "CREATE TABLE action_clients(num_doc integer,code_client character(10))WITH (OIDS=FALSE);ALTER TABLE action_clients  OWNER TO postgres";
                queries.Add("CREATE TABLE public.action_clients(num_doc integer, code_client character(10) COLLATE pg_catalog.default)WITH(OIDS = FALSE)TABLESPACE pg_default; ALTER TABLE public.action_clients OWNER to postgres; CREATE UNIQUE INDEX code_client_num_doc    ON public.action_clients USING btree(code_client COLLATE pg_catalog.default ASC NULLS LAST, num_doc ASC NULLS LAST)    TABLESPACE pg_default; ALTER TABLE public.action_clients CLUSTER ON code_client_num_doc;");
                queries.Add("CREATE TABLE public.action_header(date_started date,    date_end date,    num_doc integer NOT NULL,    tip smallint,    barcode character(13) COLLATE pg_catalog.default,    persent numeric(5,2),    sum numeric(10,2),    comment character(100) COLLATE pg_catalog.default,    code_tovar bigint,    marker smallint,    action_by_discount boolean DEFAULT false,    time_start integer,    time_end integer,    bonus_promotion smallint,    with_old_promotion smallint,    monday smallint,    tuesday smallint,    wednesday smallint,    thursday smallint,    friday smallint,    saturday smallint,    sunday smallint,    promo_code smallint,    sum_bonus smallint,    execution_order smallint,    gift_price numeric(10,2) NOT NULL,    kind smallint NOT NULL DEFAULT 0)WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.action_header            OWNER to postgres;        COMMENT ON COLUMN public.action_header.code_tovar            IS 'код товара';        COMMENT ON COLUMN public.action_header.marker            IS 'если значение заполнено в 1 то будет показано окно ввода штрихкода товара';        COMMENT ON COLUMN public.action_header.time_start             IS 'Время начала действия акции';        COMMENT ON COLUMN public.action_header.time_end            IS 'Время окончания действия акции';        COMMENT ON COLUMN public.action_header.bonus_promotion             IS '1 - это бонусная акция, иначе - моментальная (старого типа)';         COMMENT ON COLUMN public.action_header.with_old_promotion             IS 'пересечение с моментальными акциями: 1 - да, иначе - нет';        COMMENT ON COLUMN public.action_header.monday            IS 'акция работает по понедельникам';        COMMENT ON COLUMN public.action_header.tuesday            IS 'акция работает по вторникам';        COMMENT ON COLUMN public.action_header.wednesday            IS 'акция работает по средам';        COMMENT ON COLUMN public.action_header.thursday             IS 'акция работает по четвергам';         COMMENT ON COLUMN public.action_header.friday             IS 'акция работает по пятницам';        COMMENT ON COLUMN public.action_header.saturday            IS 'акция работает по субботам';        COMMENT ON COLUMN public.action_header.sunday            IS 'акция работает по воскресеньям';        COMMENT ON COLUMN public.action_header.promo_code            IS 'промо-код при озвучивании которого может сработать бонусная акция, если не заполнен, то не ограничивать';        COMMENT ON COLUMN public.action_header.kind            IS '0 - обычная автоматическая 1 - активируемая штрихкодом 2 - обычная автоматическая по картам';");
                //query[1]  = "CREATE TABLE action_header(date_started date,date_end date,num_doc integer NOT NULL, tip smallint,  barcode character(13),  persent numeric(5,2),  sum numeric(10,2),  comment character(100),  code_tovar integer,  marker smallint,  action_by_discount boolean DEFAULT false,  time_start integer,  time_end integer,  bonus_promotion smallint,  with_old_promotion smallint,  monday smallint,  tuesday smallint,  wednesday smallint,  thursday smallint,  friday smallint,  saturday smallint,  sunday smallint,  promo_code smallint,sum_bonus smallint)WITH (  OIDS=FALSE);ALTER TABLE action_header  OWNER TO postgres;COMMENT ON COLUMN action_header.code_tovar IS 'код товара';COMMENT ON COLUMN action_header.marker IS 'если значение заполнено в 1 то будет показано окно ввода штрихкода товара';COMMENT ON COLUMN action_header.time_start IS 'Время начала действия акции';COMMENT ON COLUMN action_header.time_end IS 'Время окончания действия акции';COMMENT ON COLUMN action_header.bonus_promotion IS '1 - это бонусная акция, иначе - моментальная (старого типа)';COMMENT ON COLUMN action_header.with_old_promotion IS 'пересечение с моментальными акциями: 1 - да, иначе - нет';COMMENT ON COLUMN action_header.monday IS 'акция работает по понедельникам';COMMENT ON COLUMN action_header.tuesday IS 'акция работает по вторникам';COMMENT ON COLUMN action_header.wednesday IS 'акция работает по средам';COMMENT ON COLUMN action_header.thursday IS 'акция работает по четвергам';COMMENT ON COLUMN action_header.friday IS 'акция работает по пятницам';COMMENT ON COLUMN action_header.saturday IS 'акция работает по субботам';COMMENT ON COLUMN action_header.sunday IS 'акция работает по воскресеньям';COMMENT ON COLUMN action_header.promo_code IS 'промо-код при озвучивании которого может сработать бонусная акция, если не заполнен, то не ограничивать';";
                //query[2] = "CREATE TABLE action_table(num_doc integer NOT NULL,  num_list integer,  code_tovar integer, CONSTRAINT action_table_code_tovar_fkey FOREIGN KEY (code_tovar)      REFERENCES tovar (code) MATCH SIMPLE      ON UPDATE NO ACTION ON DELETE NO ACTION)WITH (  OIDS=FALSE);ALTER TABLE action_table  OWNER TO postgres;COMMENT ON COLUMN action_table.code_tovar IS 'Код товара из таблицы товары';";
                queries.Add("CREATE TABLE public.action_table(    num_doc integer NOT NULL,    num_list integer,    code_tovar bigint,    price numeric(10,2),    CONSTRAINT action_table_code_tovar_fkey FOREIGN KEY(code_tovar)        REFERENCES public.tovar(code) MATCH SIMPLE        ON UPDATE NO ACTION        ON DELETE NO ACTION)WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.action_table            OWNER to postgres;        COMMENT ON COLUMN public.action_table.code_tovar            IS 'Код товара из таблицы товары';");
                //query[3] = "CREATE TABLE advertisement(advertisement_text character varying(1000), num_str smallint)WITH (  OIDS=FALSE);ALTER TABLE advertisement  OWNER TO postgres;";
                queries.Add("CREATE TABLE public.advertisement(    advertisement_text character varying(1000) COLLATE pg_catalog.default,    num_str smallint)WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.advertisement            OWNER to postgres;");
                //query[5]  = "CREATE TABLE barcode(  barcode character(13) NOT NULL,  tovar_code integer NOT NULL,  CONSTRAINT barcode_tovar_code_fkey FOREIGN KEY (tovar_code)      REFERENCES tovar (code) MATCH SIMPLE      ON UPDATE NO ACTION ON DELETE NO ACTION)WITH (  OIDS=FALSE);ALTER TABLE barcode  OWNER TO postgres;CREATE INDEX _barcode_tovar_code_  ON barcode  USING btree  (barcode COLLATE pg_catalog.default, tovar_code);";
                queries.Add("CREATE TABLE public.barcode(barcode character(13) COLLATE pg_catalog.default NOT NULL, tovar_code bigint NOT NULL, CONSTRAINT barcode_tovar_code_fkey FOREIGN KEY (tovar_code) REFERENCES public.tovar(code) MATCH SIMPLE        ON UPDATE NO ACTION        ON DELETE NO ACTION)WITH(OIDS = FALSE)TABLESPACE pg_default; ALTER TABLE public.barcode OWNER to postgres; CREATE INDEX _barcode_tovar_code_ ON public.barcode USING btree(barcode COLLATE pg_catalog.default ASC NULLS LAST, tovar_code ASC NULLS LAST)    TABLESPACE pg_default;");
                //CREATE TABLE public.bonus_cards(    code character varying(10) COLLATE pg_catalog."default",    pin character varying(11) COLLATE pg_catalog."default")WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.bonus_cards            OWNER to postgres;
                queries.Add("CREATE TABLE characteristic(tovar_code integer,name character varying(100),guid character varying(36),retail_price_characteristic numeric(10,2),opt_price numeric(10,2))WITH (OIDS=FALSE);ALTER TABLE characteristic OWNER TO postgres;");
                //query[7] = "CREATE TABLE checks_header(  document_number bigint,  client character(10),  cash_desk_number smallint NOT NULL,  comment character(50),  cash numeric(10,2) NOT NULL,  remainder numeric(10,2),  date_time_write timestamp without time zone NOT NULL,  date_time_start timestamp without time zone NOT NULL,  discount numeric(10,2) DEFAULT 0,  its_deleted numeric(1,0) NOT NULL,  action_num_doc integer,  check_type smallint,  have_action boolean,  discount_it_is_sent boolean,  its_print boolean,  is_sent smallint,  cash_money numeric(10,2),  non_cash_money numeric(10,2),  sertificate_money numeric(10,2),  sales_assistant character varying(11),  bonus_is_on smallint,  autor integer,  bonuses_it_is_written_off numeric(8,2) DEFAULT 0,  bonuses_it_is_counted numeric(8,2) DEFAULT 0,  rouble boolean,  type_pay smallint)WITH (  OIDS=FALSE);ALTER TABLE checks_header  OWNER TO postgres;COMMENT ON COLUMN checks_header.discount_it_is_sent IS 'Данные (если начислен или списан этим документом) успешно отправлены в центр';COMMENT ON COLUMN checks_header.its_print IS 'Признак того что чек был нормально напечатан на фискальном принтере';COMMENT ON COLUMN checks_header.is_sent IS '0 - не отправлен 1 - отправлен';CREATE INDEX _checks_header_date_time_write_  ON checks_header  USING btree  (date_time_write);CREATE UNIQUE INDEX checks_header_document_number_idx  ON checks_header  USING btree  (document_number);                ALTER TABLE checks_header CLUSTER ON checks_header_document_number_idx;CREATE UNIQUE INDEX cheks_header_date_time_start  ON checks_header  USING btree  (date_time_start);";
                queries.Add("CREATE TABLE public.checks_header(document_number bigint,client character varying(36) COLLATE pg_catalog.default,cash_desk_number smallint NOT NULL,comment character(50) COLLATE pg_catalog.default,cash numeric(10, 2) NOT NULL,remainder numeric(10, 2),    date_time_write timestamp without time zone NOT NULL,    date_time_start timestamp without time zone NOT NULL,    discount numeric(10, 2) DEFAULT 0,    its_deleted numeric(1, 0) NOT NULL,    action_num_doc integer,    check_type smallint,    have_action boolean,    discount_it_is_sent boolean,    its_print boolean,    is_sent smallint,    cash_money numeric(10, 2),    non_cash_money numeric(10, 2),    sertificate_money numeric(10, 2),    sales_assistant character varying(11) COLLATE pg_catalog.default,    bonus_is_on smallint,    autor integer,    bonuses_it_is_written_off numeric(8, 2) DEFAULT 0,    bonuses_it_is_counted numeric(8, 2) DEFAULT 0,    sent_to_processing_center smallint DEFAULT 0,    id_transaction character varying(10) COLLATE pg_catalog.default,    id_transaction_sale character varying(10) COLLATE pg_catalog.default,    clientinfo_vatin character varying(12) COLLATE pg_catalog.default,    clientinfo_name character varying(200) COLLATE pg_catalog.default,    id_sale bigint,    requisite smallint,    viza_d smallint,    id_transaction_terminal character varying(18) COLLATE pg_catalog.default,    system_taxation smallint DEFAULT 0,    code_authorization_terminal character varying(8) COLLATE pg_catalog.default,    its_print_p boolean,    cash_money1 numeric(10, 2) DEFAULT 0,    non_cash_money1 numeric(10, 2) DEFAULT 0,    sertificate_money1 numeric(10, 2) DEFAULT 0,    guid character varying(36) COLLATE pg_catalog.default NOT NULL DEFAULT ''::character varying)WITH(    OIDS = FALSE)TABLESPACE pg_default;                ALTER TABLE public.checks_header                    OWNER to postgres;        COMMENT ON COLUMN public.checks_header.discount_it_is_sent            IS 'Данные (если начислен или списан этим документом) успешно отправлены в центр';        COMMENT ON COLUMN public.checks_header.its_print            IS 'Признак того что чек был нормально напечатан на фискальном принтере';        COMMENT ON COLUMN public.checks_header.is_sent            IS '0 - не отправлен 1 - отправлен';        COMMENT ON COLUMN public.checks_header.id_transaction            IS 'Номер транзакции в процессинговом центре бонусной программы';        COMMENT ON COLUMN public.checks_header.id_transaction_sale            IS 'Колонка ид транзакции документа продажи на основании которого вводится возврат';        COMMENT ON COLUMN public.checks_header.clientinfo_vatin            IS 'Инн покупателя при возврате';        COMMENT ON COLUMN public.checks_header.clientinfo_name            IS 'Наименования покупателя при возврате';        COMMENT ON COLUMN public.checks_header.system_taxation            IS 'Система налогообложения';        COMMENT ON COLUMN public.checks_header.code_authorization_terminal            IS 'Это служебное поле в ответе от терминала при оплате, его необходимо указывать при возврате ';        COMMENT ON COLUMN public.checks_header.its_print_p            IS 'Признако того что чек был нормально рапечатан на фискальном принтере по налогообложению патент.';        CREATE INDEX _checks_header_date_time_write_            ON public.checks_header USING btree            (date_time_write ASC NULLS LAST)    TABLESPACE pg_default;        CREATE UNIQUE INDEX checks_header_document_number_idx    ON public.checks_header USING btree    (document_number ASC NULLS LAST)    TABLESPACE pg_default;        ALTER TABLE public.checks_header            CLUSTER ON checks_header_document_number_idx;        CREATE UNIQUE INDEX cheks_header_date_time_start    ON public.checks_header USING btree    (date_time_start ASC NULLS LAST)    TABLESPACE pg_default;                    ");
                //queries.Add("CREATE TABLE checks_table(  document_number bigint NOT NULL,  tovar_code integer NOT NULL,  quantity integer NOT NULL,  price numeric(10,2) NOT NULL,  price_at_a_discount numeric(10,2),  sum numeric(10,2) NOT NULL,  sum_at_a_discount numeric(10,2) NOT NULL,  numstr integer NOT NULL,  action_num_doc integer,   action_num_doc1 integer,  action_num_doc2 integer,  characteristic character varying(36),  bonus_standard numeric(8,2),  bonus_promotion numeric(8,2),  promotion_b_mover integer,  CONSTRAINT checks_table_tovar_code_fkey FOREIGN KEY (tovar_code)      REFERENCES tovar (code) MATCH SIMPLE      ON UPDATE NO ACTION ON DELETE NO ACTION)WITH (  OIDS=FALSE);ALTER TABLE checks_table  OWNER TO postgres;COMMENT ON COLUMN checks_table.action_num_doc IS 'сюда пишется номер акционного документа, маркер чтобы не был начислен дисконт';COMMENT ON COLUMN checks_table.action_num_doc1 IS 'когда выдается подарок сюда пишется номер акционного документа';COMMENT ON COLUMN checks_table.action_num_doc2 IS 'сюда пишется номер акционного документа, маркер того чтобы данный товар участвовал в одной акции';COMMENT ON COLUMN checks_table.bonus_standard IS 'бонусы, начисленные по стандартным значениям для производителей/марок/товаров';COMMENT ON COLUMN checks_table.bonus_promotion IS 'бонусы, начисленные по бонусным акциям';COMMENT ON COLUMN checks_table.promotion_b_mover IS 'номер акции, указывается для товаров-инициаторов сработки';");
                queries.Add("CREATE TABLE public.checks_table(document_number bigint NOT NULL,tovar_code bigint NOT NULL,quantity integer NOT NULL,price numeric(10,2) NOT NULL,price_at_a_discount numeric(10,2),sum numeric(10,2) NOT NULL,sum_at_a_discount numeric(10,2) NOT NULL,numstr integer NOT NULL,action_num_doc integer,action_num_doc1 integer,action_num_doc2 integer,characteristic character varying(36) COLLATE pg_catalog.default,bonus_standard numeric(8,2),bonus_promotion numeric(8,2),promotion_b_mover integer,item_marker character varying(200) COLLATE pg_catalog.default,guid character varying(36) COLLATE pg_catalog.default NOT NULL DEFAULT ''::character varying,    CONSTRAINT checks_table_tovar_code_fkey FOREIGN KEY (tovar_code) REFERENCES public.tovar(code) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE NO ACTION )WITH(OIDS = FALSE)TABLESPACE pg_default; ALTER TABLE public.checks_table OWNER to postgres;COMMENT ON COLUMN public.checks_table.action_num_doc IS 'сюда пишется номер акционного документа, маркер чтобы не был начислен дисконт';COMMENT ON COLUMN public.checks_table.action_num_doc1 IS 'когда выдается подарок сюда пишется номер акционного документа';COMMENT ON COLUMN public.checks_table.action_num_doc2 IS 'сюда пишется номер акционного документа, маркер того чтобы данный товар участвовал в одной акции'; COMMENT ON COLUMN public.checks_table.bonus_standard IS 'бонусы, начисленные по стандартным значениям для производителей/марок/товаров';COMMENT ON COLUMN public.checks_table.bonus_promotion IS 'бонусы, начисленные по бонусным акциям';COMMENT ON COLUMN public.checks_table.promotion_b_mover IS 'номер акции, указывается для товаров-инициаторов сработки';");

                queries.Add("CREATE TABLE discount_types(code integer NOT NULL,  discount_percent numeric(10,2) NOT NULL,  transition_sum numeric(10,2) NOT NULL,  name character(10) NOT NULL)WITH (  OIDS=FALSE);ALTER TABLE discount_types  OWNER TO postgres;CREATE UNIQUE INDEX _discount_types_  ON discount_types  USING btree  (code);");
                //queries.Add("CREATE TABLE clients(  name character(100) NOT NULL,  sum numeric(10,2),  discount_types_code integer NOT NULL,  code character(13) NOT NULL,  date_of_birth date,  its_work smallint,  phone character varying(13),  attribute character varying(1),  bonus_is_on smallint,  CONSTRAINT clients_discount_types_code_fkey FOREIGN KEY (discount_types_code)      REFERENCES discount_types (code) MATCH SIMPLE      ON UPDATE NO ACTION ON DELETE NO ACTION)WITH (  OIDS=FALSE);ALTER TABLE clients  OWNER TO postgres;COMMENT ON COLUMN clients.bonus_is_on IS 'Бонусная система включена - 1, выключена - иначе.';CREATE UNIQUE INDEX _clients_code_  ON clients  USING btree  (code COLLATE pg_catalog.default);");
                queries.Add("CREATE TABLE public.clients(name character(100) COLLATE pg_catalog.default NOT NULL,sum numeric(10,2),discount_types_code integer NOT NULL,code character varying(13) COLLATE pg_catalog.default NOT NULL,date_of_birth date,its_work smallint,phone character varying(13) COLLATE pg_catalog.default,attribute character varying(1) COLLATE pg_catalog.default,bonus_is_on smallint,notify_security smallint DEFAULT 0,reason_for_blocking character varying(500) COLLATE pg_catalog.default,CONSTRAINT clients_discount_types_code_fkey FOREIGN KEY (discount_types_code) REFERENCES public.discount_types(code) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE NO ACTION) WITH(OIDS = FALSE) TABLESPACE pg_default; ALTER TABLE public.clients OWNER to postgres;COMMENT ON COLUMN public.clients.bonus_is_on IS 'Бонусная система включена - 1, выключена - иначе.';CREATE UNIQUE INDEX _clients_code_ ON public.clients USING btree (code COLLATE pg_catalog.default ASC NULLS LAST) TABLESPACE pg_default;");
                //queries.Add("CREATE TABLE document_wil_be_printed(  document_number bigint)WITH (  OIDS=FALSE);ALTER TABLE document_wil_be_printed  OWNER TO postgres;CREATE UNIQUE INDEX document_wil_be_printed_document_number_idx  ON document_wil_be_printed  USING btree  (document_number);");
                queries.Add("CREATE TABLE public.document_wil_be_printed(document_number bigint,tax_type smallint)WITH(OIDS = FALSE)TABLESPACE pg_default;ALTER TABLE public.document_wil_be_printed OWNER to postgres; COMMENT ON COLUMN public.document_wil_be_printed.tax_type IS 'Реквизит добавлен для 3 типа налогообложения';CREATE INDEX document_wil_be_printed_document_number_idx2 ON public.document_wil_be_printed USING btree (document_number ASC NULLS LAST, tax_type ASC NULLS LAST) TABLESPACE pg_default;");
                //queries.Add("CREATE TABLE failed_input_phone(client_code character varying(10),datetime_input timestamp without time zone)WITH (  OIDS=FALSE);ALTER TABLE failed_input_phone  OWNER TO postgres;");
                queries.Add("CREATE TABLE public.failed_input_phone(client_code character varying(10) COLLATE pg_catalog.default,datetime_input timestamp without time zone)WITH(OIDS = FALSE)TABLESPACE pg_default;ALTER TABLE public.failed_input_phone OWNER to postgres;");
                //queries.Add("CREATE TABLE logs(time_event timestamp without time zone,description character(200), metadata character(50),  document_number bigint)WITH (  OIDS=FALSE);ALTER TABLE logs  OWNER TO postgres;");
                queries.Add("CREATE TABLE public.logs(time_event timestamp without time zone,description character(200) COLLATE pg_catalog.default,metadata character(50) COLLATE pg_catalog.default,document_number bigint)WITH(OIDS = FALSE)TABLESPACE pg_default;ALTER TABLE public.logs OWNER to postgres;");
                //queries.Add("CREATE TABLE rights_of_users(code smallint NOT NULL,  name character(30) NOT NULL,  CONSTRAINT _code_rights_of_users_ PRIMARY KEY (code))WITH (  OIDS=FALSE);ALTER TABLE rights_of_users  OWNER TO postgres;");
                queries.Add("CREATE TABLE public.rights_of_users(code smallint NOT NULL,name character(30) COLLATE pg_catalog.default NOT NULL,CONSTRAINT _code_rights_of_users_ PRIMARY KEY (code))WITH(OIDS = FALSE)TABLESPACE pg_default; ALTER TABLE public.rights_of_users OWNER to postgres;");
                //queries.Add("CREATE TABLE sertificates(  code integer,  code_tovar integer,  rating integer,  is_active smallint,  CONSTRAINT sertificates_code_tovar_fkey FOREIGN KEY (code_tovar)      REFERENCES tovar (code) MATCH SIMPLE      ON UPDATE NO ACTION ON DELETE NO ACTION)WITH (  OIDS=FALSE);ALTER TABLE sertificates  OWNER TO postgres;");
                queries.Add("CREATE TABLE public.sertificates(code bigint,code_tovar bigint,rating integer,is_active smallint,CONSTRAINT sertificates_code_tovar_fkey FOREIGN KEY (code_tovar)REFERENCES public.tovar(code) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE NO ACTION)WITH(OIDS = FALSE)TABLESPACE pg_default; ALTER TABLE public.sertificates OWNER to postgres;");
                queries.Add("CREATE TABLE temp_code_clients(old_code_client character(10),  new_code_client character(10),  date_time_change timestamp without time zone)WITH (  OIDS=FALSE);ALTER TABLE temp_code_clients  OWNER TO postgres;");
                queries.Add("CREATE TABLE temp_phone_clients(barcode character varying(10),  phone character varying(13))WITH (  OIDS=FALSE);ALTER TABLE temp_phone_clients  OWNER TO postgres;");
                queries.Add("CREATE TABLE tovar_action( code integer NOT NULL,  retail_price numeric(10,2) NOT NULL,  quantity integer,  characteristic_name character varying(100),  characteristic_guid character varying(36))WITH (  OIDS=FALSE);ALTER TABLE tovar_action  OWNER TO postgres;");
                queries.Add("CREATE TABLE users(  code integer NOT NULL,  name character varying(50) NOT NULL,  rights smallint NOT NULL,  shop character(3),  password_m character(47) NOT NULL,  password_b character(47),  inn character varying(12))WITH (  OIDS=FALSE);ALTER TABLE users  OWNER TO postgres;CREATE UNIQUE INDEX users_code  ON users  USING btree  (code);CREATE UNIQUE INDEX users_code_idx ON users  USING btree  (code);");
                //query[20] = "CREATE TABLE constants(  cash_desk_number smallint NOT NULL,  nick_shop character(5) NOT NULL,  width_of_symbols numeric(3,0),  use_usb_to_com_barcode_scaner boolean,  name_com_port character(20),  quantity_of_empty_lines integer DEFAULT 0,  use_fiscal_print boolean,  size_font_listview integer NOT NULL DEFAULT 12,  num_text_printer character varying,  change_path_for_main_computer character varying(255),  code_shop character varying(36),  use_debug boolean,  rate numeric(9,6),  two_currencies boolean,  path_for_web_service character varying(200),  currency character(4),  fiscal_num_port smallint,  fiscal_type_port character varying(3),  unloading_period smallint,  firma character varying(200),  inn character varying(12),  use_trassir smallint,  ip_addr_trassir character varying(15),  ip_port_trassir integer,  show_before_payment_window smallint,  execute_addcolumn smallint,  start_sum_opt_price integer,  envd boolean,  last_date_download_bonus_clients timestamp without time zone)WITH (  OIDS=FALSE);ALTER TABLE constants  OWNER TO postgres;COMMENT ON COLUMN constants.num_text_printer IS 'Номер принтера для текстовой печати';COMMENT ON COLUMN constants.change_path_for_main_computer IS 'Путь к папке обмена с главным компьютером';COMMENT ON COLUMN constants.code_shop IS 'Уникальный номер БД';COMMENT ON COLUMN constants.use_trassir IS '0. Не импользуется трассир.1. Используется трассирю';";

                queries.Add("CREATE TABLE public.constants(cash_desk_number smallint NOT NULL,    nick_shop character(5) COLLATE pg_catalog.default NOT NULL,    code_shop character varying(36) COLLATE pg_catalog.default,    use_debug boolean,    path_for_web_service character varying(200) COLLATE pg_catalog.default,currency character(4) COLLATE pg_catalog.default,    unloading_period smallint,    execute_addcolumn smallint,    envd boolean,    last_date_download_bonus_clients timestamp without time zone,    pass_promo character varying(100) COLLATE pg_catalog.default,    threshold integer,    last_date_download_bonus_cards timestamp without time zone,    print_m boolean,    system_taxation smallint NOT NULL DEFAULT 0,    work_schema smallint NOT NULL DEFAULT 1,    login_promo character varying(100) COLLATE pg_catalog.default,    version_fn smallint,    enable_stock_processing_in_memory boolean,    id_acquirer_terminal character varying(8) COLLATE pg_catalog.default,    ip_address_acquiring_terminal character varying(21) COLLATE pg_catalog.default,self_service_kiosk boolean DEFAULT false,    one_monitors_connected boolean,    version2_marking boolean DEFAULT true,    webservice_authorize boolean DEFAULT false,static_guid_in_print boolean NOT NULL DEFAULT false)WITH(    OIDS = FALSE)TABLESPACE pg_default;                ALTER TABLE public.constants                    OWNER to postgres;        COMMENT ON COLUMN public.constants.code_shop            IS 'Уникальный номер БД';        COMMENT ON COLUMN public.constants.threshold            IS 'Порог срабатывания по выдаче бонусной карты';        COMMENT ON COLUMN public.constants.last_date_download_bonus_cards            IS 'Дата последнего удачного получения данных бонусных карт';        COMMENT ON COLUMN public.constants.work_schema  IS 'Варианты работы программы 1-ЧД 2-ЕВА'; COMMENT ON COLUMN public.constants.self_service_kiosk IS 'Это киоск самообслуживания';COMMENT ON COLUMN public.constants.static_guid_in_print IS 'Переключение для печати, отправка при печати гуид документа или динамический';");

                queries.Add("INSERT INTO constants(use_debug, currency,unloading_period,execute_addcolumn,envd,last_date_download_bonus_clients,cash_desk_number,nick_shop,path_for_web_service,version_fn,version2_marking,print_m)VALUES('false','руб.',0,1,false,'01.01.2000',9,'A00','http://ch.sd2.com.ru/DiscountSystem/Ds.asmx',2,true,false);");

                queries.Add("CREATE SEQUENCE checks_header_document_number_seq  INCREMENT 1  MINVALUE 0  MAXVALUE 9223372036854775807  START 1  CACHE 1;ALTER TABLE checks_header_document_number_seq  OWNER TO postgres;");
                queries.Add("INSERT INTO discount_types(code, discount_percent, transition_sum, name) VALUES (1, 5, 5, '1000000');");
                queries.Add("CREATE TABLE date_sync(tovar date,client date)WITH(OIDS = FALSE); ALTER TABLE date_sync OWNER TO postgres");
                queries.Add("CREATE TABLE deleted_items(num_doc bigint NOT NULL,num_cash smallint NOT NULL,date_time_start timestamp without time zone NOT NULL,date_time_action timestamp without time zone NOT NULL,tovar integer NOT NULL,quantity integer NOT NULL,type_of_operation smallint NOT NULL)WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.deleted_items            OWNER to postgres");
                queries.Add("CREATE TABLE public.client_with_changed_status_to_send(client character varying(10) COLLATE pg_catalog.default NOT NULL,date_change timestamp without time zone NOT NULL,new_phone_number character varying(10) COLLATE pg_catalog.default,   CONSTRAINT client_with_changed_status_to_send_pkey PRIMARY KEY (client) )WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.client_with_changed_status_to_send OWNER to postgres");

                conn.Open();
                trans = conn.BeginTransaction();

                foreach (string str in queries)
                {
                    command = new NpgsqlCommand(str, conn);
                    command.Transaction = trans;
                    command.ExecuteNonQuery();
                }
                trans.Commit();
                conn.Close();
                await MessageBox.Show("Таблицы для базы данных созданы успешно");
            }
            catch (Exception ex)
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
                await MessageBox.Show("Неудачная попытка создать таблицы БД " + ex.ToString());
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        public async void AddField_Click(object sender, RoutedEventArgs e)
        {
            List<string> queries = new List<string>();

            //queries.Add("CREATE TABLE failed_input_phone(client_code character varying(10),  datetime_input timestamp without time zone)WITH (OIDS=FALSE);ALTER TABLE failed_input_phone  OWNER TO postgres");
            //queries.Add("ALTER TABLE tovar DROP COLUMN purchase_price");
            //queries.Add("ALTER TABLE tovar DROP COLUMN opt_price");
            //queries.Add("DROP TABLE public.date_sync; CREATE TABLE public.date_sync (tovar timestamp without time zone,    client date)WITH(    OIDS = FALSE)");
            //queries.Add("ALTER TABLE action_header ADD COLUMN execution_order smallint");
            //queries.Add("ALTER TABLE action_table ADD COLUMN price numeric(10, 2)");
            //queries.Add("UPDATE constants SET use_debug = false");
            //queries.Add("ALTER TABLE constants ADD COLUMN pass_promo character varying(100)");
            ////queries.Add("CREATE TABLE bonus_cards(code character varying(10), pin character varying(11))WITH(OIDS = FALSE); ALTER TABLE bonus_cards OWNER TO postgres;");
            //queries.Add("ALTER TABLE constants ADD COLUMN threshold integer;COMMENT ON COLUMN constants.threshold IS 'Порог срабатывания по выдаче бонусной карты';UPDATE public.constants	SET threshold=0;");
            //queries.Add("ALTER TABLE constants ADD COLUMN last_date_download_bonus_cards timestamp without time zone;COMMENT ON COLUMN constants.last_date_download_bonus_cards IS 'Дата последнего удачного получения данных бонусных карт';UPDATE constants   SET last_date_download_bonus_cards='01-01-2000';");
            //queries.Add("ALTER TABLE checks_header ADD COLUMN sent_to_processing_center smallint;  ALTER TABLE checks_header ALTER COLUMN sent_to_processing_center SET DEFAULT 0;");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN id_transaction character varying(10) COLLATE pg_catalog.default; COMMENT ON COLUMN public.checks_header.id_transaction IS 'Номер транзакции в процессинговом центре бонусной программы';");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN id_transaction_sale character varying(10) COLLATE pg_catalog.default;COMMENT ON COLUMN public.checks_header.id_transaction_sale IS 'Колонка ид транзакции документа продажи на основании которого вводится возврат'; ");
            //queries.Add("ALTER TABLE checks_header ADD COLUMN clientInfo_vatin character varying(12);COMMENT ON COLUMN checks_header.clientInfo_vatin IS 'Инн покупателя при возврате';");
            //queries.Add("ALTER TABLE checks_header ADD COLUMN clientInfo_name character varying(200);COMMENT ON COLUMN checks_header.clientInfo_name IS 'Наименования покупателя при возврате';");
            ////queries.Add("ALTER TABLE public.checks_header ADD COLUMN cardTrack2 character varying(36) COLLATE pg_catalog.default; COMMENT ON COLUMN public.checks_header.cardTrack2    IS 'Номер бонусной карты'");
            ////queries.Add("ALTER TABLE public.checks_header ADD COLUMN phone character varying(12) COLLATE pg_catalog.default; COMMENT ON COLUMN public.checks_header.phone IS 'Номер телефона бонусной карты'");
            //queries.Add("ALTER TABLE checks_header ALTER COLUMN client TYPE character varying(36)");
            //queries.Add("ALTER TABLE public.tovar    ADD COLUMN its_marked smallint;UPDATE public.tovar SET its_marked=0;");
            //queries.Add("ALTER TABLE public.checks_table    ADD COLUMN item_marker character varying(100)");
            //queries.Add("ALTER TABLE public.checks_header   ADD COLUMN id_sale bigint");
            //queries.Add("ALTER TABLE public.constants    ADD COLUMN print_m boolean;UPDATE public.constants	SET print_m=true;");
            //queries.Add("CREATE TABLE public.client_with_changed_status_to_send(client character varying(10) COLLATE pg_catalog.default NOT NULL,date_change timestamp without time zone NOT NULL,new_phone_number character varying(10) COLLATE pg_catalog.default,   CONSTRAINT client_with_changed_status_to_send_pkey PRIMARY KEY (client) )WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.client_with_changed_status_to_send OWNER to postgres");
            //queries.Add("ALTER TABLE action_header ADD COLUMN gift_price numeric(10, 2); ALTER TABLE action_header ALTER COLUMN gift_price SET NOT NULL;");
            //queries.Add("ALTER TABLE action_header ADD COLUMN gift_price numeric(10, 2);UPDATE public.action_header SET gift_price=0; ALTER TABLE action_header ALTER COLUMN gift_price SET NOT NULL;");
            ////queries.Add("ALTER TABLE constants ADD COLUMN usn_income_out_come boolean; UPDATE public.constants SET usn_income_out_come=false;");
            //queries.Add("ALTER TABLE checks_table ALTER COLUMN item_marker TYPE character varying(200)");
            //queries.Add("CREATE TABLE deleted_items(num_doc bigint NOT NULL,num_cash smallint NOT NULL,date_time_start timestamp without time zone NOT NULL,date_time_action timestamp without time zone NOT NULL,tovar integer NOT NULL,quantity integer NOT NULL,type_of_operation smallint NOT NULL)WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.deleted_items            OWNER to postgres");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN system_taxation smallint NOT NULL DEFAULT 0;");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN work_schema smallint NOT NULL DEFAULT 1; COMMENT ON COLUMN public.constants.work_schema IS 'Варианты работы программы 1-ЧД 2-ЕВА';");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN login_promo character varying(100) COLLATE pg_catalog.default;");
            //queries.Add("ALTER TABLE clients ALTER COLUMN code TYPE varchar(13)");
            //queries.Add("ALTER TABLE tovar ALTER COLUMN code TYPE bigint");
            //queries.Add("ALTER TABLE sertificates ALTER COLUMN code TYPE bigint");
            //queries.Add("ALTER TABLE sertificates ALTER COLUMN code_tovar TYPE bigint");
            //queries.Add("ALTER TABLE checks_table ALTER COLUMN tovar_code TYPE bigint");
            //queries.Add("ALTER TABLE barcode ALTER COLUMN tovar_code TYPE bigint");
            //queries.Add("ALTER TABLE action_table ALTER COLUMN code_tovar TYPE bigint");
            //queries.Add("ALTER TABLE action_header ALTER COLUMN code_tovar TYPE bigint");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN version_fn smallint; UPDATE public.constants SET version_fn=2");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN requisite smallint;");
            //queries.Add("ALTER TABLE deleted_items ALTER COLUMN tovar TYPE bigint");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN viza_d smallint;");
            //queries.Add("CREATE UNIQUE INDEX _clients_code_  ON clients  USING btree(code COLLATE pg_catalog.default);");
            //queries.Add("CREATE TABLE public.roll_up_temp(code_tovar bigint,name_tovar character varying(200) COLLATE pg_catalog.default,    characteristic_guid character varying(36) COLLATE pg_catalog.default," +
            //    "characteristic_name character varying(200) COLLATE pg_catalog.default,quantity integer,price numeric(10,2),price_at_a_discount numeric(10,2),sum numeric(10,2),sum_at_a_discount numeric(10,2)," +
            //    "action_num_doc integer,action_num_doc1 integer,action_num_doc2 integer,item_marker character varying(200) COLLATE pg_catalog.default)WITH(OIDS = FALSE)TABLESPACE pg_default;" +
            //    "ALTER TABLE public.roll_up_temp OWNER to postgres;");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN Enable_stock_processing_in_memory boolean;UPDATE public.constants	SET Enable_stock_processing_in_memory=false;");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN id_acquirer_terminal character varying(8) COLLATE pg_catalog.default;UPDATE public.constants	SET id_acquirer_terminal=''");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN ip_address_acquiring_terminal character varying(21) COLLATE pg_catalog.default;UPDATE public.constants	SET ip_address_acquiring_terminal=''");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN self_service_kiosk boolean;COMMENT ON COLUMN public.constants.self_service_kiosk IS 'Это киоск самообслуживания'; UPDATE public.constants	SET self_service_kiosk=false");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN one_monitors_connected boolean;UPDATE public.constants	SET one_monitors_connected=false");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN id_transaction_terminal character varying(18) COLLATE pg_catalog.default;");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN system_taxation smallint DEFAULT 0; COMMENT ON COLUMN public.checks_header.system_taxation IS 'Система налогообложения';");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN code_authorization_terminal character varying(8) COLLATE pg_catalog.default;COMMENT ON COLUMN public.checks_header.code_authorization_terminal IS 'Это служебное поле в ответе от терминала при оплате, его необходимо указывать при возврате ';");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN its_print_p boolean;COMMENT ON COLUMN public.checks_header.its_print_p IS 'Признако того что чек был нормально рапечатан на фискальном принтере по налогообложению патент.';");
            //queries.Add("ALTER TABLE public.document_wil_be_printed ADD COLUMN tax_type smallint; COMMENT ON COLUMN public.document_wil_be_printed.tax_type IS 'Реквизит добавлен для 3 типа налогообложения';");
            //queries.Add("DROP INDEX public.document_wil_be_printed_document_number_idx;");
            //queries.Add("CREATE INDEX document_wil_be_printed_document_number_idx2    ON public.document_wil_be_printed USING btree    (document_number ASC NULLS LAST, tax_type ASC NULLS LAST)    TABLESPACE pg_default;");
            //queries.Add("ALTER TABLE public.action_header ADD COLUMN kind smallint NOT NULL DEFAULT 0;");
            //queries.Add("CREATE UNIQUE INDEX code_client_num_doc ON public.action_clients USING btree (code_client, num_doc); ALTER TABLE public.action_clients CLUSTER ON code_client_num_doc;");
            //queries.Add("ALTER TABLE public.tovar ADD COLUMN its_excise smallint NOT NULL DEFAULT 0;COMMENT ON COLUMN public.tovar.its_excise IS '0 - обычный товар 1 - подакцизный товар';");
            //queries.Add("UPDATE public.constants SET enable_stock_processing_in_memory=true");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN version2_marking boolean  DEFAULT true");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN cash_money1 numeric(10,2) DEFAULT 0");
            //queries.Add("ALTER TABLE public.checks_header ADD COLUMN non_cash_money1 numeric(10,2) DEFAULT 0");
            //queries.Add("ALTER TABLE public.checks_header    ADD COLUMN sertificate_money1 numeric(10,2) DEFAULT 0");
            //queries.Add("ALTER TABLE public.checks_header    ADD COLUMN guid character varying(36) COLLATE pg_catalog.default NOT NULL DEFAULT ''::character varying");
            //queries.Add("ALTER TABLE public.checks_table    ADD COLUMN guid character varying(36) COLLATE pg_catalog.default NOT NULL DEFAULT ''::character varying");
            //queries.Add("ALTER TABLE public.clients ADD COLUMN notify_security smallint DEFAULT 0;");

            queries.Add("ALTER TABLE public.clients ADD COLUMN reason_for_blocking character varying(500) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN webservice_authorize boolean  DEFAULT false;");
            queries.Add("ALTER TABLE public.action_header ADD COLUMN sum1 numeric(12,2); COMMENT ON COLUMN public.action_header.sum1 IS 'Для 12 типа акций сумма по 2 списку';");
            queries.Add("ALTER TABLE public.checks_header ADD COLUMN guid1 character varying(36) COLLATE pg_catalog.default;COMMENT ON COLUMN public.checks_header.guid1 IS 'Для печати чека по патенту';");
            //queries.Add("ALTER TABLE public.constants ADD COLUMN static_guid_in_print boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN printing_using_libraries boolean DEFAULT false;COMMENT ON COLUMN public.constants.printing_using_libraries IS 'Печать с использованием библиотек';");
            queries.Add("ALTER TABLE public.constants ADD COLUMN fn_sreial_port character varying(20) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE public.deleted_items ADD COLUMN guid character varying(36) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE public.checks_header ADD COLUMN payment_by_sbp boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE checks_header ALTER COLUMN id_transaction_terminal TYPE varchar(32);");
            queries.Add("ALTER TABLE  public.constants ALTER COLUMN cdn_token TYPE character varying(36);");
            queries.Add("ALTER TABLE public.constants ADD COLUMN cdn_token character varying(36) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN enable_cdn_markers boolean DEFAULT false;");
            queries.Add("ALTER TABLE checks_header ALTER COLUMN autor TYPE bigint;");
            queries.Add("ALTER TABLE users ALTER COLUMN code TYPE bigint;");
            queries.Add("ALTER TABLE public.clients DROP CONSTRAINT clients_discount_types_code_fkey");
            queries.Add("ALTER TABLE public.clients DROP COLUMN discount_types_code");
            queries.Add("CREATE INDEX _client_phone_ ON public.clients USING btree(phone COLLATE pg_catalog.default ASC NULLS LAST) TABLESPACE pg_default;");
            queries.Add("CREATE INDEX _time_event_ ON public.logs USING btree (time_event ASC NULLS LAST) TABLESPACE pg_default;");
            queries.Add("ALTER TABLE public.logs ALTER COLUMN description TYPE text COLLATE pg_catalog.default;");
            //ALTER TABLE имя_таблицы ALTER COLUMN имя_столбца TYPE text;
            queries.Add("ALTER TABLE checks_header ALTER COLUMN autor TYPE character varying (12) USING autor::character varying(12);");
            queries.Add("ALTER TABLE users ALTER COLUMN code TYPE character varying (12) USING code::character varying(12);");
            queries.Add("ALTER TABLE public.tovar ADD COLUMN cdn_check boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE public.checks_table ALTER COLUMN quantity TYPE numeric(10, 3);");
            queries.Add("ALTER TABLE public.tovar ADD COLUMN fractional boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE public.deleted_items ADD COLUMN autor character varying(12) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE public.deleted_items ADD COLUMN reason character varying(50) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN scale_serial_port character varying(20) COLLATE pg_catalog.default");
            queries.Add("ALTER TABLE public.constants ADD COLUMN get_weight_automatically boolean DEFAULT false;");
            queries.Add("ALTER TABLE public.constants RENAME COLUMN fn_sreial_port TO fn_serial_port;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN variant_connect_fn smallint NOT NULL DEFAULT 0;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN fn_ipaddr character varying(20) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE checks_header ALTER COLUMN id_sale TYPE character varying(36) USING id_sale::character varying(36)");
            queries.Add("ALTER TABLE public.constants ADD COLUMN acquiring_bank smallint DEFAULT 0;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN do_not_prompt_marking_code boolean NOT NULL DEFAULT false; COMMENT ON COLUMN public.constants.do_not_prompt_marking_code IS 'Не запрашивать код марикровки';");
            queries.Add("CREATE TABLE IF NOT EXISTS public.cdn_log (num_cash smallint NOT NULL,date timestamp without time zone NOT NULL,cdn_answer character varying COLLATE pg_catalog.default NOT NULL,numdoc character varying COLLATE pg_catalog.default,is_sent smallint DEFAULT 0)WITH(    OIDS = FALSE)TABLESPACE pg_default;        ALTER TABLE public.cdn_log            OWNER to postgres; COMMENT ON COLUMN public.cdn_log.is_sent    IS '0 - не отправлен 1 - отправлен'; ");
            queries.Add("ALTER TABLE public.cdn_log ADD COLUMN mark character varying(300) COLLATE pg_catalog.default;");
            queries.Add("ALTER TABLE public.cdn_log ADD COLUMN status smallint NOT NULL DEFAULT 0;COMMENT ON COLUMN public.cdn_log.status IS '1 - Ответ от cdn 2 - Отладочная информация 3 - Ошибка при работе с CDN';");
            queries.Add("CREATE TABLE IF NOT EXISTS public.cdn_cash(host character varying(100) COLLATE pg_catalog.default,latensy bigint,date timestamp without time zone)WITH(OIDS = FALSE)TABLESPACE pg_default; ALTER TABLE public.cdn_cash OWNER to postgres;");
            queries.Add("ALTER TABLE public.constants ADD COLUMN constant_conversion_to_kilograms integer NOT NULL DEFAULT 0;");
            queries.Add("CREATE TABLE IF NOT EXISTS public.errors_log(error_message text COLLATE pg_catalog.default,date_time_record timestamp without time zone,num_doc bigint,method_name character varying(255) COLLATE pg_catalog.default,description character varying(255) COLLATE pg_catalog.default)WITH(OIDS = FALSE)TABLESPACE pg_default;ALTER TABLE public.errors_log OWNER to postgres;");
            queries.Add("ALTER TABLE public.tovar ADD COLUMN refusal_of_marking boolean NOT NULL DEFAULT false;");
            queries.Add("CREATE INDEX idx_action_table_doc_tovar_list ON public.action_table USING btree(num_doc ASC NULLS LAST, code_tovar ASC NULLS LAST, num_list ASC NULLS LAST) TABLESPACE pg_default; ALTER TABLE public.action_table CLUSTER ON idx_action_table_doc_tovar_list;");
            queries.Add("ALTER TABLE public.barcode ALTER COLUMN barcode TYPE character(14);");
            queries.Add("ALTER TABLE public.action_header ADD COLUMN picture text COLLATE pg_catalog.default");
            queries.Add("ALTER TABLE public.checks_header ALTER COLUMN action_num_doc TYPE integer[] USING ARRAY[action_num_doc]::integer[]");
            queries.Add("ALTER TABLE IF EXISTS public.advertisement ADD COLUMN picture text COLLATE pg_catalog.default");
            queries.Add("ALTER TABLE IF EXISTS public.constants ADD COLUMN nds_ip smallint DEFAULT 0; COMMENT ON COLUMN public.constants.nds_ip IS 'Ставка ндс для ИП у которого превышен порог нулевого ндс';");
            queries.Add("ALTER TABLE IF EXISTS public.users    ADD COLUMN fiscals_forbidden boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE IF EXISTS public.tovar ADD COLUMN rr_not_control_owner boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE public.cdn_log ALTER COLUMN date TYPE timestamp(3) without time zone;");
            queries.Add("CREATE TABLE IF NOT EXISTS public.open_close_shop (open timestamp without time zone NOT NULL, close timestamp without time zone, date date NOT NULL) TABLESPACE pg_default; ALTER TABLE IF EXISTS public.open_close_shop OWNER to postgres;");
            queries.Add("ALTER TABLE IF EXISTS public.open_close_shop ADD COLUMN its_sent boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE IF EXISTS public.constants ADD COLUMN ip_adress_local_ch_z character varying(15) COLLATE pg_catalog.default");
            queries.Add("ALTER TABLE IF EXISTS public.constants ADD COLUMN kitchen_print character varying(50) COLLATE pg_catalog.default;COMMENT ON COLUMN public.constants.kitchen_print IS 'Адрес/имя кухонного принтера';");
            queries.Add("ALTER TABLE IF EXISTS public.checks_header ADD COLUMN kitchen_print boolean NOT NULL DEFAULT false;");
            queries.Add("ALTER TABLE IF EXISTS public.constants ADD COLUMN piot_url character varying(200) COLLATE pg_catalog.default;");


            //            Блок по созданию индексов для проверки кодов маркировки при офлайн проверке корректности кодов маркировки
            //            --Индекс на коде маркировки
            queries.Add("CREATE INDEX idx_checks_table_marker ON checks_table(item_marker);");
            //            --Составной индекс для быстрого поиска по продажам
            queries.Add("CREATE INDEX idx_checks_header_sales ON checks_header(guid, check_type, its_deleted) WHERE check_type = 0 AND its_deleted = 0;");
            //            --Индекс для возвратов
            queries.Add("CREATE INDEX idx_checks_header_returns ON checks_header(guid, check_type, its_deleted)WHERE check_type = 1 AND its_deleted = 0;");
            //            --Оптимальный индекс для JOIN запросов
            queries.Add("CREATE INDEX idx_checks_header_active ON checks_header(guid, check_type, its_deleted);");
            //            --Частичный индекс для активных чеков
            queries.Add("CREATE INDEX idx_active_checks ON checks_header(its_deleted, check_type, guid)WHERE its_deleted = 0;");

            queries.Add("ALTER TABLE IF EXISTS public.constants ADD COLUMN IF NOT EXISTS include_piot boolean NOT NULL DEFAULT false;");

            foreach (string str in queries)
            {
                append_column(str);
            }

            check_and_correct();
            check_and_correct_date_sync();
            //if (MainStaticClass.CashDeskNumber != 9)
            //{
            //check_system_taxation();
            //}

            await MessageBox.Show(" Дополнительные колонки добавлены ");
        }

        private void append_column(string query)
        {

            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                //MessageBox.Show(ex.Message,query);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, query);
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
        /// Исправление старого типа автор
        /// в колонке
        /// </summary>
        private void check_and_correct()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlTransaction tran = null;
            try
            {
                conn.Open();
                tran = conn.BeginTransaction();
                string query = "SELECT data_type FROM information_schema.columns where table_name = 'errors_log' AND column_name = 'error_message'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                if (command.ExecuteScalar().ToString().Trim() != "text")//старый тип колонки в бд, меняем на новый
                {
                    query = "ALTER TABLE public.errors_log ALTER COLUMN error_message TYPE text COLLATE pg_catalog.default";
                    command = new NpgsqlCommand(query, conn);
                    command.Transaction = tran;
                    command.ExecuteNonQuery();
                }
                tran.Commit();
                conn.Close();
                command.Dispose();
            }
            catch
            {
                if (tran != null)
                {
                    tran.Rollback();
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

        private void check_and_correct_date_sync()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlTransaction tran = null;
            try
            {
                conn.Open();
                tran = conn.BeginTransaction();
                string query = "SELECT COUNT(*) FROM date_sync";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                if (Convert.ToInt16(command.ExecuteScalar()) == 0)//в таблицен нет записей, далее там только обновление, поэтому сделаем 1 запись
                {
                    query = "INSERT INTO date_sync(tovar, client) VALUES ('01.01.2000', '01.01.2000');";
                    command = new NpgsqlCommand(query, conn);
                    command.Transaction = tran;
                    command.ExecuteNonQuery();
                }
                tran.Commit();
                conn.Close();
                command.Dispose();
            }
            catch
            {
                if (tran != null)
                {
                    tran.Rollback();
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
        }

        private void ChangeVisibleBeforeWrite()
        {
            if (_button4 != null) _button4.IsVisible = true;
            if (_button2 != null) _button2.IsVisible = true;
            if (_label1 != null) _label1.IsVisible = true;
            if (_ipaddrServer != null) _ipaddrServer.IsVisible = true;
            if (_label2 != null) _label2.IsVisible = true;
            if (_nameDataBase != null) _nameDataBase.IsVisible = true;
            if (_textBox2 != null) _textBox2.IsVisible = false;
            if (_button1 != null) _button1.IsVisible = false;
            if (_servicePassword != null) _servicePassword.IsVisible = true;
            if (_label5 != null) _label5.IsVisible = true;
            if (_label6 != null) _label6.IsVisible = true;
            if (_repeatPassword != null) _repeatPassword.IsVisible = true;
            if (_checkBox1 != null)
            {
                _checkBox1.IsVisible = true;
                _checkBox1.IsChecked = true;
            }
            if (_groupBox1 != null) _groupBox1.IsVisible = true;
            if (_groupBox2 != null) _groupBox2.IsVisible = true;
            if (_postgresUser != null) _postgresUser.IsVisible = true;
            if (_portServer != null) _portServer.IsVisible = true;
            if (_portPostgres != null) _portPostgres.IsVisible = true;
            if (_userPostrgres != null) _userPostrgres.IsVisible = true;
            if (_checkBox2 != null) _checkBox2.IsVisible = true;
        }

        private void ChangeVisibleAfterWrite()
        {
            //if (_button4 != null) _button4.IsVisible = false;
            if (_button2 != null) _button2.IsVisible = false;
            if (_label1 != null) _label1.IsVisible = false;
            if (_ipaddrServer != null) _ipaddrServer.IsVisible = false;
            if (_label2 != null) _label2.IsVisible = false;
            if (_nameDataBase != null) _nameDataBase.IsVisible = false;
            if (_textBox2 != null) _textBox2.IsVisible = true;
            if (_button1 != null) _button1.IsVisible = true;
            if (_servicePassword != null) _servicePassword.IsVisible = false;
            if (_label5 != null) _label5.IsVisible = false;
            if (_label6 != null) _label6.IsVisible = false;
            if (_repeatPassword != null) _repeatPassword.IsVisible = false;
            if (_checkBox1 != null)
            {
                _checkBox1.IsVisible = false;
                _checkBox1.IsChecked = false;
            }
            if (_groupBox1 != null) _groupBox1.IsVisible = false;
            if (_groupBox2 != null) _groupBox2.IsVisible = false;
            if (_postgresUser != null) _postgresUser.IsVisible = false;
            if (_portServer != null) _portServer.IsVisible = false;
            if (_portPostgres != null) _portPostgres.IsVisible = false;
            if (_userPostrgres != null) _userPostrgres.IsVisible = false;
        }
    }
}