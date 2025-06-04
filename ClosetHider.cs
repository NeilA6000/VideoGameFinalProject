// This is my "Closet Hider" script. For context, I have this habit of putting code that SHOULD be separated into different scripts into one script. So apologies for that!
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ClosetHider : MonoBehaviour
{
    // These variables are all essential for the game features.
    string HidingSpotType = "None"; // Tracks where the player is hiding

    public FirstPersonController firstPersonControllerScript; // Needed to freeze player movement
    public float reenableDelay = 5f;

    // Audio settings
    public AudioSource[] audioSources;
    public AudioSource caughtSound;

    // Time + UI
    public Text timeText;
    public Text roundsText;

    // Flashlight flicker
    public Light flickerLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 2.0f;
    public float flickerSpeed = 0.05f;

    // Camera settings
    public Camera mainCamera;
    public float shakeAmplitude = 0.05f;
    public float shakeFrequency = 1.5f;

    // Heartbeat effects
    public RawImage heartbeatVignette;
    public float heartbeatFrequency = 1.5f;
    public float minHeartbeatAlpha = 0.2f;
    public float maxHeartbeatAlpha = 0.7f;

    // Monster settings
    public MonsterController[] monsters;
    public float faceMonsterDuration = 3f;

    // Flicker/visual stuff
    private float flickerTimer;
    private float shakeTimer;
    private float heartbeatTimer;

    // Timer and round stuff
    public float targetTime = 60.0f;
    private List<string> activeHidingSpots = new List<string>();
    private bool timerTriggered = false;

    private int round = 0;
    private const int maxRounds = 3;

    private string chosenSpot = "";
    public bool isIntermission = false;

    private Vector3 originalCamPos;
    private Quaternion originalCamRotation;
    // Quaternion is a type of math used for rotation in 3D. Honestly, I'm in 8th grade so this stuff was a bit of a struggle for me, I just used what Unity docs/forums said.

    private bool isFacingMonster = false;
    private float faceMonsterTimer = 0f;

    public Text currentHidingSpotText;

    // Track usage of hiding spots
    private Dictionary<string, int> hidingSpotUsage = new Dictionary<string, int>()
    {
        // Tracks how often each spot is picked to reduce reuse
        { "Closet", 0 },
        { "Bathroom", 0 },
        { "Kitchen", 0 },
        { "Bedroom", 0 }
    };

    void Start()
    {
        UpdateRoundsText();
        if (mainCamera != null)
        {
            // To lock the camera position + rotation
            originalCamPos = mainCamera.transform.localPosition;
            originalCamRotation = mainCamera.transform.localRotation;
        }

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
        if (IsHidingSpot(other.tag))
        {
            activeHidingSpots.Remove(other.tag);
            HidingSpotType = activeHidingSpots.Count == 0 ? "None" : activeHidingSpots[activeHidingSpots.Count - 1];
        }
    }

    private bool IsHidingSpot(string tag)
    {
        return tag == "Closet" || tag == "Bathroom" || tag == "Kitchen" || tag == "Bedroom";
    }

    private System.Collections.IEnumerator ReenableMovementAfterDelay()
    {
        yield return new WaitForSeconds(reenableDelay);
        firstPersonControllerScript.playerCanMove = true;
    }

    void Update()
    {
        if (currentHidingSpotText != null)
        {
            currentHidingSpotText.text = "Current Hiding Spot: " + HidingSpotType;
        }

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

        if (isIntermission)
        {
            HandleLightFlicker();
            HandleCameraShake();
            HandleHeartbeatVignette();
        }
        else
        {
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

    private void HandleCameraShake()
    {
        if (mainCamera == null) return;

        shakeTimer += Time.deltaTime * shakeFrequency * Mathf.PI * 2;
        float offsetY = Mathf.Sin(shakeTimer) * shakeAmplitude;
        mainCamera.transform.localPosition = originalCamPos + new Vector3(0, offsetY, 0);
    }

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

    private System.Collections.IEnumerator HandleTimerEndSequence()
    {
        Debug.Log($"Intermission started. Player hiding spot: {HidingSpotType}");

        if (HidingSpotType == "None")
        {
            chosenSpot = "None";
        }
        else
        {
            // Randomly pick spot, but less likely if it's been used a lot
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

        Debug.Log($"Chosen hiding spot is: {chosenSpot}");

        firstPersonControllerScript.playerCanMove = false;
        isIntermission = true;

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
                SceneManager.LoadScene(3);
            }
            else
            {
                firstPersonControllerScript.playerCanMove = true;
                targetTime = 15.0f;
                timerTriggered = false;
            }
        }
    }

    private void UpdateRoundsText()
    {
        if (roundsText != null)
            roundsText.text = "Rounds Remaining: " + (maxRounds - round).ToString();
    }

    public string GetHidingSpotType()
    {
        return HidingSpotType;
    }

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

            Invoke("LoadTitleScreen", 1.5f);

            Vector3 monsterHeadPos = closestMonster.transform.position + Vector3.up * 4f;
            Vector3 dir = (monsterHeadPos - mainCamera.transform.position).normalized;

            Quaternion lookRotation = Quaternion.LookRotation(dir);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, lookRotation, Time.deltaTime * 3f);
        }
        else
        {
            Debug.LogWarning("No monster found for chosenSpot: " + chosenSpot);
        }
    }

    private void LoadTitleScreen()
    {
        SceneManager.LoadScene(0);
    }
}
