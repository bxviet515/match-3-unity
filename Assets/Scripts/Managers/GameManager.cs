using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private Vector2Int dimensions;
    [SerializeField] private Text gridOutput;
    private MatchablePool pool;
    private MatchableGrid grid;
    private void Start()
    {
        pool = (MatchablePool)MatchablePool.Instance;
        grid = (MatchableGrid)MatchableGrid.Instance;

        StartCoroutine(Setup());
    }

    private IEnumerator Setup()
    {
        // it's a good idea to put a loading screen here

        // pool the matchables
        pool.PoolObjects(dimensions.x * dimensions.y);
        // create the grid
        grid.InitializeGrid(dimensions);


        yield return null;
        StartCoroutine(grid.PopulateGrid(false, true));

        // then remove the loading screen down here

        // Check for gridlock and offer the player a hint if they need it
        grid.CheckPossibleMoves();
    }

    public void NoMoreMoves()
    {
        
    }
}
