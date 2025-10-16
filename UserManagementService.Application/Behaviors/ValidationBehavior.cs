using FluentValidation;
using MediatR;
using UserManagementService.Application.Common;

namespace UserManagementService.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that automatically validates all commands and queries.
    /// This ensures validation runs before any handler executes.
    /// Follows the Open/Closed Principle - we add validation without modifying handlers.
    /// </summary>
    public class ValidationBehavior<TRequest,TResponse>:IPipelineBehavior<TRequest,TResponse> where TRequest: IRequest<TResponse>
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
            if(!_validators.Any())
            {
                return await next();
            }

            var context=new ValidationContext<TRequest>(request);

            var validationResults=await Task.WhenAll(
                _validators.Select(v=>v.ValidateAsync(context,cancellationToken)));

            var failures=validationResults
                .SelectMany(r=>r.Errors)
                .Where(f=>f!=null)
                .ToList();

            if (failures.Count != 0)
            {
                var errors=failures.Select(f=>f.ErrorMessage).ToList();

                var resultType = typeof(TResponse);
                if(resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var dataType = resultType.GetGenericArguments()[0];
                    var failureMethod = typeof(Result<>)
                        .MakeGenericType(dataType)
                        .GetMethod("Failure");

                    var result=failureMethod?.Invoke(null,new object[] {"Validation failed", errors });
                    return (TResponse)result!;
                }
            }
            return await next();
        }
    }
}