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
 * Storage → Each instance stores its .mdf and .ldf files in a subfolder under `%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances\`


## How this project works

### Instance Startup Flow

This flow happens once per `SqlInstance`, usually once before any tests run.

```mermaid
flowchart TD

    start[Start]
    checkExists{Instance<br>Exists?}
    checkRunning{Instance<br>Running?}
    deleteInstance[Delete Instance]
    checkDataFile{Data File Exists?}
    stopAndDelete[Stop & Delete Instance]
    cleanDir[Clean Directory]
    checkTimestamp{Timestamp<br>Match?}
    checkCallback{Callback<br>Exists?}
    createInstance[Create Instance]

    subgraph openMasterForNewBox[Open Master Connection]
        optimizeModel[Optimize Model DB]
        rebuildTemplate
        deleteFiles[Delete Template Files]
        createTemplateDb[Create Template DB]
        subgraph openTemplateForNewBox[Open Template Connection]
            runBuildTemplate[Run buildTemplate]
            checkCallbackAfterBuild{Callback<br>Exists?}
            runCallbackAfterBuild[Run Callback]
        end
        detachShrink[Shrink & Detach Template]
    end
    setTimestamp[Set Creation Timestamp]

    subgraph openMasterForExistingBox[Open Master Connection]
        attachTemplate[Attach Template DB]
        subgraph openTemplateForExistingBox[Open Template Connection]
            runCallback[Run Callback]
        end
        detachTemplate[Detach Template DB]
    end


    done[Done]

    start --> checkExists

    checkExists -->|No| cleanDir
    checkExists -->|Yes| checkRunning

    checkRunning -->|No| deleteInstance
    deleteInstance --> cleanDir

    checkRunning -->|Yes| checkDataFile

    checkDataFile -->|No| stopAndDelete
    stopAndDelete --> cleanDir

    checkDataFile -->|Yes| checkTimestamp

    checkTimestamp -->|No| rebuildTemplate

    checkTimestamp -->|Yes| checkCallback
    cleanDir --> createInstance
    createInstance --> optimizeModel
    optimizeModel --> rebuildTemplate
    rebuildTemplate --> deleteFiles
    deleteFiles --> createTemplateDb
    createTemplateDb --> runBuildTemplate
    runBuildTemplate --> checkCallbackAfterBuild
    checkCallbackAfterBuild -->|Yes| runCallbackAfterBuild
    checkCallbackAfterBuild -->|No| detachShrink
    runCallbackAfterBuild --> detachShrink
    detachShrink --> setTimestamp

    checkCallback -->|Yes| attachTemplate
    attachTemplate --> runCallback
    runCallback --> detachTemplate

    checkCallback -->|No| done
    detachTemplate --> done
    setTimestamp --> done
```


### Create DB From Template Flow

This happens once per `SqlInstance.Build`, usually once per test method.

```mermaid
flowchart TD
    entry[CreateDatabaseFromTemplate]
    entry --> openMaster[Open Master Connection]
    openMaster --> takeOffline[Take DB Offline if exists]
    takeOffline --> copyFiles[Copy Data & Log Files]
    copyFiles --> createOrOnline[Create or Make Online]
    createOrOnline --> openNewConn[Open New Connection]
    openNewConn --> returnConn[Return Connection]
```
