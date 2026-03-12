////using Avalonia;
////using Avalonia.Controls;
////using Avalonia.Controls.ApplicationLifetimes;
////using Avalonia.Input;
////using Avalonia.Layout;
////using Avalonia.Media;
////using Avalonia.Styling;
////using Avalonia.Threading;
////using System;
////using System.Linq;
////using System.Threading.Tasks;

////// ENUM ТИПОВ СООБЩЕНИЙ
////public enum MessageBoxType
////{
////    Info,
////    Warning,
////    Error,
////    Question
////}

////// ENUM КНОПОК
////public enum MessageBoxButton
////{
////    OK,
////    OKCancel,
////    YesNo,
////    YesNoCancel
////}

////// ENUM РЕЗУЛЬТАТОВ
////public enum MessageBoxResult
////{
////    None,
////    OK,
////    Cancel,
////    Yes,
////    No
////}



////// КЛАСС MESSAGEBOX
////public static class MessageBox
////{
////    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

////    // Простая версия (owner - опциональный параметр)
////    public static async Task Show(string message, string title = "", Window owner = null)
////    {
////        await ShowInternal(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
////    }

////    // Полная версия (owner - опциональный параметр)
////    public static async Task<MessageBoxResult> Show(string message, string title,
////                                                     MessageBoxButton buttons,
////                                                     MessageBoxType type = MessageBoxType.Info,
////                                                     Window owner = null)
////    {
////        return await ShowInternal(message, title, buttons, type, owner);
////    }

////    // ========== ОСНОВНОЙ ВНУТРЕННИЙ МЕТОД ==========
////    private static async Task<MessageBoxResult> ShowInternal(string message, string title,
////                                                             MessageBoxButton buttons,
////                                                             MessageBoxType type,
////                                                             Window explicitOwner)
////    {
////        if (Application.Current?.ApplicationLifetime is
////            IClassicDesktopStyleApplicationLifetime desktop)
////        {
////            var tcs = new TaskCompletionSource<MessageBoxResult>();

////            // УМНАЯ ЛОГИКА ОПРЕДЕЛЕНИЯ РОДИТЕЛЬСКОГО ОКНА
////            Window ownerWindow = null;

////            // Приоритет 1: Явно указанное окно
////            if (explicitOwner != null && explicitOwner.IsVisible)
////            {
////                ownerWindow = explicitOwner;
////            }
////            // Приоритет 2: Активное окно приложения (старая логика)
////            else if (desktop.Windows.Count > 0)
////            {
////                ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ??
////                              desktop.Windows.FirstOrDefault(w => w.IsVisible);
////            }
////            // Приоритет 3: Главное окно
////            else if (desktop.MainWindow != null && desktop.MainWindow.IsVisible)
////            {
////                ownerWindow = desktop.MainWindow;
////            }

////            // СОЗДАЕМ ОСНОВНОЕ ОКНО
////            var mainWindow = new Window
////            {
////                Title = "",
////                MinWidth = 420,
////                MinHeight = 220,
////                MaxWidth = 800,
////                MaxHeight = 600,
////                WindowStartupLocation = ownerWindow != null
////                    ? WindowStartupLocation.CenterOwner
////                    : WindowStartupLocation.CenterScreen,
////                CanResize = true,
////                CanMinimize = false,
////                CanMaximize = false,
////                ShowInTaskbar = false,
////                SystemDecorations = SystemDecorations.None,
////                Topmost = ownerWindow == null, // Только поверх других окон, если нет родителя
////                SizeToContent = SizeToContent.WidthAndHeight,
////                Background = Brushes.Transparent
////            };

////            // ГЛАВНЫЙ КОНТЕЙНЕР
////            var mainContainer = new Grid();

////            // ========== ОСНОВНАЯ ПАНЕЛЬ С ВЫРАЗИТЕЛЬНОЙ РАМКОЙ ==========
////            var mainBorder = new Border
////            {
////                Background = Brushes.White,
////                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)), // Яркая синяя рамка
////                BorderThickness = new Thickness(3),
////                CornerRadius = new CornerRadius(5),
////                ZIndex = 2
////            };

////            // ========== СИНЯЯ ПАНЕЛЬ ВВЕРХУ ==========
////            var blueHeader = new Border
////            {
////                Height = 30,
////                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), // #FF007ACC
////                CornerRadius = new CornerRadius(5, 5, 0, 0),
////                HorizontalAlignment = HorizontalAlignment.Stretch,
////                VerticalAlignment = VerticalAlignment.Top,
////                ZIndex = 3,
////                Child = new Grid
////                {
////                    VerticalAlignment = VerticalAlignment.Stretch,
////                    Children =
////                    {
////                        // ЗАГОЛОВОК
////                        new TextBlock
////                        {
////                            Text = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
////                            Foreground = Brushes.White,
////                            FontSize = 14,
////                            FontWeight = FontWeight.Bold,
////                            VerticalAlignment = VerticalAlignment.Center,
////                            HorizontalAlignment = HorizontalAlignment.Left,
////                            Margin = new Thickness(15, 0, 0, 0)
////                        },
////                        // КНОПКА ЗАКРЫТИЯ
////                        new Button
////                        {
////                            Content = "✕",
////                            Width = 26,
////                            Height = 26,
////                            HorizontalAlignment = HorizontalAlignment.Right,
////                            VerticalAlignment = VerticalAlignment.Center,
////                            Margin = new Thickness(0, 0, 8, 0),
////                            FontSize = 14,
////                            FontWeight = FontWeight.Bold,
////                            Background = Brushes.Transparent,
////                            BorderThickness = new Thickness(0),
////                            Foreground = Brushes.White,
////                            Cursor = new Cursor(StandardCursorType.Hand)
////                        }
////                    }
////                }
////            };

////            // ========== ОСНОВНОЙ КОНТЕНТ ==========
////            var contentGrid = new Grid
////            {
////                Margin = new Thickness(25, 45, 25, 25)
////            };

////            var contentStack = new StackPanel
////            {
////                Spacing = 25,
////                VerticalAlignment = VerticalAlignment.Center,
////                HorizontalAlignment = HorizontalAlignment.Center
////            };

////            // СООБЩЕНИЕ И ИКОНКА
////            var messageStack = new StackPanel
////            {
////                Orientation = Orientation.Horizontal,
////                Spacing = 20,
////                HorizontalAlignment = HorizontalAlignment.Center
////            };

////            var iconText = new TextBlock
////            {
////                Text = GetIconEmoji(type),
////                FontSize = 32,
////                VerticalAlignment = VerticalAlignment.Center,
////                Foreground = GetIconColor(type)
////            };

////            var messageText = new TextBlock
////            {
////                Text = message,
////                TextWrapping = TextWrapping.Wrap,
////                FontSize = 14,
////                VerticalAlignment = VerticalAlignment.Center,
////                MaxWidth = 500,
////                MinWidth = 220,
////                Foreground = Brushes.Black
////            };

////            messageStack.Children.Add(iconText);
////            messageStack.Children.Add(messageText);

////            // КНОПКИ
////            var buttonStack = new StackPanel
////            {
////                Orientation = Orientation.Horizontal,
////                HorizontalAlignment = HorizontalAlignment.Center,
////                Spacing = 15
////            };

////            // СОЗДАЕМ КНОПКИ В ЗАВИСИМОСТИ ОТ ТИПА
////            switch (buttons)
////            {
////                case MessageBoxButton.OK:
////                    var okButton = CreateNeutralButton("OK", MessageBoxResult.OK, true, mainWindow, tcs);
////                    buttonStack.Children.Add(okButton);
////                    break;

////                case MessageBoxButton.OKCancel:
////                    var okBtn = CreateNeutralButton("OK", MessageBoxResult.OK, true, mainWindow, tcs);
////                    var cancelBtn = CreateNeutralButton("Отмена", MessageBoxResult.Cancel, false, mainWindow, tcs);
////                    buttonStack.Children.Add(okBtn);
////                    buttonStack.Children.Add(cancelBtn);
////                    break;

////                case MessageBoxButton.YesNo:
////                    var yesBtn = CreateNeutralButton("Да", MessageBoxResult.Yes, true, mainWindow, tcs);
////                    var noBtn = CreateNeutralButton("Нет", MessageBoxResult.No, false, mainWindow, tcs);
////                    buttonStack.Children.Add(yesBtn);
////                    buttonStack.Children.Add(noBtn);
////                    break;

////                case MessageBoxButton.YesNoCancel:
////                    var yesButton = CreateNeutralButton("Да", MessageBoxResult.Yes, true, mainWindow, tcs);
////                    var noButton = CreateNeutralButton("Нет", MessageBoxResult.No, false, mainWindow, tcs);
////                    var cancelButton = CreateNeutralButton("Отмена", MessageBoxResult.Cancel, false, mainWindow, tcs);
////                    buttonStack.Children.Add(yesButton);
////                    buttonStack.Children.Add(noButton);
////                    buttonStack.Children.Add(cancelButton);
////                    break;
////            }

////            // СБОРКА ИНТЕРФЕЙСА
////            contentStack.Children.Add(messageStack);
////            contentStack.Children.Add(buttonStack);
////            contentGrid.Children.Add(contentStack);

////            // ========== ВНУТРЕННЯЯ ПАНЕЛЬ ==========
////            var innerBorder = new Border
////            {
////                Background = Brushes.White,
////                BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
////                BorderThickness = new Thickness(1),
////                CornerRadius = new CornerRadius(3),
////                Margin = new Thickness(2),
////                Child = new Grid
////                {
////                    Children =
////                    {
////                        contentGrid,
////                        blueHeader
////                    }
////                }
////            };

////            mainBorder.Child = innerBorder;
////            mainContainer.Children.Add(mainBorder);
////            mainWindow.Content = mainContainer;

////            // ОБРАБОТЧИКИ СОБЫТИЙ
////            mainWindow.Closed += (s, e) =>
////            {
////                if (!tcs.Task.IsCompleted)
////                    tcs.TrySetResult(MessageBoxResult.None);
////            };

////            // ОБРАБОТЧИК ДЛЯ КНОПКИ ЗАКРЫТИЯ
////            if (blueHeader.Child is Grid headerGrid)
////            {
////                foreach (var child in headerGrid.Children)
////                {
////                    if (child is Button closeButton && closeButton.Content as string == "✕")
////                    {
////                        closeButton.Click += (s, e) =>
////                        {
////                            tcs.TrySetResult(MessageBoxResult.Cancel);
////                            mainWindow.Close();
////                        };

////                        closeButton.PointerEntered += (s, e) =>
////                        {
////                            closeButton.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
////                        };

////                        closeButton.PointerExited += (s, e) =>
////                        {
////                            closeButton.Background = Brushes.Transparent;
////                        };
////                    }
////                }
////            }

////            // ОБРАБОТКА КЛАВИШ
////            mainWindow.KeyDown += (s, e) =>
////            {
////                if (e.Key == Avalonia.Input.Key.Escape)
////                {
////                    e.Handled = true;
////                    tcs.TrySetResult(MessageBoxResult.Cancel);
////                    mainWindow.Close();
////                }
////            };

////            // ========== ПОКАЗ ОКНА ==========
////            if (ownerWindow != null)
////            {
////                // Модальное окно с родителем
////                await mainWindow.ShowDialog(ownerWindow);
////            }
////            else
////            {
////                // Без родителя (старая логика)
////                mainWindow.Show();
////            }

////            return await tcs.Task;
////        }

////        return MessageBoxResult.None;
////    }

////    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

////    private static Button CreateNeutralButton(string content, MessageBoxResult buttonResult,
////                                            bool isDefault, Window dialog,
////                                            TaskCompletionSource<MessageBoxResult> tcs)
////    {
////        var normalBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
////        var hoverBackground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
////        var pressedBackground = new SolidColorBrush(Color.FromRgb(210, 210, 210));
////        var borderColor = new SolidColorBrush(Color.FromRgb(180, 180, 180));

////        var button = new Button
////        {
////            Content = new TextBlock
////            {
////                Text = content,
////                FontSize = 13,
////                FontWeight = FontWeight.Medium,
////                HorizontalAlignment = HorizontalAlignment.Center,
////                VerticalAlignment = VerticalAlignment.Center,
////                Foreground = Brushes.Black
////            },
////            MinWidth = 90,
////            Height = 35,
////            HorizontalAlignment = HorizontalAlignment.Center,
////            Background = normalBackground,
////            BorderBrush = borderColor,
////            BorderThickness = new Thickness(1),
////            Cursor = new Cursor(StandardCursorType.Hand),
////            CornerRadius = new CornerRadius(3),
////            Padding = new Thickness(20, 0, 20, 0)
////        };

////        // ЭФФЕКТ ПРИ НАВЕДЕНИИ
////        button.PointerEntered += (s, e) =>
////        {
////            button.Background = hoverBackground;
////            button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
////        };

////        button.PointerExited += (s, e) =>
////        {
////            button.Background = normalBackground;
////            button.BorderBrush = borderColor;
////        };

////        button.PointerPressed += (s, e) =>
////        {
////            button.Background = pressedBackground;
////            button.BorderBrush = new SolidColorBrush(Color.FromRgb(140, 140, 140));
////        };

////        button.PointerReleased += (s, e) =>
////        {
////            button.Background = hoverBackground;
////            button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
////        };

////        button.Click += (s, e) =>
////        {
////            tcs.TrySetResult(buttonResult);
////            dialog.Close();
////        };

////        // УСТАНАВЛИВАЕМ ФОКУС НА КНОПКУ ПО УМОЛЧАНИЮ
////        if (isDefault)
////        {
////            dialog.Opened += (s, e) =>
////            {
////                Dispatcher.UIThread.Post(() =>
////                {
////                    button.Focus();
////                }, DispatcherPriority.Input);
////            };
////        }

////        // ОБРАБОТКА ENTER
////        if (isDefault)
////        {
////            dialog.KeyDown += (s, e) =>
////            {
////                if (e.Key == Avalonia.Input.Key.Enter)
////                {
////                    e.Handled = true;
////                    tcs.TrySetResult(buttonResult);
////                    dialog.Close();
////                }
////            };
////        }

////        return button;
////    }

////    private static IBrush GetIconColor(MessageBoxType type)
////    {
////        switch (type)
////        {
////            case MessageBoxType.Info:
////                return new SolidColorBrush(Color.FromRgb(0, 122, 204));
////            case MessageBoxType.Warning:
////                return new SolidColorBrush(Color.FromRgb(255, 140, 0));
////            case MessageBoxType.Error:
////                return new SolidColorBrush(Color.FromRgb(220, 53, 69));
////            case MessageBoxType.Question:
////                return new SolidColorBrush(Color.FromRgb(40, 167, 69));
////            default:
////                return new SolidColorBrush(Color.FromRgb(0, 122, 204));
////        }
////    }

////    private static string GetDefaultTitle(MessageBoxType type)
////    {
////        switch (type)
////        {
////            case MessageBoxType.Info: return "Информация";
////            case MessageBoxType.Warning: return "Предупреждение";
////            case MessageBoxType.Error: return "Ошибка";
////            case MessageBoxType.Question: return "Вопрос";
////            default: return "Сообщение";
////        }
////    }

////    private static string GetIconEmoji(MessageBoxType type)
////    {
////        switch (type)
////        {
////            case MessageBoxType.Info: return "ℹ️";
////            case MessageBoxType.Warning: return "⚠️";
////            case MessageBoxType.Error: return "❌";
////            case MessageBoxType.Question: return "❓";
////            default: return "💬";
////        }
////    }
////}

//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Controls.ApplicationLifetimes;
//using Avalonia.Input;
//using Avalonia.Interactivity;
//using Avalonia.Layout;
//using Avalonia.Media;
//using Avalonia.Threading;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//// ENUM ТИПОВ СООБЩЕНИЙ
//public enum MessageBoxType
//{
//    Info,
//    Warning,
//    Error,
//    Question
//}

//// ENUM КНОПОК
//public enum MessageBoxButton
//{
//    OK,
//    OKCancel,
//    YesNo,
//    YesNoCancel
//}

//// ENUM РЕЗУЛЬТАТОВ
//public enum MessageBoxResult
//{
//    None,
//    OK,
//    Cancel,
//    Yes,
//    No
//}

//// КЛАСС MESSAGEBOX
//public static class MessageBox
//{
//    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

//    // Простая версия (owner - обязательный параметр для Linux)
//    public static async Task Show(string message, string title = "", Window owner = null)
//    {
//        await ShowInternal(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
//    }

//    // Полная версия (owner - обязательный параметр для Linux)
//    public static async Task<MessageBoxResult> Show(string message, string title,
//                                                     MessageBoxButton buttons,
//                                                     MessageBoxType type = MessageBoxType.Info,
//                                                     Window owner = null)
//    {
//        return await ShowInternal(message, title, buttons, type, owner);
//    }

//    // ========== ОСНОВНОЙ ВНУТРЕННИЙ МЕТОД ==========
//    private static async Task<MessageBoxResult> ShowInternal(string message, string title,
//                                                             MessageBoxButton buttons,
//                                                             MessageBoxType type,
//                                                             Window explicitOwner)
//    {
//        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
//        {
//            return MessageBoxResult.None;
//        }

//        var tcs = new TaskCompletionSource<MessageBoxResult>();

//        // УМНАЯ ЛОГИКА ОПРЕДЕЛЕНИЯ РОДИТЕЛЬСКОГО ОКНА
//        Window ownerWindow = null;

//        // Приоритет 1: Явно указанное окно
//        if (explicitOwner != null && explicitOwner.IsVisible)
//        {
//            ownerWindow = explicitOwner;
//        }
//        // Приоритет 2: Активное окно приложения
//        else if (desktop.Windows.Count > 0)
//        {
//            ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ??
//                          desktop.Windows.FirstOrDefault(w => w.IsVisible);
//        }
//        // Приоритет 3: Главное окно
//        else if (desktop.MainWindow != null && desktop.MainWindow.IsVisible)
//        {
//            ownerWindow = desktop.MainWindow;
//        }

//        // СОЗДАЕМ ОСНОВНОЕ ОКНО
//        var mainWindow = new Window
//        {
//            Title = "",
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
//            Topmost = false,
//            SizeToContent = SizeToContent.WidthAndHeight,
//            Background = Brushes.Transparent
//        };

//        // ГЛАВНЫЙ КОНТЕЙНЕР
//        var mainContainer = new Grid();

//        // ========== ОСНОВНАЯ ПАНЕЛЬ С ВЫРАЗИТЕЛЬНОЙ РАМКОЙ ==========
//        var mainBorder = new Border
//        {
//            Background = Brushes.White,
//            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
//            BorderThickness = new Thickness(3),
//            CornerRadius = new CornerRadius(5),
//            ZIndex = 2
//        };

//        // ========== СИНЯЯ ПАНЕЛЬ ВВЕРХУ ==========
//        var blueHeader = new Border
//        {
//            Height = 30,
//            Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
//            CornerRadius = new CornerRadius(5, 5, 0, 0),
//            HorizontalAlignment = HorizontalAlignment.Stretch,
//            VerticalAlignment = VerticalAlignment.Top,
//            ZIndex = 3,
//            Child = new Grid
//            {
//                VerticalAlignment = VerticalAlignment.Stretch,
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
//                        Name = "CloseButton",
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
//                        Cursor = new Cursor(StandardCursorType.Hand)
//                    }
//                }
//            }
//        };

//        // ========== ОСНОВНОЙ КОНТЕНТ ==========
//        var contentGrid = new Grid
//        {
//            Margin = new Thickness(25, 45, 25, 25)
//        };

//        var contentStack = new StackPanel
//        {
//            Spacing = 25,
//            VerticalAlignment = VerticalAlignment.Center,
//            HorizontalAlignment = HorizontalAlignment.Center
//        };

//        // СООБЩЕНИЕ И ИКОНКА
//        var messageStack = new StackPanel
//        {
//            Orientation = Orientation.Horizontal,
//            Spacing = 20,
//            HorizontalAlignment = HorizontalAlignment.Center
//        };

//        var iconText = new TextBlock
//        {
//            Text = GetIconEmoji(type),
//            FontSize = 32,
//            VerticalAlignment = VerticalAlignment.Center,
//            Foreground = GetIconColor(type)
//        };

//        var messageText = new TextBlock
//        {
//            Text = message,
//            TextWrapping = TextWrapping.Wrap,
//            FontSize = 14,
//            VerticalAlignment = VerticalAlignment.Center,
//            MaxWidth = 500,
//            MinWidth = 220,
//            Foreground = Brushes.Black
//        };

//        messageStack.Children.Add(iconText);
//        messageStack.Children.Add(messageText);

//        // КНОПКИ
//        var buttonStack = new StackPanel
//        {
//            Orientation = Orientation.Horizontal,
//            HorizontalAlignment = HorizontalAlignment.Center,
//            Spacing = 15,
//            Name = "ButtonStack"
//        };

//        Button defaultButton = null;

//        // СОЗДАЕМ КНОПКИ В ЗАВИСИМОСТИ ОТ ТИПА
//        switch (buttons)
//        {
//            case MessageBoxButton.OK:
//                defaultButton = CreateNeutralButton("OK", MessageBoxResult.OK, true, mainWindow, tcs);
//                buttonStack.Children.Add(defaultButton);
//                break;

//            case MessageBoxButton.OKCancel:
//                var okBtn = CreateNeutralButton("OK", MessageBoxResult.OK, true, mainWindow, tcs);
//                var cancelBtn = CreateNeutralButton("Отмена", MessageBoxResult.Cancel, false, mainWindow, tcs);
//                buttonStack.Children.Add(okBtn);
//                buttonStack.Children.Add(cancelBtn);
//                defaultButton = okBtn;
//                break;

//            case MessageBoxButton.YesNo:
//                var yesBtn = CreateNeutralButton("Да", MessageBoxResult.Yes, true, mainWindow, tcs);
//                var noBtn = CreateNeutralButton("Нет", MessageBoxResult.No, false, mainWindow, tcs);
//                buttonStack.Children.Add(yesBtn);
//                buttonStack.Children.Add(noBtn);
//                defaultButton = yesBtn;
//                break;

//            case MessageBoxButton.YesNoCancel:
//                var yesButton = CreateNeutralButton("Да", MessageBoxResult.Yes, true, mainWindow, tcs);
//                var noButton = CreateNeutralButton("Нет", MessageBoxResult.No, false, mainWindow, tcs);
//                var cancelButton = CreateNeutralButton("Отмена", MessageBoxResult.Cancel, false, mainWindow, tcs);
//                buttonStack.Children.Add(yesButton);
//                buttonStack.Children.Add(noButton);
//                buttonStack.Children.Add(cancelButton);
//                defaultButton = yesButton;
//                break;
//        }

//        // СБОРКА ИНТЕРФЕЙСА
//        contentStack.Children.Add(messageStack);
//        contentStack.Children.Add(buttonStack);
//        contentGrid.Children.Add(contentStack);

//        // ========== ВНУТРЕННЯЯ ПАНЕЛЬ ==========
//        var innerBorder = new Border
//        {
//            Background = Brushes.White,
//            BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
//            BorderThickness = new Thickness(1),
//            CornerRadius = new CornerRadius(3),
//            Margin = new Thickness(2),
//            Child = new Grid
//            {
//                Children =
//                {
//                    contentGrid,
//                    blueHeader
//                }
//            }
//        };

//        mainBorder.Child = innerBorder;
//        mainContainer.Children.Add(mainBorder);
//        mainWindow.Content = mainContainer;

//        // ========== УПРОЩЁННАЯ УСТАНОВКА ФОКУСА ==========

//        // Метод для установки фокуса
//        async Task TryFocusOnButton(Button button)
//        {
//            for (int attempt = 0; attempt < 5; attempt++) // 5 попыток
//            {
//                await Dispatcher.UIThread.InvokeAsync(() =>
//                {
//                    // Активируем окно
//                    mainWindow.Activate();
//                    // mainWindow.Focus(); // Обычно не нужно, если используется Activate

//                    // Устанавливаем фокус на кнопку
//                    if (button != null)
//                    {
//                        button.Focus();
//                        Console.WriteLine($"✓ Попытка фокуса на кнопке {button.Content} - {attempt + 1}");
//                    }
//                });

//                // Небольшая задержка для оконного менеджера
//                await Task.Delay(50);
//            }
//        }

//        // Обработка открытия окна
//        mainWindow.Opened += async (s, e) =>
//        {
//            Console.WriteLine("✓ Окно MessageBox открыто");

//            // Делаем окно поверх всех для гарантии фокуса
//            mainWindow.Topmost = true;

//            // Небольшая задержка для стабилизации
//            await Task.Delay(100);

//            if (defaultButton != null)
//            {
//                await TryFocusOnButton(defaultButton);
//            }
//            else if (buttonStack.Children.Count > 0 && buttonStack.Children[0] is Button firstButton)
//            {
//                await TryFocusOnButton(firstButton);
//            }

//            // Возвращаем обычный режим после установки фокуса
//            mainWindow.Topmost = false;
//        };

//        // Обработка активации окна
//        mainWindow.Activated += (s, e) =>
//        {
//            Console.WriteLine("✓ MessageBox активирован");

//            Dispatcher.UIThread.Post(async () =>
//            {
//                await Task.Delay(30);

//                // Делаем окно поверх всех для гарантии фокуса
//                mainWindow.Topmost = true;

//                // Проверяем, есть ли фокус на кнопке, если нет - пробуем установить
//                var focusedElement = KeyboardNavigation.GetTabOnceActiveElement(mainWindow); // Это публичный API
//                if (focusedElement == null && defaultButton != null)
//                {
//                    defaultButton.Focus();
//                }

//                // Возвращаем обычный режим
//                mainWindow.Topmost = false;
//            }, DispatcherPriority.Background);
//        };

//        // Обработка деактивации (потеря фокуса)
//        mainWindow.Deactivated += (s, e) =>
//        {
//            Console.WriteLine("⚠ MessageBox потерял фокус, пробуем вернуть...");

//            Dispatcher.UIThread.Post(async () =>
//            {
//                await Task.Delay(50);

//                // Делаем окно поверх всех для гарантии возврата фокуса
//                mainWindow.Topmost = true;
//                mainWindow.Activate();

//                if (defaultButton != null)
//                {
//                    defaultButton.Focus();
//                }

//                // Возвращаем обычный режим
//                mainWindow.Topmost = false;
//            }, DispatcherPriority.Background);
//        };

//        // ОБРАБОЧИКИ СОБЫТИЙ
//        mainWindow.Closed += (s, e) =>
//        {
//            if (!tcs.Task.IsCompleted)
//                tcs.TrySetResult(MessageBoxResult.None);

//            Console.WriteLine("✕ MessageBox закрыт");

//            // Возвращаем фокус родительскому окну
//            if (ownerWindow != null)
//            {
//                Dispatcher.UIThread.Post(async () =>
//                {
//                    await Task.Delay(50);
//                    ownerWindow.Activate();
//                    ownerWindow.Focus();
//                }, DispatcherPriority.Background);
//            }
//        };

//        // ОБРАБОЧИК ДЛЯ КНОПКИ ЗАКРЫТИЯ
//        if (blueHeader.Child is Grid headerGrid)
//        {
//            foreach (var child in headerGrid.Children)
//            {
//                if (child is Button closeButton && closeButton.Content as string == "✕")
//                {
//                    closeButton.Click += (s, e) =>
//                    {
//                        tcs.TrySetResult(MessageBoxResult.Cancel);
//                        mainWindow.Close();
//                    };

//                    closeButton.PointerEntered += (s, e) =>
//                    {
//                        closeButton.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
//                    };

//                    closeButton.PointerExited += (s, e) =>
//                    {
//                        closeButton.Background = Brushes.Transparent;
//                    };
//                }
//            }
//        }

//        // ОБРАБОТКА КЛАВИШ
//        mainWindow.KeyDown += (s, e) =>
//        {
//            if (e.Key == Key.Escape)
//            {
//                e.Handled = true;
//                tcs.TrySetResult(MessageBoxResult.Cancel);
//                mainWindow.Close();
//                return;
//            }

//            if (e.Key == Key.Enter)
//            {
//                e.Handled = true;

//                // Попробуем использовать стандартное поведение TabNavigation или активную кнопку
//                var focused = KeyboardNavigation.GetTabOnceActiveElement(mainWindow);

//                if (focused is Button focusedButton && buttonStack.Children.Contains(focusedButton))
//                {
//                    focusedButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
//                }
//                else if (defaultButton != null)
//                {
//                    defaultButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
//                }
//                else if (buttonStack.Children.Count > 0 && buttonStack.Children[0] is Button firstBtn)
//                {
//                    firstBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
//                }
//            }
//        };

//        // ========== ПОКАЗ ОКНА ==========
//        try
//        {
//            if (ownerWindow != null)
//            {
//                // Убедимся, что владелец активен перед показом дочернего окна
//                ownerWindow.Activate();
//                await Task.Delay(20); // Небольшая задержка

//                await mainWindow.ShowDialog(ownerWindow); // Используем ShowDialog
//            }
//            else
//            {
//                mainWindow.Show();

//                await Task.Delay(100);
//                mainWindow.Topmost = true;
//                mainWindow.Activate();

//                if (defaultButton != null)
//                {
//                    defaultButton.Focus();
//                }

//                mainWindow.Topmost = false;
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"✗ Ошибка при показе MessageBox: {ex.Message}");
//            return MessageBoxResult.None;
//        }

//        return await tcs.Task;
//    }

//    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

//    private static Button CreateNeutralButton(string content, MessageBoxResult buttonResult,
//                                            bool isDefault, Window dialog,
//                                            TaskCompletionSource<MessageBoxResult> tcs)
//    {
//        var normalBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
//        var hoverBackground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
//        var pressedBackground = new SolidColorBrush(Color.FromRgb(210, 210, 210));
//        var borderColor = new SolidColorBrush(Color.FromRgb(180, 180, 180));

//        var button = new Button
//        {
//            Content = new TextBlock
//            {
//                Text = content,
//                FontSize = 13,
//                FontWeight = FontWeight.Medium,
//                HorizontalAlignment = HorizontalAlignment.Center,
//                VerticalAlignment = VerticalAlignment.Center,
//                Foreground = Brushes.Black
//            },
//            MinWidth = 90,
//            Height = 35,
//            HorizontalAlignment = HorizontalAlignment.Center,
//            Background = normalBackground,
//            BorderBrush = borderColor,
//            BorderThickness = new Thickness(1),
//            Cursor = new Cursor(StandardCursorType.Hand),
//            CornerRadius = new CornerRadius(3),
//            Padding = new Thickness(20, 0, 20, 0),
//            Tag = buttonResult
//        };

//        button.PointerEntered += (s, e) =>
//        {
//            button.Background = hoverBackground;
//            button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
//        };

//        button.PointerExited += (s, e) =>
//        {
//            button.Background = normalBackground;
//            button.BorderBrush = borderColor;
//        };

//        button.PointerPressed += (s, e) =>
//        {
//            button.Background = pressedBackground;
//            button.BorderBrush = new SolidColorBrush(Color.FromRgb(140, 140, 140));
//        };

//        button.PointerReleased += (s, e) =>
//        {
//            button.Background = hoverBackground;
//            button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
//        };

//        button.Click += (s, e) =>
//        {
//            tcs.TrySetResult(buttonResult);
//            dialog.Close();
//        };

//        return button;
//    }

//    private static IBrush GetIconColor(MessageBoxType type)
//    {
//        return type switch
//        {
//            MessageBoxType.Info => new SolidColorBrush(Color.FromRgb(0, 122, 204)),
//            MessageBoxType.Warning => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
//            MessageBoxType.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
//            MessageBoxType.Question => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
//            _ => new SolidColorBrush(Color.FromRgb(0, 122, 204))
//        };
//    }

//    private static string GetDefaultTitle(MessageBoxType type)
//    {
//        return type switch
//        {
//            MessageBoxType.Info => "Информация",
//            MessageBoxType.Warning => "Предупреждение",
//            MessageBoxType.Error => "Ошибка",
//            MessageBoxType.Question => "Вопрос",
//            _ => "Сообщение"
//        };
//    }

//    private static string GetIconEmoji(MessageBoxType type)
//    {
//        return type switch
//        {
//            MessageBoxType.Info => "ℹ️",
//            MessageBoxType.Warning => "⚠️",
//            MessageBoxType.Error => "❌",
//            MessageBoxType.Question => "❓",
//            _ => "💬"
//        };
//    }
//}

//// ВСПОМОГАТЕЛЬНЫЙ КЛАСС ДЛЯ ВЫЗОВА MESSAGEBOX С АКТИВАЦИЕЙ
//public static class MessageBoxHelper
//{
//    public static async Task ActivateWindow(Window window)
//    {
//        if (window == null) return;

//        await Dispatcher.UIThread.InvokeAsync(() =>
//        {
//            if (window.IsVisible)
//            {
//                // Попытка активировать окно
//                window.Activate();
//                window.Focus();

//                // Для Linux - трюк с Topmost
//                if (OperatingSystem.IsLinux())
//                {
//                    window.Topmost = true;
//                    window.Topmost = false;
//                }
//            }
//        }, DispatcherPriority.Render);

//        // Дайте оконному менеджеру время отреагировать
//        if (OperatingSystem.IsLinux())
//        {
//            await Task.Delay(100); // 100 мс для надежности
//        }
//        else
//        {
//            await Task.Delay(10); // Для Windows достаточно
//        }
//    }

//    // Простая версия
//    public static async Task Show(string message, string title = "", Window owner = null)
//    {
//        await MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
//        await ActivateWindow(owner);
//    }

//    // Полная версия с результатом
//    public static async Task<MessageBoxResult> Show(string message, string title,
//                                                     MessageBoxButton buttons,
//                                                     MessageBoxType type = MessageBoxType.Info,
//                                                     Window owner = null)
//    {
//        var result = await MessageBox.Show(message, title, buttons, type, owner);
//        await ActivateWindow(owner);
//        return result;
//    }

//    // Версия с принудительной активацией конкретного окна (если нужно активировать не owner)
//    public static async Task<MessageBoxResult> ShowAndActivate(string message, string title,
//                                                                MessageBoxButton buttons,
//                                                                MessageBoxType type,
//                                                                Window messageOwner,
//                                                                Window windowToActivate)
//    {
//        var result = await MessageBox.Show(message, title, buttons, type, messageOwner);
//        await ActivateWindow(windowToActivate);
//        return result;
//    }
//}

//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Controls.ApplicationLifetimes;
//using Avalonia.Input;
//using Avalonia.Interactivity;
//using Avalonia.Layout;
//using Avalonia.Media;
//using Avalonia.Threading;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//// ENUM ТИПОВ СООБЩЕНИЙ
//public enum MessageBoxType
//{
//    Info,
//    Warning,
//    Error,
//    Question
//}

//// ENUM КНОПОК
//public enum MessageBoxButton
//{
//    OK,
//    OKCancel,
//    YesNo,
//    YesNoCancel
//}

//// ENUM РЕЗУЛЬТАТОВ
//public enum MessageBoxResult
//{
//    None,
//    OK,
//    Cancel,
//    Yes,
//    No
//}

//// КЛАСС MESSAGEBOX
//public static class MessageBox
//{
//    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

//    public static async Task Show(string message, string title = "", Window owner = null)
//    {
//        await ShowInternal(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
//    }

//    public static async Task<MessageBoxResult> Show(string message, string title,
//                                                     MessageBoxButton buttons,
//                                                     MessageBoxType type = MessageBoxType.Info,
//                                                     Window owner = null)
//    {
//        return await ShowInternal(message, title, buttons, type, owner);
//    }

//    // ========== ОСНОВНОЙ ВНУТРЕННИЙ МЕТОД ==========
//    //private static async Task<MessageBoxResult> ShowInternal(string message, string title,
//    //                                                         MessageBoxButton buttons,
//    //                                                         MessageBoxType type,
//    //                                                         Window explicitOwner)
//    //{
//    //    if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
//    //    {
//    //        return MessageBoxResult.None;
//    //    }

//    //    var tcs = new TaskCompletionSource<MessageBoxResult>();

//    //    // ОПРЕДЕЛЕНИЕ РОДИТЕЛЬСКОГО ОКНА
//    //    Window ownerWindow = null;

//    //    if (explicitOwner != null && explicitOwner.IsVisible)
//    //    {
//    //        ownerWindow = explicitOwner;
//    //    }
//    //    else if (desktop.Windows.Count > 0)
//    //    {
//    //        ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ??
//    //                      desktop.Windows.FirstOrDefault(w => w.IsVisible);
//    //    }
//    //    else if (desktop.MainWindow != null && desktop.MainWindow.IsVisible)
//    //    {
//    //        ownerWindow = desktop.MainWindow;
//    //    }

//    //    // СОЗДАЕМ ОКНО СРАЗУ С TOPMOST = TRUE
//    //    var mainWindow = new Window
//    //    {
//    //        Title = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
//    //        MinWidth = 420,
//    //        MinHeight = 220,
//    //        MaxWidth = 800,
//    //        MaxHeight = 600,
//    //        WindowStartupLocation = ownerWindow != null
//    //            ? WindowStartupLocation.CenterOwner
//    //            : WindowStartupLocation.CenterScreen,
//    //        CanResize = false,
//    //        CanMinimize = false,
//    //        CanMaximize = false,
//    //        ShowInTaskbar = false,
//    //        SystemDecorations = SystemDecorations.None,
//    //        Topmost = true,  // ВАЖНО: сразу true
//    //        SizeToContent = SizeToContent.WidthAndHeight,
//    //        Background = Brushes.Transparent
//    //    };

//    //    // ========== СОЗДАНИЕ UI ==========

//    //    var mainBorder = new Border
//    //    {
//    //        Background = Brushes.White,
//    //        BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
//    //        BorderThickness = new Thickness(3),
//    //        CornerRadius = new CornerRadius(5)
//    //    };

//    //    var blueHeader = new Border
//    //    {
//    //        Height = 30,
//    //        Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
//    //        CornerRadius = new CornerRadius(5, 5, 0, 0),
//    //        HorizontalAlignment = HorizontalAlignment.Stretch,
//    //        VerticalAlignment = VerticalAlignment.Top,
//    //        Child = new Grid
//    //        {
//    //            Children =
//    //            {
//    //                new TextBlock
//    //                {
//    //                    Text = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
//    //                    Foreground = Brushes.White,
//    //                    FontSize = 14,
//    //                    FontWeight = FontWeight.Bold,
//    //                    VerticalAlignment = VerticalAlignment.Center,
//    //                    HorizontalAlignment = HorizontalAlignment.Left,
//    //                    Margin = new Thickness(15, 0, 0, 0)
//    //                },
//    //                new Button
//    //                {
//    //                    Content = "✕",
//    //                    Width = 26,
//    //                    Height = 26,
//    //                    HorizontalAlignment = HorizontalAlignment.Right,
//    //                    VerticalAlignment = VerticalAlignment.Center,
//    //                    Margin = new Thickness(0, 0, 8, 0),
//    //                    FontSize = 14,
//    //                    FontWeight = FontWeight.Bold,
//    //                    Background = Brushes.Transparent,
//    //                    BorderThickness = new Thickness(0),
//    //                    Foreground = Brushes.White,
//    //                    Cursor = new Cursor(StandardCursorType.Hand)
//    //                }
//    //            }
//    //        }
//    //    };

//    //    var messageStack = new StackPanel
//    //    {
//    //        Orientation = Orientation.Horizontal,
//    //        Spacing = 20,
//    //        HorizontalAlignment = HorizontalAlignment.Center
//    //    };

//    //    var iconText = new TextBlock
//    //    {
//    //        Text = GetIconEmoji(type),
//    //        FontSize = 32,
//    //        VerticalAlignment = VerticalAlignment.Center,
//    //        Foreground = GetIconColor(type)
//    //    };

//    //    var messageText = new TextBlock
//    //    {
//    //        Text = message,
//    //        TextWrapping = TextWrapping.Wrap,
//    //        FontSize = 14,
//    //        VerticalAlignment = VerticalAlignment.Center,
//    //        MaxWidth = 500,
//    //        MinWidth = 220,
//    //        Foreground = Brushes.Black
//    //    };

//    //    messageStack.Children.Add(iconText);
//    //    messageStack.Children.Add(messageText);

//    //    var buttonStack = new StackPanel
//    //    {
//    //        Orientation = Orientation.Horizontal,
//    //        HorizontalAlignment = HorizontalAlignment.Center,
//    //        Spacing = 15
//    //    };

//    //    Button defaultButton = null;

//    //    // СОЗДАНИЕ КНОПОК
//    //    switch (buttons)
//    //    {
//    //        case MessageBoxButton.OK:
//    //            defaultButton = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs);
//    //            buttonStack.Children.Add(defaultButton);
//    //            break;

//    //        case MessageBoxButton.OKCancel:
//    //            var okBtn = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs);
//    //            var cancelBtn = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs);
//    //            buttonStack.Children.Add(okBtn);
//    //            buttonStack.Children.Add(cancelBtn);
//    //            defaultButton = okBtn;
//    //            break;

//    //        case MessageBoxButton.YesNo:
//    //            var yesBtn = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs);
//    //            var noBtn = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs);
//    //            buttonStack.Children.Add(yesBtn);
//    //            buttonStack.Children.Add(noBtn);
//    //            defaultButton = yesBtn;
//    //            break;

//    //        case MessageBoxButton.YesNoCancel:
//    //            var yesButton = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs);
//    //            var noButton = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs);
//    //            var cancelButton = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs);
//    //            buttonStack.Children.Add(yesButton);
//    //            buttonStack.Children.Add(noButton);
//    //            buttonStack.Children.Add(cancelButton);
//    //            defaultButton = yesButton;
//    //            break;
//    //    }

//    //    var contentStack = new StackPanel
//    //    {
//    //        Spacing = 25,
//    //        VerticalAlignment = VerticalAlignment.Center,
//    //        HorizontalAlignment = HorizontalAlignment.Center
//    //    };

//    //    contentStack.Children.Add(messageStack);
//    //    contentStack.Children.Add(buttonStack);

//    //    var contentGrid = new Grid
//    //    {
//    //        Margin = new Thickness(25, 45, 25, 25),
//    //        Children = { contentStack }
//    //    };

//    //    var innerBorder = new Border
//    //    {
//    //        Background = Brushes.White,
//    //        BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
//    //        BorderThickness = new Thickness(1),
//    //        CornerRadius = new CornerRadius(3),
//    //        Margin = new Thickness(2),
//    //        Child = new Grid
//    //        {
//    //            Children = { contentGrid, blueHeader }
//    //        }
//    //    };

//    //    mainBorder.Child = innerBorder;
//    //    mainWindow.Content = mainBorder;

//    //    // ========== СОХРАНЯЕМ ССЫЛКУ НА КНОПКУ ==========
//    //    var capturedDefaultButton = defaultButton;

//    //    // ========== ЕДИНСТВЕННЫЙ ОБРАБОТЧИК ОТКРЫТИЯ ==========
//    //    mainWindow.Opened += async (s, e) =>
//    //    {
//    //        // ===== ГАРАНТИРОВАННЫЙ ФОКУС ДЛЯ LINUX =====
//    //        await Task.Delay(50); // Даём время оконному менеджеру

//    //        await Dispatcher.UIThread.InvokeAsync(async () =>
//    //        {
//    //            // 1. Активируем окно
//    //            mainWindow.Activate();
//    //            mainWindow.Focus();

//    //            // 2. Трюк с Topmost для Linux
//    //            mainWindow.Topmost = false;
//    //            mainWindow.Topmost = true;

//    //            // 3. Задержка для применения
//    //            await Task.Delay(100);

//    //            // 4. Фокус на кнопку
//    //            if (capturedDefaultButton != null)
//    //            {
//    //                capturedDefaultButton.Focus();
//    //            }

//    //            // 5. Финальная активация
//    //            mainWindow.Activate();

//    //        }, DispatcherPriority.Render);
//    //    };

//    //    // ========== ОБРАБОТЧИК ПОТЕРИ ФОКУСА ==========
//    //    mainWindow.Deactivated += async (s, e) =>
//    //    {           
//    //        // Если окно ещё видимо - возвращаем фокус
//    //        if (mainWindow.IsVisible)
//    //        {
//    //            await Dispatcher.UIThread.InvokeAsync(async () =>
//    //            {
//    //                await Task.Delay(50);

//    //                mainWindow.Topmost = false;
//    //                mainWindow.Topmost = true;
//    //                mainWindow.Activate();

//    //                if (capturedDefaultButton != null)
//    //                {
//    //                    capturedDefaultButton.Focus();
//    //                }

//    //            }, DispatcherPriority.Render);
//    //        }
//    //    };

//    //    // ========== ОБРАБОТЧИК ЗАКРЫТИЯ ==========
//    //    mainWindow.Closed += (s, e) =>
//    //    {
//    //        if (!tcs.Task.IsCompleted)
//    //            tcs.TrySetResult(MessageBoxResult.None);
//    //    };

//    //    // ========== КНОПКА ЗАКРЫТИЯ ==========
//    //    if (blueHeader.Child is Grid headerGrid)
//    //    {
//    //        foreach (var child in headerGrid.Children)
//    //        {
//    //            if (child is Button closeButton && closeButton.Content as string == "✕")
//    //            {
//    //                closeButton.Click += (s, e) =>
//    //                {
//    //                    tcs.TrySetResult(MessageBoxResult.Cancel);
//    //                    mainWindow.Close();
//    //                };

//    //                closeButton.PointerEntered += (s, e) =>
//    //                {
//    //                    closeButton.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
//    //                };

//    //                closeButton.PointerExited += (s, e) =>
//    //                {
//    //                    closeButton.Background = Brushes.Transparent;
//    //                };
//    //            }
//    //        }
//    //    }

//    //    // ========== ОБРАБОТКА КЛАВИШ ==========
//    //    mainWindow.KeyDown += (s, e) =>
//    //    {
//    //        if (e.Key == Key.Escape)
//    //        {
//    //            e.Handled = true;
//    //            tcs.TrySetResult(MessageBoxResult.Cancel);
//    //            mainWindow.Close();
//    //            return;
//    //        }

//    //        if (e.Key == Key.Enter)
//    //        {
//    //            e.Handled = true;
//    //            if (capturedDefaultButton != null)
//    //            {
//    //                tcs.TrySetResult((MessageBoxResult)capturedDefaultButton.Tag);
//    //                mainWindow.Close();
//    //            }
//    //        }
//    //    };

//    //    // ========== ПОКАЗ ОКНА ==========
//    //    try
//    //    {
//    //        if (ownerWindow != null)
//    //        {
//    //            await mainWindow.ShowDialog(ownerWindow);
//    //        }
//    //        else
//    //        {
//    //            mainWindow.Show();
//    //            // Ждём результата
//    //            await tcs.Task;
//    //        }
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        Console.WriteLine($"Ошибка MessageBox: {ex.Message}");
//    //        return MessageBoxResult.None;
//    //    }

//    //    return await tcs.Task;
//    //}

//    // ========== ОСНОВНОЙ ВНУТРЕННИЙ МЕТОД ==========
//    private static async Task<MessageBoxResult> ShowInternal(string message, string title,
//                                                             MessageBoxButton buttons,
//                                                             MessageBoxType type,
//                                                             Window explicitOwner)
//    {
//        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
//        {
//            return MessageBoxResult.None;
//        }

//        var tcs = new TaskCompletionSource<MessageBoxResult>();

//        // ========== ОПРЕДЕЛЕНИЕ РОДИТЕЛЬСКОГО ОКНА ==========
//        Window ownerWindow = null;
//        if (explicitOwner != null && explicitOwner.IsVisible)
//        {
//            ownerWindow = explicitOwner;
//        }
//        else if (desktop.Windows.Count > 0)
//        {
//            ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ??
//                          desktop.Windows.FirstOrDefault(w => w.IsVisible);
//        }
//        else if (desktop.MainWindow != null && desktop.MainWindow.IsVisible)
//        {
//            ownerWindow = desktop.MainWindow;
//        }

//        // ========== СОЗДАЕМ ОКНО ==========
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
//            Background = Brushes.Transparent
//        };

//        // ========== СОЗДАНИЕ UI ==========
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
//            {
//                new TextBlock
//                {
//                    Text = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
//                    Foreground = Brushes.White,
//                    FontSize = 14,
//                    FontWeight = FontWeight.Bold,
//                    VerticalAlignment = VerticalAlignment.Center,
//                    HorizontalAlignment = HorizontalAlignment.Left,
//                    Margin = new Thickness(15, 0, 0, 0)
//                },
//                new Button
//                {
//                    Content = "✕",
//                    Width = 26,
//                    Height = 26,
//                    HorizontalAlignment = HorizontalAlignment.Right,
//                    VerticalAlignment = VerticalAlignment.Center,
//                    Margin = new Thickness(0, 0, 8, 0),
//                    FontSize = 14,
//                    FontWeight = FontWeight.Bold,
//                    Background = Brushes.Transparent,
//                    BorderThickness = new Thickness(0),
//                    Foreground = Brushes.White,
//                    Cursor = new Cursor(StandardCursorType.Hand)
//                }
//            }
//            }
//        };

//        var messageStack = new StackPanel
//        {
//            Orientation = Orientation.Horizontal,
//            Spacing = 20,
//            HorizontalAlignment = HorizontalAlignment.Center
//        };

//        var iconText = new TextBlock
//        {
//            Text = GetIconEmoji(type),
//            FontSize = 32,
//            VerticalAlignment = VerticalAlignment.Center,
//            Foreground = GetIconColor(type)
//        };

//        var messageText = new TextBlock
//        {
//            Text = message,
//            TextWrapping = TextWrapping.Wrap,
//            FontSize = 14,
//            VerticalAlignment = VerticalAlignment.Center,
//            MaxWidth = 500,
//            MinWidth = 220,
//            Foreground = Brushes.Black
//        };

//        messageStack.Children.Add(iconText);
//        messageStack.Children.Add(messageText);

//        var buttonStack = new StackPanel
//        {
//            Orientation = Orientation.Horizontal,
//            HorizontalAlignment = HorizontalAlignment.Center,
//            Spacing = 15
//        };

//        Button defaultButton = null;

//        // ========== СОЗДАНИЕ КНОПОК ==========
//        switch (buttons)
//        {
//            case MessageBoxButton.OK:
//                defaultButton = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs);
//                buttonStack.Children.Add(defaultButton);
//                break;
//            case MessageBoxButton.OKCancel:
//                var okBtn = CreateButton("OK", MessageBoxResult.OK, mainWindow, tcs);
//                var cancelBtn = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs);
//                buttonStack.Children.Add(okBtn);
//                buttonStack.Children.Add(cancelBtn);
//                defaultButton = okBtn;
//                break;
//            case MessageBoxButton.YesNo:
//                var yesBtn = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs);
//                var noBtn = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs);
//                buttonStack.Children.Add(yesBtn);
//                buttonStack.Children.Add(noBtn);
//                defaultButton = yesBtn;
//                break;
//            case MessageBoxButton.YesNoCancel:
//                var yesButton = CreateButton("Да", MessageBoxResult.Yes, mainWindow, tcs);
//                var noButton = CreateButton("Нет", MessageBoxResult.No, mainWindow, tcs);
//                var cancelButton = CreateButton("Отмена", MessageBoxResult.Cancel, mainWindow, tcs);
//                buttonStack.Children.Add(yesButton);
//                buttonStack.Children.Add(noButton);
//                buttonStack.Children.Add(cancelButton);
//                defaultButton = yesButton;
//                break;
//        }

//        var contentStack = new StackPanel
//        {
//            Spacing = 25,
//            VerticalAlignment = VerticalAlignment.Center,
//            HorizontalAlignment = HorizontalAlignment.Center
//        };
//        contentStack.Children.Add(messageStack);
//        contentStack.Children.Add(buttonStack);

//        var contentGrid = new Grid
//        {
//            Margin = new Thickness(25, 45, 25, 25),
//            Children = { contentStack }
//        };

//        var innerBorder = new Border
//        {
//            Background = Brushes.White,
//            BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
//            BorderThickness = new Thickness(1),
//            CornerRadius = new CornerRadius(3),
//            Margin = new Thickness(2),
//            Child = new Grid
//            {
//                Children = { contentGrid, blueHeader }
//            }
//        };

//        mainBorder.Child = innerBorder;
//        mainWindow.Content = mainBorder;

//        // ========== СОХРАНЯЕМ ССЫЛКУ НА КНОПКУ ==========
//        var capturedDefaultButton = defaultButton;

//        // ========== ФЛАГ ДЛЯ ПРЕДОТВРАЩЕНИЯ ГОНОК ==========
//        bool isClosing = false;

//        // ========== ОБРАБОТЧИК ОТКРЫТИЯ (исправленный) ==========
//        EventHandler? openedHandler = null;
//        openedHandler = async (s, e) =>
//        {
//            // Отписываемся сразу, чтобы не сработал повторно
//            mainWindow.Opened -= openedHandler;

//            await SetFocusSafelyAsync();

//            async Task SetFocusSafelyAsync()
//            {
//                if (isClosing || !mainWindow.IsVisible) return;

//                try
//                {
//                    // Задержка ДО InvokeAsync, а не внутри!
//                    await Task.Delay(50);

//                    if (isClosing || !mainWindow.IsVisible) return;

//                    await Dispatcher.UIThread.InvokeAsync(() =>
//                    {
//                        if (isClosing || !mainWindow.IsVisible) return;

//                        mainWindow.Activate();
//                        mainWindow.Focus();

//                        // Платформенно-безопасное восстановление фокуса
//                        if (OperatingSystem.IsLinux())
//                        {
//                            mainWindow.Topmost = false;
//                            mainWindow.Topmost = true;
//                        }

//                        capturedDefaultButton?.Focus();
//                        mainWindow.Activate();
//                    }, DispatcherPriority.Render);
//                }
//                catch (ObjectDisposedException)
//                {
//                    // Окно уже закрыто — это нормально
//                }
//                catch (Exception ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[MessageBox.Opened] {ex.Message}");
//                }
//            }
//        };
//        mainWindow.Opened += openedHandler;

//        // ========== ОБРАБОТЧИК ПОТЕРИ ФОКУСА (исправленный) ==========
//        EventHandler? deactivatedHandler = null;

//        deactivatedHandler = (s, e) =>
//        {
//            // Fire-and-forget с явным игнорированием
//            _ = RestoreFocusSafelyAsync();

//            async Task RestoreFocusSafelyAsync()
//            {
//                if (isClosing || !mainWindow.IsVisible) return;

//                try
//                {
//                    // Задержка ДО InvokeAsync, а не внутри!
//                    await Task.Delay(50);

//                    if (isClosing || !mainWindow.IsVisible) return;

//                    await Dispatcher.UIThread.InvokeAsync(() =>
//                    {
//                        if (isClosing || !mainWindow.IsVisible) return;

//                        // Платформенно-безопасное восстановление фокуса
//                        if (OperatingSystem.IsLinux())
//                        {
//                            // На Linux Topmost-трюки могут конфликтовать с WM
//                            mainWindow.Activate();
//                            capturedDefaultButton?.Focus();
//                        }
//                        else
//                        {
//                            // На Windows можно использовать Topmost для гарантии
//                            mainWindow.Topmost = false;
//                            mainWindow.Topmost = true;
//                            mainWindow.Activate();
//                            capturedDefaultButton?.Focus();
//                        }
//                    }, DispatcherPriority.Render);
//                }
//                catch (ObjectDisposedException)
//                {
//                    // Окно уже закрыто — это нормально
//                }
//                catch (Exception ex) when (OperatingSystem.IsLinux())
//                {
//                    // На Linux оконный менеджер может отказать в активации — не критично
//                    System.Diagnostics.Debug.WriteLine($"[MessageBox.Deactivated] {ex.Message}");
//                }
//            }
//        };

//        mainWindow.Deactivated += deactivatedHandler;

//        // ========== ОБРАБОТЧИК ЗАКРЫТИЯ ==========
//        mainWindow.Closed += (s, e) =>
//        {
//            isClosing = true;  // ⚠️ Важно: ставим флаг ДО отписки

//            // Отписываемся от событий для предотвращения утечек
//            mainWindow.Deactivated -= deactivatedHandler;
//            mainWindow.Opened -= openedHandler;

//            if (!tcs.Task.IsCompleted)
//                tcs.TrySetResult(MessageBoxResult.None);
//        };

//        // ========== КНОПКА ЗАКРЫТИЯ ==========
//        if (blueHeader.Child is Grid headerGrid)
//        {
//            foreach (var child in headerGrid.Children)
//            {
//                if (child is Button closeButton && closeButton.Content as string == "✕")
//                {
//                    closeButton.Click += (s, e) =>
//                    {
//                        try
//                        {
//                            tcs.TrySetResult(MessageBoxResult.Cancel);
//                            if (mainWindow.IsVisible)
//                            {
//                                mainWindow.Close();
//                            }
//                        }
//                        catch (ObjectDisposedException)
//                        {
//                            System.Diagnostics.Debug.WriteLine("[CloseButton.Click] Window already disposed");
//                        }
//                    };
//                    closeButton.PointerEntered += (s, e) =>
//                    {
//                        closeButton.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
//                    };
//                    closeButton.PointerExited += (s, e) =>
//                    {
//                        closeButton.Background = Brushes.Transparent;
//                    };
//                }
//            }
//        }

//        // ========== ОБРАБОТКА КЛАВИШ ==========
//        EventHandler<KeyEventArgs>? keyDownHandler = null;
//        keyDownHandler = (s, e) =>
//        {
//            if (e.Key == Key.Escape)
//            {
//                e.Handled = true;
//                tcs.TrySetResult(MessageBoxResult.Cancel);
//                if (mainWindow.IsVisible)
//                {
//                    mainWindow.Close();
//                }
//                return;
//            }
//            if (e.Key == Key.Enter && !isClosing)
//            {
//                e.Handled = true;
//                if (capturedDefaultButton != null)
//                {
//                    tcs.TrySetResult((MessageBoxResult)capturedDefaultButton.Tag);
//                    if (mainWindow.IsVisible)
//                    {
//                        mainWindow.Close();
//                    }
//                }
//            }
//        };
//        mainWindow.KeyDown += keyDownHandler;

//        // ========== ПОКАЗ ОКНА ==========
//        try
//        {
//            if (ownerWindow != null)
//            {
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
//            Console.WriteLine($"Ошибка MessageBox: {ex.Message}");
//            return MessageBoxResult.None;
//        }

//        return await tcs.Task;
//    }

//    // ========== СОЗДАНИЕ КНОПКИ ==========
//    private static Button CreateButton(string content, MessageBoxResult buttonResult,
//                                       Window dialog, TaskCompletionSource<MessageBoxResult> tcs)
//    {
//        var normalBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
//        var hoverBackground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
//        var pressedBackground = new SolidColorBrush(Color.FromRgb(210, 210, 210));
//        var borderColor = new SolidColorBrush(Color.FromRgb(180, 180, 180));

//        var button = new Button
//        {
//            Content = new TextBlock
//            {
//                Text = content,
//                FontSize = 13,
//                FontWeight = FontWeight.Medium,
//                HorizontalAlignment = HorizontalAlignment.Center,
//                VerticalAlignment = VerticalAlignment.Center,
//                Foreground = Brushes.Black
//            },
//            MinWidth = 90,
//            Height = 35,
//            HorizontalAlignment = HorizontalAlignment.Center,
//            Background = normalBackground,
//            BorderBrush = borderColor,
//            BorderThickness = new Thickness(1),
//            Cursor = new Cursor(StandardCursorType.Hand),
//            CornerRadius = new CornerRadius(3),
//            Padding = new Thickness(20, 0, 20, 0),
//            Tag = buttonResult
//        };

//        button.PointerEntered += (s, e) =>
//        {
//            button.Background = hoverBackground;
//            button.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
//        };

//        button.PointerExited += (s, e) =>
//        {
//            button.Background = normalBackground;
//            button.BorderBrush = borderColor;
//        };

//        button.PointerPressed += (s, e) =>
//        {
//            button.Background = pressedBackground;
//        };

//        button.PointerReleased += (s, e) =>
//        {
//            button.Background = hoverBackground;
//        };

//        button.Click += (s, e) =>
//        {
//            tcs.TrySetResult(buttonResult);
//            dialog.Close();
//        };

//        return button;
//    }

//    private static IBrush GetIconColor(MessageBoxType type)
//    {
//        return type switch
//        {
//            MessageBoxType.Info => new SolidColorBrush(Color.FromRgb(0, 122, 204)),
//            MessageBoxType.Warning => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
//            MessageBoxType.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
//            MessageBoxType.Question => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
//            _ => new SolidColorBrush(Color.FromRgb(0, 122, 204))
//        };
//    }

//    private static string GetDefaultTitle(MessageBoxType type)
//    {
//        return type switch
//        {
//            MessageBoxType.Info => "Информация",
//            MessageBoxType.Warning => "Предупреждение",
//            MessageBoxType.Error => "Ошибка",
//            MessageBoxType.Question => "Вопрос",
//            _ => "Сообщение"
//        };
//    }

//    //private static string GetIconEmoji(MessageBoxType type)
//    //{
//    //    return type switch
//    //    {
//    //        MessageBoxType.Info => "ℹ️",
//    //        MessageBoxType.Warning => "⚠️",
//    //        MessageBoxType.Error => "❌",
//    //        MessageBoxType.Question => "❓",
//    //        _ => "💬"
//    //    };
//    //}

//    private static string GetIconEmoji(MessageBoxType type) => type switch
//    {
//        MessageBoxType.Info => "\u2139",  // ℹ
//        MessageBoxType.Warning => "\u26A0",  // ⚠
//        MessageBoxType.Error => "\u274C",  // ❌
//        MessageBoxType.Question => "\u2753",  // ❓
//        _ => "\u2022"   // • (BMP-символ вместо 💬)
//    };   
//}

//// ========== УПРОЩЁННЫЙ HELPER ==========
//public static class MessageBoxHelper
//{
//    public static async Task ActivateWindow(Window window)
//    {
//        if (window == null) return;

//        await Task.Delay(50);

//        await Dispatcher.UIThread.InvokeAsync(async () =>
//        {
//            window.Activate();
//            window.Focus();

//            if (OperatingSystem.IsLinux())
//            {
//                window.Topmost = false;
//                window.Topmost = true;
//            }

//            await Task.Delay(100);

//        }, DispatcherPriority.Render);
//    }

//    public static async Task Show(string message, string title = "", Window owner = null)
//    {
//        await MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxType.Info, owner);
//        await ActivateWindow(owner);
//    }

//    public static async Task<MessageBoxResult> Show(string message, string title,
//                                                     MessageBoxButton buttons,
//                                                     MessageBoxType type = MessageBoxType.Info,
//                                                     Window owner = null)
//    {
//        var result = await MessageBox.Show(message, title, buttons, type, owner);
//        await ActivateWindow(owner);
//        return result;
//    }
//}

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

    private static async Task<MessageBoxResult> ShowInternal(string message, string title,
                                                             MessageBoxButton buttons,
                                                             MessageBoxType type,
                                                             Window? explicitOwner)
    {
        await _showSemaphore.WaitAsync();

        try
        {
            lock (_showTimeLock)
            {
                var elapsed = DateTime.UtcNow - _lastShowTime;
                if (elapsed < _minShowInterval)
                {
                    var delay = _minShowInterval - elapsed;
                    Task.Delay(delay).Wait();
                }
            }

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

            // --- UI Creation (сокращено, логика прежняя) ---
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