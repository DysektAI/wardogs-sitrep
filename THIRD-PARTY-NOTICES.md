# Third-party notices

This notice accompanies the local SITREP trial folder and ZIP. It grants no license to SITREP itself and no permission to use third-party software in WARDOGS.

| Distributed component | Version / source | Terms |
| --- | --- | --- |
| Tesseract .NET wrapper | NuGet Tesseract 5.2.0, https://github.com/charlesw/tesseract; Copyright 2012–2022 Charles Weld | Apache-2.0, `notices/Apache-2.0.txt` |
| Native Tesseract | 5.2.0, `x64/tesseract50.dll`, https://github.com/tesseract-ocr/tesseract | Apache-2.0, `notices/Apache-2.0.txt` |
| Leptonica | 1.82.0, `x64/leptonica-1.82.0.dll`, https://github.com/DanBloomberg/leptonica | BSD-2-Clause-style Leptonica license, `notices/Leptonica.txt` (not Apache-2.0) |
| InteropDotNet (inside wrapper) | Copyright 2014 Andrey Akinshin, https://github.com/AndreyAkinshin/InteropDotNet | MIT, `notices/InteropDotNet-MIT.txt` |
| English LSTM model | tessdata_fast revision `87416418657359cb625c412a48b6e1d6d41c29bd`, https://github.com/tesseract-ocr/tessdata_fast | Apache-2.0, `notices/Apache-2.0.txt`; SHA256 in `tessdata/README.md` |
| L81 table | Apollyon revision `e8d1ee9ceb07fe9ec4923b7b55e836f8b3bb3e05`, https://github.com/apollyon-sys/wardogs-calculator | Original material MIT (`notices/Apollyon-MIT.txt`); third-party assets excluded. See `data/PROVENANCE.md` for scope and uncertainty. |
| .NET / Windows Desktop runtime | Self-contained .NET 10 Windows x64 runtime | Microsoft/.NET Foundation MIT and third-party terms; retain SDK-supplied `LICENSE.txt` and `ThirdPartyNotices.txt` in the published folder. |

The native DLLs are distributed unmodified from the locked NuGet package. A complete native codec/subdependency license inventory is not supplied by that package and remains a redistribution review prerequisite; do not interpret this inventory as legal clearance for a public release.

WARDOGS names, marks and game materials belong to their respective holders. No affiliation, endorsement, publisher permission, anti-cheat compatibility, or rights to redistribute game imagery are claimed. No game imagery is bundled. The table is a source-specific uncorrected calculation, not an independently measured game guarantee.

The Visual C++ x64 runtime is an external prerequisite (not installed or downloaded by SITREP).
