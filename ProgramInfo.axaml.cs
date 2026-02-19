using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Svg.Skia;
using System;
using System.IO;
using System.Reflection;

namespace Cash8Avalon;

public partial class ProgramInfo : Window
{
    // ✅ Кэш логотипа (чтобы не рендерить SVG каждый раз при открытии окна)
    private static Bitmap? _cachedLogo;

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

    /// <summary>
    /// Загружает логотип: сначала SVG (приоритет), затем PNG (fallback)
    /// </summary>
    private void LoadLogo()
    {
        try
        {
            var logoImage = this.FindControl<Image>("LogoImage");
            if (logoImage == null) return;

            string appPath = AppContext.BaseDirectory;

            // ✅ Сначала пробуем загрузить SVG
            string svgPath = Path.Combine(appPath, "Assets", "logo.svg");

            if (File.Exists(svgPath))
            {
                if (LoadSvgLogo(logoImage, svgPath))
                {
                    System.Diagnostics.Debug.WriteLine($"✓ SVG загружен: {svgPath}");
                    return; // Успех — выходим
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ℹ SVG не найден, пробуем PNG: {svgPath}");
            }

            // ✅ Если SVG не загрузился — пробуем PNG
            string pngPath = Path.Combine(appPath, "Assets", "logo.png");
            if (File.Exists(pngPath))
            {
                if (LoadPngLogo(logoImage, pngPath))
                {
                    System.Diagnostics.Debug.WriteLine($"✓ PNG загружен (fallback): {pngPath}");
                    return;
                }
            }

            // ❌ Ни один формат не загрузился
            System.Diagnostics.Debug.WriteLine($"✗ Не удалось загрузить логотип (SVG/PNG)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Критическая ошибка загрузки логотипа: {ex}");
        }
    }

    /// <summary>
    /// Загружает SVG и конвертирует в Bitmap
    /// </summary>
    private bool LoadSvgLogo(Image logoImage, string svgPath)
    {
        try
        {
            // Проверяем кэш
            if (_cachedLogo != null)
            {
                logoImage.Source = _cachedLogo;
                return true;
            }

            using var svg = SKSvg.CreateFromFile(svgPath);
            if (svg?.Picture == null) return false;

            int width = 128;
            int height = 128;

            using var skBitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(skBitmap);
            canvas.Clear(SKColors.Transparent);

            // Масштабируем SVG под нужный размер
            var svgRect = svg.Picture.CullRect;
            if (svgRect.Width > 0 && svgRect.Height > 0)
            {
                float scale = Math.Min(width / (float)svgRect.Width, height / (float)svgRect.Height);
                canvas.Scale(scale);
            }

            canvas.DrawPicture(svg.Picture);
            canvas.Flush();

            // Конвертируем SKBitmap в Avalonia Bitmap
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();

            _cachedLogo = new Bitmap(stream);
            logoImage.Source = _cachedLogo;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Ошибка загрузки SVG: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Загружает PNG напрямую
    /// </summary>
    private bool LoadPngLogo(Image logoImage, string pngPath)
    {
        try
        {
            // Проверяем кэш
            if (_cachedLogo != null)
            {
                logoImage.Source = _cachedLogo;
                return true;
            }

            _cachedLogo = new Bitmap(pngPath);
            logoImage.Source = _cachedLogo;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Ошибка загрузки PNG: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Устанавливает версию программы из AssemblyInfo
    /// </summary>
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
            System.Diagnostics.Debug.WriteLine($"Ошибка установки версии: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработчик кнопки OK
    /// </summary>
    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    /// <summary>
    /// Освобождает кэш логотипа при закрытии приложения (опционально)
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // Раскомментируйте, если нужно освобождать память:
        // _cachedLogo?.Dispose();
        // _cachedLogo = null;
    }
}