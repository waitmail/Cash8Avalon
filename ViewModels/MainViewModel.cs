//using Avalonia.Controls;
//using System;
//using System.ComponentModel;
//using System.Runtime.CompilerServices;
//using System.Windows.Input;

//namespace Cash8Avalon.ViewModels
//{
//    public class MainViewModel : INotifyPropertyChanged
//    {
//        private string _statusMessage = "Готово";
//        public string StatusMessage
//        {
//            get => _statusMessage;
//            set
//            {
//                if (_statusMessage != value)
//                {
//                    _statusMessage = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        // Свойство для содержимого внутри главного окна
//        private Control _currentContent;
//        public Control CurrentContent
//        {
//            get => _currentContent;
//            set
//            {
//                if (_currentContent != value)
//                {
//                    _currentContent = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        // Команды
//        public ICommand OpenConstantsCommand => new RelayCommand(() => OpenConstants());
//        public ICommand OpenFiscalPrinterCommand => new RelayCommand(() => OpenFiscalPrinter());
//        public ICommand ExitCommand => new RelayCommand(() => ExitApplication());
//        public ICommand OpenReceiptsCommand => new RelayCommand(() => OpenReceipts());
//        public void OpenCashChecks()
//        {
//            OpenReceipts();
//        }
//        public ICommand CloseContentCommand => new RelayCommand(() => CloseCurrentContent());
//        public ICommand OpenSettingConnectCommand => new RelayCommand(() => OpenSettingConnect());
//        public ICommand OpenInternetDataLoadCommand => new RelayCommand(() => OpenInternetDataLoad());
//        public ICommand OpenProgramInfoCommand => new RelayCommand(() => OpenProgramInfo());
//        public ICommand OpenLoadProgramFromInternetCommand => new RelayCommand(() => OpenLoadProgramFromInternet());

//        // Обработчик закрытия окна параметров БД
//        private void OnSettingConnectRequestClose(object sender, EventArgs e)
//        {
//            Console.WriteLine("Получено событие RequestClose из SettingConnect");
//            CloseCurrentContent();
//        }

//        private void OpenLoadProgramFromInternet()
//        {
//            ShowWindow(new LoadProgramFromInternet(), "Обновление программы");
//        }


//        private void OpenProgramInfo()
//        {
//            ShowWindow(new ProgramInfo(), "О программе");
//        }

//        // Использование:
//        private void OpenSettingConnect()
//        {
//            ShowWindow(new SettingConnect(), "параметров соединения с БД");
//        }

//        private void OpenInternetDataLoad()
//        {
//            ShowWindow(new LoadDataWebService(), "загрузки данных из интернет");
//        }

//        private void ShowWindow(Window window, string operationName)
//        {
//            StatusMessage = $"Открытие {operationName}...";
//            Console.WriteLine($"Открываем {operationName} в отдельном окне");

//            window.Closed += (sender, e) =>
//            {
//                Console.WriteLine($"Окно {operationName} закрыто");
//                StatusMessage = "Готово";
//            };

//            window.Show();
//            StatusMessage = $"{operationName} открыт в отдельном окне";
//        }

//        private void OpenConstants()
//        {
//            ShowWindow(new Constants(), "Настройки");         
//        }

//        private void OnLoadDataRequestClose(object sender, EventArgs e)
//        {
//            Console.WriteLine("Получено событие RequestClose из LoadDataWebService");
//            CloseCurrentContent();
//        }

//        // Отдельный метод для обработки события
//        private void OnConstantsRequestClose(object sender, EventArgs e)
//        {
//            Console.WriteLine("Получено событие RequestClose из Constants");
//            CloseCurrentContent();
//        }

//        private void CloseCurrentContent()
//        {
//            Console.WriteLine("Закрываем CurrentContent");
//            CurrentContent = null;
//            StatusMessage = "Готово";
//        }

//        private void OpenFiscalPrinter()
//        {
//            ShowWindow(new FPTK22(), "Настройка фискального принтера...");          
//        }

//        // Метод для обработки закрытия FPTK22
//        private void OnFPTK22RequestClose(object sender, EventArgs e)
//        {
//            Console.WriteLine("Получено событие CloseRequested из FPTK22");
//            CloseCurrentContent();
//        }

//        private void ExitApplication()
//        {
//            Environment.Exit(0);
//        }

//        private void OpenReceipts()
//        {
//            StatusMessage = "Открытие кассовых чеков...";
//            Console.WriteLine("Открываем кассовые чеки");

//            // Создаем UserControl
//            var cashChecksControl = new Cash_checks();

//            // ДОБАВЬТЕ ЭТУ ПОДПИСКУ НА СОБЫТИЕ ЗАКРЫТИЯ
//            cashChecksControl.RequestClose += OnCashChecksRequestClose;

//            // Устанавливаем как текущее содержимое
//            CurrentContent = cashChecksControl;
//            StatusMessage = "Кассовые чеки открыты";
//        }

//        // ДОБАВЬТЕ ЭТОТ МЕТОД ДЛЯ ОБРАБОТКИ ЗАКРЫТИЯ CASH_CHECKS
//        private void OnCashChecksRequestClose(object sender, EventArgs e)
//        {
//            Console.WriteLine("Получено событие RequestClose из Cash_checks");
//            CloseCurrentContent();
//        }

//        public event PropertyChangedEventHandler? PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // Интерфейс для контролов, которые можно закрыть
//    public interface IClosableControl
//    {
//        event EventHandler RequestClose;
//    }

//    public class RelayCommand : ICommand
//    {
//        private readonly Action _execute;
//        private readonly Func<bool>? _canExecute;

//        public event EventHandler? CanExecuteChanged;

//        public RelayCommand(Action execute, Func<bool>? canExecute = null)
//        {
//            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
//            _canExecute = canExecute;
//        }

//        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

//        public void Execute(object? parameter) => _execute();

//        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
//    }
//}

using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Cash8Avalon.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _statusMessage = "Готово";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        private Control _currentContent;
        public Control CurrentContent
        {
            get => _currentContent;
            set
            {
                if (_currentContent != value)
                {
                    _currentContent = value;
                    OnPropertyChanged();
                }
            }
        }

        // Хранилище открытых окон
        private readonly Dictionary<Type, Window> _openWindows = new();

        // Флаг для UserControl
        private bool _isReceiptsOpen = false;

        // === КОМАНДЫ ===
        public ICommand OpenConstantsCommand { get; }
        public ICommand OpenFiscalPrinterCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand OpenReceiptsCommand { get; }
        public ICommand OpenCashChecksCommand { get; } // <-- Вернул эту команду
        public ICommand CloseContentCommand { get; }
        public ICommand OpenSettingConnectCommand { get; }
        public ICommand OpenInternetDataLoadCommand { get; }
        public ICommand OpenProgramInfoCommand { get; }
        public ICommand OpenLoadProgramFromInternetCommand { get; }

        public MainViewModel()
        {
            // Инициализация команд с проверкой CanExecute (обратите внимание на <T>)
            OpenConstantsCommand = new RelayCommand(OpenConstants, () => CanOpenWindow<Constants>());
            OpenFiscalPrinterCommand = new RelayCommand(OpenFiscalPrinter, () => CanOpenWindow<FPTK22>());
            OpenInternetDataLoadCommand = new RelayCommand(OpenInternetDataLoad, () => CanOpenWindow<LoadDataWebService>());
            OpenSettingConnectCommand = new RelayCommand(OpenSettingConnect, () => CanOpenWindow<SettingConnect>());
            OpenProgramInfoCommand = new RelayCommand(OpenProgramInfo, () => CanOpenWindow<ProgramInfo>());
            OpenLoadProgramFromInternetCommand = new RelayCommand(OpenLoadProgramFromInternet, () => CanOpenWindow<LoadProgramFromInternet>());

            // Простые команды
            ExitCommand = new RelayCommand(ExitApplication);
            OpenReceiptsCommand = new RelayCommand(OpenReceipts);
            OpenCashChecksCommand = new RelayCommand(OpenCashChecks); // <-- Инициализация
            CloseContentCommand = new RelayCommand(CloseCurrentContent);
        }

        // === ПРОВЕРКА CANEXECUTE (ИСПРАВЛЕНО: ДОБАВЛЕНО <T>) ===
        private bool CanOpenWindow<T>() where T : Window
        {
            return !_openWindows.ContainsKey(typeof(T));
        }

        // === УНИВЕРСАЛЬНЫЙ МЕТОД ОТКРЫТИЯ (ИСПРАВЛЕНО: ДОБАВЛЕНО <T>) ===
        private T OpenWindow<T>(string operationName) where T : Window, new()
        {
            try
            {
                StatusMessage = $"Открытие {operationName}...";

                // Если окно уже открыто, активируем его
                if (_openWindows.TryGetValue(typeof(T), out var existingWindow) && existingWindow != null)
                {
                    existingWindow.Activate();
                    StatusMessage = $"{operationName} уже открыто";
                    return (T)existingWindow;
                }

                // Создаем и открываем новое окно
                var window = new T();
                _openWindows[typeof(T)] = window;

                window.Closed += (s, e) =>
                {
                    _openWindows.Remove(typeof(T));
                    StatusMessage = "Готово";
                    RefreshAllCommands();
                };

                window.Show();
                StatusMessage = $"{operationName} открыто";

                return window;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка открытия {operationName}: {ex.Message}");
                StatusMessage = $"Ошибка: {ex.Message}";
                _openWindows.Remove(typeof(T));
                return null;
            }
            finally
            {
                RefreshAllCommands();
            }
        }

        private void RefreshAllCommands()
        {
            (OpenConstantsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenFiscalPrinterCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenInternetDataLoadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenSettingConnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenProgramInfoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenLoadProgramFromInternetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // === МЕТОДЫ ОТКРЫТИЯ ===

        // Восстановленный метод OpenCashChecks
        public void OpenCashChecks()
        {
            OpenReceipts();
        }

        private void OpenConstants() => OpenWindow<Constants>("Настройки");
        private void OpenFiscalPrinter() => OpenWindow<FPTK22>("Настройка ФП");
        private void OpenInternetDataLoad() => OpenWindow<LoadDataWebService>("загрузки данных");
        private void OpenSettingConnect() => OpenWindow<SettingConnect>("параметры БД");
        private void OpenProgramInfo() => OpenWindow<ProgramInfo>("О программе");
        private void OpenLoadProgramFromInternet() => OpenWindow<LoadProgramFromInternet>("Обновление");

        // === ЛОГИКА ДЛЯ USERCONTROL ===
        private void OpenReceipts()
        {
            if (_isReceiptsOpen)
            {
                StatusMessage = "Кассовые чеки уже открыты";
                return;
            }

            try
            {
                _isReceiptsOpen = true;
                StatusMessage = "Открытие кассовых чеков...";

                var cashChecksControl = new Cash_checks();
                cashChecksControl.RequestClose += (s, e) =>
                {
                    _isReceiptsOpen = false;
                    CloseCurrentContent();
                };

                CurrentContent = cashChecksControl;
                StatusMessage = "Кассовые чеки открыты";
            }
            catch (Exception ex)
            {
                _isReceiptsOpen = false;
                Console.WriteLine($"✗ Ошибка открытия чеков: {ex.Message}");
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        private void CloseCurrentContent()
        {
            CurrentContent = null;
            StatusMessage = "Готово";
        }

        private void ExitApplication()
        {
            Environment.Exit(0);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // === ИНТЕРФЕЙСЫ И КОМАНДЫ ===
    public interface IClosableControl
    {
        event EventHandler RequestClose;
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}