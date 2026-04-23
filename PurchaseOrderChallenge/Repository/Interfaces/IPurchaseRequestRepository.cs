using PurchaseOrderChallenge.Models;

namespace PurchaseOrderChallenge.Repository.Interfaces;

public interface IPurchaseRequestRepository
{
    /// <summary>
    /// Retorna todos os pedidos cadastrados.
    /// </summary>
    IEnumerable<PurchaseRequest> GetAll();

    /// <summary>
    /// Busca um pedido pelo Id.
    /// </summary>
    PurchaseRequest? GetById(int id);

    /// <summary>
    /// Insere um novo pedido.
    /// </summary>
    PurchaseRequest Insert(PurchaseRequest request);

    /// <summary>
    /// Atualiza um pedido existente.
    /// </summary>
    PurchaseRequest Update(PurchaseRequest request);
}
