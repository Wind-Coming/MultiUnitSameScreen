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

namespace BitBenderGames {

  /// <summary>
  /// A little helper-script that allows to set the mobile touch camera boundary
  /// by help of a collider that marks the extends of the game-world.
  /// Simply attach this script to the camera and assign a collider to it.
  ///
  /// Note that the collider must be enabled when the game starts.
  /// To prevent your gameobjects from colliding with this collider you may either:
  /// 
  ///  * Set the IsTrigger flag of the collider to true.
  ///  * Assign a custom layer to this collider GO and disable collisions in the Layer Collision Matrix section of the Physics Settings.
  ///  * Or simply move the collider up above, or down below the game-world.
  /// </summary>
  [RequireComponent(typeof(MobileTouchCamera))]
  public class SetBoundaryFromCollider : MonoBehaviour {

    [SerializeField]
    private BoxCollider boxCollider;

    public void Start() {

      if(boxCollider == null) {
        Debug.LogError("This script requires a box collider to be assigned.");
        return;
      }

      MobileTouchCamera mobileTouchCam = GetComponent<MobileTouchCamera>();

      var boxMin = boxCollider.bounds.min;
      var boxMax = boxCollider.bounds.max;
      if(mobileTouchCam.CameraAxes == CameraPlaneAxes.XY_2D_SIDESCROLL) {
        mobileTouchCam.BoundaryMin = new Vector2(boxMin.x, boxMin.y);
        mobileTouchCam.BoundaryMax = new Vector2(boxMax.x, boxMax.y);
      } else {
        mobileTouchCam.BoundaryMin = new Vector2(boxMin.x, boxMin.z);
        mobileTouchCam.BoundaryMax = new Vector2(boxMax.x, boxMax.z);
      }
      mobileTouchCam.ResetCameraBoundaries();
    }

  }
}
