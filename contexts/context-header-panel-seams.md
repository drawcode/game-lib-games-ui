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

## The SMALL card's CUSTOMIZE button (2026-09-16)

The small rig (results, products, customize, customize colors/RPG) had the same button as a Latin-1
NGUI `UILabel`, so it could never localize. Only the button converts: `panel-character-small-front`
(foreground band), staged through `SetCharacterSmallToolkit(isToolkitMigrated)`, which
`HandleCharacterDisplay` calls before `ShowCharacter`. The bot and its backer stay NGUI.

One trap: the view finishes loading BEFORE the delayed `showCharacterCo`, and its continuation hides
it. `TweenUtil.ShowObjectTop(UIRef)` only tweens translate and opacity, never display, so the show
must call `UIUtil.ShowObject` first or the button never appears.

## The localization consequence of a mirror (2026-09-19)

Every bridge built for §2 — read the legacy `UILabel`'s text, write it onto the toolkit element —
**overwrites whatever the view authored**, including `@loc:` text that resolved correctly at build
time. The label is localized for exactly as long as it takes the selector to run once.

Found by a qps pseudo-locale sweep, which is the only reliable way to see it: the view file says
`@loc:`, the key exists in all 15 locales, the validator passes, and the screen still reads English.

Three placeholders came back this way on the customize colours panel —
`SELECT A COLOR PRESET`, `SELECT A  STYLE` (authored double space) and `My Previous Uniform` — all
mirrored from the NGUI label by `GameUIPanelCustomizeCharacterColors.MirrorLabel`. The fix keeps
one writer: the mirror still owns the element, but `LocalizePresetName` maps each sentinel string
back to its key on the way through, so a mirrored write and a localized write can never race.

The same pass keyed the `- TYPE: X -` plate in `UICustomizeProfileCharacters`: the bot's name is a
proper noun and stays, only the chrome around it is keyed, and it goes through
`L10n.TrOrDefault(key, "- TYPE: {0} -", name)` so games that don't ship the key keep the English
template. `UpdateToolkitInfoCard` still derives the card's plain form by stripping `"- "` and
`" -"`, which holds for every locale template added so far — revisit it if that card is refactored.

**Rule:** a label that a mirror writes is not localized by its view. Key it where the mirror
writes, or stop mirroring it.
