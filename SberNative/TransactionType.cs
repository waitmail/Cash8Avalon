using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cash8Avalon
{
    public enum TransactionType
    {
        None = 0,
        Purchase = 1,
        Refund = 3,
        CancelAuthorization = 7,
    }
}
