using Org.Product.Domain.Entities.Account;

namespace Org.Product.Domain.ValueObjects.UserValue;

public class UserSetting
{
    public string Language { get; set; } = "Chinese";

    #region navigation

    public User User { get; set; } = null!;

    #endregion navigation
}
