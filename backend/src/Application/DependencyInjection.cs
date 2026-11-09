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
        // Yetki ve kapsam doğrulamadan önce: yetkisiz kullanıcı alan hatalarını (400) değil 403'ü görür.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ScopeAuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Security.PermissionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services.AddAutoMapper(assembly);

        services.AddScoped<Common.Interfaces.ISubscriptionService, Subscriptions.SubscriptionService>();
        services.AddScoped<Common.Interfaces.ISupplierAccessService, Common.Security.SupplierAccessService>();
        services.AddScoped<Auctions.Status.AuctionStatusService>();
        services.AddScoped<Auctions.Participation.PreOfferBidService>();
        services.AddScoped<Common.Notifications.InternalNotificationAudience>();
        services.AddScoped<Common.Security.IDataScopeService, Common.Security.DataScopeService>();
        services.AddScoped<Common.Security.IPermissionService, Common.Security.PermissionService>();
        services.AddScoped<Notifications.Email.NotificationEmailDispatcher>();
        services.AddScoped<Erp.ErpSyncDispatcher>();

        return services;
    }
}
