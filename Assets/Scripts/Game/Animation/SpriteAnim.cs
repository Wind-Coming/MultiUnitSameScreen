// using System;
// using UnityEngine;
// using DG.Tweening;
// using System.Collections.Generic;

public enum SpriteAnimation
{
    idle,
    walk,
    attack
}

// [Serializable]
// public class NameToClip
// {    
//     public SpriteAnimation name;
//     public SpriteClip clip;
// }

// [RequireComponent(typeof(SpriteRenderer))]
// public class SpriteAnim : MonoBehaviour
// {
//     public NameToClip[] m_kClips = new NameToClip[4];
//     public Dictionary<SpriteAnimation, SpriteClip> m_kClipsDic = new Dictionary<SpriteAnimation, SpriteClip>();

//     [HideInInspector]
//     public SpriteAnimation m_kAnimState;//动画状态

//     public int m_kAngleInteval = 45;
//     public bool m_bUseFilp = true;
//     public int m_iUpdateFrameNum = 2;//更次更新帧的数字

//     public int m_iFaceAngle;//角度

//     [HideInInspector]
//     public int m_iAnimIndex = 0;

//     public float m_fTickTime = 0.0666f;
    
//     [HideInInspector]
//     public Vector3[] m_firePoints = new Vector3[8]; 

//     [HideInInspector]
//     public SpriteRenderer[] m_kSpriteRenderers;
//     private Material m_orgMaterial;

//     private float m_fUpdateTime = 0;

//     private int m_bCamp = 1;// 1在地图的下方, 2是上方，在上方的话动画需要旋转180

//     private List<SpriteAnimation> m_AnimQueue = new List<SpriteAnimation>();//播放队列

//     private bool hasUpdated = false;

//     private int m_initLayerCount = 1;

//     private int m_logicLayerCount = 1;

//     private Vector3 m_currenDir;

//     #region PublicFunc

//     void Awake()
//     {
//         Init(m_kAnimState, m_iFaceAngle);
//     }

//     public void Init(SpriteAnimation anim, int unit_angle)
//     {
//         for(int i = 0; i < m_kClips.Length; i++)
//         {
//             if(!m_kClipsDic.ContainsKey(m_kClips[i].name)){
//                 m_kClipsDic.Add(m_kClips[i].name, m_kClips[i].clip);
//             }
//             else
//             {
//                 m_kClipsDic[m_kClips[i].name] = m_kClips[i].clip;
//             }

//             if(m_kClips[i].clip != null)
//             {
//                 m_initLayerCount = Mathf.Max( m_initLayerCount, m_kClips[i].clip.layerCount);
//                 m_logicLayerCount = m_initLayerCount;
//             }
//         }

//         if(m_kSpriteRenderers == null || m_kSpriteRenderers.Length < m_initLayerCount){
//             m_kSpriteRenderers = new SpriteRenderer[m_initLayerCount];
//         }

//         if(m_kSpriteRenderers[0] == null)
//             m_kSpriteRenderers[0] = GetComponent<SpriteRenderer>();

//         m_orgMaterial = m_kSpriteRenderers[0].sharedMaterial;

//         Play(anim, true);
//         SetAngle( unit_angle );

//         UpdateSprite();
//     }

//     public void SetCamp(int camp)
//     {
//         m_bCamp = camp;
//     }

//     public Vector3 GetFirePoint()
//     {
//         int angel = GetDisplayAngle(m_iFaceAngle, m_kAngleInteval);
//         for(int i = 0; i < m_firePoints.Length; i++)
//         {
//             if(angel == i * m_kAngleInteval)
//             {
//                 return m_firePoints[i] + transform.position;
//             }
//         }
//         return transform.position;
//     }

//     public int GetCurAngle()
//     {
//         return m_iFaceAngle;
//     }

//     public void Play(SpriteAnimation anim, bool forceUpdate = false)
//     {
//         if (forceUpdate || m_kAnimState != anim)
//         {
//             m_kAnimState = anim;
            
//             ResetRender();

//             m_iAnimIndex = 0;
//         }
//         m_AnimQueue.Clear();
//     }

//     private void ResetRender()
//     {
//         int lcount = Mathf.Min( GetLayerCount(), GetClipLayerCount(m_kAnimState));
//         for (int i = 0; i < m_initLayerCount; i++)
//         {
//             if (i < lcount)
//             {
//                 if (m_kSpriteRenderers[i] == null)
//                 {
//                     GameObject go = new GameObject("lay" + i);
//                     go.transform.parent = transform;
//                     go.transform.localPosition = new Vector3(0, 0, -0.2f * i);
//                     go.transform.localRotation = Quaternion.identity;
//                     go.transform.localScale = Vector3.one;
//                     m_kSpriteRenderers[i] = go.AddComponent<SpriteRenderer>();
//                     m_kSpriteRenderers[i].material = m_kSpriteRenderers[0].sharedMaterial;
//                     m_kSpriteRenderers[i].color = Color.black;
//                 }
//                 else
//                 {
//                     if(!m_kSpriteRenderers[i].enabled)
//                         m_kSpriteRenderers[i].enabled = true;
//                 }
//             }
//             else
//             {
//                 if (m_kSpriteRenderers[i] != null && m_kSpriteRenderers[i].enabled)
//                     m_kSpriteRenderers[i].enabled = false;
//             }
//         }
//     }

//     public void PlayQueue(SpriteAnimation cur, SpriteAnimation next)
//     {
//         Play(cur);
//         m_AnimQueue.Add(next);
//     }
    
//     public void PlayAngle(SpriteAnimation anim, int angle)
//     {
//         if ((m_iFaceAngle != angle) || m_kAnimState != anim)
//         {
//             SetAngle( angle );
//             Play(anim);
//         }
//     }

//     public void ForceFrame(int frame)
//     {
//         m_iAnimIndex = frame;
//     }

//     public int GetDirAngle(Vector3 targetDir)
//     {
//         if (m_bCamp == 2)
//         {
//             targetDir = -targetDir;
//         }

//         return (int)(Vector3.Angle(Vector3.forward, targetDir) * Mathf.Sign(Vector3.Cross(Vector3.forward, targetDir).y));
//     }

//     public void PlayDir(SpriteAnimation anim, Vector3 targetDir)
//     {
//         m_currenDir = targetDir;
//         PlayAngle(anim, GetDirAngle(m_currenDir));
//     }

//     public void SetDir(Vector3 targetDir)
//     {
//         m_currenDir = targetDir;
//         SetAngle( GetDirAngle(m_currenDir) );
//     }

//     public Vector3 GetDir()
//     {
//         return m_currenDir;
//     }

//     public void PlayPercent(float percent)
//     {
//         if(!m_kClipsDic[m_kAnimState].playPercent)
//         {
//             return;
//         }

//         percent = Mathf.Clamp01(percent);
//         int count = GetFrameCount(m_kAnimState);
//         m_iAnimIndex = (int)(count * percent);

//         UpdateSprite();
//         hasUpdated = true;
//     }

//     public void SetAngle(int angle)
//     {
//         if (m_iFaceAngle != angle)
//         {
//             m_iFaceAngle = angle;
//         }
//     }

//     public void PlayHurt()
//     {
//         for(int i = 0; i < m_kSpriteRenderers.Length; i++)
//         {
//             if(m_kSpriteRenderers[i] != null && m_kSpriteRenderers[i].enabled)
//             {
//                 m_kSpriteRenderers[i].color = Color.red;
//                 m_kSpriteRenderers[i].DOColor(Color.black, 1f).SetLoops(1);
//             }
//         }
//     }

//     public void PlayAdd()
//     {
//         for(int i = 0; i < m_kSpriteRenderers.Length; i++)
//         {
//             if(m_kSpriteRenderers[i] != null && m_kSpriteRenderers[i].enabled)
//             {
//                 m_kSpriteRenderers[i].color = new Color(0.796f, 0.776f, 0.137f);
//                 m_kSpriteRenderers[i].DOColor(Color.black, 1f).SetLoops(1);
//             }
//         }
//     }

//     #endregion

//     #region PriviteFunc

//     private void OnDisable()
//     {
//         for(int i = 0; i < m_kSpriteRenderers.Length; i++)
//         {
//             if(m_kSpriteRenderers[i] != null)
//             {
//                 m_kSpriteRenderers[i].DOKill(true);
//             }
//         }
//     }

//     public void LateUpdate()
//     {
//         if(hasUpdated){
//             hasUpdated = false;
//             return;
//         }

//         m_fUpdateTime += Time.deltaTime;
//         if (m_fUpdateTime > m_fTickTime)
//         {

//             m_fUpdateTime = 0;
//             m_iAnimIndex += m_iUpdateFrameNum;

//             if(m_iAnimIndex >= GetFrameCount(m_kAnimState) * m_iUpdateFrameNum)
//             {
//                 if(m_AnimQueue.Count > 0)
//                 {
//                     Play(m_AnimQueue[0]);
//                 }
//                 m_iAnimIndex = 0;
//             }

//             UpdateSprite();
//         }
//     }

//     public void UpdateWithTotalTime(float totalTime)
//     {
//         int index = (int)(totalTime / m_fTickTime);

//         m_iAnimIndex = (index % GetFrameCount(m_kAnimState)) * m_iUpdateFrameNum;

//         UpdateSprite();

//         hasUpdated = true;
//     }

//     //更新精灵
//     public void UpdateSprite()
//     {
//         bool flipx = false;
//         int angel = GetDisplayAngleStr(m_iFaceAngle, m_kAngleInteval, ref flipx);

//         int count = GetLayerCount();
//         for(int i = 0; i < count; i++){
//             Sprite sp = GetSprite(m_kAnimState, angel, m_iAnimIndex, i);

//             // if(m_kSpriteRenderers[i] == null)
//             // {
//             //     ResetRender();
//             // }

//             if (m_kSpriteRenderers[i] != null && sp != null){
//                 m_kSpriteRenderers[i].flipX = flipx;
//                 m_kSpriteRenderers[i].sprite = sp;
//             }
//         }
//     }

//     public Sprite GetShadowSprite(int offsetAngle)
//     {
//         bool flipx = false;
//         int angel = GetDisplayAngleStr(m_iFaceAngle + offsetAngle, m_kAngleInteval, ref flipx);
//         return GetSprite(m_kAnimState, angel, m_iAnimIndex, 0);
//     }

//     public void SetColor(Color color)
//     {
//         for(int i = 0; i < m_kSpriteRenderers.Length; i++)
//         {
//             if(m_kSpriteRenderers[i] != null && m_kSpriteRenderers[i].enabled)
//             {
//                 m_kSpriteRenderers[i].color = color;
//             }
//         }
//     }

//     public void SetMaterial(Material mat)
//     {
//         for(int i = 0; i < m_kSpriteRenderers.Length; i++)
//         {
//             if(m_kSpriteRenderers[i] != null && m_kSpriteRenderers[i].enabled)
//             {
//                 m_kSpriteRenderers[i].material = mat;
//                 m_kSpriteRenderers[i].color = Color.white;
//             }
//         }
//     }

//     public void SetDefaultMaterial()
//     {
//         for(int i = 0; i < m_kSpriteRenderers.Length; i++)
//         {
//             if(m_kSpriteRenderers[i] != null && m_kSpriteRenderers[i].enabled)
//             {
//                 m_kSpriteRenderers[i].material = m_orgMaterial;
//                 m_kSpriteRenderers[i].color = Color.black;
//             }
//         }
//     }

//     private Sprite GetSprite(SpriteAnimation animState, int angle, int frame, int layer)
//     {
//         if(!m_kClipsDic.ContainsKey(m_kAnimState))
//             return null;
            
//         if(m_kClipsDic == null )
//         {   
//             Debug.LogError("有错！" + m_kAnimState);
//             return null;
//         }
//         return m_kClipsDic[m_kAnimState].GetSprite(angle, frame, layer);
//     }

//     private int GetFrameCount(SpriteAnimation animState)
//     {
//         if(!m_kClipsDic.ContainsKey(m_kAnimState))
//             return 0;
//         return m_kClipsDic[m_kAnimState].FrameCount;
//     }

//     public int GetLayerCount()
//     {
//         return m_logicLayerCount;
//     }

//     public int GetClipLayerCount(SpriteAnimation anim)
//     {
//         if(!m_kClipsDic.ContainsKey(m_kAnimState))
//             return 0;

//         return m_kClipsDic[m_kAnimState].layerCount;
//     }

//     public void ResetLayerCount()
//     {
//         m_logicLayerCount = m_initLayerCount;
//         ResetRender();
//     }

//     public void SetLayerCount(int layerCount)
//     {
//         if(!m_kClipsDic.ContainsKey(m_kAnimState))
//             return;
//         m_logicLayerCount = Mathf.Min(layerCount, m_kClipsDic[m_kAnimState].layerCount);
//         ResetRender();
//     }

//     public static int GetDisplayAngle(int angle, int inteval)
//     {
//         var displayangle = angle + inteval / 2;
//         if (displayangle < 0)
//         {
//             displayangle += 360;
//         }

//         displayangle = ((displayangle / inteval) * inteval) % 360;
//         return displayangle;
//     }


//     private int GetDisplayAngleStr(int angle, int inteval, ref bool filpX)
//     {
//         angle = GetDisplayAngle(angle, inteval);
//         filpX = false;
//         if (m_bUseFilp && angle > 180)
//         {
//             filpX = true;
//             angle = 360 - angle;
//         }
//         return angle;
//     }

//     #endregion
// }