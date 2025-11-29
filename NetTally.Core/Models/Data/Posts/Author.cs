namespace NetTally.Models;

/// <summary>
/// An identifying name for a forum user or plan.
/// </summary>
public abstract record Author();

/// <summary>
/// Represents an author identified by a specific name.
/// </summary>
/// <param name="Name">The name of the author. Cannot be null or empty.</param>
public sealed record NamedAuthor(string Name) : Author;

/// <summary>
/// Represents an author whose identity is unknown or unspecified.
/// </summary>
/// <remarks>Use this type to indicate that author information is unavailable or cannot be determined. This is
/// typically returned when a source does not provide author details.</remarks>
public sealed record UnknownAuthor() : Author;

/// <summary>
/// Represents an authorless entity, indicating that no author information is available or applicable.
/// </summary>
/// <remarks>Use this type when an author is required by the API but no author data exists or should be specified.
/// This can be useful for anonymous content or system-generated items.</remarks>
public sealed record NoAuthor() : Author;
