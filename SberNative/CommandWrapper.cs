using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public static class CommandWrapper
    {
        private const int SUCCESS = 0;
        private const int SPASIBO = 4353;
        private const int BUSY = 115;
        private const int NOPINPAD = 99;
        private const int NOLINK = 4119;
        private const int USER_CANCEL = 2000;
        private const int TIMEOUT_CANCEL = 2002;
        // Строка для работы функционала "Возврат без карты", будет работать при соответствующей настройке терминала
        private static readonly string RequestCancelWithoutCard = "QSELECT";
        public static string return_slip = "";

        //    public static int TestPinPad()
        //    {
        //        int result = Pilot.TestPinpad();
        //        return result;
        //    }

        //    public static string CloseDay()
        //    {
        //        AuthAnswer authAnswer = new AuthAnswer { TransactionType = TransactionType.CancelAuthorization };
        //        CheckError(() => Pilot.CloseDay(ref authAnswer));
        //        return GlobalFree(authAnswer);
        //    }

        //    public static int IsConnected()
        //    {
        //        StringBuilder outputTerminalID = new StringBuilder();
        //        int result = Pilot.GetTerminalID(outputTerminalID);
        //        return result;
        //    }

        //    private static void CheckError(Func<int> action)
        //    {
        //        var errorCode = action();
        //        if (IsSpasiboFirstCall((int)errorCode))
        //            return;
        //        if (errorCode == USER_CANCEL)
        //        {
        //            throw new Exception("Клиент отказался от выполнения операции");
        //        }

        //        if (errorCode == TIMEOUT_CANCEL)
        //        {
        //            throw new Exception("Истек период ожидания реакции клиента");
        //        }

        //        if (errorCode != SUCCESS)
        //        {
        //            throw new Exception($"В методе произошла ошибка при обращении к сервисам сбербанка с кодом {errorCode}");
        //        }
        //    }

        //    public static bool IsSpasiboFirstCall(int error)
        //    {
        //        return error == SPASIBO;
        //    }

        //    /// Процедура авторизации. Если после вызвать RollBack то процедура будет отменена. 
        //    public static AuthAnswer13 Authorization(int amount)
        //    {
        //        var authAnswer = new AuthAnswer13
        //        {
        //            ans = new AuthAnswer
        //            {
        //                TransactionType = TransactionType.Purchase,
        //                Amount = amount,
        //            }
        //        };

        //        authAnswer = Authorization(authAnswer, null);
        //        return authAnswer;
        //    }

        //    public static AuthAnswer13 AuthorizationWithHash(int amount, string hash)
        //    {
        //        AuthAnswer13 authAnswer = new AuthAnswer13
        //        {
        //            ans = new AuthAnswer
        //            {
        //                TransactionType = TransactionType.Purchase,
        //                Amount = amount,
        //            },

        //            Hash = hash
        //        };

        //        authAnswer = Authorization(authAnswer, null);
        //        return authAnswer;
        //    }

        //    public static AuthAnswer13 ReturnAmountToCard(int amount, string RRN)
        //    {
        //        AuthAnswer13 authAnswer = new AuthAnswer13
        //        {
        //            ans = new AuthAnswer
        //            {
        //                TransactionType = TransactionType.Refund,
        //                Amount = amount,
        //            },

        //            RRN = RRN
        //        };

        //        authAnswer = Authorization(authAnswer, RequestCancelWithoutCard);
        //        return authAnswer;
        //    }

        //    static AuthAnswer13 Authorization(AuthAnswer13 answer, string track2)
        //    {
        //        CheckError(() => Pilot.CardAuthorize(track2, ref answer));
        //        var slip = GlobalFree(answer.ans);
        //        Trace.WriteLine(slip);
        //        return_slip = Regex.Replace(slip, @"~S\u0001", "").Trim();
        //        return answer;
        //    }

        //    static string GlobalFree(AuthAnswer answer)
        //    {
        //        string checkStr = Marshal.PtrToStringAnsi(answer.Cheque);
        //        if (checkStr != null)
        //            Pilot.GlobalFree(answer.Cheque);

        //        return checkStr;
        //    }

        //    public static void SuspendTransaction(int amount)
        //    {
        //        CheckError(() => Pilot.SuspendTransaction(amount, null));
        //    }

        //    public static void CommitTransaction(int amount)
        //    {
        //        CheckError(() => Pilot.CommitTransaction(amount, null));
        //    }

        //    public static void RollBackTransaction(int amount)
        //    {
        //        CheckError(() => Pilot.RollBackTransaction(amount, null));
        //    }

        //    public static string GetFullReport()
        //    {
        //        var authAnswer = new AuthAnswer
        //        {
        //            TransactionType = TransactionType.None,
        //        };
        //        var res = Pilot.GetStatistics(ref authAnswer);
        //        string checkStr = Marshal.PtrToStringAnsi(authAnswer.Cheque);
        //        return checkStr;
        //        //Trace.WriteLine(checkStr);
        //        //TestPinPad();
        //    }
    }
}
