using MixinApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApi.Models;

namespace MixinApi.Utilities
{
    public static class MixinConverters
    {
        // Product Converters
        public static WebProduct ToWebProduct(MxProduct jsonProduct)
        {
            decimal? regularPrice = null;
            if (jsonProduct.CompareAtPrice.HasValue)
            {
                regularPrice = jsonProduct.CompareAtPrice.Value;
            }
            decimal? salePrice = null;
            if (jsonProduct.Price.HasValue)
            {
                salePrice = jsonProduct.Price.Value;
            }
            
            if (regularPrice.HasValue && !salePrice.HasValue)
            {
                salePrice = regularPrice;
            }
            else if (!regularPrice.HasValue && salePrice.HasValue)
            {
                regularPrice = salePrice;
            }

            return new WebProduct
            {
                Id = jsonProduct.Id,
                Name = jsonProduct.Name ?? jsonProduct.EnglishName,
                Slug = (jsonProduct.EnglishName != null ? jsonProduct.EnglishName.GenerateSlug() :
                        jsonProduct.Name != null ? jsonProduct.Name.GenerateSlug() : null),
                DateCreated = DateTime.Now,
                DateModified = DateTime.Now,
                Description = string.Empty,
                Sku = string.Empty,
                BuyPrice = null,
                RegularPrice = regularPrice ?? 0,
                SalePrice = salePrice ?? 0,
                StockQuantity = jsonProduct.StockType == "unlimited" ? 10 : 0,
                Categories = new List<WebCategory>
            {
                new WebCategory { Id = jsonProduct.MainCategory ?? 0 }
            },
                IconPath = string.Empty,
                Type = jsonProduct.HasVariants ? "variable" : "simple",
                Attributes = new List<WebApi.Models.Attribute>()
            };
        }
        public static WebProduct MxVariantToMcProduct(MxVariant variant)
        {
            return new WebProduct
            {
                Id = variant.Id,
                Name = variant.Name,
                Attributes = variant.Attributes.Select(a => new WebApi.Models.Attribute
                {
                    Id = a.Id,
                    Value = a.Value
                }).ToList(),
                RegularPrice = variant.CompareAtPrice ?? variant.Price ?? 0,
                SalePrice = variant.Price,
                StockQuantity = variant.Stock,
            };
        }

        public static MxProduct ToMxProduct(WebProduct webProduct)
        {
            int? compareAtPrice = null;
            if (webProduct.RegularPrice != 0)
            {
                compareAtPrice = (int)webProduct.RegularPrice;
            }
            var categoryId = webProduct.Categories?.FirstOrDefault()?.Id;
            if (categoryId == null || categoryId == 0)
            {
                categoryId = 1; // Default to category ID 1 if none provided
            }
            string stockType = webProduct.StockQuantity <= 0 ? "out_of_stock" : "limited";

            return new MxProduct
            {
                Id = webProduct.Id,
                Name = webProduct.Name,
                EnglishName = webProduct.Slug,
                MainCategory = categoryId,
                Brand = null,
                Price = (int)webProduct.SalePrice,
                CompareAtPrice = compareAtPrice,
                StockType = stockType,
                Stock = webProduct.StockQuantity,
                HasVariants = webProduct.Type == "variable",
                Available = true,
                Images = new List<object>(),
                Variants = new List<MxVariant>()
            };
        }

        // Category Converters
        public static WebCategory ToWebCategory(MxCategory jsonCategory)
        {
            return new WebCategory
            {
                Id = jsonCategory.Id,
                Name = jsonCategory.Name,
                ParentId = jsonCategory.Parent == 0 ? null : jsonCategory.Parent,
                Description = string.Empty, // Not available in JSON
                IconPath = string.Empty // Not available in JSON
            };
        }

        public static MxCategory ToMxCategory(WebCategory webCategory)
        {
            return new MxCategory
            {
                Id = webCategory.Id,
                Name = webCategory.Name,
                Parent = webCategory.ParentId == 0 ? null : webCategory.ParentId,
                Available = true
            };
        }

        // Customer Converters
        public static WebCustomer ToWebCustomer(MxCustomer jsonCustomer)
        {
            return new WebCustomer
            {
                Id = jsonCustomer.Id,
                FirstName = jsonCustomer.FirstName,
                LastName = jsonCustomer.LastName,
                Address1 = string.Empty, // Not available in JSON
                Address2 = string.Empty, // Not available in JSON
                City = string.Empty, // Not available in JSON
                State = string.Empty, // Not available in JSON
                Postcode = string.Empty, // Not available in JSON
                Country = string.Empty, // Not available in JSON
                Email = jsonCustomer.Email,
                PhoneNumbers = new List<string> { jsonCustomer.PhoneNumber?.ToString() },
                CreatedDate = jsonCustomer.DateJoined
            };
        }

        public static MxCustomer ToMxCustomer(WebCustomer webCustomer)
        {
            var phoneNumber = webCustomer.PhoneNumbers?.FirstOrDefault();
            return new MxCustomer
            {
                Id = webCustomer.Id,
                Username = $"{webCustomer.FirstName?.ToLower()}{webCustomer.LastName?.ToLower()}",
                FirstName = webCustomer.FirstName,
                LastName = webCustomer.LastName,
                PhoneNumber = long.TryParse(phoneNumber, out long phone) ? phone : 0,
                Email = webCustomer.Email,
                NationalNumber = 0, // Not available in WebCustomer
                IsActive = true,
                DateJoined = webCustomer.CreatedDate ?? DateTime.Now,
                LastLogin = DateTime.Now
            };
        }

        // Order Converters
        public static WebOrder ToWebOrder(MxOrderDetail jsonOrder)
        {
            var paymentMethod = MapPaymentMethod(jsonOrder.PaymentMethod);
            var orderStatus = MapOrderStatus(jsonOrder.Status);

            return new WebOrder
            {
                Id = jsonOrder.Id,
                CustomerId = 0, // Not directly available, would need customer lookup
                CustomerNote = jsonOrder.CustomerNote,
                PaymentMethod = paymentMethod,
                TransactionId = string.Empty, // Not available in JSON
                Status = orderStatus,
                IsConverted = false,
                InvoiceNumber = jsonOrder.Id.ToString(),
                StatusText = jsonOrder.Status,
                DateCreated = jsonOrder.CreationDate,
                DateModified = jsonOrder.CreationDate, // Using creation date as fallback
                ShippingTotal = jsonOrder.ShippingPrice ?? 0,
                OrderTax = jsonOrder.TaxAmount ?? 0,
                OrderDiscount = jsonOrder.CouponDiscountAmount ?? 0,
                UnitPrice = 0, // Will be calculated from items
                Billing = MapMxCustomerToWebCustomer(jsonOrder),
                Shipping = MapMxShippingToWebCustomer(jsonOrder),
                ShippingDetail = new WebShippingDetail
                {
                    Id = 0,
                    VehicleId = 0,
                    VehicleName = jsonOrder.ShippingMethodName,
                    VehiclePrice = jsonOrder.ShippingPrice ?? 0,
                    VehicleDescription = jsonOrder.ShippingMethodName
                },
                OrderItems = jsonOrder.Items?.Select(item => new WebOrderDetail
                {
                    Id = item.Id,
                    Name = item.Name,
                    ProductId = item.ProductId,
                    VariationId = 0,
                    Quantity = item.Quantity,
                    UnitPrice = item.CompareAtPrice ?? item.Price,
                    UnitDiscount = (item.CompareAtPrice ?? item.Price) - item.Price,
                    UnitTax = 0 // Tax calculation would need to be distributed
                }).ToList() ?? new List<WebOrderDetail>()
            };
        }

        public static MxOrderDetail ToMxOrder(WebOrder webOrder)
        {
            return new MxOrderDetail
            {
                Id = webOrder.Id,
                CreationDate = webOrder.DateCreated,
                Status = MapWebOrderStatusToJson(webOrder.Status),
                FirstName = webOrder.Billing?.FirstName ?? string.Empty,
                LastName = webOrder.Billing?.LastName ?? string.Empty,
                PhoneNumber = webOrder.Billing?.PhoneNumbers?.FirstOrDefault() ?? string.Empty,
                CustomerNote = webOrder.CustomerNote ?? string.Empty,
                PaymentMethod = MapWebPaymentMethodToJson(webOrder.PaymentMethod),
                PaymentStatus = "paid", // Default assumption
                ShippingMethodName = webOrder.ShippingDetail?.VehicleName ?? string.Empty,
                ShippingDate = null,
                ShippingStartTime = null,
                ShippingEndTime = null,
                ShippingProvince = webOrder.Shipping?.State ?? string.Empty,
                ShippingCity = webOrder.Shipping?.City ?? string.Empty,
                ShippingAddress = $"{webOrder.Shipping?.Address1} {webOrder.Shipping?.Address2}".Trim(),
                ShippingZipCode = webOrder.Shipping?.Postcode ?? string.Empty,
                ShippingFirstName = webOrder.Shipping?.FirstName ?? string.Empty,
                ShippingLastName = webOrder.Shipping?.LastName ?? string.Empty,
                ShippingPhoneNumber = webOrder.Shipping?.PhoneNumbers?.FirstOrDefault() ?? string.Empty,
                ShippingTrackingCode = null,
                CouponDiscountAmount = null,
                DiscountAmount = webOrder.OrderDiscount,
                CartPrice = webOrder.SubTotal,
                TaxAmount = webOrder.OrderTax,
                ShippingPrice = webOrder.ShippingTotal,
                FinalPrice = webOrder.Total,
                Items = webOrder.OrderItems?.Select(item => new MxOrderItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Quantity = (int)item.Quantity,
                    CompareAtPrice = (int)(item.UnitPrice + item.UnitDiscount),
                    Price = (int)item.UnitPrice,
                    TotalPrice = (int)item.Total,
                    Image = string.Empty,
                    Url = $"product/{item.ProductId}"
                }).ToList() ?? new List<MxOrderItem>(),
                Events = new List<MxOrderEvent>()
            };
        }

        // Helper Methods
        public static WebCustomer MapMxCustomerToWebCustomer(MxOrderDetail jsonOrder)
        {
            return new WebCustomer
            {
                FirstName = jsonOrder.FirstName,
                LastName = jsonOrder.LastName,
                PhoneNumbers = new List<string> { jsonOrder.PhoneNumber },
                Address1 = string.Empty,
                Address2 = string.Empty,
                City = string.Empty,
                State = string.Empty,
                Postcode = string.Empty,
                Country = string.Empty,
                Email = string.Empty
            };
        }

        public static WebCustomer MapMxShippingToWebCustomer(MxOrderDetail jsonOrder)
        {
            return new WebCustomer
            {
                FirstName = jsonOrder.ShippingFirstName,
                LastName = jsonOrder.ShippingLastName,
                PhoneNumbers = new List<string> { jsonOrder.ShippingPhoneNumber },
                Address1 = jsonOrder.ShippingAddress,
                Address2 = string.Empty,
                City = jsonOrder.ShippingCity,
                State = jsonOrder.ShippingProvince,
                Postcode = jsonOrder.ShippingZipCode,
                Country = string.Empty,
                Email = string.Empty
            };
        }

        public static WebPaymentMethod MapPaymentMethod(string paymentMethod)
        {
            var paymentMethods = new Dictionary<string, int>
            {
                { "online", 1 },
                { "cash", 2 },
                { "card", 3 }
            };

            paymentMethods.TryGetValue(paymentMethod?.ToLower() ?? "", out int methodId);

            return new WebPaymentMethod
            {
                Id = methodId,
                Title = paymentMethod,
                Description = paymentMethod
            };
        }

        public static OrderStatus MapOrderStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return OrderStatus.Other;

            switch (status.ToLower())
            {
                case "processing":
                    return OrderStatus.Pending;
                case "shipping":
                    return OrderStatus.Processing;
                case "finished":
                    return OrderStatus.Completed;
                case "draft":
                    return OrderStatus.OnHold;
                case "canceled":
                    return OrderStatus.Cancelled;
                default:
                    return OrderStatus.Other;
            }
        }

        public static string MapWebOrderStatusToJson(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Pending:
                    return "processing";
                case OrderStatus.Processing:
                    return "shipping";
                case OrderStatus.Completed:
                    return "finished";
                case OrderStatus.OnHold:
                    return "draft";
                case OrderStatus.Cancelled:
                    return "canceled";
                default:
                    return "processing";
            }
        }
        public static string MapWebPaymentMethodToJson(WebPaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
                return "online";

            switch (paymentMethod.Id)
            {
                case 1:
                    return "online";
                case 2:
                    return "cash";
                case 3:
                    return "card";
                default:
                    return "online";
            }
        }

        // Extension method for slug generation (if not already available)
        public static string GenerateSlug(this string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return string.Empty;

            return phrase.ToLower()
                        .Replace(" ", "-")
                        .Replace("_", "-");
        }
    }

}
