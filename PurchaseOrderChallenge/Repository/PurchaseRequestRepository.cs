using Microsoft.EntityFrameworkCore;
using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Repository.Interfaces;

namespace PurchaseOrderChallenge.Repository;

public class PurchaseRequestRepository(PurchaseOrderDbContext context) 
    : IPurchaseRequestRepository
{
    private readonly PurchaseOrderDbContext _context = context;

    public IEnumerable<PurchaseRequest> GetAll()
    {
        return _context.PurchaseRequests;
    }

    public PurchaseRequest? GetById(int id)
    {
        return _context.PurchaseRequests
            .Include(r => r.ApprovalSteps)
            .Include(r => r.History)
            .Include(r => r.Items)
            .FirstOrDefault(x => x.Id == id);
    }

    public PurchaseRequest Insert(PurchaseRequest request)
    {
        var purchaseRequestAdded = _context.PurchaseRequests.Add(request);
        _context.SaveChanges();

        return purchaseRequestAdded.Entity;
    }

    public PurchaseRequest Update(PurchaseRequest request)
    {
        _context.PurchaseRequests.Update(request);
        _context.SaveChanges();
        return request;
    }
}
