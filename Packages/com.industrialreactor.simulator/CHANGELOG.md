# Changelog

All notable changes to the Industrial Reactor Simulator package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2024

### Added
- **SimulationMaterialManager** - New component for automatic material switching between Editor mode (metal materials) and Play mode (simulation shaders)
- **Sequential Pipe Flow** - Water visually fills inlet pipe first, then tank starts filling. Outlet pipe shows water draining
- **Flow Rate in L/s** - All flow rates now configurable in Liters per second with editor UI
- **Z-Axis Water Fill** - Water shader now supports filling along Z-axis for horizontal tanks
- **Pipe Flow Events** - `OnPipeFilled` and `OnPipeEmptied` events for flow coordination
- **Water Fill Axis** - `WaterFillAxis` enum (Y or Z) for flexible tank orientation
- **Pipe Flow State** - `PipeFlowState` enum (Empty, Filling, Filled, Draining)

### Changed
- **ReactorWater.shader** - Complete rewrite with:
  - Z-axis fill support via `_FillAxis` property (0=Y, 1=Z)
  - Realistic depth-based coloring (shallow to deep)
  - Procedural caustics effect
  - Surface foam at edges
  - Fresnel reflections
  - Multi-layer wave animation
  - Subsurface scattering approximation
- **PipeFlow.shader** - Complete rewrite with:
  - UV-based fill progress (`_FillProgress` property)
  - Top-to-bottom flow direction
  - Water color with depth variation
  - Caustics effect inside pipes
  - Fresnel highlights
- **TankCutaway.shader** - Enhanced with:
  - Better normal mapping support
  - Interior metallic/smoothness controls
  - Shadow casting support
- **TankCutawayController** - Now supports dual material slots:
  - Slot 0: Metal material (visible on remaining half when cut)
  - Slot 1: Cutaway shader
- **PipeFlowVisualizer** - Major update:
  - Flow rate in L/s (`FlowRateLitersPerSecond` property)
  - Pipe dimensions (length, cross-section) for realistic fill timing
  - Fill progress animation
  - Events for tank coordination
- **WaterSimulator** - Enhanced with:
  - Z-axis fill mode support
  - `CanFill` property for sequential flow control
  - Events: `OnWaterLevelChanged`, `OnTankFull`, `OnTankEmpty`
- **ReactorController** - Updated for:
  - Sequential flow coordination (inlet pipe → tank → outlet pipe)
  - Material manager integration
  - Flow rate in L/s
- **ReactorSimulationData** - New properties:
  - `defaultInletFlowRateLPS`, `defaultOutletFlowRateLPS`
  - `maxInletFlowRateLPS`, `maxOutletFlowRateLPS`
  - `defaultPipeLength`, `defaultPipeCrossSectionArea`
  - `CalculatePipeFillTime()` method

### Fixed
- Tank cutaway now properly shows metal material on remaining half
- Water fills along correct axis (Z for horizontal tanks)
- Pipe flow is now visible and animates correctly

## [1.0.0] - 2024-01-01

### Added
- Initial release of Industrial Reactor Simulator
- Custom Editor Window (Tools > Industrial Reactor Simulator)
- ReactorController - Main simulation coordinator
- ValveController - Inlet/outlet valve control with smooth transitions
- AgitatorController - RPM-based rotation with spin up/down curves
- WaterSimulator - Water level management with mesh scaling
- TankCutawayController - Shader clipping and mesh hiding modes
- BubbleEffectController - Particle intensity linked to agitator RPM
- PipeFlowVisualizer - UV scrolling flow animation
- ReactorSimulationData - ScriptableObject configuration
- Event-driven architecture (ReactorEvents)
- URP Shaders:
  - ReactorWater.shader - Swirl, turbulence, emission effects
  - TankCutaway.shader - Plane-based clipping with interior rendering
  - PipeFlow.shader - Animated flow texture scrolling
- Comprehensive documentation and setup guide
- Sample scenes and presets

### Technical
- Play Mode only operation
- MaterialPropertyBlock for efficient shader updates
- Coroutine-based smooth transitions
- Assembly definitions for clean compilation
- Unity 6 and URP 17+ support
