# L81 data provenance

- Immutable source: https://raw.githubusercontent.com/apollyon-sys/wardogs-calculator/e8d1ee9ceb07fe9ec4923b7b55e836f8b3bb3e05/data/weapons.json
- Upstream revision: `e8d1ee9ceb07fe9ec4923b7b55e836f8b3bb3e05`; all 84 L81 samples and both weapon limits compared with the local profile on 2026-09-16, exact match. Only the `mortar.ballistics.single` data was adapted; no game images or other weapon data are included.
- Retrieved: 2026-09-16
- Weapon: L81 Mortar (`mortar`), min 132 m, max 684 m, table coverage 80–697 m
- Units: range meters, elevation game MIL; coordinate scale 100 m per map unit
- Interpolation: linear, inside both table coverage and weapon limits; no extrapolation, no terrain correction
- License scope: upstream MIT covers original code and other original material, not WARDOGS assets or third-party materials. See https://raw.githubusercontent.com/apollyon-sys/wardogs-calculator/e8d1ee9ceb07fe9ec4923b7b55e836f8b3bb3e05/docs/legal.md and `notices/Apollyon-MIT.txt`. The numeric table is distributed upstream in its calculator source, but that statement alone does not establish the table's authorship/rights; confirm before public binary redistribution. No publisher permission is implied.
- Local file SHA256 (UTF-8/LF): `38A0FAF415FD8DF451FE4F2F356CFFE481296D6B227EEC0F63DA0C075D0794F2`. Values unchanged; the prior CRLF serialization hashed to `FF15BC5828E0E09A5EA09CD2F74B10AFD932A96682C2FE841D18CA94F098DF18`.
- Comparison source (not authoritative): https://schaulers.com/tools/wardogs-mortar-calculator (approx 120–700 m)
