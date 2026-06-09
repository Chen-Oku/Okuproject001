using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.TryGetComponent<BallController>(out _)) return;
        triggered = true;
        ScoreManager.Instance.AddScore();
    }
}
