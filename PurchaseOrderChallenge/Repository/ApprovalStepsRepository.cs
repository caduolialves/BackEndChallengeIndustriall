using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Repository
{
    public class ApprovalStepsRepository
    {
        private readonly PurchaseOrderDbContext _context;

        public ApprovalStepsRepository(PurchaseOrderDbContext context)
        {
            _context = context;
        }
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