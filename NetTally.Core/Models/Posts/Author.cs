namespace NetTally.Models;

public abstract record Author();
public sealed record NamedAuthor(string Name) : Author;
public sealed record UnknownAuthor() : Author;
public sealed record NoAuthor() : Author;
