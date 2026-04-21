using Template.Web.Domain.Entities.Account;

namespace Template.Web.Domain.ValueObjects.UserValue
{
    public class UserDetail
    {
        public Gender Gender { get; set; } = Gender.Unknow;
        public string? AboutMe { get; set; }

        #region navigation

        public User User { get; set; } = null!;

        #endregion navigation
    }

    public enum Gender
    {
        Male,
        Female,
        Unknow
    }
}