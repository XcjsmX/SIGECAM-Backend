using Google.Cloud.Firestore;

namespace SigecamBackend.Models;

[FirestoreData]
public class DailySlot
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("centroId")]
    public string CentroId { get; set; } = string.Empty;

    [FirestoreProperty("servicio")]
    public string Servicio { get; set; } = string.Empty;

    // Formato YYYY-MM-DD (ej. "2026-11-30")
    [FirestoreProperty("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [FirestoreProperty("cupoTotal")]
    public int CupoTotal { get; set; }

    [FirestoreProperty("cupoOcupado")]
    public int CupoOcupado { get; set; } = 0;
}