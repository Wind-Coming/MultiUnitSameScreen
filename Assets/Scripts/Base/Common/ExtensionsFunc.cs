using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spenve;
/// <summary>
/// 扩展函数
/// </summary>
public static class ExtensionsFunc {

    public static void SetFalseAfterTime(this GameObject target, float time)
    {
        Timer t = new Timer(() => target.SetActive(false), time);
        t.Start();
    }
}
