using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    internal class Piot
    {

        // Добавляем новый метод для получения информации из API
        // Добавляем новый метод для получения информации из API
        public PiotInfo GetPiotInfo()
        {
            // Проверяем, есть ли уже данные в статическом классе
            if (MainStaticClass.PiotInfo != null)
            {
                return MainStaticClass.PiotInfo;
            }

            try
            {
                // 1. Добавляем поддержку TLS 1.2 (обязательно для современных серверов)
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;

                // 2. Разрешаем самоподписанные сертификаты для локального сервера
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;

                string url = MainStaticClass.GetPiotUrl + "/info";
                //string url = "https://esm-emu.ao-esp.ru/info";
                //string url = "127.0.0.1:51401/info";

                // Создаем запрос
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

                // Для POST запроса обязательно нужно тело
                byte[] data = Encoding.UTF8.GetBytes("{}");
                request.ContentLength = data.Length;

                // Пишем тело запроса
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                // Получаем ответ
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string jsonResponse = reader.ReadToEnd();
                    var info = JsonConvert.DeserializeObject<PiotInfo>(jsonResponse);

                    // Сохраняем в статический класс для последующего использования
                    MainStaticClass.PiotInfo = info;

                    return info;
                }
            }
            catch (WebException ex)
            {
                // Обработка ошибок HTTP
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        string errorText = reader.ReadToEnd();
                        throw new Exception($"Ошибка при получении информации ПИОТ: {(int)errorResponse.StatusCode} - {errorText}", ex);
                    }
                }
                throw new Exception("Ошибка сети при получении информации ПИОТ: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при получении информации ПИОТ: " + ex.Message, ex);
            }
        }


        // Класс для десериализации ответа от API информации
        public class PiotInfo
        {
            [JsonProperty("tspiotId")]
            public string tspiotId { get; set; }

            [JsonProperty("kktSerial")]
            public string kktSerial { get; set; }

            [JsonProperty("fnSerial")]
            public string fnSerial { get; set; }

            [JsonProperty("kktInn")]
            public string kktInn { get; set; }

            [JsonProperty("codesCheckTimeout")]
            public int codesCheckTimeout { get; set; }

            // Метод для вывода информации в читаемом формате
            public override string ToString()
            {
                return $"TSPIOT ID: {tspiotId}\r\n" +
                       $"Серийный номер ККТ: {kktSerial}\r\n" +
                       $"Серийный номер ФН: {fnSerial}\r\n" +
                       $"ИНН ККТ: {kktInn}";
            }
        }

        // Пример использования в существующем методе (можно добавить вызов где нужно)
        public async void CheckPiotConnection()
        {
            try
            {
                PiotInfo info = GetPiotInfo();
                await MessageBox.Show($"Успешное подключение к ПИОТ:\r\n{info.ToString()}",
                    "Информация ПИОТ", MessageBoxButton.OK, MessageBoxType.Info);
            }
            catch (Exception ex)
            {
                await MessageBox.Show($"Ошибка подключения к ПИОТ:\r\n{ex.Message}",
                    "Ошибка ПИОТ", MessageBoxButton.OK, MessageBoxType.Error);
            }
        }

        /// <summary>
        /// Получает баланс продаж/возвратов для маркировки
        /// </summary>
        /// <returns>Баланс: >0 - продан, =0 - нейтрально, <0 - возвращен</returns>
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


        private string ToHexDump(string input)
        {
            if (string.IsNullOrEmpty(input)) return "[EMPTY]";
            var bytes = Encoding.UTF8.GetBytes(input);
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        


        //public bool cdn_check_marker_code(List<string> codes, string mark_str, ref HttpWebRequest request, string mark_str_cdn, Dictionary<string, string> d_tovar, Cash_check cash_Check, ProductData productData)
        public async Task<bool> cdn_check_marker_code(List<string> codes, string mark_str, Int64 numdoc, HttpWebRequest request, string mark_str_cdn, Dictionary<string, string> d_tovar, Cash_check cash_Check, ProductData productData)
        {
            bool result_check = false;

            StringBuilder sb = new StringBuilder();

            //string code = "MDEwNDYyOTMwODg3NzA0NDIxRHprY1l0Mh04MDA1MTc3MDAwHTkzZEdWeg==";
            string url = "";
            //if (cash_Check.comboBox_mode.SelectedIndex == 0)
            //{
            //url = "https://esm-emu.ao-esp.ru/api/v1/codes/check";
            //url = "https://127.0.0.1:51401/api/v1/codes/check";
            url = MainStaticClass.GetPiotUrl + "/codes/check";
            //}


            //если 1.13 и 2 строка в документе тогда включается локальный модуль 
            //Потом это надо будет убрать 
            //if (((cash_Check.comboBox_mode.SelectedItem == "1.13")&&(cash_Check.listView1.Items.Count>0)) 
            //    || cash_Check.comboBox_mode.SelectedItem == "1.14"
            //    || cash_Check.comboBox_mode.SelectedItem == "1.16"
            //    || cash_Check.comboBox_mode.SelectedItem == "1.17")
            //{
            //    url = "https://esm-emu.ao-esp.ru/api/v1/codes/checkoffline";//оффлайн 
            //}

        //    // Использование:
        //    string originalHex = ToHexDump(mark_str);
        //    string marking_code = mark_str.Replace("\u001d", "\\u001d"); // Заменяем СИМВОЛ на текст
        //    //string marking_code = mark_str.Replace("\\u001d", @"u001d");

        //    string resultHex = ToHexDump(marking_code);

        //    await MessageBox.Show(
        //$"ОРИГИНАЛ (HEX): {originalHex}\n\n" +
        //$"РЕЗУЛЬТАТ (HEX): {resultHex}\n\n" +
        //$"Было байт: {originalHex.Length}, Стало байт: {resultHex.Length}",
        //        "GS Debug Hex");

            ApiResponse apiResponse = null;

            //string marking_code = mark_str.Replace("\u001d", "\\u001d"); // Заменяем СИМВОЛ на текст

            string marking_code = mark_str.Replace("\\u001d", @"u001d");
            //string marking_code = mark_str.Replace("\u001d", @"u001d");            

            // Заполняем информацию о клиенте
            var clientInfo = new ClientInfo
            {
                name = "Cash8Avalon",
                version = "1.0.0",
                id = "7c9e6679-7425-40de-944b-e07fc1f90ae7",
                token = "6ba7b810-9dad-11d1-80b4-00c04fd430c8" // Замените на реальный токен
            };

            // Отправляем запрос
            var apiClient = new ApiClient();
            try
            {
                byte[] textAsBytes = Encoding.Default.GetBytes(marking_code);
                //byte[] textAsBytes = Encoding.Default.GetBytes(mark_str_cdn);

                string imc = Convert.ToBase64String(textAsBytes);
                var response = apiClient.SendCodeRequest(imc, url, clientInfo);
                if (!response.Success)
                {
                    //if (cash_Check.comboBox_mode.SelectedItem != null)
                    //{
                    //    if ((cash_Check.comboBox_mode.SelectedItem == "1.15") || (cash_Check.comboBox_mode.SelectedItem == "1.17"))
                    //    {
                    //        MessageBox.Show(response.Exception.Message, "Онлайн проверка. Ошибка при работе с ПИот", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //        url = "https://esm-emu.ao-esp.ru/api/v1/codes/checkoffline";//оффлайн 
                    //        response = apiClient.SendCodeRequest(imc, url, clientInfo);
                    //    }
                    //    else
                    //    {
                    //        throw new Exception(response.Exception.Message, response.Exception);
                    //    }
                    //}
                    //else
                    //{
                    throw new Exception(response.Exception.Message, response.Exception);
                    //}
                }

                //строка ниже это когда офлайн ответ

                //                string response = @"{
                //""codesResponse"": {
                //""codesResponse"": [
                //{
                //""code"": 0,
                //""codes"": [
                //{
                //""cis"": ""0104670540176099215MpGKy"",
                //""found"": false,
                //""valid"": false,
                //""printView"": ""0104670540176099215MpGKy"",
                //""gtin"": ""04670540176099"",
                //""groupIds"": [],
                //""verified"": false,
                //""realizable"": false,
                //""utilised"": false,
                //""isBlocked"": true,
                //""ogvs"": []
                //}
                //],
                //""reqId"": ""c9188551-817a-85a7-93e4-7042d907ab13"",
                //""reqTimestamp"": ""1757681987579"",
                //""isCheckedOffline"": true,
                //""version"": ""6e7f1224-0e08-41ed-844c-d386675f4e50"",
                //""inst"": ""4679b3db-da6a-44e0-a2e6-a684437bafb0""
                //}
                //]
                //}
                //}";
                apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response.Data);
                //var answer_check_mark=;
                //if (apiResponse.errorCode != null)
                //{
                //    answer_check_mark = apiResponse.codesResponse.codesResponse[0];
                //}
                //else
                //{
                //    return result_check;
                //}

                ResponseItem answer_check_mark = null; // Инициализируем как null

                if (apiResponse.codesResponse != null && apiResponse.codesResponse.codesResponse != null && apiResponse.codesResponse.codesResponse.Count > 0)
                {
                    answer_check_mark = apiResponse.codesResponse.codesResponse[0];
                }
                else if (apiResponse.errorCode != null)
                {
                    // Это аварийный режим или другая ошибка
                    result_check = true;
                    return result_check;
                }
                else
                {
                    // Неожиданный формат ответа
                    return result_check;
                }

                if (answer_check_mark.code == 0) // Это успех
                {
                    if (answer_check_mark.codes[0].errorCode == 0)
                    {
                        if (!answer_check_mark.isCheckedOffline)//Это была онлайн проверка 
                        {
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
                                //sb.AppendLine("Не найден в ГИС МТ!".ToUpper());
                                //MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " не найден в ГИС МТ", "Документ чек", cash_Check.numdoc.ToString());
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
                            if (answer_check_mark.codes[0].isBlocked)
                            {
                                result_check = false;
                                await MessageBox.Show("Офлайн проверка кода маркировки\r\nДанный код заблокирован", "Ошибка при работе с кодом аркировки", MessageBoxButton.OK, MessageBoxType.Error);
                            }
                            else
                            {

                                if (GetMarkingBalance(mark_str) > 0)
                                {
                                    await MessageBox.Show("Данный код марикровки найден в уже проданных.", "Ошибка при продаже марикрованного товара", MessageBoxButton.OK, MessageBoxType.Error);
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
                                requisite1260.req1265 = "UUID=" + answer_check_mark.reqId + "&Time=" + answer_check_mark.reqTimestamp;
                                cash_Check.verifyCDN.Add(mark_str, requisite1260);

                                result_check = true;
                            }

                        }
                    }
                    else
                    {
                        if (answer_check_mark.codes[0].errorCode == 10)
                        {
                            await MessageBox.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки данный код не найден в БД ЧЗ", "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error);
                        }
                        else
                        {
                            await MessageBox.Show("Произошли ошибки при запросе к ПИОТ \r\nКод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message, "Ошибка при работе с ПИот", MessageBoxButton.OK, MessageBoxType.Error);
                        }
                        result_check = false;
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show(ex.Message, "Ошибка при запросе к ПИот", MessageBoxButton.OK, MessageBoxType.Error);
                //MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  не пройдена криптографическая проверка."+ ex.Message, cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
                result_check = false;
            }

            return result_check;
        }

        //public bool cdn_check_marker_code_online(List<string> codes, string mark_str, Int64 numdoc, ref HttpWebRequest request, string mark_str_cdn, Dictionary<string, string> d_tovar, Cash_check cash_Check, ProductData productData)
        //{
        //    bool result_check = false;

        //    StringBuilder sb = new StringBuilder();

        //    //string code = "MDEwNDYyOTMwODg3NzA0NDIxRHprY1l0Mh04MDA1MTc3MDAwHTkzZEdWeg==";
        //    string url = "";
        //    //if (cash_Check.comboBox_mode.SelectedIndex == 0)
        //    //{
        //    url = "https://esm-emu.ao-esp.ru/api/v1/codes/check";//онлайн                 
        //    //}


        //    //если 1.13 и 2 строка в документе тогда включается локальный модуль 
        //    //Потом это надо будет убрать 
        //    if (((cash_Check.comboBox_mode.SelectedItem == "1.13") && (cash_Check.listView1.Items.Count > 0))
        //        || cash_Check.comboBox_mode.SelectedItem == "1.14"
        //        || cash_Check.comboBox_mode.SelectedItem == "1.15"
        //        || cash_Check.comboBox_mode.SelectedItem == "1.16"
        //        || cash_Check.comboBox_mode.SelectedItem == "1.17")
        //    {
        //        url = "https://esm-emu.ao-esp.ru/api/v1/codes/checkoffline";//оффлайн 
        //    }



        //    ApiResponse apiResponse = null;
        //    string marking_code = mark_str.Replace("\\u001d", @"u001d");
        //    //string marking_code = mark_str.Replace("\u001d", @"u001d");


        //    // Заполняем информацию о клиенте
        //    var clientInfo = new ClientInfo
        //    {
        //        name = "Cash8",
        //        version = "1.0.0",
        //        id = "client123",
        //        token = "your_token_here" // Замените на реальный токен
        //    };

        //    // Отправляем запрос
        //    var apiClient = new ApiClient();
        //    try
        //    {
        //        byte[] textAsBytes = Encoding.Default.GetBytes(marking_code);
        //        //byte[] textAsBytes = Encoding.Default.GetBytes(mark_str_cdn);

        //        string imc = Convert.ToBase64String(textAsBytes);

        //        string response = apiClient.SendCodeRequest(imc, url, clientInfo);
        //        //строка ниже это когда офлайн ответ

        //        //                string response = @"{
        //        //""codesResponse"": {
        //        //""codesResponse"": [
        //        //{
        //        //""code"": 0,
        //        //""codes"": [
        //        //{
        //        //""cis"": ""0104670540176099215MpGKy"",
        //        //""found"": false,
        //        //""valid"": false,
        //        //""printView"": ""0104670540176099215MpGKy"",
        //        //""gtin"": ""04670540176099"",
        //        //""groupIds"": [],
        //        //""verified"": false,
        //        //""realizable"": false,
        //        //""utilised"": false,
        //        //""isBlocked"": true,
        //        //""ogvs"": []
        //        //}
        //        //],
        //        //""reqId"": ""c9188551-817a-85a7-93e4-7042d907ab13"",
        //        //""reqTimestamp"": ""1757681987579"",
        //        //""isCheckedOffline"": true,
        //        //""version"": ""6e7f1224-0e08-41ed-844c-d386675f4e50"",
        //        //""inst"": ""4679b3db-da6a-44e0-a2e6-a684437bafb0""
        //        //}
        //        //]
        //        //}
        //        //}";
        //        apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);
        //        var answer_check_mark = apiResponse.codesResponse.codesResponse[0];

        //        if (answer_check_mark.code == 0) // Это успех
        //        {
        //            if (answer_check_mark.codes[0].errorCode == 0)
        //            {
        //                if (!answer_check_mark.isCheckedOffline)//Это была онлайн проверка 
        //                {
        //                    string s = "ТОВАР НЕ МОЖЕТ БЫТЬ ПРОДАН!\r\n";
        //                    if (!answer_check_mark.codes[0].isOwner)
        //                    {
        //                        if (answer_check_mark.codes[0].groupIds != null)
        //                        {
        //                            if ((answer_check_mark.codes[0].groupIds[0] != 23) && (answer_check_mark.codes[0].groupIds[0] != 8) && (answer_check_mark.codes[0].groupIds[0] != 15) && (answer_check_mark.codes[0].groupIds[0] != 3))
        //                            {
        //                                if (!productData.RrNotControlOwner())
        //                                {
        //                                    MessageBox.Show(" Исключения групп маркрировки  23|8|15 \r\n Текущая группа маркировки  " + answer_check_mark.codes[0].groupIds[0].ToString());
        //                                    if (cash_Check.check_type.SelectedIndex == 0)
        //                                    {
        //                                        sb.AppendLine("Вы не являетесь владельцем!".ToUpper());
        //                                        MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " Вы не являетесь владельцем ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //                                    }
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            sb.AppendLine("Не удалось определить группу товара");
        //                        }
        //                    }

        //                    if (!answer_check_mark.codes[0].valid)
        //                    {
        //                        sb.AppendLine("Результат проверки валидности структуры КИ / КиЗ не прошла проверку !".ToUpper());
        //                        MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + "Проверки валидности структуры КИ / КиЗ не прошла проверку !", "Документ чек", cash_Check.numdoc.ToString());
        //                    }

        //                    if (!answer_check_mark.codes[0].found)
        //                    {
        //                        sb.AppendLine("Не найден в ГИС МТ!".ToUpper());
        //                        MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " не найден в ГИС МТ", "Документ чек", cash_Check.numdoc.ToString());
        //                        if ((!answer_check_mark.codes[0].realizable) && (!answer_check_mark.codes[0].sold))
        //                        {
        //                            sb.AppendLine("Нет информации о вводе в оборот!".ToUpper());
        //                            MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " нет информации о вводе в оборот. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //                        }
        //                    }

        //                    if (answer_check_mark.codes[0].found)
        //                    {
        //                        //sb.AppendLine("Не найден в ГИС МТ!".ToUpper());
        //                        //MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " не найден в ГИС МТ", "Документ чек", cash_Check.numdoc.ToString());
        //                        if (answer_check_mark.codes[0].groupIds[0] != 3)//Для табака исключение 
        //                        {
        //                            if ((!answer_check_mark.codes[0].realizable) && (!answer_check_mark.codes[0].sold) && (answer_check_mark.codes[0].utilised))
        //                            {
        //                                sb.AppendLine("Нет информации о вводе в оборот!".ToUpper());
        //                                MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " нет информации о вводе в оборот. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //                            }
        //                        }
        //                    }

        //                    if (!answer_check_mark.codes[0].utilised)
        //                    {
        //                        sb.AppendLine("Эмитирован, но нет информации о его нанесении!".ToUpper());
        //                        MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " эмитирован, но нет информации о его нанесении. ", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //                    }

        //                    if (!answer_check_mark.codes[0].verified)
        //                    {
        //                        sb.AppendLine("Не пройдена криптографическая проверка!".ToUpper());
        //                        MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  не пройдена криптографическая проверка.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //                    }

        //                    if (answer_check_mark.codes[0].sold)
        //                    {
        //                        if (cash_Check.check_type.SelectedIndex == 0)
        //                        {
        //                            sb.AppendLine("Уже выведен из оборота!".ToUpper());
        //                            MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  уже выведен из оборота.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //                        }
        //                    }

        //                    if (answer_check_mark.codes[0].isBlocked)
        //                    {
        //                        sb.AppendLine("Заблокирован по решению ОГВ!".ToUpper());
        //                        MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  заблокирован по решению ОГВ.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //                    }
        //                    if (answer_check_mark.codes[0].expireDate.Year > 2000)
        //                    {
        //                        if (answer_check_mark.codes[0].expireDate < DateTime.Now)
        //                        {
        //                            sb.AppendLine("Истек срок годности!".ToUpper());
        //                            MainStaticClass.write_cdn_log("CDN У товара с кодом маркировки " + mark_str_cdn + "  истек срок годности.", cash_Check.numdoc.ToString(), codes[0].ToString(), "1");

        //                        }
        //                    }
        //                    if (sb.Length == 0)
        //                    {

        //                        if (cash_Check.verifyCDN.ContainsKey(mark_str))
        //                        {
        //                            cash_Check.verifyCDN.Remove(mark_str);
        //                        }

        //                        Cash_check.Requisite1260 requisite1260 = new Cash_check.Requisite1260();
        //                        requisite1260.req1262 = "030";
        //                        requisite1260.req1263 = "21.11.2023";
        //                        requisite1260.req1264 = "1944";
        //                        requisite1260.req1265 = "UUID=" + answer_check_mark.reqId + "&Time=" + answer_check_mark.reqTimestamp;
        //                        cash_Check.verifyCDN.Add(mark_str, requisite1260);

        //                        result_check = true;
        //                    }
        //                    else
        //                    {
        //                        int stringCount = sb.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
        //                        if (stringCount == 1)
        //                        {
        //                            sb.Insert(0, "Код маркировки " + mark_str + "\r\nне прошел проверку по следующей причине:\r\n".ToUpper());
        //                        }
        //                        else
        //                        {
        //                            sb.Insert(0, "Код маркировки " + mark_str + "\r\nне прошел проверку по следующим причинам:\r\n".ToUpper());
        //                        }
        //                        sb.Append(s);
        //                        sb.AppendLine(d_tovar.Keys.ElementAt(0));
        //                        sb.AppendLine(d_tovar[d_tovar.Keys.ElementAt(0)]);
        //                        MessageBox.Show(sb.ToString());
        //                    }
        //                }
        //                else//это была офлайн проверка 
        //                {
        //                    if (answer_check_mark.codes[0].isBlocked)
        //                    {
        //                        result_check = false;
        //                        MessageBox.Show("Офлайн проверка кода маркировки\r\nДанный код заблокирован");
        //                    }
        //                    else
        //                    {
        //                        if (cash_Check.verifyCDN.ContainsKey(mark_str))
        //                        {
        //                            cash_Check.verifyCDN.Remove(mark_str);
        //                        }

        //                        Cash_check.Requisite1260 requisite1260 = new Cash_check.Requisite1260();
        //                        requisite1260.req1262 = "030";
        //                        requisite1260.req1263 = "21.11.2023";
        //                        requisite1260.req1264 = "1944";
        //                        requisite1260.req1265 = "UUID=" + answer_check_mark.reqId + "&Time=" + answer_check_mark.reqTimestamp;
        //                        cash_Check.verifyCDN.Add(mark_str, requisite1260);

        //                        result_check = true;
        //                    }

        //                }
        //            }
        //            else
        //            {
        //                MessageBox.Show("Произошли ошибки при запросе к ПИОТ \r\nкод ошибки = " + answer_check_mark.codes[0].errorCode + "\r\nТекст ошибки " + answer_check_mark.codes[0].message);
        //                result_check = false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Произошли ошибки при запросе к ПИОТ \r\n" + ex.Message);
        //        //MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  не пройдена криптографическая проверка."+ ex.Message, cash_Check.numdoc.ToString(), codes[0].ToString(), "1");
        //        result_check = false;
        //    }

        //    return result_check;
        //}



        public class ApiResponse
        {

            // Поля для ошибки
            [JsonProperty("code")]
            public int? errorCode { get; set; }

            [JsonProperty("message")]
            public string errorMessage { get; set; }

            [JsonProperty("codesResponse")]
            public CodesResponseWrapper codesResponse { get; set; }
        }

        public class CodesResponseWrapper
        {
            [JsonProperty("codesResponse")]
            public List<ResponseItem> codesResponse { get; set; }
        }

        public class ResponseItem
        {
            [JsonProperty("code")]
            public int code { get; set; }

            [JsonProperty("description")]
            public string description { get; set; }

            [JsonProperty("codes")]
            public List<CodeDetail> codes { get; set; }

            [JsonProperty("reqId")]
            public string reqId { get; set; }

            [JsonProperty("reqTimestamp")]
            public long reqTimestamp { get; set; }

            [JsonProperty("isCheckedOffline")]
            public bool isCheckedOffline { get; set; }
        }

        public class CodeDetail
        {
            [JsonProperty("cis")]
            public string cis { get; set; }

            [JsonProperty("found")]
            public bool found { get; set; }

            [JsonProperty("valid")]
            public bool valid { get; set; }

            [JsonProperty("printView")]
            public string printView { get; set; }

            [JsonProperty("gtin")]
            public string gtin { get; set; }

            [JsonProperty("groupIds")]
            public List<int> groupIds { get; set; }

            [JsonProperty("verified")]
            public bool verified { get; set; }

            [JsonProperty("realizable")]
            public bool realizable { get; set; }

            [JsonProperty("utilised")]
            public bool utilised { get; set; }

            [JsonProperty("productionDate")]
            public DateTime? productionDate { get; set; }

            [JsonProperty("isOwner")]
            public bool isOwner { get; set; }

            [JsonProperty("isBlocked")]
            public bool isBlocked { get; set; }

            [JsonProperty("ogvs")]
            public List<object> ogvs { get; set; }

            [JsonProperty("errorCode")]
            public int errorCode { get; set; }

            [JsonProperty("message")]
            public string message { get; set; }

            [JsonProperty("isTracking")]
            public bool isTracking { get; set; }

            [JsonProperty("sold")]
            public bool sold { get; set; }

            [JsonProperty("mrp")]
            public int? mrp { get; set; }

            [JsonProperty("grayZone")]
            public bool grayZone { get; set; }

            [JsonProperty("packageType")]
            public string packageType { get; set; }

            [JsonProperty("producerInn")]
            public string producerInn { get; set; }

            [JsonProperty("expireDate")]
            public DateTime expireDate { get; set; }

        }


        public class ClientInfo
        {
            public string name { get; set; }
            public string version { get; set; }
            public string id { get; set; }
            public string token { get; set; }
        }

        public class ClientData
        {
            public List<string> codes { get; set; }
            public ClientInfo client_info { get; set; }
        }


        //public class ApiClient
        //{
        //    public string SendCodeRequest(string code, string url, ClientInfo clientInfo)
        //    {
        //        try
        //        {
        //            // Создаем и заполняем объект данных
        //            var clientData = new ClientData
        //            {
        //                codes = new List<string> { code },
        //                client_info = clientInfo
        //            };

        //            // Сериализуем в JSON с помощью Newtonsoft.Json
        //            string jsonData = JsonConvert.SerializeObject(clientData);

        //            // Настраиваем web-запрос
        //            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        //            var request = (HttpWebRequest)WebRequest.Create(url);
        //            request.Timeout = 1500;
        //            request.Method = "POST";
        //            request.ContentType = "application/json";
        //            request.Accept = "application/json";
        //            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        //            // Записываем данные в тело запроса
        //            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
        //            {
        //                streamWriter.Write(jsonData);
        //                streamWriter.Flush();
        //            }

        //            // Получаем ответ
        //            using (var response = (HttpWebResponse)request.GetResponse())
        //            using (var streamReader = new StreamReader(response.GetResponseStream()))
        //            {
        //                string result = streamReader.ReadToEnd();
        //                return result;
        //            }
        //        }
        //        catch (WebException ex)
        //        {
        //            // Обработка ошибок HTTP
        //            if (ex.Response != null)
        //            {
        //                using (var errorResponse = (HttpWebResponse)ex.Response)
        //                using (var reader = new StreamReader(errorResponse.GetResponseStream()))
        //                {
        //                    string errorText = reader.ReadToEnd();
        //                    throw new Exception($"HTTP Error: {(int)errorResponse.StatusCode} - {errorText}", ex);
        //                }
        //            }
        //            throw new Exception("Network error: " + ex.Message, ex);
        //        }
        //    }
        //}

        public class ApiClient
        {
            public class ApiResponse
            {
                public bool Success { get; set; }
                public string Data { get; set; }
                public int? HttpStatusCode { get; set; }
                public Exception Exception { get; set; }

                public static ApiResponse CreateSuccess(string data, int statusCode)
                {
                    return new ApiResponse
                    {
                        Success = true,
                        Data = data,
                        HttpStatusCode = statusCode
                    };
                }

                public static ApiResponse CreateError(Exception exception, int? httpStatusCode = null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        HttpStatusCode = httpStatusCode,
                        Exception = exception
                    };
                }
            }

            public ApiResponse SendCodeRequest(string code, string url, ClientInfo clientInfo)
            {
                try
                {
                    // 🔥 1. Нормализуем URL — добавляем протокол, если отсутствует
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        // По умолчанию используем https для локального сервера с сертификатом
                        url = "https://" + url.TrimStart('/');
                    }

                    // Создаем и заполняем объект данных
                    var clientData = new ClientData
                    {
                        codes = new List<string> { code },
                        client_info = clientInfo
                    };

                    // Сериализуем в JSON с помощью Newtonsoft.Json
                    string jsonData = JsonConvert.SerializeObject(clientData);

                    // 🔥 2. Настраиваем TLS протоколы (обязательно для современных серверов)
                    ServicePointManager.SecurityProtocol =
                        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    // 🔥 3. ПРИНИМАЕМ САМОПОДПИСАННЫЕ СЕРТИФИКАТЫ (для локального сервера)
                    ServicePointManager.ServerCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) =>
                        {
                            // Для отладки можно логировать ошибки сертификатов
//#if DEBUG
//                            if (sslPolicyErrors != SslPolicyErrors.None)
//                            {
//                                System.Diagnostics.Debug.WriteLine(
//                                    $"[CDN] Cert warning: {sslPolicyErrors}, Subject: {certificate?.Subject}, Issuer: {certificate?.Issuer}");
//                            }
//#endif
                            // ✅ Принимаем все сертификаты (только для локального/тестового сервера!)
                            return true;
                        };

                    // 🔥 4. Отключаем проверку отзыва сертификата (для локального сервера)
                    ServicePointManager.CheckCertificateRevocationList = false;

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 1500; // 1.5 секунды таймаут
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Accept = "application/json";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

                    // 🔥 5. Записываем данные в тело запроса с явной кодировкой UTF-8
                    using (var stream = request.GetRequestStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.Write(jsonData);
                        writer.Flush();
                    }

                    // Получаем ответ
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var responseStream = response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string result = reader.ReadToEnd();
                        return ApiResponse.CreateSuccess(result, (int)response.StatusCode);
                    }
                }
                catch (WebException ex)
                {
                    // 🔥 6. Детальное логирование для диагностики
                    System.Diagnostics.Debug.WriteLine($"[CDN] WebException: Status={ex.Status}, Message={ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CDN] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");

                        // 🔥 Ключевое: если AuthenticationException — это проблема сертификата
                        if (ex.InnerException is System.Security.Authentication.AuthenticationException authEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CDN] ❗ СЕРТИФИКАТ: {authEx.Message}");
                        }
                    }

                    int? statusCode = null;
                    Exception exceptionToReturn = ex;

                    if (ex.Response != null)
                    {
                        try
                        {
                            using (var errorResponse = (HttpWebResponse)ex.Response)
                            using (var reader = new StreamReader(errorResponse.GetResponseStream(), Encoding.UTF8))
                            {
                                string errorText = reader.ReadToEnd();
                                statusCode = (int)errorResponse.StatusCode;
                                exceptionToReturn = new Exception($"HTTP Error: {statusCode} - {errorText}", ex);
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки чтения тела ошибки
                        }
                    }

                    return ApiResponse.CreateError(exceptionToReturn, statusCode);
                }
                catch (TimeoutException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CDN] Timeout: {ex.Message}");
                    return ApiResponse.CreateError(ex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CDN] Unexpected error: {ex.GetType().Name}: {ex.Message}");
                    return ApiResponse.CreateError(ex);
                }
            }

            //    public ApiResponse SendCodeRequest(string code, string url, ClientInfo clientInfo)
            //    {
            //        try
            //        {
            //            // Создаем и заполняем объект данных
            //            var clientData = new ClientData
            //            {
            //                codes = new List<string> { code },
            //                client_info = clientInfo
            //            };

            //            // Сериализуем в JSON с помощью Newtonsoft.Json
            //            string jsonData = JsonConvert.SerializeObject(clientData);

            //            // Настраиваем web-запрос
            //            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            //            var request = (HttpWebRequest)WebRequest.Create(url);
            //            //request.Timeout = MainStaticClass.PiotInfo.codesCheckTimeout == 0 ? 1500 : MainStaticClass.PiotInfo.codesCheckTimeout;
            //            request.Timeout = 1500;
            //            request.Method = "POST";
            //            request.ContentType = "application/json";
            //            request.Accept = "application/json";
            //            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

            //            // Записываем данные в тело запроса
            //            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            //            {
            //                streamWriter.Write(jsonData);
            //                streamWriter.Flush();
            //            }

            //            // Получаем ответ
            //            using (var response = (HttpWebResponse)request.GetResponse())
            //            using (var streamReader = new StreamReader(response.GetResponseStream()))
            //            {
            //                string result = streamReader.ReadToEnd();
            //                return ApiResponse.CreateSuccess(result, (int)response.StatusCode);
            //            }
            //        }
            //        catch (WebException ex)
            //        {
            //            int? statusCode = null;
            //            Exception exceptionToReturn = ex;

            //            if (ex.Response != null)
            //            {
            //                using (var errorResponse = (HttpWebResponse)ex.Response)
            //                using (var reader = new StreamReader(errorResponse.GetResponseStream()))
            //                {
            //                    string errorText = reader.ReadToEnd();
            //                    statusCode = (int)errorResponse.StatusCode;

            //                    // Создаем исключение с деталями HTTP
            //                    exceptionToReturn = new Exception($"HTTP Error: {statusCode} - {errorText}", ex);
            //                }
            //            }

            //            return ApiResponse.CreateError(exceptionToReturn, statusCode);
            //        }
            //        catch (TimeoutException ex)
            //        {
            //            return ApiResponse.CreateError(ex);
            //        }
            //        catch (Exception ex)
            //        {
            //            return ApiResponse.CreateError(ex);
            //        }
            //    }
            //}
        }
    }
}
