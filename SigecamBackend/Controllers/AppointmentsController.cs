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
public class AppointmentsController : ControllerBase
{
    private readonly FirestoreDb _firestore;

    public AppointmentsController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    // 1. Solicitar una cita (Solo Paciente, usando Transacción de Firestore)
    [HttpPost]
    [Authorize(Roles = "Paciente")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var pacienteId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var pacienteNombre = User.FindFirstValue(ClaimTypes.Name) ?? "Paciente";

        // Validar que el paciente no tenga ya una cita activa para el mismo servicio
        var activeQuery = await _firestore.Collection("Appointments")
            .WhereEqualTo("pacienteId", pacienteId)
            .WhereEqualTo("servicio", dto.Servicio)
            .WhereEqualTo("estado", "programada")
            .GetSnapshotAsync();

        if (activeQuery.Count > 0)
        {
            return BadRequest(new
            {
                mensaje = $"Ya tienes una cita activa programada para {dto.Servicio}.",
                citaExistenteId = activeQuery.Documents[0].Id
            });
        }

        var servicioNormalizado = dto.Servicio.Trim().ToLower().Replace(" ", "_");
        var slotId = $"{dto.CentroId}_{servicioNormalizado}_{dto.Fecha}";
        var slotRef = _firestore.Collection("Slots").Document(slotId);
        var appointmentsRef = _firestore.Collection("Appointments");

        try
        {
            var createdAppointment = await _firestore.RunTransactionAsync(async transaction =>
            {
                var slotSnap = await transaction.GetSnapshotAsync(slotRef);
                if (!slotSnap.Exists)
                {
                    throw new InvalidOperationException("No hay cupos habilitados para este centro, servicio y fecha.");
                }

                var slot = slotSnap.ConvertTo<DailySlot>();
                if (slot.CupoOcupado >= slot.CupoTotal)
                {
                    throw new InvalidOperationException("El cupo del día ya se agotó. Por favor selecciona otra fecha.");
                }

                // Incrementar cupo ocupado de forma atómica
                transaction.Update(slotRef, "cupoOcupado", slot.CupoOcupado + 1);

                var newDocRef = appointmentsRef.Document();
                var appointment = new Appointment
                {
                    Id = newDocRef.Id,
                    PacienteId = pacienteId,
                    PacienteNombre = pacienteNombre,
                    CentroId = dto.CentroId,
                    Servicio = dto.Servicio,
                    Fecha = dto.Fecha,
                    Estado = "programada",
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };

                transaction.Set(newDocRef, appointment);
                return appointment;
            });

            return Ok(createdAppointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // 2. Ver mis citas (Paciente solo ve su propio historial)
    [HttpGet("my-appointments")]
    [Authorize(Roles = "Paciente")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var pacienteId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var snapshot = await _firestore.Collection("Appointments")
            .WhereEqualTo("pacienteId", pacienteId)
            .GetSnapshotAsync();

        var list = snapshot.Documents.Select(d =>
        {
            var item = d.ConvertTo<Appointment>();
            item.Id = d.Id;
            return item;
        }).ToList();

        return Ok(list);
    }

    // 3. Cancelar cita y liberar cupo automáticamente (Paciente o PersonalSalud)
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelAppointment(string id, [FromBody] CancelAppointmentDto? dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var appointmentRef = _firestore.Collection("Appointments").Document(id);

        try
        {
            await _firestore.RunTransactionAsync(async transaction =>
            {
                var appSnap = await transaction.GetSnapshotAsync(appointmentRef);
                if (!appSnap.Exists)
                    throw new InvalidOperationException("La cita no existe.");

                var appointment = appSnap.ConvertTo<Appointment>();

                if (userRole == "Paciente" && appointment.PacienteId != userId)
                    throw new UnauthorizedAccessException("No tienes permiso para cancelar esta cita.");

                if (appointment.Estado != "programada")
                    throw new InvalidOperationException($"No se puede cancelar una cita en estado '{appointment.Estado}'.");

                var servicioNormalizado = appointment.Servicio.Trim().ToLower().Replace(" ", "_");
                var slotId = $"{appointment.CentroId}_{servicioNormalizado}_{appointment.Fecha}";
                var slotRef = _firestore.Collection("Slots").Document(slotId);
                var slotSnap = await transaction.GetSnapshotAsync(slotRef);

                if (slotSnap.Exists)
                {
                    var slot = slotSnap.ConvertTo<DailySlot>();
                    if (slot.CupoOcupado > 0)
                    {
                        transaction.Update(slotRef, "cupoOcupado", slot.CupoOcupado - 1);
                    }
                }

                transaction.Update(appointmentRef, new Dictionary<string, object>
                {
                    { "estado", "cancelada" },
                    { "justificacion", dto?.Justificacion ?? "Cancelada por el usuario" },
                    { "fechaActualizacion", DateTime.UtcNow }
                });
            });

            return Ok(new { mensaje = "Cita cancelada y cupo liberado correctamente." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}