using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class speedui : MonoBehaviour
{
    public FlyFree fly;
    public Text text;
    float speed;
    // Start is called before the first frame update
   
    void Update()
    {
        if (speed!=fly.moveSpeed)
        {
            speed=fly.moveSpeed;
            text.text = "ÒÆ¶¯ËÙ¶È "+ speed.ToString()+" m/s";

        }
    }
}
