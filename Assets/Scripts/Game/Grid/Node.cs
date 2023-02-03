using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MainModule = UnityEngine.ParticleSystem.MainModule;

public class Node : MonoBehaviour
{
    public enum Owner { Neutral, P1, P2 };
    [Serializable]
    public struct Direction
    {
        public Node Node;
        public ParticleSystem particles;
    }
    public Vector3 Dimensions;

    [Serializable]
    public struct Coords { public int x, y; }
    public Coords Coord;

    [Space]
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

    public Owner CurrentOwner { get; protected set; }

    private void Start()
    {
        MainModule p1main = P1Selected.main;
        p1main.startColor = PlayerOneColor;
        
        MainModule p2main = P2Selected.main;
        p2main.startColor = PlayerTwoColor;
    }

    public void SetState(Owner owner)
    {
        Neutral.SetActive(owner == Owner.Neutral);
        PlayerOne.SetActive(owner == Owner.P1);
        PlayerTwo.SetActive(owner == Owner.P2);
        CurrentOwner = owner;
        UpdateColor(owner);
    }

    protected void UpdateColor(Owner state)
    {
        Color color;
        switch (state)
        {
            case Owner.P1: color = PlayerOneColor; break;
            case Owner.P2: color = PlayerTwoColor; break;
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

    public void SetSelected(Owner state, bool selected)
    {
        if (state == Owner.P1)
        {
            if (selected) P1Selected.Play();
            else P1Selected.Stop();
        }
        else if (state == Owner.P2)
        {
            if (selected) P2Selected.Play();
            else P2Selected.Stop();
        }
    }
}
