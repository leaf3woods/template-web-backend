namespace Org.Product.Application.Abstractions.Authentication;

public interface IAccessTokenIssuer : IApplicationAbstraction
{
    IssuedAccessToken Issue(Guid userId, IEnumerable<Guid> roleIds);
}

public sealed record IssuedAccessToken(string Value, TimeSpan Lifetime);
