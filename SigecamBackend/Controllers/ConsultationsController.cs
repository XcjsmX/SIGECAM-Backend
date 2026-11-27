using System.Security.Claims;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigecamBackend.DTOs;
using SigecamBackend.Models;

namespace SigecamBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultationsController : ControllerBase
{
    private readonly FirestoreDb _firestore;

    public ConsultationsController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    [HttpPost]
    [Authorize(Roles = "PersonalSalud")]
    public async Task<IActionResult> CompleteConsultation([FromBody] CompleteConsultationDto dto)
    {
        var personalId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var citaRef = _firestore.Collection("Appointments").Document(dto.CitaId);

        try
        {
            var result = await _firestore.RunTransactionAsync(async transaction =>
            {
                var citaSnap = await transaction.GetSnapshotAsync(citaRef);
                if (!citaSnap.Exists)
                    throw new InvalidOperationException("La cita especificada no existe.");

                var cita = citaSnap.ConvertTo<Appointment>();
                if (cita.Estado != "programada")
                    throw new InvalidOperationException($"Solo se pueden atender citas en estado 'programada'. Estado actual: '{cita.Estado}'.");

                var medDocs = new List<(DocumentReference Ref, Medication Med, int Cantidad)>();
                foreach (var item in dto.MedicamentosDispensados)
                {
                    var medRef = _firestore.Collection("Medications").Document(item.MedicamentoId);
                    var medSnap = await transaction.GetSnapshotAsync(medRef);

                    if (!medSnap.Exists)
                        throw new InvalidOperationException($"El medicamento con ID '{item.MedicamentoId}' no existe.");

                    var med = medSnap.ConvertTo<Medication>();
                    if (med.CentroId != cita.CentroId)
                        throw new InvalidOperationException($"El medicamento '{med.Nombre}' no pertenece al centro de salud de esta cita.");

                    if (med.StockActual < item.Cantidad)
                        throw new InvalidOperationException($"Stock insuficiente de '{med.Nombre}'. Disponible: {med.StockActual}, solicitado: {item.Cantidad}.");

                    medDocs.Add((medRef, med, item.Cantidad));
                }

                var consultationRef = _firestore.Collection("Consultations").Document();
                var consultation = new Consultation
                {
                    Id = consultationRef.Id,
                    CitaId = dto.CitaId,
                    PacienteId = cita.PacienteId,
                    CentroId = cita.CentroId,
                    PersonalSaludId = personalId,
                    Diagnostico = dto.Diagnostico,
                    Observaciones = dto.Observaciones,
                    FechaConsulta = DateTime.UtcNow
                };
                transaction.Set(consultationRef, consultation);

                transaction.Update(citaRef, new Dictionary<string, object>
                {
                    { "estado", "atendida" },
                    { "fechaActualizacion", DateTime.UtcNow }
                });

                var dispensations = new List<Dispensation>();
                foreach (var (medRef, med, cantidad) in medDocs)
                {
                    transaction.Update(medRef, "stockActual", med.StockActual - cantidad);

                    var dispRef = _firestore.Collection("Dispensations").Document();
                    var dispensation = new Dispensation
                    {
                        Id = dispRef.Id,
                        ConsultaId = consultation.Id,
                        PacienteId = cita.PacienteId,
                        CentroId = cita.CentroId,
                        MedicamentoId = medRef.Id,
                        MedicamentoNombre = med.Nombre,
                        CantidadEntregada = cantidad,
                        DispensadoPorId = personalId,
                        FechaDispensacion = DateTime.UtcNow
                    };
                    transaction.Set(dispRef, dispensation);
                    dispensations.Add(dispensation);
                }

                return new { Consulta = consultation, Dispensaciones = dispensations };
            });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("patient/{pacienteId}/dispensations")]
    [Authorize(Roles = "Admin,PersonalSalud,Paciente")]
    public async Task<IActionResult> GetPatientDispensations(string pacienteId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        if (userRole == "Paciente" && userId != pacienteId)
            return Forbid();

        var snapshot = await _firestore.Collection("Dispensations")
            .WhereEqualTo("pacienteId", pacienteId)
            .GetSnapshotAsync();

        var list = snapshot.Documents.Select(d =>
        {
            var item = d.ConvertTo<Dispensation>();
            item.Id = d.Id;
            return item;
        }).ToList();

        return Ok(list);
    }
}