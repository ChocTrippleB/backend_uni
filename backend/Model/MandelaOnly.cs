using System;
using System.ComponentModel.DataAnnotations;

public class MandelaOnlyAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var email = value as string;

        if (!string.IsNullOrEmpty(email) && email.EndsWith("@mandela.ac.za", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult("Email must end with @mandela.ac.za.");
    }
}