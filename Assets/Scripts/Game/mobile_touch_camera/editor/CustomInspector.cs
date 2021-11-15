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

  public class CustomInspector : Editor {

    protected const float sizeLabel = 100;
    protected const float sizeFloatInput = 70;

    protected const float subSettingsInset = 5;

    protected void DrawErrorLine(string errorMessage, Color lineColor) {
      if (string.IsNullOrEmpty(errorMessage) == false) {
        Color colorDefault = GUI.color;
        GUI.color = lineColor;
        EditorGUILayout.TextArea(errorMessage);
        GUI.color = colorDefault;
      }
    }

    protected void DrawPropertyField(string fieldName) {
      DrawPropertyField(fieldName, true, true);
    }

    protected void DrawPropertyField(string fieldName, bool isValid) {
      DrawPropertyField(fieldName, isValid, true);
    }

    protected void DrawPropertyField(string fieldName, bool isValid, bool isValidWarning, float inset) {
      if (inset > 0) {
        GUILayout.BeginHorizontal();
        GUILayout.Space(inset);
      }
      DrawPropertyField(fieldName, isValid, isValidWarning);
      if (inset > 0) {
        GUILayout.EndHorizontal();
      }
    }

    protected void DrawPropertyField(string fieldName, bool isValid, bool isValidWarning) {
      WrapWithValidationColor(() => {
        SerializedProperty serializedProperty = serializedObject.FindProperty(fieldName);
        EditorGUILayout.PropertyField(serializedProperty);
      }, isValid, isValidWarning);
    }

    protected void WrapWithValidationColor(System.Action method, bool isValid) {
      WrapWithValidationColor(method, isValid, true);
    }

    protected void WrapWithValidationColor(System.Action method, bool isValid, bool isValidWarning) {
      Color colorBackup = GUI.color;
      if (isValid == false) {
        GUI.color = Color.red;
      } else if (isValidWarning == false) {
        GUI.color = Color.yellow;
      }
      method.Invoke();
      GUI.color = colorBackup;
    }

  }
}
