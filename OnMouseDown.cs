using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    // this happens when the game starts (we're not using it but Unity wants it here)
    void Start()
    {
        // nothing here but it’s chill
    }

    // this runs every frame like forever
    void Update()
    {
        // if you press space, boom, load scene 1
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(1); // take the player to the main scene with the monster as seen in the vid
        }
    }

    // if you click on the object this is attached to
    public void OnMouseDown()
    {
        SceneManager.LoadScene(1); // also load scene 1 because accessiblity is key
    }
}
