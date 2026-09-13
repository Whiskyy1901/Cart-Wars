using System;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
   private void Awake()
   {
      try
      {
         Steamworks.SteamClient.Init(480);
         Debug.Log("Connected");
      }
      catch (System.Exception e)
      {
         Debug.Log("Initialization Failed!");
      }
      DontDestroyOnLoad(this.gameObject);
   }

   private void OnDisable()
   {
      try
      {
         Steamworks.SteamClient.Shutdown();
      }
      catch (System.Exception e)
      {
         Debug.Log(e.Message);
      }
   }
   
   private void Update()
   {
      Steamworks.SteamClient.RunCallbacks();
   }
}
