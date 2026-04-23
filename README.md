# VideoManager - Sony A6700 Post-Production Tool

**VideoManager** est une application de bureau Windows (WPF) conçue pour automatiser et simplifier le workflow de post-production des créateurs utilisant un Sony A6700. Elle permet de gérer le tri, la conversion et la visualisation des rushs vidéos et des photos en un seul endroit.

## 🚀 Objectifs du Projet

- **Automatisation du Tri** : Détection automatique de la carte SD et transfert intelligent vers le PC (structuré par type de fichier).
- **Optimisation du Flux Vidéo** : Conversion des fichiers 4:2:2 10-bits (généralement lourds à lire) vers un format 4:2:0 8-bits fluide via HandBrake.
- **Visualisation Native** : Lecture des rushs Sony haute qualité directement dans l'application grâce à l'intégration de VLC.
- **Gestion de Photothèque** : Tri et visualisation des fichiers RAW (.ARW) et JPEG.
- **Statistiques & Dashboard** : Vue d'ensemble de la bibliothèque et extraction du Shutter Count (nombre de déclenchements) via ExifTool.

## 🛠️ Fonctionnement Technique

L'application repose sur plusieurs outils puissants intégrés :
- **FFmpeg / FFprobe** : Analyse technique des fichiers et extraction des miniatures.
- **HandBrakeCLI** : Moteur de conversion vidéo performant avec presets personnalisés.
- **LibVLCSharp** : Moteur de rendu vidéo pour une lecture fluide de tous les formats.
- **ExifTool** : Extraction des métadonnées avancées des fichiers Sony.

## 🎨 Design & UX
- Interface **Custom Dark** avec effets de **Glassmorphism**.
- Mise en page dynamique de type **Masonry** (Pinterest style) pour le Dashboard.
- Utilisation de `WindowChrome` pour une immersion totale.

## ⚙️ Installation & Prérequis
L'application nécessite les binaires suivants à la racine (exclus du repo GitHub pour des raisons de taille) :
- `ffmpeg.exe`
- `ffprobe.exe`
- `HandBrakeCLI.exe`
- `exiftool.exe`

## 📝 Licence
Projet privé - Tous droits réservés.
