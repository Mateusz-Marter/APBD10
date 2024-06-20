using Microsoft.AspNetCore.Mvc;
using APBD_10.Services;

namespace APBD_10.Controllers;

[ApiController]
[Route("api/prescriptions")]
public class PrescriptionController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;
    
    public PrescriptionController(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPrescription(int idPrescription)
    {
        if (!await _prescriptionService.PrescriptionExists(idPrescription))
            return NotFound("Prescription not exists or Prescription Medicament not exists");

        var data = await _prescriptionService.GetPrescription(idPrescription);
        return Ok(data);
    }
   
}