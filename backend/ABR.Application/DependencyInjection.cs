using ABR.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ABR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
        return services;
    }
}
