using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.IO;
//using UnityEditor;


public class DynamicDeviceClient : MonoBehaviour {

    NW_TCPClient client;
    public string serverIp = "172.31.1.86";
    string str = "server say:";
    ReceivedData receivedData;

    private void OnDisable()
    {
        if(client != null)
            client.CloseSocket();
    }

    private void ConnectServer()
    {
        client = new NW_TCPClient();
        client.Init();
        client.ConnectServer(serverIp, 9999, null, OnReceive);
    }

    // Update is called once per frame
    void Update () {
#if !DYNAMIC_LUA

        if(receivedData != null && receivedData.Completed) {
            FileStream stream = File.Create(Application.persistentDataPath + "/syncpackage");
            Debug.Log(Application.persistentDataPath + "/syncpackage");
            stream.Write(receivedData.data, 0, receivedData.Length);
            stream.Close();
            receivedData = null;
            ReloadUpdate();
        }
#else
        if (receivedData != null && receivedData.Completed) {
            MemoryStream mm = new MemoryStream(receivedData.data);
            byte[] len = new byte[4];
            mm.Read(len, 0, 4);

            int nameLength = System.BitConverter.ToInt32(len, 0);
            Debug.Log(nameLength);

            byte[] namebuffer = new byte[nameLength];
            mm.Read(namebuffer, 0, nameLength);

            string name = System.Text.Encoding.Default.GetString(namebuffer);
            Debug.Log(name);

            byte[] realBuffer = new byte[receivedData.data.Length - nameLength - 4];
            mm.Read(realBuffer, 0, realBuffer.Length);

            FileStream stream = File.Create(Application.persistentDataPath + "/" + name + ".lua");
            stream.Write(realBuffer, 0, realBuffer.Length);
            stream.Close();
            receivedData = null;
        }
#endif
    }

    private void ReloadUpdate()
    {
        AssetBundle.UnloadAllAssetBundles(false);
        AssetBundle ab = AssetBundle.LoadFromFile(Application.persistentDataPath + "/syncpackage");
        string[] names = ab.GetAllAssetNames();
        for(int i = 0; i < names.Length; i++) {
            if(names[i].EndsWith(".shader")) {
                Shader s = ab.LoadAsset<Shader>(names[i]);

                //后期(如果有需要调试后期其他特效，可以在此基础上加)
                Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
                for(int r = 0; r < renderers.Length; r ++ ) {
                    if(renderers[r].sharedMaterial != null && renderers[r].sharedMaterial.shader.name == s.name ) {
                        renderers[r].sharedMaterial.shader = s;
                    }
                }
            }
            else if(names[i].EndsWith(".mat")) {
                Material mat = ab.LoadAsset<Material>(names[i]);

                Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
                for (int r = 0; r < renderers.Length; r++) {
                    if (renderers[r].sharedMaterial != null && renderers[r].sharedMaterial.name == mat.name) {
                        renderers[r].sharedMaterial = mat;
                    }
                }
            }
        }

        ab.Unload(false);
    }

    private void OnReceive(byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        if(receivedData == null) {
            receivedData = new ReceivedData();

            MemoryStream mms = new MemoryStream(data);
            BinaryReader br = new BinaryReader(mms);
            receivedData.Length = br.ReadInt32();
            receivedData.data = new byte[receivedData.Length];
            receivedData.WriteBytes(br.ReadBytes(data.Length - 4));
            br.Close();
            mms.Close();
        }
        else {
            receivedData.WriteBytes(data);
        }
        str += "receive:" + data.Length;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 200, 100), "connect")) {
            //byte[] bs = Encoding.Unicode.GetBytes("hello");
            //client.Send(bs, bs.Length);
            ConnectServer();
        }
        serverIp = GUI.TextField(new Rect(0, 100, 200, 100), serverIp);

        GUI.Label(new Rect(0, 200, 300, 200), str);
    }

    public class ReceivedData
    {
        public byte[] data;
        public int Length;
        private int currentWriteLength = 0;
        private MemoryStream ms;
        private BinaryWriter bw;

        public void WriteBytes(byte[] bytes)
        {
            if(ms == null)
                ms = new MemoryStream(data);
            if(bw == null)
                bw = new BinaryWriter(ms);
            bw.Write(bytes);
            currentWriteLength += bytes.Length;

            if (Completed) {
                bw.Close();
                ms.Close();
            }
        }

        public bool Completed
        {
            get
            {
                return Length == currentWriteLength;
            }
        }
    }
}
