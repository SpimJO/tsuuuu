# Training images for Label Studio clone

Keep `.\start_labeling.ps1` running (http://127.0.0.1:9091) while labeling.

## SF08

Import:

```
data/raw/label_studio_sf08_import.json
```

Labeling XML: `data/templates/label_studio_config.xml`

Images: `data/raw/images/sf08/`

## Accomplishment Report (AR)

Import:

```
data/raw/label_studio_ar_import.json
```

Labeling XML: `data/templates/label_studio_config_ar.xml`

Images (organized like `dataset/AR`):

```
data/raw/images/accomplishment/
  Org 1 (MAHARLIKA)/
  Org 2 (tsu communicators guild)/
  Org 3 (ASFE CoED)/
  Org 4/
  Org 5/
  Org 6/
  Org 7/
  Org 8/
  Org 9/
```

Create a **separate Label Studio project** for AR (do not mix with SF08).
