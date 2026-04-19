# Audio Cue Workflow

Status: implemented.

This document explains how designers and engineers add a new audio cue and trigger it in gameplay or UI.

## Runtime Source Of Truth

The runtime source of truth is:

`Assets/Data/Audio/AudioCatalog.asset`

The catalog is edited through the custom Inspector provided by the reusable package:

`Packages/com.martian.audio/Editor/AudioCatalogEditor.cs`

Do not create a second project-specific audio table for the current workflow. The `AudioCatalog` asset is the catalog that runtime code reads.

## Designer Workflow

1. Select `AudioCatalog.asset` in the Project window.
2. Click `Add Cue` to create a blank cue, or select one or more `AudioClip` assets and click `Add Selected AudioClips`.
3. Set `Cue ID` to a stable semantic id, for example `sfx.card.destroy`, `ui.error`, or `bgm.shop`.
4. Set `Bus`:
   - `Music` for BGM.
   - `Sfx` for gameplay sound effects.
   - `Ui` for button, panel, and UI feedback sounds.
5. Drag one or more clips into `Clips`.
6. Configure optional fields:
   - `Base Volume` for cue-level volume.
   - `Pitch Min` / `Pitch Max` for variation.
   - `Cooldown` to avoid repeated spam.
   - `Loop` for looping music or ambient sounds.

The Inspector warns when a cue id is empty or duplicated. Duplicate ids are risky because the runtime lookup keeps the last cue with that id.

## Triggering A Cue In Code

For one-shot SFX or UI audio:

```csharp
using Martian.Audio;

AudioServices.Current.Play(AudioPlayRequest.Create("sfx.example"));
```

For BGM:

```csharp
using Martian.Audio;

AudioServices.Current.PlayMusic("bgm.example");
```

## Triggering A Cue From A Button

For simple button click audio:

1. Add `AudioOnClick` to the Button GameObject.
2. Set `_cueId` to the cue id registered in `AudioCatalog.asset`.
3. Make sure the scene has `[Audio]` with `AudioBootstrap` and `AudioEventBridge`.

Current examples:

- `Canvas/PhaseButton` uses `ui.button`.
- `Canvas/GameOverPanel/Panel/PlayAgainButton` uses `ui.button`.

## Triggering A Cue From Gameplay Events

For gameplay events, prefer adding the mapping to:

`Assets/Scripts/Runtime/Integration/Martian/Audio/AudioEventBridge.cs`

This keeps audio routing centralized and avoids scattering direct `AudioServices` calls across gameplay systems.

Current mappings:

| Event | Cue |
|---|---|
| `GameEvents.CardPlayed` | `sfx.card.play` |
| `GameEvents.TurnStarted` | `sfx.turn.start` |
| `GameEvents.CardRewardOffered` | `sfx.reward.show` |
| `GameEvents.CardRewardSelected` | `sfx.reward.pick` |
| `GameEvents.MoneyChanged` with positive delta | `sfx.coin.gain` |
| `GameEvents.MoneyChanged` with zero or negative delta | `sfx.coin.lose` |
| `GameEvents.GameOver` | `bgm.result` |
| `AudioEventBridge.Start` | `bgm.main` |

## Validation

Minimum validation after adding a cue:

1. Confirm `AudioCatalog.asset` has the cue id and at least one valid clip.
2. Enter Play Mode and trigger the action.
3. Check the Unity Console for missing cue or missing clip warnings.
4. Confirm the cue plays on the expected bus so volume controls affect it correctly.

## 当前仓库备注

- 当前主干里 `Assets/Data/Audio/AudioCatalog.asset` 与 `Assets/Data/Audio/AudioSettings.asset` 已存在，不需要重复创建。
- `Assets/Scenes/SampleScene.unity` 已挂 `[Audio]` 节点，可直接作为新增 cue 的第一验证场景。
