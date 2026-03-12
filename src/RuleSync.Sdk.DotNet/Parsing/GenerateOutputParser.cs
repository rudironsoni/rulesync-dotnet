#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Rulesync.Sdk.DotNet.Models;

namespace Rulesync.Sdk.DotNet.Parsing;

/// <summary>
/// Parses text output from rulesync generate command.
/// </summary>
internal static class GenerateOutputParser
{
    /// <summary>
    /// Parses verbose output from generate command.
    /// </summary>
    public static GenerateResult Parse(string output)
    {
        var rulesPaths = new List<string>();
        var ignorePaths = new List<string>();
        var mcpPaths = new List<string>();
        var commandsPaths = new List<string>();
        var subagentsPaths = new List<string>();
        var skillsPaths = new List<string>();
        var hooksPaths = new List<string>();
        
        if (string.IsNullOrWhiteSpace(output))
        {
            return CreateResult(rulesPaths, ignorePaths, mcpPaths, commandsPaths, subagentsPaths, skillsPaths, hooksPaths);
        }

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Parse "Generated rules: <file>" lines
            if (trimmed.StartsWith("Generated rules:", StringComparison.OrdinalIgnoreCase))
            {
                var file = trimmed["Generated rules:".Length..].Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    rulesPaths.Add(file);
                }
            }
            // Parse "Generated ignore: <file>" lines
            else if (trimmed.StartsWith("Generated ignore:", StringComparison.OrdinalIgnoreCase))
            {
                var file = trimmed["Generated ignore:".Length..].Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    ignorePaths.Add(file);
                }
            }
            // Parse "Generated mcp: <file>" lines
            else if (trimmed.StartsWith("Generated mcp:", StringComparison.OrdinalIgnoreCase))
            {
                var file = trimmed["Generated mcp:".Length..].Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    mcpPaths.Add(file);
                }
            }
            // Parse "Generated commands: <file>" lines
            else if (trimmed.StartsWith("Generated commands:", StringComparison.OrdinalIgnoreCase))
            {
                var file = trimmed["Generated commands:".Length..].Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    commandsPaths.Add(file);
                }
            }
            // Parse "Generated subagents: <file>" lines
            else if (trimmed.StartsWith("Generated subagents:", StringComparison.OrdinalIgnoreCase))
            {
                var file = trimmed["Generated subagents:".Length..].Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    subagentsPaths.Add(file);
                }
            }
            // Parse "Generated skills: <file>" lines
            else if (trimmed.StartsWith("Generated skills:", StringComparison.OrdinalIgnoreCase))
            {
                var file = trimmed["Generated skills:".Length..].Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    skillsPaths.Add(file);
                }
            }
            // Parse "Generated hooks: <file>" lines
            else if (trimmed.StartsWith("Generated hooks:", StringComparison.OrdinalIgnoreCase))
            {
                var file = trimmed["Generated hooks:".Length..].Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    hooksPaths.Add(file);
                }
            }
        }

        return CreateResult(rulesPaths, ignorePaths, mcpPaths, commandsPaths, subagentsPaths, skillsPaths, hooksPaths);
    }

    private static GenerateResult CreateResult(
        List<string> rulesPaths,
        List<string> ignorePaths,
        List<string> mcpPaths,
        List<string> commandsPaths,
        List<string> subagentsPaths,
        List<string> skillsPaths,
        List<string> hooksPaths)
    {
        return new GenerateResult
        {
            RulesCount = rulesPaths.Count,
            RulesPaths = rulesPaths,
            IgnoreCount = ignorePaths.Count,
            IgnorePaths = ignorePaths,
            McpCount = mcpPaths.Count,
            McpPaths = mcpPaths,
            CommandsCount = commandsPaths.Count,
            CommandsPaths = commandsPaths,
            SubagentsCount = subagentsPaths.Count,
            SubagentsPaths = subagentsPaths,
            SkillsCount = skillsPaths.Count,
            SkillsPaths = skillsPaths,
            HooksCount = hooksPaths.Count,
            HooksPaths = hooksPaths,
            HasDiff = rulesPaths.Count > 0 || 
                     ignorePaths.Count > 0 || 
                     mcpPaths.Count > 0 || 
                     commandsPaths.Count > 0 || 
                     subagentsPaths.Count > 0 || 
                     skillsPaths.Count > 0 || 
                     hooksPaths.Count > 0
        };
    }
}
