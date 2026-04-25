using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UberShaderEditor : ShaderGUI
{
    public enum BlendModes
    {
        Opaque = 0,
        AlphaBlend = 1,
        Additive = 2
    }

    public enum CullingModes
    {
        Off = 0,
        Front = 1,
        Back = 2
    }

    public enum WorkflowMode
    {
        Specular = 0,
        Metallic = 1
    }

    public enum Source
    {
        SpecularAlpha = 0,
        AlbedoAlpha = 1,

    }

    public class UberLitProperties
    {
        public MaterialProperty _BaseMap;
        public MaterialProperty _BaseColor;

        public MaterialProperty _Smoothness;
        public MaterialProperty _SmoothnessTextureChannel;

        public MaterialProperty _Metallic;
        public MaterialProperty _MetallicGlossMap;

        public MaterialProperty _SpecColor;
        public MaterialProperty _SpecGlossMap;


        public MaterialProperty _WorkflowMode;

        public MaterialProperty _BumpMap;
        public MaterialProperty _BumpScale;

        public MaterialProperty _OcclusionMap;
        public MaterialProperty _OcclusionStrength;

        public MaterialProperty _Emission;
        public MaterialProperty _EmissionColor;
        public MaterialProperty _EmissionMap;

        public MaterialProperty _DetailMask;
        public MaterialProperty _DetailAlbedoMapScale;
        public MaterialProperty _DetailAlbedoMap;
        public MaterialProperty _DetailNormalMapScale;
        public MaterialProperty _DetailNormalMap;

        public MaterialProperty _SSSMap;
        public MaterialProperty _SSS_Power;
        public MaterialProperty _SSS_Distortion;
        public MaterialProperty _SSS_Scale;
        public MaterialProperty _SSS_Color;


        public MaterialProperty _SpecularHighlights;
        public MaterialProperty _EnvironmentReflections;

        public MaterialProperty _Cull;
        public MaterialProperty _Blend;
        public MaterialProperty _ZWrite;
        public MaterialProperty _Surface;

        public MaterialProperty _Debug;

        public MaterialProperty _DecalColor;
        public MaterialProperty _DecalTexture;
        public MaterialProperty _DecalTextureB;

        public MaterialProperty _AcessibilityColor;
        public MaterialProperty _FresnelDebug;
        public MaterialProperty _Tiling;
        public MaterialProperty _BrushStrokeTexture;




        public UberLitProperties(MaterialProperty[] properties)
        {
            _BaseMap = FindProperty(nameof(_BaseMap), properties, false);
            _BaseColor = FindProperty(nameof(_BaseColor), properties, false);

            _Smoothness = FindProperty(nameof(_Smoothness), properties, false);
            _SmoothnessTextureChannel = FindProperty(nameof(_SmoothnessTextureChannel), properties, false);

            _Metallic = FindProperty(nameof(_Metallic), properties, false);
            _MetallicGlossMap = FindProperty(nameof(_MetallicGlossMap), properties, false);
            _SpecColor = FindProperty(nameof(_SpecColor), properties, false);
            _SpecGlossMap = FindProperty(nameof(_SpecGlossMap), properties, false);
            _WorkflowMode = FindProperty(nameof(_WorkflowMode), properties, false);

            _BumpMap = FindProperty(nameof(_BumpMap), properties, false);
            _BumpScale = FindProperty(nameof(_BumpScale), properties, false);

            _OcclusionMap = FindProperty(nameof(_OcclusionMap), properties, false);
            _OcclusionStrength = FindProperty(nameof(_OcclusionStrength), properties, false);

            _Emission = FindProperty(nameof(_Emission), properties, false);
            _EmissionColor = FindProperty(nameof(_EmissionColor), properties, false);
            _EmissionMap = FindProperty(nameof(_EmissionMap), properties, false);

            _DetailMask = FindProperty(nameof(_DetailMask), properties, false);
            _DetailAlbedoMapScale = FindProperty(nameof(_DetailAlbedoMapScale), properties, false);
            _DetailAlbedoMap = FindProperty(nameof(_DetailAlbedoMap), properties, false);
            _DetailNormalMapScale = FindProperty(nameof(_DetailNormalMapScale), properties, false);
            _DetailNormalMap = FindProperty(nameof(_DetailNormalMap), properties, false);

            _SpecularHighlights = FindProperty(nameof(_SpecularHighlights), properties, false);
            _EnvironmentReflections = FindProperty(nameof(_EnvironmentReflections), properties, false);

            _SSSMap = FindProperty(nameof(_SSSMap), properties, false);
            _SSS_Power = FindProperty(nameof(_SSS_Power), properties, false);
            _SSS_Distortion = FindProperty(nameof(_SSS_Distortion), properties, false);
            _SSS_Scale = FindProperty(nameof(_SSS_Scale), properties, false);
            _SSS_Color = FindProperty(nameof(_SSS_Color), properties, false);

            _DecalColor = FindProperty(nameof(_DecalColor), properties, false);
            _DecalTexture = FindProperty(nameof(_DecalTexture), properties, false);
            _DecalTextureB = FindProperty(nameof(_DecalTextureB), properties, false);

            _Cull = FindProperty(nameof(_Cull), properties, false);
            _Blend = FindProperty(nameof(_Blend), properties, false);
            _ZWrite = FindProperty(nameof(_ZWrite), properties, false);
            _Surface = FindProperty(nameof(_Surface), properties, false);
            _Debug = FindProperty(nameof(_Debug), properties, false);

            _AcessibilityColor = FindProperty(nameof(_AcessibilityColor), properties, false);
            _FresnelDebug = FindProperty(nameof(_FresnelDebug), properties, false);
            _Tiling = FindProperty(nameof(_Tiling), properties, false);
            _BrushStrokeTexture = FindProperty(nameof(_BrushStrokeTexture), properties, false);


        }
    }

    public bool surfaceInputs = true;
    public bool surfaceOptions = true;
    public bool detailInputs = true;
    public bool emission = false;
    public bool advancedOptions = true;
    public bool additionalPasses = true;

    public BlendModes blendModeSelected;
    public CullingModes cullingModeSelected;
    public WorkflowMode workflowMode;
    public Source specularSource;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;
        UberLitProperties uberLitProperties = new UberLitProperties(properties);
        //base.OnGUI(materialEditor, properties);

        var boldtext = new GUIStyle(GUI.skin.label);
        boldtext.fontStyle = FontStyle.Bold;

        surfaceOptions = EditorGUILayout.Foldout(surfaceOptions, "Surface Options");
        if (surfaceOptions) 
        {
            workflowMode = (WorkflowMode)EditorGUILayout.EnumPopup("Surface Type", (WorkflowMode)uberLitProperties._WorkflowMode.floatValue);
            
            cullingModeSelected = (CullingModes)EditorGUILayout.EnumPopup("Render Face", (CullingModes)uberLitProperties._Cull.floatValue);
            SetupFaceCulling(material, cullingModeSelected);

            blendModeSelected = (BlendModes)EditorGUILayout.EnumPopup("Blend Mode", (BlendModes)uberLitProperties._Blend.floatValue);
            uberLitProperties._Blend.floatValue = (int)blendModeSelected;
            SetupBlendMode(material, blendModeSelected);
            if (blendModeSelected != BlendModes.Opaque)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                uberLitProperties._Surface.floatValue = 1.0f;
            }
            else 
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                uberLitProperties._Surface.floatValue = 0.0f;
            }

        }


        surfaceInputs = EditorGUILayout.Foldout(surfaceInputs, "Surface Inputs");
        if (surfaceInputs)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(uberLitProperties._BaseMap.displayName), uberLitProperties._BaseMap, uberLitProperties._BaseColor);
            SpecularSetup(material, workflowMode, materialEditor, uberLitProperties);

            specularSource = (Source)EditorGUILayout.EnumPopup("Source", (Source)uberLitProperties._SmoothnessTextureChannel.floatValue);
            uberLitProperties._SmoothnessTextureChannel.floatValue = (int)specularSource;
            SpecularSource(material, specularSource, materialEditor, uberLitProperties);
            materialEditor.ShaderProperty(uberLitProperties._Smoothness, uberLitProperties._Smoothness.displayName);

            materialEditor.TexturePropertySingleLine(new GUIContent(uberLitProperties._BumpMap.displayName), uberLitProperties._BumpMap);
            if (uberLitProperties._BumpMap.textureValue != null) 
            {
                material.EnableKeyword("_NORMALMAP");
                materialEditor.ShaderProperty(uberLitProperties._BumpScale, uberLitProperties._BumpScale.displayName);
            }
            else 
            {
                material.DisableKeyword("_NORMALMAP");
            }

            materialEditor.TexturePropertySingleLine(new GUIContent(uberLitProperties._OcclusionMap.displayName), uberLitProperties._OcclusionMap);
            if (uberLitProperties._OcclusionMap.textureValue != null)
            {
                material.EnableKeyword("_OCCLUSIONMAP");
                materialEditor.ShaderProperty(uberLitProperties._OcclusionStrength, uberLitProperties._OcclusionStrength.displayName);
            }
            else 
            {
                material.DisableKeyword("_OCCLUSIONMAP");
            }

            materialEditor.ShaderProperty(uberLitProperties._Emission, uberLitProperties._Emission.displayName);
            if (uberLitProperties._Emission.floatValue > 0)
            {
                materialEditor.TexturePropertyWithHDRColor(new GUIContent(uberLitProperties._EmissionMap.displayName), uberLitProperties._EmissionMap, uberLitProperties._EmissionColor, false);
                material.EnableKeyword("_EMISSION");
                materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            materialEditor.TexturePropertySingleLine(new GUIContent(uberLitProperties._SSSMap.displayName), uberLitProperties._SSSMap);
            if (uberLitProperties._SSSMap.textureValue != null)
            {
                material.EnableKeyword("_SUBSURFACE_SCATTER");
                //material.DisableKeyword("_SUBSURFACE_SCATTER");

                materialEditor.ShaderProperty(uberLitProperties._SSS_Power, uberLitProperties._SSS_Power.displayName);
                materialEditor.ShaderProperty(uberLitProperties._SSS_Distortion, uberLitProperties._SSS_Distortion.displayName);
                materialEditor.ShaderProperty(uberLitProperties._SSS_Scale, uberLitProperties._SSS_Scale.displayName);
                materialEditor.ShaderProperty(uberLitProperties._SSS_Color, uberLitProperties._SSS_Color.displayName);
            }
            else 
            {
                material.DisableKeyword("_SUBSURFACE_SCATTER");
            }


            materialEditor.TextureScaleOffsetProperty(uberLitProperties._BaseMap);
        }
        
        materialEditor.ShaderProperty(uberLitProperties._Debug, uberLitProperties._Debug.displayName);

        detailInputs = EditorGUILayout.Foldout(detailInputs, "Detail Inputs");
        if (detailInputs) 
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Mask"), uberLitProperties._DetailMask);
            materialEditor.TexturePropertySingleLine(new GUIContent("Base Map"), uberLitProperties._DetailAlbedoMap);
            if (uberLitProperties._DetailAlbedoMap.textureValue != null)
            {
                materialEditor.ShaderProperty(uberLitProperties._DetailAlbedoMapScale, uberLitProperties._DetailAlbedoMapScale.displayName);
            }
            materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), uberLitProperties._DetailNormalMap);
            if (uberLitProperties._DetailNormalMap.textureValue != null)
            {
                materialEditor.ShaderProperty(uberLitProperties._DetailNormalMapScale, uberLitProperties._DetailNormalMapScale.displayName);
            }

            if (uberLitProperties._DetailNormalMap.textureValue != null || uberLitProperties._DetailAlbedoMap.textureValue != null)
            {
                if (uberLitProperties._DetailAlbedoMapScale.floatValue != 1)
                {
                    material.EnableKeyword("_DETAIL_SCALED");
                    material.DisableKeyword("_DETAIL_MULX2");
                }
                else
                {
                    material.DisableKeyword("_DETAIL_SCALED");
                    material.EnableKeyword("_DETAIL_MULX2");
                }
                materialEditor.TextureScaleOffsetProperty(uberLitProperties._DetailAlbedoMap);
            }
            else 
            {
                material.DisableKeyword("_DETAIL_MULX2");
                material.DisableKeyword("_DETAIL_SCALED");
            }

            if (uberLitProperties._DecalColor != null)
            {
                materialEditor.ShaderProperty(uberLitProperties._DecalColor, uberLitProperties._DecalColor.displayName);
                materialEditor.TexturePropertySingleLine(new GUIContent("Decal Map A"), uberLitProperties._DecalTexture);
                materialEditor.TexturePropertySingleLine(new GUIContent("Decal Map B"), uberLitProperties._DecalTextureB);
            }
        }

        advancedOptions = EditorGUILayout.Foldout(advancedOptions, "Advanced Options");
        if (advancedOptions)
        {
            materialEditor.ShaderProperty(uberLitProperties._SpecularHighlights, uberLitProperties._SpecularHighlights.displayName);
            materialEditor.ShaderProperty(uberLitProperties._EnvironmentReflections, uberLitProperties._EnvironmentReflections.displayName);
        }

        additionalPasses = EditorGUILayout.Foldout(additionalPasses, "Additional Passes");
        if (additionalPasses) 
        {
            materialEditor.ShaderProperty(uberLitProperties._AcessibilityColor, uberLitProperties._AcessibilityColor.displayName);
            materialEditor.ShaderProperty(uberLitProperties._FresnelDebug, uberLitProperties._FresnelDebug.displayName);
            materialEditor.ShaderProperty(uberLitProperties._Tiling, uberLitProperties._Tiling.displayName);
            materialEditor.TexturePropertySingleLine(new GUIContent(uberLitProperties._BrushStrokeTexture.displayName), uberLitProperties._BrushStrokeTexture);
        }



    }

    public void SpecularSetup(Material material, WorkflowMode workflowMode, MaterialEditor materialEditor, UberLitProperties uberLitProperties)
    {
        switch (workflowMode)
        {
            case WorkflowMode.Specular:
                materialEditor.TexturePropertySingleLine(new GUIContent(uberLitProperties._SpecGlossMap.displayName), uberLitProperties._SpecGlossMap, uberLitProperties._SpecColor);
                material.EnableKeyword("_SPECULAR_SETUP");
                if (uberLitProperties._SpecGlossMap.textureValue != null)
                {
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                else
                {
                    material.DisableKeyword("_METALLICSPECGLOSSMAP");
                }
                break;
            case WorkflowMode.Metallic:
                materialEditor.TexturePropertySingleLine(new GUIContent(uberLitProperties._MetallicGlossMap.displayName), uberLitProperties._MetallicGlossMap, uberLitProperties._Metallic);
                material.DisableKeyword("_SPECULAR_SETUP");
                if (uberLitProperties._MetallicGlossMap.textureValue != null)
                {
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                else
                {
                    material.DisableKeyword("_METALLICSPECGLOSSMAP");
                }
                break;
            default:
                break;
        }
    }

    public void SpecularSource(Material material, Source specularSource, MaterialEditor materialEditor, UberLitProperties uberLitProperties)
    {
        switch (specularSource)
        {
            case Source.SpecularAlpha:
                material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                break;
            case Source.AlbedoAlpha:
                material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                break;
            default:
                break;
        }
    }

    public void SetupFaceCulling(Material material, CullingModes cullingModes)
    {
        switch (cullingModes)
        {
            case CullingModes.Off:
                material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                break;
            case CullingModes.Front:
                material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
                break;
            case CullingModes.Back:
                material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
                break;
        }
    }

    public void SetupBlendMode(Material material, BlendModes blendModes)
    {
        switch (blendModes)
        {
            case BlendModes.Opaque:
                material.SetOverrideTag("Queue", "Geometry");
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                material.SetInt("_Pass", (int)UnityEngine.Rendering.StencilOp.Replace);
                break;
            case BlendModes.AlphaBlend:
                //Cambia la tag a Transparent
                material.SetOverrideTag("Queue", "Transparent");
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetOverrideTag("IgnoreProjector", "True");
                //Cambia el Src y Dst
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_Additive", 0);
                //Desactiva el ZWrite
                material.SetInt("_ZWrite", 0);
                //Cambia la Render Queue
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetInt("_Pass", (int)UnityEngine.Rendering.StencilOp.Keep);
                break;
            case BlendModes.Additive:
                material.SetOverrideTag("Queue", "Transparent");
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetOverrideTag("IgnoreProjector", "True");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_ZWrite", 0);
                material.SetInt("_Pass", (int)UnityEngine.Rendering.StencilOp.Keep);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
        }
    }

}

