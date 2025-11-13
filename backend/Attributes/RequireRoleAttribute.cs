using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using backend.Model;

namespace backend.Attributes
{
    /// <summary>
    /// Authorization attribute to restrict access based on user roles
    /// Usage: [RequireRole("Admin")] or [RequireRole("Admin", "Moderator")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Authentication required. Please login."
                });
                return;
            }

            // Get user's role from claims (using ClaimTypes.Role)
            var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userRole))
            {
                context.Result = new ForbiddenResult();
                return;
            }

            // Check if user has required role
            if (!_roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = $"Access denied. Required role: {string.Join(" or ", _roles)}"
                })
                {
                    StatusCode = 403 // Forbidden
                };
                return;
            }

            // Authorization successful
        }
    }

    /// <summary>
    /// Custom result for 403 Forbidden
    /// </summary>
    public class ForbiddenResult : ObjectResult
    {
        public ForbiddenResult() : base(new
        {
            success = false,
            message = "Access denied. Insufficient permissions."
        })
        {
            StatusCode = 403;
        }
    }
}
