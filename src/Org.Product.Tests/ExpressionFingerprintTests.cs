using System.Globalization;
using System.Linq.Expressions;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Shared.Expressions;

namespace Org.Product.Tests;

public sealed class ExpressionFingerprintTests
{
    [Fact]
    public void ParameterNameDiffers_SameFingerprint()
    {
        Expression<Func<User, bool>> first = user => user.Username == "operator";
        Expression<Func<User, bool>> second = account => account.Username == "operator";

        var firstFingerprint = CreateFingerprint(first);
        var secondFingerprint = CreateFingerprint(second);

        Assert.Equal(firstFingerprint, secondFingerprint);
    }

    [Fact]
    public void MemberDiffers_DifferentFingerprint()
    {
        Expression<Func<User, bool>> first = user => user.Username == "operator";
        Expression<Func<User, bool>> second = user => user.Name == "operator";

        Assert.NotEqual(CreateFingerprint(first), CreateFingerprint(second));
    }

    [Fact]
    public void MethodCalls_AreNotSupported()
    {
        Expression<Func<User, bool>> startsWith = user => user.Username.StartsWith("a");
        Expression<Func<User, bool>> contains = user => user.Username.Contains("a");

        Assert.False(FingerprintVisitor.TryCreate(startsWith, out _));
        Assert.False(FingerprintVisitor.TryCreate(contains, out _));
    }

    [Fact]
    public void UnlistedOperator_IsNotSupported()
    {
        Expression<Func<User, bool>> predicate = user => user.SortOrder + 1 > 2;

        Assert.False(FingerprintVisitor.TryCreate(predicate, out _));
    }

    [Fact]
    public void CapturedValueDiffers_DifferentFingerprint()
    {
        var first = CreateUsernamePredicate("operator");
        var second = CreateUsernamePredicate("administrator");

        Assert.Equal(first.ToString(), second.ToString());
        Assert.NotEqual(CreateFingerprint(first), CreateFingerprint(second));
    }

    [Fact]
    public void CapturedPropertyChanges_DifferentFingerprint()
    {
        var filter = new UserFilter { Username = "operator" };
        Expression<Func<User, bool>> predicate = user => user.Username == filter.Username;

        var fingerprint = CreateFingerprint(predicate);
        filter.Username = "administrator";

        Assert.NotEqual(fingerprint, CreateFingerprint(predicate));
    }

    [Fact]
    public void ExpressionStructureDiffers_DifferentFingerprint()
    {
        Expression<Func<User, bool>> first = user => user.Username == "operator";
        Expression<Func<User, bool>> second = user => user.Username != "operator";

        Assert.NotEqual(CreateFingerprint(first), CreateFingerprint(second));
    }

    [Fact]
    public void ComparisonOperatorsDiffer_DifferentFingerprints()
    {
        Expression<Func<User, bool>> greaterThan = user => user.Username.Length > 3;
        Expression<Func<User, bool>> greaterThanOrEqual = user => user.Username.Length >= 3;
        Expression<Func<User, bool>> lessThan = user => user.Username.Length < 3;
        Expression<Func<User, bool>> lessThanOrEqual = user => user.Username.Length <= 3;

        var fingerprints = new[]
        {
            CreateFingerprint(greaterThan),
            CreateFingerprint(greaterThanOrEqual),
            CreateFingerprint(lessThan),
            CreateFingerprint(lessThanOrEqual),
        };

        Assert.Equal(fingerprints.Length, fingerprints.Distinct().Count());
    }

    [Fact]
    public void ConstantTypeDiffers_DifferentFingerprint()
    {
        Assert.NotEqual(
            CreateFingerprint(Expression.Constant(1)),
            CreateFingerprint(Expression.Constant("1"))
        );
    }

    [Fact]
    public void CurrentCultureDiffers_SameFingerprint()
    {
        var threshold = 1234.5m;
        Expression<Func<User, bool>> predicate = user => user.SortOrder >= threshold;
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var first = CreateFingerprint(predicate);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var second = CreateFingerprint(predicate);

            Assert.Equal(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void CapturedComplexValue_IsNotSupported()
    {
        var usernames = new[] { "operator" };
        Expression<Func<User, bool>> predicate = user => usernames.Contains(user.Username);

        Assert.False(FingerprintVisitor.TryCreate(predicate, out _));
    }

    [Fact]
    public void StaticProperty_IsNotSupported()
    {
        Expression<Func<User, bool>> predicate = user => user.RegisterTime <= DateTime.UtcNow;

        Assert.False(FingerprintVisitor.TryCreate(predicate, out _));
    }

    [Fact]
    public void CapturedNullableValue_DifferentFingerprint()
    {
        DateTime? birthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Expression<Func<User, bool>> predicate = user => user.BirthDate == birthDate;

        var fingerprint = CreateFingerprint(predicate);
        birthDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.NotEqual(fingerprint, CreateFingerprint(predicate));
    }

    private sealed class UserFilter
    {
        public string Username { get; set; } = null!;
    }

    private static Expression<Func<User, bool>> CreateUsernamePredicate(string username) =>
        user => user.Username == username;

    private static string CreateFingerprint(Expression expression)
    {
        Assert.True(FingerprintVisitor.TryCreate(expression, out var fingerprint));
        return fingerprint;
    }
}
