using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class AppStoreOffer
    {
        public long end_time { get; set; } = 0;
        public string id { get; set; } = "";
        public bool show_timer { get; set; } = false;
        public AppStoreOfferPrice price { get; set; } = new AppStoreOfferPrice();
        public string promo_benefit { get; set; } = null;
        public AppStoreOfferPrice strikethrough_price { get; set; } = new AppStoreOfferPrice();
    }
    public class AppStoreTrialOffer
    {
        public List<string> descriptions { get; set; } = new List<string>();
        public long trial_start_time { get; set; } = 0;
        public long trial_end_time { get; set; } = 0;
        public string id { get; set; } = "";
        public bool show_timer { get; set; } = false;
        public AppStoreOfferPrice price { get; set; } = new AppStoreOfferPrice();
        public string promo_benefit { get; set; } = null;
        public AppStoreOfferPrice strikethrough_price { get; set; } = new AppStoreOfferPrice();
    }

    public class AppStoreOfferPrice
    {
        public string offset_amount { get; set; } = "0";

        public int offset_amount_numerical
        {
            get
            {
                return Convert.ToInt32(offset_amount);
            }
            set
            {
                offset_amount = value.ToString();
            }
        }

        public string currency { get; set; } = "USD";

        public string formatted
        {
            get
            {
                return GetFormattedPrice(offset_amount_numerical, currency);
            }
        }

        public static string GetFormattedPrice(long price, string currency)
        {
            long cents = price % 100;
            long dollars = (price - cents) / 100;
            string formattedPrice = dollars + "." + cents.ToString("00");
            switch (currency)
            {
                case "AUD":
                    return "A$" + formattedPrice;
                case "USD":
                    return "$" + formattedPrice;
                case "EUR":
                    return "€" + formattedPrice;
            }

            return formattedPrice;
        }
        
        public static string GetFormattedPrice(int price, string currency)
        {
            return GetFormattedPrice(Convert.ToInt64(price), currency);
        }
    }
}
