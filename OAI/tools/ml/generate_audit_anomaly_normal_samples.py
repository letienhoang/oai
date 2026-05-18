"""Generate deterministic normal audit anomaly samples for Phase 12A T131."""

from __future__ import annotations

import csv
import random
import uuid
from pathlib import Path


RANDOM_SEED = 20260518
ROW_COUNT = 1000

REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SAMPLE_CSV_PATH = REPOSITORY_ROOT / "docs" / "ml" / "audit_anomaly_dataset_sample.csv"
OUTPUT_CSV_PATH = (
    REPOSITORY_ROOT / "docs" / "ml" / "generated" / "audit_anomaly_normal_1000.csv"
)

SCENARIOS = [
    ("normal_straight_through", 30),
    ("normal_minor_correction", 22),
    ("normal_validation_then_approval", 18),
    ("normal_pending_review", 12),
    ("normal_exported_after_approval", 12),
    ("normal_two_user_review", 6),
]

BOOLEAN_COLUMNS = {
    "vendor_changed",
    "invoice_number_changed",
    "currency_changed",
    "edited_after_approved",
    "exported_after_rejected",
    "outside_business_hours",
    "has_deleted_line_item",
    "has_reopened_invoice",
    "total_tax_mismatch",
    "repeated_processing_attempts",
}

NUMERIC_COLUMNS = {
    "label",
    "edit_count",
    "approve_count",
    "reject_count",
    "status_changed_count",
    "distinct_user_count",
    "validation_count",
    "export_count",
    "subtotal_change_ratio",
    "tax_change_ratio",
    "total_change_ratio",
    "vendor_changed",
    "invoice_number_changed",
    "currency_changed",
    "edited_after_approved",
    "exported_after_rejected",
    "outside_business_hours",
    "has_deleted_line_item",
    "has_reopened_invoice",
    "total_tax_mismatch",
    "repeated_processing_attempts",
    "minutes_between_create_and_approve",
    "max_updates_within_10_minutes",
    "audit_duration_minutes",
}

FORBIDDEN_ANOMALY_FLAGS = {
    "edited_after_approved",
    "exported_after_rejected",
    "currency_changed",
    "has_reopened_invoice",
    "total_tax_mismatch",
    "repeated_processing_attempts",
}


def main() -> None:
    header = read_sample_header()
    rng = random.Random(RANDOM_SEED)
    rows = [generate_row(rng, index) for index in range(1, ROW_COUNT + 1)]

    validate_rows(rows, header)

    OUTPUT_CSV_PATH.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_CSV_PATH.open("w", encoding="utf-8", newline="") as csv_file:
        writer = csv.DictWriter(csv_file, fieldnames=header)
        writer.writeheader()
        writer.writerows(rows)

    print(f"Wrote {len(rows)} normal audit anomaly samples to {OUTPUT_CSV_PATH}")


def read_sample_header() -> list[str]:
    with SAMPLE_CSV_PATH.open("r", encoding="utf-8", newline="") as csv_file:
        return next(csv.reader(csv_file))


def generate_row(rng: random.Random, index: int) -> dict[str, str]:
    scenario = weighted_scenario(rng)
    base = {
        "sample_id": f"NORMAL-{index:04d}",
        "invoice_id": str(uuid.uuid5(uuid.NAMESPACE_URL, f"oai-normal-audit-sample-{index}")),
        "generated_scenario": scenario,
        "label": "0",
        "reject_count": "0",
        "currency_changed": "0",
        "edited_after_approved": "0",
        "exported_after_rejected": "0",
        "has_reopened_invoice": "0",
        "total_tax_mismatch": "0",
        "repeated_processing_attempts": "0",
        "anomaly_reason_codes": "",
    }

    scenario_values = build_scenario_values(rng, scenario)
    row = {**base, **scenario_values}
    row["notes"] = normal_note(scenario)
    return row


def weighted_scenario(rng: random.Random) -> str:
    names = [item[0] for item in SCENARIOS]
    weights = [item[1] for item in SCENARIOS]
    return rng.choices(names, weights=weights, k=1)[0]


def build_scenario_values(rng: random.Random, scenario: str) -> dict[str, str]:
    values = {
        "edit_count": 0,
        "approve_count": 1,
        "status_changed_count": 1,
        "distinct_user_count": weighted_int(rng, [(1, 74), (2, 24), (3, 2)]),
        "validation_count": weighted_int(rng, [(0, 42), (1, 48), (2, 10)]),
        "export_count": weighted_int(rng, [(0, 62), (1, 38)]),
        "vendor_changed": 0,
        "invoice_number_changed": 0,
        "outside_business_hours": 1 if rng.random() < 0.035 else 0,
        "has_deleted_line_item": 0,
        "max_updates_within_10_minutes": weighted_int(rng, [(0, 45), (1, 42), (2, 11), (3, 2)]),
    }

    if scenario == "normal_straight_through":
        values.update(
            edit_count=weighted_int(rng, [(0, 70), (1, 25), (2, 5)]),
            validation_count=weighted_int(rng, [(0, 55), (1, 42), (2, 3)]),
            distinct_user_count=weighted_int(rng, [(1, 88), (2, 12)]),
            max_updates_within_10_minutes=weighted_int(rng, [(0, 65), (1, 32), (2, 3)]),
        )
    elif scenario == "normal_minor_correction":
        values.update(
            edit_count=weighted_int(rng, [(1, 50), (2, 38), (3, 12)]),
            validation_count=weighted_int(rng, [(1, 62), (2, 38)]),
            vendor_changed=1 if rng.random() < 0.025 else 0,
            invoice_number_changed=1 if rng.random() < 0.08 else 0,
            has_deleted_line_item=1 if rng.random() < 0.055 else 0,
        )
    elif scenario == "normal_validation_then_approval":
        values.update(
            edit_count=weighted_int(rng, [(0, 20), (1, 44), (2, 28), (3, 8)]),
            validation_count=weighted_int(rng, [(1, 42), (2, 58)]),
            status_changed_count=weighted_int(rng, [(1, 75), (2, 25)]),
        )
    elif scenario == "normal_pending_review":
        values.update(
            approve_count=0,
            export_count=0,
            status_changed_count=weighted_int(rng, [(0, 35), (1, 65)]),
            edit_count=weighted_int(rng, [(0, 36), (1, 42), (2, 18), (3, 4)]),
            validation_count=weighted_int(rng, [(0, 30), (1, 55), (2, 15)]),
            distinct_user_count=weighted_int(rng, [(1, 82), (2, 18)]),
        )
    elif scenario == "normal_exported_after_approval":
        values.update(
            approve_count=1,
            export_count=1,
            status_changed_count=weighted_int(rng, [(1, 78), (2, 22)]),
            edit_count=weighted_int(rng, [(0, 38), (1, 42), (2, 17), (3, 3)]),
        )
    elif scenario == "normal_two_user_review":
        values.update(
            distinct_user_count=weighted_int(rng, [(2, 86), (3, 14)]),
            edit_count=weighted_int(rng, [(1, 38), (2, 46), (3, 16)]),
            validation_count=weighted_int(rng, [(1, 50), (2, 50)]),
            status_changed_count=weighted_int(rng, [(1, 60), (2, 40)]),
        )
    else:
        raise ValueError(f"Unsupported scenario: {scenario}")

    ratios = normal_change_ratios(rng, values["edit_count"])
    values.update(ratios)
    values["minutes_between_create_and_approve"] = approval_minutes(rng, values["approve_count"])
    values["audit_duration_minutes"] = audit_duration_minutes(
        rng,
        values["approve_count"],
        values["export_count"],
        values["minutes_between_create_and_approve"],
    )

    return {key: str(value) for key, value in values.items()}


def weighted_int(rng: random.Random, weighted_values: list[tuple[int, int]]) -> int:
    values = [item[0] for item in weighted_values]
    weights = [item[1] for item in weighted_values]
    return rng.choices(values, weights=weights, k=1)[0]


def normal_change_ratios(rng: random.Random, edit_count: int) -> dict[str, str]:
    if edit_count == 0:
        subtotal = 0.0
        tax = 0.0
    else:
        upper_bound = 0.018 if edit_count == 1 else 0.035 if edit_count == 2 else 0.050
        subtotal = rng.triangular(0.0, upper_bound, upper_bound / 3)
        tax = rng.triangular(0.0, upper_bound, upper_bound / 3)

    total = max(0.0, min(0.050, (subtotal * 0.82) + (tax * 0.18) + rng.uniform(-0.003, 0.003)))
    return {
        "subtotal_change_ratio": format_ratio(subtotal),
        "tax_change_ratio": format_ratio(tax),
        "total_change_ratio": format_ratio(total),
    }


def approval_minutes(rng: random.Random, approve_count: int) -> int:
    if approve_count == 0:
        return 0
    return weighted_int(
        rng,
        [
            (rng.randint(15, 90), 24),
            (rng.randint(91, 480), 45),
            (rng.randint(481, 1440), 31),
        ],
    )


def audit_duration_minutes(
    rng: random.Random,
    approve_count: int,
    export_count: int,
    minutes_between_create_and_approve: int,
) -> int:
    if approve_count == 0:
        return rng.randint(10, 2880)

    minimum = max(10, minutes_between_create_and_approve)
    export_padding = rng.randint(20, 720) if export_count == 1 else rng.randint(0, 360)
    return min(2880, max(minimum, minutes_between_create_and_approve + export_padding))


def format_ratio(value: float) -> str:
    return f"{value:.3f}"


def normal_note(scenario: str) -> str:
    notes = {
        "normal_straight_through": "Invoice followed a low-touch review and approval path.",
        "normal_minor_correction": "Small pre-approval correction resolved during routine review.",
        "normal_validation_then_approval": "Validation was rerun before ordinary approval.",
        "normal_pending_review": "Invoice remains in pending review with no approval or export.",
        "normal_exported_after_approval": "Invoice was exported only after approval.",
        "normal_two_user_review": "Two reviewers participated in a normal approval workflow.",
    }
    return notes[scenario]


def validate_rows(rows: list[dict[str, str]], header: list[str]) -> None:
    if len(rows) != ROW_COUNT:
        raise ValueError(f"Expected {ROW_COUNT} rows, got {len(rows)}.")

    expected_columns = set(header)
    for row_number, row in enumerate(rows, start=1):
        missing = expected_columns.difference(row)
        extra = set(row).difference(expected_columns)
        if missing or extra:
            raise ValueError(
                f"Row {row_number} does not match expected header. Missing={missing}, Extra={extra}"
            )

        if row["label"] != "0":
            raise ValueError(f"Row {row_number} has non-normal label {row['label']}.")

        if row["anomaly_reason_codes"] != "":
            raise ValueError(f"Row {row_number} has anomaly reason codes.")

        for column in BOOLEAN_COLUMNS:
            if row[column] not in {"0", "1"}:
                raise ValueError(f"Row {row_number} has invalid boolean {column}={row[column]}.")

        for column in NUMERIC_COLUMNS:
            if float(row[column]) < 0:
                raise ValueError(f"Row {row_number} has negative {column}={row[column]}.")

        for column in FORBIDDEN_ANOMALY_FLAGS:
            if row[column] != "0":
                raise ValueError(f"Row {row_number} has forbidden anomaly flag {column}=1.")

        if row["approve_count"] == "0" and row["minutes_between_create_and_approve"] != "0":
            raise ValueError(f"Row {row_number} has approval minutes without approval.")

        if row["generated_scenario"] == "normal_pending_review":
            if row["approve_count"] != "0" or row["export_count"] != "0":
                raise ValueError(f"Row {row_number} pending review row was approved or exported.")

        if row["export_count"] == "1" and row["approve_count"] != "1":
            raise ValueError(f"Row {row_number} was exported without approval.")


if __name__ == "__main__":
    main()
