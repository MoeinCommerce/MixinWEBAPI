using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MixinApi.Utilities
{
    public class QueryFilterBuilder
    {
        private readonly Dictionary<string, string> _queryParameters;

        public QueryFilterBuilder()
        {
            _queryParameters = new Dictionary<string, string>();
        }

        /// <summary>
        /// Adds or updates a query parameter.
        /// </summary>
        /// <param name="key">The query parameter key.</param>
        /// <param name="value">The query parameter value.</param>
        /// <returns>The current QueryFilterBuilder instance.</returns>
        public QueryFilterBuilder AddOrUpdateParameter(string key, string value)
        {
            if (_queryParameters.ContainsKey(key))
            {
                _queryParameters[key] = value; // Update existing parameter
            }
            else
            {
                _queryParameters.Add(key, value); // Add new parameter
            }

            return this; // Enables chaining
        }

        /// <summary>
        /// Removes a query parameter if it exists.
        /// </summary>
        /// <param name="key">The query parameter key to remove.</param>
        /// <returns>The current QueryFilterBuilder instance.</returns>
        public QueryFilterBuilder RemoveParameter(string key)
        {
            if (_queryParameters.ContainsKey(key))
            {
                _queryParameters.Remove(key);
            }

            return this; // Enables chaining
        }

        /// <summary>
        /// Generates the full URL with the applied query parameters.
        /// </summary>
        /// <param name="baseUrl">The base URL (without query string).</param>
        /// <returns>The complete URL with query parameters.</returns>
        public string Build(string baseUrl)
        {
            if (!_queryParameters.Any()) return baseUrl; // Return the base URL if no query parameters exist
            var queryString = string.Join("&", _queryParameters
                .Where(param => !string.IsNullOrWhiteSpace(param.Value)) // Filter out null or empty values
                .Select(param => $"{HttpUtility.UrlEncode(param.Key)}={HttpUtility.UrlEncode(param.Value)}"));

            if (string.IsNullOrWhiteSpace(queryString)) return baseUrl; // Return the base URL if no valid query parameters exist
            return $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// Clears all the query parameters.
        /// </summary>
        public void ClearParameters()
        {
            _queryParameters.Clear();
        }
    }
}
