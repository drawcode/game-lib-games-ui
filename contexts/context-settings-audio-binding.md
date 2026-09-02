---
name: context-settings-audio-binding
description: "Settings: Audio carried the same two faults the controls page did — its slider fields sat in the #if USE_UI_NGUI branch so BindElements skipped them, and nothing raised a change event for a toolkit slider — plus a third: the view authored a flat 0.8 and nothing ever synced the profile into it. Also the panel-by-panel result of the audit the controls page asked for."
metadata:
  type: repo
  repo: game-lib-games-ui
  path: Assets/Code/Libs/game-lib-games-ui
  created: 2026-09-02
---

# Settings: Audio — the same trap, one page over

`context-settings-controls-binding` ended by asking for an audit of every migrated panel
carrying an `#if USE_UI_NGUI … #else <UIRef> #endif` field mirror. Audio is the first thing
that audit turned up, and it had **all three** of the faults that page could have.

## Fault 1 — the fields could not bind

```csharp
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UISlider sliderAudioMusicVolume;
    public UISlider sliderAudioEffectsVolume;
#else
    public Engine.UI.UIRef sliderAudioMusicVolume;   // dead: this project compiles the #if
#endif
```

Identical to the controls page. `BindElements` binds only fields whose type is **exactly**
`UIRef`, so both were skipped and warned. The old file's own comment claimed "the `#else` UIRef
fields bind to the bitty view's SliderAudioMusicVolume / SliderAudioEffectsVolume" — that
sentence was never true here.

## Fault 2 — nothing would have raised the event either

The panel listens on `SliderEvents.EVENT_ITEM_CHANGE`, broadcast by a MonoBehaviour riding the
NGUI slider's GameObject. A toolkit slider is a `VisualElement`. Registered
`UIUtil.SetSliderHandlerChange` in a `BindElements` override instead, exactly as controls does.

## Fault 3 — and the page never showed the saved volume

`panel-settings-audio.json` authors **`"value": 0.8`** on both sliders and nothing copied the
profile into them. So the page opened at 0.8 whatever the player had chosen, and the first drag
from that position wrote a value they never picked. `SyncSliderValues` now runs from
`BindElements` **and** `HandleShow` — re-shows reuse an already-bound view, so `BindElements`
does not run a second time.

## Write through `GameAudio.SetProfile*Volume`, not the profile setter

The old handler did `GameProfiles.Current.SetAudio*Volume(v)` then an unconditional
`GameState.SaveProfile()`. A drag broadcasts a change **per frame** and a full profile save is
50-66 ms on the main thread (see `game-lib-games/contexts/context-profile-save-cost.md`), so
that path would have dropped frames for the length of every drag. `GameAudio.SetProfileAmbienceVolume`
/ `SetProfileEffectsVolume` apply the volume live **and** skip the save when the value is
unchanged.

## Naming: rename the toolkit field, don't reuse the NGUI one

The NGUI names stay on the prefab's serialised fields, so the new refs are
`sliderAudioMusic` / `sliderAudioEffects` and the bind manifest maps those to the same
`SliderAudioMusicVolume` / `SliderAudioEffectsVolume` elements. `BaseGameUIPanelLoader` had
already reached for the same trick independently (`labelLoadingRef`, `sliderProgressRef`) —
that is the convention to follow.

**Anyone fixing one of these must update `Resources/ui/binds/<view>.json` too**: the manifest is
keyed on FIELD name, so renaming the field without the manifest silently falls through to the
name conventions and usually misses.

## The audit, for the 27 panels that have a toolkitViewKey

Buttons are **not** affected: clicks dispatch by name (`UIUtil.IsButtonClicked` compares
`.name`) and the NGUI prefab is still instantiated, so a `UIImageButton` field still matches.
Only **value writes and change events** break. What is left:

| panel | field | state |
| --- | --- | --- |
| `BaseGameUIPanelSettingsAudio` | both sliders | **fixed here** |
| `BaseGameUIPanelSettingsControls` | three toggles | fixed, iter 9 |
| `BaseGameUIPanelWorlds` | title, description | already worked around by name (`isToolkitPanel` branch) |
| `BaseGameUIPanelHeader` | `labelSection` | already worked around (`ResolveDeep`) |
| `BaseGameUIPanelLoader` | `labelLoading`, `sliderProgress` | working `*Ref` twins exist; the dead mirror is just clutter |
| `BaseGameUIPanelSettingsProfile` | `inputProfileName` | **OPEN — and bigger than the others** |
| `BaseGameUIPanelResults` | `labelContentStateDisplayName` | **OPEN, different shape**: `panel-results.json` has no element for it at all |

### The profile-name input is the one that still needs engine work

`inputProfileName` is a `UIInput`, so `ChangeUsername`'s `UIUtil.SetInputValue` writes the
suppressed NGUI widget, and `OnProfileInputChanged` compares against that widget's `.name`.
Binding it is the easy half. The hard half is that `IUIBackend` still has **no text-field change
API** — iter 9 added `SetToggleHandlerChange` and `SetSliderHandlerChange` and stopped there. So
this one needs an engine change first, not just a rename.

## Verified

`Assembly-CSharp` recompiles clean, console clear, and all six new symbols are present in
`Library/ScriptAssemblies/Assembly-CSharp.dll`. **Not** verified live: this Editor session never
gets past the content-sync stage of boot (`GameUISceneRoot.OnContentSyncShipContentSuccess`
never fires, so `GameSceneDynamic` never loads and no menu panel is ever instantiated), and a
synthetic panel probe fights the panel lifecycle — it parks itself inactive, which frees the
view before the async load can bind. The wiring is the same shape the controls page was verified
on in iter 9.

## Related

- `context-settings-controls-binding.md` — the same two faults, one page over
- `game-lib-engine/contexts/context-ui-control-change-events.md` — the backend half
- `game-lib-games/contexts/context-audio-boot-volume-swap.md` — the device-only boot bug found alongside
