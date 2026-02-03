using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    /// <summary>
    /// Класс для получения ответа по операции авторизации от сбербанка. Класс называется AuthAnswer13 так как в библиотеке pilot он назван также
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct AuthAnswer13
    {
        public AuthAnswer ans;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string AuthCode;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 25)]
        public string CardID;

        public int ErrorCode;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string TransDate;

        public int TransNumber;
        public int SberOwnCard;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        public string Hash;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 104)]
        public string Track3;

        public uint RequestID;
        public uint Department;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
        public string RRN;



        public uint CurrencyCode;
        byte CardEntryMode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string CardName;


        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string AID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string FullErrorText;
    }
}
