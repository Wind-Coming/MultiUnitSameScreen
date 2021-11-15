using UnityEngine;
using UnityEditor;
using System.Collections;

public class STEditorStyles
{
    static STEditorStyles s_instance;
    public static STEditorStyles Instance { get { if (s_instance == null) s_instance = new STEditorStyles(); return s_instance; } }

    public GUIStyle visibleToggleStyle = new GUIStyle(EditorStyles.toggle)
    {
        normal = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleoff") },
        active = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleoff") },
        focused = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleoff") },
        hover = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleoff") },
        onNormal = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleon") },
        onActive = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleon") },
        onFocused = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleon") },
        onHover = { background = EditorGUIUtility.FindTexture("animationvisibilitytoggleon") },
    };

    public GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox)
    {
        fontStyle = FontStyle.Bold,
        fontSize = 18,
        normal = { textColor = Color.blue },
    };
    
    public GUIStyle richHelpBox = new GUIStyle("HelpBox")
    {
        richText = true,
    };

    public GUIStyle richButton = new GUIStyle("Button")
    {
        richText = true,
    };

    public GUIStyle richLabel = new GUIStyle("Label")
    {
        richText = true,
    };

    public GUIStyle boldWindow = new GUIStyle("Window")
    {
        fontStyle = FontStyle.Bold,
    };
}
