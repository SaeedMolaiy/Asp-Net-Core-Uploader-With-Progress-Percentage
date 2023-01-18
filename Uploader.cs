using Microsoft.AspNetCore.Http;

namespace FileUploader
{
    public class Uploader
    {
        private static int _completedPercentOfUploadProgress;
        private bool _isInProgress;

        private readonly string _uploadPath;
        private readonly string _fileName;
        private readonly bool _generateGuidName;

        public Uploader(string uploadPath, string fileName, bool generateGuidName = false)
        {
            _uploadPath = uploadPath;
            _fileName = EnsureTheFileNameIsCorrect(fileName);
            _generateGuidName = generateGuidName;
        }

        public async Task<bool> UploadMultipleFile(List<IFormFile> formFiles)
        {
            try
            {
                if (_isInProgress)
                {
                    return false;
                }

                var totalBytes = formFiles.Sum(f => f.Length);

                foreach (var formFile in formFiles)
                {
                    _isInProgress = true;

                    var buffer = new byte[16 * 1024];

                    await using var fileStream = File.Create(GetUploadPath() + Path.GetExtension(formFile.FileName));
                    await using var stream = formFile.OpenReadStream();

                    long totalReadBytes = 0;
                    int readBytes;

                    while ((readBytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, readBytes);

                        totalReadBytes += readBytes;

                        _completedPercentOfUploadProgress = (int)(totalReadBytes / (float)totalBytes * 100.0);
                    }
                }

                _isInProgress = false;
                _completedPercentOfUploadProgress = 0;

                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        public async Task<bool> UploadSingleFile(IFormFile formFile)
        {
            try
            {
                if (_isInProgress)
                {
                    return false;
                }

                var bytes = formFile.Length;

                var buffer = new byte[16 * 1024];

                _isInProgress = true;

                await using var fileStream = File.Create(GetUploadPath() + Path.GetExtension(formFile.FileName));
                await using var stream = formFile.OpenReadStream();

                long totalReadBytes = 0;
                int readBytes;

                while ((readBytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, readBytes);

                    totalReadBytes += readBytes;

                    _completedPercentOfUploadProgress = (int)(totalReadBytes / (float)bytes * 100.0);
                }

                _isInProgress = false;
                _completedPercentOfUploadProgress = 0;

                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        public int CompletedPercentOfUploadProgress()
        {
            return _completedPercentOfUploadProgress;
        }

        public bool IsInProgress()
        {
            return _isInProgress;
        }

        private static string EnsureTheFileNameIsCorrect(string filename)
        {
            if (filename.Contains("\\"))
            {
                filename = filename[(filename.LastIndexOf("\\", StringComparison.Ordinal) + 1)..];
            }

            return filename;
        }

        private string GetUploadPath()
        {
            string path;

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            if (_generateGuidName)
            {
                path = _uploadPath + Guid.NewGuid();
            }
            else
            {
                path = _uploadPath + _fileName;
            }

            return path;
        }

    }
}
