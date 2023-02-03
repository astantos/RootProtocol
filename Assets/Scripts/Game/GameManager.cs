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

    [SyncVar][SerializeField] Player PlayerOne;
    [SyncVar][SerializeField] Player PlayerTwo;

    public Vector3 PlayerPos
    {
        get
        {
            Vector3 firstPos = GridManager.Grid[0][0].transform.position;
            int width = GridManager.Grid.Length;
            int height = GridManager.Grid[width - 1].Length;
            Vector3 secondPos = GridManager.Grid[width - 1][height - 1].transform.position;

            return new Vector3(
                (firstPos.x + secondPos.x)/2,
                (firstPos.y + secondPos.y)/2,
                (firstPos.z + secondPos.z)/2
            );
        }
    }

    protected Coroutine gameRoutine;

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
                result = 0;
            }
            else if (PlayerTwo == null)
            {
                PlayerTwo = playerScript;
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

        GridManager.SetNodeState(0, 0, (int)Node.State.P1);
        GridManager.SetNodeState(GridManager.GridDimensions.Width - 1, GridManager.GridDimensions.Height - 1, (int)Node.State.P2);
    }

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
