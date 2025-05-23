using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchableGrid : GridSystem<Matchable>
{
    [SerializeField] private Vector3 offScreenOffset;
    private MatchablePool pool;
    private void Start()
    {
        pool = (MatchablePool)MatchablePool.Instance;
    }
    public IEnumerator PopulateGrid(bool allowMatches = false)
    {
        Matchable newMatchable;
        Vector3 onScreenPosition;
       
        for (int y = 0; y != Dimensions.y; ++y)
        {
            for (int x = 0; x != Dimensions.x; ++x)
            {
                // get a matchable from the pool
                newMatchable = pool.GetRandomMatchable();

                // position the matchable on screen
                //newMatchable.transform.position = transform.position + new Vector3(x, y);
                onScreenPosition = transform.position + new Vector3(x, y);
                newMatchable.transform.position = onScreenPosition + offScreenOffset;

                // activate the matchable
                newMatchable.gameObject.SetActive(true);
                Debug.Log("x: " + x + ", y: " + y);
                // place the matchable in the grid
                PutItemAt(newMatchable, x, y);
                int type = newMatchable.Type;
                while (!allowMatches && IsPartOfAMatch(newMatchable))
                {
                    // change the matchable's type until it isn't a match anymore
                    if (pool.NextType(newMatchable) == type)
                    {
                        Debug.LogWarning("Failed to find a matchable type that didn't match at (" + x + ", " + y + ")");
                        Debug.Break();
                        break;
                    }

                }
                // move the matchable to its on screen position
                StartCoroutine(newMatchable.MoveToPosition(onScreenPosition));
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    
    private bool IsPartOfAMatch(Matchable matchable)
    {
        return false;
    }
}
