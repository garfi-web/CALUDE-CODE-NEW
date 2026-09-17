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

1. Le badge du mois (`JUIN 2026` → à changer).
2. Le visuel d'en-tête.
3. Le paragraphe "Le constat" (l'accroche).
4. Chaque bloc produit / bien, délimité par
   `<!-- PRODUIT n --> … <!-- FIN PRODUIT n -->` (ou `BIEN n` côté immobilier) :
   titre, description, chiffres clés (durée/coupon/capital ou
   prix/rendement/fiscalité), lien du bouton d'appel à l'action.

Pour ajouter un produit : dupliquez un bloc `<tr>…</tr>` complet entre les
commentaires "PRODUIT"/"FIN PRODUIT". Pour en retirer un : supprimez le bloc
en entier. Ne touchez pas aux sections **bloc légal**, **RGPD** et
**signature** sans repasser par la checklist conformité ci-dessous.

## Images

Toutes les images sont actuellement des espaces réservés `placehold.co`
(pour prévisualiser la mise en page). Avant l'envoi :

1. Hébergez vos visuels réels : soit dans la bibliothèque d'images de Brevo
   (Campagnes > Galerie), soit sur minleh-conseil.com.
2. Remplacez chaque `src="https://placehold.co/..."` par l'URL définitive,
   en gardant des dimensions proches de celles indiquées en commentaire
   au-dessus de chaque `<img>` pour ne pas déformer la mise en page.
3. Le logo Minleh Conseil (écusson + colonne) est recréé en SVG inline à
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
