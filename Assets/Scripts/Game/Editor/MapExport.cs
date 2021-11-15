using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapExport : Editor
{

    private static Vector4 GetSpreiteRect(Sprite sprite)
    {
        var uvs = sprite.uv;
        Vector4 rect = new Vector4();
        rect[0] = uvs[1].x - uvs[0].x;
        rect[1] = uvs[0].y - uvs[2].y;
        rect[2] = uvs[2].x;
        rect[3] = uvs[2].y;
        return rect;
    }

    [MenuItem("Assets/ExportTile")]
    static void Exprot()
    {
        if(Selection.activeObject == null)
        {
            Debug.LogError("请选择要导出的tile prefab！");
            return;
        }

        TileData td = AssetDatabase.LoadAssetAtPath("Assets/Res/Map/MapData/" + Selection.activeObject.name + ".asset", typeof(TileData)) as TileData;
        if(td == null){
            td = ScriptableObject.CreateInstance<TileData>();
            AssetDatabase.CreateAsset(td, "Assets/Res/Map/MapData/" + Selection.activeObject.name + ".asset");
        }

        GameObject go = Selection.activeObject as GameObject;
        LodObj[] lodObjs = go.GetComponentsInChildren<LodObj>(true);

        for(int lod = 5; lod > 0; lod --)
        {
            List<TileUnit> unitsList = td.GetUnits(lod);
            unitsList.Clear();

            for(int i = 0; i < lodObjs.Length; i++)
            {
                LodObj lo = lodObjs[i];
                for(int j = 0; j < lo.LodGameObjs.Length; j++)
                {
                    LodGameObj lgo = lo.LodGameObjs[j];
                    if(lgo.lodRange.x <= lod && lgo.lodRange.y >= lod)
                    {
                        SpriteRenderer[] srs = lgo.obj.GetComponentsInChildren<SpriteRenderer>(true);
                        for(int sn = 0; sn < srs.Length; sn ++)
                        {
                            SpriteRenderer sr = srs[sn];
                            TileUnit tu = new TileUnit();
                            tu.isSprite = true;
                            tu.mesh = Resources.GetBuiltinResource(typeof(Mesh) ,"Quad.fbx") as Mesh;
                            tu.material = sr.sharedMaterial;
                            tu.scale = sr.transform.lossyScale;
                            tu.rect = GetSpreiteRect(sr.sprite);
                            tu.uvs = sr.sprite.uv;
                            tu.localPosition = sr.transform.position;
                            unitsList.Add(tu);
                        }

                        MeshRenderer[] mrs = lgo.obj.GetComponentsInChildren<MeshRenderer>(true);
                        for(int sn = 0; sn < mrs.Length; sn ++)
                        {
                            MeshRenderer sr = mrs[sn];
                            TileUnit tu = new TileUnit();
                            tu.isSprite = false;
                            tu.mesh = sr.gameObject.GetComponent<MeshFilter>().sharedMesh;
                            tu.material = sr.sharedMaterial;
                            tu.scale = sr.transform.lossyScale;
                            tu.localPosition = sr.transform.position;
                            tu.rotation = sr.transform.rotation;
                            unitsList.Add(tu);
                        }
                    }
                }
            }
        }

        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();
    }

}
