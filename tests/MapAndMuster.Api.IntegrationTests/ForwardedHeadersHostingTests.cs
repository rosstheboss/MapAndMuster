using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class ForwardedHeadersHostingTests
{
    [Fact]
    public void EnablesForwardedHeadersInProductionByDefault()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var production = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var development = new TestHostEnvironment { EnvironmentName = Environments.Development };

        Assert.True(ForwardedHeadersHosting.ShouldEnable(configuration, production));
        Assert.False(ForwardedHeadersHosting.ShouldEnable(configuration, development));
    }

    [Fact]
    public void ExplicitSettingOverridesEnvironmentDefault()
    {
        var enabled = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ForwardedHeaders:Enabled"] = "true" })
            .Build();
        var disabled = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ForwardedHeaders:Enabled"] = "false" })
            .Build();
        var production = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var development = new TestHostEnvironment { EnvironmentName = Environments.Development };

        Assert.True(ForwardedHeadersHosting.ShouldEnable(enabled, development));
        Assert.False(ForwardedHeadersHosting.ShouldEnable(disabled, production));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "MapAndMuster.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
