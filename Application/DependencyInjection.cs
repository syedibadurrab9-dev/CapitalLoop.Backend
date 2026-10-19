using Application.Common.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // 2. Discover and register all CQRS Lite Command & Query Handlers as Scoped
        var handlerInterfaceDefinitions = new[]
        {
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>)
        };

        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var type in handlerTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && handlerInterfaceDefinitions.Contains(iface.GetGenericTypeDefinition()))
                {
                    services.AddScoped(iface, type);
                }
            }
        }

        return services;
    }
}
