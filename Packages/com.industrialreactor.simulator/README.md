# Industrial Reactor Simulator

Professional Unity addon for industrial reactor simulation with custom editor UI controls.

![Unity](https://img.shields.io/badge/Unity-6.x-black?logo=unity)
![URP](https://img.shields.io/badge/URP-17.0+-blue)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Table of Contents

1. [Features](#features)
2. [Requirements](#requirements)
3. [Installation](#installation)
4. [Complete Setup Guide](#complete-setup-guide)
5. [Component Reference](#component-reference)
6. [Shader Setup](#shader-setup)
7. [Particle System Setup](#particle-system-setup)
8. [Using the Editor Window](#using-the-editor-window)
9. [Scripting API](#scripting-api)
10. [Troubleshooting](#troubleshooting)

---

## Features

- ✅ Custom Editor Window (Tools > Industrial Reactor Simulator)
- ✅ Play Mode only simulation
- ✅ Water fill/drain with smooth animation
- ✅ Agitator mixing with RPM control
- ✅ Bubble particle effects
- ✅ Tank cutaway visualization
- ✅ Valve open/close with transitions
- ✅ Pipe flow animation
- ✅ Temperature simulation
- ✅ Real-time monitoring dashboard
- ✅ Event-driven architecture
- ✅ ScriptableObject configuration

---

## Requirements

- **Unity Version:** 6.x (6000.0 or higher)
- **Render Pipeline:** Universal Render Pipeline (URP) 17.0+
- **Platform:** Windows, Mac, Linux

---

## Installation

### Method 1: Via Package Manager (Recommended)

1. Open Unity
2. Go to **Window → Package Manager**
3. Click the **+** button (top-left)
4. Select **"Add package from git URL..."**
5. Paste this URL:
```
https://github.com/kumaramsindishtech-web/Unity.git?path=Packages/com.industrialreactor.simulator
```
6. Click **Add**

### Method 2: Via manifest.json

1. Open your project's `Packages/manifest.json` file
2. Add this line to the `dependencies` section:
```json
{
  "dependencies": {
    "com.industrialreactor.simulator": "https://github.com/kumaramsindishtech-web/Unity.git?path=Packages/com.industrialreactor.simulator",
    ...other dependencies...
  }
}
```
3. Save the file and return to Unity

### Method 3: Local Installation

1. Download/clone the repository
2. Copy the `Packages/com.industrialreactor.simulator` folder
3. Paste it into your project's `Packages` folder

---

## Complete Setup Guide

### Step 1: Prepare Your Scene Hierarchy

Create this exact hierarchy in your scene. You can use existing 3D models or create primitive shapes for testing:

```
Reactor                    ← Empty GameObject (parent)
├── Tank                   ← Your tank 3D model
├── Water                  ← Cylinder or custom water mesh
├── Agitator               ← Your agitator/mixer 3D model
├── Inlet_pipe             ← Pipe 3D model for inlet
├── Outlet_Pipe            ← Pipe 3D model for outlet
├── Valve_Inlet            ← Valve 3D model for inlet
└── Valve_Outlet           ← Valve 3D model for outlet
```

**To create test objects:**
1. Right-click in Hierarchy → **Create Empty** → Name it "Reactor"
2. Right-click on Reactor → **3D Object → Cylinder** → Name it "Tank"
3. Right-click on Reactor → **3D Object → Cylinder** → Name it "Water" (scale smaller)
4. Right-click on Reactor → **3D Object → Cylinder** → Name it "Agitator"
5. Right-click on Reactor → **3D Object → Capsule** → Name it "Inlet_pipe"
6. Right-click on Reactor → **3D Object → Capsule** → Name it "Outlet_Pipe"
7. Right-click on Reactor → **3D Object → Cube** → Name it "Valve_Inlet"
8. Right-click on Reactor → **3D Object → Cube** → Name it "Valve_Outlet"

---

### Step 2: Add Scripts to GameObjects

#### 2.1 ReactorController (REQUIRED - Add First)

**GameObject:** `Reactor` (parent object)

1. Select the **Reactor** GameObject
2. Click **Add Component**
3. Search for **"Reactor Controller"**
4. Add the component

**Inspector Settings:**
| Field | What to Assign |
|-------|----------------|
| Inlet Valve | Drag `Valve_Inlet` GameObject here |
| Outlet Valve | Drag `Valve_Outlet` GameObject here |
| Agitator | Drag `Agitator` GameObject here |
| Water Simulator | Drag `Water` GameObject here |
| Tank Cutaway | Drag `Tank` GameObject here |
| Bubble Effect | Drag `Water` GameObject here (or child with particles) |
| Inlet Pipe Flow | Drag `Inlet_pipe` GameObject here |
| Outlet Pipe Flow | Drag `Outlet_Pipe` GameObject here |
| Simulation Data | Create and assign (see Step 3) |

**Quick Setup:** Right-click on ReactorController component → **"Auto Find Components"**

---

#### 2.2 ValveController (Add to BOTH valves)

**GameObjects:** `Valve_Inlet` AND `Valve_Outlet`

1. Select **Valve_Inlet**
2. Click **Add Component** → Search **"Valve Controller"**
3. Repeat for **Valve_Outlet**

**Inspector Settings for Valve_Inlet:**
| Field | Value |
|-------|-------|
| Valve Type | **Inlet** |
| Valve Handle | Drag the rotating part (or self) |
| Rotation Axis | (0, 1, 0) for Y-axis rotation |
| Open Rotation | 90 (degrees) |
| Transition Time | 0.5 |
| Valve Renderer | Drag the valve's MeshRenderer |
| Closed Color | Red |
| Open Color | Green |

**Inspector Settings for Valve_Outlet:**
| Field | Value |
|-------|-------|
| Valve Type | **Outlet** |
| (other settings same as above) |

---

#### 2.3 AgitatorController

**GameObject:** `Agitator`

1. Select **Agitator**
2. Click **Add Component** → Search **"Agitator Controller"**

**Inspector Settings:**
| Field | Value |
|-------|-------|
| Agitator Transform | Drag the rotating part (or self) |
| Rotation Axis | (0, 1, 0) for Y-axis |
| Max RPM | 300 |
| Spin Up Time | 2 |
| Spin Down Time | 3 |
| Motor Audio | (Optional) AudioSource for motor sound |

---

#### 2.4 WaterSimulator

**GameObject:** `Water`

1. Select **Water**
2. Click **Add Component** → Search **"Water Simulator"**

**Inspector Settings:**
| Field | Value |
|-------|-------|
| Water Mesh | Drag the Water transform itself |
| Scale Mode | **ScaleY** (recommended) |
| Min Scale | 0.01 |
| Max Scale | 1.0 |
| Tank Capacity | 1000 |
| Water Renderer | Drag Water's MeshRenderer |
| Use Material Property Block | ✓ Checked |

**Scale Mode Options:**
- **ScaleY** - Water scales vertically (Y-axis only)
- **ScaleXYZ** - Uniform scaling
- **MoveAndScale** - Moves position and scales
- **ShaderOnly** - Only updates shader, no mesh changes

---

#### 2.5 TankCutawayController

**GameObject:** `Tank`

1. Select **Tank**
2. Click **Add Component** → Search **"Tank Cutaway Controller"**

**Inspector Settings:**
| Field | Value |
|-------|-------|
| Cutaway Mode | **ShaderClipping** |
| Tank Renderer | Drag Tank's MeshRenderer |
| Cutaway Plane | (Optional) Empty transform for plane position |
| Clip Direction | (1, 0, 0) for X-axis cut |
| Transition Time | 0.5 |
| Smooth Transition | ✓ Checked |

**Cutaway Mode Options:**
- **ShaderClipping** - Uses shader to clip geometry (requires Tank Cutaway shader)
- **MeshHiding** - Hides front mesh, shows interior mesh
- **Combined** - Both methods

---

#### 2.6 BubbleEffectController

**GameObject:** `Water` (or create child object "BubbleEmitter")

1. Select **Water**
2. Click **Add Component** → Search **"Bubble Effect Controller"**

**Inspector Settings:**
| Field | Value |
|-------|-------|
| Bubble Particles | Drag ParticleSystem here (create in Step 5) |
| Foam Particles | (Optional) Secondary particle system |
| Min Emission Rate | 0 |
| Max Emission Rate | 100 |
| Fade Time | 1 |
| Min Bubble Size | 0.01 |
| Max Bubble Size | 0.05 |
| Base Upward Velocity | 0.5 |

---

#### 2.7 PipeFlowVisualizer (Add to BOTH pipes)

**GameObjects:** `Inlet_pipe` AND `Outlet_Pipe`

1. Select **Inlet_pipe**
2. Click **Add Component** → Search **"Pipe Flow Visualizer"**
3. Repeat for **Outlet_Pipe**

**Inspector Settings for Inlet_pipe:**
| Field | Value |
|-------|-------|
| Associated Valve Type | **Inlet** |
| Pipe Renderer | Drag pipe's MeshRenderer |
| Scroll Speed | 2 |
| Scroll Direction | (0, 1) - along pipe |
| Use Base Map Offset | ✓ Checked |
| Flow Particles | (Optional) ParticleSystem |
| Fade Time | 0.3 |

**Inspector Settings for Outlet_Pipe:**
| Field | Value |
|-------|-------|
| Associated Valve Type | **Outlet** |
| (other settings same as above) |

---

### Step 3: Create Simulation Data Asset

1. In Project window, right-click
2. Select **Create → Industrial Reactor → Simulation Data**
3. Name it "ReactorSimulationData"
4. Select it and configure in Inspector:

| Parameter | Default Value | Description |
|-----------|---------------|-------------|
| Configuration Name | "Default" | Name identifier |
| Simulation Time Scale | 1 | Speed multiplier |
| Tank Capacity | 1000 | Liters |
| Initial Water Level | 0 | 0-1 normalized |
| Valve Transition Time | 0.5 | Seconds |
| Default Inlet Flow Rate | 20 | L/s |
| Default Outlet Flow Rate | 15 | L/s |
| Max Inlet Flow Rate | 100 | L/s |
| Max Outlet Flow Rate | 80 | L/s |
| Default Agitator RPM | 60 | RPM |
| Max Agitator RPM | 300 | RPM |
| Spin Up Time | 2 | Seconds |
| Spin Down Time | 3 | Seconds |
| Ambient Temperature | 25 | °C |
| Min/Max Temperature | 0/100 | °C |

5. Drag this asset to **ReactorController → Simulation Data** field

---

## Shader Setup

### Step 4: Create Materials with Custom Shaders

#### 4.1 Water Material

1. Right-click in Project → **Create → Material**
2. Name it "WaterMaterial"
3. In Inspector, click **Shader** dropdown
4. Select **Industrial Reactor → Water**
5. Configure:

| Property | Value |
|----------|-------|
| Water Color | Light blue (0.2, 0.5, 0.8, 0.8) |
| Transparency | 0.7 |
| Fresnel Power | 2 |
| Swirl Speed | 0 (controlled by script) |
| Swirl Strength | 0.5 |
| Turbulence | 0 (controlled by script) |
| Emission Color | Soft blue HDR |
| Emission Intensity | 0 (controlled by script) |
| Smoothness | 0.9 |

6. Drag material to **Water** GameObject's MeshRenderer

---

#### 4.2 Tank Cutaway Material

1. Create new material named "TankMaterial"
2. Set shader to **Industrial Reactor → Tank Cutaway**
3. Configure:

| Property | Value |
|----------|-------|
| Color | Silver/metallic (0.8, 0.8, 0.85) |
| Metallic | 0.5 |
| Smoothness | 0.7 |
| Enable Clip | ✓ ON |
| Interior Color | Dark gray (0.3, 0.3, 0.35) |

4. Drag material to **Tank** GameObject's MeshRenderer

---

#### 4.3 Pipe Flow Material

1. Create new material named "PipeMaterial"
2. Set shader to **Industrial Reactor → Pipe Flow**
3. Configure:

| Property | Value |
|----------|-------|
| Pipe Color | Gray metallic |
| Metallic | 0.7 |
| Smoothness | 0.6 |
| Flow Color | Cyan/blue (0.3, 0.6, 1.0) |
| Flow Speed | 2 |
| Flow Intensity | 1 |
| Flow Emission | 1 |

4. Drag material to **Inlet_pipe** and **Outlet_Pipe** MeshRenderers

---

#### 4.4 Valve Material (Standard URP)

1. Create new material named "ValveMaterial"
2. Use default **Universal Render Pipeline/Lit** shader
3. Set Base Color to red (will change via script)
4. Drag to both valve MeshRenderers

---

## Particle System Setup

### Step 5: Create Bubble Particles

1. Select **Water** GameObject
2. Right-click → **Effects → Particle System**
3. Name it "BubbleParticles"
4. Configure these settings:

**Main Module:**
| Setting | Value |
|---------|-------|
| Duration | 5 |
| Looping | ✓ ON |
| Start Lifetime | 2 |
| Start Speed | 0.5 |
| Start Size | 0.02 |
| Gravity Modifier | -0.1 (float up) |
| Max Particles | 500 |

**Emission:**
| Setting | Value |
|---------|-------|
| Rate over Time | 0 (controlled by script) |

**Shape:**
| Setting | Value |
|---------|-------|
| Shape | Box or Sphere |
| Scale | Match water interior size |

**Color over Lifetime:**
- Add gradient: White (alpha 1) → White (alpha 0)

**Size over Lifetime:**
- Add curve: 0.5 → 1.0 (grow slightly)

**Renderer:**
| Setting | Value |
|---------|-------|
| Render Mode | Billboard |
| Material | Create simple white circle material |

5. Drag **BubbleParticles** to **BubbleEffectController → Bubble Particles** field

---

## Using the Editor Window

### Open the Tool

**Menu: Tools → Industrial Reactor Simulator**

### Interface Overview

```
┌─────────────────────────────────────┐
│    Industrial Reactor Simulator     │
├─────────────────────────────────────┤
│ ⚡ Simulation Controls              │
│  [Start Simulation] [Stop Simulation]│
│  [Reset Simulation]                 │
├─────────────────────────────────────┤
│ 🔧 Valve Controls                   │
│  Inlet:  [Open] [Close]  ● CLOSED   │
│  Outlet: [Open] [Close]  ● CLOSED   │
├─────────────────────────────────────┤
│ ⚙️ Agitator Controls                │
│  [Start Agitator] [Stop Agitator]   │
├─────────────────────────────────────┤
│ 📊 Control Parameters               │
│  Inlet Flow:  ═══════○═══  20 L/s   │
│  Outlet Flow: ═══════○═══  15 L/s   │
│  Agitator RPM:═══════○═══  60 RPM   │
│  Temperature: ═══════○═══  25 °C    │
├─────────────────────────────────────┤
│ 📈 Realtime Monitoring              │
│  Reactor State: [  RUNNING  ]       │
│  Tank Fill:     ████████░░  80%     │
│  Temperature:   [  45.2 °C  ]       │
│  Agitator RPM:  [  120 RPM  ]       │
│  Bubble:        [   40%     ]       │
└─────────────────────────────────────┘
```

### Operation Steps

1. **Enter Play Mode** (Press Play button or Ctrl+P)
2. Click **"Start Simulation"** - Tank cutaway activates
3. Click **"Open"** next to Inlet Valve - Water flows in
4. Watch the tank fill (progress bar updates)
5. Click **"Start Agitator"** - Mixing begins, bubbles appear
6. Adjust **Agitator RPM** slider - Changes mixing intensity
7. Click **"Open"** next to Outlet Valve - Water drains
8. Click **"Stop Agitator"** - Smooth slowdown
9. Click **"Stop Simulation"** - Returns to idle

---

## Scripting API

### Subscribe to Events

```csharp
using UnityEngine;
using IndustrialReactorSimulator;

public class MyReactorHandler : MonoBehaviour
{
    void OnEnable()
    {
        // Simulation events
        ReactorEvents.OnSimulationStarted += HandleSimulationStarted;
        ReactorEvents.OnSimulationStopped += HandleSimulationStopped;
        ReactorEvents.OnSimulationReset += HandleSimulationReset;
        
        // Water events
        ReactorEvents.OnWaterLevelChanged += HandleWaterLevel;
        ReactorEvents.OnWaterTemperatureChanged += HandleTemperature;
        ReactorEvents.OnTankFull += HandleTankFull;
        ReactorEvents.OnTankEmpty += HandleTankEmpty;
        
        // Valve events
        ReactorEvents.OnValveStateChanged += HandleValveState;
        ReactorEvents.OnInletValveOpened += HandleInletOpened;
        ReactorEvents.OnOutletValveClosed += HandleOutletClosed;
        
        // Agitator events
        ReactorEvents.OnAgitatorRPMChanged += HandleRPMChanged;
        ReactorEvents.OnAgitatorStarted += HandleAgitatorStarted;
        ReactorEvents.OnAgitatorStopped += HandleAgitatorStopped;
        
        // Effect events
        ReactorEvents.OnBubbleIntensityChanged += HandleBubbleIntensity;
    }

    void OnDisable()
    {
        // Unsubscribe all events
        ReactorEvents.OnSimulationStarted -= HandleSimulationStarted;
        ReactorEvents.OnWaterLevelChanged -= HandleWaterLevel;
        // ... unsubscribe others
    }

    void HandleWaterLevel(float level)
    {
        Debug.Log($"Water Level: {level * 100:F1}%");
    }

    void HandleRPMChanged(float rpm)
    {
        Debug.Log($"Agitator RPM: {rpm:F0}");
    }

    void HandleValveState(ValveType type, ValveState state)
    {
        Debug.Log($"{type} valve is now {state}");
    }
}
```

### Control Reactor via Script

```csharp
using UnityEngine;
using IndustrialReactorSimulator;

public class AutomatedReactor : MonoBehaviour
{
    void Start()
    {
        // Get reactor reference
        ReactorController reactor = ReactorController.Instance;
        
        // Start simulation
        reactor.StartSimulation();
        
        // Control valves
        reactor.OpenInletValve();
        reactor.CloseInletValve();
        reactor.OpenOutletValve();
        reactor.CloseOutletValve();
        
        // Control agitator
        reactor.StartAgitator();
        reactor.SetAgitatorRPM(150f);
        reactor.StopAgitator();
        
        // Set parameters
        reactor.InletFlowSpeed = 30f;
        reactor.OutletFlowSpeed = 20f;
        reactor.TargetTemperature = 50f;
        
        // Read state
        float waterLevel = reactor.CurrentWaterLevel;
        float temperature = reactor.CurrentTemperature;
        float rpm = reactor.CurrentAgitatorRPM;
        ReactorState state = reactor.CurrentState;
        
        // Reset
        reactor.ResetSimulation();
        
        // Stop
        reactor.StopSimulation();
    }
}
```

---

## Troubleshooting

### "Simulation won't start"
- ✅ Make sure you're in **Play Mode**
- ✅ Check ReactorController exists in scene
- ✅ Verify all component references are assigned

### "Water not filling"
- ✅ Check WaterSimulator has Water Mesh assigned
- ✅ Verify Scale Mode is set correctly
- ✅ Ensure inlet valve ValveController is configured
- ✅ Check inlet valve type is set to "Inlet"

### "Cutaway not working"
- ✅ Assign Tank Renderer to TankCutawayController
- ✅ Use **Industrial Reactor/Tank Cutaway** shader
- ✅ Enable Clip property in material
- ✅ Check Clip Direction vector

### "No bubble effects"
- ✅ Create ParticleSystem and assign to BubbleEffectController
- ✅ Ensure agitator is running (bubbles depend on RPM)
- ✅ Check particle emission rate settings

### "Shaders not found"
- ✅ Ensure URP is installed and configured
- ✅ Check Project Settings → Graphics → Render Pipeline Asset
- ✅ Shaders are in: `Packages/com.industrialreactor.simulator/Runtime/Shaders/`

### "Components not found in Add Component"
- ✅ Check package is properly installed
- ✅ Look under **Industrial Reactor** category
- ✅ Or search by name: "Reactor Controller", "Valve Controller", etc.

---

## File Locations

After installation, files are located at:

```
Packages/com.industrialreactor.simulator/
├── Runtime/
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
│   └── Shaders/
│       ├── ReactorWater.shader
│       ├── TankCutaway.shader
│       └── PipeFlow.shader
├── Editor/
│   └── ReactorSimulatorWindow.cs
└── Documentation~/
    └── SETUP_GUIDE.md
```

---

## Support

- **Repository:** https://github.com/kumaramsindishtech-web/Unity
- **Issues:** https://github.com/kumaramsindishtech-web/Unity/issues

---

## License

MIT License - See LICENSE file for details.
