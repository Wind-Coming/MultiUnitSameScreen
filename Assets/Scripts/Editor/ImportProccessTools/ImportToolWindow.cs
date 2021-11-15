using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
#if UNITY_EDITOR
public class ImportToolWindow : EditorWindow
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
    private static int ConfigLength = 52;
    private static string[] FileExtension = { "*.png", "*.tga", "*.FBX", "*.exr" };

    private enum FolderType
    {
        None,
        Texture,
        Model,
        Audio,
    }
    private enum Tangents
    {
        None,
        Calculatelegacy,
        CalculateMikktspace,
        Ignore,
    }
    private enum TextureType
    {
        TextureDefault,
        Sprite,
        LightMap,
        Ignore,
    }
    private enum MaxSize
    {
        MaxSize16,
        MaxSize32,
        MaxSize64,
        MaxSize128,
        MaxSize256,
        MaxSize512,
        MaxSize1024,
        MaxSize2048,
        MaxSize4096,
        Igonre,
    }
    private enum MeshCompress
    {
        Off,
        Low,
        Medium,
        High,
        Ignore,
    }
    private enum AndroidFormat
    {
        ETC2_RGB4,
        ETC2_RGBA8,
        ETC_RGB4,
        RGB16,
        RGBA16,
        RGB24,
        RGBA32,
        ASTC_RGB_6X6,
        Ignore,
    }
    private enum iPhoneFormat
    {
        PVRTC_RGB2,
        PVRTC_RGBA2,
        PVRTC_RGB4,
        PVRTC_RGBA4,
        RGB16,
        RGBA16,
        RGB24,
        RGBA32,
        ASTC_RGB_6X6,
        Ignore,
    }
    private enum StandaloneFormat
    {
        DXT1,
        DXT5,
        ARGB16,
        RGB24,
        RGBA32,
        ARGB32,
        Ignore,
    }
    private enum TextureNPOTScale
    {
        None = 0,
        ToNearest = 1,
        ToLarger = 2,
        ToSmaller = 3,
        Ignore = 4,
    }
    private enum FilterImportMode
    {
        Point = 0,
        Bilinear = 1,
        Trilinear = 2,
        Ignore = 3,
    }
    private enum WrapImportMode
    {
        Repeat = 0,
        Clamp = 1,
        Mirror = 2,
        MirrorOnce = 3,
        Ignore = 4,
    }

    private class ImportToolStruct
    {
        public CSVData Configuration;
        public bool mShowFold;
        public FolderType mFoldertype;
        public TextureType mTextureType;
        public MaxSize mMaxsize;
        public WrapImportMode mWrapMode;
        public FilterImportMode mFilterMode;
        public TextureNPOTScale mNPOTScale;
        public bool mMipMap;
        public bool mTextureRW;
        public bool mModelRW;
        public bool mHaveanim;
        public MeshCompress mMeshCompress;
        public bool mOptimizeMesh;
        public Tangents mTangents;
        public bool mOptimizeGO;
        public bool mImportBlendshap;
        public bool mImportVisibilty;
        public bool mImportCamera;
        public bool mImportLight;
        public bool mNormals;
        public bool mWeldVertices;
        public bool mForceToMono;
        public AndroidFormat mATextureFormat;
        public iPhoneFormat mITextureFormat;
        public StandaloneFormat mWTextureFormat;
        public List<ImportToolStruct> mChild;
        public bool[] mIgnore;
        public int mPriority;

        public ImportToolStruct(CSVData pConfiguration, TextureType pmTextureType, MaxSize pmMaxsize, WrapImportMode pmWrapMode,
         FilterImportMode pmFilterMode, TextureNPOTScale pmNPOTScale, bool pmMipMap, bool pmTextureRW, bool pmModelRW, bool pmHaveanim,
         MeshCompress pmMeshCompress, bool pmOptimizeMesh, Tangents pmTangents, bool pmOptimizeGO, bool pmImportBlendshap, bool pmImportVisibilty,
         bool pmImportCamera, bool pmImportLight, bool pmNormals, bool pmWeldVertices, bool pmForceToMono, AndroidFormat pmATextureFormat,
         iPhoneFormat pmITextureFormat, StandaloneFormat pmWTextureFormat, bool[] pmIgnore, int pmPriority)
        {
            this.Configuration.mParameters = pConfiguration.mParameters;
            this.mTextureType = pmTextureType;
            this.mMaxsize = pmMaxsize;
            this.mWrapMode = pmWrapMode;
            this.mFilterMode = pmFilterMode;
            this.mNPOTScale = pmNPOTScale;
            this.mMipMap = pmMipMap;
            this.mTextureRW = pmTextureRW;
            this.mModelRW = pmModelRW;
            this.mHaveanim = pmHaveanim;
            this.mMeshCompress = pmMeshCompress;
            this.mOptimizeMesh = pmOptimizeMesh;
            this.mTangents = pmTangents;
            this.mOptimizeGO = pmOptimizeGO;
            this.mImportBlendshap = pmImportBlendshap;
            this.mImportVisibilty = pmImportVisibilty;
            this.mImportCamera = pmImportCamera;
            this.mImportLight = pmImportLight;
            this.mNormals = pmNormals;
            this.mWeldVertices = pmWeldVertices;
            this.mForceToMono = pmForceToMono;
            this.mATextureFormat = pmATextureFormat;
            this.mITextureFormat = pmITextureFormat;
            this.mWTextureFormat = pmWTextureFormat;
            this.mIgnore = pmIgnore;
            this.mPriority = pmPriority;
        }
        public ImportToolStruct()
        {

        }

        public ImportToolStruct Clone(ImportToolStruct kTar)
        {
            ImportToolStruct kClone = new ImportToolStruct(kTar.Configuration, kTar.mTextureType, kTar.mMaxsize, kTar.mWrapMode, kTar.mFilterMode,
                kTar.mNPOTScale, kTar.mMipMap, kTar.mTextureRW, kTar.mModelRW, kTar.mHaveanim, kTar.mMeshCompress, kTar.mOptimizeMesh, kTar.mTangents
                , kTar.mOptimizeGO, kTar.mImportBlendshap, kTar.mImportVisibilty, kTar.mImportCamera, kTar.mImportLight, kTar.mNormals, kTar.mWeldVertices, kTar.mForceToMono
                , kTar.mATextureFormat, kTar.mITextureFormat, kTar.mWTextureFormat, kTar.mIgnore, kTar.mPriority);
            return kClone;
        }
    }
    private struct CSVData
    {
        public string mPath;
        public List<string> mParameters;

    }
    private struct LogMSG
    {
        public string mPath;
        public string mName;
        public List<string> mChangeMSG;
        public bool mChange;
    }
    private const string LogPath = "/Scripts/Editor/ImportProccessTools/ChangeLog.txt";
    private const string CSVPath = "/Scripts/Editor/ImportProccessTools/按文件夹配置.csv";
    private const string ImportToolPath = "/Scripts/Editor/ImportProccessTools";
    private static List<string> CSVColName = new List<string>();

    private static List<ImportToolStruct> mConfigs = new List<ImportToolStruct>();
    private static Vector2 scrollPos = Vector2.zero;
    private static bool mRead = true;
    private static bool mChildRead = true;
    private static int iOriidx;
    [MenuItem("Tools/导入设置工具/ImportToolWindow")]
    private static void OpenWindow()
    {
        mRead = true;
        CSVColName.Clear();
        mConfigs.Clear();
        string prjpath = Application.dataPath + CSVPath;
        string[] FileDatas;
        FileDatas = File.ReadAllLines(prjpath);
        for (int i = 0; i < FileDatas.Length; i++)
        {
            string[] LineData = FileDatas[i].Split(',');
            if (i == 0)
            {
                for (int j = 0; j < LineData.Length; j++)
                {
                    CSVColName.Add(LineData[j]);
                }
            }
            else
            {
                ImportToolStruct cfg = new ImportToolStruct();
                cfg.mIgnore = new bool[14];
                for (int j = 0; j < cfg.mIgnore.Length; j++)
                {
                    cfg.mIgnore[j] = false;
                }
                CSVData Specification = new CSVData();
                Specification.mParameters = new List<string>();
                Specification.mPath = LineData[0];
                for (int j = 1; j < LineData.Length; j++)
                {
                    Specification.mParameters.Add(LineData[j]);
                }
                cfg.Configuration = Specification;
                cfg.mShowFold = false;
                cfg.mFoldertype = FolderType.None;
                cfg.mPriority = int.Parse(Specification.mParameters[PRIORITY]);
                if (cfg.Configuration.mParameters[PRIORITY] == "1")
                {
                    mConfigs.Add(cfg);
                }
                else
                {
                    foreach (var _cfg in mConfigs)
                    {
                        if (cfg.Configuration.mPath.Contains(_cfg.Configuration.mPath) && !cfg.Configuration.mPath.Substring(_cfg.Configuration.mPath.Length + 1).Contains("/"))
                        {
                            if (_cfg.mChild == null)
                            {
                                _cfg.mChild = new List<ImportToolStruct>();
                            }
                            _cfg.mChild.Add(cfg);
                        }
                    }
                }
            }
        }
        EditorWindow.GetWindow(typeof(ImportToolWindow));
    }
    private static void RefreshWindow()
    {
        mRead = true;
        CSVColName.Clear();
        mConfigs.Clear();
        string prjpath = Application.dataPath + CSVPath;
        string[] FileDatas;
        FileDatas = File.ReadAllLines(prjpath);
        for (int i = 0; i < FileDatas.Length; i++)
        {
            string[] LineData = FileDatas[i].Split(',');
            if (i == 0)
            {
                for (int j = 0; j < LineData.Length; j++)
                {
                    CSVColName.Add(LineData[j]);
                }
            }
            else
            {
                ImportToolStruct cfg = new ImportToolStruct();
                cfg.mIgnore = new bool[14];
                for (int j = 0; j < cfg.mIgnore.Length; j++)
                {
                    cfg.mIgnore[j] = false;
                }
                CSVData Specification = new CSVData();
                Specification.mParameters = new List<string>();
                Specification.mPath = LineData[0];
                for (int j = 1; j < LineData.Length; j++)
                {
                    Specification.mParameters.Add(LineData[j]);
                }
                cfg.Configuration = Specification;
                cfg.mShowFold = false;
                cfg.mFoldertype = FolderType.None;
                cfg.mPriority = int.Parse(Specification.mParameters[PRIORITY]);
                if (cfg.Configuration.mParameters[PRIORITY] == "1")
                {
                    mConfigs.Add(cfg);
                }
                else
                {
                    foreach (var _cfg in mConfigs)
                    {
                        if (cfg.Configuration.mPath.Contains(_cfg.Configuration.mPath))
                        {
                            if (_cfg.mChild == null)
                            {
                                _cfg.mChild = new List<ImportToolStruct>();
                            }
                            _cfg.mChild.Add(cfg);
                        }
                    }
                }
            }
        }
        Debug.Log("配置表读取完毕");
    }
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("创建新约束路径"))
        {
            NewFolderConstraint();
        }
        if (GUILayout.Button("保存改动"))
        {
            SaveToCSV();
        }
        if (GUILayout.Button("强制更改约束路径下不符合约束的资源"))
        {
            ChangeResource();
        }
        if (GUILayout.Button("创建约束路径中不存在的文件夹"))
        {
            CreatFolder();
        }
        if (GUILayout.Button("打开配置文件目录"))
        {
            OpenConfigsFolder();
        }
        if(GUILayout.Button("加载配置表"))
        {
            ImportProccess.ReadCSV();
        }
        GUILayout.EndHorizontal();
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
        for (int i = 0; i < mConfigs.Count; i++)
        {
            GUILayout.BeginHorizontal();
            mConfigs[i].mShowFold = EditorGUILayout.Foldout(mConfigs[i].mShowFold, mConfigs[i].Configuration.mPath);
            if (GUILayout.Button("创建文件夹下文件名特殊设置"))
            {
                NewChildFileConstraint(mConfigs[i]);
            }
            if (GUILayout.Button("复制约束路径设置"))
            {
                iOriidx = i;
                EditorWindow.GetWindow(typeof(CopyWindow));
            }
            if (GUILayout.Button("检查并更改该目录下美术资源"))
            {
                ChangeSignleFolderResource(mConfigs[i]);
            }
            if (GUILayout.Button("删除约束路径"))
            {
                DeleteFolderConstraint(i);
                break;
            }
            GUILayout.EndHorizontal();
            if (mRead)
            {
                #region ReadTextureType
                if (mConfigs[i].Configuration.mParameters[TEXTURETYPE] == "1")
                {
                    mConfigs[i].mTextureType = TextureType.TextureDefault;
                }
                else if (mConfigs[i].Configuration.mParameters[TEXTURETYPE] == "2")
                {
                    mConfigs[i].mTextureType = TextureType.Sprite;
                }
                else if (mConfigs[i].Configuration.mParameters[TEXTURETYPE] == "3")
                {
                    mConfigs[i].mTextureType = TextureType.LightMap;
                }
                else
                {
                    mConfigs[i].mTextureType = TextureType.Ignore;
                }
                #endregion
                #region ReadMaxSize
                if (mConfigs[i].Configuration.mParameters[MAXSIZE4096] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize4096;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE2048] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize2048;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE1024] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize1024;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE512] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize512;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE256] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize256;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE128] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize128;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE64] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize64;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE32] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize32;
                }
                else if (mConfigs[i].Configuration.mParameters[MAXSIZE16] == "1")
                {
                    mConfigs[i].mMaxsize = MaxSize.MaxSize16;
                }
                else
                {
                    mConfigs[i].mMaxsize = MaxSize.Igonre;
                }
                #endregion
                #region ReadToNearest
                if (mConfigs[i].Configuration.mParameters[NPOTSCALE] == "1")
                {
                    mConfigs[i].mNPOTScale = TextureNPOTScale.ToNearest;
                }
                else if (mConfigs[i].Configuration.mParameters[NPOTSCALE] == "2")
                {
                    mConfigs[i].mNPOTScale = TextureNPOTScale.ToLarger;
                }
                else if (mConfigs[i].Configuration.mParameters[NPOTSCALE] == "3")
                {
                    mConfigs[i].mNPOTScale = TextureNPOTScale.ToLarger;
                }
                else if (mConfigs[i].Configuration.mParameters[NPOTSCALE] == "4")
                {
                    mConfigs[i].mNPOTScale = TextureNPOTScale.None;
                }
                else
                {
                    mConfigs[i].mNPOTScale = TextureNPOTScale.Ignore;
                }
                #endregion
                #region ReadWrapMode
                if (mConfigs[i].Configuration.mParameters[WRAPMODE] == "1")
                {
                    mConfigs[i].mWrapMode = WrapImportMode.Clamp;
                }
                else if (mConfigs[i].Configuration.mParameters[WRAPMODE] == "2")
                {
                    mConfigs[i].mWrapMode = WrapImportMode.Mirror;
                }
                else if (mConfigs[i].Configuration.mParameters[WRAPMODE] == "3")
                {
                    mConfigs[i].mWrapMode = WrapImportMode.MirrorOnce;
                }
                else if (mConfigs[i].Configuration.mParameters[WRAPMODE] == "4")
                {
                    mConfigs[i].mWrapMode = WrapImportMode.Repeat;
                }
                else
                {
                    mConfigs[i].mWrapMode = WrapImportMode.Ignore;
                }
                #endregion
                #region ReadFilteMode
                if (mConfigs[i].Configuration.mParameters[FILTERMODE] == "2")
                {
                    mConfigs[i].mFilterMode = FilterImportMode.Bilinear;
                }
                else if (mConfigs[i].Configuration.mParameters[FILTERMODE] == "3")
                {
                    mConfigs[i].mFilterMode = FilterImportMode.Trilinear;
                }
                else if (mConfigs[i].Configuration.mParameters[FILTERMODE] == "1")
                {
                    mConfigs[i].mFilterMode = FilterImportMode.Point;
                }
                else
                {
                    mConfigs[i].mFilterMode = FilterImportMode.Ignore;
                }
                #endregion
                #region ReadMipmap
                if (mConfigs[i].Configuration.mParameters[MIPMAP] == "1")
                {
                    mConfigs[i].mMipMap = true;
                }
                else if (mConfigs[i].Configuration.mParameters[MIPMAP] == "2")
                {
                    mConfigs[i].mMipMap = false;
                }
                else
                {
                    mConfigs[i].mIgnore[0] = true;
                }
                #endregion
                #region ReadTextureRW
                if (mConfigs[i].Configuration.mParameters[TEXTURERW] == "1")
                {
                    mConfigs[i].mTextureRW = true;
                }
                else if (mConfigs[i].Configuration.mParameters[TEXTURERW] == "2")
                {
                    mConfigs[i].mTextureRW = false;
                }
                else
                {
                    mConfigs[i].mIgnore[1] = true;
                }
                #endregion
                #region ReadModelRW
                if (mConfigs[i].Configuration.mParameters[MODELRW] == "1")
                {
                    mConfigs[i].mModelRW = true;
                }
                else if (mConfigs[i].Configuration.mParameters[MODELRW] == "2")
                {
                    mConfigs[i].mModelRW = false;
                }
                else
                {
                    mConfigs[i].mIgnore[2] = true;
                }
                #endregion
                #region ReadModelHaveAnim
                if (mConfigs[i].Configuration.mParameters[MODELHAVEANIM] == "1")
                {
                    mConfigs[i].mHaveanim = true;
                }
                else if (mConfigs[i].Configuration.mParameters[MODELHAVEANIM] == "2")
                {
                    mConfigs[i].mHaveanim = false;
                }
                else
                {
                    mConfigs[i].mIgnore[3] = true;
                }
                #endregion
                #region Read Mesh Compress
                if (mConfigs[i].Configuration.mParameters[MESHCOMPRESS] == "4")
                {
                    mConfigs[i].mMeshCompress = MeshCompress.Off;
                }
                else if (mConfigs[i].Configuration.mParameters[MESHCOMPRESS] == "1")
                {
                    mConfigs[i].mMeshCompress = MeshCompress.Low;
                }
                else if (mConfigs[i].Configuration.mParameters[MESHCOMPRESS] == "2")
                {
                    mConfigs[i].mMeshCompress = MeshCompress.Medium;
                }
                else if (mConfigs[i].Configuration.mParameters[MESHCOMPRESS] == "3")
                {
                    mConfigs[i].mMeshCompress = MeshCompress.High;
                }
                else
                {
                    mConfigs[i].mMeshCompress = MeshCompress.Ignore;
                }
                #endregion
                #region Read OptimizeMesh
                if (mConfigs[i].Configuration.mParameters[OPTIMIZEMESH] == "1")
                {
                    mConfigs[i].mOptimizeMesh = false;
                }
                else if (mConfigs[i].Configuration.mParameters[OPTIMIZEMESH] == "2")
                {
                    mConfigs[i].mOptimizeMesh = true;
                }
                else
                {
                    mConfigs[i].mIgnore[4] = true;
                }
                #endregion
                #region Read Tangents
                if (mConfigs[i].Configuration.mParameters[TANGENTS] == "1")
                {
                    mConfigs[i].mTangents = Tangents.None;
                }
                else if (mConfigs[i].Configuration.mParameters[TANGENTS] == "2")
                {
                    mConfigs[i].mTangents = Tangents.Calculatelegacy;
                }
                else if (mConfigs[i].Configuration.mParameters[TANGENTS] == "3")
                {
                    mConfigs[i].mTangents = Tangents.CalculateMikktspace;
                }
                else
                {
                    mConfigs[i].mTangents = Tangents.Ignore;
                }
                #endregion
                #region ReadOptimizeGameobject
                if (mConfigs[i].Configuration.mParameters[OPTIMIZEGAMEOBJECT] == "1")
                {
                    mConfigs[i].mOptimizeGO = true;
                }
                else if (mConfigs[i].Configuration.mParameters[OPTIMIZEGAMEOBJECT] == "2")
                {
                    mConfigs[i].mOptimizeGO = false;
                }
                else
                {
                    mConfigs[i].mIgnore[6] = true;
                }
                #endregion
                #region ReadImportBlendShap
                if (mConfigs[i].Configuration.mParameters[IMPORTBLENDSHAPES] == "1")
                {
                    mConfigs[i].mImportBlendshap = true;
                }
                else if (mConfigs[i].Configuration.mParameters[IMPORTBLENDSHAPES] == "2")
                {
                    mConfigs[i].mImportBlendshap = false;
                }
                else
                {
                    mConfigs[i].mIgnore[7] = true;
                }
                #endregion
                #region ReadImportVisi
                if (mConfigs[i].Configuration.mParameters[IMPORTVISIBILITY] == "1")
                {
                    mConfigs[i].mImportVisibilty = true;
                }
                else if (mConfigs[i].Configuration.mParameters[IMPORTVISIBILITY] == "2")
                {
                    mConfigs[i].mImportVisibilty = false;
                }
                else
                {
                    mConfigs[i].mIgnore[8] = true;
                }
                #endregion
                #region ReadImportCamera
                if (mConfigs[i].Configuration.mParameters[IMPORTCAMERAS] == "1")
                {
                    mConfigs[i].mImportCamera = true;
                }
                else if (mConfigs[i].Configuration.mParameters[IMPORTCAMERAS] == "2")
                {
                    mConfigs[i].mImportCamera = false;
                }
                else
                {
                    mConfigs[i].mIgnore[9] = true;
                }
                #endregion
                #region ReadImportLight
                if (mConfigs[i].Configuration.mParameters[IMPORTLIGHTS] == "1")
                {
                    mConfigs[i].mImportLight = true;
                }
                else if (mConfigs[i].Configuration.mParameters[IMPORTLIGHTS] == "2")
                {
                    mConfigs[i].mImportLight = false;
                }
                else
                {
                    mConfigs[i].mIgnore[10] = true;
                }
                #endregion
                #region ReadNormals
                if (mConfigs[i].Configuration.mParameters[NORMALS] == "1")
                {
                    mConfigs[i].mNormals = true;
                }
                else if (mConfigs[i].Configuration.mParameters[NORMALS] == "2")
                {
                    mConfigs[i].mNormals = false;
                }
                else
                {
                    mConfigs[i].mIgnore[11] = true;
                }
                #endregion
                #region ReadAudioForcetoMono
                if (mConfigs[i].Configuration.mParameters[AUDIOFORCETOMONO] == "1")
                {
                    mConfigs[i].mForceToMono = true;
                }
                else if (mConfigs[i].Configuration.mParameters[AUDIOFORCETOMONO] == "2")
                {
                    mConfigs[i].mForceToMono = false;
                }
                else
                {
                    mConfigs[i].mIgnore[13] = true;
                }
                #endregion
                #region ReadAndroidFormat
                if (mConfigs[i].Configuration.mParameters[AETC2RGB4] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.ETC2_RGB4;
                }
                else if (mConfigs[i].Configuration.mParameters[AETC2RGBA8] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.ETC2_RGBA8;
                }
                else if (mConfigs[i].Configuration.mParameters[AETCRGB4] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.ETC_RGB4;
                }
                else if (mConfigs[i].Configuration.mParameters[ARGB16] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.RGB16;
                }
                else if (mConfigs[i].Configuration.mParameters[ARGBA16] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.RGBA16;
                }
                else if (mConfigs[i].Configuration.mParameters[ARGB24] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.RGB24;
                }
                else if (mConfigs[i].Configuration.mParameters[ARGBA32] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.RGBA32;
                }
                else if (mConfigs[i].Configuration.mParameters[AASTCRGB6X6] == "1")
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.ASTC_RGB_6X6;
                }
                else
                {
                    mConfigs[i].mATextureFormat = AndroidFormat.Ignore;
                }
                #endregion
                #region ReadIphoneFormat
                if (mConfigs[i].Configuration.mParameters[IPVRTCRGB2] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.PVRTC_RGB2;
                }
                else if (mConfigs[i].Configuration.mParameters[IPVRTCRGBA2] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.PVRTC_RGBA2;
                }
                else if (mConfigs[i].Configuration.mParameters[IPVRTCRGB4] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.PVRTC_RGB4;
                }
                else if (mConfigs[i].Configuration.mParameters[IPVRTCRGBA4] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.PVRTC_RGBA4;
                }
                else if (mConfigs[i].Configuration.mParameters[IRGB16] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.RGB16;
                }
                else if (mConfigs[i].Configuration.mParameters[IRGB24] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.RGB24;
                }
                else if (mConfigs[i].Configuration.mParameters[IRGBA32] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.RGBA32;
                }
                else if (mConfigs[i].Configuration.mParameters[IRGBA16] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.RGBA16;
                }
                else if (mConfigs[i].Configuration.mParameters[IASTCRGB6X6] == "1")
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.ASTC_RGB_6X6;
                }
                else
                {
                    mConfigs[i].mITextureFormat = iPhoneFormat.Ignore;
                }
                #endregion
                #region ReadStandloneFormt
                if (mConfigs[i].Configuration.mParameters[WDXT1] == "1")
                {
                    mConfigs[i].mWTextureFormat = StandaloneFormat.DXT1;
                }
                else if (mConfigs[i].Configuration.mParameters[WDXT5] == "1")
                {
                    mConfigs[i].mWTextureFormat = StandaloneFormat.DXT5;
                }
                else if (mConfigs[i].Configuration.mParameters[WARGB16] == "1")
                {
                    mConfigs[i].mWTextureFormat = StandaloneFormat.ARGB16;
                }
                else if (mConfigs[i].Configuration.mParameters[WRGB24] == "1")
                {
                    mConfigs[i].mWTextureFormat = StandaloneFormat.RGB24;
                }
                else if (mConfigs[i].Configuration.mParameters[WRGBA32] == "1")
                {
                    mConfigs[i].mWTextureFormat = StandaloneFormat.RGBA32;
                }
                else if (mConfigs[i].Configuration.mParameters[WARGB32] == "1")
                {
                    mConfigs[i].mWTextureFormat = StandaloneFormat.ARGB32;
                }
                else
                {
                    mConfigs[i].mWTextureFormat = StandaloneFormat.Ignore;
                }
                #endregion
                if (mConfigs[i].Configuration.mParameters[WELDVERTICES] == "1")
                {
                    mConfigs[i].mWeldVertices = true;
                }
                else if (mConfigs[i].Configuration.mParameters[NORMALS] == "2")
                {
                    mConfigs[i].mWeldVertices = false;
                }
                else
                {
                    mConfigs[i].mIgnore[12] = true;
                }
            }
            if (mConfigs[i].mChild != null)
            {
                for (int j = 0; j < mConfigs[i].mChild.Count; j++)
                {
                    if (mRead)
                    {
                        #region ReadTextureType
                        if (mConfigs[i].mChild[j].Configuration.mParameters[TEXTURETYPE] == "1")
                        {
                            mConfigs[i].mChild[j].mTextureType = TextureType.TextureDefault;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[TEXTURETYPE] == "2")
                        {
                            mConfigs[i].mChild[j].mTextureType = TextureType.Sprite;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[TEXTURETYPE] == "3")
                        {
                            mConfigs[i].mChild[j].mTextureType = TextureType.LightMap;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mTextureType = TextureType.Ignore;
                        }
                        #endregion
                        #region ReadMaxSize
                        if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE4096] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize4096;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE2048] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize2048;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE1024] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize1024;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE512] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize512;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE256] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize256;
                        }
                        else if (mConfigs[i].Configuration.mParameters[MAXSIZE128] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize128;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE64] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize64;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE32] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize32;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MAXSIZE16] == "1")
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.MaxSize16;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mMaxsize = MaxSize.Igonre;
                        }
                        #endregion
                        #region ReadToNearest
                        if (mConfigs[i].mChild[j].Configuration.mParameters[NPOTSCALE] == "1")
                        {
                            mConfigs[i].mChild[j].mNPOTScale = TextureNPOTScale.ToNearest;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[NPOTSCALE] == "2")
                        {
                            mConfigs[i].mChild[j].mNPOTScale = TextureNPOTScale.ToLarger;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[NPOTSCALE] == "3")
                        {
                            mConfigs[i].mChild[j].mNPOTScale = TextureNPOTScale.ToLarger;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[NPOTSCALE] == "4")
                        {
                            mConfigs[i].mChild[j].mNPOTScale = TextureNPOTScale.None;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mNPOTScale = TextureNPOTScale.Ignore;
                        }
                        #endregion
                        #region ReadWrapMode
                        if (mConfigs[i].mChild[j].Configuration.mParameters[WRAPMODE] == "1")
                        {
                            mConfigs[i].mChild[j].mWrapMode = WrapImportMode.Clamp;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WRAPMODE] == "2")
                        {
                            mConfigs[i].mChild[j].mWrapMode = WrapImportMode.Mirror;
                        }
                        else if (mConfigs[i].Configuration.mParameters[WRAPMODE] == "3")
                        {
                            mConfigs[i].mChild[j].mWrapMode = WrapImportMode.MirrorOnce;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WRAPMODE] == "4")
                        {
                            mConfigs[i].mChild[j].mWrapMode = WrapImportMode.Repeat;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mWrapMode = WrapImportMode.Ignore;
                        }
                        #endregion
                        #region ReadFilteMode
                        if (mConfigs[i].mChild[j].Configuration.mParameters[FILTERMODE] == "2")
                        {
                            mConfigs[i].mChild[j].mFilterMode = FilterImportMode.Bilinear;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[FILTERMODE] == "3")
                        {
                            mConfigs[i].mChild[j].mFilterMode = FilterImportMode.Trilinear;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[FILTERMODE] == "1")
                        {
                            mConfigs[i].mChild[j].mFilterMode = FilterImportMode.Point;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mFilterMode = FilterImportMode.Ignore;
                        }
                        #endregion
                        #region ReadMipmap
                        if (mConfigs[i].mChild[j].Configuration.mParameters[MIPMAP] == "1")
                        {
                            mConfigs[i].mChild[j].mMipMap = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MIPMAP] == "2")
                        {
                            mConfigs[i].mChild[j].mMipMap = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[0] = true;
                        }
                        #endregion
                        #region ReadTextureRW
                        if (mConfigs[i].mChild[j].Configuration.mParameters[TEXTURERW] == "1")
                        {
                            mConfigs[i].mChild[j].mTextureRW = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[TEXTURERW] == "2")
                        {
                            mConfigs[i].mChild[j].mTextureRW = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[1] = true;
                        }
                        #endregion
                        #region ReadModelRW
                        if (mConfigs[i].mChild[j].Configuration.mParameters[MODELRW] == "1")
                        {
                            mConfigs[i].mChild[j].mModelRW = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MODELRW] == "2")
                        {
                            mConfigs[i].mChild[j].mModelRW = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[2] = true;
                        }
                        #endregion
                        #region ReadModelHaveAnim
                        if (mConfigs[i].mChild[j].Configuration.mParameters[MODELHAVEANIM] == "1")
                        {
                            mConfigs[i].mChild[j].mHaveanim = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MODELHAVEANIM] == "2")
                        {
                            mConfigs[i].mChild[j].mHaveanim = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[3] = true;
                        }
                        #endregion
                        #region Read Mesh Compress
                        if (mConfigs[i].mChild[j].Configuration.mParameters[MESHCOMPRESS] == "4")
                        {
                            mConfigs[i].mChild[j].mMeshCompress = MeshCompress.Off;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MESHCOMPRESS] == "1")
                        {
                            mConfigs[i].mChild[j].mMeshCompress = MeshCompress.Low;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MESHCOMPRESS] == "2")
                        {
                            mConfigs[i].mChild[j].mMeshCompress = MeshCompress.Medium;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[MESHCOMPRESS] == "3")
                        {
                            mConfigs[i].mChild[j].mMeshCompress = MeshCompress.High;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mMeshCompress = MeshCompress.Ignore;
                        }
                        #endregion
                        #region Read OptimizeMesh
                        if (mConfigs[i].mChild[j].Configuration.mParameters[OPTIMIZEMESH] == "1")
                        {
                            mConfigs[i].mChild[j].mOptimizeMesh = false;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[OPTIMIZEMESH] == "2")
                        {
                            mConfigs[i].mChild[j].mOptimizeMesh = true;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[4] = true;
                        }
                        #endregion
                        #region Read Tangents
                        if (mConfigs[i].mChild[j].Configuration.mParameters[TANGENTS] == "1")
                        {
                            mConfigs[i].mChild[j].mTangents = Tangents.None;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[TANGENTS] == "2")
                        {
                            mConfigs[i].mChild[j].mTangents = Tangents.Calculatelegacy;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[TANGENTS] == "3")
                        {
                            mConfigs[i].mChild[j].mTangents = Tangents.CalculateMikktspace;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mTangents = Tangents.Ignore;
                        }
                        #endregion
                        #region ReadOptimizeGameobject
                        if (mConfigs[i].mChild[j].Configuration.mParameters[OPTIMIZEGAMEOBJECT] == "1")
                        {
                            mConfigs[i].mChild[j].mOptimizeGO = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[OPTIMIZEGAMEOBJECT] == "2")
                        {
                            mConfigs[i].mChild[j].mOptimizeGO = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[6] = true;
                        }
                        #endregion
                        #region ReadImportBlendShap
                        if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTBLENDSHAPES] == "1")
                        {
                            mConfigs[i].mChild[j].mImportBlendshap = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTBLENDSHAPES] == "2")
                        {
                            mConfigs[i].mChild[j].mImportBlendshap = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[7] = true;
                        }
                        #endregion
                        #region ReadImportVisi
                        if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTVISIBILITY] == "1")
                        {
                            mConfigs[i].mChild[j].mImportVisibilty = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTVISIBILITY] == "2")
                        {
                            mConfigs[i].mChild[j].mImportVisibilty = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[8] = true;
                        }
                        #endregion
                        #region ReadImportCamera
                        if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTCAMERAS] == "1")
                        {
                            mConfigs[i].mChild[j].mImportCamera = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTCAMERAS] == "2")
                        {
                            mConfigs[i].mChild[j].mImportCamera = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[9] = true;
                        }
                        #endregion
                        #region ReadImportLight
                        if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTLIGHTS] == "1")
                        {
                            mConfigs[i].mChild[j].mImportLight = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IMPORTLIGHTS] == "2")
                        {
                            mConfigs[i].mChild[j].mImportLight = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[10] = true;
                        }
                        #endregion
                        #region ReadNormals
                        if (mConfigs[i].mChild[j].Configuration.mParameters[NORMALS] == "1")
                        {
                            mConfigs[i].mChild[j].mNormals = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[NORMALS] == "2")
                        {
                            mConfigs[i].mChild[j].mNormals = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[11] = true;
                        }
                        #endregion
                        #region ReadAudioForcetoMono
                        if (mConfigs[i].mChild[j].Configuration.mParameters[AUDIOFORCETOMONO] == "1")
                        {
                            mConfigs[i].mChild[j].mForceToMono = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[AUDIOFORCETOMONO] == "2")
                        {
                            mConfigs[i].mChild[j].mForceToMono = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[13] = true;
                        }
                        #endregion
                        #region ReadAndroidFormat
                        if (mConfigs[i].mChild[j].Configuration.mParameters[AETC2RGB4] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.ETC2_RGB4;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[AETC2RGBA8] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.ETC2_RGBA8;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[AETCRGB4] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.ETC_RGB4;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[ARGB16] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.RGB16;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[ARGBA16] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.RGBA16;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[ARGB24] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.RGB24;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[ARGBA32] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.RGBA32;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[AASTCRGB6X6] == "1")
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.ASTC_RGB_6X6;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mATextureFormat = AndroidFormat.Ignore;
                        }
                        #endregion
                        #region ReadIphoneFormat
                        if (mConfigs[i].mChild[j].Configuration.mParameters[IPVRTCRGB2] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.PVRTC_RGB2;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IPVRTCRGBA2] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.PVRTC_RGBA2;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IPVRTCRGB4] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.PVRTC_RGB4;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IPVRTCRGBA4] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.PVRTC_RGBA4;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IRGB16] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.RGB16;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IRGB24] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.RGB24;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IRGBA32] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.RGBA32;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IRGBA16] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.RGBA16;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[IASTCRGB6X6] == "1")
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.ASTC_RGB_6X6;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mITextureFormat = iPhoneFormat.Ignore;
                        }
                        #endregion
                        #region ReadStandloneFormt
                        if (mConfigs[i].mChild[j].Configuration.mParameters[WDXT1] == "1")
                        {
                            mConfigs[i].mChild[j].mWTextureFormat = StandaloneFormat.DXT1;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WDXT5] == "1")
                        {
                            mConfigs[i].mChild[j].mWTextureFormat = StandaloneFormat.DXT5;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WARGB16] == "1")
                        {
                            mConfigs[i].mChild[j].mWTextureFormat = StandaloneFormat.ARGB16;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WRGB24] == "1")
                        {
                            mConfigs[i].mChild[j].mWTextureFormat = StandaloneFormat.RGB24;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WRGBA32] == "1")
                        {
                            mConfigs[i].mChild[j].mWTextureFormat = StandaloneFormat.RGBA32;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WARGB32] == "1")
                        {
                            mConfigs[i].mChild[j].mWTextureFormat = StandaloneFormat.ARGB32;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mWTextureFormat = StandaloneFormat.Ignore;
                        }
                        #endregion
                        if (mConfigs[i].mChild[j].Configuration.mParameters[WELDVERTICES] == "1")
                        {
                            mConfigs[i].mChild[j].mWeldVertices = true;
                        }
                        else if (mConfigs[i].mChild[j].Configuration.mParameters[WELDVERTICES] == "2")
                        {
                            mConfigs[i].mChild[j].mWeldVertices = false;
                        }
                        else
                        {
                            mConfigs[i].mChild[j].mIgnore[12] = true;
                        }
                    }
                }
            }
            if (mConfigs[i].mShowFold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("文件夹路径:");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                mConfigs[i].Configuration.mPath = GUILayout.TextField(mConfigs[i].Configuration.mPath);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                mConfigs[i].mFoldertype = (FolderType)EditorGUILayout.EnumPopup("Folder Type", mConfigs[i].mFoldertype);
                GUILayout.EndHorizontal();
                switch (mConfigs[i].mFoldertype)
                {
                    case FolderType.None:
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUILayout.Label("none");
                            GUILayout.EndHorizontal();
                            break;
                        }
                    case FolderType.Texture:
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mTextureType = (TextureType)EditorGUILayout.EnumPopup("纹理类型", mConfigs[i].mTextureType);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mMaxsize = (MaxSize)EditorGUILayout.EnumPopup("纹理最大尺寸", mConfigs[i].mMaxsize);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mNPOTScale = (TextureNPOTScale)EditorGUILayout.EnumPopup("纹理非二次幂是否缩放", mConfigs[i].mNPOTScale);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[0])
                                mConfigs[i].mMipMap = GUILayout.Toggle(mConfigs[i].mMipMap, "MipMap是否打开（请确认是否有必要 会增加30%左右内存）");
                            mConfigs[i].mIgnore[0] = GUILayout.Toggle(mConfigs[i].mIgnore[0], "Ignore 纹理Mipmap设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[1])
                                mConfigs[i].mTextureRW = GUILayout.Toggle(mConfigs[i].mTextureRW, "纹理读写开关（请确认是否有必要打开 会增加一倍内存）");
                            mConfigs[i].mIgnore[1] = GUILayout.Toggle(mConfigs[i].mIgnore[1], "Ignore 纹理读写设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mWrapMode = (WrapImportMode)EditorGUILayout.EnumPopup("纹理的WrapMode", mConfigs[i].mWrapMode);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mFilterMode = (FilterImportMode)EditorGUILayout.EnumPopup("纹理的FilterMode", mConfigs[i].mFilterMode);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mATextureFormat = (AndroidFormat)EditorGUILayout.EnumPopup("Android压缩格式", mConfigs[i].mATextureFormat);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mITextureFormat = (iPhoneFormat)EditorGUILayout.EnumPopup("IPhone压缩格式", mConfigs[i].mITextureFormat);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mWTextureFormat = (StandaloneFormat)EditorGUILayout.EnumPopup("StandLone压缩格式", mConfigs[i].mWTextureFormat);
                            GUILayout.EndHorizontal();
                            break;
                        }
                    case FolderType.Model:
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mMeshCompress = (MeshCompress)EditorGUILayout.EnumPopup("网格压缩(如果可以请尽量使用 但请确认最终效果是否达标)", mConfigs[i].mMeshCompress);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[2])
                                mConfigs[i].mModelRW = GUILayout.Toggle(mConfigs[i].mModelRW, "模型读写开关（请确认是否有必要打开）");
                            mConfigs[i].mIgnore[2] = GUILayout.Toggle(mConfigs[i].mIgnore[2], "Ignore 模型读写设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[3])
                                mConfigs[i].mHaveanim = GUILayout.Toggle(mConfigs[i].mHaveanim, "模型是否包含动画");
                            mConfigs[i].mIgnore[3] = GUILayout.Toggle(mConfigs[i].mIgnore[3], "Ignore 模型动画设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[4])
                                mConfigs[i].mOptimizeMesh = GUILayout.Toggle(mConfigs[i].mOptimizeMesh, "网格优化关闭（关闭请确认是否有必要）");
                            mConfigs[i].mIgnore[4] = GUILayout.Toggle(mConfigs[i].mIgnore[4], "Ignore 网格优化设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[11])
                                mConfigs[i].mNormals = GUILayout.Toggle(mConfigs[i].mNormals, "法线Normals");
                            mConfigs[i].mIgnore[11] = GUILayout.Toggle(mConfigs[i].mIgnore[11], "Ignore 法线设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            mConfigs[i].mTangents = (Tangents)EditorGUILayout.EnumPopup("切线Tangets:", mConfigs[i].mTangents);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[6])
                                mConfigs[i].mOptimizeGO = GUILayout.Toggle(mConfigs[i].mOptimizeGO, "优化骨骼结点（开启后需在模型RIG面板中自行设置运行时暴露的结点）");
                            mConfigs[i].mIgnore[6] = GUILayout.Toggle(mConfigs[i].mIgnore[6], "Ignore 优化骨骼结点设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[7])
                                mConfigs[i].mImportBlendshap = GUILayout.Toggle(mConfigs[i].mImportBlendshap, "ImportBlendShapes");
                            mConfigs[i].mIgnore[7] = GUILayout.Toggle(mConfigs[i].mIgnore[7], "Ignore ImportBlendShapes设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[8])
                                mConfigs[i].mImportVisibilty = GUILayout.Toggle(mConfigs[i].mImportVisibilty, "ImportVisibility");
                            mConfigs[i].mIgnore[8] = GUILayout.Toggle(mConfigs[i].mIgnore[8], "Ignore ImportVisibility设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[9])
                                mConfigs[i].mImportCamera = GUILayout.Toggle(mConfigs[i].mImportCamera, "ImportCameras");
                            mConfigs[i].mIgnore[9] = GUILayout.Toggle(mConfigs[i].mIgnore[9], "Ignore ImportCameras设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[10])
                                mConfigs[i].mImportLight = GUILayout.Toggle(mConfigs[i].mImportLight, "ImportLight");
                            mConfigs[i].mIgnore[10] = GUILayout.Toggle(mConfigs[i].mIgnore[10], "Ignore ImportLight设置");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[12])
                                mConfigs[i].mWeldVertices = GUILayout.Toggle(mConfigs[i].mWeldVertices, "WeldVertices（打开后相同位置的顶点会被合并）");
                            mConfigs[i].mIgnore[12] = GUILayout.Toggle(mConfigs[i].mIgnore[12], "Ignore 网格相同位置顶点合并设置");
                            GUILayout.EndHorizontal();
                            break;
                        }
                    case FolderType.Audio:
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (!mConfigs[i].mIgnore[13])
                                mConfigs[i].mForceToMono = GUILayout.Toggle(mConfigs[i].mForceToMono, "单声道");
                            mConfigs[i].mIgnore[13] = GUILayout.Toggle(mConfigs[i].mIgnore[13], "Ignore 音频声道设置");
                            GUILayout.EndHorizontal();
                            break;
                        }
                }
                if (mConfigs[i].mChild != null)
                {
                    for (int j = 0; j < mConfigs[i].mChild.Count; j++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        mConfigs[i].mChild[j].mShowFold = EditorGUILayout.Foldout(mConfigs[i].mChild[j].mShowFold, mConfigs[i].mChild[j].Configuration.mPath);
                        if (GUILayout.Button("删除特殊文件词缀"))
                        {
                            DeleteFileConstraint(j, mConfigs[i]);
                            break;
                        }
                        GUILayout.EndHorizontal();
                        if (mConfigs[i].mChild[j].mShowFold)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(40);
                            GUILayout.Label("文件夹路径+特殊文件词缀:");
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(40);
                            mConfigs[i].mChild[j].Configuration.mPath = GUILayout.TextField(mConfigs[i].mChild[j].Configuration.mPath);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(40);
                            mConfigs[i].mChild[j].mFoldertype = (FolderType)EditorGUILayout.EnumPopup("Folder Type", mConfigs[i].mChild[j].mFoldertype);
                            GUILayout.EndHorizontal();
                            switch (mConfigs[i].mChild[j].mFoldertype)
                            {
                                case FolderType.None:
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        GUILayout.Label("none");
                                        GUILayout.EndHorizontal();
                                        break;
                                    }
                                case FolderType.Texture:
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mTextureType = (TextureType)EditorGUILayout.EnumPopup("纹理类型", mConfigs[i].mChild[j].mTextureType);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mMaxsize = (MaxSize)EditorGUILayout.EnumPopup("纹理最大尺寸", mConfigs[i].mChild[j].mMaxsize);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mNPOTScale = (TextureNPOTScale)EditorGUILayout.EnumPopup("纹理非二次幂是否缩放", mConfigs[i].mChild[j].mNPOTScale);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[0])
                                            mConfigs[i].mChild[j].mMipMap = GUILayout.Toggle(mConfigs[i].mChild[j].mMipMap, "MipMap是否打开（请确认是否有必要 会增加30%左右内存）");
                                        mConfigs[i].mChild[j].mIgnore[0] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[0], "Ignore 模型MipMap设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[1])
                                            mConfigs[i].mChild[j].mTextureRW = GUILayout.Toggle(mConfigs[i].mChild[j].mTextureRW, "纹理读写开关（请确认是否有必要打开 会增加一倍内存）");
                                        mConfigs[i].mChild[j].mIgnore[1] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[1], "Ignore 纹理读写设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mWrapMode = (WrapImportMode)EditorGUILayout.EnumPopup("纹理的WrapMode", mConfigs[i].mChild[j].mWrapMode);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mFilterMode = (FilterImportMode)EditorGUILayout.EnumPopup("纹理的FilterMode", mConfigs[i].mChild[j].mFilterMode);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mATextureFormat = (AndroidFormat)EditorGUILayout.EnumPopup("Android压缩格式", mConfigs[i].mChild[j].mATextureFormat);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mITextureFormat = (iPhoneFormat)EditorGUILayout.EnumPopup("IPhone压缩格式", mConfigs[i].mChild[j].mITextureFormat);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mWTextureFormat = (StandaloneFormat)EditorGUILayout.EnumPopup("StandLone压缩格式", mConfigs[i].mChild[j].mWTextureFormat);
                                        GUILayout.EndHorizontal();
                                        break;
                                    }
                                case FolderType.Model:
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mMeshCompress = (MeshCompress)EditorGUILayout.EnumPopup("网格压缩(如果可以请尽量使用 但请确认最终效果是否达标)", mConfigs[i].mMeshCompress);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[2])
                                            mConfigs[i].mChild[j].mModelRW = GUILayout.Toggle(mConfigs[i].mChild[j].mModelRW, "模型读写开关（请确认是否有必要打开）");
                                        mConfigs[i].mChild[j].mIgnore[2] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[2], "Ignore 模型读写设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[3])
                                            mConfigs[i].mChild[j].mHaveanim = GUILayout.Toggle(mConfigs[i].mChild[j].mHaveanim, "模型是否包含动画");
                                        mConfigs[i].mChild[j].mIgnore[3] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[3], "Ignore 模型动画设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[4])
                                            mConfigs[i].mChild[j].mOptimizeMesh = GUILayout.Toggle(mConfigs[i].mChild[j].mOptimizeMesh, "网格优化关闭（关闭请确认是否有必要）");
                                        mConfigs[i].mChild[j].mIgnore[4] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[4], "Ignore 网格优化设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[11])
                                            mConfigs[i].mChild[j].mNormals = GUILayout.Toggle(mConfigs[i].mChild[j].mNormals, "法线Normals");
                                        mConfigs[i].mChild[j].mIgnore[11] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[11], "Ignore 法线设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        mConfigs[i].mChild[j].mTangents = (Tangents)EditorGUILayout.EnumPopup("切线Tangets:", mConfigs[i].mChild[j].mTangents);
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[6])
                                            mConfigs[i].mChild[j].mOptimizeGO = GUILayout.Toggle(mConfigs[i].mChild[j].mOptimizeGO, "优化骨骼结点（开启后需在模型RIG面板中自行设置运行时暴露的结点）");
                                        mConfigs[i].mChild[j].mIgnore[6] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[6], "Ignore 优化骨骼结点设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[7])
                                            mConfigs[i].mChild[j].mImportBlendshap = GUILayout.Toggle(mConfigs[i].mChild[j].mImportBlendshap, "ImportBlendShapes");
                                        mConfigs[i].mChild[j].mIgnore[7] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[7], "Ignore ImportBlendShapes设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[8])
                                            mConfigs[i].mChild[j].mImportVisibilty = GUILayout.Toggle(mConfigs[i].mChild[j].mImportVisibilty, "ImportVisibility");
                                        mConfigs[i].mChild[j].mIgnore[8] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[8], "Ignore ImportVisibility设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[9])
                                            mConfigs[i].mChild[j].mImportCamera = GUILayout.Toggle(mConfigs[i].mChild[j].mImportCamera, "ImportCameras");
                                        mConfigs[i].mChild[j].mIgnore[9] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[9], "Ignore ImportCameras设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[10])
                                            mConfigs[i].mChild[j].mImportLight = GUILayout.Toggle(mConfigs[i].mChild[j].mImportLight, "ImportLight");
                                        mConfigs[i].mChild[j].mIgnore[10] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[10], "Ignore ImportLight设置");
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[12])
                                            mConfigs[i].mChild[j].mWeldVertices = GUILayout.Toggle(mConfigs[i].mChild[j].mWeldVertices, "WeldVertices（打开后相同位置的顶点会被合并）");
                                        mConfigs[i].mChild[j].mIgnore[12] = GUILayout.Toggle(mConfigs[i].mChild[j].mIgnore[12], "Ignore 网格相同位置顶点合并设置");
                                        GUILayout.EndHorizontal();
                                        break;
                                    }
                                case FolderType.Audio:
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(40);
                                        if (!mConfigs[i].mChild[j].mIgnore[13])
                                            mConfigs[i].mChild[j].mForceToMono = GUILayout.Toggle(mConfigs[i].mChild[j].mForceToMono, "单声道");
                                        mConfigs[i].mChild[j].mIgnore[13] = GUILayout.Toggle(mConfigs[i].mIgnore[13], "Ignore 音频声道设置");
                                        GUILayout.EndHorizontal();
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }
        GUILayout.EndScrollView();
        mRead = false;
    }
    private static void NewFolderConstraint()
    {
        ImportToolStruct cfg = new ImportToolStruct();
        CSVData temp = new CSVData();
        temp.mPath = "Assets/";
        temp.mParameters = new List<string>();
        for (int i = 0; i < ConfigLength; i++)
        {
            temp.mParameters.Add("");
        }
        temp.mParameters[PRIORITY] = "1";
        cfg.Configuration = temp;
        cfg.mPriority = 1;
        cfg.mTextureType = TextureType.Ignore;
        cfg.mFilterMode = FilterImportMode.Ignore;
        cfg.mWrapMode = WrapImportMode.Ignore;
        cfg.mNPOTScale = TextureNPOTScale.Ignore;
        cfg.mMipMap = false;
        cfg.mTextureRW = false;
        cfg.mMaxsize = MaxSize.Igonre;
        cfg.mModelRW = false;
        cfg.mOptimizeMesh = false;
        cfg.mHaveanim = false;
        cfg.mTangents = Tangents.Ignore;
        cfg.mNormals = false;
        cfg.mImportBlendshap = false;
        cfg.mImportCamera = false;
        cfg.mImportLight = false;
        cfg.mImportVisibilty = false;
        cfg.mWeldVertices = false;
        cfg.mMeshCompress = MeshCompress.Ignore;
        cfg.mATextureFormat = AndroidFormat.Ignore;
        cfg.mITextureFormat = iPhoneFormat.Ignore;
        cfg.mWTextureFormat = StandaloneFormat.Ignore;
        cfg.mIgnore = new bool[14];
        for (int j = 0; j < cfg.mIgnore.Length; j++)
        {
            cfg.mIgnore[j] = true;
        }
        mConfigs.Add(cfg);
    }
    private static void NewChildFileConstraint(ImportToolStruct Parent)
    {
        ImportToolStruct cfg = new ImportToolStruct();
        CSVData temp = new CSVData();
        temp.mPath = Parent.Configuration.mPath + "/";
        temp.mParameters = new List<string>();
        for (int i = 0; i < ConfigLength; i++)
        {
            temp.mParameters.Add("");
        }
        temp.mParameters[PRIORITY] = "2";
        cfg.Configuration = temp;
        cfg.mPriority = 2;
        cfg.mTextureType = TextureType.Ignore;
        cfg.mFilterMode = FilterImportMode.Ignore;
        cfg.mWrapMode = WrapImportMode.Ignore;
        cfg.mNPOTScale = TextureNPOTScale.Ignore;
        cfg.mMipMap = false;
        cfg.mTextureRW = false;
        cfg.mMaxsize = MaxSize.Igonre;
        cfg.mModelRW = false;
        cfg.mOptimizeMesh = false;
        cfg.mHaveanim = false;
        cfg.mTangents = Tangents.Ignore;
        cfg.mNormals = false;
        cfg.mImportBlendshap = false;
        cfg.mImportCamera = false;
        cfg.mImportLight = false;
        cfg.mImportVisibilty = false;
        cfg.mWeldVertices = false;
        cfg.mMeshCompress = MeshCompress.Ignore;
        cfg.mATextureFormat = AndroidFormat.Ignore;
        cfg.mITextureFormat = iPhoneFormat.Ignore;
        cfg.mWTextureFormat = StandaloneFormat.Ignore;
        cfg.mIgnore = new bool[14];
        for (int j = 0; j < cfg.mIgnore.Length; j++)
        {
            cfg.mIgnore[j] = true;
        }
        if (Parent.mChild == null)
        {
            Parent.mChild = new List<ImportToolStruct>();
        }
        Parent.mChild.Add(cfg);
        CopyParent(Parent, cfg);
    }
    private static void DeleteFolderConstraint(int index)
    {
        mConfigs.Remove(mConfigs[index]);
    }
    private static void DeleteFileConstraint(int index, ImportToolStruct Parent)
    {
        Parent.mChild.Remove(Parent.mChild[index]);
    }
    private static void Save(ImportToolStruct mConfig)
    {
        switch (mConfig.mFoldertype)
        {
            case FolderType.Texture:
                {
                    for (int j = 0; j < TextureSetEndIdx; j++)
                    {
                        mConfig.Configuration.mParameters[j] = "";
                    }
                    for (int j = AudioSetEndidx; j < mConfig.Configuration.mParameters.Count; j++)
                    {
                        mConfig.Configuration.mParameters[j] = "";
                    }
                    switch (mConfig.mMaxsize)
                    {
                        case MaxSize.MaxSize4096:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE4096] = "1";
                                break;
                            }
                        case MaxSize.MaxSize2048:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE2048] = "1";
                                break;
                            }
                        case MaxSize.MaxSize1024:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE1024] = "1";
                                break;
                            }
                        case MaxSize.MaxSize512:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE512] = "1";
                                break;
                            }
                        case MaxSize.MaxSize256:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE256] = "1";
                                break;
                            }
                        case MaxSize.MaxSize128:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE128] = "1";
                                break;
                            }
                        case MaxSize.MaxSize64:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE64] = "1";
                                break;
                            }
                        case MaxSize.MaxSize32:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE32] = "1";
                                break;
                            }
                        case MaxSize.MaxSize16:
                            {
                                mConfig.Configuration.mParameters[MAXSIZE16] = "1";
                                break;
                            }
                    }
                    switch (mConfig.mTextureType)
                    {
                        case TextureType.Sprite:
                            {
                                mConfig.Configuration.mParameters[TEXTURETYPE] = "2";
                                break;
                            }
                        case TextureType.TextureDefault:
                            {
                                mConfig.Configuration.mParameters[TEXTURETYPE] = "1";
                                break;
                            }
                        case TextureType.LightMap:
                            {
                                mConfig.Configuration.mParameters[TEXTURETYPE] = "3";
                                break;
                            }

                    }
                    if (mConfig.mTextureRW && !mConfig.mIgnore[1])
                    {
                        mConfig.Configuration.mParameters[TEXTURERW] = "1";
                    }
                    else if (!mConfig.mTextureRW && !mConfig.mIgnore[1])
                    {
                        mConfig.Configuration.mParameters[TEXTURERW] = "2";
                    }
                    switch (mConfig.mNPOTScale)
                    {
                        case TextureNPOTScale.ToLarger:
                            {
                                mConfig.Configuration.mParameters[NPOTSCALE] = "2";
                                break;
                            }
                        case TextureNPOTScale.ToNearest:
                            {
                                mConfig.Configuration.mParameters[NPOTSCALE] = "1";
                                break;
                            }
                        case TextureNPOTScale.ToSmaller:
                            {
                                mConfig.Configuration.mParameters[NPOTSCALE] = "3";
                                break;
                            }
                        case TextureNPOTScale.None:
                            {
                                mConfig.Configuration.mParameters[NPOTSCALE] = "4";
                                break;
                            }
                    }
                    switch (mConfig.mWrapMode)
                    {
                        case WrapImportMode.Clamp:
                            {
                                mConfig.Configuration.mParameters[WRAPMODE] = "1";
                                break;
                            }
                        case WrapImportMode.Repeat:
                            {
                                mConfig.Configuration.mParameters[WRAPMODE] = "4";
                                break;
                            }
                        case WrapImportMode.Mirror:
                            {
                                mConfig.Configuration.mParameters[WRAPMODE] = "2";
                                break;
                            }
                        case WrapImportMode.MirrorOnce:
                            {
                                mConfig.Configuration.mParameters[WRAPMODE] = "3";
                                break;
                            }
                    }
                    switch (mConfig.mFilterMode)
                    {
                        case FilterImportMode.Point:
                            {
                                mConfig.Configuration.mParameters[FILTERMODE] = "1";
                                break;
                            }
                        case FilterImportMode.Bilinear:
                            {
                                mConfig.Configuration.mParameters[FILTERMODE] = "2";
                                break;
                            }
                        case FilterImportMode.Trilinear:
                            {
                                mConfig.Configuration.mParameters[FILTERMODE] = "3";
                                break;
                            }
                        default:
                            {
                                mConfig.Configuration.mParameters[FILTERMODE] = "";
                                break;
                            }
                    }
                    if (mConfig.mMipMap && !mConfig.mIgnore[0])
                    {
                        mConfig.Configuration.mParameters[MIPMAP] = "1";
                    }
                    else if (!mConfig.mMipMap && !mConfig.mIgnore[0])
                    {
                        mConfig.Configuration.mParameters[MIPMAP] = "2";
                    }
                    switch (mConfig.mATextureFormat)
                    {
                        case AndroidFormat.ETC2_RGB4:
                            {
                                mConfig.Configuration.mParameters[AETC2RGB4] = "1";
                                break;
                            }
                        case AndroidFormat.ETC2_RGBA8:
                            {
                                mConfig.Configuration.mParameters[AETC2RGBA8] = "1";
                                break;
                            }
                        case AndroidFormat.ETC_RGB4:
                            {
                                mConfig.Configuration.mParameters[AETCRGB4] = "1";
                                break;
                            }
                        case AndroidFormat.RGB16:
                            {
                                mConfig.Configuration.mParameters[ARGB16] = "1";
                                break;
                            }
                        case AndroidFormat.RGBA16:
                            {
                                mConfig.Configuration.mParameters[ARGBA16] = "1";
                                break;
                            }
                        case AndroidFormat.RGB24:
                            {
                                mConfig.Configuration.mParameters[ARGB24] = "1";
                                break;
                            }
                        case AndroidFormat.RGBA32:
                            {
                                mConfig.Configuration.mParameters[ARGBA32] = "1";
                                break;
                            }
                        case AndroidFormat.ASTC_RGB_6X6:
                            {
                                mConfig.Configuration.mParameters[AASTCRGB6X6] = "1";
                                break;
                            }
                    }
                    switch (mConfig.mITextureFormat)
                    {
                        case iPhoneFormat.PVRTC_RGB2:
                            {
                                mConfig.Configuration.mParameters[IPVRTCRGB2] = "1";
                                break;
                            }
                        case iPhoneFormat.PVRTC_RGBA2:
                            {
                                mConfig.Configuration.mParameters[IPVRTCRGBA2] = "1";
                                break;
                            }
                        case iPhoneFormat.PVRTC_RGB4:
                            {
                                mConfig.Configuration.mParameters[IPVRTCRGB4] = "1";
                                break;
                            }
                        case iPhoneFormat.PVRTC_RGBA4:
                            {
                                mConfig.Configuration.mParameters[IPVRTCRGBA4] = "1";
                                break;
                            }
                        case iPhoneFormat.RGB16:
                            {
                                mConfig.Configuration.mParameters[IRGB16] = "1";
                                break;
                            }
                        case iPhoneFormat.RGBA16:
                            {
                                mConfig.Configuration.mParameters[IRGBA16] = "1";
                                break;
                            }
                        case iPhoneFormat.RGB24:
                            {
                                mConfig.Configuration.mParameters[IRGB24] = "1";
                                break;
                            }
                        case iPhoneFormat.RGBA32:
                            {
                                mConfig.Configuration.mParameters[IRGBA32] = "1";
                                break;
                            }
                        case iPhoneFormat.ASTC_RGB_6X6:
                            {
                                mConfig.Configuration.mParameters[IASTCRGB6X6] = "1";
                                break;
                            }
                    }
                    switch (mConfig.mWTextureFormat)
                    {
                        case StandaloneFormat.DXT1:
                            {
                                mConfig.Configuration.mParameters[WDXT1] = "1";
                                break;
                            }
                        case StandaloneFormat.DXT5:
                            {
                                mConfig.Configuration.mParameters[WDXT5] = "1";
                                break;
                            }
                        case StandaloneFormat.ARGB16:
                            {
                                mConfig.Configuration.mParameters[WARGB16] = "1";
                                break;
                            }
                        case StandaloneFormat.RGB24:
                            {
                                mConfig.Configuration.mParameters[WRGB24] = "1";
                                break;
                            }
                        case StandaloneFormat.RGBA32:
                            {
                                mConfig.Configuration.mParameters[WRGBA32] = "1";
                                break;
                            }
                        case StandaloneFormat.ARGB32:
                            {
                                mConfig.Configuration.mParameters[WARGB32] = "1";
                                break;
                            }
                    }
                    break;
                }
            case FolderType.Model:
                {
                    for (int j = TextureSetEndIdx; j < ModelSetEndidx; j++)
                    {
                        mConfig.Configuration.mParameters[j] = "";
                    }
                    switch (mConfig.mMeshCompress)
                    {
                        case MeshCompress.Off:
                            {
                                mConfig.Configuration.mParameters[MESHCOMPRESS] = "4";
                                break;
                            }
                        case MeshCompress.Low:
                            {
                                mConfig.Configuration.mParameters[MESHCOMPRESS] = "1";
                                break;
                            }
                        case MeshCompress.Medium:
                            {
                                mConfig.Configuration.mParameters[MESHCOMPRESS] = "2";
                                break;
                            }
                        case MeshCompress.High:
                            {
                                mConfig.Configuration.mParameters[MESHCOMPRESS] = "3";
                                break;
                            }
                        case MeshCompress.Ignore:
                            {
                                mConfig.Configuration.mParameters[MESHCOMPRESS] = "";
                                break;
                            }
                    }
                    if (mConfig.mHaveanim && !mConfig.mIgnore[3])
                    {
                        mConfig.Configuration.mParameters[MODELHAVEANIM] = "1";
                    }
                    else if (!mConfig.mHaveanim && !mConfig.mIgnore[3])
                    {
                        mConfig.Configuration.mParameters[MODELHAVEANIM] = "2";
                    }
                    if (mConfig.mOptimizeMesh && !mConfig.mIgnore[4])
                    {
                        mConfig.Configuration.mParameters[OPTIMIZEMESH] = "1";
                    }
                    else if (!mConfig.mOptimizeMesh && !mConfig.mIgnore[4])
                    {
                        mConfig.Configuration.mParameters[OPTIMIZEMESH] = "2";
                    }
                    if (mConfig.mModelRW && !mConfig.mIgnore[2])
                    {
                        mConfig.Configuration.mParameters[MODELRW] = "1";
                    }
                    else if (!mConfig.mModelRW && !mConfig.mIgnore[2])
                    {
                        mConfig.Configuration.mParameters[MODELRW] = "2";
                    }
                    switch (mConfig.mTangents)
                    {
                        case Tangents.None:
                            {
                                mConfig.Configuration.mParameters[TANGENTS] = "1";
                                break;
                            }
                        case Tangents.Calculatelegacy:
                            {
                                mConfig.Configuration.mParameters[TANGENTS] = "2";
                                break;
                            }
                        case Tangents.CalculateMikktspace:
                            {
                                mConfig.Configuration.mParameters[TANGENTS] = "3";
                                break;
                            }
                    }
                    if (mConfig.mOptimizeGO && !mConfig.mIgnore[6])
                    {
                        mConfig.Configuration.mParameters[OPTIMIZEGAMEOBJECT] = "1";
                    }
                    else if (!mConfig.mOptimizeGO && !mConfig.mIgnore[6])
                    {
                        mConfig.Configuration.mParameters[OPTIMIZEGAMEOBJECT] = "2";
                    }
                    if (mConfig.mImportBlendshap && !mConfig.mIgnore[7])
                    {
                        mConfig.Configuration.mParameters[IMPORTBLENDSHAPES] = "1";
                    }
                    else if (!mConfig.mImportBlendshap && !mConfig.mIgnore[7])
                    {
                        mConfig.Configuration.mParameters[IMPORTBLENDSHAPES] = "2";
                    }
                    if (mConfig.mImportVisibilty && !mConfig.mIgnore[8])
                    {
                        mConfig.Configuration.mParameters[IMPORTVISIBILITY] = "1";
                    }
                    else if (mConfig.mImportVisibilty && !mConfig.mIgnore[8])
                    {
                        mConfig.Configuration.mParameters[IMPORTVISIBILITY] = "2";
                    }
                    if (mConfig.mImportCamera && !mConfig.mIgnore[9])
                    {
                        mConfig.Configuration.mParameters[IMPORTCAMERAS] = "1";
                    }
                    else if (!mConfig.mImportCamera && !mConfig.mIgnore[9])
                    {
                        mConfig.Configuration.mParameters[IMPORTCAMERAS] = "2";
                    }
                    if (mConfig.mImportLight && !mConfig.mIgnore[10])
                    {
                        mConfig.Configuration.mParameters[IMPORTLIGHTS] = "1";
                    }
                    else if (!mConfig.mImportLight && !mConfig.mIgnore[10])
                    {
                        mConfig.Configuration.mParameters[IMPORTLIGHTS] = "2";
                    }
                    if (mConfig.mNormals && !mConfig.mIgnore[11])
                    {
                        mConfig.Configuration.mParameters[NORMALS] = "1";
                    }
                    else if (!mConfig.mNormals && !mConfig.mIgnore[11])
                    {
                        mConfig.Configuration.mParameters[NORMALS] = "2";
                    }
                    if (mConfig.mWeldVertices && !mConfig.mIgnore[12])
                    {
                        mConfig.Configuration.mParameters[WELDVERTICES] = "1";
                    }
                    else if (!mConfig.mWeldVertices && !mConfig.mIgnore[12])
                    {
                        mConfig.Configuration.mParameters[WELDVERTICES] = "2";
                    }
                    break;
                }
            case FolderType.Audio:
                {
                    if (mConfig.mForceToMono && !mConfig.mIgnore[13])
                    {
                        mConfig.Configuration.mParameters[AUDIOFORCETOMONO] = "1";
                    }
                    else if (mConfig.mForceToMono && !mConfig.mIgnore[13])
                    {
                        mConfig.Configuration.mParameters[AUDIOFORCETOMONO] = "2";
                    }
                    break;
                }
        }
        mConfig.Configuration.mParameters[PRIORITY] = mConfig.mPriority.ToString();
    }
    private static void SaveToCSV()
    {
        string prjpath = Application.dataPath + CSVPath;
        for (int i = 0; i < mConfigs.Count; i++)
        {
            Save(mConfigs[i]);
            if (mConfigs[i].mChild != null)
            {
                for (int j = 0; j < mConfigs[i].mChild.Count; j++)
                {
                    Save(mConfigs[i].mChild[j]);
                }
            }
        }
        FileInfo fi = new FileInfo(prjpath);
        FileStream fs = new FileStream(prjpath, System.IO.FileMode.Create, System.IO.FileAccess.Write);
        StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
        Sort();
        string LineData = "";
        for (int j = 0; j < CSVColName.Count; j++)
        {
            LineData += CSVColName[j];
            if (j < CSVColName.Count - 1)
            {
                LineData += ",";
            }
        }
        sw.WriteLine(LineData);
        for (int i = 0; i < mConfigs.Count; i++)
        {
            LineData = "";
            for (int j = 0; j < mConfigs[i].Configuration.mParameters.Count + 1; j++)
            {
                if (j == 0)
                {
                    LineData += mConfigs[i].Configuration.mPath;
                    LineData += ",";
                }
                else
                {
                    LineData += mConfigs[i].Configuration.mParameters[j - 1];
                    if (j < mConfigs[i].Configuration.mParameters.Count)
                    {
                        LineData += ",";
                    }
                }
            }
            sw.WriteLine(LineData);
            if (mConfigs[i].mChild != null)
            {
                for (int c = 0; c < mConfigs[i].mChild.Count; c++)
                {
                    LineData = "";
                    for (int j = 0; j < mConfigs[i].Configuration.mParameters.Count + 1; j++)
                    {
                        if (j == 0)
                        {
                            LineData += mConfigs[i].mChild[c].Configuration.mPath;
                            LineData += ",";
                        }
                        else
                        {
                            LineData += mConfigs[i].mChild[c].Configuration.mParameters[j - 1];
                            if (j < mConfigs[i].Configuration.mParameters.Count)
                            {
                                LineData += ",";
                            }
                        }
                    }
                    sw.WriteLine(LineData);
                }
            }
        }
        sw.Close();
        sw.Dispose();
        fs.Close();
        ImportProccess.ReadCSV();
    }
    //更改多线程弊端问题 目录优先级会被打乱，父目录设置可能取缔子目录设置
    private static void ChangeResource()
    {
        int count = 0;
        List<LogMSG> kLog = new List<LogMSG>();
        DirectoryInfo TargetDir;
        ImportToolStruct kPath;
        foreach (var path in mConfigs)
        {
            kPath = path;
            string TargetDirPath = Application.dataPath + path.Configuration.mPath.Replace("Assets", "");
            TargetDir = new DirectoryInfo(TargetDirPath);
            if (TargetDir.Exists)
            {
                foreach (var fe in FileExtension)
                {
                    FileInfo[] infos = TargetDir.GetFiles(fe, SearchOption.AllDirectories);
                    foreach (var FilePath in infos)
                    {
                        if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("强制更改不符合设置的资源中... 文件[{0}/{1}] ", count, infos.Length), path.Configuration.mPath, count * 1f / infos.Length))
                        {
                            break;
                        }
                        int index = Application.dataPath.Length;
                        string mPath = "Assets" + FilePath.ToString().Substring(index);
                        mPath.Replace("\\", "/");
                        kPath = ContainsFileName(path, FilePath.Name);
                        if (FilePath.Extension.Contains(".tga") || FilePath.Extension.Contains(".png") || FilePath.Extension.Contains(".exr"))
                        {
                            LogMSG kLogmsg = new LogMSG();
                            kLogmsg.mChangeMSG = new List<string>();
                            TextureImporter textureImporter = AssetImporter.GetAtPath(mPath) as TextureImporter;
                            kLogmsg.mPath = FilePath.ToString();
                            kLogmsg.mName = FilePath.Name;
                            TextureImporterSettings kTsetting = new TextureImporterSettings();
                            textureImporter.ReadTextureSettings(kTsetting);
                            TextureImporterPlatformSettings kSettings = new TextureImporterPlatformSettings();
                            #region 检查Alphasource
                            //bool bAlpha = textureImporter.DoesSourceTextureHaveAlpha();
                            bool bAlpha = CheckTextureAlpha(textureImporter);
                            if (bAlpha)
                            {
                                if (textureImporter.alphaSource != TextureImporterAlphaSource.FromInput)
                                {
                                    textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更AlpahSource状态为FromInput");
                                }
                            }
                            else
                            {
                                if (textureImporter.alphaSource != TextureImporterAlphaSource.None)
                                {
                                    textureImporter.alphaSource = TextureImporterAlphaSource.None;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更AlpahSource状态为None");
                                }
                            }
                            #endregion
                            #region 检查读写
                            if (kPath.mTextureRW != textureImporter.isReadable && !kPath.mIgnore[1])
                            {
                                textureImporter.isReadable = kPath.mTextureRW;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更Read/Write为{0}", kPath.mTextureRW));
                            }
                            #endregion
                            textureImporter.alphaIsTransparency = true;
                            #region 检查纹理非二次幂缩放
                            switch (kPath.mNPOTScale)
                            {
                                case TextureNPOTScale.None:
                                    {
                                        if (textureImporter.npotScale != TextureImporterNPOTScale.None)
                                        {
                                            textureImporter.npotScale = TextureImporterNPOTScale.None;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为None");
                                        }
                                        break;
                                    }
                                case TextureNPOTScale.ToNearest:
                                    {
                                        if (textureImporter.npotScale != TextureImporterNPOTScale.ToNearest)
                                        {
                                            textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为ToNearest");
                                        }
                                        break;
                                    }
                                case TextureNPOTScale.ToLarger:
                                    {
                                        if (textureImporter.npotScale != TextureImporterNPOTScale.ToLarger)
                                        {
                                            textureImporter.npotScale = TextureImporterNPOTScale.ToLarger;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为ToLarge");
                                        }
                                        break;
                                    }
                                case TextureNPOTScale.ToSmaller:
                                    {
                                        if (textureImporter.npotScale != TextureImporterNPOTScale.ToSmaller)
                                        {
                                            textureImporter.npotScale = TextureImporterNPOTScale.ToSmaller;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为ToSmall");
                                        }
                                        break;
                                    }
                            }
                            #endregion
                            #region 检查纹理是否开启mipmap
                            if (kPath.mMipMap != textureImporter.mipmapEnabled && !kPath.mIgnore[0])
                            {
                                textureImporter.mipmapEnabled = kPath.mMipMap;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更MipMap状态为{0}", kPath.mMipMap));
                            }
                            #endregion
                            #region 检查纹理filtermode
                            switch (kPath.mFilterMode)
                            {
                                case FilterImportMode.Point:
                                    {
                                        if (textureImporter.filterMode != FilterMode.Point)
                                        {
                                            textureImporter.filterMode = FilterMode.Point;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理FilterMode为Point");
                                        }
                                        break;
                                    }
                                case FilterImportMode.Bilinear:
                                    {
                                        if (textureImporter.filterMode != FilterMode.Bilinear)
                                        {
                                            textureImporter.filterMode = FilterMode.Bilinear;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理FilterMode为Bilinear");
                                        }
                                        break;
                                    }
                                case FilterImportMode.Trilinear:
                                    {
                                        if (textureImporter.filterMode != FilterMode.Trilinear)
                                        {
                                            textureImporter.filterMode = FilterMode.Trilinear;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理FilterMode为Trilinear");
                                        }
                                        break;
                                    }
                            }
                            #endregion
                            #region 检查纹理WrapMode
                            switch (kPath.mWrapMode)
                            {
                                case WrapImportMode.Clamp:
                                    {
                                        if (textureImporter.wrapMode != TextureWrapMode.Clamp)
                                        {
                                            textureImporter.wrapMode = TextureWrapMode.Clamp;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理WrapMode为Clamp");
                                        }
                                        break;
                                    }
                                case WrapImportMode.Mirror:
                                    {
                                        if (textureImporter.wrapMode != TextureWrapMode.Mirror)
                                        {
                                            textureImporter.wrapMode = TextureWrapMode.Mirror;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理WrapMode为Mirror");
                                        }
                                        break;
                                    }
                                case WrapImportMode.MirrorOnce:
                                    {
                                        if (textureImporter.wrapMode != TextureWrapMode.MirrorOnce)
                                        {
                                            textureImporter.wrapMode = TextureWrapMode.MirrorOnce;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理WrapMode为MirrorOnce");
                                        }
                                        break;
                                    }
                                case WrapImportMode.Repeat:
                                    {
                                        if (textureImporter.wrapMode != TextureWrapMode.Repeat)
                                        {
                                            textureImporter.wrapMode = TextureWrapMode.Repeat;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更纹理WrapMode为Repeat");
                                        }
                                        break;
                                    }
                            }
                            #endregion
                            #region 检查纹理type
                            switch (kPath.mTextureType)
                            {
                                case TextureType.TextureDefault:
                                    {
                                        if (textureImporter.textureType != TextureImporterType.Default)
                                        {
                                            textureImporter.textureType = TextureImporterType.Default;
                                            textureImporter.textureShape = TextureImporterShape.Texture2D;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("TextureType修改为Default");
                                        }
                                        break;
                                    }
                                case TextureType.Sprite:
                                    {
                                        if (textureImporter.textureType != TextureImporterType.Sprite)
                                        {
                                            textureImporter.textureType = TextureImporterType.Sprite;
                                            textureImporter.spriteImportMode = SpriteImportMode.Single;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("TextureType修改为Sprite");
                                        }
                                        break;
                                    }
                                case TextureType.LightMap:
                                    {
                                        if (textureImporter.textureType != TextureImporterType.Lightmap)
                                        {
                                            textureImporter.textureType = TextureImporterType.Lightmap;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("TextureType修改为LightMap");
                                        }
                                        break;
                                    }
                            }
                            #endregion
                            #region  检查纹理平台设置
                            TextureImporterFormat kFormat;
                            int maxSize;
                            string kName = "";
                            kSettings.textureCompression = TextureImporterCompression.Uncompressed;
                            kSettings.allowsAlphaSplitting = false;
                            kSettings.overridden = true;
                            //Android
                            kName = "Android";
                            kSettings.name = kName;
                            textureImporter.GetPlatformTextureSettings("Android", out maxSize, out kFormat);
                            switch (kPath.mMaxsize)
                            {
                                case MaxSize.MaxSize4096:
                                    {
                                        if (maxSize != 4096)
                                        {
                                            maxSize = 4096;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为4096");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize2048:
                                    {
                                        if (maxSize != 2048)
                                        {
                                            maxSize = 2048;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为2048");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize1024:
                                    {
                                        if (maxSize != 1024)
                                        {
                                            maxSize = 1024;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为1024");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize512:
                                    {
                                        if (maxSize != 512)
                                        {
                                            maxSize = 512;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为512");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize256:
                                    {
                                        if (maxSize != 256)
                                        {
                                            maxSize = 256;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为256");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize128:
                                    {
                                        if (maxSize != 128)
                                        {
                                            maxSize = 128;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为128");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize64:
                                    {
                                        if (maxSize != 64)
                                        {
                                            maxSize = 64;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为64");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize32:
                                    {
                                        if (maxSize != 32)
                                        {
                                            maxSize = 32;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为32");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize16:
                                    {
                                        if (maxSize != 16)
                                        {
                                            maxSize = 16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android MaxSize为16");
                                        }
                                        break;
                                    }
                            }
                            kSettings.maxTextureSize = maxSize;
                            switch (kPath.mATextureFormat)
                            {
                                case AndroidFormat.ETC2_RGB4:
                                    {
                                        if (kFormat != TextureImporterFormat.ETC2_RGB4)
                                        {
                                            kFormat = TextureImporterFormat.ETC2_RGB4;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ETC2_RGB4");
                                        }
                                        break;
                                    }
                                case AndroidFormat.ETC2_RGBA8:
                                    {
                                        if (kFormat != TextureImporterFormat.ETC2_RGBA8)
                                        {
                                            kFormat = TextureImporterFormat.ETC2_RGBA8;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ETC2_RGBA8");
                                        }
                                        break;
                                    }
                                case AndroidFormat.ETC_RGB4:
                                    {
                                        if (kFormat != TextureImporterFormat.ETC_RGB4)
                                        {
                                            kFormat = TextureImporterFormat.ETC_RGB4;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ETC_RGB4");
                                        }
                                        break;
                                    }
                                case AndroidFormat.RGB16:
                                    {
                                        if (kFormat != TextureImporterFormat.RGB16)
                                        {
                                            kFormat = TextureImporterFormat.RGB16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGB16");
                                        }
                                        break;
                                    }
                                case AndroidFormat.RGBA16:
                                    {
                                        if (kFormat != TextureImporterFormat.RGBA16)
                                        {
                                            kFormat = TextureImporterFormat.RGBA16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGBA16");
                                        }
                                        break;
                                    }
                                case AndroidFormat.RGB24:
                                    {
                                        if (kFormat != TextureImporterFormat.RGB24)
                                        {
                                            kFormat = TextureImporterFormat.RGB24;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGB24");
                                        }
                                        break;
                                    }
                                case AndroidFormat.RGBA32:
                                    {
                                        if (kFormat != TextureImporterFormat.RGBA32)
                                        {
                                            kFormat = TextureImporterFormat.RGBA32;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGBA32");
                                        }
                                        break;
                                    }
                                case AndroidFormat.ASTC_RGB_6X6:
                                    {
                                        if (kFormat != TextureImporterFormat.ASTC_RGB_6x6)
                                        {
                                            kFormat = TextureImporterFormat.ASTC_RGB_6x6;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ASTC_RGB_6x6");
                                        }
                                        break;
                                    }
                            }
                            kSettings.format = kFormat;
                            textureImporter.SetPlatformTextureSettings(kSettings);
                            //iPhone
                            kName = "iPhone";
                            kSettings.name = kName;
                            textureImporter.GetPlatformTextureSettings("iPhone", out maxSize, out kFormat);
                            switch (kPath.mMaxsize)
                            {
                                case MaxSize.MaxSize4096:
                                    {
                                        if (maxSize != 4096)
                                        {
                                            maxSize = 4096;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为4096");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize2048:
                                    {
                                        if (maxSize != 2048)
                                        {
                                            maxSize = 2048;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为2048");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize1024:
                                    {
                                        if (maxSize != 1024)
                                        {
                                            maxSize = 1024;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为1024");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize512:
                                    {
                                        if (maxSize != 512)
                                        {
                                            maxSize = 512;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为512");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize256:
                                    {
                                        if (maxSize != 256)
                                        {
                                            maxSize = 256;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为256");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize128:
                                    {
                                        if (maxSize != 128)
                                        {
                                            maxSize = 128;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为128");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize64:
                                    {
                                        if (maxSize != 64)
                                        {
                                            maxSize = 64;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为64");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize32:
                                    {
                                        if (maxSize != 32)
                                        {
                                            maxSize = 32;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为32");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize16:
                                    {
                                        if (maxSize != 16)
                                        {
                                            maxSize = 16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为16");
                                        }
                                        break;
                                    }
                            }
                            kSettings.maxTextureSize = maxSize;
                            switch (kPath.mITextureFormat)
                            {
                                case iPhoneFormat.PVRTC_RGB2:
                                    {
                                        if (kFormat != TextureImporterFormat.PVRTC_RGB2)
                                        {
                                            kFormat = TextureImporterFormat.PVRTC_RGB2;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGB2");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.PVRTC_RGBA2:
                                    {
                                        if (kFormat != TextureImporterFormat.PVRTC_RGBA2)
                                        {
                                            kFormat = TextureImporterFormat.PVRTC_RGBA2;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGBA2");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.PVRTC_RGB4:
                                    {
                                        if (kFormat != TextureImporterFormat.PVRTC_RGB4)
                                        {
                                            kFormat = TextureImporterFormat.PVRTC_RGB4;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGB4");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.PVRTC_RGBA4:
                                    {
                                        if (kFormat != TextureImporterFormat.PVRTC_RGBA4)
                                        {
                                            kFormat = TextureImporterFormat.PVRTC_RGBA4;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGBA4");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.RGB16:
                                    {
                                        if (kFormat != TextureImporterFormat.RGB16)
                                        {
                                            kFormat = TextureImporterFormat.RGB16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGB16");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.RGBA16:
                                    {
                                        if (kFormat != TextureImporterFormat.RGBA16)
                                        {
                                            kFormat = TextureImporterFormat.RGBA16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGBA16");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.RGB24:
                                    {
                                        if (kFormat != TextureImporterFormat.RGB24)
                                        {
                                            kFormat = TextureImporterFormat.RGB24;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGB24");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.RGBA32:
                                    {
                                        if (kFormat != TextureImporterFormat.RGBA32)
                                        {
                                            kFormat = TextureImporterFormat.RGBA32;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGBA32");
                                        }
                                        break;
                                    }
                                case iPhoneFormat.ASTC_RGB_6X6:
                                    {
                                        if (kFormat != TextureImporterFormat.ASTC_RGB_6x6)
                                        {
                                            kFormat = TextureImporterFormat.ASTC_RGB_6x6;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFromat为ASTC_RGB_6x6");
                                        }
                                        break;
                                    }
                            }
                            kSettings.format = kFormat;
                            textureImporter.SetPlatformTextureSettings(kSettings);
                            //Standalone
                            kName = "Standalone";
                            kSettings.name = kName;
                            textureImporter.GetPlatformTextureSettings("Standalone", out maxSize, out kFormat);
                            switch (kPath.mMaxsize)
                            {
                                case MaxSize.MaxSize4096:
                                    {
                                        if (maxSize != 4096)
                                        {
                                            maxSize = 4096;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为4096");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize2048:
                                    {
                                        if (maxSize != 2048)
                                        {
                                            maxSize = 2048;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为2048");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize1024:
                                    {
                                        if (maxSize != 1024)
                                        {
                                            maxSize = 1024;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为1024");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize512:
                                    {
                                        if (maxSize != 512)
                                        {
                                            maxSize = 512;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为512");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize256:
                                    {
                                        if (maxSize != 256)
                                        {
                                            maxSize = 256;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为256");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize128:
                                    {
                                        if (maxSize != 128)
                                        {
                                            maxSize = 128;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为128");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize64:
                                    {
                                        if (maxSize != 64)
                                        {
                                            maxSize = 64;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为64");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize32:
                                    {
                                        if (maxSize != 32)
                                        {
                                            maxSize = 32;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为32");
                                        }
                                        break;
                                    }
                                case MaxSize.MaxSize16:
                                    {
                                        if (maxSize != 16)
                                        {
                                            maxSize = 16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为16");
                                        }
                                        break;
                                    }
                            }
                            kSettings.maxTextureSize = maxSize;
                            switch (kPath.mWTextureFormat)
                            {
                                case StandaloneFormat.DXT1:
                                    {
                                        if (kFormat != TextureImporterFormat.DXT1)
                                        {
                                            kFormat = TextureImporterFormat.DXT1;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为DXT1");
                                        }
                                        break;
                                    }
                                case StandaloneFormat.DXT5:
                                    {
                                        if (kFormat != TextureImporterFormat.DXT5)
                                        {
                                            kFormat = TextureImporterFormat.DXT5;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为DXT5");
                                        }
                                        break;
                                    }
                                case StandaloneFormat.ARGB16:
                                    {
                                        if (kFormat != TextureImporterFormat.ARGB16)
                                        {
                                            kFormat = TextureImporterFormat.ARGB16;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Widnows平台TextureFormat为ARGB16");
                                        }
                                        break;
                                    }
                                case StandaloneFormat.RGB24:
                                    {
                                        if (kFormat != TextureImporterFormat.RGB24)
                                        {
                                            kFormat = TextureImporterFormat.RGB24;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为RGB24");
                                        }
                                        break;
                                    }
                                case StandaloneFormat.RGBA32:
                                    {
                                        if (kFormat != TextureImporterFormat.RGBA32)
                                        {
                                            kFormat = TextureImporterFormat.RGBA32;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为RGBA32");
                                        }
                                        break;
                                    }
                                case StandaloneFormat.ARGB32:
                                    {
                                        if (kFormat != TextureImporterFormat.ARGB32)
                                        {
                                            kFormat = TextureImporterFormat.ARGB32;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGB24");
                                        }
                                        break;
                                    }
                            }
                            kSettings.format = kFormat;
                            textureImporter.SetPlatformTextureSettings(kSettings);
                            //
                            #endregion
                            textureImporter.SetTextureSettings(kTsetting);
                            AssetDatabase.ImportAsset(mPath);
                            if (kLogmsg.mChange)
                            {
                                kLog.Add(kLogmsg);
                            }
                        }
                        if (FilePath.Extension.Contains(".FBX"))
                        {
                            LogMSG kLogmsg = new LogMSG();
                            kLogmsg.mChangeMSG = new List<string>();
                            ModelImporter modelImporter = AssetImporter.GetAtPath(mPath) as ModelImporter;
                            kLogmsg.mPath = FilePath.ToString();
                            kLogmsg.mName = FilePath.Name;
                            #region  检查Model读写
                            if (modelImporter.isReadable != kPath.mModelRW && !kPath.mIgnore[2])
                            {
                                modelImporter.isReadable = kPath.mModelRW;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更模型读写为{0}", kPath.mModelRW));
                            }
                            #endregion
                            #region 检查模型动画开关
                            if (kPath.mHaveanim && !kPath.mIgnore[3])
                            {
                                if (modelImporter.animationType != ModelImporterAnimationType.Generic)
                                {
                                    modelImporter.animationType = ModelImporterAnimationType.Generic;
                                    modelImporter.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更模型AnimationType为Generic");
                                }
                            }
                            else if (!kPath.mHaveanim && !kPath.mIgnore[3])
                            {
                                if (modelImporter.animationType != ModelImporterAnimationType.None)
                                {
                                    modelImporter.animationType = ModelImporterAnimationType.None;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更模型AnimationType为None");
                                }
                            }
                            #endregion
                            #region 检查Model Mesh压缩
                            //switch (kPath.mMeshCompress)
                            //{
                            //    case MeshCompress.Off:
                            //        {
                            //            if (modelImporter.meshCompression != ModelImporterMeshCompression.Off)
                            //            {
                            //                modelImporter.meshCompression = ModelImporterMeshCompression.Off;
                            //                kLogmsg.mChange = true;
                            //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为OFF");
                            //            }
                            //            break;
                            //        }
                            //    case MeshCompress.Low:
                            //        {
                            //            if (modelImporter.meshCompression != ModelImporterMeshCompression.Low)
                            //            {
                            //                modelImporter.meshCompression = ModelImporterMeshCompression.Low;
                            //                kLogmsg.mChange = true;
                            //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为low");
                            //            }
                            //            break;
                            //        }
                            //    case MeshCompress.Medium:
                            //        {
                            //            if (modelImporter.meshCompression != ModelImporterMeshCompression.Medium)
                            //            {
                            //                modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
                            //                kLogmsg.mChange = true;
                            //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为Medium");
                            //            }
                            //            break;
                            //        }
                            //    case MeshCompress.High:
                            //        {
                            //            if (modelImporter.meshCompression != ModelImporterMeshCompression.High)
                            //            {
                            //                modelImporter.meshCompression = ModelImporterMeshCompression.High;
                            //                kLogmsg.mChange = true;
                            //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为High");
                            //            }
                            //            break;
                            //        }
                            //}
                            #endregion
                            #region 检查model是否打开mesh优化
                            if (kPath.mOptimizeMesh != modelImporter.optimizeMesh && !kPath.mIgnore[4])
                            {
                                modelImporter.optimizeMesh = kPath.mOptimizeMesh;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更model Mesh优化为{0}", kPath.mOptimizeMesh));
                            }
                            #endregion
                            #region 检查model是否打开Tangent空间
                            switch (kPath.mTangents)
                            {
                                case Tangents.None:
                                    {
                                        if (modelImporter.importTangents != ModelImporterTangents.None)
                                        {
                                            modelImporter.importTangents = ModelImporterTangents.None;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Model Tangent为None");
                                        }
                                        break;
                                    }
                                case Tangents.Calculatelegacy:
                                    {
                                        if (modelImporter.importTangents != ModelImporterTangents.CalculateLegacy)
                                        {
                                            modelImporter.importTangents = ModelImporterTangents.CalculateLegacy;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Model Tangent为CalculateLegacy");
                                        }
                                        break;
                                    }
                                case Tangents.CalculateMikktspace:
                                    {
                                        if (modelImporter.importTangents != ModelImporterTangents.CalculateMikk)
                                        {
                                            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                                            kLogmsg.mChange = true;
                                            kLogmsg.mChangeMSG.Add("变更Model Tangent为CalculateMikk");
                                        }
                                        break;
                                    }
                            }

                            #endregion
                            if (modelImporter.optimizeGameObjects != kPath.mOptimizeGO && !kPath.mIgnore[6])
                            {
                                modelImporter.optimizeGameObjects = kPath.mOptimizeGO;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更model 动画设置中OptimizeGameObjec优化为{0}", kPath.mOptimizeGO));
                            }
                            if (modelImporter.importBlendShapes != kPath.mImportBlendshap && !kPath.mIgnore[7])
                            {
                                modelImporter.importBlendShapes = kPath.mImportBlendshap;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更model ImportBlendShapes为{0}", kPath.mImportBlendshap));
                            }
                            if (modelImporter.importVisibility != kPath.mImportVisibilty && !kPath.mIgnore[8])
                            {
                                modelImporter.importVisibility = kPath.mImportVisibilty;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更model ImportBisibility为{0}", kPath.mImportVisibilty));
                            }
                            if (modelImporter.importCameras != kPath.mImportCamera && !kPath.mIgnore[9])
                            {
                                modelImporter.importCameras = kPath.mImportCamera;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更model ImportCameras为{0}", kPath.mImportCamera));
                            }
                            if (modelImporter.importLights != kPath.mImportLight && !kPath.mIgnore[10])
                            {
                                modelImporter.importLights = kPath.mImportLight;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更model ImportLights为{0}", kPath.mImportLight));
                            }
                            if (modelImporter.weldVertices != kPath.mWeldVertices && !kPath.mIgnore[12])
                            {
                                modelImporter.weldVertices = kPath.mWeldVertices;
                                kLogmsg.mChange = true;
                                kLogmsg.mChangeMSG.Add(string.Format("变更model WeldVertices为{0}", kPath.mWeldVertices));
                            }
                            #region 检查model Normal空间
                            if (kPath.mNormals && !kPath.mIgnore[11])
                            {
                                if (modelImporter.importNormals != ModelImporterNormals.Import)
                                {
                                    modelImporter.importNormals = ModelImporterNormals.Import;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Model Normals为Import");
                                }
                            }
                            else if (!kPath.mNormals && !kPath.mIgnore[11])
                            {
                                if (modelImporter.importNormals != ModelImporterNormals.None)
                                {
                                    modelImporter.importNormals = ModelImporterNormals.None;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Model Normals为None");
                                }
                            }
                            #endregion
                            modelImporter.SaveAndReimport();
                            AssetDatabase.ImportAsset(mPath);
                            if (kLogmsg.mChange)
                            {
                                kLog.Add(kLogmsg);
                            }
                        }
                        count += 1;
                    }
                    EditorUtility.ClearProgressBar();
                    count = 0;
                }
            }
            else
            {
                Debug.Log(TargetDir + "文件夹不存在");
                count += 1;
            }
        }
        StreamWriter sw;
        FileInfo fi = new FileInfo(Application.dataPath + LogPath);
        if (fi.Exists)
        {
            sw = fi.AppendText();
        }
        else
        {
            sw = fi.CreateText();
        }

        sw.WriteLine(System.DateTime.Now);
        foreach (var logMsg in kLog)
        {
            sw.WriteLine(logMsg.mPath);
            sw.WriteLine(logMsg.mName);
            foreach (var lg in logMsg.mChangeMSG)
            {
                sw.WriteLine(lg);
            }
            sw.WriteLine();
        }
        sw.Close();
        sw.Dispose();
        System.Diagnostics.Process.Start(Application.dataPath + LogPath);
    }
    private static void ChangeSignleFolderResource(ImportToolStruct path)
    {
        int count = 0;
        List<LogMSG> kLog = new List<LogMSG>();
        ImportToolStruct kPath = path;
        string TargetDirPath = Application.dataPath + path.Configuration.mPath.Replace("Assets", "");
        DirectoryInfo TargetDir = new DirectoryInfo(TargetDirPath);
        if (TargetDir.Exists)
        {
            FileInfo[] infos = TargetDir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var FilePath in infos)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("强制更改不符合设置的资源中... [{0}/{1}] ", count, infos.Length), FilePath.Name, count * 1f / infos.Length))
                {
                    break;
                }
                int index = Application.dataPath.Length;
                string mPath = "Assets" + FilePath.ToString().Substring(index);
                mPath.Replace("\\", "/");
                kPath = ContainsFileName(path, FilePath.Name);
                if (FilePath.Extension.Contains(".tga") || FilePath.Extension.Contains(".png") || FilePath.Extension.Contains(".exr"))
                {
                    LogMSG kLogmsg = new LogMSG();
                    kLogmsg.mChangeMSG = new List<string>();
                    TextureImporter textureImporter = AssetImporter.GetAtPath(mPath) as TextureImporter;
                    kLogmsg.mPath = FilePath.ToString();
                    kLogmsg.mName = FilePath.Name;
                    TextureImporterSettings kTsetting = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(kTsetting);
                    TextureImporterPlatformSettings kSettings = new TextureImporterPlatformSettings();
                    #region 检查Alphasource
                    //bool bAlpha = textureImporter.DoesSourceTextureHaveAlpha();
                    bool bAlpha = CheckTextureAlpha(textureImporter);
                    if (bAlpha)
                    {
                        if (textureImporter.alphaSource != TextureImporterAlphaSource.FromInput)
                        {
                            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                            kLogmsg.mChange = true;
                            kLogmsg.mChangeMSG.Add("变更AlpahSource状态为FromInput");
                        }
                    }
                    else
                    {
                        if (textureImporter.alphaSource != TextureImporterAlphaSource.None)
                        {
                            textureImporter.alphaSource = TextureImporterAlphaSource.None;
                            kLogmsg.mChange = true;
                            kLogmsg.mChangeMSG.Add("变更AlpahSource状态为None");
                        }
                    }
                    #endregion
                    #region 检查读写
                    if (kPath.mTextureRW != textureImporter.isReadable && !kPath.mIgnore[1])
                    {
                        textureImporter.isReadable = kPath.mTextureRW;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更Read/Write为{0}", kPath.mTextureRW));
                    }
                    #endregion
                    textureImporter.alphaIsTransparency = true;
                    #region 检查纹理非二次幂缩放
                    switch (kPath.mNPOTScale)
                    {
                        case TextureNPOTScale.None:
                            {
                                if (textureImporter.npotScale != TextureImporterNPOTScale.None)
                                {
                                    textureImporter.npotScale = TextureImporterNPOTScale.None;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为None");
                                }
                                break;
                            }
                        case TextureNPOTScale.ToNearest:
                            {
                                if (textureImporter.npotScale != TextureImporterNPOTScale.ToNearest)
                                {
                                    textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为ToNearest");
                                }
                                break;
                            }
                        case TextureNPOTScale.ToLarger:
                            {
                                if (textureImporter.npotScale != TextureImporterNPOTScale.ToLarger)
                                {
                                    textureImporter.npotScale = TextureImporterNPOTScale.ToLarger;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为ToLarge");
                                }
                                break;
                            }
                        case TextureNPOTScale.ToSmaller:
                            {
                                if (textureImporter.npotScale != TextureImporterNPOTScale.ToSmaller)
                                {
                                    textureImporter.npotScale = TextureImporterNPOTScale.ToSmaller;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理二次幂缩放为ToSmall");
                                }
                                break;
                            }
                    }
                    #endregion
                    #region 检查纹理是否开启mipmap
                    if (kPath.mMipMap != textureImporter.mipmapEnabled && !kPath.mIgnore[0])
                    {
                        textureImporter.mipmapEnabled = kPath.mMipMap;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更MipMap状态为{0}", kPath.mMipMap));
                    }
                    #endregion
                    #region 检查纹理filtermode
                    switch (kPath.mFilterMode)
                    {
                        case FilterImportMode.Point:
                            {
                                if (textureImporter.filterMode != FilterMode.Point)
                                {
                                    textureImporter.filterMode = FilterMode.Point;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理FilterMode为Point");
                                }
                                break;
                            }
                        case FilterImportMode.Bilinear:
                            {
                                if (textureImporter.filterMode != FilterMode.Bilinear)
                                {
                                    textureImporter.filterMode = FilterMode.Bilinear;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理FilterMode为Bilinear");
                                }
                                break;
                            }
                        case FilterImportMode.Trilinear:
                            {
                                if (textureImporter.filterMode != FilterMode.Trilinear)
                                {
                                    textureImporter.filterMode = FilterMode.Trilinear;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理FilterMode为Trilinear");
                                }
                                break;
                            }
                    }
                    #endregion
                    #region 检查纹理WrapMode
                    switch (kPath.mWrapMode)
                    {
                        case WrapImportMode.Clamp:
                            {
                                if (textureImporter.wrapMode != TextureWrapMode.Clamp)
                                {
                                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理WrapMode为Clamp");
                                }
                                break;
                            }
                        case WrapImportMode.Mirror:
                            {
                                if (textureImporter.wrapMode != TextureWrapMode.Mirror)
                                {
                                    textureImporter.wrapMode = TextureWrapMode.Mirror;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理WrapMode为Mirror");
                                }
                                break;
                            }
                        case WrapImportMode.MirrorOnce:
                            {
                                if (textureImporter.wrapMode != TextureWrapMode.MirrorOnce)
                                {
                                    textureImporter.wrapMode = TextureWrapMode.MirrorOnce;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理WrapMode为MirrorOnce");
                                }
                                break;
                            }
                        case WrapImportMode.Repeat:
                            {
                                if (textureImporter.wrapMode != TextureWrapMode.Repeat)
                                {
                                    textureImporter.wrapMode = TextureWrapMode.Repeat;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更纹理WrapMode为Repeat");
                                }
                                break;
                            }
                    }
                    #endregion
                    #region 检查纹理type
                    switch (kPath.mTextureType)
                    {
                        case TextureType.TextureDefault:
                            {
                                if (textureImporter.textureType != TextureImporterType.Default)
                                {
                                    textureImporter.textureType = TextureImporterType.Default;
                                    textureImporter.textureShape = TextureImporterShape.Texture2D;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("TextureType修改为Default");
                                }
                                break;
                            }
                        case TextureType.Sprite:
                            {
                                if (textureImporter.textureType != TextureImporterType.Sprite)
                                {
                                    textureImporter.textureType = TextureImporterType.Sprite;
                                    textureImporter.spriteImportMode = SpriteImportMode.Single;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("TextureType修改为Sprite");
                                }
                                break;
                            }
                        case TextureType.LightMap:
                            {
                                if (textureImporter.textureType != TextureImporterType.Lightmap)
                                {
                                    textureImporter.textureType = TextureImporterType.Lightmap;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("TextureType修改为LightMap");
                                }
                                break;
                            }
                    }
                    #endregion
                    #region  检查纹理平台设置
                    TextureImporterFormat kFormat;
                    int maxSize;
                    string kName = "";
                    kSettings.textureCompression = TextureImporterCompression.Uncompressed;
                    kSettings.allowsAlphaSplitting = false;
                    kSettings.overridden = true;
                    //Android
                    kName = "Android";
                    kSettings.name = kName;
                    textureImporter.GetPlatformTextureSettings("Android", out maxSize, out kFormat);
                    switch (kPath.mMaxsize)
                    {
                        case MaxSize.MaxSize4096:
                            {
                                if (maxSize != 4096)
                                {
                                    maxSize = 4096;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为4096");
                                }
                                break;
                            }
                        case MaxSize.MaxSize2048:
                            {
                                if (maxSize != 2048)
                                {
                                    maxSize = 2048;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为2048");
                                }
                                break;
                            }
                        case MaxSize.MaxSize1024:
                            {
                                if (maxSize != 1024)
                                {
                                    maxSize = 1024;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为1024");
                                }
                                break;
                            }
                        case MaxSize.MaxSize512:
                            {
                                if (maxSize != 512)
                                {
                                    maxSize = 512;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为512");
                                }
                                break;
                            }
                        case MaxSize.MaxSize256:
                            {
                                if (maxSize != 256)
                                {
                                    maxSize = 256;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为256");
                                }
                                break;
                            }
                        case MaxSize.MaxSize128:
                            {
                                if (maxSize != 128)
                                {
                                    maxSize = 128;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为128");
                                }
                                break;
                            }
                        case MaxSize.MaxSize64:
                            {
                                if (maxSize != 64)
                                {
                                    maxSize = 64;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为64");
                                }
                                break;
                            }
                        case MaxSize.MaxSize32:
                            {
                                if (maxSize != 32)
                                {
                                    maxSize = 32;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为32");
                                }
                                break;
                            }
                        case MaxSize.MaxSize16:
                            {
                                if (maxSize != 16)
                                {
                                    maxSize = 16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android MaxSize为16");
                                }
                                break;
                            }
                    }
                    kSettings.maxTextureSize = maxSize;
                    switch (kPath.mATextureFormat)
                    {
                        case AndroidFormat.ETC2_RGB4:
                            {
                                if (kFormat != TextureImporterFormat.ETC2_RGB4)
                                {
                                    kFormat = TextureImporterFormat.ETC2_RGB4;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ETC2_RGB4");
                                }
                                break;
                            }
                        case AndroidFormat.ETC2_RGBA8:
                            {
                                if (kFormat != TextureImporterFormat.ETC2_RGBA8)
                                {
                                    kFormat = TextureImporterFormat.ETC2_RGBA8;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ETC2_RGBA8");
                                }
                                break;
                            }
                        case AndroidFormat.ETC_RGB4:
                            {
                                if (kFormat != TextureImporterFormat.ETC_RGB4)
                                {
                                    kFormat = TextureImporterFormat.ETC_RGB4;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ETC_RGB4");
                                }
                                break;
                            }
                        case AndroidFormat.RGB16:
                            {
                                if (kFormat != TextureImporterFormat.RGB16)
                                {
                                    kFormat = TextureImporterFormat.RGB16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGB16");
                                }
                                break;
                            }
                        case AndroidFormat.RGBA16:
                            {
                                if (kFormat != TextureImporterFormat.RGBA16)
                                {
                                    kFormat = TextureImporterFormat.RGBA16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGBA16");
                                }
                                break;
                            }
                        case AndroidFormat.RGB24:
                            {
                                if (kFormat != TextureImporterFormat.RGB24)
                                {
                                    kFormat = TextureImporterFormat.RGB24;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGB24");
                                }
                                break;
                            }
                        case AndroidFormat.RGBA32:
                            {
                                if (kFormat != TextureImporterFormat.RGBA32)
                                {
                                    kFormat = TextureImporterFormat.RGBA32;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGBA32");
                                }
                                break;
                            }
                        case AndroidFormat.ASTC_RGB_6X6:
                            {
                                if (kFormat != TextureImporterFormat.ASTC_RGB_6x6)
                                {
                                    kFormat = TextureImporterFormat.ASTC_RGB_6x6;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为ASTC_RGB_6x6");
                                }
                                break;
                            }
                    }
                    kSettings.format = kFormat;
                    textureImporter.SetPlatformTextureSettings(kSettings);
                    //iPhone
                    kName = "iPhone";
                    kSettings.name = kName;
                    textureImporter.GetPlatformTextureSettings("iPhone", out maxSize, out kFormat);
                    switch (kPath.mMaxsize)
                    {
                        case MaxSize.MaxSize4096:
                            {
                                if (maxSize != 4096)
                                {
                                    maxSize = 4096;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为4096");
                                }
                                break;
                            }
                        case MaxSize.MaxSize2048:
                            {
                                if (maxSize != 2048)
                                {
                                    maxSize = 2048;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为2048");
                                }
                                break;
                            }
                        case MaxSize.MaxSize1024:
                            {
                                if (maxSize != 1024)
                                {
                                    maxSize = 1024;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为1024");
                                }
                                break;
                            }
                        case MaxSize.MaxSize512:
                            {
                                if (maxSize != 512)
                                {
                                    maxSize = 512;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为512");
                                }
                                break;
                            }
                        case MaxSize.MaxSize256:
                            {
                                if (maxSize != 256)
                                {
                                    maxSize = 256;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为256");
                                }
                                break;
                            }
                        case MaxSize.MaxSize128:
                            {
                                if (maxSize != 128)
                                {
                                    maxSize = 128;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为128");
                                }
                                break;
                            }
                        case MaxSize.MaxSize64:
                            {
                                if (maxSize != 64)
                                {
                                    maxSize = 64;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为64");
                                }
                                break;
                            }
                        case MaxSize.MaxSize32:
                            {
                                if (maxSize != 32)
                                {
                                    maxSize = 32;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为32");
                                }
                                break;
                            }
                        case MaxSize.MaxSize16:
                            {
                                if (maxSize != 16)
                                {
                                    maxSize = 16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone MaxSize为16");
                                }
                                break;
                            }
                    }
                    kSettings.maxTextureSize = maxSize;
                    switch (kPath.mITextureFormat)
                    {
                        case iPhoneFormat.PVRTC_RGB2:
                            {
                                if (kFormat != TextureImporterFormat.PVRTC_RGB2)
                                {
                                    kFormat = TextureImporterFormat.PVRTC_RGB2;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGB2");
                                }
                                break;
                            }
                        case iPhoneFormat.PVRTC_RGBA2:
                            {
                                if (kFormat != TextureImporterFormat.PVRTC_RGBA2)
                                {
                                    kFormat = TextureImporterFormat.PVRTC_RGBA2;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGBA2");
                                }
                                break;
                            }
                        case iPhoneFormat.PVRTC_RGB4:
                            {
                                if (kFormat != TextureImporterFormat.PVRTC_RGB4)
                                {
                                    kFormat = TextureImporterFormat.PVRTC_RGB4;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGB4");
                                }
                                break;
                            }
                        case iPhoneFormat.PVRTC_RGBA4:
                            {
                                if (kFormat != TextureImporterFormat.PVRTC_RGBA4)
                                {
                                    kFormat = TextureImporterFormat.PVRTC_RGBA4;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为PVRTC_RGBA4");
                                }
                                break;
                            }
                        case iPhoneFormat.RGB16:
                            {
                                if (kFormat != TextureImporterFormat.RGB16)
                                {
                                    kFormat = TextureImporterFormat.RGB16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGB16");
                                }
                                break;
                            }
                        case iPhoneFormat.RGBA16:
                            {
                                if (kFormat != TextureImporterFormat.RGBA16)
                                {
                                    kFormat = TextureImporterFormat.RGBA16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGBA16");
                                }
                                break;
                            }
                        case iPhoneFormat.RGB24:
                            {
                                if (kFormat != TextureImporterFormat.RGB24)
                                {
                                    kFormat = TextureImporterFormat.RGB24;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGB24");
                                }
                                break;
                            }
                        case iPhoneFormat.RGBA32:
                            {
                                if (kFormat != TextureImporterFormat.RGBA32)
                                {
                                    kFormat = TextureImporterFormat.RGBA32;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFormat为RGBA32");
                                }
                                break;
                            }
                        case iPhoneFormat.ASTC_RGB_6X6:
                            {
                                if (kFormat != TextureImporterFormat.ASTC_RGB_6x6)
                                {
                                    kFormat = TextureImporterFormat.ASTC_RGB_6x6;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更iPhone平台TextureFromat为ASTC_RGB_6x6");
                                }
                                break;
                            }
                    }
                    kSettings.format = kFormat;
                    textureImporter.SetPlatformTextureSettings(kSettings);
                    //Standalone
                    kName = "Standalone";
                    kSettings.name = kName;
                    textureImporter.GetPlatformTextureSettings("Standalone", out maxSize, out kFormat);
                    switch (kPath.mMaxsize)
                    {
                        case MaxSize.MaxSize4096:
                            {
                                if (maxSize != 4096)
                                {
                                    maxSize = 4096;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为4096");
                                }
                                break;
                            }
                        case MaxSize.MaxSize2048:
                            {
                                if (maxSize != 2048)
                                {
                                    maxSize = 2048;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为2048");
                                }
                                break;
                            }
                        case MaxSize.MaxSize1024:
                            {
                                if (maxSize != 1024)
                                {
                                    maxSize = 1024;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为1024");
                                }
                                break;
                            }
                        case MaxSize.MaxSize512:
                            {
                                if (maxSize != 512)
                                {
                                    maxSize = 512;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为512");
                                }
                                break;
                            }
                        case MaxSize.MaxSize256:
                            {
                                if (maxSize != 256)
                                {
                                    maxSize = 256;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为256");
                                }
                                break;
                            }
                        case MaxSize.MaxSize128:
                            {
                                if (maxSize != 128)
                                {
                                    maxSize = 128;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为128");
                                }
                                break;
                            }
                        case MaxSize.MaxSize64:
                            {
                                if (maxSize != 64)
                                {
                                    maxSize = 64;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为64");
                                }
                                break;
                            }
                        case MaxSize.MaxSize32:
                            {
                                if (maxSize != 32)
                                {
                                    maxSize = 32;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为32");
                                }
                                break;
                            }
                        case MaxSize.MaxSize16:
                            {
                                if (maxSize != 16)
                                {
                                    maxSize = 16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更windows平台 MaxSize为16");
                                }
                                break;
                            }
                    }
                    kSettings.maxTextureSize = maxSize;
                    switch (kPath.mWTextureFormat)
                    {
                        case StandaloneFormat.DXT1:
                            {
                                if (kFormat != TextureImporterFormat.DXT1)
                                {
                                    kFormat = TextureImporterFormat.DXT1;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为DXT1");
                                }
                                break;
                            }
                        case StandaloneFormat.DXT5:
                            {
                                if (kFormat != TextureImporterFormat.DXT5)
                                {
                                    kFormat = TextureImporterFormat.DXT5;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为DXT5");
                                }
                                break;
                            }
                        case StandaloneFormat.ARGB16:
                            {
                                if (kFormat != TextureImporterFormat.ARGB16)
                                {
                                    kFormat = TextureImporterFormat.ARGB16;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Widnows平台TextureFormat为ARGB16");
                                }
                                break;
                            }
                        case StandaloneFormat.RGB24:
                            {
                                if (kFormat != TextureImporterFormat.RGB24)
                                {
                                    kFormat = TextureImporterFormat.RGB24;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为RGB24");
                                }
                                break;
                            }
                        case StandaloneFormat.RGBA32:
                            {
                                if (kFormat != TextureImporterFormat.RGBA32)
                                {
                                    kFormat = TextureImporterFormat.RGBA32;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Windows平台TextureFormat为RGBA32");
                                }
                                break;
                            }
                        case StandaloneFormat.ARGB32:
                            {
                                if (kFormat != TextureImporterFormat.ARGB32)
                                {
                                    kFormat = TextureImporterFormat.ARGB32;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Android平台TextureFormat为RGB24");
                                }
                                break;
                            }
                    }
                    kSettings.format = kFormat;
                    textureImporter.SetPlatformTextureSettings(kSettings);
                    //
                    #endregion
                    textureImporter.SetTextureSettings(kTsetting);
                    AssetDatabase.ImportAsset(mPath);
                    if (kLogmsg.mChange)
                    {
                        kLog.Add(kLogmsg);
                    }
                }
                if (FilePath.Extension.Contains(".FBX"))
                {
                    LogMSG kLogmsg = new LogMSG();
                    kLogmsg.mChangeMSG = new List<string>();
                    ModelImporter modelImporter = AssetImporter.GetAtPath(mPath) as ModelImporter;
                    kLogmsg.mPath = FilePath.ToString();
                    kLogmsg.mName = FilePath.Name;
                    #region  检查Model读写
                    if (modelImporter.isReadable != kPath.mModelRW && !kPath.mIgnore[2])
                    {
                        modelImporter.isReadable = kPath.mModelRW;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更模型读写为{0}", kPath.mModelRW));
                    }
                    #endregion
                    #region 检查模型动画开关
                    if (kPath.mHaveanim && !kPath.mIgnore[3])
                    {
                        if (modelImporter.animationType != ModelImporterAnimationType.Generic)
                        {
                            modelImporter.animationType = ModelImporterAnimationType.Generic;
                            modelImporter.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                            kLogmsg.mChange = true;
                            kLogmsg.mChangeMSG.Add("变更模型AnimationType为Generic");
                        }
                    }
                    else if (!kPath.mHaveanim && !kPath.mIgnore[3])
                    {
                        if (modelImporter.animationType != ModelImporterAnimationType.None)
                        {
                            modelImporter.animationType = ModelImporterAnimationType.None;
                            kLogmsg.mChange = true;
                            kLogmsg.mChangeMSG.Add("变更模型AnimationType为None");
                        }
                    }
                    #endregion
                    #region 检查Model Mesh压缩
                    //switch (kPath.mMeshCompress)
                    //{
                    //    case MeshCompress.Off:
                    //        {
                    //            if (modelImporter.meshCompression != ModelImporterMeshCompression.Off)
                    //            {
                    //                modelImporter.meshCompression = ModelImporterMeshCompression.Off;
                    //                kLogmsg.mChange = true;
                    //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为OFF");
                    //            }
                    //            break;
                    //        }
                    //    case MeshCompress.Low:
                    //        {
                    //            if (modelImporter.meshCompression != ModelImporterMeshCompression.Low)
                    //            {
                    //                modelImporter.meshCompression = ModelImporterMeshCompression.Low;
                    //                kLogmsg.mChange = true;
                    //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为low");
                    //            }
                    //            break;
                    //        }
                    //    case MeshCompress.Medium:
                    //        {
                    //            if (modelImporter.meshCompression != ModelImporterMeshCompression.Medium)
                    //            {
                    //                modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
                    //                kLogmsg.mChange = true;
                    //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为Medium");
                    //            }
                    //            break;
                    //        }
                    //    case MeshCompress.High:
                    //        {
                    //            if (modelImporter.meshCompression != ModelImporterMeshCompression.High)
                    //            {
                    //                modelImporter.meshCompression = ModelImporterMeshCompression.High;
                    //                kLogmsg.mChange = true;
                    //                kLogmsg.mChangeMSG.Add("变更模型Mesh压缩为High");
                    //            }
                    //            break;
                    //        }
                    //}
                    #endregion
                    #region 检查model是否打开mesh优化
                    if (kPath.mOptimizeMesh != modelImporter.optimizeMesh && !kPath.mIgnore[4])
                    {
                        modelImporter.optimizeMesh = kPath.mOptimizeMesh;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更model Mesh优化为{0}", kPath.mOptimizeMesh));
                    }
                    #endregion
                    #region 检查model是否打开Tangent空间
                    switch (kPath.mTangents)
                    {
                        case Tangents.None:
                            {
                                if (modelImporter.importTangents != ModelImporterTangents.None)
                                {
                                    modelImporter.importTangents = ModelImporterTangents.None;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Model Tangent为None");
                                }
                                break;
                            }
                        case Tangents.Calculatelegacy:
                            {
                                if (modelImporter.importTangents != ModelImporterTangents.CalculateLegacy)
                                {
                                    modelImporter.importTangents = ModelImporterTangents.CalculateLegacy;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Model Tangent为CalculateLegacy");
                                }
                                break;
                            }
                        case Tangents.CalculateMikktspace:
                            {
                                if (modelImporter.importTangents != ModelImporterTangents.CalculateMikk)
                                {
                                    modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                                    kLogmsg.mChange = true;
                                    kLogmsg.mChangeMSG.Add("变更Model Tangent为CalculateMikk");
                                }
                                break;
                            }
                    }
                    #endregion
                    if (modelImporter.optimizeGameObjects != kPath.mOptimizeGO && !kPath.mIgnore[6])
                    {
                        modelImporter.optimizeGameObjects = kPath.mOptimizeGO;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更model 动画设置中OptimizeGameObjec优化为{0}", kPath.mOptimizeGO));
                    }
                    if (modelImporter.importBlendShapes != kPath.mImportBlendshap && !kPath.mIgnore[7])
                    {
                        modelImporter.importBlendShapes = kPath.mImportBlendshap;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更model ImportBlendShapes为{0}", kPath.mImportBlendshap));
                    }
                    if (modelImporter.importVisibility != kPath.mImportVisibilty && !kPath.mIgnore[8])
                    {
                        modelImporter.importVisibility = kPath.mImportVisibilty;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更model ImportBisibility为{0}", kPath.mImportVisibilty));
                    }
                    if (modelImporter.importCameras != kPath.mImportCamera && !kPath.mIgnore[9])
                    {
                        modelImporter.importCameras = kPath.mImportCamera;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更model ImportCameras为{0}", kPath.mImportCamera));
                    }
                    if (modelImporter.importLights != kPath.mImportLight && !kPath.mIgnore[10])
                    {
                        modelImporter.importLights = kPath.mImportLight;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更model ImportLights为{0}", kPath.mImportLight));
                    }
                    if (modelImporter.weldVertices != kPath.mWeldVertices && !kPath.mIgnore[12])
                    {
                        modelImporter.weldVertices = kPath.mWeldVertices;
                        kLogmsg.mChange = true;
                        kLogmsg.mChangeMSG.Add(string.Format("变更model WeldVertices为{0}", kPath.mWeldVertices));
                    }
                    #region 检查model Normal空间
                    if (kPath.mNormals && !kPath.mIgnore[11])
                    {
                        if (modelImporter.importNormals != ModelImporterNormals.Import)
                        {
                            modelImporter.importNormals = ModelImporterNormals.Import;
                            kLogmsg.mChange = true;
                            kLogmsg.mChangeMSG.Add("变更Model Normals为Import");
                        }
                    }
                    else if (!kPath.mNormals && !kPath.mIgnore[11])
                    {
                        if (modelImporter.importNormals != ModelImporterNormals.None)
                        {
                            modelImporter.importNormals = ModelImporterNormals.None;
                            kLogmsg.mChange = true;
                            kLogmsg.mChangeMSG.Add("变更Model Normals为None");
                        }
                    }
                    #endregion
                    modelImporter.SaveAndReimport();
                    AssetDatabase.ImportAsset(mPath);
                    if (kLogmsg.mChange)
                    {
                        kLog.Add(kLogmsg);
                    }
                }
                count += 1;
            }
        }
        EditorUtility.ClearProgressBar();
        StreamWriter sw;
        FileInfo fi = new FileInfo(Application.dataPath + LogPath);
        if (fi.Exists)
        {
            sw = fi.AppendText();
        }
        else
        {
            sw = fi.CreateText();
        }

        sw.WriteLine(System.DateTime.Now);
        foreach (var logMsg in kLog)
        {
            sw.WriteLine(logMsg.mPath);
            sw.WriteLine(logMsg.mName);
            foreach (var lg in logMsg.mChangeMSG)
            {
                sw.WriteLine(lg);
            }
            sw.WriteLine();
        }
        sw.Close();
        sw.Dispose();
        System.Diagnostics.Process.Start(Application.dataPath + LogPath);
    }
    private static void CreatFolder()
    {
        foreach (var kPath in mConfigs)
        {
            string FolderPath = Application.dataPath + "/" + kPath.Configuration.mPath.Replace("Assets/", "");
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }
        }
        AssetDatabase.Refresh();
    }
    private static void OpenConfigsFolder()
    {
        System.Diagnostics.Process.Start(Application.dataPath + ImportToolPath);
    }
    private static void Sort()
    {
        ImportToolStruct temp;
        for (int i = 0; i < mConfigs.Count - 1; i++)
        {
            for (int j = 0; j < mConfigs.Count - i - 1; j++)
            {
                if (mConfigs[j].Configuration.mPath.Length > mConfigs[j + 1].Configuration.mPath.Length)
                {
                    temp = mConfigs[j + 1];
                    mConfigs[j + 1] = mConfigs[j];
                    mConfigs[j] = temp;
                }
            }
        }
        for (int i = 0; i < mConfigs.Count; i++)
        {
            if (mConfigs[i].mChild != null)
            {
                for (int j = 0; j < mConfigs[i].mChild.Count - 1; j++)
                {
                    for (int f = 0; f < mConfigs[i].mChild.Count - j - 1; f++)
                    {
                        if (mConfigs[i].mChild[f].Configuration.mPath.Length < mConfigs[i].mChild[f + 1].Configuration.mPath.Length)
                        {
                            temp = mConfigs[i].mChild[f + 1];
                            mConfigs[i].mChild[f + 1] = mConfigs[i].mChild[f];
                            mConfigs[i].mChild[f] = temp;
                        }
                    }
                }
            }
        }
    }
    private static void CopyParent(ImportToolStruct Parent, ImportToolStruct Child)
    {
        if (EditorUtility.DisplayDialog("是否复制该目录设置", "只修改部分设置时请点确认", "确认", "取消"))
        {
            Child = Child.Clone(Parent);
            Child.mPriority = 2;
        }
    }
    private static ImportToolStruct ContainsFileName(ImportToolStruct Parent, string FileName)
    {
        if (Parent.mChild != null)
        {
            int idx = Parent.Configuration.mPath.Length + 1;
            foreach (var Child in Parent.mChild)
            {
                string Name = Child.Configuration.mPath.Substring(idx);
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
    public class CopyWindow : EditorWindow
    {
        private static string CurFolder = "None";
        private static ImportToolStruct CurTarget;
        private bool bDropDown = false;
        private void OnGUI()
        {
            GUILayout.Label("要复制的对象文件夹设置:");
            if (GUILayout.Button(CurFolder))
            {
                bDropDown = true;
            }
            if (bDropDown)
            {
                foreach (var Name in mConfigs)
                {
                    if (GUILayout.Button(Name.Configuration.mPath))
                    {
                        bDropDown = false;
                        CurFolder = Name.Configuration.mPath;
                        CurTarget = Name;
                    }
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("确认"))
            {
                if (CurTarget != null)
                {
                    mConfigs[iOriidx] = mConfigs[iOriidx].Clone(CurTarget);
                    CurFolder = "None";
                    this.Close();
                }

            }
            if (GUILayout.Button("返回"))
            {
                CurFolder = "None";
                this.Close();
            }
            GUILayout.EndHorizontal();
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
}
#endif
