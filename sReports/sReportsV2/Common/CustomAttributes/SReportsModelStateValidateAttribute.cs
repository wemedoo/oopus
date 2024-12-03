using Serilog;
using sReportsV2.Common.Extensions;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using sReportsV2.Common.Exceptions;
using Microsoft.AspNetCore.Http;

namespace sReportsV2.Common.CustomAttributes
{
    public class SReportsModelStateValidateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext = Ensure.IsNotNull(filterContext, nameof(filterContext));

            if (filterContext.Controller is Controller controller)
            {
                if (!controller.ViewData.ModelState.IsValid)
                {
                    var allErrors = controller.ViewData.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    var allErrorsStr = string.Join(", ", allErrors);
                    throw new UserAdministrationException(StatusCodes.Status400BadRequest, allErrorsStr);
                }
            }
            else
            {
                throw new UserAdministrationException(StatusCodes.Status500InternalServerError, "The filterContext.Controller is not of type Controller.");
            }
        }
    }
}
