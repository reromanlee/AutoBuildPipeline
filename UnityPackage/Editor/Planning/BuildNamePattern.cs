using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Validates and formats configurable folder and player file name patterns using tokens such as
    /// <c>{name}</c>, <c>{product}</c>, <c>{identifier}</c>, <c>{version}</c>, <c>{code}</c>,
    /// <c>{platform}</c>, <c>{label}</c>, <c>{date}</c> and <c>{time}</c>.
    /// </summary>
    internal static class BuildNamePattern
    {
        public const string DefaultReleaseFolderPattern = "{name}-{version}-{code}";
        public const string DefaultPreviewFolderPattern = "{name}-{label}";
        public const string SupportedTokensList = "{name}, {product}, {identifier}, {version}, {code}, {platform}, {label}, {date}, {time}";

        private const string SeparatorCharacters = "-_. ";

        private static readonly HashSet<string> SupportedTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "name",
            "product",
            "identifier",
            "version",
            "code",
            "platform",
            "label",
            "date",
            "time"
        };

        /// <summary>
        /// Token values resolved for a single build.
        /// </summary>
        internal readonly struct Tokens
        {
            public readonly string Name;
            public readonly string Product;
            public readonly string Identifier;
            public readonly string Version;
            public readonly string Code;
            public readonly string Platform;
            public readonly string Label;
            public readonly string Date;
            public readonly string Time;

            public Tokens(
                string name,
                string product,
                string identifier,
                string version,
                string code,
                string platform,
                string label,
                DateTime timestamp)
            {
                Name = name;
                Product = FileNames.Sanitize(product);
                Identifier = string.IsNullOrWhiteSpace(identifier) ? Product : FileNames.Sanitize(identifier);
                Version = version ?? string.Empty;
                Code = code ?? string.Empty;
                Platform = FileNames.Sanitize(platform);
                Label = label ?? string.Empty;
                Date = timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                Time = timestamp.ToString("HHmm", CultureInfo.InvariantCulture);
            }

            public string GetValue(string tokenName)
            {
                switch (tokenName.ToLowerInvariant())
                {
                    case "name":
                    {
                        return Name;
                    }
                    case "product":
                    {
                        return Product;
                    }
                    case "identifier":
                    {
                        return Identifier;
                    }
                    case "version":
                    {
                        return Version;
                    }
                    case "code":
                    {
                        return Code;
                    }
                    case "platform":
                    {
                        return Platform;
                    }
                    case "label":
                    {
                        return Label;
                    }
                    case "date":
                    {
                        return Date;
                    }
                    case "time":
                    {
                        return Time;
                    }
                    default:
                    {
                        return string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Formats and sanitizes a release folder name. Requires <c>{version}</c> so two releases never share a folder.
        /// </summary>
        /// <exception cref="BuildFailedException">The pattern is invalid or missing <c>{version}</c>.</exception>
        public static string FormatReleaseFolder(string pattern, in Tokens tokens)
        {
            string effective = string.IsNullOrWhiteSpace(pattern) ? DefaultReleaseFolderPattern : pattern.Trim();
            List<Segment> segments = Parse(effective, "Release Folder Pattern");
            if (!ContainsToken(segments, "version"))
            {
                throw new BuildFailedException("Release Folder Pattern must include {version} so releases do not share a folder. Change it in Project Settings ▸ Auto Build Pipeline.");
            }

            return FormatSegments(segments, tokens);
        }

        /// <summary>
        /// Formats and sanitizes a preview folder name. Requires <c>{label}</c> so previews do not overwrite releases.
        /// </summary>
        /// <exception cref="BuildFailedException">The pattern is invalid or missing <c>{label}</c>.</exception>
        public static string FormatPreviewFolder(string pattern, in Tokens tokens)
        {
            string effective = string.IsNullOrWhiteSpace(pattern) ? DefaultPreviewFolderPattern : pattern.Trim();
            List<Segment> segments = Parse(effective, "Preview Folder Pattern");
            if (!ContainsToken(segments, "label"))
            {
                throw new BuildFailedException("Preview Folder Pattern must include {label} so previews do not overwrite releases. Change it in Project Settings ▸ Auto Build Pipeline.");
            }

            return FormatSegments(segments, tokens);
        }

        /// <summary>
        /// Formats and sanitizes a player file (or WebGL folder) name without extension.
        /// Returns null when the pattern is blank, signaling the platform's default naming.
        /// </summary>
        /// <exception cref="BuildFailedException">The pattern is invalid.</exception>
        public static string FormatPlayerName(string pattern, in Tokens tokens)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            List<Segment> segments = Parse(pattern.Trim(), "Player Name Pattern");
            return FormatSegments(segments, tokens);
        }

        /// <summary>
        /// Validates a pattern for syntax, allowed tokens, single-segment path safety and optional required token.
        /// </summary>
        public static bool TryValidate(string pattern, string settingName, string requiredToken, out string error)
        {
            try
            {
                List<Segment> segments = Parse(pattern, settingName);
                if (!string.IsNullOrEmpty(requiredToken) && !ContainsToken(segments, requiredToken))
                {
                    error = $"{settingName} must include {{{requiredToken}}}.";
                    return false;
                }

                error = null;
                return true;
            }
            catch (BuildFailedException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static List<Segment> Parse(string pattern, string settingName)
        {
            if (pattern.IndexOf('/') >= 0 || pattern.IndexOf('\\') >= 0 || pattern.Contains(".."))
            {
                throw new BuildFailedException($"{settingName} must be a single name without path separators ('/', '\\') or '..'. Change it in Project Settings ▸ Auto Build Pipeline.");
            }

            List<Segment> segments = new();
            StringBuilder literal = new();
            int index = 0;
            while (index < pattern.Length)
            {
                char current = pattern[index];
                if (current == '{')
                {
                    int closeIndex = pattern.IndexOf('}', index + 1);
                    if (closeIndex < 0)
                    {
                        throw new BuildFailedException($"{settingName} has an unclosed '{{'. Change it in Project Settings ▸ Auto Build Pipeline.");
                    }

                    string tokenName = pattern.Substring(index + 1, closeIndex - index - 1).Trim();
                    if (tokenName.Length == 0 || tokenName.IndexOf('{') >= 0)
                    {
                        throw new BuildFailedException($"{settingName} has a malformed token. Supported tokens: {SupportedTokensList}.");
                    }

                    if (!SupportedTokens.Contains(tokenName))
                    {
                        throw new BuildFailedException($"{settingName} uses unknown token '{{{tokenName}}}'. Supported tokens: {SupportedTokensList}.");
                    }

                    if (literal.Length > 0)
                    {
                        segments.Add(Segment.FromLiteral(literal.ToString()));
                        literal.Clear();
                    }

                    segments.Add(Segment.FromToken(tokenName));
                    index = closeIndex + 1;
                    continue;
                }

                if (current == '}')
                {
                    throw new BuildFailedException($"{settingName} has an unmatched '}}'. Change it in Project Settings ▸ Auto Build Pipeline.");
                }

                literal.Append(current);
                index++;
            }

            if (literal.Length > 0)
            {
                segments.Add(Segment.FromLiteral(literal.ToString()));
            }

            return segments;
        }

        private static bool ContainsToken(List<Segment> segments, string tokenName)
        {
            foreach (Segment segment in segments)
            {
                if (segment.IsToken && string.Equals(segment.Value, tokenName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatSegments(List<Segment> segments, in Tokens tokens)
        {
            StringBuilder builder = new();
            bool isLastLiteral = false;
            bool isSkipLeadingSeparator = false;

            foreach (Segment segment in segments)
            {
                if (!segment.IsToken)
                {
                    string text = segment.Value;
                    if (isSkipLeadingSeparator && text.Length > 0 && IsSeparator(text[0]))
                    {
                        text = text.Substring(1);
                    }

                    isSkipLeadingSeparator = false;
                    if (text.Length > 0)
                    {
                        builder.Append(text);
                        isLastLiteral = true;
                    }

                    continue;
                }

                string resolved = tokens.GetValue(segment.Value);
                if (string.IsNullOrEmpty(resolved))
                {
                    // Drop the adjacent separator attached to an empty optional token such as {code} on Windows.
                    if (isLastLiteral && builder.Length > 0 && IsSeparator(builder[builder.Length - 1]))
                    {
                        builder.Length--;
                        isLastLiteral = false;
                    }
                    else if (builder.Length == 0)
                    {
                        isSkipLeadingSeparator = true;
                    }

                    continue;
                }

                builder.Append(resolved);
                isLastLiteral = false;
                isSkipLeadingSeparator = false;
            }

            return FileNames.Sanitize(builder.ToString());
        }

        private static bool IsSeparator(char character)
        {
            return SeparatorCharacters.IndexOf(character) >= 0;
        }

        private readonly struct Segment
        {
            public readonly bool IsToken;
            public readonly string Value;

            private Segment(bool isToken, string value)
            {
                IsToken = isToken;
                Value = value;
            }

            public static Segment FromLiteral(string value)
            {
                return new Segment(isToken: false, value);
            }

            public static Segment FromToken(string tokenName)
            {
                return new Segment(isToken: true, tokenName);
            }
        }
    }
}
