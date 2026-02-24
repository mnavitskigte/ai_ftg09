using EtlFunction.Models;
using FluentValidation;

namespace EtlFunction.Validators;

/// <summary>
/// FluentValidation validator for supplier records.
/// </summary>
public sealed class SupplierRecordValidator : AbstractValidator<SupplierRecord>
{
    /// <summary>
    /// Initializes validator rules.
    /// </summary>
    public SupplierRecordValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.BankAccountName).NotEmpty();
        RuleFor(x => x.BankAccountNumber).NotEmpty();
        RuleFor(x => x.BankRoutingNumber).NotEmpty();
        RuleFor(x => x.AddressLine1).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.CountryCode).NotEmpty();
    }
}
