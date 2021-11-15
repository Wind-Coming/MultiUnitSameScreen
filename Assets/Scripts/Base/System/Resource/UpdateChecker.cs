using Spenve;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;


public class UpdateDataInfo
{
    public int step;
    public int index;
    public float process;
    public UpdateDataInfo(int s, int i, float p)
    {
        step = s;
        index = i;
        process = p;
    }
}

public class TotalUpdateData
{
    public List<string> urlList = new List<string>();
    public List<int> saveList = new List<int>();
    public int x = 0;
    public TotalUpdateData(List<string> uList,List<int> sList)
    {
        urlList = uList;
        saveList = sList;
    }
}

public class UpdateChecker : MonoBehaviour 
{
    //
    public bool skipUpdate;

    //error code
    public const int ERR_NetworkNotReachable = -1;
    public const int ERR_NetworkError = -2;
    public const int ERR_FileUnvalid = -3;          //文件不完整
    public const int ERR_LocalUnvalid = -4;         //本地文件非法
    public const int ERR_UpdateAppRequire = -5;     //需要下载更新



    private Action<int> onResult;
    private Action<float> onProcess;
    private Action<int, int, float> handleUpdate;

    private XmlDocument localConfig;
    private XmlDocument remoteConfig;


    private List<string> urlList = new List<string>();
    private List<int> sizeList = new List<int>();
    private int totalSize = 0;

    //version info
    private string lVersion;
    private string sVersion;
    public int TotalSizeToUpdate { get { return totalSize; } }

	// Use this for initialization
	void Start () 
    {
        //just for test
        //onResult = (a) => { Debug.Log(" server info : " + a );};
        onResult = HandleResult;
        onProcess = (a) => { Debug.Log(" progress info : " + a);};
        handleUpdate = HandleUpdate;

        if (!skipUpdate)
		    CheckerInit();
	}
	
	// Update is called once per frame
	void Update ()
    {

	}

    public void HandleResult(int code)
    {
        Debug.Log(code);
        //LuaClient.Instance.CallLuaFunc("HandleUpdateChecker.HandlerResultCode", code);
    }

    public void HandleUpdate(int step, int index, float process)
    {
        //UpdateDataInfo data = new UpdateDataInfo(step, index, process);
        print("---------process-------" + process);
        //LuaClient.Instance.CallLuaFunc("HandleUpdateChecker.HandleProcess", data);
    }

    public void CheckerInit()
    {
#if UNITY_EDITOR
        if (!EditorConfig.SimulateAssetBundleInEditor)
            return;
#endif
        StartCoroutine(Checker());
    }


    IEnumerator Checker()
    {
        LoadLocalConfig();

        yield return LoadServerConfig();

       lVersion = localConfig.SelectSingleNode("Assets").Attributes["version"].Value;
       sVersion = remoteConfig.SelectSingleNode("Assets").Attributes["version"].Value;

       Debug.Log("Local Server : " + lVersion);
       Debug.Log("Server server : " + sVersion);

       if( !CompareConfig()) {
            yield break;
        }

        Debug.Log("Need Update files : " +  this.urlList.Count);

        foreach (var name in this.urlList)
        {
           Debug.Log(name) ;
        }

        //yield return DownUpdateFiles(( a, b , v) => { Debug.Log("Step : " + a + " index : " + b + " progress : " + v );});

        yield return DownUpdateFiles(handleUpdate);
    }

    public IEnumerator LoadServerConfig()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult(ERR_NetworkNotReachable);
            yield break;
        }

        string url = GameConfig.GetUpdateResourceUrl("update.xml");

        UnityWebRequest download = UnityWebRequest.Get(url);

        download.SendWebRequest();

        while (!download.isDone) {
            onProcess(download.downloadProgress * 100);
            yield return download;
        }

        if (!string.IsNullOrEmpty(download.error)) {
            Debug.Log(url + " download fail with msg: " + download.error);
            onResult(ERR_NetworkError);
            yield break;
        }


        remoteConfig = new XmlDocument();
        try {
            remoteConfig.LoadXml(download.downloadHandler.text);
            onResult(0);
        }
        catch (Exception e) {
            Debug.LogException(e);
            onResult(ERR_FileUnvalid);
            yield break;
        }
    }


    public void LoadLocalConfig()
    {
        string url = Path.Combine(Utils.GetExternalPath(true, false), "config.xml"); 

        localConfig = new XmlDocument();
        string lstreamConfig = null;

        if (File.Exists(url))
        {
            lstreamConfig = File.ReadAllText(url);
        }
        else {
            string innerPath = Path.Combine(Utils.GetInnerPath(), "config.xml");
            print(innerPath);
            lstreamConfig = File.ReadAllText(innerPath);
        }

        if (!string.IsNullOrEmpty(lstreamConfig))
            localConfig.LoadXml(lstreamConfig);
    }

    private long GetVersionCodes(string str) 
    {
        if (string.IsNullOrEmpty(str))
            return 0;

        string[] temp = str.Split('.');
        long result = 0;

        long max = 1000000000;
        for( int i = 0; i < temp.Length; ++i )
        {
            result += long.Parse(temp[i]) * max;
            max /= 1000;
        }

        return result;
    }

    public bool CompareConfig() 
    {
        long lversion = 0;
        long rversion = 0;
        try
        {
            string lv = localConfig.SelectSingleNode("Assets").Attributes["version"].Value;
            string rv =  remoteConfig.SelectSingleNode("Assets").Attributes["version"].Value;
            lversion = GetVersionCodes(lv);
            rversion = GetVersionCodes(rv);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        if(lversion >= rversion) {
            return false;
        }

        //check all size
        XmlNodeList rRoot = remoteConfig.SelectSingleNode("Assets").ChildNodes;
        XmlNode lRoot = localConfig.SelectSingleNode("Assets");
        foreach(XmlElement element in rRoot)
        {
            XmlNode local = lRoot.SelectSingleNode(element.LocalName);
            if (local == null)
            {
                this.urlList.Add(element.LocalName);
                this.sizeList.Add(int.Parse(element.Attributes["size"].Value));
                totalSize += this.sizeList[this.sizeList.Count - 1];
            }
            else
            {
                string lhash = local.Attributes["hashcode"].Value;
                string rhash = element.Attributes["hashcode"].Value;

                if (string.CompareOrdinal(lhash, rhash) != 0)
                {
                    this.urlList.Add(element.LocalName);
                    this.sizeList.Add(int.Parse(local.Attributes["size"].Value));

                    totalSize += this.sizeList[this.sizeList.Count - 1];
                }
            }
        }

        return true;
    }


    //index, step, float
    public IEnumerator DownUpdateFiles(Action<int, int, float> onDownload) 
    {
        //nothing to update
        if (this.urlList.Count <= 0)
        {
            onResult(0);
            yield break;
        }

        onDownload(-1, 0, 0);
        onDownload(0, 0, 0);
        for (int i = 0; i < this.urlList.Count; ++i )
        {
            onDownload(i+1,0,0);

            string url = urlList[i];
            string downurl = GameConfig.GetUpdateResourceUrl(url);

            Debug.Log("DownLoad : "+ downurl);

            UnityWebRequest download = UnityWebRequest.Get(downurl);

            download.SendWebRequest();

            while (!download.isDone) {
                onDownload(i + 1, 0, download.downloadProgress);
                yield return new WaitForEndOfFrame();
            }

            if (!string.IsNullOrEmpty(download.error)) {
                onResult(ERR_NetworkError);
                yield break;
            }

            onDownload(i + 1, 1, 0);

            string path = Path.Combine(Utils.GetExternalPath(true, false), url);

            string pathname = path.Substring(0, path.LastIndexOf('/'));
            if (!Directory.Exists(pathname)) {
                Directory.CreateDirectory(pathname);
            }

            File.WriteAllBytes(path, download.downloadHandler.data);

            //update local version
            UpdateLocalVersion(url);
        }

        //downloaded
        DownLoadOver();
    }


    public void UpdateLocalVersion( string name )
    {
        XmlNode  rRoot = remoteConfig.SelectSingleNode("Assets");
        XmlNode  lRoot = localConfig.SelectSingleNode("Assets") ;

        string singlecode = "descendant::" + name;
        XmlNode server = rRoot.SelectSingleNode(singlecode);
        XmlNode local = lRoot.SelectSingleNode(singlecode);
       if(server == null){
           Debug.LogError("Valid asset name : " + name);
          return;
       }

        if(local == null){
                 XmlElement elm =  localConfig.CreateElement(name);
                elm.SetAttribute("hashcode" , server.Attributes["hashcode"].Value);
                lRoot.AppendChild(elm);
        }
        else
        {
            string lhash = local.Attributes["hashcode"].Value;
            string shash = server.Attributes["hashcode"].Value;

            local.Attributes["hashcode"].Value = shash;
        }

        localConfig.Save(Path.Combine(Utils.GetExternalPath(true, false), "config.xml"));
    }

    public void DownLoadOver()
    {
        //重新加载gameconfig
        GameConfig.Roload();
        XmlNode root = localConfig.SelectSingleNode("Assets");
        root.Attributes["version"].Value = sVersion;
        localConfig.Save(Path.Combine(Utils.GetExternalPath(true, false), "config.xml"));
    }

  
}
