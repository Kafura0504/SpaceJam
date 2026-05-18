using UnityEngine;
using System;

public class Exp : MonoBehaviour
{
    public int level = 1;

    public float exp = 0;

    public float expNeeded = 10;

    private float lastEXPNeed = 10;
    private float expmult = 1f;

    public event Action OnLevelUp;

    public void AddEXP(float amount)
    {
        exp += amount * expmult;

        while (exp >= expNeeded)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        exp -= expNeeded;

        level++;
        if (level % 10 == 0)
        {
            expmult += 0.5f;
        }

        UpdateEXPReq();

        OnLevelUp?.Invoke();

        Debug.Log("Level Up -> " + level);
    }

    void UpdateEXPReq()
    {
        expNeeded += 10;
    }
}
