# Newsletters Minleh Conseil

Deux modèles HTML prêts pour Brevo, dans l'identité visuelle Minleh Conseil (à
partir de votre exemple Canva et des couleurs de votre logo) :

- **`les-filons.html`** — newsletter produits financiers (assurance vie, PER,
  compte-titres, fonds structurés…). Thème vert sapin `#0C332E` / vert menthe
  `#A8F7C0`.
- **`les-pepites-immobilieres.html`** — newsletter produits immobiliers
  (LMNP, SCPI, résidences gérées, opérations de promotion…). Thème bleu marine
  `#14284A` / or `#C9A15C`.

Les deux partagent la même structure, la même typographie (Playfair Display
pour les titres, Arial/Inter pour le texte), le même bloc légal/RGPD et la
même carte de signature — seule la couleur et le contenu produit changent,
pour que vos deux communications restent immédiatement identifiables comme
venant de vous tout en étant distinguables l'une de l'autre.

## Mise à jour mensuelle

Chaque fichier contient des commentaires `<!-- >>> À MODIFIER CHAQUE MOIS -->`
qui balisent :

1. Le badge du mois (`SEPTEMBRE 2026` → à changer).
2. Le visuel d'en-tête (bandeau).
3. Le paragraphe "Le constat" (l'accroche).
4. Chaque case placement / bien (3 ou 4 selon le mois), délimitée par
   `<!-- PRODUIT n --> … <!-- FIN PRODUIT n -->` (ou `BIEN n` côté immobilier) :
   logo de la compagnie, titre, description courte, chiffres clés
   (durée / coupon annualisé / capital, ou prix / rendement / fiscalité),
   lien du bouton d'appel à l'action.

Seules ces cases placements et le badge du mois sont censés varier d'un
envoi à l'autre — tout le reste (intro, "Le constat" mis à part, bandeau
optimisation fiscale, mentions légales, signature) est un élément fixe du
modèle.

Pour ajouter une case : dupliquez un bloc `<tr>…</tr>` complet entre les
commentaires "PRODUIT"/"FIN PRODUIT" (`les-filons.html` en compte 4
actuellement, `les-pepites-immobilieres.html` en compte 3). Pour en
retirer une : supprimez le bloc en entier. Ne touchez pas aux sections
**bloc légal**, **RGPD**, **bandeau citron pressé** et **signature** sans
repasser par la checklist conformité ci-dessous.

Pour chaque case placement, le format simplifié à respecter (validé par
Grégory) est volontairement minimal pour un premier contact :
- Nom du placement + type d'enveloppe (assurance vie, PER, compte-titres…)
- Une phrase : sur quel indice/action il est basé
- Durée : de 1 an à *X* ans maximum
- Coupon annualisé : *X* % par an
- Capital : "Protégé*" (protection à 100 %, hors défaut de l'émetteur) ou
  "Protégé jusqu'à *X* %" quand la protection est partielle
- "Pour plus d'informations, nous consulter."

## Images

- **`les-filons.html`** utilise les vraies images issues de votre export
  Canva (dossier `assets/`) : le visuel d'en-tête recadré en bandeau
  (`visuel-juin-2026.jpg`), les logos `logo-abeille.png` et
  `logo-generali.png` en haute résolution, chacun affiché dans un encadré
  blanc pour bien ressortir (élément de réassurance important), et votre
  photo `photo-gregory-arfi.jpg` dans la signature. Ces chemins relatifs
  s'affichent si vous ouvrez le fichier dans un navigateur depuis votre
  ordinateur (le dossier `assets/` doit rester à côté du fichier `.html`),
  mais **ne fonctionnent pas envoyés par email** (Brevo ou autre) : un
  email ne peut pas aller chercher des fichiers sur votre disque.
  **`logo-cardif.png` est un texte "CARDIF" recréé par mes soins**, faute
  d'avoir reçu le fichier logo officiel — envoyez-le-moi (ou remplacez le
  fichier vous-même, mêmes dimensions) dès que possible pour une vraie
  fidélité de marque.
- **`les-pepites-immobilieres.html`** utilise encore des espaces réservés
  `placehold.co` en attendant vos photos de biens.

Avant tout envoi (test ou groupé) :

1. Hébergez chaque image définitive : uploadez le contenu de `assets/`
   (et vos futurs visuels) dans la bibliothèque d'images de Brevo
   (Campagnes > Galerie), ou sur minleh-conseil.com.
2. Remplacez chaque `src="assets/..."` (ou `src="https://placehold.co/..."`)
   par l'URL hébergée obtenue, en gardant des dimensions proches de
   celles indiquées en commentaire au-dessus de chaque `<img>` pour ne pas
   déformer la mise en page.
3. Le visuel d'en-tête et les logos partenaires n'ont besoin d'être changés
   que lorsque le produit ou l'assureur change ce mois-ci ; votre photo et
   le logo Minleh Conseil peuvent rester d'un mois sur l'autre.
4. Le logo Minleh Conseil (écusson + colonne) est recréé en SVG inline à
   partir de votre PDF — pratique car il ne dépend d'aucune image externe,
   mais ce n'est pas le fichier vectoriel officiel. Si vous avez le logo
   d'origine en SVG/PNG haute définition, il est préférable de le faire
   remplacer par un développeur pour une fidélité de marque parfaite.

## Import dans Brevo

1. Brevo > **Campagnes** > **Créer une campagne email**.
2. Choisissez **Éditeur "Glisser-déposer"** puis **Importer un code HTML**
   (ou, si vous créez un **modèle**, Templates > Créer un modèle > Code HTML).
3. Collez l'intégralité du fichier `.html`.
4. Renseignez l'objet et le nom d'expéditeur. Objets suggérés :
   - Filons : *« Les Filons du mois — [Mois Année] : [X] opportunités
     sélectionnées pour vous »*
   - Pépites Immobilières : *« Les Pépites Immobilières — [Mois Année] :
     [X] biens sélectionnés pour vous »*
5. Envoyez-vous un **test** et vérifiez impérativement le rendu sur mobile
   et sur Outlook (icône "aperçu" de Brevo, ou envoi test à une adresse
   Outlook) avant tout envoi groupé.
6. Vérifiez que vos listes de diffusion (clients / prospects / apporteurs
   d'affaires) ont bien donné leur consentement à recevoir ces
   communications, et segmentez si besoin (le contenu et le ton diffèrent
   selon qu'on s'adresse à un client, un prospect ou un apporteur).

Balises Brevo déjà intégrées, à ne pas modifier :
- `{{ mirror }}` — lien "voir dans le navigateur"
- `{{ unsubscribe }}` — lien de désinscription (obligatoire, généré par Brevo)
- `{{contact.FIRSTNAME}}` — prénom du destinataire

## Où mènent les boutons "Je souhaite en savoir plus" / "Je réserve mon rendez-vous" ?

Ce sont des liens `mailto:` vers **g.arfi@minleh-conseil.com**, avec un objet
d'email pré-rempli selon le produit (ex. "Phoenix Mémoire Target Tec 10 - Je
souhaite en savoir plus"). Concrètement : le client clique, son propre
logiciel de messagerie s'ouvre avec un email déjà adressé et déjà rédigé
dans l'objet, il n'a plus qu'à cliquer sur "Envoyer". L'adresse email est la
même partout (intro, chaque case placement, RGPD, bandeau fiscal) : votre
adresse personnelle `g.arfi@minleh-conseil.com`, plus le rappel juste sous
le numéro de téléphone ("Pas de réponse immédiate ? Envoyez-moi un message,
je vous rappelle rapidement.").
Si vous préférez que ces boutons ouvrent plutôt un appel téléphonique
(`tel:+33774444474`) ou une page de prise de rendez-vous en ligne (Calendly
ou autre), dites-le-moi et je remplace tous les liens en une fois.

## Points de conformité RGPD / CIF — à faire valider avant premier envoi

Ces modèles reprennent les mentions déjà présentes dans votre document
existant (SIREN, ORIAS, ANACOFI, avertissement risques, désinscription,
droits RGPD), mais **vous restez responsable de la conformité finale** :

- Le bloc légal de `les-filons.html` reprend quasi mot pour mot le texte de
  votre document d'exemple (déjà utilisé par vous). Il reste néanmoins
  recommandé de le faire revalider périodiquement par votre RCCI/ANACOFI.
- Le bloc légal de `les-pepites-immobilieres.html` contient un texte
  **rédigé pour ce modèle** (l'immobilier n'a pas la même base réglementaire
  que les produits financiers : pas de DIC PRIIPs sur un achat immobilier
  direct, notion de risque locatif plutôt que de risque de marché
  financier, etc.). **Ce paragraphe doit être relu et validé par votre RCCI
  avant le premier envoi**, en particulier la formulation de votre rôle
  (CGP / mise en relation avec des partenaires) vis-à-vis des biens
  présentés.
- Assurez-vous que chaque destinataire a bien consenti à recevoir ce type
  de communication commerciale (opt-in), que le lien de désinscription
  fonctionne, et que vos traitements de données sont bien déclarés dans
  votre registre RGPD interne.
- Conservez la mention "Communication à caractère publicitaire" et
  l'avertissement sur les risques sur toute variante future du modèle.
- Le bandeau "citron pressé" (optimisation fiscale) en bas de newsletter
  promet de "réduire votre imposition par deux" : c'est une accroche
  volontairement forte, mais gardez à l'esprit qu'elle engage — un résultat
  chiffré aussi précis doit rester défendable au cas par cas selon la
  situation du client. À vous de juger si la formulation vous convient
  telle quelle ou si vous préférez une tournure plus prudente (ex. "réduire
  significativement votre imposition").
