
using UnityEngine;

[CreateAssetMenu(
    fileName = "WaveData",
    menuName = "Scriptable Objects/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Info")]
    [SerializeField] private string waveName;

    [Header("Enemy Collection")]
    [SerializeField] private GameObject enemyCollectionPrefab;

    public string WaveName => waveName;
    public GameObject EnemyCollectionPrefab => enemyCollectionPrefab;
}
