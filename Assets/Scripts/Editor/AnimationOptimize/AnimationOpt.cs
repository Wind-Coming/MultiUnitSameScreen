using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using System.IO;
using UnityEngine.Profiling;

namespace EditorTool
{
    class AnimationOpt
    {
        static Dictionary<uint, string> floatFormatDic;
        static MethodInfo getAnimationClipStats;
        static FieldInfo sizeInfo;
        static object[] param = new object[1];

        static AnimationOpt()
        {
            floatFormatDic = new Dictionary<uint, string>();
            for (uint i = 1; i < 6; i++)
            {
                floatFormatDic.Add(i, "f" + i.ToString());
            }
            Assembly asm = Assembly.GetAssembly(typeof(Editor));
            getAnimationClipStats = typeof(AnimationUtility).GetMethod("GetAnimationClipStats", BindingFlags.Static | BindingFlags.NonPublic);
            Type aniclipstats = asm.GetType("UnityEditor.AnimationClipStats");
            sizeInfo = aniclipstats.GetField("size", BindingFlags.Public | BindingFlags.Instance);
        }

        AnimationClip aniClip;
        private readonly string path;

        public string aniPath { get { return path; } }

        public long originFileSize { get; private set; }

        public int originMemorySize { get; private set; }

        public int originInspectorSize { get; private set; }

        public long optFileSize { get; private set; }

        public int optMemorySize { get; private set; }

        public int optInspectorSize { get; private set; }

        public AnimationOpt(string path, AnimationClip clip)
        {
            this.path = path;
            aniClip = clip;
            GetOriginSize();
        }

        void GetOriginSize()
        {
            originFileSize = GetFileSize();
            originMemorySize = GetMemSize();
            originInspectorSize = GetInspectorSize();
        }

        void GetOptSize()
        {
            optFileSize = GetFileSize();
            optMemorySize = GetMemSize();
            optInspectorSize = GetInspectorSize();
        }

        long GetFileSize()
        {
            FileInfo fi = new FileInfo(path);
            return fi.Length;
        }

        int GetMemSize()
        {
            return Profiler.GetRuntimeMemorySize(aniClip);
        }

        int GetInspectorSize()
        {
            param[0] = aniClip;
            var stats = getAnimationClipStats.Invoke(null, param);
            return (int)sizeInfo.GetValue(stats);
        }

        void OptmizeAnimationScaleCurve()
        {
            if (aniClip != null)
            {
                //去除scale曲线
                foreach (EditorCurveBinding theCurveBinding in AnimationUtility.GetCurveBindings(aniClip))
                {
                    string name = theCurveBinding.propertyName.ToLower();
                    if (name.Contains("scale"))
                    {
                        AnimationUtility.SetEditorCurve(aniClip, theCurveBinding, null);
                        Debug.LogFormat("关闭{0}的scale curve", aniClip.name);
                    }
                }
            }
        }

        void OptmizeAnimationFloat(uint x)
        {
            if (aniClip != null && x > 0)
            {
                //浮点数精度压缩到f3
                AnimationClipCurveData[] curves = AnimationUtility.GetAllCurves(aniClip);
                Keyframe key;
                Keyframe[] keyFrames;
                string floatFormat;
                if (floatFormatDic.TryGetValue(x, out floatFormat))
                {
                    if (curves != null && curves.Length > 0)
                    {
                        for (int ii = 0; ii < curves.Length; ++ii)
                        {
                            AnimationClipCurveData curveDate = curves[ii];
                            if (curveDate.curve == null || curveDate.curve.keys == null)
                            {
                                //Debug.LogWarning(string.Format("AnimationClipCurveData {0} don't have curve; Animation name {1} ", curveDate, animationPath));
                                continue;
                            }
                            keyFrames = curveDate.curve.keys;
                            for (int i = 0; i < keyFrames.Length; i++)
                            {
                                key = keyFrames[i];
                                key.value = float.Parse(key.value.ToString(floatFormat));
                                key.inTangent = float.Parse(key.inTangent.ToString(floatFormat));
                                key.outTangent = float.Parse(key.outTangent.ToString(floatFormat));
                                keyFrames[i] = key;
                            }
                            curveDate.curve.keys = keyFrames;
                            aniClip.SetCurve(curveDate.path, curveDate.type, curveDate.propertyName, curveDate.curve);
                        }
                    }
                }
                else
                {
                    Debug.LogErrorFormat("目前不支持{0}位浮点", x);
                }
            }
        }

        public void Optimize(bool scaleOpt, uint floatSize)
        {
            if (scaleOpt)
            {
                OptmizeAnimationScaleCurve();
            }
            OptmizeAnimationFloat(floatSize);
            GetOptSize();
        }

        public void Optimize_Scale_Float3()
        {
            Optimize(true, 3);
        }

        public void LogOrigin()
        {
            LogSize(originFileSize, originMemorySize, originInspectorSize);
        }

        public void LogOpt()
        {
            LogSize(optFileSize, optMemorySize, optInspectorSize);
        }

        public void LogDelta()
        {

        }

        void LogSize(long fileSize, int memSize, int inspectorSize)
        {
            Debug.LogFormat("{0} \nSize=[ {1} ]", path, string.Format("FSize={0} ; Mem->{1} ; inspector->{2}",
                EditorUtility.FormatBytes(fileSize), EditorUtility.FormatBytes(memSize), EditorUtility.FormatBytes(inspectorSize)));
        }
    }

    public class OptimizeAnimationClipTool
    {
        static List<AnimationOpt> animOptList = new List<AnimationOpt>();
        static List<string> errors = new List<string>();
        static int index = 0;

        [MenuItem("Tools/动画优化/裁剪浮点数去除Scale")]
        public static void Optimize()
        {
            animOptList = FindAnims();
            if (animOptList.Count > 0)
            {
                index = 0;
                errors.Clear();
                EditorApplication.update = ScanAnimationClip;
            }
        }

        private static void ScanAnimationClip()
        {
            AnimationOpt animOpt = animOptList[index];
            bool isCancel = EditorUtility.DisplayCancelableProgressBar("优化AnimationClip", animOpt.aniPath, (float)index / (float)animOptList.Count);
            animOpt.Optimize_Scale_Float3();
            index++;
            if (isCancel || index >= animOptList.Count)
            {
                EditorUtility.ClearProgressBar();
                Debug.Log(string.Format("--优化完成--    错误数量: {0}    总数量: {1}/{2}    错误信息↓:\n{3}\n----------输出完毕----------", errors.Count, index, animOptList.Count, string.Join(string.Empty, errors.ToArray())));
                Resources.UnloadUnusedAssets();
                GC.Collect();
                AssetDatabase.SaveAssets();
                EditorApplication.update = null;
                animOptList.Clear();
                cachedOpts.Clear();
                index = 0;
            }
        }

        static Dictionary<string, AnimationOpt> cachedOpts = new Dictionary<string, AnimationOpt>();

        static AnimationOpt GetNewAOpt(string path)
        {
            AnimationOpt opt = null;
            if (!cachedOpts.ContainsKey(path))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    opt = new AnimationOpt(path, clip);
                    cachedOpts[path] = opt;
                }
            }
            return opt;
        }

        static List<AnimationOpt> FindAnims()
        {
            string[] guidArr = null;
            List<string> pathList = new List<string>();
            List<AnimationOpt> assetsList = new List<AnimationOpt>();
            UnityEngine.Object[] objArr = Selection.GetFiltered(typeof(object), SelectionMode.Assets);
            if (objArr.Length > 0)
            {
                for (int i = 0; i < objArr.Length; i++)
                {
                    if (objArr[i].GetType() == typeof(AnimationClip))
                    {
                        string assetPath = AssetDatabase.GetAssetPath(objArr[i]);
                        AnimationOpt animopt = GetNewAOpt(assetPath);
                        if (animopt != null)
                            assetsList.Add(animopt);
                    }
                    else
                        pathList.Add(AssetDatabase.GetAssetPath(objArr[i]));
                }
                if (pathList.Count > 0)
                    guidArr = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(AnimationClip).ToString().Replace("UnityEngine.", "")), pathList.ToArray());
                else
                    guidArr = new string[] { };
            }
            for (int i = 0; i < guidArr.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guidArr[i]);
                AnimationOpt animopt = GetNewAOpt(assetPath);
                if (animopt != null)
                    assetsList.Add(animopt);
            }
            return assetsList;
        }
    }
}