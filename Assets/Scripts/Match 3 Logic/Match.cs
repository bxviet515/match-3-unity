using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum Orientation
{
    none,
    horizontal,
    vertical,
    both
}
public class Match
{
    public Orientation orientation = Orientation.none;
    private int unlisted = 0;
    private List<Matchable> matchables;
    public List<Matchable> Matchables
    {
        get
        {
            return matchables;
        }
    }
    public int Count {
        get{
            return matchables.Count + unlisted;
        }
    }
    public bool Contains(Matchable toCompare)
    {
        return matchables.Contains(toCompare);
    }
    public Match()
    {
        matchables = new List<Matchable>(5);
    }

    public Match(Matchable original) : this()
    {
        AddMatchable(original);
    }

    public void AddMatchable(Matchable toAdd)
    {
        matchables.Add(toAdd);
    }

    public void AddUnlisted()
    {
        ++unlisted;
    }

    public void Merge(Match toMerge)
    {
        matchables.AddRange(toMerge.matchables);
    }
    public override string ToString()
    {
        string s = "Match of type " + matchables[0].Type + " : ";
        foreach (Matchable m in matchables)
        {
            s += "(" + m.position.x + ", " + m.position.y + ") ";
        }
        return s;
    }
}
