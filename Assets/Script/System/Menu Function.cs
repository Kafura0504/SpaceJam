using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuFunction : MonoBehaviour
{
    public void pickRandomLevel (){
        int rand = UnityEngine.Random.Range(0,3);
        switch (rand)
        {
            case 0:
            SceneManager.LoadScene("cryogla");
            break;

            case 1:
            SceneManager.LoadScene("noir331");
            break;

            case 2:
            SceneManager.LoadScene("turmos");
            break;
        }
    }
}