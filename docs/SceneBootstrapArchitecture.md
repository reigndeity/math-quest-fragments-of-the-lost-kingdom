# Scene Bootstrap Architecture

`Root` is the project entry scene. It contains the persistent `Bootstrapper` object and is the scene to open when testing the complete game. Always press Play from `Root`.

`GameBootstrapper` reads `GameBootstrapConfig` and loads its configured scenes additively, in order. A scene that is already loaded (such as `Root` itself) is skipped.

The bootstrap object survives scene changes (`DontDestroyOnLoad`), so it can later host genuinely global services. Domain state should still live in its own system rather than accumulating inside the bootstrapper.

## Why bootstrap

Without a persistent entry scene:

- Whichever scene is open when you press Play becomes the whole game, so testing depends on the editor's current state.
- Anything global must be duplicated in every scene or is missing when one is tested alone.
- Loading a new scene destroys everything in the old one, including objects that should persist.

With `Root`:

- Startup is identical every time.
- Global objects have a home that never unloads.
- Scenes are swappable pieces loaded and unloaded additively.
- The startup composition is data in an asset, not code.

This matters most for the planned design: one shared Battle scene reused for every level, with different content supplied per level. Something persistent has to sit above the scenes to carry that choice. That hand-off is not built yet.

## Files

All under `Math Quest/Assets/Math Quest/`:

| File | Purpose |
|---|---|
| `Scripts/Bootstrap/GameBootstrapper.cs` | Persistent loader living on `Root`. |
| `Scripts/Bootstrap/GameBootstrapConfig.cs` | ScriptableObject listing the scenes to load. |
| `Scripts/MathQuest.Runtime.asmdef` | Runtime assembly for all game code. |
| `Config/GameBootstrapConfig.asset` | The startup composition. |
| `Scenes/Root.unity` | Entry scene (Build Settings index 0). |

## Designer workflow

Open `Config/GameBootstrapConfig.asset` to change what loads at startup. Scene fields accept scene assets and store their project paths for runtime loading.

- Do not list `Root` in the config. It is already loaded.
- Enable **Make Active Scene** on at most one entry, so objects created without an explicit scene belong to that scene instead of `Root`.
- Every configured scene must be enabled in Build Settings, with `Root` at index 0. A missing scene logs an error and stops loading.

## Testing

Open `Root.unity` and enter Play Mode. The configured scenes appear in the Hierarchy under `Root` automatically. There is no need to open them additively by hand.

## Project setup

- Unity 6000.3.8f1, 2D URP, Android only.
- Landscape only: Default Orientation is Auto Rotation with only Landscape Left and Landscape Right allowed.
- UI Canvas Scaler: Scale With Screen Size, reference resolution 1920 x 1080, Match 0.5.

## Not built yet

Scene transitions at runtime, passing the selected level into the Battle scene, UI screen management, and game data definitions are intentionally deferred.
