using UnityEngine;

using Engine.UI;

// Kill-switch owner for baked UIQuadSprite twins under this GameObject (UIQuadSpriteBaker adds it).
// Toolkit views on -> the quads draw and the legacy NGUI widgets are disabled; off -> the reverse.
// Synced on every enable, so pooled/instantiated objects (HUD indicators) pick it up on spawn.
// BaseGameUIPanelBackgrounds keeps its own sync (it predates this component).
public class UIQuadSpriteSwap : MonoBehaviour {

    void OnEnable() {
        Sync();
    }

    public void Sync() {

        bool useQuads = UIPlatform.toolkitViewsEnabled;

        foreach(UIQuadSprite quad in GetComponentsInChildren<UIQuadSprite>(true)) {

            quad.SetVisible(useQuads);

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            UIWidget widget = quad.GetComponent<UIWidget>();

            if(widget != null) {
                widget.enabled = !useQuads;
            }
#endif
        }
    }
}
