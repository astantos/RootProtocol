using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/***************/
/* SERVER ONLY */
/***************/
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

    public Node[][] Grid()
    {
        if (_grid == null)
        {
            _grid = new Node[GridDimensions.Width][];
            for (int x = 0; x < _grid[x].Length; x++)
            {
                _grid[x] = new Node[GridDimensions.Height];
            }
        }
        return _grid;
    }
    protected Node[][] _grid;


    public void Spawn()
    {
        InitializeGrid();
        SetupNodes();
    }

    protected void InitializeGrid()
    {
        for (int x = 0; x < Grid.Length; x++)
        {
            for (int y = 0; y < Grid[x].Length; y++)
            {
                Grid[x][y] = GameObject.Instantiate(NodePrefab);
                Grid[x][y].Coords = new Vector2(x, y);
                NetworkServer.Spawn(Grid[x][y].gameObject);

                Grid[x][y].transform.position = new Vector3
                (
                    x * (Grid[x][y].Dimensions.x + Margin),
                    y * (Grid[x][y].Dimensions.y + Margin),
                    0
                );
            }
        }
    }

    protected void SetupNodes()
    {
        Debug.LogWarning($"Grid: {Grid.Length}");
        for (int x = 0; x < Grid.Length; x++)
        {
            for (int y = 0; y < Grid[x].Length; y++)
            {
                // Direction UP
                if (y < Grid[x].Length - 1) 
                    Grid[x][y].UpGO = Grid[x][y + 1].gameObject;
                
                // Direction DOWN 
                if (y > 0) 
                    Grid[x][y].DownGO = Grid[x][y - 1].gameObject;

                // Direction LEFT
                if (x > 0)
                    Grid[x][y].LeftGO =  Grid[x - 1][y].gameObject;

                // Direction RIGHT
                if (x < Grid.Length - 1)
                    Grid[x][y].RightGO = Grid[x + 1][y].gameObject;
            }
        }
    }

    #region Network Callbacks
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            SetupNodes();
        }
    }
    #endregion
}
