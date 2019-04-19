using UnityEngine;

namespace UnityEditor.VFXToolbox
{
    internal class CurveToTextureUtility
    {
        public static void CurveToTexture(AnimationCurve curve, ref Texture2D texture)
        {
            if(texture != null && curve!= null && texture.height == 1 && texture.width > 1)
            {
                Color[] colors = new Color[texture.width];
                for (int i = 0; i < texture.width; i++)
                {
                    float t = (float)i / (texture.width - 1);
                    float v = curve.Evaluate(t);
                    colors[i] = new Color(v, v, v, 1);
                }
                texture.SetPixels(colors);
                texture.Apply();
            }
        }

        public static void GradientToTexture(Gradient gradient, ref Texture2D texture, bool linear = false)
        {
            if(texture != null && gradient != null && texture.height == 1 && texture.width > 1)
            {
                Color[] colors = new Color[texture.width];
                for (int i = 0; i < texture.width; i++)
                {
                    float t = (float)i / (texture.width - 1);
                    if(linear)
                        colors[i] = gradient.Evaluate(t).linear;
                    else
                        colors[i] = gradient.Evaluate(t);
                }
                texture.SetPixels(colors);
                texture.Apply();
            }
        }
        public static void GradientToTexture(Gradient gradient, AnimationCurve curve,  ref Texture2D texture, bool linear = false)
        {
            if (texture != null && gradient != null && texture.height == 1 && texture.width > 1)
            {
                Color[] colors = new Color[texture.width];
                for (int i = 0; i < texture.width; i++)
                {
                    float t = (float)i / (texture.width - 1);
                    float b = curve.Evaluate(t);
                    if (linear)
                        colors[i] = b * gradient.Evaluate(t).linear;
                    else
                        colors[i] = b * gradient.Evaluate(t);
                }
                texture.SetPixels(colors);
                texture.Apply();
            }
        }
    }
}
