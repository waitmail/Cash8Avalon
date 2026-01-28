using Cash8Avalon;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Cash8Avalon
{
    class CDN
    {
        string kontur_url = "https://cdn.crpt.ru";//рабочий контур
        //string kontur_url     = "https://markirovka.sandbox.crptech.ru";//тестовый контур
        string info_url = "/api/v4/true-api/cdn/info";
        string health_url = "/api/v4/true-api/cdn/health/check";
        string codes_url = "/api/v4/true-api/codes/check";
        //string token    = "537e95bb-eb82-4eb5-83f6-4d177d4eed49";//тестовый токен
        //string token      = "c09faf94-383f-4fcf-a2da-2e786a585d8e";//боевой токен
        public int error_timeout = 0;//это счетчик ошибок при проверке кодов маркировки поможет понять что например нет интрнета


        //Если при проверке продукции через CDN-площадку 3 раза подряд не удаётся получить ответ на
        //запрос в течение 1.5 секунд, то необходимо пометить в своей информационной системе эту
        //площадку на 15 минут как недоступную и переключиться на следующую по приоритету в списке
        //CDN-площадку
        public class Host
        {
            public string host { get; set; }
            public int avgTimeMs { get; set; }
            public long latensy { get; set; }
            public DateTime dateTime { get; set; }//здесь ставится время по которому идет отбор доступных,
        }

        public class CDN_List
        {
            public int code { get; set; }
            public string description { get; set; }
            public List<Host> hosts { get; set; }
            public DateTime createDateTime { get; set; }
        }

        public class CDN_Health
        {
            public int code { get; set; }
            public string description { get; set; }
            public int avgTimeMs { get; set; }
        }

        public class CheckMark
        {
            public List<string> codes { get; set; }
            public string fiscalDriveNumber { get; set; }
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Code
        {
            public string cis { get; set; }
            public bool valid { get; set; }
            public string printView { get; set; }
            public string gtin { get; set; }
            public List<int> groupIds { get; set; }
            public bool verified { get; set; }
            public bool found { get; set; }
            public bool realizable { get; set; }
            public bool utilised { get; set; }
            public bool isBlocked { get; set; }
            public bool isOwner { get; set; }
            public DateTime expireDate { get; set; }
            public DateTime productionDate { get; set; }
            public int errorCode { get; set; }
            public bool isTracking { get; set; }
            public bool sold { get; set; }
            public string packageType { get; set; }
            public string producerInn { get; set; }
            public bool grayZone { get; set; }
            public int soldUnitCount { get; set; }
            public int innerUnitCount { get; set; }
        }

        public class AnswerCheckMark
        {
            public int code { get; set; }
            public string description { get; set; }
            public List<Code> codes { get; set; }
            public string reqId { get; set; }
            public long reqTimestamp { get; set; }
        }

        private CDN_List get_cdn_info()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            string url = kontur_url + info_url;
            CDN_List list = null;
            try
            {
                // Создание объекта HttpWebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 5000;
                // Добавление заголовков            
                request.Headers.Add("X-API-KEY", MainStaticClass.CDN_Token);
                request.ContentType = "application/json";

                // Получение ответа от сервера
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // Вывод статус кода ответа
                    //Console.WriteLine("Status Code: " + (int)response.StatusCode);
                    int status_code = (int)response.StatusCode;
                    if (status_code != 200)
                    {
                        MessageBox.Show("Получен неверный ответ от сервера при запросе списка CDN серверов, кодответа = " + status_code.ToString(), "Получение списка CDN серверов");
                        MainStaticClass.write_cdn_log("Получен неверный ответ от сервера при запросе списка CDN серверов, код ответа = " + status_code.ToString(), "0", "", "3");
                        return list;
                    }

                    // Чтение и вывод содержимого ответа
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string sdn_list = reader.ReadToEnd();
                            //MessageBox.Show("Response: " + sdn_list);
                            //txtB_result.Text = sdn_list;
                            list = JsonConvert.DeserializeObject<CDN_List>(sdn_list);
                            list.createDateTime = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MainStaticClass.write_event_in_log("Получение списка CDN get_cdn_info " + ex.Message, "Документ чек", "0");
                MainStaticClass.write_cdn_log("Получение списка CDN get_cdn_info " + ex.Message, "0", "", "3");
                MessageBox.Show("Ошибка при запросе списка CDN площадок " + ex.Message);
            }

            if (list == null)//Необходимо заполнить его из кеша
            {
                list = load_cash_cdn();
                MainStaticClass.write_cdn_log("Cписок CDN успешно загружен из кеша load_cash_cdn ", "0", "", "2");
            }

            return list;
        }

        private CDN.CDN_List load_cash_cdn()
        {
            NpgsqlConnection conn = MainStaticClass.NpgsqlConn();
            NpgsqlCommand command = null;
            CDN.CDN_List list = new CDN_List();
            list.createDateTime = DateTime.Now;
            list.hosts = new List<Host>();
            list.code = 0;

            try
            {

                string query = "SELECT host, latensy, date FROM public.cdn_cash;";
                conn.Open();
                command = new NpgsqlCommand(query, conn);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Host host = new Host();
                    host.host = reader["host"].ToString();
                    list.hosts.Add(host);
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Произошла ошибка при попытке загрузить список CDN из кеша" + ex.Message, "Загрузка списка CDN из кеша");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при попытке загрузить список CDN из кеша" + ex.Message, "Загрузка списка CDN из кеша");
            }
            finally
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
                }
                if (command != null)
                {
                    command.Dispose();
                }
            }

            return list;
        }


        public class CDNHealth
        {
            public int code { get; set; }
            public string description { get; set; }
            public int avgTimeMs { get; set; }
            public long latency { get; set; }
        }

        ///// <summary>
        ///// Количество подходящих 
        ///// CDN серверов
        ///// </summary>
        ///// <param name="cdn_list"></param>
        ///// <returns></returns>
        //public int selection_and_sorting(CDN_List cdn_list)
        //{
        //    cdn_list.hosts = cdn_list.hosts.Where(h => h.dateTime < DateTime.Now).OrderBy(h => h.latensy).ToList();
        //    return cdn_list.hosts.Count;
        //}


        /// <summary>
        /// Возращает список cdn серверов
        /// </summary>
        /// <returns></returns>
        public CDN_List get_cdn_list()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            CDN_List list = get_cdn_info();
            if (list != null)
            {
                list = get_cdn_list_health(list);
            }
            //Проверить если доступные сдн, может быть так что весь список серверов недоступен если так то запросить новый список серверов  

            return list;
        }

        private CDNHealth invoke_CDNHealth(string url)
        {
            CDNHealth result = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // Запуск функции с параметром в новом потоке            
            Task<CDNHealth> task = Task.Factory.StartNew(() => cdn_health_check(url));

            try
            {
                // Ожидание результата функции в течение 5 секунд
                bool isCompletedSuccessfully = task.Wait(TimeSpan.FromSeconds(3));

                if (isCompletedSuccessfully)
                {
                    // Если задача завершена успешно, получаем результат
                    MainStaticClass.write_event_in_log("Удачное завершение invoke_CDNHealth " + url, "Документ чек", "0");
                    result = task.Result;
                    //Console.WriteLine("Результат функции: " + result);
                }
                else
                {
                    // Если результат не был получен в течение 5 секунд
                    //Console.WriteLine("Функция не завершила выполнение в отведённое время.");
                    MainStaticClass.write_event_in_log("Произошли ошибка при invoke_CDNHealth " + url + " Timeout ", "Документ чек", "0");
                    cts.Cancel(); // Отправка запроса на отмену задачи
                }
            }
            catch (AggregateException ae)
            {
                //Обработка исключений, которые могли быть выброшены во время выполнения функции
                foreach (var e in ae.InnerExceptions)
                {
                    //Console.WriteLine("Исключение: " + e.Message);
                    MainStaticClass.write_event_in_log("Произошли ошибка при invoke_CDNHealth " + url + " " + e.Message, "Документ чек", "0");
                }
            }

            return result;
        }


        /// <summary>
        /// Возращает список cdn серверов с данными по доступу к ним
        /// если нет доступа то устанваливается время текущее  + 15 мин.
        /// </summary>
        /// <returns></returns>
        public CDN_List get_cdn_list_health(CDN_List list)
        {
            if (list != null)
            {
                try
                {
                    CDNHealth cDNHealth = null;
                    if (list.code == 0)//ответ без ошибок 
                    {
                        foreach (Host host in list.hosts)
                        {
                            // MessageBox.Show(host.host.ToString(), "cdn_health_check");
                            //cDNHealth = cdn_health_check(host.host.ToString());
                            cDNHealth = invoke_CDNHealth(host.host.ToString());//gaa пока убираю многопоток 
                            //cDNHealth = cdn_health_check(host.host.ToString());
                            if (cDNHealth != null)
                            {
                                if (cDNHealth.code == 0)
                                {
                                    host.avgTimeMs = cDNHealth.avgTimeMs;
                                    host.dateTime = DateTime.Now;
                                }
                                else
                                {
                                    host.dateTime = DateTime.Now.AddMinutes(15);
                                }
                                host.latensy = cDNHealth.latency;
                            }
                            else
                            {
                                host.dateTime = DateTime.Now.AddMinutes(15);
                                host.latensy = 999999;
                            }
                        }
                        //list.hosts = list.hosts.OrderBy(h => h.latensy).ThenByDescending(h => h.dateTime).ToList();
                        //возвращает список с наименьшим latensy и где время доступности меньше чем текущее, время может быть больше если площадка была помечена как не рабочая на 15 минут 
                        //list.hosts = list.hosts.Where(h => h.dateTime < DateTime.Now).OrderBy(h => h.latensy).ToList();
                    }
                    else
                    {
                        //MainStaticClass.write_event_in_log("Произошли ошибка при опросе досутности CDN серверов, код ошибки  " + list.code + ", описание ошибки " + list.description, "Документ чек", "0");
                        //MessageBox.Show("Произошли ошибка при опросе досутности CDN серверов, код ошибки  " + list.code + " , описание ошибки " + list.description);
                        MainStaticClass.write_cdn_log("Произошли ошибка при опросе досутности CDN серверов, код ошибки  " + list.code + ", описание ошибки " + list.description, "0", "", "3");
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("Произошли ошибки при опросе досутности CDN серверов " + ex.Message);
                    //MainStaticClass.write_event_in_log("Произошли ошибки при опросе досутности CDN серверов " + ex.Message, "Документ чек", "0");
                    MainStaticClass.write_cdn_log("Произошли ошибки при опросе досутности CDN серверов " + ex.Message, "0", "", "3");
                }
            }

            // Сортировка: сначала доступные серверы (dateTime <= Now), затем недоступные
            // Внутри каждой группы - по возрастанию времени отклика
            //list.hosts = list.hosts
            //    .OrderBy(h => h.dateTime > DateTime.Now ? 1 : 0) // 0 для доступных, 1 для недоступных
            //    .ThenBy(h => h.latensy) // затем по времени отклика
            //    .ToList();

            return list;
        }

        ///// <summary>
        ///// Возращает список cdn серверов
        ///// </summary>
        ///// <returns></returns>
        //public CDN_List get_cdn_list(CDN_List list)
        //{
        //    //CDN_List list = get_cdn_info();
        //    try
        //    {
        //        CDNHealth cDNHealth = null;
        //        if (list.code == 0)//ответ без ошибок 
        //        {
        //            foreach (Host host in list.hosts)
        //            {
        //                cDNHealth = cdn_health_check(host.host.ToString());
        //                if (cDNHealth.code == 0)
        //                {
        //                    host.avgTimeMs = cDNHealth.avgTimeMs;
        //                    host.dateTime = DateTime.Now;
        //                }
        //                else
        //                {
        //                    host.dateTime = DateTime.Now.AddMinutes(15);
        //                }
        //            }
        //            list.hosts = list.hosts.OrderBy(h => h.latensy).ThenByDescending(h => h.dateTime).ToList();
        //        }
        //        else
        //        {
        //            MessageBox.Show("Произошли ошибка при опросе досутности CDN серверов, код ошибки  " + list.code + " , описание ошибки " + list.description);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Произошли ошибки при опросе досутности CDN серверов " + ex.Message);
        //    }

        //    return list;
        //}

        private CDNHealth cdn_health_check(string url_sdn)
        {
            string url = url_sdn + health_url;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            long latency = 9999999999;
            CDNHealth cDNHealth = null;// new CDNHealth();            
            //cDNHealth.latency = latency;//при недоступности сразу будет максимальное время
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                // Создание запроса
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 3000;

                // Добавление заголовков            
                request.Headers.Add("X-API-KEY", MainStaticClass.CDN_Token);
                request.ContentType = "application/json";

                // Запуск таймера непосредственно перед отправкой запроса
                stopwatch.Start();
                MainStaticClass.write_event_in_log("stopwatch старт " + url_sdn, "Документ чек", "0");

                // Отправка запроса и получение ответа
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // Остановка таймера сразу после получения ответа
                    stopwatch.Stop();

                    // Вывод времени задержки в миллисекундах
                    latency = stopwatch.ElapsedMilliseconds;

                    MainStaticClass.write_event_in_log(" stopwatch стоп,получение latency " + latency.ToString() + " " + url_sdn, "Документ чек", "0");

                    int status_code = (int)response.StatusCode;
                    if (status_code != 200)
                    {
                        MainStaticClass.write_event_in_log(" Получен неверный ответ при запросе о доступности для CDN площадки" + url + ", код ответа = " + status_code.ToString(), "Документ чек", "0");
                        //MessageBox.Show("Получен неверный ответ при запросе о доступности для CDN площадки" + url + ", код ответа = " + status_code.ToString(), "Опрос статуса доступности CDN площадки");                        
                        return cDNHealth;
                    }

                    // Чтение и вывод содержимого ответа
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            MainStaticClass.write_event_in_log(" Чтение ответа о времени обработки запроса " + url_sdn, "Документ чек", "0");
                            string cdn_health = reader.ReadToEnd();
                            cDNHealth = JsonConvert.DeserializeObject<CDNHealth>(cdn_health);
                            cDNHealth.latency = latency;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainStaticClass.write_cdn_log("Проверка доступности CDN cdn_health_check " + url_sdn + "  " + ex.Message, "0", "", "3");
            }

            return cDNHealth;
        }

        public static string process_marking_code(string markingCode)
        {
            if (string.IsNullOrEmpty(markingCode))
            {
                return markingCode;
            }

            // Разделяем строку по символу \u001d
            string[] parts = markingCode.Split('\u001d');

            // Если разделителей нет или только один - возвращаем первую часть без разделителей
            if (parts.Length == 1)
            {
                return parts[0];
            }
            // Если разделителей два или больше - возвращаем содержимое до второго разделителя
            else if (parts.Length >= 2)
            {
                return parts[0] + parts[1];
            }

            return markingCode;
        }

        public bool check_lm_ch_z(Cash_check cash_Check, string mark_str)
        {
            bool result_check = false;
            AnswerCheckMark answer_check_mark = null;
            //string error_description = "";
            // Параметры
            //string server = "192.168.2.50";
            mark_str = process_marking_code(mark_str);
            string server = MainStaticClass.GetIpAddrLmChZ;// "192.168.2.50";
            int port = 5995;
            string cis = mark_str;//"0104640043469202215Y1a67"; // ваш КИ
            string username = "admin";
            string password = "admin";
            string xClientId = MainStaticClass.FiscalDriveNumber;//."8710000100123456"; // опционально
                                                                 //string xClientId = MainStaticClass.CDN_Token;// "8710000100123456"; // опционально


            // URL-кодирование КИ (RFC 3986)
            string encodedCis = Uri.EscapeDataString(cis);
            string url = $"http://{server}:{port}/api/v1/cis/check?cis={encodedCis}";

            // Создание запроса
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";

            // Basic Auth
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            request.Headers["Authorization"] = $"Basic {credentials}";

            // Опциональный заголовок
            //if (!string.IsNullOrEmpty(xClientId))
            //{
            //    request.Headers["X-ClientId"] = xClientId;
            //}

            try
            {
                // Синхронный вызов — поток заблокируется до получения ответа
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string responseBody = reader.ReadToEnd();

                    //Console.WriteLine($"Статус: {(int)response.StatusCode} {response.StatusDescription}");
                    //Console.WriteLine($"Ответ:\n{responseBody}");
                    answer_check_mark = JsonConvert.DeserializeObject<AnswerCheckMark>(responseBody);
                    if (answer_check_mark.code == 0)//!answer_check_mark.codes[0].isBlocked)//Не заблокирован к продаже 
                    {
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
                        throw new Exception(answer_check_mark.description);
                    }
                }
            }
            catch (WebException ex)
            {
                // Обработка ошибок (404, 500, таймаут и т.д.)
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        string errorText = reader.ReadToEnd();
                        MessageBox.Show($"Ошибка HTTP при проверке кода в лм чз {(int)errorResponse.StatusCode}: {errorText}");
                    }
                }
                else
                {
                    MessageBox.Show($"Сетевая ошибка при проверке кода в лм чз: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неизвестная ошибка при проверке кода в лм чз: {ex.Message}");
            }

            return result_check;
        }

        public async Task<bool> cdn_check_marker_code(List<string> codes, string mark_str, Int64 numdoc, HttpWebRequest request, string mark_str_cdn, Dictionary<string, string> d_tovar, Cash_check cash_Check, ProductData productData)
        {

            await MainStaticClass.write_cdn_log(" Начало проверки на CDN ", numdoc.ToString(), codes[0].ToString(), "0");
            StringBuilder sb = new StringBuilder();
            int error_5000 = 0;
            bool result_check = false;
            AnswerCheckMark answer_check_mark = null;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            CDN_List cdn_list = MainStaticClass.CDN_List;

            if (cdn_list == null || cdn_list.hosts.Count == 0)
            {
                await MainStaticClass.write_cdn_log("Нет доступных CDN площадок для проверки кода маркировки, пробуем еще раз check_marker_code", numdoc.ToString(), codes[0].ToString(), "2");
                cdn_list = MainStaticClass.CDN_List;
                if (cdn_list == null || cdn_list.hosts.Count == 0)
                {
                    await MainStaticClass.write_cdn_log("Список CDN серверов пустой после 2-й попытки check_marker_code ", numdoc.ToString(), codes[0].ToString(), "2");
                    return result_check;
                }
            }



            string url = "";
            bool error = false;

            CheckMark check_mark = new CheckMark();
            check_mark.codes = codes;
            check_mark.fiscalDriveNumber = MainStaticClass.FiscalDriveNumber;

            string body = JsonConvert.SerializeObject(check_mark, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            body = body.Replace("\\u001d", @"u001d");
            error = false;

            //Если в результате все в таймаутах или недоступно тогда пробуем хоть куда то выполнить запрос.
            if (cdn_list.hosts.Count == 0)
            {
                cdn_list = MainStaticClass.CDN_List;
            }

            cdn_list.hosts = cdn_list.hosts
            .OrderBy(h => h.dateTime <= DateTime.Now ? 0 : 1) // доступные (время < текущего) первыми
            .ThenBy(h => h.latensy) // затем по возрастанию latency
            .ToList();

            for (int i = 0; i < cdn_list.hosts.Count; i++)
            {
                Host host = cdn_list.hosts[i];
                url = host.host + codes_url;

                try
                {
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.KeepAlive = true;
                    request.Timeout = 1500;

                    request.Method = "POST";

                    // Добавление заголовков            
                    request.Headers.Add("X-API-KEY", MainStaticClass.CDN_Token);
                    request.ContentType = "application/json";

                    byte[] byteArray = Encoding.UTF8.GetBytes(body);

                    // Устанавливаем заголовок Content-Length
                    request.ContentLength = byteArray.Length;

                    // Пишем данные в поток запроса
                    using (var dataStream = request.GetRequestStream())
                    {
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        dataStream.Flush();
                        dataStream.Close();
                        dataStream.Dispose();
                    }

                    // Получаем ответ
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        // Открываем поток ответа
                        using (var responseStream = response.GetResponseStream())
                        {
                            // Читаем поток ответа
                            using (var reader = new StreamReader(responseStream))
                            {
                                string responseFromServer = reader.ReadToEnd();
                                //Записываем лог 
                                await MainStaticClass.write_cdn_log(responseFromServer, numdoc.ToString(), codes[0].ToString(), "1");
                                answer_check_mark = JsonConvert.DeserializeObject<AnswerCheckMark>(responseFromServer);
                            }
                        }
                    }

                    if (answer_check_mark != null)
                    {
                        error = false;
                        //StringBuilder sb = new StringBuilder();

                        string s = "ТОВАР НЕ МОЖЕТ БЫТЬ ПРОДАН!\r\n";
                        if (!answer_check_mark.codes[0].isOwner)
                        {
                            if (answer_check_mark.codes[0].groupIds != null)
                            {
                                if ((answer_check_mark.codes[0].groupIds[0] != 23) && (answer_check_mark.codes[0].groupIds[0] != 8) && (answer_check_mark.codes[0].groupIds[0] != 15))
                                //if ((answer_check_mark.codes[0].groupIds[0] != 23) && (answer_check_mark.codes[0].groupIds[0] != 8))
                                {
                                    if (!productData.RrNotControlOwner())
                                    {
                                        await MessageBox.Show(" Исключения групп маркировки  23|8|15 \r\n Текущая группа маркировки  " + answer_check_mark.codes[0].groupIds[0].ToString());
                                        //if (cash_Check.check_type.SelectedIndex == 0)
                                        //{
                                        //    //MessageBox.Show("Код маркировки " + answer_check_mark.codes[0].gtin + " Вы не являетесь владельцем " + s, "CDN проверка");
                                        sb.AppendLine("Вы не являетесь владельцем!".ToUpper());
                                        //    //MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " Вы не являетесь владельцем ", "Документ чек", numdoc.ToString());
                                        //    MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " Вы не являетесь владельцем ", numdoc.ToString(), codes[0].ToString(), "1");
                                        //}
                                    }
                                }
                            }
                            else
                            {
                                sb.AppendLine("Не удалось определить группу товара");
                            }
                        }
                        if (!answer_check_mark.codes[0].found)
                        {
                            sb.AppendLine("Не найден в ГИС МТ!".ToUpper());
                            MainStaticClass.write_event_in_log("CDN Код маркировки " + mark_str_cdn + " не найден в ГИС МТ", "Документ чек", numdoc.ToString());
                            if ((!answer_check_mark.codes[0].realizable) && (!answer_check_mark.codes[0].sold))
                            {
                                sb.AppendLine("Нет информации о вводе в оборот!".ToUpper());
                                await MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " нет информации о вводе в оборот. ", numdoc.ToString(), codes[0].ToString(), "1");
                            }
                        }
                        if (!answer_check_mark.codes[0].utilised)
                        {
                            sb.AppendLine("Эмитирован, но нет информации о его нанесении!".ToUpper());
                            await MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + " эмитирован, но нет информации о его нанесении. ", numdoc.ToString(), codes[0].ToString(), "1");
                        }
                        if (!answer_check_mark.codes[0].verified)
                        {
                            sb.AppendLine("Не пройдена криптографическая проверка!".ToUpper());
                            await MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  не пройдена криптографическая проверка.", numdoc.ToString(), codes[0].ToString(), "1");
                        }
                        if (answer_check_mark.codes[0].sold)
                        {
                            //if (cash_Check.check_type.SelectedIndex == 0)
                            //{
                            //    sb.AppendLine("Уже выведен из оборота!".ToUpper());
                            //    MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  уже выведен из оборота.", numdoc.ToString(), codes[0].ToString(), "1");
                            //}
                        }
                        if (answer_check_mark.codes[0].isBlocked)
                        {
                            sb.AppendLine("Заблокирован по решению ОГВ!".ToUpper());
                            await MainStaticClass.write_cdn_log("CDN Код маркировки " + mark_str_cdn + "  заблокирован по решению ОГВ.", numdoc.ToString(), codes[0].ToString(), "1");
                        }
                        if (answer_check_mark.codes[0].expireDate.Year > 2000)
                        {
                            if (answer_check_mark.codes[0].expireDate < DateTime.Now)
                            {
                                sb.AppendLine("Истек срок годности!".ToUpper());
                                await MainStaticClass.write_cdn_log("CDN У товара с кодом маркировки " + mark_str_cdn + "  истек срок годности.", numdoc.ToString(), codes[0].ToString(), "1");

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
                            break;//проверка успешно завершена
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
                            await MessageBox.Show(sb.ToString());
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.Timeout || ex.Status == WebExceptionStatus.ConnectFailure || ex.Status == WebExceptionStatus.ConnectionClosed)
                    {
                        // Обработка таймаута и ошибок соединения
                        error = true;
                        await MainStaticClass.write_cdn_log("Timeout or connection error: " + ex.Message + "  " + host.host.ToString(), numdoc.ToString(), codes[0].ToString(), "3");

                        if (error_timeout == 0)
                        {
                            i--; // Повторить попытку
                        }
                        error_timeout++;
                        continue; // или continue, если это в цикле
                    }

                    // Если есть HTTP-ответ — обрабатываем его
                    if (ex.Response is HttpWebResponse errorResponse)
                    {
                        await MainStaticClass.write_cdn_log("check_marker_code " + host.host + " " + ex.Message, numdoc.ToString(), codes[0].ToString(), "3");

                        using (var errorStream = errorResponse.GetResponseStream())
                        using (var reader = new StreamReader(errorStream))
                        {
                            string errorResponseText = reader.ReadToEnd();
                            await MainStaticClass.write_cdn_log($"HTTP Error {(int)errorResponse.StatusCode}: {errorResponseText}", numdoc.ToString(), codes[0].ToString(), "2");

                            answer_check_mark = JsonConvert.DeserializeObject<AnswerCheckMark>(errorResponseText);
                            if (answer_check_mark != null)
                            {
                                if (answer_check_mark.code == 5000)
                                {
                                    error_5000++;
                                }
                                else
                                {
                                    await MessageBox.Show("При проверке кода маркировки на сервере CDN произошла ошибка \r\n Код  " + answer_check_mark.code + " \r\n Описание " + answer_check_mark.description);
                                }
                            }
                        }

                        // Другие HTTP ошибки
                        error = true;
                        if (error_5000 == 0)
                        {
                            await MessageBox.Show("WebException check_marker_code " + host.host + " " + ex.Message, "check_marker_code");
                            //host.dateTime = DateTime.Now.AddMinutes(15);
                        }
                        else if (error_5000 > 1)
                        {
                            error = false;
                            result_check = true;
                        }

                        //MainStaticClass.write_cdn_log("WebException check_marker_code " + host.host + " " + ex.Message, numdoc.ToString(), codes[0].ToString(), "3");
                        //MainStaticClass.UpdateHostDateTimeCdnHost(host.host, DateTime.Now.AddMinutes(15));
                    }
                    else
                    {
                        // Другие WebException без Response
                        await MessageBox.Show("WebException without response: " + ex.Message);
                        //MainStaticClass.write_cdn_log("WebException without response: " + ex.Message, numdoc.ToString(), codes[0].ToString(), "3");
                        error = true;
                    }
                    await MainStaticClass.write_cdn_log("WebException check_marker_code " + host.host + " " + ex.Message, numdoc.ToString(), codes[0].ToString(), "3");
                    //MainStaticClass.UpdateHostDateTimeCdnHost(host.host, DateTime.Now.AddMinutes(15));
                }
                catch (Exception ex)
                {
                    await MessageBox.Show("Exception check_marker_code " + host.host + " " + ex.Message, "check_marker_code");
                    await MainStaticClass.write_cdn_log("check_marker_code " + host.host + " " + ex.Message, numdoc.ToString(), codes[0].ToString(), "3");
                    //MainStaticClass.UpdateHostDateTimeCdnHost(host.host, DateTime.Now.AddMinutes(15));
                    error = true;
                }
                if (!error)
                {
                    break;
                }
            }

            if ((!result_check) && (sb.Length == 0))//не удалось проверить и все попытки проверить завершились ошибкой
            {
                if (MainStaticClass.GetIpAddrLmChZ != "")//ip адрес сервера лм чз не пустой, значит он установлен
                {
                    result_check = check_lm_ch_z(cash_Check, mark_str);
                }
            }


            return result_check;
        }
    }
}
