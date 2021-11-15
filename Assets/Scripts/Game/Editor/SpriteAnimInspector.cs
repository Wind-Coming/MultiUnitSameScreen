// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEditor;

// [CustomEditor(typeof(SpriteAnim))]
// public class SpriteAnimInspector : Editor
// {
//     SpriteAnim spriteAnim;
//     bool toggle;
//     void OnEnable()
//     {
//         spriteAnim = (SpriteAnim)target;
//         spriteAnim.Init(spriteAnim.m_kAnimState, spriteAnim.m_iFaceAngle);
//     }

//     public override void  OnInspectorGUI()
//     {
//         base.OnInspectorGUI();

//         SpriteAnimation sa = (SpriteAnimation)EditorGUILayout.EnumPopup("当前动画", spriteAnim.m_kAnimState);
//         if(sa != spriteAnim.m_kAnimState)
//         {
//             spriteAnim.Play(sa);
//             spriteAnim.UpdateSprite();
//         }

//         int tFrame = EditorGUILayout.IntSlider("帧", spriteAnim.m_iAnimIndex, 0, spriteAnim.m_kClipsDic[spriteAnim.m_kAnimState].FrameCount - 1);
//         if(spriteAnim.m_iAnimIndex != tFrame)
//         {
//             spriteAnim.m_iAnimIndex = tFrame;
//             spriteAnim.UpdateSprite();
//         }
//         if(GUILayout.Button("同步精灵"))
//         {
//             spriteAnim.UpdateSprite();
//         }
//         if(GUILayout.Button("下一个角度"))
//         {
//             spriteAnim.m_iFaceAngle += spriteAnim.m_kAngleInteval;
//             spriteAnim.UpdateSprite();
//         }
//         toggle = EditorGUILayout.Foldout(toggle, "挂点");
//         if(toggle){
//             int num = EditorGUILayout.IntField("数量", spriteAnim.m_firePoints.Length );
//             if(num != spriteAnim.m_firePoints.Length)
//             {
//                 SerializedProperty property = serializedObject.FindProperty("m_firePoints");
//                 property.arraySize = num;
//                 property.serializedObject.ApplyModifiedProperties();
//             }
//             for(int i = 0; i < spriteAnim.m_firePoints.Length; i++)
//             {
//                 EditorGUILayout.BeginHorizontal();
//                 spriteAnim.m_firePoints[i] = EditorGUILayout.Vector3Field(i * spriteAnim.m_kAngleInteval + "度", spriteAnim.m_firePoints[i]);
//                 if(GUILayout.Button("引用"))
//                 {
//                     if(Selection.activeGameObject != null)
//                     {
//                         spriteAnim.m_firePoints[i] = Selection.activeGameObject.transform.position - spriteAnim.transform.position;
//                     }
//                     else
//                     {
//                         Debug.LogWarning("没有选择节点!");
//                     }
//                 }
//                 EditorGUILayout.EndHorizontal();
//             }
//         }

//     }
// }
