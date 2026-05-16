using Ecommerce_Project.Data;
using Ecommerce_Project.Models; // Ensure you include your models namespace
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String & DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. IMPORTANT: Change IdentityUser to ApplicationUser and add Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false; // Set to false during development for easier testing
})
    .AddRoles<IdentityRole>() // You MUST add this line to support Admin roles
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Ensure Authentication is before Authorization
app.UseAuthorization();

// ==========================================
// ADDED: Area Route Configuration
// ==========================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

// Existing default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 3. Role & Admin Seeding Logic
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Create Admin Role
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    var adminEmail = "halqannas04@gmail.com";
    var user = await userManager.FindByEmailAsync(adminEmail);

    if (user == null)
    {
        // CREATE the user if they don't exist
        var newAdmin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Admin User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(newAdmin, "_Heba123456"); // Use a strong password!
        await userManager.AddToRoleAsync(newAdmin, "Admin");
    }
    else if (!(await userManager.IsInRoleAsync(user, "Admin")))
    {
        await userManager.AddToRoleAsync(user, "Admin");
    }

    // ── BALANCED TEST SEEDING SCRIPT ────────────────────────────
    using (var scop = app.Services.CreateScope())
    {
        var context = scop.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var testProd = context.Products.FirstOrDefault();
        var testUser = context.Users.FirstOrDefault();

        if (testProd != null && testUser != null && !context.productComments.Any())
        {
            // 1. Plant separate rating records
            var sampleRating1 = new ProductRating { ProductId = testProd.Id, UserId = testUser.Id, RatingValue = 5, IsApproved = true };
            var sampleRating2 = new ProductRating { ProductId = testProd.Id, UserId = testUser.Id, RatingValue = 2, IsApproved = true };

            context.productRatings.AddRange(sampleRating1, sampleRating2);
            context.SaveChanges();

            // 2. Plant matching text comments linked via ProductId & UserId
            var sampleComments = new List<ProductComment>
        {
            new ProductComment
            {
                Comment = "Stunning piece! The weight balances excellently and the emerald theme fits perfectly inside my collection.",
                IsApproved = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now.AddDays(-1),
                ProductId = testProd.Id,
                UserId = testUser.Id
            },
            new ProductComment
            {
                Comment = "The box arrived dented and scratched up. The item inside is okay but the logistics pipeline needs adjustments.",
                IsApproved = false,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                ProductId = testProd.Id,
                UserId = testUser.Id
            }
        };

            context.productComments.AddRange(sampleComments);
            context.SaveChanges();
        }

        // ── TEMPORARY SEED DATA FOR LOGGED TESTIMONIAL VAULT ───────────
        using (var sco = app.Services.CreateScope())
        {
            var contex = sco.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sampleUser = context.Users.FirstOrDefault();

            if (sampleUser != null && !context.testimonials.Any())
            {
                var mockTestimonials = new List<Testimonial>
        {
            new Testimonial
            {
                Message = "The user interface of LUXORA is exceptional. The transaction pipeline runs flawlessly and matches top-tier luxury expectations.",
                IsApproved = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now.AddDays(-5),
                UserId = sampleUser.Id
            },
            new Testimonial
            {
                Message = "Ordering process execution is completely seamless. The customer service support is premium tier and handled everything beautifully.",
                IsApproved = false,
                IsDeleted = false,
                CreatedAt = DateTime.Now.AddDays(-2),
                UserId = sampleUser.Id
            },
            new Testimonial
            {
                Message = "Cross-border coordination was immaculate. The certified authentication logs give absolute confidence in every single purchase.",
                IsApproved = false,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UserId = sampleUser.Id
            }
        };

                context.testimonials.AddRange(mockTestimonials);
                context.SaveChanges();
            }
        }
        // ────────────────────────────────────────────────────────────

        using (var scoe = app.Services.CreateScope())
        {
            var conext = scoe.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var luxuryProd = context.Products.FirstOrDefault();

            if (luxuryProd != null && !context.Discount.Any())
            {
                var mockCampaigns = new List<Discount>
        {
            new Discount
            {
                Title = "Ramadan Elite Sale",
                Percentage = 15,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = true,
                MinmumAmount = 500,
                ProductId = luxuryProd.Id
            },
            new Discount
            {
                Title = "End of Season Clearance",
                Percentage = 30,
                StartDate = DateTime.Now.AddDays(-15),
                EndDate = DateTime.Now.AddDays(-1),
                IsActive = false,
                MinmumAmount = 0,
                ProductId = luxuryProd.Id
            }
        };

                context.Discount.AddRange(mockCampaigns);
                context.SaveChanges();
            }
        }
    }
}

app.Run();