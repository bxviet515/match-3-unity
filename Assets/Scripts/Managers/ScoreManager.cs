using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Text))]
public class ScoreManager : Singleton<ScoreManager>
{
    private Text scoreText;
    private MatchableGrid grid;
    private MatchablePool pool;

    [SerializeField] private Transform collectionPoint;
    private int score;
    public int Score
    {
        get
        {
            return score;
        }
    }

    protected override void Init()
    {
        scoreText = GetComponent<Text>();
        
    }

    private void Start()
    {
        grid = (MatchableGrid)MatchableGrid.Instance;
        pool = (MatchablePool)MatchablePool.Instance;
    }
    public void AddScore(int amount)
    {
        score += amount;
        scoreText.text = "Score: " + score;
    }

    public IEnumerator ResolveMatch(Match toResolve)
    {
        Matchable powerup = null;
        Matchable matchable;
        Transform target = collectionPoint;
        // if larger match is made, create a powerup
        if (toResolve.Count > 3)
        {
            powerup = pool.UpgradeMatchable(toResolve.ToBeUpgraded, toResolve.Type);
            toResolve.RemoveMatchable(powerup);
            target = powerup.transform;
            powerup.SortingOrder = 3;
        }
        
        for (int i = 0; i != toResolve.Count; ++i)
        {
            matchable = toResolve.Matchables[i];
            // remove the matchables from the grid
            grid.RemoveItemAt(matchable.position);

            // move them off to the side of the screen
            if (i == toResolve.Count - 1)
            {
                yield return StartCoroutine(matchable.Resolve(target));
            }
            else
            {
                StartCoroutine(matchable.Resolve(target));
            }
        }



        // update the player's score
        AddScore(toResolve.Count * toResolve.Count);
        yield return null;

        // if there was a powerup, reset the sorting order
        if (powerup != null)
        {
            powerup.SortingOrder = 1;
        }
    }
}
