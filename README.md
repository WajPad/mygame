# Forest Survival (Unity 2022.3)

Prvo-licna igra preživljavanja na ostrvu, inspirisana igrom The Forest.
Ceo svet (teren, drveće, životinje, mutanti, vatra...) pravi se kodom pri pokretanju, bez eksternih asseta.

## Kako dobiti .exe preko GitHub-a

1. Napravi **prazan** repo na GitHub-u i ubaci sve iz ovog foldera (ovaj folder je koren repoa).
2. **Jednokratno: Unity licenca** (besplatan Personal nalog je dovoljan)
   1. Repo → **Actions** → `1 - Unity aktivacija` → **Run workflow**. Preuzmi artifact `Unity_v2022.3.45f1.alf`.
   2. Otvori https://license.unity3d.com/manual , otpremi `.alf`, izaberi Personal, preuzmi `.ulf`.
   3. Repo → **Settings → Secrets and variables → Actions → New repository secret**:
      - `UNITY_LICENSE` = ceo sadržaj `.ulf` fajla
      - `UNITY_EMAIL` = email Unity naloga
      - `UNITY_PASSWORD` = lozinka Unity naloga
3. **Actions → `2 - Build Windows` → Run workflow** (ili samo push na `main`).
4. Kad završi (10-20 min prvi put), na dnu stranice runa je artifact **ForestSurvival-Windows**. Raspakuj i pokreni `ForestSurvival.exe`.

## Komande

| Taster | Akcija |
|---|---|
| WASD / miš | kretanje / pogled |
| Shift / Space | trčanje / skok |
| Levi klik | seci drvo, napadni |
| E | uzmi, koristi, pij vodu (stani u more) |
| F | jedi |
| C | ispeci meso kraj vatre |
| 1 | logorska vatra (2 drva + 3 štapa) |
| 2 | sklonište (6 drva + 4 štapa), E u njemu noću = spavanje |
| 3 | kamena sekira (3 kamena + 2 štapa) |
| H / Esc | pomoć / pauza |

## Cilj
Preživi 7 dana. Čuvaj glad, žeđ i zdravlje. Noću izlaze mutanti, a beže od vatre.
