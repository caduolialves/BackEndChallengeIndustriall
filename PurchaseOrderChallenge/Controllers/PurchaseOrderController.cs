using Microsoft.AspNetCore.Mvc;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Service;
using PurchaseOrderChallenge.Models.DTOs;

namespace PurchaseOrderChallenge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrderController(
        PurchaseOrderService purchaseOrderService
    ) : ControllerBase
    {
        private readonly PurchaseOrderService _purchaseOrderService = purchaseOrderService;

        [HttpGet]
        public ActionResult<PurchaseRequest> Get()
        {
            var _orders = _purchaseOrderService.GetAllPurchaseRequests();
            return Ok(_orders);
        }

        [HttpGet("{id}")]
        public ActionResult<PurchaseRequest> GetById(int id)
        {
            var order = _purchaseOrderService.GetPurchaseRequestsById(id);

            if (order is null)
                return NotFound();

            return Ok(order);
        }

        [HttpPost]
        public ActionResult<PurchaseRequest> Post(PurchaseRequest request)
        {
            /// RN1: Validação de pelo menos um item no pedido, caso contrário retorna BadRequest.
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest("O pedido precisa ter ao menos um item.");
            }
            _purchaseOrderService.CreatePurchaseRequest(request);
            
            return CreatedAtAction(
                nameof(GetById),
                new { id = request.Id },
                request
            );
        }

        [HttpPut("{id}/approve")]
        public ActionResult<PurchaseRequest> Approve(int id, PurchaseRequestActionRequest approval)
        {
            try
            {
                var order = _purchaseOrderService.ApprovePurchaseRequest(id, approval);
                return Ok(order);
            }
            catch (InvalidOperationException ex) when (ex.Message == "Pedido não encontrado.")
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/review")]
        public ActionResult<PurchaseRequest> Review(int id, PurchaseRequestActionRequest review)
        {
            try
            {
                var order = _purchaseOrderService.ReviewPurchaseRequest(id, review);
                return Ok(order);
            }
            catch (InvalidOperationException ex) when (ex.Message == "Pedido não encontrado.")
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        
    }
}
