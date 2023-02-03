using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    [Space(50)]

    public GameManager GameManagerPrefab;

    protected GameManager gameManager;

    #region Server Callbacks
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[ SERVER ] Server has been started");
        gameManager = GameObject.Instantiate(GameManagerPrefab);
        NetworkServer.Spawn(gameManager.gameObject);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("[ SERVER ] Server has been stopped");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"[ SERVER ] Client {conn.connectionId} has connected!");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log($"[ SERVER ] Client {conn.connectionId} has disconnected!");
    }
    #endregion
}
