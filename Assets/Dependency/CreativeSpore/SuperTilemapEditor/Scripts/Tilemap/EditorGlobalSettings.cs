using UnityEngine;
using System.Collections;

namespace CreativeSpore.SuperTilemapEditor
{
    public static class EditorGlobalSettings
    {

        public static int Color32ToInt(Color32 color)
        {
            return (color.r << 24 | color.g << 16 | color.b << 8 | color.a);
        }

        public static Color32 IntToColor32(int value)
        {
            return new Color32( (byte)((value >> 24) & 0xFF), (byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
        }

        public static Color TilemapColliderColor
        {
            get
            {
                return IntToColor32(PlayerPrefs.GetInt("STE_TilemapColliderColor", Color32ToInt(new Color32(0, 255, 0, 160))));
            }
            set { PlayerPrefs.SetInt("STE_TilemapColliderColor", Color32ToInt(value)); }
        }

        public static Color TilemapGridColor
        {
            get
            {
                return IntToColor32(PlayerPrefs.GetInt("STE_TilemapGridColor", Color32ToInt(new Color32(255, 255, 255, 60))));
            }
            set { PlayerPrefs.SetInt("STE_TilemapGridColor", Color32ToInt(value)); }
        }
    }
}