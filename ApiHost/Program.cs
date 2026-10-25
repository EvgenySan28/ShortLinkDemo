using System.Threading.RateLimiting;
using ApiHost.Options;
using Application;
using Data;
using Data.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.18.0.0/16"));
});
builder.Services
    .AddOptions<CreateShortLinkRateLimitOptions>()
    .Bind(builder.Configuration.GetSection("CreateShortLinkRateLimit"))
    .ValidateOnStart();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("create-short-link", httpContext =>
    {
        var rateLimit = httpContext.RequestServices
            .GetRequiredService<IOptions<CreateShortLinkRateLimitOptions>>()
            .Value;

        return RateLimitPartition.GetTokenBucketLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = rateLimit.PermitLimit,
                TokensPerPeriod = rateLimit.PermitLimit,
                ReplenishmentPeriod = rateLimit.Window,
                QueueLimit = 0
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        var rateLimit = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<CreateShortLinkRateLimitOptions>>()
            .Value;

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                error = rateLimit.FormatError(),
                permitLimit = rateLimit.PermitLimit,
                windowSeconds = rateLimit.Window.TotalSeconds
            },
            cancellationToken);
    };
});
builder.Services
    .AddOptions<ShortLinkOptions>()
    .Bind(builder.Configuration.GetSection("ShortLinks"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<RedirectCacheOptions>()
    .Bind(builder.Configuration.GetSection("RedirectCache"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
        exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.MapControllers();

app.Run();
