using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Repository.Interfaces;

public interface IApprovalStepsRepository
{
    IEnumerable<ApprovalStep> GetAllByStatus(int id, ApprovalStepStatus status);
    ApprovalStep? GetByStatus(int id, ApprovalStepStatus status);
}
