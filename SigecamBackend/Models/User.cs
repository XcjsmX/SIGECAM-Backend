using Google.Cloud.Firestore;

namespace SigecamBackend.Models;

[FirestoreData]
public class User
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty("email")]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    // Roles permitidos: "Admin", "PersonalSalud", "Paciente"
    [FirestoreProperty("rol")]
    public string Rol { get; set; } = "Paciente";

    // Aplica solo si es PersonalSalud (ID del centro asignado)
    [FirestoreProperty("centroId")]
    public string? CentroId { get; set; }

    [FirestoreProperty("servicioAsignado")]
    public string? ServicioAsignado { get; set; }

    [FirestoreProperty("activo")]
    public bool Activo { get; set; } = true;

    [FirestoreProperty("fechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}