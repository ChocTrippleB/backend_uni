using Hangfire.Dashboard;
using System.Text;

namespace backend.Services
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly string _username;
        private readonly string _password;

        public HangfireAuthorizationFilter(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Get Authorization header
            string authHeader = httpContext.Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic "))
            {
                // Decode the base64 credentials
                var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedCredentials.Split(':', 2);

                if (credentials.Length == 2)
                {
                    var username = credentials[0];
                    var password = credentials[1];

                    // Validate credentials
                    if (username == _username && password == _password)
                    {
                        return true;
                    }
                }
            }

            // Require authentication
            httpContext.Response.StatusCode = 401;
            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            return false;
        }
    }
}
