using System;

namespace FinTrack.Application.Reports.DTOs;

public record ExportFileResultDto(
    byte[] Content,
    string ContentType,
    string FileName
);
