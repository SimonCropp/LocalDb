# LocalDB Wrapper Workflow

This document describes how `Wrapper.cs` manages LocalDB instances.

## Instance Startup Flow

```mermaid
flowchart TD
    A[Start] --> B[InnerStart]
    B --> C{Instance Exists?}

    C -->|No| D[CleanStart]
    C -->|Yes| E{Instance Running?}

    E -->|No| F[Delete Instance]
    F --> D

    E -->|Yes| G{Data File Exists?}

    G -->|No| H[Stop and Delete Instance]
    H --> D

    G -->|Yes| I{Timestamp Match?}

    I -->|Yes| J[Skip Rebuild]
    J --> K[CreateAndDetachTemplate<br/>rebuildTemplate: false<br/>optimizeModelDb: false]

    I -->|No| L[CreateAndDetachTemplate<br/>rebuildTemplate: true<br/>optimizeModelDb: false]

    D --> M[Flush Directory]
    M --> N[Create Instance]
    N --> O[Start Instance]
    O --> P[CreateAndDetachTemplate<br/>rebuildTemplate: true<br/>optimizeModelDb: true]
```

## CreateAndDetachTemplate Flow

```mermaid
flowchart TD
    A[CreateAndDetachTemplate] --> B[Open Master Connection]
    B --> C{Optimize ModelDb?}

    C -->|Yes| D[Execute Optimize ModelDb Command]
    D --> E{Rebuild Template?}
    C -->|No| E

    E -->|Yes| F{Template Provided?}
    F -->|No| G[Rebuild Template]
    F -->|Yes| H{Callback Exists?}

    E -->|No| H

    G --> I[Delete Template Files]
    I --> J[Create Template Database]
    J --> K[Mark Files Writable]
    K --> L[Open Template Connection]
    L --> M[Run buildTemplate Callback]
    M --> N{Callback Exists?}
    N -->|Yes| O[Run Callback]
    N -->|No| P[Detach and Shrink Template]
    O --> P
    P --> Q[Set Creation Timestamp]

    H -->|Yes| R[Attach Template Database]
    R --> S[Open Template Connection]
    S --> T[Run Callback]
    T --> U[Detach Template Database]

    H -->|No| V[Done]
    U --> V
    Q --> V
```

## Create Database From Template Flow

```mermaid
flowchart TD
    A[CreateDatabaseFromTemplate] --> B{Name = 'template'?}
    B -->|Yes| C[Throw Exception]
    B -->|No| D{Valid Filename?}
    D -->|No| E[Throw ArgumentException]
    D -->|Yes| F[Build File Paths]
    F --> G[Await Startup Task]
    G --> H[Open Master Connection]
    H --> I[Take DB Offline if exists]
    I --> J[Copy Data File]
    I --> K[Copy Log File]
    J --> L[Mark Files Writable]
    K --> L
    L --> M[Create or Make Online]
    M --> N[Open New Connection]
    N --> O[Return Connection]
```

