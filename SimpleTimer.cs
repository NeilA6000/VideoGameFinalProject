using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimpleTimer : MonoBehaviour
{
    public float targetTime = 60.0f; // how much time before something happens
    public Text timeText; // the timer thing on the screen

    [Header("Reference to ClosetHider Script")]
    public ClosetHider closetHider; // drag the thing with ClosetHider script into here in the inspector

    private bool timerTriggered = false; // so it doesn't trigger like 100 times

    void Update()
    {
        // if time is still ticking and nothing triggered yet
        if (targetTime > 0.0f && !timerTriggered)
        {
            targetTime -= Time.deltaTime; // tick tick tick
            timeText.text = Mathf.Round(targetTime).ToString(); // show the time (rounded cause decimals are ugly)

            if (targetTime <= 0.0f)
            {
                timerTriggered = true; // so we don’t do this more than once
                timerEnded(); // okay now we do the thing
            }
        }
    }

    void timerEnded()
    {
        // uh oh you forgot to assign the ClosetHider script
        if (closetHider == null)
        {
            return; // can’t do anything if we don’t know where player is hiding
        }

        // make a list of spots someone could hide
        string[] hidingSpots = { "Closet", "Bathroom", "Kitchen" };
        string chosenSpot = hidingSpots[Random.Range(0, hidingSpots.Length)]; // pick one randomly

        // if player guessed the right spot, restart the game or go to next scene
        if (closetHider.GetHidingSpotType() == chosenSpot)
        {
            SceneManager.LoadScene(0); // boom restart the scene
        }
        else
        {
            // nothing happens if they were wrong (for now)
        }
    }
}
