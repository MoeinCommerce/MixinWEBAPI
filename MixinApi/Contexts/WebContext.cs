using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebApi.Contexts;
using WebApi.Contexts.Interfaces;
using WebApi.Exceptions;
using WebApi.Models;
using MixinApi.Models;
using MixinApi.Utilities;
using RestSharp.Authenticators;

namespace MixinApi.Contexts
{
    public class WebContext : BaseWebContext, IWebContext
    {
        private readonly RestClient _client;
        private int _currentPage = 1;
        private int _pageSize = 100;

        public WebContext(string url, Dictionary<string, string> configs)
            : base(url, configs)
        {
            try
            {
                var apiKey = configs["MixinApiKey"];
                const string path = "api/management/v1";

                var options = new RestClientOptions(new Uri(new Uri(url), path));

                _client = new RestClient(options);
                _client.AddDefaultHeader("Accept", "application/json");
                _client.AddDefaultHeader("Authorization", $"Api-Key {apiKey}");
            }
            catch (Exception ex)
            {
                throw new WebInvalidFieldException(ex.Source, ex.Message);
            }
        }


        private Task<T> SendRequest<T>(RestRequest request, object body = null, List<ExcludedFields> excludedFields = null)
        {
            try
            {
                if (body != null)
                {
                    var jsonBody = JsonConvert.SerializeObject(body);

                    if (excludedFields != null && excludedFields.Count > 0)
                    {
                        var jsonObject = JObject.Parse(jsonBody);

                        foreach (var excludedField in excludedFields)
                        {
                            var fieldNames = GetFieldNamesFromEnum(excludedField);
                            foreach (var fieldName in fieldNames)
                            {
                                if (jsonObject.ContainsKey(fieldName))
                                {
                                    jsonObject.Remove(fieldName);
                                }
                            }
                        }

                        jsonBody = jsonObject.ToString();
                    }

                    request.AddJsonBody(jsonBody);
                }

                var response = _client.Execute(request);

                var decodedContent = response.RawBytes != null
                    ? Encoding.UTF8.GetString(response.RawBytes)
                    : response.Content ?? string.Empty;

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        return Task.FromResult(JsonConvert.DeserializeObject<T>(decodedContent));

                    case HttpStatusCode.NotFound:
                        throw new WebDoesNotExistException();

                    case HttpStatusCode.BadRequest:
                        if (response.Content != null)
                        {
                            if (response.Content.Contains("product_invalid_id"))
                            {
                                throw new WebDoesNotExistException();
                            }
                            if (response.Content.Contains("product_invalid_sku"))
                            {
                                throw new WebInvalidFieldException(WebExceptionFields.InvalidSku, response.Content);
                            }
                            if (response.Content.Contains("stock_quantity"))
                            {
                                throw new WebInvalidFieldException(WebExceptionFields.InvalidQuantity, response.Content);
                            }
                            if (response.Content.Contains("term_exists"))
                            {
                                throw new WebInvalidFieldException(WebExceptionFields.DuplicateCategoryName, response.Content);
                            }
                        }
                        throw new WebInvalidFieldException("BadRequest", response.Content);

                    case HttpStatusCode.Unauthorized:
                        throw new WebAuthenticationException();

                    case HttpStatusCode.Forbidden:
                        throw new WebInvalidFieldException("Forbidden", response.Content);

                    case HttpStatusCode.InternalServerError:
                        if (response.Content != null)
                        {
                            if (response.Content.Contains("duplicate_term_slug"))
                            {
                                throw new WebInvalidFieldException(WebExceptionFields.DuplicateCategoryName, response.Content);
                            }
                            if (response.Content.Contains("missing_parent"))
                            {
                                throw new WebInvalidFieldException(WebExceptionFields.MissingParentCategoryId, response.Content);
                            }
                        }
                        throw new InternalServerErrorException();

                    case 0:
                        throw new NetworkError();

                    default:
                        throw new WebInvalidFieldException($"Error! Status code: {response.StatusCode}", response.Content);
                }
            }
            catch (WebInvalidFieldException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP Request error: {ex.Message}");
                throw;
            }
        }

        private List<string> GetFieldNamesFromEnum(ExcludedFields field)
        {
            switch (field)
            {
                case ExcludedFields.ProductPrice:
                    return new List<string> { "price", "compare_at_price" };

                case ExcludedFields.ProductDiscount:
                    return new List<string> { "price" };

                case ExcludedFields.ProductName:
                    return new List<string> { "name", "english_name" };

                case ExcludedFields.Sku:
                    return new List<string> { "product_identifier", "parent" };

                case ExcludedFields.Stock:
                    return new List<string> { "stock", "stock_type" };

                case ExcludedFields.ProductAttributes:
                    return new List<string> { "attributes" };

                case ExcludedFields.ProductDescription:
                    return new List<string> { "description", "analysis" };

                case ExcludedFields.CategoryOfProduct:
                    return new List<string> { "main_category", "other_categories" };

                case ExcludedFields.DraftStatus:
                    return new List<string> { "draft", "available" };

                case ExcludedFields.CategoryName:
                    return new List<string> { "name" };

                default:
                    throw new ArgumentException($"Unsupported field: {field}");
            }
        }

        private IEnumerable<T> GetAllWithPagination<T>(RestRequest request, Func<IEnumerable<T>, bool> processPage)
        {
            var results = new List<T>();
            while (true)
            {
                var response = _client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    break;
                }

                var responseData = JsonConvert.DeserializeObject<MxPaginatedResponse<T>>(response.Content);
                if (responseData?.Results == null || !responseData.Results.Any())
                {
                    break;
                }

                results.AddRange(responseData.Results);

                if (responseData.Next == null)
                {
                    break;
                }

                NextPage(request);
            }
            ResetPage(request);
            return results;
        }

        private void ResetPage(RestRequest request)
        {
            _currentPage = 1;
            request.AddOrUpdateParameter("per_page", _pageSize.ToString());
            request.AddOrUpdateParameter("page", _currentPage.ToString());
        }

        private void NextPage(RestRequest request)
        {
            _currentPage++;
            request.AddOrUpdateParameter("per_page", _pageSize.ToString());
            request.AddOrUpdateParameter("page", _currentPage.ToString());
        }

        public new bool ValidateConnection()
        {
            const string endPoint = "products/";
            var request = new RestRequest(endPoint, Method.Get);
            var response = _client.Execute(request);
            return response.StatusCode == HttpStatusCode.OK;
        }

        #region Product Methods

        public new long CreateProduct(WebProduct entity, List<ExcludedFields> excludedFields = null)
        {
            const string endpoint = "products/";
            var mxProduct = MixinConverters.ToMxProduct(entity);
            var request = new RestRequest(endpoint, Method.Post);
            var createdProduct = SendRequest<MxProduct>(request, mxProduct, excludedFields).Result;
            long encodedId = EncodedProduct.Encode(createdProduct.Id, 0);
            return encodedId;
        }

        public new long UpdateProduct(long id, WebProduct entity, List<ExcludedFields> excludedFields = null)
        {
            if (excludedFields == null)
            {
                excludedFields = new List<ExcludedFields>();
            }
            if (!excludedFields.Contains(ExcludedFields.ProductAttributes))
            {
                excludedFields.Add(ExcludedFields.ProductAttributes);
            }
            if (!excludedFields.Contains(ExcludedFields.DraftStatus))
            {
                excludedFields.Add(ExcludedFields.DraftStatus);
            }

            long decodedId = EncodedProduct.Decode(id).variableId;
            entity.Id = decodedId;

            var endpoint = $"products/{decodedId}/";
            var request = new RestRequest(endpoint, Method.Put);
            var updatedProductData = MixinConverters.ToMxProduct(entity);
            
            if (excludedFields.Contains(ExcludedFields.ProductDiscount))
            {
                updatedProductData.Price = updatedProductData.CompareAtPrice;
                excludedFields.Remove(ExcludedFields.ProductDiscount);
            }

            var updatedProduct = SendRequest<MxProduct>(request, updatedProductData, excludedFields).Result;
            long encodedId = EncodedProduct.Encode(updatedProduct.Id, 0);
            return encodedId;
        }

        public new WebProduct GetProductById(long id)
        {
            long decodedId = EncodedProduct.Decode(id).variableId;
            var endpoint = $"products/{decodedId}/";
            var request = new RestRequest(endpoint, Method.Get);
            var mxProduct = SendRequest<MxProduct>(request).Result;
            mxProduct.Id = id;
            return MixinConverters.ToWebProduct(mxProduct);
        }

        public new IEnumerable<WebProduct> GetAllProducts()
        {
            const string endpoint = "products/";
            var request = new RestRequest(endpoint, Method.Get);
            var results = new List<MxProduct>();
            return GetAllWithPagination<MxProduct>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Select(mxProduct =>
            {
                mxProduct.Id = EncodedProduct.Encode(mxProduct.Id, 0);
                return MixinConverters.ToWebProduct(mxProduct);
            }).ToList();
        }

        public new int GetTotalProductsCount(string searchTerm)
        {
            const string endpoint = "products/";
            var request = new RestRequest(endpoint, Method.Get);
            _pageSize = 100;
            ResetPage(request);
            if (searchTerm != null)
            {
                request.AddParameter("search", searchTerm);
            }
            var results = new List<MxProduct>();
            return GetAllWithPagination<MxProduct>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Count();
        }

        public new IEnumerable<WebProduct> GetAllProductsExcludingIds(IList<long> idsToExclude)
        {
            const string endPoint = "products/";
            var request = new RestRequest(endPoint, Method.Get);
            var products = SendRequest<MxPaginatedResponse<MxProduct>>(request).Result;
            return products.Results.Where(p => !idsToExclude.Contains(p.Id))
                                  .Select(mxProduct =>
                                  {
                                        mxProduct.Id = EncodedProduct.Encode(mxProduct.Id, 0);
                                        return MixinConverters.ToWebProduct(mxProduct);
                                  }).ToList();
        }

        public new IEnumerable<WebProduct> SearchProducts(string searchTerm, ProductTypes productType, int page = 1, int pageSize = 10, int maxPage = 1)
        {
            if (searchTerm == null)
            {
                return new List<WebProduct>();
            }

            _currentPage = page < 1 ? 1 : page;
            _pageSize = pageSize > 100 || pageSize < 1 ? 100 : pageSize;

            const string endPoint = "products/";
            var request = new RestRequest(endPoint, Method.Get);

            request.AddOrUpdateParameter("page", _currentPage.ToString());

            var response = GetAllWithPagination<MxProduct>(request, _ => true);
            if (productType == ProductTypes.Simple)
            {
                response = response.Where(p => !p.HasVariants).ToList();
            }
            else if (productType == ProductTypes.Variable)
            {
                response = response.Where(p => p.HasVariants).ToList();
            }
            return response.Where(r => r.Name.Contains(searchTerm)).Select(mxProduct =>
            { 
                mxProduct.Id = EncodedProduct.Encode(mxProduct.Id, 0);
                return MixinConverters.ToWebProduct(mxProduct); 
            });
        }

        public new IEnumerable<WebProduct> GetAllProductsWithFields(ProductTypes productType)
        {
            const string endPoint = "products/";
            var request = new RestRequest(endPoint, Method.Get);            
            var results = GetAllWithPagination<MxProduct>(request, pageResults => true);
            
            if (productType == ProductTypes.Simple)
            {
                results = results.Where(p => !p.HasVariants);
            }
            else if (productType == ProductTypes.Variable)
            {
                results = results.Where(p => p.HasVariants);
            }
            var productsByDetail = new List<WebProduct>();

            foreach (var product in results)
            {
                product.Id = EncodedProduct.Encode(product.Id, 0);
                WebProduct retreivedProduct = GetProductById(product.Id);
                productsByDetail.Add(retreivedProduct);
            }
            return productsByDetail;
        }

        public new long GetMaxProductId()
        {
            const string endpoint = "products/";
            var request = new RestRequest(endpoint, Method.Get);
            var response = SendRequest<MxPaginatedResponse<MxProduct>>(request).Result;
            return response.Results.Select(product =>
            {
                return EncodedProduct.Encode(product.Id, 0);
            }).Max(product => product);
        }

        public new IEnumerable<WebProduct> GetVariableProductsBySearch(string searchTerm)
        {
            const string endPoint = "products/";
            var request = new RestRequest(endPoint, Method.Get);
            if (searchTerm != null)
            {
                request.AddParameter("search", searchTerm);
            }

            var response = GetAllWithPagination<MxProduct>(request, _ => true);
            response = response.Where(p => p.HasVariants).ToList();
            return response.Where(r => r.Name.Contains(searchTerm)).Select(mxProduct => 
            {
                mxProduct.Id = EncodedProduct.Encode(mxProduct.Id, 0); 
                return MixinConverters.ToWebProduct(mxProduct); 
            });
        }

        public new IEnumerable<WebProduct> GetVariationProductsByVariableId(long variableId)
        {
            long decodedVariableId = EncodedProduct.Decode(variableId).variableId;
            string endPoint = $"products/{decodedVariableId}/variants/";
            var request = new RestRequest(endPoint, Method.Get);
            return GetAllWithPagination<MxVariant>(request, pageResults => true)
                .Select(variation =>
                {
                    var webProduct = MixinConverters.MxVariantToMcProduct(variation);
                    webProduct.Id = EncodedProduct.Encode(decodedVariableId, variation.Id);
                    return webProduct;
                }).ToList();
        }
        public new void UpdateVariationProduct(long variableId, WebProduct variationProduct, List<ExcludedFields> excludedFields = null)
        {
            if (excludedFields == null)
            {
                excludedFields = new List<ExcludedFields>();
            }
            if (!excludedFields.Contains(ExcludedFields.ProductName))
            {
                excludedFields.Add(ExcludedFields.ProductName);
            }
            if (!excludedFields.Contains(ExcludedFields.ProductAttributes))
            {
                excludedFields.Add(ExcludedFields.ProductAttributes);
            }
            if (!excludedFields.Contains(ExcludedFields.DraftStatus))
            {
                excludedFields.Add(ExcludedFields.DraftStatus);
            }
            (long decodedVariableId, long decodedVariationId) = EncodedProduct.Decode(variationProduct.Id);
            variationProduct.Id = decodedVariationId;

            var endPoint = $"products/{decodedVariableId}/variants/{decodedVariationId}/";
            var request = new RestRequest(endPoint, Method.Put);
            var updatedVariantData = MixinConverters.ToMxProduct(variationProduct);
            if (excludedFields.Contains(ExcludedFields.ProductDiscount))
            {
                updatedVariantData.Price = updatedVariantData.CompareAtPrice;
                excludedFields.Remove(ExcludedFields.ProductDiscount);
            }
            SendRequest<MxVariant>(request, updatedVariantData, excludedFields);
        }

        #endregion

        #region Category Methods

        public new WebCategory GetCategoryById(long id)
        {
            var endPoint = $"categories/{id}/";
            var request = new RestRequest(endPoint, Method.Get);
            var mxCategory = SendRequest<MxCategory>(request).Result;
            return MixinConverters.ToWebCategory(mxCategory);
        }

        public new IEnumerable<WebCategory> GetAllCategories()
        {
            const string endPoint = "categories/";
            var request = new RestRequest(endPoint, Method.Get);
            var results = new List<MxCategory>();
            return GetAllWithPagination<MxCategory>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Select(MixinConverters.ToWebCategory).ToList();
        }

        public new long CreateCategory(WebCategory entity, List<ExcludedFields> excludedFields = null)
        {
            const string endPoint = "categories/";
            var mxCategory = MixinConverters.ToMxCategory(entity);
            var request = new RestRequest(endPoint, Method.Post);
            var createdCategory = SendRequest<MxCategory>(request, mxCategory, excludedFields).Result;
            return createdCategory.Id;
        }

        public new long UpdateCategory(long id, WebCategory entity, List<ExcludedFields> excludedFields = null)
        {
            excludedFields.Add(ExcludedFields.Sku);
            var endPoint = $"categories/{id}/";
            var request = new RestRequest(endPoint, Method.Put);
            var updatedCategoryData = MixinConverters.ToMxCategory(entity);
            var updatedCategory = SendRequest<MxCategory>(request, updatedCategoryData, excludedFields).Result;
            return updatedCategory.Id;
        }

        public new IList<WebCategory> GetAllCategoriesWithFields(IList<string> fields)
        {
            if (fields == null || !fields.Any())
            {
                return new List<WebCategory>();
            }
            const string endPoint = "categories/";
            var request = new RestRequest(endPoint, Method.Get);
            var results = new List<MxCategory>();
            return GetAllWithPagination<MxCategory>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Select(MixinConverters.ToWebCategory).ToList();
        }

        public new long GetMaxCategoryId()
        {
            const string endpoint = "categories/";
            var request = new RestRequest(endpoint, Method.Get);
            var response = SendRequest<MxPaginatedResponse<MxCategory>>(request).Result;
            return response.Results.Max(category => category.Id);
        }

        public new IList<WebCategory> SearchCategories(string searchTerm, int page = 1, int pageSize = 10, int maxPage = 1)
        {
            _currentPage = page < 1 ? 1 : page;
            _pageSize = pageSize > 100 || pageSize < 1 ? 100 : pageSize;

            const string endPoint = "categories/";
            var request = new RestRequest(endPoint, Method.Get);
            if (searchTerm == null)
            {
                return new List<WebCategory>();
            }

            request.AddParameter("search", searchTerm);
            var results = new List<MxCategory>();
            return GetAllWithPagination<MxCategory>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Select(MixinConverters.ToWebCategory).ToList();
        }

        #endregion

        #region Customer Methods

        public new IEnumerable<WebCustomer> SearchCustomers(string searchTerm, int page = 1, int pageSize = 10, int maxPage = 1)
        {
            _currentPage = page < 1 ? 1 : page;
            _pageSize = pageSize > 100 || pageSize < 1 ? 100 : pageSize;

            const string endPoint = "customers/";
            var request = new RestRequest(endPoint, Method.Get);
            if (searchTerm == null)
            {
                return new List<WebCustomer>();
            }

            request.AddParameter("search", searchTerm);
            var results = new List<MxCustomer>();
            return GetAllWithPagination<MxCustomer>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Select(MixinConverters.ToWebCustomer).ToList();
        }

        public new IEnumerable<WebCustomer> GetAllCustomersWithFields(IList<string> fields)
        {
            if (fields == null || !fields.Any())
            {
                return new List<WebCustomer>();
            }
            const string endPoint = "customers/";
            var request = new RestRequest(endPoint, Method.Get);
            var results = new List<MxCustomer>();
            return GetAllWithPagination<MxCustomer>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Select(MixinConverters.ToWebCustomer).ToList();
        }

        public new IEnumerable<KeyValuePair<long, string>> GetCustomerIdAndNameBySearch(
            string searchTerm,
            int page = 1,
            int pageSize = 10,
            int maxPage = 1)
        {
            _currentPage = page < 1 ? 1 : page;
            _pageSize = pageSize > 100 || pageSize < 1 ? 100 : pageSize;

            const string endPoint = "customers/";
            var request = new RestRequest(endPoint, Method.Get);
            if (searchTerm == null)
            {
                return new List<KeyValuePair<long, string>>();
            }

            request.AddParameter("search", searchTerm);
            var results = new List<MxCustomer>();
            return GetAllWithPagination<MxCustomer>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            }).Select(customer =>
                new KeyValuePair<long, string>(customer.Id, $"{customer.FirstName} {customer.LastName}"))
                .ToList();
        }

        public new WebCustomer GetCustomerById(long id)
        {
            var endPoint = $"customers/{id}/";
            var request = new RestRequest(endPoint, Method.Get);
            var mxCustomer = SendRequest<MxCustomer>(request).Result;
            return MixinConverters.ToWebCustomer(mxCustomer);
        }

        #endregion

        #region Order Methods

        public new IEnumerable<WebOrder> GetAllOrdersExcludeById(IEnumerable<long> idsToExclude, DateTime? startDate, DateTime? endDate)
        {
            const string endPoint = "orders/";
            var request = new RestRequest(endPoint, Method.Get);

            if (startDate.HasValue)
            {
                request.AddParameter("start_date", startDate.Value.ToString("yyyy-MM-dd"));
            }
            if (endDate.HasValue)
            {
                request.AddParameter("end_date", endDate.Value.ToString("yyyy-MM-dd"));
            }

            var results = new List<MxOrderSummary>();
            var allOrders = GetAllWithPagination<MxOrderSummary>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            });

            return allOrders.Where(order => !idsToExclude.Contains(order.Id))
                           .Select(orderSummary => new WebOrder
                           {
                               Id = orderSummary.Id,
                               DateCreated = orderSummary.CreationDate,
                               Status = MixinConverters.MapOrderStatus(orderSummary.Status),
                           }).ToList();
        }

        public new IEnumerable<WebOrder> GetOrdersByFilters(DateTime? startDate, DateTime? endDate, IEnumerable<long> idsToExclude = null, IEnumerable<OrderStatus> orderStatuses = null)
        {
            const string endPoint = "orders/";
            if (idsToExclude == null)
            {
                idsToExclude = new List<long>();
            }

            var request = new RestRequest(endPoint, Method.Get);
            List<string> statusStrings = new List<string>();

            if (orderStatuses != null && orderStatuses.Any() && !orderStatuses.Contains(OrderStatus.Other))
            {
                statusStrings = orderStatuses.Select(status => MixinConverters.MapWebOrderStatusToJson(status)).ToList();
                request.AddParameter("status", string.Join(",", statusStrings));
            }
            else if (orderStatuses == null || !orderStatuses.Any())
            {
                return new List<WebOrder>();
            }

            if (startDate.HasValue)
            {
                request.AddParameter("start_date", startDate.Value.ToString("yyyy-MM-dd"));
            }
            if (endDate.HasValue)
            {
                request.AddParameter("end_date", endDate.Value.ToString("yyyy-MM-dd"));
            }

            var results = new List<MxOrderSummary>();
            var allOrders = GetAllWithPagination<MxOrderSummary>(request, pageResults =>
            {
                results.AddRange(pageResults);
                return true;
            });

            var orders = new List<WebOrder>();
            foreach (var order in allOrders)
            {
                if (!idsToExclude.Contains(order.Id) && order.CreationDate >= startDate && order.CreationDate <= endDate && (orderStatuses.Contains(OrderStatus.Other) || statusStrings.Contains(order.Status)))
                {
                    WebOrder updatedOrder = GetOrderById(order.Id);
                    orders.Add(updatedOrder);
                }
            }
            return orders;
        }

        public new IEnumerable<WebOrder> GetOrdersBySearch(
            IEnumerable<long> idsToExclude,
            string searchTerm,
            IEnumerable<OrderStatus> orderStatuses,
            long? customerId,
            decimal totalMin,
            decimal totalMax,
            DateTime startDate,
            DateTime endDate,
            int page = 1,
            int pageSize = 10,
            int maxPage = 1)
        {
            _currentPage = page < 1 ? 1 : page;
            _pageSize = pageSize > 100 || pageSize < 1 ? 100 : pageSize;

            const string endPoint = "orders/";
            var request = new RestRequest(endPoint, Method.Get);

            request.AddOrUpdateParameter("per_page", _pageSize.ToString());
            request.AddOrUpdateParameter("page", _currentPage.ToString());

            var response = SendRequest<MxPaginatedResponse<MxOrderSummary>>(request).Result;
            var filteredOrders = response.Results;

            if (idsToExclude != null && idsToExclude.Any())
            {
                filteredOrders = filteredOrders.Where(order => !idsToExclude.Contains(order.Id)).ToList();
            }

            var results = filteredOrders.Select(orderSummary => new WebOrder
            {
                Id = orderSummary.Id,
                DateCreated = orderSummary.CreationDate,
                Status = MixinConverters.MapOrderStatus(orderSummary.Status),
            }).ToList();

            var orders = new List<WebOrder>();
            foreach (var result in results)
            {
                WebOrder updatedOrder = GetOrderById(result.Id);
                orders.Add(updatedOrder);
            }
            if (orderStatuses != null && orderStatuses.Any() && !orderStatuses.Contains(OrderStatus.Other))
            {
                orders = orders.Where(o => orderStatuses.Contains(o.Status)).ToList();
            }
            return orders;
        }

        private WebOrder GetOrderById(long id)
        {
            var endPoint = $"orders/{id}/";
            var request = new RestRequest(endPoint, Method.Get);
            var mxOrder = SendRequest<MxOrderDetail>(request).Result;

            foreach (var result in mxOrder.Items)
            {
                string productUrl = result.Url;
                var (p1, v1) = ProductUrlParser.Parse(productUrl);

                if (v1.HasValue)
                {
                    // Try to validate / correct variant id
                    long? resolvedVid = ResolveVariantId(result.Name, p1);
                    if (resolvedVid.HasValue)
                    {
                        result.ProductId = EncodedProduct.Encode(p1, resolvedVid.Value);
                    }
                    else
                    {
                        result.ProductId = EncodedProduct.Encode(p1, v1.Value);
                    }
                }
                else
                {
                    // No variant from url, fallback
                    result.ProductId = EncodedProduct.Encode(p1, 0);
                }
            }

            return MixinConverters.ToWebOrder(mxOrder);
        }

        private long? ResolveVariantId(string itemName, long productId)
        {
            // Call product variants endpoint
            var endPoint = $"products/{productId}/variants/";
            var request = new RestRequest(endPoint, Method.Get);
            var variantsResponse = SendRequest<MxPaginatedResponse<MxVariant>>(request).Result;

            // Extract attributes from item name
            // Example: "لباس مردانه1 - Blue / 30"
            if (!itemName.Contains(" - ")) return null;

            string[] parts = itemName
                .Split(new[] { " - " }, StringSplitOptions.None)[1]
                .Split(new[] { "/" }, StringSplitOptions.None)
                .Select(p => p.Trim())
                .ToArray();

            foreach (var variant in variantsResponse.Results)
            {
                var values = variant.Attributes.Select(a => a.Value).ToList();
                if (parts.All(p => values.Contains(p)))
                {
                    return variant.Id; // Correct variant id
                }
            }
            return null;
        }


        public new void UpdateOrderStatus(long orderId, OrderStatus orderStatus)
        {
            throw new NotImplementedException("Order status update is not supported by Mixin API");
        }

        #endregion

        #region Payment Methods

        public new IEnumerable<WebPaymentMethod> GetAllPaymentMethods()
        {
            // Since Mixin API doesn't seem to have a dedicated payment methods endpoint
            // Return common payment methods based on the order data structure
            return new List<WebPaymentMethod>
            {
                new WebPaymentMethod { Id = 1, Title = "Online", Description = "Online Payment" },
                new WebPaymentMethod { Id = 2, Title = "Cash", Description = "Cash Payment" },
                new WebPaymentMethod { Id = 3, Title = "Card", Description = "Card Payment" }
            };
        }

        #endregion

        public new void Dispose()
        {
            _client?.Dispose();
        }
    }

    // Helper class for paginated responses
    public class MxPaginatedResponse<T>
    {
        [JsonProperty("next")]
        public string Next { get; set; }

        [JsonProperty("previous")]
        public string Previous { get; set; }

        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }

        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }

        [JsonProperty("per_page")]
        public int PerPage { get; set; }

        [JsonProperty("result")]
        public List<T> Results { get; set; }
    }
}