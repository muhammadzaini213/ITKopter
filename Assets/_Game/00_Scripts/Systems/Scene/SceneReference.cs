using UnityEngine;

[CreateAssetMenu(fileName = "SceneReference", menuName = "Scene Reference")]
public class SceneReference : ScriptableObject
{
    [SerializeField] private string sceneName;

    public string SceneName => sceneName;
}