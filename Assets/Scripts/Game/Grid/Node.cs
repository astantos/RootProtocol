using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public enum State { Neutral, P1, P2};
    public Vector3 Dimensions;

    public Node Up;
    public Node Down;
    public Node Left;
    public Node Right;

    [Header("References")]
    public GameObject Neutral;
    public GameObject PlayerOne;
    public GameObject PlayerTwo;

    public void SetState(State state)
    {
        Neutral.SetActive(state == State.Neutral);
        PlayerOne.SetActive(state == State.P1);
        PlayerTwo.SetActive(state == State.P2);
    }
}
