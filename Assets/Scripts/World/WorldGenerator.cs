﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class WorldGenerator : DCallBack
{
    public static WorldGenerator Instance;
    public Transform Player;
    public UnityStandardAssets.Utility.SmoothFollow Cam;
    public GameObject ChunkGO;
    public int RenderDistance;
    public int DisRenderDistance = 5;

    Dictionary<Vector3, Chunk> chunkMap;
    Dictionary<Vector3, bool> ClientchunkMap;
    public List<Chunk> ChunksList = new List<Chunk>();

    public Transform SlectedBlock;
    public WorldType CurrentWorld;
    public int Seed = 0;
    public Texture2D HeightTeste;

    [Header("TimeData")]
    public int h;
    public int d;
    public int m;

    void Awake()
    {
        Instance = this;
        chunkMap = new Dictionary<Vector3, Chunk>();
        ClientchunkMap = new Dictionary<Vector3, bool>();
#if Client
        Seed = Game.GameManager.Seed;

        Player = Game.GameManager.MyPlayer.MyObject.transform;
        Player.GetComponent<EntityPlayer>().World = this.transform;
        Cam.target = Game.GameManager.MyPlayer.MyObject.transform;
#endif
    }

    public override void OnRespawn()
    {
        Player = Game.GameManager.MyPlayer.MyObject.transform;
        Player.GetComponent<EntityPlayer>().World = this.transform;
        Cam.target = Game.GameManager.MyPlayer.MyObject.transform;

        base.OnRespawn();
    }

    void Start()
    {
        UpdateFindChunk();
    }


    public void UpdateFindChunk()
    {
        if (Game.GameManager.SinglePlayer)
        {
            FindChunksToLoad();
            DeleteChunk();
        }
        else if (Game.GameManager.MultiPlayer)
        {
            NetFindChunksToLoad();
            NetFindDeleteChunk();
        }
    }

    void Update()
    {
    #if Client
        if (MouselockFake.IsLock == false)
        {
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                if (Cam.GetComponent<Camera>().orthographicSize > 90)
                {
                    Cam.GetComponent<Camera>().orthographicSize -= 1;
                }
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                if (Cam.GetComponent<Camera>().fieldOfView < 20)
                {
                    Cam.GetComponent<Camera>().fieldOfView += 1;
                }
            }

            if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                Cam.GetComponent<Camera>().fieldOfView = 60f;
            }

            h = DataTime.Hora;
            d = DataTime.Dia;
            m = DataTime.Mes;
        }
    #endif
    }

    void FindChunksToLoad()
    {
        if (Player)
        {
            int xPos = (int)Player.position.x;
            int zPos = (int)Player.position.z;

            for (int i = -RenderDistance; i < RenderDistance; i++)
            {
                for (int z = -RenderDistance; z < RenderDistance; z++)
                {
                    MakeChunkAt(xPos + i, zPos + z);
                }
            }
        }

        /*for (int i = xPos - Chunk.Size; i < xPos + (2 * Chunk.Size); i += Chunk.Size)
        {
            for (int j = yPos - Chunk.Size; j < yPos + (2 * Chunk.Size); j += Chunk.Size)
            {
                MakeChunkAt(i, j);
            }
        }*/
    }

    void MakeChunkAt(int x, int z)
    {
        x = Mathf.FloorToInt(x / (float)Chunk.Size) * Chunk.Size;
        z = Mathf.FloorToInt(z / (float)Chunk.Size) * Chunk.Size;

        if (chunkMap.ContainsKey(new Vector3(x, 0, z)) == false)
        {
            GameObject go = Instantiate(ChunkGO, new Vector3(x, 0, z), Quaternion.identity);

            chunkMap.Add(new Vector3(x, 0,z), go.GetComponent<Chunk>());
        }
    }

    void DeleteChunk()
    {
        if (Player)
        {
            List<Chunk> DeleteChuks = new List<Chunk>(chunkMap.Values);
            Queue<Chunk> deletechuks = new Queue<Chunk>();

            for (int i = 0; i < DeleteChuks.Count; i++)
            {
                float Distanc = Vector3.Distance(Player.position, DeleteChuks[i].transform.position);

                if (Distanc > DisRenderDistance * Chunk.Size)
                {
                    deletechuks.Enqueue(DeleteChuks[i]);
                }

                while (deletechuks.Count > 0)
                {
                    Chunk chuks = deletechuks.Dequeue();
                    chunkMap.Remove(chuks.transform.position);
                    Destroy(chuks.gameObject);
                }
            }
        }
    }

    public Chunk GetChunkAt(int x, int z)
    {
        x = Mathf.FloorToInt(x / (float)Chunk.Size) * Chunk.Size;
        z = Mathf.FloorToInt(z / (float)Chunk.Size) * Chunk.Size;

        Vector3 chunkpos = new Vector3(x, 0, z);

        if (chunkMap.ContainsKey(chunkpos))
        {
            return chunkMap[chunkpos];
        }
        else
        {
            return null;
        }
    }

    public Tile GetTileAt(int x, int z)
    {
        Chunk chunk = GetChunkAt(x, z);

        if (chunk != null)
        {
            return chunk.tiles[x - (int)chunk.transform.position.x, z - (int)chunk.transform.position.z];
        }
        return null;
    }

    public Tile GetTileAt(float x, float z)
    {
        int mx = Mathf.FloorToInt(x);
        int mz = Mathf.FloorToInt(z);

        Chunk chunk = GetChunkAt(mx, mz);

        if (chunk != null)
        {
            return chunk.tiles[mx - (int)chunk.transform.position.x, mz - (int)chunk.transform.position.z];
        }
        return null;
    }

    public Vector3 GetPos(float x, float z)
    {
        int mx = Mathf.FloorToInt(x);
        int mz = Mathf.FloorToInt(z);

        Chunk chunk = GetChunkAt(mx, mz);

        if (chunk != null)
        {
            return new Vector3(mx - (int)chunk.transform.position.x, 0, mz - (int)chunk.transform.position.z);
        }
        return Vector3.zero;
    }

    //<!---------------------------------------->

    void NetFindChunksToLoad()
    {
        if (Player)
        {
            int xPos = (int)Player.position.x;
            int zPos = (int)Player.position.z;

            int xx = 0;
            int zz = 0;

            for (int i = -RenderDistance; i < RenderDistance; i++)
            {
                for (int z = -RenderDistance; z < RenderDistance; z++)
                {
                    xx = xPos + i;
                    zz = zPos + z;

                    xx = Mathf.FloorToInt(xx / (float)Chunk.Size) * Chunk.Size;
                    zz = Mathf.FloorToInt(zz / (float)Chunk.Size) * Chunk.Size;

                    if (ClientchunkMap.ContainsKey(new Vector3(xx, 0, zz)) == false)
                    {
                        Game.GameManager.GenerateChunkNet(xPos + i, zPos + z);
                        ClientchunkMap.Add(new Vector3(xx, 0, zz), true);
                    }
                }
            }
        }
    }

    void NetFindDeleteChunk()
    {
        if (Player)
        {
            List<Chunk> DeleteChuks = new List<Chunk>(chunkMap.Values);
            Queue<Chunk> deletechuks = new Queue<Chunk>();
            for (int i = 0; i < DeleteChuks.Count; i++)
            {
                float Distanc = Vector3.Distance(Player.position, DeleteChuks[i].transform.position);
                if (Distanc > DisRenderDistance * Chunk.Size)
                {
                    deletechuks.Enqueue(DeleteChuks[i]);
                }
                while (deletechuks.Count > 0)
                {
                    Chunk chuks = deletechuks.Dequeue();
                    Game.GameManager.DeleteChunkNet((int)chuks.transform.position.x, (int)chuks.transform.position.z);
                    ClientchunkMap.Remove(chuks.transform.position);
                    chunkMap.Remove(chuks.transform.position);
                    Destroy(chuks.gameObject);
                }
            }
        }
    }

    public void ClientMakeChunkAt(int x, int z, TileSave[] tile)
    {
        x = Mathf.FloorToInt(x / (float)Chunk.Size) * Chunk.Size;
        z = Mathf.FloorToInt(z / (float)Chunk.Size) * Chunk.Size;

        if (chunkMap.ContainsKey(new Vector3(x, 0, z)) == false)
        {
            GameObject go = Instantiate(ChunkGO, new Vector3(x, 0, z), Quaternion.identity);
            chunkMap.Add(new Vector3(x, 0,z), go.GetComponent<Chunk>());
            go.GetComponent<Chunk>().ClientSetUpChunk(tile);
        }
    }

    public TileSave[] ServerMakeChunkAt(int x, int z, long unique)
    {
        x = Mathf.FloorToInt(x / (float)Chunk.Size) * Chunk.Size;
       z = Mathf.FloorToInt(z / (float)Chunk.Size) * Chunk.Size;

        if (chunkMap.ContainsKey(new Vector3(x, 0, z)) == false)
        {
            GameObject go = Instantiate(ChunkGO, new Vector3(x, 0, z), Quaternion.identity);

            chunkMap.Add(new Vector3(x, 0,z), go.GetComponent<Chunk>());

            go.GetComponent<Chunk>().Players.Add(unique);

            return go.GetComponent<Chunk>().MakeChunk();
        }
        else
        {
            return chunkMap[new Vector3(x, 0, z)].Tiles_Save.ToArray();
        }
    }

    public void NetDeleteChunk(int x, int z, long player)
    {
        if (chunkMap.ContainsKey(new Vector3(x, 0,z)) == true && chunkMap[new Vector3(x, 0,z)].Players.Count == 0 || chunkMap.ContainsKey(new Vector3(x, 0, z)) == true && chunkMap[new Vector3(x, 0, z)].Players.Count == 1 && chunkMap[new Vector3(x, 0, z)].Players[0] == player)
        {
            Chunk chunk = chunkMap[new Vector3(x, 0,z)];
            chunkMap[new Vector3(x, 0, z)].Players.RemoveAt(0);
            chunkMap.Remove(new Vector3(x, 0, z));
            Destroy(chunk.gameObject);
        }
        else if (chunkMap.ContainsKey(new Vector3(x, 0, z)) == true)
        {
            chunkMap[new Vector3(x, 0, z)].Players.Remove(player);
        }
    }
}

public enum WorldType
{
    Normal = 0, DemondWorld = 1, Caves = 2, Dungeons = 3, Sky = 4
}
