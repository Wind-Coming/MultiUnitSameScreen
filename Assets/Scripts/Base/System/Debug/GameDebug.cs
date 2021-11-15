using UnityEngine;
using System.Collections;
using Spenve;
using System.Collections.Generic;
using System.IO;
using System;

namespace Spenve
{
    [SerializeField]
    public enum GameLogType
    {
        LOG_NONE = 0,
        LOG_CORE = 1,
        LOG_RES = 2,
    }

    public class GLog
    {
        private static StreamWriter fileWriter = null;

        private static HashSet<GameLogType> logStates = new HashSet<GameLogType>();
        public static void Create( bool toFile ) 
        {
            if (toFile)
            {
                string fileName = Application.persistentDataPath + "\\" + DateTime.UtcNow.ToFileTimeUtc().ToString() + ".log";
                fileWriter = new StreamWriter(File.Open(fileName, FileMode.Create));

                Application.logMessageReceived += LogToFile;
            }
        }

        static void LogToFile(string con, string sta, LogType type)
        {
            string msg = string.Format("{0}:{1}",type.ToString(), con);

            if (fileWriter != null)
            {
                fileWriter.WriteLine(msg);
                fileWriter.Flush();

                //change to next file
                if (fileWriter.BaseStream.Length >= 1024 * 1024)
                {
                    fileWriter.Close();
                    fileWriter.Dispose();

                    string fileName = Application.persistentDataPath + "\\log\\" + DateTime.UtcNow.ToFileTimeUtc().ToString() + ".log";
                    fileWriter = new StreamWriter(File.Open(fileName, FileMode.Create));
                }
            }
        }

        public static void ChangeLogState(GameLogType t, bool enable ) 
        {
            if (enable)
            {
                logStates.Add(t);
            }
            else
            {
                logStates.Remove(t);
            }
        }


        public static void Dispose()
        {
            if (fileWriter != null)
            {
                fileWriter.Flush();
                fileWriter.Close();
                fileWriter.Dispose();
                fileWriter = null;
            }

            Application.logMessageReceived -= LogToFile;
        }

        public static void Log(string t, GameLogType type)
        {
            if( logStates.Contains(type) )
                Debug.Log(t);
        }

        public static void Error(string t, GameLogType type)
        {
            Debug.LogError(t);
        }

        public static void Warning(string t, GameLogType type)
        {
            if (logStates.Contains(type))
                Debug.Log(t);
        }
}


    class FPSHandler
    {
        public float fpsMeasuringDelta = 2.0f;

        private float timePassed;
        private int m_FrameCount = 0;
        private float m_FPS = 0.0f;

        public FPSHandler()
        {
            timePassed = 0.0f;
        }

        public void Update()
        {
            m_FrameCount = m_FrameCount + 1;
            timePassed = timePassed + Time.deltaTime;

            if (timePassed > fpsMeasuringDelta)
            {
                m_FPS = m_FrameCount / timePassed;

                timePassed = 0.0f;
                m_FrameCount = 0;
            }
        }

        public void OnGUI()
        {
            GUIStyle bb = new GUIStyle();
            bb.normal.background = null;    //这是设置背景填充的
            bb.normal.textColor = new Color(1.0f, 0.5f, 0.0f);   //设置字体颜色的
            bb.fontSize = 40;       //当然，这是字体大小

            //居中显示FPS
            GUI.Label(new Rect((Screen.width / 2) - 40, 0, 200, 200), "FPS: " + m_FPS, bb);
        }

    }


    public class GameDebug : MonoBehaviour
    {
        public bool logToFile = false;

        public bool showLog = false;

        public bool showFPS = false;

        public bool bNeedTutorial = true;

        public List<GameLogType> openedLog = new List<GameLogType>();


        static string logText = "";

        private FPSHandler fps;


        void Awake()
        {
#if DEBUG || GLOG
            Application.logMessageReceived += Log;

            GameObject.DontDestroyOnLoad(this.gameObject);

            GLog.Create(logToFile);

            for (int i = 0; i < openedLog.Count; ++i )
            {
                if (openedLog[i] != GameLogType.LOG_NONE)
                {
                    GLog.ChangeLogState(openedLog[i], true);
                }
            }
#endif
#if DEBUG || FPS
            if (showFPS)
            {
                this.fps = new FPSHandler();
            }
#endif
        }

        void OnDestroy() 
        {
            GLog.Dispose();

            this.fps = null;
        }

        void Log(string con, string sta, LogType type)
        {
            if (showLog)
            {
                string msg = string.Format("{0}\n", con);
                logText = msg + logText;
                if (logText.Length > 2048)
                {
                    logText = logText.Substring(0, 2048);
                }
            }
        }

        void Update() 
        {
            if (showFPS&&fps != null)
            {
                fps.Update();
            }

            if(Input.GetKeyDown(KeyCode.Home))
            {
                PlayerPrefs.DeleteAll();
            }
        }

        void OnGUI()
        {
            if (showLog)
            {
                Color old = GUI.color;

                GUI.color = Color.red;
                GUI.Label(new Rect(0, 0, Screen.width/2, Screen.height), logText);
                GUI.color = old;
            }

            if (showFPS && fps != null)
            {
                fps.OnGUI();
            }
        }


        public static void Log(params object[] args)
        {
            string log = "";
            for(int i = 0; i < args.Length; i++) {
                log += args[i].ToString() + "    ";
            }
            Debug.Log(log);
        }
        
    }

}


