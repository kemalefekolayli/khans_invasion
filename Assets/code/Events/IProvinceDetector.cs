using UnityEngine;

/// <summary>
/// Interface for entities that can detect and interact with provinces.
/// Implement this on Horse, Generals, Armies, etc.
/// </summary>
public interface IProvinceDetector
{
    ProvinceModel CurrentProvince { get; }
    Vector3 Position { get; }
}