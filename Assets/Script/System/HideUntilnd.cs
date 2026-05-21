using Unity.VisualScripting;
using UnityEngine;

public class HideUntilnd : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(GameManager.Instance.isfirstLoad){
           gameObject.SetActive(false); 
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}
