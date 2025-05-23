using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class Matchable : Movable
{
    private int type;
    private Cursor cursor;
    public int Type
    {
        get
        {
            return type;
        }
    }
    private SpriteRenderer spriteRenderer;
    // where is this matchable in the grid?
    public Vector2Int position;
    private void Awake()
    {
        cursor = Cursor.Instance;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetType(int type, Sprite sprite, Color color)
    {
        this.type = type;
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
    }
    private void OnMouseDown()
    {
        cursor.SelectFirst(this);
    }

    private void OnMouseUp()
    {
        cursor.SelectFirst(null);
    }
    private void OnMouseEnter()
    {
        cursor.SelectSecond(this);
    }

    public override string ToString()
    {
        return gameObject.name;
    }
}
