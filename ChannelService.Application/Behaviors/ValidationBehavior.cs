using ChannelService.Application.Common;
using FluentValidation;
using MediatR;

namespace ChannelService.Application.Behaviors
{
    public class ValidationBehavior<TRequest,TResponse>:IPipelineBehavior<TRequest,TResponse> where TRequest : IRequest<TResponse>
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
            if (!_validators.Any())
            {
                await next();
            }

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if(failures.Any())
            {
                var errors = failures.Select(f => f.ErrorMessage).ToList();

                var resultType = typeof(TResponse);
                if(resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var dataType=resultType.GetGenericArguments()[0];
                    var failuredMethod = typeof(Result<>)
                        .MakeGenericType(dataType)
                        .GetMethod("Failure");

                    var result = failuredMethod?.Invoke(null, new object[] { "Validation failed", errors });
                    return (TResponse)result!;
                }
            }

            return await next();
        }
    }
}