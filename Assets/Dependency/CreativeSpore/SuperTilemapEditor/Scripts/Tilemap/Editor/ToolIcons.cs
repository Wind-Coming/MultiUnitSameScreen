using UnityEngine;
using System.Collections;

namespace CreativeSpore.SuperTilemapEditor
{    
    public static class ToolIcons
    {
        public enum eToolIcon
        {
            Pencil,
            Erase,
            Fill,
            FlipH,
            FlipV,
            Rot90,
            Info,
            Refresh,
        }

        public enum ePaintModeIcon
        {
            Pencil,
            Line,
            Rect,
            FilledRect,
            Ellipse,
            FilledEllipse,
        }

        private static float[][] s_paintModeIcons = new float[][]
        {
            new float[] //Pencil
            {
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 1, 1, 1,
                0, 0, 0, 0, 1, 0, 1, 0,
                0, 0, 0, 1, 0, 1, 0, 0,
                0, 0, 1, 0, 1, 0, 0, 0,
                0, 1, 0, 1, 0, 0, 0, 0,
                1, 1, 1, 0, 0, 0, 0, 0,
                1, 1, 0, 0, 0, 0, 0, 0,
            },
            new float[] //Line
            {
                0, 0, 0, 0, 0, 0, .2f, 1,
                0, 0, 0, 0, 0, .2f, 1, .2f,
                0, 0, 0, 0, .2f, 1, .2f, 0,
                0, 0, 0, .2f, 1, .2f, 0, 0,
                0, 0, .2f, 1, .2f, 0, 0, 0,
                0, .2f, 1, .2f, 0, 0, 0, 0,
                .2f, 1, .2f, 0, 0, 0, 0, 0,
                1, .2f, 0, 0, 0, 0, 0, 0,
            },
            new float[] //Rect
            {
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 0, 0, 0, 0, 0, 0, 1,
                1, 0, 0, 0, 0, 0, 0, 1,
                1, 0, 0, 0, 0, 0, 0, 1,
                1, 0, 0, 0, 0, 0, 0, 1,
                1, 0, 0, 0, 0, 0, 0, 1,
                1, 0, 0, 0, 0, 0, 0, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
            },
            new float[] //FilledRect
            {
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
            },
            new float[] //Circle
            {
                0, 0, .5f, 1, 1, .5f, 0, 0,
                0, .5f, 1, 0, 0, 1, .5f, 0,
                .5f, 1, 0, 0, 0, 0, 1, .5f,
                1, 0, 0, 0, 0, 0, 0, 1,
                1, 0, 0, 0, 0, 0, 0, 1,
                .5f, 1, 0, 0, 0, 0, 1, .5f,
                0, .5f, 1, 0, 0, 1, .5f, 0,
                0, 0, .5f, 1, 1, .5f, 0, 0,
            },
            new float[] //FilledCircle
            {
                0, 0, .5f, 1, 1, .5f, 0, 0,
                0, .5f, 1, 1, 1, 1, .5f, 0,
                .5f, 1, 1, 1, 1, 1, 1, .5f,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                .5f, 1, 1, 1, 1, 1, 1, .5f,
                0, .5f, 1, 1, 1, 1, .5f, 0,
                0, 0, .5f, 1, 1, .5f, 0, 0,
            },
        };
        private static Texture2D[] s_paintModeIconTexture = new Texture2D[s_paintModeIcons.GetLength(0)];

        private static float[][] s_icons = new float[][]
        {
            new float[] //Pencil
            {
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 1, 1, 1,
                0, 0, 0, 0, 1, 0, 1, 0,
                0, 0, 0, 1, 0, 1, 0, 0,
                0, 0, 1, 0, 1, 0, 0, 0,
                0, 1, 0, 1, 0, 0, 0, 0,
                1, 1, 1, 0, 0, 0, 0, 0,
                1, 1, 0, 0, 0, 0, 0, 0,
            },
            new float[] //Erase
            {
                0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 0, 1, 0,
                0, 1, 1, 1, 0, 0, 0, 1,
                1, 1, 1, 1, 1, 0, 1, 0,
                0, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0,
            },
            new float[] //Fill
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 0,
                0, 0, 1, 0, 1, 1, 1, 0,
                0, 1, 0, 0, 0, 1, 1, 1,
                1, 0, 0, 0, 0, 0, 1, 1,
                0, 1, 0, 0, 0, 1, 0, 1,
                0, 0, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0,
            },
            new float[] //FlipV
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 0, 0, 0, 1, 1, 1, 1,
                1, 0, 0, 1, 1, 1, 1, 1,
                1, 0, 1, 1, 0, 0, 1, 1,
                1, 0, 0, 1, 1, 1, 1, 1,
                1, 0, 0, 0, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
            },
            new float[] //FlipH
            {
                0, 1, 1, 1, 1, 1, 1, 1,
                0, 1, 0, 0, 0, 0, 0, 1,
                0, 1, 0, 0, 1, 0, 0, 1,
                0, 1, 0, 1, 1, 1, 0, 1,
                0, 1, 1, 1, 0, 1, 1, 1,
                0, 1, 1, 1, 0, 1, 1, 1,
                0, 1, 1, 1, 1, 1, 1, 1,
                0, 1, 1, 1, 1, 1, 1, 1,
            },
            new float[] //Rot90
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 1, 1, 1,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
            },            
            new float[] //Info
            {
                1, 1, 1, 1, 1, 1, 1, 0,
                1, 0, 0, 0, 0, 0, 1, 0,
                1, 0, 1, 1, 1, 0, 1, 0,
                1, 0, 0, 0, 0, 0, 1, 0,
                1, 0, 1, 1, 0, 0, 1, 0,
                1, 0, 0, 0, 0, 0, 1, 0,
                1, 0, 0, 0, 0, 1, 1, 0,
                1, 1, 1, 1, 1, 1, 0, 0,
            },
            new float[] //Refresh
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 0,
                0, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 1, 0, 0,
                0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
            },
        };
        private static Texture2D[] s_iconTexture = new Texture2D[s_icons.GetLength(0)];

        public static Texture2D GetToolTexture(eToolIcon toolIcon)
        {
            return GetToolTexture(s_iconTexture, s_icons, (int)toolIcon);
        }

        public static Texture2D GetToolTexture(ePaintModeIcon toolIcon)
        {
            return GetToolTexture(s_paintModeIconTexture, s_paintModeIcons, (int)toolIcon);
        }

        private static Texture2D GetToolTexture(Texture2D[] textArr, float[][] dataArr, int idx)
        {
            if (textArr[idx] == null)
            {
                Texture2D iconTexture = new Texture2D(8, 8);
                iconTexture.hideFlags = HideFlags.DontSave;
                iconTexture.wrapMode = TextureWrapMode.Clamp;
                Color[] colors = new Color[dataArr[idx].Length];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[(8 - 1 - (i / 8)) * 8 + i % 8] = new Color(1f, 1f, 1f, dataArr[idx][i]);
                }
                iconTexture.SetPixels(colors);
                iconTexture.Apply();
                textArr[idx] = iconTexture;
            }
            return textArr[idx];
        }
    }
}