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
        var start11 = NavigationManager.GiveMeRootToStartFrom(new NodeRenderedContent("This is one day in a life of a human living in the heart of the African savannah together with his herd.", "Savana", "0-1"))
            .AddSingleNextNode(new NodeRenderedContent("X heard a strange noise. Looking up, you saw a group of giraffes driving trucks and bulldozers through the grassland, tearing down trees.", "Savana", "0-2"));

        var choice1 = start11.AddSingleNextNode(new NodeRenderedContent("Leave to search for another habitat, or stay here?", "Giraffe", "0-3"))
            .AddChoiceNodes(new NodeRenderedContent("Leave", "Giraffe"), new NodeRenderedContent("Stay", "Savana"));
        
 

        var c1 = choice1.Item1.AddSingleNextNode(new NodeRenderedContent("Your herd and you successfully found a new habitat within the savannah, with enough trees and water. This had been a long journey. Now you really need to take a break.", "Savana", "01-1"));
        var c2 = choice1.Item2.AddSingleNextNode(new NodeRenderedContent("Over time, X noticed that the grassland was shrinking, and the trees were disappearing. It's becoming increasingly hard for your herd to find enough food and water to survive.", "Giraffe", "02-1"));

       var choice2 = c1.AddSingleNextNode(new NodeRenderedContent("Go to sleep?", "Avatar", "01-2"))
            .AddChoiceNodes(new NodeRenderedContent("Yes", "Avatar"), new NodeRenderedContent("No", "Avatar"));

        var choice3 = c2.AddSingleNextNode(new NodeRenderedContent("Go searching for food and water, or stay here??", "Avatar", "02-2"))
            .AddChoiceNodes(new NodeRenderedContent("Go", "Avatar"), new NodeRenderedContent("Stay", "Avatar"));



        var c11 = choice2.Item1.AddSingleNextNode(new NodeRenderedContent("The sound of gunshots rang out across the savannah. It's the poachers. While wildlife trade is forbidden by law, many giraffes still would take the risk hunting and poaching human for the value of their skin and meat. Unfortunately, you became the target of the poachers and died. However, the gunshots served as an alert for most of the others in your herd. They survived, and would continue their journey to search for a shelter. \nBE 1: Die in sleep", "Avatar", "011-1"))
            .AddSingleNextNode(new NodeRenderedContent("While the wildlife trade is forbidden by law, many giraffes still would take the risk of hunting and poaching humans for the value of their skin and meat.", "Avatar", "011-2"))
            .AddSingleNextNode(new NodeRenderedContent("Unfortunately, you became the target of the poachers and died. However, the gunshots served as an alert for most of the others in your herd. They survived, and would continue their journey to search for shelter. \nBE 1: Die in sleep", "Avatar", "011-3"));/*updated*/
        var c12 = choice2.Item2.AddSingleNextNode(new NodeRenderedContent("You remained on high alert, always scanning the horizon for any sign of danger. You knew that you had to be careful if your herd wanted to survive.", "Avatar", "012-1"))
            .AddSingleNextNode(new NodeRenderedContent("Later this night, the sound of gunshots rang out across the savannah. You knew that the poachers had come and that they were after your herd.", "Avatar", "012-2"));/*updated*/
        var c21 = choice3.Item1.AddSingleNextNode(new NodeRenderedContent("Find a place with a few trees and a small river", "Avatar", "021-1"));
        var c22 = choice3.Item2.AddSingleNextNode(new NodeRenderedContent("Your habitat destroyed by giraffe activities. You saw your own herd dwindle in numbers, and the once-vibrant savannah became a barren wasteland.", "Avatar", "022-1"))
            .AddSingleNextNode(new NodeRenderedContent("One day, eventually, you starve to die. \nBE 4: habitats destroyed", "Avatar", "022-2"));/*updated*/


        var choice4 = c12.AddSingleNextNode(new NodeRenderedContent("What do you do now?", "Avatar", "012-3"))
            .AddChoiceNodes(new NodeRenderedContent("Run", "Avatar"), new NodeRenderedContent("Fight", "Avatar"));

        var choice5 = c21.AddSingleNextNode(new NodeRenderedContent("What do you do now?", "Avatar", "021-2"))
            .AddChoiceNodes(new NodeRenderedContent("Eat and drink", "Avatar"), new NodeRenderedContent("Return to notify your herd", "Avatar"));



        var c121 = choice4.Item1.AddSingleNextNode(new NodeRenderedContent("You wake up your herd and start to run as fast as you could, trying to outrun the poachers' bullets. The poacher chase you but they are slow and tall meanwhile you're quick and small. In front there are 2 path.", "Avatar", "0121-1"));
        var c122 = choice4.Item2.AddSingleNextNode(new NodeRenderedContent("You fight the poachers. Those giraffes headshot you and you die instantly. \nBE 2: Resistance", "Avatar", "0122-1"));
        var c211 = choice5.Item1.AddSingleNextNode(new NodeRenderedContent("The night came and you got lost, unable to find your way back to the herd. You cannot protect yourself yet. You died of lion predation. \nBE 5: Out of the herd", "Avatar", "0211-1"));
        var c212 = choice5.Item2.AddSingleNextNode(new NodeRenderedContent("Your herd decide to move to the new habitat you have found.Your herd decide to move to the new habitat you have found.", "Avatar", "0212-1"));


        var choice6 = c121.AddSingleNextNode(new NodeRenderedContent("Where do you go?", "Avatar", "0121-2"))
            .AddChoiceNodes(new NodeRenderedContent("A cave", "Avatar"), new NodeRenderedContent("Keep going", "Avatar"));

        var choice7 = c212.AddSingleNextNode(new NodeRenderedContent("What to do now?", "Avatar", "0212-2"))
            .AddChoiceNodes(new NodeRenderedContent("Eat", "Avatar"), new NodeRenderedContent("Drink", "Avatar"));


        var c1211 = choice6.Item1.AddSingleNextNode(new NodeRenderedContent("You found refuge in a cave because the giraffes couldn't enter due to their height. As you ventured deeper into the darkness, you heard the voices of fellow humans. Relief flooded over you as you discovered a human community shelter.", "Avatar", "01211-1"))
            .AddSingleNextNode(new NodeRenderedContent("The people in the shelter were kind and welcoming, providing a much-needed respite from the dangers outside. You waited until it was safe before returning to lead the rest of your herd to the shelter.", "Avatar", "01211-2"))
            .AddSingleNextNode(new NodeRenderedContent("Finally, you and your herd could settle down and take a break.", "Avatar", "01211-3"))
            .AddSingleNextNode(new NodeRenderedContent("Though, there was a second when confusion passed through your mind. Somehow, you felt the poachers that day were intentionally leading you to enter this cave. But that could not be possible. Why would they do that? \nNE: SHELTER", "Avatar", "01211-4"));/*updated*/
        var c1212 = choice6.Item2.AddSingleNextNode(new NodeRenderedContent("The giraffes shot your legs and you're injured. In front of you there are 2 path .", "Avatar", "01212-1"));
        var c2121 = choice7.Item1.AddSingleNextNode(new NodeRenderedContent("You and your herd started to eat leaves from the trees.", "Avatar", "02121-1"))
            .AddSingleNextNode(new NodeRenderedContent("In the first few days, everything was fine. Over-browse from the same trees activate the trees defense mechanism and prompt increased production of tannins, which makes leaves bitter-tasting.", "Avatar", "02121-2"));/*updated*/
        var c2122 = choice7.Item2.AddSingleNextNode(new NodeRenderedContent("You and your herd enjoyed the water from the river. A few moments later, you started to feel sick, so did most of people in your herd.", "Avatar", "02122-1"))
            .AddSingleNextNode(new NodeRenderedContent("Walking along the river with weak legs, you saw a vast expanse of cultivated land and a large factory emitting dark smoke into the sky.", "Avatar", "02122-2"))
            .AddSingleNextNode(new NodeRenderedContent("You died of polluted water. \nBE 6: Contaminated water", "Avatar", "02122-3"));/*updated*/

        var choice8 = c1212.AddSingleNextNode(new NodeRenderedContent("Which path will you choose?", "Avatar", "01212-2"))
            .AddChoiceNodes(new NodeRenderedContent("Left", "Avatar"), new NodeRenderedContent("Right", "Avatar"));

        var choice9 = c2121.AddSingleNextNode(new NodeRenderedContent("What to do now?", "Avatar", "02121-3"))
            .AddChoiceNodes(new NodeRenderedContent("Continue to eat the bitter leaves", "Avatar"), new NodeRenderedContent("Leave to find another habitat", "Avatar"));


        var c12121 = choice8.Item1.AddSingleNextNode(new NodeRenderedContent("You got rid of the poachers, but you were also too weak to move anymore. You lost consciousness.", "Avatar", "012121-1"))
            .AddSingleNextNode(new NodeRenderedContent("After you wake up, you realized you were in a giraffe's place. He has no bad intentions, though. He's a veterinarian that saved your life.", "Avatar", "012121-2"))
            .AddSingleNextNode(new NodeRenderedContent("After you were cured, he sent you to a zoo, where you spent the rest of your life. You have endless food and water. You do not need to worry about predators anymore. Every day a lot of giraffes, young and old, come to see you. You brought profit to the zoo. All these giraffes love you.", "Avatar", "012121-3"))
            .AddSingleNextNode(new NodeRenderedContent("Once in a while, at midnight, you will think of your herd and savannah. \nIs what you have now worth giving up freedom? That is a question. \nHE: Zoo animal", "Avatar", "012121-4"));/*updated*/
        var c12122 = choice8.Item2.AddSingleNextNode(new NodeRenderedContent("Keep running right. You found a small hut where you can lay low for a bit.", "Avatar", "012122-1"));
        var c21211 = choice9.Item1.AddSingleNextNode(new NodeRenderedContent("Increased production of tannins in these leaves inhibit your ability to digest food. Poor digestion causes you and your herd in poor health condition.", "Avatar", "021211-1"))
            .AddSingleNextNode(new NodeRenderedContent("One day, you died of lion predation, and the remaining members of your herd will continue their journey to search for a shelter. \nBE 7: Overbrowsing", "Avatar", "021211-2"));/*updated*/
        var c21212 = choice9.Item2.AddSingleNextNode(new NodeRenderedContent("You and your herd encountered poachers on the way.", "Avatar", "021212-1"))
            .AddSingleNextNode(new NodeRenderedContent("While the wildlife trade is forbidden by law, many giraffes still would take the risk of hunting and poaching humans for the value of their skin and meat.", "Avatar", "021212-2"))
            .AddSingleNextNode(new NodeRenderedContent("You tried to run, but the shortage of food and water left you with limited energy to do that. You died. \nBE 8: Greed", "Avatar", "021212-3"));/*updated*/



        var choice10 = c21212.AddSingleNextNode(new NodeRenderedContent("What to do next?", "Avatar", "012122-2"))
            .AddChoiceNodes(new NodeRenderedContent("Go Left", "Avatar"), new NodeRenderedContent("Go Right", "Avatar"));

        var c121221 = choice10.Item1.AddSingleNextNode(new NodeRenderedContent("Stay low. you are surrounded by giraffes and they capture you. \nBE 3: Fall at the last hurdle", "Avatar", "0121221-1"));
        var c121222 = choice10.Item2.AddSingleNextNode(new NodeRenderedContent("Lay low, you survived.", "Avatar", "0121222-1"));

        c121222.AddSingleNextNode(new NodeRenderedContent("You barely escaped poachers, but your fellow humans weren't as lucky. Their lives were cut short by giraffe greed. Over time, more and more humans disappeared, and the once-vibrant savannah became barren.", "Avatar", "0121222-2"))
            .AddSingleNextNode(new NodeRenderedContent("A few years later, the giraffes started a conservation program to protect humans. You journeyed to the protected area, weak and tired, but excited to be reunited with fellow humans. You found belonging and hope for the first time in years.", "Avatar", "0121222-3"))
            .AddSingleNextNode(new NodeRenderedContent("Despite the conservationists' best efforts, you passed away. Your legacy lived on through those who survived and found refuge.", "Avatar", "0121222-4"))
            .AddSingleNextNode(new NodeRenderedContent("True Ending: No Regrets"));/*updated*/

    }

}
