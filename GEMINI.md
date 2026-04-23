# Mémoire du Projet : Gestionnaire Vidéo Sony A6700

## 1. Vision du Projet
Application de bureau Windows (WPF) conçue pour automatiser le workflow de post-production des rushs d'un Sony A6700 : tri depuis la carte SD, conversion 4:2:2 10-bits vers 4:2:0 8-bits, et gestion de photothèque.

## 2. Stack Technique & Architecture
- **Framework :** .NET / C# / WPF.
- **Architecture :** Approche "Code-Behind" assumée. Ne pas tenter de migrer vers un pattern MVVM strict (sauf demande explicite).
- **Logique de Données :** La classe `VideoItem` implémente `INotifyPropertyChanged` pour les mises à jour UI en temps réel.
- **Asynchronisme :** Utilisation systématique de `Task.Run()` pour les opérations d'E/S (E/S disque, lancements de processus externes) afin de ne jamais bloquer le thread UI.

## 3. Règles UI / UX (Critique)
Le design est "Custom Dark" et ne doit pas être altéré par des composants Windows natifs :
- **Couleur de Fond :** #18181A.
- **Fenêtrage :** Utilisation de `WindowChrome`. La barre de titre est gérée manuellement.
- **Correction Plein Écran :** Un trigger sur `WindowState == Maximized` ajoute un `Padding` de 8px sur la Border principale pour éviter le bug de troncature Windows. **Ne pas supprimer ce trigger.**
- **Immersion Sombre :** Utilisation de l'API `dwmapi.dll` (`DwmSetWindowAttribute`) pour forcer le mode sombre immersif sur la barre système et colorer la bordure résiduelle en #18181A (BGR: 0x001A1818).
- **Dialogues :** Pas de `MessageBox` classique ou de nouvelles fenêtres. Utiliser exclusivement le système d'**Overlays** (Grilles superposées avec Z-Index élevé et fond semi-transparent).

## 4. Dépendances Externes
L'agent doit savoir que l'application repose sur des binaires situés dans le dossier racine :
- **ffprobe.exe :** Analyse du format (pix_fmt) et de la durée.
- **ffmpeg.exe :** Extraction des miniatures (générées dans `%TEMP%` puis lues en `byte[]` pour éviter les verrous de fichiers).
- **HandBrakeCLI.exe :** Conversion vidéo avec import de preset JSON (`Conversion420.json`).
- **PowerShell :** Utilisé pour l'éjection forcée de la carte SD.
- **Microsoft.VisualBasic :** Utilisé pour la méthode `FileSystem.DeleteFile` (envoi à la Corbeille au lieu d'une suppression définitive).

## 5. Directives de Code pour l'Agent
- **Modifications XAML :** Prioriser la lisibilité. Toujours vérifier si un nouveau composant nécessite d'être ajouté à la propriété `shell:WindowChrome.IsHitTestVisibleInChrome="True"` pour être cliquable.
- **Nouveaux Styles :** S'inspirer des styles existants (`RoundedButton`, `ActionButton`). Utiliser les polices `Segoe MDL2 Assets` pour les icônes.
- **Gestion des Fichiers :** Toute copie ou déplacement de fichier doit vérifier l'existence des fichiers compagnons (ex: fichiers `.XML` de métadonnées Sony).
- **Erreurs :** En cas d'échec d'un processus externe, mettre à jour la propriété `Status` de `VideoItem` pour informer l'utilisateur via l'icône et la couleur correspondante.

## 6. Variables Clés
- `dossierDestBase` : Chemin racine sur le PC (par défaut `Vidéos\A6700_RAW`).
- `dossierSource` : Détecté automatiquement via le dossier `PRIVATE\M4ROOT\CLIP` sur la carte SD.