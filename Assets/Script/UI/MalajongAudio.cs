using UnityEngine;
using System.Collections.Generic;

public class MalajongAudio : MonoBehaviour
{
    public static MalajongAudio Instance { get; private set; }

    [Range(0f, 1f)] public float MasterVolume = 0.8f;
    [Range(0f, 1f)] public float SfxVolume = 1.0f;

    private AudioSource audioSource;
    private Dictionary<string, AudioClip> proceduralClips = new Dictionary<string, AudioClip>();

    private const int SampleRate = 44100;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        GenerateAllProceduralClips();
    }

    private void GenerateAllProceduralClips()
    {
        // 1. Tile Selects with ascending pitch (indices 0 to 5)
        for (int i = 0; i <= 6; i++)
        {
            float baseFreq = 520f * Mathf.Pow(1.08f, i);
            proceduralClips[$"TileSelect_{i}"] = CreateTileClickClip($"TileSelect_{i}", baseFreq, 0.055f);
        }

        proceduralClips["TileDeselect"] = CreateTileClickClip("TileDeselect", 380f, 0.045f);
        proceduralClips["TileHover"] = CreateSoftTickClip("TileHover", 880f, 0.02f);
        proceduralClips["ChipTick"] = CreateChipTickClip("ChipTick");
        proceduralClips["MultPop"] = CreateMultPopClip("MultPop");
        proceduralClips["ScoreCrunch"] = CreateBassImpactClip("ScoreCrunch");
        proceduralClips["CashChime"] = CreateCashChimeClip("CashChime");
        proceduralClips["RoundWin"] = CreateFanfareClip("RoundWin");
        proceduralClips["GameOver"] = CreateGameOverClip("GameOver");
    }

    public void PlayTileSelect(int selectIndex = 0)
    {
        int clampedIndex = Mathf.Clamp(selectIndex, 0, 6);
        PlayClip($"TileSelect_{clampedIndex}", 0.7f);
    }

    public void PlayTileDeselect()
    {
        PlayClip("TileDeselect", 0.5f);
    }

    public void PlayTileHover()
    {
        PlayClip("TileHover", 0.25f);
    }

    public void PlayScoreChipTick()
    {
        PlayClip("ChipTick", 0.85f);
    }

    public void PlayMultPop()
    {
        PlayClip("MultPop", 0.9f);
    }

    public void PlayScoreCrunchSlam()
    {
        PlayClip("ScoreCrunch", 1.0f);
    }

    public void PlayCashChime()
    {
        PlayClip("CashChime", 0.9f);
    }

    public void PlayRoundWin()
    {
        PlayClip("RoundWin", 1.0f);
    }

    public void PlayGameOver()
    {
        PlayClip("GameOver", 0.9f);
    }

    private void PlayClip(string key, float volumeScale = 1f)
    {
        if (proceduralClips.TryGetValue(key, out AudioClip clip) && clip != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip, volumeScale * MasterVolume * SfxVolume);
            }
        }
    }

    // --- Procedural Audio Synthesizer Functions ---

    private AudioClip CreateTileClickClip(string name, float frequency, float duration)
    {
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / totalSamples;
            
            // Decaying envelope
            float env = Mathf.Exp(-progress * 22f);
            
            // Wood block body tone + transient snap
            float freqSweep = frequency * Mathf.Exp(-progress * 15f) + frequency * 0.7f;
            float wave = Mathf.Sin(2f * Mathf.PI * freqSweep * t);
            float harmonic = 0.35f * Mathf.Sin(4f * Mathf.PI * freqSweep * t);
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-progress * 50f) * 0.4f;

            samples[i] = (wave + harmonic + noise) * env;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSoftTickClip(string name, float frequency, float duration)
    {
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float progress = (float)i / totalSamples;
            float t = (float)i / SampleRate;
            float env = Mathf.Exp(-progress * 35f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateChipTickClip(string name)
    {
        float duration = 0.08f;
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / totalSamples;
            float env = Mathf.Exp(-progress * 28f);
            
            float tone = Mathf.Sin(2f * Mathf.PI * 750f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 1500f * t);
            samples[i] = tone * env;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateMultPopClip(string name)
    {
        float duration = 0.16f;
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / totalSamples;
            float env = Mathf.Exp(-progress * 12f);
            
            // Rising pitch chirp
            float freq = 400f + progress * 600f;
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * freq * t);
            samples[i] = tone * env;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBassImpactClip(string name)
    {
        float duration = 0.35f;
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / totalSamples;
            float env = Mathf.Exp(-progress * 9f);
            
            // Punchy bass sweep from 150Hz down to 40Hz
            float freq = Mathf.Lerp(160f, 45f, progress);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
            float punch = (Random.value * 2f - 1f) * Mathf.Exp(-progress * 60f) * 0.5f;

            samples[i] = (wave + punch) * env;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCashChimeClip(string name)
    {
        float duration = 0.28f;
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / totalSamples;
            float env = Mathf.Exp(-progress * 10f);

            // Two bells (C6 1046Hz, E6 1318Hz)
            float bell1 = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * Mathf.Exp(-progress * 14f);
            float bell2 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * Mathf.Exp(-Mathf.Max(0, progress - 0.08f) * 12f);

            samples[i] = (bell1 + bell2 * 1.2f) * env;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateFanfareClip(string name)
    {
        float duration = 0.65f;
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6
        float noteLen = duration / notes.Length;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            int noteIndex = Mathf.Clamp((int)(t / noteLen), 0, notes.Length - 1);
            float noteTime = t - noteIndex * noteLen;
            float noteProgress = noteTime / noteLen;

            float env = Mathf.Exp(-noteProgress * 6f);
            float wave = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);
            float harmonic = 0.3f * Mathf.Sin(4f * Mathf.PI * notes[noteIndex] * t);

            samples[i] = (wave + harmonic) * env * 0.8f;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateGameOverClip(string name)
    {
        float duration = 0.7f;
        int totalSamples = (int)(SampleRate * duration);
        float[] samples = new float[totalSamples];
        float[] notes = { 440f, 415.3f, 392f, 329.6f }; // Descending minor
        float noteLen = duration / notes.Length;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            int noteIndex = Mathf.Clamp((int)(t / noteLen), 0, notes.Length - 1);
            float noteTime = t - noteIndex * noteLen;
            float noteProgress = noteTime / noteLen;

            float env = Mathf.Exp(-noteProgress * 5f);
            float wave = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);

            samples[i] = wave * env * 0.8f;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
