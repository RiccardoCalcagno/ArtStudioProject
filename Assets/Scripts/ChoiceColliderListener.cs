using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoiceColliderListener : MonoBehaviour
{
    public MainController mainController;
    private GameObject tracker;
    public bool isLeft = true;

    public void Start()
    {
        tracker = gameObject.GetComponentInParent<observeTrakerListener>().gameObject;
    }

    public void OnTriggerEnter(Collider other)
    {
        mainController.OnTragetInCollider(tracker, other.gameObject, isLeft);
    }

    public void OnTriggerExit(Collider other)
    {
        mainController.OnTragetOutCollider(tracker, other.gameObject, isLeft);
    }
}
