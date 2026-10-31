# ForzaTuneLap — CLAUDE.md

Application compagnon pour **Forza Motorsport (2023)**. Ce fichier résume le but du projet, les décisions prises, l'état d'avancement et les règles de travail. À placer à la racine du dossier `ForzaTuneLap`.

## 1. But de l'application

- Stocker **tous les réglages** (plusieurs par voiture).
- Stocker le **meilleur chrono par parcours** (tracé d'un circuit) **et par réglage**.
- **Comparer des réglages** selon n'importe quel critère de la voiture.
- **Comparer des chronos** avec des filtres (sur un parcours précis, filtres sur les critères de la voiture).

## 2. Contraintes

- EF Core + SQLite, **données uniquement locales**.
- Outils gratuits : VS Code, Visual Studio 2026, Android Studio.
- Cibles : **Windows, Android, macOS, iOS**. L'utilisateur ne peut **pas compiler Apple** : ce code est écrit sans pouvoir être vérifié, rester prudent (pas de réflexion, JSON généré par source generator). Voir `NOTES-APPLE.md`.
- Interface en **français et anglais**, **unités au choix** de l'utilisateur.
- Le jeu est terminé (plus de mises à jour) : le référentiel est figé.

## 3. Préférences de l'utilisateur (à respecter)

- Avancer **point par point**, poser **une question à la fois** avant de coder quand c'est ambigu.
- **Aucun acronyme** dans les noms. Noms en français.
- Les noms de divisions restent ceux du jeu (ex. « Forza GT2 »).
- **Ne jamais rajouter `using Microsoft.Extensions.DependencyInjection;` dans `App.xaml.cs`** (inutilisé, l'utilisateur l'a retiré plusieurs fois ; `GetRequiredService` y compile sans).
- Utiliser `DisplayAlertAsync` (pas `DisplayAlert`).
- Les tests xUnit : utiliser `TestContext.Current.CancellationToken` (xUnit1051 désactivé via `.editorconfig` du projet de tests).
- L'utilisateur compile et teste sous Windows et sur un téléphone Android, et rapporte les résultats.
- Ne pas saisir de données du jeu de mémoire (voitures, valeurs d'origine) : l'utilisateur les saisit lui-même ou fournit une source.

## 4. Environnement technique

- .NET 10, .NET MAUI (MVVM, CommunityToolkit.Mvvm).
- EF Core 10.0.13 + SQLite (paquets Sqlite et Design).
- xunit.v3 4.0.2, Test SDK 18.11.0, runner 4.0.1.
- Solution `ForzaTuneLap.slnx`, 3 projets :
  - `ForzaTuneLap.Core` (net10.0) : **toute la logique métier**.
  - `ForzaTuneLap.Core.Tests` : tests xUnit, SQLite en mémoire (`BaseDeDonneesDeTest`, `FabriqueDeContextesDeTest`).
  - `ForzaTuneLap.App` (MAUI) : **interface uniquement**.
- TargetFrameworks MAUI : android toujours ; windows si on est sous Windows ; ios et maccatalyst seulement sous macOS.
- **Android : architectures `android-arm64;android-arm;android-x64`** (propriété `RuntimeIdentifiers` du `.csproj`). Sans `android-arm`, l'installation échoue sur les téléphones ARM 32 bits (par exemple le Galaxy J6, Android 9) avec `ADB0020`. Minimum : Android 5 (API 21).
- **Tout changement du modèle EF exige une migration** (sinon `PendingModelChangesWarning` fait échouer tous les tests de base). Migrations existantes : `Initiale`, `DateModificationChrono`.

## 5. Structure du Core

- `Modeles/` : Referentiel.cs (Pays, Circuit, Parcours, Marque, Voiture), Reglage.cs, SectionsDeReglage.cs, Chrono.cs (DureeMillisecondes, DateModification).
- `Enumerations/` : Division (50 valeurs), Transmission (TransmissionIntegrale, Traction, Propulsion), CategorieDeReglage, SensPrefere, TypeDeGrandeur, ClasseDePerformance.
- `Unites/` : `PreferencesUnites`, `CatalogueUnites`.
- `Criteres/` : `CatalogueCriteres` (~60 critères avec Lire/Ecrire/bornes/EstFacultatif), `DescriptionCritere` (`AccepteLesNegatifs` : borne minimale négative). Écrit sans réflexion.
- `Donnees/` : ContexteDonnees, FabriqueContexteConception, FichiersReferentiel, InitialiseurBaseDeDonnees, ValidateurReferentiel.
- `Services/` : ServiceReglages, **ServiceChronos** (ajout/correction/suppression, `ListerParParcoursAsync`, `ListerDeLaVoitureAsync`, `ListerDuReglageAsync`, `ReglagesAvecChronoAsync`), ServiceComparaisonChronos, ServiceComparaisonReglages, ServiceListeVoitures, ServiceCatalogue (voitures et marques personnalisées, fusions), ServiceSauvegarde (export/import JSON, modes `Fusionner` et `TousRemplacer`), ServiceDemarrage, UniteDeTravail / FabriqueUniteDeTravail, FiltreReglages, FiltreChronos, ApplicationFiltre, ComparaisonDeNoms, CopieurDeReglage, IndicesDePerformance, ErreurMetierException (~25 codes).
- `Edition/` (état des formulaires, testable sans interface) : EditeurReglage (`AModifications`, `MarquerCommeEnregistre`), SectionEditable, SectionRapports, ChampReglage, RapportEditable, **EditeurChrono**, **DureesDeTour** (lecture et affichage « m:ss,mmm », écart), **EditeurFiltreReglages** (filtres des comparaisons, critères numériques à la demande), **EditeurVoiturePersonnalisee**, Nombres, PresenceDesSections.
- `Preferences/` : ServicePreferences, IStockageDePreferences.
- `Ressources/` : Libelles.resx (FR) + Libelles.en.resx, ServiceLibelles.
- `Referentiel/` : `circuits.json`, `voitures.json` (**vide pour l'instant**), `voitures.exemple.json` (non embarqué).
- `Migrations/` : migrations EF.

## 6. Application MAUI

- `MauiProgram.cs` (injection de dépendances), `App.xaml(.cs)` (routes `voiture`, `reglage`, `chrono`, `editionvoiture` enregistrées dans le constructeur).
- `AppShell` : 4 onglets — Voitures, Chronos, Comparer, Paramètres.
- `Services/` : DialoguesMaui / IDialogues (message, confirmation avec libellés, menu de choix), IFichiers / FichiersMaui (partage et sélecteur de fichiers), MessagesErreurs, JournalErreurs, StockageDePreferencesMaui, ProposeurDeFusions (fusion avec le catalogue officiel proposée au démarrage), Claviers.
- `Traductions/` : Texte, TraduireExtension, Textes.resx / Textes.en.resx (~199 clés, FR et EN identiques en clés).
- `Pages/` : Demarrage, Voitures, Voiture (détail, bascule Réglages/Chronos, modifier/supprimer si personnalisée), EditionVoiture, EditionReglage (avec avertissement de modifications non enregistrées et section Chronos), Chronos (liste par parcours), Chrono (ajout/correction), Comparaison (bascule Chronos/Réglages), Parametres (langue, unités, section Données : export/import).
- `Views/` : VueFiltreReglages, VueComparaisonChronos, VueComparaisonReglages (contrôles réutilisés dans Comparaison).
- Icône et écran de démarrage : chronomètre blanc sur fond bleu nuit `#12151F` (`Resources/AppIcon`, `Resources/Splash`).
- `README.md` (racine) explique le premier lancement et le format de `voitures.json`.

### Décisions d'interface
- **Chronos** : saisis depuis l'onglet Chronos (circuit puis parcours en cascade, voiture puis réglage), depuis le détail d'une voiture ou depuis la page d'un réglage (réglage déjà choisi). La durée se tape dans **un seul champ** « m:ss,mmm » (clavier texte, car le clavier numérique n'a pas « : »). Les réglages ayant déjà un chrono sur le parcours sont signalés.
- **Comparer** : bascule Chronos/Réglages. Filtres communs (recherche, marque, division, transmission, origine, années, indice) + critères numériques ajoutés à la demande, qui filtrent **et** s'affichent sur chaque ligne. Les lignes sont numérotées 1, 2, 3… (position dans la liste). Comparaison des réglages : « Trier par » + sens (le meilleur d'abord par défaut), écart avec la première ligne.
- **Voitures personnalisées** : formulaire unique (marque existante ou « Nouvelle marque… »), zone « Avancé » (prix, contenu téléchargeable). Fusion avec le catalogue officiel proposée au démarrage, une correspondance à la fois (« Fusionner » / « Plus tard »).
- **Import/export** : partage du système à l'export ; à l'import, « Fusionner » ou « Tout remplacer » (confirmation explicite).
- **Éditeur de réglage** : en quittant avec des modifications, menu Enregistrer / Abandonner / Rester (interception de `Shell.Navigating` ; ouvrir une page par-dessus, comme un chrono, n'est pas un départ).
- **Claviers** : les champs pouvant être négatifs (`AccepteLesNegatifs`) utilisent le clavier normal sur iOS/macOS (pas de signe « - » au clavier numérique).

### Pièges connus
- Crash au changement de langue (« Pending Navigations still processing ») : sélectionner l'onglet **avant** d'attacher le Shell à la fenêtre, et différer le remplacement via `Dispatcher.Dispatch`.
- Les services ne renvoient jamais de texte : ils lèvent `ErreurMetierException(CodeErreurMetier)` ; l'App traduit via la clé `Erreur_<Code>` dans Textes.resx.
- Un contexte EF neuf par opération (`UniteDeTravail`).
- Regroupement des marques : grouper par `IdentifiantMarque`, pas par objet.
- **Ne pas utiliser `RelativeSource AncestorType` dans les liaisons compilées d'un `DataTemplate`** : la source est nulle à l'exécution (NullReferenceException). Donner à chaque ligne sa propre commande (`OuvrirCommand`), comme `LigneReglageViewModel` et `LigneChronoCourteViewModel`.
- Les `Picker` : déclarer `ItemsSource` avant `SelectedIndex` / `SelectedItem` ; ne remplacer les listes que si elles ont changé (réaffecter efface la sélection).
- La saisie d'un rapport de boîte est évaluée à chaque caractère : les rapports suivants sont recalculés à partir de leurs valeurs d'avant la saisie (« 0 » puis « 0,8 » ne doit pas les laisser à 0).

## 7. Modèle de données et règles métier

- **Unités canoniques en base** ; conversion à l'affichage et à la saisie.
- Identifiants du référentiel < 100000 (issus du JSON). Marques et voitures personnalisées à partir de `ConstantesReferentiel.PremierIdentifiantPersonnalise` = 100000. Réglages et chronos : `Guid`.
- JSON embarqué, désérialisé par source generator (sans réflexion, sûr pour iOS).
- Modifier le **contenu** d'un réglage supprime ses chronos après confirmation ; le renommer ne les supprime pas.
- **Un chrono par (réglage, parcours)** : l'ajout d'un doublon est refusé, la modification d'un existant est toujours permise. Un chrono peut être rattaché à un réglage d'origine.
- Les réglages d'origine sont en lecture seule (leurs chronos restent saisissables).
- **Import** : le plus récent gagne selon `DateModification`. Modes `Fusionner` et `TousRemplacer`. Import atomique.
- Voitures/marques personnalisées : fusionnées avec le catalogue officiel après confirmation de l'utilisateur.
- **Rapports de boîte** (10 max) en ordre décroissant : saisir une valeur abaisse les rapports suivants qui la dépassent (et ils sont signalés « ajusté automatiquement » jusqu'à modification) ; un rapport supérieur au précédent est seulement signalé.
- L'éditeur ne réécrit que les champs réellement modifiés (évite des arrondis qui supprimeraient les chronos).
- Classes de performance : E 0–300, D 301–400, C 401–500, B 501–600, A 601–700, S 701–800, R 801–900, P 901–998, X 999. Affichées aussi dans la liste des voitures.
- Tris de la liste des voitures : nom, indice de performance, puissance, vitesse maximale, accélération 0–97 km/h ; **sens croissant/décroissant séparé**, valable pour tout tri.
- Comparaison de chronos : un parcours précis + filtres sur les critères de la voiture.
- Comparaison de réglages : **toutes** les voitures répondant aux filtres (pas une sélection de 2 à 6).

## 8. État des tests

357 tests, tous passent (dernier rapport). Le projet de tests utilise xunit.v3 en mode « plateforme de test » : sous le SDK 10, `dotnet test` échoue avec une erreur VSTest ; lancer plutôt le projet comme un exécutable. Commandes (depuis la racine) :

```
dotnet run --project ForzaTuneLap.Core.Tests
dotnet build ForzaTuneLap.App/ForzaTuneLap.App.csproj -f net10.0-windows10.0.19041.0
dotnet build ForzaTuneLap.App/ForzaTuneLap.App.csproj -f net10.0-android
dotnet ef migrations add <Nom> --project ForzaTuneLap.Core
```

Vérifié par l'utilisateur à l'écran : Windows (toutes les fonctions) et Android (Galaxy J6, Android 9, ARM 32 bits : clavier du temps, export par partage, import). **Non vérifié** : iOS et macOS, la fusion au démarrage avec une vraie correspondance.

## 9. Reste à faire

1. Saisie des **681 voitures** dans `voitures.json` : l'utilisateur la fait de son côté (voir `README.md` pour le format). Une voiture personnalisée au vrai nom a été créée pour tester la fusion au démarrage une fois la voiture ajoutée au fichier.
2. Vérifications sur Apple quand un Mac sera disponible : voir `NOTES-APPLE.md`.
3. Test Android à compléter : retour matériel avec des modifications non enregistrées, performances sur le catalogue complet.

## 10. Points à confirmer avec l'utilisateur

- Noms anglais des divisions (traduits de mémoire).
- `Differentiel.EquilibreCentral`.
- Unités d'origine des anciennes bornes.
- Seuils 97 / 161 / 193 km/h supposés correspondre à 60 / 100 / 120 mph.
- Valeurs à zéro traitées comme « non renseignées ».

## 11. Conventions de code

- Noms français, sans acronyme, explicites.
- Logique dans `Core`, jamais dans `App`.
- Pas de réflexion (compatibilité iOS / trimming).
- Pas de texte renvoyé par les services : codes d'erreur + ressources.
- Ajouter ou mettre à jour des tests pour chaque règle métier.
- L'ancien modèle venait de SQLiteNetExtensions : méfiance envers les champs superflus.
- Les fichiers modifiés sur disque pendant une session (par l'utilisateur ou un outil) sont l'état courant : ne pas les écraser.
