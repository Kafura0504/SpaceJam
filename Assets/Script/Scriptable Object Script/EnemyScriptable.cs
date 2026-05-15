using System;
using UnityEngine;

[CreateAssetMenu(fileName ="New Enemy", menuName ="MyAsset/Enemy")]
public class EnemyScriptable : ScriptableObject
{
    public String name;
    public int health;
    public int attack;
    public int exp;
}
