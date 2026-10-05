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

    // Dead actors keep their body for a moment before the pool reclaims them; their (empty) bar
    // should not linger with it. Renderers only, so this component keeps updating and shows the
    // bar again when the actor respawns. Touched only on a change: no per-frame cost or garbage.
    readonly List<Renderer> barRenderers = new List<Renderer>();
    bool barHiddenForDeath = false;

    void SyncDeathVisibility() {

        bool dead = gamePlayerController != null && gamePlayerController.isDead;

        if (dead == barHiddenForDeath) {
            return;
        }

        barHiddenForDeath = dead;

        GetComponentsInChildren<Renderer>(true, barRenderers);

        for (int i = 0; i < barRenderers.Count; i++) {
            barRenderers[i].enabled = !dead;
        }
    }

    public override void Update() {

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