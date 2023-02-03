using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public enum State { Neutral, P1, P2};
    [Serializable]
    public struct Direction
    {
        public Node Node;
        public ParticleSystem particles;
    }
    public Vector3 Dimensions;

    public Direction Up;
    public Direction Down;
    public Direction Left;
    public Direction Right;

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
