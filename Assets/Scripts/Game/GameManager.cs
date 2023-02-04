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

    [Space]
    [SyncVar] [SerializeField] Player PlayerOne;
    [SyncVar] [SerializeField] Player PlayerTwo;

    [Space]
    public float KillInterval;

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
        GridManager.SetNodeOwner(0, 0, (int)Node.Owner.P1);

        // Player Two Setup
        int x = GridManager.Grid.Length - 1;
        int y = GridManager.Grid[x].Length - 1;
        PlayerTwo.SetCurrentNode(x, y);
        PlayerTwo.SetStartNode(x, y);
        GridManager.SetNodeOwner(x, y, (int)Node.Owner.P2);

        PlayerOne.StartPlayerControl();
        PlayerTwo.StartPlayerControl();
    }
    #endregion

    #region Game Logic
    public void MovePlayer(int player, int x, int y, bool moveAnimation)
    {
        Node.Owner owner = (Node.Owner)player;
        Debug.Log($"[GAME MANAGER] {owner.ToString()} requested to move to {x}, {y}");
        if (owner == Node.Owner.P1)
        {
            if (moveAnimation)
                PlayerOne.PlayMoveAnimation(PlayerOne.Current.Coord.x, PlayerOne.Current.Coord.y, x, y);
            else
                CompleteMove(player, x, y);
        }
        else if (owner == Node.Owner.P2)
        {
            if (moveAnimation)
                PlayerTwo.PlayMoveAnimation(PlayerTwo.Current.Coord.x, PlayerTwo.Current.Coord.y, x, y);
            else
                CompleteMove(player, x, y);
        }
    }

    public void CompleteMove(int player, int x, int y)
    {
        Node.Owner owner = (Node.Owner)player;
        bool contested = false;
        if (GridManager.Grid[x][y].CurrentOwner == owner) 
        {
            if (owner == Node.Owner.P1)
            {
                PlayerOne.SetCurrentNode(x, y);
                if (PlayerTwoTarget != null && x == PlayerTwoTarget.Coord.x && y == PlayerTwoTarget.Coord.y)
                {
                    contested = true;
                    PlayerOneTarget = GridManager.Grid[x][y];
                    PlayerOne.StartPlayerCapture(contested);
                    
                    PlayerTwo.SetContested(contested);
                }
            }
            else if (owner == Node.Owner.P2)
            {
                PlayerTwo.SetCurrentNode(x, y);
                PlayerTwoTarget = GridManager.Grid[x][y];
                if (PlayerOneTarget != null && x == PlayerOneTarget.Coord.x && y == PlayerOneTarget.Coord.y)
                {
                    contested = true;
                    PlayerTwoTarget = GridManager.Grid[x][y];
                    PlayerTwo.StartPlayerCapture(contested);
                    
                    PlayerOne.SetContested(true);
                }
            }
        }
        else if (GridManager.Grid[x][y].CurrentOwner == Node.Owner.Neutral)
        {
            if (owner == Node.Owner.P1)
            {
                PlayerOne.SetCurrentNode(x, y);
                
                if (PlayerTwoTarget != null && x == PlayerTwoTarget.Coord.x && y == PlayerTwoTarget.Coord.y)
                {
                    contested = true;
                    PlayerTwo.SetContested(contested);
                }
                
                PlayerOneTarget = GridManager.Grid[x][y];
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
                
                PlayerTwoTarget = GridManager.Grid[x][y];
                PlayerTwo.StartPlayerCapture(contested);
                    
            }
        }
        else // Opponent's Node
        {
            if (owner == Node.Owner.P1)
            {
                PlayerOne.SetCurrentNode(x, y);
                if (PlayerTwo.Current.Coord.x == x && PlayerTwo.Current.Coord.y == y)
                {
                    contested = true;
                    PlayerTwoTarget = GridManager.Grid[x][y];
                    PlayerTwo.StartPlayerCapture(contested);
                }
                PlayerOneTarget = GridManager.Grid[x][y];
                PlayerOne.StartPlayerCapture(contested);
            }
            else if (owner == Node.Owner.P2)
            {
                PlayerTwo.SetCurrentNode(x, y);
                if (PlayerOne.Current.Coord.x == x && PlayerOne.Current.Coord.y == y)
                {
                    contested = true;
                    PlayerOneTarget = GridManager.Grid[x][y];
                    PlayerOne.StartPlayerCapture(contested);
                }
                PlayerTwoTarget = GridManager.Grid[x][y];
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
        Debug.Log($"[GAME MANAGER] Node Captured by {owner.ToString()}!");
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
            GridManager.SetNodeOwner(player.Current.Coord.x, player.Current.Coord.y, (int)owner);
            if (CheckGameWon(player) == false)
            {
                player.StartPlayerControl();
            }
            else
            {
                player.RunEndGame(true);
                if(player.PlayerOwner == Node.Owner.P1)
                {
                    PlayerTwo.RunEndGame(false);    
                }
                else if (player.PlayerOwner == Node.Owner.P2)
                {
                    PlayerOne.RunEndGame(false);    
                }
                else
                {
                    // Should never occur
                    Debug.LogError("[ GAME MANAGER - CRITICAL ERROR] Invalid Player provded to end to game");
                }
            }
        }
    }

    protected void NotifyPlayerLoseNode(Node.Owner owner)
    {
        Debug.Log($"[GAME MANAGER] Notifyng {owner.ToString()} about Node Loss! Will Start Player Control.");
        if (owner == Node.Owner.P1)
        {
            PlayerOne.AcceptLoseNode();
        }
        else if (owner == Node.Owner.P2)
        {
            PlayerTwo.AcceptLoseNode();
        }
    }

    protected bool CheckGameWon(Player player)
    {
        Node target = null;
        if (player.PlayerOwner == Node.Owner.P1)
        {
            target = PlayerTwo.StartNode;
        }
        else if (player.PlayerOwner == Node.Owner.P2)
        {
            target = PlayerOne.StartNode;
        }

        if (player.Current.Coord.x == target.Coord.x && player.Current.Coord.y == target.Coord.y)
        {
            return true;
        }
        return false;
    }
    #endregion

    #region Game End
    public void RunKillRoutine(int startX, int startY, int x, int y)
    {
        KillRoutineRPC(startX, startY, x, y);
    }

    protected IEnumerator KillRoutine(int startX, int startY, int x, int y)
    {
        List<Node> frontNodes = new List<Node>();
        frontNodes.Add(GridManager.Inst.GetNode(startX, startY));

        float timer = KillInterval;
        while (true)
        {
            bool foundTarget = false;
            if (timer >= KillInterval)
            {
                timer -= KillInterval;
                List<Node> newFront = new List<Node>();
                for (int index = 0; index < frontNodes.Count; index++)
                {
                    Node current = frontNodes[index];
                    if (current.Up.Node != null && current.Up.Node.CurrentOwner != Node.Owner.Dead) newFront.Add(current.Up.Node);
                    if (current.Down.Node != null && current.Down.Node.CurrentOwner != Node.Owner.Dead) newFront.Add(current.Down.Node);
                    if (current.Left.Node != null && current.Left.Node.CurrentOwner != Node.Owner.Dead) newFront.Add(current.Left.Node);
                    if (current.Right.Node != null && current.Right.Node.CurrentOwner != Node.Owner.Dead) newFront.Add(current.Right.Node);
                    current.SetState(Node.Owner.Dead);
                    if (current.Coord.x == x && current.Coord.y == y) foundTarget = true;
                }
                frontNodes = newFront;
            }

            if (foundTarget) break;

            yield return null;
            timer += Time.deltaTime;
        }

        if (PlayerOne.isLocalPlayer) PlayerOne.LaunchSummary(x, y);
        if (PlayerTwo.isLocalPlayer) PlayerTwo.LaunchSummary(x, y);

        Debug.LogWarning("[GAME MANAGER] GAME IS OVER");
    }
    #endregion

    #region RPCs
    [ClientRpc]
    public void KillRoutineRPC(int startX, int startY, int x, int y)
    {
        StartCoroutine(KillRoutine(startX, startY, x,y));
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
