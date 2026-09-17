# Guide d'envoi — de la newsletter finie à l'envoi groupé sur Brevo

Ce guide part du principe que vous débutez sur Brevo. On avance étape par
étape ; dites-moi où vous en êtes et je vous aide à la suite.

## Pourquoi pas un "email normal" (Gmail/Outlook) ?

Copier le texte de la newsletter dans une fenêtre de rédaction Gmail ou
Outlook et l'envoyer à toute votre liste **n'est pas une option**, pour
plusieurs raisons concrètes :

- **La mise en page casse.** Gmail/Outlook réécrivent le code HTML à leur
  façon : les tableaux, les couleurs de fond, les polices et souvent les
  images sautent.
- **Vous seriez bloqué très vite.** Les messageries grand public limitent
  l'envoi à quelques dizaines/centaines de destinataires par jour et
  détectent un envoi groupé identique comme un comportement suspect
  (spam), avec un risque de blocage de votre adresse.
- **Aucune gestion RGPD automatique.** Pas de lien de désinscription
  fonctionnel, pas de traçabilité du consentement, pas de registre des
  envois — Brevo gère tout cela pour vous.
- **Aucune statistique.** Avec Brevo vous savez qui a ouvert, qui a
  cliqué sur quel produit, etc. — utile pour savoir qui rappeler en
  priorité.

➡️ **La bonne méthode : coller le code HTML dans une campagne Brevo.**
C'est justement l'étape 4 ci-dessous.

## Étape 0 — Avant de commencer

- Un compte Brevo (gratuit pour démarrer) sur [app.brevo.com](https://app.brevo.com).
- Votre adresse d'expédition (ex. g.arfi@minleh-conseil.com) **vérifiée**
  dans Brevo (Paramètres > Expéditeurs, senders & IP > Ajouter un
  expéditeur). Sans cela, vos emails partent avec un taux de délivrabilité
  très dégradé.
- Vos listes de contacts importées dans Brevo (Contacts > Listes），idéalement
  déjà séparées en 3 catégories : **Clients**, **Prospects**,
  **Apporteurs d'affaires** — le ton n'est pas le même selon le
  destinataire.

## Étape 1 — Mettre en place la "page de capture" (récupérer les coordonnées du client)

Vous avez raison : un lien `mailto:` (comme actuellement sur les boutons
"Je souhaite en savoir plus") ouvre juste la messagerie du client, il ne
capture rien automatiquement dans votre CRM tant que le client n'a pas
lui-même cliqué sur "Envoyer". Pour un vrai formulaire qui enregistre
Nom/Email/Téléphone directement dans Brevo dès que le client valide,
utilisez l'outil **Formulaires** de Brevo (gratuit, inclus) :

1. Dans Brevo, menu **Contacts** > **Formulaires** > **Créer un
   formulaire**.
2. Choisissez un formulaire simple ("formulaire d'inscription" /
   "Subscription form").
3. Ajoutez les champs :
   - Prénom
   - Nom
   - Email (obligatoire)
   - Téléphone (SMS/WhatsApp field, ou champ texte)
   - Un champ liste déroulante **"Produit qui vous intéresse"** avec les
     options : *Phoenix Mémoire Target Tec 10*, *Opti Strike Siemens
     Energy*, *Taux Fixe Bonus*, *Athena Airbag Tec 10*, *Autre demande*
     (pratique pour savoir de quoi parler au rappel, sans faire un
     formulaire différent par produit).
4. Dans l'onglet **Paramètres**, choisissez la liste Brevo où les
   nouveaux contacts doivent atterrir (ex. "Prospects — Filons").
5. Personnalisez rapidement les couleurs/texte du formulaire si vous
   voulez (pas obligatoire).
6. Enregistrez, puis onglet **Partager** : Brevo vous donne une **URL
   publique** du formulaire (ex.
   `https://xxxxx.brevosend.com/xxxxxx`). C'est cette URL qu'il faut me
   transmettre.

Une fois que vous avez cette URL, envoyez-la-moi : je remplace en une
fois tous les boutons "Je souhaite en savoir plus" (et "Je réserve mon
rendez-vous") des deux newsletters pour qu'ils pointent vers ce
formulaire au lieu du lien email. On pourra garder l'email et le
téléphone en complément dans le texte, pour les clients qui préfèrent
vous contacter directement.

*(Si votre plan Brevo donne aussi accès aux "Landing pages", c'est une
alternative plus habillée graphiquement — mais le formulaire simple fait
exactement le travail demandé : capturer les coordonnées.)*

## Étape 2 — Héberger les images

Le fichier HTML utilise pour l'instant des chemins locaux
(`assets/logo-abeille.png`, etc.) qui ne fonctionnent que si vous ouvrez
le fichier sur votre ordinateur. Pour un envoi, les images doivent être
en ligne :

1. Brevo > **Campagnes** > **Galerie** (ou l'icône image dans l'éditeur
   de campagne).
2. Uploadez chaque fichier du dossier `assets/` (visuel du mois, logos,
   votre photo).
3. Pour chaque image uploadée, Brevo vous donne une URL (clic droit sur
   l'image dans la galerie > copier le lien, ou bouton "Copier l'URL").
4. Dans le code HTML, remplacez chaque `src="assets/xxx"` par l'URL
   copiée. (Je peux le faire pour vous si vous me collez les 5 URLs.)

## Étape 3 — Créer la campagne dans Brevo

1. **Campagnes** > **Email** > **Créer une campagne email**.
2. Donnez un nom interne à la campagne (ex. "Filons — Septembre 2026",
   pour vous, invisible du destinataire).
3. Objet du mail : *"Les Filons du mois — Septembre 2026 : 4 opportunités
   sélectionnées pour vous"*.
4. Nom et email d'expéditeur : Grégory Arfi / g.arfi@minleh-conseil.com.
5. Dans le choix du type d'éditeur, prenez **"Éditeur de code" / "Import
   HTML"** (parfois listé comme "Rich text/HTML" selon les versions de
   Brevo) plutôt que le glisser-déposer.
6. Collez l'intégralité du contenu du fichier `les-filons.html` (déjà mis
   à jour avec les URLs d'images et, une fois prêt, le lien du
   formulaire).
7. Choisissez la/les liste(s) de destinataires (Clients, Prospects,
   Apporteurs — vous pouvez lancer 3 envois séparés si vous voulez
   adapter le ton à chaque audience).

## Étape 4 — Tester avant d'envoyer (obligatoire)

1. Dans l'écran de la campagne, bouton **"Envoyer un test"** : envoyez-la
   à votre propre adresse (et idéalement une adresse Outlook si vous en
   avez une, pour vérifier le rendu).
2. Vérifiez sur votre téléphone ET sur ordinateur : images visibles,
   logos bien affichés, boutons cliquables, lien de désinscription
   présent.
3. Cliquez vous-même sur "Je souhaite en savoir plus" pour vérifier que
   ça ouvre bien le bon email ou le bon formulaire.

## Étape 5 — Envoyer

1. Une fois le test validé, retournez sur la campagne, choisissez
   **"Envoyer maintenant"** ou **"Planifier"** (utile pour programmer
   l'envoi à une heure optimale, ex. 9h ou 18h).
2. Confirmez. C'est parti.

## Après l'envoi

- Brevo vous donne un tableau de bord : taux d'ouverture, taux de clic
  par lien (utile pour voir quel produit a le plus intéressé).
- Les nouveaux contacts du formulaire (étape 1) arrivent dans la liste
  Brevo choisie : pensez à les consulter régulièrement pour les rappeler.

---

**On commence quand vous voulez : dites-moi simplement où vous en êtes**
(compte Brevo déjà créé ou non, expéditeur déjà vérifié ou non) et je
vous accompagne sur l'étape suivante.
