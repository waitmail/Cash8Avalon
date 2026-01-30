using Cash8Avalon;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public partial class SendDataOnSalesPortions
    {
        private SalesPortions salesPortions = null;

        private string nick_shop = "";
        public bool show_messages = false;
        private StringBuilder document_guid_list;
        private bool were_mistakes = false;//были ошибки
        //private DataTable dt = null;

        public SendDataOnSalesPortions()
        {
            //    InitializeComponent();
            //    this.Load += new EventHandler(SendDataOnSales_Load);
            nick_shop = MainStaticClass.Nick_Shop.Trim();
            if (nick_shop.Trim().Length == 0)
            {
                if (show_messages)
                {
                    //await MessageBox.Show(" Не удалось получить название магазина ");
                }                
            }
        }

        //private void SendDataOnSales_Load(object sender, EventArgs e)
        //{
        //    nick_shop = MainStaticClass.Nick_Shop.Trim();
        //    if (nick_shop.Trim().Length == 0)
        //    {
        //        if (show_messages)
        //        {
        //            MessageBox.Show(" Не удалось получить название магазина ");
        //        }
        //        this.Close();
        //    }
        //}

        private string get_numDocOnGuid(string guid)
        {
            // Добавляем проверку на null или пустую строку
            if (string.IsNullOrWhiteSpace(guid))
            {
                return "";
            }

            string result = "";
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                // Используем параметризованный запрос для защиты от SQL-инъекций
                string query = "SELECT document_number FROM checks_header WHERE guid = @guid";
                conn.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@guid", guid);

                    var scalarResult = command.ExecuteScalar();

                    // Проверяем на null перед вызовом ToString()
                    if (scalarResult != null && scalarResult != DBNull.Value)
                    {
                        result = scalarResult.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // Лучше добавить логирование ошибки
                // Logger.LogError(ex, "Ошибка при получении document_number");
                result = "";
            }
            finally
            {
                // Закрываем соединение только если оно открыто
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return result;
        }

        private void getdata_h()
        {
            salesPortions.ListSalesPortionsHeader = new List<SalesPortionsHeader>();
            StringBuilder result = new StringBuilder();
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT document_number," +
                    " cash_desk_number," +
                    " client," +
                    " bonuses_it_is_counted," +
                    " discount," +
                    " cash," +
                    " check_type," +
                    " have_action," +
                    " date_time_start," +
                    " date_time_write," +
                    " its_deleted," +
                    " bonuses_it_is_written_off, " +
                    " action_num_doc, " +
                    " cash_money, " +
                    " non_cash_money, " +
                    " sertificate_money," +
                    //" sales_assistant,"+
                    " autor, " +
                    " comment, " +
                    " CASE WHEN its_print = true AND its_print_p = true THEN true else false end AS its_print , " +
                    " id_transaction," +
                    " id_transaction_sale," +
                    " remainder, " +
                    " bonuses_it_is_counted ," +
                    " id_sale, " +
                    //" viza_d, "+
                    " system_taxation," +
                    " cash_money1, " +
                    " non_cash_money1, " +
                    " sertificate_money1," +
                    " guid," +
                    " payment_by_sbp, " +
                    " clients.phone " +
                    " FROM checks_header LEFT JOIN clients ON checks_header.client=clients.code WHERE guid in  (" + document_guid_list.ToString() + ")  ";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    //Новое заполнение 
                    SalesPortionsHeader salesPortionsHeader = new SalesPortionsHeader();
                    salesPortionsHeader.Shop = nick_shop;
                    salesPortionsHeader.Num_doc = reader["document_number"].ToString();
                    salesPortionsHeader.Num_cash = reader["cash_desk_number"].ToString();
                    salesPortionsHeader.Client = reader["client"].ToString();
                    salesPortionsHeader.Bonus_counted = reader["bonuses_it_is_counted"].ToString().Replace(",", ".");
                    salesPortionsHeader.Discount = reader["discount"].ToString().Replace(",", ".");
                    salesPortionsHeader.Sum = reader["cash"].ToString().Replace(",", ".");
                    salesPortionsHeader.Check_type = reader["check_type"].ToString();
                    salesPortionsHeader.Have_action = Convert.ToBoolean(reader["have_action"]) ? "1" : "0";  //reader
                    salesPortionsHeader.Date_time_start = Convert.ToDateTime(reader["date_time_start"]).ToString("dd-MM-yyyy HH:mm:ss");
                    salesPortionsHeader.Date_time_write = Convert.ToDateTime(reader["date_time_write"]).ToString("dd-MM-yyyy HH:mm:ss");
                    salesPortionsHeader.Its_deleted = reader["its_deleted"].ToString();
                    salesPortionsHeader.Bonus_writen_off = reader["bonuses_it_is_written_off"].ToString().Replace(",", ".");
                    salesPortionsHeader.Action = reader["action_num_doc"].ToString();
                    salesPortionsHeader.Sum_cash = reader["cash_money"].ToString().Replace(",", ".");
                    salesPortionsHeader.Sum_terminal = reader["non_cash_money"].ToString().Replace(",", ".");
                    salesPortionsHeader.Sum_certificate = reader["sertificate_money"].ToString().Replace(",", ".");
                    //salesPortionsHeader.Sales_assistant = reader["sales_assistant"].ToString();
                    salesPortionsHeader.Autor = reader["autor"].ToString();
                    //if (salesPortionsHeader.Autor.Trim() == "")
                    //{
                    //    salesPortionsHeader.Autor = MainStaticClass.Cash_Operator_Client_Code;
                    //}
                    salesPortionsHeader.Comment = reader["comment"].ToString().Trim();
                    if (reader["its_print"].ToString() == "")
                    {
                        salesPortionsHeader.Its_print = "0";
                    }
                    else
                    {
                        salesPortionsHeader.Its_print = (Convert.ToBoolean(reader["its_print"]) == true ? "1" : "0");
                    }
                    salesPortionsHeader.Id_transaction = reader["id_transaction"].ToString();
                    salesPortionsHeader.Id_transaction_sale = reader["id_transaction_sale"].ToString();
                    salesPortionsHeader.SumCashRemainder = reader["remainder"].ToString().Replace(",", ".");
                    salesPortionsHeader.NumOrder4 = reader["id_sale"].ToString();
                    salesPortionsHeader.NumOrder = get_numDocOnGuid(reader["id_sale"].ToString());
                    //salesPortionsHeader.VizaD = reader["viza_d"].ToString();
                    //if (salesPortionsHeader.VizaD == "")
                    //{
                    //    salesPortionsHeader.VizaD = "0";
                    //}
                    salesPortionsHeader.SystemTaxation = reader["system_taxation"].ToString();
                    salesPortionsHeader.Sum_cash1 = reader["cash_money1"].ToString().Replace(",", ".");
                    salesPortionsHeader.Sum_terminal1 = reader["non_cash_money1"].ToString().Replace(",", ".");
                    salesPortionsHeader.Sum_certificate1 = reader["sertificate_money1"].ToString().Replace(",", ".");
                    salesPortionsHeader.Guid = reader["guid"].ToString();
                    salesPortionsHeader.SBP = (Convert.ToBoolean(reader["payment_by_sbp"]) == true ? 1 : 0).ToString();
                    salesPortionsHeader.ClientPhone = (reader["phone"].ToString() == "" ? reader["client"].ToString() : reader["phone"].ToString()).Replace("+7", "");
                    salesPortions.ListSalesPortionsHeader.Add(salesPortionsHeader);
                    //Конец Новое заполнение 
                    ////////////////////////////////////////////////////////////////////////
                    //DataRow row = dt.NewRow();
                    //row["guid"] = reader["guid"].ToString();
                    //row["sum_header"] = Convert.ToDouble(salesPortionsHeader.Sum_cash.Replace(".",",")) +
                    //    Convert.ToDouble(salesPortionsHeader.Sum_terminal.Replace(".", ",")) + 
                    //    Convert.ToDouble(salesPortionsHeader.Sum_certificate.Replace(".", ","));
                    //row["sum_table"] = 0;
                    //dt.Rows.Add(row);
                    ////////////////////////////////////////////////////////////////////////
                }
                conn.Close();
                reader.Close();
            }
            catch (NpgsqlException ex)
            {
                if (show_messages)
                {
                    MessageBox.Show(" getdata_h " + ex.Message);
                }
                were_mistakes = true;
            }
            catch (Exception ex)
            {
                if (show_messages)
                {
                    MessageBox.Show(" getdata_h " + ex.Message);
                }
                were_mistakes = true;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void getdata_t()
        {
            salesPortions.ListSalesPortionsTable = new List<SalesPortionsTable>();
            StringBuilder result = new StringBuilder();
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "SELECT checks_header.document_number,checks_header.cash_desk_number,checks_table.tovar_code,checks_table.quantity, checks_table.price," +
                    " checks_table.price_at_a_discount,checks_table.sum,checks_table.sum_at_a_discount,checks_table.action_num_doc," +
                    " checks_table.action_num_doc1, checks_table.action_num_doc2,checks_table.characteristic,checks_header.date_time_start,checks_table.numstr, " +
                    " checks_table.bonus_standard,checks_table.bonus_promotion,checks_table.promotion_b_mover,checks_table.item_marker,checks_header.guid " +
                    " FROM checks_header " +
                    " LEFT JOIN checks_table ON checks_header.guid = checks_table.guid " +
                    " WHERE checks_table.guid in (" + document_guid_list.ToString() + ")";

                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    //Новое заполнение 
                    SalesPortionsTable salesPortionsTable = new SalesPortionsTable();
                    salesPortionsTable.Shop = nick_shop;
                    salesPortionsTable.Num_doc = reader["document_number"].ToString();
                    salesPortionsTable.Num_cash = reader["cash_desk_number"].ToString();
                    salesPortionsTable.Tovar = reader["tovar_code"].ToString();
                    salesPortionsTable.Quantity = reader["quantity"].ToString().Replace(",", ".");
                    salesPortionsTable.Price = reader["price"].ToString().Replace(",", ".");
                    salesPortionsTable.Price_d = reader["price_at_a_discount"].ToString().Replace(",", ".");
                    salesPortionsTable.Sum = reader["sum"].ToString().Replace(",", ".");
                    salesPortionsTable.Sum_d = reader["sum_at_a_discount"].ToString().Replace(",", ".");
                    salesPortionsTable.Action1 = reader["action_num_doc"].ToString();
                    salesPortionsTable.Action2 = reader["action_num_doc1"].ToString();
                    salesPortionsTable.Action3 = reader["action_num_doc2"].ToString();
                    salesPortionsTable.Characteristic = reader["characteristic"].ToString();
                    salesPortionsTable.Date_time_write = Convert.ToDateTime(reader["date_time_start"]).ToString("dd-MM-yyyy HH:mm:ss");
                    salesPortionsTable.Num_str = reader["numstr"].ToString();
                    salesPortionsTable.Bonus_stand = (reader["bonus_standard"].ToString() == "" ? "0" : reader["bonus_standard"].ToString().Replace(",", "."));
                    salesPortionsTable.Bonus_prom = (reader["bonus_promotion"].ToString() == "" ? "0" : reader["bonus_promotion"].ToString().Replace(",", "."));
                    salesPortionsTable.Promotion_b_mover = (reader["promotion_b_mover"].ToString() == "" ? "0" : reader["promotion_b_mover"].ToString().Replace(",", "."));
                    salesPortionsTable.MarkingCode = reader["item_marker"].ToString();
                    salesPortionsTable.Guid = reader["guid"].ToString();

                    salesPortions.ListSalesPortionsTable.Add(salesPortionsTable);
                    //Конец Новой заполнение

                    //DataRow row = dt.NewRow();
                    //row["guid"] = reader["guid"].ToString();
                    //row["sum_header"] = 0;
                    //row["sum_table"] = Convert.ToDouble(salesPortionsTable.Sum_d.Replace(".", ","));
                    //dt.Rows.Add(row);
                }
                conn.Close();
                reader.Close();
            }
            catch (NpgsqlException ex)
            {
                if (show_messages)
                {
                    MessageBox.Show(" getdata_t " + ex.Message);
                }
                were_mistakes = true;
            }
            catch (Exception ex)
            {
                if (show_messages)
                {
                    MessageBox.Show(" getdata_t " + ex.Message);
                }
                were_mistakes = true;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        //private void check_sum_header_and_table()
        //{
        //    // Фильтрация строк и подсчет суммы
        //    var filteredData = from row in dt.AsEnumerable()
        //                       group row by row.Field<string>("guid") into grp
        //                       let total_sum_header = grp.Sum(r => r.Field<double>("sum_header"))
        //                       let total_sum_table = grp.Sum(r => r.Field<double>("sum_table"))
        //                       where total_sum_header != total_sum_table
        //                       select new
        //                       {
        //                           guid = grp.Key,
        //                           TotalSumHeader = total_sum_header,
        //                           TotalSumTable = total_sum_table
        //                       };
        //    if (filteredData != null && filteredData.Any())
        //    {                
        //        foreach (var item in filteredData)
        //        {                   
        //            MainStaticClass.write_event_in_log("guid: " + item.guid +" Сумма по шапке: " + item.TotalSumHeader+" Сумма по строкам: " + item.TotalSumTable,"Отправка чеков","0");                    
        //        }
        //    }            
        //}

        private void get_data_on_sales()
        {
            getdata_h();
            getdata_t();
        }

        /// <summary>
        /// Заполним список номеров документов которые необходимо отправить
        /// </summary>
        /// <returns></returns>
        private bool get_document_list_not_sent()
        {
            bool result = true;
            document_guid_list = new StringBuilder();
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                string its_deleted = "";
                conn.Open();
                string query = "SELECT guid,its_deleted FROM checks_header WHERE is_sent=0 order by document_number ";
                //" WHERE date_time_write > ";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    document_guid_list.Append("'" + reader["guid"] + "',");
                    its_deleted = reader["its_deleted"].ToString();
                }
                reader.Close();
                conn.Close();

                string temp = document_guid_list.ToString();
                document_guid_list = new StringBuilder();
                document_guid_list.Append(temp.Substring(0, temp.Length - 1));

                //document_number_list.Append(document_number_list.ToString().Substring().Append(s[i] + ",");)

                if (its_deleted == "2")//последняя строка со статусом 2, чтобы не было фокусов с перезаписью последний документ со статусом 2 не выгружаем
                {
                    string[] s = document_guid_list.ToString().Split(',');
                    document_guid_list = new StringBuilder();//обнуляем список 
                    if (s.Length == 1)
                    {
                        document_guid_list = new StringBuilder();//обнуляем список 
                    }
                    else
                    {
                        for (int i = 0; i < s.Length - 1; i++)
                        {
                            if (i == s.Length - 2)
                            {
                                document_guid_list.Append(s[i]);
                            }
                            else
                            {
                                document_guid_list.Append(s[i] + ",");
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException)
            {
                result = false;
            }
            catch (Exception)
            {
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


        /// <summary>
        /// Попытка отправить данные о проданных и погашенных сертификатах
        /// Получаем список документов не отправленных
        ///  
        /// </summary>
        /// <returns></returns>
        private bool its_sent_sertificate()
        {

            bool result = true;
            //Заполняем спиок номеров документов которые не отправлены
            if (!get_document_list_not_sent())
            {
                result = false;
                return result;
            }
            if (document_guid_list.ToString().Length == 0)
            {
                result = false;
                return result;
            }
            //return true;
            //Список нормально заполнился 
            string data = get_not_sent_sertificates();
            if (data == "-1")
            {
                result = false;
                return result;
            }
            if (data != "")// есть данные по сертификатам
            {
                if (!MainStaticClass.service_is_worker())
                {
                    result = false;
                    return result;
                }

                DS ds = MainStaticClass.get_ds();
                ds.Timeout = 60000;

                //Получить параметра для запроса на сервер 
                nick_shop = MainStaticClass.Nick_Shop.Trim();
                if (nick_shop.Trim().Length == 0)
                {
                    result = false;
                    return result;
                }

                string code_shop = MainStaticClass.Code_Shop.Trim();
                if (code_shop.Trim().Length == 0)
                {
                    result = false;
                    return result;
                }

                string count_day = CryptorEngine.get_count_day();

                string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
                string encrypt_string = CryptorEngine.Encrypt(data, true, key);
                string result_web_query = ds.SetStatusSertificat(nick_shop, encrypt_string, MainStaticClass.GetWorkSchema.ToString());
                if (result_web_query == "-1")
                {
                    result = false;
                    return result;
                }
            }

            return result;
        }

        ///// <summary>
        ///// Получаем строку код сертификата и его номинал
        ///// </summary>
        ///// <returns></returns>
        //private string get_not_sent_sertificates()
        //{
        //    string result="";

        //        NpgsqlConnection conn=MainStaticClass.NpgsqlConn();

        //    try
        //    {                
        //        conn.Open();
        //        string query = "SELECT "+
        //            " checks_table.document_number"+","+
        //            " checks_table.tovar_code" + ","+//Здесь при изменении схемы с сертификатами старое поле оставлено как псевдоним
        //            " price" + "," +//Здесь при изменении схемы с сертификатами старое поле оставлено как псевдоним
        //            " checks_header.cash_desk_number" +","+
        //            " sertificates.code AS sertificates_code"+"," +
        //            " checks_header.date_time_write "+
        //        " FROM checks_table LEFT JOIN tovar ON checks_table.tovar_code = tovar.code "+
        //        " LEFT JOIN checks_header ON checks_header.document_number = checks_table.document_number " +
        //        " LEFT JOIN sertificates ON checks_table.tovar_code = sertificates.code_tovar " +
        //        " where checks_table.document_number in (" +
        //        document_number_list.ToString()  +
        //        ") AND tovar.its_certificate = 1 AND checks_header.its_deleted = 0 ";//сертификаты только из проведенных документов 
        //        NpgsqlCommand command=new NpgsqlCommand(query,conn);
        //        NpgsqlDataReader reader=command.ExecuteReader();
        //        while(reader.Read())
        //        {
        //            result += reader["document_number"].ToString()   +","+
        //                reader["tovar_code"].ToString()              +","+
        //                reader["price"].ToString().Replace(",", ".") +","+
        //                reader["cash_desk_number"].ToString()        +","+
        //                reader["sertificates_code"].ToString()       +","+
        //                reader.GetDateTime(5).ToString("dd-MM-yyyy HH:mm:ss") + "|";                       

        //        }
        //        if (result != "")
        //        {
        //            result = result.Substring(0, result.Length - 1);
        //        }
        //        reader.Close();
        //        conn.Close();
        //    }
        //    catch(NpgsqlException ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //        result="-1";

        //    }
        //    catch(Exception)
        //    {
        //        result="-1";
        //    }
        //    finally
        //    {
        //        if(conn.State== ConnectionState.Open)
        //        {
        //            conn.Close();
        //        }
        //    }


        //    return result; 
        //}


        /// <summary>
        /// Получаем строку код сертификата и его номинал
        /// </summary>
        /// <returns></returns>
        private string get_not_sent_sertificates()
        {
            string result = "";

            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();

            try
            {
                conn.Open();
                string query = "SELECT " +
                    " checks_table.document_number" + "," +
                    " checks_table.tovar_code" + "," +//Здесь при изменении схемы с сертификатами старое поле оставлено как псевдоним
                                                      //" checks_table.sum_at_a_discount*-1 AS price " + "," +//Здесь при изменении схемы с сертификатами старое поле оставлено как псевдоним
                    " checks_table.sum_at_a_discount AS price " + "," +//Здесь при изменении схемы с сертификатами старое поле оставлено как псевдоним
                    " checks_header.cash_desk_number" + "," +
                    " checks_table.item_marker AS sertificates_code" + "," +
                    " checks_header.date_time_write " + "," +
                    " checks_header.check_type " +
                " FROM checks_table LEFT JOIN tovar ON checks_table.tovar_code = tovar.code " +
                " LEFT JOIN checks_header ON checks_header.guid = checks_table.guid " +
                //" LEFT JOIN sertificates ON checks_table.tovar_code = sertificates.code_tovar " +
                " where checks_table.guid in (" +
                document_guid_list.ToString() +
                ") AND tovar.its_certificate = 1 AND checks_header.its_deleted = 0 ";//сертификаты только из проведенных документов 
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result += reader["document_number"].ToString() + "," +
                        reader["tovar_code"].ToString() + "," +
                        reader["price"].ToString().Replace(",", ".") + "," +
                        reader["cash_desk_number"].ToString() + "," +
                        reader["sertificates_code"].ToString() + "," +
                        reader.GetDateTime(5).ToString("dd-MM-yyyy HH:mm:ss") + "," +
                        reader["check_type"].ToString() + "|";
                }
                if (result != "")
                {
                    result = result.Substring(0, result.Length - 1);
                }
                reader.Close();
                conn.Close();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(" get_not_sent_sertificates " + ex.Message);
                result = "-1";

            }
            catch (Exception ex)
            {
                MessageBox.Show(" get_not_sent_sertificates " + ex.Message);
                result = "-1";
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

        /// <summary>
        /// Обновим статусы после успешной отправки документов
        /// </summary>
        private void update_status_is_sent()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlTransaction trans = null;

            try
            {
                conn.Open();
                trans = conn.BeginTransaction();
                string query = "UPDATE checks_header SET is_sent=1 WHERE guid in (" +
                    document_guid_list.ToString() + ")";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                command.Transaction = trans;
                command.ExecuteNonQuery();
                trans.Commit();
                command.Dispose();
                trans.Dispose();
                conn.Close();
                MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
            }
            catch (NpgsqlException)
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
            }
            catch (Exception)
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        public class SalesPortions
        {
            public string Version { get; set; }
            public string Shop { get; set; }
            public string Guid { get; set; }
            public List<SalesPortionsHeader> ListSalesPortionsHeader { get; set; }
            public List<SalesPortionsTable> ListSalesPortionsTable { get; set; }
        }

        public class SalesPortionsHeader
        {
            public string Shop { get; set; }
            public string Num_doc { get; set; }
            public string Num_cash { get; set; }
            public string Client { get; set; }
            public string Discount { get; set; }
            public string Sum { get; set; }
            public string Check_type { get; set; }
            public string Have_action { get; set; }
            public string Its_deleted { get; set; }
            public string Bonus_counted { get; set; }
            public string Bonus_writen_off { get; set; }
            public string Date_time_write { get; set; }
            public string Action { get; set; }
            public string Sum_cash { get; set; }
            public string Sum_terminal { get; set; }
            public string Sum_certificate { get; set; }
            public string Sum_cash1 { get; set; }
            public string Sum_terminal1 { get; set; }
            public string Sum_certificate1 { get; set; }
            public string Date_time_start { get; set; }
            //public string Sales_assistant { get; set; }            
            public string Comment { get; set; }
            public string Autor { get; set; }
            public string Its_print { get; set; }
            public string Id_transaction { get; set; }
            public string Id_transaction_sale { get; set; }
            public string ClientInfo_vatin { get; set; }
            public string ClientInfo_name { get; set; }
            public string SumCashRemainder { get; set; }
            public string NumOrder { get; set; }
            public string NumOrder4 { get; set; }
            //public string VizaD { get; set; }
            public string SystemTaxation { get; set; }
            public string Guid { get; set; }
            public string SBP { get; set; }
            public string ClientPhone { get; set; }
        }

        public class SalesPortionsTable
        {
            public string Shop { get; set; }
            public string Num_doc { get; set; }
            public string Num_cash { get; set; }
            public string Tovar { get; set; }
            public string Characteristic { get; set; }
            public string Quantity { get; set; }
            public string Price { get; set; }
            public string Price_d { get; set; }
            public string Sum { get; set; }
            public string Sum_d { get; set; }
            public string Action1 { get; set; }
            public string Action2 { get; set; }
            public string Action3 { get; set; }
            public string Date_time_write { get; set; }
            public string Num_str { get; set; }
            public string Bonus_stand { get; set; }
            public string Bonus_prom { get; set; }
            public string Promotion_b_mover { get; set; }
            public string MarkingCode { get; set; }
            public string Guid { get; set; }

        }

        public async Task send_sales_data_Click(object sender, EventArgs e)
        {
            if (await MainStaticClass.GetUnloadingInterval() == 0)
            {
                return;
            }

            int documents_out_of_the_range_of_dates = MainStaticClass.get_documents_out_of_the_range_of_dates();

            if (documents_out_of_the_range_of_dates > 0 || documents_out_of_the_range_of_dates < 0)
            {
                return;
            }

            if (MainStaticClass.CashDeskNumber == 9)
            {
                return;
            }
            //gaa
            if (!its_sent_sertificate()) //не удалось отправить данные по сертификатам, отправка основных данных прервана 
            {
                return;
            }

            if (!MainStaticClass.service_is_worker())
            {
                return;
            }

            DS ds = MainStaticClass.get_ds();
            ds.Timeout = 180000;

            //Получить параметра для запроса на сервер 
            nick_shop = MainStaticClass.Nick_Shop.Trim();
            if (nick_shop.Trim().Length == 0)
            {
                return;
            }
            string code_shop = MainStaticClass.Code_Shop.Trim();
            if (code_shop.Trim().Length == 0)
            {
                return;
            }
            string count_day = CryptorEngine.get_count_day();
            salesPortions = new SalesPortions();
            salesPortions.Shop = nick_shop;
            salesPortions.Guid = code_shop;
            salesPortions.Version = MainStaticClass.version().Replace(".", "");
            //if (dt == null)
            //{
            //    dt = new DataTable();

            //    DataColumn guid = new DataColumn();
            //    guid.DataType = System.Type.GetType("System.String");
            //    guid.ColumnName = "guid";
            //    dt.Columns.Add(guid);

            //    DataColumn sum_header = new DataColumn();
            //    sum_header.DataType = System.Type.GetType("System.Double");
            //    sum_header.ColumnName = "sum_header";
            //    dt.Columns.Add(sum_header);

            //    DataColumn sum_table = new DataColumn();
            //    sum_table.DataType = System.Type.GetType("System.Double");
            //    sum_table.ColumnName = "sum_table";
            //    dt.Columns.Add(sum_table);
            //}
            //else
            //{
            //    dt.Rows.Clear();
            //}

            get_data_on_sales();
            //check_sum_header_and_table();
            if (were_mistakes)//Произошли какие то ошибки при выгрузке
            {
                return;
            }
            if ((salesPortions.ListSalesPortionsHeader.Count == 0) || (salesPortions.ListSalesPortionsTable.Count == 0))
            {
                return;
            }
            string key = nick_shop.Trim() + count_day.Trim() + code_shop.Trim();
            bool result_web_quey = false;
            string data = JsonConvert.SerializeObject(salesPortions, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            string data_crypt = CryptorEngine.Encrypt(data, true, key);
            try
            {
                result_web_quey = ds.UploadDataOnSalesPortionJson(nick_shop, data_crypt, MainStaticClass.GetWorkSchema.ToString());
            }
            catch (Exception ex)
            {
                write_error(ex.Message);
            }

            if (result_web_quey)
            {
                update_status_is_sent();                
                MainStaticClass.Last_Send_Last_Successful_Sending = DateTime.Now;
            }
        }

        private void write_error(string error)
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            try
            {
                conn.Open();
                string query = "INSERT INTO errors_on_send_portions(" +
                    "time_event," +
                    "errors_text," +
                    ")VALUES('" +
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" +
                    error + "')";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                command.ExecuteNonQuery();
                conn.Close();
            }
            catch
            {

            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        //private void _close__Click(object sender, EventArgs e)
        //{
        //    this.Close();
        //}
    }
}
