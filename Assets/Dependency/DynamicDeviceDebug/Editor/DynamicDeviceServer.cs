using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

public class DynamicDeviceServer : EditorWindow
{
    NW_TCPServer server;
    List<TCPClientState> clients = new List<TCPClientState>();
    bool focus = false;

    [MenuItem("Tools/动态调试", false, 70)]

    static void Initialize()
    {
        DynamicDeviceServer window = EditorWindow.GetWindow<DynamicDeviceServer>("DynamicDeviceServer", true);
        window.minSize = new Vector2(100, 300);
        window.autoRepaintOnSceneChange = true;
        window.Show();
        Selection.selectionChanged += () =>
        {
            window.Focus();
        };
    }

    public static string GetLocalIP()
    {
        string hostName = Dns.GetHostName();   //获取本机名
        IPHostEntry localhost = Dns.GetHostEntry(hostName);    
        for (int i = 0; i < localhost.AddressList.Length; i++) {
            if (localhost.AddressList[i].IsIPv6LinkLocal == false) {
                return localhost.AddressList[i].ToString();
            }
        }
        return localhost.AddressList[0].ToString();
    }

    void OnEnable () {
        Debug.Log(GetLocalIP());
        server = new NW_TCPServer(System.Net.IPAddress.Parse(GetLocalIP()), 9999);
        server.Start();
        server.ClientConnected += new System.EventHandler<AsyncEventArgs>(ClientConnected);
        server.ClientDisconnected += new System.EventHandler<AsyncEventArgs>(ClientDisconnected);
        server.DataReceived += new System.EventHandler<AsyncEventArgs>(DataReceived);
    }

    private void OnDisable()
    {
        if(server != null)
            server.Stop();
    }

    private void Update()
    {
        if(focus) {
            focus = false;
            this.Focus();
        }
    }

    private void ClientConnected(object o, AsyncEventArgs arg)
    {
        clients.Add(arg._state);
        focus = true;
        Debug.Log("connected:" + arg._state.TcpClient.Client.RemoteEndPoint);
    }

    private void ClientDisconnected(object o, AsyncEventArgs arg)
    {
        clients.Remove(arg._state);
        focus = true;
        Debug.Log("diconnected:" + arg._state.TcpClient.Client.RemoteEndPoint);
    }

    private void DataReceived(object o, AsyncEventArgs arg)
    {
    }

    void OnGUI()
    {
        for(int i = 0; i < clients.Count; i++) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(clients[i].TcpClient.Client.RemoteEndPoint.ToString());
            GUILayout.EndHorizontal();
        }

        if (Selection.objects != null) {
            for(int i = 0; i < Selection.objects.Length; i++) {
                EditorGUILayout.ObjectField(Selection.objects[i], typeof(Object));
            }

            if(GUILayout.Button("同步选中资源到设备(shader和材质)"))
            {
                string[] paths = new string[Selection.objects.Length];

                for (int i = 0; i < Selection.objects.Length; i++) {
                    paths[i] = AssetDatabase.GetAssetPath(Selection.objects[i]);
                }

                AssetBundleBuild abb = new AssetBundleBuild();
                abb.assetBundleName = "syncpackage";
                abb.assetNames = paths;
                BuildPipeline.BuildAssetBundles(Application.dataPath, new AssetBundleBuild[] { abb }, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

                SyncToAllClient();

                File.Delete(Application.dataPath + "/Assets.manifest");
                File.Delete(Application.dataPath + "/Assets");
                File.Delete(Application.dataPath + "/syncpackage.manifest");
                File.Delete(Application.dataPath + "/syncpackage");
            }

            if (GUILayout.Button("同步选中lua到设备")) {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                path = Application.dataPath + path.Replace("Assets", "");
                byte[] bs = File.ReadAllBytes(path);

                string filename = Selection.activeObject.name;
                Debug.Log(filename);

                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                byte[] fileNameBuffer = System.Text.Encoding.Default.GetBytes(filename);
                Debug.Log(fileNameBuffer.Length);

                bw.Write(4 + fileNameBuffer.Length + bs.Length);
                bw.Write(fileNameBuffer.Length);
                bw.Write(fileNameBuffer);
                bw.Write(bs);
                byte[] allbytes = ms.ToArray();

                for (int i = 0; i < clients.Count; i++) {
                    SyncResToClient(clients[i], allbytes);
                }
            }
        }
    }

    void SyncToAllClient()
    {
        FileStream stream = File.Open(Application.dataPath + "/syncpackage", FileMode.Open);
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);

        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        bw.Write((int)stream.Length);
        bw.Write(buffer);
        byte[] allbytes = ms.ToArray();

        stream.Close();
        bw.Close();
        ms.Close();

        for (int i = 0; i < clients.Count; i++) {
            SyncResToClient(clients[i], allbytes);
        }
    }

    void SyncResToClient(TCPClientState state, byte[] buffer)
    {
        server.Send(state.TcpClient, buffer);
    }


    void Binary()
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        bw.Write(5);
        bw.Write(10);
        bw.Close();
        byte[] allbytes = ms.ToArray();
        ms.Close();

        MemoryStream mms = new MemoryStream(allbytes);
        BinaryReader br = new BinaryReader(mms);
        Debug.Log( br.ReadInt32() );
        Debug.Log(br.ReadInt32());
        br.Close();
        mms.Close();
    }
}
