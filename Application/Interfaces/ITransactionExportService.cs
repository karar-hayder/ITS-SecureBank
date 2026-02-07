using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ITransactionExportService
    {
        Task<byte[]> ExportTransactionsCsvAsync(int userId, DateTime? from = null, DateTime? to = null);
    }
}
