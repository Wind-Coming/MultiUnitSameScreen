using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Spenve
{
    public class TimerSystem : SingletonAutoCreate<TimerSystem>
    {
        List<Timer> sts = new List<Timer>();

        public void Add(Action _func, float _time, bool _loop = false)
        {
            Timer st = new Timer(_func, _time, _loop);
            st.Start();
        }

        public void Push(Timer std)
        {
            sts.Add(std);
        }

        public void Pop(Timer std)
        {
            sts.Remove(std);
        }

        void Update()
        {
            for (int i = 0; i < sts.Count; i++)
            {
                Timer st = sts[i];
                st.curTime += Time.deltaTime;

                if(st.update != null)
                {
                    st.update();
                }

                if (st.curTime >= st.time)
                {
                    st.func();
                    if (st.loop)
                    {
                        st.curTime -= st.time;
                    }
                    else
                    {
                        st.Stop();
                        i--;
                        continue;
                    }
                }
            }
        }
    }

    public class Timer
    {
        public Action func;
        public bool loop;
        public float time;
        public float curTime;
        public Action update;

        public Timer(Action _func, float _time, bool _loop = false)
        {
            func = _func;
            time = _time;
            loop = _loop;
        }

        public void Start()
        {
            curTime = 0;
            TimerSystem.Instance.Push(this);
        }

        public void Stop()
        {
            TimerSystem.Instance.Pop(this);
        }
    }
}