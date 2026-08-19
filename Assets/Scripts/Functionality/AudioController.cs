using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Best.SocketIO;

public class AudioController : MonoBehaviour
{
  [SerializeField] internal AudioSource bg_adudio;
  [SerializeField] internal AudioSource audioPlayer_wl;
  [SerializeField] internal AudioSource audioPlayer_button;
  [SerializeField] internal AudioSource audioSpin_button;
  [SerializeField] private AudioClip[] clips;
  [SerializeField] private AudioClip[] Bonusclips;
  [SerializeField] private AudioSource bg_audioBonus;
  [SerializeField] private AudioSource audioPlayer_Bonus;
  private void Start()
  {
    if (bg_adudio) bg_adudio.Play();
    audioPlayer_button.clip = clips[clips.Length - 2];
    audioSpin_button.clip = clips[clips.Length - 3];
  }

  internal void SwitchBGSound(bool isbonus)
  {
    if (isbonus)
    {
      if (bg_audioBonus) bg_audioBonus.enabled = true;
      if (bg_adudio) bg_adudio.enabled = false;
    }
    else
    {
      if (bg_audioBonus) bg_audioBonus.enabled = false;
      if (bg_adudio) bg_adudio.enabled = true;
    }
  }

  internal void PlayWLAudio(string type)
  {
    audioPlayer_wl.loop = false;
    int index = 0;
    switch (type)
    {
      case "spin":
        index = 0;
        audioPlayer_wl.loop = true;
        break;
      case "win":
        index = 1;
        break;
      case "lose":
        index = 2;
        break;
      case "spinStop":
        index = 3;
        break;
      case "megaWin":
        index = 4;
        break;
      case "phone":
        index = 7;
        break;
    }
    StopWLAaudio();
    audioPlayer_wl.clip = clips[index];
    audioPlayer_wl.Play();
  }

  //same clip keys as PlayWLAudio, but layered ON TOP of whatever audioPlayer_wl is already
  //playing instead of replacing it — for stingers that should stack over the win sound
  internal void PlayWLAudioOneShot(string type)
  {
    int index = WLClipIndex(type);
    if (index < 0 || index >= clips.Length) return;
    audioPlayer_wl.PlayOneShot(clips[index]);
  }

  private int WLClipIndex(string type)
  {
    switch (type)
    {
      case "spin": return 0;
      case "win": return 1;
      case "lose": return 2;
      case "spinStop": return 3;
      case "megaWin": return 4;
      case "phone": return 7;
    }
    return -1;
  }

  internal void PlayBonusAudio(string type)
  {
    audioPlayer_wl.loop = false;
    int index = 0;
    switch (type)
    {
      case "win":
        index = 0;
        break;
      case "lose":
        index = 1;
        break;
      case "cycleSpin":
        index = 2;
        break;
    }
    StopBonusAaudio();
    audioPlayer_Bonus.clip = Bonusclips[index];
    audioPlayer_Bonus.Play();

  }

  internal void PlayButtonAudio()
  {
    audioPlayer_button.Play();
  }

  internal void PlaySpinButtonAudio()
  {
    audioSpin_button.Play();
  }

  internal void StopWLAaudio()
  {
    audioPlayer_wl.loop = false;
    audioPlayer_wl.Stop();
  }

  internal void StopBonusAaudio()
  {
    audioPlayer_Bonus.Stop();
    audioPlayer_Bonus.loop = false;
  }

  internal void StopBgAudio()
  {
    bg_adudio.Stop();
  }

  internal void ToggleMute(bool toggle, string type = "all")
  {
    switch (type)
    {
      case "bg":
        bg_adudio.mute = toggle;
        bg_audioBonus.mute = toggle;
        break;
      case "button":
        audioPlayer_button.mute = toggle;
        audioSpin_button.mute = toggle;
        break;
      case "wl":
        audioPlayer_wl.mute = toggle;
        audioPlayer_Bonus.mute = toggle;
        break;
      case "all":
        audioPlayer_wl.mute = toggle;
        bg_adudio.mute = toggle;
        audioPlayer_button.mute = toggle;
        audioSpin_button.mute = toggle;
        break;
    }
  }
}
