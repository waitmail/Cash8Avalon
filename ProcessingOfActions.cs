using Avalonia.Controls;
using Avalonia.Layout;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cash8Avalon.Cash_check;


namespace Cash8Avalon
{
    public partial class ProcessingOfActions
    {

        public DataTable dt = new DataTable();
        public DataTable dt_copy = new DataTable();//эта таблица необходима для временного хранения строк которые позднее будут добавлены в осносную базу в тот момент когда идет перебор строк основной таблицы в нее добавлять строки нельзя
        //private DataTable dt_gift = new DataTable();
        public string client_code = "";
        //public int action_num_doc = 0;
        public ArrayList action_barcode_list = new ArrayList();//Доступ из формы ввода акционного штрихкода 
        public bool inpun_action_barcode = false;//Доступ из формы ввода акционного штрихкода
        public bool have_action = false;
        public decimal discount = 0;
        public bool show_messages = false;
        public Cash_check cc = null;

        public ProcessingOfActions()
        {

        }

        private bool actions_birthday()
        {

            bool result = false;

            if (client_code == "")
            {
                return result;
            }

            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT COUNT(*)FROM clients WHERE code='" + client_code + "' AND date_part('month',date_of_birth)=" + DateTime.Now.Date.Month +
                    " AND  date_part('day',date_of_birth) BETWEEN " + DateTime.Now.Date.AddDays(-1).Day.ToString() + " AND " + DateTime.Now.Date.AddDays(1).Day.ToString() +
                    " AND date_of_birth<>'01.01.1900'";

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                if (Convert.ToInt16(command.ExecuteScalar()) != 0)
                {
                    result = true;
                }
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message, "Акция день рождения");
                result = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Акция день рождения");
                result = false;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            return result;
        }

        private bool check_and_create_checks_table_temp()
        {
            bool result = true;
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {

                conn.Open();
                //string query = "select COUNT(*) from information_schema.tables 		where table_schema='public' 	and table_name='checks_table_temp'";
                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                command.CommandText = "select COUNT(*) from information_schema.tables 		where table_schema='public' 	and table_name='checks_table_temp'	";
                if (Convert.ToInt16(command.ExecuteScalar()) == 0)
                {
                    command.CommandText = "CREATE TABLE checks_table_temp( tovar integer)WITH (  OIDS=FALSE);ALTER TABLE checks_table_temp  OWNER TO postgres;";

                }
                else
                {
                    command.CommandText = "DROP TABLE checks_table_temp;CREATE TABLE checks_table_temp( tovar integer)WITH (  OIDS=FALSE);ALTER TABLE checks_table_temp  OWNER TO postgres;";
                }

                command.ExecuteNonQuery();

                StringBuilder sb = new StringBuilder();
                /*foreach (ListViewItem lvi in listView1.Items)
                {
                    sb.Append("INSERT INTO checks_table_temp(tovar)VALUES (" + lvi.SubItems[0].Text + ");");
                }*/
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("INSERT INTO checks_table_temp(tovar)VALUES (" + row["tovar_code"].ToString() + ");");
                }

                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();

                conn.Close();

            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
                result = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                result = false;

            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }

        public DataTable CreateDataTableFromProducts(List<ProductItem> products)
        {
            // Получаем готовую структуру таблицы
            DataTable dt = MainStaticClass.CreateDataTableForActions();
            dt.Clear(); // Очищаем если есть данные

            foreach (var product in products)
            {
                DataRow row = dt.NewRow();

                // Заполняем данные из ProductItem
                row["tovar_code"] = (double)product.Code;
                row["tovar_name"] = product.Tovar;
                row["characteristic_code"] = DBNull.Value; // Пусто
                row["characteristic_name"] = DBNull.Value; // Пусто
                row["quantity"] = (double)product.Quantity;
                row["price"] = product.Price;
                row["price_at_discount"] = product.PriceAtDiscount;
                row["sum_full"] = product.Sum;
                row["sum_at_discount"] = product.SumAtDiscount;
                row["action"] = product.Action;
                row["gift"] = product.Gift;
                row["action2"] = product.Action2;
                row["bonus_reg"] = 0; // По умолчанию
                row["bonus_action"] = 0; // По умолчанию
                row["bonus_action_b"] = 0; // По умолчанию
                row["marking"] = product.Mark;
                row["promo_description"] = DBNull.Value; // Пусто

                dt.Rows.Add(row);
            }

            return dt;
        }

        public List<ProductItem> CreateProductsFromDataTable(DataTable dt)
        {
            var products = new List<ProductItem>();

            foreach (DataRow row in dt.Rows)
            {
                var product = new ProductItem
                {
                    Code = Convert.ToInt32(row["tovar_code"]),
                    Tovar = row["tovar_name"].ToString(),
                    Quantity = Convert.ToDecimal(row["quantity"]),
                    Price = Convert.ToDecimal(row["price"]),
                    PriceAtDiscount = Convert.ToDecimal(row["price_at_discount"]),
                    Sum = Convert.ToDecimal(row["sum_full"]),
                    SumAtDiscount = Convert.ToDecimal(row["sum_at_discount"]),
                    Action = Convert.ToInt32(row["action"]),
                    Gift = Convert.ToInt32(row["gift"]),
                    Action2 = Convert.ToInt32(row["action2"]),
                    Mark = row["marking"].ToString()
                };

                products.Add(product);
            }

            return products;
        }

        ///// <summary>
        ///// Создаем таблицу значений в которую помещаем данные листвью
        ///// обработка акций далее будет происходить над этим новым объектом
        ///// </summary>
        ///// <param name="dt"></param>
        ///// <returns></returns>
        //public DataTable[] to_process_actions(DataTable dt)
        //{
        //    to_define_the_action_dt(false);//вызываем обработку акций
        //    DataTable[] dt_tables = new DataTable[2];
        //    dt_tables[0] = dt;
        //    //dt_tables[1] = dt_gift;

        //    return dt_tables;
        //}


        /// <summary>
        /// Подсчет суммы документа
        /// 
        /// </summary>
        /// <returns></returns>
        private decimal calculation_of_the_sum_of_the_document_dt()
        {
            decimal total = 0;
            foreach (DataRow row in dt.Rows)
            {
                total += Convert.ToDecimal(row["sum_at_discount"]);
            }
            /* foreach (DataRow row in dt_gift.Rows)
             {
                 total += Convert.ToDecimal(row["sum_at_discount"]);
             }*/
            return total;
        }

        /*Поиск товара по штрихкоду
       * и добвление его в табличную часть
       * стандартное добавление товара
       */
        public void find_barcode_or_code_in_tovar_dt(string barcode)
        {
            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;
                if (barcode.Length > 6)
                {
                    command.CommandText = "select tovar.code AS tovar_code,tovar.name AS tovar_name,tovar.retail_price AS retail_price,characteristic.name AS characteristic_name,characteristic.guid AS characteristic_guid," +
                        " characteristic.retail_price_characteristic AS retail_price_characteristic ,tovar.its_certificate AS tovar_its_certificate" +
                        " from  barcode left join tovar ON barcode.tovar_code=tovar.code " +
                    " left join characteristic ON tovar.code = characteristic.tovar_code " +
                    " where barcode='" + barcode + "' AND its_deleted=0  AND (retail_price<>0 OR characteristic.retail_price_characteristic<>0) AND tovar.its_certificate=0";
                }
                else
                {
                    command.CommandText = "select tovar.code AS tovar_code,tovar.name AS tovar_name,tovar.retail_price AS retail_price, characteristic.name AS characteristic_name,characteristic.guid AS characteristic_guid," +
                        " characteristic.retail_price_characteristic AS retail_price_characteristic,tovar.its_certificate AS tovar_its_certificate " +
                        " FROM tovar left join characteristic  ON tovar.code = characteristic.tovar_code where tovar.its_deleted=0 AND (retail_price<>0 OR characteristic.retail_price_characteristic<>0) " +
                        " AND tovar.code='" + barcode + "' AND tovar.its_certificate=0";
                }

                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {

                    bool new_row = false;
                    DataRow[] findRow;
                    string expression = "code=" + reader[1].ToString(); // sql подобный запрос
                    findRow = dt.Select(expression);
                    DataRow row = null;
                    if (findRow.Length > 0)
                    {
                        row = findRow[0];
                    }
                    else
                    {
                        row = dt.NewRow();
                        new_row = true;
                    }

                    row["tovar_code"] = reader["tovar_code"].ToString();
                    row["tovar_name"] = reader["tovar_name"].ToString();
                    row["characteristic_code"] = reader["characteristic_guid"].ToString();
                    row["characteristic_name"] = reader["characteristic_name"].ToString();

                    row["quantity"] = "1";// lvi.SubItems[3].Text;Количество 1 без кратности
                    row["price"] = reader["retail_price"];
                    row["price_at_discount"] = reader["retail_price"]; //lvi.SubItems[5].Text; //?
                    row["sum_full"] = Convert.ToDecimal(row["sum_full"]) + Convert.ToDecimal(row["price"]) * Convert.ToDecimal(row["quantity"]); //lvi.SubItems[6].Text;
                    row["sum_at_discount"] = Convert.ToDecimal(row["sum_at_discount"]) + Convert.ToDecimal(row["price_at_discount"]) * Convert.ToDecimal(row["quantity"]);
                    if (new_row)
                    {
                        dt.Rows.Add(row);
                    }
                }

                reader.Close();
                //                 conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    // conn.Dispose();
                }
            }

            //write_new_document("0", "0", "0", "0", false, "0", "0", "0");
        }


        /// <summary>
        /// обработка акций вызывается в двух режимах
        /// 1. Без окна вызова ввода штрихкода Предварительный рассчет
        /// 2. С вызовом всех дополнительных окон, окончательный рассчет
        /// </summary>
        /// <param name="show_query_window_barcode"></param>
        public async Task to_define_the_action_dt()
        {

            if (!check_and_create_checks_table_temp())
            {
                return;
            }

            //total_seconnds = 0;
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            Int64 count_minutes = Convert.ToInt64((DateTime.Now - DateTime.Now.Date).TotalMinutes);
            short tip_action;// = 0;            
            decimal persent = 0;
            Int32 num_doc = 0;
            string comment = "";
            short marker = 0;
            decimal sum = 0;
            decimal sum1 = 0;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "SELECT tip,num_doc,persent,comment,code_tovar,sum,barcode,marker,execution_order,sum1 FROM action_header " +
                    " WHERE '" + DateTime.Now.Date.ToString("yyy-MM-dd") + "' between date_started AND date_end " +
                    " AND " + count_minutes.ToString() + " between time_start AND time_end  AND tip<>10 AND kind=0 AND num_doc in(" +
                    " SELECT DISTINCT action_table.num_doc FROM checks_table_temp " +
                    " LEFT JOIN action_table ON checks_table_temp.tovar = action_table.code_tovar)  order by execution_order asc, tip asc ";

                command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    //listView1.Focus();
                    if (reader.GetString(6).Trim().Length != 0)
                    {
                        continue;
                    }

                    tip_action = Convert.ToInt16(reader["tip"]);
                    persent = Convert.ToDecimal(reader["persent"]);
                    num_doc = Convert.ToInt32(reader["num_doc"]);
                    comment = reader["comment"].ToString().Trim();
                    marker = Convert.ToInt16(reader["marker"]);
                    sum = Convert.ToDecimal(reader["sum"]);
                    sum1 = Convert.ToDecimal(reader["sum1"]);

                    /* Обработать акцию по типу 1
                    * первый тип это скидка на конкретный товар
                    * если есть процент скидки то дается скидка 
                    * иначе выдается сообщение о подарке*/
                    if (tip_action == 1)
                    {
                        //start_action = DateTime.Now;
                        if (persent != 0)
                        {
                            //action_1_dt(num_doc, persent, comment);//Дать скидку на эту позицию  
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                await action_1_dt(num_doc, persent, comment);
                            }
                            else
                            {
                                await action_1_dt(num_doc, persent, comment, LoadActionDataInMemory.AllActionData1);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{
                            // action_1_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            //}
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                await action_1_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            }
                            else
                            {
                                await action_1_dt(num_doc, comment, marker, show_messages, LoadActionDataInMemory.AllActionData1); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    else if (tip_action == 2)
                    {
                        //start_action = DateTime.Now;

                        if (persent != 0)
                        {
                            if (LoadActionDataInMemory.AllActionData2 == null)
                            {
                                await action_2_dt(num_doc, persent, comment);
                            }
                            else
                            {
                                await action_2_dt(num_doc, persent, comment, LoadActionDataInMemory.AllActionData2);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{
                            //action_2_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            //}
                            if (LoadActionDataInMemory.AllActionData2 == null)
                            {
                                await action_2_dt(num_doc, comment, show_messages);
                            }
                            else
                            {
                                await action_2_dt(num_doc, comment, show_messages, LoadActionDataInMemory.AllActionData2);
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());

                    }
                    else if (tip_action == 3)
                    {
                        //start_action = DateTime.Now;                        
                        if (persent != 0)
                        {
                            //action_3_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                await action_3_dt(num_doc, persent, sum, comment);
                            }
                            else
                            {
                                await action_3_dt(num_doc, persent, sum, comment, LoadActionDataInMemory.AllActionData1);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{
                            //action_3_dt(num_doc, comment, sum, marker,show_messages); //Сообщить о подарке                           
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                await action_3_dt(num_doc, comment, sum, marker, show_messages);
                            }
                            else
                            {
                                await action_3_dt(num_doc, comment, sum, marker, show_messages, LoadActionDataInMemory.AllActionData1);
                            }
                            //}
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());

                    }
                    else if (tip_action == 4)
                    {
                        //start_action = DateTime.Now;

                        if (persent != 0)
                        {
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                await action_4_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                            }
                            else
                            {
                                await action_4_dt(num_doc, persent, sum, comment, LoadActionDataInMemory.AllActionData1);//Дать скидку на все позиции из списка позицию                                                 
                            }
                        }
                        else
                        {
                            if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            {
                                if (LoadActionDataInMemory.AllActionData1 == null)
                                {
                                    await action_4_dt(num_doc, comment, sum, show_messages);
                                }
                                else
                                {
                                    await action_4_dt(num_doc, comment, sum, show_messages, LoadActionDataInMemory.AllActionData1);
                                }
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    else if (tip_action == 6)
                    {           //Номер документа  //Сообщение о подарке //Сумма в данном случае шаг акции
                        //start_action = DateTime.Now;
                        if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                        {
                            action_6_dt(num_doc, comment, sum, marker);
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    else if (tip_action == 8)
                    {
                        //start_action = DateTime.Now;
                        if (persent != 0)
                        {
                            if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            {
                                await action_8_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                            }
                        }
                        else
                        {
                            if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            {
                                await action_8_dt(num_doc, comment, sum, marker);
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    else if (tip_action == 9)//Акция работает в день рождения владельца дисконтной карты
                    {
                        //start_action = DateTime.Now;
                        if (!actions_birthday())
                        {
                            //write_time_execution("проверка на день рождения", tip_action.ToString());
                            continue;
                        }

                        //if (reader.GetDecimal(2) != 0)
                        if (persent != 0)
                        {
                            await action_1_dt(num_doc, persent, comment);
                        }
                        else
                        {
                            //action_1_dt(reader.GetInt32(1), reader.GetString(3), reader.GetInt16(7), reader.GetInt32(4)); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    //else if (tip_action == 10)
                    //{
                    //    if (sum <= calculation_of_the_sum_of_the_document_dt())
                    //    {
                    //        //MessageBox.Show(reader[3].ToString());
                    //        action_num_doc = num_doc; //Convert.ToInt32(reader["num_doc"].ToString());
                    //    }
                    //}
                    else if (tip_action == 12)
                    {

                        await action_12_dt(num_doc, persent, sum, sum1);
                    }
                    else if (tip_action == 13)
                    {
                        action_13_dt(num_doc);
                    }
                    else
                    {
                        await MessageBox.Show("Неопознанный тип акции в документе  № " + reader["num_doc"].ToString(), " Обработка акций ");
                        MainStaticClass.WriteRecordErrorLog("Неопознанный тип акции в документе  № " + reader["num_doc"].ToString(), "to_define_the_action_dt", num_doc, MainStaticClass.CashDeskNumber, "Основная обработка акций");
                    }
                }
                reader.Close();

                if (show_messages)
                {
                    //decimal divisor = action_10_dt(Convert.ToInt32(reader["num_doc"]));
                    //if (divisor > 0)
                    //{
                    query = "SELECT tip,num_doc,persent,comment,code_tovar,sum,barcode,marker,execution_order FROM action_header " +
                     " WHERE '" + DateTime.Now.Date.ToString("yyy-MM-dd") + "' between date_started AND date_end " +
                     " AND " + count_minutes.ToString() + " between time_start AND time_end AND bonus_promotion=0 " +
                     " AND barcode='' AND tip=10 AND num_doc in(" +//AND tip<>10 
                     " SELECT DISTINCT action_table.num_doc FROM checks_table_temp " +
                     " LEFT JOIN action_table ON checks_table_temp.tovar = action_table.code_tovar) order by execution_order asc, tip asc";//date_started asc,, tip desc

                    command = new NpgsqlCommand(query, conn);
                    reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        decimal divisor = await action_10_dt(Convert.ToInt32(reader["num_doc"]));
                        //if (Convert.ToDecimal(reader["sum"]) <= action_10_dt(Convert.ToInt32(reader["num_doc"])))
                        if ((Convert.ToDecimal(reader["sum"]) <= divisor) && (divisor > 0))
                        {
                            //int multiplicity = (int)(calculation_of_the_sum_of_the_document_dt() / action_10_dt(Convert.ToInt32(reader["num_doc"])));
                            int multiplicity = (int)(calculation_of_the_sum_of_the_document_dt() / divisor);
                            await MessageBox.Show("Кратность " + multiplicity.ToString() + " " + reader["comment"].ToString());
                            int num = Convert.ToInt32(reader["num_doc"]);
                            if (cc != null)
                            {
                                if (!cc.action_num_doc.Contains(num))
                                {

                                    cc.action_num_doc.Add(num);
                                }
                            }
                        }
                    }

                    reader.Close();
                    //}
                    conn.Close();
                    command.Dispose();
                }

                if (show_messages)
                {
                    await checked_action_10_dt();//Отдельная проверка поскольку может не быть товарной части, а все акции выше проверяются именно на вхождение товаров документа в таб части акционных документов
                }

            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "ошибка при обработке акций");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
            //MessageBox.Show(total_seconds.ToString());
        }

        /// <summary>
        /// Это сработка акций по группе клиентов
        /// 
        /// </summary>
        public async Task to_define_the_action_personal_dt(string code_client)
        {

            if (!check_and_create_checks_table_temp())
            {
                return;
            }

            //total_seconnds = 0;
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            short tip_action;// = 0;
            decimal persent = 0;
            Int32 num_doc = 0;
            string comment = "";
            short marker = 0;
            decimal sum = 0;
            Int64 count_minutes = Convert.ToInt64((DateTime.Now - DateTime.Now.Date).TotalMinutes);
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                //string query = "SELECT tip,num_doc,persent,comment,code_tovar,sum,barcode,marker,execution_order FROM action_header " +
                string query = "SELECT tip,num_doc,persent,comment,sum,barcode,marker,execution_order FROM action_header " +
                    " WHERE '" + DateTime.Now.Date.ToString("yyy-MM-dd") + "' between date_started AND date_end " +
                    " AND " + count_minutes.ToString() + " between time_start AND time_end  AND kind=2 AND num_doc in(" +
                    " (SELECT DISTINCT action_table.num_doc FROM checks_table_temp " +
                    " LEFT JOIN action_table ON checks_table_temp.tovar = action_table.code_tovar " +
                    " WHERE  action_table.num_doc in(SELECT num_doc	FROM action_clients where code_client='" + code_client + "')))  order by execution_order asc, tip asc ";

                command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    //listView1.Focus();
                    if (reader["barcode"].ToString().Trim().Length != 0)
                    {
                        continue;
                    }

                    tip_action = Convert.ToInt16(reader["tip"]);
                    persent = Convert.ToDecimal(reader["persent"]);
                    num_doc = Convert.ToInt32(reader["num_doc"]);
                    comment = reader["comment"].ToString();
                    marker = Convert.ToInt16(reader["marker"]);
                    sum = Convert.ToDecimal(reader["sum"]);
                    /* Обработать акцию по типу 1
                    * первый тип это скидка на конкретный товар
                    * если есть процент скидки то дается скидка 
                    * иначе выдается сообщение о подарке*/
                    if (tip_action == 1)
                    {
                        //start_action = DateTime.Now;
                        if (persent != 0)
                        {
                            //action_1_dt(num_doc, persent, comment);//Дать скидку на эту позицию  
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                await action_1_dt(num_doc, persent, comment);
                            }
                            else
                            {
                                await action_1_dt(num_doc, persent, comment, LoadActionDataInMemory.AllActionData1);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{
                            // action_1_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            //}
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                await action_1_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            }
                            else
                            {
                                await action_1_dt(num_doc, comment, marker, show_messages, LoadActionDataInMemory.AllActionData1); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    else if (tip_action == 2)
                    {
                        //start_action = DateTime.Now;

                        if (persent != 0)
                        {
                            if (LoadActionDataInMemory.AllActionData2 == null)
                            {
                                await action_2_dt(num_doc, persent, comment);
                            }
                            else
                            {
                                await action_2_dt(num_doc, persent, comment, LoadActionDataInMemory.AllActionData2);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{                         
                            //action_2_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок                          
                            //}
                            if (LoadActionDataInMemory.AllActionData2 == null)
                            {
                                await action_2_dt(num_doc, comment, show_messages);
                            }
                            else
                            {
                                await action_2_dt(num_doc, comment, show_messages, LoadActionDataInMemory.AllActionData2);
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());

                    }
                    else if (tip_action == 3)
                    {
                        //start_action = DateTime.Now;

                        //action_2(reader.GetInt32(1));
                        if (persent != 0)
                        {
                            await action_3_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{                                
                            await action_3_dt(num_doc, comment, sum, marker, show_messages); //Сообщить о подарке                           
                            //}
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());

                    }
                    else if (tip_action == 4)
                    {
                        //start_action = DateTime.Now;

                        if (persent != 0)
                        {
                            await action_4_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{                            
                            await action_4_dt(num_doc, comment, sum, show_messages);
                            //}
                        }
                    }
                    else
                    {
                        await MessageBox.Show("Неопознанный тип акции в документе  № " + reader["num_doc"].ToString(), " Обработка акций ");
                    }
                }
                reader.Close();

                conn.Close();
                command.Dispose();

            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "ошибка при обработке акций");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
            //MessageBox.Show(total_seconnds.ToString());
        }
        
        /// <summary>
        /// Для Евы напоминание о подарке
        /// После сработки всех акций 
        /// 1.Сначала в табличной части проверяем товары которые не участвовали в акциях
        /// 2.Формируем список акций 2 типа(скидочные) которые в периоде действия и в них максимальный номер списка=2
        /// 3.Последовательно проверяем свободные товары на вхождение во 2-й список этих акций подокументно и выводим первые 10 позиций 1 списка в сообщение 
        /// </summary>
        /// <param name="num_doc"></param>
        private void load_list1_action2_dt()
        {
            string list_code_tovar = "";

            foreach (DataRow row in dt.Rows)
            {
                //index++;
                if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем                  
                {
                    continue;
                }

                list_code_tovar += row["tovar_code"].ToString() + ",";
            }

            if (list_code_tovar == "")
            {
                return;
            }
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT action_header.num_doc FROM action_header " +
                    " LEFT JOIN action_table ON action_header.num_doc = action_table.num_doc " +
                    " WHERE tip = 2 AND  '" + DateTime.Now.ToString("dd-MM-yyyy") + "' between date_started AND date_end " +
                    " AND action_table.code_tovar in (" + list_code_tovar.Substring(0, list_code_tovar.Length - 1) +
                    " ) AND action_table.num_list = 2 GROUP BY action_header.num_doc HAVING MAX(action_table.num_list)=2 ";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    show_list1_dt(reader[0].ToString());
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Ошибка при подсказке по 2 акции" + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при подсказке по 2 акции" + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// вывести сообщение по 1 списку в акции 2 типа 
        /// </summary>
        /// <param name="num_doc"></param>
        private async Task show_list1_dt(string num_doc)
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT tovar.name FROM public.action_table LEFT JOIN tovar ON action_table.code_tovar = tovar.code where num_doc=" + num_doc + " and num_list= 1 limit 10";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                string result = "";
                while (reader.Read())
                {
                    result += reader[0].ToString().Trim() + "\r\n";
                }
                if (result != "")
                {
                    //MessageBox.Show(result,"Для сработки акции необходимо добавить любую позицию ИЗ ... ");
                    //MyMessageBox myMessageBox = new MyMessageBox();
                    //myMessageBox.text_message.Text = result;
                    //myMessageBox.Text = " Для сработки акции необходимо добавить любую позицию ИЗ ... ";
                    //myMessageBox.text_message.TextAlign = HorizontalAlignment.Left;
                    //myMessageBox.ShowDialog();
                    await MessageBox.Show(" Для сработки акции необходимо добавить любую позицию ИЗ ...\r\n" + result,"Подсказка по акции 2 типа",MessageBoxButton.OK,MessageBoxType.Info);
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show("Ошибка при подсказке по 2 акции" + ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show("Ошибка при подсказке по 2 акции" + ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// обработка акций вызывается в двух режимах
        /// 1. Без окна вызова ввода штрихкода Предварительный рассчет
        /// 2. С вызовом всех дополнительных окон, окончательный рассчет
        /// </summary>
        /// <param name="show_query_window_barcode"></param>
        public void to_define_the_action_dt(string barcode)
        {

            if (!check_and_create_checks_table_temp())
            {
                return;
            }

            //total_seconnds = 0;
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            short tip_action;// = 0;
            decimal persent = 0;
            Int32 num_doc = 0;
            string comment = "";
            short marker = 0;
            decimal sum = 0;

            Int64 count_minutes = Convert.ToInt64((DateTime.Now - DateTime.Now.Date).TotalMinutes);
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                //string query = "SELECT tip,num_doc,persent,comment,code_tovar,sum,barcode,marker,action_by_discount FROM action_header WHERE '" + DateTime.Now.Date.ToString("yyy-MM-dd") + "' between date_started AND date_end AND barcode='" + barcode + "' AND kind = 1";
                string query = "SELECT tip,num_doc,persent,comment,sum,barcode,marker,action_by_discount FROM action_header WHERE '" +
                    DateTime.Now.Date.ToString("yyy-MM-dd") + "' between date_started AND date_end AND barcode='" + barcode + "' AND kind = 1";

                command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {

                    tip_action = Convert.ToInt16(reader["tip"]);
                    persent = Convert.ToDecimal(reader["persent"]);
                    num_doc = Convert.ToInt32(reader["num_doc"]);
                    comment = reader["comment"].ToString();
                    marker = Convert.ToInt16(reader["marker"]);
                    sum = Convert.ToDecimal(reader["sum"]);
                    /* Обработать акцию по типу 1
                    * первый тип это скидка на конкретный товар
                    * если есть процент скидки то дается скидка 
                    * иначе выдается сообщение о подарке*/
                    if (tip_action == 1)
                    {
                        //start_action = DateTime.Now;
                        if (persent != 0)
                        {
                            //action_1_dt(num_doc, persent, comment);//Дать скидку на эту позицию  
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                action_1_dt(num_doc, persent, comment);
                            }
                            else
                            {
                                action_1_dt(num_doc, persent, comment, LoadActionDataInMemory.AllActionData1);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{
                            // action_1_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            //}
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                action_1_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            }
                            else
                            {
                                action_1_dt(num_doc, comment, marker, show_messages, LoadActionDataInMemory.AllActionData1); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    else if (tip_action == 2)
                    {
                        //start_action = DateTime.Now;

                        if (persent != 0)
                        {
                            if (LoadActionDataInMemory.AllActionData2 == null)
                            {
                                action_2_dt(num_doc, persent, comment);
                            }
                            else
                            {
                                action_2_dt(num_doc, persent, comment, LoadActionDataInMemory.AllActionData2);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{                         
                            //action_2_dt(num_doc, comment, marker, show_messages); //Сообщить о подарке, а так же добавить товар в подарок если указан код товара                          
                            //}
                            if (LoadActionDataInMemory.AllActionData2 == null)
                            {
                                action_2_dt(num_doc, comment, show_messages);
                            }
                            else
                            {
                                action_2_dt(num_doc, comment, show_messages, LoadActionDataInMemory.AllActionData2);
                            }
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());

                    }
                    else if (tip_action == 3)
                    {
                        ////////start_action = DateTime.Now;

                        ////////action_2(reader.GetInt32(1));
                        //////if (persent != 0)
                        //////{                                                                        
                        //////    action_3_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                        //////}
                        //////else
                        //////{
                        //////    //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                        //////    //{                                
                        //////        action_3_dt(num_doc, comment, sum, marker, show_messages); //Сообщить о подарке                           
                        //////    //}
                        //////}
                        ////////write_time_execution(reader[1].ToString(), tip_action.ToString());
                        //start_action = DateTime.Now;                        
                        if (persent != 0)
                        {
                            //action_3_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                action_3_dt(num_doc, persent, sum, comment);
                            }
                            else
                            {
                                action_3_dt(num_doc, persent, sum, comment, LoadActionDataInMemory.AllActionData1);
                            }
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{
                            //action_3_dt(num_doc, comment, sum, marker,show_messages); //Сообщить о подарке                           
                            if (LoadActionDataInMemory.AllActionData1 == null)
                            {
                                action_3_dt(num_doc, comment, sum, marker, show_messages);
                            }
                            else
                            {
                                action_3_dt(num_doc, comment, sum, marker, show_messages, LoadActionDataInMemory.AllActionData1);
                            }
                            //}
                        }

                    }
                    else if (tip_action == 4)
                    {
                        //start_action = DateTime.Now;

                        if (persent != 0)
                        {
                            action_4_dt(num_doc, persent, sum, comment);//Дать скидку на все позиции из списка позицию                                                 
                        }
                        else
                        {
                            //if (show_messages)//В этой акции в любом случае всплывающие окна, в предварительном рассчете она не будет участвовать
                            //{                            
                            action_4_dt(num_doc, comment, sum, show_messages);
                            //}
                        }
                        //write_time_execution(reader[1].ToString(), tip_action.ToString());
                    }
                    else if (tip_action == 5)
                    {
                        action_5_dt(num_doc, sum, comment);
                    }
                    else
                    {
                        MessageBox.Show("Неопознанный тип акции в документе  № " + reader[1].ToString(), " Обработка акций ");
                    }
                }
                reader.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ошибка при обработке акций");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    // conn.Dispose();
                }
            }
            //MessageBox.Show(total_seconnds.ToString());
        }

        /*Пометка товарных позиций которые участвовали в акции
        * для того чтобы они не участвовали в следующих акциях
        */
        private void marked_action_tovar_dt(DataTable dtCopy, int num_doc, string comment)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                foreach (DataRow row in dtCopy.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем
                    {
                        continue;
                    }
                    string query = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"].ToString() + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query, conn);
                    Int16 result = Convert.ToInt16(command.ExecuteScalar());
                    if (result == 1)
                    {
                        row["action2"] = num_doc.ToString();
                        if (dt.Columns.Contains("promo_description"))
                        {
                            row["promo_description"] = comment;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "пометка товарных позиций участвующих в акции");
            }

            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        /*Пометка товарных позиций которые участвовали в акции
       * для того чтобы они не участвовали в следующих акциях
       */
        private void marked_action_tovar_dt(int num_doc, string comment)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем
                    {
                        continue;
                    }
                    string query = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"].ToString() + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query, conn);
                    Int16 result = Convert.ToInt16(command.ExecuteScalar());
                    if (result == 1)
                    {
                        row["action2"] = num_doc.ToString();
                        if (dt.Columns.Contains("promo_description"))
                        {
                            row["promo_description"] = comment;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "пометка товарных позиций участвующих в акции");
            }

            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }


        /// <summary>
        /// Пометка товарных позиций, которые участвовали в акции,
        /// чтобы они не участвовали в следующих акциях.
        /// </summary>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="actionPricesByDoc">Словарь с данными о товарах и их ценах по документам.</param>
        private void marked_action_tovar_dt(int num_doc, string comment, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            try
            {
                // Проверяем, есть ли данные для текущего документа в словаре
                if (actionPricesByDoc.ContainsKey(num_doc))
                {
                    // Получаем словарь с товарами для текущего документа
                    var tovarPrices = actionPricesByDoc[num_doc];

                    // Проходим по всем строкам в DataTable (предполагается, что dt — это DataTable)
                    foreach (DataRow row in dt.Rows)
                    {
                        // Пропускаем товары, которые уже участвовали в акции
                        if (Convert.ToInt32(row["action2"]) > 0)
                        {
                            continue;
                        }

                        // Получаем код товара из строки
                        long tovarCode = Convert.ToInt64(row["tovar_code"]);

                        // Проверяем, есть ли товар в словаре для текущего документа
                        if (tovarPrices.ContainsKey(tovarCode))
                        {
                            // Если товар участвовал в акции, помечаем его
                            row["action2"] = num_doc.ToString();

                            // Добавляем комментарий, если есть соответствующая колонка
                            if (dt.Columns.Contains("promo_description"))
                            {
                                row["promo_description"] = comment;
                            }
                        }
                    }
                }
                else
                {
                    // Если данных для текущего документа нет, можно вывести предупреждение
                    MessageBox.Show($"Данные для документа {num_doc} отсутствуют в словаре.");
                    MainStaticClass.WriteRecordErrorLog("Данные для документа {num_doc} отсутствуют в словаре.", "marked_action_tovar_dt(int num_doc, string comment, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)", num_doc, MainStaticClass.CashDeskNumber, "Отметка позиций уже участовавших в акции");
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                MessageBox.Show(ex.Message, "Ошибка при пометке товарных позиций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Отметка позиций уже участовавших в акции");
            }
        }

        /// <summary>
        /// Пометка товарных позиций, которые участвовали в акции,
        /// чтобы они не участвовали в следующих акциях.
        /// </summary>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="actionPricesByDoc">Словарь с данными о товарах и их ценах по документам.</param>
        private async Task marked_action_tovar_dt(DataTable dtCopy, int num_doc, string comment, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            try
            {
                // Проверяем, есть ли данные для текущего документа в словаре
                if (actionPricesByDoc.ContainsKey(num_doc))
                {
                    // Получаем словарь с товарами для текущего документа
                    var tovarPrices = actionPricesByDoc[num_doc];

                    // Проходим по всем строкам в DataTable (предполагается, что dt — это DataTable)
                    foreach (DataRow row in dtCopy.Rows)
                    {
                        // Пропускаем товары, которые уже участвовали в акции
                        if (Convert.ToInt32(row["action2"]) > 0)
                        {
                            continue;
                        }

                        // Получаем код товара из строки
                        long tovarCode = Convert.ToInt64(row["tovar_code"]);

                        // Проверяем, есть ли товар в словаре для текущего документа
                        if (tovarPrices.ContainsKey(tovarCode))
                        {
                            // Если товар участвовал в акции, помечаем его
                            row["action2"] = num_doc.ToString();

                            // Добавляем комментарий, если есть соответствующая колонка
                            if (dt.Columns.Contains("promo_description"))
                            {
                                row["promo_description"] = comment;
                            }
                        }
                    }
                }
                else
                {
                    // Если данных для текущего документа нет, можно вывести предупреждение
                    await MessageBox.Show($"Данные для документа {num_doc} отсутствуют в словаре.");
                    MainStaticClass.WriteRecordErrorLog($"Данные для документа {num_doc} отсутствуют в словаре.", "marked_action_tovar_dt(int num_doc, string comment, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)", num_doc, MainStaticClass.CashDeskNumber, "Отметка позиций уже участовавших в акции");
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                await MessageBox.Show(ex.Message, "Ошибка при пометке товарных позиций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Отметка позиций уже участвовавших в акции");
            }
        }




        ///// <summary>
        ///// Возвращает true если в табличной части условия акций есть строки и false
        ///// если строк нет
        ///// </summary>
        ///// <param name="num_action_doc"></param>
        ///// <returns></returns>
        private async Task<bool> CheckedActionTableRows(int num_action_doc)
        {
            bool result = false;

            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            Int64 count_minutes = Convert.ToInt64((DateTime.Now - DateTime.Now.Date).TotalMinutes);
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = "SELECT COUNT(*) FROM action_table WHERE num_doc=" + num_action_doc.ToString();
                command = new NpgsqlCommand(query, conn);
                int result_query = Convert.ToInt32(command.ExecuteScalar());
                if (result_query > 0)
                {
                    result = true;
                }
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            return result;
        }


        private async Task checked_action_10_dt()
        {

            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            Int64 count_minutes = Convert.ToInt64((DateTime.Now - DateTime.Now.Date).TotalMinutes);
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = "SELECT tip,num_doc,persent,comment,code_tovar,sum,barcode,marker FROM action_header " +
                    " WHERE '" + DateTime.Now.Date.ToString("yyy-MM-dd") + "' between date_started AND date_end " +
                    " AND " + count_minutes.ToString() + " between time_start AND time_end and tip = 10";
                command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (await CheckedActionTableRows(Convert.ToInt32(reader[1].ToString())))
                    {
                        continue;
                    }

                    if (reader.GetDecimal(5) <= calculation_of_the_sum_of_the_document_dt())// action_10(Convert.ToInt32(reader["num_doc"]))
                    {
                        int multiplicity = (int)(calculation_of_the_sum_of_the_document_dt() / reader.GetDecimal(5));
                        await MessageBox.Show("Кратность " + multiplicity.ToString() + " " + reader[3].ToString());
                        //action_num_doc = Convert.ToInt32(reader[1].ToString());
                        int num = Convert.ToInt32(reader["num_doc"]);
                        if (!cc.action_num_doc.Contains(num))
                        {
                            cc.action_num_doc.Add(num);
                        }
                    }
                }
                reader.Close();
                conn.Close();
                command.Dispose();

            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }


        /// <summary>
        /// Это 10 акция для тех акционных документов у которых есть строки и 
        /// здесь происходит проверка по сумме на вхождение строк документа чек
        /// в строки акционного документа
        /// 
        /// </summary>
        /// <param name="num_doc"></param>
        /// <returns></returns>
        private async Task<Decimal> action_10_dt(int num_doc)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            Decimal result = 0;
            //bool is_found = false;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query = "";
                foreach (DataRow row in dt.Rows)
                {
                    query = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"].ToString() + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query, conn);
                    if (Convert.ToInt16(command.ExecuteScalar()) == 1)//вхождение найдено 
                    {
                        result += Convert.ToDecimal(row["sum_at_discount"].ToString());
                        //is_found = true;
                    }
                }
                conn.Close();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            //if (is_found)
            //{
            //    result = calculation_of_the_sum_of_the_document(); 
            //}

            return result;
        }
        
        /// Обработка акций 13 типа
        /// </summary>
        /// <param name="num_doc"></param>
        private async Task action_13_dt(int num_doc)
        {
            try
            {
                using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    // Создаем резервную копию таблицы
                    DataTable originalDt = dt.Copy();

                    try
                    {
                        // Группируем строки по code_tovar
                        var groupedRows = dt.AsEnumerable()
                            .GroupBy(row => row.Field<double>("tovar_code"))
                            .Where(group => group.All(row => row.Field<int>("action2") == 0)) // Товар не участвовал в других акциях
                            .ToList();

                        foreach (var group in groupedRows)
                        {
                            double codeTovar = group.Key;
                            double totalQuantity = group.Sum(row => row.Field<double>("quantity"));

                            decimal? actionPrice = GetPriceAction13(num_doc, codeTovar, totalQuantity, conn);
                            if (!actionPrice.HasValue) continue;

                            // Обновляем все строки с этим товаром
                            foreach (DataRow row in group)
                            {
                                decimal qty = Convert.ToDecimal(row.Field<double>("quantity"));
                                decimal price = row.Field<decimal>("price");

                                row["price_at_discount"] = actionPrice.Value;
                                row["sum_full"] = (qty * price).ToString();
                                //row["sum_at_discount"] = (Math.Ceiling((decimal)qty * actionPrice.Value * 100) / 100).ToString();//((decimal)qty * actionPrice.Value).ToString();
                                row["sum_at_discount"] = Math.Round((decimal)qty * actionPrice.Value, 2, MidpointRounding.AwayFromZero).ToString();
                                row["action"] = num_doc.ToString();
                                row["action2"] = num_doc.ToString();
                            }
                        }
                    }
                    catch
                    {
                        // Восстанавливаем исходное состояние таблицы при ошибке
                        dt.Clear();
                        foreach (DataRow row in originalDt.Rows)
                        {
                            dt.ImportRow(row);
                        }

                        throw; // Перебрасываем исключение дальше
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 13 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Обработка акций 13 типа");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 13 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Обработка акций 13 типа");
            }
        }

        public decimal? GetPriceAction13(int numDoc, double codeTovar, double quantity, NpgsqlConnection conn)
        {
            const string query = @"
        SELECT price 
        FROM action_table 
        WHERE num_doc = @numDoc 
          AND code_tovar = @codeTovar 
          AND num_list <= @quantity
        ORDER BY num_list DESC 
        LIMIT 1";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@numDoc", numDoc);
                cmd.Parameters.AddWithValue("@codeTovar", codeTovar);
                cmd.Parameters.AddWithValue("@quantity", quantity);

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : (decimal?)null;
            }
        }
        
        /*Поиск товара по штрихкоду
        * и добавление его в табличную часть
        * это подарочный товар
        * добавляется по нулевой цене
        * barcode это код или штрихкод товара
        * count это количество позиций
        * sum_null если true тогда сумма и сумма со скидкой 0 иначе как обычный товар
        * это для акции 
        */
        public async Task FindBarcodeOrCodeInTovarAction_dt(string barcode, int count, bool sum_null, int num_doc, int mode, DataTable dtCopy = null)
        {

            NpgsqlConnection conn = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;
                if (barcode.Length > 6)
                {
                    command.CommandText = "select tovar.code,tovar.name,tovar.retail_price from  barcode left join tovar ON barcode.tovar_code=tovar.code where barcode='" + barcode + "' AND tovar.its_deleted=0 ";
                }
                else
                {
                    command.CommandText = "select tovar.code,tovar.name,tovar.retail_price from  tovar where tovar.code='" + barcode + "' AND tovar.its_deleted=0 ";
                }

                NpgsqlDataReader reader = command.ExecuteReader();

                bool there_are_goods = false;//Флаг для понимания есть ли акционный товар
                DataRow row = null;
                while (reader.Read())
                {
                    if (dtCopy == null)
                    {
                        if (mode == 0)
                        {
                            row = dt.NewRow();
                        }
                        else
                        {
                            row = dt_copy.NewRow();
                        }
                    }
                    else
                    {
                        row = dtCopy.NewRow();
                    }

                    row["tovar_code"] = reader.GetInt64(0).ToString();//ListViewItem lvi = new ListViewItem(reader.GetInt32(0).ToString());                    
                    row["tovar_name"] = reader.GetString(1);//Наименование
                    row["characteristic_code"] = "";
                    row["characteristic_name"] = "";
                    row["quantity"] = count;//Количество
                    row["price"] = reader.GetDecimal(2);//Цена
                    string retail_price = GetGiftPrice(num_doc);
                    if (retail_price != "")
                    {
                        //row["price_at_discount"] = reader.GetDecimal(2);//Цена со скидкой    
                        row["price_at_discount"] = Decimal.Parse(retail_price);
                    }

                    //if (sum_null)//это пережиток
                    //{
                    //    row["sum_full"] = 0;// reader.GetDecimal(2);//Цена
                    //    row["sum_at_discount"] = 0;// reader.GetDecimal(2);//Цена со скидкой                            
                    //}
                    //else
                    //{
                    row["sum_full"] = Convert.ToDecimal(row["price"]) * Convert.ToDecimal(row["quantity"]);// lvi.SubItems.Add((Convert.ToDecimal(lvi.SubItems[2].Text) * Convert.ToDecimal(lvi.SubItems[3].Text)).ToString());//Сумма
                    row["sum_at_discount"] = Convert.ToDecimal(row["price_at_discount"]) * Convert.ToDecimal(row["quantity"]); //lvi.SubItems.Add((Convert.ToDecimal(lvi.SubItems[2].Text) * Convert.ToDecimal(lvi.SubItems[4].Text)).ToString()); //Сумма со скидкой                        
                    //}
                    row["action"] = 0;// lvi.SubItems.Add("0");
                    row["gift"] = num_doc;
                    row["action2"] = num_doc;
                    row["bonus_reg"] = 0;
                    row["bonus_action"] = 0;
                    row["bonus_action_b"] = 0;
                    row["marking"] = "0";
                    if (dtCopy == null)
                    {
                        if (mode == 0)
                        {
                            dt.Rows.Add(row);
                        }
                        else
                        {
                            dt_copy.Rows.Add(row);
                        }
                    }
                    else
                    {
                        dtCopy.Rows.Add(row);
                    }
                    there_are_goods = true;
                }

                reader.Close();
                conn.Close();
                if (!there_are_goods)
                {
                    await MessageBox.Show("ВНИМАНИЕ ПОДАРОК НЕ НАЙДЕН !!! СООБЩИТЕ АДМИНИСТРАТОРУ !!! ", "ОШИБКА ПРИ РАБОТЕ С АКЦИЯМИ");
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }

            }
        }
        private async Task<bool?> ShowQueryWindowBarcode(int call_type, int count, int num_doc, int mode)
        {
            bool? result = null;

            //InputActionBarcode ib = new InputActionBarcode();
            //ib.count = count;
            //ib.caller = this;
            //ib.call_type = call_type;
            //ib.num_doc = num_doc;
            InputActionBarcode dialog = null;

            try
            {
                dialog = new InputActionBarcode();
                dialog.count = count;
                dialog.num_doc = num_doc;
                dialog.mode = mode;
                dialog.call_type = 1;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.CanResize = false;
                dialog.SystemDecorations = SystemDecorations.None;
                dialog.Topmost = true;

                result = await dialog.ShowModalBlocking(cc);
                string enteredBarcode = dialog.EnteredBarcode; // Сохраняем значение
                if (result == true && !string.IsNullOrEmpty(enteredBarcode))
                {                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка: {ex.Message}");
                await MessageBox.Show($"Ошибка: {ex.Message}", "Поиск клиента", MessageBoxButton.OK, MessageBoxType.Error, cc);
            }
            finally
            {
                //dialog?.Close();
                //InputSearchProduct.Focus();
            }


            return result;
        }

        private async Task<bool?> ShowQueryWindowBarcode(int call_type, int count, int num_doc, int mode,DataTable dtCopy)
        {
            bool? result = null;

            //InputActionBarcode ib = new InputActionBarcode();
            //ib.count = count;
            //ib.caller = this;
            //ib.call_type = call_type;
            //ib.num_doc = num_doc;
            InputActionBarcode dialog = null;

            try
            {
                dialog = new InputActionBarcode();
                dialog.count = count;
                dialog.num_doc = num_doc;
                dialog.mode = mode;
                dialog.call_type = 1;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.CanResize = false;
                dialog.SystemDecorations = SystemDecorations.None;
                dialog.Topmost = true;

                result = await dialog.ShowModalBlocking(cc);
                string enteredBarcode = dialog.EnteredBarcode; // Сохраняем значение
                if (result == true && !string.IsNullOrEmpty(enteredBarcode))
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка: {ex.Message}");
                await MessageBox.Show($"Ошибка: {ex.Message}", "Поиск клиента", MessageBoxButton.OK, MessageBoxType.Error, cc);
            }
            finally
            {
                //dialog?.Close();
                //InputSearchProduct.Focus();
            }


            return result;
        }

        //private async Task<bool> show_query_window_barcode(int call_type, int count, int num_doc, int mode)
        //{
        //    InputActionBarcode ib = new InputActionBarcode();
        //    ib.count = count;
        //    //ib.caller2 = this;
        //    ib.call_type = call_type;
        //    ib.num_doc = num_doc;
        //    ib.mode = mode;

        //    return await ib.ShowDialog<bool>(cc);

        //}

        //private async Task<bool> show_query_window_barcode(int call_type, int count, int num_doc, int mode, DataTable dtCopy)
        //{
        //    InputActionBarcode ib = new InputActionBarcode();
        //    ib.count = count;
        //    //ib.caller2 = this;
        //    ib.call_type = call_type;
        //    ib.num_doc = num_doc;
        //    ib.mode = mode;
        //    ib.dtCopy = dtCopy;
        //    return await ib.ShowDialog<bool>(cc);
        //}
        
        /// <summary>                
        ///Обработать акцию по типу 1
        ///первый тип это скидка на конкретный товар
        ///если есть процент скидки то дается скидка 
        ///иначе выдается сообщение о подарке
        ///Здесь скидка чтение с диска
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="persent"></param>
        /// <param name="comment"></param>
        private async Task action_1_dt(int num_doc, decimal percent, string comment)
        {
            DataTable dtCopy = null; // Копия DataTable

            try
            {
                // Создаем копию DataTable перед началом обработки
                dtCopy = dt.Copy();

                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    var query = "SELECT code_tovar, price FROM action_table WHERE num_doc = @num_doc";
                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@num_doc", num_doc);
                        using (var reader = command.ExecuteReader())
                        {
                            var actionPrices = new Dictionary<long, decimal>();
                            while (reader.Read())
                            {
                                actionPrices[reader.GetInt64(0)] = reader.GetDecimal(1);
                            }

                            // Обрабатываем строки копии DataTable
                            ProcessRows(dtCopy, actionPrices, num_doc, percent, comment);

                            // Если все прошло успешно, заменяем оригинальный DataTable на измененную копию
                            dt = dtCopy;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка базы данных");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 1 типа акций");
            }
            finally
            {
                // Если произошла ошибка, dt остается неизменным
            }
        }


        private void ProcessRows(DataTable dtCopy, Dictionary<long, decimal> actionPrices, int num_doc, decimal percent, string comment)
        {
            foreach (DataRow row in dtCopy.Rows)
            {

                if (Convert.ToInt32(row["action2"]) > 0)
                {
                    continue;
                }

                //if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                //{
                //    continue;
                //}

                long tovarCode = Convert.ToInt64(row["tovar_code"]);
                if (actionPrices.TryGetValue(tovarCode, out var price))
                {
                    double priceAtDiscount;
                    if (price == 0)
                    {
                        priceAtDiscount = Math.Round(Convert.ToDouble(row["price"]) * (1 - (double)percent / 100), 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        priceAtDiscount = Convert.ToDouble(price);
                    }

                    row["price_at_discount"] = priceAtDiscount;
                    row["sum_full"] = Math.Round(Convert.ToDouble(row["quantity"]) * Convert.ToDouble(row["price"]), 2, MidpointRounding.AwayFromZero);
                    row["sum_at_discount"] = Math.Round(Convert.ToDouble(row["quantity"]) * priceAtDiscount, 2, MidpointRounding.AwayFromZero);
                    row["action"] = num_doc.ToString();
                    row["action2"] = num_doc.ToString();

                    if (dtCopy.Columns.Contains("promo_description"))
                    {
                        row["promo_description"] = comment;
                    }
                }
            }
        }


        /// <summary>
        /// Обрабатывает акции типа "1" - скидка на товары.
        /// </summary>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="percent">Процент скидки.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="actionPricesByDoc">Словарь, где ключ - номер документа, значение - словарь с товарами и их ценами по акции.</param>
        private async Task action_1_dt(int num_doc, decimal percent, string comment, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            DataTable dtCopy = null; // Копия DataTable

            try
            {
                // Создаем копию DataTable перед началом обработки
                dtCopy = dt.Copy();

                // Проверяем, есть ли данные для текущего документа
                if (!actionPricesByDoc.ContainsKey(num_doc))
                {
                    await MessageBox.Show($"Данные для документа {num_doc} не найдены.", "Обработка акций 1 типа");
                    MainStaticClass.WriteRecordErrorLog($"Данные для документа {num_doc} не найдены.", "action_1_dt", num_doc, MainStaticClass.CashDeskNumber, "Обработка акций 1 типа скидка чтение с диска, номер документа здесь это номер ак. док.");
                    return;
                }

                // Получаем словарь с ценами для текущего документа
                var actionPrices = actionPricesByDoc[num_doc];

                // Проходим по всем строкам в копии DataTable
                foreach (DataRow row in dtCopy.Rows)
                {
                    // Пропускаем товары, уже участвовавшие в акциях
                    if (Convert.ToInt32(row["action2"]) > 0)
                    {
                        continue;
                    }

                    //if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                    //{
                    //    continue;
                    //}

                    // Получаем код товара
                    long tovarCode = Convert.ToInt64(row["tovar_code"]);

                    // Проверяем, есть ли товар в словаре акций
                    if (actionPrices.TryGetValue(tovarCode, out var price))
                    {
                        // Вычисляем цену со скидкой
                        double priceAtDiscount;
                        if (price == 0)
                        {
                            // Если цена в акции не указана, применяем процент скидки
                            priceAtDiscount = Math.Round(Convert.ToDouble(row["price"]) * (1 - (double)percent / 100), 2, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            // Используем цену из акции
                            priceAtDiscount = Convert.ToDouble(price);
                        }

                        // Обновляем данные в строке
                        row["price_at_discount"] = priceAtDiscount;
                        row["sum_full"] = Math.Round(Convert.ToDouble(row["quantity"]) * Convert.ToDouble(row["price"]), 2, MidpointRounding.AwayFromZero);
                        row["sum_at_discount"] = Math.Round(Convert.ToDouble(row["quantity"]) * priceAtDiscount, 2, MidpointRounding.AwayFromZero);
                        row["action"] = num_doc.ToString();
                        row["action2"] = num_doc.ToString();

                        // Добавляем комментарий, если колонка существует
                        if (dtCopy.Columns.Contains("promo_description"))
                        {
                            row["promo_description"] = comment;
                        }
                    }
                }

                // Если все прошло успешно, заменяем оригинальный DataTable на измененную копию
                dt = dtCopy;
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                await MessageBox.Show(ex.Message, "Ошибка при обработке 1 типа акций");
            }
            finally
            {
                // Если произошла ошибка, dt остается неизменным
            }
        }
        
        /// <summary>
        ///Обработать акцию по типу 1
        ///первый тип это скидка на конкретный товар
        ///если есть процент скидки то дается скидка 
        ///иначе выдается сообщение о подарке
        ///здесь сообщение о подарке
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="comment"></param>
        /// <param name="marker"></param>
        /// <param name="show_messages"></param>
        private async Task action_1_dt(int num_doc, string comment, int marker, bool show_messages)
        {
            DataTable dtCopy = null; // Копия DataTable

            try
            {
                // Создаем копию DataTable перед началом обработки
                dtCopy = dt.Copy();

                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    // Загружаем все товары, участвующие в акции, одним запросом
                    var actionItems = LoadActionItems(conn, num_doc);

                    // Обрабатываем строки копии DataTable
                    ProcessRows(dtCopy, actionItems, num_doc, comment, marker, show_messages);

                    // Если все прошло успешно, заменяем оригинальный DataTable на измененную копию
                    dt = dtCopy;
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка базы данных");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Обработка акций 1 типа подарок чтение с диска, номер документа здесь это номер ак. док.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке акции 1 типа");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Обработка акций 1 типа подарок чтение с диска, номер документа здесь это номер ак. док.");
            }
            finally
            {
                // Если произошла ошибка, dt остается неизменным
            }
        }

        private HashSet<long> LoadActionItems(NpgsqlConnection conn, int num_doc)
        {
            var actionItems = new HashSet<long>();

            string query = "SELECT code_tovar FROM action_table WHERE num_doc = @num_doc";
            using (var command = new NpgsqlCommand(query, conn))
            {
                command.Parameters.AddWithValue("@num_doc", num_doc);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        actionItems.Add(reader.GetInt64(0));
                    }
                }
            }

            return actionItems;
        }

        /// <summary>
        /// Обрабатывает акцию по типу 1 с использованием предварительно загруженных данных.
        /// </summary>
        /// <param name="num_doc">Номер документа.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="marker">Маркер для дополнительной логики.</param>
        /// <param name="show_messages">Флаг показа сообщений.</param>
        /// <param name="actionPricesByDoc">Словарь с данными о товарах и их ценах.</param>
        private async Task action_1_dt(int num_doc, string comment, int marker, bool show_messages, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            DataTable dtCopy = null; // Копия DataTable

            try
            {
                // Создаем копию DataTable перед началом обработки
                dtCopy = dt.Copy();

                // Проверяем, есть ли данные для текущего документа
                if (!actionPricesByDoc.ContainsKey(num_doc))
                {
                    await MessageBox.Show($"Данные для документа {num_doc} не найдены.", "Обработка акций 1 типа");
                    return;
                }

                // Получаем товары, участвующие в акции
                var actionItems = new HashSet<long>(actionPricesByDoc[num_doc].Keys); // Используем конструктор HashSet<T>

                // Обрабатываем строки копии DataTable
                ProcessRows(dtCopy, actionItems, num_doc, comment, marker, show_messages);

                // Если все прошло успешно, заменяем оригинальный DataTable на измененную копию
                dt = dtCopy;
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке акции 1 типа");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Обработка акций 1 типа подарок со словарем, номер документа здесь это номер ак. док.");
            }
            finally
            {
                // Если произошла ошибка, dt остается неизменным
            }
        }


        /// <summary>
        /// Обрабатывает строки DataTable и применяет акцию.
        /// </summary>
        /// <param name="dtCopy">Копия DataTable для обработки.</param>
        /// <param name="actionItems">Список товаров, участвующих в акции.</param>
        /// <param name="num_doc">Номер документа.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="marker">Маркер для дополнительной логики.</param>
        /// <param name="show_messages">Флаг показа сообщений.</param>
        private async Task ProcessRows(DataTable dtCopy, HashSet<long> actionItems, int num_doc, string comment, int marker, bool show_messages)
        {
            foreach (DataRow row in dtCopy.Rows)
            {
                if (Convert.ToInt32(row["action2"]) > 0)
                {
                    continue; // Пропускаем товары, уже участвовавшие в акциях
                }

                if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                {
                    continue;
                }

                long tovarCode = Convert.ToInt64(row["tovar_code"]);
                if (actionItems.Contains(tovarCode))
                {
                    have_action = true; // Признак срабатывания акции
                    row["gift"] = num_doc.ToString(); // Тип акции

                    if (show_messages)
                    {
                        await MessageBox.Show("Сработала акция, НЕОБХОДИМО выдать подарок " + comment);

                        if (marker == 1)
                        {
                            var result = ShowQueryWindowBarcode(2, 1, num_doc, 1, dtCopy);
                            //if (result == DialogResult.OK)
                            //{
                            //    // Дополнительная логика, если требуется
                            //}
                        }
                    }
                }
            }
        }
        
        #region Ации 2 типа 
        /// </summary>
        /// Здесь акция с предварительно 
        /// загруженными данными в память
        /// <param name="num_doc"></param>
        /// <param name="percent"></param>
        /// <param name="comment"></param>
        /////Обработать акцию по 2 типу
        /////это значит в документе должен быть товар
        /////по вхождению в акционный список 
        /////Здесь дается скидка на кратное количество позиций из 1-го списка
        ///// </summary>
        ///// <param name="num_doc"></param>
        ///// <param name="persent"></param>
        private async Task action_2_dt(int num_doc, decimal percent, string comment,
                         Dictionary<int, LoadActionDataInMemory.ActionDataContainer> allActionData2)
        {
            DataTable dtCopy = null; // Копия DataTable

            try
            {
                // Создаем копию DataTable перед началом обработки
                dtCopy = dt.Copy();

                if (!allActionData2.ContainsKey(num_doc))
                {
                    await MessageBox.Show($"Данные для документа {num_doc} не найдены.", "Обработка акций 2 типа");
                    MainStaticClass.WriteRecordErrorLog($"Данные для документа {num_doc} не найдены.", "action_2_dt скидка", Convert.ToInt16(num_doc), MainStaticClass.CashDeskNumber, "Обработка акций 2 типа с предварительно загруженным словарем");
                    return;
                }

                var container = allActionData2[num_doc];

                // Создаем копии словарей для работы с текущим документом
                var listItems = new Dictionary<int, List<long>>(container.ListItems);
                var listQuantities = new Dictionary<int, int>(container.ListQuantities);

                // Обрабатываем данные
                ProcessActionData(dtCopy, num_doc, percent, comment, listItems, listQuantities);

                // Если все прошло успешно, заменяем оригинальный DataTable на измененную копию
                dt = dtCopy;
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 2 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, Convert.ToInt16(num_doc), MainStaticClass.CashDeskNumber, "Обработка акций 2 типа с предварительно загруженным словарем");
            }
        }


        /// </summary>
        /// Здесь акция данные читаются с диска                
        /////Обработать акцию по 2 типу
        /////это значит в документе должен быть товар
        /////по вхождению в акционный список 
        /////Здесь дается скидка на кратное количество позиций из 1-го списка
        ///// </summary>
        ///// <param name="num_doc"></param>
        ///// <param name="persent"></param>
        private async Task action_2_dt(int num_doc, decimal percent, string comment)
        {
            DataTable dtCopy = null; // Копия DataTable

            try
            {
                // Создаем копию DataTable перед началом обработки
                dtCopy = dt.Copy();

                Dictionary<int, List<long>> listItems = new Dictionary<int, List<long>>();
                Dictionary<int, int> listQuantities = new Dictionary<int, int>();

                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    // Загружаем данные из action_table
                    string query = @"
                SELECT num_list, code_tovar 
                FROM action_table 
                WHERE num_doc = @num_doc 
                ORDER BY num_list, code_tovar";

                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@num_doc", num_doc);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int num_list = reader.GetInt32(0);
                                long code_tovar = reader.GetInt64(1);

                                if (!listItems.ContainsKey(num_list))
                                {
                                    listItems[num_list] = new List<long>();
                                }
                                listItems[num_list].Add(code_tovar);

                                if (!listQuantities.ContainsKey(num_list))
                                {
                                    listQuantities[num_list] = 0;
                                }
                            }
                        }
                    }
                }

                // Обрабатываем данные
                ProcessActionData(dtCopy, num_doc, percent, comment, listItems, listQuantities);

                // Если все прошло успешно, заменяем оригинальный DataTable на измененную копию
                dt = dtCopy;
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 2 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, Convert.ToInt16(num_doc), MainStaticClass.CashDeskNumber, "Обработка акций 2 типа чтение с диска ");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 2 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, Convert.ToInt16(num_doc), MainStaticClass.CashDeskNumber, "Обработка акций 2 типа чтение с диска");
            }
        }

        private async Task ProcessActionData(DataTable dtCopy, int num_doc, decimal percent, string comment,
                              Dictionary<int, List<long>> listItems,
                              Dictionary<int, int> listQuantities)
        {
            if (!listItems.ContainsKey(1))
            {
                await MessageBox.Show("Первый список товаров отсутствует.\r\nНомер акции " + num_doc.ToString(), "Обработка акций 2 типа");
                MainStaticClass.WriteRecordErrorLog("Первый список товаров отсутствует.", "action_2_dt скидка", Convert.ToInt16(num_doc), MainStaticClass.CashDeskNumber, "Обработка акций 2 типа общий метод для чтения с диска и словаря");
                return;
            }

            // Очищаем значения listQuantities перед подсчетом
            foreach (var key in listQuantities.Keys.ToList())
            {
                listQuantities[key] = 0;
            }

            Dictionary<long, int> firstListItems = new Dictionary<long, int>();
            int min_quantity = int.MaxValue;

            try
            {
                // Инициализируем firstListItems
                foreach (var code_tovar in listItems[1])
                {
                    firstListItems[code_tovar] = 0;
                }

                // Анализируем dtCopy для подсчета количества товаров из каждого списка
                foreach (DataRow row in dtCopy.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)
                    {
                        continue;
                    }
                    //if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                    //{
                    //    continue;
                    //}

                    long tovar_code = Convert.ToInt64(row["tovar_code"]);
                    int quantity_of_pieces = Convert.ToInt32(row["quantity"]);

                    // Проверяем, к какому списку принадлежит товар
                    foreach (var num_list in listQuantities.Keys.ToList())
                    {
                        if (listItems.ContainsKey(num_list) && listItems[num_list].Contains(tovar_code))
                        {
                            listQuantities[num_list] += quantity_of_pieces;
                        }
                    }

                    // Обновляем количество товаров из первого списка
                    if (firstListItems.ContainsKey(tovar_code))
                    {
                        firstListItems[tovar_code] += quantity_of_pieces;
                    }
                }

                // Находим минимальное количество для применения скидки
                if (listQuantities.Any())
                {
                    min_quantity = listQuantities.Values.Min();
                }

                // Применяем скидку к товарам из первого списка
                ApplyDiscountsToEligibleItems(dtCopy, num_doc, percent, min_quantity, firstListItems);

                if (min_quantity != 0)
                {
                    //Помечаем товары, участвовавшие в акции
                    marked_action_tovar_dt(dtCopy, num_doc, comment);
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 2 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, Convert.ToInt16(num_doc), MainStaticClass.CashDeskNumber, "Обработка акций 2 типа общий метод для чтения с диска и словаря");
            }
        }

        private void ApplyDiscountsToEligibleItems(DataTable dtCopy, int num_doc, decimal percent, int min_quantity, Dictionary<long, int> firstListItems)
        {
            DataRow newRow = null;

            foreach (DataRow row in dtCopy.Rows)
            {
                if (Convert.ToInt32(row["action2"]) > 0)
                {
                    continue;
                }
                if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                {
                    continue;
                }

                long tovar_code = Convert.ToInt64(row["tovar_code"]);
                int quantity_of_pieces = Convert.ToInt32(row["quantity"]);

                if (firstListItems.ContainsKey(tovar_code) && firstListItems[tovar_code] >= min_quantity)
                {
                    int discountedQuantity = Math.Min(quantity_of_pieces, min_quantity);

                    if (discountedQuantity > 0)
                    {
                        if (quantity_of_pieces <= discountedQuantity)
                        {
                            ApplyDiscountToRow(row, percent, num_doc);
                        }
                        else
                        {
                            newRow = CreateNewRow(dtCopy, row, discountedQuantity, percent, num_doc);
                            row["quantity"] = Convert.ToInt32(row["quantity"]) - discountedQuantity;
                            row["sum_at_discount"] = Math.Round(Convert.ToDouble(row["quantity"]) * Convert.ToDouble(row["price_at_discount"]), 2, MidpointRounding.AwayFromZero);
                        }

                        firstListItems[tovar_code] -= discountedQuantity;

                        if (firstListItems[tovar_code] <= 0)
                        {
                            firstListItems.Remove(tovar_code);
                        }
                    }
                }
            }

            if (newRow != null)
            {
                dtCopy.Rows.Add(newRow);
            }
        }

        private void ApplyDiscountToRow(DataRow row, decimal percent, int num_doc)
        {
            double price = Convert.ToDouble(row["price"]);
            double discountPrice = Math.Round(price - price * (double)percent / 100, 2, MidpointRounding.AwayFromZero);

            row["price_at_discount"] = discountPrice;
            row["sum_full"] = Math.Round(Convert.ToDouble(row["quantity"]) * price, 2, MidpointRounding.AwayFromZero);
            row["sum_at_discount"] = Convert.ToDouble(row["quantity"]) * discountPrice;
            row["action"] = num_doc.ToString();
            row["action2"] = num_doc.ToString();
        }

        private DataRow CreateNewRow(DataTable dtCopy, DataRow originalRow, int quantity, decimal percent, int num_doc)
        {
            DataRow newRow = dtCopy.NewRow();
            newRow.ItemArray = originalRow.ItemArray;
            newRow["quantity"] = quantity;
            ApplyDiscountToRow(newRow, percent, num_doc);
            return newRow;
        }

        #endregion


        /// <summary>
        /// Пометка товарных позиций, которые участвовали в акции,
        /// чтобы они не участвовали в следующих акциях.
        /// </summary>
        /// <param name="dtCopy">DataTable с товарами.</param>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="listQuantities">Словарь с данными о товарах и их количестве по документам.</param>
        /// <param name="show_messages">Флаг, указывающий, нужно ли показывать сообщения об ошибках.</param>
        private void marked_action_tovar_dt(DataTable dtCopy, int num_doc, string comment, Dictionary<int, int> listQuantities, bool show_messages)
        {
            try
            {
                // Проходим по всем строкам в DataTable
                foreach (DataRow row in dtCopy.Rows)
                {
                    // Пропускаем товары, которые уже участвовали в акции
                    if (Convert.ToInt32(row["action2"]) > 0)
                    {
                        continue;
                    }

                    // Получаем код товара из строки
                    int tovarCode = Convert.ToInt32(row["tovar_code"]);

                    // Проверяем, есть ли товар в словаре для текущего документа
                    if (listQuantities.ContainsKey(tovarCode))
                    {
                        // Если товар участвовал в акции, помечаем его
                        row["action2"] = num_doc.ToString();

                        // Добавляем комментарий, если есть соответствующая колонка
                        if (dtCopy.Columns.Contains("promo_description"))
                        {
                            row["promo_description"] = comment;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                if (show_messages)
                {
                    MessageBox.Show(ex.Message, "Пометка товарных позиций участвующих в акции");
                }

                // Логирование ошибки
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Пометка товарных позиций участвующих в акции");
            }
        }

        /*
  * Обработать акцию по 2 типу
  * это значит в документе должен быть товар 
  * по вхождению в акционный список 
    * 
  * Здесь выдается сообщение о подарке*/
        private async  Task action_2_dt(int num_doc, string comment, bool show_messages, Dictionary<int, LoadActionDataInMemory.ActionDataContainer> allActionData2)
        {
            if (!allActionData2.ContainsKey(num_doc))
            {
                await MessageBox.Show($"Данные для документа {num_doc} не найдены.", "Обработка акций 2 типа");
                MainStaticClass.WriteRecordErrorLog($"Данные для документа {num_doc} не найдены.", "action_2_dt(int num_doc, string comment, bool show_messages, Dictionary<int, LoadActionDataInMemory.ActionDataContainer> allActionData2)", num_doc, MainStaticClass.CashDeskNumber, "Акции 2 типа выдается сообщение о подарке");
                return;
            }

            var container = allActionData2[num_doc];

            // Создаем копии словарей для работы с текущим документом
            var listItems = new Dictionary<int, List<long>>(container.ListItems);
            var listQuantities = new Dictionary<int, int>(container.ListQuantities);

            // Обрабатываем данные для подарков
            ProcessGifts(num_doc, comment, listItems, listQuantities, show_messages);
        }


        /*
      * Обработать акцию по 2 типу
      * это значит в документе должен быть товар 
      * по вхождению в акционный список 
        * 
      * Здесь выдается сообщение о подарке*/
        private async Task action_2_dt(int num_doc, string comment, bool show_messages)
        {
            Dictionary<int, List<long>> listItems = new Dictionary<int, List<long>>();
            Dictionary<int, int> listQuantities = new Dictionary<int, int>();

            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    // Загружаем данные из action_table
                    string query = @"
          SELECT num_list, code_tovar 
          FROM action_table 
          WHERE num_doc = @num_doc 
          ORDER BY num_list, code_tovar";

                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@num_doc", num_doc);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int num_list = reader.GetInt32(0);
                                long code_tovar = reader.GetInt64(1);

                                if (!listItems.ContainsKey(num_list))
                                {
                                    listItems[num_list] = new List<long>();
                                }
                                listItems[num_list].Add(code_tovar);

                                if (!listQuantities.ContainsKey(num_list))
                                {
                                    listQuantities[num_list] = 0;
                                }
                            }
                        }
                    }
                }

                // Обрабатываем данные для подарков
                ProcessGifts(num_doc, comment, listItems, listQuantities, show_messages);
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 2 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 2 типа выдается сообщение о подарке");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 2 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 2 типа выдается сообщение о подарке");
            }
        }


        /** Обработать акцию по 2 типу
      * это значит в документе должен быть товар 
      * по вхождению в акционный список       
      * Здесь выдается сообщение о подарке*/
        private async Task ProcessGifts(int num_doc, string comment,
                          Dictionary<int, List<long>> listItems,
                          Dictionary<int, int> listQuantities, bool show_messages)
        {
            // Создаем копию DataTable для работы
            DataTable dtCopy = dt.Copy();

            if (!listItems.ContainsKey(1))
            {
                await MessageBox.Show("Первый список товаров отсутствует.", "Обработка акций 2 типа");
                MainStaticClass.WriteRecordErrorLog("Первый список товаров отсутствует.", @"ProcessGifts(int num_doc, string comment,
              Dictionary<int, List<long>> listItems,
              Dictionary<int, int> listQuantities, bool show_messages)", num_doc, MainStaticClass.CashDeskNumber, "Акции 2 типа выдается сообщение о подарке");
                return;
            }

            // Очищаем значения listQuantities перед подсчетом
            foreach (var key in listQuantities.Keys.ToList())
            {
                listQuantities[key] = 0;
            }

            Dictionary<long, int> firstListItems = new Dictionary<long, int>();

            try
            {
                // Инициализируем firstListItems
                foreach (var code_tovar in listItems[1])
                {
                    firstListItems[code_tovar] = 0;
                }

                // Анализируем dtCopy для подсчета количества товаров из каждого списка
                foreach (DataRow row in dtCopy.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)
                    {
                        continue;
                    }
                    if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                    {
                        continue;
                    }

                    long tovar_code = Convert.ToInt64(row["tovar_code"]);
                    int quantity_of_pieces = Convert.ToInt32(row["quantity"]);

                    // Проверяем, к какому списку принадлежит товар
                    foreach (var num_list in listQuantities.Keys.ToList())
                    {
                        if (listItems.ContainsKey(num_list) && listItems[num_list].Contains(tovar_code))
                        {
                            listQuantities[num_list] += quantity_of_pieces;
                        }
                    }

                    // Обновляем количество товаров из первого списка
                    if (firstListItems.ContainsKey(tovar_code))
                    {
                        firstListItems[tovar_code] += quantity_of_pieces;
                    }
                }

                int giftCount = 0;
                // Находим минимальное количество для применения подарков
                if (listQuantities.Any())
                {
                    giftCount = listQuantities.Values.Min();
                }

                // Выводим сообщение о количестве подарков
                if (giftCount > 0)
                {
                    if (show_messages)
                    {
                        await MessageBox.Show($"Сработала акция, НЕОБХОДИМО выдать подарок количестве {giftCount} шт.  {comment}", "Акция 2 типа: Подарки");
                    }
                }

                // Помечаем товары, участвовавшие в акции (работаем с копией)
                if (LoadActionDataInMemory.AllActionData1 == null)
                {
                    marked_action_tovar_dt(dtCopy, num_doc, comment);
                }
                else
                {
                    marked_action_tovar_dt(dtCopy, num_doc, comment, LoadActionDataInMemory.AllActionData1);
                }

                // Если ошибок не произошло, применяем изменения к оригинальной таблице
                dt = dtCopy;
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при обработке 2 типа акций");
                MainStaticClass.WriteRecordErrorLog("Ошибка при обработке акции.", @"ProcessGifts(int num_doc, string comment,
              Dictionary<int, List<long>> listItems,
              Dictionary<int, int> listQuantities, bool show_messages)", num_doc, MainStaticClass.CashDeskNumber, "Акции 2 типа выдается сообщение о подарке");
            }
        }     


        /// <summary>
        /// Эта акция срабатывает, когда сумма без скидки в документе >= сумме акции.
        /// Тогда дается скидка на те позиции, которые перечисляются в условии акции.
        /// </summary>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="percent">Процент скидки.</param>
        /// <param name="sum">Сумма акции.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="actionPricesByDoc">Словарь с данными из базы данных.</param>
        private async Task action_3_dt(int num_doc, decimal percent, decimal sum, string comment, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            // Создаем копию DataTable для работы
            DataTable dtCopy = dt.Copy();

            try
            {
                // Получаем коды товаров, участвующих в акции, из словаря
                var tovarCodesInAction = GetTovarCodesInActionFromDictionary(actionPricesByDoc, num_doc);

                // Вычисляем общую сумму документа без скидок
                decimal sumOnDoc = CalculateTotalSumWithoutDiscount(dtCopy, tovarCodesInAction);

                // Проверяем условия акции
                if (CheckActionConditions(sumOnDoc, sum))
                {
                    have_action = true; // Признак того, что в документе есть сработка по акции

                    // Применяем скидку к товарам
                    ApplyDiscountToTovars(dtCopy, tovarCodesInAction, num_doc, percent);

                    // Если все успешно, применяем изменения к исходной DataTable
                    dt = dtCopy;
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "ошибка при обработке 3 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 3 типа скидка");
            }
        }

        /// <summary>
        /// Получает коды товаров, участвующих в акции, из словаря.
        /// </summary>
        /// <param name="actionPricesByDoc">Словарь с данными из базы данных.</param>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <returns>Набор кодов товаров.</returns>
        private HashSet<long> GetTovarCodesInActionFromDictionary(Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc, int num_doc)
        {
            if (actionPricesByDoc.ContainsKey(num_doc))
            {
                return new HashSet<long>(actionPricesByDoc[num_doc].Keys);
            }
            return new HashSet<long>();
        }

        /// <summary>
        /// Эта акция срабатывает, когда сумма без скидки в документе >= сумме акции.
        /// Тогда выдается сообщение о подарке.
        /// </summary>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="sum">Сумма акции.</param>
        /// <param name="marker">Маркер для дополнительной логики.</param>
        /// <param name="show_messages">Флаг, указывающий, нужно ли показывать сообщения.</param>
        /// <param name="actionPricesByDoc">Словарь с данными из базы данных.</param>
        private async Task action_3_dt(int num_doc, string comment, decimal sum, int marker, bool show_messages, Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            // Создаем копию DataTable для работы
            DataTable dtCopy = dt.Copy();

            try
            {
                // Получаем коды товаров, участвующих в акции, из словаря
                var tovarCodesInAction = GetTovarCodesInActionFromDictionary(actionPricesByDoc, num_doc);

                // Обработка данных в копии DataTable
                decimal totalSumWithoutDiscount;
                int index;
                CalculateTotalSum(dtCopy, tovarCodesInAction, out totalSumWithoutDiscount, out index);

                if (CheckActionConditions(totalSumWithoutDiscount, sum))
                {
                    // Применяем изменения к копии DataTable
                    //dtCopy.Rows[index]["gift"] = num_doc.ToString();

                    if (show_messages)
                    {
                        await MessageBox.Show(comment, " АКЦИЯ !!!");
                        if (marker == 1)
                        {
                            //var dr = show_query_window_barcode(2, 1, num_doc, 0);
                            await ShowQueryWindowBarcode(2, 1, num_doc, 0, dtCopy);
                        }
                    }

                    // Логика для отметки товаров (если нужно)
                    //marked_action_tovar_dt(num_doc, comment);
                    marked_action_tovar_dt(dtCopy, num_doc, comment);
                }

                // Если все успешно, применяем изменения к исходной DataTable
                dt = dtCopy;
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "ошибка при обработке 3 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 3 типа сообщение о подарке");
            }
        }


        /// <summary>
        /// Эта акция срабатывает когда сумма без скидки в документе >= сумме акции
        /// тогда дается скидка на те позиции которые перечисляются в условии акции
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="percent"></param>
        /// <param name="sum"></param>
        /// <param name="comment"></param>
        private async Task action_3_dt(int num_doc, decimal percent, decimal sum, string comment)
        {
            // Создаем копию DataTable для работы
            DataTable dtCopy = dt.Copy();

            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    // Получаем коды товаров, участвующих в акции
                    var tovarCodesInAction = GetTovarCodesInAction(conn, num_doc);

                    // Вычисляем общую сумму документа без скидок
                    decimal sumOnDoc = CalculateTotalSumWithoutDiscount(dtCopy, tovarCodesInAction);

                    // Проверяем условия акции
                    if (CheckActionConditions(sumOnDoc, sum))
                    {
                        have_action = true; // Признак того, что в документе есть сработка по акции

                        // Применяем скидку к товарам
                        ApplyDiscountToTovars(dtCopy, tovarCodesInAction, num_doc, percent);

                        // Если все успешно, применяем изменения к исходной DataTable
                        dt = dtCopy;
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                //Logger.LogError(ex, "Ошибка при работе с базой данных");
                await MessageBox.Show(ex.Message, "ошибка при обработке 3 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 3 типа скидка");
            }
            catch (Exception ex)
            {
                //Logger.LogError(ex, "Ошибка при обработке акции");
                await MessageBox.Show(ex.Message, "ошибка при обработке 3 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 3 типа скидка");
            }
        }

        private HashSet<long> GetTovarCodesInAction(NpgsqlConnection conn, int num_doc)
        {
            var tovarCodesInAction = new HashSet<long>();
            string query = "SELECT code_tovar FROM action_table WHERE num_doc = @num_doc";

            using (var command = new NpgsqlCommand(query, conn))
            {
                command.Parameters.AddWithValue("@num_doc", num_doc);
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    tovarCodesInAction.Add(reader.GetInt64(0)); // Используем GetInt64 для long
                }
            }

            return tovarCodesInAction;
        }

        private decimal CalculateTotalSumWithoutDiscount(DataTable dtCopy, HashSet<long> tovarCodesInAction)
        {
            decimal sumOnDoc = 0;

            foreach (DataRow row in dtCopy.Rows)
            {
                if (Convert.ToInt32(row["action2"]) > 0)
                {
                    continue;
                }

                if (tovarCodesInAction.Contains(Convert.ToInt64(row["tovar_code"]))) // Используем Convert.ToInt64 для long
                {
                    sumOnDoc += Convert.ToDecimal(row["sum_at_discount"]);
                }
            }

            return sumOnDoc;
        }

        private bool CheckActionConditions(decimal sumOnDoc, decimal sum)
        {
            return sumOnDoc >= sum;
        }

        private void ApplyDiscountToTovars(DataTable dtCopy, HashSet<long> tovarCodesInAction, int num_doc, decimal percent)
        {
            foreach (DataRow row in dtCopy.Rows)
            {
                if (Convert.ToInt32(row["action2"]) > 0)
                {
                    continue;
                }
                if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                {
                    continue;
                }

                if (tovarCodesInAction.Contains(Convert.ToInt64(row["tovar_code"]))) // Используем Convert.ToInt64 для long
                {
                    decimal price = Convert.ToDecimal(row["price"]);
                    decimal priceAtDiscount = Math.Round(price - price * percent / 100, 2);
                    decimal quantity = Convert.ToDecimal(row["quantity"]);

                    row["price_at_discount"] = priceAtDiscount.ToString();
                    row["sum_full"] = (quantity * price).ToString();
                    row["sum_at_discount"] = (quantity * priceAtDiscount).ToString();
                    row["action"] = num_doc.ToString();
                    row["action2"] = num_doc.ToString();
                }
            }
        }
       
        /// <summary>
        ///   Эта акция срабатывает когда сумма без скидки в документе >= сумме акции
        ///   тогда выдается сообщение о подарке
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="comment"></param>
        /// <param name="sum"></param>
        /// <param name="marker"></param>
        /// <param name="show_messages"></param>
        private async Task action_3_dt(int num_doc, string comment, decimal sum, int marker, bool show_messages)
        {
            // Создаем копию DataTable для работы
            DataTable dtCopy = dt.Copy();

            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    conn.Open();

                    // Чтение данных из базы
                    var tovarCodesInAction = GetTovarCodesInAction(conn, num_doc);

                    // Обработка данных в копии DataTable
                    decimal totalSumWithoutDiscount;
                    int index;
                    CalculateTotalSum(dtCopy, tovarCodesInAction, out totalSumWithoutDiscount, out index);

                    if (CheckActionConditions(totalSumWithoutDiscount, sum))
                    {
                        // Применяем изменения к копии DataTable
                        //dtCopy.Rows[index]["gift"] = num_doc.ToString();
                        //dtCopy.Rows[index]["action2"] = num_doc.ToString();

                        if (show_messages)
                        {
                            await MessageBox.Show(comment, " АКЦИЯ !!!");
                            if (marker == 1)
                            {
                                var dr = ShowQueryWindowBarcode(2, 1, num_doc, 0, dtCopy);
                                //var dr = show_query_window_barcode(2, 1, num_doc, 1);

                            }
                        }

                        // Логика для отметки товаров (если нужно)
                        //MarkActionTovar(conn, num_doc, comment);
                        marked_action_tovar_dt(dtCopy, num_doc, comment);
                    }

                    // Если все успешно, применяем изменения к исходной DataTable
                    dt = dtCopy;
                }
            }
            catch (NpgsqlException ex)
            {
                // В случае ошибки при чтении из базы, dt остается неизменной
                //Logger.LogError(ex, "Ошибка при работе с базой данных");
                await MessageBox.Show(ex.Message, "ошибка при обработке 3 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 3 типа сообщение о подарке");
            }
            catch (Exception ex)
            {
                // В случае любой другой ошибки, dt остается неизменной
                //Logger.LogError(ex, "Ошибка при обработке акции");
                await MessageBox.Show(ex.Message, "ошибка при обработке 3 типа акций");
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акции 3 типа сообщение о подарке");
            }
        }



        private void CalculateTotalSum(DataTable dtCopy, HashSet<long> tovarCodesInAction, out decimal totalSumWithoutDiscount, out int index)
        {
            totalSumWithoutDiscount = 0;
            index = 0;

            foreach (DataRow row in dtCopy.Rows)
            {
                if (Convert.ToInt32(row["action2"]) > 0)
                {
                    continue;
                }

                if (tovarCodesInAction.Contains(Convert.ToInt32(row["tovar_code"])))
                {
                    totalSumWithoutDiscount += Convert.ToDecimal(row["sum_at_discount"]);
                    index = dtCopy.Rows.IndexOf(row);
                }
            }
        }



        /// <summary>
        /// Создание временной таблицы для 4 типа акций
        /// </summary>
        /// <returns></returns>
        private async Task<bool> create_temp_tovar_table_4()
        {
            bool result = true;

            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = "select COUNT(*) from information_schema.tables where table_schema='public' and table_name='tovar_action'";
                command = new NpgsqlCommand(query, conn);

                if (Convert.ToInt16(command.ExecuteScalar()) == 0)
                {
                    // СОЗДАЕМ ТАБЛИЦУ С ПОЛЕМ ДЛЯ МАРКИРОВКИ
                    query = @"CREATE TABLE tovar_action(
                code bigint NOT NULL,
                retail_price numeric(10,2) NOT NULL,
                quantity integer,
                retail_price_discount numeric(10,2),
                characteristic_name character varying(100),
                characteristic_guid character varying(36),
                marking character varying(200)  -- ДОБАВЛЕНО ПОЛЕ ДЛЯ МАРКИРОВКИ
            ) WITH (OIDS=FALSE);
            ALTER TABLE tovar_action OWNER TO postgres;";
                }
                else
                {
                    // УДАЛЯЕМ И ПЕРЕСОЗДАЕМ ТАБЛИЦУ С ПОЛЕМ ДЛЯ МАРКИРОВКИ
                    query = @"DROP TABLE tovar_action;
            CREATE TABLE tovar_action(
                code bigint NOT NULL,
                retail_price numeric(10,2) NOT NULL,
                quantity integer,
                retail_price_discount numeric(10,2),
                characteristic_name character varying(100),
                characteristic_guid character varying(36),
                marking character varying(200)  -- ДОБАВЛЕНО ПОЛЕ ДЛЯ МАРКИРОВКИ
            ) WITH (OIDS=FALSE);
            ALTER TABLE tovar_action OWNER TO postgres;";
                }

                command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                command.Dispose();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, " Ошибка при создании временной таблицы ");
                result = false;
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, " Ошибка при создании временной таблицы ");
                result = false;
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }

        /*Эта акция срабатывает когда количество товаров 
        * в документе >= сумме(количество) товаров в акции
        * тогда дается скидка на кратное количество товара
        * на самый дешевый товар из участвующих в акции 
        * 
        */
        private async Task action_4_dt(int num_doc, decimal persent, decimal sum, string comment)
        {
            if (!await create_temp_tovar_table_4())
            {
                return;
            }

            DataTable dt2 = dt.Copy();
            dt2.Rows.Clear();
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            decimal quantity_on_doc = 0; //количество позиций в документе            
            StringBuilder query = new StringBuilder();
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query_string = "";

                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0) //Этот товар уже участвовал в акции значит его пропускаем                  
                    {
                        DataRow row2 = dt2.NewRow();
                        row2.ItemArray = row.ItemArray;
                        dt2.Rows.Add(row2);
                        continue;
                    }

                    query_string = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"] + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query_string, conn);

                    if (Convert.ToInt16(command.ExecuteScalar()) != 0)
                    {
                        // ВСТАВКА С МАРКИРОВКОЙ И ПРАВИЛЬНЫМ ПРЕОБРАЗОВАНИЕМ
                        for (int i = 0; i < Convert.ToInt32(row["quantity"]); i++)
                        {
                            query.Append(@"INSERT INTO tovar_action(code, retail_price, quantity, characteristic_name, characteristic_guid, marking)
                        VALUES(" +
                                row["tovar_code"].ToString() + "," +
                                row["price"].ToString().Replace(",", ".") + "," +
                                "1,'" +
                                row["characteristic_name"].ToString().Replace("'", "''") + "','" + // ЭКРАНИРОВАНИЕ АПОСТРОФОВ
                                row["characteristic_code"].ToString() + "','" +
                                row["marking"].ToString().Replace("'", "vasya2021") + "');"); // ПРАВИЛЬНОЕ ПРЕОБРАЗОВАНИЕ МАРКИРОВКИ
                        }
                        quantity_on_doc += Convert.ToDecimal(row["quantity"]);
                    }
                    else //Не участвует в акции убираем пока в сторонку
                    {
                        DataRow row2 = dt2.NewRow();
                        row2.ItemArray = row.ItemArray;
                        dt2.Rows.Add(row2);
                    }
                }

                if (quantity_on_doc >= sum) //Есть вхождение в акцию
                {
                    have_action = true; //Признак того что в документе есть сработка по акции                    
                    dt.Rows.Clear();
                    foreach (DataRow row2 in dt2.Rows)
                    {
                        DataRow row = dt.NewRow();
                        row.ItemArray = row2.ItemArray;
                        dt.Rows.Add(row);
                    }

                    command = new NpgsqlCommand(query.ToString(), conn); //устанавливаем акционные позиции во временную таблицу
                    command.ExecuteNonQuery();
                    query.Append("DELETE FROM tovar_action;"); //Очищаем таблицу акционных товаров 

                    int multiplication_factor = (int)(quantity_on_doc / sum);
                    int totalItems = (int)quantity_on_doc;

                    // ЗАПРОС С ИМЕНАМИ КОЛОНОК И СОРТИРОВКОЙ ПО ВОЗРАСТАНИЮ ЦЕНЫ
                    query_string = @"SELECT 
                code AS code,
                retail_price AS retail_price,
                quantity AS quantity,
                characteristic_name AS characteristic_name,
                characteristic_guid AS characteristic_guid,
                marking AS marking
            FROM tovar_action 
            ORDER BY retail_price ASC"; // СОРТИРОВКА ПО ВОЗРАСТАНИЮ (САМЫЕ ДЕШЕВЫЕ ПЕРВЫМИ)

                    command = new NpgsqlCommand(query_string, conn);
                    NpgsqlDataReader reader = command.ExecuteReader();
                    int currentItemIndex = 0; // Индекс текущего товара (начинаем с 0)

                    query.Clear(); // Очищаем StringBuilder для новой вставки

                    while (reader.Read())
                    {
                        // ПОЛУЧАЕМ ЗНАЧЕНИЯ ПО ИМЕНАМ КОЛОНОК
                        long code = reader.GetInt64(reader.GetOrdinal("code"));
                        decimal retailPrice = reader.GetDecimal(reader.GetOrdinal("retail_price"));
                        string characteristicName = reader.GetString(reader.GetOrdinal("characteristic_name"));
                        string characteristicGuid = reader.GetString(reader.GetOrdinal("characteristic_guid"));
                        string marking = reader.GetString(reader.GetOrdinal("marking")); // Маркировка из БД (уже с vasya2021)

                        // ПРАВИЛЬНАЯ ПРОВЕРКА: каждый sum-й товар получает скидку
                        bool hasDiscount = multiplication_factor > 0 &&
                                          (currentItemIndex % (int)sum == 0) &&
                                          (currentItemIndex + (int)sum <= totalItems);

                        decimal discountPrice;
                        if (hasDiscount)
                        {
                            // ТОВАР СО СКИДКОЙ
                            discountPrice = Math.Round(retailPrice - retailPrice * persent / 100, 2);
                            multiplication_factor--;
                        }
                        else
                        {
                            // ТОВАР БЕЗ СКИДКИ
                            discountPrice = retailPrice;
                        }

                        // ВСТАВКА С МАРКИРОВКОЙ (сохраняем как есть из БД)
                        query.Append(@"INSERT INTO tovar_action(code, retail_price, quantity, characteristic_name, characteristic_guid, retail_price_discount, marking)
                    VALUES(" +
                            code + "," +
                            retailPrice.ToString().Replace(",", ".") + "," +
                            "1,'" +
                            characteristicName.Replace("'", "''") + "','" +
                            characteristicGuid + "'," +
                            discountPrice.ToString().Replace(",", ".") + ",'" +
                            marking + "');"); // Маркировка уже содержит vasya2021 вместо апострофов

                        currentItemIndex++;
                    }
                    reader.Close();

                    command = new NpgsqlCommand(query.ToString(), conn);
                    command.ExecuteNonQuery();

                    // ФИНАЛЬНЫЙ ЗАПРОС С ГРУППИРОВКОЙ И МАРКИРОВКОЙ
                    query_string = @"SELECT 
                tovar_action.code AS code,
                tovar.name AS name,
                tovar_action.retail_price AS retail_price,
                tovar_action.retail_price_discount AS retail_price_discount,
                SUM(tovar_action.quantity) AS total_quantity,
                tovar_action.characteristic_name AS characteristic_name,
                tovar_action.characteristic_guid AS characteristic_guid,
                tovar_action.marking AS marking
            FROM tovar_action 
            LEFT JOIN tovar ON tovar_action.code = tovar.code 
            GROUP BY tovar_action.code, tovar.name, tovar_action.retail_price, tovar_action.retail_price_discount,
                     tovar_action.characteristic_name, tovar_action.characteristic_guid, tovar_action.marking";

                    command = new NpgsqlCommand(query_string, conn);
                    reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        // ПОЛУЧАЕМ ЗНАЧЕНИЯ ПО ИМЕНАМ КОЛОНОК
                        long code = reader.GetInt64(reader.GetOrdinal("code"));
                        string name = reader.GetString(reader.GetOrdinal("name"));
                        decimal retailPrice = reader.GetDecimal(reader.GetOrdinal("retail_price"));
                        decimal discountPrice = reader.GetDecimal(reader.GetOrdinal("retail_price_discount"));
                        decimal totalQuantity = reader.GetDecimal(reader.GetOrdinal("total_quantity"));
                        string characteristicName = reader.GetString(reader.GetOrdinal("characteristic_name"));
                        string characteristicGuid = reader.GetString(reader.GetOrdinal("characteristic_guid"));
                        string marking = reader.GetString(reader.GetOrdinal("marking")).Replace("vasya2021", "'"); // ВОССТАНАВЛИВАЕМ АПОСТРОФЫ

                        DataRow row = dt.NewRow();
                        row["tovar_code"] = code;
                        row["tovar_name"] = name.Trim();
                        row["characteristic_name"] = characteristicName;
                        row["characteristic_code"] = characteristicGuid;
                        row["quantity"] = totalQuantity;
                        row["price"] = retailPrice;
                        row["price_at_discount"] = discountPrice;
                        row["sum_full"] = totalQuantity * retailPrice;
                        row["sum_at_discount"] = totalQuantity * discountPrice;

                        // УСТАНАВЛИВАЕМ action В ЗАВИСИМОСТИ ОТ СКИДКИ
                        row["action"] = (retailPrice != discountPrice) ? num_doc.ToString() : "0";
                        row["gift"] = "0";
                        row["action2"] = num_doc.ToString();
                        row["bonus_reg"] = 0;
                        row["bonus_action"] = 0;
                        row["bonus_action_b"] = 0;
                        row["marking"] = marking; // СОХРАНЯЕМ МАРКИРОВКУ С ВОССТАНОВЛЕННЫМИ АПОСТРОФАМИ

                        dt.Rows.Add(row);
                    }
                    reader.Close();

                    /*акция сработала
                     * надо отметить все товарные позиции 
                     * чтобы они не участвовали в других акциях 
                     */
                    marked_action_tovar_dt(num_doc, comment);
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, " Ошибка при обработке 4 типа акций ");
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// в документе >= сумме(количество) товаров в акции  
        /// тогда дается скидка на кратное количество товара
        /// на самый дешевый товар из участвующих в акции 
        /// здесь метод без обращения к бд
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="percent"></param>
        /// <param name="sum"></param>
        /// <param name="comment"></param>
        /// <param name="actionPricesByDoc"></param>
        private async Task action_4_dt(int num_doc,
                    decimal percent,
                    decimal sum,
                    string comment,
                    Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            // Проверка на целочисленность sum
            if (sum != Math.Floor(sum))
                throw new ArgumentException("Параметр 'sum' должен быть целым числом");

            // Сохраняем оригинальные данные для возможного отката
            DataTable originalDt = dt.Copy();
            DataTable tempDt = dt.Clone();

            try
            {
                // 1. Переносим строки, не участвующие в акции
                foreach (DataRow row in originalDt.Rows)
                {
                    if (row.Field<int>("action2") > 0 ||
                        !IsTovarInAction(actionPricesByDoc, num_doc, (long)row.Field<double>("tovar_code")))
                    {
                        tempDt.ImportRow(row);
                    }
                }

                // 2. Подготовка данных для обработки С МАРКИРОВКОЙ
                var items = new List<ItemData>();
                foreach (DataRow row in originalDt.Rows)
                {
                    if (row.Field<int>("action2") > 0)
                    {
                        continue;
                    }

                    if (Convert.ToInt32(row["sum_at_discount"]) < 1)
                    {
                        continue;
                    }

                    long tovarCode = (long)row.Field<double>("tovar_code");
                    if (!IsTovarInAction(actionPricesByDoc, num_doc, tovarCode)) continue;

                    items.Add(new ItemData
                    {
                        Code = row.Field<double>("tovar_code"),
                        TovarName = row.Field<string>("tovar_name"),
                        CharName = row.Field<string>("characteristic_name"),
                        CharGuid = row.Field<string>("characteristic_code"),
                        Price = row.Field<decimal>("price"),
                        Quantity = row.Field<double>("quantity"),
                        Marking = row.Field<string>("marking") ?? "0" // ДОБАВЛЕНО МАРКИРОВКУ
                    });
                }

                bool isActionApplied;
                // 3. Обработка и группировка С МАРКИРОВКОЙ
                var processedItems = ProcessItems(items, num_doc, percent, (int)sum, out isActionApplied);

                // 4. Заполнение таблицы
                dt.BeginLoadData();
                try
                {
                    dt.Clear();

                    // Добавляем обработанные элементы
                    foreach (var group in processedItems)
                    {
                        DataRow newRow = dt.NewRow();
                        FillDataRow(newRow, group, num_doc);
                        dt.Rows.Add(newRow);
                    }

                    // Добавляем неизмененные строки
                    foreach (DataRow row in tempDt.Rows)
                    {
                        dt.ImportRow(row);
                    }
                }
                finally
                {
                    dt.EndLoadData();
                }

                if (isActionApplied)
                {
                    // 5. Отмечаем товары, участвовавшие в акции
                    await marked_action_tovar_dt(dt, num_doc, comment, actionPricesByDoc);
                }
            }
            catch (Exception ex)
            {
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акция 4 типа без обращения к бд");
                // Восстановление данных при ошибке
                dt.Clear();
                foreach (DataRow row in originalDt.Rows)
                {
                    dt.ImportRow(row);
                }
                throw;
            }
            finally
            {
                originalDt.Dispose();
                tempDt.Dispose();
            }
        }

        // Вспомогательные классы
        private class ItemData
        {
            public double Code { get; set; }
            public string TovarName { get; set; }  // Наименование товара
            public string CharName { get; set; }   // Название характеристики
            public string CharGuid { get; set; }   // Идентификатор характеристики
            public decimal Price { get; set; }     // Цена товара
            public double Quantity { get; set; }  // Количество товара
            public string Marking { get; set; }   // Маркировка
        }

        private class GroupedItem
        {
            public double Code { get; set; }
            public string TovarName { get; set; } // Наименование товара
            public string CharName { get; set; }  // Название характеристики
            public string CharGuid { get; set; }  // Идентификатор характеристики
            public decimal Price { get; set; }    // Цена товара
            public decimal Discount { get; set; } // Цена со скидкой
            public double Count { get; set; }     // Количество товара
            public decimal SumFull { get; set; }  // Сумма без скидки
            public decimal SumDiscount { get; set; } // Сумма со скидкой
            public int Action { get; set; }       // Флаг акции
            public int Gift { get; set; }         //флаг подарка    
            public string Marking { get; set; }   // Маркировка
        }


        private List<GroupedItem> ProcessItems(
     List<ItemData> items,
     int num_doc,
     decimal percent,
     int sum,
     out bool isActionApplied)
        {
            isActionApplied = false;
            var flatItems = new List<ItemData>();

            // "Разворачиваем" товары
            foreach (var item in items)
            {
                if (item.Quantity != Math.Floor(item.Quantity))
                    throw new ArgumentException("Quantity must be integer value");

                int quantity = (int)item.Quantity;

                for (int i = 0; i < quantity; i++)
                {
                    flatItems.Add(new ItemData
                    {
                        Code = item.Code,
                        TovarName = item.TovarName,
                        CharName = item.CharName,
                        CharGuid = item.CharGuid,
                        Price = item.Price,
                        Quantity = 1.0,
                        Marking = item.Marking // СОХРАНЯЕМ МАРКИРОВКУ
                    });
                }
            }

            // Сортируем по цене (самые дешевые первыми)
            flatItems.Sort((a, b) => a.Price.CompareTo(b.Price));

            var groups = new Dictionary<string, GroupedItem>();

            // Обрабатываем все товары
            for (int i = 0; i < flatItems.Count; i++)
            {
                var item = flatItems[i];

                // Определяем, является ли товар со скидкой (каждый sum-й товар)
                bool hasDiscount = (i % sum) == 0 && i + sum <= flatItems.Count;

                if (hasDiscount)
                {
                    isActionApplied = true;
                }

                // Цена со скидкой или обычная
                decimal price = hasDiscount
                    ? Math.Round(item.Price - item.Price * percent / 100, 2)
                    : item.Price;

                // Ключ для группировки
                // Для маркированных товаров используем уникальный ключ
                string key;
                if (item.Marking != "0")
                {
                    // Уникальный ключ для каждого маркированного товара
                    key = $"{item.Code}|{item.CharName}|{item.CharGuid}|{item.Price}|{price}|{item.Marking}|{Guid.NewGuid()}";
                }
                else
                {
                    // Обычный ключ для немаркированных
                    key = $"{item.Code}|{item.CharName}|{item.CharGuid}|{item.Price}|{price}|{item.Marking}";
                }

                if (!groups.TryGetValue(key, out GroupedItem group))
                {
                    group = new GroupedItem
                    {
                        Code = item.Code,
                        TovarName = item.TovarName,
                        CharName = item.CharName,
                        CharGuid = item.CharGuid,
                        Price = item.Price,
                        Discount = price,
                        Action = hasDiscount ? num_doc : 0,
                        Gift = 0, // Не подарок, а скидка
                        Count = 0.0,
                        SumFull = 0.0m,
                        SumDiscount = 0.0m,
                        Marking = item.Marking // СОХРАНЯЕМ МАРКИРОВКУ
                    };
                    groups[key] = group;
                }

                group.Count += 1.0;
                group.SumFull += item.Price;
                group.SumDiscount += price;
            }

            return groups.Values.ToList();
        }

        private void FillDataRow(DataRow row, GroupedItem item, int num_doc)
        {
            row["tovar_code"] = item.Code;
            row["tovar_name"] = item.TovarName ?? string.Empty;
            row["characteristic_name"] = item.CharName ?? string.Empty;
            row["characteristic_code"] = item.CharGuid ?? string.Empty;
            row["quantity"] = item.Count;
            row["price"] = item.Price;
            row["price_at_discount"] = item.Discount;
            row["sum_full"] = item.SumFull;
            row["sum_at_discount"] = item.SumDiscount;
            row["action"] = item.Action; // 0 или num_doc
            row["gift"] = 0; // Не подарок
            row["action2"] = item.Action; // 0 или num_doc
            row["bonus_reg"] = 0m;
            row["bonus_action"] = 0m;
            row["bonus_action_b"] = 0m;
            row["marking"] = item.Marking ?? "0"; // ВОССТАНОВИТЬ МАРКИРОВКУ
        }

        private bool IsTovarInAction(
            Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc,
            int num_doc,
            long tovarCode)
        {
            return actionPricesByDoc.TryGetValue(num_doc, out var docPrices)
                   && docPrices.ContainsKey(tovarCode);
        }


        /// <summary>
        /// Возвращает цену подарка для указанного номера документа акции.
        /// </summary>
        /// <param name="numDoc">Номер документа акции.</param>
        /// <returns>Цена подарка или 0, если не найдена.</returns>
        private string GetGiftPrice(int numDoc)
        {
            // 1. Проверка кэша
            if (TryGetCachedPrice(numDoc, out double cachedPrice))
            {
                return cachedPrice.ToString();
            }

            // 2. Запрос к базе данных
            try
            {
                using (var connection = MainStaticClass.NpgsqlConn())
                {
                    return QueryDatabase(connection, numDoc).ToString();
                }
            }
            catch (Exception ex) when (ex is NpgsqlException || ex is InvalidOperationException)
            {
                LogError(ex, numDoc);
                return "1";
            }
        }

        //-------------------------------------------------------
        // Вспомогательные методы
        //-------------------------------------------------------

        /// <summary>
        /// Пытается получить цену из кэша.
        /// </summary>
        private bool TryGetCachedPrice(int numDoc, out double price)
        {
            price = 0;

            if (!InventoryManager.completeDictionaryProductData)
                return false;

            return InventoryManager.DictionaryPriceGiftAction.TryGetValue(numDoc, out price);
        }

        /// <summary>
        /// Выполняет запрос к базе данных.
        /// </summary>
        private decimal QueryDatabase(NpgsqlConnection connection, int numDoc)
        {
            const string query = @"
        SELECT gift_price 
        FROM action_header 
        WHERE num_doc = @numDoc 
          AND tip IN (1,2,3,4,5,6,8)";

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@numDoc", numDoc);
                connection.Open();

                var result = command.ExecuteScalar();
                return Convert.ToDecimal(result ?? 0m);
            }
        }

        /// <summary>
        /// Логирует ошибки.
        /// </summary>
        private void LogError(Exception ex, int numDoc)
        {
            string errorContext = $"Ошибка при получении цены для документа {numDoc}";
            MainStaticClass.WriteRecordErrorLog(
                ex,
                numDoc,
                MainStaticClass.CashDeskNumber,
                errorContext
            );
            MessageBox.Show(errorContext);
        }



        ///// <summary>
        ///// Возвращает цену подарка
        ///// </summary>
        ///// <param name="num_doc"></param>
        ///// <returns></returns>
        //private string get_price_action(int num_doc)
        //{
        //    string result = "";

        //    if (InventoryManager.complete)
        //    {
        //        var giftPrice = InventoryManager.DictionaryPriceGiftAction;
        //        if (giftPrice.Count != 0)
        //        {
        //            if (giftPrice.TryGetValue(2, out double price))
        //            {
        //                result = price.ToString();
        //                return result;
        //            }
        //        }
        //    }

        //    using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
        //    {

        //        try
        //        {

        //            conn.Open();
        //            string query = "SELECT action_header.tip, action_header.gift_price  FROM action_header where action_header.num_doc=" + num_doc.ToString();
        //            NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //            NpgsqlDataReader reader = command.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                if ((Convert.ToInt16(reader["tip"]) == 1) || (Convert.ToInt16(reader["tip"]) == 2) || (Convert.ToInt16(reader["tip"]) == 3) || (Convert.ToInt16(reader["tip"]) == 4) || (Convert.ToInt16(reader["tip"]) == 5) || (Convert.ToInt16(reader["tip"]) == 6) || (Convert.ToInt16(reader["tip"]) == 8))
        //                {
        //                    result = reader["gift_price"].ToString();//получить розничную цену подарка                     
        //                }
        //            }
        //            reader.Close();
        //            conn.Close();
        //        }
        //        catch (NpgsqlException ex)
        //        {
        //            MessageBox.Show(ex.Message);
        //            MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Получение цены для подарка");
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message);
        //            MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "Получение цены для подарка");
        //        }
        //    }             

        //    return result;
        //}

        /*Эта акция срабатывает когда количество товаров в документе >= сумме(количество) товаров в акции
        * тогда выдается сообщение о подарке
        * самый дешевый товар из документа дается в подарок кратное число единиц 
        * и еще добавляется некий товар из акционного документа         
        */
        //private void action_4(int num_doc, string comment, decimal sum, long code_tovar)
        private async Task action_4_dt(int num_doc, string comment, decimal sum, bool show_messages)
        {
            if (!await create_temp_tovar_table_4()) //Создать временную таблицу для акционного товара
            {
                return;
            }

            DataTable dt2 = dt.Copy();
            dt2.Rows.Clear();
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            decimal quantity_on_doc = 0; //количество позиций в документе            
            StringBuilder query = new StringBuilder();
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                string query_string = "";

                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0) //Этот товар уже участвовал в акции значит его пропускаем                  
                    {
                        DataRow row2 = dt2.NewRow();
                        row2.ItemArray = row.ItemArray;
                        dt2.Rows.Add(row2);
                        continue;
                    }
                    query_string = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"] + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query_string, conn);
                    if (Convert.ToInt16(command.ExecuteScalar()) != 0)
                    {
                        // ВСТАВКА С МАРКИРОВКОЙ И ЭКРАНИРОВАНИЕМ АПОСТРОФОВ
                        query.Append("INSERT INTO tovar_action(code, retail_price, quantity, characteristic_name, characteristic_guid, marking) VALUES(" +
                            row["tovar_code"].ToString() + "," +
                            row["price"].ToString().Replace(",", ".") + "," +
                            "1,'" +
                            row["characteristic_name"].ToString().Replace("'", "''") + "','" + // ЭКРАНИРОВАНИЕ АПОСТРОФОВ
                            row["characteristic_code"].ToString() + "','" +
                            row["marking"].ToString().Replace("'", "vasya2021") + "');");

                        quantity_on_doc += Convert.ToDecimal(row["quantity"]);
                    }
                    else //Не участвует в акции убираем пока в сторону
                    {
                        DataRow row2 = dt2.NewRow();
                        row2.ItemArray = row.ItemArray;
                        dt2.Rows.Add(row2);
                    }
                }

                if (quantity_on_doc >= sum) //Есть вхождение в акцию
                {
                    have_action = true; //Признак того что в документе есть сработка по акции                    
                    dt.Rows.Clear();
                    foreach (DataRow row2 in dt2.Rows)
                    {
                        DataRow row = dt.NewRow();
                        row.ItemArray = row2.ItemArray;
                        dt.Rows.Add(row);
                    }

                    command = new NpgsqlCommand(query.ToString(), conn); //устанавливаем акционные позиции во временную таблицу
                    command.ExecuteNonQuery();
                    query.Append("DELETE FROM tovar_action;"); //Очищаем таблицу акционных товаров 

                    int multiplication_factor = (int)(quantity_on_doc / sum);
                    int totalItems = (int)quantity_on_doc; // Общее количество товаров участвующих в акции

                    // ЗАПРОС С ИМЕНАМИ КОЛОНОК И СОРТИРОВКОЙ ПО ВОЗРАСТАНИЮ ЦЕНЫ
                    query_string = @"SELECT 
                tovar_action.code AS code,
                tovar.name AS name,
                tovar_action.retail_price AS retail_price,
                tovar_action.quantity AS quantity,
                tovar_action.characteristic_name AS characteristic_name,
                tovar_action.characteristic_guid AS characteristic_guid,
                tovar_action.marking AS marking
            FROM tovar_action 
            LEFT JOIN tovar ON tovar_action.code = tovar.code 
            ORDER BY tovar_action.retail_price ASC";

                    command = new NpgsqlCommand(query_string, conn);
                    NpgsqlDataReader reader = command.ExecuteReader();
                    int currentItemIndex = 0; // Индекс текущего товара (начинаем с 0)

                    while (reader.Read())
                    {
                        // ПОЛУЧАЕМ ЗНАЧЕНИЯ ПО ИМЕНАМ КОЛОНОК
                        long code = reader.GetInt64(reader.GetOrdinal("code"));
                        string name = reader.GetString(reader.GetOrdinal("name"));
                        decimal retailPrice = reader.GetDecimal(reader.GetOrdinal("retail_price"));
                        string characteristicName = reader.GetString(reader.GetOrdinal("characteristic_name"));
                        string characteristicGuid = reader.GetString(reader.GetOrdinal("characteristic_guid"));
                        string marking = reader.GetString(reader.GetOrdinal("marking")).Replace("vasya2021", "'");

                        DataRow row = dt.NewRow();
                        row["tovar_code"] = code;
                        row["tovar_name"] = name.Trim();
                        row["characteristic_name"] = characteristicName;
                        row["characteristic_code"] = characteristicGuid;
                        row["quantity"] = 1;
                        row["price"] = retailPrice;

                        // ПРАВИЛЬНАЯ ПРОВЕРКА: каждый sum-й товар является подарком
                        // Пример: sum = 3, тогда товары с индексами 0, 3, 6, 9... - подарки
                        bool isGift = multiplication_factor > 0 &&
                                     (currentItemIndex % (int)sum == 0) &&
                                     (currentItemIndex + (int)sum <= totalItems);

                        if (isGift)
                        {
                            // ЭТО ПОДАРОК - ИСПОЛЬЗУЕМ АКЦИОННУЮ ЦЕНУ
                            decimal giftPrice = Convert.ToDecimal(GetGiftPrice(num_doc));
                            row["price_at_discount"] = giftPrice;
                            row["sum_full"] = retailPrice;
                            row["sum_at_discount"] = giftPrice;
                            row["action"] = "0";
                            row["gift"] = num_doc.ToString();
                            multiplication_factor--;
                        }
                        else
                        {
                            // НЕ ПОДАРОК - ОБЫЧНАЯ ЦЕНА
                            row["price_at_discount"] = retailPrice;
                            row["sum_full"] = retailPrice;
                            row["sum_at_discount"] = retailPrice;
                            row["action"] = "0";
                            row["gift"] = "0";
                        }

                        row["action2"] = num_doc.ToString();
                        row["bonus_reg"] = 0;
                        row["bonus_action"] = 0;
                        row["bonus_action_b"] = 0;
                        row["marking"] = marking; // СОХРАНЯЕМ МАРКИРОВКУ

                        dt.Rows.Add(row);
                        currentItemIndex++; // Увеличиваем индекс для следующего товара
                    }
                    reader.Close();

                    /*акция сработала
                     * надо отметить все товарные позиции 
                     * чтобы они не участвовали в других акциях 
                     */
                    marked_action_tovar_dt(num_doc, comment);
                }
                roll_up_dt();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "ошибка при обработке 4 типа акций");
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }


        /// <summary>
        /// Эта акция срабатывает, когда количество товаров в документе >= сумме (количество) товаров в акции.
        /// Тогда выдается сообщение о подарке. Самый дешевый товар из документа дается в подарок кратное число единиц,
        /// и еще добавляется некий товар из акционного документа. Метод работает без обращения к базе данных.
        /// </summary>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="comment">Комментарий к акции.</param>
        /// <param name="sum">Количество товаров, необходимое для срабатывания акции.</param>
        /// <param name="show_messages">Флаг, указывающий, нужно ли показывать сообщения.</param>
        /// <param name="actionPricesByDoc">Словарь с ценами товаров по документам акций.</param>
        private async Task action_4_dt(int num_doc,
                         string comment,
                         decimal sum,
                         bool show_messages,
                         Dictionary<int, Dictionary<long, decimal>> actionPricesByDoc)
        {
            if (sum != Math.Floor(sum))
                throw new ArgumentException("Параметр 'sum' должен быть целым числом");

            // Создаем копию исходной таблицы
            DataTable originalDt = dt.Copy();
            DataTable tempDt = dt.Clone(); // Временная таблица для обработки данных

            try
            {
                // Фильтрация строк, которые не участвуют в акции
                foreach (DataRow row in originalDt.Rows)
                {
                    if (row.Field<int>("action2") > 0 ||
                        !IsTovarInAction(actionPricesByDoc, num_doc, (long)row.Field<double>("tovar_code")))
                    {
                        tempDt.ImportRow(row);
                    }
                }

                // Сбор данных о товарах, участвующих в акции
                var items = new List<ItemData>();
                foreach (DataRow row in originalDt.Rows)
                {
                    if (row.Field<int>("action2") > 0) continue;

                    long tovarCode = (long)row.Field<double>("tovar_code");
                    if (!IsTovarInAction(actionPricesByDoc, num_doc, tovarCode)) continue;
                    
                    items.Add(new ItemData
                    {
                        Code = row.Field<double>("tovar_code"),
                        TovarName = row.Field<string>("tovar_name"),
                        CharName = row.Field<string>("characteristic_name") ?? string.Empty,
                        CharGuid = row.Field<string>("characteristic_code") ?? string.Empty,
                        Price = row.Field<decimal>("price"),
                        Quantity = row.Field<double>("quantity"),
                        Marking = row.Field<string>("marking") ?? "0"
                    });
                }

                bool isActionApplied; // Переменная для флага
                var processedItems = ProcessItems(items, num_doc, (int)sum, out isActionApplied);
                // Обработка товаров для определения подарков
                //var processedItems = ProcessItems(items, num_doc, (int)sum);

                // Создаем временную таблицу для новых данных
                DataTable newDt = dt.Clone();
                newDt.BeginLoadData();

                try
                {
                    // Добавление обработанных товаров во временную таблицу
                    foreach (var group in processedItems)
                    {
                        DataRow newRow = newDt.NewRow();
                        FillDataRowGift(newRow, group, num_doc);
                        newDt.Rows.Add(newRow);
                    }

                    // Добавление отфильтрованных товаров во временную таблицу
                    foreach (DataRow row in tempDt.Rows)
                    {
                        newDt.ImportRow(row);
                    }
                }
                finally
                {
                    newDt.EndLoadData();
                }

                // Если всё прошло успешно, заменяем оригинальную таблицу новой
                dt.Clear();
                foreach (DataRow row in newDt.Rows)
                {
                    dt.ImportRow(row);
                }

                if (isActionApplied)
                {
                    // Помечаем товары, участвующие в акции
                    await marked_action_tovar_dt(dt, num_doc, comment, actionPricesByDoc);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                MainStaticClass.WriteRecordErrorLog(ex, num_doc, MainStaticClass.CashDeskNumber, "Акция 4 типа без обращения к бд");

                // Восстановление исходного состояния таблицы
                dt.Clear();
                foreach (DataRow row in originalDt.Rows)
                {
                    dt.ImportRow(row);
                }

                throw; // Повторно выбрасываем исключение
            }
            finally
            {
                // Освобождение ресурсов
                originalDt.Dispose();
                tempDt.Dispose();
            }
        }

        /// <summary>
        /// Заполняет строку DataRow данными из GroupedItem.
        /// </summary>
        /// <param name="row">Строка DataRow для заполнения.</param>
        /// <param name="item">Группированный товар.</param>
        /// <param name="num_doc">Номер документа акции.</param>
        private void FillDataRowGift(DataRow row, GroupedItem item, int num_doc)
        {
            row["tovar_code"] = item.Code;
            row["tovar_name"] = item.TovarName ?? string.Empty; // Заполняем наименование товара
            row["characteristic_name"] = item.CharName ?? string.Empty;
            row["characteristic_code"] = item.CharGuid ?? string.Empty;
            row["quantity"] = item.Count;
            row["price"] = item.Price;
            row["price_at_discount"] = item.Discount;
            row["sum_full"] = item.SumFull;
            row["sum_at_discount"] = item.SumDiscount;
            row["action"] = 0; // Указываем, что это акция
            row["gift"] = item.Gift; // Указываем, является ли товар подарком
            row["action2"] = item.Gift; //isActionApplied ? num_doc : 0 ; // Номер акции
            row["bonus_reg"] = 0m; // Бонусы (по умолчанию 0)
            row["bonus_action"] = 0m; // Бонусы акции (по умолчанию 0)
            row["bonus_action_b"] = 0m; // Дополнительные бонусы акции (по умолчанию 0)
            row["marking"] = item.Marking ?? "0"; // Маркировка товара (по умолчанию "0")
        }

        /// <summary>
        /// Обрабатывает список товаров, группирует их и определяет подарки.
        /// </summary>
        /// <param name="items">Список товаров для обработки.</param>
        /// <param name="num_doc">Номер документа акции.</param>
        /// <param name="sum">Количество товаров, необходимое для срабатывания акции.</param>
        /// <returns>Список сгруппированных товаров.</returns>
        private List<GroupedItem> ProcessItems(
    List<ItemData> items,
    int num_doc,
    int sum,
    out bool isActionApplied)
        {
            isActionApplied = false;
            var flatItems = new List<ItemData>();

            foreach (var item in items)
            {
                if (item.Quantity != Math.Floor(item.Quantity))
                    throw new ArgumentException("Quantity must be integer value");

                int quantity = (int)item.Quantity;

                for (int i = 0; i < quantity; i++)
                {
                    flatItems.Add(new ItemData
                    {
                        Code = item.Code,
                        TovarName = item.TovarName,
                        CharName = item.CharName,
                        CharGuid = item.CharGuid,
                        Price = item.Price,
                        Quantity = 1.0,
                        Marking = item.Marking // СОХРАНИТЬ МАРКИРОВКУ
                    });
                }
            }

            // РАЗДЕЛИМ НА МАРКИРОВАННЫЕ И НЕМАРКИРОВАННЫЕ ДЛЯ ОБРАБОТКИ
            var markedItems = flatItems.Where(x => x.Marking != "0").ToList();
            var nonMarkedItems = flatItems.Where(x => x.Marking == "0").ToList();

            // ДЛЯ РАСЧЕТА АКЦИИ ИСПОЛЬЗУЕМ ВСЕ ТОВАРЫ
            var allItemsForCalculation = flatItems;
            allItemsForCalculation.Sort((a, b) => a.Price.CompareTo(b.Price));

            var groups = new Dictionary<string, GroupedItem>();

            // ОБРАБОТКА ВСЕХ ТОВАРОВ ДЛЯ РАСЧЕТА АКЦИИ
            for (int i = 0; i < allItemsForCalculation.Count; i++)
            {
                var item = allItemsForCalculation[i];
                bool isGift = (i % sum) == 0 && i + sum <= allItemsForCalculation.Count;

                if (isGift)
                {
                    isActionApplied = true;
                }

                decimal price = isGift
                    ? Convert.ToDecimal(GetGiftPrice(num_doc))
                    : Convert.ToDecimal(item.Price);

                // ЕСЛИ ТОВАР МАРКИРОВАН - ОН НИКОГДА НЕ ГРУППИРУЕТСЯ
                // ДЛЯ МАРКИРОВАННЫХ КЛЮЧ ДОЛЖЕН БЫТЬ УНИКАЛЬНЫМ
                string key;
                if (item.Marking != "0")
                {
                    // УНИКАЛЬНЫЙ КЛЮЧ ДЛЯ КАЖДОГО МАРКИРОВАННОГО ТОВАРА
                    key = $"{item.Code}|{item.CharName}|{item.CharGuid}|{item.Price}|{price}|{isGift}|{item.Marking}|{Guid.NewGuid()}";
                }
                else
                {
                    // ГРУППИРОВКА ДЛЯ НЕМАРКИРОВАННЫХ
                    key = $"{item.Code}|{item.CharName}|{item.CharGuid}|{item.Price}|{price}|{isGift}|{item.Marking}";
                }

                if (!groups.TryGetValue(key, out GroupedItem group))
                {
                    group = new GroupedItem
                    {
                        Code = item.Code,
                        TovarName = item.TovarName,
                        CharName = item.CharName,
                        CharGuid = item.CharGuid,
                        Price = item.Price,
                        Discount = price,
                        Action = 0,
                        Gift = isGift ? num_doc : 0,
                        Count = 0.0,
                        SumFull = 0.0m,
                        SumDiscount = 0.0m,
                        Marking = item.Marking // СОХРАНИТЬ МАРКИРОВКУ
                    };
                    groups[key] = group;
                }

                group.Count += 1.0;
                group.SumFull += Convert.ToDecimal(item.Price);
                group.SumDiscount += price;
            }

            return groups.Values.ToList();
        }

        /*Эта акция работает только по предъявлению 
        * акционного купона то есть по штрихкоду
        * если в чеке есть товары из списка. в случае, если сумма этих товаров в чеке, меньше суммы, 
        * на которую даётся скидка, разница покупателю не возвращается, а цены товаров ставятся равными 0.01грн.; 
        * скидка выдаётся при предъявлении специального купона с ШК. в случае, если в чеке вообще нет товаров, 
        * на которые должна выдаваться скидка, то выдаётся предупреждение и купон не используется. 
        * в одном чеке может использоваться только один акционный купон!!!
        * имеется ввиду что сколько раз не предъявляй купон скидка не накапливается
        */
        private void action_5_dt(int num_doc, decimal sum, string comment)
        {
            // 1 сНачала надо проверить сработку акции
            //bool the_action_has_worked = false;
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            Int16 result = 0;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                //Поскольку документ не записан еще найти строки которые могут участвовать в акции можно только последовательным перебором 
                string query = "";
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем                  
                    {
                        continue;
                    }
                    query = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"] + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query, conn);
                    result = Convert.ToInt16(command.ExecuteScalar());

                    if (result == 1)//Сработала акция
                    {
                        have_action = true;//Признак того что в документе есть сработка по акции

                        decimal the_sum_without_a_discount = Convert.ToDecimal(row["sum_full"]);
                        if (sum >= the_sum_without_a_discount)
                        {
                            sum = sum - the_sum_without_a_discount;
                            row["price_at_discount"] = (Convert.ToDecimal(1 / 100)).ToString();
                            row["sum_full"] = ((Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price"])).ToString());
                            row["sum_at_discount"] = ((Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price_at_discount"])).ToString());
                            row["action"] = num_doc.ToString(); //Номер акционного документа
                            row["action2"] = num_doc.ToString(); //Номер акционного документа                           
                        }
                        else
                        {
                            //Здесь сначала получаем сумму путем вычитания оставшейся суммы скидки 
                            //от суммы без скидки, а только затем получаем цену со скидкой
                            row["sum_at_discount"] = Convert.ToDecimal(the_sum_without_a_discount - sum).ToString();//Сумма со скидкой
                            row["price_at_discount"] = Math.Round(Convert.ToDecimal(row["sum_at_discount"]) / Convert.ToDecimal(row["quantity"]), 2).ToString();
                            row["action"] = num_doc.ToString(); //Номер акционного документа
                            row["action2"] = num_doc.ToString(); //Номер акционного документа
                            sum = 0; break; //Поскольку сумма скидки закончилась прерываем цикл
                        }
                    }
                }
                //                conn.Close();
                /*Помечаем позиции которые 
                 * остались не помеченными 
                 * при сработке акции                 
                 */
                marked_action_tovar_dt(num_doc, comment);

            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ошибка при обработке 5 типа акций");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    // conn.Dispose();
                }
            }
        }

        /*новый тип акции (6): за каждые ***руб. внутри чека (товаров из определённого списка) выдаётся сообщение типа 
        * "акция такая-то сработала *** раз, выдайте *** подарков" 
        * (в реальной акции, которая должна быть, будут выдаваться стикера, которые наклеиваются на купон, когда человек 
        * собирает 10 стикеров на этом купоне, то может обменять этот купон с наклеенными стикерами на подарочный комплект,
        * состав может быть разный, контролировать будет кассир)
        * 
        */
        private void action_6_dt(int num_doc, string comment, decimal sum, Int32 marker)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            Int16 result = 0;
            decimal action_sum_of_this_document = 0;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                //Поскольку документ не записан еще найти строки которые могут участвовать в акции можно только последовательным перебором 
                string query = "";
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем                  
                    {
                        continue;
                    }
                    query = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"] + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query, conn);
                    result = Convert.ToInt16(command.ExecuteScalar());
                    if (result == 1)//Акция сработала
                    {
                        have_action = true;//Признак того что в документе есть сработка по акции

                        action_sum_of_this_document += Convert.ToDecimal(row["sum_full"]);
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при работе с базой данных");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ошибка при обработке 6 типа акций");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            int quantity_of_gifts = (int)(action_sum_of_this_document / sum);
            //decimal quantity_of_gifts = Math.Round(action_sum_of_this_document / sum,0);

            if (quantity_of_gifts > 0)//значит акция сработала
            {
                if (show_messages)
                {
                    MessageBox.Show(comment + " количество подарков = " + quantity_of_gifts.ToString() + " шт. ", " АКЦИЯ !!!");
                }
                /*акция сработала
             * надо отметить все товарные позиции 
             * чтобы они не участвовали в других акциях 
             */
                if (marker == 1)
                {
                    for (int i = 0; i < quantity_of_gifts; i++)
                    {
                        ShowQueryWindowBarcode(2, 1, num_doc, 0);
                    }
                }
                marked_action_tovar_dt(num_doc, comment);
            }
            roll_up_dt();
        }

        /*новый тип акции (7). для фиксирования выданных подарков по акции (6)
         * человек приносит купон и выбирает определённые товары с полки (согласно условиям акции),
         * кассир пробивает эти товары (подарки) в отдельный чек и проводит ШК купона по сканеру.
         * в чеке обнуляются цены на эти товары и добавляется строка типа "ПОДАРОК 0.01грн" - 4шт. 
         * (количество равно количеству товаров-подарков). кассир сам будет контролировать товарный состав чека, точнее, чтобы количество 
         * подарков было такое, которое позволяет купон со стикерами, если вдруг в чеке будут обычные товары, то их цена не должна меняться 
         * или можно выдать предупреждение, что для этого типа акции наличие в чеке не акционных товаров недопустимо.
         */
        //private void action_7(int num_doc, long code_tovar)
        //{
        //    NpgsqlConnection conn = null;
        //    NpgsqlCommand command = null;
        //    int gift = 0;
        //    Int16 result = 0;
        //    try
        //    {
        //        conn = MainStaticClass.NpgsqlConn();
        //        conn.Open();
        //        //Поскольку документ не записан еще найти строки которые могут участвовать в акции можно только последовательным перебором 
        //        string query = "";
        //        foreach (DataRow row in dt.Rows)
        //        {
        //            query = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"] + " AND num_doc=" + num_doc.ToString();
        //            command = new NpgsqlCommand(query, conn);
        //            result = Convert.ToInt16(command.ExecuteScalar());
        //            if (result > 0)
        //            {
        //                have_action = true;//Признак того что в документе есть сработка по акции

        //                //lvi.SubItems[4].Text = ((decimal)1 / 100).ToString();
        //                row["sum_full"] = "0";
        //                row["sum_at_discount"] = "0"; //((Convert.ToDecimal(lvi.SubItems[2].Text) * Convert.ToDecimal(lvi.SubItems[4].Text)).ToString());
        //                row["action"] = num_doc.ToString(); //Номер акционного документа
        //                row["action"] = num_doc.ToString(); //Номер акционного документа
        //                gift += Convert.ToInt32(row["quantity"]);
        //            }
        //            //else
        //            //{
        //            //    MessageBox.Show("Обнаружен неакционный товар " + row["code"]);
        //            //}
        //        }
        //        find_barcode_or_code_in_tovar_action_dt(code_tovar.ToString(), gift, false, num_doc, 0);
        //        //                conn.Close();
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        MessageBox.Show(ex.Message, "Ошибка при работе с базой данных");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, "ошибка при обработке 7 типа акций");
        //    }
        //    finally
        //    {
        //        if (conn.State == ConnectionState.Open)
        //        {
        //            conn.Close();
        //            // conn.Dispose();
        //        }
        //    }
        //}

        /// <summary>
        /// При покупке указанного количества 
        /// товаров покупатель может получить 
        /// указанную скидку на эти товары 
        /// либо указанный подарок,
        /// здесь дается скидка на все позиции 
        /// из списка
        ///  
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="persent"></param>
        /// <param name="sum"></param>
        private async Task action_8_dt(int num_doc, decimal persent, decimal sum, string comment)
        {

            if (!await create_temp_tovar_table_4())
            {
                return;
            }

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlCommand command = null;
            string query_string = "";
            StringBuilder query = new StringBuilder();
            decimal quantity_on_doc = 0;
            DataTable dt2 = dt.Copy();
            dt2.Rows.Clear();


            try
            {

                conn.Open();
                //ListView clon = new ListView();
                //int total_quantity = 0;
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем                  
                    {
                        DataRow row2 = dt2.NewRow();
                        row2.ItemArray = row.ItemArray;
                        dt2.Rows.Add(row2);
                        continue;
                    }

                    query_string = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"] + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query_string, conn);

                    if (Convert.ToInt16(command.ExecuteScalar()) != 0)
                    {

                        for (int i = 0; i < Convert.ToInt32(row["quantity"]); i++)
                        {
                            query.Append("INSERT INTO tovar_action(code, retail_price, quantity,characteristic_name,characteristic_guid)VALUES(" +
                                row["tovar_code"].ToString() + "," +
                                row["price"].ToString().Replace(",", ".") + "," +
                               "1,'" +
                               row["characteristic_name"].ToString() + "','" +
                               row["characteristic_code"].ToString() + "');");
                        }
                        quantity_on_doc += Convert.ToDecimal(row["quantity"]);
                    }
                    else//Не участвует в акции убираем пока в сторонку
                    {
                        DataRow row2 = dt2.NewRow();
                        row2.ItemArray = row.ItemArray;
                        dt2.Rows.Add(row2);
                    }
                }


                if (quantity_on_doc >= sum)//Есть вхождение в акцию
                {
                    have_action = true;//Признак того что в документе есть сработка по акции                    
                    dt.Rows.Clear();
                    foreach (DataRow row2 in dt2.Rows)
                    {
                        DataRow row = dt.NewRow();
                        row.ItemArray = row2.ItemArray;
                        dt.Rows.Add(row);
                    }
                    //have_action = true;//Признак того что в документе есть сработка по акции                    

                    command = new NpgsqlCommand(query.ToString(), conn);//устанавливаем акционные позиции во временную таблицу
                    command.ExecuteNonQuery();
                    //query = new StringBuilder();
                    //query.Append("DELETE FROM tovar_action;");//Очищаем таблицу акционных товаров 
                    //иначе результат задваивается ранее эта строка была закомментирована и при 2 товарах по 1 шт. учавстсующих в акции
                    //работала неверно

                    //int multiplication_factor = (int)(quantity_on_doc / sum)-1;
                    //query_string = " SELECT code, retail_price, quantity FROM tovar_action ORDER BY retail_price ";//запросим товары отсортированные по цене
                    query_string = " SELECT tovar_action.code,tovar.name, tovar.retail_price,tovar_action.retail_price, quantity FROM tovar_action LEFT JOIN tovar ON tovar_action.code=tovar.code ";//запросим товары отсортированные по цене
                    command = new NpgsqlCommand(query_string, conn);
                    NpgsqlDataReader reader = command.ExecuteReader();
                    decimal _sum_ = sum;
                    while (reader.Read())
                    {

                        //if ((multiplication_factor > 0) || (_sum_ > 0))
                        //{
                        //    if ((_sum_ == 0) && (multiplication_factor > 0))
                        //    {
                        //        _sum_ = sum;
                        //        multiplication_factor--;
                        //    }
                        //    _sum_ -= 1;

                        DataRow row = dt.NewRow();
                        row["tovar_code"] = reader[0].ToString();
                        row["tovar_name"] = reader[1].ToString().Trim();
                        row["characteristic_name"] = "";// reader[5].ToString();
                        row["characteristic_code"] = "";// reader[6].ToString();
                        row["quantity"] = reader[4].ToString().Trim();
                        row["price"] = reader.GetDecimal(2).ToString();
                        row["price_at_discount"] = Math.Round(reader.GetDecimal(2) - reader.GetDecimal(2) * persent / 100, 2).ToString();// get_price_action(num_doc);
                        row["sum_full"] = (Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price"])).ToString();
                        row["sum_at_discount"] = (Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price_at_discount"])).ToString();
                        if (Convert.ToDecimal(row["price"]) != Convert.ToDecimal(row["price_at_discount"]))
                        {
                            row["action"] = num_doc.ToString();
                        }
                        else
                        {
                            row["action"] = "0";
                        }
                        row["gift"] = "0";
                        row["action2"] = num_doc.ToString();
                        row["bonus_reg"] = 0;
                        row["bonus_action"] = 0;
                        row["bonus_action_b"] = 0;
                        row["marking"] = "0";
                        dt.Rows.Add(row);
                        //multiplication_factor--;

                        //}
                        //else
                        //{
                        //    DataRow row = dt.NewRow();
                        //    row["tovar_code"] = reader[0].ToString();
                        //    row["tovar_name"] = reader[1].ToString().Trim();
                        //    row["characteristic_name"] = "";// reader[5].ToString();
                        //    row["characteristic_code"] = "";// reader[6].ToString();
                        //    row["quantity"] = reader[4].ToString().Trim();
                        //    row["price"] = reader.GetDecimal(2).ToString();
                        //    row["price_at_discount"] = reader.GetDecimal(2).ToString();
                        //    row["sum_full"] = (Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price"])).ToString();
                        //    row["sum_at_discount"] = (Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price_at_discount"])).ToString();
                        //    if (Convert.ToDecimal(row["price"]) != Convert.ToDecimal(row["price_at_discount"]))
                        //    {
                        //        row["action"] = num_doc.ToString();
                        //    }
                        //    else
                        //    {
                        //        row["action"] = "0";
                        //    }
                        //    row["gift"] = "0";
                        //    row["action2"] = num_doc.ToString();
                        //    row["bonus_reg"] = 0;
                        //    row["bonus_action"] = 0;
                        //    row["bonus_action_b"] = 0;
                        //    row["marking"] = "0";
                        //    dt.Rows.Add(row);
                        //    multiplication_factor--;
                        //}
                    }
                    /*акция сработала
             * надо отметить все товарные позиции 
             * чтобы они не участвовали в других акциях 
             */
                    marked_action_tovar_dt(num_doc, comment);
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message, " 8 акция ");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, " 8 акция ");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    // conn.Dispose();
                }
            }
            roll_up_dt();
        }

        /// <summary>
        /// При покупке указанного количества 
        /// товаров покупатель может получить 
        /// указанную скидку на эти товары 
        /// либо указанный подарок,
        /// здесь выдается подарок
        /// из списка
        /// если маркер =1 и заполнен код товара то будет выдан запрос
        /// на ввод кода или штрихкода и ему будет проставлена цена 
        /// кода товара
        /// Проверено 23.05.2019
        ///  
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="persent"></param>
        /// <param name="sum"></param>
        private async Task action_8_dt(int num_doc, string comment, decimal sum, Int32 marker)
        {

            if (!await create_temp_tovar_table_4())
            {
                return;
            }

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlCommand command = null;
            string query_string = "";
            //StringBuilder query = new StringBuilder();
            decimal quantity_on_doc = 0;

            try
            {

                conn.Open();
               
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем                  
                    {
                        continue;
                    }

                    query_string = "SELECT COUNT(*) FROM action_table WHERE code_tovar=" + row["tovar_code"] + " AND num_doc=" + num_doc.ToString();
                    command = new NpgsqlCommand(query_string, conn);

                    if (Convert.ToInt16(command.ExecuteScalar()) != 0)
                    {
                        quantity_on_doc += Convert.ToDecimal(row["quantity"]);
                    }
                }

                if (quantity_on_doc >= sum)//Есть вхождение в акцию
                {
                    have_action = true;//Признак того что в документе есть сработка по акции
                    int multiplication_factor = (int)(quantity_on_doc / sum);
                    await MessageBox.Show(comment.Trim() + " количество подарков = " + multiplication_factor.ToString() + " шт. ", " АКЦИЯ !!!");
                    if (marker == 1)
                    {
                        for (int i = 0; i < multiplication_factor; i++)
                        {
                            await ShowQueryWindowBarcode(2, 1, num_doc, 0);
                        }
                    }
                    marked_action_tovar_dt(num_doc, comment);
                }
                roll_up_dt();
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message, " 8 акция ");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, " 8 акция ");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    // conn.Dispose();
                }
            }
        }

        /// <summary>
        /// Здесь мы проверяем сумму по 1 списку товаров,
        /// если товар имеет вхождение в список мы 
        /// получаем сумму
        /// </summary>
        /// <param name="num_doc"></param>
        /// <param name="persent"></param>
        /// <param name="sum"></param>
        /// <param name="sum2"></param>
        private async Task action_12_dt(int num_doc, decimal persent, decimal sum, decimal sum1)
        {
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            decimal fact1 = 0;//фактическая сумма в чеке по товарам из 1-го списка
            decimal fact2 = 0;//фактическая сумма в чеке по товарам из 2-го списка
            int count_list = 0;//количество списков в условии акций

            DataTable table_list1 = new DataTable();

            DataColumn tovar_code = new DataColumn();
            tovar_code.DataType = System.Type.GetType("System.Double");
            tovar_code.ColumnName = "tovar_code";
            table_list1.Columns.Add(tovar_code);

            DataColumn sum_at_discount = new DataColumn();
            sum_at_discount.DataType = System.Type.GetType("System.Double");
            sum_at_discount.ColumnName = "sum_at_discount";
            table_list1.Columns.Add(sum_at_discount);

            DataTable table_list2 = new DataTable();

            tovar_code = new DataColumn();
            tovar_code.DataType = System.Type.GetType("System.Double");
            tovar_code.ColumnName = "tovar_code";
            table_list2.Columns.Add(tovar_code);

            sum_at_discount = new DataColumn();
            sum_at_discount.DataType = System.Type.GetType("System.Double");
            sum_at_discount.ColumnName = "sum_at_discount";
            table_list2.Columns.Add(sum_at_discount);

            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();

                string query = "DROP TABLE IF EXISTS table12;CREATE TEMP TABLE table12(tovar_code bigint,sum_at_a_discount numeric(12, 2));";

                //Поскольку документ не записан еще найти строки которые могут участвовать в акции можно только последовательным перебором 
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["action2"]) > 0)//Этот товар уже участвовал в акции значит его пропускаем
                    {
                        continue;
                    }
                    query += "INSERT INTO table12(tovar_code,sum_at_a_discount)VALUES(" + row["tovar_code"].ToString() + "," + row["sum_at_discount"].ToString().Replace(",", ".") + ");";
                }

                command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();

                query = " SELECT MAX(num_list)AS count_list FROM action_table WHERE num_doc =" + num_doc + ";" +

                    " Select coalesce(num_list,2)AS num_list,SUM(sum_at_a_discount) AS sum_at_a_discount" +
                    " FROM(SELECT code_tovar, coalesce(num_list, 2) AS num_list, num_doc" +
                    " FROM action_table WHERE action_table.num_doc =" + num_doc + ") AS Action12" +
                    " FULL JOIN  table12 ON tovar_code = code_tovar" +
                    " GROUP BY coalesce(num_list, 2);" +

                    " Select coalesce(CASE WHEN Action12.num_list = 1 THEN" +
                    " tovar_code::varchar(255)" +
                    " WHEN Action12.num_list = 2 OR Action12.num_list = NULL THEN" +
                    " 'num_list2'" +
                    " END,'0') AS code_action, coalesce(num_list, 2)AS num_list, sum_at_a_discount" +
                    " FROM(SELECT code_tovar, coalesce(num_list, 2) AS num_list, num_doc" +
                    " FROM action_table WHERE action_table.num_doc =" + num_doc + ") AS Action12" +
                    " FULL JOIN  table12 ON tovar_code = code_tovar;" +

                    " Select coalesce(CASE WHEN Action12.num_list = 1 THEN" +
                    " tovar_code::varchar(255)" +
                    " WHEN Action12.num_list = 2 OR Action12.num_list = NULL THEN" +
                    " 'num_list2'" +
                    " END,'0') AS code_action, coalesce(num_list, 2), sum_at_a_discount" +
                    " FROM(SELECT code_tovar, coalesce(num_list, 2) AS num_list, num_doc" +
                    " FROM action_table WHERE action_table.num_doc =" + num_doc + ") AS Action12" +
                    " LEFT JOIN  table12 ON tovar_code = code_tovar; ";

                command = new NpgsqlCommand(query, conn);
                //command.Transaction = tran;
                NpgsqlDataReader reader = command.ExecuteReader();
                Int16 num_query = 1;
                while (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        if (num_query == 1)
                        {
                            count_list = Convert.ToUInt16(reader["count_list"]);
                        }
                        else if (num_query == 2)
                        {
                            if (Convert.ToInt16(reader["num_list"]) == 1)
                            {
                                fact1 = Convert.ToDecimal(reader["sum_at_a_discount"]);
                            }
                            else if (Convert.ToInt16(reader["num_list"]) == 2)
                            {
                                fact2 = Convert.ToDecimal(reader["sum_at_a_discount"]);
                            }
                        }
                        else if (num_query == 3)
                        {
                            if (Convert.ToInt16(reader["num_list"]) == 1)
                            {
                                DataRow new_row = table_list1.NewRow();
                                new_row["tovar_code"] = Convert.ToInt64(reader["code_action"]);
                                new_row["sum_at_discount"] = Convert.ToInt64(reader["sum_at_a_discount"]);
                                table_list1.Rows.Add(new_row);
                            }
                            else if (Convert.ToInt16(reader["num_list"]) == 2)
                            {
                                DataRow new_row = table_list2.NewRow();
                                new_row["tovar_code"] = Convert.ToInt64(reader["code_action"]);
                                new_row["sum_at_discount"] = Convert.ToInt64(reader["sum_at_a_discount"]);
                                table_list2.Rows.Add(new_row);
                            }
                        }
                    }
                    reader.NextResult();
                    num_query++;
                }

                int multiplicity = (int)(fact2 / sum1);//это кратность суммы скидки, а именно скидка дана будет товарам на сумму multiplicity*sum

                if ((multiplicity > 0) && (fact1 > 0))//Выполнились условия для сработки акции
                {
                    decimal action_sum = multiplicity * sum;
                    foreach (DataRow row_list1 in table_list1.Rows)
                    {
                        if (action_sum == 0)
                        {
                            break;
                        }
                        foreach (DataRow row_dt in dt.Rows)
                        {
                            if (action_sum == 0)
                            {
                                break;
                            }
                            if (Convert.ToDouble(row_list1["tovar_code"]) == Convert.ToDouble(row_dt["tovar_code"]))//на эту строку необходимо дать скидку но проверить сумму 
                            {
                                if (action_sum >= Convert.ToDecimal(row_dt["sum_at_discount"]))//сумма в строке меньше чем сумма на которую должна распространиться скидка
                                {
                                    action_sum = action_sum - Convert.ToDecimal(row_dt["sum_at_discount"]);
                                    row_dt["price_at_discount"] = Math.Round(Convert.ToDecimal(Convert.ToDecimal(row_dt["price"]) - Convert.ToDecimal(row_dt["price"]) * persent / 100), 2);//Цена со скидкой                                                                        
                                    row_dt["sum_at_discount"] = Convert.ToDecimal(row_dt["quantity"]) * Convert.ToDecimal(row_dt["price_at_discount"]);//сумма со скидкой                                    
                                    row_dt["action"] = num_doc.ToString(); //Номер акционного документа                        
                                    row_dt["action2"] = num_doc.ToString();//Тип акции 
                                }
                                else if (action_sum < Convert.ToDecimal(row_dt["sum_at_discount"]))//сумма в строке больше чем сумма на которую должна распространиться скидка, значит необходимо дать скидку на какое то число товаров
                                {
                                    int required_quantity = (int)(action_sum / Convert.ToDecimal(row_dt["price"]));//это то количество товара на которе будет дана скидка, строка разделится надвое

                                    row_dt["quantity"] = Convert.ToInt32(row_dt["quantity"]) - Convert.ToInt32(required_quantity);
                                    row_dt["sum_full"] = (Convert.ToDecimal(row_dt["quantity"]) * Convert.ToDecimal(row_dt["price"])).ToString();
                                    row_dt["sum_at_discount"] = (Convert.ToDecimal(row_dt["quantity"]) * Convert.ToDecimal(row_dt["price_at_discount"])).ToString();

                                    DataRow row = dt.NewRow();

                                    row["tovar_code"] = Convert.ToInt64(row_dt["tovar_code"]);
                                    row["tovar_name"] = row_dt["tovar_name"].ToString();
                                    row["characteristic_code"] = row_dt["characteristic_code"].ToString();
                                    row["characteristic_name"] = row_dt["characteristic_name"].ToString();
                                    row["quantity"] = required_quantity;
                                    row["price"] = Convert.ToDecimal(row_dt["price"]);
                                    row["price_at_discount"] = Math.Round(Convert.ToDecimal(Convert.ToDecimal(row["price"]) - Convert.ToDecimal(row["price"]) * persent / 100), 2);//Цена со скидкой                                                                        
                                    row["sum_full"] = Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price"]);
                                    row["sum_at_discount"] = Convert.ToDecimal(row["quantity"]) * Convert.ToDecimal(row["price_at_discount"]);
                                    row["action"] = num_doc.ToString(); //Номер акционного документа                        
                                    row["gift"] = "0";
                                    row["action2"] = num_doc.ToString();//Тип акции 
                                    row["bonus_reg"] = 0;
                                    row["bonus_action"] = 0;
                                    row["bonus_action_b"] = 0;
                                    row["marking"] = "0";

                                    dt.Rows.Add(row);

                                    action_sum = 0;
                                    break;
                                }
                            }
                        }
                    }
                    if (count_list == 2)
                    {
                        foreach (DataRow row_list2 in table_list2.Rows)//отметить позиции 2 списка как участвовавшие в акции
                        {
                            foreach (DataRow row_dt in dt.Rows)
                            {
                                if (Convert.ToDouble(row_list2["tovar_code"]) == Convert.ToDouble(row_dt["tovar_code"]))//на эту строку необходимо дать скидку но проверить сумму 
                                {
                                    row_dt["action2"] = num_doc.ToString();//Тип акции 
                                }
                            }
                        }
                    }
                    else if (count_list == 1)//отметить позиции 2 списка как участвовавшие в акции, в данном варианте 2 список это весь товарный состав
                    {
                        foreach (DataRow row_dt in dt.Rows)
                        {
                            if (row_dt["action2"].ToString() != num_doc.ToString())
                            {
                                row_dt["action2"] = num_doc.ToString();//Тип акции 
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                await MessageBox.Show(ex.Message + " | ошибка при обработке 12 типа акций ");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "ошибка при обработке 12 типа акций");
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
        }

        private void roll_up_dt()
        {
            //return;
            string query = "";
            NpgsqlConnection conn = null;
            NpgsqlCommand command = null;
            NpgsqlTransaction trans = null;
            try
            {
                conn = MainStaticClass.NpgsqlConn();
                conn.Open();
                trans = conn.BeginTransaction();
                query = "DROP TABLE IF EXISTS roll_up_temp;" +
                    " CREATE TEMP TABLE roll_up_temp " +
                    " (code_tovar bigint," +
                    " name_tovar character varying(200) COLLATE pg_catalog.default," +
                    " characteristic_guid character varying(36) COLLATE pg_catalog.default," +
                    " characteristic_name character varying(200) COLLATE pg_catalog.default," +
                    " quantity numeric(10, 3)," +
                    " price numeric(10, 2)," +
                    " price_at_a_discount numeric(10,2)," +
                    " sum numeric(10,2)," +
                    " sum_at_a_discount numeric(10,2)," +
                    " action_num_doc integer," +
                    " action_num_doc1 integer," +
                    " action_num_doc2 integer," +
                    " item_marker character varying(200) COLLATE pg_catalog.default);";
                command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                command.ExecuteNonQuery();

                foreach (DataRow row in dt.Rows)
                {
                    query = "INSERT INTO roll_up_temp(code_tovar," +
                        " name_tovar, " +
                        "characteristic_guid, " +
                        "characteristic_name, " +
                        "quantity, " +
                        "price, " +
                        "price_at_a_discount, " +
                        "sum, " +
                        "sum_at_a_discount, " +
                        "action_num_doc, " +
                        "action_num_doc1, " +
                        "action_num_doc2, " +
                        "item_marker)VALUES(" +
                        row["tovar_code"] + ",'" +
                        row["tovar_name"] + "','" +
                        row["characteristic_code"] + "','" +
                        row["characteristic_name"] + "'," +
                        row["quantity"].ToString().Replace(",", ".") + "," +
                        row["price"].ToString().Replace(",", ".") + "," +
                        row["price_at_discount"].ToString().Replace(",", ".") + "," +
                        row["sum_full"].ToString().Replace(",", ".") + "," +
                        row["sum_at_discount"].ToString().Replace(",", ".") + "," +
                        row["action"] + "," +
                        row["gift"] + "," +
                        row["action2"] + ",'" +
                        row["marking"].ToString().Replace("'", "vasya2021") + "')";

                    command = new NpgsqlCommand(query, conn);
                    command.Transaction = trans;
                    command.ExecuteNonQuery();
                }

                // ИЗМЕНЕНИЕ ЗДЕСЬ: добавили item_marker в GROUP BY
                query = "SELECT code_tovar, name_tovar, characteristic_guid, characteristic_name, SUM(quantity) AS quantity, price," +
                    " price_at_a_discount, SUM(sum), SUM(sum_at_a_discount), action_num_doc, action_num_doc1, action_num_doc2, item_marker" +
                    " FROM roll_up_temp" +
                    " GROUP BY code_tovar, name_tovar, characteristic_guid, characteristic_name, price," +
                    " price_at_a_discount, action_num_doc, action_num_doc1, action_num_doc2, item_marker;";

                command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                NpgsqlDataReader reader = command.ExecuteReader();

                dt.Rows.Clear();
                while (reader.Read())
                {
                    DataRow row = dt.NewRow();
                    row["tovar_code"] = reader.GetInt64(0);
                    row["tovar_name"] = reader[1].ToString().Trim();
                    row["characteristic_code"] = reader[2].ToString();
                    row["characteristic_name"] = reader[3].ToString();
                    row["quantity"] = reader.GetDecimal(4);
                    row["price"] = reader.GetDecimal(5);
                    row["price_at_discount"] = reader.GetDecimal(6);
                    row["sum_full"] = reader.GetDecimal(7);
                    row["sum_at_discount"] = reader.GetDecimal(8);
                    row["action"] = reader.GetInt32(9);
                    row["gift"] = reader.GetInt32(10);
                    row["action2"] = reader.GetInt32(11);
                    row["bonus_reg"] = 0;
                    row["bonus_action"] = 0;
                    row["bonus_action_b"] = 0;
                    row["marking"] = reader[12].ToString().Replace("vasya2021", "'");

                    dt.Rows.Add(row);
                }
                reader.Close();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }
    }
}
