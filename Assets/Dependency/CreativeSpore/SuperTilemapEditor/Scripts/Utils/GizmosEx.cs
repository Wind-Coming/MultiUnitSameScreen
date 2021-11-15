using UnityEngine;
using System.Collections;

namespace CreativeSpore.SuperTilemapEditor
{
    public static class GizmosEx
    {
        public static float GetGizmoSize(Vector3 position)
        {
            Camera current = Camera.current;
            position = Gizmos.matrix.MultiplyPoint(position);

            if (current)
            {
                Transform transform = current.transform;
                Vector3 position2 = transform.position;
                float z = Vector3.Dot(position - position2, transform.TransformDirection(new Vector3(0f, 0f, 1f)));
                Vector3 a = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(0f, 0f, z)));
                Vector3 b = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(1f, 0f, z)));
                float magnitude = (a - b).magnitude;
                return 80f / Mathf.Max(magnitude, 0.0001f);
            }

            return 20f;
        }

        public static void DrawRect(Transform transform, Rect rect, Color color)
        {
            Vector3[] rectVerts = { 
            transform.TransformPoint(new Vector3(rect.x, rect.y, 0)), 
			transform.TransformPoint(new Vector3(rect.x + rect.width, rect.y, 0)), 
			transform.TransformPoint(new Vector3(rect.x + rect.width, rect.y + rect.height, 0)), 
			transform.TransformPoint(new Vector3(rect.x, rect.y + rect.height, 0)) };
            Color savedColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(rectVerts[0], rectVerts[1]);
            Gizmos.DrawLine(rectVerts[1], rectVerts[2]);
            Gizmos.DrawLine(rectVerts[2], rectVerts[3]);
            Gizmos.DrawLine(rectVerts[3], rectVerts[0]);
            Gizmos.color = savedColor;
        }

        public static void DrawDot(Transform transform, Vector3 position, float size, Color color)
        {
            Rect rDot = new Rect(-size / (2 * transform.localScale.x), -size / (2 * transform.localScale.y), size / transform.localScale.x, size / transform.localScale.y);
            Vector3[] rectVerts = { 
            transform.TransformPoint( position + new Vector3(rDot.x, rDot.y, 0)), 
			transform.TransformPoint( position + new Vector3(rDot.x + rDot.width, rDot.y, 0)), 
			transform.TransformPoint( position + new Vector3(rDot.x + rDot.width, rDot.y + rDot.height, 0)), 
			transform.TransformPoint( position + new Vector3(rDot.x, rDot.y + rDot.height, 0)) };
            Color savedColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(rectVerts[0], rectVerts[1]);
            Gizmos.DrawLine(rectVerts[1], rectVerts[2]);
            Gizmos.DrawLine(rectVerts[2], rectVerts[3]);
            Gizmos.DrawLine(rectVerts[3], rectVerts[0]);
            Gizmos.color = savedColor;
        }
        

        public static void DrawRect(Rect rect, Color color)
        {
            Vector3[] rectVerts = { 
            new Vector3(rect.x, rect.y, 0), 
			new Vector3(rect.x + rect.width, rect.y, 0), 
			new Vector3(rect.x + rect.width, rect.y + rect.height, 0), 
			new Vector3(rect.x, rect.y + rect.height, 0) };
            Color savedColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(rectVerts[0], rectVerts[1]);
            Gizmos.DrawLine(rectVerts[1], rectVerts[2]);
            Gizmos.DrawLine(rectVerts[2], rectVerts[3]);
            Gizmos.DrawLine(rectVerts[3], rectVerts[0]);
            Gizmos.color = savedColor;
        }

        public static void DrawDot(Vector3 position, float size, Color color)
        {
            Rect rDot = new Rect(-size / 2, -size / 2, size, size);
            Vector3[] rectVerts = { 
            position + new Vector3(rDot.x, rDot.y, 0), 
			position + new Vector3(rDot.x + rDot.width, rDot.y, 0), 
			position + new Vector3(rDot.x + rDot.width, rDot.y + rDot.height, 0), 
			position + new Vector3(rDot.x, rDot.y + rDot.height, 0) };
            Color savedColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(rectVerts[0], rectVerts[1]);
            Gizmos.DrawLine(rectVerts[1], rectVerts[2]);
            Gizmos.DrawLine(rectVerts[2], rectVerts[3]);
            Gizmos.DrawLine(rectVerts[3], rectVerts[0]);
            Gizmos.color = savedColor;
        }
    }
}
