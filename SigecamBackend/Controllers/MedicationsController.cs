using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigecamBackend.DTOs;
using SigecamBackend.Models;

namespace SigecamBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicationsController : ControllerBase
{
    private readonly FirestoreDb _firestore;

    public MedicationsController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMedicationDto dto)
    {
        var med = new Medication
        {
            CentroId = dto.CentroId,
            Nombre = dto.Nombre,
            Presentacion = dto.Presentacion,
            StockActual = dto.StockActual,
            StockMinimo = dto.StockMinimo,
            Activo = true
        };

        var docRef = await _firestore.Collection("Medications").AddAsync(med);
        med.Id = docRef.Id;
        return Ok(med);
    }

    [HttpGet("center/{centroId}")]
    [Authorize(Roles = "Admin,PersonalSalud")]
    public async Task<IActionResult> GetByCenter(string centroId)
    {
        var snapshot = await _firestore.Collection("Medications")
            .WhereEqualTo("centroId", centroId)
            .WhereEqualTo("activo", true)
            .GetSnapshotAsync();

        var meds = snapshot.Documents.Select(d =>
        {
            var m = d.ConvertTo<Medication>();
            m.Id = d.Id;
            return new
            {
                m.Id,
                m.CentroId,
                m.Nombre,
                m.Presentacion,
                m.StockActual,
                m.StockMinimo,
                AlertaStockBajo = m.StockActual <= m.StockMinimo
            };
        }).ToList();

        return Ok(meds);
    }

    [HttpPatch("{id}/restock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restock(string id, [FromBody] RestockMedicationDto dto)
    {
        var docRef = _firestore.Collection("Medications").Document(id);

        try
        {
            var nuevoStock = await _firestore.RunTransactionAsync(async transaction =>
            {
                var snap = await transaction.GetSnapshotAsync(docRef);
                if (!snap.Exists)
                    throw new InvalidOperationException("Medicamento no encontrado.");

                var med = snap.ConvertTo<Medication>();
                var actualizado = med.StockActual + dto.CantidadAgregar;
                transaction.Update(docRef, "stockActual", actualizado);
                return actualizado;
            });

            return Ok(new { mensaje = "Inventario reabastecido.", stockActual = nuevoStock });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}