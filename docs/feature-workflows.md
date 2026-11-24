# Feature workflows

Each workflow file contains one existing public operation. Its service keeps its
public identity, interfaces, routes, permissions and response contracts. Service
construction and dependencies stay in the service declaration file.

Each operation has an action folder, such as `Create/` or `Details/`.
Keep all existing filenames when organizing folders. The workflow stays directly
inside the action folder. All supporting files go in `Tasks/` with their
original filenames, even when there is only one task file.

```text
Batches/
    BatchService.cs
    Create/
        BatchesCreateWorkflow.cs
        Tasks/
            BatchesCreateValidation.cs
            BatchesCreatePreparation.cs
            BatchesCreatePersistence.cs
    Details/
        BatchesDetailsWorkflow.cs
        Tasks/
            BatchesDetailsTask.cs
```

The same action-folder layout applies to all sixteen operations in Batches,
Developers, Dashboard and Training. Existing filenames, namespaces, partial
service classes and shared helper ownership remain unchanged.

```mermaid
flowchart LR
    User[Authorized user] --> Read[View batches, developers, schedules and attendance]
    User --> Create[Create a batch or developer]
    User --> Assign[Assign developers]
    User --> Mark[Record attendance]
    User --> Report[View dashboard or download reports]
```

```mermaid
flowchart TD
    Start[Start an existing operation] --> Task[Call the next named task]
    Task --> Inspect[Read its Result and log values, reason and next action]
    Inspect --> Decision{Succeeded?}
    Decision -->|No| Failure[Return the failure outcome]
    Decision -->|Yes| More{Another task?}
    More -->|Yes| Task
    More -->|No| Success[Return the successful outcome]
```

Attendance recording preserves its existing failure precedence. Calendar lookup
and the class-day eligibility gate run before marking validation; valid markings
are required before checking the roster or preparing any writes. Each gate is a
separate task, and no task invokes the next stage.

```mermaid
flowchart TD
    Start[Record attendance] --> Calendar[Find the calendar date]
    Calendar --> Found{Date found?}
    Found -->|No| Failure[Return the existing failure reason]
    Found -->|Yes| Day[Check class-day eligibility]
    Day --> Allowed{Attendance allowed?}
    Allowed -->|No| Failure
    Allowed -->|Yes| Validate[Validate marking input]
    Validate --> Valid{Statuses and markings valid?}
    Valid -->|No| Failure
    Valid -->|Yes| Roster[Check each assigned developer has one marking]
    Roster --> Complete{Roster matches?}
    Complete -->|No| Failure
    Complete -->|Yes| Prepare[Prepare attendance changes]
    Prepare --> Save[Save once at the existing persistence boundary]
    Save --> Success[Return saved outcome]
```

Paged and downloadable attendance summaries reuse the same attendance criteria,
projection type and metric calculation. Those shared helpers retain their summary
owner instead of being copied into the report tasks. Unexpected failures keep the
existing service-boundary handling, and cancellation propagates.
