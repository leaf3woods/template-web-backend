using System.Xml.Linq;

namespace Template.Web.Domain.Shared.Extensions
{
    public static class TypeExtension
    {
        public static bool IsDatabaseType(this Type type)
        {
            return (!type.IsClass || type == typeof(string)) && !type.IsInterface;
        }

        public static string GetServiceTypeScopeDefine(this Type type)
        {
            if (type.FullName == null || !type.FullName.Contains("Services"))
                throw new ArgumentException("not service type");
            // var indexes = type.Name.ToCharArray()
            //     .Select((c, i) => (ch: c, index: i))
            //     .Where(tu => char.IsUpper(tu.ch))
            //     .ToArray();
            var @class = type.Name.Replace("Service", "");
            //indexes.Length <= 1 ? type.Name : type.Name[..indexes[1].index];
            var @namespace = type.Namespace!.Split('.')[^1];
            return $"{@namespace}:{@class}:".ToLower();
        }
    }
}
