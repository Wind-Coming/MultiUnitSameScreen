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

  public class PinchUpdateData {

    public Vector3 pinchCenter;
    public float pinchDistance;
    public float pinchStartDistance;
    public float pinchAngleDelta;
    public float pinchAngleDeltaNormalized;
    public float pinchTiltDelta;
    public float pinchTotalFingerMovement;
  }
}
