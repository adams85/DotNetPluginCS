using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fclp;
using Fclp.Internals;
using Fclp.Internals.Extensions;

namespace DotNetPlugin
{
    public class CommandLineHelpFormatter : ICommandLineOptionFormatter
    {
        private string _header;

        public CommandLineHelpFormatter(string header = null)
        {
            _header = header;
        }

        public string Format(IEnumerable<ICommandLineOption> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var optionsArray = options.ToArray();

            if (!optionsArray.Any())
                return "No options are available.";

            var sb = new StringBuilder();
            sb.AppendLine();

            // add headers first
            if (!_header.IsNullOrWhiteSpace())
            {
                sb.AppendLine(_header);
                sb.AppendLine();
            }

            foreach (var option in optionsArray)
            {
                sb.Append('\t');

                if (option.ShortName.IsNullOrWhiteSpace())
                    sb.Append("--").Append(option.LongName);
                else if (option.LongName.IsNullOrWhiteSpace())
                    sb.Append("-").Append(option.ShortName);
                else
                    sb.Append("-").Append(option.ShortName).Append(", ").Append("--").Append(option.LongName);

                sb.Append('\t').Append('\t');

                sb.Append(option.Description);

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
