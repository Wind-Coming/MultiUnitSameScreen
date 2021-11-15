using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CreativeSpore.SuperTilemapEditor.PathFindingLib;

namespace CreativeSpore.SuperTilemapEditor
{
    public class PathfindingBehaviour : MonoBehaviour
    {
        public MapPathFinding PathFinding = new MapPathFinding();

        public enum eComputeMode
        {
            /// <summary>
            /// Stops execution until path is computed
            /// </summary>
            Synchronous, 
            /// <summary>
            /// Using a coroutine
            /// </summary>
            Asynchronous,
        }
        public eComputeMode ComputeMode = eComputeMode.Asynchronous;
        public int AsyncCoroutineIterations = 20;
        public float MovingSpeed = 1f;
        [Tooltip("Distance to the next node to be considered reached and move to the next one")]
        public float ReachNodeDistance = 0.1f;

        public delegate void OnComputedPathDelegate(PathfindingBehaviour source);
        public OnComputedPathDelegate OnComputedPath;


        private bool m_isComputingPath = false;
        private LinkedList<IPathNode> m_pathNodes;
        private LinkedListNode<IPathNode> m_curNode;
        private Vector2 m_targetPosition;

        void Start()
        {
            if (!PathFinding.TilemapGroup)
                PathFinding.TilemapGroup = FindObjectOfType<TilemapGroup>();
            if (PathFinding.CellSize == default(Vector2))
                PathFinding.CellSize = PathFinding.TilemapGroup[0].CellSize;
        }

        void Update()
        {
            // compute path to destination
            if(Input.GetMouseButtonDown(0))
            {
                m_targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                switch(ComputeMode)
                { 
                    case eComputeMode.Asynchronous:
                        StopAllCoroutines();
                        StartCoroutine(UpdatePathAsync(transform.position, m_targetPosition, AsyncCoroutineIterations));
                        break;
                    case eComputeMode.Synchronous:
                        UpdatePathSync(transform.position, m_targetPosition);
                        break;
                }
            }

            //Move to destination
            if (m_curNode != null)
            {
                Vector2 position = transform.position;
                Vector2 dest = m_curNode.Next == null ? m_targetPosition : (Vector2)m_curNode.Value.Position;

                Vector3 dir = dest - position;
                if (dir.magnitude <= ReachNodeDistance)
                    m_curNode = m_curNode.Next;
                transform.position += dir.normalized * MovingSpeed * Time.deltaTime;
            }

#if UNITY_EDITOR
            DebugDrawPath();
#endif
        }

        void DebugDrawPath()
        {
            if (!m_isComputingPath)
            {
                if (m_pathNodes != null && m_pathNodes.First != null)
                {
                    Color color = Color.red;
                    LinkedListNode<IPathNode> node = m_pathNodes.First;
                    while (node.Next != null)
                    {
                        Debug.DrawLine((node.Value as MapTileNode).Position, (node.Next.Value as MapTileNode).Position, color);
                        node = node.Next;
                        color = node.Next != null && node.Next.Next != null ? Color.white : Color.green;
                    }
                }
            }
        }

        void UpdatePathSync(Vector2 startPos, Vector2 endPos)
        {
            m_pathNodes = PathFinding.GetRouteFromTo(startPos, endPos);
            ProcessComputedPath();
        }

        IEnumerator UpdatePathAsync(Vector2 startPos, Vector2 endPos, int stepIterations)
        {
            m_isComputingPath = true;
            IEnumerator coroutine = PathFinding.GetRouteFromToAsync(startPos, endPos);
            bool isFinished = false;
            do
            {
                for (int i = 0; i < stepIterations && !isFinished; ++i)
                {
                    if(coroutine.Current is IEnumerator)
                    {
                        if(!(coroutine.Current as IEnumerator).MoveNext())
                            isFinished = !coroutine.MoveNext();
                    }
                    else
                    {
                        isFinished = !coroutine.MoveNext();
                    }
                }
                yield return null;
            }
            while (!isFinished);
            //Debug.Log("GetRouteFromToAsync execution time(ms): " + (Time.realtimeSinceStartup - now) * 1000);
            m_pathNodes = coroutine.Current as LinkedList<IPathNode>;
            ProcessComputedPath();
            m_isComputingPath = false;
            if (OnComputedPath != null)
            {
                OnComputedPath(this);
            }
            yield return null;
        }

        void ProcessComputedPath()
        {
            //+++find closest node and take next one if possible
            m_curNode = m_pathNodes.Count >= 2? m_pathNodes.First : null;
            if (m_curNode != null)
            {
                Vector2 vPos = transform.position;
                while (m_curNode != null && m_curNode.Next != null)
                {
                    MapTileNode n0 = m_curNode.Value as MapTileNode;
                    MapTileNode n1 = m_curNode.Next.Value as MapTileNode;
                    float distSqr = (vPos - (Vector2)n0.Position).sqrMagnitude;
                    float distSqr2 = (vPos - (Vector2)n1.Position).sqrMagnitude;
                    if (distSqr2 < distSqr)
                        m_curNode = m_curNode.Next;
                    else
                        break;
                }
                // take next one, avoid moving backward in the path
                if (m_curNode.Next != null)
                    m_curNode = m_curNode.Next;
            }
            //---
        }
    }
}
