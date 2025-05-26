using System.Net.Http.Json;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class ProductsMicroserviceClient
{
    private readonly HttpClient _httpClient;
    
    public ProductsMicroserviceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<ProductDTO?> GetProductByProductId(Guid productId)
    {
        var response = await _httpClient.GetAsync($"/api/products/search/product-id/{productId}");

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) 
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

        return product;
    }
}