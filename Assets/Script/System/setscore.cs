using TMPro;
using UnityEngine;

public class setscore : MonoBehaviour
{

    public HealthAndScore Score;
    public TextMeshProUGUI text;

    void OnEnable()
    {
        text.SetText("SCORE: "+Score.score);
    }
}
