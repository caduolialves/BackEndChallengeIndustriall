using Microsoft.EntityFrameworkCore;
using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;

namespace PurchaseOrderChallenge.Repository;


public class PurchaseRequestRepository
{
    private readonly PurchaseOrderDbContext _context;

    public PurchaseRequestRepository(PurchaseOrderDbContext context)
    {
        _context = context;
    }

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

    public void Insert(PurchaseRequest request)
    {
        _context.PurchaseRequests.Add(request);
        _context.SaveChanges();
    }

    public PurchaseRequest Update(PurchaseRequest request)
    {
        _context.PurchaseRequests.Update(request);
        _context.SaveChanges();
        return request;
    }
}