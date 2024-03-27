using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Helpers;
using Objects.Other;
using UnityEngine;
using Material = UnityEngine.Material;
using SMesh = Objects.Geometry.Mesh;
using SColor = System.Drawing.Color;

namespace Objects.Converter.Unity
{
    public partial class ConverterUnity
    {
        protected const string DefaultShader = "Standard";

        protected static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        protected static readonly int Metallic = Shader.PropertyToID("_Metallic");
        protected static readonly int Glossiness = Shader.PropertyToID("_Glossiness");

        [field: SerializeField]
        [field: Tooltip("The shader to use when converting opaque RenderMaterials to native")]
        public Shader OpaqueMaterialShader { get; set; }

        [field: SerializeField]
        [field: Tooltip("The shader to use when converting non-opaque RenderMaterials to native")]
        public Shader TranslucentMaterialShader { get; set; }

#nullable enable

        /// <summary>
        ///  List of officially supported shaders. We will attempt to convert shaders not on this list, but will throw warning.
        /// </summary>
        protected static HashSet<string> SupportedShadersToSpeckle =
            new() { "Legacy Shaders/Transparent/Diffuse", "Standard" };

        #region ToNative

        public Material[] RenderMaterialsToNative(IEnumerable<SMesh> meshes)
        {
            return meshes
                .Select(m => RenderMaterialToNative(m["renderMaterial"] as RenderMaterial))
                .ToArray();
        }

        //Just used as cache key for the default (null) material
        private static readonly RenderMaterial DefaultMaterialPlaceholder =
            new() { id = "defaultMaterial" };

        public virtual Material RenderMaterialToNative(RenderMaterial? renderMaterial)
        {
            //todo support more complex materials

            // 1. If no renderMaterial was passed, use default material
            if (renderMaterial == null)
            {
                if (
                    !LoadedAssets.TryGetObject(
                        DefaultMaterialPlaceholder,
                        out Material? defaultMaterial
                    )
                )
                {
                    defaultMaterial = new Material(OpaqueMaterialShader);
                    LoadedAssets.TrySaveObject(DefaultMaterialPlaceholder, defaultMaterial);
                }
                return defaultMaterial;
            }

            // 2. Try get existing/override material from asset cache
            if (LoadedAssets.TryGetObject(renderMaterial, out Material? loadedMaterial))
                return loadedMaterial;

            // 3. Otherwise, convert fresh!
            string name = CoreUtils.GenerateObjectName(renderMaterial);
            Color diffuse = renderMaterial.diffuse.ToUnityColor();
            bool isOpaque = Math.Abs(renderMaterial.opacity - 1d) < Constants.EPS;
            Color color = new(diffuse.r, diffuse.g, diffuse.b, (float)renderMaterial.opacity);
            float metalic = (float)renderMaterial.metalness;
            float gloss = 1f - (float)renderMaterial.roughness;
            bool hasEmission = renderMaterial.emissive != SColor.Black.ToArgb();
            Color emission = renderMaterial.emissive.ToUnityColor();

            Shader shader =
                renderMaterial.opacity < 1 ? TranslucentMaterialShader : OpaqueMaterialShader;
            var mat = new Material(shader);
            mat.color = color;
            mat.name = name;
            mat.SetFloat(Metallic, metalic);
            mat.SetFloat(Glossiness, gloss);
            mat.SetColor(EmissionColor, emission);
            if (hasEmission)
                mat.EnableKeyword("_EMISSION");

            if (!isOpaque)
            {
                if (shader.name == "Standard")
                {
                    ShaderHelpers.SetupMaterialWithBlendMode_Standard(
                        mat,
                        ShaderHelpers.BlendMode.Transparent,
                        true
                    );
                }
                else if (shader.name == "Lit") //URP lit
                { }
            }

            LoadedAssets.TrySaveObject(renderMaterial, mat);
            return mat;
        }

        #endregion

        #region ToSpeckle

        public virtual RenderMaterial MaterialToSpeckle(Material nativeMaterial)
        {
            //Warning message for unknown shaders
            if (!SupportedShadersToSpeckle.Contains(nativeMaterial.shader.name))
                Debug.LogWarning(
                    $"Material Shader \"{nativeMaterial.shader.name}\" is not explicitly supported, the resulting material may be incorrect"
                );

            var color = nativeMaterial.color;
            var opacity = 1f;
            if (nativeMaterial.shader.name.ToLower().Contains("transparent"))
            {
                opacity = color.a;
                color.a = 255;
            }

            var emissive = nativeMaterial.IsKeywordEnabled("_EMISSION")
                ? nativeMaterial.GetColor(EmissionColor).ToIntColor()
                : SColor.Black.ToArgb();

            var materialName = !string.IsNullOrWhiteSpace(nativeMaterial.name)
                ? nativeMaterial.name.Replace("(Instance)", string.Empty).TrimEnd()
                : $"material-{Guid.NewGuid().ToString().Substring(0, 8)}";

            var metalness = nativeMaterial.HasProperty(Metallic)
                ? nativeMaterial.GetFloat(Metallic)
                : 0;

            var roughness = nativeMaterial.HasProperty(Glossiness)
                ? 1 - nativeMaterial.GetFloat(Glossiness)
                : 1;

            return new RenderMaterial
            {
                name = materialName,
                diffuse = color.ToIntColor(),
                opacity = opacity,
                metalness = metalness,
                roughness = roughness,
                emissive = emissive,
            };
        }

        #endregion
    }
}
