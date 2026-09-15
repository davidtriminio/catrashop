namespace CommerceHub.Api.Performance;

/// <summary>
/// Host-level performance knobs (config section <c>Performance</c>). Everything has safe defaults so the app
/// runs with no configuration; production can tune or disable each control. Phase 22.
/// </summary>
public sealed class PerformanceOptions
{
    public const string SectionName = "Performance";

    public ResponseCompressionOptions ResponseCompression { get; set; } = new();
    public OutputCacheOptions OutputCache { get; set; } = new();

    public sealed class ResponseCompressionOptions
    {
        /// <summary>Compress JSON responses with Brotli/Gzip when the client negotiates it.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Compress over HTTPS. Off by default: compressing a TLS response that reflects attacker-influenced
        /// input alongside a secret is the BREACH oracle, and a TLS-terminating proxy usually compresses
        /// anyway. Turn on only when the API is not fronted by a compressing proxy and the risk is understood.
        /// </summary>
        public bool EnableForHttps { get; set; }
    }

    public sealed class OutputCacheOptions
    {
        /// <summary>Serve identical anonymous catalog reads from an in-memory output cache.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>How long a cached catalog read stays fresh. Bounds staleness after an admin edit.</summary>
        public int CatalogTtlSeconds { get; set; } = 60;
    }
}
