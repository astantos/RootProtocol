using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour 
{
    public Camera MainCamera;

    public Node.Owner PlayerOwner;
    public Node Current { get; protected set; }

    #region Network Callbacks 
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[PLAYER_SCRIPT] Client Started");
        if (!isLocalPlayer)
            GameObject.Destroy(MainCamera.gameObject);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log("[PLAYER_SCRIPT] Local Player Started");
        RequestPlayerRegistration();
    }
    #endregion

    #region Commands
    [Command]
    public void RequestPlayerRegistration(NetworkConnectionToClient conn = null)
    {
        int result = GameManager.Inst.RegisterPlayer(gameObject);
        PlayerRegistrationResponse(conn, result);
    }

    #endregion

    #region RPCS
    [TargetRpc]
    public void PlayerRegistrationResponse(NetworkConnection conn, int result)
    {
        Debug.Log($"[ CLIENT ] Registration Response {(result != -1 ? "SUCCESS" : "FAILURE")}");
    }

    [ClientRpc]
    public void SetPlayerOwner(int owner)
    {
        PlayerOwner = (Node.Owner)owner;
    }

    [ClientRpc]
    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    [ClientRpc]
    public void SetCurrentNode(int x, int y)
    {
        if (Current != null)
        {
            Current.SetSelected(PlayerOwner, false);
        }
        Current = GridManager.Inst.GetNode(x, y);
        Current.SetSelected(PlayerOwner, true);
    }

    #endregion
}
