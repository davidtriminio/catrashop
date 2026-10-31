using Xunit;

namespace Catrashop.IntegrationTests;

/// <summary>Boots the in-memory API host once and shares it across every integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<CatrashopApiFactory>
{
    public const string Name = "Catrashop API";
}
