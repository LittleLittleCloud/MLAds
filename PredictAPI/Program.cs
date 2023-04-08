using adsScore;
using Microsoft.EntityFrameworkCore;
using PredictAPI.Data;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Internal;
using Prometheus;
using PredictAPI;

var builder = WebApplication.CreateBuilder(args);

var ConectionString = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true ? "Server=localhost;Port=3306;Database=developixads;Uid=root;Pwd=;" : "Server=127.0.0.1;Port=3306;Database=DevelopixAds;Uid=developix;Pwd=2^3ApMepHzxtBT3r;";
builder.Services.AddDbContext<PredictAPIContext>(options =>
    options.UseMySQL(ConectionString));

builder.WebHost.UseUrls("http://127.0.0.1:7014");

builder.Services.AddHttpClient(AdsMLService.HttpClientName).UseHttpClientMetrics();

builder.Services.AddHostedService<AdsMLService>();

builder.Services.AddHealthChecks()
    .AddCheck<AdsMLHealthCheck>(nameof(AdsMLHealthCheck))
    .ForwardToPrometheus();

builder.Services.AddMemoryCache();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
});

builder.Services.AddSingleton<AdsServices>();

builder.Services.AddScoped<PredictAPI.Respository>();

builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Lifetime.ApplicationStarted.Register(OnStarted);

app.UseHttpMetrics();

app.UseAuthorization();

app.UseEndpoints(endpoints => endpoints.MapMetrics());

app.MapControllers();

app.Run();

void OnStarted()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetService<PredictAPIContext>();

        var myRepository = scope.ServiceProvider.GetService<PredictAPI.Respository>();

        myRepository.GetData();
    }
}