using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public abstract class BaseWindow : Window
    {
        public bool? DialogResult { get; protected set; }

        // ✅ ВАЖНО: Метод возвращает Task<bool?>
        public async Task<bool?> ShowModal(Window owner = null)
        {
            var tcs = new TaskCompletionSource<bool?>();

            // Устанавливаем владельца и позицию
            if (owner != null)
            {
                this.Owner = owner; // явно задаём владельца
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                Show();
            }
            else
            {
                this.Owner = null;
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Show();
            }

            Closed += (s, e) => tcs.SetResult(DialogResult);
            return await tcs.Task;
        }

        // ✅ ПРОСТОЙ МЕТОД — ТОЛЬКО ЗАТЕМНЕНИЕ
        public async Task<bool?> ShowModalBlocking(Window owner = null)
        {
            // 1. БЛОКИРУЕМ ОКНО (без изменения Opacity всего окна!)
            if (owner != null)
            {
                owner.IsEnabled = false;

                // Добавляем тёмную рамку для визуального эффекта
                owner.BorderBrush = new SolidColorBrush(Colors.Gray);
                owner.BorderThickness = new Thickness(3);
            }

            // 2. Показываем диалог
            Show(owner);

            // 3. Ждём закрытия диалога
            var tcs = new TaskCompletionSource<bool?>();
            Closed += OnDialogClosed;

            bool? result = await tcs.Task;
            return result;

            void OnDialogClosed(object s, EventArgs e)
            {
                Closed -= OnDialogClosed;

                // 4. ВОССТАНАВЛИВАЕМ ОКНО
                if (owner != null)
                {
                    owner.IsEnabled = true;
                    owner.BorderBrush = null;
                    owner.BorderThickness = new Thickness(0);

                    Dispatcher.UIThread.Post(() => owner.Focus());
                }

                tcs.SetResult(DialogResult);
            }
        }

    }
}