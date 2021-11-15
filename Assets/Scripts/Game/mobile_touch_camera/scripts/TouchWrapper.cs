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
using System.Collections.Generic;

namespace BitBenderGames {

  public class TouchWrapper {

    public static int TouchCount {
      get {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
        #region unity remote codepath
        if (Input.touchCount > 0) {
          return (Input.touchCount);
        }
        #endregion

        if (Input.GetMouseButton(0) == true) {
          return (1);
        } else {
          return (0);
        }
#else
        return (Input.touchCount);
#endif
      }
    }

    public static WrappedTouch Touch0 {
      get {
        if (TouchCount > 0) {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
          #region unity remote codepath
          if (Input.touchCount > 0) {
            return WrappedTouch.FromTouch(Input.touches[0]);
          }
          #endregion

          return (new WrappedTouch() { Position = Input.mousePosition });
#else
          return WrappedTouch.FromTouch(Input.touches[0]);
#endif
        } else {
          return (null);
        }
      }
    }

    public static bool IsFingerDown {
      get {
        return (TouchCount > 0);
      }
    }

    public static List<WrappedTouch> Touches {
      get {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
        #region unity remote codepath
        if (Input.touchCount > 0) {
          return (GetTouchesFromInputTouches());
        }
        #endregion

        return new List<WrappedTouch>() { Touch0 };
#else
        return (GetTouchesFromInputTouches());
#endif
      }
    }

    private static List<WrappedTouch> GetTouchesFromInputTouches() {
      List<WrappedTouch> touches = new List<WrappedTouch>();
      foreach (var touch in Input.touches) {
        touches.Add(WrappedTouch.FromTouch(touch));
      }
      return (touches);
    }

    public static Vector2 AverageTouchPos {
      get {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
        #region unity remote codepath
        if (Input.touchCount > 0) {
          return (GetAverageTouchPosFromInputTouches());
        }
        #endregion

        return (Input.mousePosition);
#else
        return (GetAverageTouchPosFromInputTouches());
#endif

      }
    }

    private static Vector2 GetAverageTouchPosFromInputTouches() {
      Vector2 averagePos = Vector2.zero;
      if (Input.touches != null && Input.touches.Length > 0) {
        foreach (var touch in Input.touches) {
          averagePos += touch.position;
        }
        averagePos /= (float)Input.touches.Length;
      }
      return (averagePos);
    }
  }
}
