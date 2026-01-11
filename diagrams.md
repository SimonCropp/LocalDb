# LocalDb - Architecture Diagrams

This document contains Mermaid diagrams explaining how the LocalDb project works.

## 1. Architecture Overview

High-level view of the three NuGet packages and their relationships.

```mermaid
graph TB
    subgraph "User Test Code"
        TC[Test Class]
    end

    subgraph "NuGet Packages"
        subgraph "EfLocalDb"
            EFI[SqlInstance&lt;TDbContext&gt;]
            EFD[SqlDatabase&lt;TDbContext&gt;]
        end

        subgraph "EfClassicLocalDb"
            EF6I[SqlInstance&lt;TDbContext&gt;]
            EF6D[SqlDatabase&lt;TDbContext&gt;]
        end

        subgraph "LocalDb (Core)"
            SI[SqlInstance]
            SD[SqlDatabase]
            W[Wrapper]
            API[LocalDbApi]
        end
    end

    subgraph "SQL Server"
        LDB[(LocalDB Instance)]
    end

    TC --> EFI
    TC --> EF6I
    TC --> SI

    EFI --> W
    EF6I --> W
    SI --> W

    EFI --> EFD
    EF6I --> EF6D
    SI --> SD

    W --> API
    API -->|P/Invoke| LDB
```

## 2. Class Relationships

Key classes and their dependencies.

```mermaid
classDiagram
    class SqlInstance {
        -Wrapper wrapper
        -string name
        +Build(testFile, databaseSuffix) SqlDatabase
        +Build(dbName) SqlDatabase
        +Cleanup()
    }

    class SqlInstanceGeneric~TDbContext~ {
        -Wrapper wrapper
        -IModel Model
        -ConstructInstance~TDbContext~ constructInstance
        +Build(data, testFile) SqlDatabase~TDbContext~
        +BuildContext(data) TDbContext
    }

    class SqlDatabase {
        +string Name
        +SqlConnection Connection
        +string ConnectionString
        +OpenNewConnection() SqlConnection
        +Delete()
    }

    class SqlDatabaseGeneric~TDbContext~ {
        +TDbContext Context
        +TDbContext NoTrackingContext
        +AddData(entities)
        +RemoveData(entities)
        +Single~T~(predicate) T
        +NewDbContext() TDbContext
    }

    class Wrapper {
        -string name
        -string directory
        -byte[] templateMdf
        -byte[] templateLdf
        +Start(timestamp, buildTemplate)
        +CreateDatabaseFromTemplate(name)
        +DeleteDatabase(name)
    }

    class LocalDbApi {
        +CreateInstance(name)$
        +StartInstance(name)$
        +StopInstance(name)$
        +DeleteInstance(name)$
        +GetInstanceInfo(name)$
    }

    SqlInstance --> Wrapper : uses
    SqlInstanceGeneric --> Wrapper : uses
    SqlInstance --> SqlDatabase : creates
    SqlInstanceGeneric --> SqlDatabaseGeneric : creates
    Wrapper --> LocalDbApi : calls
    SqlDatabase --> SqlConnection : provides
    SqlDatabaseGeneric --> SqlDatabase : extends
    SqlDatabaseGeneric --> DbContext : provides
```

## 3. Database Creation Sequence

Step-by-step flow of creating a test database.

```mermaid
sequenceDiagram
    participant Test as Test Code
    participant SI as SqlInstance
    participant W as Wrapper
    participant API as LocalDbApi
    participant LDB as LocalDB
    participant FS as File System

    Test->>SI: new SqlInstance(name, buildTemplate)
    SI->>W: new Wrapper(name, directory)
    SI->>W: Start(timestamp, buildTemplate)

    W->>W: Check if instance exists

    alt Instance doesn't exist
        W->>API: CreateInstance(name)
        API->>LDB: Native CreateInstance
        W->>API: StartInstance(name)
        API->>LDB: Native StartInstance
    end

    W->>W: Check template cache validity

    alt Template needs rebuild
        W->>LDB: Open master connection
        W->>LDB: Execute optimization SQL
        W->>Test: Call buildTemplate delegate
        Test->>LDB: Create schema (EnsureCreated)
        W->>LDB: Detach template database
        W->>FS: Read template.mdf into memory
        W->>FS: Read template_log.ldf into memory
    end

    Test->>SI: Build(dbName)
    SI->>W: CreateDatabaseFromTemplate(dbName)
    W->>FS: Write template bytes as dbName.mdf
    W->>FS: Write template bytes as dbName_log.ldf
    W->>LDB: Attach database
    W-->>SI: Return connection string
    SI-->>Test: Return SqlDatabase
```

## 4. Instance Lifecycle State Machine

All possible states of a LocalDB instance.

```mermaid
stateDiagram-v2
    [*] --> NonExistent

    NonExistent --> Creating: CreateInstance()
    Creating --> Created: Success
    Creating --> NonExistent: Failure

    Created --> Starting: StartInstance()
    Starting --> Running: Success
    Starting --> Created: Failure

    Running --> BuildingTemplate: First Build() call
    BuildingTemplate --> TemplateReady: Template created

    TemplateReady --> CloningDatabase: Build(dbName)
    CloningDatabase --> TemplateReady: Database ready

    Running --> Stopping: StopInstance()
    TemplateReady --> Stopping: StopInstance()
    Stopping --> Created: Success

    Created --> Deleting: DeleteInstance()
    Running --> Deleting: Cleanup()
    TemplateReady --> Deleting: Cleanup()
    Deleting --> NonExistent: Success

    NonExistent --> [*]
```

## 5. Template Caching Flow

Decision tree for cache invalidation and template management.

```mermaid
flowchart TD
    A[Start Build] --> B{Instance exists?}
    B -->|No| C[Create LocalDB Instance]
    B -->|Yes| D{Instance running?}

    C --> E[Start Instance]
    D -->|No| F[Delete & Recreate]
    D -->|Yes| G{Template MDF exists?}

    F --> C
    E --> G

    G -->|No| H[Build Template]
    G -->|Yes| I{Timestamp matches?}

    I -->|No| J[Delete old template]
    I -->|Yes| K[Use cached template]

    J --> H

    H --> L[Execute buildTemplate delegate]
    L --> M[Run schema migrations]
    M --> N[Detach template DB]
    N --> O[Cache MDF/LDF in memory]
    O --> K

    K --> P[Clone template to new DB]
    P --> Q[Write MDF/LDF to disk]
    Q --> R[Attach new database]
    R --> S[Return SqlDatabase]
```

## 6. File System Organization

How databases are stored on disk.

```mermaid
graph TB
    subgraph "LocalDB Data Root"
        subgraph "%TEMP%/LocalDb"
            subgraph "Instance1/"
                T1[template.mdf]
                T1L[template_log.ldf]
                DB1[TestClass_Test1.mdf]
                DB1L[TestClass_Test1_log.ldf]
                DB2[TestClass_Test2.mdf]
                DB2L[TestClass_Test2_log.ldf]
            end

            subgraph "Instance2/"
                T2[template.mdf]
                T2L[template_log.ldf]
                DB3[OtherTest.mdf]
                DB3L[OtherTest_log.ldf]
            end
        end
    end

    style T1 fill:#f9f,stroke:#333
    style T1L fill:#f9f,stroke:#333
    style T2 fill:#f9f,stroke:#333
    style T2L fill:#f9f,stroke:#333
```

## 7. EF Core Integration Data Flow

How data moves through the EF Core integration layer.

```mermaid
flowchart LR
    subgraph "Test Layer"
        TC[Test Code]
        E[Entities]
    end

    subgraph "EfLocalDb Layer"
        SI[SqlInstance&lt;TDbContext&gt;]
        SD[SqlDatabase&lt;TDbContext&gt;]
        CTX[DbContext]
        NCTX[NoTrackingContext]
    end

    subgraph "LocalDb Core"
        W[Wrapper]
        API[LocalDbApi]
    end

    subgraph "Database"
        LDB[(LocalDB)]
    end

    TC -->|Build with data| SI
    SI -->|CreateDatabaseFromTemplate| W
    W -->|P/Invoke| API
    API -->|Native calls| LDB

    SI -->|Returns| SD
    SD -->|Provides| CTX
    SD -->|Provides| NCTX

    TC -->|AddData| SD
    SD -->|SaveChanges| CTX
    CTX -->|SQL| LDB

    E -->|Seed data| SD

    TC -->|Query| NCTX
    NCTX -->|SQL| LDB
```

## 8. Package Dependencies

External and internal dependencies between packages.

```mermaid
graph BT
    subgraph "External Dependencies"
        MSDC[Microsoft.Data.SqlClient]
        EFCS[Microsoft.EntityFrameworkCore.SqlServer]
        EF6[EntityFramework 6.x]
    end

    subgraph "LocalDb Packages"
        LDB[LocalDb<br/>Core Package]
        EFLDB[EfLocalDb<br/>EF Core Package]
        EF6LDB[EfClassicLocalDb<br/>EF6 Package]
    end

    LDB --> MSDC
    EFLDB --> LDB
    EFLDB --> EFCS
    EF6LDB --> LDB
    EF6LDB --> EF6

    subgraph "Native Layer"
        SQLAPI[SQL Server LocalDB<br/>Native API]
    end

    LDB -->|P/Invoke| SQLAPI
```
