using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MixinApi.Utilities
{
    public class ProductUrlParser
    {
        private static readonly Regex ProductRegex = new Regex(
            @"/product/(?<productId>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public static (string productId, string variantId) Parse(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));

            // Extract productId
            var match = ProductRegex.Match(url);
            if (!match.Success)
                throw new InvalidOperationException("Invalid product URL format.");

            string productId = match.Groups["productId"].Value;

            // Extract variantId if exists (?vid=)
            string variantId = null;
            var queryIndex = url.IndexOf("vid=", StringComparison.OrdinalIgnoreCase);
            if (queryIndex >= 0)
            {
                var queryPart = url.Substring(queryIndex + 4); // skip "vid="
                
                var tempVariantId = queryPart.Split('&')[0];
                if (!string.IsNullOrWhiteSpace(tempVariantId))
                {
                    variantId = tempVariantId;
                }
            }

            return (productId, variantId);
        }
    }
}
