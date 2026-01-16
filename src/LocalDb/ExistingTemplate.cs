#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif

/// <summary>
/// Represents an existing template database that can be used instead of building one from scratch.
/// When provided to a <c>SqlInstance</c> constructor, the specified .mdf and .ldf files are used
/// as the template, and the <c>buildTemplate</c> delegate is not called.
/// </summary>
/// <remarks>
/// This is useful for scenarios where:
/// <list type="bullet">
///   <item><description>The template database is pre-built as part of a CI/CD pipeline</description></item>
///   <item><description>The template is shared across multiple test projects</description></item>
///   <item><description>Template creation is expensive and you want to reuse a cached version</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var existingTemplate = new ExistingTemplate(
///     dataPath: @"C:\Templates\MyApp_Template.mdf",
///     logPath: @"C:\Templates\MyApp_Template_log.ldf");
///
/// var instance = new SqlInstance&lt;MyDbContext&gt;(
///     builder => new MyDbContext(builder.Options),
///     existingTemplate: existingTemplate);
/// </code>
/// </example>
public struct ExistingTemplate
{
    /// <summary>
    /// Gets the file path to the template database data file (.mdf).
    /// </summary>
    public string DataPath { get; }

    /// <summary>
    /// Gets the file path to the template database log file (.ldf).
    /// </summary>
    public string LogPath { get; }

    /// <summary>
    /// Initializes a new <see cref="ExistingTemplate"/> with the specified data and log file paths.
    /// </summary>
    /// <param name="dataPath">
    /// The absolute file path to the template database data file (.mdf).
    /// The file must exist and be accessible.
    /// </param>
    /// <param name="logPath">
    /// The absolute file path to the template database log file (.ldf).
    /// The file must exist and be accessible.
    /// </param>
    /// <example>
    /// <code>
    /// var template = new ExistingTemplate(
    ///     @"C:\Templates\MyDb.mdf",
    ///     @"C:\Templates\MyDb_log.ldf");
    /// </code>
    /// </example>
    public ExistingTemplate(string dataPath, string logPath)
    {
        Ensure.NotNullOrWhiteSpace(dataPath);
        Ensure.NotNullOrWhiteSpace(logPath);
        DataPath = dataPath;
        LogPath = logPath;
    }
}