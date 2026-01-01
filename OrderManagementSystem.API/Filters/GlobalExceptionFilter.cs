using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OrderManagementSystem.Application.Exceptions;

namespace OrderManagementSystem.API.Filters
{
    public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger = logger;

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            switch (exception)
            {
                case NotFoundException notFoundException:
                    _logger.LogWarning(notFoundException, "Resource not found");
                    context.Result = new NotFoundObjectResult(new { error = notFoundException.Message });
                    context.ExceptionHandled = true;
                    break;

                case ValidationException validationException:
                    _logger.LogWarning(validationException, "Validation error");
                    context.Result = new BadRequestObjectResult(new { error = validationException.Message });
                    context.ExceptionHandled = true;
                    break;

                case BusinessRuleException businessRuleException:
                    _logger.LogWarning(businessRuleException, "Business rule violation");
                    context.Result = new BadRequestObjectResult(new { error = businessRuleException.Message });
                    context.ExceptionHandled = true;
                    break;

                default:
                    _logger.LogError(exception, "Unhandled exception");
                    context.Result = new ObjectResult(new { error = "An unexpected error occurred" })
                    {
                        StatusCode = StatusCodes.Status500InternalServerError
                    };
                    context.ExceptionHandled = true;
                    break;
            }
        }
    }
}