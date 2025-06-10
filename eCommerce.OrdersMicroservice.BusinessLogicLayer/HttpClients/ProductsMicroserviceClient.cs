using System.Net.Http.Json;
using System.Text.Json;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class ProductsMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductsMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public ProductsMicroserviceClient(HttpClient httpClient, ILogger<ProductsMicroserviceClient> logger, IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }
    
    public async Task<ProductDTO?> GetProductByProductId(Guid productId)
    {
        try
        {
            //Key: product:123
            //Value: { "ProductName: "...", ...}

            string cacheKey = $"product:{productId}";
            string? cachedProduct = await _distributedCache.GetStringAsync(cacheKey);
            
            // Return product if is in cache
            if (cachedProduct != null)
            {
                ProductDTO? productFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedProduct);
                return productFromCache;
            }
            
            // If product is not in cache, make an HTTP request to the Products Microservice
            HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/products/search/product-id/{productId}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    ProductDTO? productFromFallback = await response.Content.ReadFromJsonAsync<ProductDTO>();

                    if (productFromFallback == null)
                    {
                        throw new NotImplementedException("Fallback policy was not implemented");
                    }

                    return productFromFallback;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound) 
                {
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                }
            }
        
            ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();

            if (product == null) 
            {
                throw new ArgumentException("Invalid Product ID");
            }
            
            // Cache the product after successful retrieval
            string productJson = JsonSerializer.Serialize(product);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(300));
            
            string cacheKeyToWrite = $"product:{productId}";

            await _distributedCache.SetStringAsync(cacheKeyToWrite, productJson, options);
            
            return product;
        }
        catch (BulkheadRejectedException e)
        {
            _logger.LogError(e, "Bulkhead isolation blocks the request since the request queue is full");

            return new ProductDTO(
                ProductID: Guid.NewGuid(),
                ProductName: "Temporarily Unavailable (Bulkhead)",
                Category: "Temporarily Unavailable (Bulkhead)",
                UnitPrice: 0,
                QuantityInStock: 0);
        }
        
    }
}