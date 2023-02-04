using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public enum Effect { 
        Move,
        StartCapture,
        Capture, 
        SendBuffer, 
        ReceiveBuffer
    }
    public AudioSource source;

    [Serializable]
    public struct SoundEffect 
    {
        public Effect LocalEffect;
        public AudioClip LocalPlayerClip;
        public AudioClip OtherPlayerClip;
        public float Volume;
    }

    public List<SoundEffect> SoundEffects;

    public void PlayEffect(Effect effect)
    {
        for (int index = 0; index < SoundEffects.Count; index++)
        {
            if (SoundEffects[index].LocalEffect == effect)
            {
                source.PlayOneShot(SoundEffects[index].LocalPlayerClip, SoundEffects[index].Volume);
                return;
            }
        }
        Debug.Log($"[AUDIO MANAGER] Warning: No Sound Effect found for Effect {effect}");
    }
}
