using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigMeshTest : MonoBehaviour
{
    private List<Mesh> meshes = new List<Mesh>();
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < 20; i++)
        {
            UpdateMesh();
        }
    }

    public void UpdateMesh()
    {
        int num_tiles = 2000;
        int num_vertices = num_tiles * 4;
        int num_triangles = num_tiles * 6;

        // Generate the mesh data
        Vector3[] vertices = new Vector3[num_vertices];
        Vector3[] normals = new Vector3[num_vertices];
        Vector2[] uv = new Vector2[num_vertices];

        int[] triangles = new int[num_triangles];

        int index = 0;
        for (int i = 0; i < num_tiles; i++)
        {
            normals[index] = Vector3.up;
            vertices[index] = new Vector3(i / 50 - 0.5f, 0, i % 50 - 0.5f);
            uv[index] = new Vector2(0, 0);
            index++;

            normals[index] = Vector3.up;
            vertices[index] = new Vector3(i / 50 - 0.5f, 0, i % 50 + 0.5f);
            uv[index] = new Vector2(0, 1);
            index++;

            normals[index] = Vector3.up;
            vertices[index] = new Vector3(i / 50 + 0.5f, 0, i % 50 + 0.5f);
            uv[index] = new Vector2(1, 1);
            index++;

            normals[index] = Vector3.up;
            vertices[index] = new Vector3(i / 50 + 0.5f, 0, i % 50 - 0.5f);
            uv[index] = new Vector2(1, 0);
            index++;
        }


        // Populate triangles
        for (int z = 0; z < num_tiles; z++)
        {
            triangles[z * 6 + 0] = z * 4 + 0;
            triangles[z * 6 + 1] = z * 4 + 1;
            triangles[z * 6 + 2] = z * 4 + 2;

            triangles[z * 6 + 3] = z * 4 + 0;
            triangles[z * 6 + 4] = z * 4 + 2;
            triangles[z * 6 + 5] = z * 4 + 3;
        }

        // Create a new mesh and populate with the data
        Mesh m_kMesh = new Mesh();
        m_kMesh.vertices = vertices;
        m_kMesh.triangles = triangles;
        m_kMesh.normals = normals;
        m_kMesh.uv = uv;
        meshes.Add(m_kMesh);

        matrices.Add(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UpdateMesh();
        }

        for(int i = 0; i < meshes.Count; i++)
        {
            Graphics.DrawMesh(meshes[i], Vector3.zero, Quaternion.identity, material, 0);
        }
        //Graphics.DrawMeshInstanced(meshes[0], 0, material, matrices);
    }
}
