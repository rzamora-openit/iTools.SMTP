using BlazorPro.BlazorSize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.ViewModels
{
	public class ThemeViewModel
	{
        internal string Id { get; private set; } = "theme_" + Guid.NewGuid();

        public string Primary { get; set; }
		public string Secondary { get; set; }
		public string Background { get; set; }
		public string Surface { get; set; }
		public string OnPrimary { get; set; }
		public string OnSecondary { get; set; }
		public string OnSurface { get; set; }

        private void GenerateStyle(StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(Primary))
            {
                sb.AppendLine($"--mdc-theme-primary: {Primary};");
            }

            if (!string.IsNullOrEmpty(Secondary))
            {
                sb.AppendLine($"--mdc-theme-secondary: {Secondary};");
            }

            if (!string.IsNullOrEmpty(Background))
            {
                sb.AppendLine($"--mdc-theme-background: {Background};");
            }

            if (!string.IsNullOrEmpty(Surface))
            {
                sb.AppendLine($"--mdc-theme-surface: {Surface};");
            }

            if (!string.IsNullOrEmpty(OnPrimary))
            {
                sb.AppendLine($"--mdc-theme-on-primary: {OnPrimary};");
            }

            if (!string.IsNullOrEmpty(OnSecondary))
            {
                sb.AppendLine($"--mdc-theme-on-secondary: {OnSecondary};");
            }

            if (!string.IsNullOrEmpty(OnSurface))
            {
                sb.AppendLine($"--mdc-theme-on-surface: {OnSurface};");
            }
        }

        public string GetStyle()
        {
            var sb = new StringBuilder();
            GenerateStyle(sb);
            return sb.ToString();
        }

        public string GetStyleTag()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<style>");
            sb.Append(".");
            sb.AppendLine(Id);
            sb.AppendLine("{");
            GenerateStyle(sb);
            sb.AppendLine("}");
            sb.AppendLine("</style>");
            return sb.ToString();
        }
    }
}
