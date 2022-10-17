using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAuth2.Line.Core.Database;
using OAuth2.Line.Core.Jwt;
using OAuth2.Line.Core.LineLogin;
using OAuth2.Line.Core.LineNotify;
using OAuth2.Line.Core.LineNotifyBinding;
using OAuth2.Line.Core.Message;

namespace OAuth2.Line.Core;

public static class CoreServiceCollectionExtenstions
{
    public static void AddCoreLibs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        services.AddDbContext<LineNotifyBindingContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("LineNotifyBinding");
            options.UseMySql(connectionString, Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.8.3-mariadb"));
        });

        services.Configure<LineLoginConfig>(configuration.GetSection("LineLogin"));
        services.Configure<LineNotifyConfig>(configuration.GetSection("LineNotify"));
        services.Configure<JwtConfig>(configuration.GetSection("Jwt"));

        services.AddScoped<LineLoginService>();
        services.AddScoped<LineNotifyService>();
        services.AddScoped<JwtService>();
        services.AddScoped<LineNotifyBindingService>();
        services.AddScoped<MessageService>();
    }
}