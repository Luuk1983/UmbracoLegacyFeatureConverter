using AutoBlockList.Models;
using AutoBlockList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Controllers;

namespace AutoBlockList.Controllers;

/// <summary>
/// API controller for the new converter-based functionality.
/// Provides endpoints for converter discovery, document type selection, and execution.
/// </summary>
[IsBackOffice]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ConverterApiController(
    IConverterService converterService,
    IConversionHistoryService historyService,
    ILogger<ConverterApiController> logger) : UmbracoApiController
{
    /// <summary>
    /// Gets all available converters with metadata.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConverters()
    {
        try
        {
            var metadata = await converterService.GetConverterMetadataAsync();
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting converters");
            return StatusCode(500, new { error = "Failed to get converters", details = ex.Message });
        }
    }

            /// <summary>
            /// Gets document types affected by a specific converter.
            /// </summary>
            [HttpGet]
            public async Task<IActionResult> GetDocumentTypes(string converterName)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(converterName))
                    {
                        return BadRequest(new { error = "Converter name is required" });
                    }

                    var documentTypes = await converterService.GetAffectedDocumentTypesAsync(converterName);
                    return Ok(documentTypes);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting document types for converter {ConverterName}", converterName);
                    return StatusCode(500, new { error = "Failed to get document types", details = ex.Message });
                }
            }

            /// <summary>
            /// Executes a conversion with the specified options.
            /// </summary>
            [HttpPost]
            public async Task<IActionResult> ExecuteConversion([FromBody] ConversionRequest request)
            {
                try
                {
                    if (request == null)
                    {
                        return BadRequest(new { error = "Request body is required" });
                    }

                    if (string.IsNullOrWhiteSpace(request.ConverterType))
                    {
                        return BadRequest(new { error = "Converter type is required" });
                    }

                    var converter = converterService.GetConverterByName(request.ConverterType);
                    if (converter == null)
                    {
                        return NotFound(new { error = $"Converter '{request.ConverterType}' not found" });
                    }

                    var options = new ConversionOptions
                    {
                        ConverterType = request.ConverterType,
                        SelectedDocumentTypeKeys = request.SelectedDocumentTypeKeys,
                        IsTestRun = request.IsTestRun,
                        PerformingUserKey = request.PerformingUserKey ?? Guid.Empty // TODO: Get from current user
                    };

                    logger.LogInformation("Starting conversion: {ConverterType}, TestRun: {IsTestRun}, DocumentTypes: {Count}",
                        options.ConverterType, options.IsTestRun, options.SelectedDocumentTypeKeys?.Length ?? 0);

                    var result = await converter.ExecuteConversionAsync(options);

                    logger.LogInformation("Conversion completed: {ConversionId}, Status: {Status}, Success: {SuccessCount}, Failed: {FailureCount}",
                        result.ConversionId, result.Status, result.SuccessCount, result.FailureCount);

                    return Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing conversion");
                    return StatusCode(500, new { error = "Conversion failed", details = ex.Message });
                }
            }

            /// <summary>
            /// Gets conversion history with pagination.
            /// </summary>
            [HttpGet]
            public async Task<IActionResult> GetHistory(int page = 1, int pageSize = 20)
            {
                try
                {
                    if (page < 1) page = 1;
                    if (pageSize < 1 || pageSize > 100) pageSize = 20;

                    var history = await historyService.GetHistoryListAsync(page, pageSize);
                    return Ok(history);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting conversion history");
                    return StatusCode(500, new { error = "Failed to get history", details = ex.Message });
                }
            }

            /// <summary>
            /// Gets detailed information about a specific conversion.
            /// </summary>
            [HttpGet]
            public async Task<IActionResult> GetConversionDetails(Guid id)
            {
                try
                {
                    var history = await historyService.GetHistoryAsync(id);
                    if (history == null)
                    {
                        return NotFound(new { error = "Conversion not found" });
                    }

                    var logs = await historyService.GetLogEntriesAsync(id);

                    return Ok(new
                    {
                        history,
                        logs = logs.OrderBy(l => l.Timestamp)
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting conversion details for {ConversionId}", id);
                    return StatusCode(500, new { error = "Failed to get conversion details", details = ex.Message });
                }
            }
        }

        /// <summary>
        /// Request model for executing a conversion.
        /// </summary>
        public class ConversionRequest
        {
            public string ConverterType { get; set; } = string.Empty;
            public Guid[]? SelectedDocumentTypeKeys { get; set; }
            public bool IsTestRun { get; set; }
            public Guid? PerformingUserKey { get; set; }
        }
