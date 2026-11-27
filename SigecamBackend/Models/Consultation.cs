using Google.Cloud.Firestore;

namespace SigecamBackend.Models;

[FirestoreData]
public class Consultation
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("citaId")]
    public string CitaId { get; set; } = string.Empty;

    [FirestoreProperty("pacienteId")]
    public string PacienteId { get; set; } = string.Empty;

    [FirestoreProperty("centroId")]
    public string CentroId { get; set; } = string.Empty;

    [FirestoreProperty("personalSaludId")]
    public string PersonalSaludId { get; set; } = string.Empty;

    [FirestoreProperty("diagnostico")]
    public string Diagnostico { get; set; } = string.Empty;

    [FirestoreProperty("observaciones")]
    public string Observaciones { get; set; } = string.Empty;

    [FirestoreProperty("fechaConsulta")]
    public DateTime FechaConsulta { get; set; } = DateTime.UtcNow;
}