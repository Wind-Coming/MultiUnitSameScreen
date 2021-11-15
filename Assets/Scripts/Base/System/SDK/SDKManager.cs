using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Spenve;
public class SDKManager : SystemSingleton<SDKManager>
{

    private  AndroidJavaClass _sdk = null; 
    protected  AndroidJavaClass sdk
    {
        get
        {
            if (_sdk != null)
                return _sdk;

            //using (_sdk = new AndroidJavaClass(GameConfig.SdkClass))
            {
                return _sdk;
            }
        }
    }

    private  ISDKInterface  _sdkInstance = null;
    public  ISDKInterface   sdkInstance
    {
        get{
            if(_sdkInstance != null)
                return _sdkInstance;
            else
            {
              SubInit();
              return _sdkInstance;
            }
        }
    }

    public void SubInit()
    {
       Debug.Log("SDKManager inited !");
       //if(  GameConfig.SdkClass  == "koo")
       //    _sdkInstance = new KooSdkManager();
    }

    public void CallMethod(string methodName, params object[] args)
    {
        sdk.Call(methodName, args);
    }
    public ReturnType CallMethod<ReturnType>(string methodName, params object[] args)
    {
        return sdk.Call<ReturnType>(methodName, args);
    }

    public void CallStaticMethod(string methodName, params object[] args)
    {
        sdk.CallStatic(methodName, args);
    }
    public ReturnType CallStaticMethod<ReturnType>(string methodName, params object[] args)
    {
        return sdk.CallStatic<ReturnType>(methodName, args);
    }
}
