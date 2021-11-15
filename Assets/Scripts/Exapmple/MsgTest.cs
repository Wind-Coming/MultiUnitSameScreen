using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MsgTest : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            Spenve.MsgSystem.Instance.AddListener<int>("TestA", TestA);
        }
        if(Input.GetKeyDown(KeyCode.B))
        {
            Spenve.MsgSystem.Instance.RemoveListener<int>("TestA", TestA);
        }
        if(Input.GetKeyDown(KeyCode.C))
        {
            Spenve.MsgSystem.Instance.PostMessage<int>("TestA", 1);
        }

        if(Input.GetKeyDown(KeyCode.D))
        {
            Spenve.MsgSystem.Instance.AddListener<MsgTest>("TestB", TestB);
        }
        if(Input.GetKeyDown(KeyCode.E))
        {
            Spenve.MsgSystem.Instance.RemoveListener<MsgTest>("TestB", TestB);
        }
        if(Input.GetKeyDown(KeyCode.F))
        {
            Spenve.MsgSystem.Instance.PostMessage<MsgTest>("TestB", this);
        }

    }

    public void TestA(int a)
    {
        Debug.Log("TestA:" + a);
    }

    public void TestB(MsgTest a)
    {
        Debug.Log("TestB:" + a);
    }

}
