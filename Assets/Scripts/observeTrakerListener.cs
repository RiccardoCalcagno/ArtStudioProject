using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class observeTrakerListener : MonoBehaviour
{
    public MainController mainController;


    public void OnTrackerVisible()
    {
        mainController.OnTragetFound(gameObject);
    }

    public void OnTrackerLost()
    {
        mainController.OnTargetLost(gameObject);
    }
}
