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

  [CustomEditor(typeof(MobilePickingController))]
  public class MobilePickingControllerEditor : CustomInspector {

    public override void OnInspectorGUI() {

      DrawPropertyField("m_Script");

      DrawPropertyField("snapToGrid");
      DrawPropertyField("snapUnitSize");
      DrawPropertyField("snapOffset");
      DrawPropertyField("snapAngle");
      DrawPropertyField("requireLongTapForMove");
      DrawPropertyField("OnPickableTransformSelected");
      DrawPropertyField("OnPickableTransformSelectedExtended");
      DrawPropertyField("OnPickableTransformDeselected");
      DrawPropertyField("OnPickableTransformMoveStarted");
      DrawPropertyField("OnPickableTransformMoved");
      DrawPropertyField("OnPickableTransformMoveEnded");

      DrawPropertyField("expertModeEnabled");
      SerializedProperty serializedPropertyExpertMode = serializedObject.FindProperty("expertModeEnabled");
      if(serializedPropertyExpertMode.boolValue == true) {
        DrawPropertyField("deselectPreviousColliderOnClick");
        DrawPropertyField("repeatEventSelectedOnClick");
        DrawPropertyField("useLegacyTransformMovedEventOrder");
      }

      if (GUI.changed) {
        serializedObject.ApplyModifiedProperties();
      }
    }
  }
}
