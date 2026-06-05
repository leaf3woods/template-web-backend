using Template.Web.Domain.Entities.Account;

namespace Template.Web.Domain.ValueObjects.UserValue
{
    public class UserSetting
    {
        public string Language { get; set; } = "Chinese";

        #region navigation

        public User User { get; set; } = null!;

        #endregion navigation
    }
}
