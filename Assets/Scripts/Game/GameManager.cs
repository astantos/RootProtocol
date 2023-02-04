using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*********************/
/* SERVER AND CLIENT */
/*********************/
public class GameManager : NetworkBehaviour
{
    public static GameManager Inst
    {
        get
        {
            if (_inst == null)
            {
                _inst = FindObjectOfType<GameManager>();
            }

            if (_inst == null)
            {
                Debug.LogError("[ CRITICAL ERROR ] There is no GameManager present in the scene");
                return null;
            }
            return _inst;
        }
    }
    public static GameManager _inst;

    public GridManager GridManagerPrefab;
    public GridManager GridManager;

    [SyncVar] [SerializeField] Player PlayerOne;
    [SyncVar] [SerializeField] Player PlayerTwo;

    public Vector3 PlayerPos
    {
        get
        {
            Vector3 firstPos = GridManager.Grid[0][0].transform.position;
            int width = GridManager.Grid.Length;
            int height = GridManager.Grid[width - 1].Length;
            Vector3 secondPos = GridManager.Grid[width - 1][height - 1].transform.position;

            return new Vector3(
                (firstPos.x + secondPos.x) / 2,
                (firstPos.y + secondPos.y) / 2,
                (firstPos.z + secondPos.z) / 2
            );
        }
    }

    protected Coroutine gameRoutine;
    #region Game Init
    public void Initialize()
    {
        GridManager = GameObject.Instantiate(GridManagerPrefab);
        NetworkServer.Spawn(GridManager.gameObject);

        gameRoutine = StartCoroutine(WaitForPlayers());
    }

    public int RegisterPlayer(GameObject player)
    {
        Player playerScript = player.GetComponent<Player>();
        int result = -1;

        if (player != null)
        {
            if (PlayerOne == null)
            {
                PlayerOne = playerScript;
                PlayerOne.SetPlayerOwner((int)Node.Owner.P1);
                result = 0;
            }
            else if (PlayerTwo == null)
            {
                PlayerTwo = playerScript;
                PlayerTwo.SetPlayerOwner((int)Node.Owner.P2);

                // Somewhat shifty code . . .
                // Also Register Player One, which must exist since we are on Player 2
                PlayerOne.SetPlayerOwner((int)Node.Owner.P1);
                result = 1;
            }

            playerScript.SetPosition(PlayerPos);
        }

        return result;
    }

    protected IEnumerator WaitForPlayers()
    {
        while (true)
        {
            if (PlayerOne != null && PlayerTwo != null) break;
            yield return null;
        }
        StartGame();
    }

    public void StartGame()
    {
        Debug.Log($"[GAME MANAGER] Starting Game");

        // Player One Setup
        PlayerOne.SetCurrentNode(0, 0);
        PlayerOne.SetStartNode(0, 0);
        GridManager.SetNodeState(0, 0, (int)Node.Owner.P1);

        // Player Two Setup
        int x = GridManager.Grid.Length - 1;
        int y = GridManager.Grid[x].Length - 1;
        PlayerTwo.SetCurrentNode(x, y);
        GridManager.SetNodeState(x, y, (int)Node.Owner.P2);

        PlayerOne.StartPlayerControl();
        PlayerTwo.StartPlayerControl();
    }
    #endregion

    #region Game Logic
    public void MovePlayer(int player, int x, int y)
    {
        Node.Owner owner = (Node.Owner)player;
        Debug.Log($"[GAME MANAGER] {owner.ToString()} requested to move to {x}, {y}");

        if (GridManager.Grid[x][y].CurrentOwner == Node.Owner.Neutral)
        {
            //GridManager.SetNodeState(x, y, player);
            if (owner == Node.Owner.P1)
                PlayerOne.SetCurrentNode(x, y);
            else if (owner == Node.Owner.P2)
                PlayerTwo.SetCurrentNode(x, y);
            PlayerStartCapture(x, y, owner);
        }
        else if (GridManager.Grid[x][y].CurrentOwner == owner)
        {
            if (owner == Node.Owner.P1)
                PlayerOne.SetCurrentNode(x, y);
            else if (owner == Node.Owner.P2)
                PlayerTwo.SetCurrentNode(x, y);
        }
    }

    public void PlayerStartCapture(int x, int y, Node.Owner owner)
    {
        if (owner == Node.Owner.P1)
            PlayerOne.StartPlayerCapture();
        else if (owner == Node.Owner.P2)
            PlayerTwo.StartPlayerCapture();
    }

    public void PlayerCaptureNode(Node.Owner owner)
    {
        Player player = null;
        if (owner == Node.Owner.P1)
            player = PlayerOne;
        else if (owner == Node.Owner.P2)
            player = PlayerTwo;

        if (player != null)
        {
            GridManager.SetNodeState(player.Current.Coord.x, player.Current.Coord.y, (int)owner);
            player.StartPlayerControl();
        }


    }

    #endregion

    #region Network Callbacks
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[GAME MANAGER] Client Connected");
    }
    #endregion

    #region Commands
    #endregion
}
