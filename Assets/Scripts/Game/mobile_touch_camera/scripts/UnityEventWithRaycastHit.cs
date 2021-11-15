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
using UnityEngine.Events;

namespace BitBenderGames {

  [System.Serializable]
  public class UnityEventWithRaycastHit : UnityEvent<RaycastHit> { }
}