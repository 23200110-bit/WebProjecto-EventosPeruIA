using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.DTOs.Reports
{
    public class ExportFileResponseDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    }
}
