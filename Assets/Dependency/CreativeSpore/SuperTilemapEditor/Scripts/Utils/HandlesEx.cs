using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    public static class HandlesEx
    {
        public static void DrawRectWithOutline(Transform transform, Rect rect, Color color, Color colorOutline)
        {
            Vector3[] rectVerts = { 
            transform.TransformPoint(new Vector3(rect.x, rect.y, 0)), 
			transform.TransformPoint(new Vector3(rect.x + rect.width, rect.y, 0)), 
			transform.TransformPoint(new Vector3(rect.x + rect.width, rect.y + rect.height, 0)), 
			transform.TransformPoint(new Vector3(rect.x, rect.y + rect.height, 0)) };
            Handles.DrawSolidRectangleWithOutline(rectVerts, color, colorOutline);
        }

        public static void DrawDotOutline(Transform transform, Vector3 position, float size, Color color, Color colorOutline)
        {
            Rect rDot = new Rect(-size / (2 * transform.localScale.x), -size / (2 * transform.localScale.y), size / transform.localScale.x, size / transform.localScale.y);
            Vector3[] rectVerts = { 
            transform.TransformPoint( position + new Vector3(rDot.x, rDot.y, 0)), 
			transform.TransformPoint( position + new Vector3(rDot.x + rDot.width, rDot.y, 0)), 
			transform.TransformPoint( position + new Vector3(rDot.x + rDot.width, rDot.y + rDot.height, 0)), 
			transform.TransformPoint( position + new Vector3(rDot.x, rDot.y + rDot.height, 0)) };
            Handles.DrawSolidRectangleWithOutline(rectVerts, color, colorOutline);
        }

        public static void DrawDottedLine(Transform transform, Rect rect, float screenSpaceSize)
        {
            Vector3[] rectVerts = { 
            transform.TransformPoint(new Vector3(rect.x, rect.y, 0)), 
			transform.TransformPoint(new Vector3(rect.x + rect.width, rect.y, 0)), 
			transform.TransformPoint(new Vector3(rect.x + rect.width, rect.y + rect.height, 0)), 
			transform.TransformPoint(new Vector3(rect.x, rect.y + rect.height, 0)) };
            Handles.DrawDottedLine(rectVerts[0], rectVerts[1], screenSpaceSize);
            Handles.DrawDottedLine(rectVerts[1], rectVerts[2], screenSpaceSize);
            Handles.DrawDottedLine(rectVerts[2], rectVerts[3], screenSpaceSize);
            Handles.DrawDottedLine(rectVerts[3], rectVerts[0], screenSpaceSize);
        }

        public static void DrawRectWithOutline(Rect rect, Color color, Color colorOutline)
        {
            Vector3[] rectVerts = { 
            new Vector3(rect.x, rect.y, 0), 
			new Vector3(rect.x + rect.width, rect.y, 0), 
			new Vector3(rect.x + rect.width, rect.y + rect.height, 0), 
			new Vector3(rect.x, rect.y + rect.height, 0) };
            Handles.DrawSolidRectangleWithOutline(rectVerts, color, colorOutline);
        }

        public static void DrawDotOutline(Vector3 position, float size, Color color, Color colorOutline)
        {
            Rect rDot = new Rect(-size / 2, -size / 2, size, size);
            Vector3[] rectVerts = { 
            position + new Vector3(rDot.x, rDot.y, 0), 
			position + new Vector3(rDot.x + rDot.width, rDot.y, 0), 
			position + new Vector3(rDot.x + rDot.width, rDot.y + rDot.height, 0), 
			position + new Vector3(rDot.x, rDot.y + rDot.height, 0) };
            Handles.DrawSolidRectangleWithOutline(rectVerts, color, colorOutline);
        }

        public static void DrawDottedLine(Rect rect, float screenSpaceSize)
        {
            Vector3[] rectVerts = { 
            new Vector3(rect.x, rect.y, 0), 
			new Vector3(rect.x + rect.width, rect.y, 0), 
			new Vector3(rect.x + rect.width, rect.y + rect.height, 0), 
			new Vector3(rect.x, rect.y + rect.height, 0) };
            Handles.DrawDottedLine(rectVerts[0], rectVerts[1], screenSpaceSize);
            Handles.DrawDottedLine(rectVerts[1], rectVerts[2], screenSpaceSize);
            Handles.DrawDottedLine(rectVerts[2], rectVerts[3], screenSpaceSize);
            Handles.DrawDottedLine(rectVerts[3], rectVerts[0], screenSpaceSize);
        }

        public static void DrawDottedPolyLine(Vector3[] points, float screenSpaceSize, Color color)
        {
            Color savedColor = Handles.color;
            Handles.color = color;
            for (int i = 0; i < points.Length; ++i )
                Handles.DrawDottedLine(points[i], points[(i + 1) % points.Length], screenSpaceSize);
            Handles.color = savedColor;
        }
    }
}
#endif