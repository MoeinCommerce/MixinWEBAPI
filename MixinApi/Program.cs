using MixinApi.Contexts;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using WebApi.Contexts.Interfaces;
using WebApi.Models;

namespace MixinApi
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // This is a simple console application that serves as an entry point for the MixinApi project.
            // You can add your code here to interact with the Mixin API or perform other tasks.

            Console.WriteLine("Welcome to the Mixin API Console Application!");
            
            string url = "https://eskortyadak.ir";
            var connectionConfig = new Dictionary<string, string>
            {
                { "MixinApiKey", "AKzJn28GUSxmxUIwHl_iNksc7D9_eECgi20fofSqaTAqgZMZY-GUuslqNeJ0JVMN"}
            };
            IWebContext webContext = new WebContext(url, connectionConfig);
            
            var products = webContext.GetAllProducts();
            const string path = "api/management/v1";
            RestClient _client;

            var options = new RestClientOptions(new Uri(new Uri(url), path));

            _client = new RestClient(options);
            _client.AddDefaultHeader("Accept", "application/json");
            _client.AddDefaultHeader("Authorization", $"Api-Key AKzJn28GUSxmxUIwHl_iNksc7D9_eECgi20fofSqaTAqgZMZY-GUuslqNeJ0JVMN");
            foreach (var product in products)
            {
                var category = new
                {
                    main_category = 43
                };
                var request = new RestRequest($"products/{product.Id}/", Method.Patch);
                request.AddJsonBody(category);
                var result = _client.Put(request);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
