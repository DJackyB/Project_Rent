# Minimal Setup Sample

This sample keeps the package inert until you explicitly install it.

## Included

- `MinimalAudioSampleController.cs`
- a suggested cue list you can mirror into your own `AudioCatalog`
- a suggested setup flow for `AudioBootstrap`

## Suggested Cue Ids

- `bgm.main`
- `sfx.confirm`
- `ui.click`

## How To Use

1. Import this sample into a sandbox project or a temporary scene.
2. Create an `AudioCatalog` and `AudioSettingsData`.
3. Add a `GameObject` with `AudioBootstrap`.
4. Assign the catalog and settings.
5. Add `MinimalAudioSampleController` to any object in the scene.
6. Press `M` for music, `Alpha1` for confirm, and `Alpha2` for click.

No project-side game integration is required.
