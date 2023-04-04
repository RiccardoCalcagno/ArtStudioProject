using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainController : MonoBehaviour
{
    public static readonly string NAME_OF_AVATAR_GAME_OBJ = "AvatarOBJ";

    public Dictionary<GameObject, bool> TrackerRecognized = new Dictionary<GameObject, bool>();


    public int counterOfUpdates = 0;
    private float each10millsec = 0;

    public AudioSource audioSource;


    private GameObject floatingObj = null;
    public float tolerance = 0.05f;
    public int DIFF_BUFFER_LENGTH = 20;
    public Vector3[] prevDifferences = null;
    private int indexOfBuffer = 0;
    public float currentVariance = 0;




    private void Start()
    {
        CreateDecisionTree();
    }


    private void Update()
    {
        each10millsec += Time.deltaTime;
        if (each10millsec >= 0.01f) // contatore aggiornato ogni millisecondo
        {
            each10millsec -= 0.01f;
            counterOfUpdates++;
            counterOfUpdates %= 10000;



            if(NavigationManager.CurrentNode == null || NavigationManager.CurrentNode.RequireUserInteraction == false)
            {
                floatingObj = null;
            }

            if (floatingObj != null && TrackerRecognized[floatingObj] == true)
            {
                indexOfBuffer++; indexOfBuffer %= DIFF_BUFFER_LENGTH;
                prevDifferences[indexOfBuffer] = NavigationManager.CurrentNode.ReferredTrackingMetaData.Tracker.transform.position - floatingObj.transform.position;

                currentVariance = VectorVariance(prevDifferences);


                if (currentVariance < tolerance)
                {
                    NavigationManager.MoveToNextThanksToCard(floatingObj, IsTheNewTrackerOnTheLeftSide());

                    floatingObj = null;
                    currentVariance = 0;
                }
            }
        }
    }


    private float VectorVariance(Vector3[] vectors)
    {
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < vectors.Length; i++)
        {
            sum += vectors[i];
        }

        Vector3 average = sum / vectors.Length;

        float sumSquares = 0f;

        for (int i = 0; i < vectors.Length; i++)
        {
            float sqrMag = (vectors[i] - average).sqrMagnitude;
            sumSquares += sqrMag;
        }

        return sumSquares / vectors.Length;
    }



    private bool IsTheNewTrackerOnTheLeftSide()
    {
        var transfOfCurrentNode = NavigationManager.CurrentNode.ReferredTrackingMetaData.Tracker.transform;
        Vector3 obj1Forward = transfOfCurrentNode.forward;
        Vector3 obj1ToObj2 = floatingObj.transform.position - transfOfCurrentNode.position;
        float angle = Vector3.SignedAngle(obj1Forward, obj1ToObj2, Vector3.up);
        return angle < 0;
    }



    public void TryToSkipCurrentStep()
    {
        NavigationManager.CurrentNode.EndAnyTimerOfCard();
    }


    public void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        NavigationManager.Initialize(this);
    }



    public void OnTragetFound(GameObject traker)
    {
        if(TrackerRecognized.Keys.Contains(traker) == false)
        {
            if(TrackerRecognized.Count == 0)
            {
                NavigationManager.StartFromFirstNode(traker);
            }
            else
            {
                Debug.LogWarning("OnTragetFound " + traker.GetInstanceID() + ", CurrentTracker:  "
                    +NavigationManager.CurrentNode.ReferredTrackingMetaData.Tracker.GetInstanceID()
                    + ", RequireUserInteraction:"+ NavigationManager.CurrentNode.RequireUserInteraction);

                if(NavigationManager.CurrentNode.RequireUserInteraction == true)
                {
                    // Found the next tracker to consider where is going to stop

                    floatingObj = traker;

                    prevDifferences = new Vector3[DIFF_BUFFER_LENGTH];
                    for(int i=0; i< DIFF_BUFFER_LENGTH; i++)
                    {
                        prevDifferences[i] = UnityEngine.Random.insideUnitSphere * 10000;
                    }
                }
            }
        }


        TrackerRecognized[traker] = true;
        NavigationManager.SetVisibilityForTarget(traker, true);
    }

    public void OnTargetLost(GameObject tracker)
    {
        //TODO how to handle?

        TrackerRecognized[tracker] = false;
        NavigationManager.SetVisibilityForTarget(tracker, false);
    }




    public void PlayerHasMovedToAnotherNode()
    {
        var nextNode = NavigationManager.CurrentNode;
    }




    public void CreateDecisionTree()
    {
        var start11 = NavigationManager.GiveMeRootToStartFrom(new NodeRenderedContent("This is one day in a life of a human living in the heart of the African savannah together with his herd.", "Savana", "successSound"))
            .AddSingleNextNode(new NodeRenderedContent("X heard a strange noise. Looking up, you saw a group of giraffes driving trucks and bulldozers through the grassland, tearing down trees.", "Savana", "successSound"));

        var choice1 = start11.AddSingleNextNode(new NodeRenderedContent("Leave to search for another habitat, or stay here?", "", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Leave", "Giraffe"), new NodeRenderedContent("Stay", "Savana"));
        
 

        var c1 = choice1.Item1.AddSingleNextNode(new NodeRenderedContent("Your herd and you successfully found a new habitat within the savannah, with enough trees and water. This had been a long journey. Now you really need to take a break.", "Savana", "successSound"));
        var c2 = choice1.Item2.AddSingleNextNode(new NodeRenderedContent("Over time, X noticed that the grassland was shrinking, and the trees were disappearing. It's becoming increasingly hard for your herd to find enough food and water to survive.", "Giraffe", "successSound"));

       var choice2 = c1.AddSingleNextNode(new NodeRenderedContent("Go to sleep?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Yes", "Avatar"), new NodeRenderedContent("No", "Avatar"));

        var choice3 = c2.AddSingleNextNode(new NodeRenderedContent("Go searching for food and water, or stay here??", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Go", "Avatar"), new NodeRenderedContent("Stay", "Avatar"));


        var c11 = choice2.Item1.AddSingleNextNode(new NodeRenderedContent("The sound of gunshots rang out across the savannah. It's the poachers. While wildlife trade is forbidden by law, many giraffes still would take the risk hunting and poaching human for the value of their skin and meat. Unfortunately, you became the target of the poachers and died. However, the gunshots served as an alert for most of the others in your herd. They survived, and would continue their journey to search for a shelter. \nBE 1: Die in sleep", "Avatar", "successSound"));
        var c12 = choice2.Item2.AddSingleNextNode(new NodeRenderedContent("You remained on high alert, always scanning the horizon for any sign of danger. You knew that you had to be careful if your herd wanted to survive. Later this night, the sound of gunshots rang out across the savannah. You knew that the poachers had come and that they were after your herd.", "Avatar", "successSound"));
        var c21 = choice3.Item1.AddSingleNextNode(new NodeRenderedContent("Find a place with a few trees and a small river", "Avatar", "successSound"));
        var c22 = choice3.Item2.AddSingleNextNode(new NodeRenderedContent("Your habitat destroyed by giraffe activities. You saw your own herd dwindle in numbers, and the once-vibrant savannah became a barren wasteland. One day, eventually, you starve to die. BE 4: habitats destroyed", "Avatar", "successSound"));


        var choice4 = c12.AddSingleNextNode(new NodeRenderedContent("What do you do now?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Run", "Avatar"), new NodeRenderedContent("Fight", "Avatar"));

        var choice5 = c21.AddSingleNextNode(new NodeRenderedContent("What do you do now?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Eat and drink", "Avatar"), new NodeRenderedContent("Return to notify your herd", "Avatar"));


        var c121 = choice4.Item1.AddSingleNextNode(new NodeRenderedContent("You wake up your herd and start to run as fast as you could, trying to outrun the poachers' bullets. The poacher chase you but they are slow and tall meanwhile you're quick and small. In front there are 2 path.", "Avatar", "successSound"));
        var c122 = choice4.Item2.AddSingleNextNode(new NodeRenderedContent("You fight the poachers. Those giraffes headshot you and you die instantly. BE 2: Resistance", "Avatar", "successSound"));
        var c211 = choice5.Item1.AddSingleNextNode(new NodeRenderedContent("The night came and you got lost, unable to find your way back to the herd. You cannot protect yourself yet. You died of lion predation. BE 5: Out of the herd", "Avatar", "successSound"));
        var c212 = choice5.Item2.AddSingleNextNode(new NodeRenderedContent("Your herd decide to move to the new habitat you have found.Your herd decide to move to the new habitat you have found.", "Avatar", "successSound"));


        var choice6 = c121.AddSingleNextNode(new NodeRenderedContent("What do you do go?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("A cave", "Avatar"), new NodeRenderedContent("Keep going", "Avatar"));

        var choice7 = c212.AddSingleNextNode(new NodeRenderedContent("What to do now?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Eat", "Avatar"), new NodeRenderedContent("Drink", "Avatar"));


        var c1211 = choice6.Item1.AddSingleNextNode(new NodeRenderedContent("You are safe because the giraffes are too tall to enter the cave. While the inside of the cave is dark, you heard some familiar voices. It's human! You found a human community shelter! The people in this shelter are all very welcoming. You waited until you do not hear the poachers  anymore and then go back to happily bring all the remaining members of your herd to this cave. You can finally settle down. Though, there was a second when confusion passed through your mind. Somehow, you felt the poachers that day were intentionally leading you to enter this cave. But that could not be possible. Why would they do that? NE: SHELTER", "Avatar", "successSound"));
        var c1212 = choice6.Item2.AddSingleNextNode(new NodeRenderedContent("The giraffes shot your legs and you're injured. In front of you there are 2 path .", "Avatar", "successSound"));
        var c2121 = choice7.Item1.AddSingleNextNode(new NodeRenderedContent("You and your herd started to eat leaves from the trees. In the first few days, everything was fine. Over-browse from the same trees activate the trees defense mechanism and prompt increased production of tannins, which makes leaves bitter-tasting.", "Avatar", "successSound"));
        var c2122 = choice7.Item2.AddSingleNextNode(new NodeRenderedContent("You and your herd enjoyed the water from the river. A few moments later, you started to feel sick, so did most of people in your herd. Walking along the river with weak legs, you saw a vast expanse of cultivated land and a large factory emitting dark smoke into the sky. You died of polluted water. BE 6: Contaminated water", "Avatar", "successSound"));


        var choice8 = c1212.AddSingleNextNode(new NodeRenderedContent("Which path will you choose?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Left", "Avatar"), new NodeRenderedContent("Right", "Avatar"));

        var choice9 = c2121.AddSingleNextNode(new NodeRenderedContent("What to do now?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Continue to eat the bitter leaves", "Avatar"), new NodeRenderedContent("Leave to find another habitat", "Avatar"));


        var c12121 = choice8.Item1.AddSingleNextNode(new NodeRenderedContent("You got rid of the poachers, but you were also too weak to move anymore. You lost your consciousness. After you wake up, you realized you were in a giraffe's place. He has no bad intention, though. He's a veterinary that saved your life. After you were cured, he sent you to a zoo, where you spent the rest of your life. You have endless food and water. You do not need to worry about predators anymore. Everyday a lot of giraffes, young and old, come to see you. You brought profit to the zoo. You are loved by all these giraffes. Once in a while, in the midnight, you will think of your herd, and your savannah. Is what you have now worth giving up freedom? That is a question. HE: Zoo animal", "Avatar", "successSound"));
        var c12122 = choice8.Item2.AddSingleNextNode(new NodeRenderedContent("Keep running right. You found a small hut where you can lay low for a bit.", "Avatar", "successSound"));
        var c21211 = choice9.Item1.AddSingleNextNode(new NodeRenderedContent("Increased production of tannins in these leaves inhibit your ability to digest food. Poor digestion causes you and your herd in poor health condition. One day, you died of lion predation, and the remaining members of your herd will continue their journey to search for a shelter. BE 7: Overbrowsing", "Avatar", "successSound"));
        var c21212 = choice9.Item2.AddSingleNextNode(new NodeRenderedContent("You and your herd encountered poachers on the way. While wildlife trade is forbidden by law, many giraffes still would take the risk hunting and poaching human for the value of their skin and meat. You tried to run, but shortage of food and water left you limited energy to do that. You died. BE 8: Greed", "Avatar", "successSound"));


        var choice10 = c21212.AddSingleNextNode(new NodeRenderedContent("What to do now?", "Avatar", "successSound"))
            .AddChoiceNodes(new NodeRenderedContent("Continue to eat the bitter leaves", "Avatar"), new NodeRenderedContent("Leave to find another habitat", "Avatar"));

        var c121221 = choice10.Item1.AddSingleNextNode(new NodeRenderedContent("Stay low. you are surrounded by giraffes and they capture you. BE 3: Fall at the last hurdle", "Avatar", "successSound"));
        var c121222 = choice10.Item2.AddSingleNextNode(new NodeRenderedContent("Lay low, you survived.", "Avatar", "successSound"));

        c121222.AddSingleNextNode(new NodeRenderedContent("You were lucky to escape the poachers, but you knew that many of your fellow humans had not been so fortunate. You grieved for your fallen herd members, knowing that their lives had been cut short by giraffe greed. Years went by, and you watched as more and more of your kind disappeared, your once habitats destroyed by giraffe activities. The once-vibrant savannah became a barren wasteland.  But despite the challenges, you remained determined to survive, and never gave in to hunters and poachers. Later, the giraffes started a new conservation program aimed at protecting the remaining human populations from extinction. The program created protected areas where humans could roam freely without the threat of giraffe activities. You were thrilled to hear about this and immediately set out to find the protected area. After a long journey, you finally arrived and was reunited with fellow humans. For the first time in years, you felt a sense of hope and belonging. However, the journey was so long that you became weaker than ever before. Despite the best efforts of the conservationists, you eventually passed away. True Ending: No regrets", "Avatar", "successSound"));
    }

}
