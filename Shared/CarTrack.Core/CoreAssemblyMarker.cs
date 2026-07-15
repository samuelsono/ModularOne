namespace CarTrack.Core;

/// <summary>
/// Marker type identifying the CarTrack.Core assembly.
/// Holds domain-agnostic primitives (<see cref="IAuditable"/>). Must not depend
/// on Entity Framework, ASP.NET, or any module (ADR 0001).
/// </summary>
public sealed class CoreAssemblyMarker;
