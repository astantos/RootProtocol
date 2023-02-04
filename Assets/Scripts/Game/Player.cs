using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class Player : NetworkBehaviour 
{
    public Camera MainCamera;

    public Node.Owner PlayerOwner;

    [Header("Capture")]
    public TextMeshProUGUI Difficulty;
    public TextMeshProUGUI CommandText;
    public TextMeshProUGUI CommandTextMatched;
    public TextMeshProUGUI InputText;
    public TextMeshProUGUI TextHistory;
    
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

    #region Coroutines
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

        int difficulty = SetDifficulty();
        List<string> commandList = SetCommandText(difficulty);

        string currentInput = "";
        char frameInput;
        while (true)
        {
            if (Input.inputString.Length > 0)
            {
                frameInput = Input.inputString[0];

                if (frameInput == '\b' && currentInput.Length > 0) // Backspace
                {
                    currentInput = currentInput.Substring(0, currentInput.Length - 1);
                }
                else if (frameInput == '\n' || frameInput == '\r') // Enter/Return
                {
                    // Do Comparison Here
                }
                else if (chars.Contains(char.ToUpper(frameInput)) || frameInput == ' ')
                {
                    currentInput = $"{currentInput}{char.ToUpper(frameInput)}";
                }

                SetInputText(currentInput);
            }
            yield return null;
        }
    }
    #endregion

    #region Capture
    protected int SetDifficulty()
    {
        int difficulty = CountDifficulty();

        Difficulty.text = $"Node Difficulty: <color=\"red\">{difficulty}</color>";

        return difficulty;
    }

    protected int CountDifficulty()
    {
        int difficulty = 0;
        if (Current.Up.Node != null && Current.Up.Node.CurrentOwner != Node.Owner.Neutral
            && Current.Up.Node.CurrentOwner != PlayerOwner)
        {
            difficulty++;
        }
        
        if (Current.Down.Node != null && Current.Down.Node.CurrentOwner != Node.Owner.Neutral
            && Current.Down.Node.CurrentOwner != PlayerOwner)
        {
            difficulty++;
        }
        
        if (Current.Left.Node != null && Current.Left.Node.CurrentOwner != Node.Owner.Neutral
            && Current.Left.Node.CurrentOwner != PlayerOwner)
        {
            difficulty++;
        }
        
        if (Current.Right.Node != null && Current.Right.Node.CurrentOwner != Node.Owner.Neutral
            && Current.Right.Node.CurrentOwner != PlayerOwner)
        {
            difficulty++;
        }
        
        if (Current.Up.Node != null && Current.Up.Node.CurrentOwner == PlayerOwner)
        {
            difficulty--;
        }
        
        if (Current.Down.Node != null && Current.Down.Node.CurrentOwner == PlayerOwner)
        {
            difficulty--;
        }
        
        if (Current.Left.Node != null && Current.Left.Node.CurrentOwner == PlayerOwner)
        {
            difficulty--;
        }
        
        if (Current.Right.Node != null && Current.Right.Node.CurrentOwner == PlayerOwner)
        {
            difficulty--;
        }

        difficulty = Mathf.Clamp(difficulty, 0, 3);
        return difficulty;
    }

    protected string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    protected List<string> SetCommandText(int difficulty)
    {
        CommandText.text = "";
        CommandTextMatched.text = "";
        List<string> commandList = new List<string>();
        for (int line = 0; line < (difficulty + 2) * 4; line++)
        {
            string word = "";
            for (int c = 0; c < 4; c++)
            {
                word += chars[Random.Range(0, chars.Length)];
            }
            commandList.Add(word);
            CommandText.text = $"{CommandText.text}{word} ";
        }
        return commandList;
    }

    protected void SetInputText(string currentInput)
    {
        InputText.text = $"<color=\"red\">$udo ></color> {currentInput}";
    }
    #endregion
}
