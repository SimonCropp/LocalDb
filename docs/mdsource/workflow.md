# LocalDB Wrapper Workflow

This document describes how `Wrapper.cs` manages LocalDB instances.

## Instance Startup Flow

```mermaid
flowchart TD
    start[Start] --> innerStart[InnerStart]
    innerStart --> checkExists{Instance Exists?}

    checkExists -->|No| cleanStart[CleanStart]
    checkExists -->|Yes| checkRunning{Instance Running?}

    checkRunning -->|No| deleteInstance[Delete Instance]
    deleteInstance --> cleanStart

    checkRunning -->|Yes| checkDataFile{Data File Exists?}

    checkDataFile -->|No| stopAndDelete[Stop and Delete Instance]
    stopAndDelete --> cleanStart

    checkDataFile -->|Yes| checkTimestamp{Timestamp Match?}

    checkTimestamp -->|Yes| createNoRebuild[CreateAndDetachTemplate<br/>rebuildTemplate: false<br/>optimizeModelDb: false]

    checkTimestamp -->|No| createRebuild[CreateAndDetachTemplate<br/>rebuildTemplate: true<br/>optimizeModelDb: false]

    cleanStart --> flushDir[Flush Directory]
    flushDir --> createInstance[Create Instance]
    createInstance --> startInstance[Start Instance]
    startInstance --> createFull[CreateAndDetachTemplate<br/>rebuildTemplate: true<br/>optimizeModelDb: true]
```

## CreateAndDetachTemplate Flow

```mermaid
flowchart TD
    entry[CreateAndDetachTemplate] --> openMaster[Open Master Connection]
    openMaster --> checkOptimize{Optimize ModelDb?}

    checkOptimize -->|Yes| executeOptimize[Execute Optimize ModelDb Command]
    executeOptimize --> checkRebuild{Rebuild Template?}
    checkOptimize -->|No| checkRebuild

    checkRebuild -->|Yes| checkProvided{Template Provided?}
    checkProvided -->|No| rebuildTemplate[Rebuild Template]
    checkProvided -->|Yes| checkCallback{Callback Exists?}

    checkRebuild -->|No| checkCallback

    rebuildTemplate --> deleteFiles[Delete Template Files]
    deleteFiles --> createTemplateDb[Create Template Database]
    createTemplateDb --> markWritable[Mark Files Writable]
    markWritable --> openTemplateConn[Open Template Connection]
    openTemplateConn --> runBuildTemplate[Run buildTemplate Callback]
    runBuildTemplate --> checkCallbackAfterBuild{Callback Exists?}
    checkCallbackAfterBuild -->|Yes| runCallbackAfterBuild[Run Callback]
    checkCallbackAfterBuild -->|No| detachShrink[Detach and Shrink Template]
    runCallbackAfterBuild --> detachShrink
    detachShrink --> setTimestamp[Set Creation Timestamp]

    checkCallback -->|Yes| attachTemplate[Attach Template Database]
    attachTemplate --> openForCallback[Open Template Connection]
    openForCallback --> runCallback[Run Callback]
    runCallback --> detachTemplate[Detach Template Database]

    checkCallback -->|No| done[Done]
    detachTemplate --> done
    setTimestamp --> done
```

## Create Database From Template Flow

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
    copyData --> markWritable[Mark Files Writable]
    copyLog --> markWritable
    markWritable --> createOrOnline[Create or Make Online]
    createOrOnline --> openNewConn[Open New Connection]
    openNewConn --> returnConn[Return Connection]
```

## Delete Database Flow

```mermaid
flowchart TD
    entry[DeleteDatabase] --> buildCommand[Build Delete Command]
    buildCommand --> openMaster[Open Master Connection]
    openMaster --> executeDelete[Execute Delete Command]
    executeDelete --> deleteDataFile[Delete Data File]
    deleteDataFile --> deleteLogFile[Delete Log File]
```

## Delete Instance Flow

```mermaid
flowchart TD
    entry[DeleteInstance] --> stopInstance[Stop LocalDB Instance]
    stopInstance --> deleteInstance[Delete LocalDB Instance]
    deleteInstance --> deleteDir[Delete Directory]
    deleteDir --> disposeSemaphore[Dispose Semaphore]
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
