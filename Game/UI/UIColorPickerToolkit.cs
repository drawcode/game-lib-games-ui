using System;

using UnityEngine;

using Engine.UI;

// The UI Toolkit half of the customize-colors picker.
//
// The legacy picker is not a UI widget at all: `ColorHuePicker` and `ColorSaturationBrightness
// Picker` are quads with MeshRenderers, painted by two URP shaders, dragged by `ColorDraggable`
// raycasting a MeshCollider out of its own Update, and wired together with SendMessage. None of
// that survives the flip to a VisualElement, so this class reproduces the two shaders as textures
// and the drag maths as normalized points, and leaves the colour model itself (`HSBColor`)
// untouched — the picker must keep producing exactly the colours the profile already stores.
//
// ------------------------------------------------------------------------------------------
// THE LEGACY PICKER IS MIRRORED ON BOTH AXES. This is deliberate here, not an oversight.
//
// `ColorDraggable.OnColorDrag` reports `Vector3.one - normalizedPosition`, so the legacy screen
// reads: saturation is FULL at the LEFT edge and zero at the right; brightness is FULL at the
// BOTTOM and zero at the top; and hue runs magenta -> red backwards along the strip. Verified
// against the baseline capture at four points inside the square and three along the strip — e.g.
// the square's centre measures #BC0604, which is exactly `0.499 + (red - 1) * 0.501` gamma
// encoded, and only under the mirrored reading.
//
// Un-mirroring it would be a behaviour change to a shipped screen, so the mirror lives HERE, in
// one place, named, instead of being spread through the panel. Flip `mirrored` to false if that
// call is ever made.
// ------------------------------------------------------------------------------------------
public static class UIColorPickerToolkit {

    public const bool mirrored = true;

    // Wide enough that a 380-unit strip never shows a stair-step, small enough to build in one
    // frame. The square is the expensive one — it is rebuilt on every hue change.
    public const int hueWidth = 256;
    public const int squareSize = 128;

    // SHADER REPRODUCTION
    //
    // Both shaders write LINEAR values straight to the target. A Texture2D authored here is an
    // sRGB texture, so it is decoded to linear when sampled — meaning the byte we store must be
    // the GAMMA encoding of the shader's output, or every gradient renders twice-darkened. Same
    // trap as the palette (rule 57) and the toast's alpha (rule 63), one layer further out.

    // ColorHue.shader: six linear ramps between the primaries, indexed by uv.x.
    private static Color HueRamp(float u) {

        float p = Mathf.Floor(u * 6f);
        float t = u * 6f - p;

        if (p < 1f) {
            return new Color(1f, t, 0f);
        }

        if (p < 2f) {
            return new Color(1f - t, 1f, 0f);
        }

        if (p < 3f) {
            return new Color(0f, 1f, t);
        }

        if (p < 4f) {
            return new Color(0f, 1f - t, 1f);
        }

        if (p < 5f) {
            return new Color(t, 0f, 1f);
        }

        return new Color(1f, 0f, 1f - t);
    }

    private static Color Encode(Color linear) {

        return new Color(
            Mathf.LinearToGammaSpace(Mathf.Clamp01(linear.r)),
            Mathf.LinearToGammaSpace(Mathf.Clamp01(linear.g)),
            Mathf.LinearToGammaSpace(Mathf.Clamp01(linear.b)),
            1f);
    }

    public static Texture2D BuildHueTexture() {

        Texture2D tex = new Texture2D(hueWidth, 1, TextureFormat.RGBA32, false);

        tex.name = "color-picker-hue";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        for (int x = 0; x < hueWidth; x++) {

            float f = hueWidth == 1 ? 0f : (float)x / (hueWidth - 1);
            float u = mirrored ? 1f - f : f;

            tex.SetPixel(x, 0, Encode(HueRamp(u)));
        }

        tex.Apply(false, false);

        return tex;
    }

    // ColorSaturationBrightness.shader: `uv.y + (_Color.rgb - 1) * uv.x`, with _Color the pure
    // hue. Texture row 0 is the BOTTOM under Unity's convention, and the shader's uv.y is 0 at
    // the top of the mirrored quad, so the row index walks uv.y downward.
    public static Texture2D BuildSaturationBrightnessTexture(float hue, Texture2D reuse) {

        Texture2D tex = reuse;

        if (tex == null) {

            tex = new Texture2D(squareSize, squareSize, TextureFormat.RGBA32, false);

            tex.name = "color-picker-saturation-brightness";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
        }

        Color c = new HSBColor(hue, 1f, 1f).ToColor();

        for (int y = 0; y < squareSize; y++) {

            // row 0 is the bottom of the image; uv.y is 1 at the bottom under the mirror.
            float fy = (float)y / (squareSize - 1);
            float uvY = mirrored ? 1f - fy : fy;

            for (int x = 0; x < squareSize; x++) {

                float fx = (float)x / (squareSize - 1);
                float uvX = mirrored ? 1f - fx : fx;

                Color linear = new Color(
                    uvY + (c.r - 1f) * uvX,
                    uvY + (c.g - 1f) * uvX,
                    uvY + (c.b - 1f) * uvX);

                tex.SetPixel(x, y, Encode(linear));
            }
        }

        tex.Apply(false, false);

        return tex;
    }

    // THE DRAG / THUMB MAPPING
    //
    // One pair of functions per axis pair, so the panel never re-derives the mirror. `point` is
    // what UIUtil.SetElementDragHandler reports: (0,0) bottom-left .. (1,1) top-right.

    public static float HueFromDrag(Vector2 point) {

        return Mathf.Clamp01(mirrored ? 1f - point.x : point.x);
    }

    public static Vector2 HueThumbOffset(float hue) {

        return new Vector2(mirrored ? 1f - Mathf.Clamp01(hue) : Mathf.Clamp01(hue), 0.5f);
    }

    // x -> saturation, y -> brightness.
    public static Vector2 SaturationBrightnessFromDrag(Vector2 point) {

        return new Vector2(
            Mathf.Clamp01(mirrored ? 1f - point.x : point.x),
            Mathf.Clamp01(mirrored ? 1f - point.y : point.y));
    }

    public static Vector2 SaturationBrightnessThumbOffset(float saturation, float brightness) {

        return new Vector2(
            mirrored ? 1f - Mathf.Clamp01(saturation) : Mathf.Clamp01(saturation),
            mirrored ? 1f - Mathf.Clamp01(brightness) : Mathf.Clamp01(brightness));
    }

    // The texture the panel hands to a hue THUMB: one flat swatch of the current hue, so the
    // marker reads as "this is the hue you picked" the way the legacy quad's SolidColor material
    // did. A colour, not a gradient — the panel sets it through UIUtil.SetElementColor.
    public static Color PureHue(float hue) {

        return new HSBColor(Mathf.Clamp01(hue), 1f, 1f).ToColor();
    }

    public static void Release(Texture2D tex) {

        if (tex == null) {
            return;
        }

        // Built with HideAndDontSave, so nothing else will ever collect these.
        UnityEngine.Object.DestroyImmediate(tex);
    }
}
