using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpriteDrawTest : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public Sprite[] sprites;

    private MaterialPropertyBlock block;
    List<Matrix4x4[]> allMatrices = new List<Matrix4x4[]>();

    private Vector4 GetSpreiteRect(Sprite sprite)
    {
        var uvs = sprite.uv;
        Vector4 rect = new Vector4();
        rect[0] = uvs[1].x - uvs[0].x;
        rect[1] = uvs[0].y - uvs[2].y;
        rect[2] = uvs[2].x;
        rect[3] = uvs[2].y;
        return rect;
    }

    void OnEnable()
    {
        if(sprites == null || sprites.Length == 0)
        {
            return;
        }


        block = new MaterialPropertyBlock();
        List<Vector4> rects = new List<Vector4>();
        for(int i = 0; i < 1023; i++)
        {
            rects.Add(GetSpreiteRect(sprites[Random.Range(0, sprites.Length)]));
        }
        block.SetVectorArray("_Offset", rects);
    }

    // Update is called once per frame
    void Update()
    {
        if(mesh == null || material == null || sprites == null)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            Matrix4x4[] m = new Matrix4x4[1023];
            for(int i = 0; i < m.Length; i++)
            {
                m[i] = Matrix4x4.TRS(new Vector3(i / 50, 0, i % 50), Quaternion.Euler(45, 0, 0), Vector3.one);
            }
            allMatrices.Add(m);
        }

        for(int i = 0; i < allMatrices.Count; i++)
            Graphics.DrawMeshInstanced(mesh, 0, material, allMatrices[i], allMatrices[i].Length, block);
    }
}
