# Aether N-Body Simulation

A real-time 2D gravitational laboratory for exploring orbital systems, spiral galaxies, black holes, quasars, pulsars, stellar evolution, and controlled cosmic expansion.

Aether combines a Raylib-based simulation viewport with a dedicated WPF control panel. The viewport is embedded inside the control panel so the project behaves as one desktop application: the GPU-facing window is reserved for the universe, while controls remain in a clean, structured workspace.

> **Status:** Active development — phases 1, 2, and the foundations of phase 3 are implemented.

![Platform](https://img.shields.io/badge/platform-Windows-0078D6)
![.NET](https://img.shields.io/badge/.NET-8-512BD4)
![C%23](https://img.shields.io/badge/C%23-12-239120)
![Raylib](https://img.shields.io/badge/rendering-Raylib--cs-000000)
![WPF](https://img.shields.io/badge/interface-WPF-68217A)
![License](https://img.shields.io/badge/license-TBD-lightgrey)

## Overview

Aether is designed as an interactive numerical sandbox rather than a strict relativistic astrophysics solver. Its goal is to make large-scale gravitational behaviour understandable and visually compelling while keeping the simulation code explicit and extensible.

The application can represent:

- Planetary systems and orbital rings.
- Binary and compact-body systems.
- Dense asteroid belts.
- Spiral galaxies with four-arm structure.
- Multiple separated galaxies with possible encounters.
- A controlled Big Bang-inspired expansion phase.
- Black-hole tidal deformation and absorption effects.
- Quasar accretion-disk and jet visuals.
- Pulsar rotation and polar plasma jets.
- Accelerated stellar evolution from stars to pulsars or black holes.

## Features

### Simulation

- 2D camera with zoom and panning.
- Barnes–Hut quadtree gravity solver.
- Legacy naive $O(n^2)$ solver retained for comparison.
- Leapfrog integration for better long-term orbital behaviour than explicit Euler.
- Static and dynamic bodies.
- Frame-time protection through bounded physics timesteps.
- Camera culling: bodies outside the visible world rectangle are skipped during rendering.
- Adjustable simulation speed from `0.125x` to `32x`.

### Celestial body model

The current body taxonomy includes:

- `PlanetBody`
- `StarBody`
- `BlackHoleBody`
- `QuasarBody`
- `PulsarBody`
- `AsteroidBody`

Black holes expose tidal and event-horizon radii. Quasars extend black holes with jets. Pulsars expose spin speed and polar jet length.

### Visual effects

When a body approaches a black hole:

1. Its tidal absorption progress increases.
2. Its visible radius contracts.
3. A directional tail is rendered opposite the black hole.
4. Pixel-like fragments appear near the event horizon.
5. The body is eventually consumed.

These effects are artistic 2D approximations inspired by cinematic black-hole imagery; they are not a relativistic ray-tracing implementation.

## Physics model

### Gravity

For a body at position $\mathbf r_i$, the acceleration contributed by a body of mass $m_j$ is approximated as:

$$
\mathbf a_{ij} = G m_j \frac{\mathbf r_j - \mathbf r_i}{\left(\|\mathbf r_j - \mathbf r_i\|^2 + \varepsilon^2\right)^{3/2}}
$$

where:

- $G$ is the project-scale gravitational constant.
- $\varepsilon$ is a softening value that avoids singularities during close encounters.
- Positions, masses, and velocities are expressed in simulation units rather than SI units.

### Circular orbital velocity

Scene generation uses the circular-orbit approximation:

$$
 v_c = \sqrt{\frac{GM}{r}}
$$

This gives initial velocities that are coherent with the dominant mass and orbital radius instead of assigning one fixed speed to every body.

### Leapfrog integration

The active integrator follows a kick-drift-kick structure:

1. Apply half of the acceleration impulse to velocity.
2. Advance position using the updated velocity.
3. Recompute acceleration with the quadtree.
4. Apply the remaining half impulse.

Leapfrog is symplectic in its ideal form and generally preserves orbital structure better over long runs than a basic explicit Euler step.

### Barnes–Hut quadtree

The quadtree recursively partitions space and stores:

- Total mass per node.
- Centre of mass per node.
- One body index in leaf nodes.

If a distant node is sufficiently small compared with its distance to the evaluated body, its contents are approximated as one mass. The opening criterion is controlled by `Theta`:

$$
\frac{s}{d} < \theta
$$

A smaller $\theta$ improves accuracy but increases traversal cost. A larger $\theta$ is faster but more approximate.

## Simulation phases

### Phase 1 — structured multi-galaxy universe

`Universo Fase 1: galaxias` creates several separated galaxies. Each galaxy contains:

- A moving central quasar.
- Four logarithmic spiral arms.
- Stars distributed along the arms.
- Low-mass dust particles that reinforce the visual structure.
- Planetary systems around a subset of stars.
- A pulsar for compact-object events.
- Optional initial collision geometry controlled by probability.

### Phase 2 — controlled cosmic expansion

`Universo Fase 2: Big Bang` starts with already structured galaxies and adds a gentle radial expansion component. This is deliberately controlled rather than a physically complete cosmological model: it preserves readable spiral systems while allowing the galaxies to separate over time.

### Phase 3 — evolution and interaction

The current phase-three foundation provides:

- Simulation-speed control.
- Stellar age and lifetime.
- Progressive star-radius evolution.
- Massive stars becoming black holes.
- Lower-mass stars becoming pulsars.
- Body spawning and deletion.
- Body replacement between supported types.
- Selection and value inspection through the external panel.

## Architecture

```text
Aether_NBody_Simulation.slnx
├── Aether_NBody_Simulation/
│   ├── Program.cs               # Raylib loop, rendering, commands, camera
│   ├── PhysicsEngine.cs         # Leapfrog, gravity, lifecycle and effects
│   ├── QuadTreeNode.cs          # Barnes–Hut spatial partition
│   ├── Body.cs                  # Celestial body hierarchy
│   ├── GalaxyBuilder.cs         # Test scenes and universe generators
│   ├── SceneModels.cs           # Scene and body-kind definitions
│   ├── SimulationRepository.cs  # SQLite persistence
│   ├── ControlPipeServer.cs     # Local WPF-to-Raylib command channel
│   └── SimulationCommand.cs     # Shared command format
├── Aether_ControlPanel/
│   ├── MainWindow.xaml          # WPF control layout
│   ├── MainWindow.xaml.cs       # Controls and native Raylib hosting
│   └── App.xaml                 # Theme and control styles
└── README.md
```

## Technology stack

- **C# 12**
- **.NET 8**
- **Raylib-cs 8** for real-time 2D rendering and input.
- **WPF** for the external control interface.
- **Windows Forms hosting** to embed the native Raylib surface into the WPF window.
- **Dapper** for lightweight SQL mapping.
- **Microsoft.Data.Sqlite** for snapshots and saved simulations.
- **Parallel.For** for independent Barnes–Hut acceleration queries.

## Build and run

From the repository root:

```powershell
dotnet build Aether_NBody_Simulation.slnx -c Debug
dotnet run --project Aether_NBody_Simulation.slnx
```

The default launcher opens the WPF control panel. The Raylib simulation is embedded into the right side of the same window.

### Standalone Raylib modes

Run the viewer with the legacy embedded controls:

```powershell
dotnet run --project Aether_NBody_Simulation/Aether_NBody_Simulation.csproj -- --embedded-ui
```

Run the clean viewer for external-control development:

```powershell
dotnet run --project Aether_NBody_Simulation/Aether_NBody_Simulation.csproj -- --external-ui --hosted
```

Run the physics benchmark:

```powershell
dotnet run --project Aether_NBody_Simulation/Aether_NBody_Simulation.csproj -- --benchmark-quadtree
```

## Control panel workflow

The WPF panel provides:

- Start/restart simulation.
- Pause and resume.
- Simulation speed slider.
- Full scene catalog selection.
- Body type selection.
- Mass, radius, and world-coordinate input.
- Body spawning.
- Selection by world coordinates.
- Selected-body information.
- Deletion of the selected body.

The simulation and panel communicate through a local Windows named pipe. This avoids coupling the rendering loop to WPF controls while still allowing commands to be handled on the simulation thread.

## Performance notes

The simulation is CPU-bound by the physics workload before rendering becomes the only concern. The main costs are:

- Rebuilding the quadtree each physics step.
- Traversing the tree once per body.
- Multiple substeps at high simulation speed.
- Rendering many visible bodies.

The quadtree reduces the dominant interaction calculation from the naive all-pairs approach toward approximately $O(n\log n)$ in typical distributions, but it does not guarantee a fixed frame rate. High speed multipliers intentionally execute more simulated time and therefore require more CPU work.

## Development principles

- Keep scene generation deterministic with explicit random seeds.
- Use the same scale law for generated orbital velocities.
- Keep the physics timestep bounded.
- Separate rendering, simulation, persistence, and UI transport.
- Prefer inspectable, testable generation functions over hidden magic numbers.
- Treat visual black-hole effects as a presentation layer over the physical body state.

## Roadmap

- Rich inspector editing for position, velocity, mass, radius, and static state.
- More reliable native-host lifecycle and graceful shutdown.
- Collision and merger events for compact objects.
- Configurable galaxy-generator form.
- Multi-body selection and editing.
- More accurate stellar evolution stages.
- Optional cosmological expansion parameters.
- Automated physics regression tests.
- Release packaging for Windows.


## License

No license has been selected yet. Add a license before distributing the project publicly.
