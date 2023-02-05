using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

public class Player : NetworkBehaviour
{
    public Camera MainCamera;

    public Node.Owner PlayerOwner;

    [Header("Audio")]
    public AudioSource BGM;
    public float BGMIntroDuration;
    public AudioManager AudioManager;

    [Header("PARTICLES")]
    [Header("Movement")]
    public float MovementDuration;
    public ParticleSystem TravelParticlesPrefab;

    [Header("Death")]
    public ParticleSystem DeathParticlesPrefab;

    [Header("Capture")]
    public ParticleSystem CaptureParticlesPrefab;

    [Header("UI")]
    public GameObject PlayerOneLabel;
    public GameObject PlayerTwoLabel;

    [Header("Capture")]
    public GameObject GameUI;
    public TextMeshProUGUI Difficulty;
    public TextMeshProUGUI CommandText;
    public TextMeshProUGUI CommandTextMatched;
    public TextMeshProUGUI InputText;
    public TextMeshProUGUI TextHistory;
    public GameObject Instructions;
    public CaptureLine PlayerOneCapture;
    public CaptureLine PlayerTwoCapture;

    [Header("End Game Parameters")]
    public float LoseGameDuration;
    public float LoseGameOverlayAlpha;
    [Space]
    public float SpecialConsoleDuration;
    public AnimationCurve SpecialConsoleCurve;
    [Space]
    public string RemoveCommand;
    public float KeyInterval;
    [Space]
    public float SummaryDuration;

    [Header("End Game Objects")]
    public Image LoseGameOverlay;
    public RectTransform SpecialConsole;
    public TextMeshProUGUI SpecialConsoleText;
    public GameObject SpecialConsoleInstructions;
    public Image SummaryScreen;
    public TextMeshProUGUI WinText;
    public TextMeshProUGUI LoseText;

    public Node StartNode { get; protected set; }
    public Node Previous { get; protected set; }
    public Node Current { get; protected set; }
    protected Coroutine controlRoutine;

    #region Capture Variables
    protected List<string> commandList;
    protected bool contested;
    #endregion

    protected bool gameOver = false;

    private void Start()
    {
        commandList = new List<string>();
    }

    protected void InitializeAudio()
    {
        StartCoroutine(BGMIntroRoutine());
    }

    #region Network Callbacks 
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[PLAYER_SCRIPT] Client Started");
        if (!isLocalPlayer)
        {
            GameObject.Destroy(MainCamera.gameObject);
            GameObject.Destroy(BGM);
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log("[PLAYER_SCRIPT] Local Player Started");

        InitializeAudio();

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
    public void RequestPlayerMove(int x, int y, bool moveAnimation)
    {
        if (controlRoutine != null)
        {
            StopCoroutine(controlRoutine);
            controlRoutine = null;
        }
        GameManager.Inst.MovePlayer((int)PlayerOwner, x, y, moveAnimation);
    }

    [Command]
    public void ReportBufferComplete()
    {
        GameManager.Inst.AcceptBufferComplete(PlayerOwner);
    }

    [Command]
    public void RequestNodeCapture()
    {
        PlayCaptureAnimation(Current.Coord.x, Current.Coord.y);
    }

    [Command]
    public void PlayDeathAnimation(int x, int y)
    {
        PlayDeathAnimationRpc(x, y);
    }

    [Command]
    public void RequestKillRoutine()
    {
        GameManager.Inst.RunKillRoutine(
            StartNode.Coord.x, StartNode.Coord.y,
            Current.Coord.x, Current.Coord.y
        );
    }
   
    [Command]    
    public void UpdateCaptureProportion(float proportion)
    {
        UpdateCaptureProportionRpc(proportion);
    }

    [Command]
    public void StopCaptureLine()
    {
        StopCaptureLineRpc();
    }
    #endregion

    #region RPCS
    [TargetRpc]
    public void PlayerRegistrationResponse(NetworkConnection conn, int result)
    {
        Debug.Log($"[ CLIENT ] Registration Response {(result != -1 ? "SUCCESS" : "FAILURE")}");
        PlayerOneLabel.SetActive(result == 0);
        PlayerTwoLabel.SetActive(result == 1);
    }

    [ClientRpc]
    public void PlayMoveAnimation(int startX, int startY, int x, int y)
    {
        StartCoroutine(MoveRoutine(startX, startY, x, y));
    }

    [ClientRpc]
    public void PlayDeathAnimationRpc(int x, int y)
    {
        ParticleSystem particles = GameObject.Instantiate(DeathParticlesPrefab);

        particles.transform.position = GridManager.Inst.GetNode(x, y).transform.position;
        particles.transform.Translate(Vector3.back);

        MainModule main = particles.main;
        main.startColor = PlayerOwner == Node.Owner.P1 ? Current.PlayerOneColor : Current.PlayerTwoColor;

        particles.Play();
        AudioManager.PlayEffect(AudioManager.Effect.PlayerDeath, isLocalPlayer);
    }

    [ClientRpc]
    public void PlayCaptureAnimation(int x, int y)
    {
        StartCoroutine(CaptureAnimationRoutine(x, y));
    }

    [ClientRpc]
    public void SetPlayerOwner(int owner)
    {
        PlayerOwner = (Node.Owner)owner;
    }

    [ClientRpc]
    public void StartPlayerControl()
    {
        if (!isLocalPlayer || gameOver) return;
        
        if (controlRoutine != null)
            StopCoroutine(controlRoutine);

        controlRoutine = StartCoroutine(PlayerControlRoutine());
    }

    [ClientRpc]
    public void StartPlayerCapture(bool cont)
    {
        if (gameOver) return;

        if (isLocalPlayer)
        {
            if (controlRoutine != null)
                StopCoroutine(controlRoutine);

            contested = cont;
            controlRoutine = StartCoroutine(PlayerCaptureRoutine());
        }

        CaptureLine line = PlayerOwner == Node.Owner.P1 ? PlayerOneCapture : PlayerTwoCapture;
        line.StartDataLine(StartNode.Coord.x, StartNode.Coord.y, Current.Coord.x, Current.Coord.y);
    }

    [ClientRpc]
    public void AcceptIncomingBuffer()
    {
        Debug.Log($"[PLAYER] Server Sent Buffer {PlayerOwner.ToString()}");
        if (!isLocalPlayer)
        {
            Debug.Log($"[PLAYER] Is not Local Player {PlayerOwner.ToString()}, not taking Buffer");
            return; // Only Local Player should be receiving Buffers
        }

        Debug.Log($"[PLAYER] Is Local Player {PlayerOwner.ToString()}, taking Buffer");
        AudioManager.PlayEffect(AudioManager.Effect.ReceiveBuffer, isLocalPlayer);
        AddRandomCommand();
    }

    [ClientRpc]
    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    [ClientRpc]
    public void SetStartNode(int x, int y)
    {
        StartNode = GridManager.Inst.GetNode(x, y);
    }

    [ClientRpc]
    public void SetCurrentNode(int x, int y)
    {
        if (Current != null)
        {
            Previous = Current;
            Previous.SetSelected(PlayerOwner, false);
        }
        Current = GridManager.Inst.GetNode(x, y);
        Current.SetSelected(PlayerOwner, true);
    }

    [ClientRpc]
    public void SetContested(bool cont)
    {
        if (isLocalPlayer) contested = cont;
    }

    [ClientRpc]
    public void AcceptLoseNode()
    {
        CaptureLine line = PlayerOwner == Node.Owner.P1 ? PlayerOneCapture : PlayerTwoCapture;
        line.StopDataLine();

        if (!isLocalPlayer)
        {
            return;
        }

        WipeAllConsole();

        if (Previous == null || Previous.CurrentOwner != PlayerOwner)
        {
            PlayDeathAnimation(Current.Coord.x, Current.Coord.y);
            Previous = null;
            RequestPlayerMove(StartNode.Coord.x, StartNode.Coord.y, false);
        }
        else
        {
            Node temp = Previous;
            Previous = null;
            RequestPlayerMove(temp.Coord.x, temp.Coord.y, true);
        }
    }

    [ClientRpc]
    public void RunEndGame(bool won)
    {
        gameOver = true;

        if (!isLocalPlayer) return;
        
        if (controlRoutine != null)
            StopCoroutine(controlRoutine);

        if (won)
            StartCoroutine(WinGameRoutine());
        else
            StartCoroutine(LoseGameRoutine());
    }
    
    [ClientRpc] 
    public void UpdateCaptureProportionRpc(float proportion)
    {
        CaptureLine line = PlayerOwner == Node.Owner.P1 ? PlayerOneCapture : PlayerTwoCapture;
        line.UpdateCaptureProportion(proportion);
    }

    [ClientRpc]
    public void StopCaptureLineRpc()
    {
        CaptureLine line = PlayerOwner == Node.Owner.P1 ? PlayerOneCapture : PlayerTwoCapture;
        line.StopDataLine();
    }
    #endregion

    #region Coroutines
    protected IEnumerator BGMIntroRoutine()
    {
        float timer = 0; 
        while (timer < BGMIntroDuration)
        {
            float proportion = timer / BGMIntroDuration;
            BGM.volume = proportion;

            yield return null;
            timer += Time.deltaTime;
        }
        BGM.volume = 1;
    }

    protected IEnumerator CaptureAnimationRoutine(int x, int y)
    {
        StopCaptureLine();
        ParticleSystem particles = GameObject.Instantiate(CaptureParticlesPrefab);

        particles.transform.position = GridManager.Inst.GetNode(x, y).transform.position;
        particles.transform.Translate(Vector3.back);

        MainModule main = particles.main;
        main.startColor = PlayerOwner == Node.Owner.P1 ? Current.PlayerOneColor : Current.PlayerTwoColor;

        particles.Play();

        AudioManager.PlayEffect(AudioManager.Effect.StartCapture, isLocalPlayer);

        yield return new WaitForSeconds(particles.main.duration + 0.5f);

        AudioManager.PlayEffect(AudioManager.Effect.Capture, isLocalPlayer);

        if (isServer)
            GameManager.Inst.PlayerCaptureNode(PlayerOwner);
    }

    protected IEnumerator MoveRoutine(int startX, int startY, int x, int y)
    {
        AudioManager.PlayEffect(AudioManager.Effect.Move, isLocalPlayer);

        Current.SetSelected(PlayerOwner, false);
        ParticleSystem travelparticles = GameObject.Instantiate(TravelParticlesPrefab);

        // Calculate Direction
        Vector3 rot = Vector3.zero;
        if (x > startX) // RIGHT
            rot = new Vector3(0, 90, 0);
        else if (x < startX) // LEFT
            rot = new Vector3(0, -90, 0);
        else if (y > startY) // UP
            rot = new Vector3(-90, 0, 0);
        else
            rot = new Vector3(90, 0, 0);

        // Offset
        travelparticles.transform.position = Current.transform.position;
        travelparticles.transform.Translate(Vector3.back);

        // Apply Rotation
        travelparticles.transform.eulerAngles = rot;

        // Change Color
        MainModule main = travelparticles.main;
        main.startColor = PlayerOwner == Node.Owner.P1 ? Current.PlayerOneColor : Current.PlayerTwoColor;

        travelparticles.Play();

        float timer = 0;
        while (timer < MovementDuration)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        travelparticles.Stop();

        GridManager.Inst.GetNode(x, y).SetSelected(PlayerOwner, true);
        if (isServer) GameManager.Inst.CompleteMove((int)PlayerOwner, x, y);
    }

    #region Permanent Routines
    protected IEnumerator PlayerControlRoutine()
    {
        Debug.Log($"[PLAYER] {((Node.Owner)PlayerOwner).ToString()} Control Started");
        while (true)
        {
            Node target = null;
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (Current.Up.Node != null)
                {
                    target = Current.Up.Node;
                }
                else
                {
                    GridManager.Inst.GetNode(Current.Coord.x, Current.Coord.y).Shake();
                    AudioManager.PlayEffect(AudioManager.Effect.InvalidMove, isLocalPlayer);
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (Current.Down.Node != null)
                {
                    target = Current.Down.Node;
                }
                else
                {
                    GridManager.Inst.GetNode(Current.Coord.x, Current.Coord.y).Shake();
                    AudioManager.PlayEffect(AudioManager.Effect.InvalidMove, isLocalPlayer);
                }
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (Current.Left.Node != null)
                {
                    target = Current.Left.Node;
                }
                else
                {
                    GridManager.Inst.GetNode(Current.Coord.x, Current.Coord.y).Shake();
                    AudioManager.PlayEffect(AudioManager.Effect.InvalidMove, isLocalPlayer);
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) && Current.Right.Node != null)
            {
                if (Current.Right.Node != null)
                {
                    target = Current.Right.Node;
                }
                else
                {
                    GridManager.Inst.GetNode(Current.Coord.x, Current.Coord.y).Shake();
                    AudioManager.PlayEffect(AudioManager.Effect.InvalidMove, isLocalPlayer);
                }
            }

            if (target != null)
            {
                RequestPlayerMove(target.Coord.x, target.Coord.y, true);
                break;
            }
            
            yield return null;
        }
    }

    protected IEnumerator PlayerCaptureRoutine()
    {
        Debug.Log($"[PLAYER] {((Node.Owner)PlayerOwner).ToString()} Starting Capture Process!");

        int difficulty = SetDifficulty();
        SetCommandText(difficulty);

        string currentInput = "";
        SetInputText(currentInput);

        Instructions.SetActive(true);

        int bufferCount = 0;
        int matchedIndex = 0;
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
                    AddTextHistory(currentInput);
                    string trimmed = currentInput.Trim();
                    currentInput = "";
                    SetInputText(currentInput);
                    if (trimmed.Equals(commandList[matchedIndex]))
                    {
                        CommandTextMatched.text = $"{CommandTextMatched.text}{trimmed} ";
                        matchedIndex++;
                        UpdateCaptureProportion(((float)matchedIndex) / commandList.Count);

                        if (contested)
                        {
                            bufferCount++;
                            if (bufferCount >= GameManager.Inst.ContestBuffer)
                            {
                                bufferCount = 0;
                                AudioManager.PlayEffect(AudioManager.Effect.SendBuffer, isLocalPlayer);
                                ReportBufferComplete();
                            }
                        }

                        if (matchedIndex >= commandList.Count)
                        {
                            WipeAllConsole();
                            break;
                        }
                    }
                    else
                    {
                        AddTextHistoryError();
                    }
                }
                else if (chars.Contains(char.ToUpper(frameInput)) || frameInput == ' ')
                {
                    currentInput = $"{currentInput}{char.ToUpper(frameInput)}";
                }

                SetInputText(currentInput);
            }
            yield return null;
        }
        RequestNodeCapture();
    }
    #endregion

    #region End Game Routines    
    protected IEnumerator WinGameRoutine()
    {
        GameUI.SetActive(false);

        yield return LaunchSpecialConsole();
        yield return TypeSpecialCommand();
        yield return CloseSpecialConsole();
        RequestKillRoutine();

        Debug.Log("[PLAYER] GAME OVER");

        yield return null;
    }

    protected IEnumerator LoseGameRoutine()
    {
        WipeAllConsole();
        GameUI.SetActive(false);

        float maxAlpha = LoseGameOverlayAlpha / 255;
        Color overlayColor = LoseGameOverlay.color;
        float timer = 0; 
        while (timer < LoseGameDuration)
        {
            float proportion = timer / LoseGameDuration;

            overlayColor.a = proportion * maxAlpha;
            LoseGameOverlay.color = overlayColor;
            
            yield return null;
            timer += Time.deltaTime;
        }
        overlayColor.a = maxAlpha;
        LoseGameOverlay.color = overlayColor;
    }

    protected IEnumerator LaunchSpecialConsole()
    {
        float timer = 0;
        while(timer < SpecialConsoleDuration)
        {
            float proportion = SpecialConsoleCurve.Evaluate(timer / SpecialConsoleDuration);
            SpecialConsole.localScale = Vector3.Lerp( Vector3.zero, Vector3.one, proportion);

            yield return null;
            timer += Time.deltaTime;
        }
        SpecialConsole.localScale = Vector3.one;
    }

    protected IEnumerator TypeSpecialCommand()
    {
        // Type Command
        int index = 0;
        float timer = KeyInterval;
        while (true)
        {

            if (timer >= KeyInterval)
            {
                timer -= KeyInterval;
                SpecialConsoleText.text += RemoveCommand[index];
                index++;
            }

            if (index >= RemoveCommand.Length)
                break;

            yield return null;
            timer += Time.deltaTime;
        }

        SpecialConsoleInstructions.SetActive(true);

        // Wait for ENTER key
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return)) break;
            yield return null;
        }
    }

    protected IEnumerator CloseSpecialConsole()
    {
        float timer = 0;
        while(timer < SpecialConsoleDuration)
        {
            float proportion = SpecialConsoleCurve.Evaluate(timer / SpecialConsoleDuration);
            proportion = 1 - proportion;
            SpecialConsole.localScale = Vector3.Lerp( Vector3.zero, Vector3.one, proportion);

            yield return null;
            timer += Time.deltaTime;
        }
        SpecialConsole.localScale = Vector3.zero;
    }

    protected IEnumerator SummaryRoutine(bool won)
    {
        SummaryScreen.gameObject.SetActive(true);
        Color imageColor = SummaryScreen.color;

        TextMeshProUGUI summaryText = won ? WinText : LoseText;
        Color textColor = summaryText.color;

        float timer = 0;
        while (timer < SummaryDuration)
        {
            float proportion = timer / SummaryDuration;

            imageColor.a = proportion;
            SummaryScreen.color = imageColor;

            textColor.a = proportion;
            summaryText.color = textColor;
            
            yield return null;
            timer += Time.deltaTime;
        }
        imageColor.a = 1;
        SummaryScreen.color = imageColor;

        textColor.a = 1;
        summaryText.color = textColor;
    }

    #endregion
    
    #endregion

    #region Capture
    protected int SetDifficulty()
    {
        int difficulty = CountDifficulty();

        Difficulty.text = $"Node Difficulty: <color=\"yellow\">{difficulty}</color>";

        return difficulty;
    }

    protected int CountDifficulty()
    {
        int difficulty = 0;
        
        if (Current.CurrentOwner != Node.Owner.Neutral && Current.CurrentOwner != PlayerOwner)
        {
            difficulty += GameManager.Inst.EnemyNodeDifficulty;
        }

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

        // Max Difficulty: 3 Surrouding Enemy Nodes + Enemy Controlled Node value - Friendly Node you came from
        difficulty = Mathf.Clamp(difficulty, 0, 3 + GameManager.Inst.EnemyNodeDifficulty - 1);

        return difficulty;
    }

    protected string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    protected List<string> SetCommandText(int difficulty)
    {
        CommandText.text = "";
        CommandTextMatched.text = "";
        commandList.Clear();
        for (int com = 0; com < (difficulty + GameManager.Inst.CaptureBaseLines) /** 4*/; com++)
        {
            AddRandomCommand();
        }
        return commandList;
    }

    protected void AddRandomCommand()
    {
        string word = "";
        for (int c = 0; c < 4; c++)
        {
            word += chars[Random.Range(0, chars.Length)];
        }
        commandList.Add(word);
        CommandText.text = $"{CommandText.text}{word} ";
    }

    protected void SetInputText(string currentInput)
    {
        InputText.text = $"<color=#0206FF>$udo ></color> {currentInput}";
    }

    protected void AddTextHistory(string currentInput)
    {
        TextHistory.text = $"{TextHistory.text}\n<color=#0206FF>$udo ></color> {currentInput}";
    }

    protected void AddTextHistoryError()
    {
        TextHistory.text = $"{TextHistory.text}\n<color=#0206FF>Error: Unrecognised command</color>";
    }

    protected void WipeAllConsole()
    {
        TextHistory.text = "";
        InputText.text = "";
        Difficulty.text = "";
        CommandText.text = "";
        CommandTextMatched.text = "";
        Instructions.SetActive(false);
    }
    #endregion

    public void LaunchSummary (int x, int y)
    {
        bool won = !(x == StartNode.Coord.x && y == StartNode.Coord.y);
        StopAllCoroutines();
        StartCoroutine(SummaryRoutine(won));
    }
}
