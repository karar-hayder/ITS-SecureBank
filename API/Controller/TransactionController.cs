using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionExportService _transactionExportService;

        public TransactionController(ITransactionExportService transactionExportService)
        {
            _transactionExportService = transactionExportService;
        }

        [HttpGet("download-transactions")]
        [Authorize]
        public async Task<IActionResult> DownloadTransactions([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var csvBytes = await _transactionExportService.ExportTransactionsCsvAsync(userId, from, to);
            var fileName = $"transactions_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            return File(csvBytes, "text/csv", fileName);
        }
    }
}
