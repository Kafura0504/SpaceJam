using UnityEngine;

public class LevelUpManager : MonoBehaviour
{   
    public BuffData[] allBuffs;

    public BuffData[] currentChoices = new BuffData[3];

    public void GenerateChoices()
    {
        for(int i = 0; i < 3; i++)
        {
            int rand = Random.Range(0, allBuffs.Length);

            currentChoices[i] = allBuffs[rand];
        }
    }
}
