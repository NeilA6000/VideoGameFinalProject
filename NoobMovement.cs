using UnityEngine;

public class NoobMovement : MonoBehaviour
{
    public float walkSpeed = 5f; // how fast you walk
    public float runSpeed = 10f; // how fast you go when you press shift
    public float jumpForce = 5f; // how high you jump
    public Transform groundCheck; // thingy to check if you're on the floor
    public float groundDistance = 0.4f; // how far down to check
    public LayerMask groundMask; // what counts as the floor

    private CharacterController controller;
    private Vector3 velocity; // gravity stuff
    private bool isGrounded; // are you on the ground?
    private float speed; // current speed
    private Camera mainCam; // the camera thing
    private float normalFOV; // regular camera FOV
    public float sprintFOV = 80f; // zoom-out FOV when sprinting

    void Start()
    {
        controller = GetComponent<CharacterController>(); // get the movement box
        mainCam = Camera.main; // get the main camera
        normalFOV = mainCam.fieldOfView; // save the default FOV
    }

    void Update()
    {
        // check if you're touching the ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // if you're on the ground and falling, stop falling
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // tiny push down to keep you stuck to the ground
        }

        // get WASD input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // figure out movement direction
        Vector3 move = transform.right * x + transform.forward * z;

        // are you pressing shift? go zoom!
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = runSpeed;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, sprintFOV, 10f * Time.deltaTime); // FOV go brr
        }
        else
        {
            speed = walkSpeed;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, normalFOV, 10f * Time.deltaTime); // FOV normal again
        }

        controller.Move(move * speed * Time.deltaTime); // actually move the player

        // press space to jump if you're on the ground
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y); // I got this from a YouTube tutorial ngl
        }

        // apply gravity so you fall and stuff
        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime); // now you fall
    }
}
