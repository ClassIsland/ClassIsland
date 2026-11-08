using System;

namespace ClassIsland.Models.Profile;

internal class ProfileLoadException(string message, Exception? innerException = null)
    : Exception(message, innerException);

internal sealed class ProfileDowngradeException(string migrationId)
    : ProfileLoadException($"档案包含不允许降级的迁移：{migrationId}");

internal sealed class ProfileReadException(Exception innerException)
    : ProfileLoadException("无法读取档案。", innerException);
