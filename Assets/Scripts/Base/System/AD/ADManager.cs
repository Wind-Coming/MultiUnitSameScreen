using Spenve;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UnityAd
using UnityEngine.Advertisements;

namespace Spenve
{
    public class ADManager : ScriptSingleton<ADManager>, ISystem
    {
        public const int SUCCESS = 0;
        public const int SKIP = 1;
        public const int FAILED = 2;
        public const int NETWROK_FAILED = -100;
        public const int NOT_INITED = -101;
        public const int REWARD_NOT_AREADY = -102;
        //public const int READY = 0xFF;

        private Action<int> callback;

        public void ShowAD(string zoneId, Action<int> callback)
        {
            this.callback = callback;

            if(Application.internetReachability == NetworkReachability.NotReachable )
            {
                OnCallback(NETWROK_FAILED);
                return;
            }

            if(!Advertisement.isInitialized)
            {
                OnCallback(NOT_INITED);
                return;
            }
            if (!Advertisement.IsReady(zoneId))
            {
                OnCallback(REWARD_NOT_AREADY);
                return;
            }
            ShowOptions op = new ShowOptions();
            op.resultCallback = this.OnReward;
            Advertisement.Show(zoneId, op);
        }

        public void ShowSimpleAD(Action<int> callback)
        {
            this.callback = callback;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                OnCallback(NETWROK_FAILED);
                return;
            }

            if (!Advertisement.isInitialized)
            {
                OnCallback(NOT_INITED);
                return;
            }
            if (!Advertisement.IsReady())
            {
                OnCallback(REWARD_NOT_AREADY);
                return;
            }

            Advertisement.Show();

            OnReward(ShowResult.Finished);
        }

        public int CanShowAD(string zoneId)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return NETWROK_FAILED;
            }

            if (!Advertisement.isInitialized)
            {
                return NOT_INITED;
            }
            if (!Advertisement.IsReady(zoneId))
            {
                return REWARD_NOT_AREADY;
            }

            return SUCCESS;
        }

        public int CanShowNormalAD()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return NETWROK_FAILED;
            }

            if (!Advertisement.isInitialized)
            {
                return NOT_INITED;
            }
            if (!Advertisement.IsReady())
            {
                return NETWROK_FAILED;
            }

            return SUCCESS;
        }


        private void OnReward(ShowResult result)
        {
            switch (result)
            {
                case ShowResult.Finished:
                    Debug.Log("The ad was successfully shown.");
                    OnCallback(SUCCESS);
                    break;
                case ShowResult.Skipped:
                    OnCallback(SKIP);
                    break;
                case ShowResult.Failed:
                    OnCallback(FAILED);
                    break;
            }
        }

        private void OnCallback(int result)
        {
            if(callback != null)
                callback.Invoke(result);

            callback = null;
        }

        [NoToLua]
        public void Reset()
        {
            
        }

        [NoToLua]
        public void Load()
        {
           
        }
        
        [NoToLua]
        public void Launch()
        {
            
        }

        [NoToLua]
        public void Dispose()
        {
            
        }
    }
}


#else

namespace Spenve
{
    public class ADManager : SingletonAutoCreate<ADManager>
    {
        public const int SUCCESS = (int)2;
        public const int SKIP = (int)1;
        public const int FAILED = (int)0;
        public const int NETWROK_FAILED = -1;
        public const int NOT_INITED = -2;
        public const int REWARD_NOT_AREADY = -3;

        public void ShowAD(string zoneId, Action<int> callback)
        {
            if(callback != null)
                callback.Invoke(SUCCESS);
        }
		public int CanShowAD(string zoneId)
		{
			return SUCCESS;
		}
		public void ShowSimpleAD(Action<int> callback)
		{
			if(callback != null)
                callback.Invoke(SUCCESS);
		}
		public int CanShowNormalAD()
		{
			return SUCCESS;
		}
    }
}

#endif