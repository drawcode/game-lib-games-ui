using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Utility;

public class UIGameRPGPlayerHitHealth : UIGameRPGPlayerObject {

    public override void Start() {
        incrementValue = .01;
        profileValue = 1;
        lastValue = 0;
        UpdateValue();
    }

    public override void UpdateValue() {

        if(gamePlayerController == null) {
            return;
        }

        if(gamePlayerController.runtimeData == null) {
            return;
        }

        profileValue = gamePlayerController.runtimeData.hitHealthRemaining;

    }

    public override void UpdateInterval() {
        if(lastTime > 1f) {
            lastTime = 0f;
            UpdateValue();
        }
    }

    public override void HandleUpdate(bool updateTimeInterval) {

        lastTime += Time.deltaTime;

        if(updateTimeInterval) {
            UpdateInterval();
        }

        base.HandleUpdate(false);
    }

    // OVERHEAD BAR BY ROLE (iter 31, owner). The bar over an actor takes that actor's outline
    // colour -- sidekicks green, enemies red -- and the player gets none: the HUD's three-part bar
    // is the player's. World bars only (useWorldQuads); the HUD panel's copy is untouched. It is
    // also smaller, higher and tighter than the authored prefab, applied once from the prefab's
    // own values so a pooled actor never compounds it.
    public static bool overheadRoleStyle = true;
    public static float overheadScale = .8f;
    public static float overheadLift = .25f;
    public static float overheadPadding = 2f;

    const int roleNone = -1;
    const int rolePlayer = 0;
    const int roleSidekick = 1;
    const int roleEnemy = 2;
    const int roleOther = 3;

    int barRole = roleNone;
    bool overheadGeometryApplied = false;

    // The quads that carry the role colour (the fill and the tinted icon), found once from the
    // prefab's own red so a later recolour can find them again.
    readonly List<Engine.UI.UIQuadSprite> roleTintQuads = new List<Engine.UI.UIQuadSprite>();

    int GetBarRole() {

        if (gamePlayerController == null) {
            return roleNone;
        }

        if (gamePlayerController.IsPlayerControlled) {
            return rolePlayer;
        }

        if (gamePlayerController.IsSidekickControlled) {
            return roleSidekick;
        }

        if (gamePlayerController.IsAgentControlled) {
            return roleEnemy;
        }

        return roleOther;
    }

    // Recolours only on a role change (a pooled actor is re-inited into a new role): one int
    // compare per frame otherwise, no allocation.
    void SyncRoleStyle() {

        if (!overheadRoleStyle || !useWorldQuads) {
            return;
        }

        ApplyOverheadGeometry();

        int role = GetBarRole();

        if (role == barRole) {
            return;
        }

        barRole = role;

        Color roleColor;

        if (role == roleSidekick) {
            roleColor = UIColors.colorGreen;
        }
        else if (role == roleEnemy) {
            roleColor = UIColors.colorRed;
        }
        else {
            return;
        }

        for (int i = 0; i < roleTintQuads.Count; i++) {
            Engine.UI.UIQuadSprite quad = roleTintQuads[i];
            if (quad != null) {
                roleColor.a = quad.color.a;
                quad.SetColor(roleColor);
            }
        }
    }

    void ApplyOverheadGeometry() {

        if (overheadGeometryApplied) {
            return;
        }

        overheadGeometryApplied = true;

        transform.localScale = transform.localScale * overheadScale;
        transform.localPosition = transform.localPosition + Vector3.up * overheadLift;

        // Tighter: the dark backer hugs the fill with an even border instead of the legacy
        // 72x18 plate offset to the left.
        Transform fill = null;
        Transform backer = null;

        roleTintQuads.Clear();

        foreach (Engine.UI.UIQuadSprite quad in GetComponentsInChildren<Engine.UI.UIQuadSprite>(true)) {

            bool isFill = quad.fillAxis != Engine.UI.UIQuadSprite.FillAxis.None;
            bool isTinted = quad.color.r > .5f && quad.color.g < .2f && quad.color.b < .2f;

            if (isFill) {
                fill = quad.transform;
            }
            else if (quad.gameObject.name == "Background") {
                backer = quad.transform;
            }

            if (isFill || isTinted) {
                roleTintQuads.Add(quad);
            }
        }

        if (fill != null && backer != null) {
            Vector3 backerPos = fill.localPosition;
            backerPos.z = backer.localPosition.z;
            backer.localPosition = backerPos;
            backer.localScale = new Vector3(
                fill.localScale.x + overheadPadding * 2f,
                fill.localScale.y + overheadPadding * 2f,
                backer.localScale.z);
        }
    }

    // Dead actors keep their body for a moment before the pool reclaims them; their (empty) bar
    // should not linger with it -- and the player's overhead bar is never shown (above). Renderers
    // only, so this component keeps updating and shows the bar again when the actor respawns.
    // Touched only on a change: no per-frame cost or garbage. Unknown after an enable, since the
    // quad twins' sync can switch renderers back on.
    readonly List<Renderer> barRenderers = new List<Renderer>();
    const int visibilityUnknown = -1;
    int barHidden = visibilityUnknown;

    public override void OnEnable() {
        base.OnEnable();
        barHidden = visibilityUnknown;
    }

    void SyncDeathVisibility() {

        bool hidden = gamePlayerController != null && gamePlayerController.isDead;

        if (overheadRoleStyle && useWorldQuads && barRole == rolePlayer) {
            hidden = true;
        }

        int state = hidden ? 1 : 0;

        if (state == barHidden) {
            return;
        }

        barHidden = state;

        GetComponentsInChildren<Renderer>(true, barRenderers);

        for (int i = 0; i < barRenderers.Count; i++) {
            barRenderers[i].enabled = !hidden;
        }
    }

    public override void Update() {

        SyncRoleStyle();

        SyncDeathVisibility();

        HandleUpdate(true);

        if(GameKeyCodes.isActionPlayerHitAdd) {
            LogUtil.Log("PlayerHitAdd:" + incrementValue);
            //gamePlayerController.Hit(1);
        }
        else if(GameKeyCodes.isActionPlayerHitSubtract) {
            LogUtil.Log("PlayerHitSubtract:" + incrementValue);
            //gamePlayerController.Hit(-1);
        }
    }
}