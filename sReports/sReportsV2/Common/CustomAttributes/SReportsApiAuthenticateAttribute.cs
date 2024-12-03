using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using sReportsV2.Common.Helpers;
using System.Net;

namespace sReportsV2.Common.CustomAttributes
{
    public class SReportsApiAuthenticateAttribute : TypeFilterAttribute
    {
        public SReportsApiAuthenticateAttribute() : base(typeof(SReportsApiAuthenticationFilter))
        {
        }
    }

    public class SReportsApiAuthenticationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!IsAuthenticated(context.HttpContext.Request))
            {
                HandleUnAuthenticatedRequest(context);
            }
        }

        private bool IsAuthenticated(HttpRequest request)
        {
            return HasValidAccessKey(request.Headers["Access-Token"]);
        }

        private bool HasValidAccessKey(string accessToken)
        {
            return AccessTokenHelper.GetAccessToken() == accessToken;
        }

        private void HandleUnAuthenticatedRequest(AuthorizationFilterContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status403Forbidden;
            string forbiddenMessage = $"Could not access {context.ActionDescriptor.DisplayName}";
            context.Result = new JsonResult(forbiddenMessage);
        }
    }
}
