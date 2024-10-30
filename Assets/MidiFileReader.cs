using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;

public class MidiFileReader : MonoBehaviour
{
    public GameObject notePrefab; // Prefab for falling notes
    public string midiFilePath;   // Path to your .mid file
    public float noteSpeed = 20f; // Speed at which the notes fall
    public float spawnHeight = 10f; // Adjustable Y-height for notes when spawning
    public float bpm = 90f; // Beats per minute for tempo adjustment
    public AudioSource audioSource; // Audio source for playing note sounds

    private Dictionary<int, Transform> pianoKeys = new Dictionary<int, Transform>();
    private TempoMap tempoMap;

    void Start()
    {
        // Automatically map the piano keys in the scene by name (Key_21 to Key_108)
        for (int i = 21; i <= 108; i++)
        {
            string keyName = "Key_" + i;
            Transform keyTransform = GameObject.Find(keyName)?.transform;

            if (keyTransform != null)
            {
                pianoKeys.Add(i, keyTransform);
            }
            else
            {
                Debug.LogWarning("Key " + keyName + " not found in the scene.");
            }
        }

        // Load the MIDI file
        LoadMidiFile(midiFilePath);
    }

    void LoadMidiFile(string filePath)
    {
        // Read the MIDI file and get its tempo map
        MidiFile midiFile = MidiFile.Read(filePath);
        tempoMap = midiFile.GetTempoMap();

        // Get the notes from the MIDI file
        var notes = midiFile.GetNotes();

        // Start spawning the notes from the MIDI data
        StartCoroutine(SpawnNotesFromMidi(notes));
    }

    IEnumerator SpawnNotesFromMidi(IEnumerable<Note> notes)
    {
        double lastNoteTime = -1;
        List<Note> currentTimeGroup = new List<Note>();

        foreach (var midiNote in notes)
        {
            var noteTime = midiNote.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
            var noteLength = midiNote.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;

            if (lastNoteTime == -1 || Mathf.Abs((float)(noteTime - lastNoteTime)) < 0.01f)
            {
                currentTimeGroup.Add(midiNote);
            }
            else
            {
                SpawnNoteGroup(currentTimeGroup);

                float waitTime = Mathf.Max(0.01f, (float)(noteTime - lastNoteTime));
                yield return new WaitForSeconds(waitTime);

                currentTimeGroup.Clear();
                currentTimeGroup.Add(midiNote);
            }

            lastNoteTime = noteTime;
        }

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
            double noteLength = midiNote.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;

            if (pianoKeys.ContainsKey(midiNoteNumber))
            {
                Vector3 spawnPosition = pianoKeys[midiNoteNumber].position;
                spawnPosition.y += spawnHeight;

                GameObject newNote = Instantiate(notePrefab, spawnPosition, Quaternion.identity);
                newNote.name = "Note_" + midiNoteNumber;

                FallingNote fallingNote = newNote.GetComponent<FallingNote>();
                if (fallingNote != null)
                {
                    fallingNote.SetFallSpeed(noteSpeed);
                    fallingNote.noteLength = noteLength; // Pass note duration to FallingNote
                }
            }
        }
    }

    // Method to play the note when it reaches the bottom of the screen
    public void PlayAudioForNoteOnDestruction(int midiNoteNumber, double noteDuration)
    {
        // Create a new GameObject with its own AudioSource to play the sound
        GameObject noteSoundObject = new GameObject("Note_" + midiNoteNumber);
        AudioSource noteAudioSource = noteSoundObject.AddComponent<AudioSource>();

        noteAudioSource.clip = audioSource.clip;

        // Adjust the pitch based on the MIDI note number relative to C4 (MIDI note 60)
        float pitch = Mathf.Pow(2f, (midiNoteNumber - 60) / 12f);
        noteAudioSource.pitch = pitch;

        // Play the note with fade-in
        noteAudioSource.volume = 0f;
        noteAudioSource.Play();
        StartCoroutine(FadeIn(noteAudioSource, 0.05f));

        // Calculate note duration or max clip length
        float playDuration = Mathf.Min((float)noteDuration, noteAudioSource.clip.length / noteAudioSource.pitch);

        // Stop the note with fade-out after it finishes
        StartCoroutine(StopNoteWithFadeOut(noteAudioSource, playDuration, 0.05f));
    }

    IEnumerator FadeIn(AudioSource audioSource, float fadeTime)
    {
        float startVolume = 0f;

        while (audioSource.volume < 1f)
        {
            audioSource.volume += Time.deltaTime / fadeTime;
            yield return null;
        }

        audioSource.volume = 1f;
    }

    IEnumerator StopNoteWithFadeOut(AudioSource audioSource, float playDuration, float fadeOutTime)
    {
        yield return new WaitForSeconds(playDuration);

        float startVolume = audioSource.volume;

        while (audioSource.volume > 0f)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeOutTime;
            yield return null;
        }

        audioSource.Stop();
        Destroy(audioSource.gameObject);
    }
}