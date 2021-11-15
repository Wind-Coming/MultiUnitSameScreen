using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;


class UpdatePrefabPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, 
                                        string[] movedAssets, string[] movedFromAssetPaths)
    {
    }

    private void SetTextureFormat(ref TextureImporter texImporter, bool bUse32Bit, bool bHasAlpha = true, int iFormat = 0, string kAssetPath = "")
    {
        bool bAlpha = bHasAlpha && texImporter.DoesSourceTextureHaveAlpha();
        bool bNormal = false;
        bool bNoCompress = false;
        TextureImporterPlatformSettings kSetting = texImporter.GetPlatformTextureSettings("Android");

        if (kSetting != null)
        {
            kSetting.overridden = true;
            if(iFormat > 0)
            {
                kSetting.format = (TextureImporterFormat)iFormat;
            }
            else
            {
                if (bAlpha)
                {
                    kSetting.format = TextureImporterFormat.ETC2_RGBA8;
                }
                else
                {
                    if (bNormal || bNoCompress)
                    {
                        kSetting.format = TextureImporterFormat.RGB24;
                    }
                    else
                    {
                        kSetting.format = TextureImporterFormat.ETC_RGB4;
                    }
                }
            }
            texImporter.SetPlatformTextureSettings(kSetting);
        }
        /*
        kSetting = texImporter.GetPlatformTextureSettings("iPhone");

        if (kSetting != null)
        {
            kSetting.overridden = true;
            if (bAlpha)
            {
                if (bUse32Bit)
                {
                    kSetting.format = TextureImporterFormat.RGBA32;
                }
                else if (texImporter.npotScale == TextureImporterNPOTScale.None)
                {
                    if (assetPath.Contains("UI/Atlas"))
                    {
                        kSetting.format = TextureImporterFormat.PVRTC_RGBA4;
                        kSetting.compressionQuality = 100;
                    }
                    else
                    {
                        kSetting.format = TextureImporterFormat.RGBA16;
                    }
                }
                else
                {

                    {
                        kSetting.format = TextureImporterFormat.PVRTC_RGBA4;
                    }
                }
            }
            else
            {
                if (bNormal || bNoCompress)
                {
                    kSetting.format = TextureImporterFormat.RGB24;
                }
                else
                {
                    bool bUseRGB16 = kAssetPath.Contains("_S1") ||
                                     (kAssetPath.Contains("头") && kAssetPath.Contains("_S")) ||
                                     kAssetPath.Contains("RGB16");
                    kSetting.format = bUseRGB16 ? TextureImporterFormat.RGB16 : TextureImporterFormat.PVRTC_RGB4;
                }
            }

            if (kSetting.maxTextureSize > IosMaxSize)
            {
                kSetting.maxTextureSize = IosMaxSize;
            }
            texImporter.SetPlatformTextureSettings(kSetting);
        }
        if (kSetting != null)
        {
            kSetting = texImporter.GetPlatformTextureSettings("Standalone");

            kSetting.overridden = true;
            if (bAlpha)
            {
                kSetting.format = bUse32Bit ? TextureImporterFormat.RGBA32 : TextureImporterFormat.DXT5;
            }
            else
            {
                kSetting.format = TextureImporterFormat.DXT1;

            }

            if (kSetting.maxTextureSize > AndroidMaxSize)
            {
                kSetting.maxTextureSize = AndroidMaxSize;
            }
            texImporter.SetPlatformTextureSettings(kSetting);
        }
        
        //textures no need compression----------------
        bool trueColor = false;
        foreach (string textureName in TruecolorTextruesName)
        {
            if (assetPath.Contains(textureName) == true)
            {
                trueColor = true;
                break;
            }
        }
        if (assetPath.Contains("msz_") == true)
        {
            trueColor = true;
        }
        if (trueColor == true)
        {
            texImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
        }
        */
    }

//    public void OnPreprocessTexture()
//    {
//#if EDITOR_SHADOW
//        return;
//#endif
//        TextureImporter texImporter = assetImporter as TextureImporter;

//        if (null == texImporter)
//        {
//            return;
//        }
//        texImporter.isReadable = false;

//        if (assetPath.Contains("Bundle/Atlas") || assetPath.Contains("UI/Texture") )
//        {
//            if (assetPath.Contains("Bundle/Atlas") || assetPath.Contains("RGBA32") || assetPath.Contains("Dither"))
//            {
//                texImporter.npotScale = TextureImporterNPOTScale.None;
//            }
//            texImporter.alphaIsTransparency = true;
//            texImporter.mipmapEnabled = false;
//            texImporter.filterMode = FilterMode.Bilinear;
            
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"));
//        }
//        else if (assetPath.Contains("Bundle/Art/Scene/World"))
//        {
//            texImporter.alphaIsTransparency = true;
//            texImporter.mipmapEnabled = false;
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"));
//        }
//        //else if (assetPath.Contains("Bundle/Art/Scene/City"))
//        //{
//        //    texImporter.alphaIsTransparency = true;
//        //    texImporter.mipmapEnabled = true;
//        //    SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"), (int)TextureImporterFormat.RGB24);
//        //}
//        else if(assetPath.Contains("Bundle/Textures/UI"))//UI Textures
//        {
//            texImporter.alphaIsTransparency = true;
//            texImporter.mipmapEnabled       = false;
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"));
//        }
//        else if(assetPath.Contains("Bundle/Art/Unit/Character/Bake2D"))//序列帧 -RGB Compressed ASTC 8x8 block
//        {
//            texImporter.alphaIsTransparency = true;
//            texImporter.mipmapEnabled = true;
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"),(int)TextureImporterFormat.ETC2_RGBA8);//TextureImporterFormat.ASTC_RGBA_8x8
//        }
//        else if(assetPath.Contains("Bundle/Art/Unit/Character/Materials"))//角色贴图
//        {
//            bool bHero = assetPath.Contains("Bundle/Art/Unit/Character/Materials/Hero");
//            bool bUnit = assetPath.Contains("Bundle/Art/Unit/Character/Materials/Arms");
//            texImporter.alphaIsTransparency = false;
//            texImporter.mipmapEnabled = false;
//            int iFormat = 0;
//            if (assetPath.Contains("_D"))//颜色贴图带Alpha通道
//            {
//                texImporter.alphaIsTransparency = true;
//                texImporter.mipmapEnabled = true;
//                iFormat = (int)TextureImporterFormat.ETC2_RGBA8;
//            }
//            else if(assetPath.Contains("_N"))//法线贴图
//            {
//                texImporter.mipmapEnabled = false;
//                iFormat = (int)TextureImporterFormat.ETC_RGB4;
//            }
//            else if (assetPath.Contains("_F"))//功能贴图
//            {
//                texImporter.mipmapEnabled = false;
//                iFormat = (int)TextureImporterFormat.ETC2_RGBA8;
//            }
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"), iFormat);
//        }
//        else if(assetPath.Contains("Bundle/Art/Scene/City/Materials"))//主城场景贴图路径
//        {
//            texImporter.alphaIsTransparency = false;
//            texImporter.mipmapEnabled = true;
//            int iFormat = (int)TextureImporterFormat.RGB24;
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"), iFormat);
//        }
//        else if (assetPath.Contains("Bundle/Art/Scene/World/Materials"))//野外地图贴图路径
//        {
//            texImporter.alphaIsTransparency = false;
//            texImporter.mipmapEnabled = true;
//            int iFormat = (int)TextureImporterFormat.ETC_RGB4;
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"), iFormat);
//        }
//        else if (assetPath.Contains("Bundle/Art/Unit/Building"))//场景功能建筑贴图路径
//        {
//            texImporter.alphaIsTransparency = false;
//            texImporter.mipmapEnabled = true;
//            int iFormat = (int)TextureImporterFormat.ETC_RGB4;
//            SetTextureFormat(ref texImporter, assetPath.Contains("RGBA32"), !assetPath.Contains("NoAlpha"), iFormat);
//        }
//    }

    void OnPreprocessModel()
    {
        //ModelImporter kImporter = assetImporter as ModelImporter;

        //if (assetPath.Contains("NoCompress"))
        //{
        //    kImporter.animationCompression = ModelImporterAnimationCompression.Off;
        //}
        //else if (assetPath.Contains("KeyframeReduction"))
        //{
        //    kImporter.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        //}
        //else
        //{
        //    kImporter.animationCompression = ModelImporterAnimationCompression.Optimal;
        //}

        //kImporter.isReadable = false;
        //kImporter.meshCompression = ModelImporterMeshCompression.Medium;
        //kImporter.optimizeGameObjects = true;
        /*
        if (assetPath.Contains("FxRes/Model/"))
        {
            kImporter.isReadable = assetPath.Contains("NeedWriteRead");

            if (assetPath.Contains("OffCompress"))
            {
                kImporter.meshCompression = ModelImporterMeshCompression.Off;
            }
            else if (assetPath.Contains("NoCompress"))
            {
                kImporter.meshCompression = ModelImporterMeshCompression.Low;
            }
            else
            {
                kImporter.meshCompression = ModelImporterMeshCompression.High;
            }
        }
        else
        {

            if (assetPath.Contains("_Terrain"))
            {
                kImporter.meshCompression = ModelImporterMeshCompression.Low;
            }
            else if (assetPath.Contains("NoCompress"))
            {
                kImporter.meshCompression = ModelImporterMeshCompression.Medium;
            }
            else
            {
                kImporter.meshCompression = ModelImporterMeshCompression.High;
            }
        }
        */

        ModelImporter kImporter = assetImporter as ModelImporter;
        if (null == kImporter)
            return;

        kImporter.importMaterials = false;
    }

    private void OnPostprocessModel(GameObject model)
    {
        if (null == model) return;

        Renderer[] renders = model.GetComponentsInChildren<Renderer>();
        if (null == renders) return;
        foreach (Renderer render in renders) {
            render.sharedMaterials = new Material[render.sharedMaterials.Length];
        }
    }

    [MenuItem("Tools/导入设置工具/Reimport all FBX")]
    public static void ReimportAllFBX()
    {
        var files = AssetDatabase.GetAllAssetPaths();
        foreach (var vv in files) {
            var vvLower = vv.ToLower();
            if (vvLower.EndsWith("fbx")) {
                AssetDatabase.ImportAsset(vv, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            }
        }
    }

    void OnPreprocessAudio()
    {
        AudioImporter kImporter = assetImporter as AudioImporter;
        if (null == kImporter)
            return;
        kImporter.forceToMono = false;
        kImporter.loadInBackground = false;
        kImporter.preloadAudioData = false;

        var settings = kImporter.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.CompressedInMemory;                
        kImporter.SetOverrideSampleSettings("Standalone", settings);
        kImporter.SetOverrideSampleSettings("iOS", settings);
        kImporter.SetOverrideSampleSettings("Android", settings);


    }
}