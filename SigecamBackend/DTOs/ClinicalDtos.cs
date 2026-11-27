using System.ComponentModel.DataAnnotations;

namespace SigecamBackend.DTOs;

public class CreateMedicationDto
{
    [Required]
    public string CentroId { get; set; } = string.Empty;

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Presentacion { get; set; } = string.Empty;

    [Range(0, 100000)]
    public int StockActual { get; set; }

    [Range(0, 10000)]
    public int StockMinimo { get; set; }
}

public class RestockMedicationDto
{
    [Range(1, 100000)]
    public int CantidadAgregar { get; set; }
}

public class DispenseItemDto
{
    [Required]
    public string MedicamentoId { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Cantidad { get; set; }
}

public class CompleteConsultationDto
{
    [Required]
    public string CitaId { get; set; } = string.Empty;

    [Required]
    public string Diagnostico { get; set; } = string.Empty;

    public string Observaciones { get; set; } = string.Empty;

    public List<DispenseItemDto> MedicamentosDispensados { get; set; } = new();
}