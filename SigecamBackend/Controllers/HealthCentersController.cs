using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigecamBackend.DTOs;
using SigecamBackend.Models;

namespace SigecamBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCentersController : ControllerBase
{
    private readonly FirestoreDb _firestore;

    public HealthCentersController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    // 1. Listar centros activos (Pacientes y personal pueden filtrar por municipio o servicio)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? municipio, [FromQuery] string? servicio)
    {
        Query query = _firestore.Collection("HealthCenters").WhereEqualTo("activo", true);

        if (!string.IsNullOrWhiteSpace(municipio))
        {
            query = query.WhereEqualTo("municipio", municipio);
        }

        if (!string.IsNullOrWhiteSpace(servicio))
        {
            query = query.WhereArrayContains("servicios", servicio);
        }

        var snapshot = await query.GetSnapshotAsync();
        var centers = snapshot.Documents.Select(doc =>
        {
            var center = doc.ConvertTo<HealthCenter>();
            center.Id = doc.Id;
            return center;
        }).ToList();

        return Ok(centers);
    }

    // 2. Registrar un nuevo centro de salud (Solo Admin)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateHealthCenterDto dto)
    {
        var center = new HealthCenter
        {
            Nombre = dto.Nombre,
            Municipio = dto.Municipio,
            Servicios = dto.Servicios,
            Horario = dto.Horario,
            Activo = true
        };

        var docRef = await _firestore.Collection("HealthCenters").AddAsync(center);
        center.Id = docRef.Id;

        return Ok(center);
    }

    // 3. Editar centro de salud (Solo Admin)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateHealthCenterDto dto)
    {
        var docRef = _firestore.Collection("HealthCenters").Document(id);
        var snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
            return NotFound(new { mensaje = "Centro de salud no encontrado." });

        await docRef.UpdateAsync(new Dictionary<string, object>
        {
            { "nombre", dto.Nombre },
            { "municipio", dto.Municipio },
            { "servicios", dto.Servicios },
            { "horario", dto.Horario }
        });

        return Ok(new { mensaje = "Centro actualizado correctamente." });
    }

    // 4. Desactivar centro sin eliminar historial (Solo Admin)
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var docRef = _firestore.Collection("HealthCenters").Document(id);
        var snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
            return NotFound(new { mensaje = "Centro de salud no encontrado." });

        await docRef.UpdateAsync("activo", false);
        return Ok(new { mensaje = "Centro desactivado correctamente; su historial permanece intacto." });
    }

    // 5. Definir o actualizar cupos diarios por centro y servicio (Solo Admin)
    [HttpPost("slots")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetDailySlot([FromBody] SetDailySlotDto dto)
    {
        var centerDoc = await _firestore.Collection("HealthCenters").Document(dto.CentroId).GetSnapshotAsync();
        if (!centerDoc.Exists)
            return NotFound(new { mensaje = "El centro de salud especificado no existe." });

        // ID compuesto único para evitar duplicados del mismo centro, servicio y fecha
        var servicioNormalizado = dto.Servicio.Trim().ToLower().Replace(" ", "_");
        var slotId = $"{dto.CentroId}_{servicioNormalizado}_{dto.Fecha}";
        var slotRef = _firestore.Collection("Slots").Document(slotId);
        var slotSnap = await slotRef.GetSnapshotAsync();

        if (slotSnap.Exists)
        {
            var existing = slotSnap.ConvertTo<DailySlot>();
            if (dto.CupoTotal < existing.CupoOcupado)
                return BadRequest(new { mensaje = $"No puedes reducir el cupo total a {dto.CupoTotal} porque ya hay {existing.CupoOcupado} citas ocupadas." });

            await slotRef.UpdateAsync("cupoTotal", dto.CupoTotal);
            existing.CupoTotal = dto.CupoTotal;
            existing.Id = slotId;
            return Ok(existing);
        }

        var newSlot = new DailySlot
        {
            Id = slotId,
            CentroId = dto.CentroId,
            Servicio = dto.Servicio,
            Fecha = dto.Fecha,
            CupoTotal = dto.CupoTotal,
            CupoOcupado = 0
        };

        await slotRef.SetAsync(newSlot);
        return Ok(newSlot);
    }

    // 6. Consultar cupos disponibles de un centro (Pacientes y Admin)
    [HttpGet("{centroId}/slots")]
    public async Task<IActionResult> GetSlots(string centroId, [FromQuery] string? fecha)
    {
        Query query = _firestore.Collection("Slots").WhereEqualTo("centroId", centroId);

        if (!string.IsNullOrWhiteSpace(fecha))
        {
            query = query.WhereEqualTo("fecha", fecha);
        }

        var snapshot = await query.GetSnapshotAsync();
        var slots = snapshot.Documents.Select(doc =>
        {
            var slot = doc.ConvertTo<DailySlot>();
            slot.Id = doc.Id;
            return new
            {
                slot.Id,
                slot.CentroId,
                slot.Servicio,
                slot.Fecha,
                slot.CupoTotal,
                slot.CupoOcupado,
                Disponibles = slot.CupoTotal - slot.CupoOcupado
            };
        }).ToList();

        return Ok(slots);
    }
}