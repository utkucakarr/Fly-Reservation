using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FlyReservation.Api.Filters
{
    // For the endpoints that read the refresh token cookie. A browser sends cookies by itself, so another site
    // could trigger these requests (CSRF). A form or an image on that site cannot add a custom header,
    // and a script can only add one if CORS allows its origin. SameSite=Strict is the first layer, this is the second
    public class RequireCsrfHeaderAttribute : ActionFilterAttribute
    {
        public const string HeaderName = "X-Requested-With";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.ContainsKey(HeaderName))
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = $"The {HeaderName} header is required."
                })
                { StatusCode = StatusCodes.Status400BadRequest };
            }
        }
    }
}
