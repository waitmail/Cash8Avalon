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
        public ICommand OpenCashChecksCommand { get; }
        public ICommand CloseContentCommand { get; }
        public ICommand OpenSettingConnectCommand { get; }
        public ICommand OpenInternetDataLoadCommand { get; }
        public ICommand OpenProgramInfoCommand { get; }
        public ICommand OpenLoadProgramFromInternetCommand { get; }

        public MainViewModel()
        {
            OpenConstantsCommand = new RelayCommand(OpenConstants, () => CanOpenWindow<Constants>());
            OpenFiscalPrinterCommand = new RelayCommand(OpenFiscalPrinter, () => CanOpenWindow<FPTK22>());
            OpenInternetDataLoadCommand = new RelayCommand(OpenInternetDataLoad, () => CanOpenWindow<LoadDataWebService>());
            OpenSettingConnectCommand = new RelayCommand(OpenSettingConnect, () => CanOpenWindow<SettingConnect>());
            OpenProgramInfoCommand = new RelayCommand(OpenProgramInfo, () => CanOpenWindow<ProgramInfo>());
            OpenLoadProgramFromInternetCommand = new RelayCommand(OpenLoadProgramFromInternet, () => CanOpenWindow<LoadProgramFromInternet>());

            ExitCommand = new RelayCommand(ExitApplication);
            OpenReceiptsCommand = new RelayCommand(OpenReceipts);
            OpenCashChecksCommand = new RelayCommand(OpenCashChecks);
            CloseContentCommand = new RelayCommand(CloseCurrentContent);
        }

        private bool CanOpenWindow<T>() where T : Window
        {
            return !_openWindows.ContainsKey(typeof(T));
        }

        private T OpenWindow<T>(string operationName) where T : Window, new()
        {
            try
            {
                StatusMessage = $"Открытие {operationName}...";

                if (_openWindows.TryGetValue(typeof(T), out var existingWindow) && existingWindow != null)
                {
                    existingWindow.Activate();
                    StatusMessage = $"{operationName} уже открыто";
                    return (T)existingWindow;
                }

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

        // === ИСПРАВЛЕННАЯ ЛОГИКА ДЛЯ USERCONTROL ===
        private void OpenReceipts()
        {
            // ИЗМЕНЕНИЕ: Если чеки уже открыты, мы сначала "закрываем" их,
            // чтобы создать заново (аналог перезагрузки при перелогине).
            if (_isReceiptsOpen)
            {
                // Сначала очищаем контент (это вызовет Unloaded у старого контрола)
                CurrentContent = null;
                // Сбрасываем флаг
                _isReceiptsOpen = false;
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