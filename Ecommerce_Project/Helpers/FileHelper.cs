namespace Ecommerce_Project.Helpers
{
    public static class FileHelper
    {
        public static async Task<string> UploadFile(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return string.Empty;

            // 1. Define the save path
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName);

            // 2. Ensure folder exists
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // 3. Generate unique filename
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            // 4. Save to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Return the database-friendly relative path
            return $"/{folderName}/{fileName}";
        }
    }
}