using Microsoft.AspNetCore.Authorization;

namespace Template.Web.Application.Auth.Requirements
{
    public class PermissionAuthorizationRequirement : IAuthorizationRequirement
    {
        public PermissionAuthorizationRequirement(string permission)
        {
            Permission = permission;
        }

        public string Permission { get; private set; }
    }
}