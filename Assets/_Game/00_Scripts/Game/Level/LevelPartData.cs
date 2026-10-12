
using UnityEngine;

[CreateAssetMenu(fileName = "LevelPartData", menuName = "Scriptable Objects/Level Part Data")]
public class LevelPartData : ScriptableObject
{
    [Header("Area Info")]
    [SerializeField] private string partName;
    [SerializeField] private GameObject areaPrefab;

    [Header("Enemy Waves")]
    [SerializeField] private WaveData[] enemyWaves;

    public string PartName => partName;
    public GameObject AreaPrefab => areaPrefab;
    public WaveData[] EnemyWaves => enemyWaves;
}