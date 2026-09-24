# Changelog — gregMod.MoreSpools

Format: [Keep a Changelog](https://keepachangelog.com/de/1.0.0/). Version: siehe [`VERSION`](VERSION).

## [Unreleased]

### Fixed

- Kauf-Duplikate: `GetPrefabForItem`-Prefix liefert den Kauf-Klon jetzt inaktiv
  unter dem TemplateHolder (Prefab-Semantik) statt als Live-Objekt in der Szene.
  Pro Kauf blieb sonst ein Orphan zurueck (sichtbarer Klon-Haufen + Save-Pollution
  → verdoppelte Rollen nach Save/Load).

### Added

- Einheitliches Open-Source-Layout (README, Docs, Badges) nach gregCore-Vorbild.

## [0.1.0] — 2026-09-22

- Initialer standardisierter Stand.
