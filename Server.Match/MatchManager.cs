using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Match;

public class MatchManager
{
    private readonly string Host;
    private readonly int Port;
    public Socket MainSocket;
    public bool ServerIsClosed;
    private StateObject State;
    private readonly int BufferSize = 8096;
    private readonly int MaxClients = 1000;
    private readonly Stack<SocketAsyncEventArgs> Pool = new Stack<SocketAsyncEventArgs>();

    public MatchManager(string Host, int Port)
    {
        this.Host = Host;
        this.Port = Port;
    }

    public bool Start()
    {
        try
        {
            MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            MainSocket.IOControl(-1744830452, new byte[1], null);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(Host), Port);
            MainSocket.Bind(localEP);
            for (int i = 0; i < MaxClients; i++)
            {
                SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
                saea.SetBuffer(new byte[BufferSize], 0, BufferSize);
                saea.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                saea.Completed += OnReceive;
                Pool.Push(saea);
            }
            StartReceive();
            CLogger.Print($"Match Serv Address {localEP}", LoggerType.Info);
            return true;
        }
        catch (Exception ex)
        {
            CLogger.Print("Start Error: " + ex.Message, LoggerType.Error, ex);
            return false;
        }
    }

    private void StartReceive()
    {
        if (!ServerIsClosed && Pool.Count != 0)
        {
            SocketAsyncEventArgs saea;
            lock (Pool)
            {
                saea = Pool.Pop();
            }
            if (!MainSocket.ReceiveFromAsync(saea))
            {
                ProcessReceive(saea);
            }
        }
    }

    private void OnReceive(object sender, SocketAsyncEventArgs e)
    {
        ProcessReceive(e);
        StartReceive();
    }

    private void ProcessReceive(SocketAsyncEventArgs e)
    {
        if (ServerIsClosed || e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
        {
            lock (Pool)
            {
                Pool.Push(e);
                return;
            }
        }
        byte[] received = new byte[e.BytesTransferred];
        Buffer.BlockCopy(e.Buffer, 0, received, 0, e.BytesTransferred);
        if (received.Length >= 22)
        {
            MatchClient client = new MatchClient(MainSocket, (IPEndPoint)e.RemoteEndPoint);
            BeginReceive(client, received);
        }
        e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        lock (Pool)
        {
            Pool.Push(e);
        }
    }

    public bool StartOLD()
    {
        try
        {
            MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            uint IOC_IN = 2147483648u;
            uint IOC_VENDOR = 402653184u;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 0xC;
            MainSocket.IOControl((int)SIO_UDP_CONNRESET, new byte[1] { Convert.ToByte(value: false) }, null);
            IPEndPoint Local = new IPEndPoint(IPAddress.Parse(Host), Port);
            State = new StateObject
            {
                WorkSocket = MainSocket,
                LocalPoint = Local
            };
            MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
            MainSocket.SetIPProtectionLevel(IPProtectionLevel.EdgeRestricted);
            MainSocket.Bind(Local);
            Thread ThreadMatch = new Thread(Read)
            {
                Priority = ThreadPriority.Highest
            };
            ThreadMatch.Start();
            CLogger.Print($"Match Serv Address {Local.Address}:{Local.Port}", LoggerType.Info);
            return true;
        }
        catch (Exception ex)
        {
            CLogger.Print(ex.Message, LoggerType.Error, ex);
            return false;
        }
    }

    private void Read()
    {
        try
        {
            MainSocket.BeginReceiveFrom(State.Buffer, 0, 8192, SocketFlags.None, ref State.LocalPoint, AcceptCallback, State);
        }
        catch (Exception ex)
        {
            CLogger.Print(ex.Message, LoggerType.Error, ex);
        }
    }

    private void AcceptCallback(IAsyncResult result)
    {
        if (ServerIsClosed)
        {
            return;
        }
        StateObject state = (StateObject)result.AsyncState;
        Socket socket = state.WorkSocket;
        EndPoint remoteEP = state.LocalPoint;
        try
        {
            int received = socket.EndReceiveFrom(result, ref remoteEP);
            if (received > 8192)
            {
                CLogger.Print($"Received Size Exceeded! (Size: {received})", LoggerType.Warning);
                return;
            }
            byte[] packetData = new byte[received];
            Buffer.BlockCopy(state.Buffer, 0, packetData, 0, received);
            if (packetData.Length >= 22)
            {
                MatchClient client = new MatchClient(socket, (IPEndPoint)remoteEP);
                BeginReceive(client, packetData);
            }
        }
        catch (Exception ex)
        {
            CLogger.Print("UDP Receive Error: " + ex.Message, LoggerType.Error, ex);
        }
        try
        {
            StateObject stateObject = new StateObject();
            stateObject.WorkSocket = socket;
            stateObject.LocalPoint = new IPEndPoint(IPAddress.Any, 0);
            stateObject.Buffer = new byte[8192];
            StateObject nextState = stateObject;
            socket.BeginReceiveFrom(nextState.Buffer, 0, 8192, SocketFlags.None, ref nextState.LocalPoint, AcceptCallback, nextState);
        }
        catch (Exception ex2)
        {
            CLogger.Print("Error starting next BeginReceiveFrom: " + ex2.Message, LoggerType.Error, ex2);
        }
    }

    public void BeginReceive(MatchClient Client, byte[] Buffer)
    {
        try
        {
            Client?.BeginReceive(Buffer, DateTimeUtil.Now());
        }
        catch (Exception ex)
        {
            CLogger.Print(ex.Message, LoggerType.Error, ex);
        }
    }

    public void SendPacket(byte[] Data, IPEndPoint Address)
    {
        MainSocket.SendTo(Data, 0, Data.Length, SocketFlags.None, Address);
    }
}
