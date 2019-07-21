using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class Server : MonoBehaviour
{
    private const int MAT_USER = 8;
    private const int PORT = 26000;
    private const int WEB_PORT = 26001;
    private const int BYTE_SIZE = 1024;


    private byte reliableChannel;

    private int hostID;
    private int webHostId;

    private bool isStarted = false;
    private byte error;

    #region Monovehaviour
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Init();
    }

	void Update()
	{
		UpdateMessagePump();
	}
	#endregion

	public void Init()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);
        HostTopology topo = new HostTopology(cc, MAT_USER);
        hostID = NetworkTransport.AddHost(topo, PORT, null);
        webHostId = NetworkTransport.AddWebsocketHost(topo, WEB_PORT, null);

        Debug.Log(string.Format("Opening connection on port {0} and webport {1}", PORT, WEB_PORT));
        isStarted = true;
    }

    public void ShutDown()
    {
        isStarted = false;
        NetworkTransport.Shutdown();
    }

	public void UpdateMessagePump()
	{
        if (!isStarted)
        {
            return;
        }

        int recHostId; // From Web or standalone
        int connectionId; // Which user
        int channelId; // Which lane is the message from

        byte[] recBuffer = new byte[BYTE_SIZE];
        int datasize;

        NetworkEventType type = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, BYTE_SIZE, out datasize, out error);
        switch (type)
        {
            case NetworkEventType.Nothing:
                break;

            case NetworkEventType.ConnectEvent:
                Debug.Log(string.Format("User {0} has connected through host {1}!", connectionId, recHostId));
                break;

            case NetworkEventType.DisconnectEvent:
                Debug.Log(string.Format("User {0} has disconnected!", connectionId));
                break;

            case NetworkEventType.BroadcastEvent:
                Debug.Log(string.Format("Unexpected network event type"));
                break;

            case NetworkEventType.DataEvent:
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(recBuffer);
                NetMsg msg = (NetMsg)formatter.Deserialize(ms);
                OnData(connectionId, channelId, recHostId, msg);
                break;

            default:
                break;

        }
	}

    #region OnData
    private void OnData(int connectionId, int channelId, int recHostId, NetMsg msg)
    {
        switch (msg.OP)
        {
            case NetOP.None:
                Debug.Log("Unexpected NETOP");
                break;

            case NetOP.CreateAccount:
                CreateAccount(connectionId, connectionId, connectionId, (Net_CreateAccount)msg);
                break;

            case NetOP.LoginRequest:
                LoginRequest(connectionId, connectionId, connectionId, (Net_LoginRequest)msg);
                break;
        }
    }

    private void CreateAccount(int connectionId, int channelId, int recHostId, Net_CreateAccount ca)
    {
        Debug.Log(string.Format("{0}, {1}, {2}", ca.Username, ca.Password, ca.Email));

        Net_OnCreateAccount oca = new Net_OnCreateAccount();
        oca.Success = 1;
        oca.Information = "Account was created!";

        SendClient(recHostId, connectionId, oca);
    }

    private void LoginRequest(int connectionId, int channelId, int recHostId, Net_LoginRequest lr)
    {
        Debug.Log(string.Format("{0}, {1}", lr.UsernameOrEmail, lr.Password));

        Net_OnLoginRequest olr = new Net_OnLoginRequest();
        olr.Success = 1;
        olr.Information = "Logged in!";
        olr.Username = "troy";
        olr.Discriminator = "0000";
        olr.Token = "Token";

        SendClient(recHostId, connectionId, olr);
    }
    #endregion

    #region send
    public void SendClient (int recHost, int connectionId, NetMsg msg)
    {
        byte[] buffer = new byte[BYTE_SIZE];

        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms, msg);
        if(recHost == 1)
        {
            NetworkTransport.Send(hostID, connectionId, reliableChannel, buffer, BYTE_SIZE, out error);
        }
        else
        {
            NetworkTransport.Send(webHostId, connectionId, reliableChannel, buffer, BYTE_SIZE, out error);
        }
    }
    #endregion
}
