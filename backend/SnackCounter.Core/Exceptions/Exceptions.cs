namespace SnackCounter.Core.Exceptions;

public class DomainException(string message) : Exception(message);

public class ConflictException(string message) : DomainException(message);

public class NotFoundException(string message) : DomainException(message);

public class BudgetExceededException(string message, decimal requested, decimal available)
    : DomainException(message)
{
    public decimal Requested { get; } = requested;
    public decimal Available { get; } = available;
}

public class InvalidStateTransitionException(string message) : DomainException(message);

public class ValidationException(string message) : DomainException(message);
