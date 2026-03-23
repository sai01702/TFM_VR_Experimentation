using System;
using UnityEngine;
using MazeGen;

[CreateAssetMenu(fileName = "Obstacle", menuName = "MazeGen/DemoScript/MazeCustomPart/Obstacle")]
public class Obstacle : ScriptableObject
{
    public MazePart.PrimordialPart primordialShape;
    public GameObject gameObject;
    [Range(0,1)] public double prob;
    [NonSerialized] public int typeID;
}
