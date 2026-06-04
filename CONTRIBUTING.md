# Contributing

Please keep the uploader conservative and transparent.

Rules:

- Do not add telemetry.
- Do not collect, store, or transmit tokens.
- Do not silently disable safety checks.
- Do not overwrite an existing `.gitignore`; append only missing safe defaults.
- Keep GitHub authentication delegated to GitHub CLI.

Before opening a pull request:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-source-hygiene.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```
