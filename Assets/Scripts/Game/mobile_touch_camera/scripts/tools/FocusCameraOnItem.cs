// /************************************************************
// *                                                           *
// *   Mobile Touch Camera                                     *
// *                                                           *
// *   Created 2015 by BitBender Games                         *
// *                                                           *
// *   bitbendergames@gmail.com                                *
// *                                                           *
// ************************************************************/

using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace BitBenderGames {

  /// <summary>
  /// A little helper script that allows to focus the camera on a transform either
  /// via code, or by wiring it up with one of the many events of the mobile touch camera
  /// or mobile picking controller.
  /// </summary>
  [RequireComponent(typeof(MobileTouchCamera))]
  public class FocusCameraOnItem : MonoBehaviourWrapped {

    [SerializeField]
    private float transitionDuration = 0.5f;

    private MobileTouchCamera MobileTouchCamera { get; set; }

    private Vector3 posTransitionStart;
    private Vector3 posTransitionEnd;
    private float timeTransitionStart;
    private bool isTransitionStarted;

    public void Awake() {
      MobileTouchCamera = GetComponent<MobileTouchCamera>();
      isTransitionStarted = false;
    }

    public void LateUpdate() {

      if (MobileTouchCamera.IsDragging || MobileTouchCamera.IsPinching) {
        timeTransitionStart = Time.time - transitionDuration;
      }

      if (isTransitionStarted == true) {
        if (Time.time < timeTransitionStart + transitionDuration) {
          UpdatePosition();
        } else {
          SetPosition(posTransitionEnd);
          isTransitionStarted = false;
        }
      }
    }

    private void UpdatePosition() {
      float progress = (Time.time - timeTransitionStart) / transitionDuration;
      Vector3 positionNew = Vector3.Lerp(posTransitionStart, posTransitionEnd, Mathf.Sin(-Mathf.PI * 0.5f + progress * Mathf.PI) * 0.5f + 0.5f);
      SetPosition(positionNew);
    }

    public void OnPickItem(RaycastHit hitInfo) {
      FocusCameraOnTransform(hitInfo.transform);
    }

    public void OnPickItem2D(RaycastHit2D hitInfo2D) {
      FocusCameraOnTransform(hitInfo2D.transform);
    }

    public void OnPickableTransformSelected(Transform pickableTransform) {
      FocusCameraOnTransform(pickableTransform);
    }

    public void FocusCameraOnTransform(Transform targetTransform) {
      if (targetTransform == null) {
        return;
      }
      FocusCameraOnTarget(targetTransform.position);
    }

    public void FocusCameraOnTransform(Vector3 targetPosition) {
      FocusCameraOnTarget(targetPosition);
    }

    public void FocusCameraOnTarget(Vector3 targetPosition) {
      if (Mathf.Approximately(transitionDuration, 0)) {
        SetPosition(targetPosition);
        return;
      }
      timeTransitionStart = Time.time;
      isTransitionStarted = true;
      posTransitionStart = Transform.position;

      Vector3 intersectionScreenCenter = MobileTouchCamera.GetIntersectionPoint(MobileTouchCamera.Cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0)));
      Vector3 focusVector = targetPosition - intersectionScreenCenter;
      posTransitionEnd = MobileTouchCamera.GetClampToBoundaries(posTransitionStart + focusVector, true);
    }

    private void SetPosition(Vector3 newPosition) {
      Vector3 camPos = Transform.position;
      switch (MobileTouchCamera.CameraAxes) {
        case CameraPlaneAxes.XY_2D_SIDESCROLL:
          camPos.x = newPosition.x;
          camPos.y = newPosition.y;
          break;
        case CameraPlaneAxes.XZ_TOP_DOWN:
          camPos.x = newPosition.x;
          camPos.z = newPosition.z;
          break;
      }
      Transform.position = camPos;
    }
  }
}
