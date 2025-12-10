using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixinApi.Models
{
    public class EncodedProduct
    {
        public MxProduct Product { get; }

        public EncodedProduct(MxProduct product)
        {
            Product = product;
        }

        // Encode product ID as (product.Id, 0)
        public string EncodedId => Encode(Product.Id, "0");

        // Variants encoded as (product.Id, variant.Id)
        public IEnumerable<(MxVariant Variant, string EncodedId)> EncodedVariants =>
            Product.Variants?.Select(v => (v, Encode(Product.Id, v.Id))) ?? Enumerable.Empty<(MxVariant, string)>();

        // Static encode/decode utilities
        public static string Encode(string variableIdStr, string variationIdStr)
        {
            long variableId = long.Parse(variableIdStr);
            long variationId = long.Parse(variationIdStr);

            long encodedValue = ((variableId + variationId) * (variableId + variationId + 1)) / 2 + variationId;
            return encodedValue.ToString();
        }

        public static (string variableId, string variationId) Decode(string zStr)
        {

            long z = long.Parse(zStr);

            long w = (long)Math.Floor((Math.Sqrt(8.0 * z + 1) - 1) / 2);
            long t = (w * (w + 1)) / 2;
            long variationId = z - t;
            long variableId = w - variationId;
            
            string variableIdStr = variationId.ToString();
            string variationIdStr = variableId.ToString();

            return (variableIdStr, variationIdStr);
        }
    }

}
