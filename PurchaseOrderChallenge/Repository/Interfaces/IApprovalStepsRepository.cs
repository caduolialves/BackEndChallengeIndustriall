using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Repository.Interfaces;

public interface IApprovalStepsRepository
{
    /// <summary>
    /// Retorna etapas de aprovação de um pedido pelo status informado.
    /// </summary>
    IEnumerable<ApprovalStep> GetAllByStatus(int id, ApprovalStepStatus status);

    /// <summary>
    /// Retorna a primeira etapa de aprovação de um pedido pelo status informado.
    /// </summary>
    ApprovalStep? GetByStatus(int id, ApprovalStepStatus status);
}
