using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Npgsql;
using System;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;

namespace Cash8Avalon
{

    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7
    }
    public partial class InputActionBarcode : Window
    {

       

        // Объявляем поля для элементов управления
        private TextBox? _inputBarcodeTextBox;
        private TextBlock? _authorizationTextBlock;

        public string EnteredBarcode => _inputBarcodeTextBox?.Text?.Trim() ?? string.Empty;

        /*Тип вызова этой формы
         * 1.Вызов для ввода акционного штрихкода (штрихкод акции)
         * 2.Вызов для ввода акционного штрихкода (штрихкод товара когда подарок)
         * 3.Вызов для ввода 
         * 4.Вызов для ввода штрихкода продавца консультанта
         * 5.вызов для ввод 4 последних цифр телефона 
         * 6.вызов для ввода QR - кода маркера товара
         */
        public Cash_check caller = null;
        public ProcessingOfActions caller2 = null;
        //public CheckActions caller3 = null;
        public DataTable dtCopy = null;


        public int call_type = 0;
        public int count = 0;
        public int num_doc = 0; //номер акционного документа по которму выдается подарок
        public int mode = 0;//Это для акционных подарков когда перебирается осносноая dt тогда нужна другая dt,эта переменная показывает в какую dt вствлять строки
        //System.Windows.Forms.Timer input_barcode_timer = null;

        public InputActionBarcode()
        {
            InitializeComponent();

            // Инициализируем элементы после загрузки XAML
            InitializeControls();

            // Устанавливаем позицию окна
            Position = new PixelPoint(332, 99);

            // Подписываемся на события
            SubscribeToEvents();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            // Находим элементы по имени из XAML
            _inputBarcodeTextBox = this.FindControl<TextBox>("inputBarcodeTextBox");
            _authorizationTextBlock = this.FindControl<TextBlock>("authorizationTextBlock");

            if (_inputBarcodeTextBox == null)
            {
                Console.WriteLine("Ошибка: TextBox inputBarcodeTextBox не найден!");
            }

            if (_authorizationTextBlock == null)
            {
                Console.WriteLine("Ошибка: TextBlock authorizationTextBlock не найден!");
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e); // ← ЭТА СТРОКА ДОБАВЛЕНА (ВАЖНО!)
                        
            if (e.Key == Key.Escape) 
            {
                Close(false);
                e.Handled = true;
            }
        }

        private void SubscribeToEvents()
        {
            // Подписываемся на событие загрузки окна
            Loaded += OnWindowLoaded;

            // Подписываемся на события TextBox
            if (_inputBarcodeTextBox != null)
            {
                _inputBarcodeTextBox.KeyDown += InputBarcodeTextBox_KeyDown;
            }

            // Глобальная обработка клавиш для всего окна
            this.KeyDown += Window_KeyDown;
        }

        private void OnWindowLoaded(object? sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус на TextBox после загрузки окна
            _inputBarcodeTextBox?.Focus();

            if (call_type == 1)
            {
                _authorizationTextBlock.Text = "Введите штрихкод(промокод), включающий акцию";
                _inputBarcodeTextBox.MaxLength = 13;
            }
            else if (call_type == 2)
            {
                _authorizationTextBlock.Text = "Введите штрихкод подарка";
                _inputBarcodeTextBox.MaxLength = 13;
            }
            else if (call_type == 3)
            {
                _authorizationTextBlock.Text = "Введите штрихкод администратора";
                _inputBarcodeTextBox.MaxLength = 13;
            }
            else if (call_type == 4)
            {
                //authorization.Text = "Введите штрихкод продавца";
                //input_barcode_timer = new System.Windows.Forms.Timer();
                //input_barcode_timer.Interval = 700;
                //this.input_barcode_timer.Tick += new EventHandler(input_barcode_timer_Tick);
                //this.input_barcode.MouseUp += new MouseEventHandler(input_barcode_MouseUp);
                //this.input_barcode.MaxLength = 13;
            }
            else if (call_type == 5)
            {
                _authorizationTextBlock.Text = "Введите последние 4 цифры номера телефона";
                _inputBarcodeTextBox.MaxLength = 4;
            }
            else if (call_type == 6)
            {
                _authorizationTextBlock.Text = " Просканируйте код маркировки. Отказ - Esc ";
                //this.input_barcode.MaxLength = 100;
            }
            else if (call_type == 7) // Ввод клиента
            {
                _authorizationTextBlock.Text = "Введите код карты (10 символов) или номер телефона (13 символов)";
                _inputBarcodeTextBox.MaxLength = 13;
            }            
        }

        // Обработка нажатия клавиш в TextBox
        //private void InputBarcodeTextBox_KeyDown(object? sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        ProcessBarcode();
        //        e.Handled = true;
        //    }
        //    else if (e.Key == Key.Escape)
        //    {
        //        Close();
        //        e.Handled = true;
        //    }
        //}


        private async void InputBarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (call_type == 1)
            {
                //Cash_check parent = ((Cash_check)this.Parent);

                //if (e.Key == Key.Enter)
                //{
                //    if (caller != null)//Чек продажи 
                //    {
                //        if (!(caller.check_action(_inputBarcodeTextBox.Text)))
                //        {
                //            await MessageBox.Show("Акция с таким штрихкодом не найдена");
                //        }
                //        else
                //        {
                //            if (_inputBarcodeTextBox.Text.Trim().Length > 4)
                //            {
                //                if (caller.action_barcode_list.IndexOf(_inputBarcodeTextBox.Text) == -1)
                //                {
                //                    caller.action_barcode_list.Add(_inputBarcodeTextBox.Text);//Для обычных акций
                //                }
                //            }
                //            else
                //            {
                //                if (caller.action_barcode_bonus_list.IndexOf(_inputBarcodeTextBox.Text) == -1)
                //                {
                //                    caller.action_barcode_bonus_list.Add(_inputBarcodeTextBox.Text);//Для бонусных акций
                //                }
                //            }
                //        }
                //        caller.inpun_action_barcode = false;
                //    }
                //    else if (caller3 != null)//проверка акций
                //    {
                //        if (!(caller3.check_action(_inputBarcodeTextBox.Text)))
                //        {
                //            await MessageBox.Show("Акция с таким штрихкодом не найдена");
                //        }
                //        else
                //        {
                //            if (_inputBarcodeTextBox.Text.Trim().Length > 4)
                //            {
                //                if (caller3.action_barcode_list.IndexOf(_inputBarcodeTextBox.Text) == -1)
                //                {
                //                    caller3.action_barcode_list.Add(_inputBarcodeTextBox.Text);//Для обычных акций
                //                }
                //            }
                //            //else
                //            //{
                //            //    if (caller3.action_barcode_bonus_list.IndexOf(input_barcode.Text) == -1)
                //            //    {
                //            //        caller3.action_barcode_bonus_list.Add(input_barcode.Text);//Для бонусных акций
                //            //    }
                //            //}
                //        }

                //    }

                //    this.Close();
                //}
                //else if (e.Key == Key.Escape)
                //{
                //    if (call_type == 1)
                //    {
                //        caller.inpun_action_barcode = false;
                //    }
                //    //this.Close();
                //    // Закрываем окно с результатом false (отмена)
                //    Close(false);
                //    e.Handled = true;
                //}
            }
            else if (call_type == 2)//После сообщения о подарке ввод штрихкода товара
            {
            //    //Cash_check parent = ((Cash_check)this.Parent);
            //    if (e.KeyChar == 13)
            //    {
            //        if (caller != null)
            //        {
            //            caller.find_barcode_or_code_in_tovar_action(this._inputBarcodeTextBox.Text, count, true, num_doc);
            //        }
            //        else
            //        {
            //            //if (dtCopy == null)
            //            //{
            //            //caller2.find_barcode_or_code_in_tovar_action_dt(this.input_barcode.Text, count, true, num_doc, mode);
            //            //}
            //            //else
            //            //{
            //            caller2.find_barcode_or_code_in_tovar_action_dt(this._inputBarcodeTextBox.Text, count, true, num_doc, mode, dtCopy);
            //            //}
            //        }

            //        this.Close();
            //    }
            //}
            //else if (call_type == 3)//Проверка на удаление документа
            //{
            //    if (e.KeyChar == 13)
            //    {
            //        caller.inpun_action_barcode = false;
            //        this.Close();
            //    }
            //}
            //else if (call_type == 5)//Проверка на 4 последние цифры телефона 
            //{
            //    //if (e.KeyChar != 13)
            //    //{
            //    //    //MessageBox.Show("необходимо ввести 4 цифры ");
            //    //    return;
            //    //}
            //    //if (input_barcode.Text.Trim().Length < 4)
            //    //{
            //    //    await MessageBox.Show("Необходимо ввести 4 цифры ");
            //    //    return;
            //    //}
            //    //int result = 0;
            //    //string client = caller.client.Tag.ToString();
            //    //NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            //    //try
            //    //{
            //    //    conn.Open();
            //    //    string query = "SELECT COUNT(*) FROM clients where right(phone, 4)='" + input_barcode.Text + "' AND code='" + caller.client.Tag.ToString() + "'";
            //    //    NpgsqlCommand command = new NpgsqlCommand(query, conn);
            //    //    result = Convert.ToInt16(command.ExecuteScalar());
            //    //    conn.Close();
            //    //}
            //    //catch (NpgsqlException ex)
            //    //{
            //    //    await MessageBox.Show("Ошибка при проверке номера телефона " + ex.Message);
            //    //}
            //    //catch (Exception ex)
            //    //{
            //    //    await MessageBox.Show("Ошибка при проверке номера телефона " + ex.Message);
            //    //}
            //    //finally
            //    //{
            //    //    if (conn.State == ConnectionState.Open)
            //    //    {
            //    //        conn.Close();
            //    //    }
            //    //}
            //    //if (result != 1)
            //    //{
            //    //    await MessageBox.Show("Введенные цифры не верны");
            //    //    _inputBarcodeTextBox.Text = "";
            //    //    if (MainStaticClass.ckeck_failed_input_phone_on_client(caller.client.Tag.ToString()) > 2)
            //    //    {
            //    //        await MessageBox.Show("Вы превысили число попыток(3) ввести последние 4 цифры номера телефона");
            //    //        this.Close();
            //    //    }
            //    //    insert_record_failed_input_phone();
            //    //}
            //    //else
            //    //{
            //    //    this.DialogResult = DialogResult.OK;
            //    //    this.Close();
            //    //}

            }
            else if (call_type == 6)//Проверка на длину кода маркировки
            {
                if (e.Key != Key.Enter)
                {
                    return;
                }
                //длина строки маркера не должна быть меньше 31 символов
                if (_inputBarcodeTextBox.Text.Trim().Length < 14)
                {
                    await MessageBox.Show("Длина строки кода маркера меньше 14 символа, это ошибка !!! ");
                    _inputBarcodeTextBox.Text = "";
                    return;
                }
                /////////////////////////////////////////////////ЭТО НАДО ВСЕ ПРОВЕРИТЬ !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //Здесь проверяем, на отсутствие символов кириллицы
                //Regex reg = new Regex(@"^([^а-яА-Я]+)$");
                //System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(e.KeyChar.ToString(), "[а-яА-Я]");
                Regex reg = new Regex("[а-яА-ЯёЁ]");
                if (reg.IsMatch(_inputBarcodeTextBox.Text.Trim()))
                {
                    await MessageBox.Show("Обнаружены кириллические символы,ПЕРЕКЛЮЧИТЕ ЯЗЫК ВВОДА НА АНГЛИЙСКИЙ И ПОВТОРИТЕ ВВОД КОДА МАРКИРОВКИ ЕЩЕ РАЗ");
                    _inputBarcodeTextBox.Text = "";
                    return;
                }
                
                this.Close(true);
                e.Handled = true;
            }
            else if (call_type == 7)
            {
                if (e.Key == Key.Enter)
                {
                    string code = _inputBarcodeTextBox.Text?.Trim() ?? "";

                    if (string.IsNullOrEmpty(code))
                    {
                        await MessageBox.Show("Введите код клиента");
                        return;
                    }

                    if (code.Length != 10 && code.Length != 13)
                    {
                        await MessageBox.Show("Код должен содержать 10 или 13 символов");
                        _inputBarcodeTextBox.SelectAll();
                        return;
                    }

                    // Проверяем, если телефон должен начинаться с 9
                    if (code.Length == 13 && !code.StartsWith("9"))
                    {
                        await MessageBox.Show("Номер телефона должен начинаться с 9");
                        _inputBarcodeTextBox.SelectAll();
                        return;
                    }                  
                    e.Handled = true;
                    this.Close(true);
                }
            }

            //if (e.Key == Key.Escape)
            //{
            //    if (call_type == 1)
            //    {
            //        caller.inpun_action_barcode = false;
            //    }
            //    //this.Close();
            //    // Закрываем окно с результатом false (отмена)
            //    Close(false);
            //    e.Handled = true;
            //}

            //else if (e.KeyChar == 27)
            //{
            //    if (call_type == 1)
            //    {
            //        caller.inpun_action_barcode = false;
            //    }
            //    this.Close();
            //}
        }


        // Глобальная обработка клавиш для всего окна
        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            // Дополнительная обработка клавиш, если нужно
        }

        private void ProcessBarcode()
        {
            if (_inputBarcodeTextBox == null) return;

            string barcode = _inputBarcodeTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(barcode))
            {
                // Можно показать сообщение об ошибке
                return;
            }

            // Вызываем событие
            OnBarcodeEntered(barcode);

            // Очищаем поле и возвращаем фокус
            _inputBarcodeTextBox.Text = string.Empty;
            _inputBarcodeTextBox.Focus();
        }

        // Свойство для получения введенного штрихкода
       

        // Метод для очистки поля ввода
        public void ClearInput()
        {
            if (_inputBarcodeTextBox != null)
            {
                _inputBarcodeTextBox.Text = string.Empty;
                _inputBarcodeTextBox.Focus();
            }
        }

        // Метод для установки позиции окна
        public void SetPosition(int x, int y)
        {
            Position = new PixelPoint(x, y);
        }

        // Метод для изменения текста авторизации
        public void SetAuthorizationMessage(string message)
        {
            if (_authorizationTextBlock != null)
            {
                _authorizationTextBlock.Text = message;
            }
        }

        // Событие для передачи результата
        public event EventHandler<string>? BarcodeEntered;

        protected virtual void OnBarcodeEntered(string barcode)
        {
            BarcodeEntered?.Invoke(this, barcode);
        }

        // Метод для установки фокуса на поле ввода
        public void FocusInputField()
        {
            _inputBarcodeTextBox?.Focus();
        }
    }
}