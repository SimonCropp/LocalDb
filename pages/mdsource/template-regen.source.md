# Template Regen

Template re-generating performs the following action

 * Delete the existing template files.
 * Create a new template database.
 * Apply the default schema and data as defined by the `buildTemplate` parameter in the `SqlInstance` constructor.
 * Detach the database

Re-generating the template database is a relatively expensive operation. Ideally, it should only be performed when the default schema and data requires a change. To enable this a timestamp convention is used. When the template re-generating, its file creation time is set to a timestamp. On the next run, that timestamp is compared to avoid re-generation on a match. The timestamp can be controlled via `timestamp` parameter in the `SqlInstance` constructor.

If `timestamp` parameter is not defined the following conventions are used:

 * For Raw SQL the last modified time of the calling Assembly [Assembly.GetCallingAssembly](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getcallingassembly). This will usually result in the last modified time of the unit test assembly, and hence avoid re-generation unless a solution build has occurred.
 * For EntityFramework the last modified time of the Assembly containing the DataContext.

There is a timestamp helper class to help derive last modified time of an Assembly (if the above conventions do not suffice):

snippet: Timestamp