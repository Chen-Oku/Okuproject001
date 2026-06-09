using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RingSegment : MonoBehaviour
{
    public bool IsDangerous { get; private set; }

    Renderer rend;

    void Awake() => rend = GetComponent<Renderer>();

    public void Setup(bool dangerous, Material safeMat, Material dangerMat)
    {
        IsDangerous = dangerous;
        rend.material = dangerous ? dangerMat : safeMat;
    }

    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.TryGetComponent<BallController>(out var ball)) return;

        if (IsDangerous)
            ball.Die();
    }
}
