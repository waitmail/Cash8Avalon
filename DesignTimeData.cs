using System;
using System.Collections.ObjectModel;

namespace Cash8Avalon
{
    public static class DesignTimeData
    {
        public static ObservableCollection<CheckItem> GetDesignTimeCheckItems()
        {
            return new ObservableCollection<CheckItem>
            {
                new CheckItem
                {
                    ItsDeleted = 0,
                    DateTimeWrite = DateTime.Now.AddHours(-2),
                    ClientName = "Дизайнер: Иванов И.И.",
                    Cash = 1500.75m,
                    Remainder = 0.75m,
                    Comment = "Тест дизайнера 1",
                    CheckType = "Продажа",
                    DocumentNumber = "DT001",
                    ItsPrint = true,
                    ItsPrintP = false
                },
                new CheckItem
                {
                    ItsDeleted = 1,
                    DateTimeWrite = DateTime.Now.AddHours(-1),
                    ClientName = "Дизайнер: Петров П.П.",
                    Cash = 3200.00m,
                    Remainder = 200.00m,
                    Comment = "Тест дизайнера 2",
                    CheckType = "Возврат",
                    DocumentNumber = "DT002",
                    ItsPrint = false,
                    ItsPrintP = true
                }
            };
        }
    }
}