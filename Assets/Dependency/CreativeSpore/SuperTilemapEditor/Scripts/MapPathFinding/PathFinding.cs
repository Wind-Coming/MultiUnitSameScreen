using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace CreativeSpore.SuperTilemapEditor.PathFindingLib
{
    public class IPathNode
    {

        public virtual bool IsPassable() { return false; }
        public virtual int GetNeighborCount() { return 0; }
        public virtual IPathNode GetNeighbor(int nodeIdx) { return null; }
        public virtual float GetNeigborMovingCost(int neigborIdx) { return 1f; }
        public virtual float GetHeuristic() { return 0f; } //H
        public virtual Vector3 Position { get; set; }

        // Internal parameters are filled in the path finding algorithm
        internal IPathNode ParentNode = null;
        internal float Cost = 0f; //G
        internal float Score = 0f; //F
        internal int Distance = 0;

        internal int openComputeId;
        internal int closeComputeId;
    };

    //ref: http://homepages.abdn.ac.uk/f.guerin/pages/teaching/CS1013/practicals/aStarTutorial.htm
    public class PathFinding
    {
        const int k_IterationsPerProcessChunk = 10; // how many iterations before returning the coroutine
        public const float k_InfiniteCostValue = float.MaxValue; // use this value to define an infinite cost

        public class FindingParams
        {
            public IPathNode startNode;
            public IPathNode endNode;
            public LinkedList<IPathNode> computedPath;
            public int maxPathDistance = int.MaxValue;
        }

        public int MaxIterations = 8000; // <= 0, for infinite iterations
        public bool IsComputing { get; private set; }

        private LinkedList<IPathNode> m_openList = new LinkedList<IPathNode>();
        private LinkedList<IPathNode> m_closeList = new LinkedList<IPathNode>();
        private int m_computeId;

        public LinkedList<IPathNode> ComputePath(IPathNode startNode, IPathNode endNode, int maxDistance = int.MaxValue)
        {
            FindingParams findingParams = new FindingParams()
            {
                startNode = startNode,
                endNode = endNode,
                computedPath = new LinkedList<IPathNode>(),
                maxPathDistance = maxDistance,
            };

            IEnumerator coroutine = ComputePathCoroutine(findingParams);
            while (coroutine.MoveNext());
            return findingParams.computedPath;
        }

        public IEnumerator ComputePathAsync(IPathNode startNode, IPathNode endNode, int maxDistance = int.MaxValue)
        {
            FindingParams findingParams = new FindingParams()
            {
                startNode = startNode,
                endNode = endNode,
                computedPath = new LinkedList<IPathNode>(),
                maxPathDistance = maxDistance,
            };
            
            yield return ComputePathCoroutine(findingParams);
            yield return findingParams.computedPath;
        }

        public IEnumerator ComputePathCoroutine(FindingParams findingParams)
        {
            //NOTE: m_computeId will be different for each call.
            // if openComputeId == m_computeId, it means the node in in openList, same for closeComputeId and closeList.
            // this is faster than reset a bool isInOpenList for all nodes before next call to this method
            ++m_computeId;          

            IsComputing = true;
            if (findingParams.startNode == findingParams.endNode 
                || !findingParams.endNode.IsPassable())
            {
                findingParams.computedPath.AddLast(findingParams.startNode);
            }
            else
            {
                //1) Add the starting square (or node) to the open list.
                m_closeList.Clear();
                m_openList.Clear();
                m_openList.AddLast(findingParams.startNode);
                findingParams.startNode.openComputeId = m_computeId;
                //reset the first node only. The rest of nodes will be recalculated if needed using m_computeId
                findingParams.startNode.Score = findingParams.startNode.Cost = findingParams.startNode.Distance = 0; 

                //2) Repeat the following:
                LinkedListNode<IPathNode> curNode;
                int iterations = 0;
                int iterChunkCounter = k_IterationsPerProcessChunk;
                do
                {
                    ++iterations;
                    --iterChunkCounter;
                    if (iterChunkCounter == 0)
                    {
                        iterChunkCounter = k_IterationsPerProcessChunk;
                        yield return null;
                    }

                    //a) Look for the lowest F cost square on the open list. We refer to this as the current square.
                    //curNode = m_vOpen.First(c => c.Score == m_vOpen.Min(c2 => c2.Score));
                    curNode = null;
                    for (LinkedListNode<IPathNode> pathNode = m_openList.First; pathNode != null; pathNode = pathNode.Next)
                    {
                        if (curNode == null || pathNode.Value.Score < curNode.Value.Score)
                        {
                            curNode = pathNode;
                        }
                    }

                    //b) Switch it to the closed list.
                    m_openList.Remove(curNode); curNode.Value.openComputeId = 0;
                    m_closeList.AddLast(curNode); curNode.Value.closeComputeId = m_computeId;

                    //c) For each of the 8 squares adjacent to this current square …
                    for (int i = 0, s = curNode.Value.GetNeighborCount(); i < s; ++i)
                    {
                        //If it is not walkable or if it is on the closed list, ignore it. Otherwise do the following.           
                        IPathNode neigborNode = curNode.Value.GetNeighbor(i);
                        float movingCost = curNode.Value.GetNeigborMovingCost(i);
                        bool isNeighborNodePassable = movingCost != k_InfiniteCostValue && neigborNode.IsPassable();
                        if (
                            neigborNode.closeComputeId != m_computeId && // if closeList does not contains node
                            isNeighborNodePassable
                           ) 
                        {
                            float newCost = curNode.Value.Cost + movingCost;
                            int newDist = curNode.Value.Distance + 1;
                            //If it isn’t on the open list, add it to the open list. Make the current square the parent of this square. Record the F, G, and H costs of the square. 
                            if (neigborNode.openComputeId != m_computeId // if openList does not contains node
                                && newDist <= findingParams.maxPathDistance // if distance is under limits
                                ) 
                            {
                                m_openList.AddLast(neigborNode); neigborNode.openComputeId = m_computeId;
                                neigborNode.ParentNode = curNode.Value;
                                neigborNode.Cost = newCost;
                                neigborNode.Score = neigborNode.Cost + neigborNode.GetHeuristic();
                                neigborNode.Distance = newDist;
                                if (neigborNode == findingParams.endNode)
                                {
                                    curNode.Value = neigborNode;
                                    m_openList.Clear(); // force to exit while
                                    break;
                                }
                            }
                            //If it is on the open list already, check to see if this path to that square is better, using G cost as the measure. A lower G cost means that this is a better path. 
                            else if (newCost < neigborNode.Cost)
                            {
                                //If so, change the parent of the square to the current square, and recalculate the G and F scores of the square. 
                                neigborNode.ParentNode = curNode.Value;
                                neigborNode.Cost = newCost;
                                neigborNode.Score = neigborNode.Cost + neigborNode.GetHeuristic();
                                neigborNode.Distance = newDist;
                            }
                        }
                    }
                }
                while (m_openList.Count > 0 && (MaxIterations <= 0 || iterations < MaxIterations));
                //Debug.Log("iterations: " + iterations);
                if (iterations >= MaxIterations)
                    Debug.LogWarning("Info: max iterations reached before finding path solution. MaxIterations is set to " + MaxIterations);
                //d) Stop when you:
                //Add the target square to the closed list, in which case the path has been found (see note below), or
                //Fail to find the target square, and the open list is empty. In this case, there is no path.   
                if (curNode.Value == findingParams.endNode)
                {
                    //3) Save the path. Working backwards from the target square, go from each square to its parent square until you reach the starting square. That is your path.             
                    findingParams.computedPath.AddLast(curNode.Value);
                    do
                    {
                        curNode.Value = curNode.Value.ParentNode;
                        findingParams.computedPath.AddFirst(curNode.Value);
                    }
                    while (curNode.Value != findingParams.startNode);
                }
            }
            IsComputing = false;
            yield return findingParams;
        }
    }
}
