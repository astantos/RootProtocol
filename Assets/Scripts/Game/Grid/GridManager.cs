using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/*********************/
/* SERVER AND CLIENT */
/*********************/
public class GridManager : NetworkBehaviour
{
    [Serializable]
    public struct Dimensions
    {
        public int Width, Height;
    }

    public Dimensions GridDimensions;
    public Node NodePrefab;
    public float Margin;
    [Space]
    public Node[] NodeArray;

    public Node[][] Grid { get; protected set; }

    #region
    public void Setup()
    {
        SetupGrid();
        SetupNodes();
    }

    protected void SetupGrid()
    {
        int count = 0;
        Grid = new Node[GridDimensions.Width][];
        for (int x = 0; x < Grid.Length; x++)
        {
            Grid[x] = new Node[GridDimensions.Height];
            for (int y = 0; y < Grid[x].Length; y++)
            {
                if (count >= NodeArray.Length)
                {
                    Debug.LogError("[CRITICAL ERROR] Not enough Nodes have been provided to create the Grid");
                    return;
                }

                Grid[x][y] = NodeArray[count];
                Grid[x][y].gameObject.name = $"Node{x}x{y}";
                Grid[x][y].transform.position = new Vector3
                (
                    x * (Grid[x][y].Dimensions.x + Margin),
                    y * (Grid[x][y].Dimensions.y + Margin),
                    0
                );
                Grid[x][y].SetState(Node.State.Neutral);
                count++;
            }
        }
    }

    protected void SetupNodes()
    {
        for (int x = 0; x < Grid.Length; x++)
        {
            for (int y = 0; y < Grid[x].Length; y++)
            {
                // Direction UP
                if (y < Grid[x].Length - 1) Grid[x][y].Up = Grid[x][y + 1];

                // Direction DOWN
                if (y > 0) Grid[x][y].Down = Grid[x][y - 1];

                // Direction LEFT
                if (x > 0) Grid[x][y].Left = Grid[x - 1][y];

                // Direction RIGHT
                if (x < Grid.Length - 1) Grid[x][y].Right = Grid[x + 1][y];
            }
        }
    }
    #endregion

    #region Network Callbacks
    public override void OnStartClient()
    {
        base.OnStartClient();
        Setup();
    }
    #endregion

    #region RPCs
    [ClientRpc]
    public void SetNodeState(int x, int y, int state)
    {
        Grid[x][y].SetState((Node.State)state);
    }

    #endregion
}
