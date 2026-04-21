using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Repository.Interfaces;

namespace PurchaseOrderChallenge.Repository
{
    public class PurchaseRequestHistoryRepository : IPurchaseRequestHistoryRepository
    {
        private readonly PurchaseOrderDbContext _context;

        public PurchaseRequestHistoryRepository(PurchaseOrderDbContext context)
        {
            _context = context;
        }
        public void Insert(PurchaseRequestHistory history)
        {
            _context.PurchaseRequestHistories.Add(history);
            _context.SaveChanges();
        }
    }
}
