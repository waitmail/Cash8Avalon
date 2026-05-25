//using Newtonsoft.Json;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Security;
//using System.Text;
//using System.Threading.Tasks;
//using Tmds.DBus.Protocol;

//namespace Cash8Avalon
//{
//    internal class Piot
//    {

//        // Добавляем новый метод для получения информации из API
//        // Добавляем новый метод для получения информации из API
//        public PiotInfo GetPiotInfo()
//        {
//            // Проверяем, есть ли уже данные в статическом классе
//            if (MainStaticClass.PiotInfo != null)
//            {
//                return MainStaticClass.PiotInfo;
//            }

//            try
//            {
//                // 1. Добавляем поддержку TLS 1.2 (обязательно для современных серверов)
//                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;

//                // 2. Разрешаем самоподписанные сертификаты для локального сервера
//                ServicePointManager.ServerCertificateValidationCallback =
//                    (sender, certificate, chain, sslPolicyErrors) => true;

//                string url = MainStaticClass.GetPiotUrl + "/info";
//                //string url = "https://esm-emu.ao-esp.ru/info";
//                //string url = "127.0.0.1:51401/info";

//                // Создаем запрос
//                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
//                request.Method = "POST";
//                request.ContentType = "application/json";
//                request.Accept = "application/json";
//                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

//                // Для POST запроса обязательно нужно тело
//                byte[] data = Encoding.UTF8.GetBytes("{}");
//                request.ContentLength = data.Length;

//                // Пишем тело запроса
//                using (Stream stream = request.GetRequestStream())
//                {
//                    stream.Write(data, 0, data.Length);
//                }

//                // Получаем ответ
//                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
//                using (Stream stream = response.GetResponseStream())
//                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
//                {
//                    string jsonResponse = reader.ReadToEnd();
//                    var info = JsonConvert.DeserializeObject<PiotInfo>(jsonResponse);

//                    // Сохраняем в статический класс для последующего использования
//                    MainStaticClass.PiotInfo = info;

//                    return info;
//                }
//            }
//            catch (WebException ex)
//            {
//                // Обработка ошибок HTTP
//                if (ex.Response != null)
//                {
//                    using (var errorResponse = (HttpWebResponse)ex.Response)
//                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
//                    {
//                        string errorText = reader.ReadToEnd();
//                        throw new Exception($"Ошибка при получении информации ПИОТ: {(int)errorResponse.StatusCode} - {errorText}", ex);
//                    }
//                }
//                throw new Exception("Ошибка сети при получении информации ПИОТ: " + ex.Message, ex);
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Ошибка при получении информации ПИОТ: " + ex.Message, ex);
//            }
//        }


//        // Класс для десериализации ответа от API информации
//        public class PiotInfo
//        {
//            [JsonProperty("tspiotId")]
//            public string tspiotId { get; set; }

//            [JsonProperty("kktSerial")]
//            public string kktSerial { get; set; }

//            [JsonProperty("fnSerial")]
//            public string fnSerial { get; set; }

//            [JsonProperty("kktInn")]
//            public string kktInn { get; set; }

//            [JsonProperty("codesCheckTimeout")]
//            public int codesCheckTimeout { get; set; }

//            // Метод для вывода информации в читаемом формате
//            public override string ToString()
//            {
//                return $"TSPIOT ID: {tspiotId}\r\n" +
//                       $"Серийный номер ККТ: {kktSerial}\r\n" +
//                       $"Серийный номер ФН: {fnSerial}\r\n" +
//                       $"ИНН ККТ: {kktInn}";
//            }
//        }

//        // Пример использования в существующем методе (можно добавить вызов где нужно)
//        public async void CheckPiotConnection()
//        {
//            try
//            {
//                PiotInfo info = GetPiotInfo();
//                await MessageBox.Show($"Успешное подключение к ПИОТ:\r\n{info.ToString()}",
//                    "Информация ПИОТ", MessageBoxButton.OK, MessageBoxType.Info);
//            }
//            catch (Exception ex)
//            {
//                await MessageBox.Show($"Ошибка подключения к ПИОТ:\r\n{ex.Message}",
//                    "Ошибка ПИОТ", MessageBoxButton.OK, MessageBoxType.Error);
//            }
//        }

//        /// <summary>
//        /// Получает баланс продаж/возвратов для маркировки
//        /// </summary>
//        /// <returns>Баланс: >0 - продан, =0 - нейтрально, <0 - возвращен</returns>
//        public int GetMarkingBalance(string markingCode)
//        {
//            string query = @"
//        SELECT COALESCE(
//            SUM(
//                CASE 
//                    WHEN ch.check_type = 0 THEN 1
//                    WHEN ch.check_type = 1 THEN -1
//                    ELSE 0
//                END
//            ), 0
//        ) as balance
//        FROM checks_table ct
//        INNER JOIN checks_header ch ON ct.guid = ch.guid
//        WHERE ct.item_marker = @markingCode
//            AND ch.check_type IN (0, 1)
//            AND ch.its_deleted = 0;";

//            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
//            using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
//            {
//                conn.Open();
//                command.Parameters.AddWithValue("@markingCode", markingCode);

//                var result = command.ExecuteScalar();
//                return Convert.ToInt32(result ?? 0);
//            }
//        }

//        public async Task<bool> cdn_check_marker_code(List<string> codes, string mark_str, Int64 numdoc, HttpWebRequest request, string mark_str_cdn, Dictionary<string, string> d_tovar, Cash_check cash_Check, ProductData productData)
//        {
//            bool result_check = false;

//            StringBuilder sb = new StringBuilder();

//            string url = "";            
//            url = MainStaticClass.GetPiotUrl + "/codes/check";

//            ApiResponse apiResponse = null;

//            //string marking_code = mark_str.Replace("\u001d", "\\u001d"); // Заменяем СИМВОЛ на текст

//            string marking_code = mark_str.Replace("\\u001d", @"u001d");
//            //string marking_code = mark_str.Replace("\u001d", @"u001d");            

//            // Заполняем информацию о клиенте
//            var clientInfo = new ClientInfo
//            {
//                name = "Cash8Avalon",
//                version = "1.0.0",
//                id = "fd69a394-6e87-4393-b8d6-a46102d177ad",//   "7c9e6679-7425-40de-944b-e07fc1f90ae7",
//                token = "6ba7b810-9dad-11d1-80b4-00c04fd430c8" // Замените на реальный токен
//            };

//            // Отправляем запрос
//            var apiClient = new ApiClient();
//            try
//            {
//                byte[] textAsBytes = Encoding.Default.GetBytes(marking_code);
//                //byte[] textAsBytes = Encoding.Default.GetBytes(mark_str_cdn);

//                string imc = Convert.ToBase64String(textAsBytes);
//                var response = apiClient.SendCodeRequest(imc, url, clientInfo);

//                if (!response.Success)
//                {                   
//                    throw new Exception(response.Exception.Message, response.Exception);                 
//                }

//                //Записываем лог 
//                MainStaticClass.write_cdn_log(response.Data, numdoc.ToString(), codes[0].ToString(), "1");
//                //MessageBox.Show(response.Data, "Ответ", cash_Check);
//                apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response.Data);                

//                ResponseItem answer_check_mark = null;

//                // 1. Пытаемся получить данные по коду маркировки
//                if (apiResponse.codesResponse != null && apiResponse.codesResponse.codesResponse != null && apiResponse.codesResponse.codesResponse.Count > 0)
//                {
//                    answer_check_mark = apiResponse.codesResponse.codesResponse[0];
//                }

//                // 2. Проверяем ошибки на УРОВНЕ КОДА МАРКИРОВКИ (приоритетная проверка)
//                if (answer_check_mark != null && answer_check_mark.codes != null && answer_check_mark.codes.Count > 0 && answer_check_mark.codes[0].errorCode != 0)
//                {
//                    if (answer_check_mark.codes[0].errorCode == 10)
//                    {
//                        await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = 10\r\nТекст ошибки: данный код не найден в БД ЧЗ", "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                        return false; // Выходим, дальше проверять нет смысла
//                    }
//                    else if (answer_check_mark.codes[0].errorCode == 203)
//                    {
//                        await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = 203\r\nТекст ошибки: " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                        if (!MainStaticClass.PiotError203)
//                        {
//                            MainStaticClass.PiotError203 = true;
//                            return true; // Выходим с разрешением в первый раз встретившись с аварийным режимом 
//                        }
//                        else
//                        {
//                            return false; // Выходим с отказом
//                        }
//                    }
//                    else
//                    {
//                        await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                        return false;
//                    }
//                }

//                // 3. Проверяем ошибки на УРОВНЕ ВСЕГО ЗАПРОСА (если на уровне кода ошибок не было)
//                if (apiResponse.errorCode == 203)
//                {
//                    await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = 203\r\nТекст ошибки " + apiResponse.errorMessage, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                    if (!MainStaticClass.PiotError203)
//                    {
//                        MainStaticClass.PiotError203 = true;
//                        return true;
//                    }
//                    else
//                    {
//                        return false;
//                    }
//                }

//                // 4. Если структура ответа вообще не понятна
//                if (answer_check_mark == null || answer_check_mark.codes == null || answer_check_mark.codes.Count == 0)
//                {
//                    await MessageBox.Show("Не удалось получить ответ от ПИот\r\nПРОВЕРЬТЕ РАБОТОСПОСОБНОСТЬ ПИОТ", "Ошибка работы с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                    return false;
//                }


//                if (answer_check_mark.code == 0) // Это успех
//                {
//                    if (answer_check_mark.codes[0].errorCode == 0)
//                    {
//                        if (!answer_check_mark.isCheckedOffline)//Это была онлайн проверка 
//                        {
//                            await MessageBox.Show("Онлайн проверка кода маркировки", "Онлайн", MessageBoxButton.OK, MessageBoxType.Info, cash_Check);
//                            string s = "ТОВАР НЕ МОЖЕТ БЫТЬ ПРОДАН!\r\n";
//                            if (!answer_check_mark.codes[0].isOwner)
//                            {
//                                if (answer_check_mark.codes[0].groupIds != null)
//                                {
//                                    if ((answer_check_mark.codes[0].groupIds[0] != 23) && (answer_check_mark.codes[0].groupIds[0] != 8) && (answer_check_mark.codes[0].groupIds[0] != 15) && (answer_check_mark.codes[0].groupIds[0] != 3))
//                                    {
//                                        if (!productData.RrNotControlOwner())
//                                        {
//                                            await MessageBox.Show(" Исключения групп маркрировки  23|8|15 \r\n Текущая группа маркировки  " + answer_check_mark.codes[0].groupIds[0].ToString());
//                                            if (cash_Check.check_type.SelectedIndex == 0)
//                                            {
//                                                sb.AppendLine("Вы не являетесь владельцем!".ToUpper());
//                                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " Вы не являетесь владельцем ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
//                                            }
//                                        }
//                                    }
//                                }
//                                else
//                                {
//                                    sb.AppendLine("Не удалось определить группу товара");
//                                }
//                            }

//                            if (!answer_check_mark.codes[0].valid)
//                            {
//                                sb.AppendLine("Результат проверки валидности структуры КИ / КиЗ не прошла проверку !".ToUpper());
//                                MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + "Проверки валидности структуры КИ / КиЗ не прошла проверку !", "Документ чек", cash_Check.numdoc.ToString());
//                            }

//                            if (!answer_check_mark.codes[0].found)
//                            {
//                                sb.AppendLine("Не найден в ГИС МТ!".ToUpper());
//                                MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " не найден в ГИС МТ", "Документ чек", cash_Check.numdoc.ToString());
//                                if ((!answer_check_mark.codes[0].realizable) && (!answer_check_mark.codes[0].sold))
//                                {
//                                    sb.AppendLine("Нет информации о вводе в оборот!".ToUpper());
//                                    MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " нет информации о вводе в оборот. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
//                                }
//                            }

//                            if (answer_check_mark.codes[0].found)
//                            {
//                                //sb.AppendLine("Не найден в ГИС МТ!".ToUpper());
//                                //MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " не найден в ГИС МТ", "Документ чек", cash_Check.numdoc.ToString());
//                                if (answer_check_mark.codes[0].groupIds[0] != 3)//Для табака исключение 
//                                {
//                                    if ((!answer_check_mark.codes[0].realizable) && (!answer_check_mark.codes[0].sold) && (answer_check_mark.codes[0].utilised))
//                                    {
//                                        sb.AppendLine("Нет информации о вводе в оборот!".ToUpper());
//                                        MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " нет информации о вводе в оборот. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
//                                    }
//                                }
//                            }

//                            if (!answer_check_mark.codes[0].utilised)
//                            {
//                                sb.AppendLine("Эмитирован, но нет информации о его нанесении!".ToUpper());
//                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " эмитирован, но нет информации о его нанесении. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
//                            }

//                            if (!answer_check_mark.codes[0].verified)
//                            {
//                                sb.AppendLine("Не пройдена криптографическая проверка!".ToUpper());
//                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  не пройдена криптографическая проверка.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
//                            }

//                            if (answer_check_mark.codes[0].sold)
//                            {
//                                if (cash_Check.check_type.SelectedIndex == 0)
//                                {
//                                    sb.AppendLine("Уже выведен из оборота!".ToUpper());
//                                    MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  уже выведен из оборота.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
//                                }
//                            }

//                            if (answer_check_mark.codes[0].isBlocked)
//                            {
//                                sb.AppendLine("Заблокирован по решению ОГВ!".ToUpper());
//                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  заблокирован по решению ОГВ.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
//                            }
//                            if (answer_check_mark.codes[0].expireDate.Year > 2000)
//                            {
//                                if (answer_check_mark.codes[0].expireDate < DateTime.Now)
//                                {
//                                    sb.AppendLine("Истек срок годности!".ToUpper());
//                                    MainStaticClass.write_cdn_log("CDN У товара с кодом маркировки " + mark_str_cdn + "  истек срок годности.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");

//                                }
//                            }
//                            if (sb.Length == 0)
//                            {

//                                if (cash_Check.verifyCDN.ContainsKey(mark_str))
//                                {
//                                    cash_Check.verifyCDN.Remove(mark_str);
//                                }

//                                Cash_check.Requisite1260 requisite1260 = new Cash_check.Requisite1260();
//                                requisite1260.req1262 = "030";
//                                requisite1260.req1263 = "21.11.2023";
//                                requisite1260.req1264 = "1944";
//                                requisite1260.req1265 = "UUID=" + answer_check_mark.reqId + "&Time=" + answer_check_mark.reqTimestamp;
//                                cash_Check.verifyCDN.Add(mark_str, requisite1260);

//                                result_check = true;
//                            }
//                            else
//                            {
//                                int stringCount = sb.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
//                                if (stringCount == 1)
//                                {
//                                    sb.Insert(0, "Код маркировки " + mark_str + "\r\nне прошел проверку по следующей причине:\r\n".ToUpper());
//                                }
//                                else
//                                {
//                                    sb.Insert(0, "Код маркировки " + mark_str + "\r\nне прошел проверку по следующим причинам:\r\n".ToUpper());
//                                }
//                                sb.Append(s);
//                                sb.AppendLine(d_tovar.Keys.ElementAt(0));
//                                sb.AppendLine(d_tovar[d_tovar.Keys.ElementAt(0)]);
//                                await MessageBox.Show(sb.ToString(), "Ошибки при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error);
//                            }
//                        }
//                        else//это была офлайн проверка 
//                        {
//                            await MessageBox.Show("Офлайн проверка кода маркировки заблокирован = "+answer_check_mark.codes[0].isBlocked.ToString(), "Офлайн", MessageBoxButton.OK, MessageBoxType.Info, cash_Check);
//                            MessageBox.Show(response.Data, "Ответ", cash_Check);
//                            if (answer_check_mark.codes[0].isBlocked)
//                            {
//                                result_check = false;
//                                await MessageBox.Show("Офлайн проверка кода маркировки\r\nДанный код заблокирован", "Ошибка при работе с кодом аркировки", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                            }
//                            else
//                            {

//                                if (GetMarkingBalance(mark_str) > 0)
//                                {
//                                    await MessageBox.Show("Данный код марикровки найден в уже проданных.", "Ошибка при продаже марикрованного товара", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                                    result_check = false;
//                                    return result_check;
//                                }

//                                if (cash_Check.verifyCDN.ContainsKey(mark_str))
//                                {
//                                    cash_Check.verifyCDN.Remove(mark_str);
//                                }

//                                Cash_check.Requisite1260 requisite1260 = new Cash_check.Requisite1260();
//                                requisite1260.req1262 = "030";
//                                requisite1260.req1263 = "21.11.2023";
//                                requisite1260.req1264 = "1944";
//                                requisite1260.req1265 = "UUID=" + answer_check_mark.reqId +
//                                                        "&Time=" + answer_check_mark.reqTimestamp +
//                                                        "&Inst=" + answer_check_mark.inst +
//                                                        "&Ver="+ answer_check_mark.version;

//                                cash_Check.verifyCDN.Add(mark_str, requisite1260);

//                                result_check = true;
//                            }

//                        }
//                    }
//                    else
//                    {                        
//                        if (answer_check_mark.codes[0].errorCode == 10)
//                        {
//                            await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки данный код не найден в БД ЧЗ", "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                            result_check = false;
//                        }
//                        else if (answer_check_mark.codes[0].errorCode == 203)
//                        {
//                            await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                            if (!MainStaticClass.PiotError203)
//                            {
//                                MainStaticClass.PiotError203 = true;
//                                result_check = true;
//                            }
//                            else
//                            {
//                                result_check = false;
//                            }
//                        }
//                        else
//                        {
//                            await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                            result_check = false;
//                        }                        
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                await MessageBoxHelper.Show(ex.Message+"\r\n"+ex.StackTrace, "Ошибка при запросе к ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
//                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + ex.Message + "\r\n" + ex.StackTrace, cash_Check.numdoc.ToString(), codes[0].ToString(), "2");
//                result_check = false;
//            }

//            return result_check;
//        }

//        public class ApiResponse
//        {

//            // Поля для ошибки
//            [JsonProperty("code")]
//            public int? errorCode { get; set; }

//            [JsonProperty("message")]
//            public string errorMessage { get; set; }

//            [JsonProperty("codesResponse")]
//            public CodesResponseWrapper codesResponse { get; set; }
//        }

//        public class CodesResponseWrapper
//        {
//            [JsonProperty("codesResponse")]
//            public List<ResponseItem> codesResponse { get; set; }
//        }

//        public class ResponseItem
//        {
//            [JsonProperty("code")]
//            public int code { get; set; }

//            [JsonProperty("description")]
//            public string description { get; set; }

//            [JsonProperty("codes")]
//            public List<CodeDetail> codes { get; set; }

//            [JsonProperty("reqId")]
//            public string reqId { get; set; }

//            [JsonProperty("reqTimestamp")]
//            public long reqTimestamp { get; set; }

//            [JsonProperty("isCheckedOffline")]
//            public bool isCheckedOffline { get; set; }

//            [JsonProperty("version")]
//            public string version { get; set; }

//            [JsonProperty("inst")]
//            public string inst { get; set; }
//        }

//        public class CodeDetail
//        {
//            [JsonProperty("cis")]
//            public string cis { get; set; }

//            [JsonProperty("found")]
//            public bool found { get; set; }

//            [JsonProperty("valid")]
//            public bool valid { get; set; }

//            [JsonProperty("printView")]
//            public string printView { get; set; }

//            [JsonProperty("gtin")]
//            public string gtin { get; set; }

//            [JsonProperty("groupIds")]
//            public List<int> groupIds { get; set; }

//            [JsonProperty("verified")]
//            public bool verified { get; set; }

//            [JsonProperty("realizable")]
//            public bool realizable { get; set; }

//            [JsonProperty("utilised")]
//            public bool utilised { get; set; }

//            [JsonProperty("productionDate")]
//            public DateTime? productionDate { get; set; }

//            [JsonProperty("isOwner")]
//            public bool isOwner { get; set; }

//            [JsonProperty("isBlocked")]
//            public bool isBlocked { get; set; }

//            [JsonProperty("ogvs")]
//            public List<object> ogvs { get; set; }

//            [JsonProperty("errorCode")]
//            public int errorCode { get; set; }

//            [JsonProperty("message")]
//            public string message { get; set; }

//            [JsonProperty("isTracking")]
//            public bool isTracking { get; set; }

//            [JsonProperty("sold")]
//            public bool sold { get; set; }

//            [JsonProperty("mrp")]
//            public int? mrp { get; set; }

//            [JsonProperty("grayZone")]
//            public bool grayZone { get; set; }

//            [JsonProperty("packageType")]
//            public string packageType { get; set; }

//            [JsonProperty("producerInn")]
//            public string producerInn { get; set; }

//            [JsonProperty("expireDate")]
//            public DateTime expireDate { get; set; }

//        }


//        public class ClientInfo
//        {
//            public string name { get; set; }
//            public string version { get; set; }
//            public string id { get; set; }
//            public string token { get; set; }
//        }

//        public class ClientData
//        {
//            public List<string> codes { get; set; }
//            public ClientInfo client_info { get; set; }
//        }

//        public class ApiClient
//        {
//            public class ApiResponse
//            {
//                public bool Success { get; set; }
//                public string Data { get; set; }
//                public int? HttpStatusCode { get; set; }
//                public Exception Exception { get; set; }

//                public static ApiResponse CreateSuccess(string data, int statusCode)
//                {
//                    return new ApiResponse
//                    {
//                        Success = true,
//                        Data = data,
//                        HttpStatusCode = statusCode
//                    };
//                }

//                public static ApiResponse CreateError(Exception exception, int? httpStatusCode = null)
//                {
//                    return new ApiResponse
//                    {
//                        Success = false,
//                        HttpStatusCode = httpStatusCode,
//                        Exception = exception
//                    };
//                }
//            }

//            public ApiResponse SendCodeRequest(string code, string url, ClientInfo clientInfo)
//            {
//                try
//                {
//                    // 🔥 0. Очищаем код от BOM и мусора
//                    //string cleanCode = CleanMarkingCode(code);

//                    // 🔥 Логирование для отладки (удалите в продакшене)
//                    //Debug.WriteLine($"[CDN] Original code bytes: {BitConverter.ToString(Encoding.UTF8.GetBytes(code))}");
//                    //Debug.WriteLine($"[CDN] Cleaned code: '{cleanCode}'");
//                    //Debug.WriteLine($"[CDN] Cleaned code bytes: {BitConverter.ToString(Encoding.UTF8.GetBytes(cleanCode))}");

//                    // 🔥 1. Нормализуем URL
//                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
//                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
//                    {
//                        url = "https://" + url.TrimStart('/');
//                    }

//                    // 🔥 2. Создаем данные с очищенным кодом
//                    var clientData = new ClientData
//                    {
//                        codes = new List<string> { code },  // ✅ Используем cleanCode
//                        client_info = clientInfo
//                    };

//                    // 🔥 3. Сериализуем с настройками для безопасности
//                    var jsonSettings = new JsonSerializerSettings
//                    {
//                        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
//                        NullValueHandling = NullValueHandling.Ignore
//                    };
//                    string jsonData = JsonConvert.SerializeObject(clientData, jsonSettings);

//                    // 🔥 4. Логируем отправляемый JSON (для отладки)
//                    Debug.WriteLine($"[CDN] Sending JSON: {jsonData}");

//                    // 🔥 5. Настраиваем TLS и сертификаты
//                    ServicePointManager.SecurityProtocol =
//                        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

//                    ServicePointManager.ServerCertificateValidationCallback =
//                        (sender, certificate, chain, sslPolicyErrors) => true;

//                    ServicePointManager.CheckCertificateRevocationList = false;

//                    var request = (HttpWebRequest)WebRequest.Create(url);
//                    request.Timeout = 5000;
//                    request.Method = "POST";
//                    request.ContentType = "application/json";
//                    request.Accept = "application/json";
//                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

//                    // 🔥 6. Записываем данные с UTF-8
//                    using (var stream = request.GetRequestStream())                  
//                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 8192, leaveOpen: false))
//                    {
//                        writer.Write(jsonData);
//                        writer.Flush();
//                    }

//                    // 🔥 7. Получаем ответ
//                    using (var response = (HttpWebResponse)request.GetResponse())
//                    using (var responseStream = response.GetResponseStream())
//                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
//                    {
//                        string result = reader.ReadToEnd();
//                        Debug.WriteLine($"[CDN] Response: {result}");
//                        return ApiResponse.CreateSuccess(result, (int)response.StatusCode);
//                    }
//                }
//                catch (WebException ex)
//                {
//                    Debug.WriteLine($"[CDN] WebException: Status={ex.Status}, Message={ex.Message}");
//                    if (ex.InnerException != null)
//                    {
//                        Debug.WriteLine($"[CDN] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
//                    }

//                    // 🔥 Читаем тело ошибки
//                    string errorBody = null;
//                    int? statusCode = null;

//                    if (ex.Response is HttpWebResponse errorResponse)
//                    {
//                        statusCode = (int)errorResponse.StatusCode;
//                        try
//                        {
//                            using (var reader = new StreamReader(errorResponse.GetResponseStream(), Encoding.UTF8))
//                            {
//                                errorBody = reader.ReadToEnd();
//                                Debug.WriteLine($"[CDN] Error body: {errorBody}");
//                            }
//                        }
//                        catch { }
//                    }

//                    return ApiResponse.CreateError(
//                        new Exception($"HTTP {(int?)statusCode ?? 0}: {errorBody ?? ex.Message}", ex),
//                        statusCode);
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[CDN] Unexpected error: {ex.GetType().Name}: {ex.Message}");
//                    Debug.WriteLine(ex.StackTrace);
//                    return ApiResponse.CreateError(ex);
//                }
//            }            
//        }
//    }
//}

using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PiotIntegration; // Подключаем нашу новую DLL (или пока класс в проекте)
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;

namespace Cash8Avalon
{
    internal class Piot
    {
        // Инициализируем клиент один раз. Он сам настроит TLS и сертификаты
        private readonly PiotClient _piotClient = new PiotClient(MainStaticClass.GetPiotUrl);

        /// <summary>
        /// Получает информацию о ПИОТ через DLL
        /// </summary>
        public async Task<PiotInfo> GetPiotInfoAsync()
        {
            // Проверяем кэш
            if (MainStaticClass.PiotInfo != null)
            {
                return MainStaticClass.PiotInfo;
            }

            try
            {
                // Делаем запрос через новый клиент
                var result = await _piotClient.GetInfoAsync();

                if (!result.Success)
                {
                    throw new Exception(result.ErrorMessage);
                }

                var info = JsonConvert.DeserializeObject<PiotInfo>(result.JsonData);

                // Сохраняем в статический класс для последующего использования
                MainStaticClass.PiotInfo = info;

                return info;
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при получении информации ПИОТ: " + ex.Message, ex);
            }
        }       

        // Пример использования в существующем методе
        public async Task CheckPiotConnectionAsync()
        {
            try
            {
                PiotInfo info = await GetPiotInfoAsync();
                await MessageBox.Show($"Успешное подключение к ПИОТ:\r\n{info.ToString()}",
                    "Информация ПИОТ", MessageBoxButton.OK, MessageBoxType.Info,MainStaticClass.MainWindow);
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка подключения к ПИОТ:\r\n{ex.Message}",
                    "Ошибка ПИОТ", MessageBoxButton.OK, MessageBoxType.Error, MainStaticClass.MainWindow);
            }
        }

        /// <summary>
        /// Получает баланс продаж/возвратов для маркировки из БД
        /// </summary>
        public int GetMarkingBalance(string markingCode)
        {
            string query = @"
            SELECT COALESCE(
                SUM(
                    CASE 
                        WHEN ch.check_type = 0 THEN 1
                        WHEN ch.check_type = 1 THEN -1
                        ELSE 0
                    END
                ), 0
            ) as balance
            FROM checks_table ct
            INNER JOIN checks_header ch ON ct.guid = ch.guid
            WHERE ct.item_marker = @markingCode
                AND ch.check_type IN (0, 1)
                AND ch.its_deleted = 0;";

            using (NpgsqlConnection conn = MainStaticClass.NpgsqlConn())
            using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
            {
                conn.Open();
                command.Parameters.AddWithValue("@markingCode", markingCode);

                var result = command.ExecuteScalar();
                return Convert.ToInt32(result ?? 0);
            }
        }

        ///// <summary>
        ///// Вычисляет SHA256 хэш (контрольную сумму) указанного файла
        ///// </summary>
        //private string GetFileChecksum(string filePath)
        //{
        //    // Если файла нет, возвращаем пустую строку или бросаем исключение
        //    if (!File.Exists(filePath))
        //    {
        //        return "FILE_NOT_FOUND";
        //    }

        //    using (var sha256 = SHA256.Create())
        //    using (var stream = File.OpenRead(filePath))
        //    {
        //        byte[] hashBytes = sha256.ComputeHash(stream);

        //        // Преобразуем байты в читаемую hex-строку (например: "a2b4c6...")
        //        StringBuilder builder = new StringBuilder();
        //        for (int i = 0; i < hashBytes.Length; i++)
        //        {
        //            builder.Append(hashBytes[i].ToString("x2")); // "x2" - формат hex в нижнем регистре
        //        }
        //        return builder.ToString();
        //    }
        //}

        public async Task<bool> cdn_check_marker_code(List<string> codes, string mark_str, Int64 numdoc, string mark_str_cdn, Dictionary<string, string> d_tovar, Cash_check cash_Check, ProductData productData)
        {
            bool result_check = false;
            StringBuilder sb = new StringBuilder();            

            // 3. ВЫЧИСЛЯЕМ КОНТРОЛЬНУЮ СУММУ
            string checksumValue = MainStaticClass.DllChecksum;

            // 1. ФОРМИРУЕМ ИНФОРМАЦИЮ О КЛИЕНТЕ (СЮДА ПОТОМ ВСТАВИМ КОНТРОЛЬНУЮ СУММУ)
            //string checksumValue = "ВАША_КОНТРОЛЬНАЯ_СУММА"; // TODO: Вычислить контрольную сумму

            if (checksumValue == "FILE_NOT_FOUND")
            {
                await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nНе удалось вычислить контрольную сумму билиотеки работы с ПИот ", "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                return result_check;
            }

            var clientInfo = new ClientInfo
            {
                name = "Cash8Avalon",
                version = MainStaticClass.version(),
                id = "89088fa8-8e26-4788-a2c8-128d403963c7", 
                token = checksumValue // Передаем контрольную сумму в token 
            };

            try
            {
                // 2. ОТПРАВЛЯЕМ ЗАПРОС ЧЕРЕЗ НОВЫЙ КЛИЕНТ
                PiotRequestResult result = await _piotClient.CheckCodeAsync(mark_str, clientInfo);

                if (!result.Success)
                {
                    throw new Exception(result.ErrorMessage);
                }

                // 3. ЛОГИРУЕМ И ПАРСИМ ОТВЕТ
                MainStaticClass.write_cdn_log(result.JsonData, numdoc.ToString(), codes[0].ToString(), "1");
                ApiResponse apiResponse = JsonConvert.DeserializeObject<ApiResponse>(result.JsonData);

                ResponseItem answer_check_mark = null;

                // 1. Пытаемся получить данные по коду маркировки
                if (apiResponse.codesResponse != null && apiResponse.codesResponse.codesResponse != null && apiResponse.codesResponse.codesResponse.Count > 0)
                {
                    answer_check_mark = apiResponse.codesResponse.codesResponse[0];
                }

                // 2. Проверяем ошибки на УРОВНЕ КОДА МАРКИРОВКИ (приоритетная проверка)
                if (answer_check_mark != null && answer_check_mark.codes != null && answer_check_mark.codes.Count > 0 && answer_check_mark.codes[0].errorCode != 0)
                {
                    if (answer_check_mark.codes[0].errorCode == 10)
                    {
                        await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = 10\r\nТекст ошибки: данный код не найден в БД ЧЗ", "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                        return false;
                    }
                    else if (answer_check_mark.codes[0].errorCode == 203)
                    {
                        await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = 203\r\nТекст ошибки: " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                        if (!MainStaticClass.PiotError203)
                        {
                            MainStaticClass.PiotError203 = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                        return false;
                    }
                }

                // 3. Проверяем ошибки на УРОВНЕ ВСЕГО ЗАПРОСА
                if (apiResponse.errorCode == 203)
                {
                    await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = 203\r\nТекст ошибки " + apiResponse.errorMessage, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                    if (!MainStaticClass.PiotError203)
                    {
                        MainStaticClass.PiotError203 = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                // 4. Если структура ответа вообще не понятна
                if (answer_check_mark == null || answer_check_mark.codes == null || answer_check_mark.codes.Count == 0)
                {
                    await MessageBox.Show("Не удалось получить ответ от ПИот\r\nПРОВЕРЬТЕ РАБОТОСПОСОБНОСТЬ ПИОТ", "Ошибка работы с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                    return false;
                }


                if (answer_check_mark.code == 0) // Это успех
                {
                    if (answer_check_mark.codes[0].errorCode == 0)
                    {
                        if (!answer_check_mark.isCheckedOffline)//Это была онлайн проверка 
                        {
                            await MessageBox.Show("Онлайн проверка кода маркировки", "Онлайн", MessageBoxButton.OK, MessageBoxType.Info, cash_Check);
                            string s = "ТОВАР НЕ МОЖЕТ БЫТЬ ПРОДАН!\r\n";

                            if (!answer_check_mark.codes[0].isOwner)
                            {
                                if (answer_check_mark.codes[0].groupIds != null)
                                {
                                    if ((answer_check_mark.codes[0].groupIds[0] != 23) && (answer_check_mark.codes[0].groupIds[0] != 8) && (answer_check_mark.codes[0].groupIds[0] != 15) && (answer_check_mark.codes[0].groupIds[0] != 3))
                                    {
                                        if (!productData.RrNotControlOwner())
                                        {
                                            await MessageBox.Show(" Исключения групп маркрировки  23|8|15 \r\n Текущая группа маркировки  " + answer_check_mark.codes[0].groupIds[0].ToString());
                                            if (cash_Check.check_type.SelectedIndex == 0)
                                            {
                                                sb.AppendLine("Вы не являетесь владельцем!".ToUpper());
                                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " Вы не являетесь владельцем ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    sb.AppendLine("Не удалось определить группу товара");
                                }
                            }

                            if (!answer_check_mark.codes[0].valid)
                            {
                                sb.AppendLine("Результат проверки валидности структуры КИ / КиЗ не прошла проверку !".ToUpper());
                                MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + "Проверки валидности структуры КИ / КиЗ не прошла проверку !", "Документ чек", cash_Check.numdoc.ToString());
                            }

                            if (!answer_check_mark.codes[0].found)
                            {
                                sb.AppendLine("Не найден в ГИС МТ!".ToUpper());
                                MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " не найден в ГИС МТ", "Документ чек", cash_Check.numdoc.ToString());
                                if ((!answer_check_mark.codes[0].realizable) && (!answer_check_mark.codes[0].sold))
                                {
                                    sb.AppendLine("Нет информации о вводе в оборот!".ToUpper());
                                    MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " нет информации о вводе в оборот. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                                }
                            }

                            if (answer_check_mark.codes[0].found)
                            {
                                if (answer_check_mark.codes[0].groupIds[0] != 3)//Для табака исключение 
                                {
                                    if ((!answer_check_mark.codes[0].realizable) && (!answer_check_mark.codes[0].sold) && (answer_check_mark.codes[0].utilised))
                                    {
                                        sb.AppendLine("Нет информации о вводе в оборот!".ToUpper());
                                        MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " нет информации о вводе в оборот. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                                    }
                                }
                            }

                            if (!answer_check_mark.codes[0].utilised)
                            {
                                sb.AppendLine("Эмитирован, но нет информации о его нанесении!".ToUpper());
                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " эмитирован, но нет информации о его нанесении. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                            }

                            if (!answer_check_mark.codes[0].verified)
                            {
                                sb.AppendLine("Не пройдена криптографическая проверка!".ToUpper());
                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  не пройдена криптографическая проверка.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                            }

                            if (answer_check_mark.codes[0].sold)
                            {
                                if (cash_Check.check_type.SelectedIndex == 0)
                                {
                                    sb.AppendLine("Уже выведен из оборота!".ToUpper());
                                    MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  уже выведен из оборота.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                                }
                            }

                            if (answer_check_mark.codes[0].isBlocked)
                            {
                                sb.AppendLine("Заблокирован по решению ОГВ!".ToUpper());
                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  заблокирован по решению ОГВ.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                            }

                            if (answer_check_mark.codes[0].expireDate.Year > 2000)
                            {
                                if (answer_check_mark.codes[0].expireDate < DateTime.Now)
                                {
                                    sb.AppendLine("Истек срок годности!".ToUpper());
                                    MainStaticClass.write_cdn_log("CDN У товара с кодом маркировки " + mark_str_cdn + "  истек срок годности.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                                }
                            }

                            if (sb.Length == 0)
                            {
                                if (cash_Check.verifyCDN.ContainsKey(mark_str))
                                {
                                    cash_Check.verifyCDN.Remove(mark_str);
                                }

                                Cash_check.Requisite1260 requisite1260 = new Cash_check.Requisite1260();
                                requisite1260.req1262 = "030";
                                requisite1260.req1263 = "21.11.2023";
                                requisite1260.req1264 = "1944";
                                requisite1260.req1265 = "UUID=" + answer_check_mark.reqId + "&Time=" + answer_check_mark.reqTimestamp;
                                cash_Check.verifyCDN.Add(mark_str, requisite1260);

                                result_check = true;
                            }
                            else
                            {
                                int stringCount = sb.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
                                if (stringCount == 1)
                                {
                                    sb.Insert(0, "Код маркировки " + mark_str + "\r\nне прошел проверку по следующей причине:\r\n".ToUpper());
                                }
                                else
                                {
                                    sb.Insert(0, "Код маркировки " + mark_str + "\r\nне прошел проверку по следующим причинам:\r\n".ToUpper());
                                }
                                sb.Append(s);
                                sb.AppendLine(d_tovar.Keys.ElementAt(0));
                                sb.AppendLine(d_tovar[d_tovar.Keys.ElementAt(0)]);
                                await MessageBox.Show(sb.ToString(), "Ошибки при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error);
                            }
                        }
                        else//это была офлайн проверка 
                        {
                            await MessageBox.Show("Офлайн проверка кода маркировки заблокирован = " + answer_check_mark.codes[0].isBlocked.ToString(), "Офлайн", MessageBoxButton.OK, MessageBoxType.Info, cash_Check);

                            // Исправлено: используем result.JsonData вместо старого response.Data
                            MessageBox.Show(result.JsonData, "Ответ", cash_Check);

                            if (answer_check_mark.codes[0].isBlocked)
                            {
                                result_check = false;
                                await MessageBox.Show("Офлайн проверка кода маркировки\r\nДанный код заблокирован", "Ошибка при работе с кодом аркировки", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                            }
                            else
                            {
                                if (GetMarkingBalance(mark_str) > 0)
                                {
                                    await MessageBox.Show("Данный код марикровки найден в уже проданных.", "Ошибка при продаже марикрованного товара", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                                    result_check = false;
                                    return result_check;
                                }

                                if (cash_Check.verifyCDN.ContainsKey(mark_str))
                                {
                                    cash_Check.verifyCDN.Remove(mark_str);
                                }

                                Cash_check.Requisite1260 requisite1260 = new Cash_check.Requisite1260();
                                requisite1260.req1262 = "030";
                                requisite1260.req1263 = "21.11.2023";
                                requisite1260.req1264 = "1944";
                                requisite1260.req1265 = "UUID=" + answer_check_mark.reqId +
                                                        "&Time=" + answer_check_mark.reqTimestamp +
                                                        "&Inst=" + answer_check_mark.inst +
                                                        "&Ver=" + answer_check_mark.version;

                                cash_Check.verifyCDN.Add(mark_str, requisite1260);
                                result_check = true;
                            }
                        }
                    }
                    else
                    {
                        if (answer_check_mark.codes[0].errorCode == 10)
                        {
                            await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки данный код не найден в БД ЧЗ", "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                            result_check = false;
                        }
                        else if (answer_check_mark.codes[0].errorCode == 203)
                        {
                            await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                            if (!MainStaticClass.PiotError203)
                            {
                                MainStaticClass.PiotError203 = true;
                                result_check = true;
                            }
                            else
                            {
                                result_check = false;
                            }
                        }
                        else
                        {
                            await MessageBoxHelper.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                            result_check = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageBoxHelper.Show(ex.Message + "\r\n" + ex.StackTrace, "Ошибка при запросе к ПИот", MessageBoxButton.OK, MessageBoxType.Error, cash_Check);
                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + ex.Message + "\r\n" + ex.StackTrace, cash_Check.numdoc.ToString(), codes[0].ToString(), "2");
                result_check = false;
            }

            return result_check;
        }
    }
}
