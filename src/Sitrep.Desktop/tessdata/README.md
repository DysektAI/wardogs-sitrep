# English OCR model

Run `pwsh -File scripts/setup-model.ps1` from the repository to retrieve the official `tessdata_fast` English LSTM model; application startup never downloads it.

- Source: https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/87416418657359cb625c412a48b6e1d6d41c29bd/eng.traineddata
- Revision: `87416418657359cb625c412a48b6e1d6d41c29bd`
- SHA256: `7D4322BD2A7749724879683FC3912CB542F19906C83BCC1A52132556427170B2`
- License: Apache-2.0; see packaged `notices/Apache-2.0.txt`.

A bad cached checksum is an error, not a warning. Remove the invalid model and rerun setup. A downloaded mismatch is discarded before installation. The model is gitignored and copied to the local trial folder during publish.
