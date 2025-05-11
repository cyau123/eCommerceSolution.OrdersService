using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.BusinessLogicLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}