using System;
using System.Linq;

namespace CollactionTestSelection.Models
{
    public sealed class DeployViewModel
    {
        public DeployViewModel(string tag, string result)
        {
            Tag = tag;
            Result = result;
        }

        public string Tag { get; }
        public string Result { get; }
        public string ResultAsList => $"<li>{string.Join("</li><li>", Result.Split(Environment.NewLine).Where(s => !string.IsNullOrWhiteSpace(s)))}</li>";
    }
}
