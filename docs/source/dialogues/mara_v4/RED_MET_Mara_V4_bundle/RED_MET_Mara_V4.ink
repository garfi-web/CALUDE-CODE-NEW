// RED MET — Mara Visual Novel Dialogue Module (V4 consolidated)
// Language: French
// Runtime: Ink / Inklecate
//
// POINTS D'ENTRÉE POUR LE JEU
//   mara_incursion_01_open / mara_incursion_01_close
//   mara_incursion_02_open / mara_incursion_02_close
//   mara_incursion_03_open / mara_incursion_03_close
//   mara_incursion_04_open / mara_incursion_04_close
//   mara_incursion_05_open / mara_incursion_05_close
//   mara_incursion_06_open / mara_incursion_06_close
//
// VARIABLES FOURNIES PAR LE JEU
//   redmet_doses              : entier >= 0, nombre total de doses consommées
//   redmet_taken_this_level  : booléen ; true si Nathan a pris une dose liée au niveau courant
//   nathan_stage              : entier 0..4 (0=T0, 1=T1, 2=T2, 3=M3, 4=M4)
//   corruption_score         : entier >= 0, score de corruption morale (échelle recommandée 0..100)
//   immoral_streak           : entier >= 0, nombre de choix immoraux majeurs consécutifs
//   mara_stage               : entier 0..4, stade visuel de Mara (M-T0..M-T4), calculé par le jeu
//   father_alive              : booléen ; false après le meurtre du père
//   child_mission_choice      : 0 si Nathan refuse/empêche la transaction, 1 s’il accepte/participe
//
// IMPORTANT — TIMING DES VARIABLES
//   - Avant l'ouverture d'une incursion : mettre à jour redmet_doses, nathan_stage,
//     corruption_score, immoral_streak et mara_stage à partir de l'état sauvegardé.
//   - redmet_taken_this_level peut rester false à l'ouverture si la dose se choisit
//     pendant le niveau. La scène d'ouverture s'appuie alors sur l'historique global.
//   - Avant la clôture : mettre redmet_taken_this_level à la valeur réelle du niveau
//     et actualiser les autres variables si les choix/consommations ont déjà été résolus.
//   - Réinitialiser redmet_taken_this_level à false pour le prochain niveau après la clôture.
//   - Le jeu, pas ce fichier, choisit les tenues et portraits à partir de mara_stage.
//     Ce fichier ne décrit ni ne verrouille les vêtements dans les dialogues.
//
// CONVENTION D'AFFICHAGE
//   Les préfixes « MARA: » et « NATHAN: » sont conservés dans le texte pour faciliter
//   l'intégration. Le moteur peut les parser en nom de locuteur si nécessaire.
//   Les descriptions visuelles sont des commentaires de production et ne doivent pas
//   apparaître comme texte de dialogue à l'écran.
//
// Les nœuds sont autonomes : le jeu peut les appeler via ChoosePathString("nom_du_knot").
// Chaque nœud se termine par DONE pour rendre la main au runtime.

VAR redmet_doses = 0
VAR redmet_taken_this_level = false
VAR nathan_stage = 0
VAR corruption_score = 0
VAR immoral_streak = 0
VAR mara_stage = 0
VAR father_alive = true
VAR child_mission_choice = 0

=== start ===
// Module dialogue : les nœuds sont appelés individuellement par le jeu.
-> DONE

// ============================================================================
// INCURSION 01 — LES REFLETS
// ============================================================================

=== mara_incursion_01_open ===
// VISUEL : portrait de face. L'expression et le portrait exacts viennent de mara_stage.
// INTENTION : Mara frappe les complexes de Nathan ; si la Red Met a déjà agi,
// elle remarque son aplomb naissant sans le récompenser trop tôt.

{redmet_doses == 0 and nathan_stage == 0:
    MARA: Attends.
    MARA: C'est vraiment toi ?
    NATHAN: Tu comptes me demander ça à chaque fois ?
    MARA: Non. Je voulais juste vérifier qu'il n'y avait pas eu une erreur de casting.
    MARA: Tu as la tête d'un type qui répète ses insultes sous la douche et qui répond « pas de souci » quand quelqu'un vient de l'humilier devant tout le monde.
    NATHAN: T'es qui, bordel ?
    MARA: Mara. Enchantée. J'aurais bien tendu la main, mais j'ai peur que tu prennes ça pour une demande en mariage.
    MARA: Tu me connais pas, tu vas me dire. C'est vrai. Et pourtant, je viens de te dire trois phrases et tu as déjà choisi entre me détester et essayer de me plaire.
    NATHAN: Je m'en fous de te plaire.
    MARA: Évidemment. Tu es venu dans un cauchemar, tu rencontres une femme qui te parle et tu passes immédiatement en mode « je m'en fous ».
    MARA: Tu as probablement passé des années à attendre de devenir quelqu'un d'autre avant de commencer à vivre. Un jour, tu serais plus beau. Un jour, tu serais plus fort. Un jour, tu saurais quoi dire aux femmes.
    NATHAN: Ferme-la.
    MARA: Et maintenant, tu es dans un endroit où personne ne te connaît, et tu continues à te comporter comme si tu devais présenter une autorisation avant d'exister.
    MARA: C'est presque mignon. Pas sexuellement, hein. N'allons pas trop vite. J'ai encore un peu de dignité.
    NATHAN: T'as fini ?
    MARA: Pas du tout. Mais je vais te laisser découvrir la suite tout seul.
    MARA: Ne confonds pas le fait que je me moque de toi avec le fait que je te sous-estime. Pour l'instant, je fais les deux. À toi de me donner une raison de changer d'avis.
- else:
    MARA: Oh.
    NATHAN: Quoi ?
    MARA: Rien. Tu as simplement l'air un peu moins… pliable.
    NATHAN: Pliable ?
    MARA: Avant, j'avais l'impression que si je haussais un sourcil, tu allais me demander pardon d'avoir attiré mon attention.
    MARA: Tu as changé ta façon de te tenir. Tu regardes les gens un peu plus longtemps. Tu prends davantage de place.
    NATHAN: Tu aimes ça ?
    MARA: J'aime les changements. Ça évite aux conversations d'être aussi ennuyeuses que leur propriétaire.
    MARA: Mais attention, mon grand. Tu as acheté un peu de confiance, pas encore une personnalité.
    NATHAN: Tu veux savoir ce que je supporte pas ? Les femmes qui parlent trop.
    MARA: Magnifique. Il vient de découvrir les réparties. Je suis émue.
    MARA: Continue comme ça, Nathan. Si tu deviens vraiment intéressant, je pourrais même finir par te regarder ailleurs que dans les yeux.
    {mara_stage >= 2:
        MARA: Et si tu continues à me regarder comme ça, je vais finir par croire que tu as compris où poser les yeux sans perdre tous tes moyens.
    }
    {nathan_stage >= 3 and corruption_score >= 3:
        MARA: Tu as quelque chose de plus dangereux dans le regard. J'espère que tu sais faire la différence entre être désiré et être craint.
    }
}
-> DONE

=== mara_incursion_01_close ===
// VISUEL : portrait de dos, regard par-dessus l'épaule. Fin de la première incursion.

{redmet_taken_this_level:
    MARA: Eh bien, eh bien.
    MARA: Tu as terminé le niveau, tu es encore debout et tu n'as pas passé les cinq dernières minutes à regarder tes chaussures. Trois progrès en une soirée. On devrait fêter ça.
    NATHAN: Comment ?
    MARA: Tu as une imagination très limitée quand tu es nerveux, tu sais ?
    NATHAN: Je ne suis pas nerveux.
    MARA: Bien sûr. Tu as seulement la même tête qu'un type qui vient de comprendre que la femme qu'il drague a déjà remarqué qu'il la draguait.
    MARA: Je dois reconnaître une chose : tu commences à avoir de la présence. Pas seulement quand tu te bats. Tu sembles enfin croire que tu as le droit d'être là.
    NATHAN: Tu me trouves attirant ?
    MARA: Je te trouve intéressant. Pour l'instant, c'est déjà une belle promotion.
    {mara_stage >= 2:
        MARA: Ne dépense pas tout ton nouveau courage d'un coup. Il va falloir qu'il te dure jusqu'à notre prochaine rencontre.
    - else:
        MARA: Ne t'emballe pas. Tu viens d'obtenir un entretien, pas le poste.
    }
- else:
    {redmet_doses == 0 and nathan_stage == 0:
        MARA: Alors, mon petit héros. Tu as survécu.
        NATHAN: Tu as l'air déçue.
        MARA: Pas du tout. Je pensais surtout que tu allais trouver une manière particulièrement créative de te faire tuer. J'avais presque commencé à préparer un discours.
        MARA: « Nathan. Il n'était pas très fort, pas très malin, mais il avait un magnifique sens du sacrifice… »
        MARA: Non, même en exagérant, ça ne marche pas.
        NATHAN: Tu sais quoi ? Va te faire foutre.
        MARA: Voilà. Ça, c'était mieux. Il y avait presque une émotion.
        MARA: Tu es capable de faire quelque chose de courageux, puis tu gâches le moment en regardant autour de toi pour vérifier si quelqu'un l'a remarqué.
        NATHAN: Tu ne sais rien de moi.
        MARA: Tu as raison. Et pourtant, je suis déjà fatiguée de t'entendre te défendre contre des accusations que personne n'a formulées.
    - else:
        MARA: Tu as traversé ce niveau sans la dose. Je suppose que tu attends que je te dise que tu es admirable.
        NATHAN: Je n'attends rien de toi.
        MARA: Alors c'est pratique : je ne vais pas te décevoir.
        MARA: Tu n'es pas devenu plus fort. Tu n'as pas débloqué de nouvelle arme. Mais tu es encore debout, et tu as dû gagner chaque seconde à la force de tes réflexes.
        {corruption_score >= 3:
            MARA: Tu n'es pas aussi propre que tu voudrais le croire, Nathan. Refuser une dose n'efface pas les autres décisions que tu as prises.
        - else:
            MARA: Ne transforme pas ton refus en médaille. Tu as fait un choix. Maintenant, il faut vivre avec.
        }
    }
}
-> DONE

// ============================================================================
// INCURSION 02 — LES COULOIRS DE VERRE
// ============================================================================

=== mara_incursion_02_open ===
// VISUEL : portrait de face. Le décor comporte des reflets décalés ; ne pas modifier
// la tenue dans le script, mara_stage est géré par le moteur visuel.

{redmet_doses == 0 and nathan_stage == 0:
    MARA: Tu es revenu.
    NATHAN: Tu as l'air surprise.
    MARA: Je le suis. Je pensais que tu aurais découvert une manière de te perdre dans ton propre quartier.
    MARA: Tu vois ces miroirs ? Ils sont formidables. Ils te montrent exactement ce que tu es, mais avec juste assez de déformation pour que tu puisses te convaincre que tu es mieux que ça.
    NATHAN: Tu vas arrêter de parler par énigmes ?
    MARA: Tu préfères que je te parle de ton physique ? Tu as un petit côté homme qui a commandé un costume sur Internet sans vérifier les tailles.
    MARA: Tu pourrais être méchant avec moi. Tu pourrais m'insulter, me dire de partir, essayer de reprendre le contrôle de la conversation. Mais tu passes ton temps à répondre comme si tu espérais obtenir une bonne note à la fin.
    NATHAN: Qu'est-ce que tu veux que je fasse ?
    MARA: Rien. C'est justement ça que tu ne comprends pas.
    MARA: Tu n'as pas besoin de me convaincre, Nathan. Tu n'as pas besoin de devenir fort pour que j'admette que tu existes. Tu existes déjà. C'est seulement que tu n'aimes pas beaucoup ce que tu vois.
    MARA: Si tu veux que je te désire un jour, il va falloir commencer par arrêter de me demander, toutes les trois secondes, si je te trouve désirable.
    NATHAN: Je ne t'ai rien demandé.
    MARA: Pas avec des mots.
- else:
    MARA: Je commence à voir le problème.
    NATHAN: Quel problème ?
    MARA: Tu es en train de devenir séduisant.
    MARA: Oh, cette tête ! Si j'avais su qu'il suffisait de te faire un compliment pour te faire perdre tous tes moyens, j'aurais commencé plus tôt.
    NATHAN: Tu le penses vraiment ?
    MARA: Je ne sais pas encore. Mais je commence à apprécier le spectacle.
    MARA: Je me suis toujours demandé ce que ça ferait de parler à un homme qui n'a pas l'air de vouloir s'excuser avant même d'avoir ouvert la bouche.
    NATHAN: Et alors ?
    MARA: Et alors, je découvre que c'est assez agréable. Dangereusement agréable, même.
    MARA: Ne va pas croire que je suis déjà en train d'imaginer ce que tu caches sous tes vêtements. Je suis une femme cultivée. J'ai d'abord besoin d'évaluer ton sens de la conversation.
    {mara_stage >= 2:
        MARA: Même si, entre nous, tu commences à rendre l'examen plus intéressant.
    }
    {nathan_stage >= 3:
        MARA: Et je dois reconnaître que l'idée de te faire perdre ton aplomb devient beaucoup plus amusante.
    }
}
-> DONE

=== mara_incursion_02_close ===
// VISUEL : portrait de dos, regard par-dessus l'épaule. Le niveau s'est achevé dans
// les couloirs de verre ; la scène doit rester brève, intime et inquiétante.

{redmet_taken_this_level:
    MARA: Je vais te dire quelque chose que tu vas probablement prendre comme un compliment beaucoup plus important que nécessaire.
    NATHAN: Je t'écoute.
    MARA: J'ai envie de te voir au prochain niveau.
    NATHAN: Tu veux voir si je vais survivre ?
    MARA: Ça aussi.
    MARA: Mais je me demande surtout si tu es aussi doué pour tenir une conversation quand tu n'es pas en train de distribuer des coups.
    NATHAN: Tu pourrais venir vérifier.
    MARA: Oh, Nathan. Tu viens de faire une proposition qui ressemble presque à une invitation.
    MARA: Continue comme ça et je finirai par te demander d'enlever ta veste. Pour vérifier si tu as encore quelque chose d'intéressant dessous, évidemment.
    NATHAN: Évidemment.
    MARA: Tu apprends. C'est inquiétant.
    {corruption_score >= 3:
        MARA: Mais je ne suis pas certaine d'aimer la vitesse à laquelle tu changes.
    }
- else:
    MARA: Tu sais ce que je vois, quand je te regarde ?
    NATHAN: J'imagine que tu vas me le dire.
    MARA: Un homme qui préfère avoir mal plutôt que de risquer de découvrir qu'il pourrait avoir besoin de quelque chose.
    NATHAN: Tu ne peux pas t'empêcher de salir tout ce que je fais.
    MARA: Je ne salis rien. Je te demande simplement de vérifier si tu es en train de faire le bien ou de te fabriquer une jolie histoire sur toi-même.
    {redmet_doses > 0 or nathan_stage > 0:
        MARA: Tu peux refuser la dose aujourd'hui, mais ça ne fait pas disparaître ce que tu as déjà accepté hier.
    - else:
        MARA: Tu n'es pas obligé de devenir un monstre. Mais si tu restes comme tu es, cesse de prétendre que c'est moi qui t'en empêche.
    }
}
-> DONE

// ============================================================================
// INCURSION 03 — LE SILO BLANC
// ============================================================================

=== mara_incursion_03_open ===
// VISUEL : portrait de face. Les images du décor semblent se dissoudre autour de Mara.

{redmet_doses == 0 and nathan_stage == 0:
    MARA: Tu as l'air épuisé.
    NATHAN: Merci.
    MARA: Ce n'était pas un compliment.
    MARA: Tu continues à avancer alors que tu n'as ni la force, ni l'endurance, ni le bon sens de prendre ce qui pourrait t'aider.
    NATHAN: Tu appelles ça du bon sens ?
    MARA: J'appelle ça une option. Tu peux la refuser, mais évite de me faire croire que tu n'as jamais eu envie de l'accepter.
    MARA: Il y a quelque chose de presque admirable chez toi. Tu sembles croire que devenir quelqu'un de bien devrait être une expérience agréable. Comme si ton intégrité allait finir par t'offrir un dîner gratuit, un meilleur physique et une femme qui tombe amoureuse de toi.
    NATHAN: Je ne fais pas ça pour toi.
    MARA: Je sais. Et c'est probablement la seule bonne nouvelle de la journée.
    MARA: Si tu dois souffrir pour rester celui que tu veux être, combien de temps vas-tu tenir avant de m'en vouloir de ne pas t'avoir récompensé ?
- else:
    MARA: Ah. Voilà mon problème préféré.
    NATHAN: Je suis ton problème préféré ?
    MARA: Ne prends pas la grosse tête. Tu es surtout le seul problème ici qui commence à avoir des épaules intéressantes.
    MARA: Je vais être franche. Tu as changé assez pour que je me demande ce que ça donnerait de te voir dans un endroit où personne n'essaie de nous tuer.
    NATHAN: Tu veux dire un rendez-vous ?
    MARA: J'ai dit un endroit calme. Ne confonds pas ça avec une promesse de finir dans ton lit.
    MARA: Même si je dois reconnaître qu'il devient plus difficile de t'imaginer en train de trébucher sur tes propres pieds au moment le plus intéressant.
    NATHAN: Tu y penses beaucoup ?
    MARA: Assez pour que tu arrêtes de sourire comme un idiot, oui.
    MARA: Tu me plais davantage que je ne le pensais. Et ça m'agace, parce que j'ai l'impression de découvrir que tu peux être autre chose qu'un projet de rénovation.
    NATHAN: Je ne suis pas un projet.
    MARA: Pour l'instant, tu es un très beau chantier.
    {mara_stage >= 3:
        MARA: Et je commence à me demander ce qui se passerait si je cessais de te provoquer juste pour voir si tu sais encore quoi faire.
    }
}
-> DONE

=== mara_incursion_03_close ===
// VISUEL : portrait de dos. Appeler ce nœud après la décision liée au cœur et ses conséquences.
// IMPORTANT : la conséquence familiale dépend de father_alive, pas de la consommation.

{father_alive == false:
    {nathan_stage >= 2 and corruption_score >= 3:
        MARA: Alors ? Tu as obtenu ce que tu voulais ?
        NATHAN: Oui.
        MARA: Et pourquoi tu as l'air de quelqu'un qui vient de découvrir qu'il y a un cadavre dans sa cuisine ?
        NATHAN: Tu ne sais pas de quoi tu parles.
        MARA: Non. Raconte-moi.
        NATHAN: Je n'ai pas envie d'en parler.
        MARA: Tu as pris une décision assez grande pour changer ta vie, et maintenant tu voudrais qu'on parle de la pluie.
        MARA: Tu sais ce qui est horrible ? Je te trouve plus attirant qu'avant. Je vois comment tu te tiens, comment tu me regardes, comment tu as arrêté de demander pardon avant même de toucher une poignée de porte.
        MARA: Et une partie de moi a envie de te provoquer, juste pour voir jusqu'où tu peux aller.
        MARA: Mais je ne veux pas devenir la fille qui te donne envie de continuer à faire des choses que tu ne peux pas supporter.
        NATHAN: Je ne te demande pas ça.
        MARA: Pas avec des mots.
        MARA: Tu veux que je te trouve beau. Que je te trouve fort. Que je te regarde comme si tu avais gagné quelque chose.
        NATHAN: Tu me trouves beau, oui ou non ?
        MARA: Oui.
        MARA: Et c'est justement pour ça que je suis en colère contre toi. Mon désir ne rendra jamais cet acte moins réel.
        MARA: Je peux avoir envie de toi, Nathan. Ça ne veut pas dire que je vais te tenir la main pendant que tu apprends à te détester.
    - else:
        MARA: Tu as fait quelque chose d'irréversible, Nathan.
        NATHAN: Tu ne sais pas ce qui s'est passé.
        MARA: Non. Mais je vois ton visage. Tu veux que je te demande si tu vas bien pour pouvoir répondre que ça va.
        NATHAN: Je vais bien.
        MARA: Et moi, je suis la reine d'Angleterre.
        MARA: Tu n'as pas encore appris à porter ce que tu as fait. Tu as seulement appris à le cacher derrière une phrase courte.
        MARA: Ne me demande pas de te regarder comme avant. Je ne sais même pas si toi, tu arrives encore à te regarder comme avant.
    }
- else:
    {redmet_taken_this_level:
        MARA: Tu as pris ta dose, mais tu n'as pas franchi cette ligne-là.
        NATHAN: Tu as l'air surprise.
        MARA: Je suis surprise que tu sois encore capable de séparer deux choses que tu as tellement envie de confondre : ce que tu désires et ce que tu acceptes de faire pour l'obtenir.
        MARA: Tu as changé, Nathan. Une partie de moi trouve ça attirant. Une autre se demande ce que tu vas faire quand une dose ne suffira plus à te donner l'impression d'être quelqu'un.
        MARA: Ne me regarde pas comme si j'allais te donner une réponse. Je ne suis pas ton mode d'emploi.
    - else:
        {redmet_doses == 0 and nathan_stage == 0:
            MARA: Tu as refusé.
            NATHAN: Oui.
            MARA: Tu as l'air surpris de toi-même.
            NATHAN: Je pensais que tu allais te moquer.
            MARA: Je peux toujours le faire, si ça t'aide à retrouver tes repères.
            MARA: Tu veux que je te dise que tu as fait le bon choix ? Que tu as été courageux ? Que tu mérites une médaille et un massage des épaules ?
            NATHAN: Non.
            MARA: Alors pourquoi tu as cette tête ?
            MARA: Tu voudrais refuser, te sentir fier, puis continuer ta vie en attendant que quelqu'un vienne reconnaître tes mérites.
            MARA: Tu vas rester faible, Nathan. Tu vas devoir recommencer le prochain niveau avec les mêmes deux poings et les mêmes jambes fatiguées. Et personne ne va te garantir que tu seras récompensé pour ce que tu viens de faire.
            MARA: C'est peut-être ça, le courage. Faire quelque chose sans savoir si quelqu'un t'en remerciera.
            MARA: Ne crois pas que ça me donne envie de t'embrasser. Il y a des limites à mon manque de jugement.
        - else:
            MARA: Tu as refusé la dose aujourd'hui. Je suppose que tu attends que je te dise que tu es admirable.
            NATHAN: Je n'attends rien de toi.
            MARA: Alors c'est pratique : je ne vais pas te décevoir.
            MARA: Tu n'es pas devenu plus fort, mais tu es encore debout. Ne transforme pas ton refus en médaille. Tu as fait un choix. Maintenant, il faut vivre avec.
        }
    }
}
-> DONE

// ============================================================================
// INCURSION 04 — LE PARC DES SOURIRES
// ============================================================================

=== mara_incursion_04_open ===
// VISUEL : portrait de face. Le parc et ses couleurs trop vives forment le contrepoint.

{redmet_doses == 0 and nathan_stage == 0:
    MARA: Bienvenue au parc des merveilles !
    MARA: Ici, les enfants rient, les adultes mentent et les monstres ont l'air de mascottes pourries. Tu vas te sentir comme chez toi.
    NATHAN: Tu es particulièrement insupportable aujourd'hui.
    MARA: Merci. J'ai travaillé mon personnage.
    MARA: Toi, en revanche, tu as l'air d'avoir dormi dans un abribus après une dispute avec ton propre destin.
    NATHAN: Tu peux être sérieuse deux secondes ?
    MARA: Pourquoi ? Tu comptes me dire quelque chose d'important ?
    MARA: Tu sais, il y a une chose que je respecte chez toi. Tu peux être terriblement pénible, tu peux avoir peur de tout, tu peux même être assez lâche quand personne ne regarde. Mais jusqu'ici, tu n'as pas encore décidé que tes besoins passaient avant absolument tout le monde.
    NATHAN: Tu penses que je vais céder ?
    MARA: Je pense que tu vas être tenté. Et je pense que tu vas chercher une bonne raison de céder si la tentation devient suffisamment forte.
    MARA: La question, ce n'est pas de savoir si tu es un homme bien aujourd'hui. C'est de savoir ce que tu feras quand tu auras enfin une excuse qui ressemble à une bonne raison.
    MARA: Allez, va jouer, petit héros. Et essaie de ne pas confondre le courage avec le fait de mourir lentement pour pouvoir dire que tu avais raison.
- else:
    MARA: J'adore cet endroit.
    NATHAN: Tu te fous de moi ?
    MARA: Évidemment. C'est un cauchemar avec des chevaux en plastique. Mais il y a quelque chose de charmant dans l'effort que les gens font pour rendre les choses horribles un peu plus jolies.
    MARA: Un peu comme toi, finalement.
    NATHAN: Tu me compares à un cheval en plastique ?
    MARA: Non. Le cheval, lui, ne passe pas son temps à essayer de me convaincre qu'il est irrésistible.
    {mara_stage >= 2 and nathan_stage >= 2:
        MARA: Je commence à avoir des pensées très inconvenantes à ton sujet.
        NATHAN: Tu pourrais le faire au lieu d'en parler.
        MARA: Oh ! Il est devenu audacieux. Je vais finir par devoir surveiller mes propres répliques.
        MARA: Tu me plais davantage que je ne le pensais. Et ça m'agace, parce que j'ai l'impression de découvrir que tu peux être autre chose qu'un projet de rénovation.
        NATHAN: Je ne suis pas un projet.
        MARA: Pour l'instant, tu es un très beau chantier.
        {mara_stage >= 3:
            MARA: Et je commence à me demander si je vais réussir à garder mes distances aussi facilement que je le prétends.
        }
    - else:
        MARA: Tu commences à me surprendre. Ne prends pas cet air : j'ai dit surprendre, pas me faire perdre la tête.
        MARA: Tu as encore cette manière de chercher mon approbation avant de répondre. Mais au moins, tu essaies de tenir mon regard.
        MARA: Continue, et je finirai peut-être par m'intéresser à ce que tu dis plutôt qu'à la manière dont tu essaies de le dire.
    }
}
-> DONE

=== mara_incursion_04_close ===
// VISUEL : portrait de dos, la musique de fête foraine s'éloigne.
// IMPORTANT : child_mission_choice décide de la réaction morale ; le fait d'avoir pris
// une dose ne prouve pas, à lui seul, que Nathan a accepté cette mission.

{child_mission_choice == 1:
    MARA: Tu as accepté.
    NATHAN: Oui.
    MARA: Tu sais ce qui me fait le plus peur ? Ce n'est pas que tu sois capable de faire quelque chose d'horrible. Les humains sont capables de choses horribles depuis bien avant notre naissance.
    MARA: C'est que tu arrives encore à me regarder en espérant que je vais te trouver attirant après.
    NATHAN: Je ne te demande pas de me pardonner.
    MARA: Non. Tu me demandes quelque chose de beaucoup plus compliqué.
    NATHAN: Quoi ?
    MARA: Tu veux que je puisse te désirer sans avoir à penser à ce que tu as fait.
    MARA: Une partie de moi le peut. C'est ça qui m'énerve. Je te regarde et je vois l'homme que tu es devenu. Je peux être attirée par ton assurance, par ta présence, par la façon dont tu n'hésites plus à me regarder dans les yeux.
    NATHAN: Alors pourquoi tu continues à venir me voir ?
    MARA: Parce que je veux savoir si tu vas encore réussir à me surprendre. Parce que j'ai envie de découvrir qui tu es sous tout ça. Et parce que je ne suis pas aussi raisonnable que j'aime le prétendre.
    MARA: Et parce que, merde, il faut bien reconnaître que tu as fini par devenir un sacré spectacle.
- else:
    MARA: Tu as refusé.
    NATHAN: Oui.
    MARA: Cette fois, tu n'as même pas essayé de négocier avec toi-même ?
    NATHAN: Non.
    MARA: C'est nouveau.
    MARA: Ne crois pas que je vais te dire que tu es un saint. Tu es toujours arrogant, tu as toujours besoin qu'on remarque tes efforts, et je pense que tu serais capable de te disputer avec un panneau de signalisation si tu croyais qu'il te manquait de respect.
    MARA: Mais tu as refusé de participer à quelque chose que tu savais immonde, même si tu savais que tu allais le payer.
    NATHAN: Tu es en train de me complimenter ?
    MARA: Ne t'habitue pas. Ça me donne des rides.
    MARA: Je crois que tu vas découvrir un jour que tu n'as pas besoin de devenir puissant pour être quelqu'un. Mais je ne te promets pas que ce sera agréable.
    {redmet_taken_this_level:
        MARA: Et oui, tu as pris ta dose. Ne va pas croire que je n'ai pas remarqué. Tu peux avoir cédé sur un point sans avoir cédé sur tous les autres.
        MARA: C'est presque décevant : tu es encore capable de me surprendre pour autre chose que tes muscles.
    }
    MARA: Et si tu me demandes si je te trouve attirant parce que tu as fait le bon choix, je t'envoie personnellement dans une colonie de vacances.
    NATHAN: Je n'allais pas te le demander.
    MARA: Bien sûr que non.
}
-> DONE

// ============================================================================
// INCURSION 05 — LE THÉÂTRE DES DETTES
// ============================================================================

=== mara_incursion_05_open ===
// VISUEL : portrait de face. Salle immense, silhouettes sans visage, une place vide.

{redmet_doses == 0 and nathan_stage == 0:
    MARA: Je commence à croire que tu vas vraiment aller jusqu'au bout.
    NATHAN: Tu sembles déçue.
    MARA: Je suis déçue par beaucoup de choses. Par exemple, par le fait que tu ne sois toujours pas devenu drôle.
    MARA: Mais je suis aussi un peu curieuse. Tu refuses la Red Met, tu te bats avec tes deux poings, tu encaisses des coups que tu pourrais éviter en choisissant la solution que tu refuses. Je me demande ce qui te fait tenir.
    NATHAN: Je sais ce que je ne veux pas devenir.
    MARA: C'est déjà quelque chose. Mais ce n'est pas exactement la même chose que savoir qui tu veux être.
    MARA: Tu peux passer toute ta vie à refuser de devenir ton pire cauchemar. À un moment, il faudra aussi décider ce que tu veux construire à la place.
    NATHAN: Tu es en train de me donner des conseils ?
    MARA: Je suis en train de te dire que ton manque de personnalité commence à devenir un problème narratif.
    MARA: Allez, va te battre. Au moins, quand tu te bats, tu arrêtes de chercher la bonne réponse.
- else:
    MARA: Je réfléchis.
    NATHAN: C'est dangereux.
    MARA: Pour toi, peut-être. Pour moi, c'est une activité relativement nouvelle.
    MARA: Tu as de la présence, Nathan. Tu sais entrer dans une pièce. Tu sais regarder une femme sans avoir l'air de demander une autorisation écrite.
    MARA: C'est presque dommage. Tu aurais pu apprendre tout ça sans devenir aussi compliqué.
    NATHAN: Tu me trouves compliqué ?
    MARA: Tu es une énigme qui a été passée dans un broyeur industriel.
    MARA: Mais je dois reconnaître quelque chose. Je me demande parfois ce que tu ferais si je te demandais de rester ici avec moi, loin des combats et de tout ce bordel.
    NATHAN: Tu me le demandes ?
    MARA: Pas encore.
    {nathan_stage >= 3 and mara_stage >= 3:
        MARA: Je voudrais voir si tu sais rester dans une pièce avec une femme qui te désire sans avoir besoin de lui prouver quoi que ce soit.
        MARA: Je voudrais savoir si tu es capable d'être attirant sans être dangereux. Si tu peux être fort sans avoir besoin de gagner. Si tu peux me regarder sans essayer de posséder la réponse à toutes mes questions.
        MARA: C'est beaucoup demander, je sais. Mais tu commences à me donner envie d'essayer.
        {mara_stage >= 4:
            MARA: Ne fais pas semblant de ne pas comprendre ce que je te dis. Tu es devenu très fort, mais je regarde encore ce que tu fais de cette force.
        }
    - else:
        MARA: Je me demande parfois ce que tu ferais si tu restais ici avec moi, loin des combats et de tout ce bordel.
        NATHAN: Tu me le demandes ?
        MARA: Pas encore. Ne va pas transformer une curiosité en contrat signé.
        MARA: Tu commences à me surprendre, Nathan. J'aimerais voir si tu sais tenir une conversation sans te défendre contre une insulte que je n'ai pas encore faite.
    }
}
-> DONE

=== mara_incursion_05_close ===
// VISUEL : portrait de dos devant la scène vide. Le ton dépend de la progression globale.

{nathan_stage >= 3 and mara_stage >= 3:
    MARA: Je vais te dire quelque chose, et tu vas essayer de ne pas en faire toute ta personnalité.
    NATHAN: Je t'écoute.
    MARA: J'ai envie de toi.
    NATHAN: Tu y penses beaucoup ?
    MARA: Assez pour que tu arrêtes de sourire comme un idiot.
    MARA: Oui, tu me plais. Oui, j'aime la façon dont tu as changé. Oui, je me demande ce que ça ferait de t'embrasser sans devoir entendre une seule seconde de tes petites phrases d'homme mystérieux.
    MARA: Mais je dois te dire quelque chose. Je ne veux pas que tu continues à faire des choses que tu ne supportes pas simplement parce que tu penses que je vais te trouver plus attirant.
    NATHAN: Tu es attirée par moi.
    MARA: Oui. Mais je ne suis pas un contrat d'assurance-vie. Tu ne peux pas prendre toutes les décisions du monde en pensant que je t'attendrai à la fin.
- else:
    {redmet_taken_this_level:
        MARA: Tu as encore changé. Ce n'est plus seulement une question de posture. Tu commences à croire ce que tu racontes quand tu entres dans une pièce.
        NATHAN: Et ça te plaît ?
        MARA: Ça m'intrigue. Et tu sais ce qui est dangereux avec moi ? Quand quelque chose m'intrigue, j'ai tendance à vouloir m'approcher pour voir si ça mord.
        MARA: Ne prends pas ça pour une invitation à faire le malin. Je suis encore en train de décider si tu es intéressant ou simplement très doué pour te mettre en scène.
    - else:
        MARA: Tu sais ce que tu as gagné ?
        NATHAN: Non.
        MARA: Rien.
        MARA: Enfin, rien de ce que tu peux compter. Tu n'as pas gagné de muscles. Tu n'as pas gagné d'armes. Tu n'as pas gagné ma gratitude, et tu n'as certainement pas gagné le droit de me demander de t'embrasser pour te récompenser.
        NATHAN: Je ne te demande pas ça.
        MARA: Je sais.
        MARA: Tu es encore faible. Tu es encore terrifié. Et tu n'es pas devenu l'homme que tu voulais être.
        MARA: Mais tu es toujours capable de choisir ce que tu refuses de faire.
        MARA: Ne sous-estime pas cette capacité. Le monde va passer beaucoup de temps à essayer de te la faire oublier.
    }
}
-> DONE

// ============================================================================
// INCURSION 06 — LE DERNIER SEUIL
// ============================================================================

=== mara_incursion_06_open ===
// VISUEL : portrait de face. Le monde parallèle se déchire ; des fragments des lieux
// précédents peuvent apparaître en arrière-plan. Le choix du portrait dépend de mara_stage.

{nathan_stage >= 3 and corruption_score >= 3:
    MARA: Merde.
    NATHAN: Tu l'as déjà dit.
    MARA: Je sais. J'avais préparé quelque chose de beaucoup plus élégant, mais tu as ce talent très particulier pour me faire perdre le fil de mes pensées.
    MARA: Tu es devenu l'homme que tu voulais être.
    NATHAN: Et tu me désires.
    MARA: Oui. Je te désire.
    MARA: Et je vais même te faire la faveur de ne pas prétendre que c'est uniquement à cause de tes épaules. Ce serait injuste pour tes autres qualités.
    MARA: Mais je dois t'avouer quelque chose. Je ne sais plus très bien si je te trouve irrésistible ou si je suis simplement fascinée par la catastrophe que tu es devenu.
    NATHAN: Tu peux faire les deux.
    MARA: Oh, je sais. C'est précisément ce qui m'inquiète.
    MARA: Tu as appris à prendre ce que tu voulais. Tu as appris à faire peur. Tu as appris à gagner. Tu as passé tellement de temps à chercher une manière de ne plus jamais être le garçon qu'on ignorait que tu as fini par devenir un homme qu'on ne peut plus ignorer.
    MARA: Et pourtant, quand tu me regardes, je vois encore le type qui se demandait s'il était assez bien pour m'adresser la parole.
    NATHAN: Il a disparu.
    MARA: Non. Il est là. Tu as juste appris à le faire taire.
    MARA: Tu sais ce que j'aimerais ? Une soirée normale. Un bar, un verre, une conversation. Pas de monstres, pas de sang, pas de cris. Et pas de Nathan qui cherche à savoir si je le trouve assez viril.
    NATHAN: Tu crois qu'on pourrait avoir ça ?
    MARA: Je ne sais pas.
    MARA: Mais si tu continues à me regarder comme ça, je pourrais être tentée de découvrir ce que tu sais faire quand tu n'as pas besoin de massacrer quelque chose pour attirer mon attention.
    MARA: Seulement, je veux que tu comprennes une chose avant d'aller plus loin. Je ne suis pas ton prix. Je suis Mara. Je peux te vouloir, je peux te refuser, je peux changer d'avis. Et si tu ne supportes pas cette idée, alors tu n'es pas encore aussi fort que tu le crois.
    MARA: Je veux que tu me désires parce que je suis là, devant toi. Pas parce que tu as passé ta vie à essayer de gagner tout ce qui t'avait échappé.
    MARA: Alors, Nathan… tu es encore capable de me parler sans chercher à gagner ?
- else:
    {redmet_doses == 0 and nathan_stage == 0:
        MARA: Alors, Nathan. On est arrivés au dernier chapitre.
        NATHAN: Tu as l'air déçue.
        MARA: Je pensais surtout que tu finirais par céder. C'est la partie la plus agaçante de cette histoire : j'ai passé tout ce temps à te provoquer, à te rabaisser, à te dire que tu étais trop faible pour survivre ici… et tu es encore là.
        MARA: Tu n'es pas devenu plus fort comme tu l'espérais. Tu n'as pas débloqué l'arsenal de tes rêves. Tu es toujours obligé d'observer, d'esquiver et de frapper au bon moment.
        NATHAN: Et pourtant, je suis arrivé jusqu'ici.
        MARA: Oui.
        MARA: Je n'aime pas toujours ce que tu es. Je ne suis pas amoureuse de toi. Je ne vais pas soudainement te trouver irrésistible parce que tu as résisté à la tentation.
        MARA: Mais je crois que tu as enfin compris quelque chose : tu n'as pas besoin de devenir celui que les autres désirent pour décider de ce que tu veux être.
        NATHAN: C'est ton discours d'adieu ?
        MARA: Ne t'emballe pas. J'ai encore quelques insultes en réserve.
        MARA: Arrête de croire que ton choix te rend supérieur aux autres. Tu as refusé. C'est important. Mais ça ne te donne pas le droit de regarder les gens qui ont cédé comme si tu étais né meilleur qu'eux.
        MARA: Tu ne sais pas ce qu'ils ont perdu avant d'arriver ici. Tu ne sais pas ce qu'ils avaient peur de perdre. Tu ne sais pas ce qu'ils se racontent pour continuer à respirer.
        MARA: Tu as fait ton choix, Nathan. Maintenant, tiens-toi à lui. Pas pour moi. Pas pour obtenir une récompense. Pour toi.
        MARA: Et maintenant, va sauver le monde. Mais fais attention : si tu trébuches au dernier niveau, je vais me foutre de toi pendant des siècles.
    - else:
        MARA: Tu as changé, Nathan. Pas assez pour devenir l'homme que tu imaginais, mais suffisamment pour que je ne puisse plus faire semblant de ne rien remarquer.
        MARA: Tu as gagné de l'assurance, tu as appris à te battre, et tu as découvert que certaines portes s'ouvrent plus facilement quand tu as de la force.
        MARA: Mais tu n'es pas encore arrivé au bout de ce que tu cherches.
        {redmet_doses > 0 and nathan_stage >= 2:
            MARA: Et ne crois pas que je n'ai rien remarqué. Tu commences à me troubler. Ce n'est pas encore une déclaration, ne prends pas cet air satisfait.
        }
        MARA: Il reste une dernière chose à découvrir, Nathan : ce qu'il reste de toi quand tu as enfin la possibilité de devenir tout ce que tu voulais.
    }
}
-> DONE

=== mara_incursion_06_close ===
// VISUEL : portrait final de Mara, sélectionné par mara_stage. La conclusion du jeu
// (effondrement, coma et RED MET 2) est appelée par le jeu après cette scène.
// Le choix de trajectoire est global : ne pas utiliser uniquement redmet_taken_this_level.

{nathan_stage >= 3 and corruption_score >= 3:
    MARA: Tu as fini par y arriver.
    NATHAN: À quoi ?
    MARA: À devenir quelqu'un qu'on remarque.
    MARA: Je ne vais pas te mentir. Tu es attirant, Nathan. Tu as une présence que tu n'avais pas au début. Tu n'as plus l'air de vouloir t'excuser d'être dans la même pièce que moi.
    MARA: Et oui, j'ai envie de toi. Je me demande même ce que tu ferais si je te disais de venir ici, juste à côté de moi, et de fermer enfin cette bouche qui te sert à tout gâcher.
    MARA: Ne t'emballe pas. Je n'ai pas dit que tu pouvais arrêter de réfléchir. Je te demande simplement de le faire ailleurs.
    MARA: Je peux te vouloir et te condamner. Je peux te trouver beau et penser que tu as fait quelque chose d'impardonnable. Je peux avoir envie de t'embrasser et refuser de te servir d'excuse pour continuer à te détruire.
    MARA: Tu voulais être assez fort pour obtenir tout ce que tu désirais. Eh bien, tu as obtenu une partie de ce que tu voulais.
    MARA: Maintenant, regarde ce que ça t'a coûté.
- else:
    {redmet_doses == 0 and corruption_score == 0:
        MARA: Tu as tenu.
        NATHAN: Oui.
        MARA: Tu as souffert.
        NATHAN: Oui.
        MARA: Et tu es arrivé jusqu'ici.
        NATHAN: Oui.
        MARA: Je pensais que tu finirais par céder. C'est ce que font beaucoup de gens ici.
        NATHAN: Je ne suis pas beaucoup de gens.
        MARA: Non. Tu es Nathan. C'est déjà suffisamment compliqué.
        MARA: Je ne vais pas te dire que tu es parfait. Tu es pénible, tu es complexé, tu as parfois l'air de demander pardon avant même d'avoir fait quelque chose. Et tu as passé une quantité franchement inquiétante de temps à essayer de me prouver que tu étais quelqu'un que je pourrais désirer.
        NATHAN: Et maintenant ?
        MARA: Maintenant, je pense que tu dois apprendre à vivre sans attendre que je te donne la réponse.
        NATHAN: Tu ne veux pas de moi.
        MARA: Pas comme tu le voudrais. Et je ne vais pas te mentir pour rendre ton histoire plus confortable.
        MARA: Mais je te respecte, Nathan. Pas parce que tu as refusé la Red Met. Pas parce que tu as traversé cet endroit avec deux poings et une santé ridicule. Je te respecte parce que tu as accepté de ne pas avoir tout ce que tu voulais.
        MARA: Tu sais ce qui est triste ? Il t'a fallu tout cet enfer pour commencer à comprendre que le monde ne te doit rien.
        MARA: C'est une leçon assez chère. Essaie au moins de ne pas l'oublier.
    - else:
        MARA: Tu es arrivé jusqu'ici, mais je ne sais pas encore si tu es devenu l'homme que tu voulais être ou si tu as simplement appris à jouer son rôle.
        NATHAN: Tu me trouves attirant ?
        MARA: Tu as changé. Je le vois. Et oui, je peux être attirée par toi. Mais je ne vais pas te mentir en prétendant que tes muscles répondent à toutes les questions.
        MARA: Tu as pris des doses. Tu as fait des choix. Certains t'ont changé, d'autres ont changé les gens autour de toi. Tu ne peux pas tout résumer à une silhouette dans un miroir.
        MARA: Tu as encore du chemin à faire, Nathan. Et ce chemin ne sera pas plus facile simplement parce que tu as enfin appris à faire peur.
    }
}
-> DONE

// ============================================================================
// FIN DU MODULE
// ============================================================================
