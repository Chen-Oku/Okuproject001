using UnityEngine;
using System.Collections.Generic;

public class HelixGenerator : MonoBehaviour
{
    [Header("Ring Config")]
    [SerializeField] int segmentsPerRing       = 8;
    [SerializeField] int gapsPerRing           = 2;
    [SerializeField] int dangerPerRing         = 1;
    [SerializeField] int crumblingPerRing      = 1;
    [SerializeField] int bouncyPerRing         = 1;
    [SerializeField] int checkpointEveryNRings = 8;   // 0 = desactivado
    [SerializeField] float ringSpacing         = 3f;
    [SerializeField] float ringRadius          = 2f;

    [Header("Auto-Rotate")]
    [SerializeField] float autoRotateChance   = 0.25f;  // 0-1
    [SerializeField] float minRotateSpeed     = 25f;    // grados/seg
    [SerializeField] float maxRotateSpeed     = 70f;

    [Header("Pool")]
    [SerializeField] int ringsAhead        = 14;
    [SerializeField] int ringsBehindToCull = 3;

    [Header("References")]
    [SerializeField] Transform ball;

    [Header("Powerups")]
    [SerializeField] float powerupSpawnChance = 0.2f;
    [SerializeField] float powerupDuration    = 5f;
    [SerializeField] Material firePowerupMat;
    [SerializeField] Material ghostPowerupMat;
    [SerializeField] Material slowPowerupMat;

    [Header("Materials")]
    [SerializeField] Material safeMaterial;
    [SerializeField] Material dangerMaterial;
    [SerializeField] Material crumblingMaterial;
    [SerializeField] Material bouncyMaterial;
    [SerializeField] Material checkpointMaterial;

    const float SegW = 1.35f;
    const float SegH = 0.28f;
    const float SegD = 0.45f;

    readonly List<Transform> activeRings = new();
    float nextRingY;
    int   ringIndex;

    void Start()
    {
        nextRingY = -ringSpacing;
        for (int i = 0; i < ringsAhead; i++)
            SpawnRing();
    }

    void Update()
    {
        while (nextRingY > ball.position.y - ringsAhead * ringSpacing)
            SpawnRing();

        while (activeRings.Count > 0 &&
               activeRings[0].position.y > ball.position.y + ringsBehindToCull * ringSpacing)
        {
            Destroy(activeRings[0].gameObject);
            activeRings.RemoveAt(0);
        }
    }

    void SpawnRing()
    {
        // Ajustes por zona activa
        var zone = ZoneManager.Instance != null ? ZoneManager.Instance.CurrentZone : ZoneManager.Zone.Normal;
        int   zoneDanger    = dangerPerRing    + (zone == ZoneManager.Zone.Lava  ? 1    : 0);
        int   zoneGaps      = gapsPerRing      + (zone == ZoneManager.Zone.Void  ? 1    : 0);
        float zoneAutoChance = autoRotateChance + (zone == ZoneManager.Zone.Lava ? 0.2f : 0f);
        float zoneMaxSpeed   = maxRotateSpeed   * (zone == ZoneManager.Zone.Lava ? 1.5f : 1f);

        var ring = new GameObject($"Ring_{ringIndex}").transform;
        ring.SetParent(transform);
        ring.localPosition = new Vector3(0f, nextRingY, 0f);

        // Fisher-Yates shuffle
        var indices = new List<int>(segmentsPerRing);
        for (int i = 0; i < segmentsPerRing; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Asignar tipos en orden de prioridad consumiendo el pool mezclado
        int cursor = 0;
        var typeMap = new Dictionary<int, RingSegment.SegmentType>();

        Assign(RingSegment.SegmentType.Dangerous,   zoneDanger);
        Assign(RingSegment.SegmentType.Crumbling,   crumblingPerRing);
        Assign(RingSegment.SegmentType.Bouncy,      bouncyPerRing);

        bool spawnCheckpoint = checkpointEveryNRings > 0
                               && ringIndex >= checkpointEveryNRings
                               && ringIndex % checkpointEveryNRings == 0
                               && cursor < indices.Count;
        if (spawnCheckpoint)
            Assign(RingSegment.SegmentType.Checkpoint, 1);

        void Assign(RingSegment.SegmentType type, int count)
        {
            for (int i = 0; i < count && cursor < indices.Count; i++, cursor++)
                typeMap[indices[cursor]] = type;
        }

        // Gaps: los siguientes indices se omiten al spawnear
        var gapSet = new HashSet<int>();
        for (int i = 0; i < zoneGaps && cursor < indices.Count; i++, cursor++)
            gapSet.Add(indices[cursor]);

        // Spawnear segmentos
        float angleStep = 360f / segmentsPerRing;
        for (int i = 0; i < segmentsPerRing; i++)
        {
            if (gapSet.Contains(i)) continue;

            float angle = i * angleStep * Mathf.Deg2Rad;
            float x     = Mathf.Sin(angle) * ringRadius;
            float z     = Mathf.Cos(angle) * ringRadius;

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            seg.SetParent(ring);
            seg.localPosition = new Vector3(x, 0f, z);
            seg.localRotation = Quaternion.Euler(0f, i * angleStep, 0f);
            seg.localScale    = new Vector3(SegW, SegH, SegD);

            var type = typeMap.TryGetValue(i, out var t) ? t : RingSegment.SegmentType.Safe;
            seg.gameObject.AddComponent<RingSegment>().Setup(type, MaterialFor(type));
        }

        // Score trigger debajo del anillo
        var triggerObj = new GameObject("ScoreTrigger");
        triggerObj.transform.SetParent(ring);
        triggerObj.transform.localPosition = new Vector3(0f, -(SegH * 0.5f + 0.1f), 0f);

        var col = triggerObj.AddComponent<CapsuleCollider>();
        col.radius    = ringRadius + 0.3f;
        col.height    = 0.3f;
        col.isTrigger = true;

        triggerObj.AddComponent<ScoreTrigger>();

        // Powerup pickup en la trayectoria real del ball (su X/Z en world space)
        // No se parentea al ring para que no rote con la helice
        if (powerupSpawnChance > 0f && Random.value < powerupSpawnChance)
        {
            var powerupTypes = new[]
            {
                BallController.BallPowerup.Fire,
                BallController.BallPowerup.Ghost,
                BallController.BallPowerup.Slow,
            };
            var pType = powerupTypes[Random.Range(0, powerupTypes.Length)];

            var pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            // Posicion en world space: X/Z del ball, Y a mitad entre este anillo y el siguiente
            pickup.position   = new Vector3(ball.position.x, ring.position.y - ringSpacing * 0.5f, ball.position.z);
            pickup.localScale = Vector3.one * 0.4f;

            Destroy(pickup.GetComponent<Collider>());
            var sc = pickup.gameObject.AddComponent<SphereCollider>();
            sc.radius    = 0.7f;
            sc.isTrigger = true;

            pickup.gameObject.AddComponent<PowerupPickup>().Setup(pType, powerupDuration, MaterialForPowerup(pType), ball);
        }

        // Auto-rotación individual del anillo
        if (zoneAutoChance > 0f && Random.value < zoneAutoChance)
        {
            float speed = Random.Range(minRotateSpeed, zoneMaxSpeed);
            if (Random.value < 0.5f) speed = -speed;
            ring.gameObject.AddComponent<RingAutoRotator>().Setup(speed);
        }

        activeRings.Add(ring);
        nextRingY -= ringSpacing;
        ringIndex++;
    }

    Material MaterialFor(RingSegment.SegmentType type) => type switch
    {
        RingSegment.SegmentType.Dangerous  => dangerMaterial,
        RingSegment.SegmentType.Crumbling  => crumblingMaterial  != null ? crumblingMaterial  : dangerMaterial,
        RingSegment.SegmentType.Bouncy     => bouncyMaterial     != null ? bouncyMaterial     : safeMaterial,
        RingSegment.SegmentType.Checkpoint => checkpointMaterial != null ? checkpointMaterial : safeMaterial,
        _                                  => safeMaterial,
    };

    Material MaterialForPowerup(BallController.BallPowerup type) => type switch
    {
        BallController.BallPowerup.Fire  => firePowerupMat  != null ? firePowerupMat  : dangerMaterial,
        BallController.BallPowerup.Ghost => ghostPowerupMat != null ? ghostPowerupMat : safeMaterial,
        BallController.BallPowerup.Slow  => slowPowerupMat  != null ? slowPowerupMat  : safeMaterial,
        _                                => safeMaterial,
    };
}
