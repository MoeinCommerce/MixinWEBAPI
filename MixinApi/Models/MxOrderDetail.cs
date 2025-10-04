using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixinApi.Models
{
    public class MxOrderDetail
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("creation_date")]
        public DateTime CreationDate { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonProperty("customer_note")]
        public string CustomerNote { get; set; }

        [JsonProperty("payment_method")]
        public string PaymentMethod { get; set; }

        [JsonProperty("payment_status")]
        public string PaymentStatus { get; set; }

        [JsonProperty("shipping_method_name")]
        public string ShippingMethodName { get; set; }

        [JsonProperty("shipping_date")]
        public DateTime? ShippingDate { get; set; }

        [JsonProperty("shipping_start_time")]
        public string ShippingStartTime { get; set; }

        [JsonProperty("shipping_end_time")]
        public string ShippingEndTime { get; set; }

        [JsonProperty("shipping_province")]
        public string ShippingProvince { get; set; }

        [JsonProperty("shipping_city")]
        public string ShippingCity { get; set; }

        [JsonProperty("shipping_address")]
        public string ShippingAddress { get; set; }

        [JsonProperty("shipping_zip_code")]
        public string ShippingZipCode { get; set; }

        [JsonProperty("shipping_first_name")]
        public string ShippingFirstName { get; set; }

        [JsonProperty("shipping_last_name")]
        public string ShippingLastName { get; set; }

        [JsonProperty("shipping_phone_number")]
        public string ShippingPhoneNumber { get; set; }

        [JsonProperty("shipping_tracking_code")]
        public string ShippingTrackingCode { get; set; }

        [JsonProperty("coupon_discount_amount")]
        public double? CouponDiscountAmount { get; set; }

        [JsonProperty("discount_amount")]
        public double? DiscountAmount { get; set; }

        [JsonProperty("cart_price")]
        public double? CartPrice { get; set; }

        [JsonProperty("tax_amount")]
        public double? TaxAmount { get; set; }

        [JsonProperty("shipping_price")]
        public double? ShippingPrice { get; set; }

        [JsonProperty("final_price")]
        public double? FinalPrice { get; set; }

        [JsonProperty("items")]
        public List<MxOrderItem> Items { get; set; }

        [JsonProperty("events")]
        public List<MxOrderEvent> Events { get; set; }
    }
}
