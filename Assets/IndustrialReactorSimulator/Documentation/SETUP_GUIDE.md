# Industrial Reactor Simulator - Setup Guide

## Overview
A professional Unity addon for industrial reactor simulation with custom editor UI controls. 
Designed for realtime digital twin visualization in URP.

## Version
- Version: 1.0.0
- Unity: 6.x (URP)
- Author: Industrial Reactor Simulator

---

## Quick Start

### 1. Prerequisites
- Unity 6.x with URP (Universal Render Pipeline)
- Existing reactor hierarchy:
```
Reactor
├── Tank
├── Water
├── Agitator
├── Inlet_pipe
├── Outlet_Pipe
├── Valve_Inlet
└── Valve_Outlet
```

### 2. Add Components

#### ReactorController (Main Controller)
1. Add `ReactorController` to your **Reactor** parent GameObject
2. Click **"Auto Find Components"** in context menu, OR manually assign:
   - Inlet Valve → ValveController on Valve_Inlet
   - Outlet Valve → ValveController on Valve_Outlet
   - Agitator → AgitatorController on Agitator
   - Water Simulator → WaterSimulator on Water
   - Tank Cutaway → TankCutawayController on Tank
   - Bubble Effect → BubbleEffectController on Water
   - Inlet Pipe Flow → PipeFlowVisualizer on Inlet_pipe
   - Outlet Pipe Flow → PipeFlowVisualizer on Outlet_Pipe

#### ValveController (On each valve)
1. Add `ValveController` to **Valve_Inlet** and **Valve_Outlet**
2. Configure:
   - Valve Type: Set to "Inlet" or "Outlet"
   - Valve Handle: Assign the rotating part transform
   - Rotation Axis: Usually Vector3.up (Y-axis)
   - Open Rotation: Degrees to rotate when open (e.g., 90)
   - Transition Time: Time to open/close

#### AgitatorController
1. Add `AgitatorController` to **Agitator**
2. Configure:
   - Agitator Transform: The rotating shaft/blade transform
   - Rotation Axis: Usually Vector3.up
   - Max RPM: 300 recommended
   - Spin Up/Down Time: 2-3 seconds

#### WaterSimulator
1. Add `WaterSimulator` to **Water**
2. Configure:
   - Water Mesh: The water mesh transform
   - Scale Mode: "ScaleY" for simple vertical fill
   - Min Scale: 0.01 (empty)
   - Max Scale: 1.0 (full)
   - Water Renderer: Assign the water mesh renderer

#### TankCutawayController
1. Add `TankCutawayController` to **Tank**
2. Configure:
   - Cutaway Mode: "ShaderClipping" or "MeshHiding"
   - Tank Renderer: Assign tank mesh renderer
   - Clip Direction: Vector3.right for side cutaway

#### BubbleEffectController
1. Add `BubbleEffectController` to **Water** (or child object)
2. Create a Particle System (see Particle Setup below)
3. Assign Bubble Particles reference

#### PipeFlowVisualizer (On each pipe)
1. Add `PipeFlowVisualizer` to **Inlet_pipe** and **Outlet_Pipe**
2. Configure:
   - Associated Valve Type: Match to valve
   - Pipe Renderer: Assign pipe mesh renderer
   - Scroll Direction: Along pipe axis

---

## Material Setup

### Water Material
1. Create material: **Create > Material**
2. Assign shader: **Industrial Reactor/Water**
3. Configure properties:
   - Base Color: Light blue (0.2, 0.5, 0.8, 0.8)
   - Transparency: 0.7
   - Emission Color: Soft blue glow
   - Swirl Strength: 0.5

### Tank Material (Cutaway)
1. Create material for tank
2. Assign shader: **Industrial Reactor/Tank Cutaway**
3. Configure:
   - Base Color: Metallic silver
   - Interior Color: Darker interior
   - Enable Clip: ON

### Pipe Flow Material
1. Create material for pipes
2. Assign shader: **Industrial Reactor/Pipe Flow**
3. Configure:
   - Flow Color: Blue/cyan
   - Flow Speed: 2
   - Flow Intensity: 1

---

## Particle System Setup (Bubbles)

### Create Bubble Particles
1. **GameObject > Effects > Particle System**
2. Name it "BubbleParticles"
3. Parent to Water object

### Particle Settings
```
Duration: 5
Looping: ON
Start Lifetime: 2-3
Start Speed: 0.5
Start Size: 0.01-0.03
Gravity Modifier: -0.1 (float up)
Max Particles: 500

Shape:
- Shape: Box or Sphere
- Scale: Match water volume

Emission:
- Rate over Time: 0 (controlled by script)

Color over Lifetime:
- Alpha: 1 → 0 (fade out)

Size over Lifetime:
- Curve: Start small, grow slightly

Renderer:
- Material: Bubble/sphere material
- Render Mode: Billboard
```

---

## Using the Editor Window

### Open the Tool
**Menu: Tools > Industrial Reactor Simulator**

### Controls
1. **Simulation Controls**
   - Start/Stop/Reset buttons
   - Only works in Play Mode

2. **Valve Controls**
   - Open/Close inlet valve
   - Open/Close outlet valve
   - Status indicators

3. **Agitator Controls**
   - Start/Stop agitator
   - Running status

4. **Parameter Sliders**
   - Inlet Flow Speed (L/s)
   - Outlet Flow Speed (L/s)
   - Agitator RPM
   - Water Temperature (°C)

5. **Realtime Monitoring**
   - Reactor State
   - Tank Fill %
   - Temperature
   - Agitator RPM
   - Bubble Intensity

---

## Simulation Flow

### Typical Operation
1. Enter Play Mode
2. Click **Start Simulation** (activates cutaway)
3. Click **Open Inlet Valve** (water flows in)
4. Watch tank fill
5. Click **Start Agitator** (mixing begins)
6. Adjust RPM slider for intensity
7. Click **Open Outlet Valve** (drainage)
8. Click **Stop Agitator** (smooth slowdown)
9. Click **Stop Simulation**

---

## ScriptableObject Configuration

### Create Simulation Data
1. **Assets > Create > Industrial Reactor > Simulation Data**
2. Configure all parameters
3. Assign to ReactorController

### Key Parameters
- Tank Capacity: 1000L
- Valve Transition Time: 0.5s
- Default Flow Rates: 20 L/s inlet, 15 L/s outlet
- Max Agitator RPM: 300
- Spin Up/Down Time: 2-3s
- Temperature Range: 0-100°C

---

## Events System

Subscribe to reactor events for custom behavior:

```csharp
using IndustrialReactorSimulator.Core;

void OnEnable()
{
    ReactorEvents.OnSimulationStarted += HandleStart;
    ReactorEvents.OnWaterLevelChanged += HandleWaterLevel;
    ReactorEvents.OnAgitatorRPMChanged += HandleRPM;
}

void HandleWaterLevel(float level)
{
    Debug.Log($"Water: {level * 100}%");
}
```

---

## Troubleshooting

### Simulation won't start
- Ensure you're in Play Mode
- Check ReactorController has all references assigned

### Water not filling
- Check WaterSimulator has water mesh assigned
- Verify Scale Mode is set correctly
- Ensure inlet valve is configured properly

### Cutaway not working
- Assign tank renderer to TankCutawayController
- Use "Industrial Reactor/Tank Cutaway" shader
- Check clip plane settings

### No bubble effects
- Create and assign particle system
- Check BubbleEffectController emission rates
- Ensure agitator is running

---

## Performance Tips

1. Use MaterialPropertyBlock (enabled by default)
2. Keep particle counts reasonable (500 max)
3. Use LOD on complex meshes
4. Optimize shader complexity for mobile

---

## File Structure

```
Assets/IndustrialReactorSimulator/
├── Scripts/
│   ├── Core/
│   │   ├── ReactorEnums.cs
│   │   ├── ReactorEvents.cs
│   │   └── ReactorSimulationData.cs
│   ├── Controllers/
│   │   ├── ReactorController.cs
│   │   ├── ValveController.cs
│   │   ├── AgitatorController.cs
│   │   ├── WaterSimulator.cs
│   │   ├── TankCutawayController.cs
│   │   ├── BubbleEffectController.cs
│   │   └── PipeFlowVisualizer.cs
│   └── Editor/
│       └── ReactorSimulatorWindow.cs
├── Shaders/
│   ├── ReactorWater.shader
│   ├── TankCutaway.shader
│   └── PipeFlow.shader
└── Documentation/
    └── SETUP_GUIDE.md
```
