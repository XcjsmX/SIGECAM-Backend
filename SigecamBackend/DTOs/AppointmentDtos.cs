using System.ComponentModel.DataAnnotations;

namespace SigecamBackend.DTOs;

public class CreateAppointmentDto
{
    [Required]
    public string CentroId { get; set; } = string.Empty;

    [Required]
    public string Servicio { get; set; } = string.Empty;

    [Required]
    public string Fecha { get; set; } = string.Empty; // YYYY-MM-DD
}

public class CancelAppointmentDto
{
    public string? Justificacion { get; set; }
}