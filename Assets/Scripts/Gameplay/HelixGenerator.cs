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
    [SerializeField] GameObject firePowerupVFX;
    [SerializeField] GameObject ghostPowerupVFX;
    [SerializeField] GameObject slowPowerupVFX;

    [Header("Materials")]
    [SerializeField] Material safeMaterial;
    [SerializeField] Material dangerMaterial;
    [SerializeField] Material crumblingMaterial;
    [SerializeField] Material bouncyMaterial;
    [SerializeField] Material checkpointMaterial;
    [SerializeField] Material fireLockedMaterial;

    [Header("Tutorial / Progresion")]
    [SerializeField] int tutorialRingCount      = 30;
    [SerializeField] int tutorialSafeRingCount  = 5;
    [SerializeField] int tutorialFireRingIndex  = 8;
    [SerializeField] int tutorialGhostRingIndex = 15;
    [SerializeField] int tutorialSlowRingIndex  = 22;

    public static HelixGenerator Instance { get; private set; }
    public int TutorialRingCount => tutorialRingCount;

    const float SegW = 1.35f;
    const float SegH = 0.28f;
    const float SegD = 0.45f;

    readonly List<Transform> activeRings = new();
    float nextRingY;
    int   ringIndex;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

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

        // Tutorial: anillos iniciales con generacion guiada para introducir powerups y mecanicas.
        // Se basa en ringIndex (nunca se reinicia), por lo que no vuelve a ocurrir al ciclar zonas.
        bool inTutorial         = ringIndex < tutorialRingCount;
        bool suppressAutoRotate = inTutorial;
        bool forceAllSafe       = ringIndex < tutorialSafeRingCount;

        BallController.BallPowerup? forcedPowerup = null;
        Dictionary<int, RingSegment.SegmentType> forcedTypeMap = null;
        HashSet<int> forcedGaps = null;

        if (ringIndex == tutorialFireRingIndex)
            forcedPowerup = BallController.BallPowerup.Fire;
        else if (ringIndex == tutorialGhostRingIndex)
            forcedPowerup = BallController.BallPowerup.Ghost;
        else if (ringIndex == tutorialSlowRingIndex)
            forcedPowerup = BallController.BallPowerup.Slow;
        else if (ringIndex == tutorialFireRingIndex + 1)
        {
            // Anillo cerrado: solo el fuego destruye estos segmentos y permite pasar
            forcedTypeMap = new Dictionary<int, RingSegment.SegmentType>();
            for (int i = 0; i < segmentsPerRing; i++) forcedTypeMap[i] = RingSegment.SegmentType.FireLocked;
            forcedGaps = new HashSet<int>();
        }
        else if (ringIndex == tutorialGhostRingIndex + 1)
        {
            // Anillo cerrado de peligro: solo el ghost permite atravesarlo
            forcedTypeMap = new Dictionary<int, RingSegment.SegmentType>();
            for (int i = 0; i < segmentsPerRing; i++) forcedTypeMap[i] = RingSegment.SegmentType.Dangerous;
            forcedGaps = new HashSet<int>();
        }
        else if (ringIndex == tutorialSlowRingIndex + 1)
        {
            // Huecos opuestos entre si: con el slow activo da tiempo de girar y alinearse
            forcedGaps = new HashSet<int> { 0, segmentsPerRing / 2 };
        }
        else if (ringIndex == tutorialRingCount - 1)
        {
            // Ultimo anillo del tutorial: escudo garantizado antes de pasar a la zona normal
            forcedTypeMap = new Dictionary<int, RingSegment.SegmentType>();
            for (int i = 0; i < segmentsPerRing; i++) forcedTypeMap[i] = RingSegment.SegmentType.Checkpoint;
            forcedGaps = new HashSet<int>();
        }

        var ring = new GameObject($"Ring_{ringIndex}").transform;
        ring.SetParent(transform);
        ring.localPosition = new Vector3(0f, nextRingY, 0f);

        // Asignar tipos y huecos
        var typeMap = forcedTypeMap;
        var gapSet  = forcedGaps ?? new HashSet<int>();

        if (typeMap == null)
        {
            typeMap = new Dictionary<int, RingSegment.SegmentType>();

            // Fisher-Yates shuffle del pool de indices disponibles (sin los huecos forzados)
            var pool = new List<int>(segmentsPerRing);
            for (int i = 0; i < segmentsPerRing; i++)
                if (!gapSet.Contains(i)) pool.Add(i);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            // Asignar tipos en orden de prioridad consumiendo el pool mezclado
            int cursor = 0;
            void Assign(RingSegment.SegmentType type, int count)
            {
                for (int i = 0; i < count && cursor < pool.Count; i++, cursor++)
                    typeMap[pool[cursor]] = type;
            }

            if (!forceAllSafe)
            {
                Assign(RingSegment.SegmentType.Dangerous,   zoneDanger);
                Assign(RingSegment.SegmentType.Crumbling,   crumblingPerRing);
                Assign(RingSegment.SegmentType.Bouncy,      bouncyPerRing);

                bool spawnCheckpoint = !inTutorial
                                       && checkpointEveryNRings > 0
                                       && ringIndex >= checkpointEveryNRings
                                       && ringIndex % checkpointEveryNRings == 0
                                       && cursor < pool.Count;
                if (spawnCheckpoint)
                    Assign(RingSegment.SegmentType.Checkpoint, 1);
            }

            // Huecos restantes (si no fueron forzados): siguientes indices del pool mezclado
            if (forcedGaps == null)
                for (int i = 0; i < zoneGaps && cursor < pool.Count; i++, cursor++)
                    gapSet.Add(pool[cursor]);
        }

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
        bool spawnPowerup = forcedPowerup.HasValue
                            || (!inTutorial && powerupSpawnChance > 0f && Random.value < powerupSpawnChance);
        if (spawnPowerup)
        {
            BallController.BallPowerup pType;
            if (forcedPowerup.HasValue)
            {
                pType = forcedPowerup.Value;
            }
            else
            {
                var powerupTypes = new[]
                {
                    BallController.BallPowerup.Fire,
                    BallController.BallPowerup.Ghost,
                    BallController.BallPowerup.Slow,
                };
                pType = powerupTypes[Random.Range(0, powerupTypes.Length)];
            }

            var pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            // Posicion en world space: X/Z del ball, Y a mitad entre este anillo y el siguiente
            pickup.position   = new Vector3(ball.position.x, ring.position.y - ringSpacing * 0.5f, ball.position.z);
            pickup.localScale = Vector3.one * 0.4f;

            Destroy(pickup.GetComponent<Collider>());
            var sc = pickup.gameObject.AddComponent<SphereCollider>();
            sc.radius    = 0.7f;
            sc.isTrigger = true;

            pickup.gameObject.AddComponent<PowerupPickup>().Setup(pType, powerupDuration, MaterialForPowerup(pType), VFXForPowerup(pType), ball);
        }

        // Auto-rotación individual del anillo
        if (!suppressAutoRotate && zoneAutoChance > 0f && Random.value < zoneAutoChance)
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
        RingSegment.SegmentType.FireLocked => fireLockedMaterial != null ? fireLockedMaterial : dangerMaterial,
        _                                  => safeMaterial,
    };

    Material MaterialForPowerup(BallController.BallPowerup type) => type switch
    {
        BallController.BallPowerup.Fire  => firePowerupMat  != null ? firePowerupMat  : dangerMaterial,
        BallController.BallPowerup.Ghost => ghostPowerupMat != null ? ghostPowerupMat : safeMaterial,
        BallController.BallPowerup.Slow  => slowPowerupMat  != null ? slowPowerupMat  : safeMaterial,
        _                                => safeMaterial,
    };

    GameObject VFXForPowerup(BallController.BallPowerup type) => type switch
    {
        BallController.BallPowerup.Fire  => firePowerupVFX,
        BallController.BallPowerup.Ghost => ghostPowerupVFX,
        BallController.BallPowerup.Slow  => slowPowerupVFX,
        _                                 => null,
    };
}
