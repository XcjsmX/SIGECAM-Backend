using Google.Cloud.Firestore;

namespace SigecamBackend.Models;

[FirestoreData]
public class Appointment
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("pacienteId")]
    public string PacienteId { get; set; } = string.Empty;

    [FirestoreProperty("pacienteNombre")]
    public string PacienteNombre { get; set; } = string.Empty;

    [FirestoreProperty("centroId")]
    public string CentroId { get; set; } = string.Empty;

    [FirestoreProperty("servicio")]
    public string Servicio { get; set; } = string.Empty;

    [FirestoreProperty("fecha")]
    public string Fecha { get; set; } = string.Empty;

    // Estados: "programada", "atendida", "cancelada", "no_asistio"
    [FirestoreProperty("estado")]
    public string Estado { get; set; } = "programada";

    [FirestoreProperty("justificacion")]
    public string? Justificacion { get; set; }

    [FirestoreProperty("fechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [FirestoreProperty("fechaActualizacion")]
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}