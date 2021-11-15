using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using UnityEngine;


namespace AssetBundleBrowser
{
    [System.Serializable]
    public class BuildPipelineTab
    {
        [SerializeField]
        private Vector2 m_ScrollPosition;

        public BuildPipelineTab()
        {
            
        }

        public void OnDisable()
        {

        }
        public void OnEnable(EditorWindow parent)
        {
            BuildPipelineManager.LoadPipeline();
            BuildPipelineManager.Refresh();

            GetProperties();
        }

        public void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            centeredStyle.alignment = TextAnchor.UpperCenter;
            GUILayout.Label(new GUIContent("Build Steps"), centeredStyle);
            EditorGUILayout.Space();


            GUILayout.Label("--------------------------------------------------------------------------------------------------------------------------------------", centeredStyle);

            GUILayout.BeginVertical();

            int step = 0;
            var leftStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            leftStyle.alignment = TextAnchor.UpperLeft;
            leftStyle.margin.left = 12;

            var foldStyle = new GUIStyle(GUI.skin.GetStyle("Foldout"));
            foldStyle.alignment = TextAnchor.UpperLeft;
            foldStyle.margin.left = 0;

            foreach( BasicPipeline bp in BuildPipelineManager.pipelines )
            {
                using (new EditorGUI.DisabledGroupScope(!bp.visible))
                {
                    EditorGUILayout.BeginHorizontal();
                    bool enable = EditorGUILayout.Toggle(bp.enable,GUILayout.Width(10));
                    if (bp.canDisable)
                        bp.enable = enable;

                    if (bp.configable)
                    {
                        bp.showConfig = EditorGUILayout.Foldout(bp.showConfig, new GUIContent("" + (++step) + ": " + bp.tip), foldStyle);                        
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent("Step " + (++step) + ": " + bp.tip), leftStyle);
                    }
                    EditorGUILayout.EndHorizontal();

                    //show config
                    if (bp.showConfig)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            bp.OnGUI();
                        }
                    }
                }

                GUILayout.Label("--------------------------------------------------------------------------------------------------------------------------------------", centeredStyle);
            }

            // build.
            EditorGUILayout.Space();

            

            if (GUILayout.Button("Build"))
            {
                EditorApplication.delayCall += ExecuteBuild;
            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void ExecuteBuild()
        {

            SetProperties();

            BuildPipelineManager.StartBuild();


            while( BuildPipelineManager.inProcess )
            {
                BuildPipelineManager.BuildStep();

            }  
        }



        public void ForceReloadData()
        {
            BuildPipelineManager.LoadPipeline(true);

            BuildPipelineManager.Refresh();
        }

        public void SetProperties()
        {
            foreach (BasicPipeline bp in BuildPipelineManager.pipelines)
            {
                EditorPrefs.SetBool(bp.name, bp.enable);
                bp.SetProperties();
            }
        }

        public void GetProperties()
        {
            foreach (BasicPipeline  bp in BuildPipelineManager.pipelines)
            {
              bool value = EditorPrefs.GetBool(bp.name );
                bp.enable = bp.canDisable ? value : true; ;
              bp.GetProperties();
            }
        }
    }

}