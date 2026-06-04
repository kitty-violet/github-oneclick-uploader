# GitHub One-Click Uploader

A Windows desktop helper that uploads a local project folder to GitHub using Git and GitHub CLI while preserving hidden config folders such as `.github`, `.gitignore`, and `.gitattributes`.

It is meant for people who prefer a small GUI over memorizing Git commands.

## What It Does

- Lets you choose a project folder.
- Suggests a repository name from the folder name.
- Checks Git and GitHub CLI availability.
- Checks GitHub login status.
- Scans the project for risky local files and common secret patterns.
- Adds conservative `.gitignore` defaults without overwriting existing rules.
- Initializes Git if needed.
- Uses GitHub's noreply email for local commits.
- Creates a public or private GitHub repository.
- Pushes the project with hidden config files preserved.

## What It Does Not Do

- It does not store GitHub passwords.
- It does not store GitHub tokens.
- It does not upload automatically without confirmation.
- It does not bypass GitHub login or two-factor authentication.
- It does not guarantee every possible secret format is detected.

## Requirements

- Windows.
- Git.
- GitHub CLI (`gh`).
- A logged-in GitHub CLI session with `repo` and `workflow` scopes.

Install GitHub CLI:

```powershell
winget install --id GitHub.cli -e
```

Login:

```powershell
gh auth login --web --clipboard --git-protocol https --scopes repo,workflow
```

## Build

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Run:

```powershell
.\artifacts\bin\GitHubUploader.exe
```

Package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Self-test:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

## Safety Model

The app blocks obvious risky files such as `.env`, private keys, token files, local scan logs, and backup files. It also scans small text files for common API key and token patterns.

See [docs/SAFETY.md](docs/SAFETY.md).

## License

MIT. See [LICENSE](LICENSE).
