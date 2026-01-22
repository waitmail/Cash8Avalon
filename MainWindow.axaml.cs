using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cash8Avalon.ViewModels;
using System;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Ждем пока окно появится на экране
            await Task.Delay(50);

            // Создаем окно авторизации
            var loginWindow = new Interface_switching();

            bool loginSuccess = false;

            loginWindow.AuthorizationSuccess += (s, password) =>
            {
                loginSuccess = true;
                loginWindow.Close();
            };

            loginWindow.AuthorizationCancel += (s, args) =>
            {
                loginSuccess = false;
                loginWindow.Close();
            };

            // Показываем как модальное окно
            await loginWindow.ShowDialog(this);

            if (loginSuccess)
            {
                // Создаем ViewModel после успешной авторизации
                this.DataContext = new MainViewModel();
            }
            else
            {
                // Закрываем главное окно при отмене
                this.Close();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}