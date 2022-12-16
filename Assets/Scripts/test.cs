using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class test : MonoBehaviour
{
    private MeshFilter filter;
    private Mesh mesh;
    private int frameCounter;
    Vector3[] vertices;
    // Start is called before the first frame update
    void Start()
    {
        filter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        filter.mesh = mesh;
        InitMesh();
        
    }

    void InitMesh() {
        mesh.name = "MyMesh";
        vertices = new Vector3[4];

        vertices[0] = new Vector3(1, 1, 0);
        vertices[1] = new Vector3(-1, 1, 0);
        vertices[2] = new Vector3(1, -1, 0);
        vertices[3] = new Vector3(-1, -1, 0);
        mesh.vertices = vertices;
        int[] triangles = new int[2 * 3] {
            0,3,1,0,2,3
        };
        mesh.SetIndices(triangles,MeshTopology.Points,0);
    }

    void updateVertices() {
        mesh.vertices[2].z += 0.1f;
        Debug.Log(mesh.vertices[2].z);
    }

    // Update is called once per frame
    void Update()
    {
        ++frameCounter;
        vertices[2] = new Vector3(1,(float)(-1+frameCounter*0.0001f),0);
        mesh.vertices = vertices;

    }
}
