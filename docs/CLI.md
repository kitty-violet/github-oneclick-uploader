# Command Line

GitHubOneClickUploader can run as either a desktop GUI or a command-line uploader.

## Help

```powershell
GitHubOneClickUploader.exe --help
GitHubOneClickUploader.exe --version
```

## Login

```powershell
GitHubOneClickUploader.exe login
```

GitHub opens an `Authorize your device` page. GitHub CLI copies the one-time device code to your clipboard. Paste it into the GitHub page with `Ctrl+V`.

## Check

```powershell
GitHubOneClickUploader.exe check --project "<project-path>" --repo my-app
```

The check command validates:

- project path.
- repository name.
- Git availability.
- GitHub CLI availability.
- GitHub login status.
- risky filenames.
- common secret/token patterns.

## Upload

```powershell
GitHubOneClickUploader.exe upload --project "<project-path>" --repo my-app --public --yes
```

Private repository:

```powershell
GitHubOneClickUploader.exe upload --project "<project-path>" --repo my-app --private --yes
```

Without `--yes`, the command asks you to type `UPLOAD` before pushing.

## Options

- `--project`, `-p`: project folder to upload.
- `--repo`, `-r`: GitHub repository name. Defaults to folder name.
- `--public`: create a public repository. Default.
- `--private`: create a private repository.
- `--description`, `-d`: repository description.
- `--strict`: block high and medium findings. Default.
- `--allow-medium`: block high findings only.
- `--yes`, `-y`: upload without typing `UPLOAD`.
