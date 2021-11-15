using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class PipelineTreeItem : TreeViewItem
    {
        private BasicPipeline pipeline;
        public BasicPipeline Pipeline
        {
            get { return pipeline; }
        }
        public PipelineTreeItem(BasicPipeline b, int depth)
            : base(b != null?b.name.GetHashCode():0, depth, b != null?b.name:"")
        {
            pipeline = b;
            children = new List<TreeViewItem>();
        }

    }



    public abstract class BasicPipeline
    {
        public string name;

        public string tip;

        public bool enable = true;
        public bool canDisable = true;

        public bool visible = true;

        public bool configable = false;

        protected EditorWindow configWindow;


        public bool showConfig = false;

        public abstract int Process(Dictionary<string,object> objectInPipeline);


        public virtual void Refresh() { }
        protected virtual void PreConfig() { }

        public virtual void Config() 
        {
            PreConfig();

            if (configWindow != null)
                configWindow.ShowPopup();
        }


        public virtual void OnGUI()
        {
            var style = new GUIStyle( GUI.skin.GetStyle("Box"));
            style.margin.left = 20;
            style.margin.right = 20;

            EditorGUILayout.BeginVertical(style);


            DrawGUI();


            EditorGUILayout.EndVertical();

        }

        protected virtual void DrawGUI() 
        {

        }

        protected void SetProperty<T>(string key , T value) where T : IConvertible
        {
            if( value.GetType() == typeof(string) )
               EditorPrefs.SetString(key , value.ToString());
            else if(value.GetType() == typeof(bool))
                EditorPrefs.SetBool(key ,value.ToBoolean(null));
            else if(value.GetType() == typeof(int))
                EditorPrefs.SetInt(key, value.ToInt32(null));
            else if(value.GetType() == typeof(float))
                EditorPrefs.SetFloat(key, value.ToSingle(null));
            else
                Debug.LogError("Don't surpport !");
        }

        protected void GetProperty(string key, ref int value )
        {
            //value = def;
            if(EditorPrefs.HasKey(key))
                value = EditorPrefs.GetInt(key);
        }

        protected void GetProperty(string key ,ref string value) 
        {

            //value = def;
            if (EditorPrefs.HasKey(key))
                value = EditorPrefs.GetString(key);
        }

        protected void GetProperty(string key,ref float value)
        {
            //value = def;
            if (EditorPrefs.HasKey(key))
                value = EditorPrefs.GetFloat(key);
        }

        protected void GetProperty(string key, ref bool value)
        {
            //value = def;
            if (EditorPrefs.HasKey(key))
                value = EditorPrefs.GetBool(key);
        }
        

        public virtual void SetProperties(){}
        public virtual void GetProperties(){}
    }
}
