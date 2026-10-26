using System.Collections.Generic;
using UnityEngine;

// Réserve d'objets par prefab : pas d'Instantiate ni de Destroy pendant le combat.
// Get réactive un objet rendu (ou en crée un si la réserve est vide), Release le désactive.
public static class Pool
{
    private static readonly Dictionary<Component, Stack<Component>> Free = new();
    private static readonly Dictionary<Component, Component> PrefabOf = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Free.Clear();
        PrefabOf.Clear();
    }

    public static T Get<T>(T prefab, Vector2 position) where T : Component
    {
        if (!Free.TryGetValue(prefab, out Stack<Component> stack))
            Free[prefab] = stack = new Stack<Component>();

        T instance = null;

        // Un objet détruit avec sa scène peut rester dans la réserve : on l'ignore.
        while (stack.Count > 0 && instance == null)
            instance = (T)stack.Pop();

        if (instance == null)
        {
            instance = Object.Instantiate(prefab);
            PrefabOf[instance] = prefab;
        }

        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        instance.gameObject.SetActive(true);
        return instance;
    }

    public static void Release(Component instance)
    {
        if (instance == null || !instance.gameObject.activeSelf)
            return;

        instance.gameObject.SetActive(false);

        if (PrefabOf.TryGetValue(instance, out Component prefab))
            Free[prefab].Push(instance);
        else
            Object.Destroy(instance.gameObject);
    }

    // Prépare des objets à l'avance (au chargement, hors combat).
    public static void Prewarm<T>(T prefab, int count) where T : Component
    {
        var created = new List<T>(count);

        for (int i = 0; i < count; i++)
            created.Add(Get(prefab, new Vector2(0f, -1000f)));

        foreach (T instance in created)
            Release(instance);
    }
}
