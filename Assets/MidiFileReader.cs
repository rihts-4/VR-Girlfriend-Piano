using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;

public class MidiFileReader : MonoBehaviour
{
    public GameObject notePrefab; // this is the prefab for falling notes
    public string midiFilePath;   // path to the midi file, make sure its correct lol
    public float noteSpeed = 20f; // how fast the notes fall, can change in the inspector
    public float spawnHeight = 10f; // height where notes spawn, also adjustable in inspector
    public float bpm = 90f; // bpm for the song, might need to change for diff songs
    public AudioSource audioSource; // the audio source that plays the note sound

    private Dictionary<int, Transform> pianoKeys = new Dictionary<int, Transform>();
    private TempoMap tempoMap;

    void Start()
    {
        // look for piano keys with names like Key_21 to Key_108
        for (int i = 21; i <= 108; i++)
        {
            string keyName = "Key_" + i;
            Transform keyTransform = GameObject.Find(keyName)?.transform;

            if (keyTransform != null)
            {
                pianoKeys.Add(i, keyTransform); // adding midi note numbers to key transforms, kinda important
            }
            else
            {
                Debug.LogWarning("key " + keyName + " not found... check ur scene");
            }
        }

        // load midi file into the script
        LoadMidiFile(midiFilePath);
    }

    void LoadMidiFile(string filePath)
    {
        // read the midi file and get its tempo map
        MidiFile midiFile = MidiFile.Read(filePath);
        tempoMap = midiFile.GetTempoMap();

        // get all the notes from the midi file
        var notes = midiFile.GetNotes();

        // start spawning the notes from the midi
        StartCoroutine(SpawnNotesFromMidi(notes));
    }

    IEnumerator SpawnNotesFromMidi(IEnumerable<Note> notes)
    {
        double lastNoteTime = -1;
        List<Note> currentTimeGroup = new List<Note>(); // group of notes played at the same time

        foreach (var midiNote in notes)
        {
            // time when the note should play
            var noteTime = midiNote.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;

            // length of the note (like quarter note or whatever)
            var noteLength = midiNote.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;

            // check if notes are played at the same time (grouping them)
            if (lastNoteTime == -1 || Mathf.Abs((float)(noteTime - lastNoteTime)) < 0.01f)
            {
                currentTimeGroup.Add(midiNote); // group notes that are played at the same time
            }
            else
            {
                // play the notes in the group
                SpawnNoteGroup(currentTimeGroup);

                // wait before playing the next note group
                float waitTime = Mathf.Max(0.01f, (float)(noteTime - lastNoteTime)); // make sure there is some delay
                yield return new WaitForSeconds(waitTime);

                // clear the current group and add the new note
                currentTimeGroup.Clear();
                currentTimeGroup.Add(midiNote);
            }

            lastNoteTime = noteTime;
        }

        // handle any leftover notes that didn't get played yet
        if (currentTimeGroup.Count > 0)
        {
            SpawnNoteGroup(currentTimeGroup);
        }
    }

    void SpawnNoteGroup(List<Note> noteGroup)
    {
        foreach (var midiNote in noteGroup)
        {
            int midiNoteNumber = midiNote.NoteNumber;
            double noteLength = midiNote.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds; // length of the note

            // check if the piano key exists in the scene
            if (pianoKeys.ContainsKey(midiNoteNumber))
            {
                // position the note above the corresponding piano key
                Vector3 spawnPosition = pianoKeys[midiNoteNumber].position;
                spawnPosition.y += spawnHeight; // move the note up by spawnHeight

                // create the note object in the scene
                GameObject newNote = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

                // set the falling speed for the note
                FallingNote fallingNote = newNote.GetComponent<FallingNote>();
                if (fallingNote != null)
                {
                    fallingNote.SetFallSpeed(noteSpeed); // the speed the note falls at
                }
            }

            // play the note sound with adjusted pitch and duration
            PlayAudioForNoteAndSpawn(midiNoteNumber, noteLength);
        }
    }

    void PlayAudioForNoteAndSpawn(int midiNoteNumber, double noteDuration)
    {
        // create an audio source to play the note sound
        GameObject noteSoundObject = new GameObject("Note_" + midiNoteNumber);
        AudioSource noteAudioSource = noteSoundObject.AddComponent<AudioSource>();

        // assign the audio clip (should be the c4 sound)
        noteAudioSource.clip = audioSource.clip;

        // adjust the pitch to match the midi note (based on middle c)
        float pitch = Mathf.Pow(2f, (midiNoteNumber - 60) / 12f); // pitch is relative to midi note 60 (c4)
        noteAudioSource.pitch = pitch;

        // start the volume at 0 for fade-in
        noteAudioSource.volume = 0f;

        // play the note
        noteAudioSource.Play();

        // apply fade-in so it doesn't start too loud
        StartCoroutine(FadeIn(noteAudioSource, 0.05f)); // fade-in over 0.05 seconds

        // calculate how long the note should play or the max length of the clip
        float notePlayDuration = Mathf.Min((float)noteDuration, noteAudioSource.clip.length / noteAudioSource.pitch);

        // stop the note with a fade-out after it finishes playing
        StartCoroutine(StopNoteWithFadeOut(noteAudioSource, notePlayDuration, 0.05f)); // fade-out over 0.05 seconds
    }

    IEnumerator FadeIn(AudioSource audioSource, float fadeTime)
    {
        // gradually increase volume to full over fadeTime
        float startVolume = 0f;

        while (audioSource.volume < 1f)
        {
            audioSource.volume += Time.deltaTime / fadeTime; // slowly increase volume
            yield return null;
        }

        audioSource.volume = 1f; // make sure the volume is at 100%
    }

    IEnumerator StopNoteWithFadeOut(AudioSource audioSource, float notePlayDuration, float fadeOutTime)
    {
        // wait for the note to play for its full duration
        yield return new WaitForSeconds(notePlayDuration);

        // gradually decrease volume for fade-out
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0f)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeOutTime; // slowly lower volume
            yield return null;
        }

        // stop the note and destroy the game object after fade-out
        audioSource.Stop();
        Destroy(audioSource.gameObject);
    }
}
