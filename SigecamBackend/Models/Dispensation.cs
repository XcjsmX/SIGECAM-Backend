using Google.Cloud.Firestore;

namespace SigecamBackend.Models;

[FirestoreData]
public class Dispensation
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("consultaId")]
    public string ConsultaId { get; set; } = string.Empty;

    [FirestoreProperty("pacienteId")]
    public string PacienteId { get; set; } = string.Empty;

    [FirestoreProperty("centroId")]
    public string CentroId { get; set; } = string.Empty;

    [FirestoreProperty("medicamentoId")]
    public string MedicamentoId { get; set; } = string.Empty;

    [FirestoreProperty("medicamentoNombre")]
    public string MedicamentoNombre { get; set; } = string.Empty;

    [FirestoreProperty("cantidadEntregada")]
    public int CantidadEntregada { get; set; }

    [FirestoreProperty("dispensadoPorId")]
    public string DispensadoPorId { get; set; } = string.Empty;

    [FirestoreProperty("fechaDispensacion")]
    public DateTime FechaDispensacion { get; set; } = DateTime.UtcNow;
}