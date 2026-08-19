# TSU-ORGDOCX SF08 labeling kit

Standalone Label Studio pack. Unzip / clone **this folder only** — no need for `tsuorg-ml` or the rest of the repo.

## Share this folder

Zip the **whole** `tsuorg-ml-clone` folder (must include `data\raw\images\sf08\`, ~670 MB). Send via Drive / USB. Do not push the PNGs to GitHub (file size).

On the other PC: unzip anywhere, then:

1. Install **Python 3.10+** — https://www.python.org/downloads/  
   Check **Add python.exe to PATH**.
2. Double-click **`SETUP.bat`** (once). Wait until it says Setup OK.
3. Double-click **`START.bat`**. Two windows open. Browser: **http://localhost:8081**

## First time in Label Studio

1. Create an account (local only).
2. Create project.
3. **Settings → Labeling Interface → Code** — open `data\templates\label_studio_config.xml`, copy all, paste, save.
4. **Import** `data\raw\label_studio_sf08_import.json`.
5. Label / correct boxes, **Submit**.
6. When done: **Export JSON** and send that file back for training.

Keep both START windows open while labeling (image server + Label Studio).

## Ports

| Service | URL |
|---------|-----|
| Label Studio | http://localhost:8081 |
| Page images | http://127.0.0.1:9091 |

## Labels

`FORM_TITLE`, `DATE_PROPOSAL`, `ORG_NAME`, `OBJECTIVES`, `ACTIVITY_TITLE`, `ACTIVITY_DATE`, `VENUE`, `PARTICIPANTS`, `ACTIVITY_MODE`, `ORG_OFFICER_SIGNATURE`, `ADVISER_SIGNATURE`, `SAS_SIGNATURE`

Document type: `SF08`.
