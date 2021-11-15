// /************************************************************
// *                                                           *
// *   Mobile Touch Camera                                     *
// *                                                           *
// *   Created 2015 by BitBender Games                         *
// *                                                           *
// *   bitbendergames@gmail.com                                *
// *                                                           *
// ************************************************************/

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace BitBenderGames {

  [CustomEditor(typeof(MobileTouchCamera))]
  public class MobileTouchCameraEditor : CustomInspector {

    public void OnSceneGUI() {

      MobileTouchCamera mobileTouchCamera = (MobileTouchCamera)target;

      if(Event.current.rawType == EventType.MouseUp) {
        CheckSwapBoundary(mobileTouchCamera);
      }

      Vector2 boundaryMin = mobileTouchCamera.BoundaryMin;
      Vector2 boundaryMax = mobileTouchCamera.BoundaryMax;

      float offset = mobileTouchCamera.GroundLevelOffset;
      Vector3 pBottomLeft = mobileTouchCamera.UnprojectVector2(new Vector2(boundaryMin.x, boundaryMin.y), offset);
      Vector3 pBottomRight = mobileTouchCamera.UnprojectVector2(new Vector2(boundaryMax.x, boundaryMin.y), offset);
      Vector3 pTopLeft = mobileTouchCamera.UnprojectVector2(new Vector2(boundaryMin.x, boundaryMax.y), offset);
      Vector3 pTopRight = mobileTouchCamera.UnprojectVector2(new Vector2(boundaryMax.x, boundaryMax.y), offset);

      Handles.color = new Color(0, .4f, 1f, 1f);
      float handleSize = HandleUtility.GetHandleSize(mobileTouchCamera.Transform.position) * 0.1f;

      #region min/max handles
      pBottomLeft = Handles.FreeMoveHandle(pBottomLeft, Quaternion.identity, handleSize, Vector3.one, Handles.SphereCap);
      pTopRight = Handles.FreeMoveHandle(pTopRight, Quaternion.identity, handleSize, Vector3.one, Handles.SphereCap);
      boundaryMin = mobileTouchCamera.ProjectVector3(pBottomLeft);
      boundaryMax = mobileTouchCamera.ProjectVector3(pTopRight);
      #endregion

      #region min/max handles that need to be remapped
      Vector3 pBottomRightNew = Handles.FreeMoveHandle(pBottomRight, Quaternion.identity, handleSize, Vector3.one, Handles.SphereCap);
      Vector3 pTopLeftNew = Handles.FreeMoveHandle(pTopLeft, Quaternion.identity, handleSize, Vector3.one, Handles.SphereCap);

      if(Vector3.Distance(pBottomRight, pBottomRightNew) > 0) {
        Vector2 pBottomRight2d = mobileTouchCamera.ProjectVector3(pBottomRightNew);
        boundaryMin.y = pBottomRight2d.y;
        boundaryMax.x = pBottomRight2d.x;
      }
      if(Vector3.Distance(pTopLeft, pTopLeftNew) > 0) {
        Vector2 pTopLeftNew2d = mobileTouchCamera.ProjectVector3(pTopLeftNew);
        boundaryMin.x = pTopLeftNew2d.x;
        boundaryMax.y = pTopLeftNew2d.y;
      }
      #endregion

      #region one way handles //Disabled because it doesn't draw correctly in perspective scene mode.
      Handles.color = new Color(1, 1, 1, 1);
      handleSize = HandleUtility.GetHandleSize(mobileTouchCamera.Transform.position) * 0.05f;
      boundaryMax.x = DrawOneWayHandle(mobileTouchCamera, handleSize, new Vector2(boundaryMax.x, 0.5f * (boundaryMax.y + boundaryMin.y)), offset).x;
      boundaryMax.y = DrawOneWayHandle(mobileTouchCamera, handleSize, new Vector2(0.5f * (boundaryMax.x + boundaryMin.x), boundaryMax.y), offset).y;
      boundaryMin.x = DrawOneWayHandle(mobileTouchCamera, handleSize, new Vector2(boundaryMin.x, 0.5f * (boundaryMax.y + boundaryMin.y)), offset).x;
      boundaryMin.y = DrawOneWayHandle(mobileTouchCamera, handleSize, new Vector2(0.5f * (boundaryMax.x + boundaryMin.x), boundaryMin.y), offset).y;
      #endregion

      if(Vector2.Distance(mobileTouchCamera.BoundaryMin, boundaryMin) > float.Epsilon || Vector2.Distance(mobileTouchCamera.BoundaryMax, boundaryMax) > float.Epsilon) {
        Undo.RecordObject(target, "Mobile Touch Camera Boundary Modification");
        mobileTouchCamera.BoundaryMin = boundaryMin;
        mobileTouchCamera.BoundaryMax = boundaryMax;
        EditorUtility.SetDirty(mobileTouchCamera);
      }
    }

    private Vector3 DrawOneWayHandle(MobileTouchCamera mobileTouchCamera, float handleSize, Vector2 pRelative, float offset) {
      Vector3 point = mobileTouchCamera.UnprojectVector2(pRelative, offset);
      Vector3 pointNew = Handles.FreeMoveHandle(point, Quaternion.identity, handleSize, Vector3.one, Handles.DotCap);
      return mobileTouchCamera.ProjectVector3(pointNew);
    }

    /// <summary>
    /// Method to swap the boundary min/max values in case they aren't right.
    /// </summary>
    private void CheckSwapBoundary(MobileTouchCamera mobileTouchCamera) {

      Vector2 boundaryMin = mobileTouchCamera.BoundaryMin;
      Vector2 boundaryMax = mobileTouchCamera.BoundaryMax;

      //Automatically swap min with max when necessary.
      bool autoSwap = false;
      if(boundaryMax.x < boundaryMin.x) {
        Undo.RecordObject(target, "Mobile Touch Camera Boundary Auto Swap");
        Swap(ref boundaryMax.x, ref boundaryMin.x);
        autoSwap = true;
      }
      if(boundaryMax.y < boundaryMin.y) {
        Undo.RecordObject(target, "Mobile Touch Camera Boundary Auto Swap");
        Swap(ref boundaryMax.y, ref boundaryMin.y);
        autoSwap = true;
      }

      if(autoSwap == true) {
        EditorUtility.SetDirty(mobileTouchCamera);
      }

      mobileTouchCamera.BoundaryMin = boundaryMin;
      mobileTouchCamera.BoundaryMax = boundaryMax;
    }

    /// <summary>
    /// Helper method to swap 2 float variables.
    /// </summary>
    private void Swap(ref float a, ref float b) {
      float cache = a;
      a = b;
      b = cache;
    }

    public override void OnInspectorGUI() {

      MobileTouchCamera mobileTouchCamera = (MobileTouchCamera)target;

      DrawPropertyField("m_Script");

      string camAxesError = mobileTouchCamera.CheckCameraAxesErrors();
      bool isAxesValid = string.IsNullOrEmpty(camAxesError);
      DrawPropertyField("cameraAxes", isAxesValid);
      DrawErrorLine(camAxesError, Color.red);
      GUI.enabled = mobileTouchCamera.GetComponent<Camera>().orthographic == false;
      DrawPropertyField("perspectiveZoomMode");
      GUI.enabled = true;
      bool isZoomValid = mobileTouchCamera.CamZoomMax >= mobileTouchCamera.CamZoomMin;
      DrawPropertyField("camZoomMin", isZoomValid);
      DrawPropertyField("camZoomMax", isZoomValid);
      if (isZoomValid == false) {
        DrawErrorLine("Cam Zoom Max must be bigger than Cam Zoom Min", Color.red);
      }
      DrawPropertyField("camOverzoomMargin");
      DrawPropertyField("camOverdragMargin");

      #region boundary
      SerializedProperty serializedPropertyBoundaryMin = serializedObject.FindProperty("boundaryMin");
      Vector2 vector2BoundaryMin = serializedPropertyBoundaryMin.vector2Value;

      SerializedProperty serializedPropertyBoundaryMax = serializedObject.FindProperty("boundaryMax");
      Vector2 vector2BoundaryMax = serializedPropertyBoundaryMax.vector2Value;

      EditorGUILayout.LabelField(new GUIContent("Boundary", "These values define the scrolling borders for the camera. The camera will not scroll further than defined here. The boundary is drawn as yellow rectangular gizmo in the scene-view when the camera is selected."), EditorStyles.boldLabel);

      DrawPropertyField("useBoundaryOrViewCenter");

      EditorGUILayout.BeginHorizontal();
      GUILayout.Label("Top", GUILayout.Width(sizeLabel));
      GUILayout.FlexibleSpace();
      GUILayout.FlexibleSpace();
      bool isBoundaryYValid = vector2BoundaryMax.y > vector2BoundaryMin.y;
      bool isBoundaryXValid = vector2BoundaryMax.x > vector2BoundaryMin.x;
      WrapWithValidationColor(() => {
        vector2BoundaryMax.y = EditorGUILayout.FloatField(vector2BoundaryMax.y, GUILayout.Width(sizeFloatInput));
      }, isBoundaryYValid);
      GUILayout.FlexibleSpace();
      EditorGUILayout.EndHorizontal();

      WrapWithValidationColor(() => {
        Draw2FloatFields("Left/Right", ref vector2BoundaryMin.x, ref vector2BoundaryMax.x);
      }, isBoundaryXValid);

      EditorGUILayout.BeginHorizontal();
      GUILayout.Label("Bottom", GUILayout.Width(sizeLabel));
      GUILayout.FlexibleSpace();
      GUILayout.FlexibleSpace();
      WrapWithValidationColor(() => {
        vector2BoundaryMin.y = EditorGUILayout.FloatField(vector2BoundaryMin.y, GUILayout.Width(sizeFloatInput));
      }, isBoundaryYValid);
      GUILayout.FlexibleSpace();
      EditorGUILayout.EndHorizontal();

      if(isBoundaryYValid == false) {
        DrawErrorLine("The value for Top needs to be bigger\nthan the value for Bottom.", Color.red);
      }
      if(isBoundaryXValid == false) {
        DrawErrorLine("The value for Right needs to be bigger\nthan the value for Left.", Color.red);
      }

      serializedPropertyBoundaryMin.vector2Value = vector2BoundaryMin;
      serializedPropertyBoundaryMax.vector2Value = vector2BoundaryMax;
      #endregion

      DrawPropertyField("camFollowFactor");

      #region auto scroll damp
      AutoScrollDampMode selectedDampMode = (AutoScrollDampMode)serializedObject.FindProperty("autoScrollDampMode").enumValueIndex;
      if (selectedDampMode == AutoScrollDampMode.DEFAULT && mobileTouchCamera.AutoScrollDamp != 300) {
        serializedObject.FindProperty("autoScrollDampMode").enumValueIndex = (int)AutoScrollDampMode.CUSTOM; //Set selected mode to custom in case it was set to default but the damp wasn't the default value. This may happen for users that have changed the damp and upgraded from an older version of the asset.
        selectedDampMode = AutoScrollDampMode.CUSTOM;
      }
      DrawPropertyField("autoScrollDampMode");
      if (selectedDampMode == AutoScrollDampMode.CUSTOM) {
        DrawPropertyField("autoScrollDamp", true, true, subSettingsInset);
        DrawPropertyField("autoScrollDampCurve", true, true, subSettingsInset);
      }
      #endregion

      DrawPropertyField("groundLevelOffset");
      DrawPropertyField("enableRotation");
      DrawPropertyField("enableTilt");
      const float minTiltErrorAngle = 10;
      const float minTiltWarningAngle = 40;
      const float maxTiltErrorAngle = 90;
      if (mobileTouchCamera.EnableTilt == true) {
        DrawPropertyField("tiltAngleMin", mobileTouchCamera.TiltAngleMin >= minTiltErrorAngle, mobileTouchCamera.TiltAngleMin >= minTiltWarningAngle, subSettingsInset);
        if (mobileTouchCamera.TiltAngleMin < minTiltErrorAngle) {
          DrawErrorLine("Error: The minimum tilt angle\nmust not be lower than " + minTiltErrorAngle + ".\nOtherwise the camera computation\nis guaranteed to become instable.", Color.red);
        } else if (mobileTouchCamera.TiltAngleMin < minTiltWarningAngle) {
          DrawErrorLine("Warning: The minimum tilt angle\nshould not be lower than " + minTiltWarningAngle + ".\nOtherwise the camera computations\nmay become instable.", Color.yellow);
        }
        DrawPropertyField("tiltAngleMax", mobileTouchCamera.TiltAngleMax <= maxTiltErrorAngle, true, subSettingsInset);
        if (mobileTouchCamera.TiltAngleMax > maxTiltErrorAngle) {
          DrawErrorLine("The maximum tilt angle\nmust not be higher than " + maxTiltErrorAngle + ".\nOtherwise the camera computation\nmay become instable.", Color.red);
        }
        if (mobileTouchCamera.TiltAngleMax < mobileTouchCamera.TiltAngleMin) {
          DrawErrorLine("Tilt Angle Max must be bigger than Tilt Angle Min", Color.red);
        }
      }
      DrawPropertyField("enableZoomTilt");
      if (mobileTouchCamera.EnableZoomTilt == true) {
        DrawPropertyField("zoomTiltAngleMin", mobileTouchCamera.ZoomTiltAngleMin >= minTiltErrorAngle, mobileTouchCamera.ZoomTiltAngleMin >= minTiltWarningAngle, subSettingsInset);
        DrawPropertyField("zoomTiltAngleMax", mobileTouchCamera.ZoomTiltAngleMax <= maxTiltErrorAngle, true, subSettingsInset);
      }

      DrawPropertyField("OnPickItem");
      DrawPropertyField("OnPickItem2D");
      DrawPropertyField("OnPickItemDoubleClick");
      DrawPropertyField("OnPickItem2DDoubleClick");

      DrawPropertyField("expertModeEnabled");
      SerializedProperty serializedPropertyExpertMode = serializedObject.FindProperty("expertModeEnabled");
      if(serializedPropertyExpertMode.boolValue == true) {
        DrawPropertyField("zoomBackSpringFactor");
        DrawPropertyField("dragBackSpringFactor");
        DrawPropertyField("autoScrollVelocityMax");
        DrawPropertyField("dampFactorTimeMultiplier");
        DrawPropertyField("isPinchModeExclusive");
        DrawPropertyField("customZoomSensitivity");
        DrawPropertyField("terrainCollider");
        DrawPropertyField("cameraTransform");

        DrawPropertyField("rotationDetectionDeltaThreshold");
        DrawPropertyField("rotationMinPinchDistance");
        DrawPropertyField("rotationLockThreshold");

        DrawPropertyField("pinchModeDetectionMoveTreshold");
        DrawPropertyField("pinchTiltModeThreshold");
        DrawPropertyField("pinchTiltSpeed");
      }

      if (GUI.changed) {

        serializedObject.ApplyModifiedProperties();

        //Detect modified properties.
        AutoScrollDampMode dampModeAfterApply = (AutoScrollDampMode)serializedObject.FindProperty("autoScrollDampMode").enumValueIndex;
        if (selectedDampMode != dampModeAfterApply) {
          OnScrollDampModeChanged(dampModeAfterApply);
        }
      }
    }

    private void OnScrollDampModeChanged(AutoScrollDampMode dampMode) {

      SerializedProperty serializedScrollDamp = serializedObject.FindProperty("autoScrollDamp");
      SerializedProperty serializedScrollDampCurve = serializedObject.FindProperty("autoScrollDampCurve");
      switch (dampMode) {
        case AutoScrollDampMode.DEFAULT:
          serializedScrollDamp.floatValue = 300;
          serializedScrollDampCurve.animationCurveValue = new AnimationCurve(new Keyframe(0, 1, 0, 0), new Keyframe(0.7f, 0.9f, -0.5f, -0.5f), new Keyframe(1, 0.01f, -0.85f, -0.85f));
          break;
        case AutoScrollDampMode.SLOW_FADE_OUT:
          serializedScrollDamp.floatValue = 150;
          serializedScrollDampCurve.animationCurveValue = new AnimationCurve(new Keyframe(0, 1, -1, -1), new Keyframe(1, 0.01f, -1, -1));
          break;
      }
      if (dampMode != AutoScrollDampMode.CUSTOM) {
        serializedObject.ApplyModifiedProperties();
      }
    }

    private void Draw2FloatFields(string caption, ref float valueA, ref float valueB) {

      EditorGUILayout.BeginHorizontal();
      GUILayout.Label(caption, GUILayout.Width(sizeLabel));
      GUILayout.FlexibleSpace();
      valueA = EditorGUILayout.FloatField(valueA, GUILayout.Width(sizeFloatInput));
      GUILayout.FlexibleSpace();
      valueB = EditorGUILayout.FloatField(valueB, GUILayout.Width(sizeFloatInput));
      EditorGUILayout.EndHorizontal();

    }
  }
}
