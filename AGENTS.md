# AGENTS.md — Notes for AI agents (gregMod.MoreSpools)

Repo: https://github.com/mleem97/gregMod.MoreSpools · License: Apache-2.0 · Version: see `VERSION` (1.2.1).

MelonMod for Data Center. Adds extra cable-spool (spinner) variants.

## Duties

1. **Read first:** `README.md`, `docs/INDEX.md`, `docs/ARCHITECTURE.md` — only then make changes.
2. **Do not commit secrets** (keys, tokens, `.env`). Use keys only via environment variables.
3. **Preserve history:** no `push --force`, no history rewrite without instruction.
4. **Verify changes:** before reporting done, build the mod (`dotnet build gregMod.MoreSpools.csproj -c Release` or `./build.sh MoreSpools` from `ModRepositories/`).
5. **Keep docs in sync:** for new features update `README.md` + `docs/` + `CHANGELOG.md` (Unreleased).
6. **Conventions:** Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:` …), one logical change per commit.
7. **When unsure:** stop and ask instead of guessing — especially for deletes, migrations, CI.

## Build and references

- Target: `net6.0`, x64. Game: Data Center (`MelonGame("Waseku", "Data Center")`).
- `references/` holds absolute symlinks into the Steam Data Center install.
  Never commit `references/*.dll`, `bin/`, or `obj/`.
- After a fresh clone, run `../tools/sync-melon-assemblies.sh`.
- Deploy only with `./build.sh MoreSpools --deploy`.

## Hard rules

- New spools go through `SpinnerDefinitions` (data-driven); never hard-code a
  variant into patches.
- **Never** touch gregCore types outside a soft-probe/JIT-split bridge — the
  mod must load without `gregCore.dll`.

## Layout

- `src/Core.cs` — MelonMod entry. `src/SpinnerDefinitions.cs` — spool data.
- `src/Config.cs` — prefs. `src/Patches.cs` — Harmony patches.
