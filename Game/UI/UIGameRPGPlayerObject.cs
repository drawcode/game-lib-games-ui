using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Utility;

public class UIGameRPGPlayerObject : UIGameRPGObject {

    public GamePlayerController gamePlayerController;

    // World space = the bar sits inside the player it reports on (GamePlayerObject/.../HUD, wired
    // to its own controller in the prefab). The HUD panel's copy has no controller until Start,
    // and is never a child of the player, so it stays on the toolkit view path.
    protected override bool useWorldQuads {
        get {
            return gamePlayerController != null
                && transform.IsChildOf(gamePlayerController.transform);
        }
    }

    public override void Start() {
        base.Start();

        if(gamePlayerController == null) {
            gamePlayerController = GameController.CurrentGamePlayerController;
        }
    }
}