using OAI.Application.AuditAnomaly;

namespace OAI.Application.Tests.AuditAnomaly;

public sealed class AuditAnomalyDatasetSchemaTests
{
    private static readonly string[] ExpectedCsvColumns =
    [
        "sample_id",
        "invoice_id",
        "generated_scenario",
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
        "anomaly_reason_codes",
        "notes"
    ];

    [Fact]
    public void All_ShouldContainUniqueFeatureNames()
    {
        Assert.Equal(
            AuditAnomalyFeatureNames.All.Length,
            AuditAnomalyFeatureNames.All.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void All_ShouldExcludeIdentityExplanationAndLabelColumns()
    {
        var excludedColumns = new[]
        {
            "sample_id",
            "invoice_id",
            "generated_scenario",
            "label",
            "anomaly_reason_codes",
            "notes"
        };

        Assert.DoesNotContain(AuditAnomalyFeatureNames.All, excludedColumns.Contains);
        Assert.Equal(ExpectedCsvColumns[4..^2], AuditAnomalyFeatureNames.All);
    }

    [Fact]
    public void AuditAnomalyDatasetLabel_ShouldUseCanonicalValues()
    {
        Assert.Equal(0, (int)AuditAnomalyDatasetLabel.Normal);
        Assert.Equal(1, (int)AuditAnomalyDatasetLabel.Anomaly);
    }

    [Fact]
    public void SampleCsv_ShouldContainExpectedHeaderColumns()
    {
        var repositoryRoot = FindRepositoryRoot();
        var samplePath = Path.Combine(repositoryRoot, "docs", "ml", "audit_anomaly_dataset_sample.csv");

        var header = File.ReadLines(samplePath).First();
        var actualColumns = header.Split(',');

        Assert.Equal(ExpectedCsvColumns, actualColumns);
    }

    [Fact]
    public void GeneratedNormalCsv_ShouldContainOnlyNormalSamplesWithCanonicalHeader()
    {
        var repositoryRoot = FindRepositoryRoot();
        var samplePath = Path.Combine(repositoryRoot, "docs", "ml", "audit_anomaly_dataset_sample.csv");
        var generatedPath = Path.Combine(repositoryRoot, "docs", "ml", "generated", "audit_anomaly_normal_1000.csv");

        Assert.True(File.Exists(generatedPath), $"Generated normal dataset was not found at {generatedPath}.");

        var sampleHeader = File.ReadLines(samplePath).First().Split(',');
        var generatedLines = File.ReadAllLines(generatedPath);
        var generatedHeader = generatedLines[0].Split(',');

        Assert.Equal(sampleHeader, generatedHeader);
        Assert.Equal(1000, generatedLines.Length - 1);

        var indexes = generatedHeader
            .Select((column, index) => new { column, index })
            .ToDictionary(item => item.column, item => item.index, StringComparer.Ordinal);

        var forbiddenFlags = new[]
        {
            "edited_after_approved",
            "exported_after_rejected",
            "currency_changed",
            "has_reopened_invoice",
            "total_tax_mismatch",
            "repeated_processing_attempts"
        };

        foreach (var line in generatedLines.Skip(1))
        {
            var columns = line.Split(',');

            Assert.Equal(generatedHeader.Length, columns.Length);
            Assert.Equal("0", columns[indexes["label"]]);
            Assert.Equal(string.Empty, columns[indexes["anomaly_reason_codes"]]);

            foreach (var forbiddenFlag in forbiddenFlags)
            {
                Assert.Equal("0", columns[indexes[forbiddenFlag]]);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var docsPath = Path.Combine(directory.FullName, "docs", "ml", "audit_anomaly_dataset_sample.csv");
            if (File.Exists(docsPath))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing docs/ml/audit_anomaly_dataset_sample.csv.");
    }
}
