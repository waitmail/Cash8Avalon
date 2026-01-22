using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Core;
using System;
using System.Collections.Concurrent;

namespace Cash8Avalon
{
    public abstract class ControlBase : UserControl
    {
        private readonly ConcurrentDictionary<string, WeakReference<Control>> _controlsCache = new();

        // Основной индексатор возвращает Control
        public Control this[string controlName]
        {
            get
            {
                // 1. Проверяем кэш (с WeakReference)
                if (_controlsCache.TryGetValue(controlName, out var weakRef) &&
                    weakRef.TryGetTarget(out var cachedControl))
                {
                    return cachedControl;
                }

                Control? control = null;

                // 2. Ищем через NameScope (самый быстрый способ)
                control = FindViaNameScope(controlName);

                // 3. Если не нашли - пробуем FindControl (на всякий случай)
                if (control == null)
                {
                    control = this.FindControl<Control>(controlName);
                }

                // 4. Обрабатываем результат
                if (control == null)
                {
                    throw new ControlNotFoundException(controlName, GetType().Name);
                }

                // 5. Сохраняем в кэш (WeakReference не удерживает объект в памяти)
                _controlsCache[controlName] = new WeakReference<Control>(control);
                return control;
            }
        }

        private Control? FindViaNameScope(string name)
        {
            // Это самый эффективный способ поиска контролов в Avalonia
            return NameScope.GetNameScope(this)?.Find(name) as Control;
        }

        // Type-safe методы (оставляем как есть)
        public T GetRequiredControl<T>(string controlName) where T : Control
        {
            var control = this[controlName];

            if (control is T typedControl)
                return typedControl;

            throw new InvalidOperationException(
                $"Контрол '{controlName}' имеет тип '{control.GetType().Name}', " +
                $"а ожидался '{typeof(T).Name}'");
        }

        public T? GetControl<T>(string controlName) where T : Control
        {
            try
            {
                return GetRequiredControl<T>(controlName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool HasControl(string controlName)
        {
            try
            {
                _ = this[controlName];
                return true;
            }
            catch (ControlNotFoundException)
            {
                return false;
            }
        }

        public void PreloadControls(params string[] controlNames)
        {
            foreach (var name in controlNames)
            {
                try
                {
                    _ = this[name]; // Будет закэшировано
                }
                catch (ControlNotFoundException)
                {
                    // Игнорируем
                }
            }
        }

        public void ClearCache()
        {
            _controlsCache.Clear();
        }

        // Опционально: метод для принудительного сброса конкретного контрола
        public void RemoveFromCache(string controlName)
        {
            _controlsCache.TryRemove(controlName, out _);
        }

        // Опционально: проверка есть ли контрол в кэше (живой)
        public bool IsCached(string controlName)
        {
            if (_controlsCache.TryGetValue(controlName, out var weakRef))
            {
                return weakRef.TryGetTarget(out _);
            }
            return false;
        }
    }

    public class ControlNotFoundException : Exception
    {
        public string ControlName { get; }
        public string ControlType { get; }

        public ControlNotFoundException(string controlName, string controlType)
            : base($"Контрол '{controlName}' не найден в '{controlType}'. " +
                   "Проверьте x:Name в XAML и регистр символов.")
        {
            ControlName = controlName;
            ControlType = controlType;
        }
    }
}