# Privacy

The uploader is local-first.

It reads the selected project folder, scans filenames and small text files, then calls Git and GitHub CLI.

It does not:

- store GitHub passwords.
- store GitHub tokens.
- call the GitHub API directly except through GitHub CLI.
- send telemetry.
- create remote logs.

GitHub CLI stores authentication securely using its own credential storage.

## Device Login

The GitHub device code is temporary. The app may display the code copied by GitHub CLI so you can paste it into GitHub's authorization page. The app does not store that code.
