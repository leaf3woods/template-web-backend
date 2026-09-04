using System.Data;

namespace Template.Web.Domain.Repositories
{
    public sealed record SqlCommand(string Text, IReadOnlyCollection<SqlParameter> Parameters)
    {
        public static SqlCommand Create(string text, params SqlParameter[] parameters) =>
            new(text, parameters);
    }

    public sealed record SqlParameter(string Name, object? Value, DbType? DbType = null);
}
