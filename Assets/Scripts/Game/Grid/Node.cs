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
    public ParticleSystem P1Selected;
    public ParticleSystem P2Selected;

    [Header("Colors")]
    public Color NeutralColor;
    public Color PlayerOneColor;
    public Color PlayerTwoColor;

    [Header("References")]
    public GameObject Neutral;
    public GameObject PlayerOne;
    public GameObject PlayerTwo;

    private void Start()
    {
        MainModule p1main = P1Selected.main;
        p1main.startColor = PlayerOneColor;
        
        MainModule p2main = P2Selected.main;
        p2main.startColor = PlayerTwoColor;
    }

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

    protected void SetSelected(State state, bool selected)
    {
        if (state == State.P1)
        {
            if (selected) P1Selected.Play();
            else P1Selected.Stop();
        }
        else if (state == State.P2)
        {
            if (selected) P2Selected.Play();
            else P2Selected.Stop();
        }
    }
}
