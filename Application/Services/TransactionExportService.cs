using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TransactionExportService : ITransactionExportService
    {
        private readonly IBankDbContext _context;

        public TransactionExportService(IBankDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportTransactionsCsvAsync(int userId, DateTime? from = null, DateTime? to = null)
        {
            var userAccountIds = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => a.Id)
                .ToListAsync();

            var query = _context.Transactions
                .Where(t => userAccountIds.Contains(t.AccountId));

            if (from.HasValue)
            {
                // Ensure from date is UTC if needed, or just compare as is
                query = query.Where(t => t.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= to.Value);
            }

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Amount,Type,Date");

            foreach (var t in transactions)
            {
                var dateStr = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                csv.AppendLine($"{t.Id},{t.Amount},{t.Type},{dateStr}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}
