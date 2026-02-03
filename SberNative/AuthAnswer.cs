using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct AuthAnswer
    {
        public TransactionType TransactionType;
        public int Amount;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
        public string ResultCode;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Message;

        public int CardType;
        public IntPtr Cheque;
    }
}
