using StockSense.Domain.Entities;
using System.Text.Json.Serialization;

namespace StockSense.Application.DTOs
{
    public class BuildProjection
    {
        public int BikeModelId { get; set; }
        public string BikeName { get; set; } = string.Empty;

        public int BaseCC { get; set; }
        public int BaseHP { get; set; }
        public int BaseTorque { get; set; }

        public int AddedCC { get; set; }
        public int AddedHP { get; set; }
        public int AddedTorque { get; set; }
        public int ReliabilityScore { get; set; }

        public int FinalCC { get; set; }
        public int FinalHP { get; set; }
        public int FinalTorque { get; set; }

        public decimal TotalPartsCost { get; set; }
        public decimal EstimatedLaborCost { get; set; }
        public decimal TotalCost { get; set; }

        public int? MatchedStageNumber { get; set; }
        public string MatchedStageName { get; set; } = string.Empty;

        public MaintenanceProjection? Maintenance { get; set; }

        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
        public List<string> ValidationSuggestions { get; set; } = new();

        public bool IsValid { get; set; }
    }
}