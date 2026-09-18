namespace Org.Product.Application.Abstractions.CacheStore;

public static class CacheKeys
{
    public static string Token(Guid userId) => $"sys:token:{userId}";

    public static string Captcha(Guid captchaId) => $"sys:captcha:{captchaId}";

    public static string Permissions(Guid roleId) => $"sys:permissions:{roleId}";
}
