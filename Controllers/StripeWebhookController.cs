using Agroskalka.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Agroskalka.Controllers;

[Route("api/webhooks/stripe")]
[ApiController]
public class StripeWebhookController : ControllerBase
{
    private readonly AppDbContext _context;

    public StripeWebhookController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret,
                throwOnApiVersionMismatch: false
            );

            // Zdarzenie wywoływane wyłącznie po poprawnym opłaceniu sesji
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && session.PaymentStatus == "paid")
                {
                    if (session.Metadata.TryGetValue("ticket_id", out var ticketIdStr) &&
                        int.TryParse(ticketIdStr, out int ticketId))
                    {
                        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
                        if (ticket != null)
                        {
                            ticket.IsPaid = true;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            Console.WriteLine($"[STRIPE WEBHOOK ERROR]: {e.Message}");
            return BadRequest(new { error = e.Message });
        }
    }
}