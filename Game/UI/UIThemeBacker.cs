using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Engine.Game.App;

// The theme's farthest-back colour (iter 31, owner): one value per app / IP, AppColors
// "ui-backer". It is what shows under every screen and every transition -- the backmost
// "CameraClear" cameras and the Backgrounds plain quad -- so a load or a flow never flashes a
// different colour first. Fades to white or black stay overlays ABOVE it. An app without the
// key keeps its authored colours (GetColor answers white for a miss, and before data is ready).
public static class UIThemeBacker {

    public const string colorCode = "ui-backer";
    public const string clearCameraName = "CameraClear";

    public static bool enabled = true;

    static readonly List<Camera> cameras = new List<Camera>();

    public static bool TryGetColor(out Color color) {

        color = AppColors.GetColor(colorCode);

        return enabled && color != Color.white;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterSceneHook() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        ApplyCameras();
    }

    // Solid-colour clears named CameraClear only: the other cameras clear depth or nothing, and a
    // camera's own background is never the farthest layer.
    public static void ApplyCameras() {

        Color color;

        if (!TryGetColor(out color)) {
            return;
        }

        color.a = 1f;

        cameras.Clear();
        cameras.AddRange(UnityEngine.Object.FindObjectsByType<Camera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None));

        for (int i = 0; i < cameras.Count; i++) {

            Camera cam = cameras[i];

            if (cam != null
                && cam.clearFlags == CameraClearFlags.SolidColor
                && cam.name == clearCameraName) {
                cam.backgroundColor = color;
            }
        }

        cameras.Clear();
    }

    // The Backgrounds plain quad: same colour, its own alpha (the panel fades it in and out).
    public static void ApplyQuad(GameObject go) {

        Color color;

        if (go == null || !TryGetColor(out color)) {
            return;
        }

        foreach (Engine.UI.UIQuadSprite quad in go.GetComponentsInChildren<Engine.UI.UIQuadSprite>(true)) {
            color.a = quad.color.a;
            quad.SetColor(color);
        }
    }
}
