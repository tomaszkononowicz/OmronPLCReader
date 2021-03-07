using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interactivity;

namespace OmronPLCTemperatureReader.Models
{
    public class TerminalFile
    {
        public TerminalFile(string name, Uri basePath, string relativePath, bool isDirectory)
        {
            Name = name;
            BasePath = basePath;
            RelativePath = relativePath;
            IsDirectory = isDirectory;
        }

        public string Name { get; set; }
        public Uri FullPath => new Uri(BasePath, RelativePath);
        public Uri BasePath { get; set; }
        public string RelativePath { get; set; }
        public List<TerminalFile> Children { get; set; }
        public bool IsDirectory {get; set; }
        public bool IsSelected {get; set; }
    }
}
