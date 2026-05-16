 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ecommerce_Project.Data;
using Ecommerce_Project.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Ecommerce_Project.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        // GET: Orders
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Index(string? userId)
        {
            // Start with all orders and include the User data for the names/emails
            var query = _context.orders
                .Include(o => o.User)
                .AsQueryable();

            // FEATURE: View orders by specific user
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
                var targetUser = await _context.Users.FindAsync(userId);
                ViewData["FilterTitle"] = $"Orders for {targetUser?.UserName}";
            }
            else
            {
                ViewData["FilterTitle"] = "All Orders Entries";
            }

            return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> UpdateStatus([FromBody] OrderStatusUpdate data)
        {
            if (data == null) return BadRequest();

            var order = await _context.orders.FindAsync(data.Id);
            if (order == null) return NotFound();

            order.Status = data.Status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Recalculate totals for the dashboard cards
            var allOrders = await _context.orders.ToListAsync();
            var stats = new
            {
                success = true,
                totalValue = allOrders.Sum(o => o.TotalAmount).ToString("N0"),
                completedCount = allOrders.Count(o => o.Status == "Completed"),
                processingCount = allOrders.Count(o => o.Status == "Processing"),
                cancelledCount = allOrders.Count(o => o.Status == "Cancelled")
            };

            return Ok(stats);
        }
        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.orders
                .Include(o => o.User)
                .Include(o=> o.OrderItems)
                      .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,OrderDate,TotalAmount,Status,ShippingAddress,UserId")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            order.Status = "Processing"; // Default status for new orders
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", order.UserId);
            return View(order);
        }
            
        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", order.UserId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderDate,TotalAmount,Status,ShippingAddress,UserId")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", order.UserId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.orders.FindAsync(id);
            if (order != null)
            {
                _context.orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.orders.Any(e => e.Id == id);
        }



        public async Task<IActionResult> MyOrders() { 
        
        
        var userId=User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.orders.Where(o => o.UserId == userId).ToListAsync();

            return View (orders);
        
        
        }


        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }





        public async Task<IActionResult> Checkout()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).FirstOrDefaultAsync(c => c.UserId == userId);


            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())


            {

                return RedirectToAction("MyCart", "CartItems");

            }
            decimal total = 0;
            foreach (var item in cart.CartItems)
            {
                total += item.Quantity * item.Product.Price;
            }

            Order order = new Order
            {

                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Processing",
                ShippingAddress = "Amman",
                TotalAmount = total

            };

            _context.orders.Add(order);

            await _context.SaveChangesAsync();

            foreach (var item in cart.CartItems)
            {
                OrderItem orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                };

                _context.orderItems.Add(orderItem);
            }

            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            return RedirectToAction("Payment", new { id = order.Id });
        }


        public async Task<IActionResult> Payment(int id)
        {


            var order = await _context.orders.FindAsync(id);

            return View(order);



        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            Payment payment = new Payment
            {
                OrderId = orderId,
                PaymentMethod = "Visa",
                PaymentStatus = "Paid",
                PaymentDate = DateTime.Now
            };

            _context.payments.Add(payment);

            order.Status = "Completed";

            await _context.SaveChangesAsync();

            return RedirectToAction("MyOrders");
        }

    }

    public class OrderStatusUpdate
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }
}
