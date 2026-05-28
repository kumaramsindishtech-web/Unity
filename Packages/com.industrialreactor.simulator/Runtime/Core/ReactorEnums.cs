// ============================================================================
// Industrial Reactor Simulator - Core Enums
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Represents the overall state of the reactor simulation
    /// </summary>
    public enum ReactorState
    {
        Idle,
        Starting,
        Running,
        Stopping,
        Paused,
        Error
    }

    /// <summary>
    /// Represents the state of a valve component
    /// </summary>
    public enum ValveState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    /// <summary>
    /// Represents the state of the agitator
    /// </summary>
    public enum AgitatorState
    {
        Stopped,
        Starting,
        Running,
        Stopping
    }

    /// <summary>
    /// Represents the type of valve for identification
    /// </summary>
    public enum ValveType
    {
        Inlet,
        Outlet
    }

    /// <summary>
    /// Represents flow direction for pipe visualization
    /// </summary>
    public enum FlowDirection
    {
        None,
        Forward,
        Reverse
    }

    /// <summary>
    /// Water scale mode for visualization
    /// </summary>
    public enum WaterScaleMode
    {
        ScaleY,
        ScaleXYZ,
        MoveAndScale,
        ShaderOnly
    }

    /// <summary>
    /// Cutaway visualization mode
    /// </summary>
    public enum CutawayMode
    {
        ShaderClipping,
        MeshHiding,
        Combined
    }
}
