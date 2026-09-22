
namespace Org.Product.Infrastructure.Utilities.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public int ExpireSeconds { get; set; } = 300;

    public TimeSpan Expiration => TimeSpan.FromSeconds(ExpireSeconds);
}
