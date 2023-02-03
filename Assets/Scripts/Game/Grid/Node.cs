using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MainModule = UnityEngine.ParticleSystem.MainModule;

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

    [Header("Colors")]
    public Color NeutralColor;
    public Color PlayerOneColor;
    public Color PlayerTwoColor;

    [Header("References")]
    public GameObject Neutral;
    public GameObject PlayerOne;
    public GameObject PlayerTwo;

    public void SetState(State state)
    {
        Neutral.SetActive(state == State.Neutral);
        PlayerOne.SetActive(state == State.P1);
        PlayerTwo.SetActive(state == State.P2);
        UpdateColor(state);
    }

    protected void UpdateColor(State state)
    {
        Color color;
        switch (state)
        {
            case State.P1: color = PlayerOneColor; break;
            case State.P2: color = PlayerTwoColor; break;
            default: color = NeutralColor; break;
        }

        MainModule upMain = Up.particles.main;
        upMain.startColor = color;

        MainModule downMain = Down.particles.main;
        downMain.startColor = color;

        MainModule leftMain = Left.particles.main;
        leftMain.startColor = color;
        
        MainModule rightMain = Right.particles.main;
        rightMain.startColor = color;
    }
}
