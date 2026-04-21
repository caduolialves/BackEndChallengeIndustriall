using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;
using PurchaseOrderChallenge.Repository.Interfaces;

namespace PurchaseOrderChallenge.Repository
{
    public class ApprovalStepsRepository(PurchaseOrderDbContext context) : IApprovalStepsRepository
    {
        private readonly PurchaseOrderDbContext _context = context;

        public IEnumerable<ApprovalStep> GetAllByStatus(int id, ApprovalStepStatus status)
        {
            return _context.ApprovalSteps
                .Where(step => step.PurchaseRequestId == id && step.Status == status)
                .OrderBy(step => step.Sequence);
        }
        public ApprovalStep? GetByStatus(int id, ApprovalStepStatus status)
        {
            return GetAllByStatus(id, status).FirstOrDefault();
        }
    }
}
