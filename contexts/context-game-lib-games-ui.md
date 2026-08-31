---
name: context-game-lib-games-ui
description: game-lib-games-ui submodule — shared game UI components and notification displays
metadata:
  type: repo
  repo: game-lib-games-ui
  path: .
---

# Context: game-lib-games-ui (submodule)

- **Workspace mount:** `Assets/Code/Libs/game-lib-games-ui`
- **Repo:** git@github.com:drawcode/game-lib-games-ui.git (tracks `dev`)
- **Purpose:** Shared game UI components layered on game-lib-engine/game-lib-games UI abstractions.

## Structure
- `Game/UI/` — shared UI panel/component base code.
- `UI/` — `UINotificationDisplay.cs`, `UINotificationDisplayTip.cs` (notification/tip toasts).
- `BuildInfo.cs`.

## Topic contexts in this repo
- [`context-header-panel-seams.md`](./context-header-panel-seams.md)
- [`context-settings-controls-binding.md`](./context-settings-controls-binding.md) — the two
  independent faults that made Settings: Controls inert: fields declared in the
  `#if USE_UI_NGUI` branch (which IS the branch this project compiles) can never bind, and a
  toolkit toggle has no GameObject to carry the MonoBehaviour that raises the change event.
  Both audits apply to every migrated panel.

Small library; the bulk of screen-level UI lives in the app repo (`Assets/Code/Game/Game/UI/GameUIPanel*`). Asmdef disabled (`game-lib-games-ui._asmdef`). UI backend selected via `USE_UI_NGUI_3` / `USE_UI_UNITY` defines.
