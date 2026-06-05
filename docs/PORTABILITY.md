# Portability

GitHub One-Click Uploader is designed to run on other Windows machines without local-path customization.

## Not Hardcoded

The app does not hardcode:

- a Windows username.
- a drive letter for projects.
- a GitHub username.
- a Git executable path.
- a GitHub token.

## Runtime Requirements

The target machine still needs:

- Windows.
- Git installed and available on `PATH`.
- GitHub CLI installed.
- GitHub CLI logged in with `repo` and `workflow` scopes.

## What Happens When Something Is Missing

The app checks for Git, GitHub CLI, GitHub login status, and common unsafe files before uploading.

Missing dependencies or login problems appear as high-severity findings and block upload.

## Distribution

For normal users, distribute the release zip:

```text
GitHubOneClickUploader.zip
```

It contains:

- `GitHubOneClickUploader.exe`
- `README.md`
- `LICENSE`

No local scan data, GitHub tokens, or machine-specific files are bundled.
