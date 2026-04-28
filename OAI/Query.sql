DECLARE @InvoiceId UNIQUEIDENTIFIER;

BEGIN -- Delete the invoice and related data for invoice number 'INV-2026-001'
    DECLARE @InvoiceId UNIQUEIDENTIFIER;
    SELECT @InvoiceId = Id
    FROM Invoices
    WHERE InvoiceNumber = 'INV-2026-001';
    
    IF @InvoiceId IS NOT NULL
    BEGIN
        DELETE FROM ValidationIssues
        WHERE InvoiceId = @InvoiceId;

        DELETE FROM InvoiceExtractionResults
        WHERE InvoiceId = @InvoiceId;

        DELETE FROM InvoiceLineItems
        WHERE InvoiceId = @InvoiceId;

        DELETE FROM Invoices
        WHERE Id = @InvoiceId;
    END;
END;

Select *
FROM AuditLogs;