
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace nTools.PrefabPainter
{
    public struct RaycastHitEx
    {       
        public Ray     ray;  
        public bool    isHit;
        public Vector3 point;
        public Vector3 normal;
        public Vector3 localPoint;
        public Vector3 localNormal;
        public Vector2 textureCoord;
        public Vector2 barycentricCoordinate;
        public float   distance;
        public int     triangleIndex;
    }


    public class Timer
    {
        int lastTime;

        public Timer() { lastTime = System.Environment.TickCount; }

        public float MarkTime() {
            int time = System.Environment.TickCount - lastTime;
            lastTime = System.Environment.TickCount;
            return (float)((float)time / 1000.0f);
        }
        public void LogTime(string str = null) {
            Debug.Log((str ?? "Time") + " = " + MarkTime());
        }
        public float Elapsed() {
            return (float)((float)(System.Environment.TickCount - lastTime) / 1000.0f);
        }
    }

    public class EditorTask
    {
        class Task {
			public readonly Func<float, bool> func = null;
            public readonly float      deltaTime = 0.0f;
            public readonly float      lifeTime = 0.0f;
            public readonly float      runTime = 0.0f;

            public bool                result = true;
            public float               lastCallTime = 0.0f;

			public Task(Func<float, bool> _func, float _deltaTime, float _lifeTime) {
                func = _func;
                deltaTime = _deltaTime;
                lifeTime = _lifeTime;
                runTime = Time.realtimeSinceStartup;
            }
        }

        static List<Task> tasks = new List<Task>();

		public static void Run(Func<float, bool> func, float deltaTime = 0.0f, float lifeDelta = 0.0f)
        {
            if (func != null)
            {
                if (tasks.Count == 0) {
                    EditorApplication.update += EditorApplicationUpdateCallback;
                }
                tasks.Add(new Task(func, deltaTime, lifeDelta));
            }
        }

        static void EditorApplicationUpdateCallback()
        {
            tasks.ForEach(task =>
            {                
                if (Time.realtimeSinceStartup - task.lastCallTime > task.deltaTime)
                {
                    task.lastCallTime = Time.realtimeSinceStartup;
					task.result = task.func((Time.realtimeSinceStartup-task.runTime) / task.lifeTime);
                }
            });

            int numRemoved = tasks.RemoveAll(task => task.result == false || Time.realtimeSinceStartup - task.runTime > task.lifeTime);

            if (numRemoved > 0 && tasks.Count == 0) {
                EditorApplication.update -= EditorApplicationUpdateCallback;
            }
        }
    }

	public static class Utility
	{
        // 
        public const float  kEpsilon = 0.001f;

        //
        public delegate bool HandleUtility_IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out UnityEngine.RaycastHit raycastHit);
        public static readonly HandleUtility_IntersectRayMesh IntersectRayMesh = null;



        // Static Constructor
        static Utility()
        {            
            MethodInfo methodIntersectRayMesh = typeof(HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Static | BindingFlags.NonPublic);

            if (methodIntersectRayMesh != null)
            {
                IntersectRayMesh = delegate(Ray ray, Mesh mesh, Matrix4x4 matrix, out UnityEngine.RaycastHit raycastHit)
                {
                    object[] parameters = new object[] { ray, mesh, matrix, null };
                    bool result = (bool)methodIntersectRayMesh.Invoke(null, parameters);
                    raycastHit = (UnityEngine.RaycastHit)parameters[3];
                    return result;
                };
            }
        }


        public static bool IntersectRayMesh2(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHitEx raycastHit)
        {
            raycastHit = default(RaycastHitEx);
            raycastHit.isHit = false;
            raycastHit.distance = Mathf.Infinity;
            raycastHit.ray = ray;

            UnityEngine.RaycastHit unityRaycastHit = default(UnityEngine.RaycastHit);

            if (Utility.IntersectRayMesh != null && 
                Utility.IntersectRayMesh(ray, mesh, matrix, out unityRaycastHit))
            {       
                raycastHit.isHit = true;
                raycastHit.point = unityRaycastHit.point;
                raycastHit.normal = unityRaycastHit.normal.normalized;
                raycastHit.ray = ray;
                raycastHit.triangleIndex = unityRaycastHit.triangleIndex;
                raycastHit.textureCoord = unityRaycastHit.textureCoord;
                raycastHit.distance = unityRaycastHit.distance;

                raycastHit.barycentricCoordinate.x = unityRaycastHit.barycentricCoordinate.x;
                raycastHit.barycentricCoordinate.y = unityRaycastHit.barycentricCoordinate.y;

                Matrix4x4 normal_WToL_Matrix = matrix.transpose.inverse;
                raycastHit.localNormal = normal_WToL_Matrix.MultiplyVector(raycastHit.normal).normalized;
                raycastHit.localPoint = matrix.inverse.MultiplyPoint (raycastHit.point);

                return true;
            }

            return false;
        }


        //
        //
		public static bool CompareVector2 (Vector2 a, Vector2 b) {
			return Mathf.Abs (a.x - b.x) < kEpsilon && Mathf.Abs (a.y - b.y) < kEpsilon;
		}

		public static bool CompareVector3 (Vector3 a, Vector3 b) {
			return Mathf.Abs (a.x - b.x) < kEpsilon && Mathf.Abs (a.y - b.y) < kEpsilon && Mathf.Abs (a.z - b.z) < kEpsilon;
		}

        public static float Vector2Cross(Vector2 v1, Vector2 v2)
        {
            return (v1.x*v2.y) - (v1.y*v2.x);
        }

		public static bool IsPowerOfTwo (int x)
		{
			return ((x >= 0) && ((x & (x - 1)) == 0));
		}

		public static bool IsArraysEqual(byte[] a1, byte[] a2)
		{
			if (a1 == null || a2 == null)
				return false;

			if (a1 == a2)
				return true;

			return System.Linq.Enumerable.SequenceEqual (a1, a2);
		}

		public static void MemSet(int[] array, int value)
		{
			int block = 32, index = 0;
			int length = Mathf.Min(block, array.Length);

			while (index < length) {
				array[index++] = value;
			}

			length = array.Length;
			while (index < length) {
				Buffer.BlockCopy(array, 0, array, index*sizeof(int), Mathf.Min(block, length-index)*sizeof(int));
				index += block;
				block *= 2;
			}
		}

		public static void MemSet(byte[] array, byte value)
		{
			int block = 32, index = 0;
			int length = Mathf.Min(block, array.Length);

			while (index < length) {
				array[index++] = value;
			}

			length = array.Length;
			while (index < length) {
				Buffer.BlockCopy(array, 0, array, index, Mathf.Min(block, length-index));
				index += block;
				block *= 2;
			}
		}


		public static bool PointInTriangle (Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3)
		{
			bool b1, b2, b3;

			b1 = ((point.x - v2.x) * (v1.y - v2.y) - (v1.x - v2.x) * (point.y - v2.y)) < 0.0f;
			b2 = ((point.x - v3.x) * (v2.y - v3.y) - (v2.x - v3.x) * (point.y - v3.y)) < 0.0f;
			b3 = ((point.x - v1.x) * (v3.y - v1.y) - (v3.x - v1.x) * (point.y - v1.y)) < 0.0f;

			return ((b1 == b2) && (b2 == b3));
		}



		public static bool SphereTriangleIntersection (Vector2 v1, Vector2 v2, Vector2 v3, Vector2 point, float radius)
		{
			bool b1, b2, b3;

			b1 = ((point.x - v2.x) * (v1.y - v2.y) - (v1.x - v2.x) * (point.y - v2.y)) > 0.0f; 

			if (b1)
                return HandleUtility.DistancePointToLineSegment(point, v1, v2) <= radius;

			b2 = ((point.x - v3.x) * (v2.y - v3.y) - (v2.x - v3.x) * (point.y - v3.y)) > 0.0f;

			if (b2)
                return HandleUtility.DistancePointToLineSegment(point, v2, v3) <= radius;

			b3 = ((point.x - v1.x) * (v3.y - v1.y) - (v3.x - v1.x) * (point.y - v1.y)) > 0.0f;

			if (b3)
                return HandleUtility.DistancePointToLineSegment(point, v3, v1) <= radius;

			return true;
		}



		public static bool RaycastTriangle(Ray ray, Vector3 p1, Vector3 p2, Vector3 p3, out float u, out float v, out float t)
		{
			const float epsilon = 1E-05f;

			Vector3 e1 = p2 - p1;
			Vector3 e2 = p3 - p1;

			Vector3 pvec = Vector3.Cross (ray.direction, e2);
			float det = Vector3.Dot(e1, pvec);

			u = v = t = 0;

			// ccw only
			if (det < epsilon)
				return false;

			float invDet = 1.0f / det;
			Vector3 tvec  = ray.origin  - p1;

			u = invDet * Vector3.Dot(tvec, pvec);
			if (u < 0.0f || u > 1.0f)
				return false;

			Vector3 qvec = Vector3.Cross (tvec, e1);
			v = invDet * Vector3.Dot(qvec, ray.direction);
			if (v < 0.0f || u+v > 1.0f)
				return false;

			t = Vector3.Dot(e2, qvec) * invDet;

			if(t > epsilon)
			{
				return true;
			}

			return false;
		}




        static public bool RaycastMesh(Ray ray, Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] indices, Matrix4x4 localToWorldMatrix, out RaycastHitEx raycastHit)
        {
            ray.direction.Normalize ();

            raycastHit = default(RaycastHitEx);
            raycastHit.isHit = false;
            raycastHit.distance = Mathf.Infinity;
            raycastHit.ray = ray;

            Matrix4x4 worldToLocalMatrix = localToWorldMatrix.inverse;

            // transform ray to mesh local space
            Ray localRay = new Ray(worldToLocalMatrix.MultiplyPoint(ray.origin),
                                   worldToLocalMatrix.MultiplyVector(ray.direction).normalized);


            float t = 0, u = 0, v = 0;
            int hitTriangle = 0;

            // raycast all triangles in mesh, find closest hit
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 p1 = vertices[indices[i]];
                Vector3 p2 = vertices[indices[i+1]];
                Vector3 p3 = vertices[indices[i+2]];


                if (RaycastTriangle(localRay, p1, p2, p3, out u, out v, out t))
                {
                    raycastHit.isHit = true;

                    if (t < raycastHit.distance)
                    {
                        raycastHit.barycentricCoordinate = new Vector2(u, v);
                        raycastHit.distance = t;
                        hitTriangle = i;
                    }
                }
            }


            // Calculate hit info
            if (raycastHit.isHit)
            {
                // Get hit point
                raycastHit.localPoint = localRay.GetPoint (raycastHit.distance);
                raycastHit.point = localToWorldMatrix.MultiplyPoint( raycastHit.localPoint );
                raycastHit.distance = (raycastHit.point - ray.origin).magnitude;

                // Calculate triangle index
                raycastHit.triangleIndex = hitTriangle / 3;

                // Calculate normal and transform it to world space
                if (normals != null)
                {
                    Matrix4x4 normal_LToW_Matrix = (localToWorldMatrix.transpose).inverse;        

                    Vector3 n1 = normals[indices[hitTriangle]];
                    Vector3 n2 = normals[indices[hitTriangle+1]];
                    Vector3 n3 = normals[indices[hitTriangle+2]];

                    raycastHit.localNormal = n1 + ((n2 - n1) * raycastHit.barycentricCoordinate.x) + ((n3 - n1) * raycastHit.barycentricCoordinate.y);
                    raycastHit.localNormal.Normalize();
                    raycastHit.normal = normal_LToW_Matrix.MultiplyVector(raycastHit.localNormal);
                    raycastHit.normal.Normalize();
                }


                // Calculate UVs
                if (uvs != null)
                {
                    Vector2 uv1 =  uvs[indices[hitTriangle]];
                    Vector2 uv2 =  uvs[indices[hitTriangle+1]];
                    Vector2 uv3 =  uvs[indices[hitTriangle+2]];

                    raycastHit.textureCoord = uv1 + ((uv2 - uv1) * raycastHit.barycentricCoordinate.x) + ((uv3 - uv1) * raycastHit.barycentricCoordinate.y);
                }

                return true;
            }

            return false; // no intersection 
        }



        public struct Sphere
        {
            public Vector3 position;
            public float radius;

            public Sphere(Vector3 pos, float rad) {
                position = pos;
                radius = rad;
            }
        }



        public static Sphere SphereForTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3[] v = { p2, p3, p1 };
            Vector3[] edgev = { (p1 - p2), (p2 - p3), (p3 - p1) };
            float[] edgel = { edgev[0].sqrMagnitude, edgev[1].sqrMagnitude, edgev[2].sqrMagnitude };

            int edge = edgel [0] > edgel [1] ? 0 : 1;
            edge = edgel [2] > edgel [edge] ? 2 : edge;

            return new Sphere (v[edge] + edgev[edge] * 0.5f, edgev[edge].magnitude * 0.5f);
        } 


        public static bool SphereRaycast(Ray ray, Sphere sphere)
        {
            Vector3 p = sphere.position - ray.origin;
            float radiusSq = sphere.radius * sphere.radius;
            float d = Vector3.Dot(p, ray.direction);

            if(d < 0.0f)
                return false;

            float d2 = (d*d) - Vector3.Dot(p, p);

            if (d2 > radiusSq)
                return false;

            return true;
        }


		public static int[] GenerateAdjacency(Vector3[] vertices, int[] indices)
		{	
			int vertexCount = vertices.Length;
			int[] mergedVertices = new int[vertices.Length];

			//Timer time = new Timer ();

			// Merge vertices
			{

				int[] vsum = new int[vertices.Length]; 
				int[] vindex = new int[vertices.Length]; 

				const float kMergeEpsilon = 0.001f;

				for (int i = 0; i < vertexCount; i++)
				{
					vsum [i] = (int)(((vertices [i].x + vertices [i].y + vertices [i].z)) * 1000.0f);
					vindex [i] = i;
				}


				Array.Sort (vsum, vindex);


				{
					int mergedVerticesCount = 0;
					int startVertex = 0;
					int currentSum = vsum [0];
					 
					for (int i = 0; i < vertexCount; i++)
					{
						if (currentSum != vsum [i]) {
							currentSum = vsum [i];
							startVertex = i;
						}
						
						bool found = false;
						Vector3 a = vertices [vindex [i]];

						for (int j = startVertex; j < i; j++)
						{
							if (currentSum != vsum [j])
								break;
						
							if (Mathf.Abs (a.x - vertices [vindex [j]].x) < kMergeEpsilon &&
							    Mathf.Abs (a.y - vertices [vindex [j]].y) < kMergeEpsilon &&
							    Mathf.Abs (a.z - vertices [vindex [j]].z) < kMergeEpsilon)
							{
								found = true;
								mergedVertices [vindex [i]] = vindex [j];
								mergedVerticesCount++;
								break;
							}
						}

						if (!found)
							mergedVertices [vindex [i]] = vindex [i];
					}
				}
			}




			int[] adjacency = new int[indices.Length]; 
			long[] edge_ids = new long[indices.Length];
			int[] triangle_bs = new int[indices.Length];


			for (int i = 0; i < indices.Length; i += 3)
			{
				long vertex1 = (long)mergedVertices[indices[i]];
				long vertex2 = (long)mergedVertices[indices[i+1]];
				long vertex3 = (long)mergedVertices[indices[i+2]];

				edge_ids[i] = (vertex1 < vertex2) ? (vertex1|(vertex2<<32)) : (vertex2|(vertex1<<32));
				triangle_bs[i] = i;

				edge_ids[i+1] = (vertex2 < vertex3) ? (vertex2|(vertex3<<32)) : (vertex3|(vertex2<<32));
				triangle_bs[i+1] = i+1;

				edge_ids[i+2] = (vertex3 < vertex1) ? (vertex3|(vertex1<<32)) : (vertex1|(vertex3<<32));
				triangle_bs[i+2] = i+2;
			}


			Array.Sort (edge_ids, triangle_bs);


			for (int i = 0; i < edge_ids.Length-1; i++)
			{
				if (edge_ids [i] == edge_ids [i + 1])
				{
					adjacency [triangle_bs[i]] = triangle_bs[i + 1];
					adjacency [triangle_bs[i+1]] = triangle_bs[i];
					i++;
				}
				else
				{
					adjacency [triangle_bs[i]] = -1;
				}
			}

			if (edge_ids.Length > 1 && edge_ids [edge_ids.Length - 2] != edge_ids [edge_ids.Length - 1])
				adjacency [triangle_bs[edge_ids.Length-1]] = -1;


			//Debug.Log ("Adj = " + time.MarkTime());
			return adjacency;
		}




        static public void MarkActiveSceneDirty()
        {
            // Mark scene changed
#if (UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
            EditorApplication.MarkSceneDirty ();
#else       
            UnityEngine.SceneManagement.Scene activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
#endif
        }


	} // class Utility








	public class UndoRedo
	{
		public abstract class UndoRecord
		{
			public abstract int GetMemoryUsed();
		}

		public delegate void UndoCallback(UndoRecord record);

		public int memoryLimit = 16 * 1024 * 1024;
		public int undoStepLimit = 64;

		private UndoCallback     undoCallback;
		private List<UndoRecord> undoList;
		private int 			 undoPosition;



		public UndoRedo()
		{			
			undoList = new List<UndoRecord>(20);
			undoPosition = 0;
		}

		public UndoRedo(UndoCallback callback)
		{
			undoCallback = callback;
			undoList = new List<UndoRecord>(20);
			undoPosition = 0;
		}



		public void AddUndoRecord(UndoRecord record)
		{
			if (undoPosition > 0) {
				undoList.RemoveRange (undoList.Count - undoPosition, undoPosition);
				undoPosition = 0;
			}

			undoList.Add(record);

			while (undoList.Count > 0 && 
				   ((memoryLimit > 0 && UsedMemory() > memoryLimit) ||
				    (undoStepLimit > 0 && undoList.Count > undoStepLimit)))
			{
				undoList.RemoveAt(0);
			}
		}

		public void Clear() {
			undoList.Clear ();
			undoPosition = 0;
		}


		public bool IsCanUndo() { return undoList.Count > 0 && undoPosition < undoList.Count; }
		public bool IsCanRedo() { return undoPosition > 0; }


		public void Undo()
		{
			if (undoList.Count > 0 && undoPosition < undoList.Count)
			{
				undoPosition++;
				UndoRecord undoRecord = undoList[undoList.Count-undoPosition];

				if (undoCallback != null) undoCallback(undoRecord);
			}
		}

		public void Redo()
		{
			if (undoPosition > 0)
			{
				UndoRecord undoRecord = undoList[undoList.Count-undoPosition];
				undoPosition--;

				if (undoCallback != null) undoCallback(undoRecord);
			}
		}


		public int UsedMemory()
		{
			int usedMemory = 0;
			foreach(UndoRecord undoRecord in undoList) {				
				usedMemory += undoRecord.GetMemoryUsed();
			}
			return usedMemory;
		}
           
	}

} // namespace 

#endif // UNITY_EDITOR

