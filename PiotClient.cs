//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;

//namespace PiotIntegration
//{
//    #region Модели данных

//    public class PiotInfo
//    {
//        [JsonProperty("tspiotId")]
//        public string tspiotId { get; set; }

//        [JsonProperty("kktSerial")]
//        public string kktSerial { get; set; }

//        [JsonProperty("fnSerial")]
//        public string fnSerial { get; set; }

//        [JsonProperty("kktInn")]
//        public string kktInn { get; set; }

//        [JsonProperty("codesCheckTimeout")]
//        public int codesCheckTimeout { get; set; }
//    }

//    public class ApiResponse
//    {
//        [JsonProperty("code")]
//        public int? errorCode { get; set; }

//        [JsonProperty("message")]
//        public string errorMessage { get; set; }

//        [JsonProperty("codesResponse")]
//        public CodesResponseWrapper codesResponse { get; set; }
//    }

//    public class CodesResponseWrapper
//    {
//        [JsonProperty("codesResponse")]
//        public List<ResponseItem> codesResponse { get; set; }
//    }

//    public class ResponseItem
//    {
//        [JsonProperty("code")]
//        public int code { get; set; }

//        [JsonProperty("description")]
//        public string description { get; set; }

//        [JsonProperty("codes")]
//        public List<CodeDetail> codes { get; set; }

//        [JsonProperty("reqId")]
//        public string reqId { get; set; }

//        [JsonProperty("reqTimestamp")]
//        public long reqTimestamp { get; set; }

//        [JsonProperty("isCheckedOffline")]
//        public bool isCheckedOffline { get; set; }

//        [JsonProperty("version")]
//        public string version { get; set; }

//        [JsonProperty("inst")]
//        public string inst { get; set; }
//    }

//    public class CodeDetail
//    {
//        [JsonProperty("cis")]
//        public string cis { get; set; }

//        [JsonProperty("found")]
//        public bool found { get; set; }

//        [JsonProperty("valid")]
//        public bool valid { get; set; }

//        [JsonProperty("printView")]
//        public string printView { get; set; }

//        [JsonProperty("gtin")]
//        public string gtin { get; set; }

//        [JsonProperty("groupIds")]
//        public List<int> groupIds { get; set; }

//        [JsonProperty("verified")]
//        public bool verified { get; set; }

//        [JsonProperty("realizable")]
//        public bool realizable { get; set; }

//        [JsonProperty("utilised")]
//        public bool utilised { get; set; }

//        [JsonProperty("productionDate")]
//        public DateTime? productionDate { get; set; }

//        [JsonProperty("isOwner")]
//        public bool isOwner { get; set; }

//        [JsonProperty("isBlocked")]
//        public bool isBlocked { get; set; }

//        [JsonProperty("ogvs")]
//        public List<object> ogvs { get; set; }

//        [JsonProperty("errorCode")]
//        public int errorCode { get; set; }

//        [JsonProperty("message")]
//        public string message { get; set; }

//        [JsonProperty("isTracking")]
//        public bool isTracking { get; set; }

//        [JsonProperty("sold")]
//        public bool sold { get; set; }

//        [JsonProperty("mrp")]
//        public int? mrp { get; set; }

//        [JsonProperty("grayZone")]
//        public bool grayZone { get; set; }

//        [JsonProperty("packageType")]
//        public string packageType { get; set; }

//        [JsonProperty("producerInn")]
//        public string producerInn { get; set; }

//        [JsonProperty("expireDate")]
//        public DateTime expireDate { get; set; }
//    }

//    public class ClientInfo
//    {
//        public string name { get; set; }
//        public string version { get; set; }

//        // СЮДА БУДЕМ ПЕРЕДАВАТЬ КОНТРОЛЬНУЮ СУММУ ИЛИ ТОКЕН
//        public string id { get; set; }
//        public string token { get; set; }
//    }

//    public class ClientData
//    {
//        public List<string> codes { get; set; }
//        public ClientInfo client_info { get; set; }
//    }

//    #endregion

//    /// <summary>
//    /// Результат запроса к ПИОТ. Содержит флаг успеха и сырые данные JSON.
//    /// </summary>
//    public class PiotRequestResult
//    {
//        public bool Success { get; set; }
//        public string JsonData { get; set; }
//        public string ErrorMessage { get; set; }
//    }

//    /// <summary>
//    /// Клиент для работы с ПИОТ. Только сетевое взаимодействие.
//    /// </summary>
//    public class PiotClient
//    {
//        private readonly string _baseUrl;

//        public PiotClient(string piotUrl)
//        {
//            _baseUrl = piotUrl.TrimEnd('/');

//            // Глобальные настройки сети для ПИОТ (достаточно вызвать один раз)
//            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
//            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;
//            ServicePointManager.CheckCertificateRevocationList = false;
//        }

//        /// <summary>
//        /// Получает информацию о ПИОТ
//        /// </summary>
//        public async Task<PiotRequestResult> GetInfoAsync()
//        {
//            string url = _baseUrl + "/info";
//            try
//            {
//                string jsonResponse = await PostJsonAsync(url, "{}");
//                return new PiotRequestResult { Success = true, JsonData = jsonResponse };
//            }
//            catch (Exception ex)
//            {
//                return new PiotRequestResult { Success = false, ErrorMessage = ex.Message };
//            }
//        }

//        /// <summary>
//        /// Отправляет код маркировки на проверку
//        /// </summary>
//        /// <param name="markingCode">Код маркировки (исходная строка)</param>
//        /// <param name="clientInfo">Информация о клиенте (включая id с контрольной суммой)</param>
//        public async Task<PiotRequestResult> CheckCodeAsync(string markingCode, ClientInfo clientInfo)
//        {
//            string url = _baseUrl + "/codes/check";
//            try
//            {
//                // Подготовка кода (как было в оригинале)
//                string preparedCode = markingCode.Replace("\\u001d", @"u001d");
//                byte[] textAsBytes = Encoding.Default.GetBytes(preparedCode); // Внимание: Encoding.Default зависит от ОС
//                string imc = Convert.ToBase64String(textAsBytes);

//                var clientData = new ClientData
//                {
//                    codes = new List<string> { imc },
//                    client_info = clientInfo
//                };

//                var jsonSettings = new JsonSerializerSettings
//                {
//                    StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
//                    NullValueHandling = NullValueHandling.Ignore
//                };

//                string jsonData = JsonConvert.SerializeObject(clientData, jsonSettings);

//                string jsonResponse = await PostJsonAsync(url, jsonData);
//                return new PiotRequestResult { Success = true, JsonData = jsonResponse };
//            }
//            catch (Exception ex)
//            {
//                return new PiotRequestResult { Success = false, ErrorMessage = ex.Message };
//            }
//        }

//        // Приватный метод для отправки POST запросов
//        private async Task<string> PostJsonAsync(string url, string jsonData)
//        {
//            var request = (HttpWebRequest)WebRequest.Create(url);
//            request.Timeout = 5000;
//            request.Method = "POST";
//            request.ContentType = "application/json";
//            request.Accept = "application/json";
//            request.UserAgent = "Cash8Avalon/1.0";

//            // Отправка данных
//            byte[] data = Encoding.UTF8.GetBytes(jsonData);
//            request.ContentLength = data.Length;

//            using (var stream = await request.GetRequestStreamAsync())
//            {
//                await stream.WriteAsync(data, 0, data.Length);
//            }

//            // Получение ответа
//            using (var response = (HttpWebResponse)await request.GetResponseAsync())
//            using (var responseStream = response.GetResponseStream())
//            using (var reader = new StreamReader(responseStream, Encoding.UTF8))
//            {
//                return await reader.ReadToEndAsync();
//            }
//        }
//    }
//}