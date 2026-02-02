using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
  [SerializeField] private AudioClip introMusic;

  private void Start()
  {
    // Play intro/menu music when entering menu scene (only if not already playing from Intro)
    if (AudioManager.Instance != null && introMusic != null)
    {
      if (!AudioManager.Instance.IsMusicPlaying(introMusic))
      {
        AudioManager.Instance.PlayMusic(introMusic, true);
      }
    }
  }

  public void OnPlayButton()
  {
    AudioManager.Instance?.StopMusic();
    SceneManager.LoadScene("GameScene");
  }

  public void OnQuitButton()
  {
    Application.Quit();
  }

  public void OnTutorialButton()
  {
    AudioManager.Instance?.StopMusic();
    SceneManager.LoadScene("Tutorial");
  }

  public void GoToTutorial()
  {
    AudioManager.Instance?.StopMusic();
    SceneManager.LoadScene("Tutorial");
  }
  
  public void ContinueToCameraButton()
  {
    AudioManager.Instance?.StopMusic();
    SceneManager.LoadScene("UserCamera Video");
  }
}
