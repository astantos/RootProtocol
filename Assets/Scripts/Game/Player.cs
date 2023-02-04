using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour 
{
    public Camera MainCamera;

    public Node.Owner PlayerOwner;
    public Node Current { get; protected set; }

    protected Coroutine controlRoutine;

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

    [Command]
    public void RequestPlayerMove(int x, int y)
    {
        GameManager.Inst.MovePlayer((int)PlayerOwner, x, y);
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
    public void StartPlayerControl()
    {
        if (isLocalPlayer)
            controlRoutine = StartCoroutine(PlayerControlRoutine());
    }

    [ClientRpc]
    public void StartPlayerCapture()
    {
        if (!isLocalPlayer) return;

        if (controlRoutine != null)
            StopCoroutine(controlRoutine);

        controlRoutine = StartCoroutine(PlayerCaptureRoutine());
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

    protected IEnumerator PlayerControlRoutine()
    {
        Debug.Log($"[PLAYER] {((Node.Owner)PlayerOwner).ToString()} Control Started");
        while (true)
        {
            Node target = null;
            if (Input.GetKeyDown(KeyCode.UpArrow) && Current.Up.Node != null)
            {
                target = Current.Up.Node;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) && Current.Down.Node != null)
            {
                target = Current.Down.Node;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) && Current.Left.Node != null)
            {
                target = Current.Left.Node;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) && Current.Right.Node != null)
            {
                target = Current.Right.Node;
            }

            if (target != null)
                RequestPlayerMove(target.Coord.x, target.Coord.y);

            yield return null;
        }
    }

    protected IEnumerator PlayerCaptureRoutine()
    {
        Debug.Log($"[PLAYER] {((Node.Owner)PlayerOwner).ToString()} Starting Capture Process!");
        while (true)
        {
            yield return null;
        }
    }
}
