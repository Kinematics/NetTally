namespace NetTally.Models;

/// <summary>
/// The identification information of a post or plan.
/// </summary>
public abstract record Origin();

/// <summary>
/// A type indicating that there is no origin.
/// </summary>
public sealed record NoOrigin() : Origin;

/// <summary>
/// The identification of a post.
/// </summary>
/// <param name="UserName">The use who made the post.</param>
/// <param name="Source">The location of the post.</param>
public sealed record UserOrigin(Author UserName, Source Source) : Origin;

/// <summary>
/// The identification of a plan.
/// </summary>
/// <param name="PlanName">The name of the plan.</param>
/// <param name="Author">The user who wrote the plan.</param>
/// <param name="Source">The location of the post the plan was posted in.</param>
public sealed record PlanOrigin(Author PlanName, Author Author, Source Source) : Origin;

