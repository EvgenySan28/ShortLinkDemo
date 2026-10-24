using ApiHost.Options;
using Application;
using Data;
using Data.Options;

var builder = WebApplication.CreateBuilder(args);

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

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
