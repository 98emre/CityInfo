using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace CityInfo.Controllers
{
    [Route("api/v{version:apiVersion}/files")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;

        public FilesController(FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
        {
            _fileExtensionContentTypeProvider = fileExtensionContentTypeProvider ?? 
                throw new System.ArgumentNullException(nameof(fileExtensionContentTypeProvider));
        }

        [HttpGet("{fieldId}")]
        [ApiVersion(0.1, Deprecated = true)]
        public ActionResult GetFile(string fieldId)
        {
            var pathToFile = "getting-started-with-rest-slides.pdf";

            if (!System.IO.File.Exists(pathToFile))
            {
                return NotFound();
            }

            if(!_fileExtensionContentTypeProvider.TryGetContentType(fieldId, out var contentType))
            {
                contentType = "application/pdf";
            }

            var bytes = System.IO.File.ReadAllBytes(pathToFile);

            return File(bytes, contentType, Path.GetFileName(pathToFile)); 
        }

        [HttpPost]
        public async Task<IActionResult> CreateFile(IFormFile file)
        {
            if (file.Length == 0 || file.Length > 20971520 || file.ContentType != "application/pdf")
            {
                return BadRequest("No file or an invalid one has ben inputted");
            }

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                $"uploaded_file_{Guid.NewGuid()}.pdf");

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
             
            return Ok("Your file has been uploaded successfully");
        }
    }
}
