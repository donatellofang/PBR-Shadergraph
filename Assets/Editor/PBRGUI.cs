using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class PBRGUI : ShaderGUI
{


    bool showColorsSection, showLightingSection, showRimSection, showTransitionSection, showOutlineControlSection, showHatchingSection, showVertexExtrusionSection;

    Material targetMat;

    public static Gradient colorRampGradient = new Gradient();

    public delegate void DrawSettingsMethod(MaterialEditor materialEditor, MaterialProperty[] properties);

    static GUIStyle _centeredGreyMiniLabel;
    public static GUIStyle CenteredGreyMiniLabel
    {
        get
        {
            if (_centeredGreyMiniLabel == null)
            {
                _centeredGreyMiniLabel = new GUIStyle(GUI.skin.FindStyle("MiniLabel") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("MiniLabel"))
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.gray }
                };

                // _centeredGreyMiniLabel = new GUIStyle(UnityEditor.EditorStyles.label);
                // _centeredGreyMiniLabel.normal.textColor = Color.gray;
                //_centeredGreyMiniLabel.alignment = TextAnchor.MiddleCenter;
            }
            return _centeredGreyMiniLabel;
        }
    }



    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {

        targetMat = materialEditor.target as Material;

        EditorGUILayout.LabelField("Unlit Master Shader v1.1.0", EditorStyles.boldLabel);
        if (GUILayout.Button("@alexanderameye", CenteredGreyMiniLabel)) Application.OpenURL("https://twitter.com/alexanderameye");
        EditorGUILayout.Space();
        CoreEditorUtils.DrawSplitter();

        showColorsSection = CoreEditorUtils.DrawHeaderFoldout("Color", showColorsSection, false, (Func<bool>)null, null);
        DrawPropertiesInspector(showColorsSection, materialEditor, properties, DrawColorsSettings);

        showLightingSection = CoreEditorUtils.DrawHeaderFoldout("Lighting", showLightingSection, false, (Func<bool>)null, null);
        DrawPropertiesInspector(showLightingSection, materialEditor, properties, DrawLightingSettings);

        showRimSection = CoreEditorUtils.DrawHeaderFoldout("Rim", showRimSection, false, (Func<bool>)null, null);
        DrawPropertiesInspector(showRimSection, materialEditor, properties, DrawRimSettings);

        showHatchingSection = CoreEditorUtils.DrawHeaderFoldout("Surface Effects", showHatchingSection, false, (Func<bool>)null, null);
        DrawPropertiesInspector(showHatchingSection, materialEditor, properties, DrawHatchingSettings);

        showOutlineControlSection = CoreEditorUtils.DrawHeaderFoldout("Outline", showOutlineControlSection, false, (Func<bool>)null, null);
        DrawPropertiesInspector(showOutlineControlSection, materialEditor, properties, DrawOutlineSettings);

        showVertexExtrusionSection = CoreEditorUtils.DrawHeaderFoldout("Vertex Distortion", showVertexExtrusionSection, false, (Func<bool>)null, null);
        DrawPropertiesInspector(showVertexExtrusionSection, materialEditor, properties, DrawVertexExtrusionSettings);
        EditorGUILayout.Space();


    }


    void DrawColorsSettings(MaterialEditor editor, MaterialProperty[] properties)
    {
        MaterialProperty litColor = FindProperty("_LitColor", properties);
        MaterialProperty colorTexture = FindProperty("_ColorTexture", properties);
        MaterialProperty rampTexture = FindProperty("_RampTexture", properties);
        MaterialProperty shadedColor = FindProperty("_ShadedColor", properties);
        MaterialProperty colorSteps = FindProperty("_ColorSteps", properties);
        MaterialProperty colorSpread = FindProperty("_ColorSpread", properties);
        MaterialProperty colorOffset = FindProperty("_ColorOffset", properties);
        MaterialProperty hardness = FindProperty("_TransitionHardness", properties);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        MaterialProperty control = FindProperty("_COLORSOURCE", properties);
        editor.ShaderProperty(control, "Source");
        if (Array.IndexOf(targetMat.shaderKeywords, "_COLORSOURCE_GRADIENT") != -1)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.GradientField("Ramp", colorRampGradient);
            if (GUILayout.Button("Apply")) ApplyRampTexture();
            EditorGUILayout.EndHorizontal();
        }
        else if (Array.IndexOf(targetMat.shaderKeywords, "_COLORSOURCE_RAMP") != -1)
        {
            editor.ShaderProperty(rampTexture, "Gradient");
        }
        else if (Array.IndexOf(targetMat.shaderKeywords, "_COLORSOURCE_COLOR") != -1)
        {
            editor.ShaderProperty(litColor, "Lit");
            editor.ShaderProperty(shadedColor, "Shaded");
        }
        else if (Array.IndexOf(targetMat.shaderKeywords, "_COLORSOURCE_TEXTURE") != -1)
        {
            editor.ShaderProperty(colorTexture, "Texture");

        }



        if (Array.IndexOf(targetMat.shaderKeywords, "_COLORSOURCE_TEXTURE") == -1)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
            editor.ShaderProperty(colorSpread, "Spread");
            editor.ShaderProperty(colorOffset, "Offset");
        }


        if (Array.IndexOf(targetMat.shaderKeywords, "_COLORSOURCE_COLOR") != -1)
        {

            editor.ShaderProperty(hardness, "Hardness");
            editor.ShaderProperty(colorSteps, "Steps");
        }
        EditorGUILayout.Separator();
    }

    public void ApplyRampTexture()
    {
        targetMat.SetTexture("_RampTexture", CreateGradientTexture(targetMat, colorRampGradient));
    }


    public static int width = 256;
    public static int height = 4; // needs to be multiple of 4 for DXT1 format compression

    public static Texture2D CreateGradientTexture(Material targetMaterial, Gradient gradient)
    {
        Texture2D gradientTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
        {
            name = "_LUT",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            alphaIsTransparency = true,

        };

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++) gradientTexture.SetPixel(i, j, gradient.Evaluate((float)i / (float)width));
        }

        gradientTexture.Apply(false);
        gradientTexture = SaveAndGetTexture(targetMaterial, gradientTexture);
        return gradientTexture;
    }

    private static Texture2D SaveAndGetTexture(Material targetMaterial, Texture2D sourceTexture)
    {
        string targetFolder = AssetDatabase.GetAssetPath(targetMaterial);
        targetFolder = targetFolder.Replace(targetMaterial.name + ".mat", string.Empty);

        targetFolder += "Color Ramp Textures/";

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
            AssetDatabase.Refresh();
        }

        string path = targetFolder + targetMaterial.name + sourceTexture.name + ".asset";
        // File.WriteAllBytes(path, sourceTexture.EncodeToPNG());

        AssetDatabase.CreateAsset(sourceTexture, path);
        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
        // AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
        sourceTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

        return sourceTexture;
    }


    void DrawLightingSettings(MaterialEditor editor, MaterialProperty[] properties)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Highlight", EditorStyles.boldLabel);
        editor.ShaderProperty(FindProperty("_HighlightColor", properties), "Color");
        editor.ShaderProperty(FindProperty("_HighlightSize", properties), "Size");
        editor.ShaderProperty(FindProperty("_HighlightOpacity", properties), "Opacity");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
        EditorGUILayout.Separator();
    }

    void DrawRimSettings(MaterialEditor editor, MaterialProperty[] properties)
    {
        MaterialProperty color = FindProperty("_RimColor", properties);
        MaterialProperty amount = FindProperty("_RimAmount", properties);
        MaterialProperty opacity = FindProperty("_RimOpacity", properties);
        MaterialProperty hardness = FindProperty("_RimHardness", properties);

        EditorGUILayout.Space();
        editor.ShaderProperty(color, "Color");
        editor.ShaderProperty(amount, "Amount");
        editor.ShaderProperty(opacity, "Opacity");
        editor.ShaderProperty(hardness, "Hardness");
        EditorGUILayout.Separator();
    }

    void DrawOutlineSettings(MaterialEditor editor, MaterialProperty[] properties)
    {
        MaterialProperty outlineSource = FindProperty("_OUTLINESOURCE", properties);
        MaterialProperty showVertexColors = FindProperty("SHOWVERTEXCOLORS", properties);
        MaterialProperty outlineColor = FindProperty("_OutlineColor", properties);
        MaterialProperty texture = FindProperty("_OutlineTexture", properties);

        EditorGUILayout.Space();
        editor.ShaderProperty(outlineSource, "Source");

        if (Array.IndexOf(targetMat.shaderKeywords, "_OUTLINESOURCE_TEXTURE") != -1)
        {
            editor.ShaderProperty(texture, "Texture");
            EditorGUILayout.LabelField("Using a custom texture for sectioning.", CenteredGreyMiniLabel);
        }

        else if (Array.IndexOf(targetMat.shaderKeywords, "_OUTLINESOURCE_COLOR") != -1)
        {
            EditorGUILayout.LabelField("Using the color output for sectioning.", CenteredGreyMiniLabel);

        }

        if (Array.IndexOf(targetMat.shaderKeywords, "_OUTLINESOURCE_NONE") == -1)
        {

            editor.ShaderProperty(outlineColor, "Color Control");
            EditorGUILayout.LabelField("The special value of 0 is used for shadowing!", CenteredGreyMiniLabel);
        }

        if (Array.IndexOf(targetMat.shaderKeywords, "_OUTLINESOURCE_NONE") != -1)
        {
            EditorGUILayout.LabelField("The shader now just writes black to the sectioning texture.", CenteredGreyMiniLabel);
            EditorGUILayout.LabelField("You can fake an outline using a rim effect.", CenteredGreyMiniLabel);
        }
        editor.ShaderProperty(showVertexColors, "Show Vertex Colors");
        EditorGUILayout.LabelField("Using the color output for sectioning.", CenteredGreyMiniLabel);


        EditorGUILayout.Separator();
    }

    void DrawHatchingSettings(MaterialEditor editor, MaterialProperty[] properties)
    {
        MaterialProperty scale1 = FindProperty("_MossScale1", properties);
        MaterialProperty scale2 = FindProperty("_MossScale2", properties);
        MaterialProperty color = FindProperty("_MossColor", properties);
        MaterialProperty coverage = FindProperty("_MossCoverage", properties);
        MaterialProperty height = FindProperty("_MossHeight", properties);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Moss", EditorStyles.boldLabel);
        editor.ShaderProperty(color, "Color");
        editor.ShaderProperty(FindProperty("_MossOpacity", properties), "Opacity");
        editor.ShaderProperty(coverage, "Coverage");
        editor.ShaderProperty(height, "Height");
        editor.ShaderProperty(scale1, "Scale 1");
        editor.ShaderProperty(scale2, "Scale 2");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hatching", EditorStyles.boldLabel);
        editor.ShaderProperty(FindProperty("_HatchingOpacity", properties), "Opacity");
        EditorGUILayout.Separator();
    }


    void DrawVertexExtrusionSettings(MaterialEditor editor, MaterialProperty[] properties)
    {
        //// MaterialProperty enable = ShaderGUI.FindProperty("_VERTEXDISTORTION", properties);
        MaterialProperty strength = FindProperty("_DistortionStrength", properties);
        MaterialProperty scale = FindProperty("_DistortionScale", properties);


        editor.ShaderProperty(strength, "Strength");
        editor.ShaderProperty(scale, "Scale");

        EditorGUILayout.Separator();
    }


    private void DrawPropertiesInspector(bool active, MaterialEditor editor, MaterialProperty[] properties, string[] props)
    {
        if (active)
        {
            EditorGUI.indentLevel++;
            foreach (string prop in props)
            {
                MaterialProperty reference = FindProperty(prop, properties);
                editor.ShaderProperty(reference, reference.displayName);
            }
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
        }
        CoreEditorUtils.DrawSplitter();
    }

    private static void DrawPropertiesInspector(bool active, MaterialEditor editor, MaterialProperty[] properties, DrawSettingsMethod Drawer)
    {
        if (active)
        {
            EditorGUI.indentLevel++;
            Drawer(editor, properties);
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
        }
        CoreEditorUtils.DrawSplitter();
    }
}