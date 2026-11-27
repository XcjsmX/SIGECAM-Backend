using System.ComponentModel.DataAnnotations;

namespace SigecamBackend.DTOs;

public class CreateHealthCenterDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Municipio { get; set; } = string.Empty;

    [Required]
    public List<string> Servicios { get; set; } = new();

    [Required]
    public string Horario { get; set; } = string.Empty;
}

public class SetDailySlotDto
{
    [Required]
    public string CentroId { get; set; } = string.Empty;

    [Required]
    public string Servicio { get; set; } = string.Empty;

    // Formato YYYY-MM-DD (ej. "2026-11-30")
    [Required]
    public string Fecha { get; set; } = string.Empty;

    [Range(1, 500)]
    public int CupoTotal { get; set; }
}