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

    public void Setup()
    {
        SetupGrid();
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
                count++;
            }
        }
    }


    #region Network Callbacks
    public override void OnStartClient()
    {
        base.OnStartClient();
        Setup();
    }
    #endregion
}
