// ============================================================================
// Industrial Reactor Simulator - Core Enums
// Version: 1.0.0
// Description: Core enumerations for reactor simulation states and types
// ============================================================================

namespace IndustrialReactorSimulator.Core
{
    /// <summary>
    /// Represents the overall state of the reactor simulation
    /// </summary>
    public enum ReactorState
    {
        Idle,           // Simulation not running
        Starting,       // Initializing simulation
        Running,        // Active simulation
        Stopping,       // Shutting down simulation
        Paused,         // Simulation paused
        Error           // Error state
    }

    /// <summary>
    /// Represents the state of a valve component
    /// </summary>
    public enum ValveState
    {
        Closed,         // Valve fully closed
        Opening,        // Valve transitioning to open
        Open,           // Valve fully open
        Closing         // Valve transitioning to closed
    }

    /// <summary>
    /// Represents the state of the agitator
    /// </summary>
    public enum AgitatorState
    {
        Stopped,        // Agitator not rotating
        Starting,       // Agitator spinning up
        Running,        // Agitator at target RPM
        Stopping        // Agitator spinning down
    }

    /// <summary>
    /// Represents the type of valve for identification
    /// </summary>
    public enum ValveType
    {
        Inlet,          // Input valve
        Outlet          // Output valve
    }

    /// <summary>
    /// Represents flow direction for pipe visualization
    /// </summary>
    public enum FlowDirection
    {
        None,           // No flow
        Forward,        // Normal flow direction
        Reverse         // Reverse flow direction
    }

    /// <summary>
    /// Represents water visual effect intensity levels
    /// </summary>
    public enum EffectIntensity
    {
        None,
        Low,
        Medium,
        High,
        Maximum
    }
}
