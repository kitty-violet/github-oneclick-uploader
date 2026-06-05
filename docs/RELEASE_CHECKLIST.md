# Release Checklist

1. Run the source hygiene check.
2. Build locally.
3. Package locally.
4. Launch the packaged executable.
5. Confirm Git and GitHub CLI checks work.
6. Confirm the scanner blocks `.env` and private key files.
7. Confirm `.github`, `.gitignore`, and `.gitattributes` survive upload by Git push.
8. Confirm `GitHubOneClickUploader.exe` has the app icon.
9. Confirm the release zip does not contain local logs, tokens, or machine-specific paths.
