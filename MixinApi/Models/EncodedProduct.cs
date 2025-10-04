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
        public long EncodedId => Encode(Product.Id, 0);

        // Variants encoded as (product.Id, variant.Id)
        public IEnumerable<(MxVariant Variant, long EncodedId)> EncodedVariants =>
            Product.Variants?.Select(v => (v, Encode(Product.Id, v.Id))) ?? Enumerable.Empty<(MxVariant, long)>();

        // Static encode/decode utilities
        public static long Encode(long variableId, long variationId)
        {
            return ((variableId + variationId) * (variableId + variationId + 1)) / 2 + variationId;
        }

        public static (long variableId, long variationId) Decode(long z)
        {
            long w = (long)Math.Floor((Math.Sqrt(8.0 * z + 1) - 1) / 2);
            long t = (w * (w + 1)) / 2;
            long variationId = z - t;
            long variableId = w - variationId;
            return (variableId, variationId);
        }
    }

}
