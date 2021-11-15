using Spenve;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Xml;
using DG.Tweening;
using UnityEngine.SceneManagement;

namespace Spenve
{
    public class AudioSystem : SystemSingleton<AudioSystem>
    {
        Dictionary<string, SoundInfo> allBgm = new Dictionary<string, SoundInfo>();
        Dictionary<string, SoundInfo> allSfx = new Dictionary<string, SoundInfo>();

        AudioSource bgmAudioSource;

        List<AudioSource> sfxAudioSource = new List<AudioSource>();

        bool bgmEnabled = true;
        bool sfxEnabled = true;
        bool isRestartOrReturn = false;
        float volume = 1.0f;


        private AudioListener listener;
        protected override void Awake()
        {
            base.Awake();

        }

        private void Start()
        {
            SceneManager.sceneLoaded += ClearAudioListener;
            InitAudioListener();
        }

        private void InitAudioListener()
        {
            ClearAudioListener(default(Scene), LoadSceneMode.Single);
            listener = new GameObject("audioLister", typeof(AudioListener)).GetComponent<AudioListener>();
            listener.transform.parent = transform;
            listener.transform.localPosition = Vector3.zero;
        }

        private void ClearAudioListener(Scene se,LoadSceneMode mode)
        {
            AudioListener[] listeners = GameObject.FindObjectsOfType<AudioListener>();
            for (int i = 0; i < listeners.Length; i++)
            {
               if(listeners[i] != listener)
                   Destroy(listeners[i]);
            }
        }

        public void InitData()
        {
            AudioData audioConfig = ResourceSystem.Instance.LoadAsset<AudioData>("AudioConfig");

            foreach (SoundInfo si in audioConfig.bgmList)
            {
                allBgm.Add(si.name, si);
            }

            foreach (SoundInfo si in audioConfig.sfxList)
            {
                allSfx.Add(si.name, si);
            }
        }


        void OnGUI()
        {
            //if (GUI.Button(new Rect(0, 0, 130, 30), "Play ARENA"))
            //{
            //    PlayBgm("ARENA");
            //}
            //if (GUI.Button(new Rect(150, 0, 130, 30), "Stop Bgm"))
            //{
            //    StopBgm();
            //}

            //if (GUI.Button(new Rect(300, 0, 130, 30), "Pause Bgm"))
            //{
            //    PauseBgm();
            //}

            //if (GUI.Button(new Rect(450, 0, 130, 30), "Unpause Bgm"))
            //{
            //    UnpauseBgm();
            //}

            //if (GUI.Button(new Rect(0, 60, 130, 30), "Play heartfight"))
            //{
            //    PlayBgm("heartfight");
            //}
            //if (GUI.Button(new Rect(0, 120, 130, 30), "PlayBGM Random"))
            //{
            //    PlayBgmRandom();
            //}

            //if (GUI.Button(new Rect(0, 180, 130, 30), "PlaySfx - Single Delay"))
            //{
            //    PlaySfxDelay("yell_01", 1);
            //}

            //if (GUI.Button(new Rect(150, 180, 130, 30), "StopSfx - Single"))
            //{
            //    StopSfx("yell_01");
            //}

            //if (GUI.Button(new Rect(0, 240, 130, 30), "PlaySfx - Loop"))
            //{
            //    PlaySfx("spell_buy");
            //}

            //if (GUI.Button(new Rect(150, 240, 130, 30), "StopSfx - Loop"))
            //{
            //    StopSfx("spell_buy");
            //}

            //if (GUI.Button(new Rect(0, 300, 150, 30), "PlaySfx-In_Loop_Out"))
            //{
            //    PlaySfx("gun_intro");
            //}
            //if (GUI.Button(new Rect(150, 300, 150, 30), "StopSfx-In_Loop_Out"))
            //{
            //    StopSfx("gun_intro");
            //}

            //bool cb = GUI.Toggle(new Rect(0, 360, 150, 30), bgmEnabled, "BGM");
            //if (bgmEnabled != cb)
            //{
            //    EnableBgm(cb);
            //}

            //bool cs = GUI.Toggle(new Rect(150, 360, 150, 30), sfxEnabled, "SFX");
            //if (sfxEnabled != cs)
            //{
            //    EnableSfx(cs);
            //}
        }

        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
            bgmAudioSource.volume = volume;

            for (int i = 0; i < sfxAudioSource.Count; i++)
            {
                sfxAudioSource[i].volume = volume;
            }
        }

        public bool GetBgmState()
        {
            return bgmEnabled;
        }

        public void EnableBgm(bool state)
        {
            bgmEnabled = state;
            if( bgmEnabled )
            {
                bgmAudioSource.UnPause();
            }
            else
            {
                bgmAudioSource.Pause();             
            }
        }

        public void EnableSfx(bool state)
        {
            sfxEnabled = state;
            if( !sfxEnabled )
            {
                for (int i = 0; i < sfxAudioSource.Count; i++)
                {
                    sfxAudioSource[i].Stop();
                }
            }
        }

        public void PlayBgmRandom()
        {
            string[] randomName = allBgm.Keys.ToArray<string>();
            PlayBgm(randomName[UnityEngine.Random.Range(0, randomName.Length)]);
        }

        public void PlayBgm(string name, bool loop = true)
        {
            SoundInfo si;
            if (allBgm.TryGetValue(name, out si))
            {
                bgmAudioSource = GetBgmAudioSource(name);
                bgmAudioSource.loop = loop;
                bgmAudioSource.volume = 0;
                DOTween.To(() => bgmAudioSource.volume, x => bgmAudioSource.volume = x, si.volume, 1.0f).SetEase(Ease.OutQuad);
                bgmAudioSource.Play();
            }

            if(!bgmEnabled)
            {
                bgmAudioSource.Pause();
            }
        }

        //更新资源的时候播放BGM
        public void PlayBGMUpdate(string path)
        {
            if (bgmAudioSource == null)
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
            AudioClip ac = null;//ResourceManager.Instance.LoadAsset<AudioClip>(path);
            bgmAudioSource.clip = ac;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = 0;
            DOTween.To(() => bgmAudioSource.volume, x => bgmAudioSource.volume = x, 1, 1.0f).SetEase(Ease.OutQuad);
            bgmAudioSource.Play();
        }



        public void PauseBgm()
        {
            bgmAudioSource.Pause();
        }

        public void UnpauseBgm()
        {
            bgmAudioSource.UnPause();
        }

        public void StopBgm()
        {
            bgmAudioSource.Stop();
        }

        public void PlaySfxLoop(string name)
        {
            if (!sfxEnabled)
                return;
            _PlaySfx(name,true);
        }

        public void PlaySfx(string name)
        {
            if (!sfxEnabled)
                return;
            _PlaySfx(name);
        }

        /// <summary>
        /// 用于3D音效
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pos"></param>
        public void PlaySfx(string name , Vector3 pos)
        {
            if(!sfxEnabled)
                return;
           _PlaySfx(name,pos);
        }

        public void PlaySfxDelay(string name, float time)
        {
            if (!sfxEnabled)
                return;
            SetRestartOrReturn(false);
            StartCoroutine(_PlayDelay(name, time));
        }

        IEnumerator _PlayDelay(string name, float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            if (!isRestartOrReturn)
                _PlaySfx(name);  
        }

        public void SetRestartOrReturn(bool val)
        {
            isRestartOrReturn = val;
        }

        public void StopSfx(string name, bool stopAll = false)
        {
            if (sfxEnabled)
            {
                for (int i = 0; i < sfxAudioSource.Count; i++)
                {
                    if (sfxAudioSource[i].clip != null && sfxAudioSource[i].clip.name.Equals(name) && sfxAudioSource[i].isPlaying)
                    {
                        sfxAudioSource[i].Stop();
                        SoundInfo si;
                        if (allSfx.TryGetValue(name, out si))
                        {
                            if (si.type == PlayType.InLoopOut)
                            {
                                StopSfx(si.loopClipName);
                                _PlaySfx(si.outClipName);
                            }
                        }

                        if(!stopAll)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void _PlaySfx(string name, bool forceLoop = false )
        {
            if (!sfxEnabled) {
                return;
            }

            SoundInfo si;
            if (allSfx.TryGetValue(name, out si))
            {
                string fn = GlobalFunc.GetFileName(si.path);
                AudioSource sfxAS = null;
                if(si.type == PlayType.SingTon)
                {
                    sfxAS = ExistSfx(name);
                }

                if (sfxAS == null)
                {
                    sfxAS = GetSfxAudioSource(GlobalFunc.GetFileNameWithoutExtend(fn));
                    if(sfxAS == null)
                    {
                        return;
                    }
                    AudioClip ac = ResourceSystem.Instance.LoadAsset<AudioClip>(name);
                    sfxAS.clip = ac;
                    sfxAS.volume = si.volume * volume;
                }
                sfxAS.spatialBlend = 0;
                sfxAS.Play();
                if (forceLoop || si.type == PlayType.Loop || si.type == PlayType.SingTon )
                {
                    sfxAS.loop = true;
                }
                else if( si.type == PlayType.Single)
                {
                    sfxAS.loop = false;
                }
                else if(si.type == PlayType.InLoopOut)
                {
                    sfxAS.loop = false;
                    StartCoroutine(PlayOver(sfxAS.clip.length, si));
                }
            }
            else
            {
                //Debug.Log("音效" + name + "不存在！");
            }
        }

        private void _PlaySfx(string name, Vector3 pos , bool forceLoop = false)
        {
            SoundInfo si;
            if (allSfx.TryGetValue(name, out si))
            {
                string fn = GlobalFunc.GetFileName(si.path);
                AudioSource sfxAS = null;
                if (si.type == PlayType.SingTon)
                {
                    sfxAS = ExistSfx(name);
                }

                if (sfxAS == null)
                {
                    sfxAS = GetSfxAudioSource(GlobalFunc.GetFileNameWithoutExtend(fn));
                    if (sfxAS == null)
                    {
                        return;
                    }
                    AudioClip ac = ResourceSystem.Instance.LoadAsset<AudioClip>(fn);
                    sfxAS.clip = ac;
                    sfxAS.volume = si.volume * volume;
                }

                sfxAS.spatialBlend = 1;
                sfxAS.rolloffMode = AudioRolloffMode.Linear;
                sfxAS.maxDistance = 16;

                sfxAS.transform.position = pos;
                sfxAS.Play();
                if (forceLoop || si.type == PlayType.Loop || si.type == PlayType.SingTon)
                {
                    sfxAS.loop = true;
                }
                else if (si.type == PlayType.Single)
                {
                    sfxAS.loop = false;
                }
                else if (si.type == PlayType.InLoopOut)
                {
                    sfxAS.loop = false;
                    StartCoroutine(PlayOver(sfxAS.clip.length,  si , pos));
                }
            }
            else
            {
                //Debug.Log("音效" + name + "不存在！");
            }
        }

        private AudioSource GetSfxAudioSource()
        {
            for(int i = 0; i < sfxAudioSource.Count; i++ )
            {
                if( !sfxAudioSource[i].isPlaying )
                {
                    return sfxAudioSource[i];
                }
            }

            GameObject obj = new GameObject("audiosource_" + sfxAudioSource.Count , typeof(AudioSource));
            AudioSource a = obj.GetComponent<AudioSource>();
            obj.transform.parent = gameObject.transform;

            sfxAudioSource.Add(a);
            return a;
        }

        private AudioSource GetSfxAudioSource(string name)
        {   
            AudioSource a = null;
            float maxt = -1;
            int count = 0;
            for (int i = 0; i < sfxAudioSource.Count; i++)
            {
                if (sfxAudioSource[i].isPlaying && sfxAudioSource[i].clip.name == name)
                {
                    if (sfxAudioSource[i].time < 0.2f)//如果有相同的音效在0.2秒以内播放过，则不播放
                    {
                        return null;
                    }

                    count++;
                    if( sfxAudioSource[i].time > maxt )
                    {
                        maxt = sfxAudioSource[i].time;
                        a = sfxAudioSource[i];
                    }
                }
            }

            if( count >= 3 )
            {
                return a;
            }

            for (int i = 0; i < sfxAudioSource.Count; i++)
            {
                if (!sfxAudioSource[i].isPlaying)
                {
                    return sfxAudioSource[i];
                }
            }

            //a = gameObject.AddComponent<AudioSource>();
            //sfxAudioSource.Add(a);

            GameObject obj = new GameObject("audiosource_" + sfxAudioSource.Count, typeof(AudioSource));
            a = obj.GetComponent<AudioSource>();
            obj.transform.parent = gameObject.transform;
            sfxAudioSource.Add(a);

            return a;
        }

        public AudioSource GetBgmAudioSource(string name)
        {
            if(bgmAudioSource == null)
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
            string ab = "bgm_" + name.ToLower();
            AudioClip ac = ResourceSystem.Instance.LoadAsset<AudioClip>(name);
            bgmAudioSource.clip = ac;

            return bgmAudioSource;
        }

        private AudioSource ExistSfx(string name)
        {
            for (int i = 0; i < sfxAudioSource.Count; i++)
            {
                if (name.StartsWith( sfxAudioSource[i].clip.name ))
                {
                    return sfxAudioSource[i];
                }
            }
            return null;
        }
        
        IEnumerator PlayOver(float delayTime, SoundInfo si)
        {
            yield return new WaitForSeconds(delayTime);
            _PlaySfx(si.loopClipName, true);
        }

        IEnumerator PlayOver(float delayTime ,SoundInfo si , Vector3 pos)
        {
            yield return new WaitForSeconds(delayTime);
            _PlaySfx(si.loopClipName, pos , true);
        }
        public static PlayType GetPlayType(string str)
        {
            if (PlayType.Loop.ToString() == str)
            {
                return PlayType.Loop;
            }
            else if (PlayType.InLoopOut.ToString() == str)
            {
                return PlayType.InLoopOut;
            }
            else if (PlayType.Single.ToString() == str)
            {
                return PlayType.Single;
            }
            else if (PlayType.SingTon.ToString() == str)
            {
                return PlayType.SingTon;
            }
            return PlayType.Loop;
        }
        public void Clear()
        {
            for (int i = 0; i < sfxAudioSource.Count; i++)
            {
                Destroy(sfxAudioSource[i].gameObject);
            }

            sfxAudioSource.Clear();
        }
        public void SetAudioListener(Transform parent)
        {
            if(listener == null)
            {
              InitAudioListener();   
            }

            listener.transform.parent = parent;
            listener.transform.localPosition = Vector3.zero;
        }

        public void BreakAudioListener()
        {
            if(listener == null)
            {
               InitAudioListener();
            }

            listener.transform.parent = transform;
            listener.transform.localPosition = Vector3.zero;
        }

        public void PreLoadBgm(string name)
        {

            string ab = "bgm_" + name.ToLower();
            //ResourceSystem.Instance.LoadAssetAsync(ab, name, (ac)=>{

                           SoundInfo si;
                           if (allBgm.TryGetValue(name, out si))
                           {
                               if(bgmAudioSource == null)
                               bgmAudioSource = gameObject.AddComponent<AudioSource>();

                               AudioClip ac = ResourceSystem.Instance.LoadAsset<AudioClip>(name);

                               bgmAudioSource.clip = ac; 
                           }
            //});
       }


      public void PreLoadSfx(string name)
      {
            for (int i = 0; i < sfxAudioSource.Count; i++)
			{
			   if(sfxAudioSource[i].clip.name == name)
                   return;
			}

            //ResourceSystem.Instance.LoadAssetAsync("sfx", name, (ac)=>{

                           SoundInfo si;
                           if (allSfx.TryGetValue(name, out si))
                           {
                               AudioSource tmpSource;
                               GameObject obj = new GameObject("audiosource_" + sfxAudioSource.Count, typeof(AudioSource));
                               tmpSource = obj.GetComponent<AudioSource>();
                               obj.transform.parent = gameObject.transform;
                              AudioClip ac = ResourceSystem.Instance.LoadAsset<AudioClip>(name);
                               tmpSource.clip = ac;    
                               sfxAudioSource.Add(tmpSource);
                           }
            //});
      }
    }
}

