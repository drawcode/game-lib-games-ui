using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Utility;

public class UIGameRPGObject : GameObjectBehavior {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UISlider sliderProgress;
    public UILabel labelProgress;
    public UILabel labelValue;
#else
    // Agnostic handles (BaseGameHUD.cs pattern): not serialized, bound by name at runtime.
    public Engine.UI.UIRef sliderProgress;
    public Engine.UI.UIRef labelProgress;
    public Engine.UI.UIRef labelValue;
#endif

    public double profileValue = 0;
    public double lastValue = 0;
    public double incrementValue = .01;
    public bool useGlobal = false;
    public float lastTime = 0f;

    public virtual void Start() {
        UpdateValue();
    }

    // WORLD-SPACE QUADS. A bar that floats in the level (the bot health bar on GamePlayerObject)
    // cannot become a toolkit view — overlay panels draw above every camera — so with the kill
    // switch on its sprites draw as runtime UIQuadSprite twins (Engine.UI.UIQuadSpriteTwins) and
    // the NGUI widgets stand down. Screen-space bars (HUD, header) keep useWorldQuads false: the
    // toolkit view already replaces them.
    protected virtual bool useWorldQuads {
        get {
            return false;
        }
    }

    // NGUI depth + this. 0 keeps the twins in the default sorting band like the NGUI draw call
    // (render queue + depth, sorting order 0) instead of below the world's particles.
    protected virtual int worldQuadSortingBase {
        get {
            return 0;
        }
    }

    private bool worldQuadsActive;
    private Engine.UI.UIQuadSprite progressFillQuad;
#if USE_UI_NGUI_2_7
    private UISprite progressFillSprite;
#endif

    public virtual void OnEnable() {
        SyncWorldQuads();
    }

    // Build/refresh the twins and apply the kill switch (every enable, so a re-shown bar follows
    // the current switch). A no-op for screen-space bars.
    public void SyncWorldQuads() {

        if (!useWorldQuads) {
            return;
        }

        Engine.UI.UIQuadSpriteTwins.Sync(gameObject, worldQuadSortingBase);

        worldQuadsActive = Engine.UI.UIQuadSpriteTwins.quadsActive;

#if USE_UI_NGUI_2_7
        progressFillQuad = null;
        progressFillSprite = null;

        if (sliderProgress != null && sliderProgress.foreground != null) {

            UISprite fg = sliderProgress.foreground.GetComponent<UISprite>();

            if (fg != null) {
                progressFillQuad = fg.GetComponent<Engine.UI.UIQuadSprite>();
                progressFillSprite = progressFillQuad != null ? fg : null;
            }
        }

        MirrorProgressFill();
#else
        // No legacy slider: the BAKED fill quad (UIQuadSpriteBaker, filled-sprite case) is the bar
        // itself and SetProgressValue drives it directly.
        progressFillQuad = null;

        foreach (Engine.UI.UIQuadSprite quad in GetComponentsInChildren<Engine.UI.UIQuadSprite>(true)) {
            if (quad.fillAxis != Engine.UI.UIQuadSprite.FillAxis.None) {
                progressFillQuad = quad;
                break;
            }
        }
#endif
    }

    // The legacy slider stays the source of truth: it sets the foreground's fillAmount (filled
    // sprite) or its localScale (scaled sprite), and the twin copies that. Runs from SetProgress,
    // i.e. every frame while the bar is live: unchanged -> one float compare, no allocation.
    void MirrorProgressFill() {
#if USE_UI_NGUI_2_7
        if (progressFillQuad == null) {
            return;
        }

        if (progressFillSprite.type == UISprite.Type.Filled) {
            progressFillQuad.SetFillAmount(progressFillSprite.fillAmount);
            return;
        }

        // A scaled foreground follows the transform scale by itself, but UISlider re-enables the
        // widget whenever the value rises above 0.001 and disables it at 0. With the quads active,
        // take that visibility over and keep the widget down.
        if (worldQuadsActive && progressFillSprite.enabled) {
            progressFillSprite.enabled = false;
            progressFillQuad.SetVisible(true);
        }
        else if (worldQuadsActive && sliderProgress.sliderValue < 0.001f && progressFillQuad.isVisible) {
            progressFillQuad.SetVisible(false);
        }
#endif
    }

    public virtual void UpdateValue() {
        profileValue = 0;
    }

    public virtual void SetLabelValue(double val) {

        // Runs every frame from HandleUpdate: with no label wired (the bot health bar) skip the
        // ToString, which allocated a string per bar per frame for nothing.
        if (labelValue == null) {
            return;
        }

        // Also runs every frame with the SAME value once the count-up settles, and the format
        // allocated a string each time (~40 B/frame on the header coin). Skip it only when the value,
        // the locale AND the label's current text all still match what was written: a value-only
        // cache would never refill a recycled toolkit element (they come back blank after a view
        // teardown), and a language change must reformat the digits.
        string locale = Engine.Game.App.BaseApp.L10n.CurrentCode;

        if (val == labelValueShown
            && locale == labelValueLocale
            && string.Equals(UIUtil.GetLabelValue(labelValue), labelValueText)) {
            return;
        }

        labelValueText = val.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat);
        labelValueShown = val;
        labelValueLocale = locale;

        UIUtil.SetLabelValue(labelValue, labelValueText);
    }

    double labelValueShown = double.NaN;
    string labelValueText;
    string labelValueLocale;
    double labelProgressShown = double.NaN;
    string labelProgressText;

    public virtual void SetProgress(double val) {
        SetProgressValue(val);
        SetProgressLabelValue(val);
    }

    public virtual void SetProgressLabelValue(double val) {

        if (labelProgress == null) {
            return;
        }

        // Same per-frame skip as SetLabelValue ("P0" is culture-neutral here, so no locale key).
        if (val == labelProgressShown
            && string.Equals(UIUtil.GetLabelValue(labelProgress), labelProgressText)) {
            return;
        }

        labelProgressText = val.ToString("P0");
        labelProgressShown = val;

        UIUtil.SetLabelValue(labelProgress, labelProgressText);
    }

    public virtual void SetProgressValue(double val) {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        UIUtil.SetSliderValue(sliderProgress, (float)val);
        MirrorProgressFill();
#else
        if (progressFillQuad != null) {
            progressFillQuad.SetFillAmount((float)val);
        }
        else {
            UIUtil.SetSliderValue(sliderProgress, (float)val);
        }
#endif
    }

    public virtual void UpdateInterval() {
        if(lastTime > 1f) {
            lastTime = 0f;
            UpdateValue();
        }
    }

    public virtual void HandleUpdate(bool updateIntervalBase) {

        if(updateIntervalBase) {
            lastTime += Time.deltaTime;
            UpdateInterval();
        }

        if(lastValue > profileValue) {

            double differenceValue = lastValue - profileValue;

            if(Math.Abs(differenceValue) > 50) {
                lastValue -= (Math.Round(Math.Abs(differenceValue) / 4)) * incrementValue;
            }
            else {
                lastValue -= incrementValue;
            }
        }
        else if(profileValue > lastValue) {

            double differenceValue = lastValue - profileValue;

            if(Math.Abs(differenceValue) > 50) {
                lastValue += (Math.Round(Math.Abs(differenceValue) / 4)) * incrementValue;
            }
            else {
                lastValue += incrementValue;
            }
        }

        if(incrementValue < .1) {
            lastValue = Math.Round(lastValue, 2);
        }
        else {
            lastValue = Math.Round(lastValue, 1);
        }

        if(lastValue < 0) {
            lastValue = 0;
        }

        SetProgress(lastValue);
        SetLabelValue(lastValue);
    }

    public virtual void Update() {
        HandleUpdate(true);
    }
}