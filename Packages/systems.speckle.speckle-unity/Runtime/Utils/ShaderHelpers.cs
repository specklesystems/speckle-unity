using UnityEngine;

namespace Speckle.ConnectorUnity.Utils
{
    // see https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/StandardShaderGUI.cs
    public static class ShaderHelpers
    {
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int Mode = Shader.PropertyToID("_Mode");

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade, // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        public static void SetupMaterialWithBlendMode_Standard(
            Material material,
            BlendMode blendMode,
            bool overrideRenderQueue
        )
        {
            int minRenderQueue = -1;
            int maxRenderQueue = 5000;
            int defaultRenderQueue = -1;
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetFloat(Mode, 0);
                    material.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat(DstBlend, (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetFloat(ZWrite, 1.0f);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    minRenderQueue = -1;
                    maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest - 1;
                    defaultRenderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetFloat(Mode, 1);
                    material.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat(DstBlend, (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetFloat(ZWrite, 1.0f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    minRenderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast;
                    defaultRenderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat(Mode, 2);
                    material.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetFloat(
                        DstBlend,
                        (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                    );
                    material.SetFloat(ZWrite, 0.0f);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    minRenderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1;
                    maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay - 1;
                    defaultRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat(Mode, 3);
                    material.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat(
                        DstBlend,
                        (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                    );
                    material.SetFloat(ZWrite, 0.0f);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    minRenderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1;
                    maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay - 1;
                    defaultRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }

            if (
                overrideRenderQueue
                || material.renderQueue < minRenderQueue
                || material.renderQueue > maxRenderQueue
            )
            {
                if (!overrideRenderQueue)
                    Debug.LogFormat(
                        LogType.Log,
                        LogOption.NoStacktrace,
                        null,
                        "Render queue value outside of the allowed range ({0} - {1}) for selected Blend mode, resetting render queue to default",
                        minRenderQueue,
                        maxRenderQueue
                    );
                material.renderQueue = defaultRenderQueue;
            }
        }
        
    }
}
