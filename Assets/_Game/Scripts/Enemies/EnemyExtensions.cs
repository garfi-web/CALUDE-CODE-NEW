using UnityEngine;

// Points d'extension de l'Acte III (docs/ENEMIES.md). Interfaces seulement : aucun ennemi
// ne les implémente encore. Un comportement les implémentera à côté d'EnemyBehaviour.

// 09 — Démon techno : zones électrifiées marquées au sol avant d'être actives.
public interface IHazardSpawner
{
    void SpawnHazard(Vector2 position, float radius, float markTime, int damagePerSecond);
}

// 09 — Démon techno : téléportation, réapparition annoncée par des étincelles.
public interface ITeleport
{
    void TeleportTo(Vector2 position, float warningTime);
}

// 10 — Spectre : doubles sans collision ; le vrai a une ombre au sol.
public interface IIllusionCaster
{
    void CastIllusions(int count);
}

// 11 et 12 — Boss : phases déclenchées par seuils de PV (66 % et 33 % pour le Géant).
public interface IBossPhases
{
    int CurrentPhase { get; }
    void EnterPhase(int phase);
}

// 12 — Porteur de l'aberration : modifie l'arène (plateformes, distances, perspective).
public interface IArenaModifier
{
    void ApplyArenaRule(int phase);
}
