namespace NetTally.Models;

public abstract record Origin();

public sealed record NoOrigin() : Origin;

public sealed record UserOrigin(Author UserName, OriginDetail Detail) : Origin;

public sealed record PlanOrigin(Author PlanName, Author Author, OriginDetail Detail) : Origin;

