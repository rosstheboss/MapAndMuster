using MapAndMuster.Infrastructure.Persistence;
using Npgsql;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void RenderUriBecomesAKeywordStringNpgsqlCanParse()
    {
        const string uri =
            "postgresql://my_user:p%40ss%3Bword@dpg-example-a.ohio-postgres.render.com/mapandmuster?sslmode=require";

        var normalized = PostgresConnectionString.Normalize(uri);
        var builder = new NpgsqlConnectionStringBuilder(normalized);

        Assert.Equal("dpg-example-a.ohio-postgres.render.com", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("mapandmuster", builder.Database);
        Assert.Equal("my_user", builder.Username);
        Assert.Equal("p@ss;word", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void KeywordStringsPassThrough()
    {
        const string keyword =
            "Host=localhost;Port=5432;Database=mapandmuster;Username=mapandmuster;Password=mapandmuster";

        Assert.Equal(keyword, PostgresConnectionString.Normalize(keyword));
    }

    [Fact]
    public void AngleBracketsAndQuotesAreStrippedFromAUri()
    {
        const string wrapped = "<postgresql://my_user:secret@dpg-example-a/mapandmuster>";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(wrapped));

        Assert.Equal("dpg-example-a", builder.Host);
        Assert.Equal("mapandmuster", builder.Database);
        Assert.Equal("secret", builder.Password);
    }

    [Fact]
    public void LoopbackUriDisablesSsl()
    {
        const string uri = "postgres://mapandmuster:mapandmuster@127.0.0.1:5432/mapandmuster";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(uri));

        Assert.Equal(SslMode.Disable, builder.SslMode);
    }

    [Fact]
    public void TrailingMarkdownBacktickIsStrippedFromTheDatabaseName()
    {
        const string uri = "postgresql://my_user:secret@dpg-example-a/mapandmuster`";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(uri));

        Assert.Equal("mapandmuster", builder.Database);
    }

    [Fact]
    public void WrappedMarkdownBackticksAreStrippedFromAUri()
    {
        const string wrapped = "`postgresql://my_user:secret@dpg-example-a/mapandmuster`";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(wrapped));

        Assert.Equal("dpg-example-a", builder.Host);
        Assert.Equal("mapandmuster", builder.Database);
    }

    [Fact]
    public void InvalidUriThrowsWithoutTheSecret()
    {
        const string secret = "super-secret-password";
        var exception = Assert.Throws<InvalidOperationException>(
            () => PostgresConnectionString.Normalize($"postgresql://user:{secret}@"));

        Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("postgresql://", exception.Message, StringComparison.Ordinal);
    }
}
