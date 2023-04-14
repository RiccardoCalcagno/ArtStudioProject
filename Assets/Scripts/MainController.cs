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

    public bool isInitialized = false;

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
        isInitialized = true;
    }


    private void Update()
    {
        each10millsec += Time.deltaTime;
        while (each10millsec >= 0.01f) // contatore aggiornato ogni millisecondo
        {
            each10millsec -= 0.01f;
            counterOfUpdates++;
            counterOfUpdates %= 10000;

        }

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
        if (isInitialized == false)
        {
            return;
        }
        if (TrackerRecognized.Keys.Contains(traker) == false)
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
        if (isInitialized == false)
        {
            return;
        }
        TrackerRecognized[tracker] = false;
        NavigationManager.SetVisibilityForTarget(tracker, false);
    }




    public void PlayerHasMovedToAnotherNode()
    {
        var nextNode = NavigationManager.CurrentNode;
    }




    public void CreateDecisionTree()
    {
        var start11 = NavigationManager.GiveMeRootToStartFrom(new NodeRenderedContent("This is one day in a life of a human living in the heart of the African savannah together with his herd.", "Savana", "0-1","Idle"))
            .AddSingleNextNode(new NodeRenderedContent("X heard a strange noise. Looking up, you saw a group of giraffes driving trucks and bulldozers through the grassland, tearing down trees.", "GiraffesInTrucks", "0-2", "Idle"));

        var choice1 = start11.AddSingleNextNode(new NodeRenderedContent("Leave to search for another habitat, or stay here?", "DivergingPath", "0-3", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("Leave", "ExploreSavana", "",""), new NodeRenderedContent("Stay", "SavanaWithTruck", "", ""));/*Avatar walk*/
        
 

        var c1 = choice1.Item1.AddSingleNextNode(new NodeRenderedContent("Your herd and you successfully found a new habitat within the savannah, with enough trees and water. This had been a long journey. Now you really need to take a break.", "River", "01-1", "Idle"));
        var c2 = choice1.Item2.AddSingleNextNode(new NodeRenderedContent("Over time, X noticed that the grassland was shrinking, and the trees were disappearing. It's becoming increasingly hard for your herd to find enough food and water to survive.", "DamagedHabitat", "02-1", "Idle"));

       var choice2 = c1.AddSingleNextNode(new NodeRenderedContent("Go to sleep?", "DivergingPath", "01-2", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("Yes", "Moon", "", ""), new NodeRenderedContent("No", "", "", ""));/*Avatar sit with eyes closed*/

        var choice3 = c2.AddSingleNextNode(new NodeRenderedContent("Go searching for food and water, or stay here?", "DivergingPath", "02-2", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("Go", "ExploreSavana", "", ""), new NodeRenderedContent("Stay", "DamagedHabitat", "", ""));/*Avatar walk*/



        var c11 = choice2.Item1.AddSingleNextNode(new NodeRenderedContent("The sound of gunshots rang out across the savannah. It's the poachers.", "MultiplePoachers", "011-1", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("While the wildlife trade is forbidden by law, many giraffes still would take the risk of hunting and poaching humans for the value of their skin and meat.", "MultiplePoachers", "011-2", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("Unfortunately, you became the target of the poachers and died. However, the gunshots served as an alert for most of the others in your herd. They survived, and would continue their journey to search for shelter. \nBE 1: Die in sleep", "GiraffeWithShotgun", "011-3", "Idle"));/*Avatar sit with eyes closed*/
        var c12 = choice2.Item2.AddSingleNextNode(new NodeRenderedContent("You remained on high alert, always scanning the horizon for any sign of danger. You knew that you had to be careful if your herd wanted to survive.", "ExploreSavana", "012-1", "Idle"))/*Avatar sit*/
            .AddSingleNextNode(new NodeRenderedContent("Later this night, the sound of gunshots rang out across the savannah. You knew that the poachers had come and that they were after your herd.", "MultiplePoachers", "012-2", "Idle"));
        var c21 = choice3.Item1.AddSingleNextNode(new NodeRenderedContent("Find a place with a few trees and a small river", "River", "021-1", "Idle"));
        var c22 = choice3.Item2.AddSingleNextNode(new NodeRenderedContent("Your habitat destroyed by giraffe activities. You saw your own herd dwindle in numbers, and the once-vibrant savannah became a barren wasteland.", "Wasteland", "022-1", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("One day, eventually, you starve to die.", "Wasteland", "022-2", "Idle"));/*Avatar lying on stomach*/


        var choice4 = c12.AddSingleNextNode(new NodeRenderedContent("What do you do now?", "DivergingPath", "012-3", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("Run", "DamagedHabitat", "", ""), new NodeRenderedContent("Fight", "MultiplePoachers", "", ""));/*Avatar run; fight*/

        var choice5 = c21.AddSingleNextNode(new NodeRenderedContent("What do you do now?", "DivergingPath", "021-2", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("Eat and drink", "River", "", ""), new NodeRenderedContent("Return to notify your herd", "Heard", "", ""));/*Avatar eat or drink; walk*/



        var c121 = choice4.Item1.AddSingleNextNode(new NodeRenderedContent("You wake up your herd and start to run as fast as you could, trying to outrun the poachers' bullets. The poacher chase you but they are slow and tall meanwhile you're quick and small. In front there are 2 path.", "MultiplePoachers", "0121-1", "Idle"));
        var c122 = choice4.Item2.AddSingleNextNode(new NodeRenderedContent("You fight the poachers. Those giraffes headshot you and you die instantly.", "GiraffeWithShotgun", "0122-1", "Idle"));/*Avatar injured and die*/
        var c211 = choice5.Item1.AddSingleNextNode(new NodeRenderedContent("The night came and you got lost, unable to find your way back to the herd. You cannot protect yourself yet. You died of lion predation.", "Lion", "0211-1", "Idle"));
        var c212 = choice5.Item2.AddSingleNextNode(new NodeRenderedContent("Your herd decide to move to the new habitat you have found.", "River", "0212-1", "Idle"));/*A group of people?*/


        var choice6 = c121.AddSingleNextNode(new NodeRenderedContent("Where do you go?", "DivergingPath", "0121-2", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("A cave", "Cave1", "", ""), new NodeRenderedContent("Keep going", "Desert 1", "", ""));/*Avatar run*/

        var choice7 = c212.AddSingleNextNode(new NodeRenderedContent("What to do now?", "DivergingPath", "0212-2", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("Eat", "Leaves", "", ""), new NodeRenderedContent("Drink", "WaterFromRiver", "", ""));/*Avatar eat; drink*/


        var c1211 = choice6.Item1.AddSingleNextNode(new NodeRenderedContent("You found refuge in the cave because the giraffes couldn't enter due to their height. As you ventured deeper into the darkness, you heard the voices of fellow humans. Relief flooded over you as you discovered a human community shelter.", "CaveWithHumans", "01211-1", "Idle"))/*A group of people?*/
            .AddSingleNextNode(new NodeRenderedContent("The people in the shelter were kind and welcoming, providing a much-needed respite from the dangers outside. You waited until it was safe before returning to lead the rest of your herd to the shelter.", "Heard", "01211-2", "Idle"))/*A group of people?*/
            .AddSingleNextNode(new NodeRenderedContent("Finally, you and your herd could settle down and take a break.", "Heard", "01211-3", "Idle"))/*Avatar sit with eyes closed*/
            .AddSingleNextNode(new NodeRenderedContent("Though, there was a second when confusion passed through your mind. Somehow, you felt the poachers that day were intentionally leading you to enter this cave. But that could not be possible. Why would they do that?", "MultiplePoachers", "01211-4", "Idle"));/*Avatar sit with eyes closed*/
        var c1212 = choice6.Item2.AddSingleNextNode(new NodeRenderedContent("The giraffes shot your legs and you're injured. In front of you there are 2 path .", "DivergingPath", "01212-1", "Idle"));
        var c2121 = choice7.Item1.AddSingleNextNode(new NodeRenderedContent("You and your herd started to eat leaves from the trees.", "Leaves", "02121-1", "Idle"))/*Avatar eat*/
            .AddSingleNextNode(new NodeRenderedContent("In the first few days, everything was fine. Then, however, over-browsing from the same trees activated the trees defense mechanism and prompt increased production of tannins, which makes leaves bitter-tasting.", "Leaves", "02121-2", "Idle"));
        var c2122 = choice7.Item2.AddSingleNextNode(new NodeRenderedContent("You and your herd enjoyed the water from the river. A few moments later, you started to feel sick, so did most of people in your herd.", "DrinkingFromRiver", "02122-1", "Idle"))/*Avatar feeling uncomfortable*/
            .AddSingleNextNode(new NodeRenderedContent("Walking along the river with weak legs, you saw a vast expanse of cultivated land and a large factory emitting dark smoke into the sky.", "Wasteland", "02122-2", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("You died of polluted water.", "Wasteland", "02122-3", "Idle"));/*Avatar feeling uncomfortable*/

        var choice8 = c1212.AddSingleNextNode(new NodeRenderedContent("Which path will you choose?", "DivergingPath", "01212-2", "Idle"))
            .AddChoiceNodes(new NodeRenderedContent("Left", "", "", ""), new NodeRenderedContent("Right", "", "", ""));

        var choice9 = c2121.AddSingleNextNode(new NodeRenderedContent("What to do now?", "DivergingPath", "02121-3", "Idle"))
            .AddChoiceNodes(new NodeRenderedContent("Continue to eat the bitter leaves", "Leaves", "", ""), new NodeRenderedContent("Leave to find another habitat", "Desert 1", "", ""));


        var c12121 = choice8.Item1.AddSingleNextNode(new NodeRenderedContent("You got rid of the poachers, but you were also too weak to move anymore. You lost consciousness.", "", "012121-1", "Idle"))/*Avatar sit with eyes closed*/
            .AddSingleNextNode(new NodeRenderedContent("After you wake up, you realized you were in a giraffe's place. He has no bad intentions, though. He's a veterinarian that saved your life.", "Giraffe", "012121-2", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("After you were cured, he sent you to a zoo, where you spent the rest of your life. You have endless food and water. You do not need to worry about predators anymore. Every day a lot of giraffes, young and old, come to see you. You brought profit to the zoo. All these giraffes love you.", "Zoo", "012121-3", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("Once in a while, at midnight, you will think of your herd and savannah. \nIs what you have now worth giving up freedom? That is a question.", "Zoo", "012121-4", "Idle"));
        var c12122 = choice8.Item2.AddSingleNextNode(new NodeRenderedContent("Keep running right. You found a small hut where you can lay low for a bit.", "Cabin", "012122-1", "Idle"));
        var c21211 = choice9.Item1.AddSingleNextNode(new NodeRenderedContent("Increased production of tannins in these leaves inhibit your ability to digest food. Poor digestion causes you and your herd in poor health condition.", "Leaves", "021211-1", "Idle"))/*Avatar feeling uncomfortable*/
            .AddSingleNextNode(new NodeRenderedContent("One day, you died of lion predation, and the remaining members of your herd will continue their journey to search for a shelter.", "Lion", "021211-2", "Idle"));
        var c21212 = choice9.Item2.AddSingleNextNode(new NodeRenderedContent("You and your herd encountered poachers on the way.", "GiraffesInTrucks", "021212-1", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("While the wildlife trade is forbidden by law, many giraffes still would take the risk of hunting and poaching humans for the value of their skin and meat.", "MultiplePoachers", "021212-2", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("You tried to run, but the shortage of food and water left you with limited energy to do that. You died. ", "", "021212-3", "Idle"));/*Avatar die*/



        var choice10 = c21212.AddSingleNextNode(new NodeRenderedContent("What to do next?", "DivergingPath", "012122-2", "Idle"))/*Avatar think*/
            .AddChoiceNodes(new NodeRenderedContent("Go Left", "", "", ""), new NodeRenderedContent("Go Right", "", "", ""));/*Avatar turn left, turn right (?)*/

        var c121221 = choice10.Item1.AddSingleNextNode(new NodeRenderedContent("Stay low. you are surrounded by giraffes and they capture you.", "MultiplePoachers", "0121221-1", "Idle"));
        var c121222 = choice10.Item2.AddSingleNextNode(new NodeRenderedContent("Lay low, you survived.", "Cabin", "0121222-1", "Idle"));

        c121222.AddSingleNextNode(new NodeRenderedContent("You barely escaped poachers, but your fellow humans weren't as lucky. Their lives were cut short by giraffe greed. Over time, more and more humans disappeared, and the once-vibrant savannah became barren.", "DamagedHabitat", "0121222-2", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("A few years later, the giraffes started a conservation program to protect humans. You journeyed to the protected area, weak and tired, but excited to be reunited with fellow humans. You found belonging and hope for the first time in years.", "Savana", "0121222-3", "Idle"))
            .AddSingleNextNode(new NodeRenderedContent("Despite the conservationists' best efforts, you passed away. Your legacy lived on through those who survived and found refuge.", "Savana", "0121222-4", "Idle"))/*Avatar sit with eyes closed*/
            .AddSingleNextNode(new NodeRenderedContent("True Ending: No Regrets", "Savana", "", "Idle"));

    }

}
