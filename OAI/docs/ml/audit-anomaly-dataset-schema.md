# Audit Anomaly Dataset Schema

## Purpose

This dataset defines the invoice-level feature schema for audit anomaly detection in OAI. It is designed for later tasks that will generate synthetic examples, normalize numeric features, train and evaluate a lightweight Logistic Regression model, export learned weights, and run inference from C#.

The dataset is not an LLM dataset. It does not contain prompts, completions, embeddings, free-form audit narratives, or raw document text. It is a tabular ML dataset intended for a small, explainable classifier where each numeric coefficient can be traced back to a named invoice behavior feature.

`label = 0` means normal invoice audit behavior. `label = 1` means anomalous invoice audit behavior.

## Invoice-Level Design

The dataset is invoice-level, not raw audit-log-row-level, because anomaly detection should evaluate the full behavior around one invoice lifecycle. A single `AuditLogEntry` row can describe a harmless update, but the sequence may become suspicious when many edits happen rapidly, when edits happen after approval, when rejected invoices are exported, or when amount changes are large.

Raw `AuditLogEntry` records are grouped by invoice identity, usually where:

- `AuditLogEntry.EntityName` identifies `Invoice` or related invoice-owned records such as line items.
- `AuditLogEntry.EntityId` matches the invoice id, or related records are joined back to an invoice.
- `AuditLogEntry.ActionType`, `OldValuesJson`, `NewValuesJson`, `UserId`, `UserName`, `OccurredAt`, `CorrelationId`, and `Source` provide behavioral signals.
- Invoice fields such as `VendorId`, `InvoiceNumber`, `IssueDate`, `DueDate`, `Currency`, `DeclaredSubtotal`, `DeclaredTaxAmount`, `DeclaredTotalAmount`, and `Status` provide the business state for comparison.

The feature extractor converts all audit rows for one invoice into one feature vector, then writes one row to `audit_anomaly_dataset.csv`.

## Expected Pipeline

```txt
AuditLogs + Invoice data
-> feature extraction
-> audit_anomaly_dataset.csv
-> normalization
-> train/test split
-> Logistic Regression
-> exported model weights
-> C# inference
```

## Column Schema

Boolean features are encoded as integers: `0 = false`, `1 = true`. Ratio features are non-negative decimal values. Counts and durations are non-negative integers.

| Column | Type | Allowed values / range | Source fields | Explanation | Why it may indicate anomaly |
|---|---:|---|---|---|---|
| `sample_id` | string | non-empty | Generated dataset metadata | Stable row id for generated or curated samples. | Not a model feature; used for traceability. |
| `invoice_id` | string | GUID string | `Invoice.Id`, `AuditLogEntry.EntityId` | Invoice represented by this row. | Not a model feature; used for joins and debugging. |
| `generated_scenario` | string | non-empty scenario key | Generated dataset metadata | Scenario name for synthetic or curated rows. | Helps evaluate scenario coverage but is excluded from training. |
| `label` | integer | `0`, `1` | Generated or analyst-assigned label | Target class: `0 = normal`, `1 = anomaly`. | Supervised training target, not an input feature. |
| `edit_count` | integer | `0..*` | `ActionType`, `EntityName`, `EntityId` | Number of invoice update events. | High edit volume can indicate tampering, OCR instability, or review churn. |
| `approve_count` | integer | `0..*` | `ActionType`, `NewValuesJson`, `Invoice.Status` | Number of transitions to approved. | Multiple approvals can indicate reopen/reapprove loops. |
| `reject_count` | integer | `0..*` | `ActionType`, `NewValuesJson`, `Invoice.Status` | Number of transitions to rejected. | Repeated rejection can signal disputed or inconsistent invoice data. |
| `status_changed_count` | integer | `0..*` | `OldValuesJson`, `NewValuesJson`, `Invoice.Status` | Number of detected status changes. | Excessive lifecycle movement can indicate unusual handling. |
| `distinct_user_count` | integer | `0..*` | `UserId`, `UserName` | Count of distinct users involved in the invoice audit trail. | Too many users can indicate handoff confusion or unauthorized intervention. |
| `validation_count` | integer | `0..*` | `ActionType = Validated`, validation issue records | Number of validation attempts. | Repeated validation may indicate unstable or manually forced data. |
| `export_count` | integer | `0..*` | `ActionType = Exported` | Number of export events. | Multiple exports or exports in bad states can indicate downstream risk. |
| `subtotal_change_ratio` | decimal | `0..*` | `OldValuesJson`, `NewValuesJson`, `Invoice.DeclaredSubtotal` | Largest relative subtotal change. | Large amount changes are financially sensitive. |
| `tax_change_ratio` | decimal | `0..*` | `OldValuesJson`, `NewValuesJson`, `Invoice.DeclaredTaxAmount` | Largest relative tax amount change. | Large tax changes can signal calculation or fraud risk. |
| `total_change_ratio` | decimal | `0..*` | `OldValuesJson`, `NewValuesJson`, `Invoice.DeclaredTotalAmount` | Largest relative total amount change. | Large total changes directly affect payment value. |
| `vendor_changed` | integer | `0`, `1` | `OldValuesJson`, `NewValuesJson`, `Invoice.VendorId` | Whether vendor changed after creation. | Vendor changes can redirect payment or hide duplicate invoices. |
| `invoice_number_changed` | integer | `0`, `1` | `OldValuesJson`, `NewValuesJson`, `Invoice.InvoiceNumber` | Whether invoice number changed after creation. | Invoice number changes can affect duplicate detection and audit trail integrity. |
| `currency_changed` | integer | `0`, `1` | `OldValuesJson`, `NewValuesJson`, `Invoice.Currency` | Whether currency changed after creation. | Currency changes can drastically alter accounting interpretation. |
| `edited_after_approved` | integer | `0`, `1` | `OccurredAt`, `ActionType`, `Invoice.Status` | Whether an update happened after approval. | Approved data should be stable; later edits are high-risk. |
| `exported_after_rejected` | integer | `0`, `1` | `OccurredAt`, `ActionType`, `Invoice.Status` | Whether export occurred after rejection. | Rejected invoices should not proceed to downstream export. |
| `outside_business_hours` | integer | `0`, `1` | `OccurredAt` | Whether any meaningful event happened outside configured business hours. | Off-hours changes may warrant review in finance workflows. |
| `has_deleted_line_item` | integer | `0`, `1` | `EntityName`, `ActionType = Deleted` | Whether an invoice line item was deleted. | Deleting line items can hide amount mismatches or alter totals. |
| `has_reopened_invoice` | integer | `0`, `1` | `OldValuesJson`, `NewValuesJson`, `Invoice.Status` | Whether invoice moved from approved/rejected back to review. | Reopen flows are legitimate but riskier than straight-through approval. |
| `total_tax_mismatch` | integer | `0`, `1` | Invoice amounts, line items, validation issues | Whether declared totals/tax do not match calculated values or unresolved validation issues. | Amount inconsistencies are strong anomaly signals. |
| `repeated_processing_attempts` | integer | `0`, `1` | `ActionType = Processed`, `CorrelationId`, extraction attempts | Whether processing repeated beyond normal retry expectations. | Repeated processing can indicate OCR/parser failure or manual manipulation. |
| `minutes_between_create_and_approve` | integer | `0..*` | `OccurredAt`, `ActionType`, status changes | Minutes from invoice creation to first approval; `0` when never approved. | Extremely fast approval or very delayed approval can be suspicious depending on context. |
| `max_updates_within_10_minutes` | integer | `0..*` | `OccurredAt`, `ActionType = Updated` | Maximum update count in any rolling 10-minute window. | Bursts of edits can indicate rapid tampering or unstable data entry. |
| `audit_duration_minutes` | integer | `0..*` | `OccurredAt` | Minutes between first and last audit event for the invoice. | Very long or unusually short lifecycles can stand out from normal patterns. |
| `anomaly_reason_codes` | string | empty or semicolon-delimited codes | Generated dataset metadata or analyst notes | Human-readable reason codes for generated anomaly examples. | Explanatory only; excluded from training. |
| `notes` | string | free text | Generated dataset metadata or analyst notes | Optional notes for sample review. | Explanatory only; excluded from training. |

## Training Feature Order

The canonical model input order excludes identity, explanation, and label columns:

```txt
edit_count
approve_count
reject_count
status_changed_count
distinct_user_count
validation_count
export_count
subtotal_change_ratio
tax_change_ratio
total_change_ratio
vendor_changed
invoice_number_changed
currency_changed
edited_after_approved
exported_after_rejected
outside_business_hours
has_deleted_line_item
has_reopened_invoice
total_tax_mismatch
repeated_processing_attempts
minutes_between_create_and_approve
max_updates_within_10_minutes
audit_duration_minutes
```

This order is mirrored by `OAI.Application.AuditAnomaly.AuditAnomalyFeatureNames` so future training, exported model weights, and C# inference stay aligned.

## T131 Normal Sample Generation

Phase 12A T131 generates the deterministic normal-only seed dataset at:

```txt
docs/ml/generated/audit_anomaly_normal_1000.csv
```

Regenerate it from the `OAI` repository folder with:

```powershell
python tools/ml/generate_audit_anomaly_normal_samples.py
```

The script uses only the Python standard library, reads the canonical CSV header from `docs/ml/audit_anomaly_dataset_sample.csv`, and writes exactly 1000 rows with `label = 0`. The output is deterministic because the generator uses a fixed random seed and stable UUID derivation for generated invoice ids.

T131 intentionally creates only normal samples. Anomaly samples will be generated later in T132, before normalization and model training tasks are added.
