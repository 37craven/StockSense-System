using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
        public bool IsBlocked { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public sealed class UserProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Temporary password is required.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [RegularExpression("^(Employee|Admin)$", ErrorMessage = "Select Employee or Admin as the role.")]
        public string Role { get; set; } = "Employee"; // Admin or Employee
    }





    public class UpdateServiceProductsDto
    {
        public int ServiceId { get; set; }
        public decimal Price { get; set; }
        public List<int> ProductIds { get; set; } = new();
    }
}
