using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_IPHONE
using UnityEngine.iOS;
#endif

public class Quality 
{
    public static readonly int HIGH_QUALITY     = 400;
    public static readonly int MIDDLE_QUALITY   = 300;
    public static readonly int LOW_QUALITY      = 200;
}

public class PerformanceSettings : MonoBehaviour
{
	public static int SystemMemory = 0;

#region Member Func
    void Awake()
    {
		int quality = GetQualityConfig();
		StartCoroutine(SetScreenResolution(quality));
    }

    public static int GetQualityConfig()
    {
        int quality = PlayerPrefs.GetInt("quality", 0);
        if (quality == 0)
        {
#if UNITY_ANDROID
			quality = _matchCPU_Android ();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
            quality = Quality.HIGH_QUALITY;
#elif UNITY_IPHONE
			quality = _matchCPU_iOS();
#endif
			PlayerPrefs.SetInt("quality", quality);
        }
        return quality;
    }

    public static int _matchCPU_Android()
    {
        int nGPULevel = 0;//0差 1中 2好
        //int nCPULevel = 0;
        int nMemmory = SystemMemory;
        //int nProcessor = SystemInfo.processorCount;
        int nCPUFrequency = SystemInfo.processorFrequency;

        string deviceModel = SystemInfo.deviceModel.ToLower();
        string gpuVendor = SystemInfo.graphicsDeviceVendor.Trim();
        string gpuName = SystemInfo.graphicsDeviceName.Trim();

        if (gpuVendor.Equals("ARM"))
            nGPULevel = gpuName.StartsWith("Mali-G") ? 2 : 0;
        else if (gpuVendor.Equals("Qualcomm"))
        {
            if (gpuName[gpuName.Length - 3] >= '6') //Adreno 6xx 系列
                nGPULevel = 2;
            else if (gpuName[gpuName.Length - 3] >= '5') //Adreno 5xx 系列
                nGPULevel = gpuName[gpuName.Length - 2] >= '4' ? 2 : 0;
            else
                nGPULevel = 0;
        }
        else
            nGPULevel = 0;

        if (nCPUFrequency <= 1400 || nMemmory < 2000/* || bIsHuawei*/) //腾讯TDR标准中配机型Oppo A57、vivo Y66 (骁龙625以下，主频1.4GHz 8核 3GB)
            return Quality.LOW_QUALITY;
        else if (nCPUFrequency > 1400 && nCPUFrequency < 2000)
        {
            if (nGPULevel >= 2) //CPU主频低，但GPU不错
                return Quality.MIDDLE_QUALITY;
            else
                return Quality.LOW_QUALITY;
        }
        else if (nCPUFrequency >= 2000 && nCPUFrequency < 2200)        //腾讯TDR标准中配机型Oppo R9、vivo X9 (骁龙625及以上，主频2.0GHz 8核 4GB)
        {
            return Quality.MIDDLE_QUALITY;
        }
        else if (nCPUFrequency >= 2200)                                //腾讯TDR标准高配机型Oppo R11、vivo X20 (骁龙820及以上，主频2.2GHz 8核 4GB)
        {
            if (nMemmory < 4000 || nGPULevel <= 0)  //CPU主频高，但GPU较差，或内存较小
                return Quality.MIDDLE_QUALITY;

            return Quality.HIGH_QUALITY;
        }

        return Quality.LOW_QUALITY;
        /*if (nMem <= 3000 || nProcessor <= 4 || bIsHuawei)
        {
            return Quality.LOW_QUALITY;
        }
        else 
        {
            return Quality.HIGH_QUALITY;
        }*/
    }
    public static int _matchCPU_iOS()
	{
		//int nMem = SystemMemory;
		int nProcessor = SystemInfo.processorCount;
#if UNITY_IPHONE
		if( Device.generation == DeviceGeneration.iPad2Gen ||
		    Device.generation == DeviceGeneration.iPad3Gen ||
		    Device.generation == DeviceGeneration.iPadMini1Gen ||
		    Device.generation == DeviceGeneration.iPad1Gen ||
		    Device.generation == DeviceGeneration.iPhone4 ||
		    Device.generation == DeviceGeneration.iPhone4S ||
		    Device.generation == DeviceGeneration.iPodTouch4Gen ||
		    Device.generation == DeviceGeneration.iPodTouch5Gen ) 
		{
			return Quality.LOW_QUALITY;
		}
        else if(
            UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone5 ||
		    UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone5C ||
            UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPadMini3Gen)
		{
			return Quality.MIDDLE_QUALITY;
		}
		else 
		{
			return Quality.HIGH_QUALITY;
		}
#endif
        return -1;
	}


	IEnumerator SetScreenResolution(int quality)
    {
        float division = (float)(Screen.width) / (float)(Screen.height);

		#if UNITY_EDITOR
				yield return new WaitForEndOfFrame();
		#else
			#if UNITY_IOS
						if (Screen.height > 900)
						{
							int height = 900;
							int width = (int)(division * height);
							Screen.SetResolution(width, height, true);
						}
			#elif UNITY_STANDALONE_WIN
						Screen.SetResolution(750, 1334, false);
			#else
					if (quality == Quality.LOW_QUALITY) {
						int cwidth = (int)(Screen.width);
						int cheight = (int)(Screen.height);

						if (cheight > 750) {
							int height = 750;
							int width = (int)(division * height);
							Screen.SetResolution(width, height, true);

						}
						else {
							Screen.SetResolution(cwidth, cheight, true);
						}
					}
					else if (Screen.height > 900) {
						int height = 900;
						int width = (int)(division * height);
						Screen.SetResolution(width, height, true);
					}
			#endif
			yield return new WaitForEndOfFrame();

		#endif
    }

#endregion Member Func
}
