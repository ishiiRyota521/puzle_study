using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextQueue
{
    private enum Constants
    {
        PUYO_TYPE_MAX=4,
        PUYO_NEXT_HISTORISE=2,
    };

    Queue<Vector2Int> _nexts = new();

    Vector2Int CreateNext()
    {
        return new Vector2Int(
            Random.Range(0, (int)Constants.PUYO_TYPE_MAX) + 1,
            Random.Range(0, (int)Constants.PUYO_TYPE_MAX) + 1);
    }

    public void Intialize()
    {
        for(int t = 0; t < (int)Constants.PUYO_NEXT_HISTORISE; t++)
        {
            _nexts.Enqueue(CreateNext());
        }
    }

    public Vector2Int Update()
    {
        Vector2Int next = _nexts.Dequeue();
        _nexts.Enqueue(CreateNext());

        return next;
    }

   
}
