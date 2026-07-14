namespace PolicyEngine.Domain.Common;

/// <summary>Thrown when a business rule or lifecycle invariant is violated.</summary>
public sealed class DomainException(string message) : Exception(message);
