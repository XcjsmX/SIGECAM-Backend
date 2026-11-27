using Google.Cloud.Firestore;

namespace SigecamBackend.Models;

[FirestoreData]
public class Medication
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("centroId")]
    public string CentroId { get; set; } = string.Empty;

    [FirestoreProperty("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty("presentacion")]
    public string Presentacion { get; set; } = string.Empty;

    [FirestoreProperty("stockActual")]
    public int StockActual { get; set; }

    [FirestoreProperty("stockMinimo")]
    public int StockMinimo { get; set; }

    [FirestoreProperty("activo")]
    public bool Activo { get; set; } = true;
}