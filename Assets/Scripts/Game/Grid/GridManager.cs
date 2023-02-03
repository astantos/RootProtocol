using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/***************/
/* SERVER ONLY */
/***************/
public class GridManager : MonoBehaviour
{
    [Serializable]
    public struct Dimensions
    {
        public int Width, Height;
    }

    public Dimensions GridDimensions;
    public Node NodePrefab;

    public float Margin;

    public Node[][] Grid { get; protected set; }

    public void Spawn()
    {
        Grid = new Node[GridDimensions.Width][];
        for (int x = 0; x < Grid.Length; x++)
        {
            Grid[x] = new Node[GridDimensions.Height];
            for (int y = 0; y < Grid[x].Length; y++)
            {
                Grid[x][y] = GameObject.Instantiate(NodePrefab);
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int x = 0; x < Grid.Length; x++)
            {
                for (int y = 0; y < Grid[x].Length; y++)
                {
                    Debug.Log($"{Grid[x][y].gameObject.name}");
                }
            }
        }
    }
}
