using StockSense.Domain.Entities;
using System.Text.Json.Serialization;

namespace StockSense.Application.DTOs
{
    public class MaintenanceProjection
    {
        // Oil & Fluids
        public int OilChangeIntervalKm { get; set; }

        public string OilType { get; set; } = string.Empty;

        public int CoolantChangeIntervalKm { get; set; }

        public int BrakeFluidIntervalMonths { get; set; }

        // Valvetrain
        public int ValveClearanceCheckIntervalKm { get; set; }

        public int ValveSpringReplaceIntervalKm { get; set; }

        // Bottom End
        public int PistonRingIntervalKm { get; set; }

        public int ConRodBearingIntervalKm { get; set; }

        public int MainBearingIntervalKm { get; set; }

        // Fuel & Tune
        public string FuelRequirement { get; set; } = string.Empty;

        public int ECUTuneCheckIntervalKm { get; set; }

        // Consumables
        public int ChainAdjustIntervalKm { get; set; }

        public int SprocketReplaceIntervalKm { get; set; }

        public int ClutchPlateIntervalKm { get; set; }

        // Summary
        public string MaintenanceTier { get; set; } = string.Empty; // Street, Sport, Race, Drag

        public List<string> Warnings { get; set; } = new();

        public List<string> Tips { get; set; } = new();

        // Stress factor used for calculation
        public double StressFactor { get; set; }
    }
}