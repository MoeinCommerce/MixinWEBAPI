using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixinApi.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class MxProduct
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("english_name")]
        public string EnglishName { get; set; }

        [JsonProperty("main_category")]
        public int? MainCategory { get; set; }

        [JsonProperty("price")]
        public int? Price { get; set; }

        [JsonProperty("compare_at_price")]
        public int? CompareAtPrice { get; set; }

        [JsonProperty("stock_type")]
        public string StockType { get; set; }

        [JsonProperty("stock")]
        public double Stock { get; set; }

        [JsonProperty("has_variants")]
        public bool HasVariants { get; set; }

        [JsonProperty("available")]
        public bool Available { get; set; }

        [JsonProperty("images")]
        public List<object> Images { get; set; }

        [JsonProperty("variants")]
        public List<MxVariant> Variants { get; set; }
    }

    public class MxVariant
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("attributes")]
        public List<MxAttribute> Attributes { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public int? Price { get; set; }

        [JsonProperty("compare_at_price")]
        public int? CompareAtPrice { get; set; }

        [JsonProperty("show_price")]
        public bool ShowPrice { get; set; }

        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }

        [JsonProperty("stock")]
        public int Stock { get; set; }

        [JsonProperty("length")]
        public double? Length { get; set; }

        [JsonProperty("width")]
        public double? Width { get; set; }
    }
    public class MxAttribute
    {
        [JsonProperty("id")]
        public int Id { get; set; }


        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
