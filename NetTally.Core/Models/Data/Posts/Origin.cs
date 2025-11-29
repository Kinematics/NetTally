namespace NetTally.Models;

/// <summary>
/// The identification information of a post or plan.
/// </summary>
public abstract record Origin();

/// <summary>
/// Represents an origin value indicating that no origin is specified or applicable.
/// </summary>
/// <remarks>Use this type when an entity does not have an associated origin. This can be useful for
/// scenarios where origin information is optional or unavailable.</remarks>
public sealed record NoOrigin() : Origin;

/// <summary>
/// Represents an origin location associated with a specific user.
/// </summary>
/// <param name="UserName">The user identity associated with this origin.</param>
/// <param name="Source">The source location of the origin.</param>
public sealed record UserOrigin(Author UserName, Source Source) : Origin;

/// <summary>
/// Represents the origin details of a plan, including its name, author, and the source location where it was posted.
/// </summary>
/// <param name="PlanName">The name that identifies the plan.</param>
/// <param name="Author">The user who authored the plan.</param>
/// <param name="Source">The location or post in which the plan was posted.</param>
public sealed record PlanOrigin(Author PlanName, Author Author, Source Source) : Origin;

