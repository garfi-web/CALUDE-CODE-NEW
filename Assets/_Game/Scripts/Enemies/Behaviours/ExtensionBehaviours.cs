// Points d'extension de l'Acte II (docs/ENEMIES.md). Pour l'instant, ces ennemis se battent
// comme des Voyous ; chaque classe indique ce qu'il reste à coder.

// 05 — Chasseur. À faire : état furtif (altSpeed = traque, moveSpeed = furtif), embuscade
// depuis une zone d'ombre, pose de pièges à mâchoires (immobilisation 1,2 s).
public class Ambusher : MeleeAggressive
{
}

// 07 — Psycho. Les feintes marchent déjà : une attaque avec feint = true et une annonce bleue
// dans l'archétype. À faire : accélérations brusques (altSpeed), attaque retardée, ruée.
public class Feinter : MeleeAggressive
{
}

// 08 — Clown tueur. À faire : objets lancés (leurre ou bombe), fausse ouverture
// (étoiles violettes, contre-attaque si frappé).
public class Trickster : MeleeAggressive
{
}
