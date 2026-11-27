using Google.Cloud.Firestore;

namespace SigecamBackend.Models;

[FirestoreData]
public class HealthCenter
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty("municipio")]
    public string Municipio { get; set; } = string.Empty;

    [FirestoreProperty("servicios")]
    public List<string> Servicios { get; set; } = new();

    [FirestoreProperty("horario")]
    public string Horario { get; set; } = string.Empty;

    [FirestoreProperty("activo")]
    public bool Activo { get; set; } = true;
}