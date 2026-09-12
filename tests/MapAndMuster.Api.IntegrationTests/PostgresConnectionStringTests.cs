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
        Assert.Equal(GssEncryptionMode.Disable, builder.GssEncryptionMode);
    }

    [Fact]
    public void KeywordStringsKeepTheirValuesAndGainPoolDefaults()
    {
        const string keyword =
            "Host=localhost;Port=5432;Database=mapandmuster;Username=mapandmuster;Password=mapandmuster";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(keyword));

        Assert.Equal("localhost", builder.Host);
        Assert.Equal("mapandmuster", builder.Database);
        Assert.Equal("mapandmuster", builder.Username);
        Assert.Equal("mapandmuster", builder.Password);
        Assert.Equal(20, builder.MaxPoolSize);
        Assert.Equal(1, builder.MinPoolSize);
        Assert.Equal(15, builder.Timeout);
        Assert.Equal(30, builder.CommandTimeout);
    }

    [Fact]
    public void AConfiguredPoolSizeIsNotOverwritten()
    {
        const string keyword =
            "Host=localhost;Database=mapandmuster;Username=mapandmuster;Password=mapandmuster;"
            + "Maximum Pool Size=6;Command Timeout=90";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(keyword));

        Assert.Equal(6, builder.MaxPoolSize);
        Assert.Equal(90, builder.CommandTimeout);

        // The keywords the operator did not set still get a default.
        Assert.Equal(15, builder.Timeout);
    }

    [Fact]
    public void ARenderUriAlsoGainsPoolDefaults()
    {
        const string uri = "postgresql://my_user:secret@dpg-example-a/mapandmuster?sslmode=require";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(uri));

        Assert.Equal(20, builder.MaxPoolSize);
        Assert.Equal(30, builder.CommandTimeout);
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
        Assert.Equal(GssEncryptionMode.Disable, builder.GssEncryptionMode);
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
