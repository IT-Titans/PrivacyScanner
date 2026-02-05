using System.Text.RegularExpressions;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
/// <summary>
/// Scans the text With your Regexes
/// returns warnings when matches are found
/// </summary>
public class RegexScanCommandHandler(ILogger<ProcessScanCommandHandler> logger) : IRequestHandler<RegexScanCommand, ScanResultDto>
{
    public async ValueTask<ScanResultDto> Handle(
        RegexScanCommand request,
        CancellationToken cancellationToken)
    {
        List<ScanWarningDto> warnings = new List<ScanWarningDto>();

        var linesAsync = File.ReadLinesAsync(request.FilePath.FullName, cancellationToken);
        int currentLineNumber = 1;

        string? prevLine = null;
        string current = null!;
        bool first = true;
        List<RegexRuleDto> regexRuleDtos = new List<RegexRuleDto>();

        foreach (var regexDto in request.RegexRuleList)
        {
            if (!IsValidRegex(regexDto.Rule))
            {
                logger.LogWarning("Regex: {regexName} Is not a valid regex", regexDto.RuleName);
            }
            else
            {
                regexRuleDtos.Add(regexDto);
            }
        }

        await foreach (string next in linesAsync)
        {
            if (!first)
            {
                warnings.AddRange(ScanLine(new LineToProcess
                {
                    Line = current,
                    PrevLine = prevLine,
                    NextLine = next
                }, currentLineNumber, regexRuleDtos));

                currentLineNumber++;
            }

            prevLine = current;
            current = next;
            first = false;
        }

        if (!first)
        {
            warnings.AddRange(ScanLine(new LineToProcess
            {
                Line = current,
                PrevLine = prevLine,
                NextLine = null
            }, currentLineNumber, regexRuleDtos));
        }

        var result = new ScanResultDto
        {
            FilePath = request.FilePath,
            Warnings = warnings
        };

        return await ValueTask.FromResult(result);
    }

    private static List<ScanWarningDto> ScanLine(LineToProcess lineToProcess, int currentLine, List<RegexRuleDto> regexRuleList)
    {
        List<ScanWarningDto> warnings = new List<ScanWarningDto>();

        foreach (var regex in regexRuleList)
        {
            warnings.AddRange(ScanLineWithSingleRegex(lineToProcess, currentLine, regex));
        }

        return warnings;
    }

    private static List<ScanWarningDto> ScanLineWithSingleRegex(LineToProcess lineToProcess, int currentLine, RegexRuleDto regex)
    {
        var matches = Regex.Matches(lineToProcess.Line, regex.Rule).ToList();

        List<ScanWarningDto> warnings = new List<ScanWarningDto>();

        foreach (var match in matches)
        {
            if (match.Success)
            {
                ScanWarningDto warning = new ScanWarningDto
                {
                    Line = currentLine,
                    Start = match.Index,
                    End = match.Index + match.Length,
                    SuspiciousContent = new SuspiciousContentDto
                    {
                        MatchText = match.Value,
                        PrevLine = lineToProcess.PrevLine,
                        HitLine = lineToProcess.Line,
                        NextLine = lineToProcess.NextLine
                    },
                    Type = ScanWarningType.Rule,
                    RuleName = regex.RuleName,
                    RuleId = Guid.NewGuid(),
                    SpacyLabel = null
                };

                warnings.Add(warning);
            }
        }

        return warnings;
    }

    private static bool IsValidRegex(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return false;

        try
        {
            _ = Regex.Match("Test", pattern);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }

    private class LineToProcess
    {
        public required string Line { get; init; }

        public required string? PrevLine { get; init; }

        public required string? NextLine { get; init; }
    }
}