using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class Matchable : Movable
{
    private int type;
    private MatchableGrid grid;
    private MatchablePool pool;
    private Cursor cursor;
    private MatchType powerup = MatchType.invalid;
    public bool IsGem
    {
        get {
            return powerup == MatchType.match5;
        }
    }
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
        pool = (MatchablePool)MatchablePool.Instance;
        grid = (MatchableGrid)MatchableGrid.Instance;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetType(int type, Sprite sprite, Color color)
    {
        this.type = type;
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
    }

    public IEnumerator Resolve(Transform collectionPoint)
    {
        // if matchable is a power up, resolve it as such
        if (powerup != MatchType.invalid)
        {
            // resolve a match4 powerup
            if (powerup == MatchType.match4)
            {
                // score everything adjacent to this
                grid.MatchAllAdjacent(this);
            }

            // resolve a cross powerup
            if (powerup == MatchType.cross)
            {
                grid.MatchRowAndColumn(this);
            }

            powerup = MatchType.invalid;
        }

        if (collectionPoint == null)
            yield break;

        // draw above others in the grid
            spriteRenderer.sortingOrder = 2;

        // move off the grid to a collection point
        yield return StartCoroutine(MoveToTransform(collectionPoint));

        // reset
        spriteRenderer.sortingOrder = 1;

        // return back to the pool
        pool.ReturnObjectToPool(this);
        yield return null;
    }

    // change the sprite of this matchable to be a powerup while retaining color and type
    public Matchable Upgrade(MatchType powerupType, Sprite powerUpSprite)
    {
        // if this is already a powerup, resolve it before upgrading
        if (powerup != MatchType.invalid)
        {
            idle = false;
            StartCoroutine(Resolve(null));
            idle = true;
        }

        if (powerupType == MatchType.match5)
        {
            type = -1;
            spriteRenderer.color = Color.white;
        }
        powerup = powerupType;
        spriteRenderer.sprite = powerUpSprite;
        return this;
    }

    // set the sorting order of the sprite renderer so it will be drawn above or below others
    public int SortingOrder
    {
        set
        {
            spriteRenderer.sortingOrder = value;
        }
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
