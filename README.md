# ForzaTuneLap — projet Core

Bibliothèque .NET 10 sans code spécifique à une plateforme : modèle de données, EF Core (SQLite),
unités, catalogue de critères, données de référence. L'interface .NET MAUI viendra s'appuyer dessus.

## Premier lancement dans Visual Studio 2026

1. Ouvrir `ForzaTuneLap.slnx` (la première restauration des paquets NuGet nécessite Internet), puis Générer > Générer la solution.
2. Mettre à jour les versions `10.0.0` des paquets EF Core vers la dernière 10.0.x.
3. Créer la migration initiale, dans la Console du Gestionnaire de package (projet par défaut : ForzaTuneLap.Core) :
   `Add-Migration Initiale`
   (ou en ligne de commande : `dotnet ef migrations add Initiale`)
4. Au démarrage de l'application : `await InitialiseurBaseDeDonnees.InitialiserAsync(contexte);`

## Ajouter des voitures : `Referentiel/voitures.json`

Voir `Referentiel/voitures.exemple.json` pour la structure complète. Règles :
- `identifiant` des marques et voitures : entiers uniques, **inférieurs à 100000**.
- `identifiantPays` : un identifiant de `circuits.json`.
- `division` : nom exact de l'énumération `Division` (ex. `ForzaGT2`).
- `reglageOrigine` est facultatif. Son `identifiant` est un GUID unique à générer une fois
  et à ne jamais changer. Corriger les valeurs d'un réglage d'origine remplace le réglage
  au prochain démarrage (et supprime ses chronos).
- Toutes les valeurs sont en **unités canoniques** : kg, kW, N·m, km/h, m, cm, bar, N/mm, kgf, degrés, secondes.
