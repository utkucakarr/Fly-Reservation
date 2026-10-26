using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlyReservation.Api.Extensions
{
    public static class ValidationResultExtensions
    {
        // Copies FluentValidation errors into ModelState so ValidationProblem(ModelState)
        // builds the response through ASP.NET's ProblemDetailsFactory (adds type + traceId)
        public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState)
        {
            foreach (var error in result.Errors)
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
