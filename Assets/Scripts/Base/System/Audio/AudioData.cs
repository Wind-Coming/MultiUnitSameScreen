using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Spenve
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/CreateAudioConfigData", order = 2)]
    public class AudioData : ScriptableObject
    {
        public List<SoundInfo> bgmList = new List<SoundInfo>();
        public List<SoundInfo> sfxList = new List<SoundInfo>();
    }

    [Serializable]
    public class SoundInfo
    {
        public string name;
        public string path;
        [NonSerialized]
        public AudioClip clip;
        [Range(0, 1)]
        public float volume;
        public PlayType type;
        [NonSerialized]
        public bool editorPlaying = false;

        public string loopClipName;
        public string loopClipPath;
        [NonSerialized]
        public AudioClip loopClip;
        public string outClipName;
        public string outClipPath;
        [NonSerialized]
        public AudioClip outClip;
    }
    public enum PlayType
    {
        Single = 0,
        Loop,
        InLoopOut,
        SingTon,
    }
}
