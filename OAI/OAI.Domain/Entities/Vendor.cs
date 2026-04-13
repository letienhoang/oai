using OAI.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Entities;

public sealed class Vendor : Entity
{
    public string Name { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Address { get; private set; }
    public string? Email { get; private set; }

    private Vendor()
    {
        Name = string.Empty;
    }

    public Vendor(
        string name,
        string? taxNumber = null,
        string? address = null,
        string? email = null)
    {
        UpdateProfile(name, taxNumber, address, email);
    }

    public void UpdateProfile(
        string name,
        string? taxNumber = null,
        string? address = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vendor name is required.", nameof(name));

        Name = name.Trim();
        TaxNumber = string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        Touch();
    }
}
