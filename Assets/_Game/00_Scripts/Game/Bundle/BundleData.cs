
using UnityEngine;

[CreateAssetMenu(fileName = "BundleData", menuName = "Scriptable Objects/BundleData")]
public class BundleData : ScriptableObject
{
    [SerializeField] private string bundleCategory;
    [SerializeField] private BundleEntry[] bundles;

    public string BundleCategory => bundleCategory;
    public BundleEntry[] Bundles => bundles;
}