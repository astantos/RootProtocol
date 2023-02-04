using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public enum Effect { }
    public AudioSource source;

    [Serializable]
    public struct SoundEffect 
    {
        public Effect Effect;
        public AudioClip Clip; 
    }

    public List<SoundEffect> SoundEffects;

    public void PlayEffect(Effect effect)
    {
        for (int index = 0; index < SoundEffects.Count; index++)
        {
            if (SoundEffects[index].Effect == effect)
            {
                source.PlayOneShot(SoundEffects[index].Clip);
                return;
            }
        }
        Debug.Log($"[AUDIO MANAGER] Warning: No Sound Effect found for Effect {effect}");
    }
}