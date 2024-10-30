using System.Collections;
using UnityEngine;

public class FallingNote : MonoBehaviour
{
    private float fallSpeed = 20.0f; // Default fall speed
    public double noteLength; // Store the note's length for playback

    public void SetFallSpeed(float speed)
    {
        fallSpeed = speed;
    }

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Destroy note when it reaches below a threshold and play its corresponding sound
        if (transform.position.y < -2)
        {
            int midiNoteNumber = int.Parse(gameObject.name.Replace("Note_", ""));
            FindObjectOfType<MidiFileReader>().PlayAudioForNoteOnDestruction(midiNoteNumber, noteLength);
            Destroy(gameObject);
        }
    }
}