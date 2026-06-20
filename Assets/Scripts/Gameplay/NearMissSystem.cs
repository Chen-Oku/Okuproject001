using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallController))]
public class NearMissSystem : MonoBehaviour
{
    [SerializeField] float nearMissRadius = 0.6f;

    // Alimenta el combo existente de ScoreManager; no mantiene contador propio.
    public static event Action<RingSegment> OnNearMiss;

    BallController ball;
    readonly HashSet<RingSegment> overlapping = new HashSet<RingSegment>();

    void Awake()
    {
        ball = GetComponent<BallController>();

        var sensor = gameObject.AddComponent<SphereCollider>();
        sensor.isTrigger = true;
        sensor.radius = nearMissRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<RingSegment>(out var seg)) return;
        if (!IsRisky(seg)) return;

        overlapping.Add(seg);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<RingSegment>(out var seg)) return;
        if (!overlapping.Remove(seg)) return;

        OnNearMiss?.Invoke(seg);
    }

    // Sin riesgo real (powerup neutraliza el peligro) => sin premio.
    bool IsRisky(RingSegment seg)
    {
        switch (seg.Type)
        {
            case RingSegment.SegmentType.Dangerous:
                return ball.ActivePowerup != BallController.BallPowerup.Ghost;
            case RingSegment.SegmentType.FireLocked:
                return ball.ActivePowerup != BallController.BallPowerup.Fire;
            default:
                return false;
        }
    }
}
