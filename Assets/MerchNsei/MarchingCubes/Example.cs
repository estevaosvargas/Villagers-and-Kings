﻿using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralNoiseProject;
using System.Linq;

namespace MarchingCubesProject
{

    public enum MARCHING_MODE {  CUBES, TETRAHEDRON };

    [System.Serializable]
    public struct TileNew
    {
        public int x;
        public int y;
        public int z;

        public List<Vector3> vertsTile;

        public float density;
        public TypeBlocks typeblock;
        public TakeGO tileObject;
    }

    public class Example : MonoBehaviour
    {
        public static int Size = 10;
        public TileNew[,,] Tiles;

        public Material m_material;
        public Material m_Grass;

        public void MakeMesh(TileThreadHelp tilethred)
        {
            List<CombineInstance> combine = new List<CombineInstance>();
            List<Material> Meterials = new List<Material>();

            Tiles = tilethred.voxels;

            for (int m = 0; m < tilethred.MeshList.Count; m++)
            {
                List<Vector3> splitVerts = new List<Vector3>();
                List<int> splitIndices = new List<int>();

                Vector3[] arrayseinao = tilethred.MeshList.Values.ToArray()[m].ToArray();

                for (int j = 0; j < arrayseinao.Length; j++)
                {
                    splitVerts.Add(arrayseinao[j]);
                    splitIndices.Add(j);
                }

                if (splitVerts.Count == 0) continue;

                Mesh mesh = new Mesh();

                mesh.SetVertices(splitVerts);
                mesh.SetTriangles(splitIndices, 0);
                mesh.RecalculateNormals();

                CombineInstance intancecombine = new CombineInstance();

                intancecombine.mesh = mesh;
                intancecombine.transform = Matrix4x4.identity;

                if (tilethred.MeshList.Keys.ToArray()[m] == TypeBlocks.grass)
                {
                    if (!Meterials.Contains(m_Grass))
                    {
                        Meterials.Add(m_Grass);
                    }
                }
                else
                {
                    if (!Meterials.Contains(m_material))
                    {
                        Meterials.Add(m_material);
                    }
                }

                combine.Add(intancecombine);
            }

            GameObject finalmesh = new GameObject("finalmesh");
            finalmesh.AddComponent<MeshFilter>();
            finalmesh.AddComponent<MeshRenderer>().materials = Meterials.ToArray();
            finalmesh.transform.SetParent(transform, true);


            finalmesh.GetComponent<MeshFilter>().mesh = new Mesh();
            finalmesh.GetComponent<MeshFilter>().mesh.CombineMeshes(combine.ToArray(), false);
            finalmesh.AddComponent<MeshCollider>().sharedMesh = finalmesh.GetComponent<MeshFilter>().mesh;
            finalmesh.transform.localPosition = Vector3.zero;

            //The chunk terrain is finished, nos is the datails

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        SetUpTileTree(Tiles[x, y, z]);
                    }
                }
            }
        }

        private void SetUpTileTree(TileNew tile)
        {
            System.Random ran = new System.Random(0);

            if (tile.tileObject != TakeGO.empty)
            {
                GameObject trees = Instantiate(GetPrefabOnRecources("Prefabs/Trees/" + tile.tileObject.ToString()), new Vector3(tile.x, tile.y, tile.z), Quaternion.identity);
                trees.transform.SetParent(this.transform, true);


                System.Random randomValue = new System.Random((int)tile.x + (int)tile.z);
                float size = ran.Next((int)0f, (int)0.5f);
                trees.transform.position = new Vector3(tile.x, tile.y, tile.z);
                //trees.transform.localScale = new Vector3(trees.transform.localScale.x + size, trees.transform.localScale.y + size, trees.transform.localScale.z + size);

                if (trees.GetComponent<Trees>())
                {
                    trees.GetComponent<Trees>().ThisTreeTile = null;
                }
            }
        }

        public GameObject GetPrefabOnRecources(string path)
        {
            GameObject sprites = Resources.Load<GameObject>(path);

            if (sprites)
            {
                return sprites;
            }

            Debug.LogError("Don't find this file: " + path);
            return null;
        }
    }
}

public enum TypeBlocks : byte
{
    none, grass, stone
}

public struct VerticeVoxel
{
    public Vector3 vert;
    public TypeBlocks type;

    public VerticeVoxel(Vector3 _vert, TypeBlocks _typeBlocks)
    {
        vert = _vert;
        type = _typeBlocks;
    }
}

public struct VoxelData
{
    public float value;
    public TypeBlocks type;

    public VoxelData(float _value, TypeBlocks _typeBlocks)
    {
        value = _value;
        type = _typeBlocks;
    }
}