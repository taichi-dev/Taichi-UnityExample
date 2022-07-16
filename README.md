# Taichi-UnityExample

This repository demonstrates Taichi-Unity interopability with sample scenes.

## Prerequisites

This Unity project is runnable out-of-the-box. You don't need to build taichi libraries because all the necessary native plugins are already included in `Assets/Plugins`, but in case you want to, consult [](https://github.com/taichi-dev/taichi) and [](https://github.com/taichi-dev/taichi-unity2) for building manual.

If you want to create another Unity Project with Taichi integration, you need to set your first-choice graphics API to Vulkan in *Player Settings* in Unity because currently Taichi C-API doesn't support other graphics APIs at the moment.

## Content

This repository presents several demo scenes, each can be found in `Assets/Scenes`:

- `Fractal`: A simple time-dependent texture generation kernel;
- `ImplicitFem`: A physically-based soft-body simulation emitting vertex data on-the-flight.

Also note that the project can be built into Unity Player.

## Notes for Android

Note that some player settings are changed for Android

- Minimum version is changed to 26 (Android Oreo), which is the version the native libraries are compiled;
- Scripting backend is switched to IL2CPP;
- Library arch is set to `ARM64`, support for `ARMv7` is canceled;
- Default orientation is fixed portrait.
