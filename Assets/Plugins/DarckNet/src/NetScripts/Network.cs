﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Lidgren.Network;
using System.Net;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Threading;

namespace DarckNet
{
    public enum RPCMode
    {
        All, AllNoOwner, AllNoDimension, Owner, Server
    }

    /// <summary>
    /// All Static Method, Connect, StartServer, Disconnect, Instantiate, Destroy
    /// </summary>
    public class Network
    {
        #region Vars
        internal static NetPeer MyPeer;
        internal static int ViwesIDs = 0;
        internal static NetServer Server;
        internal static NetClient Client;
        internal static NetConnection ServerConnection;
        internal static List<NetPeer> Players = new List<NetPeer>();
        internal static Dictionary<int, NetworkObj> NetworkViews = new Dictionary<int, NetworkObj>();
        internal static Dictionary<int, WorldList> NetDimension = new Dictionary<int, WorldList>();
        internal static List<DarckMonoBehaviour> Events = new List<DarckMonoBehaviour>();
        internal static NetworkPrefabs PregabsList;
        internal static NetDeliveryMethod NetDeliveryMode = NetDeliveryMethod.UnreliableSequenced;
        internal static bool Runing = false;
        internal static bool onconnectbool = false;
        internal static bool onstartserverbool = false;
        public static bool IsSelftHosteMode = false;
        public static bool IsServer { get { if (Server != null) { return true; } else { return false; } } private set { } }
        public static bool IsClient { get { if (Client != null) { return true; } else { return false; } } private set { } }
        public static bool Ready { get; private set; }
        public static NetPeerStatistics PeerStat;
        public static int DimensionGeral = -1;

        #endregion

        /// <summary>
        /// Create a server, local server or dedicated server
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="maxplayers"></param>
        /// <returns></returns>
        public static bool Create(string ip, int port, int maxplayers)
        {
            if (Client == null && Server == null)
            {
                NetDeliveryMode = NetDeliveryMethod.UnreliableSequenced;

                bool Started = false;
                long ipe = 0;
                long.TryParse(ip, out ipe);

                NetPeerConfiguration config = new NetPeerConfiguration(NetConfig.AppIdentifier);
                config.MaximumConnections = maxplayers;

                config.EnableUPnP = !NetConfig.DedicatedServer;
                config.AutoFlushSendQueue = true;
                config.DefaultOutgoingMessageCapacity = NetConfig.DefaultOutgoingMessageCapacity;
                config.UseMessageRecycling = true;
                config.SendBufferSize = NetConfig.SendBufferSize;
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                config.AcceptIncomingConnections = NetConfig.AcceptConnection;


                config.m_port = port;
                config.BroadcastAddress = new IPAddress(ipe);

                NetServer peer = new NetServer(config);
                peer.Start(); // needed for initialization

                if (config.EnableUPnP == true)
                {
                    peer.UPnP.ForwardPort(port, "Darcknetwork. UnityGame, Server");
                }

                if (peer.Status == NetPeerStatus.Running)
                {
                    Started = true;
                }

                Server = peer;
                MyPeer = peer;
                PeerStat = peer.Statistics;

                if (NetworkViews[0] != null)
                {
                    if (NetworkViews[0].IdMode == IdMode.ManualId)
                    {
                        NetworkViews[0].Owner = MyPeer.UniqueIdentifier;
                    }
                }

                Debug.Log("Unique identifier is " + NetUtility.ToHexString(peer.UniqueIdentifier));
                Ready = true;
                Runing = true;

                #region SelfHost
                if (IsSelftHosteMode)
                {
                    WorldList wlist = new WorldList();
                    wlist.Players.Add(MyPeer.UniqueIdentifier);
                    NetDimension.Add(0, wlist);
                }
                #endregion

                for (int i = 0; i < Events.Count; i++)
                {
                    Events[i].OnServerStart();
                }
                return Started;
            }
            else
            {
                Debug.LogError("Server already started");
                return false;
            }
        }

        public static void ConnectUrl(string url, int port, string password)
        {
            url = url.Replace("http://", ""); //remove http://
            url = url.Replace("https://", ""); //remove https://
            url = url.Substring(0, url.IndexOf("/")); //remove everything after the first /

            try
            {
                IPHostEntry hosts = Dns.GetHostEntry(url);
                if (hosts.AddressList.Length > 0)
                    Connect(hosts.AddressList[0].ToString(), port, password);
            }
            catch
            {
                Debug.LogError("Could not get IP for URL " + url);
            }
        }

        /// <summary>
        /// Connect to remote server.
        /// </summary>
        /// <param name="Ip"> ip to connect </param>
        /// <param name="Port"></param>
        /// <param name="Password"></param>
        public static NetPeer Connect(string Ip, int Port, string Password)
        {
            if (Server == null && Client == null)
            {
                NetDeliveryMode = NetDeliveryMethod.UnreliableSequenced;

                NetPeerConfiguration config = new NetPeerConfiguration(NetConfig.AppIdentifier);

                config.AutoFlushSendQueue = true;
                config.DefaultOutgoingMessageCapacity = NetConfig.DefaultOutgoingMessageCapacity;
                config.UseMessageRecycling = true;
                config.SendBufferSize = NetConfig.SendBufferSize;
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                config.ConnectionTimeout = NetConfig.ConnectionTimeout;

                NetClient peer = new NetClient(config);
                peer.Start(); // needed for initialization

                NetOutgoingMessage approval = peer.CreateMessage();
                approval.Write(NetConfig.SecretKey);

                peer.Connect(Ip, Port, approval);

                Client = peer;
                MyPeer = peer;
                PeerStat = peer.Statistics;

                Debug.Log("Unique identifier is " + NetUtility.ToHexString(peer.UniqueIdentifier));

                Ready = true;
                Runing = true;

                var om = peer.CreateMessage();
                peer.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, Port));
                try
                {
                    peer.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, Port));
                }
                catch (NetException nex)
                {
                    if (nex.Message != "This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently")
                        throw;
                }

                if (NetworkViews[0] != null)
                {
                    if (NetworkViews[0].IdMode == IdMode.ManualId)
                    {
                        NetworkViews[0].Owner = Network.MyPeer.UniqueIdentifier;
                    }
                }
                return peer;
            }
            else
            {
                Debug.LogError("You already connected in some server");
                return null;
            }
        }

        /// <summary>
        /// Get a connection over the id of the connection
        /// </summary>
        /// <param name="uniq"></param>
        /// <returns></returns>
        public static NetConnection GetPeer(long uniq)
        {
            return MyPeer.GetConnectionById(uniq);
        }

        /// <summary>
        /// Get a connection over the id of the connection
        /// </summary>
        /// <param name="uniq"></param>
        /// <returns></returns>
        public static NetConnection[] GetPeer(long[] uniq)
        {
            return MyPeer.GetConnectionByMultipleId(uniq);
        }

        /// <summary>
        /// Disconnect From Server, or if is server Shutdown the server.
        /// </summary>
        public static void Disconnect()
        {
            if (IsServer)
            {
                for (int i = 0; i < Events.Count; i++)
                {
                    Events[i].OnServerClose();
                }

                Server.Shutdown("ServerClosed");

                Client = null;
                Server = null;
                MyPeer = null;
                Ready = false;
                Runing = false;
                onstartserverbool = false;
                IsSelftHosteMode = false;

                NetworkViews.Clear();
                NetDimension.Clear();
                Players.Clear();

                ViwesIDs = 0;
                ServerConnection = null;
                DimensionGeral = -1;

                for (int i = 0; i < Events.Count; i++)
                {
                    Events[i].OnServerAfterClose();
                }

                Events.Clear();
            }
            else if (IsClient)
            {
                var om = MyPeer.CreateMessage();

                om.Write((byte)DataType.ExitDimension);
                om.Write(DimensionGeral);

                Client.SendToAll(om, NetDeliveryMode);

                Client.Disconnect("Disconnect");

                Client = null;
                Server = null;
                MyPeer = null;
                Runing = false;
                onconnectbool = false;
                Ready = false;
                IsSelftHosteMode = false;

                NetworkViews.Clear();
                NetDimension.Clear();
                Players.Clear();

                ViwesIDs = 0;
                ServerConnection = null;
                DimensionGeral = -1;

                Events.Clear();
            }
        }

        /// <summary>
        /// Spawn a Unity.GameObject(Networkingview) over the networking.
        /// </summary>
        /// <param name="Object"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="g"></param>
        /// <returns>Game</returns>
        public static GameObject Instantiate(GameObject Object, Vector3 position, Quaternion rotation, int g)
        {
            if (Ready)
            {
                if (NetDimension.ContainsKey(g))
                {
                    while (NetworkViews.ContainsKey(ViwesIDs) == true)
                    {
                        ViwesIDs++;
                    }

                    GameObject obj = GameObject.Instantiate(Object, position, rotation);
                    NetworkViews.Add(ViwesIDs, obj.GetComponent<NetworkObj>());
                    obj.GetComponent<NetworkObj>().SetID(ViwesIDs, obj.GetComponent<NetworkObj>().PrefabID, MyPeer.UniqueIdentifier);
                    obj.GetComponent<NetworkObj>().Dimension = g;
                    ViwesIDs++;

                    var om = MyPeer.CreateMessage();

                    om.Write((byte)DataType.Instantiate);
                    om.Write(Object.GetComponent<NetworkObj>().PrefabID);
                    om.Write(g);

                    //Position
                    om.Write(position.x);
                    om.Write(position.y);
                    om.Write(position.z);

                    //Rotation
                    om.Write(rotation.x);
                    om.Write(rotation.y);
                    om.Write(rotation.z);

                    if (IsClient)
                    {
                        Client.SendMessage(om, NetDeliveryMode);
                    }
                    else if (IsServer)
                    {
                        List<NetConnection> listanet = new List<NetConnection>(GetPeer(NetDimension[g].Players.ToArray()));

                        Server.SendToAll(om, listanet.ToArray(), NetDeliveryMode);
                    }

                    return obj;
                }
                else
                {
                    Debug.LogError("This Dimension dont exist, or isnt loaded!");
                    return null;
                }
            }
            else
            {
                Debug.LogError("You still have not, finished connecting.");
                return null;
            }
        }

        /// <summary>
        /// Destroy with time, a "Unity.GameObject(Networkingview)", over the network, only objects spawned in network.
        /// </summary>
        /// <param name="Object">Networkingview Object.</param>
        /// <param name="time">Time to Destroy, In Seconds</param>
        public static void Destroy(GameObject Object, float time)
        {
            Destroy(Object);
        }

        /// <summary>
        /// Destroy a "Unity.GameObject(Networkingview)" over the network, only objects spawned in network.
        /// </summary>
        /// <param name="Object"></param>
        public static void Destroy(GameObject Object)
        {
            if (Object.GetComponent<NetworkObj>().IdMode == IdMode.AutomaticId)
            {
                if (Ready)
                {
                    int Dimension = Object.GetComponent<NetworkObj>().Dimension;
                    var om = MyPeer.CreateMessage();
                    om.Write((byte)DataType.Destroy);
                    om.Write(Object.GetComponent<NetworkObj>().ViewID);

                    if (NetDimension.ContainsKey(Dimension))
                    {
                        if (IsClient)
                        {
                            Client.SendMessage(om, NetDeliveryMode);
                        }
                        else if (IsServer)
                        {
                            List<NetConnection> listanet = new List<NetConnection>(GetPeer(NetDimension[Dimension].Players.ToArray()));
                            Server.SendToAll(om, listanet.ToArray(), NetDeliveryMode);
                            NetworkViews.Remove(Object.GetComponent<NetworkObj>().ViewID);
                            GameObject.Destroy(Object);
                        }
                    }
                    else
                    {
                        Debug.LogError("Sorry this dimension is closed, or empty");
                    }
                }
                else
                {
                    Debug.LogError("You are disconnected from any server, you can't destroy objects!");
                }
            }
            else
            {
                Debug.LogError("This objects don't over network");
            }
        }

        /// <summary>
        /// Use to change your dimension
        /// </summary>
        public static void ChangeDimension(int id, int lastid)
        {
            DimensionGeral = id;

            var om = MyPeer.CreateMessage();

            om.Write((byte)DataType.ChangeDimension);
            om.Write(id);
            om.Write(lastid);

            foreach (var array in NetworkViews.ToArray())
            {
                if (NetworkViews[array.Key].Dimension == lastid)
                {
                    GameObject.Destroy(NetworkViews[array.Key].gameObject);
                }
            }

            Client.SendMessage(om, NetDeliveryMode);
        }

        /// <summary>
        /// Used to close a connection, just server can close the connection.
        /// </summary>
        /// <param name="Peer"></param>
        /// <param name="sendmenssagem"></param>
        public static void CloseConnection(NetConnection Peer, bool sendmenssagem)
        {
            if (IsServer)
            {
                var om = MyPeer.CreateMessage();

                om.Write((byte)DataType.CloseConnection);

                Server.SendMessage(om, Peer, NetDeliveryMode);
            }
        }

        /// <summary>
        ///  Update method network
        /// </summary>
        public static void Update()
        {
            if (MyPeer != null)
            {
                if (IsServer)
                {
                    NetIncomingMessage inc;
                    while ((inc = Server.ReadMessage()) != null)
                    {
                        switch (inc.MessageType)
                        {
                            case NetIncomingMessageType.VerboseDebugMessage:
                                Debug.LogError(inc.ReadString());
                                break;
                            case NetIncomingMessageType.DebugMessage:
                                Debug.LogError(inc.ReadString());
                                break;
                            case NetIncomingMessageType.WarningMessage:
                                Debug.LogWarning(inc.ReadString());
                                break;
                            case NetIncomingMessageType.ErrorMessage:
                                string erro = inc.ReadString();
                                Debug.LogError(erro);
                                if (erro == "Shutdown complete")
                                {
                                    for (int i = 0; i < Events.Count; i++)
                                    {
                                        Events[i].OnPlayerDisconnect(inc.SenderConnection);
                                    }
                                }
                                break;
                            case NetIncomingMessageType.Data:
                                ProceMenssagServer(inc);
                                break;
                            case NetIncomingMessageType.ConnectionApproval:
                                string s = inc.ReadString();
                                if (s == NetConfig.SecretKey)
                                    inc.SenderConnection.Approve();
                                else
                                    inc.SenderConnection.Deny();
                                break;
                            default:
                                if (inc.SenderConnection.Status == NetConnectionStatus.Connected)
                                {
                                    Debug.Log("Debu01");
                                    for (int i = 0; i < Events.Count; i++)
                                    {
                                        Events[i].OnPlayerConnect(inc.SenderConnection);
                                    }
                                    Debug.Log("Debu02");
                                    List<NetViewSerializer> netvi = new List<NetViewSerializer>();
                                    Debug.Log("Debu03");
                                    var om = MyPeer.CreateMessage();
                                    Debug.Log("This Player : " + NetUtility.ToHexString(inc.SenderConnection.RemoteUniqueIdentifier) + " Sended");
                                    om.Write((byte)DataType.Instantiate_Pool);
                                    om.Write(ViwesIDs);
                                    om.WriteVariableInt64(inc.SenderConnection.RemoteUniqueIdentifier);
                                    Debug.Log("Debu04");
                                    foreach (var kvp in GetAutomaticView(0))
                                    {
                                        NetViewSerializer neww = new NetViewSerializer();
                                        neww.PrefabID = kvp.PrefabID;
                                        neww.Owner = kvp.Owner;
                                        neww.ViewID = kvp.ViewID;
                                        neww.DeliveModo = kvp.DeliveModo;
                                        neww.IdMode = kvp.IdMode;
                                        neww.Dimension = kvp.Dimension;

                                        neww.p_x = kvp.transform.position.x;
                                        neww.p_y = kvp.transform.position.y;
                                        neww.p_z = kvp.transform.position.z;

                                        neww.r_x = kvp.transform.rotation.x;
                                        neww.r_y = kvp.transform.rotation.y;
                                        neww.r_z = kvp.transform.rotation.z;

                                        netvi.Add(neww);
                                    }

                                    if (NetDimension.ContainsKey(0))
                                    {
                                        if (NetDimension[0].Players.Contains(inc.SenderConnection.RemoteUniqueIdentifier) == false)
                                        {
                                            NetDimension[0].Players.Add(inc.SenderConnection.RemoteUniqueIdentifier);
                                        }
                                    }
                                    else
                                    {
                                        WorldList wlist = new WorldList();
                                        wlist.Players.Add(inc.SenderConnection.RemoteUniqueIdentifier);
                                        NetDimension.Add(0, wlist);
                                    }

                                    string dimendata = JsonHelper.ToJson(NetDimension.Values.ToArray());
                                    om.Write(CompressString.StringCompressor.CompressString(dimendata));

                                    Debug.LogError(dimendata);

                                    string data = JsonHelper.ToJson(netvi.ToArray());
                                    om.Write(CompressString.StringCompressor.CompressString(data));

                                    Server.SendMessage(om, inc.SenderConnection, NetDeliveryMode);
                                    //----------------------------------------------------------------//

                                    foreach (var net in Server.m_connections.ToArray())
                                    {
                                        if (Server.m_connections[net.Key] != inc.SenderConnection)
                                        {
                                            var om2 = MyPeer.CreateMessage();

                                            om2.Write((byte)DataType.EnterInWorld);

                                            om2.WriteVariableInt64(inc.SenderConnection.RemoteUniqueIdentifier);
                                            om2.Write(0);
                                            om2.Write(-1);

                                            Server.SendMessage(om2, Server.m_connections[net.Key], NetDeliveryMode);
                                        }
                                    }
                                }
                                else if (inc.SenderConnection.Status == NetConnectionStatus.RespondedConnect)
                                {
                                    Debug.Log("This Player : " + NetUtility.ToHexString(inc.SenderConnection.RemoteUniqueIdentifier) + " Are Accepted to server");
                                }
                                else if (inc.SenderConnection.Status == NetConnectionStatus.Disconnected)
                                {
                                    for (int i = 0; i < Events.Count; i++)
                                    {
                                        Events[i].OnPlayerDisconnect(inc.SenderConnection);
                                    }

                                    NetworkObj[] obj = NetworkViews.Values.ToArray();

                                    for (int i = 0; i < obj.Length; i++)
                                    {
                                        if (obj[i].Owner == inc.SenderConnection.RemoteUniqueIdentifier)
                                        {
                                            NetworkViews.Remove(obj[i].ViewID);
                                            Destroy(obj[i].gameObject);
                                        }
                                    }

                                    Debug.Log("Player : " + NetUtility.ToHexString(inc.SenderConnection.RemoteUniqueIdentifier) + " Disconnected!");
                                }
                                else if (inc.SenderConnection.Status == NetConnectionStatus.Disconnecting)
                                {
                                    //last paket sande to client, and after this is dosconnect
                                }
                                break;
                        }
                        Server.Recycle(inc);
                    }
                }
                else if (IsClient)
                {
                    NetIncomingMessage inc;
                    while ((inc = Client.ReadMessage()) != null)
                    {
                        switch (inc.MessageType)
                        {
                            case NetIncomingMessageType.VerboseDebugMessage:
                                Debug.LogError(inc.ReadString());
                                break;
                            case NetIncomingMessageType.DebugMessage:
                                Debug.LogError(inc.ReadString());
                                break;
                            case NetIncomingMessageType.WarningMessage:
                                Debug.LogWarning(inc.ReadString());
                                break;
                            case NetIncomingMessageType.ErrorMessage:
                                string erro = inc.ReadString();
                                Debug.LogError(erro);
                                if (erro == "Shutdown complete")
                                {
                                    for (int i = 0; i < Events.Count; i++)
                                    {
                                        Events[i].OnPlayerDisconnect(inc.SenderConnection);
                                    }
                                }
                                break;
                            case NetIncomingMessageType.Data:
                                ProceMenssagClient(inc);
                                break;
                            case NetIncomingMessageType.StatusChanged:
                                NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();

                                if (status == NetConnectionStatus.Disconnected)
                                {
                                    for (int i = 0; i < Events.Count; i++)
                                    {
                                        Events[i].OnDisconnect();
                                    }
                                }
                                break;
                            default:
                                if (inc.SenderConnection.Status == NetConnectionStatus.Connected)
                                {
                                    for (int i = 0; i < Events.Count; i++)
                                    {
                                        Events[i].OnPlayerConnect(inc.SenderConnection);
                                    }
                                }
                                else if (inc.SenderConnection.Status == NetConnectionStatus.RespondedConnect)
                                {
                                    Debug.LogError("This Player : " + NetUtility.ToHexString(inc.SenderConnection.RemoteUniqueIdentifier) + " Are Accepted to server");
                                }
                                else if (inc.SenderConnection.Status == NetConnectionStatus.Disconnected)
                                {
                                    for (int i = 0; i < Events.Count; i++)
                                    {
                                        Events[i].OnPlayerDisconnect(inc.SenderConnection);
                                    }



                                    Debug.Log("Player : " + NetUtility.ToHexString(inc.SenderConnection.RemoteUniqueIdentifier) + " Disconnected!");
                                }
                                else if (inc.SenderConnection.Status == NetConnectionStatus.Disconnecting)
                                {

                                }
                                break;
                        }
                        Client.Recycle(inc);
                    }
                }
            }
        }

    static void ProceMenssagServer(NetIncomingMessage inc)
    {
            DataType type = (DataType)inc.ReadByte();

            if (type == DataType.RPC)//RPC Normal
            {
                inc.ReadString(out string funcname);
                inc.ReadInt32(out int viewidd);

                NetworkObj Net = NetworkViews[viewidd];

                Net.Execute(funcname, inc);
            }
            else if (type == DataType.RPC_All)//all in dimension
            {
                RPC_All(inc);
            }
            else if (type == DataType.RPC_ALLDimension)//All in server, no in dimension
            {
                RPC_ALLDimension(inc);
            }
            else if (type == DataType.RPC_AllOwner)//All but no owner
            {
                RPC_AllOwner(inc);
            }
            else if (type == DataType.RPC_Owner)//Owener
            {
                RPC_Owner(inc);
            }
            else if (type == DataType.Instantiate)
            {
                Vector3 Pos = new Vector3();
                Quaternion Rot = new Quaternion();

                inc.ReadInt32(out int prefabid);
                inc.ReadInt32(out int dimension);

                //Position
                Pos.x = inc.ReadFloat();
                Pos.y = inc.ReadFloat();
                Pos.z = inc.ReadFloat();

                //Rotation
                Rot.x = inc.ReadFloat();
                Rot.y = inc.ReadFloat();
                Rot.z = inc.ReadFloat();
                Rot.w = 1;

                GameObject Objc = GameObject.Instantiate(PregabsList.Prefabs[prefabid], Pos, Rot);
                NetworkViews.Add(ViwesIDs, Objc.GetComponent<NetworkObj>());
                Objc.GetComponent<NetworkObj>().SetID(ViwesIDs, prefabid, inc.SenderConnection.RemoteUniqueIdentifier);
                Objc.GetComponent<NetworkObj>().Dimension = dimension;
                ViwesIDs += 1;

                Debug.Log("ViewsIDs" + ViwesIDs);

                //--------------------------------------------------//

                #region InstantiateSend
                var om = MyPeer.CreateMessage();

                om.Write((byte)DataType.Instantiate);
                om.Write(prefabid);
                om.Write(dimension);

                //Position
                om.Write(Pos.x);
                om.Write(Pos.y);
                om.Write(Pos.z);

                //Rotation
                om.Write(Rot.x);
                om.Write(Rot.y);
                om.Write(Rot.z);

                long[] ids = NetDimension[dimension].Players.ToArray();
                List<NetConnection> listanet = new List<NetConnection>();

                for (int i = 0; i < ids.Length; i++)
                {
                    if (ids[i] != inc.SenderConnection.RemoteUniqueIdentifier)
                    {
                        listanet.Add(GetPeer(ids[i]));
                    }
                }

                Server.SendToAll(om, listanet.ToArray(), NetDeliveryMode);
                #endregion
            }
            else if (type == DataType.Destroy)
            {
                inc.ReadInt32(out int viewid);

                if (NetworkViews.ContainsKey(viewid))
                {
                    NetworkObj net = NetworkViews[viewid];

                    #region DestroySend
                    var om = MyPeer.CreateMessage();

                    om.Write((byte)DataType.Destroy);
                    om.Write(viewid);

                    long[] ids = NetDimension[net.Dimension].Players.ToArray();
                    List<NetConnection> listanet = new List<NetConnection>();

                    for (int i = 0; i < ids.Length; i++)
                    {
                        listanet.Add(GetPeer(ids[i]));
                    }

                    Server.SendToAll(om, listanet.ToArray(), NetDeliveryMode);
                    #endregion

                    GameObject.Destroy(net.gameObject);
                    NetworkViews.Remove(viewid);
                }
            }
            else if (type == DataType.Destroy_Player)
            {
                NetworkObj[] obj = NetworkViews.Values.ToArray();

                for (int i = 0; i < obj.Length; i++)
                {
                    if (obj[i].Owner == inc.SenderConnection.RemoteUniqueIdentifier)
                    {
                        NetworkViews.Remove(obj[i].ViewID);
                        Destroy(obj[i].gameObject);
                    }
                }
            }
            else if (type == DataType.ChangeDimension)
            {
                inc.ReadInt32(out int dimension);
                inc.ReadInt32(out int lastdimension);

                List<NetViewSerializer> netvi = new List<NetViewSerializer>();

                var om = MyPeer.CreateMessage();

                om.Write((byte)DataType.Instantiate_PoolD);
                om.Write(ViwesIDs);

                om.WriteVariableInt64(inc.SenderConnection.RemoteUniqueIdentifier);
                om.Write(dimension);
                om.Write(lastdimension);

                foreach (var kvp in GetAutomaticView(dimension))
                {
                    NetViewSerializer neww = new NetViewSerializer();
                    neww.PrefabID = kvp.PrefabID;
                    neww.Owner = kvp.Owner;
                    neww.ViewID = kvp.ViewID;
                    neww.DeliveModo = kvp.DeliveModo;
                    neww.IdMode = kvp.IdMode;
                    neww.Dimension = kvp.Dimension;

                    neww.p_x = kvp.transform.position.x;
                    neww.p_y = kvp.transform.position.y;
                    neww.p_z = kvp.transform.position.z;

                    neww.r_x = kvp.transform.rotation.x;
                    neww.r_y = kvp.transform.rotation.y;
                    neww.r_z = kvp.transform.rotation.z;

                    netvi.Add(neww);
                }

                if (NetDimension.ContainsKey(dimension))
                {
                    if (NetDimension[dimension].Players.Contains(inc.SenderConnection.RemoteUniqueIdentifier) == false)
                    {
                        NetDimension[dimension].Players.Add(inc.SenderConnection.RemoteUniqueIdentifier);
                    }
                }
                else
                {
                    WorldList wlist = new WorldList();
                    wlist.Players.Add(inc.SenderConnection.RemoteUniqueIdentifier);
                    NetDimension.Add(dimension, wlist);
                }

                if (NetDimension.ContainsKey(lastdimension))
                {
                    NetDimension[lastdimension].Players.Remove(inc.SenderConnection.RemoteUniqueIdentifier);

                    if (NetDimension[lastdimension].Players.Count <= 0)
                    {
                        NetDimension.Remove(lastdimension);
                    }
                }

                string data = JsonHelper.ToJson(netvi.ToArray());
                om.Write(CompressString.StringCompressor.CompressString(data));

                Server.SendMessage(om, inc.SenderConnection, NetDeliveryMode);
                //-----------------------------------------//

                foreach (var net in Server.m_connections.ToArray())
                {
                    if (Server.m_connections[net.Key] != inc.SenderConnection)
                    {
                        var om2 = MyPeer.CreateMessage();

                        om2.Write((byte)DataType.EnterInWorld);

                        om2.WriteVariableInt64(inc.SenderConnection.RemoteUniqueIdentifier);
                        om2.Write(dimension);
                        om2.Write(lastdimension);

                        Server.SendMessage(om2, Server.m_connections[net.Key], NetDeliveryMode);
                    }
                }
            }
            else if (type == DataType.ExitDimension)
            {
                inc.ReadInt32(out int dimension);

                if (NetDimension.ContainsKey(dimension))
                {
                    if (NetDimension[dimension].Players.Count > 0)
                    {
                        NetDimension[dimension].Players.Remove(inc.SenderConnection.RemoteUniqueIdentifier);
                    }
                }
            }
        }

    static void ProceMenssagClient(NetIncomingMessage inc)
    {
            DataType type = (DataType)inc.ReadByte();

            if (type == DataType.RPC)
            {
                inc.ReadString(out string funcname);
                inc.ReadInt32(out int viewidd);

                NetworkObj Net = NetworkViews[viewidd];

                Net.Execute(funcname, inc);
            }
            else if (type == DataType.Instantiate)
            {
                Vector3 Pos = new Vector3();
                Quaternion Rot = new Quaternion();

                inc.ReadInt32(out int prefabid);
                inc.ReadInt32(out int dimension);

                //Position
                Pos.x = inc.ReadFloat();
                Pos.y = inc.ReadFloat();
                Pos.z = inc.ReadFloat();

                //Rotation
                Rot.x = inc.ReadFloat();
                Rot.y = inc.ReadFloat();
                Rot.z = inc.ReadFloat();
                Rot.w = 1;

                GameObject Objc = GameObject.Instantiate(PregabsList.Prefabs[prefabid], Pos, Rot);
                NetworkViews.Add(ViwesIDs, Objc.GetComponent<NetworkObj>());
                Objc.GetComponent<NetworkObj>().SetID(ViwesIDs, prefabid, inc.SenderConnection.RemoteUniqueIdentifier);
                Objc.GetComponent<NetworkObj>().Dimension = dimension;
                ViwesIDs += 1;

                Debug.LogError("ViewsIDs" + ViwesIDs);
            }
            else if (type == DataType.Destroy)
            {
                inc.ReadInt32(out int viewid);

                if (NetworkViews.ContainsKey(viewid))
                {
                    NetworkObj net = NetworkViews[viewid];

                    GameObject.Destroy(net.gameObject);

                    NetworkViews.Remove(viewid);
                }
            }
            else if (type == DataType.Instantiate_Pool)
            {
                inc.ReadInt32(out ViwesIDs);
                long uniq = inc.ReadVariableInt64();

                inc.ReadString(out string datadime);
                string datadecompress = CompressString.StringCompressor.DecompressString(datadime);
                WorldList[] world = JsonHelper.FromJson<WorldList>(datadecompress);
                Debug.LogError("Debu01");

                inc.ReadString(out string data);
                string worlddecompress = CompressString.StringCompressor.DecompressString(data);
                NetViewSerializer[] net = JsonHelper.FromJson<NetViewSerializer>(worlddecompress);
                Debug.LogError("Debu02");
                foreach (NetViewSerializer nv in net)
                {
                    GameObject Objc = GameObject.Instantiate(PregabsList.Prefabs[nv.PrefabID], new Vector3(nv.p_x, nv.p_y, nv.p_z), new Quaternion(nv.r_x, nv.r_y, nv.r_z, 1));
                    Objc.GetComponent<NetworkObj>().SetID(nv.ViewID, nv.PrefabID, nv.Owner);
                    Objc.GetComponent<NetworkObj>().Dimension = nv.Dimension;
                    NetworkViews.Add(nv.ViewID, Objc.GetComponent<NetworkObj>());
                }
                Debug.LogError("Debu03");

                if (world.Length >= 0)
                {
                    int f = -1;
                    foreach (WorldList nv in world)
                    {
                        f++;
                        NetDimension.Add(f, nv);
                    }
                }
                else
                {
                    if (NetDimension.ContainsKey(0))
                    {
                        if (NetDimension[0].Players.Contains(uniq) == false)
                        {
                            NetDimension[0].Players.Add(uniq);
                        }
                    }
                    else
                    {
                        WorldList wlist = new WorldList();
                        wlist.Players.Add(uniq);
                        NetDimension.Add(0, wlist);
                    }
                }

                Debug.LogError(datadecompress);

                Debug.LogError("Debu04");

                DimensionGeral = 0;

                Ready = true;

                for (int i = 0; i < Events.Count; i++)
                {
                    Events[i].OnConnect();
                }

                Debug.LogError("Ready To Play...");
            }
            else if (type == DataType.Instantiate_PoolD)
            {
                inc.ReadInt32(out ViwesIDs);
                long uniq = inc.ReadVariableInt64();
                inc.ReadInt32(out int dimension);
                inc.ReadInt32(out int lastdimension);

                inc.ReadString(out string data);
                string datadecompress = CompressString.StringCompressor.DecompressString(data);
                NetViewSerializer[] net = JsonHelper.FromJson<NetViewSerializer>(datadecompress);

                foreach (NetViewSerializer nv in net)
                {
                    GameObject Objc = GameObject.Instantiate(PregabsList.Prefabs[nv.PrefabID], new Vector3(nv.p_x, nv.p_y, nv.p_z), new Quaternion(nv.r_x, nv.r_y, nv.r_z, 1));
                    Objc.GetComponent<NetworkObj>().SetID(nv.ViewID, nv.PrefabID, nv.Owner);
                    Objc.GetComponent<NetworkObj>().Dimension = nv.Dimension;
                    NetworkViews.Add(nv.ViewID, Objc.GetComponent<NetworkObj>());
                }

                if (NetDimension.ContainsKey(dimension))
                {
                    if (NetDimension[dimension].Players.Contains(uniq) == false)
                    {
                        NetDimension[dimension].Players.Add(uniq);
                    }
                }
                else
                {
                    WorldList wlist = new WorldList();
                    wlist.Players.Add(uniq);
                    NetDimension.Add(dimension, wlist);
                }

                if (NetDimension.ContainsKey(lastdimension))
                {
                    NetDimension[lastdimension].Players.Remove(uniq);

                    if (NetDimension[lastdimension].Players.Count <= 0)
                    {
                        NetDimension.Remove(lastdimension);
                    }
                }
            }
            else if (type == DataType.EnterInWorld)
            {
                long uniq = inc.ReadVariableInt64();
                inc.ReadInt32(out int dimension);
                inc.ReadInt32(out int lastdimension);

                if (NetDimension.ContainsKey(dimension))
                {
                    if (NetDimension[dimension].Players.Contains(uniq) == false)
                    {
                        NetDimension[dimension].Players.Add(uniq);
                    }
                }
                else
                {
                    WorldList wlist = new WorldList();
                    wlist.Players.Add(uniq);
                    NetDimension.Add(dimension, wlist);
                }

                if (lastdimension != -1)
                {
                    if (NetDimension.ContainsKey(lastdimension))
                    {
                        NetDimension[lastdimension].Players.Remove(uniq);

                        if (NetDimension[lastdimension].Players.Count <= 0)
                        {
                            NetDimension.Remove(lastdimension);
                        }
                    }
                }
            }
            else if (type == DataType.CloseConnection)
            {
                Disconnect();
            }
            else if (type == DataType.ExitDimension)
            {
                inc.ReadInt32(out int dimension);

                if (NetDimension.ContainsKey(dimension))
                {
                    if (NetDimension[dimension].Players.Count > 0)
                    {
                        NetDimension[dimension].Players.Remove(inc.SenderConnection.RemoteUniqueIdentifier);
                    }
                }
            }
        }

        public static NetworkObj[] GetAutomaticView(int dimension)
        {
            List<NetworkObj> obj = new List<NetworkObj>();

            foreach (var kvp in NetworkViews.ToArray())
            {
                if (NetworkViews[kvp.Key].IdMode == IdMode.AutomaticId)
                {
                    if (NetworkViews[kvp.Key].Dimension == dimension)
                    {
                        obj.Add(NetworkViews[kvp.Key]);
                    }
                }
            }

            return obj.ToArray();
        }

        #region RPC_Type_Receive

        internal static void RPC_All(NetIncomingMessage inc)
        {
            inc.ReadString(out string funcname);
            inc.ReadInt32(out int viewidd);
            inc.ReadInt32(out int d);

            NetworkObj Net = NetworkViews[viewidd];

            object[] obj = Net.Execute(funcname, inc);

            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewidd);

            DoData(om, obj);

            Server.SendToAll(om, GetPeer(NetDimension[d].Players.ToArray()), Net.DeliveModo);
        }

        internal static void RPC_AllOwner(NetIncomingMessage inc)
        {
            inc.ReadString(out string funcname);
            inc.ReadInt32(out int viewidd);
            inc.ReadInt32(out int d);
            //inc.ReadInt64();

            NetworkObj Net = NetworkViews[viewidd];

            object[] obj = Net.Execute(funcname, inc);

            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewidd);

            DoData(om, obj);

            long[] ids = NetDimension[d].Players.ToArray();
            List<NetConnection> listanet = new List<NetConnection>();

            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] != Net.Owner)
                {
                    listanet.Add(GetPeer(ids[i]));
                }
            }

            Server.SendToAll(om, listanet.ToArray(), Net.DeliveModo);
        }

        internal static void RPC_Owner(NetIncomingMessage inc)
        {
            inc.ReadString(out string funcname);
            inc.ReadInt32(out int viewidd);
            NetworkObj Net = NetworkViews[viewidd];
            object[] obj = Net.ExecuteNo(funcname, inc);


            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewidd);

            DoData(om, obj);

            Server.SendMessage(om, GetPeer(Net.Owner), Net.DeliveModo);
        }

        internal static void RPC_ALLDimension(NetIncomingMessage inc)
        {
            inc.ReadString(out string funcname);
            inc.ReadInt32(out int viewidd);

            NetworkObj Net = NetworkViews[viewidd];

            object[] obj = Net.Execute(funcname, inc);


            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewidd);

            DoData(om, obj);

            Server.SendToAll(om, Net.DeliveModo);
        }

        internal static NetOutgoingMessage DoData(NetOutgoingMessage om, object[] param)
        {
            for (int i = 0; i < param.Length; i++)
            {
                if (param[i].GetType() == typeof(string))
                {
                    om.Write((string)param[i]);
                }
                else if (param[i].GetType() == typeof(int))
                {
                    om.Write((int)param[i]);
                }
                else if (param[i].GetType() == typeof(float))
                {
                    om.Write((float)param[i]);
                }
                else if (param[i].GetType() == typeof(Vector3))
                {
                    Vector3 vec = (Vector3)param[i];

                    om.Write(vec.x);
                    om.Write(vec.y);
                    om.Write(vec.z);
                }
                else if (param[i].GetType() == typeof(Vector2))
                {
                    Vector2 vec = (Vector2)param[i];

                    om.Write(vec.x);
                    om.Write(vec.y);
                }
                else if (param[i].GetType() == typeof(Quaternion))
                {
                    Quaternion vec = (Quaternion)param[i];

                    om.Write(vec.x);
                    om.Write(vec.y);
                    om.Write(vec.z);
                }
            }

            return om;
        }

        #endregion

        #region RPC_Type_Local

        internal static void RPC_All(string funcname, int viewid, int d, object[] param)
        {
            NetworkObj Net = NetworkViews[viewid];

            Net.Execute(funcname, Server.Myconnection, param);

            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewid);

            DoData(om, param);

            Server.SendToAll(om, GetPeer(NetDimension[d].Players.ToArray()), Net.DeliveModo);
        }

        internal static void RPC_AllOwner(string funcname, int viewid, int d, object[] param)
        {
            NetworkObj Net = NetworkViews[viewid];

            Net.Execute(funcname, Server.Myconnection, param);

            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewid);

            DoData(om, param);

            long[] ids = NetDimension[d].Players.ToArray();
            List<NetConnection> listanet = new List<NetConnection>();

            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] != Net.Owner)
                {
                    listanet.Add(GetPeer(ids[i]));
                }
            }

            Server.SendToAll(om, listanet.ToArray(), Net.DeliveModo);
        }

        internal static void RPC_Owner(string funcname, int viewid, object[] param)
        {
            NetworkObj Net = NetworkViews[viewid];

            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewid);

            DoData(om, param);

            Server.SendMessage(om, GetPeer(Net.Owner), Net.DeliveModo);
        }

        internal static void RPC_ALLDimension(string funcname, int viewid, object[] param)
        {
            NetworkObj Net = NetworkViews[viewid];
            Net.Execute(funcname, Server.Myconnection, param);

            var om = Network.MyPeer.CreateMessage();
            om.Write((byte)DataType.RPC);

            om.Write(funcname);
            om.Write(viewid);

            DoData(om, param);

            Server.SendToAll(om, Net.DeliveModo);
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class CallFunc
    {
        public object obj = null;
        public MethodInfo func;
        public ParameterInfo[] parameters;

        /// <summary>
        /// Execute this function with the specified number of parameters.
        /// </summary>

        public object Execute(params object[] pars)
        {
            if (func == null) return null;
            if (parameters == null)
                parameters = func.GetParameters();

            try
            {
                return (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[])) ?
                    func.Invoke(obj, new object[] { pars }) :
                    func.Invoke(obj, pars);
            }
            catch (System.Exception ex)
            {
                if (ex.GetType() == typeof(System.NullReferenceException)) return null;
                Debug.LogException(ex);
                return null;
            }
        }
    }

    /// <summary>
    /// MainClass for Network callback, and whit MonoBehaviour.
    /// </summary>
    public class DarckMonoBehaviour : MonoBehaviour
    {
        public virtual void OnPlayerConnect(NetConnection Peer) { }
        public virtual void OnPlayerDisconnect(NetConnection Peer) { }
        public virtual void OnServerStart() { }
        public virtual void OnServerClose() { }
        public virtual void OnServerAfterClose() { }
        public virtual void OnConnect() { }
        public virtual void OnDisconnect() { }

        void OnEnable()
        {
            Network.Events.Add(this);
        }

        void OnDisable()
        {
            Network.Events.Remove(this);
        }
    }

    [Serializable]
    public class NetViewSerializer
    {
        public int Dimension = 0;
        public int PrefabID = 0;
        public long Owner;
        public int ViewID = 0;
        public NetDeliveryMethod DeliveModo;
        public IdMode IdMode;

        public float p_x;
        public float p_y;
        public float p_z;

        public float r_x;
        public float r_y;
        public float r_z;
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

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class WorldList
    {
        public List<long> Players = new List<long>();
    }
}

public enum DataType : byte
{
    RPC = 0,
    Destroy = 1,
    Destroy_Player = 2,
    Instantiate = 3,
    Instantiate_Pool = 4,
    EnterInWorld = 5,
    CloseConnection = 6,
    ChangeDimension = 7,
    Instantiate_PoolD = 8,
    ExitDimension = 9,

    RPC_All = 10,
    RPC_AllOwner = 11,
    RPC_Owner = 12,
    RPC_ALLDimension = 13
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class RPC : Attribute
{

}

public static class NetConfig
{
    public static string SecretKey = "secret";
    public static int DefaultOutgoingMessageCapacity = 99999;
    public static int SendBufferSize = 131071;
    public static float ConnectionTimeout = 50;
    public static bool AcceptConnection = true;
    public static string AppIdentifier = "UnityGame";
    /// <summary>
    /// TiketRate is the milliseconds to Thread stop and continue. 15 is Recommended
    /// </summary>
    public static int TiketRate = 15;
    /// <summary>
    /// If is true Unat is false, if is false Unat is true.
    /// </summary>
    public static bool DedicatedServer = false;
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