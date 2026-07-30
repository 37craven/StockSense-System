namespace StockSense.Application.DTOs;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public List<MissingRequirement> MissingRequirements { get; set; } = new();
    public List<BuildPartConflict> Conflicts { get; set; } = new();
}

public class MissingRequirement
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int RequiredByPartId { get; set; }
    public string RequiredByPartName { get; set; } = string.Empty;
}

public class BuildPartConflict
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int ConflictingPartId { get; set; }
    public string ConflictingPartName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
