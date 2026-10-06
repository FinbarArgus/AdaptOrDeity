# AdaptOrDeity

A physiology evolution game. Start as a single cell living by diffusion, then adapt your organism
(multicellularity, nutrient tubes, airways, circulation, thermoregulation, …) to colonise every region
of a changing planet. The physiology is quantitative, built from CellML modules in
[circulatory-autogen-modules](https://github.com/physiomelinks/circulatory-autogen-modules).

- Architecture and Stage-1 plan: [docs/architecture-plan.md](docs/architecture-plan.md)
- Conventions: [docs/conventions.md](docs/conventions.md)
- Design decisions: [docs/adr/](docs/adr/)

## Layout
| Path | What |
|---|---|
| `src/AdaptOrDeity.Sim.*` | Engine-agnostic simulation (netstandard2.1 + net8.0) |
| `src/AdaptOrDeity.Game.*` | Game rules, content, saves, the session facade |
| `src/AdaptOrDeity.Headless` | Console runner (scenarios, traces, benchmarks) |
| `tests/` | NUnit tests, including the dependency-rule tests |
| `tools/kernelgen` | CellML → C# kernel generator (Python, libcellml) |
| `unity/` | Unity 6 front end (presentation and input only) |
| `external/circulatory-autogen-modules` | CellML module library (git submodule) |

## Building
```bash
git submodule update --init
dotnet build -c Release
dotnet test -c Release
dotnet run --project src/AdaptOrDeity.Headless -- 3600      # simulate one hour headless
tools/scripts/build_sim_for_unity.sh                        # copy sim DLLs into the Unity package
```
Requires the .NET 8 SDK and Unity 6 (6000.3 LTS).
