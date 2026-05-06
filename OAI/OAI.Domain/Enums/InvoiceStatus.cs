using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 0,
    PendingReview = 1,
    Approved = 2,
    Rejected = 3,
    Exported = 4
}
