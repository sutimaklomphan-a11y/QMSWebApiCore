// Models/GateDto.cs

using System.ComponentModel.DataAnnotations;

namespace QMSWebApiCore.Models
{
    public class GateInCreateDto
    {
        [Required(ErrorMessage = "Container Number is required")]
        [MaxLength(50)]
        public string ContainerNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Truck Number is required")]
        [MaxLength(20)]
        public string TruckNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Driver Name is required")]
        [MaxLength(100)]
        public string DriverName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? SealNumber { get; set; }

        public string? Remarks { get; set; }
    }

    public class GateInUpdateDto
    {
        [MaxLength(50)]
        public string? ContainerNumber { get; set; }

        [MaxLength(20)]
        public string? TruckNumber { get; set; }

        [MaxLength(100)]
        public string? DriverName { get; set; }

        [MaxLength(50)]
        public string? SealNumber { get; set; }

        public string? Remarks { get; set; }

        [MaxLength(10)]
        public string? Status { get; set; }
    }

    public class GateOutCreateDto
    {
        public string? Id { get; set; }
        public string? ContainerNumber { get; set; }
    }
}