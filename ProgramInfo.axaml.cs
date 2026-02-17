using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.IO;
using System.Reflection;

namespace Cash8Avalon;

public partial class ProgramInfo : Window
{
    public ProgramInfo()
    {
        InitializeComponent();
        this.CanResize = false;      
        LoadLogo();
        SetVersion();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadLogo()
    {
        try
        {
            var _LogoImage = this.FindControl<Image>("LogoImage");

            if (_LogoImage == null)
            {
                //System.Diagnostics.Debug.WriteLine("LogoImage = null");
                return;
            }



            // Пробуем найти файл относительно папки программы
            string appPath = AppContext.BaseDirectory;
            string relativePath = Path.Combine(appPath, "Assets", "logo.png");

            if (File.Exists(relativePath))
            {
                _LogoImage.Source = new Bitmap(relativePath);
                //System.Diagnostics.Debug.WriteLine($"Логотип загружен из: {relativePath}");
            }

        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Ошибка загрузки логотипа: {ex.Message}");
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBox.Show(
                    $"Ошибка загрузки логотипа: {ex.Message}",
                    "Загрузка логотипа",
                    MessageBoxButton.OK,
                    MessageBoxType.Error,
                    this);
            });
        }
    }

    private void SetVersion()
    {
        try
        {
            var versionText = this.FindControl<TextBlock>("VersionText");
            if (versionText != null)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    versionText.Text = $"Версия {version.Major}.{version.Minor}.{version.Build}";
                }
            }
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Ошибка установки версии: {ex.Message}");
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBox.Show(
                    $"Ошибка установки версии: {ex.Message}",
                    "Установка версии",
                    MessageBoxButton.OK,
                    MessageBoxType.Error,
                    this);
            });
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}