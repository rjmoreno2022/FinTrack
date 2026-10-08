using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace FinTrack.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures.Select(f => f.ErrorMessage).ToArray();

            // Si TResponse es Result (sin tipo genérico)
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Fail(errors);
            }

            // Si TResponse es Result<T>
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failMethod = typeof(TResponse).GetMethod("Fail", new[] { typeof(string[]) });
                if (failMethod != null)
                {
                    return (TResponse)failMethod.Invoke(null, new object[] { errors })!;
                }
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
