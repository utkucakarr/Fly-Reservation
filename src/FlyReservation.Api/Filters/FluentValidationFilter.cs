using FluentValidation;
using FlyReservation.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FlyReservation.Api.Filters
{
    // Runs before every controller action: validates each argument that has a registered
    // IValidator<T> and returns 400 ValidationProblem before the action runs
    public class FluentValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue;

                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                result.AddToModelState(context.ModelState);
            }

            if (!context.ModelState.IsValid && context.Controller is ControllerBase controller)
            {
                context.Result = controller.ValidationProblem(context.ModelState);
                return;
            }

            await next();
        }
    }
}
