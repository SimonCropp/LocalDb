## LocalDB

How MS SqlServer LocalDB is structured

```mermaid
graph TB
    subgraph PM["Physical Machine (Windows)"]
        subgraph SQLLocalDB["SqlServer LocalDB"]
            subgraph Instance1["LocalDB Instance: MSSQLLocalDB"]
                DB1A[(DB: MyApp)]
                DB1B[(DB: TestDB)]
                DB1C[(DB: master)]
            end
            
            subgraph Instance2["LocalDB Instance: ProjectsV15"]
                DB2A[(DB: WebApi)]
                DB2B[(DB: master)]
            end
            
            subgraph Instance3["LocalDB Instance: MyCustomInstance"]
                DB3A[(DB: Analytics)]
                DB3B[(DB: Staging)]
                DB3C[(DB: master)]
            end
        end
        
        UserData["Default User Data Folder<br/>%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances\"]
    end
    
    Instance1 --> |"Stores mdf/ldf files in"| UserData
    Instance2 --> |"Stores mdf/ldf files in"| UserData
    Instance3 --> |"Stores mdf/ldf files in"| UserData
```

Key relationships:

 * Physical Machine → One Windows machine can have one LocalDB engine installed
 * LocalDB Engine → Can host multiple isolated instances (each is like a mini SQL Server)
 * Instance → Each contains multiple databases (always includes system DBs like master)
 * Storage → Each instance stores its .mdf and .ldf files in a subfolder under %LOCALAPPDATA%


## Instance Startup Flow

```mermaid
flowchart TD

    start[Start]
    deleteFiles[Delete Template Files]
    flushDir[Flush Directory]
    createInstance[Create Instance]
    checkExists{Instance<br>Exists?}
    checkRunning{Instance<br>Running?}
    deleteInstance[Delete Instance]
    optimizeModel[Optimize Model DB]
    checkTimestamp{Timestamp<br>Match?}
    checkCallback{Callback<br>Exists?}
    stopAndDelete[Stop & Delete Instance]
    checkDataFile{Data File Exists?}
    openTemplateConn[Open Template Connection]
    createTemplateDb[Create Template DB]
    runBuildTemplate[Run buildTemplate]
    checkCallbackAfterBuild{Callback<br>Exists?}
    runCallbackAfterBuild[Run Callback]
    detachShrink[Shrink & Detach Template]
    setTimestamp[Set Creation Timestamp]
    attachTemplate[Attach Template DB]
    openForCallback[Open Template Connection]
    runCallback[Run Callback]
    detachTemplate[Detach Template DB]
    done[Done]

    start --> checkExists

    checkExists -->|No| flushDir
    checkExists -->|Yes| checkRunning

    checkRunning -->|No| deleteInstance
    deleteInstance --> flushDir

    checkRunning -->|Yes| checkDataFile

    checkDataFile -->|No| stopAndDelete
    stopAndDelete --> flushDir

    checkDataFile -->|Yes| checkTimestamp

    checkTimestamp -->|No| rebuildTemplate

    checkTimestamp -->|Yes| checkCallback
    flushDir --> createInstance
    createInstance --> optimizeModel
    optimizeModel --> rebuildTemplate
    rebuildTemplate --> deleteFiles
    deleteFiles --> createTemplateDb
    createTemplateDb --> openTemplateConn
    openTemplateConn --> runBuildTemplate
    runBuildTemplate --> checkCallbackAfterBuild
    checkCallbackAfterBuild -->|Yes| runCallbackAfterBuild
    checkCallbackAfterBuild -->|No| detachShrink
    runCallbackAfterBuild --> detachShrink
    detachShrink --> setTimestamp

    checkCallback -->|Yes| attachTemplate
    attachTemplate --> openForCallback
    openForCallback --> runCallback
    runCallback --> detachTemplate

    checkCallback -->|No| done
    detachTemplate --> done
    setTimestamp --> done
```

## CreateAndDetachTemplate Flow

```mermaid
flowchart TD
```

## Create DB From Template Flow

```mermaid
flowchart TD
    entry[CreateDatabaseFromTemplate] --> checkReservedName{Name = 'template'?}
    checkReservedName -->|Yes| throwReserved[Throw Exception]
    checkReservedName -->|No| checkValidName{Valid Filename?}
    checkValidName -->|No| throwInvalid[Throw ArgumentException]
    checkValidName -->|Yes| buildPaths[Build File Paths]
    buildPaths --> awaitStartup[Await Startup Task]
    awaitStartup --> openMaster[Open Master Connection]
    openMaster --> takeOffline[Take DB Offline if exists]
    takeOffline --> copyData[Copy Data File]
    takeOffline --> copyLog[Copy Log File]
    copyData --> createOrOnline[Create or Make Online]
    copyLog --> createOrOnline
    createOrOnline --> openNewConn[Open New Connection]
    openNewConn --> returnConn[Return Connection]
```

## Constructor Initialization

```mermaid
flowchart TD
    entry[Constructor] --> validateOS[Validate OS]
    validateOS --> validateSize[Validate DB Size]
    validateSize --> validateName[Validate Instance Name]
    validateName --> buildConnStrings[Build Connection Strings]
    buildConnStrings --> checkTemplate{Existing Template?}
    checkTemplate -->|No| setDefaultPaths[Set Default Template Paths]
    checkTemplate -->|Yes| useProvidedPaths[Use Provided Template Paths]
    setDefaultPaths --> createDir[Create Directory]
    useProvidedPaths --> createDir
    createDir --> resetAccess[Reset Directory Access]
    resetAccess --> setServerName[Set Server Name]
```
