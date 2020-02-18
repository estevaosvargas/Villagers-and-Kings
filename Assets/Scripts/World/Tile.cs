﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Tile
{
    public TypeBlock type;
    public TypeVariante typeVariante;
    public Placer PLACER_DATA;
    public TakeGO typego;

    public bool ConnecyToNightboors = false;
    public bool IsColider = false;

    public int x { get; private set; }
    public int y { get; private set; }
    public int z { get; private set; }

    public int RenderLevel = 0;

    public BiomeType TileBiome;

    [NonSerialized]
    public TransData[] TileTran;
    [NonSerialized]
    public bool havetran = false;

    public bool Emessive = false;
    public bool OcupedByOther = false;

    //public object InsideTile;

    public int Hora;
    public int Dia = 1;
    public int Mes = 1;

    [NonSerialized]
    public DataVector3 CityPoint;

    [NonSerialized]
    public bool OwnedByCity;
    [NonSerialized]
    public float OceanHight = 0;
    /// <summary>
    /// If AI Can Walk.
    /// </summary>
    [NonSerialized]
    public bool CanWalk = false;
    [NonSerialized]
    public bool IsServerTile = false;
    public int HP = 100;
    [NonSerialized]
    public int MaxHP = 100;
    [NonSerialized]
    public int LightOnTile = 0;

    [NonSerialized]
    public Action<Tile> OnTileTypeChange;

    [NonSerialized]
    public GameObject ObjThis;

    [NonSerialized]
    public SpriteRenderer spritetile;

    [NonSerialized]
    public ChunkInfo TileChunk;

    [NonSerialized]
    public TileObj TileObj;

    public void DataTimeSave(int h, int d, int m)
    {
        Hora = DataTime.Hora + h;
        Dia = DataTime.Dia + d;
        Mes = DataTime.Mes + m;
    }

    public void DataTimeSave(int h)
    {
        if (DataTime.Hora + h > 24)
        {
            Hora = 0;
            Hora += h;

            Dia = DataTime.Dia + 1;
            Mes = DataTime.Mes;
        }
        else
        {
            Hora = DataTime.Hora + h;
            Dia = DataTime.Dia;
            Mes = DataTime.Mes;
        }
    }

    public void Reset()
    {
        Hora = -1;
        Dia = -1;
        Mes = -1;
    }

    public Tile()
    {

    }

    public Tile(Tile newtile)
    {
        type = newtile.type;
        typeVariante = newtile.typeVariante;
        PLACER_DATA = newtile.PLACER_DATA;
        typego = newtile.typego;
        ConnecyToNightboors = newtile.ConnecyToNightboors;
        IsColider = newtile.IsColider;
        x = newtile.x;
        y = newtile.y;
        z = newtile.z;
        RenderLevel = newtile.RenderLevel;
        TileBiome = newtile.TileBiome;

        Emessive = newtile.Emessive;
        OcupedByOther = newtile.OcupedByOther;

        Hora = newtile.Hora;
        Dia = newtile.Dia;
        Mes = newtile.Mes;
    }

    public Tile(int x, int y, TypeBlock Type, ChunkInfo ChunkInfo)
    {
        TileChunk = ChunkInfo;
        this.x = x;
        //this.y = y;
        type = Type;
    }

    public Tile(int x, int y, int z, ChunkInfo ChunkInfo)
    {
        TileChunk = ChunkInfo;
        this.x = x;
        //this.y = y;
        this.z = z;

        float persistence = 39.9f;
        float frequency = 0.001f;
        float amplitude = 52.79f;
        int octaves = 184;

        int width = 50;
        int height = 50;

        float Scale = 1f;

        float xCordee = (float)octaves * x / width * Scale;
        float zCordee = (float)octaves * z / height * Scale;

        xCordee *= frequency;
        zCordee *= frequency;

        float sample = (float)new LibNoise.Unity.Generator.Perlin(0.31f, 0.6f, 2.15f, 10, Game.GameManager.Seed, LibNoise.Unity.QualityMode.Low).GetValue(x, z, 0);
        OceanHight = sample;
        if (Game.WorldGenerator.CurrentWorld == WorldType.Caves)
        {
            Debug.LogError("Sorry Caves Is Not enable in alpha!");
        }
        else if (Game.WorldGenerator.CurrentWorld == WorldType.Normal)
        {
            // normal 0.8f
            
            if (sample >= 50f)
            {
                float sample2 = (float)new LibNoise.Unity.Generator.Voronoi(0.01f, 1, Game.GameManager.Seed, false).GetValue(x, z, 0);

                sample2 *= 10;

                PerlinSetType(SetUpBiome(x, z, sample, sample2));
            }
            else if (sample > 0.08f)
            {
                TileBiome = BiomeType.Bench;
                PerlinSetType(Biome.Bench(x, z, this, sample));
            }
            else
            {
                TileBiome = BiomeType.OceanNormal;
                PerlinSetType(Biome.OceanNormal(x, z, this, sample));
            }
        }
    }

    public Tile(int x, int y, int z, ChunkInfo ChunkInfo, bool menu)
    {
        TileChunk = ChunkInfo;
        this.x = x;
        //this.y = y;
        this.z = z;

        float persistence = 39.9f;
        float frequency = 0.001f;
        float amplitude = 52.79f;
        int octaves = 184;

        int width = 50;
        int height = 50;

        float Scale = 1f;

        float xCordee = (float)octaves * x / width * Scale;
        float zCordee = (float)octaves * z / height * Scale;

        xCordee *= frequency;
        zCordee *= frequency;

        float sample = (float)new LibNoise.Unity.Generator.Perlin(0.31f, 0.6f, 2.15f, 10, Game.GameManager.Seed, LibNoise.Unity.QualityMode.Low).GetValue(x, z, 0);

        float sample2 = (float)new LibNoise.Unity.Generator.Voronoi(0.01f, 1, Game.GameManager.Seed, false).GetValue(x, z, 0);

        sample2 *= 10;

        PerlinSetType(SetUpBiome(x, z, sample, sample2));
    }

    public void PerlinSetType(TypeBlock type)
    {
        /*if (typego == TakeGO.empty)
        {
            if (type == TypeBlock.Grass)
            {
                typego = TakeGO.Grass;
            }
        }*/

        this.type = type;
        SetUpTile(this);
    }

    public void DamageBloco(int damage)
    {
        HP -= damage;

        if (HP <= 0)
        {
            switch (type)
            {
                case TypeBlock.Grass:
                    if (typeVariante != TypeVariante.none) { typeVariante = TypeVariante.none; }
                    DamageTypeSet(TypeBlock.DirtGrass);
                    break;
                case TypeBlock.Water:
                    DamageTypeSet(TypeBlock.Water);
                    break;
                case TypeBlock.IceWater:
                    DamageTypeSet(TypeBlock.Water);
                    break;
                case TypeBlock.Rock:
                    DamageTypeSet(TypeBlock.RockGround);
                    break;
                case TypeBlock.IronStone:
                    DamageTypeSet(TypeBlock.RockGround);
                    break;
                case TypeBlock.GoldStone:
                    DamageTypeSet(TypeBlock.RockGround);
                    break;
                case TypeBlock.RockGround:
                    
                    break;
                case TypeBlock.DirtGrass:
                    DamageTypeSet(TypeBlock.Dirt);
                    break;
                case TypeBlock.Dirt:
                    
                    break;
                default:
                    DamageTypeSet(TypeBlock.Air);
                    break;
            }
        }
    }

    //Place a gameobject on tile, and don't remove any thing on tile, object like chest,torch, or other things
    public void SetPlacer(Placer type)
    {
        if (PLACER_DATA != Placer.empty || typego != TakeGO.empty)// if have some object placed on this tile is dont place another with this line
        {
            return;
        }

        PLACER_DATA = type;
        Vector3 vec = Get.PlacerData(type);

        /*for (int xx = 0; xx < (int)vec.x; xx++)
        {
            for (int zz = 0; zz < (int)vec.z; zz++)
            {
                Tile tile_other = Game.WorldGenerator.GetTileAt(x + xx, z + zz);

                tile_other.OcupedByOther = true;
            }
        }*/

        if (Get.GetPlacerEntity(PLACER_DATA))
        {
            GameObject obj = DarckNet.Network.Instantiate(Game.SpriteManager.Getplacerbyname(PLACER_DATA.ToString()), new Vector3(x, y, z), Quaternion.identity, Game.WorldGenerator.World_ID);
            ObjThis = obj;
            TileChunk.ThisChunk.Entitys.Add(obj.GetComponent<ObjectEntity>());
        }
        else
        {
            GameObject obj = GameObject.Instantiate(Game.SpriteManager.Getplacerbyname(PLACER_DATA.ToString()), new Vector3(x, y, z), Quaternion.identity);
            ObjThis = obj;
            TileChunk.ThisChunk.Entitys.Add(obj.GetComponent<ObjectEntity>());
        }

        SaveChunk();
    }

    //For Change the tile to any thing do you want, like change to other, or destroy the tile
    public void DamageTypeSet(TypeBlock type)
    {
        if (typego != TakeGO.empty || PLACER_DATA != Placer.empty)//only to remove or change this tile, need are empty, dont have any object placed on this tile 
        {
            return;
        }

        this.type = type;

        Reset();

        SetUpTile(this);
        DamageSetUp(this);
        SaveChunk();
        OnTileTypeChange(this);
        TileChunk.ThisChunk.TileTransitionChange(this);


        Tile[] neighbor = GetNeighboors(true);

        for (int i = 0; i < neighbor.Length; i++)
        {
            neighbor[i].TileChunk.ThisChunk.TileTransitionChange(neighbor[i]);
            neighbor[i].OnTileTypeChange(neighbor[i]);
            TileChunk.ThisChunk.RefreshChunkTile();
        }

        TileChunk.ThisChunk.RefreshChunkTile();
    }

    public void PlaceBlockSet(TypeBlock type)
    {
        if (typego != TakeGO.empty || PLACER_DATA != Placer.empty)//only to remove or change this tile, need are empty, dont have any object placed on this tile 
        {
            return;
        }

        this.type = type;
        Reset();
        SetUpTile(this);
        SaveChunk();

        OnTileTypeChange(this);
        TileChunk.ThisChunk.TileTransitionChange(this);

        Tile[] neighbor = GetNeighboors(true);

        for (int i = 0; i < neighbor.Length; i++)
        {
            neighbor[i].TileChunk.ThisChunk.TileTransitionChange(neighbor[i]);
            neighbor[i].OnTileTypeChange(neighbor[i]);
        }

        //TileChunk.ThisChunk.RefreshChunk();
    }

    //Save all chunk
    public void SaveChunk()
    {
        TileChunk.ThisChunk.SaveChunk();
    }

    public void RegisterOnTileTypeChange(Action<Tile> callback)
    {
        OnTileTypeChange += callback;
    }

    //To get all neighboors, laterals and diagonals
    public Tile[] GetNeighboors(bool diagonals = false)
    {
        Tile[] neighbors;

        if (diagonals)
        {
            neighbors = new Tile[8];

            neighbors[0] = Game.WorldGenerator.GetTileAt(x, z + 1);//cima
            neighbors[1] = Game.WorldGenerator.GetTileAt(x + 1, z);//direita
            neighbors[2] = Game.WorldGenerator.GetTileAt(x, z - 1);//baixo
            neighbors[3] = Game.WorldGenerator.GetTileAt(x - 1, z);//esquerda

            neighbors[4] = Game.WorldGenerator.GetTileAt(x + 1, z - 1);//corn baixo direita
            neighbors[5] = Game.WorldGenerator.GetTileAt(x - 1, z + 1);//corn cima esquerda
            neighbors[6] = Game.WorldGenerator.GetTileAt(x + 1, z + 1);//corn cima direita
            neighbors[7] = Game.WorldGenerator.GetTileAt(x - 1, z - 1);//corn baixo esuqerda

        }
        else
        {
            neighbors = new Tile[4];

            neighbors[0] = Game.WorldGenerator.GetTileAt(x, z + 1);//cima
            neighbors[1] = Game.WorldGenerator.GetTileAt(x + 1, z);//direita
            neighbors[2] = Game.WorldGenerator.GetTileAt(x, z - 1);//baixo
            neighbors[3] = Game.WorldGenerator.GetTileAt(x - 1, z);//esquerda
        }

        return neighbors;
    }

    //Valus to determine whear what biome is on the positions
    public TypeBlock SetUpBiome(int x, int z, float sample, float sample2)
    {
        if ((int)sample2 == 0)
        {
            //sem nemhum
            TileBiome = BiomeType.ForestNormal;
            return Biome.ForestNormal(x, z, this, sample);
        }
        else if ((int)sample2 == 1)
        {
            //Jungle
            TileBiome = BiomeType.Jungle;
            return Biome.Jungle(x, z, this, sample);
        }
        else if ((int)sample2 == 2)
        {
            //Oceano Normal
            TileBiome = BiomeType.ForestNormal;
            return Biome.ForestNormal(x, z, this, sample);
        }
        else if ((int)sample2 == 3)
        {
            //Deserto
            TileBiome = BiomeType.Montahas;
            return Biome.Montanhas(x, z, this, sample);
        }
        else if ((int)sample2 == 4)
        {
            //sem nemhum
            TileBiome = BiomeType.Plain;
            return Biome.Plaine(x, z, this, sample);
        }
        else if ((int)sample2 == 5)
        {
            //sem nemhum
            TileBiome = BiomeType.Snow;
            return Biome.ForestSnow(x, z, this, sample);
        }
        else if ((int)sample2 == 6)
        {
            //sem nemhum
            TileBiome = BiomeType.Jungle;
            return Biome.Jungle(x, z, this, sample);
        }
        else if ((int)sample2 == 7)
        {
            //sem nemhum
            TileBiome = BiomeType.Desert;
            return Biome.Desert(x, z, this, sample);
        }
        else if ((int)sample2 == -4)
        {
            //sem nemhum
            TileBiome = BiomeType.ForestNormal_Dense;
            return Biome.ForestNormal_Dense(x, z, this, sample);
        }
        else if ((int)sample2 == 8)
        {
            //sem nemhum
            TileBiome = BiomeType.ForestNormal_Dense;
            return Biome.ForestNormal_Dense(x, z, this, sample);
        }
        else if ((int)sample2 == -8)
        {
            //sem nemhum
            TileBiome = BiomeType.ForestNormal_Dense;
            return Biome.ForestNormal_Dense(x, z, this, sample);
        }
        else if ((int)sample2 == -2)
        {
            //sem nemhum
            TileBiome = BiomeType.ForestNormal_Dense;
            return Biome.ForestNormal_Dense(x, z, this, sample);
        }
        else
        {
            Debug.Log("BiomeNum : " + (int)sample2);
            TileBiome = BiomeType.ForestNormal;
            return Biome.ForestNormal(x, z, this, sample);
        }
    }

    private void DamageSetUp(Tile tile)
    {
        switch (tile.type)
        {
            case TypeBlock.DirtGrass:
                tile.DataTimeSave(8);
                Debug.Log(Hora + " : " + Dia);
                break;
            default:
                break;
        }
    }

    public void SetUpTile(Tile tile)
    {
        switch (tile.type)
        {
            case TypeBlock.Sand:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 0;
                tile.CanWalk = true;
                break;
            case TypeBlock.Dirt:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 1;
                tile.CanWalk = false;
                break;
            case TypeBlock.Water:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 2;
                tile.CanWalk = false;
                break;
            case TypeBlock.Bloco:
                tile.ConnecyToNightboors = true;
                tile.IsColider = true;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = -1;
                tile.CanWalk = false;
                break;
            case TypeBlock.Grass:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 35;
                tile.MaxHP = 35;
                tile.RenderLevel = 3;
                tile.CanWalk = true;
                break;
            case TypeBlock.Rock:
                tile.ConnecyToNightboors = true;
                tile.IsColider = true;
                tile.HP = 200;
                tile.MaxHP = 200;
                tile.RenderLevel = 4;
                tile.CanWalk = false;
                break;
            case TypeBlock.RockGround:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 0;
                tile.CanWalk = true;
                break;
            case TypeBlock.DirtGrass:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 0;
                tile.CanWalk = true;
                break;
            case TypeBlock.BeachSand:
                tile.ConnecyToNightboors = true;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 0;
                tile.CanWalk = true;
                break;
            case TypeBlock.DirtRoad:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 2;
                tile.CanWalk = true;
                break;
            default:
                tile.ConnecyToNightboors = false;
                tile.IsColider = false;
                tile.HP = 100;
                tile.MaxHP = 100;
                tile.RenderLevel = 0;
                tile.CanWalk = false;
                break;
        }
    }
    public void RefreshTile()
    {
        SetUpTile(this);

        Tile[] neighbor = GetNeighboors();

        OnTileTypeChange(this);
        TileChunk.ThisChunk.TileTransitionChange(this);

        for (int i = 0; i < neighbor.Length; i++)
        {
            if (neighbor[i] != null)
            {
                neighbor[i].TileChunk.ThisChunk.TileTransitionChange(neighbor[i]);
                neighbor[i].OnTileTypeChange(neighbor[i]);
            }
        }
    }
}