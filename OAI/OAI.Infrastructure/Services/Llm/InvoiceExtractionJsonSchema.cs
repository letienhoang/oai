namespace OAI.Infrastructure.Services.Llm;

public static class InvoiceExtractionJsonSchema
{
    public const string Schema = """
    {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "vendorName": {
          "type": "string",
          "description": "Seller or supplier company name. Do not use OCR noise tokens."
        },
        "vendorTaxNumber": {
          "type": ["string", "null"],
          "description": "Seller tax number if present."
        },
        "vendorAddress": {
          "type": ["string", "null"],
          "description": "Seller address if present."
        },
        "vendorEmail": {
          "type": ["string", "null"],
          "description": "Seller email if present."
        },
        "invoiceNumber": {
          "type": "string",
          "description": "Invoice number exactly as written, for example INV-2026-001."
        },
        "issueDate": {
          "type": "string",
          "description": "Invoice issue date in yyyy-MM-dd format."
        },
        "dueDate": {
          "type": ["string", "null"],
          "description": "Due date in yyyy-MM-dd format, or null if not found."
        },
        "currency": {
          "type": "string",
          "enum": ["VND", "USD", "EUR"],
          "description": "Invoice currency."
        },
        "declaredSubtotal": {
          "type": "number",
          "description": "Subtotal before tax."
        },
        "declaredTaxAmount": {
          "type": "number",
          "description": "Tax/VAT amount."
        },
        "declaredTotalAmount": {
          "type": "number",
          "description": "Final total amount."
        },
        "lineItems": {
          "type": "array",
          "description": "Invoice line items.",
          "items": {
            "type": "object",
            "additionalProperties": false,
            "properties": {
              "lineNo": {
                "type": "integer"
              },
              "description": {
                "type": "string"
              },
              "quantity": {
                "type": "number"
              },
              "unitPrice": {
                "type": "number"
              },
              "taxRate": {
                "type": "number"
              }
            },
            "required": [
              "lineNo",
              "description",
              "quantity",
              "unitPrice",
              "taxRate"
            ]
          }
        }
      },
      "required": [
        "vendorName",
        "vendorTaxNumber",
        "vendorAddress",
        "vendorEmail",
        "invoiceNumber",
        "issueDate",
        "dueDate",
        "currency",
        "declaredSubtotal",
        "declaredTaxAmount",
        "declaredTotalAmount",
        "lineItems"
      ]
    }
    """;
}