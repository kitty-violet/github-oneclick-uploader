# Security Policy

## Scope

The app shells out to Git and GitHub CLI. It does not implement its own GitHub authentication.

## Reporting Security Issues

Open a GitHub issue with reproduction steps and redact all secrets, local paths, and account-specific data.

## Design Principles

- GitHub credentials stay in GitHub CLI's credential store.
- Upload requires explicit user confirmation.
- Safety findings are visible before upload.
- Strict safety mode blocks high and medium severity findings.
- Hidden config files are uploaded through Git, not browser drag-and-drop.
