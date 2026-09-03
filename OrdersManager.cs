using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    /// <summary>
    /// Вся логика интернет-заказов: синхронизация с ЦС, запись в БД,
    /// проверка статусов, оплата, актуализация дат.
    /// </summary>
    public static class OrdersManager
    {



        /// <summary>
        /// При старте КП: дата всех ожидающих заказов → текущая, время 23:59:59,
        /// номер чека не меняется (ТЗ). Индекс ix_checks_header_waiting делает быстро.
        /// </summary>
        public static async Task UpdateWaitingOrdersDateAsync()
        {
            try
            {
                using (var conn = MainStaticClass.NpgsqlConn())
                {
                    await conn.OpenAsync();
                    string query = @"UPDATE checks_header
                             SET date_time_write = CURRENT_DATE + TIME '23:59:59'
                             WHERE order_state = 1";
                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        int rows = await command.ExecuteNonQueryAsync();
                        if (rows > 0)
                            Console.WriteLine($"✓ Актуализирована дата {rows} ожидающих заказов");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка актуализации дат ожидающих заказов: {ex.Message}");
                MainStaticClass.WriteRecordErrorLog(ex, 0, MainStaticClass.CashDeskNumber, "UpdateWaitingOrdersDateAsync");
            }
        }       
    }
}