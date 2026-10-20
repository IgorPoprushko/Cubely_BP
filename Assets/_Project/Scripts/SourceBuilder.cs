using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Assembles named block templates without compiling or executing them.</summary>
    public static class SourceBuilder
    {

        private const string ProgramTemplate = @"
public static class GeneratedProgram
{
    public static void Main()
    {
        ${Body}
    }
}";
        private static readonly Regex Placeholder = new Regex(@"\$\{([^{}]*)\}");

        /// <summary>Builds from the supplied first block and prints the final source once.</summary>
        public static string Build(CodeBlockNode firstBlock)
        {
            string body = firstBlock == null ? string.Empty : BuildNode(firstBlock);
            string source = ProgramTemplate.Replace("${Body}", body);
            Debug.Log(source, firstBlock);
            return source;
        }

        private static string BuildNode(CodeBlockNode block)
        {
            if (block.Definition == null)
                throw new InvalidOperationException($"Block '{block.name}' has no definition.");

            string template = block.Definition.codeText ?? string.Empty;
            var fragments = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (CodeBlockSlot slot in block.Slots)
            {
                if (slot == null)
                    throw new InvalidOperationException($"Block '{block.name}' has an unassigned slot.");
                if (string.IsNullOrWhiteSpace(slot.Id) || fragments.ContainsKey(slot.Id))
                    throw new InvalidOperationException($"Block '{block.name}' needs non-empty, unique slot IDs. Invalid ID: '{slot.Id}'.");

                CodeBlockNode child = slot.ConnectedBlock;
                if (child != null && !template.Contains("${" + slot.Id + "}"))
                    throw new InvalidOperationException($"Block '{block.name}' has a connected slot '{slot.Id}', but its template has no ${{{slot.Id}}} placeholder.");

                fragments.Add(slot.Id, child == null ? string.Empty : BuildNode(child));
            }

            // Replace only placeholders in this template, not text inserted by a child.
            // Children are already fully assembled; repeated placeholders reuse their fragment.
            return Placeholder.Replace(template, match =>
            {
                string id = match.Groups[1].Value;
                if (!fragments.TryGetValue(id, out string fragment))
                    throw new InvalidOperationException($"Block '{block.name}' uses placeholder '${{{id}}}', but has no matching slot.");
                return fragment;
            });
        }
    }
}
