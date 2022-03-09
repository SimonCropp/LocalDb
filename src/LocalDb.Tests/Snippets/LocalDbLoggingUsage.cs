class LocalDbLoggingUsage
{
    LocalDbLoggingUsage()
    {
        #region LocalDbLoggingUsage

        LocalDbLogging.EnableVerbose();

        #endregion

        #region LocalDbLoggingUsageSqlLogging

        LocalDbLogging.EnableVerbose(sqlLogging: true);

        #endregion
    }
}