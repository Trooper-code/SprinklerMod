using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.Core.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using UnityEngine;

namespace OverheadSprinkler;

/// <summary>
/// Spawns temporary downward spray particles and vanilla sprinkler audio on
/// Overhead Sprinkler Heads during watering or additive bursts.
/// </summary>
public static class SprinklerEffects
{
    private const int MaxAudioHeadsPerCycle = 2;
    private const float CleanupTailSeconds = 1.5f;

    private static bool _clipsSearched;
    private static AudioClip? _clickClip;
    private static AudioClip? _waterClip;
    private static Material? _dropletMaterial;

    /// <summary>
    /// Spawns downward mist particles and optional sprinkler audio on each head for durationSeconds.
    /// </summary>
    public static void PlaySprayOnHeads(
        IReadOnlyList<ProceduralGridItem> heads,
        float durationSeconds,
        Color mistColor,
        bool isAdditiveBurst)
    {
        if (heads == null || heads.Count == 0)
        {
            return;
        }

        bool wantParticles = SprinklerConfig.EnableSprayParticles;
        bool wantSound = SprinklerConfig.EnableSpraySound;
        if (!wantParticles && !wantSound)
        {
            return;
        }

        if (wantSound)
        {
            EnsureVanillaClips();
        }

        float clampedDuration = Mathf.Max(0.4f, durationSeconds);
        int validHeadCount = 0;
        for (int i = 0; i < heads.Count; i++)
        {
            if (heads[i] != null && !heads[i].isGhost)
            {
                validHeadCount++;
            }
        }

        int plannedAudioCount = Mathf.Max(1, Mathf.Min(validHeadCount, MaxAudioHeadsPerCycle));
        float gameFxVolume = Singleton<AudioManager>.InstanceExists
            ? Singleton<AudioManager>.Instance.GetVolume(EAudioType.FX, scaled: true)
            : 1f;
        float effectiveVolume = Mathf.Clamp01(gameFxVolume * Mathf.Clamp01(SprinklerConfig.SoundVolume) * 0.45f / Mathf.Sqrt(plannedAudioCount));
        int audioCount = 0;

        for (int i = 0; i < heads.Count; i++)
        {
            ProceduralGridItem head = heads[i];
            if (head == null || head.isGhost)
            {
                continue;
            }

            var fxGo = new GameObject("OverheadSprinklerSprayFx");
            fxGo.transform.position = head.transform.position + Vector3.down * 0.12f;
            fxGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            ParticleSystem? ps = null;
            if (wantParticles)
            {
                ps = ConfigureParticleSystem(fxGo, mistColor, isAdditiveBurst);
                ps.Play();
            }

            AudioSource? audio = null;
            if (wantSound && audioCount < MaxAudioHeadsPerCycle && (_clickClip != null || _waterClip != null))
            {
                audioCount++;
                audio = fxGo.AddComponent<AudioSource>();
                if (Singleton<AudioManager>.InstanceExists)
                {
                    audio.outputAudioMixerGroup = Singleton<AudioManager>.Instance.MainGameMixer;
                }
                audio.spatialBlend = 1f;
                audio.minDistance = 2f;
                audio.maxDistance = 18f;
                audio.rolloffMode = AudioRolloffMode.Linear;
                audio.volume = effectiveVolume;

                if (_clickClip != null)
                {
                    audio.PlayOneShot(_clickClip, 1f);
                }

                if (_waterClip != null)
                {
                    audio.clip = _waterClip;
                    audio.loop = true;
                    audio.Play();
                }
            }

            MelonCoroutines.Start(StopAndCleanupAfterDelay(fxGo, ps, audio, clampedDuration));
        }
    }

    private static IEnumerator StopAndCleanupAfterDelay(
        GameObject fxGo,
        ParticleSystem? ps,
        AudioSource? audio,
        float durationSeconds)
    {
        yield return new WaitForSeconds(durationSeconds);

        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (audio != null && audio.isPlaying)
        {
            audio.Stop();
        }

        yield return new WaitForSeconds(CleanupTailSeconds);

        if (fxGo != null)
        {
            UnityEngine.Object.Destroy(fxGo);
        }
    }

    private static ParticleSystem ConfigureParticleSystem(GameObject host, Color mistColor, bool isAdditiveBurst)
    {
        ParticleSystem ps = host.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 2f;
        main.startLifetime = 0.85f;
        main.startSpeed = 2.4f;
        main.startSize = isAdditiveBurst ? 0.065f : 0.055f;
        main.gravityModifier = 0.45f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.startColor = new ParticleSystem.MinMaxGradient(
            mistColor,
            new Color(mistColor.r, mistColor.g, mistColor.b, mistColor.a * 0.65f));

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = isAdditiveBurst ? 75f : 110f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 26f;
        shape.radius = 0.22f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.65f, 0.7f),
                new GradientAlphaKey(0f, 1f),
            });
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = host.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetDropletMaterial();
        }

        return ps;
    }

    private static Material GetDropletMaterial()
    {
        if (_dropletMaterial != null)
        {
            return _dropletMaterial;
        }

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - half) / half;
                float ny = (y - half) / half;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha *= alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
        _dropletMaterial = new Material(shader);
        _dropletMaterial.mainTexture = tex;
        return _dropletMaterial;
    }

    private static void EnsureVanillaClips()
    {
        if (_clipsSearched)
        {
            return;
        }

        _clipsSearched = true;
        try
        {
            if (!Singleton<Registry>.InstanceExists)
            {
                return;
            }

            List<ItemDefinition> items = Singleton<Registry>.Instance.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is BuildableItemDefinition buildable && buildable.BuiltItem is Sprinkler sprinkler)
                {
                    if (_clickClip == null && sprinkler.ClickSound != null)
                    {
                        AudioSource? clickSrc = sprinkler.ClickSound.GetComponent<AudioSource>();
                        if (clickSrc != null && clickSrc.clip != null)
                        {
                            _clickClip = clickSrc.clip;
                        }
                    }

                    if (_waterClip == null && sprinkler.WaterSound != null)
                    {
                        AudioSource? waterSrc = sprinkler.WaterSound.GetComponent<AudioSource>();
                        if (waterSrc != null && waterSrc.clip != null)
                        {
                            _waterClip = waterSrc.clip;
                        }
                    }

                    if (_clickClip != null && _waterClip != null)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Could not inspect vanilla Sprinkler audio clips: {ex.Message}");
        }
    }
}
