using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cash8Avalon;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ============================================================================
// ENUM ТИПОВ СООБЩЕНИЙ
// ============================================================================
public enum MessageBoxType
{
    Info,
    Warning,
    Error,
    Question
}

// ============================================================================
// ENUM КНОПОК
// ============================================================================
public enum MessageBoxButton
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

// ============================================================================
// ENUM РЕЗУЛЬТАТОВ
// ============================================================================
public enum MessageBoxResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

// ============================================================================
// КЛАСС MESSAGEBOX (ИСПРАВЛЕННАЯ ВЕРСИЯ С ГАРАНТИРОВАННЫМ ФОКУСОМ)
// ============================================================================
public static class MessageBox
{
    private static readonly SemaphoreSlim _showSemaphore = new SemaphoreSlim(1, 1);
    private static readonly TimeSpan _minShowInterval = TimeSpan.FromMilliseconds(100);
    private static DateTime _lastShowTime = DateTime.MinValue;
    private static readonly object _showTimeLock = new object();

    public static async Task Show(string message, string title = "", Window? owner = null)
    {
        await ShowInternal(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
    }

    public static async Task<MessageBoxResult> Show(string message, string title,
                                                     MessageBoxButton buttons,
                                                     MessageBoxType type = MessageBoxType.Info,
                                                     Window? owner = null)
    {
        return await ShowInternal(message, title, buttons, type, owner);
    }

    //private static async Task<MessageBoxResult> ShowInternal(string message, string title,
    //                                                         MessageBoxButton buttons,
    //                                                         MessageBoxType type,
    //                                                         Window? explicitOwner)
    //{
    //    await _showSemaphore.WaitAsync();

    //    try
    //    {
    //        lock (_showTimeLock)
    //        {
    //            var elapsed = DateTime.UtcNow - _lastShowTime;
    //            if (elapsed < _minShowInterval)
    //            {
    //                var delay = _minShowInterval - elapsed;
    //                Task.Delay(delay).Wait();
    //            }
    //        }

    //        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
    //        {
    //            return MessageBoxResult.None;
    //        }

    //        var tcs = new TaskCompletionSource<MessageBoxResult>();
    //        Window? ownerWindow = null;

    //        if (explicitOwner != null && explicitOwner.IsVisible)
    //        {
    //            ownerWindow = explicitOwner;
    //        }
    //        else
    //        {
    //            try { ownerWindow = MainStaticClass.MainWindow; } catch { }

    //            if (ownerWindow == null && desktop.MainWindow != null && desktop.MainWindow.IsVisible)
    //            {
    //                ownerWindow = desktop.MainWindow;
    //            }
    //        }

    //        var mainWindow = new Window
    //        {
    //            Title = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
    //            MinWidth = 420,
    //            MinHeight = 220,
    //            MaxWidth = 800,
    //            MaxHeight = 600,
    //            WindowStartupLocation = ownerWindow != null
    //                ? WindowStartupLocation.CenterOwner
    //                : WindowStartupLocation.CenterScreen,
    //            CanResize = false,
    //            CanMinimize = false,
    //            CanMaximize = false,
    //            ShowInTaskbar = false,
    //            SystemDecorations = SystemDecorations.None,
    //            Topmost = true,
    //            SizeToContent = SizeToContent.WidthAndHeight,
    //            Background = Brushes.Transparent,
    //            Focusable = true // Важно: окно должно быть способно принимать фокус
    //        };

    //        // --- UI Creation (сокращено, логика прежняя) ---
    //        var mainBorder = new Border
    //        {
    //            Background = Brushes.White,
    //            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
    //            BorderThickness = new Thickness(3),
    //            CornerRadius = new CornerRadius(5)
    //        };

    //        var blueHeader = new Border
    //        {
    //            Height = 30,
    //            Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
    //            CornerRadius = new CornerRadius(5, 5, 0, 0),
    //            HorizontalAlignment = HorizontalAlignment.Stretch,
    //            VerticalAlignment = VerticalAlignment.Top,
    //            Child = new Grid
    //            {
    //                Children =
    //                {
    //                    new TextBlock
    //                    {
    //                        Text = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
    //                        Foreground = Brushes.White,
    //                        FontSize = 14,
    //                        FontWeight = FontWeight.Bold,
    //                        VerticalAlignment = VerticalAlignment.Center,
    //                        HorizontalAlignment = HorizontalAlignment.Left,
    //                        Margin = new Thickness(15, 0, 0, 0)
    //                    },
    //                    new Button
    //                    {
    //                        Content = "✕",
    //                        Width = 26,
    //                        Height = 26,
    //                        HorizontalAlignment = HorizontalAlignment.Right,
    //                        VerticalAlignment = VerticalAlignment.Center,
    //                        Margin = new Thickness(0, 0, 8, 0),
    //                        FontSize = 14,
    //                        FontWeight = FontWeight.Bold,
    //                        Background = Brushes.Transparent,
    //                        BorderThickness = new Thickness(0),
    //                        Foreground = Brushes.White,
    //                        Cursor = new Cursor(StandardCursorType.Hand),
    //                        Name = "CloseButton"
    //                    }
    //                }
    //            }
    //        };

    //        var messageStack = new StackPanel
    //        {
    //            Orientation = Orientation.Horizontal,
    //            Spacing = 20,
    //            HorizontalAlignment = HorizontalAlignment.Center
    //        };

    //        var iconText = new TextBlock { Text = GetIconEmoji(type), FontSize = 32, VerticalAlignment = VerticalAlignment.Center, Foreground = GetIconColor(type) };
    //        var messageText = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 500, MinWidth = 220, Foreground = Brushes.Black };

    //        messageStack.Children.Add(iconText);
    //        messageStack.Children.Add(messageText);

    //        var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 15 };
    //        Button? defaultButton = null;

    //        switch (buttons)
    //        {
    //            case MessageBoxButton.OK:
    //                defaultButton = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs, true);
    //                buttonStack.Children.Add(defaultButton);
    //                break;
    //            case MessageBoxButton.OKCancel:
    //                defaultButton = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs, true);
    //                var cancelBtn = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs, false);
    //                buttonStack.Children.Add(defaultButton);
    //                buttonStack.Children.Add(cancelBtn);
    //                break;
    //            case MessageBoxButton.YesNo:
    //                defaultButton = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs, true);
    //                var noBtn = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs, false);
    //                buttonStack.Children.Add(defaultButton);
    //                buttonStack.Children.Add(noBtn);
    //                break;
    //            case MessageBoxButton.YesNoCancel:
    //                defaultButton = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs, true);
    //                var noButton = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs, false);
    //                var cancelButton = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs, false);
    //                buttonStack.Children.Add(defaultButton);
    //                buttonStack.Children.Add(noButton);
    //                buttonStack.Children.Add(cancelButton);
    //                break;
    //        }

    //        var contentStack = new StackPanel { Spacing = 25, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
    //        contentStack.Children.Add(messageStack);
    //        contentStack.Children.Add(buttonStack);

    //        var contentGrid = new Grid { Margin = new Thickness(25, 45, 25, 25), Children = { contentStack } };

    //        var innerBorder = new Border
    //        {
    //            Background = Brushes.White,
    //            BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
    //            BorderThickness = new Thickness(1),
    //            CornerRadius = new CornerRadius(3),
    //            Margin = new Thickness(2),
    //            Child = new Grid { Children = { contentGrid, blueHeader } }
    //        };

    //        mainBorder.Child = innerBorder;
    //        mainWindow.Content = mainBorder;

    //        // ====================================================================
    //        // ПЕРЕМЕННЫЕ ДЛЯ ОБРАБОТЧИКОВ
    //        // ====================================================================
    //        var capturedDefaultButton = defaultButton;
    //        bool isClosing = false;

    //        // ====================================================================
    //        // FOCUS WATCHDOG
    //        // ====================================================================
    //        var focusWatchdog = new DispatcherTimer
    //        {
    //            Interval = TimeSpan.FromMilliseconds(400)
    //        };

    //        focusWatchdog.Tick += (s, e) =>
    //        {
    //            if (isClosing || !mainWindow.IsVisible)
    //            {
    //                focusWatchdog.Stop();
    //                return;
    //            }

    //            // Если окно не активно, принудительно возвращаем фокус
    //            if (!mainWindow.IsActive)
    //            {
    //                Console.WriteLine("[MessageBox] Focus lost! Forcing activation...");
    //                mainWindow.Activate();
    //                mainWindow.Focus();
    //                capturedDefaultButton?.Focus();

    //                if (OperatingSystem.IsLinux())
    //                {
    //                    mainWindow.Topmost = false;
    //                    mainWindow.Topmost = true;
    //                }
    //            }
    //        };

    //        // ====================================================================
    //        // ИСПРАВЛЕННЫЙ ОБРАБОТЧИК OPENED
    //        // ====================================================================
    //        mainWindow.Opened += async (s, e) =>
    //        {
    //            // 1. Запускаем сторожевой таймер
    //            focusWatchdog.Start();

    //            // 2. Даем время оконному менеджеру (особенно Linux/X11) "осознать" окно
    //            await Task.Delay(100);

    //            // 3. Выполняем установку фокуса в UI потоке с высоким приоритетом
    //            await Dispatcher.UIThread.InvokeAsync(() =>
    //            {
    //                // Активируем окно
    //                mainWindow.Activate();
    //                mainWindow.Focus();

    //                // Трюк для Linux: переключение Topmost пробивает защиту фокуса
    //                if (OperatingSystem.IsLinux())
    //                {
    //                    mainWindow.Topmost = false;
    //                    mainWindow.Topmost = true;
    //                }

    //                // Установка фокуса на кнопку
    //                capturedDefaultButton?.Focus();

    //                // Логирование для отладки (можно убрать в релизе)
    //                Console.WriteLine($"[MessageBox] Opened: IsActive={mainWindow.IsActive}, Focused={capturedDefaultButton?.IsFocused}");

    //            }, DispatcherPriority.Render);
    //        };

    //        mainWindow.Closed += (s, e) =>
    //        {
    //            isClosing = true;
    //            focusWatchdog.Stop();
    //            if (!tcs.Task.IsCompleted)
    //                tcs.TrySetResult(MessageBoxResult.None);
    //        };

    //        // ====================================================================
    //        // КНОПКА ЗАКРЫТИЯ
    //        // ====================================================================
    //        if (blueHeader.Child is Grid headerGrid)
    //        {
    //            foreach (var child in headerGrid.Children)
    //            {
    //                if (child is Button closeButton && closeButton.Name == "CloseButton")
    //                {
    //                    closeButton.Click += (s, e) =>
    //                    {
    //                        if (isClosing) return;
    //                        tcs.TrySetResult(MessageBoxResult.Cancel);
    //                        if (mainWindow.IsVisible) mainWindow.Close();
    //                    };
    //                }
    //            }
    //        }

    //        // ====================================================================
    //        // ОБРАБОТКА КЛАВИАТУРЫ
    //        // ====================================================================
    //        mainWindow.KeyDown += (s, e) =>
    //        {
    //            if (e.Key == Key.Escape)
    //            {
    //                e.Handled = true;
    //                if (isClosing) return;
    //                tcs.TrySetResult(MessageBoxResult.Cancel);
    //                if (mainWindow.IsVisible) mainWindow.Close();
    //                return;
    //            }

    //            // Enter обрабатывается либо кнопкой (IsDefault), либо здесь
    //            if (e.Key == Key.Enter && !isClosing)
    //            {
    //                e.Handled = true;
    //                if (capturedDefaultButton != null && capturedDefaultButton.Tag is MessageBoxResult result)
    //                {
    //                    tcs.TrySetResult(result);
    //                    if (mainWindow.IsVisible) mainWindow.Close();
    //                }
    //            }
    //        };

    //        // ====================================================================
    //        // ПОКАЗ ОКНА
    //        // ====================================================================
    //        try
    //        {
    //            if (ownerWindow != null)
    //            {
    //                // На Linux перед показом диалога лучше активировать владельца
    //                if (OperatingSystem.IsLinux())
    //                {
    //                    ownerWindow.Activate();
    //                    await Task.Delay(20);
    //                }
    //                await mainWindow.ShowDialog(ownerWindow);
    //            }
    //            else
    //            {
    //                mainWindow.Show();
    //                await tcs.Task;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"❌ MessageBox error: {ex.Message}");
    //            return MessageBoxResult.None;
    //        }

    //        lock (_showTimeLock)
    //        {
    //            _lastShowTime = DateTime.UtcNow;
    //        }

    //        return await tcs.Task;
    //    }
    //    finally
    //    {
    //        _showSemaphore.Release();
    //    }
    //}

    private static async Task<MessageBoxResult> ShowInternal(string message, string title,
                                                         MessageBoxButton buttons,
                                                         MessageBoxType type,
                                                         Window? explicitOwner)
    {
        // Семафор ждём в любом потоке — это безопасно
        await _showSemaphore.WaitAsync();

        try
        {
            // Троттлинг тоже безопасен в любом потоке
            lock (_showTimeLock)
            {
                var elapsed = DateTime.UtcNow - _lastShowTime;
                if (elapsed < _minShowInterval)
                {
                    var delay = _minShowInterval - elapsed;
                    Task.Delay(delay).Wait();
                }
            }

            // ВАЖНО: всю работу с UI-объектами выполняем на UI-потоке!
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                {
                    return MessageBoxResult.None;
                }

                var tcs = new TaskCompletionSource<MessageBoxResult>();
                Window? ownerWindow = null;

                if (explicitOwner != null && explicitOwner.IsVisible)
                {
                    ownerWindow = explicitOwner;
                }
                else
                {
                    try { ownerWindow = MainStaticClass.MainWindow; } catch { }

                    if (ownerWindow == null && desktop.MainWindow != null && desktop.MainWindow.IsVisible)
                    {
                        ownerWindow = desktop.MainWindow;
                    }
                }

                var mainWindow = new Window
                {
                    Title = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
                    MinWidth = 420,
                    MinHeight = 220,
                    MaxWidth = 800,
                    MaxHeight = 600,
                    WindowStartupLocation = ownerWindow != null
                        ? WindowStartupLocation.CenterOwner
                        : WindowStartupLocation.CenterScreen,
                    CanResize = false,
                    CanMinimize = false,
                    CanMaximize = false,
                    ShowInTaskbar = false,
                    SystemDecorations = SystemDecorations.None,
                    Topmost = true,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Background = Brushes.Transparent,
                    Focusable = true // Важно: окно должно быть способно принимать фокус
                };

                // --- UI Creation ---
                var mainBorder = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    BorderThickness = new Thickness(3),
                    CornerRadius = new CornerRadius(5)
                };

                var blueHeader = new Border
                {
                    Height = 30,
                    Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    CornerRadius = new CornerRadius(5, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new Grid
                    {
                        Children =
                    {
                        new TextBlock
                        {
                            Text = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
                            Foreground = Brushes.White,
                            FontSize = 14,
                            FontWeight = FontWeight.Bold,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(15, 0, 0, 0)
                        },
                        new Button
                        {
                            Content = "✕",
                            Width = 26,
                            Height = 26,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 8, 0),
                            FontSize = 14,
                            FontWeight = FontWeight.Bold,
                            Background = Brushes.Transparent,
                            BorderThickness = new Thickness(0),
                            Foreground = Brushes.White,
                            Cursor = new Cursor(StandardCursorType.Hand),
                            Name = "CloseButton"
                        }
                    }
                    }
                };

                var messageStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 20,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var iconText = new TextBlock { Text = GetIconEmoji(type), FontSize = 32, VerticalAlignment = VerticalAlignment.Center, Foreground = GetIconColor(type) };
                var messageText = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 500, MinWidth = 220, Foreground = Brushes.Black };

                messageStack.Children.Add(iconText);
                messageStack.Children.Add(messageText);

                var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 15 };
                Button? defaultButton = null;

                switch (buttons)
                {
                    case MessageBoxButton.OK:
                        defaultButton = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs, true);
                        buttonStack.Children.Add(defaultButton);
                        break;
                    case MessageBoxButton.OKCancel:
                        defaultButton = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs, true);
                        var cancelBtn = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs, false);
                        buttonStack.Children.Add(defaultButton);
                        buttonStack.Children.Add(cancelBtn);
                        break;
                    case MessageBoxButton.YesNo:
                        defaultButton = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs, true);
                        var noBtn = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs, false);
                        buttonStack.Children.Add(defaultButton);
                        buttonStack.Children.Add(noBtn);
                        break;
                    case MessageBoxButton.YesNoCancel:
                        defaultButton = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs, true);
                        var noButton = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs, false);
                        var cancelButton = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs, false);
                        buttonStack.Children.Add(defaultButton);
                        buttonStack.Children.Add(noButton);
                        buttonStack.Children.Add(cancelButton);
                        break;
                }

                var contentStack = new StackPanel { Spacing = 25, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                contentStack.Children.Add(messageStack);
                contentStack.Children.Add(buttonStack);

                var contentGrid = new Grid { Margin = new Thickness(25, 45, 25, 25), Children = { contentStack } };

                var innerBorder = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(2),
                    Child = new Grid { Children = { contentGrid, blueHeader } }
                };

                mainBorder.Child = innerBorder;
                mainWindow.Content = mainBorder;

                // ====================================================================
                // ПЕРЕМЕННЫЕ ДЛЯ ОБРАБОТЧИКОВ
                // ====================================================================
                var capturedDefaultButton = defaultButton;
                bool isClosing = false;

                // ====================================================================
                // FOCUS WATCHDOG
                // ====================================================================
                var focusWatchdog = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(400)
                };

                focusWatchdog.Tick += (s, e) =>
                {
                    if (isClosing || !mainWindow.IsVisible)
                    {
                        focusWatchdog.Stop();
                        return;
                    }

                    // Если окно не активно, принудительно возвращаем фокус
                    if (!mainWindow.IsActive)
                    {
                        Console.WriteLine("[MessageBox] Focus lost! Forcing activation...");
                        mainWindow.Activate();
                        mainWindow.Focus();
                        capturedDefaultButton?.Focus();

                        if (OperatingSystem.IsLinux())
                        {
                            mainWindow.Topmost = false;
                            mainWindow.Topmost = true;
                        }
                    }
                };

                // ====================================================================
                // ИСПРАВЛЕННЫЙ ОБРАБОТЧИК OPENED
                // ====================================================================
                mainWindow.Opened += async (s, e) =>
                {
                    // 1. Запускаем сторожевой таймер
                    focusWatchdog.Start();

                    // 2. Даем время оконному менеджеру (особенно Linux/X11) "осознать" окно
                    await Task.Delay(100);

                    // 3. Выполняем установку фокуса в UI потоке с высоким приоритетом
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Активируем окно
                        mainWindow.Activate();
                        mainWindow.Focus();

                        // Трюк для Linux: переключение Topmost пробивает защиту фокуса
                        if (OperatingSystem.IsLinux())
                        {
                            mainWindow.Topmost = false;
                            mainWindow.Topmost = true;
                        }

                        // Установка фокуса на кнопку
                        capturedDefaultButton?.Focus();

                        // Логирование для отладки (можно убрать в релизе)
                        Console.WriteLine($"[MessageBox] Opened: IsActive={mainWindow.IsActive}, Focused={capturedDefaultButton?.IsFocused}");

                    }, DispatcherPriority.Render);
                };

                mainWindow.Closed += (s, e) =>
                {
                    isClosing = true;
                    focusWatchdog.Stop();
                    if (!tcs.Task.IsCompleted)
                        tcs.TrySetResult(MessageBoxResult.None);
                };

                // ====================================================================
                // КНОПКА ЗАКРЫТИЯ
                // ====================================================================
                if (blueHeader.Child is Grid headerGrid)
                {
                    foreach (var child in headerGrid.Children)
                    {
                        if (child is Button closeButton && closeButton.Name == "CloseButton")
                        {
                            closeButton.Click += (s, e) =>
                            {
                                if (isClosing) return;
                                tcs.TrySetResult(MessageBoxResult.Cancel);
                                if (mainWindow.IsVisible) mainWindow.Close();
                            };
                        }
                    }
                }

                // ====================================================================
                // ОБРАБОТКА КЛАВИАТУРЫ
                // ====================================================================
                mainWindow.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Escape)
                    {
                        e.Handled = true;
                        if (isClosing) return;
                        tcs.TrySetResult(MessageBoxResult.Cancel);
                        if (mainWindow.IsVisible) mainWindow.Close();
                        return;
                    }

                    // Enter обрабатывается либо кнопкой (IsDefault), либо здесь
                    if (e.Key == Key.Enter && !isClosing)
                    {
                        e.Handled = true;
                        if (capturedDefaultButton != null && capturedDefaultButton.Tag is MessageBoxResult result)
                        {
                            tcs.TrySetResult(result);
                            if (mainWindow.IsVisible) mainWindow.Close();
                        }
                    }
                };

                // ====================================================================
                // ПОКАЗ ОКНА
                // ====================================================================
                try
                {
                    if (ownerWindow != null)
                    {
                        // На Linux перед показом диалога лучше активировать владельца
                        if (OperatingSystem.IsLinux())
                        {
                            ownerWindow.Activate();
                            await Task.Delay(20);
                        }
                        await mainWindow.ShowDialog(ownerWindow);
                    }
                    else
                    {
                        mainWindow.Show();
                        await tcs.Task;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ MessageBox error: {ex.Message}");
                    return MessageBoxResult.None;
                }

                lock (_showTimeLock)
                {
                    _lastShowTime = DateTime.UtcNow;
                }

                return await tcs.Task;
            }, DispatcherPriority.Normal);
        }
        finally
        {
            _showSemaphore.Release();
        }
    }

    // Обновленный метод создания кнопки с поддержкой IsDefault
    private static Button CreateButton(string content, MessageBoxResult buttonResult, Window dialog, TaskCompletionSource<MessageBoxResult> tcs, bool isDefault)
    {
        var normalBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
        var hoverBackground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
        var pressedBackground = new SolidColorBrush(Color.FromRgb(210, 210, 210));
        var borderColor = new SolidColorBrush(Color.FromRgb(180, 180, 180));

        var button = new Button
        {
            Content = new TextBlock { Text = content, FontSize = 13, FontWeight = FontWeight.Medium, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Black },
            MinWidth = 90,
            Height = 35,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = normalBackground,
            BorderBrush = borderColor,
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(20, 0, 20, 0),
            Tag = buttonResult,
            IsDefault = isDefault // ВАЖНО: позволяет нажимать Enter даже если фокус не на кнопке
        };

        button.PointerEntered += (s, e) => { button.Background = hoverBackground; button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160)); };
        button.PointerExited += (s, e) => { button.Background = normalBackground; button.BorderBrush = borderColor; };
        button.PointerPressed += (s, e) => button.Background = pressedBackground;
        button.PointerReleased += (s, e) => button.Background = hoverBackground;

        button.Click += (s, e) =>
        {
            tcs.TrySetResult(buttonResult);
            if (dialog.IsVisible) dialog.Close();
        };

        return button;
    }

    private static IBrush GetIconColor(MessageBoxType type) => type switch
    {
        MessageBoxType.Info => new SolidColorBrush(Color.FromRgb(0, 122, 204)),
        MessageBoxType.Warning => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
        MessageBoxType.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
        MessageBoxType.Question => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
        _ => new SolidColorBrush(Color.FromRgb(0, 122, 204))
    };

    private static string GetDefaultTitle(MessageBoxType type) => type switch
    {
        MessageBoxType.Info => "Информация",
        MessageBoxType.Warning => "Предупреждение",
        MessageBoxType.Error => "Ошибка",
        MessageBoxType.Question => "Вопрос",
        _ => "Сообщение"
    };

    private static string GetIconEmoji(MessageBoxType type) => type switch
    {
        MessageBoxType.Info => "\u2139",
        MessageBoxType.Warning => "\u26A0",
        MessageBoxType.Error => "\u274C",
        MessageBoxType.Question => "\u2753",
        _ => "\u2022"
    };
}

[Obsolete("Используйте MessageBox.Show напрямую.")]
public static class MessageBoxHelper
{
    [Obsolete("Используйте MessageBox.Show напрямую.")]
    public static async Task Show(string message, string title = "", Window? owner = null)
    {
        await MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
    }

    [Obsolete("Используйте MessageBox.Show напрямую.")]
    public static async Task<MessageBoxResult> Show(string message, string title,
                                                     MessageBoxButton buttons,
                                                     MessageBoxType type = MessageBoxType.Info,
                                                     Window? owner = null)
    {
        return await MessageBox.Show(message, title, buttons, type, owner);
    }

    [Obsolete("Метод устарел.")]
    public static async Task ActivateWindow(Window? window)
    {
        if (window == null || !window.IsVisible) return;
        await Dispatcher.UIThread.InvokeAsync(() => { if (window.IsVisible) { window.Activate(); if (!OperatingSystem.IsLinux()) window.Focus(); } }, DispatcherPriority.Background);
    }
}