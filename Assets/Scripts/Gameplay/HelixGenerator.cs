using UnityEngine;
using System.Collections.Generic;

public class HelixGenerator : MonoBehaviour
{
    [Header("Ring Config")]
    [SerializeField] int segmentsPerRing = 8;
    [SerializeField] int gapsPerRing = 2;
    [SerializeField] int dangerSegmentsPerRing = 1;
    [SerializeField] float ringSpacing = 3f;
    [SerializeField] float ringRadius = 2f;

    [Header("Pool")]
    [SerializeField] int ringsAhead = 14;
    [SerializeField] int ringsBehindToCull = 3;

    [Header("References")]
    [SerializeField] Transform ball;
    [SerializeField] Material safeMaterial;
    [SerializeField] Material dangerMaterial;

    // Segment dimensions (box aproximando arco del anillo)
    const float SegW = 1.35f;
    const float SegH = 0.28f;
    const float SegD = 0.45f;

    readonly List<Transform> activeRings = new();
    float nextRingY;
    int ringIndex;

    void Start()
    {
        nextRingY = -ringSpacing; // primer anillo debajo de la bola
        for (int i = 0; i < ringsAhead; i++)
            SpawnRing();
    }

    void Update()
    {
        // Generar anillos por delante de la bola
        while (nextRingY > ball.position.y - ringsAhead * ringSpacing)
            SpawnRing();

        // Destruir anillos demasiado lejos por arriba
        while (activeRings.Count > 0 &&
               activeRings[0].position.y > ball.position.y + ringsBehindToCull * ringSpacing)
        {
            Destroy(activeRings[0].gameObject);
            activeRings.RemoveAt(0);
        }
    }

    void SpawnRing()
    {
        var ring = new GameObject($"Ring_{ringIndex++}").transform;
        ring.SetParent(transform);
        ring.localPosition = new Vector3(0f, nextRingY, 0f);

        // Generar lista de indices y mezclar (Fisher-Yates)
        var indices = new List<int>(segmentsPerRing);
        for (int i = 0; i < segmentsPerRing; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var gapSet = new HashSet<int>();
        var dangerSet = new HashSet<int>();
        for (int i = 0; i < gapsPerRing; i++) gapSet.Add(indices[i]);
        for (int i = gapsPerRing; i < gapsPerRing + dangerSegmentsPerRing; i++) dangerSet.Add(indices[i]);

        float angleStep = 360f / segmentsPerRing;
        for (int i = 0; i < segmentsPerRing; i++)
        {
            if (gapSet.Contains(i)) continue;

            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * ringRadius;
            float z = Mathf.Cos(angle) * ringRadius;

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            seg.SetParent(ring);
            seg.localPosition = new Vector3(x, 0f, z);
            seg.localRotation = Quaternion.Euler(0f, i * angleStep, 0f);
            seg.localScale = new Vector3(SegW, SegH, SegD);

            seg.GetComponent<RingSegment>()?.Setup(dangerSet.Contains(i), safeMaterial, dangerMaterial);
            // Si CreatePrimitive no agrega RingSegment, lo agregamos manualmente
            if (seg.GetComponent<RingSegment>() == null)
                seg.gameObject.AddComponent<RingSegment>().Setup(dangerSet.Contains(i), safeMaterial, dangerMaterial);
        }

        // Trigger de puntaje justo debajo del anillo
        var triggerObj = new GameObject("ScoreTrigger");
        triggerObj.transform.SetParent(ring);
        triggerObj.transform.localPosition = new Vector3(0f, -(SegH * 0.5f + 0.1f), 0f);
        triggerObj.layer = LayerMask.NameToLayer("Default");

        var col = triggerObj.AddComponent<CapsuleCollider>();
        col.radius = ringRadius + 0.3f; // cubre el borde donde viaja la bola
        col.height = 0.3f;
        col.isTrigger = true;

        triggerObj.AddComponent<ScoreTrigger>();

        activeRings.Add(ring);
        nextRingY -= ringSpacing;
    }
}
