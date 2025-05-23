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

                // tell this matchable where it is on the grid
                newMatchable.position = new Vector2Int(x, y);

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

    public IEnumerator TrySwap(Matchable[] toBeSwapped)
    {
        // make a local copy of what we're swapping so Cursor doesn't overwrite
        Matchable[] copies = new Matchable[2];
        copies[0] = toBeSwapped[0];
        copies[1] = toBeSwapped[1];
        // yield until matchables animate Swapping
        yield return StartCoroutine(Swap(copies));
        // check for a valid match
        if (SwapWasValid())
        {
            // resolve match
        }
        else
        {
            // if there's no match, swap them back
            StartCoroutine(Swap(copies));
        }

        

    }

    private bool SwapWasValid()
    {
        return true;
    }
    private IEnumerator Swap(Matchable[] toBeSwapped)
    {
        // swap them in the grid data structure
        SwapItemsAt(toBeSwapped[0].position, toBeSwapped[1].position);

        // tell the matchables their new positions
        Vector2Int temp = toBeSwapped[0].position;
        toBeSwapped[0].position = toBeSwapped[1].position;
        toBeSwapped[1].position = temp;

        // get the world positions of both
        Vector3[] worldPosition = new Vector3[2];
        worldPosition[0] = toBeSwapped[0].transform.position;
        worldPosition[1] = toBeSwapped[1].transform.position;

        // move them to their new positions on screen
        StartCoroutine(toBeSwapped[0].MoveToPosition(worldPosition[1]));
        yield return StartCoroutine(toBeSwapped[1].MoveToPosition(worldPosition[0]));
    }
}
