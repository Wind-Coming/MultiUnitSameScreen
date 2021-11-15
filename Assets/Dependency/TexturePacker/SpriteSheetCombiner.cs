#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SBS
{
    static public class SpriteSheetCombiner
    {
        private class Sprite
        {
            public Texture2D tex;
            public string name;
            public Vector2 pivot;

            public Sprite(Texture2D tex, string name, Vector2 pivot)
            {
                this.tex = tex;
                this.name = name;
                this.pivot = pivot;
            }
        }

        [MenuItem("Assets/Sprite Baking Studio/Combine Sprites", false, 1)]
        private static void CombineSprites()
        {
            Object[] selectedObjects = Selection.objects;
            // if (selectedObjects.Length <= 1)
            //     return;

            List<Sprite> sprites = new List<Sprite>();
            List<Texture2D> textures = new List<Texture2D>();

            foreach (Object obj in selectedObjects)
            {
                if (!(obj is Texture2D))
                    continue;
                Texture2D spriteSheetTex = obj as Texture2D;

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
                if (importer == null)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(obj));
                AssetDatabase.Refresh();

                int count = 0;
                if(importer.spriteImportMode == SpriteImportMode.Single )
                {
                    Sprite sprite = new Sprite(spriteSheetTex, spriteSheetTex.name, Vector2.one * 0.5f);
                    sprites.Add(sprite);
                    textures.Add(spriteSheetTex);

                    count++;
                }
                else
                {
                    foreach (SpriteMetaData metaData in importer.spritesheet)
                    {
                        Texture2D tex = new Texture2D((int)metaData.rect.width, (int)metaData.rect.height,
                                                        TextureFormat.ARGB32, false);
                        for (int y = 0; y < tex.height; y++)
                        {
                            for (int x = 0; x < tex.width; x++)
                            {
                                Color color = spriteSheetTex.GetPixel((int)metaData.rect.x + x, (int)metaData.rect.y + y);
                                tex.SetPixel(x, y, color);
                            }
                        }

                        Sprite sprite = new Sprite(tex, metaData.name, metaData.pivot);
                        sprites.Add(sprite);
                        textures.Add(tex);

                        count++;
                    }
                }
            }

            Texture2D newSpriteSheet = new Texture2D(2048, 2048, TextureFormat.ARGB32, false);
            Rect[] texRects = newSpriteSheet.PackTextures(textures.ToArray(), 0, 2048, false);
            for (int i = 0; i < sprites.Count; i++)
            {
                Texture2D tex = sprites[i].tex;
                float newX = texRects[i].x * newSpriteSheet.width;
                float newY = texRects[i].y * newSpriteSheet.height;
                texRects[i] = new Rect(newX, newY, texRects[i].width * newSpriteSheet.width, texRects[i].height * newSpriteSheet.height);
            }

            Texture2D firstSpriteSheet = selectedObjects[0] as Texture2D;
            string filePath = AssetDatabase.GetAssetPath(firstSpriteSheet);
            string dirPath = filePath.Remove(filePath.LastIndexOf('/'));

            string fileName = "";
            for (int i = 0; i < selectedObjects.Length; ++i)
            {
                if (!(selectedObjects[i] is Texture2D))
                    continue;
                Texture2D spriteSheetTex = selectedObjects[i] as Texture2D;
                if (i > 0)
                    fileName += "+";
                fileName += spriteSheetTex.name;
            }
            string[] names = fileName.Split('_');
            fileName = names[0];
            filePath = dirPath + "/" + fileName + ".png";

            byte[] bytes = newSpriteSheet.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.Refresh();

            TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
            if (texImporter != null)
            {
                texImporter.textureType = TextureImporterType.Sprite;
                texImporter.spriteImportMode = SpriteImportMode.Multiple;
                texImporter.maxTextureSize = 2048;

                int texCount = sprites.Count;
                SpriteMetaData[] metaData = new SpriteMetaData[texCount];
                for (int i = 0; i < texCount; i++)
                {
                    metaData[i].name = sprites[i].name;
                    metaData[i].rect = texRects[i];
                    metaData[i].alignment = (int)SpriteAlignment.Custom;
                    metaData[i].pivot = sprites[i].pivot;
                }
                texImporter.spritesheet = metaData;

                AssetDatabase.ImportAsset(filePath);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif