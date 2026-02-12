using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using RAG.Domain.Interfaces;

namespace RAG.Infrastructure.Storage;

public class MinioStorageService : IDocumentStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IConfiguration configuration, ILogger<MinioStorageService> logger)
    {
        _logger = logger;

        var endpoint = configuration["MinIO:Endpoint"] ?? "minio:9000";
        var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";
        var useSSL = configuration.GetValue<bool>("MinIO:UseSSL", false);
        _bucketName = configuration["MinIO:BucketName"] ?? "documents";

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSSL)
            .Build();

        _logger.LogInformation("MinIO client initialized. Endpoint: {Endpoint}, Bucket: {Bucket}",
            endpoint, _bucketName);

        // Ensure bucket exists
        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs);

            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
                _logger.LogInformation("Created MinIO bucket: {Bucket}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring bucket exists");
        }
    }

    public async Task<string> UploadDocumentAsync(
        string documentId,
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = $"{documentId}/{fileName}";

            using var stream = new MemoryStream(content);

            var putArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(content.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putArgs, cancellationToken);

            _logger.LogInformation("Uploaded document {DocumentId} to MinIO. Size: {Size} bytes",
                documentId, content.Length);

            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {DocumentId} to MinIO", documentId);
            throw;
        }
    }

    public async Task<byte[]> DownloadDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var memoryStream = new MemoryStream();

            var getArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(documentId)
                .WithCallbackStream((stream) =>
                {
                    stream.CopyTo(memoryStream);
                });

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId} from MinIO", documentId);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(documentId);

            await _minioClient.RemoveObjectAsync(removeArgs, cancellationToken);

            _logger.LogInformation("Deleted document {DocumentId} from MinIO", documentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} from MinIO", documentId);
            return false;
        }
    }

    public async Task<bool> DocumentExistsAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(documentId);

            await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
