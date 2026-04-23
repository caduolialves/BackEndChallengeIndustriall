using Microsoft.EntityFrameworkCore;
using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Repository.Interfaces;

namespace PurchaseOrderChallenge.Repository;

public class PurchaseRequestRepository(PurchaseOrderDbContext context) 
    : IPurchaseRequestRepository
{
    private readonly PurchaseOrderDbContext _context = context;

    /// <summary>
    /// Retorna os pedidos cadastrados no banco.
    /// </summary>
    public IEnumerable<PurchaseRequest> GetAll()
    {
        return _context.PurchaseRequests;
    }

    /// <summary>
    /// Busca um pedido pelo Id e carrega itens, etapas de aprovação e histórico.
    /// </summary>
    public PurchaseRequest? GetById(int id)
    {
        return _context.PurchaseRequests
            .Include(r => r.ApprovalSteps)
            .Include(r => r.History)
            .Include(r => r.Items)
            .FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    /// Persiste um novo pedido de compra no banco.
    /// </summary>
    public PurchaseRequest Insert(PurchaseRequest request)
    {
        var purchaseRequestAdded = _context.PurchaseRequests.Add(request);
        _context.SaveChanges();

        return purchaseRequestAdded.Entity;
    }

    /// <summary>
    /// Atualiza um pedido de compra existente no banco.
    /// </summary>
    public PurchaseRequest Update(PurchaseRequest request)
    {
        _context.PurchaseRequests.Update(request);
        _context.SaveChanges();
        return request;
    }
}
