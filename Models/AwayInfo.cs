using System;

namespace DeskLock.Models;

public sealed record AwayInfo(string Message, DateTime? ReturnTime, StatusDefinition Theme);
