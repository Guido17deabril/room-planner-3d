using System;
using Godot;

namespace RoomPlanner3D.Skills
{
    /// <summary>
    /// SkillColorHarmony — Calcula paletas de color armónicas usando la
    /// regla 60-30-10 y transformaciones en el espacio HSL/HSV.
    /// Trabaja exclusivamente con la estructura <see cref="Color"/> de Godot.
    /// </summary>
    public static class SkillColorHarmony
    {
        // ── Tipos de armonía ──────────────────────────────────────────────────

        public enum HarmonyMode
        {
            Complementary,       // 180°
            Analogous,           // ± 30°
            Triadic,             // 0° / 120° / 240°
            SplitComplementary,  // base / 150° / 210°
            Tetradic             // 0° / 90° / 180° / 270°
        }

        // ── Estructura de paleta 60-30-10 ────────────────────────────────────

        public struct Palette6030
        {
            /// <summary>Color dominante — 60 % del espacio (paredes, suelo).</summary>
            public Color Dominant;
            /// <summary>Color secundario — 30 % (muebles principales).</summary>
            public Color Secondary;
            /// <summary>Color de acento — 10 % (textiles, detalles).</summary>
            public Color Accent;

            public override string ToString() =>
                $"Dom: {Dominant.ToHtml()} | Sec: {Secondary.ToHtml()} | Acc: {Accent.ToHtml()}";
        }

        // ── Conversión HSL ↔ Color ────────────────────────────────────────────

        /// <summary>
        /// Convierte un <see cref="Color"/> de Godot (RGB lineal) a HSL.
        /// </summary>
        public static (float H, float S, float L) ToHsl(Color c)
        {
            float r = c.R, g = c.G, b = c.B;
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float delta = max - min;

            float l = (max + min) / 2f;
            float s = 0f, h = 0f;

            if (delta > 1e-6f)
            {
                s = l < 0.5f ? delta / (max + min) : delta / (2f - max - min);

                if (Mathf.Abs(max - r) < 1e-6f)
                    h = ((g - b) / delta + (g < b ? 6f : 0f)) / 6f;
                else if (Mathf.Abs(max - g) < 1e-6f)
                    h = ((b - r) / delta + 2f) / 6f;
                else
                    h = ((r - g) / delta + 4f) / 6f;
            }

            return (h, s, l);
        }

        /// <summary>
        /// Convierte valores HSL a <see cref="Color"/> de Godot.
        /// </summary>
        public static Color FromHsl(float h, float s, float l, float alpha = 1f)
        {
            h = (h % 1f + 1f) % 1f; // normalizar en [0,1)

            if (s < 1e-6f)
                return new Color(l, l, l, alpha);

            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;

            return new Color(
                HueToRgb(p, q, h + 1f / 3f),
                HueToRgb(p, q, h),
                HueToRgb(p, q, h - 1f / 3f),
                alpha
            );
        }

        private static float HueToRgb(float p, float q, float t)
        {
            t = (t % 1f + 1f) % 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        // ── Rotación de matiz ─────────────────────────────────────────────────

        /// <summary>Rota el matiz de un color en <paramref name="degrees"/> grados.</summary>
        public static Color RotateHue(Color c, float degrees)
        {
            var (h, s, l) = ToHsl(c);
            return FromHsl(h + degrees / 360f, s, l, c.A);
        }

        /// <summary>Ajusta la luminosidad de un color en delta relativo (−1 a +1).</summary>
        public static Color AdjustLightness(Color c, float delta)
        {
            var (h, s, l) = ToHsl(c);
            return FromHsl(h, s, Mathf.Clamp(l + delta, 0f, 1f), c.A);
        }

        /// <summary>Ajusta la saturación de un color en delta relativo (−1 a +1).</summary>
        public static Color AdjustSaturation(Color c, float delta)
        {
            var (h, s, l) = ToHsl(c);
            return FromHsl(h, Mathf.Clamp(s + delta, 0f, 1f), l, c.A);
        }

        // ── Generación de paleta 60-30-10 ────────────────────────────────────

        /// <summary>
        /// Genera una paleta 60-30-10 a partir de un color base y un modo de armonía.
        /// El color dominante es el base (aclarado para ser neutro),
        /// el secundario se deriva del modo de armonía y el acento es el complementario.
        /// </summary>
        public static Palette6030 GeneratePalette(Color baseColor, HarmonyMode mode = HarmonyMode.Analogous)
        {
            // Dominante: versión más clara/desaturada del base (para paredes)
            Color dominant  = AdjustLightness(AdjustSaturation(baseColor, -0.25f), +0.20f);

            Color secondary;
            Color accent;

            switch (mode)
            {
                case HarmonyMode.Complementary:
                    secondary = RotateHue(baseColor, 180f);
                    accent    = AdjustLightness(secondary, -0.15f);
                    break;

                case HarmonyMode.Analogous:
                    secondary = RotateHue(baseColor, 30f);
                    accent    = RotateHue(baseColor, -30f);
                    break;

                case HarmonyMode.Triadic:
                    secondary = RotateHue(baseColor, 120f);
                    accent    = RotateHue(baseColor, 240f);
                    break;

                case HarmonyMode.SplitComplementary:
                    secondary = RotateHue(baseColor, 150f);
                    accent    = RotateHue(baseColor, 210f);
                    break;

                case HarmonyMode.Tetradic:
                    secondary = RotateHue(baseColor, 90f);
                    accent    = RotateHue(baseColor, 180f);
                    break;

                default:
                    secondary = RotateHue(baseColor, 30f);
                    accent    = RotateHue(baseColor, -30f);
                    break;
            }

            // Ajustar luminosidad para la regla 60-30-10
            secondary = AdjustLightness(secondary, -0.05f);   // más saturado
            accent    = AdjustLightness(accent, -0.10f);      // más intenso

            return new Palette6030
            {
                Dominant  = dominant,
                Secondary = secondary,
                Accent    = accent
            };
        }

        // ── Utilidades de mezcla ──────────────────────────────────────────────

        /// <summary>Mezcla lineal entre dos colores (t en [0,1]).</summary>
        public static Color Lerp(Color a, Color b, float t) => a.Lerp(b, t);

        /// <summary>
        /// Genera un gradiente de <paramref name="steps"/> colores
        /// entre <paramref name="from"/> y <paramref name="to"/>.
        /// </summary>
        public static Color[] Gradient(Color from, Color to, int steps)
        {
            if (steps < 2) return new[] { from };
            var result = new Color[steps];
            for (int i = 0; i < steps; i++)
                result[i] = from.Lerp(to, i / (float)(steps - 1));
            return result;
        }

        /// <summary>
        /// Calcula el ratio de contraste WCAG (aproximado) entre dos colores.
        /// Valores > 4.5 cumplen AA, > 7 cumplen AAA.
        /// </summary>
        public static float WcagContrast(Color c1, Color c2)
        {
            float l1 = RelativeLuminance(c1);
            float l2 = RelativeLuminance(c2);
            float lighter = Mathf.Max(l1, l2);
            float darker  = Mathf.Min(l1, l2);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance(Color c)
        {
            float r = c.R <= 0.04045f ? c.R / 12.92f : Mathf.Pow((c.R + 0.055f) / 1.055f, 2.4f);
            float g = c.G <= 0.04045f ? c.G / 12.92f : Mathf.Pow((c.G + 0.055f) / 1.055f, 2.4f);
            float b = c.B <= 0.04045f ? c.B / 12.92f : Mathf.Pow((c.B + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }
    }
}
