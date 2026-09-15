using System.IO.Compression;
using CommerceHub.BuildingBlocks.Caching;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace CommerceHub.Api.Performance;

/// <summary>
/// Composition-root wiring for the Phase 22 performance review at the HTTP layer: response compression
/// (Brotli/Gzip for JSON) and output caching for anonymous, non-personalised catalog reads. Both are
/// config-driven with safe defaults, so the app and the integration host run unchanged out of the box.
/// The data-layer half of the review — DbContext pooling and no-tracking reads — lives next to the code it
/// optimises (persistence registration and the read stores).
/// </summary>
public static class PerformanceExtensions
{
    public static WebApplicationBuilder AddCommerceHubPerformance(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(PerformanceOptions.SectionName).Get<PerformanceOptions>()
                      ?? new PerformanceOptions();
        builder.Services.Configure<PerformanceOptions>(
            builder.Configuration.GetSection(PerformanceOptions.SectionName));

        if (options.ResponseCompression.Enabled)
        {
            builder.Services.AddResponseCompression(o =>
            {
                o.EnableForHttps = options.ResponseCompression.EnableForHttps;
                o.Providers.Add<BrotliCompressionProvider>();
                o.Providers.Add<GzipCompressionProvider>();
                // The framework defaults cover application/json; add the RFC 7807 problem type explicitly so
                // error payloads compress too.
                o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/problem+json"]);
            });
            // Favour ratio over speed — API JSON is small and CPU is rarely the bottleneck here.
            builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
            builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
        }

        if (options.OutputCache.Enabled)
        {
            var ttl = TimeSpan.FromSeconds(Math.Max(1, options.OutputCache.CatalogTtlSeconds));
            builder.Services.AddOutputCache(cache =>
            {
                // Anonymous catalog reads: cache per full query string (search term, filters, sort, page) and
                // per route (slug), tagged so a future catalog write can evict the whole set.
                cache.AddPolicy(OutputCachePolicies.CatalogReads, policy => policy
                    .Expire(ttl)
                    .SetVaryByQuery("*")
                    .Tag(OutputCachePolicies.CatalogTag));
            });
        }

        return builder;
    }

    /// <summary>
    /// Inserts the performance middleware. Response compression sits outside output caching so the cache
    /// stores the uncompressed body once and each client gets its own negotiated encoding; both run before
    /// authentication so an anonymous cache hit short-circuits early.
    /// </summary>
    public static WebApplication UseCommerceHubPerformance(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<PerformanceOptions>>().Value;

        if (options.ResponseCompression.Enabled)
            app.UseResponseCompression();

        if (options.OutputCache.Enabled)
            app.UseOutputCache();

        return app;
    }
}
