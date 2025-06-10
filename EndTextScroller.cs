using UnityEngine;
//This is for the text scrolling end scene that you saw at the end of thatt video.
public class MoveForwardThenReturnLoop : MonoBehaviour
{
    public float moveSpeed = 1f;       // How fast it moves forward (units per second)
    public float duration = 60f;       // How long it keeps moving forward before turning around

    private Vector3 startPosition;     // Where it started (so it knows where to go back to)
    private bool returning = false;    // Are we on our way back yet?
    private float timer = 0f;          // How long we've been moving forward

    void Start()
    {
        // Remember where we started
        startPosition = transform.position;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!returning && timer < duration)
        {
            // Still in the forward-moving phase, so just keep going
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
        else if (!returning)
        {
            // Time's up! Turn around.
            returning = true;
        }

        if (returning)
        {
            // Head back to where we started
            transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);

            // If we’re basically back, restart the loop
            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                returning = false;
                timer = 0f;
            }
        }
    }
}
