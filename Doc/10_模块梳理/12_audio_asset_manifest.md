# Audio Asset Manifest

This file records the current audio cue-to-asset mapping for the project.

License source:
- Mixkit License: https://mixkit.co/license/

Notes:
- The current build uses Mixkit-hosted MP3 files downloaded into the project.
- BGM uses direct `music/...mp3` URLs exposed by Mixkit pages.
- SFX currently uses the preview MP3 URLs exposed by Mixkit item pages.
- If we later want higher-quality replacements, we can swap files without changing cue ids.

## Cue Mapping

| Cue ID | Local File | Source Title | Source Page |
|---|---|---|---|
| `bgm.main` | `Assets/Audio/BGM/bgm.main.mp3` | `Games Music` | https://mixkit.co/free-stock-music/mood/energetic/ |
| `bgm.result` | `Assets/Audio/BGM/bgm.result.mp3` | `Silent Descent` | https://mixkit.co/free-stock-music/cinematic/ |
| `sfx.card.play` | `Assets/Audio/SFX/sfx.card.play.mp3` | `Digital sweep effect` | https://mixkit.co/free-sound-effects/woosh/ |
| `sfx.coin.gain` | `Assets/Audio/SFX/sfx.coin.gain.mp3` | `Winning a coin, video game` | https://mixkit.co/free-sound-effects/coin/ |
| `sfx.coin.lose` | `Assets/Audio/SFX/sfx.coin.lose.mp3` | `Player losing or failing` | https://mixkit.co/free-sound-effects/wrong/ |
| `sfx.turn.start` | `Assets/Audio/SFX/sfx.turn.start.mp3` | `Success software tone` | https://mixkit.co/free-sound-effects/tones/ |
| `sfx.reward.show` | `Assets/Audio/SFX/sfx.reward.show.mp3` | `Unlock game notification` | https://mixkit.co/free-sound-effects/coin/ |
| `sfx.reward.pick` | `Assets/Audio/SFX/sfx.reward.pick.mp3` | `Correct answer reward` | https://mixkit.co/free-sound-effects/correct/ |
| `ui.button` | `Assets/Audio/SFX/ui.button.mp3` | `Select click` | https://mixkit.co/free-sound-effects/interface/ |
