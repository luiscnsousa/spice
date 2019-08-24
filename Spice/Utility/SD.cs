namespace Spice.Utility
{
    using Spice.Models;
    using System;

    public class SD
    {
        public const string DefaultFoodImage = "default_food.png";

        public const string ManagerUser = "Manager";
        public const string KitchenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerEndUser = "Customer";

        public const string ssShoppingCartCount = "ssCartCount";
        public const string ssCouponCode = "ssCouponCode";

        public const string StatusSubmitted = "Submitted";
        public const string StatusInProcess = "Being Prepared";
        public const string StatusReady = "Ready for Pickup";
        public const string StatusCompleted = "Completed";
        public const string StatusCancelled = "Cancelled";

        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusRejected = "Rejected";

        public static string ConvertToRawHtml(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            var array = new char[source.Length];
            var arrayIndex = 0;
            var inside = false;

            foreach (var let in source)
            {
                if (let == '<')
                {
                    inside = true;
                    continue;
                }

                if (let == '>')
                {
                    inside = false;
                    continue;
                }

                if (inside)
                {
                    continue;
                }

                array[arrayIndex] = let;
                arrayIndex++;
            }

            return new string(array, 0, arrayIndex);
        }

        public static double DiscountedPrice(Coupon couponFromDb, double originalOrderTotal)
        {
            if (couponFromDb == null)
            {
                return originalOrderTotal;
            }

            if (couponFromDb.MinimumAmount > originalOrderTotal)
            {
                return originalOrderTotal;
            }

            switch (Convert.ToInt32(couponFromDb.CouponType))
            {
                //everything is valid
                case (int)Coupon.ECouponType.Dollar:
                    //$10 off $100
                    return Math.Round(originalOrderTotal - couponFromDb.Discount, 2);
                case (int)Coupon.ECouponType.Percent:
                    //10% off $100
                    return Math.Round(originalOrderTotal - (originalOrderTotal * couponFromDb.Discount / 100), 2);
                default:
                    return originalOrderTotal;
            }
        }
    }
}
