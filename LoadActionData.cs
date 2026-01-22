using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public static class LoadActionDataInMemory
    {

        private static Dictionary<int, ActionDataContainer> allActionData2 = null;
        private static Dictionary<int, Dictionary<long, decimal>> allActionData1 = null;

        public class ActionDataContainer
        {
            public Dictionary<int, List<long>> ListItems { get; set; } = new Dictionary<int, List<long>>();
            public Dictionary<int, int> ListQuantities { get; set; } = new Dictionary<int, int>();
        }

        public static Dictionary<int, ActionDataContainer> AllActionData2
        {
            get => LoadAllActionData2();
            set => allActionData2 = value;
        }

        public static Dictionary<int, Dictionary<long, decimal>> AllActionData1
        {
            get => LoadAllActionData1();
            set => allActionData1 = value;
        }

        /// <summary>
        /// Загружает данные из таблицы action_table и возвращает словарь с ценами по документам.
        /// </summary>
        /// <returns>Словарь, где ключ - номер документа, значение - словарь с товарами и их ценами.</returns>
        private static Dictionary<int, Dictionary<long, decimal>> LoadAllActionData1()
        {
            //var actionPricesByDoc = new Dictionary<int, Dictionary<long, decimal>>();
            if (allActionData1 != null)
            {
                return allActionData1;
            }

            allActionData1 = new Dictionary<int, Dictionary<long, decimal>>();

            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    // Загружаем данные из action_table
                    string query = @"
                SELECT num_doc, code_tovar, price 
                FROM action_table 
                ORDER BY num_doc, code_tovar";

                    using (var command = new NpgsqlCommand(query, conn))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int num_doc = reader.GetInt32(0);
                            long code_tovar = reader.GetInt64(1);
                            decimal price = reader.GetDecimal(2);

                            // Инициализируем словарь для документа, если он еще не существует
                            if (!allActionData1.ContainsKey(num_doc))
                            {
                                allActionData1[num_doc] = new Dictionary<long, decimal>();
                            }

                            // Добавляем товар и его цену в словарь
                            allActionData1[num_doc][code_tovar] = price;
                        }
                    }
                }
            }
            catch //(Exception ex)
            {
                allActionData1 = null;
            }

            return allActionData1;
        }

        private static Dictionary<int, ActionDataContainer> LoadAllActionData2()
        {
            try
            {
                //Int64 count_minutes = Convert.ToInt64((DateTime.Now - DateTime.Now.Date).TotalMinutes);
                if (allActionData2 != null)
                {
                    return allActionData2;
                }

                allActionData2 = new Dictionary<int, ActionDataContainer>();

                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();
                    string query = @"
                        SELECT num_doc, num_list, code_tovar 
                        FROM action_table 
                        WHERE num_doc IN(SELECT num_doc FROM action_header WHERE '" + DateTime.Now.Date.ToString("yyy-MM-dd") + "' between date_started AND date_end " +
                        //" AND " + count_minutes.ToString() + " between time_start AND time_end " +
                        ") ORDER BY num_doc, num_list, code_tovar";

                    using (var command = new NpgsqlCommand(query, conn))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int num_doc = reader.GetInt32(0);
                            int num_list = reader.GetInt32(1);
                            long code_tovar = reader.GetInt64(2);

                            if (!allActionData2.ContainsKey(num_doc))
                            {
                                allActionData2[num_doc] = new ActionDataContainer();
                            }

                            var container = allActionData2[num_doc];

                            // Заполняем список товаров для каждого списка акций
                            if (!container.ListItems.ContainsKey(num_list))
                            {
                                container.ListItems[num_list] = new List<long>();
                            }
                            container.ListItems[num_list].Add(code_tovar);

                            // Инициализируем счетчики для каждого списка акций
                            if (!container.ListQuantities.ContainsKey(num_list))
                            {
                                container.ListQuantities[num_list] = 0;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                allActionData2 = null;
            }

            return allActionData2;
        }

    }
}
