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

    public int CaptureBaseLines;
    public int EnemyNodeDifficulty;
    public int ContestBuffer;

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

    // Capture \\
    protected Node PlayerOneTarget;
    protected Node PlayerTwoTarget;

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

        if (GridManager.Grid[x][y].CurrentOwner == owner)
        {
            if (owner == Node.Owner.P1)
                PlayerOne.SetCurrentNode(x, y);
            else if (owner == Node.Owner.P2)
                PlayerTwo.SetCurrentNode(x, y);
        }
        else
        {
            bool contested = false;
            if (owner == Node.Owner.P1)
            {
                PlayerOne.SetCurrentNode(x, y);
                PlayerOneTarget = GridManager.Grid[x][y];
                if (PlayerTwoTarget != null && x == PlayerTwoTarget.Coord.x && y == PlayerTwoTarget.Coord.y)
                {
                    contested = true;
                    PlayerTwo.SetContested(contested);
                }

                PlayerOne.StartPlayerCapture(contested);
            }
            else if (owner == Node.Owner.P2)
            {
                PlayerTwo.SetCurrentNode(x, y);
                PlayerTwoTarget = GridManager.Grid[x][y];
                if (PlayerOneTarget != null && x == PlayerOneTarget.Coord.x && y == PlayerOneTarget.Coord.y)
                {
                    contested = true;
                    PlayerOne.SetContested(true);
                }

                PlayerTwo.StartPlayerCapture(contested);
            }
        }
    }

    public void AcceptBufferComplete(Node.Owner owner)
    {
        if (owner == Node.Owner.P1)
        {
            PlayerTwo.AcceptIncomingBuffer();
        }
        else if (owner == Node.Owner.P2)
        {
            PlayerOne.AcceptIncomingBuffer();
        }
    }

    public void PlayerCaptureNode(Node.Owner owner)
    {
        Player player = null;
        if (owner == Node.Owner.P1)
        {
            player = PlayerOne;
            if (PlayerTwoTarget != null 
                && PlayerTwoTarget.Coord.x == PlayerOneTarget.Coord.x 
                && PlayerTwoTarget.Coord.y == PlayerOneTarget.Coord.y)
            {
                NotifyPlayerLoseNode(Node.Owner.P2);
            }
            PlayerOneTarget = null;
        }
        else if (owner == Node.Owner.P2)
        {
            player = PlayerTwo;
            if (PlayerOneTarget != null 
                && PlayerOneTarget.Coord.x == PlayerTwoTarget.Coord.x 
                && PlayerOneTarget.Coord.y == PlayerTwoTarget.Coord.y)
            {
                NotifyPlayerLoseNode(Node.Owner.P1);
            }
            PlayerTwoTarget = null;
        }

        if (player != null)
        {
            GridManager.SetNodeState(player.Current.Coord.x, player.Current.Coord.y, (int)owner);
            player.StartPlayerControl();
        }
    }

    protected void NotifyPlayerLoseNode(Node.Owner owner)
    {
        if (owner == Node.Owner.P1)
        {
            PlayerOne.AcceptLoseNode();
            PlayerOne.StartPlayerControl();
        }
        else if (owner == Node.Owner.P2)
        {
            PlayerTwo.AcceptLoseNode();
            PlayerTwo.StartPlayerControl();
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
}
