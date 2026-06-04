
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
                    "Информация ПИОТ", MessageBoxButton.OK, MessageBoxType.Info, MainStaticClass.MainWindow);
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
                            //await MessageBox.Show("Онлайн проверка кода маркировки", "Онлайн", MessageBoxButton.OK, MessageBoxType.Info, cash_Check);
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
                            //await MessageBox.Show("Офлайн проверка кода маркировки заблокирован = " + answer_check_mark.codes[0].isBlocked.ToString(), "Офлайн", MessageBoxButton.OK, MessageBoxType.Info, cash_Check);

                            // Исправлено: используем result.JsonData вместо старого response.Data
                            //MessageBox.Show(result.JsonData, "Ответ", cash_Check);

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
