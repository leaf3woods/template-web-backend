namespace Org.Product.Application.Abstractions.Security;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(Guid userId, IEnumerable<Guid> roleIds);
}

public sealed record IssuedAccessToken(string Value, TimeSpan Lifetime);
