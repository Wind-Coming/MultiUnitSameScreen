using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class BatteryAndNetManger : MonoBehaviour
{

    /// <summary>
    /// 电池状态枚举
    /// </summary>
    public enum BatteryState
    {
        Unknown = 0,//电池的状态未知
        Charging = 1,//电池正在充电
        Unplugged = 2,//电池未充电
        StateFull = 3,//电池电量充满
    }

    public class NETVo
    {
        public bool isWifi = true;
        /// <summary>
        /// 0-5
        /// </summary>
        public int strength = 5;
    }

    /// <summary>
    /// 默认的更新时长
    /// </summary>
    public const float UPDATETIME = 2.0f;

    #region 实现单例模式
    private static BatteryAndNetManger _instance;

    public static BatteryAndNetManger Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject tmp = new GameObject("BatteryAndNetManger");
                _instance = tmp.AddComponent<BatteryAndNetManger>();
            }
            return _instance;
        }
    }
    #endregion

    /// <summary>
    /// 返回值为0到100监听
    /// </summary>
    public Action<int> batteryLevelFun;
    /// <summary>
    /// 返回电池状态监听
    /// </summary>
    public Action<BatteryState> batteryStateFun;
    /// <summary>
    /// 网络状态的返回监听
    /// </summary>
    public Action<bool> netStateFun;

    private float updateTime = 1.0f;
    private NETVo netState;
    private BatteryState batteryState;
    private int batteryLevel = 100;
    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
        updateTime = 0;
        netState = new NETVo();
        batteryState = BatteryState.Unknown;
    }
#if UNITY_IPHONE
    //[DllImport("__Internal")]
    //private static extern float getBatteryLevel();

    //[DllImport("__Internal")]
    //private static extern int getBatteryStateInt();

    //private BatteryState getBatteryState()
    //{
    //    BatteryState retstate = BatteryState.Unknown;
    //    try
    //    {
    //        int sint = getBatteryStateInt();
    //        Debug.Log("IOS电池状态："+ sint);
    //        if (sint == 1)
    //        {
    //            retstate = BatteryState.Charging;
    //        }
    //        else if (sint == 2)
    //        {
    //            retstate = BatteryState.StateFull;
    //        }
    //        else
    //        {
    //            retstate = BatteryState.Unplugged;
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.Log("Failed to read battery power; " + e.Message);
    //    }
    //    return retstate;
    //}
    
#elif UNITY_STANDALONE || UNITY_EDITOR
    private float getBatteryLevel()
    {
        return 1.0f;
    }
    private BatteryState getBatteryState()
    {
        return BatteryState.Unknown;
    }
#elif UNITY_ANDROID
    private float getBatteryLevel()
    {
        try  
        {  
           string CapacityString = System.IO.File.ReadAllText("/sys/class/power_supply/battery/capacity");
           CapacityString = CapacityString.Trim();
           Debug.Log("电池电量："+CapacityString);
           return int.Parse(CapacityString)/100.0f;  
        }  
        catch (Exception e)  
        {  
            Debug.Log("Failed to read battery power; " + e.Message);  
        }  
        return 1.0f;
    }

    private BatteryState getBatteryState()
    {
        BatteryState retstate = BatteryState.Unknown;
        try
        {
            //电池状态，"Discharging","Charging","Notcharging","Full","Unknown"
            string StateString = System.IO.File.ReadAllText("/sys/class/power_supply/battery/status");
            Debug.Log("电池状态："+ StateString);
            if (StateString.IndexOf("Charging") != -1)
            {
                retstate = BatteryState.Charging;
            }
            else if (StateString.IndexOf("Full") != -1)
            {
                retstate = BatteryState.StateFull;
            }
            else
            {
                retstate = BatteryState.Unplugged;
            }
        }
        catch (Exception e)
        {
            Debug.Log("Failed to read battery power; " + e.Message);
        }
        return retstate;
    }
#endif
    private void FixedUpdate()
    {
        //updateTime -= Time.fixedDeltaTime;
        //if (updateTime < 0)
        //{
        //    updateTime = UPDATETIME;
        //    //取得网络状态  强度需要用无阻塞心跳判断，暂时全部为5
        //    NetworkReachability netstate = Application.internetReachability;
        //    if (netstate == NetworkReachability.ReachableViaCarrierDataNetwork)
        //    {
        //        netState.isWifi = false;
        //    }
        //    else if (netstate == NetworkReachability.ReachableViaLocalAreaNetwork)
        //    {
        //        netState.isWifi = true;
        //    }
        //    else
        //    {
        //        //没有联网或网络监测出现问题
        //        netState.isWifi = false;
        //    }

        //    batteryState = getBatteryState();
        //    batteryLevel = (int)(getBatteryLevel() * 100);


        //    if (batteryLevelFun != null)
        //    {
        //        batteryLevelFun(batteryLevel);
        //    }

        //    if (batteryStateFun != null)
        //    {
        //        batteryStateFun(batteryState);
        //    }

        //    if (netStateFun != null)
        //    {
        //        netStateFun(netState.isWifi);
        //    }
        //}
    }

    /// <summary>
    /// 设置信号强度
    /// </summary>
    public void setNetStrength(int strength)
    {
        if (strength < 1)
        {
            strength = 1;
        }

        if (strength > 5)
        {
            strength = 5;
        }
        if (netState != null)
        {
            netState.strength = strength;
        }
    }

}