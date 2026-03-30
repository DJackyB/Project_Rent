# Martian Audio

`Martian.Audio` is a reusable, default-disabled Unity audio package designed to stay inert until a project explicitly installs it.

## 5-Minute Setup

1. Embed or install `com.martian.audio`.
2. Create an `AudioCatalog` asset and add your cue definitions.
3. Create an `AudioSettingsData` asset and choose your default backend and bus volumes.
4. Add `AudioBootstrap` to a scene object, or call `AudioRuntimeInstaller.Install(catalog, settings)` from your own entrypoint.
5. Play cues through `AudioServices.Current`.

## Cue Naming

Use stable dotted ids so they survive refactors and cross-project moves:

- `bgm.main`
- `bgm.gameover`
- `ui.button.primary`
- `ui.invalid`
- `card.play`
- `economy.money.up`

Prefer semantic names over clip filenames. A cue can contain one or more clips and the backend will choose one at runtime.

## Writing a Custom Backend

1. Reference `Martian.Audio`.
2. Implement `IAudioBackendFactory` and `IAudioBackend`.
3. Register your factory with `AudioBackendRegistry.Register(factory)`.
4. Return `IsAvailable = false` when your plugin or runtime dependency is missing.

The installer will try:

1. The configured `preferredBackendId`
2. Any other registered available backend by priority
3. The built-in Unity backend
4. `NoOpAudioService`

## Reuse in Other Projects

- Embedded package: copy the `Packages/com.martian.audio` folder into another Unity project.
- Git package: publish this package folder to a repository and reference it through `manifest.json`.

The package does not read `Resources`, scan scenes, or create runtime objects until explicitly installed.

## Verifying Fallback After Removing an External Backend

1. Register a custom backend and set `preferredBackendId` to it.
2. Confirm installation selects that backend.
3. Remove that external backend package.
4. Reinstall with the same settings.
5. Confirm `AudioRuntimeInstaller` falls back to `unity` or `NoOpAudioService` without throwing.
