using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingNote : MonoBehaviour
{
    // no need for public fallSpeed here anymore since MidiFileReader.cs will control it
    private float fallSpeed = 20.0f; // default fall speed if none is set (just in case)

    // method to set the fall speed from MidiFileReader.cs
    public void SetFallSpeed(float speed)
    {
        fallSpeed = speed; // update the fall speed
    }

    void Update()
    {
        // make the note fall downwards using the speed set by MidiFileReader.cs
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime); // moves note down every frame

        // destroy the note if it falls below a certain point (below the piano)
        if (transform.position.y < -10) // adjust this value based on your scene layout
        {
            Destroy(gameObject); // get rid of the note after it goes offscreen
        }
    }
}
