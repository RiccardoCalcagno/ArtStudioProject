using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class NavigationManager
{
    public static void Initialize(MainController mainController)
    {
        NavigationManager.mainController = mainController;
    }

    public static MainController mainController;

    private static readonly Vector3 RELATIVE_POSITION_AVATAR = new Vector3(0, 0, -0.05f);


    public static readonly float DELAY_TO_READY_TRANSITORIAL_NODE = 5.0f; // sec
        

    public static DecisionTreeNode CurrentNode;

    private static DecisionTreeNode root = null;


    private static void MoveAvatarFromTracker(NodeMetaTrackingData newTracker)
    {
        var avatar = GameObject.Find(MainController.NAME_OF_AVATAR_GAME_OBJ);

        avatar.transform.parent = newTracker.Tracker.transform;

        avatar.transform.SetLocalPositionAndRotation(RELATIVE_POSITION_AVATAR, Quaternion.identity);

    }
    private static void IstantiateAvatarToTracker(NodeMetaTrackingData metaTracker)
    {
        // TODO
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Avatar");

        // Istanzia il prefab come figlio del gameObject padre.
        GameObject instance = GameObject.Instantiate(prefab, metaTracker.Tracker.transform);

        instance.name = MainController.NAME_OF_AVATAR_GAME_OBJ;

        instance.transform.SetLocalPositionAndRotation(RELATIVE_POSITION_AVATAR, Quaternion.identity);
    }


    public static void MoveToNextThanksToTimer(DecisionTreeNode decisionTreeExpired)
    {
        if(decisionTreeExpired != CurrentNode)
        {
            Debug.LogError("This should not happend, the node expired it is not more the current node");
            return;
        }

        if(CurrentNode.LeftNode == null)
        {
            Debug.Log("End of Decision Tree");
            return;
        }

        CurrentNode?.Unload();

        CurrentNode = CurrentNode.LeftNode;

        CurrentNode.Load();

        mainController.PlayerHasMovedToAnotherNode();
    }

    public static void MoveToNextThanksToCard(GameObject newTracker, bool isLeft = true)
    {

        DecisionTreeNode nextTrackingNode;
        string prevTextChoice = "";

        if(isLeft == true)
        {
            nextTrackingNode = CurrentNode.LeftNode.LeftNode;
            prevTextChoice = CurrentNode.LeftNode.Content.description;
        }
        else
        {
            nextTrackingNode = CurrentNode.RightNode.LeftNode;
            prevTextChoice = CurrentNode.RightNode.Content.description;
        }

        
        CurrentNode.LeftNode.Unload();
        CurrentNode.RightNode.Unload();

        CurrentNode.Content.description = CurrentNode.Content.description + ":\n\n" + prevTextChoice;
        CurrentNode.Content.UpdateView();

        var newMetaDataTracker = CreateNewMetaDataBasedOnThePrev(nextTrackingNode, newTracker);

        nextTrackingNode.ReferredTrackingMetaData = newMetaDataTracker;

        CurrentNode = nextTrackingNode;

        CurrentNode.Load();

        NavigationManager.MoveAvatarFromTracker(CurrentNode.ReferredTrackingMetaData);

        mainController.PlayerHasMovedToAnotherNode();
    }



    public static DecisionTreeNode GiveMeRootToStartFrom(NodeRenderedContent firstContent)
    {
        CurrentNode = new DecisionTreeNode(null, firstContent);

        root = CurrentNode;

        return CurrentNode;
    }

    public static void StartFromFirstNode(GameObject firstTracker)
    {
        var newMetaDataTracker = CreateNewMetaDataBasedOnThePrev(CurrentNode, firstTracker);

        CurrentNode.ReferredTrackingMetaData = newMetaDataTracker;

        CurrentNode.Load();

        NavigationManager.IstantiateAvatarToTracker(CurrentNode.ReferredTrackingMetaData);

        mainController.PlayerHasMovedToAnotherNode();
    }



    static NodeMetaTrackingData CreateNewMetaDataBasedOnThePrev(DecisionTreeNode subject, GameObject newTracker)
    {
        var prevMetaData = subject?.ParentNode?.ReferredTrackingMetaData;

        //TODO
        // prevMetaData can be null : it is the start
        return new NodeMetaTrackingData(subject, newTracker);
    }


    public static void SetVisibilityForTarget(GameObject target, bool isVisible)
    {
        DecisionTreeNode.SetVisibilityFormTarget(root, target, isVisible);
    }

}



public class NodeRenderedContent
{
    public NodeRenderedContent(string description = "", string nameOfPrefab3D = "", string nameOfAudioClip = "")
    {
        this.nameOfPrefab3D = nameOfPrefab3D;
        this.description = description;

        if(nameOfAudioClip!= "")
        {
            audioClip = Resources.Load<AudioClip>("Audios/" + nameOfAudioClip);
        }
    }

    public DecisionTreeNode nodeSubject;

    public string nameOfPrefab3D = "";

    public string description = "";

    public AudioClip audioClip = null;

    private bool audioStarted = false;

    private GameObject parentWhereItIsIstantiated = null;

    public void UpdateView()
    {
        if(parentWhereItIsIstantiated!= null)
        {
            var instance = parentWhereItIsIstantiated.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.gameObject.name == "Content")?.gameObject;

            var asset3D = instance.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.gameObject.name == "Asset3D")?.gameObject;
            if(asset3D!= null) GameObject.Destroy(asset3D);

            if (nameOfPrefab3D != "")
            {
                GameObject asset3DPrefab = Resources.Load<GameObject>("Prefabs/" + nameOfPrefab3D);

                var anchor = instance.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.gameObject.name == "PrefabAnchorCentered")?.gameObject;
                // Istanzia il prefab come figlio del gameObject padre.
                var ass3Dgmobj = GameObject.Instantiate(asset3DPrefab, anchor.transform);

                ass3Dgmobj.name = "Asset3D";

                ass3Dgmobj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            var text = instance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = description;
        }
    }

    public void InstantiateContentInPlace()
    {

        if(parentWhereItIsIstantiated!= null)
        {
            Dispose();
        }

        if(nodeSubject.IsLeftOrRightChoiceNode == false)
        {
            parentWhereItIsIstantiated = nodeSubject.ReferredTrackingMetaData.Tracker;
        }
        else if(nodeSubject.ParentNode.LeftNode == nodeSubject)
        {
            parentWhereItIsIstantiated = nodeSubject.ReferredTrackingMetaData.Tracker
                .GetComponentsInChildren<Transform>().FirstOrDefault(t => t.gameObject.name == "LeftAnchor")?.gameObject;
        }
        else
        {
            parentWhereItIsIstantiated = nodeSubject.ReferredTrackingMetaData.Tracker
                .GetComponentsInChildren<Transform>().FirstOrDefault(t => t.gameObject.name == "RightAnchor")?.gameObject;
        }

        GameObject contentPrefab = Resources.Load<GameObject>("Prefabs/Content");

        // Istanzia il prefab come figlio del gameObject padre.
        GameObject instance = GameObject.Instantiate(contentPrefab, parentWhereItIsIstantiated.transform);
        instance.name = "Content";
        instance.transform.SetAsFirstSibling();
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);


        UpdateView();
    }

    public int ResumeItsAudio()
    {
        if(audioClip != null)
        {
            if(NavigationManager.mainController.TrackerRecognized.TryGetValue(nodeSubject.ReferredTrackingMetaData.Tracker, out bool value) == true && value == true)
            {
                if (audioStarted == false)
                {
                    NavigationManager.mainController.audioSource.Stop();
                    NavigationManager.mainController.audioSource.clip = audioClip;  // Imposta l'AudioClip
                    NavigationManager.mainController.audioSource.Play();  // Riproduci l'AudioClip

                    audioStarted = true;
                }
                else
                {
                    if (NavigationManager.mainController.audioSource.clip == audioClip)
                    {
                        NavigationManager.mainController.audioSource.UnPause();
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            return (int)( (audioClip.length - NavigationManager.mainController.audioSource.time) * 1000f) + 400;  // Restituisci la durata in millisecondi
        }

        return (int)(NavigationManager.DELAY_TO_READY_TRANSITORIAL_NODE * 1000);
    }

    public void PauseAudio()
    {
        if (NavigationManager.mainController.audioSource.clip == audioClip)
        {
            NavigationManager.mainController.audioSource.Pause();
        }
    }

    public void Dispose()
    {
        if (parentWhereItIsIstantiated != null)
        {
            var child = parentWhereItIsIstantiated.GetComponentsInChildren<Transform>()
                .FirstOrDefault(t => t.gameObject.name == "Content");

            GameObject.Destroy(child?.gameObject);

            parentWhereItIsIstantiated = null;
        }

        PauseAudio();
    }
}


public class NodeMetaTrackingData
{
    DecisionTreeNode subject;

    public Quaternion RotationOffset { get; private set; }

    public GameObject Tracker { get; private set; }

    public int CounterOfTracker { get; private set; }


    public NodeMetaTrackingData(DecisionTreeNode subject, GameObject tracker)
    {
        var prevTrackerData = subject.ParentNode?.ReferredTrackingMetaData;
        if(prevTrackerData == null)
        {
            CounterOfTracker = 0;
        }
        else
        {
            CounterOfTracker = prevTrackerData.CounterOfTracker + 1;
        }

        this.subject = subject;
        this.Tracker = tracker;

        Build();
    }

    private void Build()
    {
        // TODO calculate rotation offset
        RotationOffset = Quaternion.identity;
    }
}




public class DecisionTreeNode
{

    public static void SetVisibilityFormTarget(DecisionTreeNode node, GameObject target, bool isVisibile)
    {
        if (node.ReferredTrackingMetaData == null)
        {
            node.IsNodeVisible = false;
            return;
        }
        else
        {
            if (node.ReferredTrackingMetaData.Tracker == target)
            {
                if(node.IsNodeVisible != isVisibile && node == NavigationManager.CurrentNode)
                {
                    if (isVisibile)
                    {
                        node.Content.ResumeItsAudio();
                    }
                    else
                    {
                        node.Content.PauseAudio();
                    }

                }
                node.IsNodeVisible = isVisibile;
            }
        }

        if(node.LeftNode != null)
        {
            DecisionTreeNode.SetVisibilityFormTarget(node.LeftNode, target, isVisibile);
        }
        if (node.RightNode != null)
        {
            DecisionTreeNode.SetVisibilityFormTarget(node.RightNode, target, isVisibile);
        }
    }


    public DecisionTreeNode ParentNode { get; private set; }

    public DecisionTreeNode(DecisionTreeNode parent, NodeRenderedContent content)
    {
        content.nodeSubject = this;

        Content = content;

        ParentNode = parent;
    }

    // left used as unique node when there is no choice to do
    private DecisionTreeNode leftNode = null, rightNode = null;

    public DecisionTreeNode LeftNode
    {
        get => leftNode;
        private set
        {
            if (rightNode != null && value != null)
            {
                requireUserInteraction = true;
            }
            leftNode = value;
        }
    }
    public DecisionTreeNode RightNode
    {
        get => rightNode;
        private set
        {
            if (rightNode != null && value != null)
            {
                requireUserInteraction = true;
            }
            rightNode = value;
        }
    }

    public NodeRenderedContent Content { get; private set; }

    private NodeMetaTrackingData selfTrackingMetaData = null;

    public NodeMetaTrackingData ReferredTrackingMetaData
    {
        get {
            if (selfTrackingMetaData != null)
            {
                return selfTrackingMetaData;
            }
            if(ParentNode == null)
            {
                return null;
            }
            return ParentNode.ReferredTrackingMetaData;
            }

        set
        {
            selfTrackingMetaData = value;
        }
    }


    private bool requireUserInteraction = false;
    public bool RequireUserInteraction {
        get => requireUserInteraction;
        set
        {
            if(leftNode != null && rightNode!= null && value == false)
            {
                throw new InvalidOperationException("the structure of this node require a choice by the user");
            }
            requireUserInteraction = value;
        }
    }


    public bool IsLeftOrRightChoiceNode { get; private set; } = false;


    private int millisecOfVisibility = 0;

    public bool IsNodeVisible = false;


    public void EndAnyTimerOfCard()
    {
        if(millisecOfVisibility > 0)
        {
            millisecOfVisibility = 0;
        }
    }


    public void Unload()
    {
        Content.Dispose();
    }

    public void Load()
    {
        Content.InstantiateContentInPlace();

        if(RequireUserInteraction == true && IsLeftOrRightChoiceNode == false)
        {
            if(LeftNode.IsLeftOrRightChoiceNode == true)
            {
                LeftNode.Load();
            }
            if(RightNode.IsLeftOrRightChoiceNode == true)
            {
                RightNode.Load();
            }
        }


        if(RequireUserInteraction == false && IsLeftOrRightChoiceNode == false && this == NavigationManager.CurrentNode)
        {
            int waitingTime = Content.ResumeItsAudio();

            millisecOfVisibility = waitingTime;

            NavigationManager.mainController.StartCoroutine(TimerThenGoToNext());
        }
    }

    private IEnumerator TimerThenGoToNext()
    {
        int lastCounter = NavigationManager.mainController.counterOfUpdates;

        yield return new WaitWhile(
            () =>
            {
                if(millisecOfVisibility <= 0 || NavigationManager.CurrentNode != this)
                {
                    return false;
                }

                if(lastCounter != NavigationManager.mainController.counterOfUpdates && IsNodeVisible == true)
                {
                    lastCounter = NavigationManager.mainController.counterOfUpdates;

                    millisecOfVisibility-= 10;
                }

                return true;
            }
            );

        if(NavigationManager.CurrentNode == this)
        {
            NavigationManager.MoveToNextThanksToTimer(this);
        }
    }



    public (DecisionTreeNode, DecisionTreeNode) AddChoiceNodes(NodeRenderedContent leftNodeContent, NodeRenderedContent rightNodeContent)
    {
        if (IsLeftOrRightChoiceNode == true)
        {
            throw new InvalidOperationException("Each choice node need to start from a not choice node");
        }

        if(rightNodeContent.audioClip != null)
        {
            throw new InvalidOperationException("NO! the choices of a tree should not have audio attaced, please provide in the parent node an audio that has inside also the text of the tw choices");
        }

        this.leftNode = new DecisionTreeNode(this, leftNodeContent);
        this.rightNode = new DecisionTreeNode(this, rightNodeContent);

        leftNode.IsLeftOrRightChoiceNode = true;
        rightNode.IsLeftOrRightChoiceNode = true;

        RequireUserInteraction = true;

        return (leftNode, rightNode);
    }

    public DecisionTreeNode AddSingleNextNode(NodeRenderedContent nextNodeContent, bool needsAnotherCardToContinue = false)
    {
        if (this.rightNode != null)
        {
            throw new InvalidOperationException("Pay attention probably you created before in a node a choice that now is overrided by a single flow");
        }

        this.leftNode = new DecisionTreeNode(this, nextNodeContent);

        this.leftNode.IsLeftOrRightChoiceNode = false;

        RequireUserInteraction = needsAnotherCardToContinue;

        return this.leftNode;
    }



}
