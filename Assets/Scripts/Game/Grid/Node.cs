using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right };
    public Vector3 Dimensions;

    public Node Up;
    public Node Down;
    public Node Left;
    public Node Right;
}
