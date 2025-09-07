using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
    public List<Squad> squads = new List<Squad>();

    public void AddSquad(Squad squad)
    {
        squads.Add(squad);
    }

    public void RemoveSquad(Squad squad)
    {
        squads.Remove(squad);
    }

    public Squad SelectSquad(int index)
    {
        if (index < 1 || index > squads.Count) return null;
        return squads[index-1];
    }
}
