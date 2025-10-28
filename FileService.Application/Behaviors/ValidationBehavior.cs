using FileService.Application.Common;
using FluentValidation;
using MediatR;

namespace FileService.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that automatically validates commands/queries before handling.
    /// This is the "O" in SOLID - Open/Closed Principle.
    /// We can add cross-cutting concerns like validation without modifying handlers.
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
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
            // If no validators exist for this request type, just continue
            if (!_validators.Any())
            {
                return await next();
            }

            // Run all validators
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // Collect all validation failures
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            // If there are validation errors, return a failure result
            if (failures.Count != 0)
            {
                var errors = failures.Select(f => f.ErrorMessage).ToList();

                // Create a failure result dynamically based on the response type
                var resultType = typeof(TResponse);
                if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var dataType = resultType.GetGenericArguments()[0];
                    var failureMethod = typeof(Result<>)
                        .MakeGenericType(dataType)
                        .GetMethod("Failure");

                    var result = failureMethod?.Invoke(null, new object[] { "Validation failed", errors });
                    return (TResponse)result!;
                }
            }

            // If validation passes, continue to the handler
            return await next();
        }
    }
}