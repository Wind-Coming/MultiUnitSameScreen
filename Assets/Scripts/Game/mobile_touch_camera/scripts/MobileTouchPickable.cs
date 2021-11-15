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

namespace BitBenderGames {

  public class MobileTouchPickable : MonoBehaviour {

    private static MobileTouchCamera mobileTouchCam;

    [SerializeField]
    [Tooltip("Optional. This value only needs to be set in case the collider of the pickable item is not on the root object of the pickable item.")]
    private Transform pickableTransform;

    [SerializeField]
    [Tooltip("When snapping is enabled, this value defines a position offset that is added to the center of the object when dragging. Note that this value is added on top of the snapOffset defined in the MobilePickingController. When a top-down camera is used, these 2 values are applied to the X/Z position.")]
    private Vector2 localSnapOffset = Vector2.zero;

    public Transform PickableTransform {
      get { return (pickableTransform); }
      set { pickableTransform = value; }
    }

    public Vector2 LocalSnapOffset { get { return (localSnapOffset); } }

    public void Awake() {
      if (mobileTouchCam == null) {
        mobileTouchCam = FindObjectOfType<MobileTouchCamera>();
      }
      if (mobileTouchCam == null) {
        Debug.LogError("No MobileTouchCamera found in scene. This script will not work without this.");
      }
      if (pickableTransform == null) {
        pickableTransform = this.transform;
      }
      if (gameObject.GetComponent<Collider>() == null && gameObject.GetComponent<Collider2D>() == null) {
        Debug.LogError("MobileTouchPickable must be placed on a gameObject that also has a Collider or Collider2D component attached.");
      }
      if (mobileTouchCam.GetComponent<MobilePickingController>() == null) { //Auto add picking controller component to mobile touch cam go.
        mobileTouchCam.gameObject.AddComponent<MobilePickingController>();
      }
    }
  }
}
