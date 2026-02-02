using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;

// ENUM ТИПОВ СООБЩЕНИЙ
public enum MessageBoxType
{
    Info,
    Warning,
    Error,
    Question
}

// ENUM КНОПОК
public enum MessageBoxButton
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

// ENUM РЕЗУЛЬТАТОВ
public enum MessageBoxResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

// КЛАСС MESSAGEBOX
public static class MessageBox
{
    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    // Простая версия (owner - опциональный параметр)
    public static async Task Show(string message, string title = "", Window owner = null)
    {
        await ShowInternal(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
    }

    // Полная версия (owner - опциональный параметр)
    public static async Task<MessageBoxResult> Show(string message, string title,
                                                     MessageBoxButton buttons,
                                                     MessageBoxType type = MessageBoxType.Info,
                                                     Window owner = null)
    {
        return await ShowInternal(message, title, buttons, type, owner);
    }

    // ========== ОСНОВНОЙ ВНУТРЕННИЙ МЕТОД ==========
    private static async Task<MessageBoxResult> ShowInternal(string message, string title,
                                                             MessageBoxButton buttons,
                                                             MessageBoxType type,
                                                             Window explicitOwner)
    {
        if (Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>();

            // УМНАЯ ЛОГИКА ОПРЕДЕЛЕНИЯ РОДИТЕЛЬСКОГО ОКНА
            Window ownerWindow = null;

            // Приоритет 1: Явно указанное окно
            if (explicitOwner != null && explicitOwner.IsVisible)
            {
                ownerWindow = explicitOwner;
            }
            // Приоритет 2: Активное окно приложения (старая логика)
            else if (desktop.Windows.Count > 0)
            {
                ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ??
                              desktop.Windows.FirstOrDefault(w => w.IsVisible);
            }
            // Приоритет 3: Главное окно
            else if (desktop.MainWindow != null && desktop.MainWindow.IsVisible)
            {
                ownerWindow = desktop.MainWindow;
            }

            // СОЗДАЕМ ОСНОВНОЕ ОКНО
            var mainWindow = new Window
            {
                Title = "",
                MinWidth = 420,
                MinHeight = 220,
                MaxWidth = 800,
                MaxHeight = 600,
                WindowStartupLocation = ownerWindow != null
                    ? WindowStartupLocation.CenterOwner
                    : WindowStartupLocation.CenterScreen,
                CanResize = true,
                CanMinimize = false,
                CanMaximize = false,
                ShowInTaskbar = false,
                SystemDecorations = SystemDecorations.None,
                Topmost = ownerWindow == null, // Только поверх других окон, если нет родителя
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = Brushes.Transparent
            };

            // ГЛАВНЫЙ КОНТЕЙНЕР
            var mainContainer = new Grid();

            // ========== ОСНОВНАЯ ПАНЕЛЬ С ВЫРАЗИТЕЛЬНОЙ РАМКОЙ ==========
            var mainBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)), // Яркая синяя рамка
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(5),
                ZIndex = 2
            };

            // ========== СИНЯЯ ПАНЕЛЬ ВВЕРХУ ==========
            var blueHeader = new Border
            {
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), // #FF007ACC
                CornerRadius = new CornerRadius(5, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                ZIndex = 3,
                Child = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Children =
                    {
                        // ЗАГОЛОВОК
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
                        // КНОПКА ЗАКРЫТИЯ
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
                            Cursor = new Cursor(StandardCursorType.Hand)
                        }
                    }
                }
            };

            // ========== ОСНОВНОЙ КОНТЕНТ ==========
            var contentGrid = new Grid
            {
                Margin = new Thickness(25, 45, 25, 25)
            };

            var contentStack = new StackPanel
            {
                Spacing = 25,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // СООБЩЕНИЕ И ИКОНКА
            var messageStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var iconText = new TextBlock
            {
                Text = GetIconEmoji(type),
                FontSize = 32,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = GetIconColor(type)
            };

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 500,
                MinWidth = 220,
                Foreground = Brushes.Black
            };

            messageStack.Children.Add(iconText);
            messageStack.Children.Add(messageText);

            // КНОПКИ
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 15
            };

            // СОЗДАЕМ КНОПКИ В ЗАВИСИМОСТИ ОТ ТИПА
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    var okButton = CreateNeutralButton("OK", MessageBoxResult.OK, true, mainWindow, tcs);
                    buttonStack.Children.Add(okButton);
                    break;

                case MessageBoxButton.OKCancel:
                    var okBtn = CreateNeutralButton("OK", MessageBoxResult.OK, true, mainWindow, tcs);
                    var cancelBtn = CreateNeutralButton("Отмена", MessageBoxResult.Cancel, false, mainWindow, tcs);
                    buttonStack.Children.Add(okBtn);
                    buttonStack.Children.Add(cancelBtn);
                    break;

                case MessageBoxButton.YesNo:
                    var yesBtn = CreateNeutralButton("Да", MessageBoxResult.Yes, true, mainWindow, tcs);
                    var noBtn = CreateNeutralButton("Нет", MessageBoxResult.No, false, mainWindow, tcs);
                    buttonStack.Children.Add(yesBtn);
                    buttonStack.Children.Add(noBtn);
                    break;

                case MessageBoxButton.YesNoCancel:
                    var yesButton = CreateNeutralButton("Да", MessageBoxResult.Yes, true, mainWindow, tcs);
                    var noButton = CreateNeutralButton("Нет", MessageBoxResult.No, false, mainWindow, tcs);
                    var cancelButton = CreateNeutralButton("Отмена", MessageBoxResult.Cancel, false, mainWindow, tcs);
                    buttonStack.Children.Add(yesButton);
                    buttonStack.Children.Add(noButton);
                    buttonStack.Children.Add(cancelButton);
                    break;
            }

            // СБОРКА ИНТЕРФЕЙСА
            contentStack.Children.Add(messageStack);
            contentStack.Children.Add(buttonStack);
            contentGrid.Children.Add(contentStack);

            // ========== ВНУТРЕННЯЯ ПАНЕЛЬ ==========
            var innerBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(2),
                Child = new Grid
                {
                    Children =
                    {
                        contentGrid,
                        blueHeader
                    }
                }
            };

            mainBorder.Child = innerBorder;
            mainContainer.Children.Add(mainBorder);
            mainWindow.Content = mainContainer;

            // ОБРАБОТЧИКИ СОБЫТИЙ
            mainWindow.Closed += (s, e) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetResult(MessageBoxResult.None);
            };

            // ОБРАБОТЧИК ДЛЯ КНОПКИ ЗАКРЫТИЯ
            if (blueHeader.Child is Grid headerGrid)
            {
                foreach (var child in headerGrid.Children)
                {
                    if (child is Button closeButton && closeButton.Content as string == "✕")
                    {
                        closeButton.Click += (s, e) =>
                        {
                            tcs.TrySetResult(MessageBoxResult.Cancel);
                            mainWindow.Close();
                        };

                        closeButton.PointerEntered += (s, e) =>
                        {
                            closeButton.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                        };

                        closeButton.PointerExited += (s, e) =>
                        {
                            closeButton.Background = Brushes.Transparent;
                        };
                    }
                }
            }

            // ОБРАБОТКА КЛАВИШ
            mainWindow.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Escape)
                {
                    e.Handled = true;
                    tcs.TrySetResult(MessageBoxResult.Cancel);
                    mainWindow.Close();
                }
            };

            // ========== ПОКАЗ ОКНА ==========
            if (ownerWindow != null)
            {
                // Модальное окно с родителем
                await mainWindow.ShowDialog(ownerWindow);
            }
            else
            {
                // Без родителя (старая логика)
                mainWindow.Show();
            }

            return await tcs.Task;
        }

        return MessageBoxResult.None;
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

    private static Button CreateNeutralButton(string content, MessageBoxResult buttonResult,
                                            bool isDefault, Window dialog,
                                            TaskCompletionSource<MessageBoxResult> tcs)
    {
        var normalBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
        var hoverBackground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
        var pressedBackground = new SolidColorBrush(Color.FromRgb(210, 210, 210));
        var borderColor = new SolidColorBrush(Color.FromRgb(180, 180, 180));

        var button = new Button
        {
            Content = new TextBlock
            {
                Text = content,
                FontSize = 13,
                FontWeight = FontWeight.Medium,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Black
            },
            MinWidth = 90,
            Height = 35,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = normalBackground,
            BorderBrush = borderColor,
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(20, 0, 20, 0)
        };

        // ЭФФЕКТ ПРИ НАВЕДЕНИИ
        button.PointerEntered += (s, e) =>
        {
            button.Background = hoverBackground;
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
        };

        button.PointerExited += (s, e) =>
        {
            button.Background = normalBackground;
            button.BorderBrush = borderColor;
        };

        button.PointerPressed += (s, e) =>
        {
            button.Background = pressedBackground;
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(140, 140, 140));
        };

        button.PointerReleased += (s, e) =>
        {
            button.Background = hoverBackground;
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
        };

        button.Click += (s, e) =>
        {
            tcs.TrySetResult(buttonResult);
            dialog.Close();
        };

        // УСТАНАВЛИВАЕМ ФОКУС НА КНОПКУ ПО УМОЛЧАНИЮ
        if (isDefault)
        {
            dialog.Opened += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    button.Focus();
                }, DispatcherPriority.Input);
            };
        }

        // ОБРАБОТКА ENTER
        if (isDefault)
        {
            dialog.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    e.Handled = true;
                    tcs.TrySetResult(buttonResult);
                    dialog.Close();
                }
            };
        }

        return button;
    }

    private static IBrush GetIconColor(MessageBoxType type)
    {
        switch (type)
        {
            case MessageBoxType.Info:
                return new SolidColorBrush(Color.FromRgb(0, 122, 204));
            case MessageBoxType.Warning:
                return new SolidColorBrush(Color.FromRgb(255, 140, 0));
            case MessageBoxType.Error:
                return new SolidColorBrush(Color.FromRgb(220, 53, 69));
            case MessageBoxType.Question:
                return new SolidColorBrush(Color.FromRgb(40, 167, 69));
            default:
                return new SolidColorBrush(Color.FromRgb(0, 122, 204));
        }
    }

    private static string GetDefaultTitle(MessageBoxType type)
    {
        switch (type)
        {
            case MessageBoxType.Info: return "Информация";
            case MessageBoxType.Warning: return "Предупреждение";
            case MessageBoxType.Error: return "Ошибка";
            case MessageBoxType.Question: return "Вопрос";
            default: return "Сообщение";
        }
    }

    private static string GetIconEmoji(MessageBoxType type)
    {
        switch (type)
        {
            case MessageBoxType.Info: return "ℹ️";
            case MessageBoxType.Warning: return "⚠️";
            case MessageBoxType.Error: return "❌";
            case MessageBoxType.Question: return "❓";
            default: return "💬";
        }
    }
}