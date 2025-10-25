using FluentValidation;
using MediatR;
using MessagingService.Application.Common;

namespace MessagingService.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior for automatic validation using FluentValidation.
    /// Intercepts all MediatR requests and validates them before handlers execute.
    /// If validation fails, returns a failure result without calling the handler.
    /// </summary>
    public class ValidationBehavior<TRequest,TResponse> :IPipelineBehavior<TRequest,TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // If no validators are registered for this request type, skip validation
            if (!_validators.Any())
            {
                return await next();
            }

            // Create validation context
            var context = new ValidationContext<TRequest>(request);

            // Run all validators in parallel for performance
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // Collect all validation failures
            var failures=validationResults
                .SelectMany(r=>r.Errors)
                .Where(f=>f!=null)
                .ToList();

            // If there are validation failures, return a failure result
            if (failures.Any())
            {
                var errors = failures.Select(f => f.ErrorMessage).ToList();

                // Check if the response type is Result<T>
                var resultType=typeof(TResponse);
                if(resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    // Use reflection to call Result<T>.Failure with proper generic type
                    var dataType = resultType.GetGenericArguments()[0];
                    var failureMethod = typeof(Result<>)
                        .MakeGenericType(dataType)
                        .GetMethod("Failure");

                    var result = failureMethod?.Invoke(null, new object[] { "Validation failed", errors });
                    return (TResponse)result!;
                }
            }
            return await next();
        }
    }
}