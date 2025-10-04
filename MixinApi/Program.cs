using MixinApi.Contexts;
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
            
            string url = "https://moeincommeretest.mixin.website";
            var connectionConfig = new Dictionary<string, string>
            {
                { "MixinApiKey", "uczKWlUvC2aFS4ye33alCivIlqpkAlQGYdwb-_CO1mefU9O-pGcVVgAEF5h1ax__"}
            };
            IWebContext webContext = new WebContext(url, connectionConfig);

            // product listing example
            //var products = webContext.SearchProducts("Product", ProductTypes.Variable, 1, 1);
            //foreach (var product in products)
            //{
            //    Console.WriteLine($"(ID: {product.Id})");
            //}

            // product creating example

            int randomId = new Random().Next(1000, 9999);
            var sampleProduct = new WebProduct
            {
                Name = $"Created by Mixin API {randomId}",
                RegularPrice = 20000,
                SalePrice = 10000,
                StockQuantity = 50,
                Categories = new List<WebCategory> {
                    new WebCategory
                    {
                        Id = 3,
                    }
                },
            };
            webContext.CreateProduct(sampleProduct);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
