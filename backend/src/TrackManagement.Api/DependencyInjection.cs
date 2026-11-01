using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

using TrackManagement.Api.Common;
using TrackManagement.Infrastructure.Persistence;

namespace TrackManagement.Api;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPresentation(IConfiguration configuration)
        {
            services.AddControllers();
            
            services.Configure<MvcOptions>(options =>
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

            services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            {
                var http = context.HttpContext;
                context.ProblemDetails.Instance = $"{http.Request.Method} {http.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("requestId", http.TraceIdentifier);

                var activity = http.Features.Get<IHttpActivityFeature>()?.Activity;
                if (activity is not null)
                {
                    context.ProblemDetails.Extensions.TryAdd("traceId", activity.Id);
                }
            });

            services.AddExceptionHandler<GlobalExceptionHandler>();

            services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            services.AddCors(options => options.AddPolicy(CorsPolicies.Frontend, policy => policy
                .WithOrigins(allowedOrigins)
                .WithMethods("GET", "POST", "PATCH")
                .WithHeaders("Authorization", "Content-Type")));

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(RateLimitingPolicies.Auth, httpContext =>
                {
                    var permitLimit = httpContext.RequestServices
                        .GetRequiredService<IConfiguration>()
                        .GetValue("RateLimiting:AuthPermitsPerMinute", 10);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        });
                });
            });

            services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

            return services;
        }
    }
}
