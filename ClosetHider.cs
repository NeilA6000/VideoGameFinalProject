//For context, I have this insanely bad habit of placing a lot of code that should be seperated into other scripts into one big script so apologies...
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ClosetHider : MonoBehaviour
{
    // Keeps track of where I'm hiding right now
    string HidingSpotType = "None";

    public FirstPersonController firstPersonControllerScript; // Used to stop the player from moving when hiding
    public float reenableDelay = 5f; // How long till you can move again

    // Sounds for stuff
    public AudioSource[] audioSources; // Plays creepy sounds
    public AudioSource caughtSound;    // Sound when you get caught (oops)

    // Text stuff on screen
    public Text timeText; // Shows the timer
    public Text roundsText; // Shows how many rounds are left

    // Flickering light for spooky vibes
    public Light flickerLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 2.0f;
    public float flickerSpeed = 0.05f;

    // Camera shake (like in horror movies)
    public Camera mainCamera;
    public float shakeAmplitude = 0.05f;
    public float shakeFrequency = 1.5f;

    // Heartbeat effect on screen (makes it feel tense!)
    public RawImage heartbeatVignette;
    public float heartbeatFrequency = 1.5f;
    public float minHeartbeatAlpha = 0.2f;
    public float maxHeartbeatAlpha = 0.7f;

    // Monsters that chase you (yikes)
    public MonsterController[] monsters;
    public float faceMonsterDuration = 3f; // How long you stare at the monster if caught

    // Stuff used to make the visual effects work
    private float flickerTimer;
    private float shakeTimer;
    private float heartbeatTimer;

    // Timer and hiding round stuff
    public float targetTime = 60.0f;
    private List<string> activeHidingSpots = new List<string>();
    private bool timerTriggered = false;

    private int round = 0;
    private const int maxRounds = 3; // How many rounds before the game ends

    private string chosenSpot = ""; // Random spot picked by the game
    public bool isIntermission = false; // If the game is in between rounds

    private Vector3 originalCamPos;
    private Quaternion originalCamRotation; // Don't ask, this rotation stuff is confusing

    private bool isFacingMonster = false;
    private float faceMonsterTimer = 0f;

    public Text currentHidingSpotText;

    // Keeps track of which spots you used so you don't just hide in one over and over
    private Dictionary<string, int> hidingSpotUsage = new Dictionary<string, int>()
    {
        { "Closet", 0 },
        { "Bathroom", 0 },
        { "Kitchen", 0 },
        { "Bedroom", 0 }
    };

    void Start()
    {
        UpdateRoundsText();

        // Save camera's starting position and rotation
        if (mainCamera != null)
        {
            originalCamPos = mainCamera.transform.localPosition;
            originalCamRotation = mainCamera.transform.localRotation;
        }

        // Start with no heartbeat effect showing
        if (heartbeatVignette != null)
        {
            Color c = heartbeatVignette.color;
            c.a = 0f;
            heartbeatVignette.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (firstPersonControllerScript == null)
            return;

        // If you enter a hiding spot, freeze movement and update the hiding type
        if (IsHidingSpot(other.tag))
        {
            if (!activeHidingSpots.Contains(other.tag))
                activeHidingSpots.Add(other.tag);

            firstPersonControllerScript.playerCanMove = false;
            StartCoroutine(ReenableMovementAfterDelay());
            HidingSpotType = other.tag;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // When you leave a hiding spot, reset hiding info
        if (IsHidingSpot(other.tag))
        {
            activeHidingSpots.Remove(other.tag);
            HidingSpotType = activeHidingSpots.Count == 0 ? "None" : activeHidingSpots[activeHidingSpots.Count - 1];
        }
    }

    // Checks if a tag is a hiding spot
    private bool IsHidingSpot(string tag)
    {
        return tag == "Closet" || tag == "Bathroom" || tag == "Kitchen" || tag == "Bedroom";
    }

    // Wait before letting the player move again
    private System.Collections.IEnumerator ReenableMovementAfterDelay()
    {
        yield return new WaitForSeconds(reenableDelay);
        firstPersonControllerScript.playerCanMove = true;
    }

    void Update()
    {
        // Show current hiding spot on screen
        if (currentHidingSpotText != null)
        {
            currentHidingSpotText.text = "Current Hiding Spot: " + HidingSpotType;
        }

        // If you're caught, make you stare at the monster for a bit
        if (isFacingMonster)
        {
            faceMonsterTimer += Time.deltaTime;
            FaceClosestMonsterForChosenRoom();

            if (faceMonsterTimer >= faceMonsterDuration)
            {
                isFacingMonster = false;
                firstPersonControllerScript.playerCanMove = true;
                targetTime = 15.0f;
                timerTriggered = false;
            }

            return;
        }

        // While waiting (intermission), do all the scary effects
        if (isIntermission)
        {
            HandleLightFlicker();
            HandleCameraShake();
            HandleHeartbeatVignette();
        }
        else
        {
            // If not intermission, reset visuals
            if (flickerLight != null)
                flickerLight.intensity = maxIntensity;

            if (mainCamera != null)
                mainCamera.transform.localPosition = originalCamPos;

            if (heartbeatVignette != null)
            {
                Color c = heartbeatVignette.color;
                c.a = 0f;
                heartbeatVignette.color = c;
            }
        }

        // Handle timer countdown
        if (targetTime > 0.0f && !timerTriggered)
        {
            targetTime -= Time.deltaTime;
            if (timeText != null)
                timeText.text = Mathf.Round(targetTime).ToString();

            if (targetTime <= 0.0f)
            {
                timerTriggered = true;
                StartCoroutine(HandleTimerEndSequence());
            }
        }
    }

    // Makes the flashlight flicker randomly
    private void HandleLightFlicker()
    {
        if (flickerLight != null)
        {
            flickerTimer += Time.deltaTime;
            if (flickerTimer >= flickerSpeed)
            {
                flickerLight.intensity = Random.Range(minIntensity, maxIntensity);
                flickerTimer = 0f;
            }
        }
    }

    // Makes the camera shake up and down
    private void HandleCameraShake()
    {
        if (mainCamera == null) return;

        shakeTimer += Time.deltaTime * shakeFrequency * Mathf.PI * 2;
        float offsetY = Mathf.Sin(shakeTimer) * shakeAmplitude;
        mainCamera.transform.localPosition = originalCamPos + new Vector3(0, offsetY, 0);
    }

    // Changes heartbeat effect to pulse faster/slower
    private void HandleHeartbeatVignette()
    {
        if (heartbeatVignette == null || !isIntermission) return;

        heartbeatTimer += Time.deltaTime * heartbeatFrequency * Mathf.PI * 2;
        float normalizedPulse = (Mathf.Sin(heartbeatTimer) + 1f) / 2f;
        float alpha = Mathf.Lerp(minHeartbeatAlpha, maxHeartbeatAlpha, normalizedPulse);

        Color c = heartbeatVignette.color;
        c.a = alpha;
        heartbeatVignette.color = c;
    }

    // When the timer ends, figure out if you're safe or caught
    private System.Collections.IEnumerator HandleTimerEndSequence()
    {
        // If you didn't hide, too bad
        if (HidingSpotType == "None")
        {
            chosenSpot = "None";
        }
        else
        {
            // Pick a random spot, but less likely to reuse ones you've already picked
            float totalWeight = 0f;
            Dictionary<string, float> weights = new Dictionary<string, float>();

            foreach (string spot in hidingSpotUsage.Keys)
            {
                float weight = 1f / (1 + hidingSpotUsage[spot]);
                weights[spot] = weight;
                totalWeight += weight;
            }

            float randomValue = Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var pair in weights)
            {
                cumulative += pair.Value;
                if (randomValue <= cumulative)
                {
                    chosenSpot = pair.Key;
                    hidingSpotUsage[chosenSpot]++;
                    break;
                }
            }
        }

        firstPersonControllerScript.playerCanMove = false;
        isIntermission = true;

        // Play spooky sounds
        if (audioSources != null && audioSources.Length > 0)
        {
            foreach (var source in audioSources)
            {
                if (source != null)
                    source.Play();
            }
        }

        yield return new WaitForSeconds(5f);
        isIntermission = false;

        // If you picked the same spot as the monster, uh oh
        if (HidingSpotType == chosenSpot || HidingSpotType == "None")
        {
            if (caughtSound != null)
                caughtSound.Play();

            isFacingMonster = true;
            faceMonsterTimer = 0f;
        }
        else
        {
            round++;
            UpdateRoundsText();

            if (round >= maxRounds)
            {
                SceneManager.LoadScene(3); // End game screen
            }
            else
            {
                // Keep playing
                firstPersonControllerScript.playerCanMove = true;
                targetTime = 15.0f;
                timerTriggered = false;
            }
        }
    }

    // Updates the round counter on the UI
    private void UpdateRoundsText()
    {
        if (roundsText != null)
            roundsText.text = "Rounds Remaining: " + (maxRounds - round).ToString();
    }

    // So other scripts can know where you're hiding
    public string GetHidingSpotType()
    {
        return HidingSpotType;
    }

    // Makes you look at the monster if you're caught
    private void FaceClosestMonsterForChosenRoom()
    {
        if (mainCamera == null || monsters == null) return;

        MonsterController closestMonster = null;
        float closestDistance = Mathf.Infinity;

        foreach (var monster in monsters)
        {
            if (monster != null && monster.roomTag == chosenSpot)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, monster.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestMonster = monster;
                }
            }
            else
            {
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    monster.gameObject.SetActive(false);
                }
            }
        }

        if (closestMonster != null)
        {
            if (!closestMonster.gameObject.activeInHierarchy)
                closestMonster.gameObject.SetActive(true);

            firstPersonControllerScript.enabled = false;

            Invoke("LoadTitleScreen", 1.5f); // After scaring you, go back to menu

            Vector3 monsterHeadPos = closestMonster.transform.position + Vector3.up * 4f;
            Vector3 dir = (monsterHeadPos - mainCamera.transform.position).normalized;

            Quaternion lookRotation = Quaternion.LookRotation(dir);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, lookRotation, Time.deltaTime * 3f);
        }
    }

    // Loads the main menu again
    private void LoadTitleScreen()
    {
        SceneManager.LoadScene(0);
    }
}
