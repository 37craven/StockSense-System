using System.ComponentModel.DataAnnotations;
using StockSense.Application.DTOs;

namespace StockSense.Tests;

public class CreateEmployeeDtoValidationTests
{
    [Fact]
    public void MissingRequiredStaffFieldsReturnClearMessages()
    {
        var dto = new CreateEmployeeDto
        {
            Email = "staff@example.com",
            Password = "Valid_123",
            FirstName = "",
            LastName = ""
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.ErrorMessage == "First name is required.");
        Assert.Contains(results, result => result.ErrorMessage == "Last name is required.");
    }
}
