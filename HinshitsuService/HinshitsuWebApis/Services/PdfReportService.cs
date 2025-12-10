using HinshitsuWebApis.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HinshitsuWebApis.Services;

/// <summary>
/// Service for generating detailed PDF reports from analysis results
/// </summary>
public class PdfReportService
{
    private readonly ILogger<PdfReportService> _logger;
    private readonly ResultsSummaryService _summaryService;
    private readonly BlobStorageService _blobStorageService;
    private const string ReportsContainer = "analysis-reports";

    public PdfReportService(
        ILogger<PdfReportService> logger,
        ResultsSummaryService summaryService,
        BlobStorageService blobStorageService)
    {
        _logger = logger;
        _summaryService = summaryService;
        _blobStorageService = blobStorageService;
        
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a comprehensive PDF report and uploads it to blob storage
    /// </summary>
    public async Task<string> GeneratePdfReportAsync(string userId, string requestId)
    {
        _logger.LogInformation("Generating PDF report - UserId: {UserId}, RequestId: {RequestId}", userId, requestId);

        // Get full analysis results
        var analysisResults = await _summaryService.GetFullAnalysisResultsAsync(userId, requestId);

        // Generate PDF document
        var pdfBytes = CreatePdfDocument(analysisResults, requestId);

        // Upload PDF to blob storage
        var pdfUrl = await UploadPdfToBlobAsync(pdfBytes, userId, requestId);

        _logger.LogInformation("PDF report generated and uploaded successfully. URL: {PdfUrl}", pdfUrl);
        return pdfUrl;
    }

    private byte[] CreatePdfDocument(AnalysisResults results, string requestId)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, results));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Website Analysis Report").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-DD HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeContent(IContainer container, AnalysisResults results)
    {
        container.Column(column =>
        {
            // Performance Section
            if (results.Performance != null)
            {
                column.Item().Element(c => AddPerformanceSection(c, results.Performance));
                column.Item().PageBreak();
            }

            // Security Section
            if (results.Security != null)
            {
                column.Item().Element(c => AddSecuritySection(c, results.Security));
                column.Item().PageBreak();
            }

            // UI/UX Section
            if (results.UiUx != null)
            {
                column.Item().Element(c => AddUiUxSection(c, results.UiUx));
            }
        });
    }

    private void AddPerformanceSection(IContainer container, PerformanceAnalysis performance)
    {
        container.Column(column =>
        {
            // Section Header with PerfoBot
            column.Item().Row(row =>
            {
                // Avatar image
                row.ConstantItem(50).Height(50).Image("avatars/perfobot.png");
                row.ConstantItem(10); // Spacing
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("⚡ Performance Analysis").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Analyzed by PerfoBot").FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingVertical(10);

            // Lighthouse Scores
            if (performance.Meta?.LighthouseScore != null)
            {
                column.Item().Text("Lighthouse Scores").FontSize(14).Bold();
                column.Item().PaddingVertical(5);
                
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => AddScoreCard(c, "Performance", performance.Meta.LighthouseScore.Performance));
                    row.RelativeItem().Element(c => AddScoreCard(c, "Accessibility", performance.Meta.LighthouseScore.Accessibility));
                    row.RelativeItem().Element(c => AddScoreCard(c, "SEO", performance.Meta.LighthouseScore.Seo));
                    row.RelativeItem().Element(c => AddScoreCard(c, "Best Practices", performance.Meta.LighthouseScore.BestPractices));
                });
            }

            column.Item().PaddingVertical(10);

            // Performance Details
            if (performance.Analysis?.Performance != null)
            {
                column.Item().Element(c => AddCategoryDetails(c, "Performance", performance.Analysis.Performance));
            }

            if (performance.Analysis?.Accessibility != null)
            {
                column.Item().Element(c => AddCategoryDetails(c, "Accessibility", performance.Analysis.Accessibility));
            }

            if (performance.Analysis?.Seo != null)
            {
                column.Item().Element(c => AddCategoryDetails(c, "SEO", performance.Analysis.Seo));
            }

            if (performance.Analysis?.BestPractices != null)
            {
                column.Item().Element(c => AddCategoryDetails(c, "Best Practices", performance.Analysis.BestPractices));
            }

            if (performance.Analysis?.Diagnostics != null)
            {
                column.Item().Element(c => AddCategoryDetails(c, "Diagnostics", performance.Analysis.Diagnostics));
            }
        });
    }

    private void AddScoreCard(IContainer container, string label, double score)
    {
        var scorePercent = (int)(score * 100);
        var color = score >= 0.9 ? Colors.Green.Darken2 : score >= 0.5 ? Colors.Orange.Darken2 : Colors.Red.Darken2;

        container.Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(column =>
        {
            column.Item().AlignCenter().Text(scorePercent.ToString()).FontSize(24).Bold().FontColor(color);
            column.Item().AlignCenter().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private void AddCategoryDetails(IContainer container, string title, CategoryAnalysis analysis)
    {
        container.Column(column =>
        {
            column.Item().PaddingVertical(5).Row(row =>
            {
                row.RelativeItem().Text(title).FontSize(12).Bold();
                if (analysis.Priority != null)
                {
                    var priorityColor = analysis.Priority.ToLower() == "high" ? Colors.Red.Darken1 : 
                                       analysis.Priority.ToLower() == "medium" ? Colors.Orange.Darken1 : Colors.Green.Darken1;
                    row.ConstantItem(60).AlignRight().Text(analysis.Priority.ToUpper()).FontSize(8).Bold().FontColor(priorityColor);
                }
            });

            if (analysis.Summary != null)
            {
                column.Item().PaddingTop(3).Text(analysis.Summary).FontSize(9).LineHeight(1.3f);
            }

            if (analysis.ScoreInterpretation != null)
            {
                column.Item().PaddingTop(2).Text(analysis.ScoreInterpretation).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private void AddSecuritySection(IContainer container, SecurityAnalysis security)
    {
        container.Column(column =>
        {
            // Section Header with HackoBot
            column.Item().Row(row =>
            {
                // Avatar image
                row.ConstantItem(50).Height(50).Image("avatars/hackobot.png");
                row.ConstantItem(10); // Spacing
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("🔒 Security Analysis").FontSize(18).Bold().FontColor(Colors.Red.Darken2);
                    col.Item().Text("Analyzed by HackoBot").FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingVertical(10);

            if (security.StructuredAnalysis?.Summary != null)
            {
                var summary = security.StructuredAnalysis.Summary;
                
                // Summary Box
                column.Item().Border(1).BorderColor(Colors.Red.Lighten2).Background(Colors.Red.Lighten4).Padding(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Grade: {summary.Grade}").FontSize(14).Bold();
                        row.RelativeItem().AlignRight().Text($"Risk Level: {summary.RiskLevel}").FontSize(14).Bold().FontColor(Colors.Red.Darken2);
                    });
                    col.Item().PaddingTop(5).Text($"Configured Headers: {summary.ConfiguredHeaders} | Missing Headers: {summary.MissingHeaders}").FontSize(10);
                    if (summary.OverallAssessment != null)
                    {
                        col.Item().PaddingTop(5).Text(summary.OverallAssessment).FontSize(9).LineHeight(1.3f);
                    }
                });
            }

            column.Item().PaddingVertical(10);

            // Issues and Risks
            if (security.StructuredAnalysis?.IssuesAndRisks != null && security.StructuredAnalysis.IssuesAndRisks.Count > 0)
            {
                column.Item().Text("Issues & Risks").FontSize(14).Bold();
                column.Item().PaddingVertical(5);

                foreach (var issue in security.StructuredAnalysis.IssuesAndRisks)
                {
                    column.Item().PaddingVertical(3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(issue.Header ?? "").FontSize(11).Bold();
                            if (issue.Risk != null)
                            {
                                var riskColor = issue.Risk.ToLower().Contains("critical") || issue.Risk.ToLower().Contains("high") 
                                    ? Colors.Red.Darken1 : Colors.Orange.Darken1;
                                row.ConstantItem(80).AlignRight().Text(issue.Risk).FontSize(8).Bold().FontColor(riskColor);
                            }
                        });

                        if (issue.Issue != null)
                        {
                            col.Item().PaddingTop(3).Text(issue.Issue).FontSize(9);
                        }

                        if (issue.AttackVectors != null && issue.AttackVectors.Count > 0)
                        {
                            col.Item().PaddingTop(3).Text("Attack Vectors:").FontSize(8).Bold();
                            foreach (var vector in issue.AttackVectors)
                            {
                                col.Item().Text($"• {vector}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            }
                        }
                    });
                }
            }

            column.Item().PaddingVertical(5);

            // Recommendations
            if (security.StructuredAnalysis?.Recommendations != null && security.StructuredAnalysis.Recommendations.Count > 0)
            {
                column.Item().Text("Recommendations").FontSize(14).Bold();
                column.Item().PaddingVertical(5);

                foreach (var rec in security.StructuredAnalysis.Recommendations)
                {
                    column.Item().PaddingVertical(2).Row(row =>
                    {
                        row.ConstantItem(15).Text("✓").FontColor(Colors.Green.Darken1);
                        row.RelativeItem().Text(rec).FontSize(9).LineHeight(1.3f);
                    });
                }
            }
        });
    }

    private void AddUiUxSection(IContainer container, UiUxAnalysis uiux)
    {
        container.Column(column =>
        {
            // Section Header with Thea
            column.Item().Row(row =>
            {
                // Avatar image
                row.ConstantItem(50).Height(50).Image("avatars/thea.png");
                row.ConstantItem(10); // Spacing
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("🎨 UI/UX Analysis").FontSize(18).Bold().FontColor(Colors.Purple.Darken2);
                    col.Item().Text("Analyzed by Thea Bot (the cutest)").FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingVertical(10);

            // Viewport Analysis
            if (uiux.UiAnalysis != null)
            {
                foreach (var viewport in uiux.UiAnalysis)
                {
                    if (viewport.Viewport != null && viewport.Analysis != null)
                    {
                        column.Item().Element(c => AddViewportAnalysis(c, viewport.Viewport, viewport.Analysis));
                    }
                }
            }

            column.Item().PaddingVertical(10);

            // Grammar Check
            if (uiux.GrammarCheck != null)
            {
                column.Item().Text("Grammar Check").FontSize(14).Bold();
                column.Item().PaddingVertical(5);
                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
                {
                    col.Item().Text($"Score: {uiux.GrammarCheck.Score}/10").FontSize(11).Bold();
                    if (uiux.GrammarCheck.Summary != null)
                    {
                        col.Item().PaddingTop(3).Text(uiux.GrammarCheck.Summary).FontSize(9).LineHeight(1.3f);
                    }
                });
            }

            column.Item().PaddingVertical(10);

            // Site Review
            if (uiux.SiteReview != null)
            {
                column.Item().Text("Overall Site Review").FontSize(14).Bold();
                column.Item().PaddingVertical(5);

                if (uiux.SiteReview.OverallScore != null)
                {
                    column.Item().Text($"Overall Score: {uiux.SiteReview.OverallScore}/10").FontSize(12).Bold().FontColor(Colors.Purple.Darken2);
                }

                if (uiux.SiteReview.CriticalIssues != null && uiux.SiteReview.CriticalIssues.Count > 0)
                {
                    column.Item().PaddingTop(5).Text("Critical Issues:").FontSize(11).Bold();
                    foreach (var issue in uiux.SiteReview.CriticalIssues)
                    {
                        column.Item().PaddingVertical(2).Row(row =>
                        {
                            row.ConstantItem(15).Text("⚠").FontColor(Colors.Red.Darken1);
                            row.RelativeItem().Text(issue).FontSize(9).LineHeight(1.3f);
                        });
                    }
                }

                if (uiux.SiteReview.Recommendations != null && uiux.SiteReview.Recommendations.Count > 0)
                {
                    column.Item().PaddingTop(5).Text("Recommendations:").FontSize(11).Bold();
                    foreach (var rec in uiux.SiteReview.Recommendations)
                    {
                        column.Item().PaddingVertical(2).Row(row =>
                        {
                            row.ConstantItem(15).Text("→").FontColor(Colors.Purple.Darken1);
                            row.RelativeItem().Text(rec).FontSize(9).LineHeight(1.3f);
                        });
                    }
                }

                if (uiux.SiteReview.Encouragement != null)
                {
                    column.Item().PaddingTop(10).Border(1).BorderColor(Colors.Green.Lighten2).Background(Colors.Green.Lighten4)
                        .Padding(10).Text(uiux.SiteReview.Encouragement).FontSize(10).Italic().FontColor(Colors.Green.Darken2);
                }
            }
        });
    }

    private void AddViewportAnalysis(IContainer container, string viewportName, UiAnalysisDetails analysis)
    {
        container.Column(column =>
        {
            var icon = viewportName.ToLower() == "desktop" ? "🖥️" : 
                       viewportName.ToLower() == "tablet" ? "📱" : "📱";
            
            column.Item().PaddingVertical(5).Text($"{icon} {viewportName} Analysis").FontSize(12).Bold();

            // Scores
            if (analysis.Scores != null)
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Layout: {analysis.Scores.Layout}/10").FontSize(9);
                        col.Item().Text($"Colors: {analysis.Scores.Colors}/10").FontSize(9);
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Typography: {analysis.Scores.Typography}/10").FontSize(9);
                        col.Item().Text($"Spacing: {analysis.Scores.Spacing}/10").FontSize(9);
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Overall: {analysis.Scores.Overall}/10").FontSize(9).Bold();
                    });
                });
            }

            // Issues
            if (analysis.Issues != null && analysis.Issues.Count > 0)
            {
                column.Item().PaddingTop(5).Text("Issues:").FontSize(9).Bold();
                foreach (var issue in analysis.Issues.Take(3)) // Limit to top 3 for brevity
                {
                    column.Item().Text($"• {issue}").FontSize(8).LineHeight(1.2f);
                }
            }

            // Suggestions
            if (analysis.Suggestions != null && analysis.Suggestions.Count > 0)
            {
                column.Item().PaddingTop(3).Text("Suggestions:").FontSize(9).Bold();
                foreach (var suggestion in analysis.Suggestions.Take(3)) // Limit to top 3
                {
                    column.Item().Text($"→ {suggestion}").FontSize(8).LineHeight(1.2f).FontColor(Colors.Grey.Darken1);
                }
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ").FontSize(8);
            text.CurrentPageNumber().FontSize(8);
            text.Span(" of ").FontSize(8);
            text.TotalPages().FontSize(8);
        });
    }

    private async Task<string> UploadPdfToBlobAsync(byte[] pdfBytes, string userId, string requestId)
    {
        try
        {
            var blobName = $"{userId}_{requestId}/{userId}_{requestId}_report.pdf";
            
            _logger.LogInformation("Uploading PDF report to blob storage: {BlobName}", blobName);

            await _blobStorageService.UploadBlobAsync(
                ReportsContainer,
                blobName,
                pdfBytes,
                "application/pdf");

            var sasUrl = _blobStorageService.GenerateSasUrl(
                ReportsContainer,
                blobName,
                expirationHours: 168); // 7 days

            _logger.LogInformation("PDF report uploaded successfully");
            return sasUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload PDF report for UserId: {UserId}, RequestId: {RequestId}", userId, requestId);
            throw;
        }
    }
}
