using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineItem : MonoBehaviour
{
    public float LifeTime;

    protected Transform trans;

    private void Awake()
    {
        trans = GetComponent<Transform>();
    }

    public void SetPosition(Vector3 position)
    {
        trans.position = position;
    }

    public void SetScale(Vector3 scale)
    {
        trans.localScale = scale;
    }
}
