using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[GenerateAuthoringComponent]
public struct Rotation_Com : IComponentData
{
    public float RadiansPerSecond;
}
