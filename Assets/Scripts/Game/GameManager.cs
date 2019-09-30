﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Linq;
using System.IO.Compression;
using System;
using darckcomsoft.itch;
using System.Threading;

public class UIElements : DarckNet.DarckMonoBehaviour
{
    public List<GameObject> objclist = new List<GameObject>();
    public GameObject IconHold;

    public GameObject GetElements(string name)
    {
        foreach (var item in objclist)
        {
            if (item.name == name)
            {
                return item;
            }
        }
        return null;
    }
}

public class GameManager : UIElements
{
#if UNITY_EDITOR
    public string editorApiKey;
#endif
    Queue<PathResult> results = new Queue<PathResult>();
    public static ItchAPi Itch = new ItchAPi();
    public GameObject Cnavas;
    public string UserName = "Guest";
    public string UserId = "";
    public string Version = "1.0.0";
    public int Seed = 0;
    public string WorldName = "YourWorldName";
    public static bool Playing = false;
    public bool SinglePlayer = false;
    public bool MultiPlayer = false;
    public MousePngs MousePointer;
    public static MouseType Mtype = MouseType.none;
    public GameObject DMPOP;
    public MyPlayerInfo MyPlayer;
    public bool SHOWDEBUG = false;
    public ClientConnect Client;
    public NetWorkView Net;
    public static List<DCallBack> CallBacks = new List<DCallBack>();
    public Tile t;
    public Ray ray;
    public RaycastHit hit;
    public float mouseX;
    public float mouseY;
    public float mouseZ;
    public float mouseplus = 1;
    private int LastMouseX;
    private int LastMouseY;

    void Awake()
    {
        Game.GameManager = this;
        DontDestroyOnLoad(this.gameObject);
        Client.IP = "127.0.0.1";
        Client.Port = 25000;
        Client.Password = "";
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        Game.AudioManager.LoadAudio();
        /*if (Application.isEditor)
        {
#if UNITY_EDITOR
            Itch.EditorApiKey = "5av3kO2VL0iQuBu3zp7TNTbb6b257OYL81R3KQQ6";
            Itch.StartItchApi(true, "122257");
#endif
        }
        else
        {
            Itch.StartItchApi(false);

            UserId = Itch.GetMyUserId();
            UserName = Itch.GetMyUserName();
        }*/

        if (!Directory.Exists(Path.GetFullPath("Saves./")))
        {
            Directory.CreateDirectory(Path.GetFullPath("Saves./"));
        }

        WorldInfo info = SaveWorld.LoadInfo("World");
        if (info != null)
        {
            Seed = info.Seed;
            DataTime.Hora = info.h;
            DataTime.Dia = info.d;
            DataTime.Mes = info.m;
        }
    }

    public void PopUpDamage(Vector3 position, int damage)
    {
        GameObject pop = Instantiate(DMPOP, position, Quaternion.identity);
        Text texto = pop.GetComponentInChildren<Text>();
        texto.text = damage.ToString();
        texto.color = Color.red;
        pop.transform.SetParent(Cnavas.transform);
        Destroy(pop, 1);
    }

    //recorde(local mais longe que cheguei com o player em cordenadas da unity) 2.999999e+07
    public void SetUpSinglePlayer(string seed)
    {
        SaveWorld.CreateDerectorys();
        if (seed != "")
        {
            Seed = int.Parse(seed);
        }
        else
        {
            WorldInfo info = SaveWorld.LoadInfo("World");
            if (info != null)
            {
                Seed = info.Seed;
                DataTime.Hora = info.h;
                DataTime.Dia = info.d;
                DataTime.Mes = info.m;
                DataTime.skytime = info.skytime;
            }
            else
            {
                WorldInfo newinfo = new WorldInfo(12, 1, 1, 0.5f, UnityEngine.Random.Range(-9999, 9999));
                Seed = newinfo.Seed;
                DataTime.Hora = newinfo.h;
                DataTime.Dia = newinfo.d;
                DataTime.Mes = newinfo.m;
                DataTime.skytime = newinfo.skytime;
                SaveWorld.SaveInfo(newinfo, "World");
            }
        }
        SinglePlayer = true;
        Playing = true;
        DarckNet.Network.IsSelftHosteMode = true;
        DarckNet.Network.Create("127.0.0.1", 25000, 1);
    }

    public void SetUpMultiplayer()
    {
        if (DarckNet.Network.Connect(Client.IP, Client.Port, Client.Password) == null) { return; }
        SinglePlayer = false;
        MultiPlayer = true;
        Playing = true;
    }

    /// <summary>
    /// this is for the singleplayer mode, we use the server local to make de singleplayer mode
    /// </summary>
    public override void OnServerStart()
    {
        if (SinglePlayer)
        {
            Game.MenuManager.Canavas.SetActive(Playing);
            WorldManager.This.ChangeWorld("Map", 0, 0);
        }
        base.OnServerStart();
    }

    public override void OnConnect()
    {
        Game.MenuManager.Canavas.SetActive(Playing);
        WorldManager.This.ChangeWorld("Map", 0, 0);
        base.OnConnect();
    }

    public override void OnDisconnect()
    {
        DarckNet.Network.Disconnect();
        ClearObjects();
        SinglePlayer = false;
        MultiPlayer = false;
        Playing = false;
        Game.MenuManager.PopUp("Disconnect From Server!");
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
        base.OnDisconnect();
    }

    #region ConnectMethodos
    public void SetUpIp(InputField input) { Client.IP = input.text; }
    public void SetUpPort(InputField input) { Client.Port = int.Parse(input.text); }
    #endregion

    public void ClearObjects()
    {
        GameObject[] objs = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in objs)
        {
            GameObject.Destroy(obj);
        }
    }

    public void UpdateCursor(MouseType type)
    {
        if (Mtype != type)
        {
            Mtype = type;

            if (Mtype == MouseType.none)
            {
                Cursor.SetCursor(MousePointer.cursorTexture, MousePointer.hotSpot, MousePointer.cursorMode);
            }
            else if (Mtype == MouseType.Open)
            {
                Cursor.SetCursor(MousePointer.cursorOpen1, MousePointer.hotSpot, MousePointer.cursorMode);
            }
        }
    }

    void Update()
    {
        DarckNet.Network.Update();

        #region CursorPointer
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (Mtype == MouseType.none)
            {
                Cursor.SetCursor(MousePointer.cursorTexture2, MousePointer.hotSpot, MousePointer.cursorMode);
            }
            else if (Mtype == MouseType.Open)
            {
                Cursor.SetCursor(MousePointer.cursorOpen2, MousePointer.hotSpot, MousePointer.cursorMode);
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (Mtype == MouseType.none)
            {
                Cursor.SetCursor(MousePointer.cursorTexture, MousePointer.hotSpot, MousePointer.cursorMode);
            }
            else if (Mtype == MouseType.Open)
            {
                Cursor.SetCursor(MousePointer.cursorOpen1, MousePointer.hotSpot, MousePointer.cursorMode);
            }
        }
        #endregion

        if (Playing)
        {
            if (LastMouseX != (int)Input.mousePosition.x || LastMouseY != (int)Input.mousePosition.y)
            {
                Ray rayy = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y + mouseplus, Input.mousePosition.z));

                if (Physics.Raycast(rayy, out hit, 10000))
                {
                    mouseX = hit.point.x;
                    mouseY = hit.point.y;
                    mouseZ = hit.point.z;

                    if (WorldGenerator.Instance)
                    {
                        t = WorldGenerator.Instance.GetTileAt(hit.point.x, hit.point.z);
                    }

                    if (t != null)
                    {
                        if (GetPresets.GetMouseIteract(t))
                        {
                            UpdateCursor(MouseType.Open);
                        }
                        else
                        {
                            UpdateCursor(MouseType.none);
                        }
                    }
                    else
                    {
                        UpdateCursor(MouseType.none);
                    }
                }
            }

            LastMouseX = (int)Input.mousePosition.x;
            LastMouseY = (int)Input.mousePosition.y;
        }

        if (results.Count > 0)
        {
            int itemsInQueue = results.Count;
            lock (results)
            {
                for (int i = 0; i < itemsInQueue; i++)
                {
                    PathResult result = results.Dequeue();
                    result.callback(result.path, result.success);
                }
            }
        }
    }

    private void OnDestroy()
    {
        DarckNet.Network.Disconnect();
    }

    #region RPC

    #region Client
    public void GenerateChunkNet(int x, int y)
    {
        Net.RPC("ChunkDataNet", DarckNet.RPCMode.Server, x, y);
    }

    public void DeleteChunkNet(int x, int y)
    {
        Net.RPC("DeleteChunk", DarckNet.RPCMode.Server, x, y);
    }

    [RPC]
    void ChunkData(Vector2 pos, string data)
    {
        TileSave[] tile = SaveWorld.DeserializeString(data);

        WorldGenerator.Instance.ClientMakeChunkAt((int)pos.x, (int)pos.y, tile);
    }
    #endregion

    #region Server

    [RPC]
    void ChunkDataNet(int x, int y, DarckNet.DNetConnection peer)
    {
        string Tile = SaveWorld.SerializeDataToString(WorldGenerator.Instance.ServerMakeChunkAt(x, y, peer.unique));

        Net.RPC("ChunkData", peer.NetConnection, new Vector2(x,y), Tile);
    }

    [RPC]
    void DeleteChunk(int x, int y, DarckNet.DNetConnection peer)
    {
        WorldGenerator.Instance.NetDeleteChunk(x, y, peer.unique);
    }

    #endregion

    #endregion

    #region SteamApi
    #endregion

    #region PathManager
    public void RequestPath(PathRequest request, Pathfindingentity entity)
    {
        ThreadStart threadStart = delegate {
            entity.FindPath(request, FinishedProcessingPath);
        };
        threadStart.Invoke();
    }

    public void FinishedProcessingPath(PathResult result)
    {
        lock (results)
        {
            results.Enqueue(result);
        }
    }

    public struct PathResult
    {
        public Vector3[] path;
        public bool success;
        public Action<Vector3[], bool> callback;

        public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback)
        {
            this.path = path;
            this.success = success;
            this.callback = callback;
        }

    }

    public struct PathRequest
    {
        public Vector3 pathStart;
        public Vector3 pathEnd;
        public Action<Vector3[], bool> callback;

        public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            callback = _callback;
        }

    }
    #endregion
}

public class EntityLife : MonoBehaviour
{
    public float HP = 100;
    public float MaxHP = 100;
    public bool IsAlive = false;
    public string LastAttacker = "";

    // Logic of damage, remove damage of life qunty
    public virtual bool DoDamage(int damage, string attckerid, bool isplayer)
    {
        HP -= damage;
        FinishDamage();
        LastAttacker = attckerid;
        if (isplayer == true)
        {

        }
        if (HP <= 0)
        {
            OnDead();
            return true;
        }
        return false;
    }

    public virtual void OnDead()
    {
        IsAlive = false;
    }

    public virtual void Curar(int qunty)
    {
        HP += qunty;

        if (HP > MaxHP)
        {
            HP = MaxHP;
        }

        FinishCura();
    }

    //All Logic after damage, Like Update life bar
    public virtual void FinishDamage()
    {

    }

    //All Logic after cura, Like Update life bar
    public virtual void FinishCura()
    {

    }
}

[SerializeField]
public class PlayerInfo : MonoBehaviour
{
    public string UserName = "";
    public string UserID = "";
    public WorldType CurrentWorld = WorldType.Normal;
    public Transform PlayerRoot;
    public Lidgren.Network.NetConnection Peer;
}

[System.Serializable]
public class MyPlayerInfo
{
    public GameObject MyObject;
    public Inventory MyInventory;
    public EntityPlayer MyPlayerMove;

    [Header("GUI MY INVENTORY")]
    public InfoItemGUI InveItemInfo;
}

public class DCFMath
{
    public static float PingPong(float minimalvalue, float maxvalue, float time)
    {
        float value = 0;

        if (value >= maxvalue)
        {
            value -= time;
        }
        else if (value <= minimalvalue)
        {
            value += time;
        }

        return value;
    }
}

public static class MouselockFake
{
    public static bool IsLock { get; set; }
}

public class SaveWorld
{
    public static void CreateDerectorys()
    {
        if (!Directory.Exists(Path.GetFullPath("Saves./")))
        {
            Directory.CreateDirectory(Path.GetFullPath("Saves./"));
        }

        if (!Directory.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName)))
        {
            Directory.CreateDirectory(Path.GetFullPath("Saves./" + Game.GameManager.WorldName));
        }

        if (!Directory.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "chunks./")))
        {
            Directory.CreateDirectory(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "chunks./"));
        }

        if (!Directory.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "city./")))
        {
            Directory.CreateDirectory(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "city./"));
        }

        if (!Directory.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "player./")))
        {
            Directory.CreateDirectory(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "player./"));
        }

        if (!Directory.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "Entity./")))
        {
            Directory.CreateDirectory(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./" + "Entity./"));
        }
    }

    public static string SerializeDataToString(TileSave[] staticOptions)
    {
        string playerToJason = CompressString.StringCompressor.CompressString(JsonHelper.ToJson(staticOptions));

        //Debug.Log("Tamanho : " + playerToJason.Length);

        return playerToJason;
    }

    public static TileSave[] DeserializeString(string data)
    {
        string datadescompres = CompressString.StringCompressor.DecompressString(data);

        //Debug.Log("Tamanho : " + datadescompres);

        TileSave[] tiles = JsonHelper.FromJson<TileSave>(datadescompres);

        return tiles;
    }

    #region ChunkSave
    public static void Save(Tile[,] tile, string filename)
    {

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create("Saves./" + Game.GameManager.WorldName + "./" + "chunks./" + "./"+ filename);

        bf.Serialize(file, tile);
        file.Close();
    }
    public static Tile[,] Load(string filename)
    {
        if (File.Exists("Saves./" + Game.GameManager.WorldName + "./" + "chunks./" + "./" + filename))
        {

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open("Saves./" + Game.GameManager.WorldName + "./" + "chunks./" + "./" + filename, FileMode.Open);

            Tile[,] dataa = (Tile[,])bf.Deserialize(file);
            file.Close();

            return dataa;
        }

        return null;
    }
    #endregion

    #region City
    public static void SaveCity(CityManager info, string filename)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./") + "city./" + filename + ".city");

        bf.Serialize(file, info);
        file.Close();
    }

    public static CityManager LoadCity(string filename)
    {
        if (File.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./") + "city./" + filename + ".data"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./") + "city./" + filename + ".city", FileMode.Open);

            CityManager dataa = (CityManager)bf.Deserialize(file);
            file.Close();

            return dataa;
        }

        return null;
    }
    #endregion

    #region InfoGeral
    public static void SaveInfo(WorldInfo info, string filename)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./") + "./" + filename);

        bf.Serialize(file, info);
        file.Close();
    }
    public static WorldInfo LoadInfo(string filename)
    {
        if (File.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./") + "./" + filename))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./") + "./" + filename, FileMode.Open);

            WorldInfo dataa = (WorldInfo)bf.Deserialize(file);
            file.Close();

            return dataa;
        }
        else
        {
            return null;
        }
    }
    #endregion

    #region ContainerInve
    public static void SaveInve(SaveInventory info, string userid)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./Entity./" + userid);

        bf.Serialize(file, info);
        file.Close();
    }

    public static void DeletCont(string userid)
    {
        if (File.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./Entity./" +  userid))
        {
            File.Delete(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./Entity./" +  userid);
        }
    }

    public static SaveInventory LoadInve(string userid)
    {
        if (File.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./Entity./" + userid))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./Entity./" + userid, FileMode.Open);

            SaveInventory dataa = (SaveInventory)bf.Deserialize(file);
            file.Close();

            return dataa;
        }

        return null;
    }
    #endregion

    #region PlayerSave
    public static void SavePlayer(SavePlayerInfo info, string userid)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./player./" + "Player," + userid);
       
        bf.Serialize(file, info);
        file.Close();
    }

    public static void DeletPlayer(string userid)
    {
        File.Delete(Path.GetFullPath("Saves./" + Game.GameManager.WorldName + "./player./Player," + userid));
    }

    public static SavePlayerInfo LoadPlayer(string userid)
    {
        if (File.Exists(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./player./" + "Player," + userid))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Path.GetFullPath("Saves./" + Game.GameManager.WorldName) + "./player./" + "Player," + userid, FileMode.Open);
            
            SavePlayerInfo dataa = (SavePlayerInfo)bf.Deserialize(file);
            file.Close();

            return dataa;
        }

        return null;
    }
    #endregion

    #region LoadItchOff
    public static void SaveOffData(ItchSave info)
    {
        string texto = "";
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Path.GetFullPath("./" + "ItchData" + ".itch"));

        bf.Serialize(file, info);

        file.Close();
    }
    public static ItchSave LoadOffData()
    {
        if (File.Exists(Path.GetFullPath("./" + "ItchData" + ".itch")))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Path.GetFullPath("./" + "ItchData" + ".itch"), FileMode.Open);

            ItchSave dataa = (ItchSave)bf.Deserialize(file);
            

            file.Close();

            return dataa;
        }

        return null;
    }
    #endregion
}

[System.Serializable]
public class WorldInfo
{
    public int Seed;
    public int h;
    public int d;
    public int m;
    public float skytime;

    public WorldInfo()
    {

    }

    public WorldInfo(int _H, int _D, int _M, float _Time, int _Seed)
    {
        h = _H;
        d = _D;
        m = _M;
        skytime = _Time;
        Seed = _Seed;
    }
}

[System.Serializable]
public static class DataTime
{
    public static int Hora;
    public static int Dia = 1;
    public static int Mes = 1;

    public static float skytime = 0;

    private static int lasth;

    public static void SetTimeData(int h, int d, int m)
    {
        Hora = h;
        Dia = d;
        Mes = m;
    }

    public static void SetTimeData(int h)
    {
        Hora = h;
        if (Hora >= 24)
        {
            Hora = 0;
            Dia += 1;
        }

        if (Dia >= 30)
        {
            Dia = 1;
            Mes += 1;
        }
        if (Mes >= 12)
        {
            Mes = 1;
        }

        if (lasth == Hora)
        {

        }
        else
        {
            WorldInfo newinfo = new WorldInfo{ Seed = Game.GameManager.Seed, h = Hora, d = Dia, m = Mes , skytime = skytime };

            SaveWorld.SaveInfo(newinfo, "World");
        }
        lasth = Hora;
    }
}

[System.Serializable]
public class MousePngs
{
    [Header("Textures Pointer")]
    public Texture2D cursorTexture;
    public Texture2D cursorTexture2;

    public Texture2D cursorOpen1;
    public Texture2D cursorOpen2;

    public CursorMode cursorMode = CursorMode.Auto;
    public Vector2 hotSpot = Vector2.zero;
}

public enum MouseType
{
    none, Info, Take, Open
}

[System.Serializable]
public class SavePlayerInfo
{
    public SaveInventory Inve;
    public LifeStatus Status;
    public float x;
    public float z;
    public float Life;

    public SavePlayerInfo(SaveInventory inve, Vector3 pos, float life, LifeStatus status)
    {
        Inve = inve;
        x = pos.x;
        z = pos.z;
        Life = life;
        Status = status;
    }
}

[System.Serializable]
public class ItchSave
{
    public string UserId = "";
    public string UserName = "";
}

#region Encrypt
public static class StringCipher
{
    // This constant is used to determine the keysize of the encryption algorithm in bits.
    // We divide this by 8 within the code below to get the equivalent number of bytes.
    private const int Keysize = 256;

    // This constant determines the number of iterations for the password bytes generation function.
    private const int DerivationIterations = 1000;

    public static string Encrypt(string plainText, string passPhrase)
    {
        // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
        // so that the same Salt and IV values can be used when decrypting.  
        var saltStringBytes = Generate256BitsOfRandomEntropy();
        var ivStringBytes = Generate256BitsOfRandomEntropy();
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

        var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);

        var keyBytes = password.GetBytes(Keysize / 8);
        using (var symmetricKey = new RijndaelManaged())
        {
            symmetricKey.BlockSize = 256;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;
            using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                        var cipherTextBytes = saltStringBytes;
                        cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                        cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                        memoryStream.Close();
                        cryptoStream.Close();
                        return System.Convert.ToBase64String(cipherTextBytes);
                    }
                }
            }
        }
    }

    public static string Decrypt(string cipherText, string passPhrase)
    {
        // Get the complete stream of bytes that represent:
        // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
        var cipherTextBytesWithSaltAndIv = System.Convert.FromBase64String(cipherText);
        // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
        var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
        // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
        var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
        // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
        var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();
        var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);


        var keyBytes = password.GetBytes(Keysize / 8);
        using (var symmetricKey = new RijndaelManaged())
        {
            symmetricKey.BlockSize = 256;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;
            using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
            {
                using (var memoryStream = new MemoryStream(cipherTextBytes))
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        var plainTextBytes = new byte[cipherTextBytes.Length];
                        var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memoryStream.Close();
                        cryptoStream.Close();
                        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                    }
                }
            }
        }
    }

    private static byte[] Generate256BitsOfRandomEntropy()
    {
        var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
        var rngCsp = new RNGCryptoServiceProvider();
        rngCsp.GetBytes(randomBytes);
        return randomBytes;
    }
}
#endregion

public static class LoadSystems
{
    public static void LoadingSprites()
    {
        Debug.Log("LoadingSprites");
    }

    public static void LoadedSprites()
    {
        Debug.Log("LoadedSprites");
    }

    public static void LoadCompleteGame()
    {
        Debug.Log("GameLoaded");
    }

    public static void LoadedCompletMap()
    {
        Debug.Log("MapLoaded");
    }
}

public static class Tools
{
    public static int GetStringInt(string value)
    {
        return int.Parse(value);
    }

    public static float GetStringFloat(string value)
    {
        return float.Parse(value);
    }
}

public struct ClientConnect
{
    public string IP;
    public int Port;
    public string Password;
    public bool Connected;
}

public class DCallBack : MonoBehaviour
{
    public virtual void OnRespawn() { }

    private void OnEnable()
    {
        GameManager.CallBacks.Add(this);
    }

    private void OnDisable()
    {
        GameManager.CallBacks.Remove(this);
    }

    private void OnDestroy()
    {
        GameManager.CallBacks.Remove(this);
    }

    public static void Call(CallType type)
    {
        switch (type)
        {
            case CallType.OnRespawn:
                foreach (DCallBack back in GameManager.CallBacks)
                {
                    back.OnRespawn();
                }
                break;
            default:
                Debug.LogError("This is a invalid type: " + type);
                break;
        }
    }
}

public enum CallType
{
    none, OnRespawn
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }


    #region ArrayCord

    public static T[,] FromJsonChunk<T>(string json)
    {
        WrapperChunk<T> wrapper = JsonUtility.FromJson<WrapperChunk<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[,] array)
    {
        WrapperChunk<T> wrapper = new WrapperChunk<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }
    #endregion

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    [System.Serializable]
    private class WrapperChunk<T>
    {
        public T[,] Items;
    }
}

namespace CompressString
{
    internal static class StringCompressor
    {
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
        public static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }
    }
}

public class GameInput
{
    public static KeyCode ESC_BUTTON = KeyCode.Escape;
    public static KeyCode INVENTORY_BUTTON = KeyCode.I;
    public static KeyCode CONSOLE_BUTTON = KeyCode.DoubleQuote;
    public static KeyCode DEBUG_BUTTON = KeyCode.F1;
    public static KeyCode STATUS_BUTTON = KeyCode.C;

    public static bool EscButtonDown()
    {
        return Input.GetKeyDown(ESC_BUTTON);
    }

    public static bool EscButtonUp()
    {
        return Input.GetKeyUp(ESC_BUTTON);
    }

    public static bool STATUSButtonDown()
    {
        return Input.GetKeyDown(STATUS_BUTTON);
    }

    public static bool STATUSButtonUp()
    {
        return Input.GetKeyUp(STATUS_BUTTON);
    }
}

public class AudioManager
{
    public AudioClip NONETILE;
    public AudioClip[] GRASS_FOOT;

    public void LoadAudio()
    {
        Debug.Log("Loading Audio...");
        NONETILE = Resources.Load<AudioClip>("Audio/nonetile");
        GRASS_FOOT = Resources.LoadAll<AudioClip>("Audio/Grass/");
        Debug.Log("Loading Audio Finished!");
    }

    public AudioClip GetFootSound(TypeBlock tile)
    {
        switch (tile)
        {
            case TypeBlock.Grass:
                return GRASS_FOOT[UnityEngine.Random.Range(0, GRASS_FOOT.Length - 1)];
            default:
                return NONETILE;
        }
    }
}

/// <summary>
/// Use to get instance, of the scripts
/// </summary>
public static class Game
{
    public static GameManager GameManager;
    public static PathGrid PathGrid;
    public static DebugGUI DebugGUI;
    public static TileAnimations TileAnimations;
    public static MenuManager MenuManager;
    public static AudioManager AudioManager = new AudioManager();

    #region StaticMethods

    #endregion
}