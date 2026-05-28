# Industrial Reactor Simulator - Setup Guide

## Overview
Professional Unity addon for industrial reactor simulation with custom editor UI controls.
Designed for realtime digital twin visualization in URP.

## Requirements
- Unity 6.x (6000.0+)
- Universal Render Pipeline (URP) 17.0+

## Installation

### Via Package Manager (Git URL)
1. Open **Window > Package Manager**
2. Click **+** button > **Add package from git URL**
3. Enter: `https://github.com/kumaramsindishtech-web/Unity.git?path=Packages/com.industrialreactor.simulator`

### Via manifest.json
Add to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.industrialreactor.simulator": "https://github.com/kumaramsindishtech-web/Unity.git?path=Packages/com.industrialreactor.simulator"
  }
}
```

## Quick Start

### 1. Required Hierarchy
```
Reactor (ReactorController)
├── Tank (TankCutawayController)
├── Water (WaterSimulator, BubbleEffectController)
├── Agitator (AgitatorController)
├── Inlet_pipe (PipeFlowVisualizer)
├── Outlet_Pipe (PipeFlowVisualizer)
├── Valve_Inlet (ValveController)
└── Valve_Outlet (ValveController)
```

### 2. Add Components

**ReactorController** (on Reactor parent)
- Add component > Industrial Reactor > Reactor Controller
- Click "Auto Find Components" in context menu
- Or manually assign all references

**ValveController** (on each valve)
- Set Valve Type: Inlet or Outlet
- Assign Valve Handle transform
- Set Rotation Axis and Open Rotation

**AgitatorController** (on Agitator)
- Assign Agitator Transform
- Set Rotation Axis
- Configure Max RPM, Spin Up/Down times

**WaterSimulator** (on Water)
- Assign Water Mesh transform
- Set Scale Mode
- Assign Water Renderer

**TankCutawayController** (on Tank)
- Choose Cutaway Mode
- Assign Tank Renderer
- Set Clip Direction

**BubbleEffectController** (on Water)
- Create Particle System child
- Assign to Bubble Particles

**PipeFlowVisualizer** (on each pipe)
- Set Associated Valve Type
- Assign Pipe Renderer

### 3. Materials

**Water Material**
- Shader: Industrial Reactor/Water
- Adjust swirl, emission, transparency

**Tank Material**
- Shader: Industrial Reactor/Tank Cutaway
- Enable Clip, set interior color

**Pipe Material**
- Shader: Industrial Reactor/Pipe Flow
- Set flow color and speed

### 4. Usage

1. Enter Play Mode
2. Open **Tools > Industrial Reactor Simulator**
3. Click **Start Simulation**
4. Use controls to operate reactor

## Editor Window Controls

- **Start/Stop/Reset** - Simulation control
- **Open/Close Inlet** - Inlet valve control
- **Open/Close Outlet** - Outlet valve control
- **Start/Stop Agitator** - Mixing control
- **Inlet Flow Speed** - L/s slider
- **Outlet Flow Speed** - L/s slider
- **Agitator RPM** - Speed slider
- **Temperature** - °C slider

## Events API

```csharp
using IndustrialReactorSimulator;

void OnEnable()
{
    ReactorEvents.OnWaterLevelChanged += HandleWaterLevel;
    ReactorEvents.OnAgitatorRPMChanged += HandleRPM;
    ReactorEvents.OnSimulationStarted += HandleStart;
}

void HandleWaterLevel(float level) => Debug.Log($"Water: {level * 100}%");
```

## ScriptableObject Configuration

Create: **Assets > Create > Industrial Reactor > Simulation Data**

Configure:
- Tank capacity
- Flow rates
- Agitator settings
- Temperature range
- Effect parameters

Assign to ReactorController.

## Troubleshooting

**Simulation won't start**: Ensure Play Mode is active

**Components missing**: Use "Auto Find Components" context menu

**Water not filling**: Check WaterSimulator mesh and scale mode

**Cutaway not working**: Verify shader and renderer assignment

**No bubbles**: Create and assign particle system
