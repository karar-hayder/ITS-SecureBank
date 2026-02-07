using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Globalization;

namespace Application.Services
{
        //public class TransactionExportService : ITransactionExportService
        //{
        //    private readonly IBankDbContext _context;

        //    public TransactionExportService(IBankDbContext context)
        //    {
        //        _context = context;
        //    }

        //    public async Task<byte[]> ExportTransactionsCsvAsync(int userId, DateTime? from = null, DateTime? to = null)
        //    {
        //        var userAccounts = await _context.Accounts
        //            .Where(a => a.UserId == userId)
        //            .Select(a => a.Id)
        //            .ToListAsync();

        //        var query = _context.Transactions
        //            .Where(t => userAccounts.Contains(t.AccountId));

        //        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        //        if (to.HasValue) query = query.Where(t => t.Date <= to.Value);

        //        var transactions = await query.ToListAsync();

        //        var csv = new StringBuilder();
        //        csv.AppendLine("Id,Amount,Type,Date");

        //        foreach (var t in transactions)
        //            csv.AppendLine($"{t.Id},{t.Amount},{t.Type},{t.Date:yyyy-MM-dd HH:mm}");

        //        return Encoding.UTF8.GetBytes(csv.ToString());
        //    }
        //}
    }


