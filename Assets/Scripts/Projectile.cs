using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent((typeof(SpriteRenderer)))]
public class Projectile : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Randomize()
    {
        spriteRenderer.color = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
        transform.position = new Vector2(Random.Range(-4f, 4f), Random.Range(-4f, 4f));
    }
}
