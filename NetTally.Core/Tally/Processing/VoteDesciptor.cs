namespace NetTally.Tally.Processing;
public sealed record PlanDescriptor(bool IsPlan, bool IsImplicit, string PlanName)
{
    public static PlanDescriptor None { get; } = new PlanDescriptor(false, false, string.Empty);
}
