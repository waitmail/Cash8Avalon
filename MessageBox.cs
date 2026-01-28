using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
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
    // ПРОСТАЯ ВЕРСИЯ (ТОЛЬКО СООБЩЕНИЕ)
    public static async Task Show(string message, string title = "")
    {
        await ShowDialog(message, title, MessageBoxButton.OK, MessageBoxType.Info);
    }

    // ПОЛНАЯ ВЕРСИЯ
    public static async Task<MessageBoxResult> Show(string message, string title,
                                                     MessageBoxButton buttons,
                                                     MessageBoxType type = MessageBoxType.Info)
    {
        return await ShowDialog(message, title, buttons, type);
    }

    // ОСНОВНОЙ МЕТОД
    //private static async Task<MessageBoxResult> ShowDialog(string message, string title,
    //                                                       MessageBoxButton buttons,
    //                                                       MessageBoxType type)
    //{
    //    if (Application.Current?.ApplicationLifetime is
    //        IClassicDesktopStyleApplicationLifetime desktop)
    //    {
    //        var tcs = new TaskCompletionSource<MessageBoxResult>();

    //        // СОЗДАЕМ ОКНО
    //        //var dialog = new Window
    //        //{
    //        //    Title = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
    //        //    Width = 400,
    //        //    Height = 200,
    //        //    WindowStartupLocation = WindowStartupLocation.CenterOwner,
    //        //    CanResize = false,
    //        //    SizeToContent = SizeToContent.Manual
    //        //};

    //        // В Avalonia нет WindowChrome, но можно использовать стили
    //        var dialog = new Window
    //        {
    //            Title = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
    //            Width = 400,
    //            Height = 200,
    //            WindowStartupLocation = WindowStartupLocation.CenterOwner,

    //            // Эти свойства делают кнопки неактивными (серыми)
    //            CanResize = false,
    //            CanMinimize = false,
    //            CanMaximize = false,

    //            ShowInTaskbar = false,
    //            SystemDecorations = SystemDecorations.Full, // Заголовок и кнопки видны
    //            Topmost = true
    //        };


    //        // ОСНОВНОЙ КОНТЕЙНЕР
    //        var mainStack = new StackPanel
    //        {
    //            Margin = new Thickness(20),
    //            VerticalAlignment = VerticalAlignment.Center,
    //            HorizontalAlignment = HorizontalAlignment.Center
    //        };

    //        // ИКОНКА И ТЕКСТ
    //        var contentStack = new StackPanel
    //        {
    //            Orientation = Orientation.Horizontal,
    //            Spacing = 15,
    //            Margin = new Thickness(0, 0, 0, 20),
    //            HorizontalAlignment = HorizontalAlignment.Center
    //        };

    //        // ИКОНКА (эмодзи)
    //        var iconText = new TextBlock
    //        {
    //            Text = GetIconEmoji(type),
    //            FontSize = 32,
    //            VerticalAlignment = VerticalAlignment.Center
    //        };

    //        // ТЕКСТ СООБЩЕНИЯ
    //        var messageText = new TextBlock
    //        {
    //            Text = message,
    //            TextWrapping = TextWrapping.Wrap,
    //            FontSize = 14,
    //            VerticalAlignment = VerticalAlignment.Center,
    //            MaxWidth = 300
    //        };

    //        contentStack.Children.Add(iconText);
    //        contentStack.Children.Add(messageText);

    //        // КНОПКИ
    //        var buttonStack = new StackPanel
    //        {
    //            Orientation = Orientation.Horizontal,
    //            HorizontalAlignment = HorizontalAlignment.Center,
    //            Spacing = 10
    //        };

    //        // СОЗДАЕМ КНОПКИ В ЗАВИСИМОСТИ ОТ ТИПА
    //        switch (buttons)
    //        {
    //            case MessageBoxButton.OK:
    //                var okButton = CreateButton("OK", MessageBoxResult.OK, true, dialog, tcs);
    //                buttonStack.Children.Add(okButton);
    //                break;

    //            case MessageBoxButton.OKCancel:
    //                var okBtn = CreateButton("OK", MessageBoxResult.OK, true, dialog, tcs);
    //                var cancelBtn = CreateButton("Отмена", MessageBoxResult.Cancel, false, dialog, tcs);
    //                buttonStack.Children.Add(okBtn);
    //                buttonStack.Children.Add(cancelBtn);
    //                break;

    //            case MessageBoxButton.YesNo:
    //                var yesBtn = CreateButton("Да", MessageBoxResult.Yes, true, dialog, tcs);
    //                var noBtn = CreateButton("Нет", MessageBoxResult.No, false, dialog, tcs);
    //                buttonStack.Children.Add(yesBtn);
    //                buttonStack.Children.Add(noBtn);
    //                break;

    //            case MessageBoxButton.YesNoCancel:
    //                var yesButton = CreateButton("Да", MessageBoxResult.Yes, true, dialog, tcs);
    //                var noButton = CreateButton("Нет", MessageBoxResult.No, false, dialog, tcs);
    //                var cancelButton = CreateButton("Отмена", MessageBoxResult.Cancel, false, dialog, tcs);
    //                buttonStack.Children.Add(yesButton);
    //                buttonStack.Children.Add(noButton);
    //                buttonStack.Children.Add(cancelButton);
    //                break;
    //        }

    //        // ДОБАВЛЯЕМ ВСЕ В ОКНО
    //        mainStack.Children.Add(contentStack);
    //        mainStack.Children.Add(buttonStack);
    //        dialog.Content = mainStack;

    //        // ОБРАБОТЧИК ЗАКРЫТИЯ
    //        dialog.Closed += (s, e) =>
    //        {
    //            if (!tcs.Task.IsCompleted)
    //                tcs.TrySetResult(MessageBoxResult.None);
    //        };

    //        // ПОКАЗЫВАЕМ ОКНО
    //        if (desktop.MainWindow != null)
    //        {
    //            await dialog.ShowDialog(desktop.MainWindow);
    //        }
    //        else
    //        {
    //            dialog.Show();
    //        }

    //        return await tcs.Task;
    //    }

    //    return MessageBoxResult.None;
    //}

    // ОСНОВНОЙ МЕТОД (версия со ScrollViewer)
    private static async Task<MessageBoxResult> ShowDialog(string message, string title,
                                                           MessageBoxButton buttons,
                                                           MessageBoxType type)
    {
        if (Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>();

            // СОЗДАЕМ ОКНО
            var dialog = new Window
            {
                Title = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title,
                MinWidth = 300,
                MinHeight = 150,
                MaxWidth = 800,
                MaxHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = true,
                CanMinimize = false,
                CanMaximize = false,
                ShowInTaskbar = false,
                SystemDecorations = SystemDecorations.Full,
                Topmost = true,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            // ОСНОВНОЙ КОНТЕЙНЕР
            var mainStack = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // ИКОНКА И ТЕКСТ
            var contentStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // ИКОНКА (эмодзи)
            var iconText = new TextBlock
            {
                Text = GetIconEmoji(type),
                FontSize = 32,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 0, 0)
            };

            // ТЕКСТ СООБЩЕНИЯ в ScrollViewer
            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 600,
                MinWidth = 200
            };

            // ScrollViewer для очень длинных сообщений
            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = messageText
            };

            contentStack.Children.Add(iconText);
            contentStack.Children.Add(scrollViewer);

            // КНОПКИ
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // СОЗДАЕМ КНОПКИ В ЗАВИСИМОСТИ ОТ ТИПА
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    var okButton = CreateButton("OK", MessageBoxResult.OK, true, dialog, tcs);
                    buttonStack.Children.Add(okButton);
                    break;

                case MessageBoxButton.OKCancel:
                    var okBtn = CreateButton("OK", MessageBoxResult.OK, true, dialog, tcs);
                    var cancelBtn = CreateButton("Отмена", MessageBoxResult.Cancel, false, dialog, tcs);
                    buttonStack.Children.Add(okBtn);
                    buttonStack.Children.Add(cancelBtn);
                    break;

                case MessageBoxButton.YesNo:
                    var yesBtn = CreateButton("Да", MessageBoxResult.Yes, true, dialog, tcs);
                    var noBtn = CreateButton("Нет", MessageBoxResult.No, false, dialog, tcs);
                    buttonStack.Children.Add(yesBtn);
                    buttonStack.Children.Add(noBtn);
                    break;

                case MessageBoxButton.YesNoCancel:
                    var yesButton = CreateButton("Да", MessageBoxResult.Yes, true, dialog, tcs);
                    var noButton = CreateButton("Нет", MessageBoxResult.No, false, dialog, tcs);
                    var cancelButton = CreateButton("Отмена", MessageBoxResult.Cancel, false, dialog, tcs);
                    buttonStack.Children.Add(yesButton);
                    buttonStack.Children.Add(noButton);
                    buttonStack.Children.Add(cancelButton);
                    break;
            }

            // ДОБАВЛЯЕМ ВСЕ В ОКНО
            mainStack.Children.Add(contentStack);
            mainStack.Children.Add(buttonStack);
            dialog.Content = mainStack;

            // ОБРАБОТЧИК ЗАКРЫТИЯ
            dialog.Closed += (s, e) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetResult(MessageBoxResult.None);
            };

            // ПОКАЗЫВАЕМ ОКНО
            if (desktop.MainWindow != null)
            {
                await dialog.ShowDialog(desktop.MainWindow);
            }
            else
            {
                dialog.Show();
            }

            return await tcs.Task;
        }

        return MessageBoxResult.None;
    }

    // СОЗДАНИЕ КНОПКИ
    //private static Button CreateButton(string content, MessageBoxResult buttonResult,
    //                                   bool isDefault, Window dialog,
    //                                   TaskCompletionSource<MessageBoxResult> tcs)
    //{
    //    var button = new Button
    //    {
    //        Content = content,
    //        Width = 80,
    //        Height = 30,
    //        HorizontalAlignment = HorizontalAlignment.Center
    //    };

    //    button.Click += (s, e) =>
    //    {
    //        tcs.TrySetResult(buttonResult);
    //        dialog.Close();
    //    };

    //    if (isDefault)
    //    {
    //        // ОБРАБОТКА ENTER
    //        dialog.KeyDown += (s, e) =>
    //        {
    //            if (e.Key == Avalonia.Input.Key.Enter)
    //            {
    //                e.Handled = true;
    //                tcs.TrySetResult(buttonResult);
    //                dialog.Close();
    //            }
    //        };
    //    }

    //    return button;
    //}

    private static Button CreateButton(string content, MessageBoxResult buttonResult,
                                   bool isDefault, Window dialog,
                                   TaskCompletionSource<MessageBoxResult> tcs)
    {
        var button = new Button
        {
            Content = content,
            Width = 80,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        button.Click += (s, e) =>
        {
            tcs.TrySetResult(buttonResult);
            dialog.Close();
        };

        // УСТАНАВЛИВАЕМ ФОКУС НА КНОПКУ ПО УМОЛЧАНИЮ ПРИ ОТКРЫТИИ ОКНА
        if (isDefault)
        {
            dialog.Opened += (s, e) =>
            {
                // Используем Dispatcher чтобы установить фокус после полной загрузки
                Dispatcher.UIThread.Post(() =>
                {
                    button.Focus();
                }, DispatcherPriority.Input);
            };
        }

        // ОБРАБОТКА ENTER для кнопки по умолчанию
        if (isDefault)
        {
            dialog.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    e.Handled = true; // ВАЖНО для Linux!
                    tcs.TrySetResult(buttonResult);
                    dialog.Close();
                }
            };
        }

        return button;
    }

    // ПОЛУЧЕНИЕ ЗАГОЛОВКА ПО УМОЛЧАНИЮ
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

    // ПОЛУЧЕНИЕ ИКОНКИ (ЭМОДЗИ)
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
