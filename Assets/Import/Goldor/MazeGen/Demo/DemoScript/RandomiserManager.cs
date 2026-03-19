using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomiserManager : ScriptableObject
{
    public List<Randomiser> randomisers = new List<Randomiser>();

    public void AddRandomiser(Randomiser randomiser)
    {
        randomisers.Add(randomiser);
    }

    public void RemoveRandomiser(Randomiser randomiser)
    {
        randomisers.Remove(randomiser);
    }

    public void RandomiseAll()
    {
        foreach (Randomiser randomiser in randomisers)
        {
            randomiser.Randomise();
        }
    }
}