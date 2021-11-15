using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Spenve
{
    public struct Trapezium
    {
        public Vector3 rightTop;
        public Vector3 leftTop;
        public Vector3 leftDown;
        public Vector3 rightDown;
        public Vector3 center;
    }

    public delegate IEnumerator CoroutineAction();
    public delegate void NEvent();
    public delegate void IEvent(int i);
}
