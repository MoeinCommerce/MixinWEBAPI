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

        public static (int productId, int? variantId) Parse(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));

            // Extract productId
            var match = ProductRegex.Match(url);
            if (!match.Success)
                throw new InvalidOperationException("Invalid product URL format.");

            int productId = int.Parse(match.Groups["productId"].Value);

            // Extract variantId if exists (?vid=)
            int? variantId = null;
            var queryIndex = url.IndexOf("vid=", StringComparison.OrdinalIgnoreCase);
            if (queryIndex >= 0)
            {
                var queryPart = url.Substring(queryIndex + 4); // skip "vid="
                if (int.TryParse(queryPart.Split('&')[0], out int vid))
                    variantId = vid;
            }

            return (productId, variantId);
        }
    }
}
