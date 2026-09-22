<div align="center">

# 🌌 Aether N-Body Simulation

### A real-time 2D laboratory for gravity, galaxies, compact objects, and cosmic evolution.

Explore orbital systems, spiral galaxies, quasars, pulsars, black holes, stellar evolution, and controlled cosmic expansion in an interactive Windows desktop application.

<br />

![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=for-the-badge&logo=windows)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet)
![C%23](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp)
![Raylib](https://img.shields.io/badge/rendering-Raylib--cs-000000?style=for-the-badge)
![WPF](https://img.shields.io/badge/interface-WPF-68217A?style=for-the-badge)

</div>

---

## ✨ What is Aether?

**Aether** is an interactive numerical sandbox built to make gravitational systems understandable, experimentable, and visually compelling.

It combines:

- A high-performance **Raylib simulation viewport**.
- A structured **WPF control panel** embedded in the same desktop window.
- A Barnes–Hut **quadtree solver** for large body counts.
- A Leapfrog integrator designed for more stable orbital motion.
- Procedural generators for planetary systems and multi-galaxy scenes.
- Visual effects for black-hole tides, spaghettification, quasars, pulsars, and stellar evolution.

> **Project status:** Active development — Phases 1, 2, and the foundations of Phase 3 are implemented.

## 🎯 Project goals

Aether is not intended to be a fully relativistic astrophysics engine. It is an educational and experimental simulation focused on:

1. Making gravitational behaviour visible.
2. Keeping the physics understandable and extensible.
3. Exploring the relationship between numerical models and visual perception.
4. Supporting both small orbital experiments and large procedural universes.

## 🪐 Current capabilities

| Area | Included |
| --- | --- |
| Simulation | 2D camera, zoom, panning, pause, adjustable simulation speed |
| Gravity | Barnes–Hut quadtree and legacy naive solver for comparison |
| Integration | Leapfrog kick–drift–kick integration |
| Bodies | Planets, stars, asteroids, black holes, quasars, pulsars |
| Galaxies | Spiral arms, dust, planetary systems, multi-galaxy scenes |
| Black holes | Tidal deformation, shrinking radius, directional tails, pixelation, absorption |
| Stellar evolution | Star aging, radius evolution, pulsar remnants, black-hole remnants |
| Interaction | Spawn, delete, select, replace, inspect and save simulation states |
| Persistence | SQLite snapshots through Dapper and Microsoft.Data.Sqlite |

## 🌠 Simulation phases

### Phase 1 — Structured multi-galaxy universe

The first phase creates several separated galaxies with coherent internal structure:

- A moving central **quasar**.
- Four visible logarithmic spiral arms.
- Stars distributed along the arms.
- Low-mass dust particles that reinforce the silhouette.
- Planetary systems around selected stars.
- Pulsars and compact-object events.
- Optional initial collision geometry.

The generator uses deterministic random seeds so a scene can be reproduced.

### Phase 2 — Controlled cosmic expansion

The Big Bang-inspired phase begins with already structured galaxies and applies a gentle radial expansion component.

This is intentionally a **visual and controllable cosmological model**, not a complete relativistic cosmology solver. The goal is to preserve recognizable spiral galaxies while allowing them to separate and evolve instead of exploding into unstructured particles.

### Phase 3 — Evolution and interaction

The current foundation includes:

- Simulation-speed control from `0.125x` to `32x`.
- Stellar age and configurable lifetime.
- Progressive star-radius evolution.
- Massive stars becoming black holes.
- Lower-mass stars becoming pulsars.
- Body spawning and deletion.
- Body replacement between supported types.
- Selection and value inspection through the control panel.

## 🧮 Physics model

### Gravitational acceleration

For body $i$, the softened acceleration contributed by body $j$ is approximated by:

$$
\mathbf{a}_{ij} = Gm_j\frac{\mathbf{r}_j - \mathbf{r}_i}
{\left(\lVert\mathbf{r}_j - \mathbf{r}_i\rVert^2 + \varepsilon^2\right)^{3/2}}
$$

where:

- $G$ is the project-scale gravitational constant.
- $m_j$ is the source mass.
- $\varepsilon$ softens close encounters and avoids singularities.
- Positions and velocities use simulation units rather than SI units.

The total acceleration of a body is the sum of the contributions accepted by the selected solver.

### Circular orbital velocity

Procedural scene generation uses the circular-orbit approximation:

$$
 v_c = \sqrt{\frac{GM}{r}}
$$

This produces initial velocities adapted to the dominant mass $M$ and orbital radius $r$, avoiding one fixed speed being incorrectly reused across different scales.

### Leapfrog integration

The active integrator follows a kick–drift–kick structure:

1. Apply half of the acceleration impulse to velocity.
2. Advance position using the updated velocity.
3. Rebuild the quadtree and recalculate acceleration.
4. Apply the remaining half impulse.

Leapfrog is symplectic in its ideal form and generally preserves orbital structure better over long simulations than explicit Euler integration.

### Barnes–Hut quadtree

The quadtree recursively partitions the world and stores:

- Total mass per node.
- Centre of mass per node.
- A body index in leaf nodes.

A distant node can be approximated as one concentrated mass when:

$$
\frac{s}{d} < \theta
$$

where $s$ is the node size and $d$ is its distance from the evaluated body.

- Smaller `Theta`: more accurate, more expensive.
- Larger `Theta`: faster, more approximate.

The legacy naive solver remains available for performance comparison and validation.

## 🕳️ Black holes, quasars, and pulsars

### Black holes

When a body enters a black hole's tidal radius:

1. Absorption progress increases.
2. The body contracts visually.
3. A tail stretches opposite the black hole.
4. Pixel fragments appear near the event horizon.
5. The body is eventually consumed.

These are artistic 2D approximations inspired by cinematic black-hole imagery. They do not model relativistic ray tracing, accretion-disk radiative transfer, or general relativity.

### Quasars

A `QuasarBody` extends the black-hole model with:

- A dark central silhouette.
- An elliptical accretion disk.
- Bright disk rings.
- Narrow opposing jets.

### Pulsars

A `PulsarBody` represents a rotating neutron star through:

- A luminous compact core.
- A surrounding halo.
- Two rotating triangular plasma beams.

## 🖥️ User interface

The application uses one WPF window containing:

- A left control panel for scenes, simulation state, body creation, and inspection.
- A right Raylib viewport dedicated to rendering the universe.

The panel supports:

- Scene selection.
- Pause and resume.
- Simulation speed control.
- Body type, mass, radius, and world-coordinate input.
- Body spawning.
- Body selection by world coordinates.
- Selected-body information.
- Deletion of selected bodies.

Communication between the control layer and the simulation layer uses a local Windows named pipe, keeping UI commands separate from the physics and rendering loop.

## 🏗️ Architecture

```text
Aether_NBody_Simulation.slnx
├── Aether_NBody_Simulation/
│   ├── Program.cs               # Raylib loop, rendering, camera, commands
│   ├── PhysicsEngine.cs         # Gravity, Leapfrog, lifecycle, tidal effects
│   ├── QuadTreeNode.cs          # Barnes–Hut spatial partition
│   ├── Body.cs                  # Celestial-body hierarchy and state
│   ├── GalaxyBuilder.cs         # Procedural scenes and galaxy generators
│   ├── SceneModels.cs           # Scene and body-kind definitions
│   ├── SimulationRepository.cs  # SQLite persistence
│   ├── ControlPipeServer.cs     # Named-pipe command receiver
│   └── SimulationCommand.cs     # Shared command format
├── Aether_ControlPanel/
│   ├── MainWindow.xaml          # WPF layout and visual style
│   ├── MainWindow.xaml.cs       # Controls and Raylib window hosting
│   ├── App.xaml                 # Theme and shared WPF styles
│   └── App.xaml.cs              # WPF application entry point
└── README.md
```

## 🧰 Technology stack

- **C# 12**
- **.NET 8**
- **Raylib-cs 8** — real-time 2D rendering.
- **WPF** — desktop control interface.
- **Windows Forms hosting** — native Raylib surface embedded into WPF.
- **Dapper** — lightweight SQL mapping.
- **Microsoft.Data.Sqlite** — local persistence.
- **Parallel.For** — parallel Barnes–Hut acceleration queries.

## 🚀 Build and run

### Requirements

- Windows.
- .NET 8 SDK.
- A graphics driver supporting the Raylib OpenGL backend.

### Recommended launch

From the repository root:

```powershell
dotnet build Aether_NBody_Simulation.slnx -c Debug
dotnet run --project Aether_NBody_Simulation.slnx
```

The standard launch opens the WPF control window with Raylib embedded in the simulation viewport.

### Additional modes

Run the legacy embedded Raylib interface:

```powershell
dotnet run --project Aether_NBody_Simulation/Aether_NBody_Simulation.csproj -- --embedded-ui
```

Run the clean Raylib viewer for external-control development:

```powershell
dotnet run --project Aether_NBody_Simulation/Aether_NBody_Simulation.csproj -- --external-ui --hosted
```

Run the physics benchmark:

```powershell
dotnet run --project Aether_NBody_Simulation/Aether_NBody_Simulation.csproj -- --benchmark-quadtree
```

## 📊 Performance notes

The simulation is primarily CPU-bound. The largest costs are:

- Rebuilding the quadtree each physics step.
- Traversing the tree for every body.
- Additional substeps at high simulation speed.
- Rendering large numbers of visible bodies.

The quadtree moves the dominant interaction calculation from the naive all-pairs approach toward approximately $O(n\log n)$ for typical distributions. It is an approximation, not a guarantee of constant frame time.

Increasing the simulation speed intentionally increases the amount of simulated time processed per rendered frame, so high multipliers require more CPU work.

## 📁 Development principles

- Keep scene generation deterministic with explicit seeds.
- Generate orbital velocities using the same scale law as the physics model.
- Bound the physics timestep for numerical stability.
- Separate simulation, rendering, persistence, and UI transport.
- Prefer explicit and testable generation functions over hidden magic values.
- Treat black-hole effects as a presentation layer driven by physical state.
- Validate performance with repeatable benchmark scenes.

## 🗺️ Roadmap

- Full inspector editing for position, velocity, mass, radius, and static state.
- More robust native-host lifecycle and graceful shutdown.
- Compact-object collision and merger events.
- Configurable galaxy-generator forms.
- Multi-body selection and editing.
- More detailed stellar evolution stages.
- Optional cosmological expansion parameters.
- Automated physics regression tests.
- Windows release packaging.

## 🔎 Recommended GitHub topics

```text
csharp dotnet dotnet8 raylib raylib-cs wpf n-body-simulation
gravity-simulation astrophysics computational-physics barnes-hut
quadtree leapfrog-integrator galaxy-simulation black-hole quasar
pulsar numerical-simulation desktop-application
```

## Repository description

> Interactive C#/.NET 2D N-body laboratory with Raylib and WPF: Barnes–Hut gravity, Leapfrog integration, spiral galaxies, quasars, pulsars, black-hole effects, stellar evolution, and multi-galaxy expansion.

## 🤝 Contributing

Aether is structured as an experimental and educational project. Contributions are welcome, especially in:

- Numerical stability.
- Profiling and performance.
- Procedural galaxy generation.
- Visualization techniques.
- Automated physics tests.
- UI/UX improvements.

When proposing a change, include the affected model, the expected physical or visual effect, and a reproducible test scene when possible.

## 📄 License

No license has been selected yet. Add a license before distributing the project publicly.

<div align="center">

### Built for curiosity, experimentation, and the beautiful chaos of gravity. ✨

</div>
