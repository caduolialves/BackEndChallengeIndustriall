using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;
using PurchaseOrderChallenge.Repository.Interfaces;

namespace PurchaseOrderChallenge.Repository
{
    public class ApprovalStepsRepository(PurchaseOrderDbContext context) : IApprovalStepsRepository
    {
        private readonly PurchaseOrderDbContext _context = context;

        /// <summary>
        /// Retorna todas as etapas de um pedido que possuem o status informado,
        /// ordenadas pela sequência do fluxo de aprovação.
        /// </summary>
        public IEnumerable<ApprovalStep> GetAllByStatus(int id, ApprovalStepStatus status)
        {
            return _context.ApprovalSteps
                .Where(step => step.PurchaseRequestId == id && step.Status == status)
                .OrderBy(step => step.Sequence);
        }

        /// <summary>
        /// Retorna a primeira etapa de um pedido que possui o status informado.
        /// Usado para identificar a etapa atual do fluxo sequencial.
        /// </summary>
        public ApprovalStep? GetByStatus(int id, ApprovalStepStatus status)
        {
            return GetAllByStatus(id, status).FirstOrDefault();
        }
    }
}
