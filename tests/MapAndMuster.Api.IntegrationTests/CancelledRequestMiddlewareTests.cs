using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class CancelledRequestMiddlewareTests
{
    [Fact]
    public async Task SwallowsCancellationWhenTheClientAbortedTheRequest()
    {
        using var lifetime = new CancellationTokenSource();
        lifetime.Cancel();
        var context = new DefaultHttpContext
        {
            RequestAborted = lifetime.Token,
        };

        var pipeline = Pipeline(_ => throw new OperationCanceledException(lifetime.Token));
        await pipeline(context);
    }

    [Fact]
    public async Task DoesNotSwallowCancellationWhenTheRequestWasNotAborted()
    {
        var context = new DefaultHttpContext();
        var pipeline = Pipeline(_ => throw new OperationCanceledException());
        await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline(context));
    }

    private static RequestDelegate Pipeline(RequestDelegate inner)
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        app.UseCampaignCancelledRequests();
        app.Run(inner);
        return app.Build();
    }
}
