using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RetroShaderMaterialEditor : ShaderGUI
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

    public BlendModes blendModeSelected;
    public CullingModes cullingSelected;
    public Vector2 panningSpeed;
    public int ID;
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        #region MATERIAL PROPERTIES
        MaterialProperty _Cull = FindProperty(nameof(_Cull), properties);
        MaterialProperty _BlendMode = FindProperty(nameof(_BlendMode), properties);
        MaterialProperty _SrcBlend = FindProperty(nameof(_SrcBlend), properties);
        MaterialProperty _DstBlend = FindProperty(nameof(_DstBlend), properties);
        MaterialProperty _ZWrite = FindProperty(nameof(_ZWrite), properties);
        MaterialProperty _AlphaClipping = FindProperty(nameof(_AlphaClipping), properties);
        MaterialProperty _Cutoff = FindProperty(nameof(_Cutoff), properties);

        MaterialProperty _PanningSpeed = FindProperty(nameof(_PanningSpeed), properties);
        MaterialProperty _EnablePanning = FindProperty(nameof(_EnablePanning), properties);


        MaterialProperty _Albedo = FindProperty(nameof(_Albedo), properties);
        MaterialProperty _BaseColor = FindProperty(nameof(_BaseColor), properties);

        MaterialProperty _UseMSTexture = FindProperty(nameof(_UseMSTexture), properties);
        MaterialProperty _MetallicSmoothness = FindProperty(nameof(_MetallicSmoothness), properties);
        MaterialProperty _Metallic = FindProperty(nameof(_Metallic), properties);
        MaterialProperty _Smoothness = FindProperty(nameof(_Smoothness), properties);

        //MaterialProperty _UseNormalMap = FindProperty(nameof(_UseNormalMap), properties);
        MaterialProperty _Normal = FindProperty(nameof(_Normal), properties);
        MaterialProperty _NormalStrength = FindProperty(nameof(_NormalStrength), properties);

        MaterialProperty _UseEmission = FindProperty(nameof(_UseEmission), properties);
        MaterialProperty _EmissionMap = FindProperty(nameof(_EmissionMap), properties);
        MaterialProperty _EmissionColor = FindProperty(nameof(_EmissionColor), properties);

        //MaterialProperty _UseOcclusion = FindProperty(nameof(_UseOcclusion), properties);
        MaterialProperty _Occlusion = FindProperty(nameof(_Occlusion), properties);

        MaterialProperty _UseSplatter = FindProperty(nameof(_UseSplatter), properties);
        MaterialProperty _SplatterTexture = FindProperty(nameof(_SplatterTexture), properties);
        MaterialProperty _SplatterColor = FindProperty(nameof(_SplatterColor), properties);

        MaterialProperty _UseSplatterEmission = FindProperty(nameof(_UseSplatterEmission), properties);
        MaterialProperty _SplatterEmission = FindProperty(nameof(_SplatterEmission), properties);
        MaterialProperty _SplatterEmissionColor = FindProperty(nameof(_SplatterEmissionColor), properties);

        MaterialProperty _SplatterAmount = FindProperty(nameof(_SplatterAmount), properties);

        MaterialProperty _ID = FindProperty(nameof(_ID), properties);
        MaterialProperty _NoFPU = FindProperty(nameof(_NoFPU), properties);
        //MaterialProperty _HorizontalResolution = FindProperty(nameof(_HorizontalResolution), properties);
        //MaterialProperty _VerticalResolution = FindProperty(nameof(_VerticalResolution), properties);
        MaterialProperty _AffineTextureMapping = FindProperty(nameof(_AffineTextureMapping), properties);
        MaterialProperty _DisableAffineLocally = FindProperty(nameof(_DisableAffineLocally), properties);
        MaterialProperty _DisableFPU = FindProperty(nameof(_DisableFPU), properties);
        MaterialProperty _HitEmission = FindProperty(nameof(_HitEmission), properties); 
        MaterialProperty _CubemapReflection = FindProperty(nameof(_CubemapReflection), properties);
        MaterialProperty _UseCubeMap = FindProperty(nameof(_UseCubeMap), properties);
        MaterialProperty _CubemapColor = FindProperty(nameof(_CubemapColor), properties);
        MaterialProperty _FresnelPower = FindProperty(nameof(_FresnelPower), properties);
        MaterialProperty _FresnelIntensity = FindProperty(nameof(_FresnelIntensity), properties);
        MaterialProperty _CubemapMask = FindProperty(nameof(_CubemapMask), properties);
        #endregion

        var boldtext = new GUIStyle(GUI.skin.label);
        boldtext.fontStyle = FontStyle.Bold;


        //EDITOR UI----------------------------

        //ALPHA MODES
        EditorGUILayout.LabelField("Culling & Blend Modes", boldtext);
        EditorGUILayout.Space(4);
        
        cullingSelected = (CullingModes)EditorGUILayout.EnumPopup("Cull", (CullingModes)_Cull.floatValue);
        _Cull.floatValue = (int)cullingSelected;
        SetupFaceCulling(material, cullingSelected);

        EditorGUI.BeginChangeCheck();
        blendModeSelected = (BlendModes)EditorGUILayout.EnumPopup("Blend Mode", (BlendModes)_BlendMode.floatValue);
        _BlendMode.floatValue = (int)blendModeSelected;
        if (EditorGUI.EndChangeCheck())
        {
            SetupBlendMode(material, blendModeSelected);
        }
        materialEditor.ShaderProperty(_AlphaClipping, _AlphaClipping.displayName);
        if (_AlphaClipping.floatValue > 0)
        {
            materialEditor.ShaderProperty(_Cutoff, _Cutoff.displayName);
        }
        SeparatorLine();
        //END ALPHA MODES

        //PANNING
        EditorGUILayout.LabelField("Animate UV's", boldtext);
        EditorGUILayout.Space(4);
        materialEditor.ShaderProperty(_EnablePanning, _EnablePanning.displayName);
        if (_EnablePanning.floatValue > 0)
        {
            panningSpeed = EditorGUILayout.Vector2Field("Panning Scroll Speed", _PanningSpeed.vectorValue);
            _PanningSpeed.vectorValue = panningSpeed;
        }
        SeparatorLine();
        //END PANNING

        //COLOR
        EditorGUILayout.LabelField("Color", boldtext);
        EditorGUILayout.Space(4);
        materialEditor.TextureProperty(_Albedo, _Albedo.displayName);
        materialEditor.ShaderProperty(_BaseColor, _BaseColor.displayName);
        materialEditor.ShaderProperty(_HitEmission, _HitEmission.displayName);
        SeparatorLine();
        //END COLOR

        //METALLIC/SMOOTHNESS
        EditorGUILayout.LabelField("PBR Properties", boldtext);
        EditorGUILayout.Space(4);
        materialEditor.ShaderProperty(_UseMSTexture, _UseMSTexture.displayName);
        if (_UseMSTexture.floatValue > 0)
        {
			materialEditor.TexturePropertySingleLine(new GUIContent(_MetallicSmoothness.displayName), _MetallicSmoothness, _Metallic);
            materialEditor.ShaderProperty(_Smoothness, _Smoothness.displayName);
        }
        else
        {
            materialEditor.ShaderProperty(_Metallic, _Metallic.displayName);
            materialEditor.ShaderProperty(_Smoothness, _Smoothness.displayName);
        }
        SeparatorLine();
        //END METALLIC/SMOOTHNESS

        //EMISSION
        EditorGUILayout.LabelField("Emission", boldtext);
        EditorGUILayout.Space(4);
        materialEditor.ShaderProperty(_UseEmission, _UseEmission.displayName);
        if (_UseEmission.floatValue > 0)
        {
            //materialEditor.LightmapEmissionProperty();
            materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
            materialEditor.TexturePropertySingleLine(new GUIContent(_EmissionMap.displayName), _EmissionMap, _EmissionColor);
        }
        SeparatorLine();
        //END EMISSION

        //SPLATTER
        EditorGUILayout.LabelField("Splatter", boldtext);
        EditorGUILayout.Space(4);
        materialEditor.ShaderProperty(_UseSplatter, _UseSplatter.displayName);
        if (_UseSplatter.floatValue > 0)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(_SplatterTexture.displayName), _SplatterTexture);
            materialEditor.TextureProperty(_SplatterColor, _SplatterColor.displayName);
        }
        materialEditor.ShaderProperty(_UseSplatterEmission, _UseSplatterEmission.displayName);
        if (_UseSplatterEmission.floatValue > 0)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(_SplatterEmission.displayName), _SplatterEmission,_SplatterEmissionColor);
        }
        materialEditor.ShaderProperty(_SplatterAmount, _SplatterAmount.displayName);
        SeparatorLine();

        EditorGUILayout.LabelField("Cubemap Reflection", boldtext);
        EditorGUILayout.Space(4);
        materialEditor.ShaderProperty(_UseCubeMap, _UseCubeMap.displayName);
        if (_UseCubeMap.floatValue > 0)
        {
            materialEditor.TextureProperty(_CubemapReflection, _CubemapReflection.displayName);
            materialEditor.TextureProperty(_CubemapMask, _CubemapMask.displayName);
            materialEditor.ShaderProperty(_CubemapColor, _CubemapColor.displayName);
            materialEditor.ShaderProperty(_FresnelPower, _FresnelPower.displayName);
            materialEditor.ShaderProperty(_FresnelIntensity, _FresnelIntensity.displayName);
        }
        else
        {
            _CubemapColor.colorValue = Color.black;
        }

            SeparatorLine();

        //END SPLATTER

        //NORMAL
        EditorGUILayout.LabelField("Normal Mapping", boldtext);
        EditorGUILayout.Space(4);
        materialEditor.TexturePropertySingleLine(new GUIContent(_Normal.displayName), _Normal, _NormalStrength);
        
        SeparatorLine();
        //END NORMAL

        //OCLUSSION
        EditorGUILayout.LabelField("AO", boldtext);
        EditorGUILayout.Space(4);
        
        materialEditor.TexturePropertySingleLine(new GUIContent(_Occlusion.displayName), _Occlusion);
        
        SeparatorLine();
        //END OCLUSSION

        //EFFECTS
        EditorGUILayout.LabelField("Effects & Accesibility", boldtext);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Stencil Buffer ID");
        ID = EditorGUILayout.IntSlider((int)_ID.floatValue, 0, 15);
        _ID.floatValue = ID;
        _BlendMode.floatValue = (int)blendModeSelected;
        materialEditor.ShaderProperty(_DisableAffineLocally, _DisableAffineLocally.displayName);
        materialEditor.ShaderProperty(_DisableFPU, "Disable No FPU Locally");

        SeparatorLine();
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
        //materialEditor.EmissionEnabledProperty();

        //materialEditor.ShaderProperty(_AffineTextureMapping, _AffineTextureMapping.displayName);
        //materialEditor.ShaderProperty(_NoFPU, _NoFPU.displayName);
    }

    public void SetupBlendMode (Material material, BlendModes blendModes)
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

    void SeparatorLine()
    {
        EditorGUILayout.Space(8);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.5f, 0.5f, 0.5f, 1));
        EditorGUILayout.Space(8);
    }

}
