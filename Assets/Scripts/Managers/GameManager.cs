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
    private Cursor cursor;
    private AudioMixer audioMixer;
    [SerializeField] private Fader loadingScreen;
    private void Start()
    {
        pool = (MatchablePool)MatchablePool.Instance;
        grid = (MatchableGrid)MatchableGrid.Instance;
        cursor = Cursor.Instance;
        audioMixer = AudioMixer.Instance;
        StartCoroutine(Setup());
    }

    private IEnumerator Setup()
    {
        // disable user input
        cursor.enabled = false;

        // unhide loading screen
        loadingScreen.Hide(false);

        // pool the matchables
        pool.PoolObjects(dimensions.x * dimensions.y * 2);

        // create the grid
        grid.InitializeGrid(dimensions);

        // fade out loading screen
        StartCoroutine(loadingScreen.Fade(0));

        // start background music
        audioMixer.PlayMusic();

        // populate the grid
        yield return StartCoroutine(grid.PopulateGrid(false, true));

        // Check for gridlock and offer the player a hint if they need it
        grid.CheckPossibleMoves();

        // enable user input
        cursor.enabled = true;
    }

    public void NoMoreMoves()
    {
        grid.MatchEverything();
    }
}
