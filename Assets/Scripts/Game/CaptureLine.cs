using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureLine : MonoBehaviour
{
    public float ZOffset;
    public AnimationCurve MoveCurve;
    public AnimationCurve ScaleCurve;
    public float MinMoveSpeed;
    public float MaxMoveSpeed;
    public float MinEmitSpeed;
    public float MaxEmitSpeed;
    public float StopSpeed;

    protected float CaptureProportion;
    protected float CurMoveSpeed;
    protected float CurEmitSpeed;

    public List<LineItem> ItemPrefabs;

    protected List<LineItem> itemPool;
    protected List<LineItem> activePool;

    protected Coroutine activeRoutine;

    private void Start()
    {
        itemPool = new List<LineItem>();
        activePool = new List<LineItem>();
    }

    public void StartDataLine(int startX, int startY, int x, int y)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(DataLineRoutine(startX, startY, x, y));
    }

    public void StopDataLine()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(StopDataLineRoutine());
    }

    public void UpdateCaptureProportion(float proportion)
    {
        CaptureProportion = proportion;
        CurEmitSpeed = Mathf.Lerp(MinEmitSpeed, MaxEmitSpeed, CaptureProportion);
        CurMoveSpeed = Mathf.Lerp(MinMoveSpeed, MaxMoveSpeed, CaptureProportion);
    }

    protected IEnumerator DataLineRoutine(int startX, int startY, int x, int y)
    {
        Vector3 startPos = GridManager.Inst.GetNode(startX, startY).transform.position;
        startPos = new Vector3(startPos.x, startPos.y, startPos.z + ZOffset);

        Vector3 endPos = GridManager.Inst.GetNode(x, y).transform.position;
        endPos = new Vector3(endPos.x, endPos.y, endPos.z + ZOffset);

        CurEmitSpeed = MinEmitSpeed;
        CurMoveSpeed = MinMoveSpeed;

        float emitTimer = CurEmitSpeed;
        while(true)
        {
            // Check For New Emission
            if (emitTimer >= CurEmitSpeed)
            {
                emitTimer -= CurEmitSpeed;

                LineItem item = null;
                if(itemPool.Count > 0)
                {
                    item = itemPool[Random.Range(0, itemPool.Count)];
                    itemPool.Remove(item);
                }
                else
                {
                    item = GameObject.Instantiate(ItemPrefabs[Random.Range(0, ItemPrefabs.Count)]);
                }
                item.LifeTime = 0;
                activePool.Add(item);
            }

            // Update all ActiveItems
            for (int index = activePool.Count - 1; index >= 0; index--)
            {
                LineItem item = activePool[index];
                float proportion = item.LifeTime / CurMoveSpeed;
                float moveProp = MoveCurve.Evaluate(proportion);
                float scaleProp = ScaleCurve.Evaluate(proportion);

                if (proportion < 1)
                {
                    Vector3 newPos = Vector3.Lerp(startPos, endPos, moveProp);
                    Vector3 scale = Vector3.Lerp(Vector3.zero, Vector3.one, scaleProp);

                    item.SetPosition(newPos);
                    item.SetScale(scale);
                }
                else
                {
                    activePool.Remove(item);
                    itemPool.Add(item);
                    item.LifeTime = 0;
                }
                
            }
            yield return null;
            emitTimer += Time.deltaTime;

            // Update Lifetime of all LineItems
            for (int index = 0; index < activePool.Count; index++)
            {
                activePool[index].LifeTime += Time.deltaTime;
            }
        }
    }

    public IEnumerator StopDataLineRoutine()
    {
        float timer = 0;
        while (timer < StopSpeed)
        {
            for (int index = 0; index < activePool.Count; index++)
            {
                float proportion = 1 - (timer / StopSpeed);
                activePool[index].SetScale(Vector3.one * proportion);
            }

            yield return null;
            timer += Time.deltaTime;
        }

        for (int index = activePool.Count - 1; index >= 0; index--)
        {
            LineItem temp = activePool[index];
            temp.SetScale(Vector3.zero);
            activePool.Remove(temp);
            ItemPrefabs.Add(temp);
        }

        activeRoutine = null;
    }
}
