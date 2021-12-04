// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToSceneSelector:
  MonoBehaviour
{
  private string mySceneName;

  void Start()
  {
    mySceneName = SceneManager.GetActiveScene().name;
    DontDestroyOnLoad(gameObject);
  }

  void Update()
  {
    if (Input.touches.Length >= 5 && SceneManager.GetActiveScene().name != mySceneName)
    {
      Debug.Log("ARDK Examples: Returning to Scene Selector");
      SceneManager.LoadScene(mySceneName);
    }
  }
}
