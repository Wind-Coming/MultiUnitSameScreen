using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Callbacks;
#if UNITY_EDITOR
[InitializeOnLoad]
public class ImportProccess : AssetPostprocessor
{

    /// <summary>
    ///  对应CSV表中的列位置
    /// </summary>
    #region
    private static int MAXSIZE4096 = 0;
    private static int MAXSIZE2048 = 1;
    private static int MAXSIZE1024 = 2;
    private static int MAXSIZE512 = 3;
    private static int MAXSIZE256 = 4;
    private static int MAXSIZE128 = 5;
    private static int MAXSIZE64 = 6;
    private static int MAXSIZE32 = 7;
    private static int MAXSIZE16 = 8;
    private static int NPOTSCALE = 9;
    private static int WRAPMODE = 10;
    private static int FILTERMODE = 11;
    private static int MIPMAP = 12;
    private static int TEXTURERW = 13;
    private static int TEXTURETYPE = 14;
    private static int MODELRW = 15;
    private static int MODELHAVEANIM = 16;
    private static int MESHCOMPRESS = 17;
    private static int OPTIMIZEMESH = 18;
    private static int TANGENTS = 19;
    private static int OPTIMIZEGAMEOBJECT = 20;
    private static int IMPORTBLENDSHAPES = 21;
    private static int IMPORTVISIBILITY = 22;
    private static int IMPORTCAMERAS = 23;
    private static int IMPORTLIGHTS = 24;
    private static int WELDVERTICES = 25;
    private static int NORMALS = 26;
    private static int AUDIOFORCETOMONO = 27;
    private static int AETC2RGB4 = 28;
    private static int AETC2RGBA8 = 29;
    private static int AETCRGB4 = 30;
    private static int ARGB16 = 31;
    private static int ARGBA16 = 32;
    private static int ARGB24 = 33;
    private static int ARGBA32 = 34;
    private static int AASTCRGB6X6 = 35;
    private static int IPVRTCRGB2 = 36;
    private static int IPVRTCRGBA2 = 37;
    private static int IPVRTCRGB4 = 38;
    private static int IPVRTCRGBA4 = 39;
    private static int IRGB16 = 40;
    private static int IRGBA16 = 41;
    private static int IRGB24 = 42;
    private static int IRGBA32 = 43;
    private static int IASTCRGB6X6 = 44;
    private static int WDXT1 = 45;
    private static int WDXT5 = 46;
    private static int WARGB16 = 47;
    private static int WRGB24 = 48;
    private static int WRGBA32 = 49;
    private static int WARGB32 = 50;
    private static int PRIORITY = 51;
    #endregion

    private static int TextureSetEndIdx = 15;
    private static int ModelSetEndidx = 27;
    private static int AudioSetEndidx = 28;
    private static int AndroidSetEndidx = 36;
    private static int IPhoneSetEndidx = 45;

    public static bool closeTextureProcess = false;


    private class CSVData
    {
        public string mPath;
        public List<string> mParameters;
        public List<CSVData> mChild;
    }

    private static string CSVPath = "/Scripts/Editor/ImportProccessTools/按文件夹配置.csv";
    private static List<CSVData> mConfigurations = new List<CSVData>();
    static ImportProccess()
    {
        string prjpath = Application.dataPath + CSVPath;
        string[] FileDatas;
        try
        {
            Debug.Log(prjpath);
            FileDatas = File.ReadAllLines(prjpath);
            mConfigurations.Clear();
            for (int i = 1; i < FileDatas.Length; i++)
            {
                string[] LineData = FileDatas[i].Split(',');
                CSVData Specification = new CSVData();
                Specification.mParameters = new List<string>();
                Specification.mPath = LineData[0];
                for (int j = 1; j < LineData.Length; j++)
                {
                    Specification.mParameters.Add(LineData[j]);
                }
                if (Specification.mParameters[PRIORITY] == "1")
                {
                    mConfigurations.Add(Specification);
                }
                else
                {
                    foreach (var cfg in mConfigurations)
                    {
                        if (Specification.mPath.Contains(cfg.mPath))
                        {
                            if (cfg.mChild == null)
                            {
                                cfg.mChild = new List<CSVData>();
                            }
                            cfg.mChild.Add(Specification);
                        }
                    }
                }
            }
        }
        catch
        {
            Debug.Log("Don't find File.......");
        }
    }
    [MenuItem("Tools/导入设置工具/ReadCSV")]
    public static void ReadCSV()
    {
        string prjpath = Application.dataPath + CSVPath;
        string[] FileDatas;
        try
        {
            Debug.Log(prjpath);
            FileDatas = File.ReadAllLines(prjpath);
            mConfigurations.Clear();
            for (int i = 1; i < FileDatas.Length; i++)
            {
                string[] LineData = FileDatas[i].Split(',');
                CSVData Specification = new CSVData();
                Specification.mParameters = new List<string>();
                Specification.mPath = LineData[0];
                for (int j = 1; j < LineData.Length; j++)
                {
                    Specification.mParameters.Add(LineData[j]);
                }
                if (Specification.mParameters[PRIORITY] == "1")
                {
                    mConfigurations.Add(Specification);
                }
                else
                {
                    foreach (var cfg in mConfigurations)
                    {
                        if (Specification.mPath.Contains(cfg.mPath))
                        {
                            if (cfg.mChild == null)
                            {
                                cfg.mChild = new List<CSVData>();
                            }
                            cfg.mChild.Add(Specification);
                        }
                    }
                }
            }
        }
        catch
        {
            Debug.Log("Don't find File.......");
        }
    }
    private void OnPreprocessTexture()
    {
        if (closeTextureProcess == true)
            return;

        if (mConfigurations.Count != 0)
        {
            foreach (var _cfg in mConfigurations)
            {
                int sIdx = _cfg.mPath.IndexOf('A');
                if (assetPath.Contains(_cfg.mPath.Substring(sIdx)))
                {
                    CSVData cfg = ContainsFileName(_cfg);
                    TextureImporter textureImporter = assetImporter as TextureImporter;
                    TextureImporterPlatformSettings kSettings = new TextureImporterPlatformSettings();
                    bool bAlpha = CheckTextureAlpha(textureImporter);
                    if (bAlpha)
                    {
                        textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                        textureImporter.alphaIsTransparency = true;
                    }
                    else
                    {
                        textureImporter.alphaSource = TextureImporterAlphaSource.None;
                    }
                    int maxSize = 0;
                    kSettings.textureCompression = TextureImporterCompression.Uncompressed;
                    kSettings.allowsAlphaSplitting = false;
                    kSettings.overridden = true;
                    string kName = "";
                    for (int i = 0; i < TextureSetEndIdx; i++)
                    {
                        if (cfg.mParameters[i] != "0" && cfg.mParameters[i] != "")
                        {
                            switch (i)
                            {
                                #region maxTextureSize
                                case 0:
                                    {
                                        maxSize = 4096;
                                        break;
                                    }
                                case 1:
                                    {
                                        maxSize = 2048;
                                        break;
                                    }
                                case 2:
                                    {
                                        maxSize = 1024;
                                        break;
                                    }
                                case 3:
                                    {
                                        maxSize = 512;
                                        break;
                                    }
                                case 4:
                                    {
                                        maxSize = 256;
                                        break;
                                    }
                                case 5:
                                    {
                                        maxSize = 128;
                                        break;
                                    }
                                case 6:
                                    {
                                        maxSize = 64;
                                        break;
                                    }
                                case 7:
                                    {
                                        maxSize = 32;
                                        break;
                                    }
                                case 8:
                                    {
                                        maxSize = 16;
                                        break;
                                    }
                                #endregion
                                case 9:
                                    {
                                        switch (cfg.mParameters[i])
                                        {
                                            case "1":
                                                {
                                                    textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
                                                    break;
                                                }
                                            case "2":
                                                {
                                                    textureImporter.npotScale = TextureImporterNPOTScale.ToLarger;
                                                    break;
                                                }
                                            case "3":
                                                {
                                                    textureImporter.npotScale = TextureImporterNPOTScale.ToSmaller;
                                                    break;
                                                }
                                            case "4":
                                                {
                                                    textureImporter.npotScale = TextureImporterNPOTScale.None;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case 10:
                                    {
                                        switch (cfg.mParameters[i])
                                        {
                                            case "1":
                                                {
                                                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                                                    break;
                                                }
                                            case "2":
                                                {
                                                    textureImporter.wrapMode = TextureWrapMode.Mirror;
                                                    break;
                                                }
                                            case "3":
                                                {
                                                    textureImporter.wrapMode = TextureWrapMode.MirrorOnce;
                                                    break;
                                                }
                                            case "4":
                                                {
                                                    textureImporter.wrapMode = TextureWrapMode.Repeat;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case 11:
                                    {
                                        switch (cfg.mParameters[i])
                                        {
                                            case "1":
                                                {
                                                    textureImporter.filterMode = FilterMode.Point;
                                                    break;
                                                }
                                            case "2":
                                                {
                                                    textureImporter.filterMode = FilterMode.Bilinear;
                                                    break;
                                                }
                                            case "3":
                                                {
                                                    textureImporter.filterMode = FilterMode.Trilinear;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case 12:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            textureImporter.mipmapEnabled = true;
                                        }
                                        else
                                        {
                                            textureImporter.mipmapEnabled = false;
                                        }
                                        break;
                                    }
                                case 13:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            textureImporter.isReadable = true;
                                        }
                                        else
                                        {
                                            textureImporter.isReadable = false;
                                        }
                                        break;
                                    }
                                case 14:
                                    {
                                        switch (cfg.mParameters[i])
                                        {
                                            case "1":
                                                {
                                                    textureImporter.textureType = TextureImporterType.Default;
                                                    textureImporter.textureShape = TextureImporterShape.Texture2D;
                                                    break;
                                                }
                                            case "2":
                                                {
                                                    textureImporter.textureType = TextureImporterType.Sprite;
                                                    textureImporter.spriteImportMode = SpriteImportMode.Single;
                                                    break;
                                                }
                                            case "3":
                                                {
                                                    textureImporter.textureType = TextureImporterType.Lightmap;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    if (maxSize != 0)
                    {
                        kSettings.maxTextureSize = maxSize;
                    }
                    int size = 0;
                    TextureImporterFormat Aformat;
                    TextureImporterFormat Iformat;
                    TextureImporterFormat Wformat;
                    textureImporter.GetPlatformTextureSettings("Android", out size, out Aformat);
                    textureImporter.GetPlatformTextureSettings("iPhone", out size, out Iformat);
                    textureImporter.GetPlatformTextureSettings("Standalone", out size, out Wformat);
                    for (int i = AudioSetEndidx; i < cfg.mParameters.Count; i++)
                    {
                        if (cfg.mParameters[i] != "0" && cfg.mParameters[i] != "")
                        {
                            switch (i)
                            {
                                case 28:
                                    {
                                        Aformat = TextureImporterFormat.ETC2_RGB4;
                                        break;
                                    }
                                case 29:
                                    {
                                        Aformat = TextureImporterFormat.ETC2_RGBA8;
                                        break;
                                    }
                                case 30:
                                    {
                                        Aformat = TextureImporterFormat.ETC_RGB4;
                                        break;
                                    }
                                case 31:
                                    {
                                        Aformat = TextureImporterFormat.RGB16;
                                        break;
                                    }
                                case 32:
                                    {
                                        Aformat = TextureImporterFormat.RGBA16;
                                        break;
                                    }
                                case 33:
                                    {
                                        Aformat = TextureImporterFormat.RGB24;
                                        break;
                                    }
                                case 34:
                                    {
                                        Aformat = TextureImporterFormat.RGBA32;
                                        break;
                                    }
                                case 35:
                                    {
                                        Aformat = TextureImporterFormat.ASTC_RGB_6x6;
                                        break;
                                    }
                                case 36:
                                    {
                                        Iformat = TextureImporterFormat.PVRTC_RGB2;
                                        break;
                                    }
                                case 37:
                                    {
                                        Iformat = TextureImporterFormat.PVRTC_RGBA2;
                                        break;
                                    }
                                case 38:
                                    {
                                        Iformat = TextureImporterFormat.PVRTC_RGB4;
                                        break;
                                    }
                                case 39:
                                    {
                                        Iformat = TextureImporterFormat.PVRTC_RGBA4;
                                        break;
                                    }
                                case 40:
                                    {
                                        Iformat = TextureImporterFormat.RGB16;
                                        break;
                                    }
                                case 41:
                                    {
                                        Iformat = TextureImporterFormat.RGBA16;
                                        break;
                                    }
                                case 42:
                                    {
                                        Iformat = TextureImporterFormat.RGB24;
                                        break;
                                    }
                                case 43:
                                    {
                                        Iformat = TextureImporterFormat.RGBA32;
                                        break;
                                    }
                                case 44:
                                    {
                                        Iformat = TextureImporterFormat.ASTC_RGB_6x6;
                                        break;
                                    }
                                case 45:
                                    {
                                        Wformat = TextureImporterFormat.DXT1;
                                        break;
                                    }
                                case 46:
                                    {
                                        Wformat = TextureImporterFormat.DXT5;
                                        break;
                                    }
                                case 47:
                                    {
                                        Wformat = TextureImporterFormat.ARGB16;
                                        break;
                                    }
                                case 48:
                                    {
                                        Wformat = TextureImporterFormat.RGB24;
                                        break;
                                    }
                                case 49:
                                    {
                                        Wformat = TextureImporterFormat.RGBA32;
                                        break;
                                    }
                            }
                        }
                    }
                    //
                    kName = "Android";
                    kSettings.format = Aformat;
                    kSettings.name = kName;
                    textureImporter.SetPlatformTextureSettings(kSettings);
                    //
                    kName = "iPhone";
                    kSettings.format = Iformat;
                    kSettings.name = kName;
                    textureImporter.SetPlatformTextureSettings(kSettings);
                    //
                    kSettings.format = Wformat;
                    kName = "Standalone";
                    kSettings.name = kName;
                    textureImporter.SetPlatformTextureSettings(kSettings);
                    //
                }
            }
        }
    }
    private void OnPreprocessModel()
    {
        if (mConfigurations.Count != 0)
        {
            foreach (var _cfg in mConfigurations)
            {
                int sIdx = _cfg.mPath.IndexOf('A');
                if (assetPath.Contains(_cfg.mPath.Substring(sIdx)))
                {
                    CSVData cfg = ContainsFileName(_cfg);
                    ModelImporter modelImporter = assetImporter as ModelImporter;
                    modelImporter.importMaterials = false;
                    for (int i = TextureSetEndIdx; i < ModelSetEndidx; i++)
                    {
                        if (cfg.mParameters[i] != "0" && cfg.mParameters[i] != "")
                        {
                            switch (i)
                            {
                                case 15:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.isReadable = true;
                                        }
                                        else
                                        {
                                            modelImporter.isReadable = false;
                                        }
                                        break;
                                    }
                                case 16:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.animationType = ModelImporterAnimationType.Generic;
                                            modelImporter.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                                        }
                                        else
                                        {
                                            modelImporter.animationType = ModelImporterAnimationType.None;
                                        }
                                        break;
                                    }
                                case 17:
                                    {
                                        switch (cfg.mParameters[i])
                                        {
                                            case "1":
                                            case "2":
                                            case "3":
                                            case "4":
                                                modelImporter.meshCompression = ModelImporterMeshCompression.Off;
                                                break;
                                        }
                                        break;
                                    }
                                case 18:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.optimizeMesh = false;
                                        }
                                        else
                                        {
                                            modelImporter.optimizeMesh = true;
                                        }
                                        break;
                                    }
                                case 19:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.importTangents = ModelImporterTangents.None;
                                        }
                                        else if (cfg.mParameters[i] == "2")
                                        {
                                            modelImporter.importTangents = ModelImporterTangents.CalculateLegacy;
                                        }
                                        else
                                        {
                                            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                                        }
                                        break;
                                    }
                                case 20:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.optimizeGameObjects = true;
                                        }
                                        else
                                        {
                                            modelImporter.optimizeGameObjects = false;
                                        }
                                        break;
                                    }
                                case 21:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.importBlendShapes = true;
                                        }
                                        else
                                        {
                                            modelImporter.importBlendShapes = false;
                                        }
                                        break;
                                    }
                                case 22:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.importVisibility = true;
                                        }
                                        else
                                        {
                                            modelImporter.importBlendShapes = false;
                                        }
                                        break;
                                    }
                                case 23:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.importCameras = true;
                                        }
                                        else
                                        {
                                            modelImporter.importCameras = false;
                                        }
                                        break;
                                    }
                                case 24:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.importLights = true;
                                        }
                                        else
                                        {
                                            modelImporter.importLights = false;
                                        }
                                        break;
                                    }
                                case 25:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.weldVertices = true;
                                        }
                                        else
                                        {
                                            modelImporter.weldVertices = false;
                                        }
                                        break;
                                    }
                                case 26:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            modelImporter.importNormals = ModelImporterNormals.Import;
                                        }
                                        else
                                        {
                                            modelImporter.importNormals = ModelImporterNormals.None;
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }
    }
    private void OnPreprocessAudio()
    {
        if (mConfigurations.Count != 0)
        {
            foreach (var cfg in mConfigurations)
            {
                int sIdx = cfg.mPath.IndexOf('A');
                if (assetPath.Contains(cfg.mPath.Substring(sIdx)))
                {

                    AudioImporter audioImporter = assetImporter as AudioImporter;
                    AudioImporterSampleSettings kSetting = audioImporter.defaultSampleSettings;
                    kSetting.loadType = AudioClipLoadType.CompressedInMemory;
                    kSetting.compressionFormat = AudioCompressionFormat.Vorbis;
                    audioImporter.SetOverrideSampleSettings("Standalone", kSetting);
                    audioImporter.SetOverrideSampleSettings("iOS", kSetting);
                    audioImporter.SetOverrideSampleSettings("Android", kSetting);
                    for (int i = ModelSetEndidx; i < AudioSetEndidx; i++)
                    {
                        if (cfg.mParameters[i] != "0" && cfg.mParameters[i] != "")
                        {
                            switch (i)
                            {
                                case 27:
                                    {
                                        if (cfg.mParameters[i] == "1")
                                        {
                                            audioImporter.forceToMono = true;
                                        }
                                        else
                                        {
                                            audioImporter.forceToMono = false;
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    break;
                }
            }
        }
    }
    private static bool CheckTextureAlpha(TextureImporter texImporter)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texImporter.assetPath);
        if (tex == null)
        {
            Debug.LogError(string.Format("获取贴图{0}失败", texImporter.assetPath));
            return true;
        }
        RenderTexture tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(tex, tmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;
        Texture2D mTexture2D = new Texture2D(tex.width, tex.height);
        mTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        mTexture2D.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);
        List<Color> colorTemp = new List<Color>(mTexture2D.GetPixels());
        var findIndex = colorTemp.FindIndex(x => x.a < 0.98f);
        bool hasAlpha = texImporter.DoesSourceTextureHaveAlpha() && findIndex >= 0;

        return hasAlpha;
    }
    private CSVData ContainsFileName(CSVData Parent)
    {
        if (Parent.mChild != null)
        {
            int idx = Parent.mPath.Length + 1;
            foreach (var Child in Parent.mChild)
            {
                string Name = Child.mPath.Substring(idx);
                string FileName = assetPath.Substring(idx);
                string[] sKeyword = Name.Split('_');
                bool bflag = true;
                for (int i = 1; i < sKeyword.Length; i++)
                {
                    if (!FileName.Contains(sKeyword[i]))
                    {
                        bflag = false;
                        break;
                    }
                }
                if (bflag)
                {
                    return Child;
                }
            }
        }
        return Parent;
    }

}
#endif
