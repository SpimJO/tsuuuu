# TSU-ORGDOCX Static UI

Blazor WebAssembly copy of `tsuorg-frontend`, with the same Razor screens and `dotnet run` command. Data is in-memory mock data. No backend, no ML.

Does **not** change `tsuorg-frontend`, `tsuorg-backend`, or `tsuorg-ml`.

## Run

```powershell
cd tsuorg-static
dotnet run --project src/TsuOrg.Static
```

Open http://localhost:5173 → `/login`.

HTTPS: https://localhost:7173

## Sign in

Same seeded accounts as the Blazor app. Password: `TsuOrg@2026`

| Email | Role | Portal |
|-------|------|--------|
| `officer@student.tsu.edu.ph` | Org Officer | `/officer` |
| `adviser@tsu.edu.ph` | Adviser | `/review` |
| `dean@tsu.edu.ph` | Dean | `/review` |
| `sou@tsu.edu.ph` | SOU Admin | `/sou` |
| `admin@tsu.edu.ph` | System Admin | `/sou` |
