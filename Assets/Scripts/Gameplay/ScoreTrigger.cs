using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.TryGetComponent<BallController>(out var ball)) return;
        triggered = true;

        bool skipped = !ball.TouchedSegmentThisRing;
        ball.ResetSegmentTouched();
        ScoreManager.Instance.AddScoreWithCombo(skipped);
        ZoneManager.Instance?.NotifyRingPassed();
        SurgeManager.Instance?.OnRingSafeCleared();
    }
}
