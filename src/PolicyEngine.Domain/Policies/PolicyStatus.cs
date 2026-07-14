namespace PolicyEngine.Domain.Policies;

public enum PolicyStatus
{
    Quoted = 0,
    Bound = 1,
    Active = 2,
    Cancelled = 3,
    Expired = 4
}
