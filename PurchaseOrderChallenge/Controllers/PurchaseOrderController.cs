using Microsoft.AspNetCore.Mvc;
using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.DTOs;
using PurchaseOrderChallenge.Service.Interfaces;

namespace PurchaseOrderChallenge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrderController(
        IPurchaseOrderService purchaseOrderService
    ) : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService = purchaseOrderService;

        /// <summary>
        /// Lista todos os pedidos de compra cadastrados.
        /// </summary>
        [HttpGet]
        public ActionResult<PurchaseRequest> Get()
        {
            var orders = _purchaseOrderService.GetAllPurchaseRequests();
            return Ok(orders);
        }

        /// <summary>
        /// Busca um pedido de compra pelo identificador informado na rota.
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<PurchaseRequest> GetById(int id)
        {
            var order = _purchaseOrderService.GetPurchaseRequestsById(id);

            if (order is null)
                return NotFound();

            return Ok(order);
        }

        /// <summary>
        /// Cria um novo pedido de compra.
        /// RN1: valida que o pedido possui pelo menos um item antes de enviar para o serviço.
        /// </summary>
        [HttpPost]
        public ActionResult<PurchaseRequest> Post(PurchaseRequest request)
        {
            // RN1: um pedido de compra deve conter pelo menos um item.
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

        /// <summary>
        /// Aprova a etapa atual do pedido.
        /// RN4 e RN7 são validadas no serviço.
        /// </summary>
        [HttpPut("{id}/approve")]
        public ActionResult<PurchaseRequest> Approve(
            int id,
            PurchaseRequestActionRequest approval)
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

        /// <summary>
        /// Solicita revisão do pedido na etapa atual.
        /// RN5 e RN6 são aplicadas no serviço.
        /// </summary>
        [HttpPut("{id}/review")]
        public ActionResult<PurchaseRequest> Review(
            int id,
            PurchaseRequestActionRequest review)
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

        /// <summary>
        /// Reenvia um pedido que estava em revisão para reiniciar o fluxo de aprovação.
        /// RN5 e RN6 são aplicadas no serviço.
        /// </summary>
        [HttpPut("{id}/resubmit")]
        public ActionResult<PurchaseRequest> Resubmit(
            int id,
            PurchaseRequest resubmit)
        {
            try
            {
                var order = _purchaseOrderService.ResubmitPurchaseRequest(id, resubmit);
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

        /// <summary>
        /// Cancela um pedido de compra.
        /// RN8 e RN6 são aplicadas no serviço.
        /// </summary>
        [HttpPut("{id}/cancel")]
        public ActionResult<PurchaseRequest> Cancel(
            int id,
            PurchaseRequestActionRequest cancellation)
        {
            try
            {
                var order = _purchaseOrderService.CancelPurchaseRequest(id, cancellation);
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
