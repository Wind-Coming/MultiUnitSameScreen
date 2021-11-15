using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

public class CustomShaderGUI : ShaderGUI
{

    public enum BlendMode
    {
        Blend,
        Add,
    }

    MaterialProperty blendMode = null;

    MaterialProperty _ZWriteA = null;

    MaterialProperty _UseCustomData = null;

    MaterialProperty _MainTex = null;

    MaterialProperty _SetColor = null;
    MaterialProperty _MainColor = null;
    MaterialProperty _MainColorIntensity = null;

    MaterialProperty _MainUvScroll = null;
    MaterialProperty _MainTexUV_speed = null;

    MaterialProperty _UseMask = null;
    MaterialProperty _MaskTex = null;

    MaterialProperty _UseNoise = null;
    MaterialProperty _NoiesTex = null;
    MaterialProperty _Noise_speed = null;
    MaterialProperty _NioesIntensity = null;

    MaterialProperty _DissolveFactor = null;
    MaterialProperty _DissolveTex = null;

    MaterialProperty _UseFresnal = null;
    MaterialProperty _FresnalColor = null;
    MaterialProperty _FresnalPow = null;
    MaterialProperty _FresnalIntensity = null;


    MaterialEditor m_MaterialEditor;

    public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));

    public void FindProperties(MaterialProperty[] props)
    {
        blendMode = FindProperty("_Mode", props);

        _ZWriteA = FindProperty("_ZWriteA", props);

        _UseCustomData = FindProperty("_UseCustomUv", props);

        _MainTex = FindProperty("_MainTex", props);

        _SetColor = FindProperty("_SetColor", props);
        _MainColor = FindProperty("_MainColor", props);
        _MainColorIntensity = FindProperty("_MainColorIntensity", props);

        _MainUvScroll = FindProperty("_MainUvScroll", props);
        _MainTexUV_speed = FindProperty("_MainTexUV_speed", props);

        _UseMask = FindProperty("_UseMask", props);
        _MaskTex = FindProperty("_MaskTex", props);

        _UseNoise = FindProperty("_UseNoise", props);
        _NoiesTex = FindProperty("_NoiesTex", props);
        _Noise_speed = FindProperty("_Noise_speed", props);
        _NioesIntensity = FindProperty("_NioesIntensity", props);

        _DissolveFactor = FindProperty("_DissolveFactor", props);
        _DissolveTex = FindProperty("_DissolveTex", props);

        _UseFresnal = FindProperty("_UseFresnal", props);
        _FresnalColor = FindProperty("_FresnalColor", props);
        _FresnalPow = FindProperty("_FresnalPow", props);
        _FresnalIntensity = FindProperty("_FresnalIntensity", props);
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        //选择这个shader的时候，可以设置一些默认值
        if (blendMode != null) {
            SetupMaterialWithBlendMode(material, (BlendMode)blendMode.floatValue);
        }
        else {
            SetupMaterialWithBlendMode(material, 0);
        }
    }

    override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // render the shader properties using the default GUI
        //base.OnGUI(materialEditor, properties);

        FindProperties(properties);
        m_MaterialEditor = materialEditor;
        Material targetMat = materialEditor.target as Material;
        ShaderPropertiesGUI(targetMat);
    }

    public void ShaderPropertiesGUI(Material material)
    {
        EditorGUI.BeginChangeCheck();

        var mode = (BlendMode)blendMode.floatValue;

        EditorGUI.BeginChangeCheck();

        mode = (BlendMode)EditorGUILayout.Popup("混合模式", (int)mode, blendNames);

        if (EditorGUI.EndChangeCheck()) {
            blendMode.floatValue = (float)mode;
            SetupMaterialWithBlendMode(material, mode);
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        m_MaterialEditor.ShaderProperty(_ZWriteA, "ZWrite");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        m_MaterialEditor.ShaderProperty(_UseCustomData, "使用自定义UV");
        GUILayout.Label("在CustomData -> Custom1中");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        m_MaterialEditor.TextureProperty(_MainTex, "MainTex");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        ChangeColor();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        MianTexUV();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        UseMask();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        UseNoise();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        UseDissolve();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        UseFresnal();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        m_MaterialEditor.RenderQueueField();

        if (EditorGUI.EndChangeCheck()) {
        }
    }

    public void ChangeColor()
    {
        m_MaterialEditor.ShaderProperty(_SetColor, "开启调色");
        {
            if (_SetColor.floatValue == 1) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_MainColor, "颜色");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_MainColorIntensity, "强度");
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    public void MianTexUV()
    {
        m_MaterialEditor.ShaderProperty(_MainUvScroll, "开启MainTex滚动");
        {
            if (_MainUvScroll.floatValue == 1) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("循环滚动请设置滚动速度，一次性滚动请用CustomData的xy变换");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_MainTexUV_speed, "速度");
                EditorGUILayout.EndHorizontal();
          }
        }
    }


    public void UseMask()
    {
        m_MaterialEditor.ShaderProperty(_UseMask, "使用Mask图");
        {
            if (_UseMask.floatValue == 1) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("只使用了r通道，可将贴图设为单通道图");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_MaskTex, "MaskTex");
                EditorGUILayout.EndHorizontal();
            }
        }
    }


    public void UseNoise()
    {
        m_MaterialEditor.ShaderProperty(_UseNoise, "使用噪点");
        {
            if (_UseNoise.floatValue == 1) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("只使用了r通道，可将贴图设为单通道图");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_NoiesTex, "NoiseTex");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_Noise_speed, "滚动速度");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_NioesIntensity, "强度");
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    public void UseDissolve()
    {
        m_MaterialEditor.ShaderProperty(_DissolveFactor, "使用溶解");
        {
            if (_DissolveFactor.floatValue == 1) {

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("只使用了r通道，可将贴图设为单通道图，用CustomData中Vector4中的Z值控制溶解");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_DissolveTex, "DissolveTex");
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    public void UseFresnal()
    {
        m_MaterialEditor.ShaderProperty(_UseFresnal, "菲尼尔");
        {
            if (_UseFresnal.floatValue == 1) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_FresnalColor, "颜色");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_FresnalPow, "范围");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                m_MaterialEditor.ShaderProperty(_FresnalIntensity, "强度");
                EditorGUILayout.EndHorizontal();
            }
        }
    }


    public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
    {
        switch (blendMode) {
            case BlendMode.Add:
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); 
                material.SetFloat("_DestBlend", 1);
                break;
            case BlendMode.Blend:
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DestBlend", 10);
                break;
        }
    }


}