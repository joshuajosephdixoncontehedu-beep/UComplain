namespace CommunityIncidentReporting.Application.Common.Models;

/// <summary>
/// Framework-agnostic stand-in for ASP.NET Core's IFormFile — the Application layer has
/// no dependency on ASP.NET Core, so the Api layer maps IFormFile to this at the
/// controller boundary. Content is the raw (not-yet-validated) upload stream; the
/// service is responsible for content-type sniffing before trusting ContentType/FileName.
/// </summary>
public record MediaUploadFile(string FileName, string ContentType, long Length, Stream Content);
