using Ecommerce_Project.Data;
using Ecommerce_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_Project.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductCommentsController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductCommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
            
            public async Task<IActionResult> Index()
            {
                // Fetch all comments, including related user and product data
                var comments = await _context.productComments
                    .Where(c => !c.IsDeleted)
                    .Include(p => p.Product)
                    .Include(p => p.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var ratings = await _context.productRatings.ToListAsync();
                var reviewQueue = comments.Select(c => new ReviewItemViewModel
            {
                CommentId = c.Id,
                CommentText = c.Comment,
                CreatedAt = c.CreatedAt,
                IsApproved = c.IsApproved,
                CustomerName = c.User?.FullName ?? "Unknown Client",
                CustomerEmail = c.User?.Email ?? "N/A",
                ProductName = c.Product?.Name ?? "Deleted Product",

                // Find the rating matching this specific user and product combo
                RatingValue = ratings.FirstOrDefault(r => r.ProductId == c.ProductId && r.UserId == c.UserId)?.RatingValue
            }).ToList();

            return View(reviewQueue);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var comment = await _context.productComments.FindAsync(id);
            if (comment == null) return NotFound();

            // Flip the status (Approve/Reject)
            comment.IsApproved = !comment.IsApproved;

            await _context.SaveChangesAsync();
            return Json(new { success = true, approved = comment.IsApproved });
        }

        [HttpPost]
        public async Task<IActionResult> SoftDeleteComment(int id)
        {
            var comment = await _context.productComments.FindAsync(id);
            if (comment == null) return Json(new { success = false });

            comment.IsDeleted = true; // Mark as deleted but keep the data
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        [HttpPost]

        public async Task<IActionResult> RestoreComment(int id)
        {
            // Find the comment in the vault, including hidden soft-deleted entries
            var comment = await _context.productComments.FindAsync(id);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found." });
            }

            // Flip the soft-delete flag back to false
            comment.IsDeleted = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comment successfully restored to active queue." });
        }

        [HttpGet]
        public async Task<IActionResult> Archive()
        {
            // Fetch ONLY soft-deleted records from the database
            var items = await _context.productComments
                .Where(c => c.IsDeleted)
                .Include(c => c.Product)
                .Include(c => c.User)
                .ToListAsync();

            var ratings = await _context.productRatings.ToListAsync();

            var archiveQueue = items.Select(c => new ReviewItemViewModel
            {
                CommentId = c.Id,
                CommentText = c.Comment,
                ProductName = c.Product?.Name,
                CustomerName = c.User?.FullName,
                RatingValue = ratings.FirstOrDefault(r => r.ProductId == c.ProductId && r.UserId == c.UserId)?.RatingValue
            }).ToList();

            return View(archiveQueue);
        }
    }
}
