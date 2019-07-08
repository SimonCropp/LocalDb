# Directory and Instance Name Resolution

The instance name is defined as:

snippet: GetInstanceName

That InstanceName is then used to derive the data directory. In order:

 * If `LocalDBData` environment variable exists then use `LocalDBData\InstanceName`.
 * If `AGENT_TEMPDIRECTORY` environment variable exists then use `AGENT_TEMPDIRECTORY\LocalDb\InstanceName`.
 * Use `%TempDir%\LocalDb\InstanceName`.

There is an explicit registration override that takes an instance name and a directory for that instance:


## For SQL:

snippet: RegisterExplicit


## For EF:

snippet: EfRegisterExplicit

