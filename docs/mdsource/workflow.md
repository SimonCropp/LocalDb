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
    start[Start] --> checkExists{Instance Exists?}

    checkExists -->|No| flushDir[Flush Directory]
    checkExists -->|Yes| checkRunning{Instance Running?}

    checkRunning -->|No| deleteInstance[Delete Instance]
    deleteInstance --> flushDir

    checkRunning -->|Yes| checkDataFile{Data File Exists?}

    checkDataFile -->|No| stopAndDelete[Stop and Delete Instance]
    stopAndDelete --> flushDir

    checkDataFile -->|Yes| checkTimestamp{Timestamp Match?}

    checkTimestamp -->|Yes| createNoRebuild[CreateAndDetachTemplate<br/>rebuildTemplate: false<br/>optimize Model DB: false]

    checkTimestamp -->|No| createRebuild[CreateAndDetachTemplate<br/>rebuildTemplate: true<br/>optimize Model DB: false]
    flushDir --> createInstance[Create Instance]
    createInstance --> startInstance[Start Instance]
    startInstance --> createFull[CreateAndDetachTemplate<br/>rebuildTemplate: true<br/>optimize Model DB: true]
```

## CreateAndDetachTemplate Flow

```mermaid
flowchart TD
    entry[CreateAndDetachTemplate] --> openMaster[Open Master Connection]
    openMaster --> checkOptimize{Optimize Model DB?}

    checkOptimize -->|Yes| executeOptimize[Execute Optimize Model DB Command]
    executeOptimize --> checkRebuild{Rebuild Template?}
    checkOptimize -->|No| checkRebuild

    checkRebuild -->|Yes| rebuildTemplate[Rebuild Template]
    checkRebuild -->|No| checkCallback{Callback Exists?}

    rebuildTemplate --> deleteFiles[Delete Template Files]
    deleteFiles --> createTemplateDb[Create Template DB]
    createTemplateDb --> openTemplateConn[Open Template Connection]
    openTemplateConn --> runBuildTemplate[Run buildTemplate Callback]
    runBuildTemplate --> checkCallbackAfterBuild{Callback Exists?}
    checkCallbackAfterBuild -->|Yes| runCallbackAfterBuild[Run Callback]
    checkCallbackAfterBuild -->|No| detachShrink[Detach and Shrink Template]
    runCallbackAfterBuild --> detachShrink
    detachShrink --> setTimestamp[Set Creation Timestamp]

    checkCallback -->|Yes| attachTemplate[Attach Template DB]
    attachTemplate --> openForCallback[Open Template Connection]
    openForCallback --> runCallback[Run Callback]
    runCallback --> detachTemplate[Detach Template DB]

    checkCallback -->|No| done[Done]
    detachTemplate --> done
    setTimestamp --> done
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
