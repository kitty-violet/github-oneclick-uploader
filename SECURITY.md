# Security Policy

## Scope

The app shells out to Git and GitHub CLI. It does not implement its own GitHub authentication.

## Reporting Security Issues

Please do not open a public issue for a vulnerability.

Use GitHub's private vulnerability reporting or Security Advisory flow when it is enabled for the repository. If private reporting is not available, contact the maintainer privately before posting reproduction details.

Always redact secrets, local paths, account names, and tokens from any report.

## Design Principles

- GitHub credentials stay in GitHub CLI's credential store.
- Upload requires explicit user confirmation.
- Safety findings are visible before upload.
- Strict safety mode blocks high and medium severity findings.
- Hidden config files are uploaded through Git, not browser drag-and-drop.
