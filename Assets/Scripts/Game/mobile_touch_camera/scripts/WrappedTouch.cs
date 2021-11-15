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

  public class WrappedTouch {
    public Vector3 Position { get; set; }
    public int FingerId { get; set; }

    public WrappedTouch() {
      FingerId = -1;
    }

    public static WrappedTouch FromTouch(Touch touch) {
      WrappedTouch wrappedTouch = new WrappedTouch() { Position = touch.position, FingerId = touch.fingerId };
      return (wrappedTouch);
    }
  }
}
