---
name: context-settings-controls-binding
description: Settings: Controls had TWO independent faults, either alone fatal — its fields sat in the #if USE_UI_NGUI branch so BindElements skipped them, and even bound, nothing raises a change event for a toolkit toggle. The audit both faults imply for every other migrated panel.
metadata:
  type: repo
  repo: game-lib-games-ui
  path: Assets/Code/Libs/game-lib-games-ui
  created: 2026-08-31
---

# Settings: Controls — bound to nothing, and deaf as well

Device report: vibrate and left/right do nothing on the controls page. `8e5333a`.
**Two independent faults.** Fixing either one alone still leaves a dead page, which is
probably why it survived.

## Fault 1 — the fields could not bind

```csharp
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UICheckbox checkboxControlsVibrate;
#else
    public Engine.UI.UIRef checkboxControlsVibrate;   // <-- never compiled
#endif
```

**This project compiles the `#if`.** `USE_UI_NGUI_2_7` is defined *alongside*
`USE_UI_TOOLKIT` on every platform — check `ProjectSettings.asset`. So the field is a
`UICheckbox`, and `BindElements` starts with

```csharp
if (field.FieldType != typeof(UIRef)) { continue; }
```

All three were skipped. It warns (`BindElements: unresolved field ...`) and carries on.

**The fix is to declare the UIRef fields UNCONDITIONALLY**, under their own names
(`toggleControls*`), with the legacy `UICheckbox` fields left inside the `#if` for the NGUI
path. Both branches are live at once during a migration — the legacy prefab is still
instantiated, `SuppressLegacyView` only hides it — so `SyncCheckedState` writes to both and
a dead `UIRef` no-ops.

Initialise them to `UIRef.none`, not null. Every backend op no-ops on a ref that is not
alive, so a missed bind degrades to "nothing happens" instead of an NRE.

This is the `ngui-if-branch-fields-cannot-bind` rule. **Audit every panel that has an
`#if USE_UI_NGUI ... #else <UIRef> #endif` field mirror** — the `#else` half is dead code
here and always has been.

## Fault 2 — nothing would have raised the event either

The panel listened on `CheckboxEvents.EVENT_ITEM_CHANGE`. That is broadcast by
`CheckboxEvents`, a **MonoBehaviour riding the NGUI checkbox's own GameObject**. A toolkit
toggle is a `VisualElement` and has no GameObject to carry one. Full mechanism in
`game-lib-engine/contexts/context-ui-control-change-events.md`, which added
`UIUtil.SetToggleHandlerChange` / `SetSliderHandlerChange`.

Register them in an override of **`BindElements`** — the continuation the async `LoadView`
runs once the elements are real, and after `base.BindElements` has filled the fields. Not
`Init`, not `OnEnable`; the view does not exist at either.

## Compare against the ELEMENT name, never a field's own

The old handler did

```csharp
if (checkboxName == checkboxControlsHandedRight.name) {
```

unconditionally. Under the toolkit that legacy field may be unwired — an **NRE inside a
Messenger callback, which drops every other listener on that event with it**. One dead panel
becomes several.

Both routes now land in one `HandleControlChange(string, bool)` that compares against
`checkboxName{Vibrate,HandedLeft,HandedRight}` string constants, returns quietly on a name
that is not ours (the Messenger event is global), and only then syncs and saves.

## The bind manifest keys on FIELD names

`Resources/ui/binds/<viewKey>.json` maps **field name -> element name**:

```json
"toggleControlsVibrate": "CheckboxControlsVibrate"
```

Rename a field and the manifest must follow, or the bind silently falls back to convention
(field name, then kebab-case) and then warns. The element names are deliberately the same
strings as the NGUI GameObject names, which is what lets one handler serve both routes.

## Also in this file

- The guard was `#if ENABLE_FEATURE_SETTINGS_AUDIO` on the **controls** panel. Harmless today
  (both defines ship on every platform) but wrong; now `ENABLE_FEATURE_SETTINGS_CONTROLS`.
- The indicator-size slider, normalised `0..1` over `[GameIndicatorConfigs.scaleMin, scaleMax]`
  because the bitty view's `Slider` carries no authored range. The profile is the record,
  `GameIndicatorConfigs.scale` is the live dial the indicators read, and **nothing else copies
  one to the other** — so `Init` applies it too, or a player who set it last session would not
  have it applied this one.

## Verified

Live round, driving the elements directly: vibrate writes the profile; left-handed writes it
**and** unchecks right; the slider writes both the profile and the live config. Profile values
restored afterwards.

## Related

- `game-lib-engine/contexts/context-ui-control-change-events.md` — the backend half
- workspace `ui-toolkit/context-panel-settings-controls-spec.md` — the layout half
