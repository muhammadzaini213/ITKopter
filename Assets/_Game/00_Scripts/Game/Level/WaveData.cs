
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWaveData", menuName = "Scriptable Objects/Wave Data")]
public class EnemyWaveData : ScriptableObject
{
    [Header("Wave Info")]
    [SerializeField] private string waveName;
    [SerializeField] private GameObject wavePrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay;

    public string WaveName => waveName;
    public GameObject WavePrefab => wavePrefab;
    public float SpawnDelay => spawnDelay;
}