namespace Core.Interfaces.Services;

/// <summary>
/// Marker interface for services that should be registered as Scoped
/// </summary>
public interface IScopedService
{
}

/// <summary>
/// Marker interface for services that should be registered as Singleton
/// </summary>
public interface ISingletonService
{
}

/// <summary>
/// Marker interface for services that should be registered as Transient
/// </summary>
public interface ITransientService
{
}