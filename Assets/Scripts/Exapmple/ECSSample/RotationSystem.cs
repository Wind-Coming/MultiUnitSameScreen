using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

public class RotationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = Time.DeltaTime;

        Entities.WithName("Rotation_test")
        .ForEach((ref Rotation rotation, in Rotation_Com rotSpeed ) =>
        {
            rotation.Value = math.mul(rotation.Value, quaternion.AxisAngle(math.up(), rotSpeed.RadiansPerSecond * time));
        }).ScheduleParallel();
    }
}
