using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Engine.UI;
using Engine.Utility;

public enum UIAppPanelMode {    
    ModeMain,
    ModeList
}

public class UIAppPanelMessages {
    public static string panelShow = "ui-app-panel-show";
    public static string panelHide = "ui-app-panel-hide";
}

public class UIAppPanel : GameObjectBehavior {

    public UIAppPanelMode panelMode = UIAppPanelMode.ModeMain;
    public bool _isVisible = false;
    public string className = "";

    public Camera panelCamera;

    public virtual bool isVisible {
        get {
            return _isVisible;
        }
        set {
            _isVisible = value;
        }
    }

    public virtual void Awake() {
        GetClassName(this);
    }

    public virtual void Start() {
        Init();
    }

    public virtual void Init() {

        if(GameGlobal.Instance == null) {

            Context.Current.ApplicationLoadLevelByName("GameUISceneRoot");
        }
        else {

            GetClassName(this);

            FindCameraAbove();
        }
    }

    public Camera FindCameraAbove() {

        panelCamera = gameObject.FindTypeAboveRecursive<Camera>();

        return panelCamera;
    }

    public string GetClassName(object item) {

        className = item.GetType().Name;
        //LogUtil.Log("CLASS NAME:" + className);

        return className;
    }

    // ------------------------------------------------------------------------
    // ELEMENT BINDING (UI Toolkit path)
    //
    // Resolves this panel's public UIRef fields against a loaded view. Backend-blind: it goes
    // through IUIBackend.Resolve, so the same code binds a VisualElement tree today and any
    // future backend tomorrow.
    //
    // Resolution order is MANIFEST FIRST, convention second — the reverse of the original plan.
    // Ground truth forced the inversion: NGUI prefab GameObject names are PascalCase
    // (ButtonSettingsAudio) while the fields are camelCase (buttonSettingsAudio), and ~27% of
    // widget slots are already unwired and silently null-tolerated. Convention alone would fail
    // to bind a large minority of fields without anyone noticing. The converter emits the
    // manifest from the prefab's own serialized wiring, so the manifest IS the ground truth.
    //
    // An unresolved field becomes UIRef.none (never null) and logs once. Every backend op
    // no-ops on a dead ref, so a missed bind degrades to "nothing happens" rather than an NRE.

    public UIRef viewRoot = UIRef.none;

    // panelClassName -> (fieldName -> elementName), loaded from the converter's *.bind.json.
    private static Dictionary<string, Dictionary<string, string>> bindManifests
        = new Dictionary<string, Dictionary<string, string>>();

    public static void RegisterBindManifest(string panelClassName, Dictionary<string, string> binds) {

        if (string.IsNullOrEmpty(panelClassName) || binds == null) {
            return;
        }

        bindManifests[panelClassName] = binds;
    }

    private static string ToKebabCase(string fieldName) {

        if (string.IsNullOrEmpty(fieldName)) {
            return fieldName;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < fieldName.Length; i++) {

            char c = fieldName[i];

            if (char.IsUpper(c)) {

                if (i > 0) {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public virtual void BindElements(UIRef root) {

        if (root == null || !root.alive) {
            return;
        }

        viewRoot = root;

        IUIBackend backend = UIPlatform.For(root);

        if (backend == null) {
            return;
        }

        Dictionary<string, string> manifest = null;
        bindManifests.TryGetValue(GetClassName(this), out manifest);

        FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < fields.Length; i++) {

            FieldInfo field = fields[i];

            if (field.FieldType != typeof(UIRef)) {
                continue;
            }

            if (field.Name == "viewRoot") {
                continue;
            }

            string elementName = null;

            // 1. manifest alias (authoritative)
            if (manifest != null) {
                manifest.TryGetValue(field.Name, out elementName);
            }

            UIRef resolved = UIRef.none;

            if (!string.IsNullOrEmpty(elementName)) {
                resolved = backend.Resolve(root, elementName);
            }

            // 2. convention: field name verbatim
            if (!resolved.alive) {
                resolved = backend.Resolve(root, field.Name);
            }

            // 3. convention: kebab-cased field name
            if (!resolved.alive) {
                resolved = backend.Resolve(root, ToKebabCase(field.Name));
            }

            if (!resolved.alive) {
                LogUtil.LogWarning("BindElements: unresolved field '" + field.Name
                    + "' on " + className + " (view '" + root.name + "')");
            }

            field.SetValue(this, resolved);
        }
    }

    public void ShowCamera(float delay) {
        StartCoroutine(ShowCameraaCo(delay));
    }

    IEnumerator ShowCameraaCo(float delay) {

        yield return new WaitForSeconds(delay);

        ShowCamera();
    }

    public void ShowCamera() {

        if(panelCamera != null) {
            panelCamera.ShowCameraFadeIn();
        }
    }

    public void HideCamera(float delay) {
        StartCoroutine(HideCameraCo(delay));
    }

    IEnumerator HideCameraCo(float delay) {

        yield return new WaitForSeconds(delay);

        HideCamera();
    }

    public void HideCamera() {

        if(panelCamera != null) {
            panelCamera.HideCameraFadeOut();
        }
    }
}