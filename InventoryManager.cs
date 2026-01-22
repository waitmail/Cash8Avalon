using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cash8Avalon;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public static class InventoryManager
    {
        private static Dictionary<long, ProductData> dictionaryProductData = new Dictionary<long, ProductData>();
        private static Dictionary<int, double> giftPriceAction = new Dictionary<int, double>();

        public static bool completeDictionaryProductData = false;
        //public static int rowCount = 0;
        //public static int rowCountCurrent = 0;

        //private static Dictionary<long, ProductData> DictionaryProductData
        //{
        //    get => dictionaryProductData;
        //}

        public static void ClearDictionaryProductData()
        {
            completeDictionaryProductData = false;
            dictionaryProductData.Clear();
            giftPriceAction.Clear();
        }

        //public static async Task FillDictionaryProductDataAsync()
        //{
        //    await Task.Run(() =>
        //    {
        //        try
        //        {
        //            FillDictionaryProductData();
        //        }
        //        catch (Exception ex)
        //        {
        //            // Перехват исключения и передача его в основной поток
        //            if (Application.OpenForms.Count > 0)
        //            {
        //                var mainForm = Application.OpenForms[0];
        //                mainForm.Invoke(new MethodInvoker(() =>
        //                {
        //                    MessageBox.Show($"Произошла ошибка: {ex.Message}", "Работа с кешем товаров");
        //                }));
        //            }
        //        }
        //    });
        //}

        public static async Task FillDictionaryProductDataAsync(Window parentWindow = null)
        {
            try
            {
                await Task.Run(() =>
                {
                    FillDictionaryProductData();
                });
            }
            catch (Exception ex)
            {
                // Показываем MessageBox в UI потоке
                ShowErrorDialogAsync(parentWindow, ex);
            }
        }

        private static void ShowErrorDialogAsync(Window parentWindow, Exception ex)
        {
            // Используем Dispatcher для гарантированного выполнения в UI потоке
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // Показываем MessageBox (ваш текущий вариант)
                    await MessageBox.Show(
                        $"Произошла ошибка: {ex.Message}",
                        "Работа с кешем товаров",
                        MessageBoxButton.OK,
                        MessageBoxType.Error);
                }
            });
        }

        public static Dictionary<int, double> DictionaryPriceGiftAction
        {
            get
            {
                // Ленивая инициализация
                if ((giftPriceAction == null) || (giftPriceAction.Count == 0))
                {
                    giftPriceAction = GetPriceGiftAction();
                }
                return giftPriceAction;
            }
        }

        private static Dictionary<int, double> GetPriceGiftAction()
        {
            var result = new Dictionary<int, double>();

            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    conn.Open();
                    string query = "SELECT num_doc, gift_price FROM action_header";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int numDoc = reader.GetInt32(reader.GetOrdinal("num_doc"));
                            double giftPriceValue = Convert.ToDouble(reader.GetDecimal(reader.GetOrdinal("gift_price")));
                            result[numDoc] = giftPriceValue;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Заполнение словаря ценами подарков из акций");
                    // Возвращаем пустой словарь вместо null
                    return new Dictionary<int, double>();
                }
                catch (Exception ex)
                {
                    MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Заполнение словаря ценами подарков из акций");
                    // Возвращаем пустой словарь вместо null
                    return new Dictionary<int, double>();
                }
            }

            return result;
        }



        //public static bool FillDictionaryProductData()
        //{
        //    bool result = true;

        //    dictionaryProductData.Clear();

        //    using (var conn = MainStaticClass.NpgsqlConn())
        //    {
        //        try
        //        {                    
        //            //string countQuery = "SELECT COUNT(*) FROM tovar " +
        //            //    "LEFT JOIN barcode ON tovar.code=barcode.tovar_code " +
        //            //    "WHERE tovar.its_deleted = 0 AND tovar.retail_price<>0";

        //            conn.Open();
        //            //using (var countCommand = new NpgsqlCommand(countQuery, conn))
        //            //{
        //            //    rowCount = Convert.ToInt32(countCommand.ExecuteScalar());
        //            //}

        //            string query = "SELECT tovar.code, tovar.name, tovar.retail_price, tovar.its_certificate, tovar.its_marked, tovar.cdn_check, tovar.fractional, barcode.barcode FROM tovar " +
        //            "LEFT JOIN barcode ON tovar.code=barcode.tovar_code WHERE tovar.its_deleted = 0 AND tovar.retail_price<>0";

        //            using (var command = new NpgsqlCommand(query, conn))
        //            {
        //                using (var reader = command.ExecuteReader())
        //                {
        //                    //rowCountCurrent = 0;
        //                    while (reader.Read())
        //                    {
        //                        //rowCountCurrent++;
        //                        long code = Convert.ToInt64(reader["code"]);

        //                        ProductFlags flags = ProductFlags.None;
        //                        // Создаем флаги на основе значений из базы данных                            
        //                        if (Convert.ToBoolean(reader["its_certificate"])) flags |= ProductFlags.Certificate;
        //                        if (Convert.ToBoolean(reader["its_marked"])) flags |= ProductFlags.Marked;
        //                        if (Convert.ToBoolean(reader["cdn_check"])) flags |= ProductFlags.CDNCheck;
        //                        if (Convert.ToBoolean(reader["fractional"])) flags |= ProductFlags.Fractional;

        //                        if (!dictionaryProductData.TryGetValue(code, out _))
        //                        {
        //                            var productData = new ProductData(code, reader["name"].ToString().Trim(), Convert.ToDecimal(reader["retail_price"]), flags);

        //                            AddItem(code, productData);
        //                        }

        //                        string barcode = reader["barcode"].ToString().Trim();
        //                        if (!(string.IsNullOrEmpty(barcode) || dictionaryProductData.TryGetValue(Convert.ToInt64(barcode), out _)))
        //                        {
        //                            var productData = new ProductData(Convert.ToInt64(code), reader["name"].ToString().Trim(), Convert.ToDecimal(reader["retail_price"]), flags);
        //                            AddItem(Convert.ToInt64(barcode), productData);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (NpgsqlException ex)
        //        {
        //            //MessageBox.Show($"Произошли ошибки при заполнении словаря данными о товарах: {ex.Message}", "Заполнение кеша товаров");
        //            throw new Exception($"При заполнении словаря данными о товарах: {ex.Message}", ex);
        //            //result = false;
        //        }
        //        catch (Exception ex)
        //        {
        //            //MessageBox.Show($"Произошли ошибки при заполнении словаря данными о товарах: {ex.Message}", "Заполенние кеша товаров");
        //            throw new Exception($"При заполнении словаря данными о товарах: {ex.Message}", ex);
        //            //result = false;
        //        }
        //        finally
        //        {
        //            if (conn.State == System.Data.ConnectionState.Open)
        //            {
        //                conn.Close();
        //            }
        //        }
        //        complete = result;
        //        return result;
        //    }
        //}       

        public static bool FillDictionaryProductData()
        {
            dictionaryProductData.Clear();

            using (var conn = MainStaticClass.NpgsqlConn())
            {
                try
                {
                    conn.Open();

                    string query = @"
                SELECT tovar.code, tovar.name, tovar.retail_price, tovar.its_certificate, 
                       tovar.its_marked, tovar.cdn_check, tovar.fractional, barcode.barcode,tovar.refusal_of_marking,tovar.rr_not_control_owner 
                FROM tovar 
                LEFT JOIN barcode ON tovar.code = barcode.tovar_code 
                WHERE tovar.its_deleted = 0 AND tovar.retail_price <> 0";

                    using (var command = new NpgsqlCommand(query, conn))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long code = Convert.ToInt64(reader["code"]);
                            string name = reader["name"].ToString().Trim();
                            decimal retailPrice = Convert.ToDecimal(reader["retail_price"]);
                            string barcode = reader["barcode"]?.ToString().Trim();

                            // Создаем флаги на основе значений из базы данных
                            ProductFlags flags = ProductFlags.None;
                            if (Convert.ToBoolean(reader["its_certificate"])) flags |= ProductFlags.Certificate;
                            if (Convert.ToBoolean(reader["its_marked"])) flags |= ProductFlags.Marked;
                            if (Convert.ToBoolean(reader["refusal_of_marking"])) flags |= ProductFlags.RefusalMarking;


                            //if (Convert.ToBoolean(reader["refusal_of_marking"]))
                            //{
                            //    // Сбрасываем флаг Marked, если он был установлен ранее
                            //    flags &= ~ProductFlags.Marked;
                            //}


                            if (Convert.ToBoolean(reader["cdn_check"])) flags |= ProductFlags.CDNCheck;
                            if (Convert.ToBoolean(reader["fractional"])) flags |= ProductFlags.Fractional;
                            if (Convert.ToBoolean(reader["rr_not_control_owner"])) flags |= ProductFlags.RrNotControlOwner;

                            // Добавляем товар по его коду
                            if (!dictionaryProductData.ContainsKey(code))
                            {
                                var productData = new ProductData(code, name, retailPrice, flags);
                                dictionaryProductData[code] = productData;
                            }

                            // Добавляем товар по штрихкоду
                            if (!string.IsNullOrEmpty(barcode) && long.TryParse(barcode, out var barcodeValue))
                            {
                                if (!dictionaryProductData.ContainsKey(barcodeValue))
                                {
                                    var productData = new ProductData(code, name, retailPrice, flags);
                                    dictionaryProductData[barcodeValue] = productData;
                                }
                            }
                        }
                    }
                    completeDictionaryProductData = true;
                    return completeDictionaryProductData;
                }
                catch (NpgsqlException ex)
                {
                    throw new Exception($"Ошибка при заполнении словаря данными о товарах: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка при заполнении словаря данными о товарах: {ex.Message}", ex);
                }
            }
        }

        public static void AddItem(long id, ProductData data)
        {
            if (!dictionaryProductData.ContainsKey(id))
            {
                dictionaryProductData.Add(id, data);
            }
            else
            {
                throw new ArgumentException($"Товар с идентификатором {id} уже существует.");
            }
        }

        public static ProductData GetItem(long id)
        {
            if (dictionaryProductData.TryGetValue(id, out var data))
            {
                return data;
            }
            else
            {
                return new ProductData(0, string.Empty, 0, ProductFlags.None);
            }
        }
    }
}
