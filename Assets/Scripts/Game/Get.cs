﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Get
{
    public static BiomeType[,] BiomeTable = new BiomeType[6, 6] {   
    //COLDEST        //COLDER          //COLD                  //HOT                          //HOTTER                       //HOTTEST
    { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYEST
    { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYER
    { BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //DRY
    { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //WET
    { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest },  //WETTER
    { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
    };

    public static float ColdestValue = 0.05f;
    public static float ColderValue = 0.18f;
    public static float ColdValue = 0.4f;
    public static float WarmValue = 0.6f;
    public static float WarmerValue = 0.8f;
    public static float DryerValue = 0.27f;
    public static float DryValue = 0.4f;
    public static float WetValue = 0.6f;
    public static float WetterValue = 0.8f;
    public static float WettestValue = 0.9f;

    public static bool GetMouseIteract(Block t)
    {
        if (t.Type == TypeBlock.LightBlockON)
        {
            return true;
        }
        else if (t.PLACER_DATA == Placer.BauWood || t.PLACER_DATA == Placer.BauGold || t.PLACER_DATA == Placer.BauDiamond || t.PLACER_DATA == Placer.BauDark)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static Color GetColorTile(Block block)
    {
        switch (block.Type)
        {
            case TypeBlock.RockGround:
                return Color.white;
            case TypeBlock.RockHole:
                return Color.white;
            case TypeBlock.Grass:
                return Color.white;
            case TypeBlock.GoldStone:
                return Color.white;
            case TypeBlock.IronStone:
                return Color.white;
            case TypeBlock.Rock:
                return Color.white;
            case TypeBlock.Sand:
                return Color.white;
            case TypeBlock.Dirt:
                return Color.white;
            case TypeBlock.DirtRoad:
                return Color.white;
            case TypeBlock.IceWater:
                return Color.white;
            case TypeBlock.Snow:
                return Color.white;
            case TypeBlock.BeachSand:
                return Color.white;
            case TypeBlock.WaterFloor:
                return new Color(1,1,1,0);
            case TypeBlock.JungleGrass:
                return Color.white;
            default:
                return Color.white;
        }
    }

    public static Vector3 GetLayerVerticesPlus(int index, int layer)
    {
        switch (layer)
        {
            case 0:
                return GetVert(index, 0.1f);
            case 1:
                return GetVert(index, 0.1f);
            case 2:
                return GetVert(index, 0.2f);
            case 3:
                return GetVert(index, 0.3f);
            case 4:
                return GetVert(index, 0.4f);
            case 5:
                return GetVert(index, 0.5f);
            case 6:
                return GetVert(index, 0.6f);
            case 7:
                return GetVert(index, 0.7f);
            case 8:
                return GetVert(index, 0.8f);
            case 9:
                return GetVert(index, 0.9f);
            case 10:
                return GetVert(index, 1.0f);
            default:
                return GetVert(index, 1.0f);
        }
    }

    public static float GetLayerVerticesPlusY(int layer)
    {
        switch (layer)
        {
            case 0:
                return 0.1f;
            case 1:
                return 0.1f;
            case 2:
                return 0.2f;
            case 3:
                return 0.3f;
            case 4:
                return 0.4f;
            case 5:
                return 0.5f;
            case 6:
                return 0.6f;
            case 7:
                return 0.7f;
            case 8:
                return 0.8f;
            case 9:
                return 0.9f;
            case 10:
                return 1.0f;
            default:
                return 1.0f;
        }
    }

    public static bool CanPlaceTreeBiome(float noise)
    {
        if (noise >= -1.0f && noise < 0)
        {
            return true;
        }
        return false;
    }

    public static Vector3 GetVert(int index, float y)
    {
        Vector3[] Verts = new Vector3[8]
        {
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(1.0f,0.0f,0.0f),
            new Vector3(1.0f,y,0.0f),
            new Vector3(0.0f,y,0.0f),
            new Vector3(0.0f,0.0f,1.0f),
            new Vector3(1.0f,0.0f,1.0f),
            new Vector3(1.0f,y,1.0f),
            new Vector3(0.0f,y,1.0f)
        };

        return Verts[index];
    }

    public static readonly Vector3[] Verts = new Vector3[8]
    {
        new Vector3(0.0f,0.0f,0.0f),
        new Vector3(1.0f,0.0f,0.0f),
        new Vector3(1.0f,1.0f,0.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,0.0f,1.0f),
        new Vector3(1.0f,0.0f,1.0f),
        new Vector3(1.0f,1.0f,1.0f),
        new Vector3(0.0f,1.0f,1.0f)
    };

    public static readonly int[,] Tris = new int[6, 4]
    {
        {0,3,1,2}, //Back Face 0
        {5,6,4,7},  //Front Face 1
        {3,7,2,6},  //Top Face 2
        {1,5,0,4},  //Bottom Face 3 
        {4,7,0,3},  //Left Face 4
        {1,2,5,6}   //Right Face 5
    };

    public static bool OpenInveTile(Block t)
    {
        switch (t.PLACER_DATA)
        {
            case Placer.BauDark:
                return true;
            case Placer.BauDiamond:
                return true;
            case Placer.BauGold:
                return true;
            case Placer.BauWood:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Used Yo Verify if this block ar surround by tile can have a transsition
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool HaveBlend(TypeBlock type)
    {
        bool value = false;

        switch (type)
        {
            case TypeBlock.Grass:
                value = true;
                break;
            case TypeBlock.BeachSand:
                value = true;
                break;
            case TypeBlock.Rock:
                value = true;
                break;
            default:
                value = false;
                break;
        }

        return value;
    }

    /// <summary>
    /// if this tile can calculate, trassitions
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool TileCanDoBlend(TypeBlock type)
    {
        bool value = false;

        switch (type)
        {
            case TypeBlock.Rock:
                value = false;
                break;
            default:
                value = true;
                break;
        }

        return value;
    }

    public static int GetTileRenIndex(TypeBlock tile)
    {
        switch (tile)
        {
            case TypeBlock.Water:
                return 1;
            case TypeBlock.Sand:
                return 2;
            case TypeBlock.BeachSand:
                return 2;
            case TypeBlock.Dirt:
                return 3;
            case TypeBlock.Grass:
                return 4;
            case TypeBlock.Rock:
                return 10;
            default:
                return 0;
        }
    }

    public static bool EntityCanSpawn(Block block)
    {
        switch (block.Type)
        {
            case TypeBlock.Air:
                return false;
            case TypeBlock.RockGround:
                return false;
            case TypeBlock.Grass:
                return true;
            case TypeBlock.Water:
                return false;
            case TypeBlock.GoldStone:
                return true;
            case TypeBlock.IronStone:
                return true;
            case TypeBlock.Rock:
                return false;
            case TypeBlock.Sand:
                return true;
            case TypeBlock.Bloco:
                return false;
            case TypeBlock.Dirt:
                return true;
            case TypeBlock.DirtRoad:
                return true;
            case TypeBlock.IceWater:
                return false;
            case TypeBlock.Snow:
                return true;
            case TypeBlock.LightBlockON:
                return false;
            case TypeBlock.BeachSand:
                return false;
            case TypeBlock.WaterFloor:
                return false;
            case TypeBlock.JungleGrass:
                return true;
            default:
                return false;
        }
    }

    public static bool HaveCollision(Block block)
    {
        if (block == null)
        {
            return true;
        }

        switch (block.Type)
        {
            case TypeBlock.Air:
                return false;
            case TypeBlock.RockGround:
                return true;
            case TypeBlock.Grass:
                return true;
            case TypeBlock.Water:
                return true;
            case TypeBlock.GoldStone:
                return true;
            case TypeBlock.IronStone:
                return true;
            case TypeBlock.Rock:
                return true;
            case TypeBlock.Sand:
                return true;
            case TypeBlock.Bloco:
                return true;
            case TypeBlock.Dirt:
                return true;
            case TypeBlock.DirtRoad:
                return true;
            case TypeBlock.IceWater:
                return true;
            case TypeBlock.Snow:
                return true;
            case TypeBlock.LightBlockON:
                return true;
            case TypeBlock.BeachSand:
                return true;
            case TypeBlock.WaterFloor:
                return true;
            case TypeBlock.JungleGrass:
                return true;
            default:
                return true;
        }
    }

    /// <summary>
    /// Check if the Neighboors tile, can be tarsition by the main tile
    /// </summary>
    /// <returns></returns>
    public static bool CanTransitionTo(TypeBlock MainTile, TypeBlock Neighboor)
    {
        switch (MainTile)
        {
            case TypeBlock.Grass:
                return true;
            case TypeBlock.BeachSand:
                switch (Neighboor)
                {
                    case TypeBlock.Water:
                        return true;
                    case TypeBlock.Sand:
                        return true;
                    default:
                        return false;
                }
            default:
                return true;
        }
    }

    public static Color ColorBiome(BiomeType tilebiome, TypeBlock tile)
    {
        switch (tilebiome)
        {
            case BiomeType.TropicalRainforest:
                if (tile == TypeBlock.Grass)
                {
                    return new Color(0.3128338f, 0.6981132f, 0.3446094f, 1);
                }
                else
                {
                    return Color.white;
                }
            default:
                break;
        }
        return Color.white;
    }

    public static bool GetPlacerEntity(Placer placer)
    {
        switch (placer)
        {
            case Placer.BauWood:
                return true;
            default:
                return false;
        }
    }

    public static Vector3 PlacerData(Placer placer)
    {
        switch (placer)
        {
            case Placer.VillagerHouse:
                return new Vector3(6,0,4);
            default:
                return Vector3.zero;
        }
    }
}

public enum BiomeType : byte
{
    None,
    Bench,
    Desert,
    Savanna,
    TropicalRainforest,
    Grassland,
    Woodland,
    SeasonalForest,
    TemperateRainforest,
    BorealForest,
    Tundra,
    Ice
}

public enum TypeBlock : byte
{
    Air, RockGround, RockHole,
    Grass, Water, GoldStone, IronStone,
    Rock, DirtGrass, Sand,
    Bloco, Dirt, DirtRoad, IceWater, Snow, LightBlockON,
    BeachSand, WaterFloor, JungleGrass
}

public enum TypeVariante : byte
{
    none, GrassFL1, GrassFL2, GrassRC
}

public enum TakeGO : byte
{
    empty,
    Pine, Oak,
    Bush, BigTree, BigTree2,
    Cactu, Cactu2, PalmTree,
    PalmTree2, PineSnow, Weed01, WeedTall, WeedTall_Jungle, WeedTall_Snow, RockProp, Grass, RockWall,
    Pine_Tall, PineSnow_Tall
}

public enum WorldType : byte
{
    none, Procedural, Plain, Dev, DevProcedural
}

public enum Placer : byte
{
    empty, BauWood, BauGold, BauDiamond, BauDark, CampTend, CampFire, CityHall, BlackSmith, AlchemistHouse, VillagerHouse, PostLight,
    TendaHumanos, TradeCart
}

public enum MaterialHitType: byte
{
    none, Entity, all, Meet, Wood, Rock, Dirt
}

public enum NPCTasks
{
    none, GoGetTask ,CutWood, TakeItemOnGround, GoHome, EnterInBuild, MakeRoad, Defense, Build, Farm, Minig,
    BlackSmith
}

public enum CityLevel
{
    Camp, Level0, Level1, Level2, Level3, Level4, Level5, Level6, Level7
}

public enum CityType
{
    none, Camp, farm, bigfarm, smalltown, bigtown, portcity, turistcity
}

public enum BuildType
{
    none, LivingHouse, Market, Port, Wall, WatchTower,
    ArchTower, BlackSmith, Church, Padeiro, Lenhador,
    BlackMarket, WaterBuild, Açogeuiro, Celeiro, CityHall
}

public enum VilagerVocation
{
    none, Guerreiro, Fazendeiro, açogueiro, lenhador, medico, engenheiro, bibliotecario, padre, guarda, mineiro, marinheiro, Vendedor, ferreiro
    , padeiro, aventureiro, cientista, alquimista
}

public enum EconomicType
{
    kingdom, feudal, villanger, Capitalism, Comunumism
}

public enum SexualType
{
    Others, Man, Woman
}

public enum FirstCityName
{
    Dragon, Kilkenny, Eelry, Beckinsdale, Leefside, Azmar, Braedwardith, Ramshorn, Forstford, Aylesbury, Mountmend, Stawford
}

public enum SecondCityName
{
    Vally, Orrinshire, Wimborne, Panshaw, Holbeck, Hythe, Cromer, Gormsey, Wingston, Hempholme, Jedburgh, RedHawk,
}

public enum FamilyPostiton
{
    none, Father, Mother, Son, GrandFather, GrandMother
}

public enum MaleHumanNames//14
{
    DarinHailey, GermanHarrison, WallyLee, RistonTownsend, LatimerDavenport,
    DeonteThorp,
    ThoraldNetley,
    JarvNetley,
    KristopherClifford,
    SiddelHuxley,
    TreDalton,
    FaraltAllerton,
    FidelisSwet,
    DacianSwett,
}

public enum FemaleHumanNames//20
{
    BerdineGale,
    KatarinaRylan,
    TatBrooks,
    JoyanneCamden,
    AyanaRodney,
    SavannaOakley,
    AdileneRoscoe,
    HerthaStratford,
    ChaunteHuckabee,
    RudelleHarrison,
    HarrietteWard,
    GesaHome,
    AmiteeBirkenhead,
    JulianneDenholm,
    GwendolynLincoln,
    CecilleFarnham,
    TanyaOldham,
    AshtynPaddle,
    FloridaAlden,
    HanneAppleton
}