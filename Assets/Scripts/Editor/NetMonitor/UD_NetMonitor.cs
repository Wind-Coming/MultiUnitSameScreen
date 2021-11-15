//using UnityEngine;
//using UnityEditor;
//using System.Collections.Generic;

//public class UD_NetMonitor : EditorWindow
//{
//    #region Member Data
//    protected static Vector2 kScrollViewPos;
//    protected static bool m_bToggleDownLoadDetail;
//    protected static bool m_bToggleUpLoadDetail;
//    private static UD_NetMonitor self;
//    #endregion

//    /// <summary>
//    /// Initialize this instance.
//    /// </summary>
//    [MenuItem("Windplay/Net Monitor", false, 10)]
//    static void Initialize()
//    {
//        self = EditorWindow.GetWindow<UD_NetMonitor>("Net Monitor");
//        self.minSize = new Vector2(1140, 450);

//        self.Show();

//        kScrollViewPos = new Vector2();
//        m_bToggleDownLoadDetail = false;
//        m_bToggleUpLoadDetail = false;
//    }

//    /// <summary>
//    /// Raises the GUI event.
//    /// </summary>
//    void OnGUI()
//    {
//        NW_NetMonitor kNetMonitor = NW_NetMonitor.Instance;
//        if (null == kNetMonitor || kNetMonitor.m_kMsgRecordSortList_DownLoad == null)
//        {
//            EditorGUILayout.BeginHorizontal("BOX");
//            GUILayout.Label("请先启动游戏");
//            GUILayout.EndHorizontal();
//            if (GUILayout.Button("关闭"))
//            {
//                self.Close();
//            }
//            return;
//        }

//        //计算网络速率
//        kNetMonitor.Update(UT_TimeManager.Instance.GetProcessTime());

//        EditorGUILayout.BeginHorizontal("BOX");

//        bool bIsActive = kNetMonitor.m_bActive;
//        string kBtnName = bIsActive ? "停止" : "开始";

//        if (GUILayout.Button(kBtnName, GUILayout.Width(150)))
//        {
//            if (bIsActive)
//                kNetMonitor.StopMonitor();
//            else
//                kNetMonitor.StartMonitor();
//        }

//        GUI.color = Color.cyan;
//        NW_NetMonitor.SortType eSortType = kNetMonitor.GetSortType();
//        string kSortName = (eSortType == NW_NetMonitor.SortType.ST_COUNT) ? "按包个数排序" : "按流量排序";
//        GUILayout.Label(kSortName, GUILayout.Width(100));
//        GUI.color = Color.gray;
//        if (GUILayout.Button("切换排序", GUILayout.Width(100)))
//        {
//            if (eSortType == NW_NetMonitor.SortType.ST_COUNT)
//                kNetMonitor.SetSortType(NW_NetMonitor.SortType.ST_SIZE);
//            else
//                kNetMonitor.SetSortType(NW_NetMonitor.SortType.ST_COUNT);
//        }
//        GUI.color = Color.white;

//        float fMonitorTime = kNetMonitor.m_fEndMonitorTime - kNetMonitor.m_fBeginMonitorTime;
//        GUI.color = Color.red;
//        GUILayout.Label("监控时间 " + fMonitorTime, GUILayout.Width(150));

//        GUI.color = Color.yellow;
//        GUILayout.Label("下行_包数 " + kNetMonitor.m_iTotalMsgCount_DownLoad, GUILayout.Width(150));
//        GUILayout.Label("下行_流量 " + kNetMonitor.m_iTotalMsgSize_DownLoad, GUILayout.Width(150));

//        GUI.color = Color.green;
//        GUILayout.Label("上行_包数 " + kNetMonitor.m_iTotalMsgCount_UpLoad, GUILayout.Width(150));
//        GUILayout.Label("上行_流量 " + kNetMonitor.m_iTotalMsgSize_UpLoad, GUILayout.Width(150));


//        EditorGUILayout.EndHorizontal();

//        GUI.color = Color.white;

//        EditorGUILayout.Space();

//        kScrollViewPos = GUILayout.BeginScrollView(kScrollViewPos);

//        GUI.color = Color.white;
//        GUILayout.BeginHorizontal("BOX");
//        {
//            GUILayout.Label("下行峰值", GUILayout.Width(150));
//            GUILayout.Label("单秒包数 " + kNetMonitor.m_iMaxMsgCountInPerSec_DownLoad, GUILayout.Width(200));
//            GUILayout.Label("单秒流量 " + kNetMonitor.m_iMaxMsgSizeInPerSec_DownLoad, GUILayout.Width(200));
//            GUILayout.Label("等待发送的队列数量 " + NW_NetworkManager.Instance.GetUnFinishList(), GUILayout.Width(200));
//            GUILayout.Label("等待处理的队列数量 " + NW_HandlerManager.Instance.GetTaskCount(), GUILayout.Width(200));
//        }
//        GUILayout.EndHorizontal();

//        GUI.color = Color.green;
//        GUILayout.BeginHorizontal("BOX");
//        {
//            m_bToggleDownLoadDetail = GUILayout.Toggle(m_bToggleDownLoadDetail, "下行详细", GUILayout.Width(150));
//            GUILayout.Label("URL", GUILayout.Width(400));
//            GUILayout.Label("包总数", GUILayout.Width(100));
//            GUILayout.Label("包总大小", GUILayout.Width(100));
//            //GUILayout.Label("延迟_发送延迟 " , GUILayout.Width(100));
//            GUILayout.Label("延迟_回复延迟 " , GUILayout.Width(100));
//            GUILayout.Label("延迟_平均延迟 " , GUILayout.Width(100));

//        }
//        GUILayout.EndHorizontal();

//        lock (kNetMonitor.m_kMsgRecordSortList_DownLoad) {
//            if (m_bToggleDownLoadDetail && null != kNetMonitor.m_kMsgRecordSortList_DownLoad) {
//                List<NW_NetMonitor.MsgRecord> kSortList = kNetMonitor.m_kMsgRecordSortList_DownLoad;

//                foreach (NW_NetMonitor.MsgRecord kMsgRecord in kSortList) {
//                    GUILayout.BeginHorizontal("BOX");
//                    GUILayout.Label("", GUILayout.Width(150));
//                    GUILayout.Label("" + kMsgRecord.m_url, GUILayout.Width(400));
//                    GUILayout.Label("" + kMsgRecord.m_iMsgCount, GUILayout.Width(100));
//                    GUILayout.Label("" + kMsgRecord.m_iMsgTotalSize, GUILayout.Width(100));
//                    GUILayout.Label("" + kMsgRecord.m_fLastDelay, GUILayout.Width(100));
//                    GUILayout.Label("" + kMsgRecord.m_fAvgDelay, GUILayout.Width(100));
//                    GUILayout.EndHorizontal();
//                }
//            }
//        }

//        GUI.color = Color.white;
//        GUILayout.BeginHorizontal("BOX");
//        {
//            GUILayout.Label("上行峰值", GUILayout.Width(150));
//            GUILayout.Label("单秒包数 " + kNetMonitor.m_iMaxMsgCountInPerSec_UpLoad, GUILayout.Width(200));
//            GUILayout.Label("单秒流量 " + kNetMonitor.m_iMaxMsgSizeInPerSec_UpLoad, GUILayout.Width(200));
//        }
//        GUILayout.EndHorizontal();

//        GUI.color = Color.yellow;
//        GUILayout.BeginHorizontal("BOX");
//        {
//            m_bToggleUpLoadDetail = GUILayout.Toggle(m_bToggleUpLoadDetail, "上行详细", GUILayout.Width(150));
//            GUILayout.Label("URL", GUILayout.Width(600));
//            GUILayout.Label("包总数", GUILayout.Width(100));
//            GUILayout.Label("包总大小", GUILayout.Width(100));
//        }
//        GUILayout.EndHorizontal();

//        lock (kNetMonitor.m_kMsgRecordSortList_UpLoad) {
//            if (m_bToggleUpLoadDetail && null != kNetMonitor.m_kMsgRecordSortList_UpLoad) {
//                List<NW_NetMonitor.MsgRecord> kSortList = kNetMonitor.m_kMsgRecordSortList_UpLoad;

//                foreach (NW_NetMonitor.MsgRecord kMsgRecord in kSortList) {
//                    GUILayout.BeginHorizontal("BOX");
//                    GUILayout.Label("", GUILayout.Width(150));
//                    GUILayout.Label("" + kMsgRecord.m_url, GUILayout.Width(600));
//                    GUILayout.Label("" + kMsgRecord.m_iMsgCount, GUILayout.Width(100));
//                    GUILayout.Label("" + kMsgRecord.m_iMsgTotalSize, GUILayout.Width(100));
//                    GUILayout.EndHorizontal();
//                }
//            }
//        }

//        GUILayout.EndScrollView();

//        Repaint();
//    }
//}
