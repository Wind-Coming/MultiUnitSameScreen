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
using System.Collections.Generic;
using UnityEngine.UI;

namespace BitBenderGames {

  public class DemoController : MonoBehaviour {

    [SerializeField]
    private Text textInfo;

    [SerializeField]
    private Text textDetail;

    private TouchInputController touchInputController;

    private MobileTouchCamera mobileTouchCamera;

    private MobilePickingController mobilePickingController;

    private Camera cam;

    private Coroutine coroutineHideInfoText;

    private Transform selectedPickableTransform;

    private Dictionary<Renderer, List<Color>> originalItemColorCache = new Dictionary<Renderer, List<Color>>();

    public float introTextOnScreenTime = 5;

    public void Awake() {

      Application.targetFrameRate = 60;

      cam = FindObjectOfType<Camera>();
      mobileTouchCamera = cam.GetComponent<MobileTouchCamera>();
      touchInputController = cam.GetComponent<TouchInputController>();
      mobilePickingController = cam.GetComponent<MobilePickingController>();

      #region detail callbacks
      touchInputController.OnInputClick += OnInputClick;
      touchInputController.OnDragStart += OnDragStart;
      touchInputController.OnDragStop += OnDragStop;
      touchInputController.OnDragUpdate += OnDragUpdate;
      touchInputController.OnFingerDown += OnFingerDown;
      touchInputController.OnPinchStart += OnPinchStart;
      touchInputController.OnPinchStop += OnPinchStop;
      touchInputController.OnPinchUpdateExtended += new TouchInputController.PinchUpdateExtendedDelegate(OnPinchUpdate);
      #endregion

      ShowInfoText("Mobile Touch Camera Demo\nSwipe: Scroll\nPinch: Zoom\nTap: Pick Item", introTextOnScreenTime);
    }

    public void OnPickItem(RaycastHit hitInfo) {
      Debug.Log("Picked a collider: " + hitInfo.collider);
      ShowInfoText("" + hitInfo.collider, 2);
    }

    public void OnPickItem2D(RaycastHit2D hitInfo2D) {
      Debug.Log("Picked a 2D collider: " + hitInfo2D.collider);
      ShowInfoText("" + hitInfo2D.collider, 2);
    }

    public void OnPickableTransformSelected(Transform pickableTransform) {
      Debug.Log("OnPickableTransformSelected() - pickableTransform: " + pickableTransform);
      if (pickableTransform != selectedPickableTransform) {
        StartCoroutine(AnimateScaleForSelection(pickableTransform));
      }
      SetItemColor(pickableTransform, Color.green);
      selectedPickableTransform = pickableTransform;
    }

    public void OnPickableTransformSelectedExtended(PickableSelectedData data) {
      Debug.Log("OnPickableTransformSelectedExtended() - SelectedTransform: " + data.SelectedTransform + ", IsLongTap: " + data.IsLongTap);
    }

    public void OnPickableTransformDeselected(Transform pickableTransform) {
      Debug.Log("OnPickableTransformDeselected() - pickableTransform: " + pickableTransform);
      pickableTransform.localScale = Vector3.one;
      selectedPickableTransform = null;
      RevertToOriginalItemColor(pickableTransform);
    }

    public void OnPickableTransformMoveStarted(Transform pickableTransform) {
      SetItemColor(pickableTransform, new Color(0.5f, 1, 0.5f));
    }

    public void OnPickableTransformMoved(Transform pickableTransform) {
      Debug.Log("Moved transform: " + pickableTransform);
    }

    public void OnPickableTransformMoveEnded(Vector3 startPos, Transform pickableTransform) {
      SetItemColor(pickableTransform, Color.green);
      if (GetTransformPositionValid(pickableTransform) == false) {
        pickableTransform.position = startPos;
      }
    }
  
    private void SetItemColor(Transform itemTransform, Color color) {
      foreach (var itemRenderer in itemTransform.GetComponentsInChildren<Renderer>()) {
        if(originalItemColorCache.ContainsKey(itemRenderer) == false) {
          originalItemColorCache[itemRenderer] = new List<Color>();
        }
        for(int i = 0; i < itemRenderer.materials.Length; ++i) {
          Material mat = itemRenderer.materials[i];
          originalItemColorCache[itemRenderer].Add(mat.color);
          mat.color = color;
        }
      }
    }

    private void RevertToOriginalItemColor(Transform itemTransform) {
      foreach (var itemRenderer in itemTransform.GetComponentsInChildren<Renderer>()) {
        if(originalItemColorCache.ContainsKey(itemRenderer) == true) {
          for(int i = 0; i < itemRenderer.materials.Length; ++i) {
            itemRenderer.materials[i].color = originalItemColorCache[itemRenderer][i];
          }
        }
      }
    }

    /// <summary>
    /// Method to check whether another MobileTouchPickable has the exact same position as the given transform.
    /// NOTE: This is a demo implementation that makes use of slow unity function calls.
    /// </summary>
    private bool GetTransformPositionValid(Transform pickableTransform) {

      //Expensive call. Should be optimized in live environments.
      List<MobileTouchPickable> allPickables = new List<MobileTouchPickable>(FindObjectsOfType<MobileTouchPickable>());

      allPickables.RemoveAll(item => item.PickableTransform == pickableTransform);
      foreach (var pickable in allPickables) {
        if (mobileTouchCamera.CameraAxes == CameraPlaneAxes.XY_2D_SIDESCROLL) {
          if (Mathf.Approximately(pickableTransform.position.x, pickable.PickableTransform.position.x) && Mathf.Approximately(pickableTransform.position.y, pickable.PickableTransform.position.y)) {
            return (false);
          }
        } else {
          if (Mathf.Approximately(pickableTransform.position.x, pickable.PickableTransform.position.x) && Mathf.Approximately(pickableTransform.position.z, pickable.PickableTransform.position.z)) {
            return (false);
          }
        }
      }
      return (true);
    }

    private IEnumerator AnimateScaleForSelection(Transform pickableTransform) {
      float timeAtStart = Time.time;
      const float animationDuration = 0.25f;
      while (Time.time < timeAtStart + animationDuration) {
        float progress = (Time.time - timeAtStart) / animationDuration;
        float scaleFactor = 1.0f + Mathf.Sin(progress * Mathf.PI) * 0.2f;
        pickableTransform.localScale = Vector3.one * scaleFactor;
        yield return null;
      }
      pickableTransform.localScale = Vector3.one;
    }

    public void SetCameraModeOrtho() {
      cam.orthographic = true;
      mobileTouchCamera.CamZoomMin = 4;
      mobileTouchCamera.CamZoomMax = 13;
      mobileTouchCamera.CamZoom = 7;
      mobileTouchCamera.CamOverzoomMargin = 1;
      ResetCamPosition(20);
    }

    public void SetCameraModePerspective() {
      mobileTouchCamera.PerspectiveZoomMode = PerspectiveZoomMode.FIELD_OF_VIEW;
      cam.orthographic = false;
      mobileTouchCamera.CamZoomMin = 30;
      mobileTouchCamera.CamZoomMax = 60;
      mobileTouchCamera.CamZoom = 60;
      mobileTouchCamera.CamOverzoomMargin = 10;
      ResetCamPosition(10);
    }

    public void SetCameraModePerspectiveTranslation() {
      mobileTouchCamera.PerspectiveZoomMode = PerspectiveZoomMode.TRANSLATION;
      cam.orthographic = false;
      mobileTouchCamera.CamZoomMin = 5;
      mobileTouchCamera.CamZoomMax = 40;
      mobileTouchCamera.CamZoom = 10;
      mobileTouchCamera.CamOverzoomMargin = 2;
      cam.fieldOfView = 60;
      ResetCamPosition(10);
    }

    private void ResetCamPosition(float distance) {
      if(mobileTouchCamera.CameraAxes == CameraPlaneAxes.XY_2D_SIDESCROLL) {
        mobileTouchCamera.Transform.position = new Vector3(0, 0, -distance);
      } else {
        mobileTouchCamera.Transform.position = new Vector3(0, distance, 0);
      }
      mobileTouchCamera.ResetCameraBoundaries();
    }
      
    public void SetSnapAngleStraight() {
      mobilePickingController.SnapAngle = SnapAngle.Straight_0_Degrees;
    }

    public void SetSnapAngleDiagonal() {
      mobilePickingController.SnapAngle = SnapAngle.Diagonal_45_Degrees;
    }

    public void SetSnappingEnabled(bool flag) {
      mobilePickingController.SnapToGrid = flag;
    }

    public void SetRotationEnabled(bool flag) {
      mobileTouchCamera.EnableRotation = flag;
    }

    public void SetTiltEnabled(bool flag) {
      mobileTouchCamera.EnableTilt = flag;
    }

    public void ToggleGameObjectActive(GameObject go) {
      go.SetActive(!go.activeInHierarchy);
    }

    public void ToggleCamAngle(bool angle) {
      mobilePickingController.SnapAngle = angle == true ? SnapAngle.Straight_0_Degrees : SnapAngle.Diagonal_45_Degrees;
    }

    public void SetInputOnLockedArea() {
      touchInputController.IsInputOnLockedArea = true;
    }

    private void ShowInfoText(string message, float onScreenTime) {
      if(textInfo != null) {
        textInfo.text = message;
        if (coroutineHideInfoText != null) {
          StopCoroutine(coroutineHideInfoText);
        }
        textInfo.enabled = true;
        coroutineHideInfoText = StartCoroutine(HideInfoText(onScreenTime));
      }
    }

    private IEnumerator HideInfoText(float delay) {
      if(textInfo != null) {
        yield return new WaitForSeconds(delay);
        textInfo.enabled = false;
      }
    }

    #region detail messages
    private void SetTextDetail(string message) {
      if(textDetail != null) {
        textDetail.text = message;
      }
    }

    private void OnInputClick(Vector3 clickScreenPosition, bool isDoubleClick, bool isLongTap) {
      SetTextDetail("OnInputClick(clickScreenPosition: " + clickScreenPosition + ", isDoubleClick: " + isDoubleClick + ", isLongTap: " + isLongTap + ")");
      Debug.Log("OnInputClick(clickScreenPosition: " + clickScreenPosition + ", isDoubleClick: " + isDoubleClick + ", isLongTap: " + isLongTap + ")");
    }

    private void OnPinchUpdate(PinchUpdateData pinchUpdateData) {
      SetTextDetail("OnPinchUpdate(pinchCenter: " + pinchUpdateData.pinchCenter + ", pinchDistance: " + pinchUpdateData.pinchDistance + ", pinchStartDistance: " + pinchUpdateData.pinchStartDistance + ")");
    }

    private void OnPinchStop() {
      SetTextDetail("OnPinchStop()");
    }

    private void OnPinchStart(Vector3 pinchCenter, float pinchDistance) {
      SetTextDetail("OnPinchStart(pinchCenter: " + pinchCenter + ", pinchDistance: " + pinchDistance + ")");
    }

    private void OnFingerDown(Vector3 screenPosition) {
      SetTextDetail("OnFingerDown(screenPosition: " + screenPosition + ")");
    }

    private void OnDragUpdate(Vector3 dragPosStart, Vector3 dragPosCurrent, Vector3 correctionOffset) {
      SetTextDetail("OnDragUpdate(dragPosStart: " + dragPosStart + ", dragPosCurrent: " + dragPosCurrent + ")");
    }

    private void OnDragStop(Vector3 dragStopPos, Vector3 dragFinalMomentum) {
      SetTextDetail("OnDragStop(dragStopPos: " + dragStopPos + ", dragFinalMomentum: " + dragFinalMomentum + ")");
    }

    private void OnDragStart(Vector3 pos, bool isLongTap) {
      SetTextDetail("OnDragStart(pos: " + pos + ", isLongTap: " + isLongTap + ")");
    }
    #endregion
  }
}
