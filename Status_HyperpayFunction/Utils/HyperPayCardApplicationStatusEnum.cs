using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Status_HyperpayFunction.Utils
{
    public enum HyperPayCardApplicationStatusEnum
    {
        OpeningPreApply = 0,
        OpeningPendingForpayment = 1,
        OpeningReviewing = 2,
        OpeningReviewedSuccess = 3,
        OpeningReviewedRejected = 4,
        OpeningRefunded = 5,
        OpeningShipped = 6,
        OpeningActivating = 7,
        OpeningActivationFails = 8,
        OpeningActivated = 9,
        ActivationReviewing = 11,
        ActivationReviewedRejected = 12,
        ActivationReviewedSuccess = 13,
        CancellingCard = 14,
        CancelledCard = 15,
        RefundReviewing = 21,
        RefundReviewedRejected = 22,
        RefundReviewedSuccess = 23,
    }
}
