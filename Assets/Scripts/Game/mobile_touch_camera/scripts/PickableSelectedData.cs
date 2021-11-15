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

  public class PickableSelectedData  {

    public Transform SelectedTransform { get; set; }

    public bool IsDoubleClick { get; set; }

    public bool IsLongTap { get; set; }
  }
}
