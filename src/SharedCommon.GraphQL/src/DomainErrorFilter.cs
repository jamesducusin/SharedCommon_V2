using HotChocolate;
using SharedCommon.Core.Exceptions;

namespace SharedCommon.GraphQL;

/// <summary>
/// Maps <see cref="DomainException"/> subclasses to structured GraphQL errors.
/// Prevents internal details from leaking to clients; unhandled exceptions become
/// a generic "INTERNAL_ERROR" response.
///
/// Registered automatically by <see cref="ServiceCollectionExtensions.AddSharedGraphQL"/>.
/// </summary>
public sealed class DomainErrorFilter : IErrorFilter
{
    /// <inheritdoc />
    public IError OnError(IError error)
    {
        if (error.Exception is DomainException domain)
        {
            return error
                .WithMessage(domain.Message)
                .WithCode(domain.Code)
                .RemoveException();
        }

        if (error.Exception is not null)
        {
            // Never expose internal exception details
            return error
                .WithMessage("An unexpected error occurred.")
                .WithCode("INTERNAL_ERROR")
                .RemoveException();
        }

        return error;
    }
}
