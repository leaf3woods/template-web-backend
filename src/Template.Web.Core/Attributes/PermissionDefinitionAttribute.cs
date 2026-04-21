using Template.Web.Core.Enums;

namespace Template.Web.Domain.Utilities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class PermissionDefinitionAttribute : Attribute
    {
        public PermissionDefinitionAttribute(string code, string? description = null)
        {
            Code = code;
            Description = description;
        }

        public string Code { get; private set; } = null!;

        public string? Description { get; private set; }

        private PermissionMethodType _type;

    }
}