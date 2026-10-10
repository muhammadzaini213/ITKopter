using Slafurry.Systems.Scene;
using UnityEngine;

public class ChangeSceneTrigger: MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        SceneSystem.Load(sceneName);
    }
}