namespace StockSense.Application.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
        public bool IsBlocked { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee"; // Admin or Employee
    }





    public class UpdateServiceProductsDto
    {
        public int ServiceId { get; set; }
        public decimal Price { get; set; }
        public List<int> ProductIds { get; set; } = new();
    }
}
