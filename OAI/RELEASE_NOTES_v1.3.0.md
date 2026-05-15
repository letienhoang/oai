# Release Notes - v1.3.0

## v1.3.0 - Phase 10 Upload Batch, PDF Processing, and Source File Viewer

### Added

- Added upload batch processing with background jobs.
- Added batch status API and batch detail page.
- Added file type detection for images, PDFs, ZIP archives, and unsupported files.
- Added embedded text extraction for text-based PDFs.
- Added scanned PDF page rendering.
- Added PDF page preview storage.
- Added OCR support for rendered PDF pages and merged raw text processing.
- Added secure file download API.
- Added secure file preview API.
- Added source file viewer in Invoice Detail.
- Added invoice source file list.

### Improved

- Improved upload pipeline reliability across Web/API/Worker using shared file storage configuration.
- Improved invoice source metadata tracking with InvoiceSourceFiles.
- Improved rule-based parser support for digital PDF invoice text.
- Improved PDF/image upload UI support.

### Fixed

- Fixed worker storage path resolution for uploaded files.
- Fixed source file preview routing from the Blazor Web app.
- Fixed invoice number and tax parsing issues for text-based PDF invoices.
- Fixed runtime storage files being ignored by Git.

