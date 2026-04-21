using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Repository.Interfaces;

namespace PurchaseOrderChallenge.Repository
{
    public class PurchaseRequestHistoryRepository(PurchaseOrderDbContext context) 
        : IPurchaseRequestHistoryRepository
    {
        private readonly PurchaseOrderDbContext _context = context;

        public void Insert(PurchaseRequestHistory history)
        {
            _context.PurchaseRequestHistories.Add(history);
            _context.SaveChanges();
        }
    }
}
