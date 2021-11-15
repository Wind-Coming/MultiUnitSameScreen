using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Fps : MonoBehaviour {

    public int fps;
    float time;
    int frame;
    string text;
    GUIContent gc;
    GUIStyle gs;
    public Text uitext;

	// Use this for initialization
	void Start () {
        fps = 0;
        time = 0;
        frame = 0;
        #if !UNITY_EDITOR 
        Application.targetFrameRate = 60;
        #endif
        gc = new GUIContent();
        gs = new GUIStyle();
	}
	
	// Update is called once per frame
	void Update () {

        time += Time.deltaTime;
        frame++;

        if (time >= 1)
        {
            time--;
            fps = frame;
            frame = 0;
            text = "当前帧率: " + fps.ToString();
            uitext.text = text;
        }
	}

    void OnGUI()
    {
        // gc.text = text;
        // gs.fontSize = 40;
        // gs.normal.textColor = Color.yellow;
        // GUILayout.Label(gc, gs);
        
        //GUI.contentColor = Color.yellow;
        //GUILayout.Label("FPS: " + fps.ToString(), gs);
        //GUI.Label(new Rect(20, 20, 100, 50), "FPS: " + fps.ToString()); 
    }
}
