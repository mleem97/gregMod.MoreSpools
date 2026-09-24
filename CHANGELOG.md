# Changelog — gregMod.MoreSpools

Format: [Keep a Changelog](https://keepachangelog.com/de/1.0.0/). Version: see [`VERSION`](VERSION).

## [1.3.0] — 2026-09-24

### Changed

- ID-range overlap guard (warn at 110+, RealisticModules range).

## [Unreleased]

### Fixed

- Purchase duplicates: `GetPrefabForItem` prefix now delivers the purchase clone inactive
  under the template holder (prefab semantics) instead of as a live object in the scene.
  Otherwise, one orphan remained per purchase (visible clone pile + save pollution
  → duplicated spools after save/load).

### Added

- Unified open-source layout (README, docs, badges) following the gregCore template.

## [0.1.0] — 2026-09-22

- Initial standardized baseline.
