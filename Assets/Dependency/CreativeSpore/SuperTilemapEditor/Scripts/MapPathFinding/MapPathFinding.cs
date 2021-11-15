using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CreativeSpore.SuperTilemapEditor.PathFindingLib;

namespace CreativeSpore.SuperTilemapEditor
{

    public class MapTileNode : IPathNode
    {

        public int GridX;
        public int GridY;
        public override Vector3 Position { get; set; }
        
        private TilemapGroup m_tilemapGroup;
        int[] m_neighborIdxList = new int[8]; // the index is a concat of the grid position (two int32) into a single int32
        MapPathFinding m_owner;
        private float m_costFactor = 1f;

        public override string ToString()
        {
            return "MapTileNode: " + " GridX: " + GridX + " GridY: " + GridY + " Position: " + Position;
        }

        public static int UnsafeJointTwoInts(System.Int32 a, System.Int32 b)
        {
            return ((System.UInt16)a << 16) | (System.UInt16)b;
        }

        /// <summary>
        /// Creates a new MapTileNode
        /// </summary>
        /// <param name="idx">The index is a concatenation of the two grid positions</param>
        /// <param name="owner"></param>
        public MapTileNode(TilemapGroup tilemapGroup, MapPathFinding owner, int idx) 
        {
            m_owner = owner;
            m_tilemapGroup = tilemapGroup;
            SetGridPos(idx >> 16, (int)(short)idx, m_owner.CellSize);
            //NOTE: calculate m_costFactor here using Tile parameters
            m_costFactor = 1f;
        }

        public void SetGridPos(int gridX, int gridY, Vector2 cellSize)
        {
            GridX = gridX;
            GridY = gridY;
            Position = TilemapUtils.GetGridWorldPos(gridX, gridY, cellSize);
            for (int y = -1, neighIdx = 0; y <= 1; ++y)
            {
                for (int x = -1; x <= 1; ++x)
                {
                    if ((x | y) != 0) // skip this node
                    {
                        m_neighborIdxList[neighIdx++] = UnsafeJointTwoInts(GridX + x, GridY + y);
                    }
                }
            }
        }

        #region IPathNode
        public override bool IsPassable() 
        {
            if ( m_owner.IsComputing && m_owner.AllowBlockedDestination && m_owner.EndNode == this)
                return true;
            bool isBlocked = false;
            bool isEmptyCell = true; // true is not tile is found in the node position
            for (int i = 0; !isBlocked && i < m_tilemapGroup.Tilemaps.Count; ++i)
            {
                STETilemap tilemap = m_tilemapGroup[i];
                if (tilemap && tilemap.ColliderType != eColliderType.None && tilemap.IsGridPositionInsideTilemap(GridX, GridY))
                {
                    Tile tile = tilemap.GetTile(tilemap.transform.InverseTransformPoint(Position)); // Use Position instead to allow tilemaps with offset different than 0, tilemap.GetTile(GridX, GridY);
                    isEmptyCell = false;
                    isBlocked = tile != null && tile.collData.type != eTileCollider.None;                    
                }
            }
            return !isBlocked && !isEmptyCell;
        }

        public override float GetHeuristic( ) 
        {
            //NOTE: 10f in Manhattan and 14f in Diagonal should really be 1f and 1.41421356237f, but I discovered by mistake these values improve the performance

            float h = 0f;

            switch( m_owner.HeuristicType )
            {
                case MapPathFinding.eHeuristicType.None: h = 0f; break;
                case MapPathFinding.eHeuristicType.Manhattan:
                    {
                        h = 10f * (Mathf.Abs(GridX - m_owner.EndNode.GridX) + Mathf.Abs(GridY - m_owner.EndNode.GridY));
                        break;
                    }
                case MapPathFinding.eHeuristicType.Diagonal:
                    {
                        float xf = Mathf.Abs(GridX - m_owner.EndNode.GridX);
                        float yf = Mathf.Abs(GridY - m_owner.EndNode.GridY);
                        if (xf > yf)
                            h = 14f * yf + 10f * (xf - yf);
                        else
                            h = 14f * xf + 10f * (yf - xf); 
                        break;
                    }
            }
            return h; 
        }
        

        public override float GetNeigborMovingCost(int neigborIdx) 
        {
            MapTileNode neighborNode = GetNeighbor(neigborIdx) as MapTileNode;
            if (!m_owner.AllowBlockedDestination || m_owner.EndNode != neighborNode)
            {
                if ((m_owner.PassableDetectionMode & MapPathFinding.ePassableDetectionMode.Raycast2D) != 0)
                {
                    if (RaycastCheck2D(neighborNode.Position))
                        return PathFinding.k_InfiniteCostValue;
                }
                else if ((m_owner.PassableDetectionMode & MapPathFinding.ePassableDetectionMode.Raycast3D) != 0)
                {
                    if (RaycastCheck3D(neighborNode.Position))
                        return PathFinding.k_InfiniteCostValue;
                }
            }
            float fCost = 1f;
            //567 // 
            //3X4 // neighbor index positions, X is the position of this node
            //012
            if( neigborIdx == 0 || neigborIdx == 2 || neigborIdx ==  5 || neigborIdx == 7 )
            {
                //check if can reach diagonals as it could be not possible if flank tiles are not passable      
                MapTileNode nodeN = GetNeighbor(1) as MapTileNode;
                MapTileNode nodeW = GetNeighbor(3) as MapTileNode;
                MapTileNode nodeE = GetNeighbor(4) as MapTileNode;
                MapTileNode nodeS = GetNeighbor(6) as MapTileNode;
                bool usingColliders = (m_owner.PassableDetectionMode & (MapPathFinding.ePassableDetectionMode.Raycast2D | MapPathFinding.ePassableDetectionMode.Raycast2D)) != 0;
                bool nodeNisPassable = nodeN.IsPassable() && (!usingColliders || GetNeigborMovingCost(1) != PathFinding.k_InfiniteCostValue);
                bool nodeWisPassable = nodeW.IsPassable() && (!usingColliders || GetNeigborMovingCost(3) != PathFinding.k_InfiniteCostValue);
                bool nodeEisPassable = nodeE.IsPassable() && (!usingColliders || GetNeigborMovingCost(4) != PathFinding.k_InfiniteCostValue);
                bool nodeSisPassable = nodeS.IsPassable() && (!usingColliders || GetNeigborMovingCost(6) != PathFinding.k_InfiniteCostValue);
                if (
                    !m_owner.AllowDiagonals ||
                    (neigborIdx == 0 && (!nodeNisPassable || !nodeWisPassable)) || // check North West
                    (neigborIdx == 2 && (!nodeNisPassable || !nodeEisPassable)) || // check North East
                    (neigborIdx == 5 && (!nodeSisPassable || !nodeWisPassable)) || // check South West
                    (neigborIdx == 7 && (!nodeSisPassable || !nodeEisPassable))    // check South East
                )
                {
                    return PathFinding.k_InfiniteCostValue;
                }
                else
                {
                    fCost = 1.41421356237f;
                }
            }
            else
            {
                fCost = 1f;
            }
            fCost *= m_costFactor;
            return fCost;  
        }
        public override int GetNeighborCount() { return 8; }
        public override IPathNode GetNeighbor(int neighbourIdx) { return m_owner.GetMapTileNode(m_neighborIdxList[neighbourIdx]); }

        private bool RaycastCheck2D( Vector3 targetPosition )
        {
            bool savedValue = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = false;
            Vector3 dir = targetPosition - Position;            
            bool raycastCheck = Physics2D.Raycast( Position, dir, dir.magnitude, m_owner.RaycastDetectionLayerMask);
            Physics2D.queriesHitTriggers = savedValue;
            return raycastCheck;
        }

        private bool RaycastCheck3D(Vector3 targetPosition)
        {
            bool savedValue = Physics.queriesHitTriggers;
            Physics.queriesHitTriggers = false;
            Vector3 dir = targetPosition - Position;
            bool raycastCheck = Physics.Raycast(Position, dir, dir.magnitude, m_owner.RaycastDetectionLayerMask);
            Physics.queriesHitTriggers = savedValue;
            return raycastCheck;
        }

        #endregion
    }

    [System.Serializable]
    public class MapPathFinding
    {

        public enum eHeuristicType
        {
            /// <summary>
            /// Very slow but guarantees the shortest path
            /// </summary>
            None,
            /// <summary>
            /// Faster than None, but does not guarantees the shortest path
            /// </summary>
            Manhattan,
            /// <summary>
            /// Faster than Manhattan but less accurate
            /// </summary>
            Diagonal
        }

        [System.Flags]
        public enum ePassableDetectionMode
        {
            /// <summary>
            /// Check if the tile has colliders to consider it non-passable
            /// </summary>
            TileColliderCheck = 1,
            /// <summary>
            /// Use 2D raycasting
            /// </summary>
            Raycast2D = 1 << 1,
            /// <summary>
            /// Use 3D raycasting
            /// </summary>
            Raycast3D = 1 << 2,
        }

        public eHeuristicType HeuristicType = eHeuristicType.Manhattan;
        [Tooltip("Maximum distance or number of nodes a path can have when calculating the route. If the value is 0 or below, it won't be taking into account.")]
        public int MaxDistance = 0;
        public ePassableDetectionMode PassableDetectionMode = ePassableDetectionMode.TileColliderCheck;
        public LayerMask RaycastDetectionLayerMask = -1;
        public TilemapGroup TilemapGroup;        

        public Vector2 CellSize
        {
            get
            {
                if (m_cellSize == default(Vector2) && TilemapGroup.Tilemaps.Count > 0)
                    m_cellSize = TilemapGroup[0].CellSize;
                return m_cellSize;
            }
            set { m_cellSize = value; }
        }

        /// <summary>
        /// Set if diagonal movement is allowed
        /// </summary>
        [Tooltip("Set if diagonal movement is allowed")]
        public bool AllowDiagonals = true;

        /// <summary>
        /// If this is true, the final destination tile can be a blocked tile. 
        /// Used to reach the closest tile to a blocked tile, like a door, chest, etc
        /// </summary>
        [Tooltip("If this is true, the final destination tile can be a blocked tile")]
        public bool AllowBlockedDestination = false;

        [SerializeField]
        private Vector2 m_cellSize = default(Vector2);

        /// <summary>
        /// Max iterations to find a path. Use a value <= 0 for infinite iterations.
        /// Remember max iterations will be reached when trying to find a path with no solutions.
        /// </summary>
        public int MaxIterations 
        {
            get { return m_pathFinding.MaxIterations; }
            set { MaxIterations = m_pathFinding.MaxIterations; }
        }
        
        public bool IsComputing { get { return m_pathFinding.IsComputing; } }        

        PathFinding m_pathFinding = new PathFinding();
        Dictionary<int, MapTileNode> m_dicTileNodes = new Dictionary<int, MapTileNode>();
        // EndNode is used for the heuristic
        internal MapTileNode EndNode { get; private set; }        

        public void ClearNodeDictionary()
        {
            m_dicTileNodes.Clear();
        }

        /// <summary>
        /// Get a map tile node based on its index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public MapTileNode GetMapTileNode( int idx )
        {
            MapTileNode mapTileNode;
            bool wasFound = m_dicTileNodes.TryGetValue(idx, out mapTileNode);
            if(!wasFound)
            {
                mapTileNode = new MapTileNode(TilemapGroup, this, idx);                
                m_dicTileNodes[idx] = mapTileNode;
            }
            return mapTileNode;
        }

        public MapTileNode GetMapTileNode( int gridX, int gridY )
        {
            return GetMapTileNode(MapTileNode.UnsafeJointTwoInts(gridX, gridY));
        }

        public MapTileNode GetMapTileNode( Vector2 position )
        {
            return GetMapTileNode(BrushUtil.GetGridX(position, CellSize), BrushUtil.GetGridY(position, CellSize));
        }

        /// <summary>
        /// Return a list of path nodes from the start tile to the end tile
        /// </summary>
        /// <param name="startIdx"></param>
        /// <param name="endIdx"></param>
        /// <returns></returns>
        public LinkedList<IPathNode> GetRouteFromTo(Vector2 startPos, Vector2 endPos)
        {
            LinkedList<IPathNode> nodeList = new LinkedList<IPathNode>();
            if (m_pathFinding.IsComputing)
            {
                Debug.LogWarning("PathFinding is already computing. GetRouteFromTo will not be executed!");
            }
            else
            {
                IPathNode start = GetMapTileNode(startPos);
                EndNode = GetMapTileNode(endPos );
                nodeList = m_pathFinding.ComputePath(start, EndNode, MaxDistance > 0 ? MaxDistance : int.MaxValue);         //NOTE: the path is given from end to start ( change the order? )
            }
            return nodeList;
        }

        /// <summary>
        /// Return a list of path nodes from the start tile to the end tile
        /// </summary>
        /// <param name="startIdx"></param>
        /// <param name="endIdx"></param>
        /// <returns></returns>
        public IEnumerator GetRouteFromToAsync( Vector2 startPos, Vector2 endPos)
        {
            IPathNode start = GetMapTileNode(startPos);
            EndNode = GetMapTileNode(endPos);
            return m_pathFinding.ComputePathAsync(start, EndNode, MaxDistance > 0? MaxDistance : int.MaxValue);
        }
    }

}
