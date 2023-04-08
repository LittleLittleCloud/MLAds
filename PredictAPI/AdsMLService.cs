using Prometheus;
using System.Diagnostics;

namespace PredictAPI;

public sealed class AdsMLService : BackgroundService
{
    public const string HttpClientName = "SampleServiceHttpClient";

    public AdsMLService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private readonly IHttpClientFactory _httpClientFactory;

    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    await ReadySetGoAsync(cancel);
                }
                catch
                {
                    // Something failed? OK, whatever. We will just try again.
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancel);
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
    }

    private async Task ReadySetGoAsync(CancellationToken cancel)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        var googleStopwatch = Stopwatch.StartNew();
        var microsoftStopwatch = Stopwatch.StartNew();

        const string googleUrl = "https://google.com";
        const string microsoftUrl = "https://microsoft.com";

        var googleTask = Task.Run(async delegate
        {
            using var response = await httpClient.GetAsync(googleUrl, cancel);
            googleStopwatch.Stop();
        }, cancel);

        var microsoftTask = Task.Run(async delegate
        {
            using var response = await httpClient.GetAsync(microsoftUrl, cancel);
            microsoftStopwatch.Stop();
        }, cancel);

        await Task.WhenAll(googleTask, microsoftTask);

        var exemplar = Exemplar.From(Exemplar.Pair("traceID", "1234"));

        if (googleStopwatch.Elapsed < microsoftStopwatch.Elapsed)
        {
            WinsByEndpoint.WithLabels(googleUrl).Inc(exemplar);
            LossesByEndpoint.WithLabels(microsoftUrl).Inc(exemplar);
        }
        else if (googleStopwatch.Elapsed > microsoftStopwatch.Elapsed)
        {
            WinsByEndpoint.WithLabels(microsoftUrl).Inc(exemplar);
            LossesByEndpoint.WithLabels(googleUrl).Inc(exemplar);
        }
        else
        {

        }

        var difference = Math.Abs(googleStopwatch.Elapsed.TotalSeconds - microsoftStopwatch.Elapsed.TotalSeconds);
        Difference.Observe(difference, exemplar: exemplar);

        IterationCount.Inc();
    }

    private static readonly Counter IterationCount = Metrics.CreateCounter("sampleservice_iterations_total", "Number of iterations that the sample service has ever executed.");

    private static readonly string[] ByEndpointLabelNames = new[] { "endpoint" };

    private static readonly Counter WinsByEndpoint = Metrics.CreateCounter("sampleservice_wins_total", "Number of times a target endpoint has won the competition.", ByEndpointLabelNames);
    private static readonly Counter LossesByEndpoint = Metrics.CreateCounter("sampleservice_losses_total", "Number of times a target endpoint has lost the competition.", ByEndpointLabelNames);

    private static readonly Histogram Difference = Metrics.CreateHistogram("sampleservice_difference_seconds", "How far apart the winner and loser were, in seconds.", new HistogramConfiguration
    {
        Buckets = Histogram.PowersOfTenDividedBuckets(-2, 1, 10)
    });
}