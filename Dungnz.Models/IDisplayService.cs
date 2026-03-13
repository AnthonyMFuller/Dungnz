
namespace Dungnz.Models;

/// <summary>
/// Combined display + input interface. Inherits from <see cref="IGameDisplay"/>
/// (output-only) and <see cref="IGameInput"/> (input-coupled) for backward
/// compatibility with existing call sites.
/// </summary>
/// <remarks>
/// <para>New code should depend on the narrower interface it actually needs
/// (<see cref="IGameDisplay"/> for output, <see cref="IGameInput"/> for input).
/// Existing code (GameLoop, CombatEngine, StartupOrchestrator) continues to
/// accept <c>IDisplayService</c> unchanged.</para>
/// <para>All existing implementations (ConsoleDisplayService,
/// SpectreLayoutDisplayService) already implement every method — they
/// automatically satisfy both sub-interfaces.</para>
/// </remarks>
public interface IDisplayService : IGameDisplay, IGameInput { }
