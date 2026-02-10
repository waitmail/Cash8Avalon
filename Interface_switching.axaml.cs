using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Npgsql;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class Interface_switching : Window
    {
        public bool not_change_Cash_Operator = false;
        bool result_execute_enter = false;
        Thread workerThread = null;
        public int caller_type = 0;
        public Cash_check cc = null;
        //private TextBox InputBarcode = null;

        public Interface_switching()
        {
            InitializeComponent();

            // Добавляем настройки для модального окна
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.CanResize = false;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.Title = "Авторизация";

            //// Фокус на поле ввода при загрузке
            //InputBarcode = this.FindControl<TextBox>("input_barcode");
            //InputBarcode?.Focus();
            //InputBarcode.SelectAll();
            this.Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Небольшая задержка для гарантии
            Dispatcher.UIThread.Post(() =>
            {
                SetFocusToInput();
            }, DispatcherPriority.Render);
        }

        private void SetFocusToInput()
        {
            var inputBarcode = this.FindControl<TextBox>("input_barcode");
            if (inputBarcode != null)
            {
                // Пробуем несколько способов
                bool focused = inputBarcode.Focus();

                if (!focused)
                {
                    // Если не получилось сразу, пробуем с задержкой
                    Dispatcher.UIThread.Post(() =>
                    {
                        inputBarcode.Focus();
                        inputBarcode.CaretIndex = 0;

                        // Также можно выбрать весь текст
                        inputBarcode.SelectAll();
                    }, DispatcherPriority.Render);
                }
                else
                {
                    inputBarcode.CaretIndex = 0;
                    inputBarcode.SelectAll();
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Событие для успешной авторизации
        public event EventHandler<string>? AuthorizationSuccess;

        // Событие для закрытия/отмены
        public event EventHandler? AuthorizationCancel;

        protected virtual void OnAuthorizationSuccess(string password)
        {
            AuthorizationSuccess?.Invoke(this, password);
        }

        protected virtual void OnAuthorizationCancel()
        {
            AuthorizationCancel?.Invoke(this, EventArgs.Empty);
        }

        // Обработка нажатия клавиш
        private void Input_barcode_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    string password = textBox.Text;

                    // Проверяем, что пароль не пустой
                    if (password.Trim().Length == 0)
                        return;

                    // Только проверяем пароль на минимальную валидность
                    if (IsValidPassword(password))
                    {
                        // Выполняем основную проверку пароля
                        execute_enter(password);
                    }
                    else
                    {
                        // Неверный пароль (слишком короткий и т.д.)
                        var failAutorize = this.FindControl<TextBox>("fail_autorize");
                        if (failAutorize != null)
                        {
                            failAutorize.Text = "Неверный пароль";
                            failAutorize.Foreground = Avalonia.Media.Brushes.Red;
                        }

                        // Очищаем поле
                        textBox.Text = "";
                        textBox.Focus();
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                // Отмена авторизации
                OnAuthorizationCancel();
            }
        }

        private bool IsValidPassword(string password)
        {
            // Базовая проверка - не пустой
            return !string.IsNullOrWhiteSpace(password);
        }
              

        private void Input_barcode_KeyDown(object sender, KeyEventArgs e)
        {
            // Разрешаем только определенные клавиши
            if (!IsNumericKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    execute_enter(textBox.Text);
                    e.Handled = true;
                }
            }
        }

        private void Input_barcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // Очищаем от нецифровых символов
                string newText = new string(textBox.Text.Where(char.IsDigit).ToArray());
                if (newText != textBox.Text)
                {
                    textBox.Text = newText;
                    textBox.CaretIndex = newText.Length;
                }
            }
        }

        private bool IsNumericKey(Key key)
        {
            // Цифровые клавиши
            if ((key >= Key.D0 && key <= Key.D9) ||
                (key >= Key.NumPad0 && key <= Key.NumPad9))
                return true;

            // Управляющие клавиши
            switch (key)
            {
                case Key.Back:
                case Key.Delete:
                case Key.Enter:
                case Key.Tab:
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                case Key.Escape:
                    return true;

                // Комбинации Ctrl+...
                case Key.C:
                case Key.V:
                case Key.X:
                case Key.A:
                case Key.Z:
                    // Разрешаем только с Ctrl
                    // Проверка на Ctrl делается через KeyModifiers в KeyEventArgs
                    return true;

                default:
                    return false;
            }
        }

        private async Task<int> count_users()
        {
            int rezult = 0;

            try
            {
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT COUNT(*)FROM users ";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                object result = command.ExecuteScalar();
                if (result != null)
                {
                    rezult = Convert.ToInt32(result);
                }
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                // MessageBox.Show асинхронный, но мы используем GetAwaiter().GetResult()
                // для синхронного ожидания
                await MessageBox.Show(ex.Message);//.GetAwaiter().GetResult();
            }

            return rezult;
        }

        //private async void execute_enter(string barcode)
        //{
        //    // Находим контролы
        //    var failAutorize = this.FindControl<TextBox>("fail_autorize");
        //    var inputBarcode = this.FindControl<TextBox>("input_barcode");

        //    if (failAutorize == null)
        //        return;

        //    failAutorize.Text = "";
        //    int result = -1;

        //    //if (MainStaticClass.Use_Usb_to_Com_Barcode_Scaner)
        //    //{
        //    //    inputBarcode.Text = barcode;
        //    //}

        //    // Проверка наличия таблицы если не найдена то это первый запуск
        //    NpgsqlConnection conn = null;
        //    try
        //    {
        //        conn = MainStaticClass.NpgsqlConn();
        //        conn.Open();

        //        using (NpgsqlCommand command = new NpgsqlCommand())
        //        {
        //            command.Connection = conn;
        //            command.CommandText = @"SELECT COUNT(*) FROM information_schema.tables 
        //                           WHERE table_schema='public' 
        //                           AND table_name='users'";

        //            var tableCount = command.ExecuteScalar();

        //            if (Convert.ToInt16(tableCount) == 0)
        //            {
        //                MainStaticClass.Code_right_of_user = 1;
        //                conn.Close();
        //                // Закрываем окно с успешной авторизацией
        //                OnAuthorizationSuccess(barcode);
        //                this.Close();
        //                return;
        //            }
        //        }
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        await MessageBox.Show(ex.Message);
        //        conn?.Close();
        //        return;
        //    }
        //    finally
        //    {
        //        conn?.Close();
        //    }

        //    // Подсчет пользователей
        //    int userCount = count_users();

        //    if ((userCount == 0) && (barcode.Trim() == "1"))
        //    {
        //        // Пользователей еще нет, это первый вход 
        //        MainStaticClass.Code_right_of_user = 1;
        //        OnAuthorizationSuccess(barcode);
        //        this.Close();
        //        return;
        //    }
        //    else
        //    {
        //        if (MainStaticClass.check_new_shema_autenticate() == 1)
        //        {
        //            result = find_user_role_new(barcode);
        //        }
        //        else
        //        {
        //            await MessageBox.Show("Из-за произошедших ошибок авторизация невозможна");
        //            result = 0;
        //        }
        //    }

        //    if (result == 1)
        //    {
        //        if (MainStaticClass.Use_Usb_to_Com_Barcode_Scaner)
        //        {
        //            result_execute_enter = true;
        //        }
        //        MainStaticClass.First_Login_Admin = true;

        //        if ((caller_type == 3) && (cc != null)) // Это авторизация на удаление чека
        //        {
        //            cc.enable_delete = true;
        //            OnAuthorizationSuccess(barcode);
        //            this.Close();
        //            return;
        //        }

        //        MainStaticClass.Code_right_of_user = 1;
        //        OnAuthorizationSuccess(barcode);
        //        this.Close();
        //    }
        //    else if (result == 2)
        //    {
        //        if (MainStaticClass.Use_Usb_to_Com_Barcode_Scaner)
        //        {
        //            result_execute_enter = true;
        //        }

        //        if (!MainStaticClass.First_Login_Admin)
        //        {
        //            await MessageBox.Show("Первая регистрация должна с правами администратора");
        //            return;
        //        }

        //        MainStaticClass.Code_right_of_user = 2;
        //        OnAuthorizationSuccess(barcode);
        //        this.Close();
        //    }
        //    else if (result == 13)
        //    {
        //        failAutorize.Text = "У вас нет прав для входа в программу";
        //        //inputBarcode.Focus();
        //    }
        //    else if (result == 0)
        //    {
        //        failAutorize.Text = "Неудачная попытка авторизации";
        //        //inputBarcode.Focus();
        //    }

        //    inputBarcode.Text = "";
        //}

        private async void execute_enter(string barcode)
        {
            // Находим контролы
            var failAutorize = this.FindControl<TextBox>("fail_autorize");
            var inputBarcode = this.FindControl<TextBox>("input_barcode");

            if (failAutorize == null || inputBarcode == null)
                return;

            failAutorize.Text = "";
            int result = -1;

            //if (MainStaticClass.Use_Usb_to_Com_Barcode_Scaner)
            //{
            //    inputBarcode.Text = barcode;
            //}

            // Проверка наличия таблицы если не найдена то это первый запуск
            NpgsqlConnection conn = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    command.Connection = conn;
                    command.CommandText = @"SELECT COUNT(*) FROM information_schema.tables 
                           WHERE table_schema='public' 
                           AND table_name='users'";

                    var tableCount = command.ExecuteScalar();

                    if (Convert.ToInt16(tableCount) == 0)
                    {
                        MainStaticClass.Code_right_of_user = 1;
                        conn.Close();
                        CloseWithSuccess(inputBarcode.Text);
                        return;
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
                conn?.Close();
                return;
            }
            finally
            {
                conn?.Close();
            }

            // Подсчет пользователей
            int userCount = await count_users();

            if ((userCount == 0) && (inputBarcode.Text.Trim() == "1"))
            {
                MainStaticClass.Code_right_of_user = 1;
                CloseWithSuccess(inputBarcode.Text);
                return;
            }

            // Проверка схемы авторизации
            if (MainStaticClass.check_new_shema_autenticate() != 1)
            {
                await MessageBox.Show("Из-за произошедших ошибок авторизация невозможна");
                ShowErrorMessage("Ошибка системы авторизации");
                return;
            }

            result = await find_user_role_new(inputBarcode.Text);

            // Обработка результатов
            switch (result)
            {
                case 1: // Администратор
                    HandleAdminSuccess(inputBarcode.Text);
                    break;

                case 2: // Кассир
                    HandleCashierSuccess(inputBarcode.Text);
                    break;

                case 13: // Нет прав
                    ShowErrorMessage("У вас нет прав для входа в программу");
                    break;

                case 0: // Неудачная попытка
                default:
                    ShowErrorMessage("Неудачная попытка авторизации");
                    break;
            }
        }

        // Вспомогательные методы
        private void CloseWithSuccess(string password)
        {
            OnAuthorizationSuccess(password);
            this.Close();
        }

        private void HandleAdminSuccess(string password)
        {
            //if (MainStaticClass.Use_Usb_to_Com_Barcode_Scaner)
            //{
            //    result_execute_enter = true;
            //}
            MainStaticClass.First_Login_Admin = true;

            if ((caller_type == 3) && (cc != null))
            {
                cc.enable_delete = true;
            }
            if (!not_change_Cash_Operator)
            {
                MainStaticClass.Code_right_of_user = 1;
            }
            CloseWithSuccess(password);
        }

        private async void HandleCashierSuccess(string password)
        {
            if (MainStaticClass.Use_Usb_to_Com_Barcode_Scaner)
            {
                result_execute_enter = true;
            }

            if (!MainStaticClass.First_Login_Admin)
            {
                await MessageBox.Show("Первая регистрация должна с правами администратора");
                return; // Не закрываем окно
            }

            MainStaticClass.Code_right_of_user = 2;
            CloseWithSuccess(password);
        }

        private void ShowErrorMessage(string message)
        {
            var failAutorize = this.FindControl<TextBox>("fail_autorize");
            var inputBarcode = this.FindControl<TextBox>("input_barcode");

            if (failAutorize != null)
            {
                failAutorize.Text = message;
                failAutorize.Foreground = Avalonia.Media.Brushes.Red;
            }

            if (inputBarcode != null)
            {
                inputBarcode.Text = "";
                inputBarcode.Focus();
            }
        }

        private async Task<int> find_user_role_new(string password)
        {
            int rezult = 0;

            string password_Md5Hash = MainStaticClass.getMd5Hash(password).ToUpper();

            try
            {
                NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT rights,name,code,inn FROM users where password_m='" + password_Md5Hash.Trim() + "' or password_b='" + password_Md5Hash.Trim() + "'";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rezult = Convert.ToInt32(reader.GetInt16(0));
                    if (!not_change_Cash_Operator)
                    {
                        MainStaticClass.Cash_Operator = reader["name"].ToString().Trim();
                        MainStaticClass.Cash_Operator_Client_Code = reader["code"].ToString();
                        MainStaticClass.CashOperatorInn = reader["inn"].ToString();
                    }
                }
                reader.Close();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);//.GetAwaiter().GetResult();
            }

            if (MainStaticClass.Cash_Operator.Trim().ToUpper() == "К9")
            {
                if ((MainStaticClass.CashDeskNumber != 9) || (await MainStaticClass.GetUnloadingInterval() != 0))
                {
                    rezult = 0;
                    MainStaticClass.Cash_Operator = "";
                    MainStaticClass.Cash_Operator_Client_Code = "";
                }
            }

            return rezult;
        }

        // Статический метод для удобного вызова модального окна
        public static string? ShowModal(Window owner, Cash_check? cashCheck = null, int callerType = 0)
        {
            var authWindow = new Interface_switching
            {
                Owner = owner,
                caller_type = callerType,
                cc = cashCheck
            };

            string? resultPassword = null;
            bool isSuccess = false;

            authWindow.AuthorizationSuccess += (s, password) =>
            {
                resultPassword = password;
                isSuccess = true;
            };

            authWindow.AuthorizationCancel += (s, e) =>
            {
                resultPassword = null;
                isSuccess = false;
            };

            // Показываем модально и ждем закрытия
            authWindow.ShowDialog(owner).GetAwaiter().GetResult();

            return isSuccess ? resultPassword : null;
        }
    }
}