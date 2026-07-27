using StockSense.Domain.Entities;
namespace StockSense.Application.DTOs
{
    public class BuildSummaryDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string BikeBrand { get; set; } = "";
        public string BikeModel { get; set; } = "";
        public int BikeYearStart { get; set; }
        public int BikeYearEnd { get; set; }
        public int BaseCC { get; set; }
        public int BaseHP { get; set; }
        public int BaseTorque { get; set; }
        public string EngineCode { get; set; } = "";
        public string? StageName { get; set; }
        public int? StageNumber { get; set; }
        public int CurrentCC { get; set; }
        public double ProjectedHP { get; set; }
        public double ProjectedTorque { get; set; }
        public int ReliabilityScore { get; set; }
        public decimal TotalPartsCost { get; set; }
        public decimal EstimatedLaborCost { get; set; }
        public List<PartSummaryDto> Parts { get; set; } = new();
        public int PartCount { get; set; }
        public string? MaintenanceTier { get; set; }
        public List<string> ValidationWarnings { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class PartSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Brand { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int CCGain { get; set; }
        public int HPGain { get; set; }
        public int TorqueGain { get; set; }
        public int ReliabilityImpact { get; set; }
        public decimal ListPrice { get; set; }
    }
}
