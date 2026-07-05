using Microsoft.AspNetCore.Components.Forms;
using Supabase;

namespace VMS.Web.Services;


public class SupabaseStorageService : IStorageService
{
    private readonly Client _client;
    private const string BucketName = "Attachments";

    public SupabaseStorageService(IConfiguration config)
    {
        _client = new Client(
            config["Supabase:Url"]!,
            config["Supabase:AnonKey"]!
        );

        _client.InitializeAsync().Wait();
    }

    public async Task<string> UploadAsync(IBrowserFile file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        // 1. Validate size (10MB example)
        const long maxSize = 10 * 1024 * 1024;

        if (file.Size > maxSize)
            throw new Exception("File too large (max 10MB)");

        // 2. Generate safe filename
        var extension = Path.GetExtension(file.Name);
        var fileName = $"{Guid.NewGuid()}{extension}";

        // 3. Read stream into bytes
        using var stream = file.OpenReadStream(maxSize);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        // 4. Upload to Supabase
        var bucket = _client.Storage.From(BucketName);

        await bucket.Upload(fileBytes, fileName);

        // 5. Get public URL
        var url = bucket.GetPublicUrl(fileName);

        return url;
    }
}
