using Catrashop.Api.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Catrashop.IntegrationTests;

/// <summary>
/// The boot-time guard that refuses to start with development placeholder secrets outside Development. It is a
/// pure function of environment + configuration, so it is tested directly without booting a host.
/// </summary>
public sealed class SecretGuardTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static IHostEnvironment Env(string name) => new FakeEnvironment(name);

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public FakeEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Catrashop.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public void Development_tolerates_placeholder_secrets()
    {
        var config = Config(
            ("Identity:Jwt:SigningKey", "dev-only-signing-key-change-me-please-32+chars"),
            ("Payments:WebhookSecret", "dev-only-webhook-secret-change-me"));

        // Should not throw: placeholders are how the app runs locally and in the in-memory test host.
        SecretGuard.Validate(Env(Environments.Development), config);
    }

    [Fact]
    public void Production_rejects_the_development_placeholder_signing_key()
    {
        var config = Config(
            ("Identity:Jwt:SigningKey", "dev-only-signing-key-change-me-please-32+chars"),
            ("Payments:WebhookSecret", "a-strong-production-webhook-secret"));

        var ex = Assert.Throws<InvalidOperationException>(() => SecretGuard.Validate(Env(Environments.Production), config));
        Assert.Contains("Identity:Jwt:SigningKey", ex.Message);
    }

    [Fact]
    public void Production_rejects_a_missing_secret()
    {
        var config = Config(("Identity:Jwt:SigningKey", "a-perfectly-strong-signing-key-of-length-32+"));

        var ex = Assert.Throws<InvalidOperationException>(() => SecretGuard.Validate(Env(Environments.Production), config));
        Assert.Contains("Payments:WebhookSecret", ex.Message);
    }

    [Fact]
    public void Production_accepts_strong_secrets()
    {
        var config = Config(
            ("Identity:Jwt:SigningKey", "a-perfectly-strong-signing-key-of-length-32+"),
            ("Payments:WebhookSecret", "a-strong-production-webhook-secret"));

        SecretGuard.Validate(Env(Environments.Production), config);
    }
}
