using Slafurry.Systems.Scene;
using UnityEngine;

public class ChangeSceneTrigger : MonoBehaviour
{
    public void ChangeScene(SceneReference scene)
    {
        SceneSystem.Load(scene);
    }
}