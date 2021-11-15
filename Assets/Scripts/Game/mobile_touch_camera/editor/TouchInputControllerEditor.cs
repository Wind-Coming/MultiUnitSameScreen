// /************************************************************
// *                                                           *
// *   Mobile Touch Camera                                     *
// *                                                           *
// *   Created 2016 by BitBender Games                         *
// *                                                           *
// *   bitbendergames@gmail.com                                *
// *                                                           *
// ************************************************************/

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace BitBenderGames {

  [CustomEditor(typeof(TouchInputController))]
  public class TouchInputControllerEditor : CustomInspector {

    public override void OnInspectorGUI() {

      DrawPropertyField("m_Script");

      DrawPropertyField("expertModeEnabled");
      SerializedProperty serializedPropertyExpertMode = serializedObject.FindProperty("expertModeEnabled");
      if (serializedPropertyExpertMode.boolValue == true) {
        DrawPropertyField("clickDurationThreshold");
        DrawPropertyField("doubleclickDurationThreshold");
        DrawPropertyField("tiltMoveDotTreshold");
        DrawPropertyField("tiltHorizontalDotThreshold");
        DrawPropertyField("dragStartDistanceThresholdRelative");
        DrawPropertyField("longTapStartsDrag");
      }

      if (GUI.changed) {
        serializedObject.ApplyModifiedProperties();
      }
    }
  }
}
