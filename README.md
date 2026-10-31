# ForzaTuneLap

[![Publication](https://github.com/Makushimu14/ForzaTuneLap/actions/workflows/release.yml/badge.svg)](https://github.com/Makushimu14/ForzaTuneLap/actions/workflows/release.yml)

Application compagnon pour **Forza Motorsport (2023)** : elle garde tous vos réglages de voitures, vos meilleurs chronos, et vous aide à les comparer.

> Projet personnel, **non officiel**. Il n'est ni affilié, ni approuvé par Microsoft, Turn 10 Studios ou Xbox Game Studios. *Forza Motorsport* est une marque de Microsoft.

## Ce que fait l'application

- **Réglages** : autant de réglages que vous voulez par voiture, avec tous les paramètres du jeu (pneus, alignement, ressorts, barres antiroulis, amortisseurs, différentiel, freins, aérodynamisme, boîte de vitesses jusqu'à 10 rapports…). Le réglage d'origine de chaque voiture est fourni en lecture seule.
- **Chronos** : un meilleur temps par parcours **et** par réglage, saisi au format du jeu (`1:32,450`). Vous les retrouvez dans l'onglet Chronos, dans le détail d'une voiture et dans la page d'un réglage.
- **Comparer les chronos** : choisissez un circuit et un parcours, puis filtrez sur la marque, la division, la transmission, l'année, l'indice de performance ou n'importe quel critère (puissance, poids, pression des pneus…). Le classement est numéroté, avec l'écart avec le meilleur temps.
- **Comparer les réglages** : classez tous les réglages qui répondent aux filtres sur le critère de votre choix, avec l'écart avec la première ligne.
- **Voitures personnalisées** : ajoutez une voiture ou une marque qui n'est pas dans le catalogue. Si elle y est ajoutée plus tard, l'application propose de les fusionner sans perdre vos réglages ni vos chronos.
- **Sauvegarde** : export de toutes vos données dans un fichier JSON, import en fusion (la version la plus récente gagne) ou en remplacement complet.
- **Français et anglais**, **unités au choix** (métrique, impérial, ou grandeur par grandeur).
- **Vos données restent sur votre appareil** : tout est stocké localement (SQLite), rien n'est envoyé nulle part.

## Plateformes

| Plateforme | État |
| --- | --- |
| Windows 10 (1809) et plus | Testé |
| Android 5.0 et plus (ARM 64 bits, ARM 32 bits, x64) | Testé sur un Galaxy J6 (Android 9) |
| macOS et iOS | Écrit mais **jamais compilé ni essayé** (voir [NOTES-APPLE.md](NOTES-APPLE.md)) |

## Installer

Les versions se trouvent dans l'onglet **[Releases](https://github.com/Makushimu14/ForzaTuneLap/releases)**.

- **Windows** : télécharger `ForzaTuneLap-vX.Y.Z-windows-x64.zip`, le décompresser, puis lancer `ForzaTuneLap.App.exe`. Windows peut afficher un avertissement « SmartScreen » : l'application n'est pas signée (« Informations complémentaires », puis « Exécuter quand même »).
- **Android** : télécharger `ForzaTuneLap-vX.Y.Z-android.apk` sur le téléphone et l'ouvrir. Android demande d'autoriser l'installation d'applications depuis cette source.

## Compiler depuis les sources

Prérequis : le **SDK .NET 10** et les charges de travail MAUI (`dotnet workload restore ForzaTuneLap.App/ForzaTuneLap.App.csproj`). Pour Android : le SDK Android et un JDK. Visual Studio 2026 installe tout cela avec la charge de travail « .NET MAUI ».

```bash
# Tests du Core (indépendants de toute plateforme)
dotnet run --project ForzaTuneLap.Core.Tests

# Windows
dotnet build ForzaTuneLap.App/ForzaTuneLap.App.csproj -f net10.0-windows10.0.19041.0

# Android
dotnet build ForzaTuneLap.App/ForzaTuneLap.App.csproj -f net10.0-android
```

`dotnet test` ne fonctionne pas avec le SDK 10 pour ce projet de tests (xunit.v3 en mode « plateforme de test ») : il faut le lancer comme un exécutable, avec la commande ci-dessus.

Pour modifier le modèle de données, il faut une migration EF Core :

```bash
dotnet ef migrations add <Nom> --project ForzaTuneLap.Core
```

### Organisation du dépôt

| Dossier | Contenu |
| --- | --- |
| `ForzaTuneLap.Core` | Toute la logique : modèle, EF Core et SQLite, unités, critères, services, éditeurs de formulaires, textes. Aucune dépendance de plateforme. |
| `ForzaTuneLap.Core.Tests` | Tests xUnit du Core (base SQLite en mémoire). |
| `ForzaTuneLap.App` | Application .NET MAUI : pages, modèles de vue, textes de l'interface. Uniquement de l'interface. |

Les notes de travail du projet (règles, décisions, pièges rencontrés) sont dans [CLAUDE.md](CLAUDE.md).

## Ajouter des voitures : `ForzaTuneLap.Core/Referentiel/voitures.json`

Voir `voitures.exemple.json` dans le même dossier pour la structure complète. Règles :

- `identifiant` des marques et voitures : entiers uniques, **inférieurs à 100000**.
- `identifiantPays` : un identifiant de `circuits.json`.
- `division` : nom exact de l'énumération `Division` (ex. `ForzaGT2`).
- `reglageOrigine` est facultatif. Son `identifiant` est un GUID unique à générer une fois et à ne jamais changer. Corriger les valeurs d'un réglage d'origine remplace le réglage au prochain démarrage (et supprime ses chronos).
- Toutes les valeurs sont en **unités canoniques** : kg, kW, N·m, km/h, m, cm, bar, N/mm, kgf, degrés, secondes.

## Publier une version

Une release (avec l'exécutable Windows et l'`.apk`) est créée automatiquement par GitHub Actions ([release.yml](.github/workflows/release.yml)) quand un **tag `v…`** est poussé sur un commit de la branche **`main`** :

```bash
git checkout main
git merge developpement
git push origin main

git tag v1.0.0
git push origin v1.0.0
```

- L'exécution lance d'abord les tests ; si l'un échoue, rien n'est construit. Un tag posé sur un commit qui n'est pas dans `main` ne publie rien.
- Un tag avec un tiret (`v1.0.0-beta`, `v1.0.0-rc1`) donne une **préversion**.
- Les notes de la release sont générées à partir des commits.
- L'exécution se suit dans l'onglet **Actions** du dépôt. Elle peut aussi être lancée à la main (« Run workflow ») : elle construit alors Windows et Android sans publier de release, et les fichiers restent téléchargeables dans les artefacts de l'exécution.

### Signature de l'`.apk` (à faire une fois)

Un `.apk` doit être signé. Sans clé enregistrée, le workflow en crée une temporaire à chaque fois : l'`.apk` s'installe, mais Android refusera de le mettre à jour par-dessus la version précédente (il faudra la désinstaller d'abord). Pour des mises à jour normales, créez une clé **une seule fois** :

```bash
keytool -genkeypair -v -keystore forzatunelap.keystore -alias forzatunelap -keyalg RSA -keysize 2048 -validity 10000
```

Puis ajoutez quatre **secrets** dans *Settings → Secrets and variables → Actions* du dépôt :

| Secret | Valeur |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | le contenu du fichier `.keystore` encodé en base64 (voir ci-dessous) |
| `ANDROID_KEYSTORE_PASSWORD` | le mot de passe du fichier |
| `ANDROID_KEY_ALIAS` | l'alias (ici `forzatunelap`) |
| `ANDROID_KEY_PASSWORD` | le mot de passe de la clé |

Encodage en base64, sous PowerShell (copie le résultat dans le presse-papiers) :

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("forzatunelap.keystore")) | Set-Clipboard
```

**Gardez le fichier `.keystore` et ses mots de passe en lieu sûr, hors du dépôt** (les fichiers `.keystore` et `.jks` sont ignorés par Git). Si la clé est perdue, il ne sera plus possible de publier des mises à jour installables par-dessus les versions existantes.

## Licence

Ce projet est publié sous licence **MIT** : voir le fichier [LICENSE](LICENSE).
