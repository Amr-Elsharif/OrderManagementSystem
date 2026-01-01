using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderManagementSystem.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogInformation("Handling request: {RequestName}", requestName);

            var startTime = DateTime.UtcNow;

            try
            {
                var response = await next(cancellationToken);

                var elapsedTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Request {RequestName} handled successfully in {ElapsedMilliseconds}ms",
                    requestName, elapsedTime.TotalMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling request {RequestName}", requestName);
                throw;
            }
        }
    }
}