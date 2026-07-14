using PolicyEngine.Domain.Common;

namespace PolicyEngine.Domain.Policies;

/// <summary>Value object for the period of cover.</summary>
public readonly record struct PolicyTerm(DateOnly StartDate, DateOnly EndDate)
{
    public static PolicyTerm AnnualFrom(DateOnly start) => new(start, start.AddYears(1).AddDays(-1));

    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    /// <summary>Days of cover remaining from a given date (inclusive) to the end of the term.</summary>
    public int RemainingDaysFrom(DateOnly date)
    {
        if (!Contains(date))
            throw new DomainException($"Date {date:yyyy-MM-dd} falls outside the policy term.");
        return EndDate.DayNumber - date.DayNumber + 1;
    }
}
