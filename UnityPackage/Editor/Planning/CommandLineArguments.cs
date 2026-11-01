using System;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Reads <c>-name value</c> pairs from command line arguments, ignoring the letter case of names.
    /// </summary>
    internal sealed class CommandLineArguments
    {
        private readonly string[] _arguments;

        public CommandLineArguments(string[] arguments)
        {
            _arguments = arguments ?? Array.Empty<string>();
        }

        /// <summary>
        /// Arguments the editor was started with.
        /// </summary>
        public static CommandLineArguments Current
        {
            get
            {
                return new CommandLineArguments(Environment.GetCommandLineArgs());
            }
        }

        /// <summary>
        /// Finds the value that follows the first of the given names that is present, in the order given,
        /// so a package argument wins over its GameCI alias. Returns null when none is present or has a value.
        /// </summary>
        public string GetValue(params string[] names)
        {
            foreach (string name in names)
            {
                int index = IndexOf(name);
                if (index < 0)
                {
                    continue;
                }

                // A following argument name means this one was given without a value.
                int valueIndex = index + 1;
                if (valueIndex < _arguments.Length && !IsArgumentName(_arguments[valueIndex]))
                {
                    return _arguments[valueIndex];
                }
            }

            return null;
        }

        private int IndexOf(string name)
        {
            for (int index = 0; index < _arguments.Length; index++)
            {
                if (string.Equals(_arguments[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool IsArgumentName(string argument)
        {
            return argument.Length > 1 && argument[0] == '-' && !char.IsDigit(argument[1]);
        }
    }
}
