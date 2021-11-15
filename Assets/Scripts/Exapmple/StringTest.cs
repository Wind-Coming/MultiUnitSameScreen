using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class StringTest : MonoBehaviour
{
    string a = "abc";
    string b = "def";
    StringBuilder sb = new StringBuilder();
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("tttttt");
    }

    // Update is called once per frame
    void Update()
    {
        //string x = a + b;
        sb.Clear();
        sb.Append(a);
        sb.Append(b);
        //sb.ToString();
    }
}
