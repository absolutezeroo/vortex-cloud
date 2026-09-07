using System.Text.Json.Serialization;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// What the investigation search answers, whichever shape the term turned out to be.
/// </summary>
/// <remarks>
/// The three answers share nothing but the tag, so they are three records rather than one with
/// most of it null: a correlation id has no player profile, and an unrecognised term has neither.
/// No record declares <c>kind</c> -- the serializer writes it from the attributes below, so the tag
/// and the shape it selects cannot drift apart.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CorrelationSearch), "correlationId")]
[JsonDerivedType(typeof(IdSearch), "id")]
[JsonDerivedType(typeof(UnknownSearch), "unknown")]
public abstract record DirectorySearch;
