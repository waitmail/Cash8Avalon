using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

public abstract class BaseWindow : Window
{
    public bool? DialogResult { get; protected set; }

    public async Task<bool?> ShowModal(Window owner = null)
    {
        var tcs = new TaskCompletionSource<bool?>();

        void OnWindowClosed(object sender, EventArgs e)
        {
            this.Closed -= OnWindowClosed;
            tcs.SetResult(this.DialogResult);
        }

        if (owner != null)
        {
            this.Owner = owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            this.Owner = null;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        this.Closed += OnWindowClosed;
        this.Show();

        return await tcs.Task;
    }

    /// <summary>
    /// Показывает окно как модальное с затемнением родительского окна.
    /// Родительское окно блокируется, но не закрывается.
    /// После закрытия диалога — родительское окно восстанавливается.
    /// </summary>    
    public async Task<bool?> ShowModalBlocking(Window owner = null)
    {
        var tcs = new TaskCompletionSource<bool?>();

        // 1. Блокируем родительское окно
        if (owner != null)
        {
            owner.IsEnabled = false;
            owner.BorderBrush = new SolidColorBrush(Colors.Gray);
            owner.BorderThickness = new Thickness(3);
        }

        // 2. Показываем диалог
        this.Show(owner);

        // 3. Создаем слабую ссылку на owner, чтобы избежать утечек памяти
        var weakOwner = owner != null ? new WeakReference<Window>(owner) : null;

        void OnDialogClosed(object sender, EventArgs e)
        {
            // 4. Удаляем обработчик события В ПЕРВУЮ ОЧЕРЕДЬ
            this.Closed -= OnDialogClosed;

            // 5. Восстанавливаем родительское окно
            if (weakOwner != null && weakOwner.TryGetTarget(out var targetOwner))
            {
                targetOwner.IsEnabled = true;
                targetOwner.BorderBrush = null;
                targetOwner.BorderThickness = new Thickness(0);

                // Используем Post вместо InvokeAsync для фокуса
                Dispatcher.UIThread.Post(() => targetOwner.Focus());
            }

            // 6. Устанавливаем результат
            tcs.SetResult(this.DialogResult);
        }

        this.Closed += OnDialogClosed;

        return await tcs.Task;
    }

    // Добавляем безопасный метод закрытия с установкой результата
    public void CloseWithResult(bool? result)
    {
        this.DialogResult = result;
        this.Close();
    }
}




//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Media;
//using Avalonia.Threading;
//using System;
//using System.Threading.Tasks;

//namespace Cash8Avalon
//{
//    public abstract class BaseWindow : Window
//    {
//        public bool? DialogResult { get; protected set; }

//        // ✅ ВАЖНО: Метод возвращает Task<bool?>
//        public async Task<bool?> ShowModal(Window owner = null)
//        {
//            var tcs = new TaskCompletionSource<bool?>();

//            // Устанавливаем владельца и позицию
//            if (owner != null)
//            {
//                this.Owner = owner; // явно задаём владельца
//                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
//                Show();
//            }
//            else
//            {
//                this.Owner = null;
//                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
//                Show();
//            }

//            Closed += (s, e) => tcs.SetResult(DialogResult);
//            return await tcs.Task;
//        }

//        // ✅ ПРОСТОЙ МЕТОД — ТОЛЬКО ЗАТЕМНЕНИЕ
//        public async Task<bool?> ShowModalBlocking(Window owner = null)
//        {
//            // 1. БЛОКИРУЕМ ОКНО (без изменения Opacity всего окна!)
//            if (owner != null)
//            {
//                owner.IsEnabled = false;

//                // Добавляем тёмную рамку для визуального эффекта
//                owner.BorderBrush = new SolidColorBrush(Colors.Gray);
//                owner.BorderThickness = new Thickness(3);
//            }

//            // 2. Показываем диалог
//            Show(owner);

//            // 3. Ждём закрытия диалога
//            var tcs = new TaskCompletionSource<bool?>();
//            Closed += OnDialogClosed;

//            bool? result = await tcs.Task;
//            return result;

//            void OnDialogClosed(object s, EventArgs e)
//            {
//                Closed -= OnDialogClosed;

//                // 4. ВОССТАНАВЛИВАЕМ ОКНО
//                if (owner != null)
//                {
//                    owner.IsEnabled = true;
//                    owner.BorderBrush = null;
//                    owner.BorderThickness = new Thickness(0);

//                    Dispatcher.UIThread.Post(() => owner.Focus());
//                }

//                tcs.SetResult(DialogResult);
//            }
//        }

//    }
//}