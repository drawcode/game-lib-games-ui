---
name: context-header-panel-seams
description: BaseGameUIPanelHeader's two migration seams — the shared CharacterLarge cluster (two views, two bands) and the #if USE_UI_NGUI field trap that makes a bind manifest entry silently do nothing
metadata:
  type: repo
  repo: game-lib-games-ui
  path: .
  created: 2026-08-27
---

# Header panel seams (UI Toolkit migration)

`BaseGameUIPanelHeader` owns more of the game's screens than its name suggests. Two things learned
the expensive way live here.

## 1. The CharacterLarge cluster belongs to the HEADER, not to any panel

The dark card, the posed 3D bot and the CUSTOMIZE/CHANGE BOT button that appear on the customize,
game-mode and equipment screens are all children of
`panel-header/.../CharacterLarge/ContainerCharacterLarge`. A screenshot attributes them to whatever
panel is up, which is wrong and shaped a whole wave before it was caught: **probe ownership before
assuming a panel owns what you can see on it.**

Staging flips the rig's layers, so it must be done ONCE by the owner. If individual panels staged
it, every screen showing the character would fight over those layers.

### Two views, because the cluster straddles the panel in z

    panel-character-large        UILayers.backdrop   (50)     the card
    panel-character-large-front  UILayers.foreground (9000)   the bot stage + the button

A toolkit view composites as ONE unit at ONE sort order. Legacy draws the card behind a screen's
content and the bot + button in front of it — visible in the coop baseline, where both overlap the
green mode buttons while the card does not. Shipped first as a single backdrop view; arcade and
customize-character both looked correct that way (nothing overlaps there) and only coop exposed it,
with the button invisible AND untappable because staging suppresses its legacy collider.

`LoadCharacterLargeView` loads both halves through one parameterised `LoadCharacterLargePart`
(key + band + get/set delegates) rather than a copied method — the in-flight-orphan handling is
the part that must not drift between them.

### The migration seam

`UIPanelBase.HandleCharacterDisplay()` is the single policy point; its CharacterLarge branch calls
`SetCharacterLargeToolkit(isToolkitMigrated)` BEFORE `ShowCharacterLarge()`. A migrated screen gets
the converted card; an unmigrated one keeps the legacy NGUI rig untouched (a toolkit view would
bury its NGUI content). Once every CharacterLarge screen is converted the flag is always true.

Use `isToolkitMigrated`, never `isToolkitPanel`: the latter only becomes true once the async view
build lands, so asking it during `AnimateIn` answers false on every first show — a one-show flicker
that is very hard to attribute.

Screens with `characterDisplayState == CharacterLarge`: GameMode, GameModeArcade, GameModeCoop,
GameModeChallenge, GameModeCustomize, CustomizeCharacter, CustomizeLevels, CustomizeWorlds,
Equipment. Each one migrated after the cluster costs no card work at all.

## 2. A field declared inside `#if USE_UI_NGUI` can NEVER be bound

At the top of this class:

```csharp
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonCoins;
    public UIImageButton buttonBack;
    public UILabel labelSection;
#else
    public Engine.UI.UIRef buttonCoins;   // ...
#endif
```

**That first branch is the one actually compiled here.** So `labelSection` is a legacy `UILabel`,
`BindElements` cannot rebind it however correct `binds/panel-header.json` looks, and every write
through it lands on the NGUI widget `SuppressLegacyView` has already hidden. The failure is silent:
the element just renders empty.

This is why the header band TITLE rendered blank on every migrated screen for several iterations
and was repeatedly written off as cosmetic. It is the same bug as the worlds
`labelWorldTitle`/`labelWorldDescription` pair.

`buttonBack` and `buttonCoins` are on the same unbindable side — they only work because clicks
bridge by element NAME, not through the ref.

**The fix pattern.** Branch on `isToolkitPanel` and write by element name, and CACHE the value so
it can be replayed:

```csharp
public virtual void showTitle(string title) {
    toolkitTitle = title;                       // replayed from SuppressLegacyView
    if(isToolkitPanel) {
        Engine.UI.UIRef label = UIUtil.ResolveDeep(viewRoot, "LabelSection");
        UIUtil.ShowLabel(label);
        UIUtil.SetLabelValue(label, title);
    }
    UIUtil.ShowLabel(labelSection);
    UIUtil.SetLabelValue(labelSection, title);
}
```

The cache matters because the header titles a screen from `AnimateIn`, which on a cold header runs
a frame or two before `LoadToolkitView`'s continuation. `SuppressLegacyView` runs INSIDE that
continuation, so it is the right place to replay.

**Diagnosing it takes one probe**: reflect the field and print its runtime type. `secRef=UILabel`
where a `UIRef` was expected is the whole answer.

**Third occurrence, 2026-08-28** — `UICustomizeSelectObject.labelCurrentDisplayName` /
`labelCurrentType` / `labelCurrentStatus`, which is why cycling bots on the customize screen moved
the 3D model but left the name plate frozen on its authored placeholders. Same fix, written from
`UICustomizeProfileCharacters.ChangePreset`. Two things generalise from it:

- **The writer is often not the panel.** `UICustomizeProfileCharacters` is a control living inside
  the panel prefab, so it reaches the view with `GetComponentInParent<UIPanelBase>().viewRoot`
  (cached). Walk UP — do NOT reach for a specific panel's `Instance`, or generic game-lib code
  learns the name of the one screen using it today.
- **The replay hook belongs to the PANEL.** This control populates from `Start()`, which fires a
  frame or two BEFORE `LoadToolkitView`'s continuation, so its first write always no-ops.
  `BaseGameUIPanelCustomizeCharacter.SuppressLegacyView` now re-runs
  `ShowCurrentProfileCharacter()` — the same replay slot the header title uses for `toolkitTitle`.
  Any control that populates on Start needs this, or the screen shows authored placeholders until
  the player touches something.

## The OnDisable chain (still the standing prerequisite)

`UIPanelBase.OnDisable` is what calls `FreeToolkitView`. Most `BaseGameUIPanel*` classes override
`OnDisable` to remove their Messenger listeners and never chain, so the view leaks the moment the
panel gets a `toolkitViewKey`. Fix it at the `Base*` layer, per panel, as a migration prerequisite —
checking only the concrete classes misses it.

`OnDisable` only: `UIPanelBase.OnEnable` re-adds `EVENT_BUTTON_CLICK -> OnButtonClickEventHandler`,
which these panels already subscribe themselves, so chaining `OnEnable` too would fire every button
click twice. `RemoveListener` is idempotent, so the one-sided chain is safe.
