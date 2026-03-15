# Equirectangular Skybox Demo

A complete, runnable MonoGame sample project that demonstrates an **equirectangular skybox renderer** for a space game.
The skybox is rendered from the inside of a procedurally-generated unit sphere using a custom HLSL shader, and a
procedural star-field texture is generated at runtime so no external assets are required.

---

## Controls

| Key / Input | Action |
|---|---|
| **W / A / S / D** | Move forward / left / backward / right |
| **Mouse** | Look around (captured automatically) |
| **ESC** | Quit |

---

## How to Build & Run

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- **Windows**: No extra setup needed — the MonoGame shader compiler (`mgfxc`) runs natively.
- **Linux / macOS**: Wine is required by the MonoGame shader compiler.  
  Run the [MonoGame Wine setup script](https://monogame.net/downloads/net9_mgfxc_wine_setup.sh) once:
  ```bash
  wget -qO- https://monogame.net/downloads/net9_mgfxc_wine_setup.sh | bash
  ```

### Build & Run

```bash
dotnet restore
dotnet run
```

Or for a release build:

```bash
dotnet build --configuration Release
dotnet run --configuration Release
```

---

## Using a Real Equirectangular Panorama

The project ships with a procedurally-generated star-field texture.  To replace it with a real HDRI panorama:

1. Download a free 2:1 equirectangular HDRI from [Poly Haven](https://polyhaven.com/hdris) or
   [NASA SVS](https://svs.gsfc.nasa.gov/4851) (e.g. a Milky Way panorama).
2. Convert it to a PNG or JPEG (any tool — `ffmpeg`, Photoshop, GIMP, etc.).
3. Add it to `Content/` and reference it in `Content.mgcb`:
   ```
   /importer:TextureImporter
   /processor:TextureProcessor
   /build:Textures/MySkyPanorama.png
   ```
4. In `Game1.cs`, replace the `GenerateSpaceTexture(...)` call with:
   ```csharp
   _skyTexture = Content.Load<Texture2D>("Textures/MySkyPanorama");
   ```

The shader expects a standard 2:1 (width:height) equirectangular image where:
- Left/right edges map to the same longitude (the texture wraps horizontally).
- Top edge is the north pole, bottom edge is the south pole.

---

## How It Works

### Equirectangular Projection

An equirectangular image maps the full sphere onto a flat rectangle:

```
u = atan2(dir.z, dir.x) / (2π) + 0.5   ← longitude (wraps 0–1)
v = -dir.y * 0.5 + 0.5                  ← latitude  (top=0, bottom=1)
```

The vertex shader passes each sphere vertex's position as a **view direction** to the pixel shader,
which then converts that direction to UV coordinates and samples the panorama texture.

### Camera-at-Origin Approach

For space games the camera is always at the origin of the skybox sphere.  The renderer strips the
translation component from the view matrix before multiplying by projection, so no matter how far
the player travels, the sky never moves.  This eliminates floating-point precision issues that arise
when the camera is far from the world origin.

### Render Order

1. `GraphicsDevice.Clear(Color.Black)` — clear colour + depth.
2. Draw skybox with **depth writes disabled** (`DepthStencilState.None`) and
   **clockwise culling** (rendering from inside the sphere).
3. Renderer calls `GraphicsDevice.Clear(ClearOptions.DepthBuffer …)` to reset depth.
4. Draw all scene geometry — it will always appear in front of the sky.

---

## Project Structure

```
EquirectangularSkyboxDemo/
├── Content/
│   ├── Content.mgcb                        ← MonoGame content pipeline config
│   └── Effects/
│       └── EquirectangularSkybox.fx        ← Custom HLSL skybox shader
├── .github/
│   └── workflows/
│       └── build.yml                       ← CI workflow (uses MonoGame actions for Wine)
├── Game1.cs                                ← Main game class
├── QuaternionCamera.cs                     ← FPS camera (yaw/pitch quaternion)
├── EquirectangularSkyboxRenderer.cs        ← Skybox renderer
├── SphereMesh.cs                           ← Procedural unit-sphere mesh generator
├── Program.cs                              ← Entry point
└── EquirectangularSkyboxDemo.csproj
```

---

## CI / GitHub Actions

The workflow in `.github/workflows/build.yml` uses the
[MonoGame GitHub Actions](https://github.com/MonoGame/monogame-actions) to install Wine on the
Ubuntu runner before building.  Wine is required because the MonoGame shader compiler (`mgfxc`)
is a Windows binary that runs through Wine on Linux.

```yaml
- uses: MonoGame/monogame-actions/install-wine@v1
- uses: MonoGame/monogame-actions/install-fonts@v1
```
