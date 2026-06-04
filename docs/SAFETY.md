# Safety

GitHub One-Click Uploader tries to prevent accidental public leaks before pushing a project.

## Blocked File Names

The scanner blocks common sensitive or local-only files:

- `.env`
- `.env.local`
- `.env.production`
- `.env.development`
- `id_rsa`
- `id_dsa`
- `credentials.json`
- `token.json`
- `latest.tsv`
- `daily-snapshots.tsv`
- `snapshots.csv`
- `cleanup-log.txt`
- `startup-log.txt`
- `*.pem`
- `*.pfx`
- `*.p12`
- `*.key`
- `*.bak`
- `*.bak_*`

## Secret Patterns

The scanner checks small text files for common key patterns including:

- Google API keys.
- OpenAI-style `sk-` keys.
- GitHub classic/fine-grained token prefixes.
- Generic `api_key`, `secret`, and `token` assignments.

## Limits

No scanner is perfect. Before uploading a sensitive project, review:

- `.env` and config files.
- screenshots.
- logs.
- exported data.
- generated build artifacts.
- local paths and usernames in docs.
