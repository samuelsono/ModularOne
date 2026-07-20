namespace CarTrack.Infrastructure;

/// <summary>
/// Marker type identifying the CarTrack.Infrastructure assembly.
///
/// Infrastructure holds shared technical concerns: the EF DbContext base and
/// auditing interceptor, the in-process event bus, storage, email, and caching.
/// It may depend on Core only (plus technical packages). Real types are added in
/// Phase 1.
/// </summary>
public sealed class InfrastructureAssemblyMarker;
