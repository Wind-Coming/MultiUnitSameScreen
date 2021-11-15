using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System;
using Spenve;
using System.IO;

namespace Spenve
{
    public class AudioEditor : EditorWindow
    {

        int select = 1;
        AudioData audioData;
        List<SoundInfo> allBgm = new List<SoundInfo>();
        List<SoundInfo> allSfx = new List<SoundInfo>();

        Vector2 scrollPos;
        bool chooseOther = false;

        [MenuItem("Tools/声音编辑器")]
        public static void EditorTest()
        {
            AudioEditor window = EditorWindow.GetWindow<AudioEditor>();
            window.Show();
        }

        void OnEnable()
        {
            OpenXml();
        }

        void OnGUI()
        {
            DrawTab();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (select == 0)
            {
                DrawBGM();
            }
            else
            {
                DrawSFX();
            }

            CheckDragFile();

            EditorGUILayout.EndScrollView();
            DrawButton();
        }

        void DrawTab()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(select == 0, "bgm", EditorStyles.toolbarButton))
            {
                select = 0;
            }
            if (GUILayout.Toggle(select == 1, "sfx", EditorStyles.toolbarButton))
            {
                select = 1;
            }

            GUILayout.EndHorizontal();
        }

        void CheckDragFile()
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                ////改变鼠标的外表  
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }

            if (Event.current.type == EventType.DragExited)
            {
                if (chooseOther)
                {
                    chooseOther = false;
                    return;
                }
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    for (int i = 0; i < DragAndDrop.paths.Length; i++)
                    {
                        if (DragAndDrop.paths[i].EndsWith(".WAV") || DragAndDrop.paths[i].EndsWith(".aiff") || DragAndDrop.paths[i].EndsWith(".wav") || DragAndDrop.paths[i].EndsWith(".mp3") || DragAndDrop.paths[i].EndsWith(".ogg"))
                        {
                            string p = GlobalFunc.GetFileNameWithoutExtend(DragAndDrop.paths[i]);
                            if (select == 0)
                            {
                                AddBGM(p, DragAndDrop.paths[i], (AudioClip)DragAndDrop.objectReferences[i]);
                            }
                            else
                            {
                                AddSFX(p, DragAndDrop.paths[i], (AudioClip)DragAndDrop.objectReferences[i]);
                            }
                        }
                    }
                    this.Repaint();
                    Event.current.Use();
                }
            }
        }

        bool SoundInfoExist(string name, List<SoundInfo> infos)
        {
            for (int i = 0; i < infos.Count; i++)
            {
                if (infos[i].name.Equals(name))
                {
                    return true;
                }
            }
            return false;
        }

        void AddBGM(string name, string path, AudioClip clip)
        {
            if (SoundInfoExist(name, allBgm))
            {
                this.ShowNotification(new GUIContent(name + "已经存在列表中！"));
                return;
            }
            SoundInfo si = new SoundInfo();
            si.name = name;
            si.path = path;
            si.volume = 1;
            si.type = PlayType.Single;
            si.clip = clip;
            allBgm.Add(si);
        }

        void AddSFX(string name, string path, AudioClip clip)
        {
            if (SoundInfoExist(name, allSfx))
            {
                this.ShowNotification(new GUIContent(name + "已经存在列表中！"));
                return;
            }
            SoundInfo si = new SoundInfo();
            si.name = name;
            si.path = path;
            si.volume = 1;
            si.clip = clip;
            si.type = PlayType.Single;
            allSfx.Add(si);
        }

        void DrawBGM()
        {
            for (int i = 0; i < allBgm.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(allBgm[i].name);
                EditorGUILayout.LabelField("音量", GUILayout.Width(30));
                allBgm[i].volume = GUILayout.HorizontalSlider(allBgm[i].volume, 0, 1);
                GUILayout.Space(100);
                if (GUILayout.Button(allBgm[i].editorPlaying ? "Stop" : "Play"))
                {
                    if (!allBgm[i].editorPlaying)
                    {
                        AudioHelper.PlayClip(allBgm[i].clip);
                    }
                    else
                    {
                        AudioHelper.StopClip(allBgm[i].clip);
                    }
                    allBgm[i].editorPlaying = !allBgm[i].editorPlaying;

                }
                if (allBgm[i].editorPlaying && !AudioHelper.IsClipPlaying(allBgm[i].clip))
                {
                    allBgm[i].editorPlaying = false;
                }

                if (GUILayout.Button("Delete"))
                {
                    if (allBgm[i].clip != null)
                    {
                        AudioHelper.StopClip(allBgm[i].clip);
                    }
                    allBgm.Remove(allBgm[i]);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
        }

        void DrawSFX()
        {
            for (int i = 0; i < allSfx.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(allSfx[i].name, GUILayout.Width(180));
                EditorGUILayout.LabelField("音量", GUILayout.Width(30));
                allSfx[i].volume = GUILayout.HorizontalSlider(allSfx[i].volume, 0, 1, GUILayout.Width(100));
                GUILayout.Space(50);
                EditorGUILayout.LabelField("模式", GUILayout.Width(30));
                allSfx[i].type = (PlayType)EditorGUILayout.EnumPopup(allSfx[i].type, GUILayout.Width(80));
                if (allSfx[i].type == PlayType.InLoopOut)
                {
                    bool ooo = allSfx[i].loopClip == null;
                    bool xxx = allSfx[i].outClip == null;
                    allSfx[i].loopClip = EditorGUILayout.ObjectField(allSfx[i].loopClip, typeof(AudioClip), true, GUILayout.Width(100)) as AudioClip;
                    if (ooo != (allSfx[i].loopClip == null))
                    {
                        if (allSfx[i].loopClip == null)
                        {
                            allSfx[i].loopClipName = "";
                            allSfx[i].loopClipPath = "";
                        }
                        else
                        {
                            chooseOther = true;
                            allSfx[i].loopClipName = allSfx[i].loopClip.name;
                            allSfx[i].loopClipPath = AssetDatabase.GetAssetPath(allSfx[i].loopClip);
                            if (!SfxExist(allSfx[i].loopClipName))
                            {
                                AddSFX(allSfx[i].loopClipName, allSfx[i].loopClipPath, allSfx[i].loopClip);
                            }
                        }
                    }
                    allSfx[i].outClip = EditorGUILayout.ObjectField(allSfx[i].outClip, typeof(AudioClip), true, GUILayout.Width(100)) as AudioClip;
                    if (xxx != (allSfx[i].outClip == null))
                    {
                        if (allSfx[i].outClip == null)
                        {
                            allSfx[i].outClipName = "";
                            allSfx[i].outClipPath = "";
                        }
                        else
                        {
                            chooseOther = true;
                            allSfx[i].outClipName = allSfx[i].outClip.name;
                            allSfx[i].outClipPath = AssetDatabase.GetAssetPath(allSfx[i].outClip);
                            if (!SfxExist(allSfx[i].outClipName))
                            {
                                AddSFX(allSfx[i].outClipName, allSfx[i].outClipPath, allSfx[i].outClip);
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Space(210);
                }
                GUILayout.Space(100);
                if (GUILayout.Button(allSfx[i].editorPlaying ? "Stop" : "Play"))
                {
                    if (!allSfx[i].editorPlaying)
                    {
                        AudioHelper.PlayClip(allSfx[i].clip);
                    }
                    else
                    {
                        AudioHelper.StopClip(allSfx[i].clip);
                    }
                    allSfx[i].editorPlaying = !allSfx[i].editorPlaying;

                }
                if (allSfx[i].editorPlaying && !AudioHelper.IsClipPlaying(allSfx[i].clip))
                {
                    allSfx[i].editorPlaying = false;
                }

                if (GUILayout.Button("Delete"))
                {
                    if (allSfx[i].clip != null)
                    {
                        AudioHelper.StopClip(allSfx[i].clip);
                    }
                    allSfx.Remove(allSfx[i]);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
        }

        private bool SfxExist(string name)
        {
            for(int i = 0; i < allSfx.Count; i++ )
            {
                if( allSfx[i].name.Equals(name))
                {
                    return true;
                }
            }
            return false;
        }
        public void DrawButton()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save"))
            {
                SaveXml();
            }
            EditorGUILayout.EndHorizontal();
        }

        void OpenXml()
        {
            string ppp = "Assets/Res/Config/AudioConfig.asset";
            audioData = AssetDatabase.LoadAssetAtPath<AudioData>(ppp);

            allBgm = audioData.bgmList;
            allSfx = audioData.sfxList;
        }

        void SaveXml()
        {
            if(audioData == null) {
                Debug.Log("配置文件不存在，清先创建!");
                return;
            }
            EditorUtility.SetDirty(audioData);
            AssetDatabase.Refresh();
            this.ShowNotification(new GUIContent("保存成功！"));
        }

        void OnDestroy()
        {
            AudioHelper.StopAllClips();
        }
    }
}