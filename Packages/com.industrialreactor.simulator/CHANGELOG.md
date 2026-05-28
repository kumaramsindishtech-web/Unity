# Changelog

All notable changes to the Industrial Reactor Simulator package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
