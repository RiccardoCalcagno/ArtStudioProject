using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour
{
    public static readonly string NAME_OF_AVATAR_GAME_OBJ = "AvatarOBJ";


    public GameObject[] imageTargets = new GameObject[6];


    public bool wasTheFirstTargetPlaced = false;


    public void Awake()
    {
        NavigationManager.Initialize(this);
    }



    public void OnTragetFound(GameObject traker)
    {
        if(wasTheFirstTargetPlaced == false)
        {
            wasTheFirstTargetPlaced = true;

            NavigationManager.StartFromFirstNode(traker);
        }
    }

    public void OnTargetLost(GameObject tracker)
    {
        //TODO how to handle?
    }


    public void OnTragetInCollider(GameObject tracker, GameObject otherTracker, bool isLeft)
    {
        if (NavigationManager.CurrentNode.RequireUserInteraction == true
            && NavigationManager.CurrentNode.ReferredTrackingMetaData.Tracker == tracker)
        {
            NavigationManager.MoveToNextThanksToCard(otherTracker, isLeft);
        }
    }

    public void OnTragetOutCollider(GameObject tracker, GameObject otherTracker, bool isLeft)
    {
        //TODO how to handle?
    }



    public void PlayerHasMovedToAnotherNode()
    {
        var nextNode = NavigationManager.CurrentNode;
    }


    // Start is called before the first frame update
    void Start()
    {
        // Cerca gli ImageTarget nella scena e li aggiunge all'array
        for (int i = 1; i <= 6; i++)
        {
            string objectName = "ImageTarget" + i;
            GameObject obj = GameObject.Find(objectName);
            if (obj != null)
            {
                imageTargets[i - 1] = obj;
            }
            else
            {
                Debug.LogWarning("Impossibile trovare l'oggetto " + objectName);
            }
        }


        CreateDecisionTree();
    }


    public void CreateDecisionTree()
    {
        var choice1 = NavigationManager.GiveMeRootToStartFrom(new NodeRenderedContent("This is the first card, the start of your jorney", "Avatar"))
            .AddChoiceNodes(new NodeRenderedContent("Left Choice", "Avatar"), new NodeRenderedContent("right Choice", "Avatar"));

        choice1.Item1.AddSingleNextNode(new NodeRenderedContent("Intermediate Choice", "Avatar"))
            .AddChoiceNodes(new NodeRenderedContent("Left Choice", "Avatar"), new NodeRenderedContent("right Choice", "Avatar"));

        var choice12 = choice1.Item2.AddSingleNextNode(new NodeRenderedContent("Intermediate Choice", "Avatar"))
            .AddChoiceNodes(new NodeRenderedContent("Left Choice", "Avatar"), new NodeRenderedContent("right Choice", "Avatar"));

        choice12.Item1.AddSingleNextNode(new NodeRenderedContent("Single Choice with timer", "Avatar")).AddSingleNextNode(new NodeRenderedContent("Last Single Choice", "Avatar"));
    }

}
