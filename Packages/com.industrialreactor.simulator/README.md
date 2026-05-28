# Industrial Reactor Simulator

Professional Unity addon for industrial reactor simulation with custom editor UI controls.

## Features

- **Custom Editor Window** - Tools > Industrial Reactor Simulator
- **Real-time Simulation** - Play Mode only operation
- **Water Fill/Drain** - Smooth animated water level changes
- **Agitator Mixing** - RPM-based rotation with spin up/down
- **Bubble Effects** - Particle intensity linked to agitator speed
- **Tank Cutaway** - Shader-based interior visibility
- **Valve Controls** - Smooth open/close transitions
- **Pipe Flow** - Animated flow visualization
- **Temperature Simulation** - Heating/cooling dynamics

## Requirements

- Unity 6.x (6000.0+)
- Universal Render Pipeline (URP) 17.0+

## Installation

### Via Package Manager (Git URL)
1. Open Package Manager (Window > Package Manager)
2. Click + button > Add package from git URL
3. Enter: `https://github.com/kumaramsindishtech-web/Unity.git?path=Packages/com.industrialreactor.simulator`

### Via manifest.json
Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.industrialreactor.simulator": "https://github.com/kumaramsindishtech-web/Unity.git?path=Packages/com.industrialreactor.simulator"
  }
}
```

## Quick Start

1. Create reactor hierarchy in scene
2. Add ReactorController to parent object
3. Add component scripts to child objects
4. Enter Play Mode
5. Open Tools > Industrial Reactor Simulator

## Documentation

See the [Setup Guide](Documentation~/SETUP_GUIDE.md) for detailed instructions.

## License

MIT License - See LICENSE file for details.
