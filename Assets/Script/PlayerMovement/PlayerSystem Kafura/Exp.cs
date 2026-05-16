using UnityEngine;
using System;

public class Exp : MonoBehaviour
{
    public int level = 1;

    public float exp = 0;

    public float expNeeded = 10;

    private float lastEXPNeed = 10;

    public event Action OnLevelUp;

    public void AddEXP(float amount)
    {
        exp += amount;

        while (exp >= expNeeded)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        exp -= expNeeded;

        level++;

        UpdateEXPReq();

        OnLevelUp?.Invoke();

        Debug.Log("Level Up -> " + level);
    }

    void UpdateEXPReq()
    {
        float previous = expNeeded;

        expNeeded += lastEXPNeed;

        lastEXPNeed = previous;
    }
}
