using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class observeTrakerListener : MonoBehaviour
{
    public MainController mainController;

    public GameObject traker;


    public void OnTrackerVisible()
    {
        Debug.LogError("on Tracker found");
        if(mainController.isInitialized == true)
        {
            mainController.OnTragetFound(traker);
        }
    }

    public void OnTrackerLost()
    {
        Debug.LogError("on Tracker lost");
        if (mainController.isInitialized == true)
        {
            mainController.OnTargetLost(traker);
        }
    }
}
