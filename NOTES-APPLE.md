# ForzaTuneLap — notes pour iOS et macOS

**Rien de ce qui concerne Apple n'a été compilé ni exécuté.** Le code est écrit pour rester sûr (pas de réflexion, JSON généré à la compilation, types de fichiers déclarés à la main), mais il n'a jamais tourné sur iPhone, iPad ni Mac. Ce fichier liste ce qu'il faut vérifier le jour où un Mac est disponible. Ce qui est vérifié ailleurs : Windows (toutes les fonctions) et Android (Galaxy J6, Android 9).

Les commandes et réglages ci-dessous sont à confirmer au moment de s'en servir : les outils Apple évoluent, et je ne peux pas les essayer ici.

## 1. Ce qu'il faut

- Un Mac avec **Xcode** (la version qu'exige .NET 10 pour MAUI), et un compte Apple (gratuit pour tester sur son propre appareil ; payant pour distribuer).
- Le SDK .NET 10 et la charge de travail MAUI : `dotnet workload install maui`.
- Les cibles `net10.0-ios` et `net10.0-maccatalyst` ne sont ajoutées au projet que **sous macOS** (voir `ForzaTuneLap.App.csproj`). Sous Windows, on peut aussi compiler pour iOS en s'associant à un Mac depuis Visual Studio (« Pair to Mac »).
- Identifiant de l'application : `fr.forzatunelap.app` (`ApplicationId`). Il doit être unique pour le compte Apple utilisé ; le changer si Xcode le refuse.
- Versions minimales : iOS 15, Mac Catalyst 15.

## 2. Compiler et lancer

Depuis la racine du dépôt, sur le Mac :

```
dotnet build ForzaTuneLap.App/ForzaTuneLap.App.csproj -f net10.0-maccatalyst
dotnet build ForzaTuneLap.App/ForzaTuneLap.App.csproj -f net10.0-ios
dotnet run --project ForzaTuneLap.Core.Tests
```

Les tests du Core ne dépendent d'aucune plateforme : ils doivent passer tels quels (357 au dernier rapport).

Pour lancer l'application, le plus simple est Visual Studio Code (extension .NET MAUI) ou Visual Studio, qui gèrent le simulateur, la signature et le déploiement.

## 3. À vérifier en priorité

### Compilation et démarrage
1. Les deux cibles compilent sans erreur.
2. **Création de la base** au premier lancement (SQLite via EF Core). La base est dans le dossier privé de l'application. Si le démarrage échoue, regarder d'abord le paquet natif SQLite.
3. **Compilation Release / archive.** Sur iOS, la compilation Release « coupe » le code inutilisé. EF Core n'est pas entièrement prévu pour cela. Si l'application plante seulement en Release, essayer de désactiver ou d'alléger la coupe (propriété `TrimMode`), avant de chercher ailleurs. Le JSON du projet est généré à la compilation et ne devrait pas être touché.

### Import et export (le plus fragile)
4. **Export** : le panneau de partage s'ouvre avec le fichier `forzatunelap-….json` (`FichiersMaui.PartagerAsync`). Sur **iPad**, le panneau de partage est une fenêtre flottante : si elle n'apparaît pas ou plante, renseigner une zone d'ancrage (`PresentationSourceBounds`) dans `ShareFileRequest`.
5. **Import** : le sélecteur de fichiers n'affiche que du JSON ou du texte. Les types déclarés pour Apple sont `public.json` et `public.text` (`FichiersMaui.TypesJson`). Si un fichier JSON n'est pas sélectionnable, c'est là qu'il faut regarder. Le fichier est lu avec `OpenReadAsync`.
6. **Mac Catalyst, droits du bac à sable** : j'ai ajouté `com.apple.security.files.user-selected.read-write` dans `Platforms/MacCatalyst/Entitlements.plist`, pour que l'import puisse lire un fichier choisi par l'utilisateur. Ajouté sans pouvoir l'essayer : si l'import échoue avec une erreur d'accès, c'est le premier suspect ; s'il fonctionne sans, l'entrée est inoffensive.

### Claviers
7. Le **temps du tour** utilise le clavier normal (il faut la touche « : »). Vérifier que « 1:32,450 » se tape.
8. Les champs qui acceptent des **valeurs négatives** (carrossage, parallélisme, antigéométrie) utilisent le clavier normal sur iOS et Mac (`Claviers.PourValeursNegatives`), parce que le clavier numérique d'iOS n'a pas de signe « - ». Vérifier qu'on tape « -1,5 » et que le point ou la virgule sont acceptés.
9. Les autres champs numériques gardent le clavier numérique d'iOS : vérifier la virgule décimale.

### Navigation
10. **Avertissement de modifications non enregistrées** (`PageEditionReglage` : écoute de `Shell.Navigating`). Tester le **geste de retour par glissement depuis le bord** d'iOS : il doit déclencher le même menu Enregistrer / Abandonner / Rester. Si le geste contourne l'avertissement, désactiver le geste sur cette page.
11. Le **menu de choix** (`DisplayActionSheetAsync`) sur iPad s'ouvre en fenêtre flottante : vérifier qu'il s'affiche lors de l'avertissement et de l'import.

### Affichage
12. **Icône et écran de démarrage** : générés à partir des SVG de `Resources/AppIcon` et `Resources/Splash`. Vérifier le rendu (le chronomètre ne doit pas être rogné).
13. **Langue** : `Info.plist` déclare `fr` et `en`, avec `fr` par défaut. Vérifier le changement de langue dans les Paramètres (l'application reconstruit alors son interface) et le choix « langue de l'appareil ».
14. Les listes déroulantes (`Picker`) : vérifier que la sélection n'est pas perdue quand une liste est rechargée (c'est un piège connu sous Windows).
15. Texte long dans les lignes de comparaison (critères ajoutés) : vérifier le retour à la ligne sur petit écran.

## 4. Ce qui ne devrait poser aucun problème

- La logique métier (Core) : aucune dépendance de plateforme, aucune réflexion.
- Les ressources de textes `.resx` et les JSON embarqués.
- Le stockage des préférences (`Preferences` de MAUI).

## 5. Si quelque chose casse

Noter le message d'erreur complet et la cible (iOS ou Mac Catalyst, simulateur ou appareil, Debug ou Release). Les erreurs inattendues sont aussi écrites par `JournalErreurs` dans le fichier `forzatunelap-erreurs.log` du dossier temporaire de l'application.
