using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab; // this is the prefab for the notes that fall
    public Transform[] pianoKeys; // array to hold all the piano keys in the scene
    public float spawnInterval = 1.0f; // time between spawning notes, adjustable in inspector

    void Start()
    {
        // start the note spawning process when the game starts
        StartCoroutine(SpawnNotes()); // calls the function to spawn notes
    }

    IEnumerator SpawnNotes()
    {
        while (true) // keeps spawning notes forever (or until u stop the game lol)
        {
            // pick a random key from the pianoKeys array
            int randomKeyIndex = Random.Range(0, pianoKeys.Length); // random key for testing, will pick any key

            // spawn the note at the random key's position
            SpawnNote(randomKeyIndex);

            // wait before spawning the next note, set by spawnInterval
            yield return new WaitForSeconds(spawnInterval); // pauses for the interval before next note
        }
    }

    void SpawnNote(int keyIndex)
    {
        // get the position of the piano key we picked
        Vector3 spawnPosition = pianoKeys[keyIndex].position;

        // adjust the Y position to make the note spawn above the piano
        spawnPosition.y += 50f; // you can change this value to control how high the note spawns

        // create the note at the new position
        Instantiate(notePrefab, spawnPosition, Quaternion.identity); // spawns the note at the adjusted position
    }
}
