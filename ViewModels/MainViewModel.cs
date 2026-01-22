using Avalonia.Controls;
using System;
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

        // Свойство для содержимого внутри главного окна
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

        // Команды
        public ICommand OpenConstantsCommand => new RelayCommand(() => OpenConstants());
        public ICommand OpenFiscalPrinterCommand => new RelayCommand(() => OpenFiscalPrinter());
        public ICommand ExitCommand => new RelayCommand(() => ExitApplication());
        public ICommand OpenReceiptsCommand => new RelayCommand(() => OpenReceipts());
        public ICommand CloseContentCommand => new RelayCommand(() => CloseCurrentContent());

        public ICommand OpenInternetDataLoadCommand => new RelayCommand(() => OpenInternetDataLoad());


        private void OpenInternetDataLoad()
        {
            StatusMessage = "Открытие загрузки данных...";
            Console.WriteLine("Открываем загрузку данных ВНУТРИ главного окна");

            var loadDataControl = new LoadDataWebService();

            // Подписываемся через именованный метод
            loadDataControl.RequestClose += OnLoadDataRequestClose;

            CurrentContent = loadDataControl;
            StatusMessage = "Загрузка данных из интернет открыта";

        }

        private void OpenConstants()
        {
            StatusMessage = "Открытие констант...";
            Console.WriteLine("Открываем константы ВНУТРИ главного окна");

            var constantsControl = new Constants();

            // Подписываемся через именованный метод
            constantsControl.RequestClose += OnConstantsRequestClose;

            CurrentContent = constantsControl;
            StatusMessage = "Константы открыты";
        }

        private void OnLoadDataRequestClose(object sender, EventArgs e)
        {
            Console.WriteLine("Получено событие RequestClose из Constants");
            CloseCurrentContent();
        }

        // Отдельный метод для обработки события
        private void OnConstantsRequestClose(object sender, EventArgs e)
        {
            Console.WriteLine("Получено событие RequestClose из Constants");
            CloseCurrentContent();
        }

        private void CloseCurrentContent()
        {
            Console.WriteLine("Закрываем CurrentContent");
            CurrentContent = null;
            StatusMessage = "Готово";
        }

        private void OpenFiscalPrinter()
        {
            StatusMessage = "Настройка фискального принтера...";
            Console.WriteLine("Открываем фискальный принтер");

            var fptk22Control = new FPTK22();

            // Подписываемся на событие закрытия
            fptk22Control.CloseRequested += OnFPTK22RequestClose;

            CurrentContent = fptk22Control;
            StatusMessage = "Фискальный регистратор открыт";
        }

        // Метод для обработки закрытия FPTK22
        private void OnFPTK22RequestClose(object sender, EventArgs e)
        {
            Console.WriteLine("Получено событие CloseRequested из FPTK22");
            CloseCurrentContent();
        }

        private void ExitApplication()
        {
            Environment.Exit(0);
        }

        private void OpenReceipts()
        {
            StatusMessage = "Открытие кассовых чеков...";
            Console.WriteLine("Открываем кассовые чеки");

            // Создаем UserControl
            var cashChecksControl = new Cash_checks();

            // ДОБАВЬТЕ ЭТУ ПОДПИСКУ НА СОБЫТИЕ ЗАКРЫТИЯ
            cashChecksControl.RequestClose += OnCashChecksRequestClose;

            // Устанавливаем как текущее содержимое
            CurrentContent = cashChecksControl;
            StatusMessage = "Кассовые чеки открыты";
        }

        // ДОБАВЬТЕ ЭТОТ МЕТОД ДЛЯ ОБРАБОТКИ ЗАКРЫТИЯ CASH_CHECKS
        private void OnCashChecksRequestClose(object sender, EventArgs e)
        {
            Console.WriteLine("Получено событие RequestClose из Cash_checks");
            CloseCurrentContent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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