// Models/GateDto.cs

using System.ComponentModel.DataAnnotations;

namespace QMSWebApiCore.Models
{
    public class GateInCreateDto
    {
        [Required(ErrorMessage = "Barcode is required")]
        [StringLength(50, ErrorMessage = "Barcode cannot exceed 50 characters")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Driver Name is required")]
        [StringLength(100, ErrorMessage = "Driver Name cannot exceed 100 characters")]
        public string DriverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "License Truck is required")]
        [StringLength(20, ErrorMessage = "License Truck cannot exceed 20 characters")]
        public string LicenseTruck { get; set; } = string.Empty;

        [Range(1, 9999, ErrorMessage = "RSU ID must be between 1 and 9999")]
        public int? RSUID { get; set; }

        [StringLength(500, ErrorMessage = "Remark cannot exceed 500 characters")]
        public string? GateInRemark { get; set; }

        [StringLength(50)]
        public string? SealNumber { get; set; }

        [StringLength(10)]
        public string Status { get; set; } = "1";

        [Required]
        [StringLength(10)]
        public string DCCode { get; set; }

        [StringLength(50)]
        public string? ActionBy { get; set; }

        public int? TruckTypeId { get; set; }
        public int? Id { get; set; }

        public int? Gate_id { get; set; }
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

    public class GateInCreateDto2
    {
        public string? Id { get; set; }
        public string? barcode { get; set; }
    }
}