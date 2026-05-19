using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameFunction : MonoBehaviour
{   
    bool ispause = false;
    public GameObject pausemenu;
    public GameObject GameMenu;
    public event Action onPause;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && ispause)
        {
            unpause();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && !ispause)
        {
            paused();
        }
    }

    public void paused()
    {
        Time.timeScale = 0f;
        ispause = true;
        onPause?.Invoke();
        pausemenu.SetActive(true);
        GameMenu.SetActive(false);

    }
    public void unpause()
    {
        Time.timeScale = 1f;
        ispause = false;
        pausemenu.SetActive(false);
        GameMenu.SetActive(true);
    }

    public void backtoMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

}
