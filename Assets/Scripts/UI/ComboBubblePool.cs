using UnityEngine;

// Punto unico de entrada para los globos de combo: escucha AbyssEvents y
// elige que instancia del pool usar. Las instancias (ComboBubble) no se
// conocen entre si ni conocen el evento, solo saben animarse cuando se les pide.
public class ComboBubblePool : MonoBehaviour
{
    [SerializeField] ComboBubble[] pool;

    void Awake()
    {
        foreach (var bubble in pool)
            bubble.gameObject.SetActive(false);
    }

    void OnEnable()  => AbyssEvents.OnComboChanged += HandleComboChanged;
    void OnDisable() => AbyssEvents.OnComboChanged -= HandleComboChanged;

    void HandleComboChanged(int tier)
    {
        if (tier <= 0) return;
        PickInstance().Show(tier);
    }

    ComboBubble PickInstance()
    {
        foreach (var bubble in pool)
            if (!bubble.gameObject.activeSelf) return bubble;

        ComboBubble oldest = pool[0];
        for (int i = 1; i < pool.Length; i++)
            if (pool[i].TimeRemaining < oldest.TimeRemaining) oldest = pool[i];
        return oldest;
    }
}
