using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProcureLive.Application.Common.Behaviors;

namespace ProcureLive.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services.AddAutoMapper(assembly);

        services.AddScoped<Common.Interfaces.ISubscriptionService, Subscriptions.SubscriptionService>();
        services.AddScoped<Common.Interfaces.ISupplierAccessService, Common.Security.SupplierAccessService>();
        services.AddScoped<Auctions.Status.AuctionStatusService>();

        return services;
    }
}
