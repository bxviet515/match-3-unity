using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class MatchableGrid : GridSystem<Matchable>
{
    [SerializeField] private Vector3 offScreenOffset;
    private List<Matchable> possibleMoves;
    private MatchablePool pool;
    private ScoreManager score;
    private HintIndicator hint;
    private void Start()
    {
        pool = (MatchablePool)MatchablePool.Instance;
        score = ScoreManager.Instance;
        hint = HintIndicator.Instance;
    }

    public IEnumerator PopulateGrid(bool allowMatches = false, bool initialPopulation = false)
    {
        // list of new matchables addes during population
        List<Matchable> newMatchables = new List<Matchable>();
        Matchable newMatchable;
        Vector3 onScreenPosition;

        for (int y = 0; y != Dimensions.y; ++y)
        {
            for (int x = 0; x != Dimensions.x; ++x)
                if (IsEmpty(x, y))
                {
                    // get a matchable from the pool
                    newMatchable = pool.GetRandomMatchable();


                    newMatchable.transform.position = transform.position + new Vector3(x, y) + offScreenOffset;

                    // activate the matchable
                    newMatchable.gameObject.SetActive(true);

                    // tell this matchable where it is on the grid
                    newMatchable.position = new Vector2Int(x, y);

                    // place the matchable in the grid
                    PutItemAt(newMatchable, x, y);

                    // add the new matchable to the list
                    newMatchables.Add(newMatchable);

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
                }
        }
        for (int i = 0; i != newMatchables.Count; ++i)
        {
            // position the matchable on screen
            onScreenPosition = transform.position + new Vector3(newMatchables[i].position.x, newMatchables[i].position.y);
            // move the matchable to its on screen position
            if (i == newMatchables.Count - 1)
            {
                yield return StartCoroutine(newMatchables[i].MoveToPosition(onScreenPosition));
            }
            else
                StartCoroutine(newMatchables[i].MoveToPosition(onScreenPosition));
            if (initialPopulation)
                yield return new WaitForSeconds(0.1f);
        }
    }

    private bool IsPartOfAMatch(Matchable toMatch)
    {
        int horizontalMatches = 0,
            verticalMatches = 0;

        // first look for the left and right
        horizontalMatches += CountMatchesInDirection(toMatch, Vector2Int.left);
        horizontalMatches += CountMatchesInDirection(toMatch, Vector2Int.right);

        if (horizontalMatches > 1)
        {
            return true;
        }

        // look up and down
        verticalMatches += CountMatchesInDirection(toMatch, Vector2Int.up);
        verticalMatches += CountMatchesInDirection(toMatch, Vector2Int.down);

        if (verticalMatches > 1)
        {
            return true;
        }
        return false;
    }

    // count the number of matches on the grid starting from the matchable to match moving in the direction indicated
    private int CountMatchesInDirection(Matchable toMatch, Vector2Int direction)
    {
        int matches = 0;
        Vector2Int position = toMatch.position + direction;
        while (CheckBounds(position) && !IsEmpty(position) && GetItemAt(position).Type == toMatch.Type)
        {
            ++matches;
            position += direction;
        }
        return matches;
    }
    public IEnumerator TrySwap(Matchable[] toBeSwapped)
    {
        // make a local copy of what we're swapping so Cursor doesn't overwrite
        Matchable[] copies = new Matchable[2];
        copies[0] = toBeSwapped[0];
        copies[1] = toBeSwapped[1];

        // hide the hint indicator
        hint.CancelHint();

        // yield until matchables animate Swapping
        yield return StartCoroutine(Swap(copies));

        // special cases for gems
        // if both are gems, then match everything on the grid
        if (copies[0].IsGem && copies[1].IsGem)
        {
            MatchEverything();
            yield break;
        }
        // if 1 is a gem, then mathc all matching the color of the others
        else if (copies[0].IsGem)
        {
            MatchEverythingByType(copies[0], copies[1].Type);
            yield break;
        }
        else if (copies[1].IsGem)
        {
            MatchEverythingByType(copies[1], copies[0].Type);
            yield break;
        }


        // check for a valid match
        Match[] matches = new Match[2];
        matches[0] = GetMatch(copies[0]);
        matches[1] = GetMatch(copies[1]);

        // if we make valid matches, resolve them
        if (matches[0] != null)
        {
            // resolve match
            StartCoroutine(score.ResolveMatch(matches[0]));

        }
        if (matches[1] != null)
        {
            StartCoroutine(score.ResolveMatch(matches[1]));
        }

        // if there's no match, swap them back
        if (matches[0] == null && matches[1] == null)
        {

            yield return StartCoroutine(Swap(copies));
            if (ScanForMatches())
            {
                StartCoroutine(FillAndScanGrid());
            }
        }
        else
        {
            StartCoroutine(FillAndScanGrid());
        }
    }

    private IEnumerator FillAndScanGrid()
    {
        // collapse and repopulate the grid
        CollapseGird();
        yield return StartCoroutine(PopulateGrid(true));

        // scan grid for chain reactions
        if (ScanForMatches())
        {
            StartCoroutine(FillAndScanGrid());
        }
        // if no chain reactions, grid is idle, so check for possible moves
        else
        {
            CheckPossibleMoves();
        }
    }

    public void CheckPossibleMoves()
    {
        if (ScanForMoves() == 0)
        {
            // no moves!
            GameManager.Instance.NoMoreMoves();
        }
        else
        {
            // offer a hint
            //hint.EnableHintButton();
            hint.StartAutoHint(possibleMoves[Random.Range(0, possibleMoves.Count)].transform);
        }
    }
    private Match GetMatch(Matchable toMatch)
    {
        Match match = new Match(toMatch);
        Match horizontalMatch, verticalMatch;

        // first get horizontal matches to the left and right
        horizontalMatch = GetMatchesInDirection(match, toMatch, Vector2Int.left);
        horizontalMatch.Merge(GetMatchesInDirection(match, toMatch, Vector2Int.right));

        horizontalMatch.orientation = Orientation.horizontal;
        if (horizontalMatch.Count > 1)
        {
            match.Merge(horizontalMatch);
            // scan for vertical branches
            GetBranches(match, horizontalMatch, Orientation.vertical);
        }

        // then get vertical matches up and down
        verticalMatch = GetMatchesInDirection(match, toMatch, Vector2Int.up);
        verticalMatch.Merge(GetMatchesInDirection(match, toMatch, Vector2Int.down));

        verticalMatch.orientation = Orientation.vertical;
        if (verticalMatch.Count > 1)
        {
            match.Merge(verticalMatch);
            // scan for horizontal branches
            GetBranches(match, verticalMatch, Orientation.horizontal);
        }

        if (match.Count == 1)
        {
            return null;
        }
        return match;
    }

    private void GetBranches(Match tree, Match branchToSearch, Orientation perpendicular)
    {
        Match branch;
        foreach (Matchable matchable in branchToSearch.Matchables)
        {
            branch = GetMatchesInDirection(tree, matchable, perpendicular == Orientation.horizontal ? Vector2Int.left : Vector2Int.down);
            branch.Merge(GetMatchesInDirection(tree, matchable, perpendicular == Orientation.horizontal ? Vector2Int.right : Vector2Int.up));

            branch.orientation = perpendicular;
            if (branch.Count > 1)
            {
                tree.Merge(branch);
                GetBranches(tree, branch, perpendicular == Orientation.horizontal ? Orientation.vertical : Orientation.horizontal);
            }
        }
    }

    // add each matching matchable in the direction to a match and return it
    private Match GetMatchesInDirection(Match tree, Matchable toMatch, Vector2Int direction)
    {
        Match match = new Match();
        Vector2Int position = toMatch.position + direction;
        Matchable next;
        while (CheckBounds(position) && !IsEmpty(position))
        {
            next = GetItemAt(position);
            if (next.Type == toMatch.Type && next.Idle)
            {
                if (!tree.Contains(next))
                    match.AddMatchable(next);
                else match.AddUnlisted();

                position += direction;
            }
            else break;

        }
        return match;
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

    private void CollapseGird()
    {
        /*
        * Go through each column left to right,
        * search from bottom up to find an empty space,
        * then look above the empty space, and up through the rest of the column,
        * until find a non empty space.
        * Move the matchable at the non empty space into the empty space,
        * then continue looking for empty spaces
        */
        for (int x = 0; x != Dimensions.x; ++x)
        {
            for (int yEmpty = 0; yEmpty != Dimensions.y - 1; ++yEmpty)
            {
                if (IsEmpty(x, yEmpty))
                {
                    for (int yNotEmpty = yEmpty + 1; yNotEmpty != Dimensions.y; ++yNotEmpty)
                    {
                        if (!IsEmpty(x, yNotEmpty) && GetItemAt(x, yNotEmpty).Idle)
                        {
                            MoveMatchableToPosition(GetItemAt(x, yNotEmpty), x, yEmpty);
                            break;
                        }
                    }
                }
            }
        }
    }

    private void MoveMatchableToPosition(Matchable toMove, int x, int y)
    {
        // move the matchable to its new position in the grid
        MoveItemTo(toMove.position, new Vector2Int(x, y));

        // update the matchable's internal grid position
        toMove.position = new Vector2Int(x, y);

        // start animation to move it on screen
        StartCoroutine(toMove.MoveToPosition(transform.position + new Vector3(x, y)));
    }

    // scan the grid for any matches and resolve them
    private bool ScanForMatches()
    {
        bool madeAMatch = false;
        Matchable toMatch;
        Match match;

        // iterate through the grid, looking for non-empty and idle matchables
        for (int y = 0; y != Dimensions.y; ++y)
        {
            for (int x = 0; x != Dimensions.x; ++x)
            {
                if (!IsEmpty(x, y))
                {
                    toMatch = GetItemAt(x, y);
                    if (!toMatch.Idle)
                        continue;

                    // try match and resolve
                    match = GetMatch(toMatch);
                    if (match != null)
                    {
                        madeAMatch = true;
                        StartCoroutine(score.ResolveMatch(match));
                    }
                }
            }
        }
        return madeAMatch;
    }

    // make a match of all matchables adjacent to this power up on the grid and resolve it
    public void MatchAllAdjacent(Matchable powerup)
    {
        Match allAdjacent = new Match();
        for (int y = powerup.position.y - 1; y != powerup.position.y + 2; ++y)
        {
            for (int x = powerup.position.x - 1; x != powerup.position.x + 2; ++x)
            {
                if (CheckBounds(x, y) && !IsEmpty(x, y) && GetItemAt(x, y).Idle)
                {
                    allAdjacent.AddMatchable(GetItemAt(x, y));
                }
            }
        }
        StartCoroutine(score.ResolveMatch(allAdjacent, MatchType.match4));
    }

    // make a match of everything in the row and column that contains the powerup and resolve it
    public void MatchRowAndColumn(Matchable powerup)
    {
        Match rowAndColumn = new Match();
        for (int y = 0; y != Dimensions.y; ++y)
            if (CheckBounds(powerup.position.x, y) && !IsEmpty(powerup.position.x, y) && GetItemAt(powerup.position.x, y).Idle)
                rowAndColumn.AddMatchable(GetItemAt(powerup.position.x, y));

        for (int x = 0; x != Dimensions.x; ++x)
            if (CheckBounds(x, powerup.position.y) && !IsEmpty(x, powerup.position.y) && GetItemAt(x, powerup.position.y).Idle)
                rowAndColumn.AddMatchable(GetItemAt(x, powerup.position.y));

        StartCoroutine(score.ResolveMatch(rowAndColumn, MatchType.cross));
    }


    // make everything on the grid with a specific type and resolve it
    public void MatchEverythingByType(Matchable gem, int type)
    {
        Match everythingByType = new Match(gem);
        for (int y = 0; y != Dimensions.y; ++y)
            for (int x = 0; x != Dimensions.x; ++x)
                if (CheckBounds(x, y) && !IsEmpty(x, y) && GetItemAt(x, y).Idle && GetItemAt(x, y).Type == type)
                    everythingByType.AddMatchable(GetItemAt(x, y));

        StartCoroutine(score.ResolveMatch(everythingByType, MatchType.match5));
        StartCoroutine(FillAndScanGrid());
    }

    // make everything on the grid and resolve it
    public void MatchEverything()
    {
        Match everything = new Match();
        for (int y = 0; y != Dimensions.y; ++y)
            for (int x = 0; x != Dimensions.x; ++x)
                if (CheckBounds(x, y) && !IsEmpty(x, y) && GetItemAt(x, y).Idle)
                    everything.AddMatchable(GetItemAt(x, y));

        StartCoroutine(score.ResolveMatch(everything, MatchType.match5));
        StartCoroutine(FillAndScanGrid());
    }

    // scan for all possible moves on the grid
    private int ScanForMoves()
    {
        possibleMoves = new List<Matchable>();

        // scan through the entire grid
        for (int y = 0; y != Dimensions.y; ++y)
        {
            for (int x = 0; x != Dimensions.x; ++x)
            {
                if (CheckBounds(x, y) && !IsEmpty(x, y) && CanMove(GetItemAt(x, y)))
                    possibleMoves.Add(GetItemAt(x, y));
            }
        }
        return possibleMoves.Count;
    }

    private bool CanMove(Matchable toCheck)
    {
        if (CanMove(toCheck, Vector2Int.up)
                || CanMove(toCheck, Vector2Int.down)
                || CanMove(toCheck, Vector2Int.left)
                || CanMove(toCheck, Vector2Int.right))
        {
            return true;
        }
        if (toCheck.IsGem)
        {
            // if it's a gem, it can move in any direction
            return true;
        }

        return false;
    }

    private bool CanMove(Matchable toCheck, Vector2Int direction)
    {
        Vector2Int position1 = toCheck.position + direction ;
        Vector2Int position2 = toCheck.position + direction * 2;

        if (IsAPotentialMatch(toCheck, position1, position2))
        {
            return true;
        }

        // what is the clockwise direction?
        Vector2Int cw = new Vector2Int(direction.y, -direction.x),
                    ccw = new Vector2Int(-direction.y, direction.x);


        // Look diagonally clockwise
        position1 = toCheck.position + direction + cw;
        position2 = toCheck.position + direction + cw * 2;
        if (IsAPotentialMatch(toCheck, position1, position2))
        {
            return true;
        }

        // Look diagonally both ways
        position2 = toCheck.position + direction * ccw;
        if (IsAPotentialMatch(toCheck, position1, position2))
        {
            return true;
        }

        // Look diagonally counter clockwise
        position1 = toCheck.position + direction + ccw * 2;
        if (IsAPotentialMatch(toCheck, position1, position2))
        {
            return true;
        }


        return false;
    }

    // Will these matchables form a potential match?
    private bool IsAPotentialMatch(Matchable toCompare, Vector2Int position1, Vector2Int position2)
    {
        if (CheckBounds(position1) && CheckBounds(position2)
            && !IsEmpty(position1) && !IsEmpty(position2)
            && GetItemAt(position1).Type == toCompare.Type
            && GetItemAt(position2).Type == toCompare.Type
            && GetItemAt(position1).Idle && GetItemAt(position2).Idle)
        {
            return true;
        }
        return false;
    }

    // Show a hint for the player
    public void ShowHint()
    {
        ScanForMoves();
        hint.IndicateHint(possibleMoves[Random.Range(0, possibleMoves.Count)].transform);
    }
}
