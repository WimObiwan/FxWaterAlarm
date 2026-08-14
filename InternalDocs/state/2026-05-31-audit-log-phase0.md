# Audit Log - Phase 0 (Functional and Technical Baseline)

Status: Draft for approval
Scope: End-user actions first, extendable to admins and tooling later

## 1) Goal of Phase 0

Lock the non-code design decisions so implementation can start without rework:
- Canonical audit event schema
- Canonical action catalog and naming
- Retention, rotation, and storage policy
- Acceptance criteria for Phase 1 implementation

## 2) Canonical Audit Event Schema

Storage format: JSON Lines (one JSON document per line)
Encoding: UTF-8
Timestamp: UTC only, ISO-8601 (e.g. 2026-05-24T14:53:21.123Z)

### 2.1 Required top-level fields

- timestampUtc: Event timestamp in UTC
- correlationId: Request/action correlation id
- outcome: Attempted | Succeeded | Denied | Failed
- action: Stable action name (see action catalog)
- actor: Who did it
- client: Client metadata
- target: WaterAlarm domain targets
- changes: Old/new values (if applicable)
- details: Optional extra context

### 2.2 Field specification

- timestampUtc (string, required)
- correlationId (string, required)
- outcome (string enum, required)
- action (string, required)
- actor (object, required)
  - identity (string, required): login email or subject-like identifier
  - authType (string, required): Cookie | ApiKey | Anonymous | AdminTool
  - isAdmin (boolean, required)
- client (object, required)
  - ipAddress (string, required)
  - userAgent (string, optional)
  - requestPath (string, optional)
- target (object, required)
  - accountUid (string GUID, optional)
  - accountLink (string, optional)
  - sensorUid (string GUID, optional)
  - sensorLink (string, optional)
  - devEui (string, optional)
- changes (array, optional)
  - Each item:
    - entity (string, required)
    - key (object, required)
    - property (string, required)
    - oldValue (any scalar/json, nullable)
    - newValue (any scalar/json, nullable)
- details (object, optional)
  - reason (string, optional)
  - message (string, optional)
  - exceptionType (string, optional)
  - validationErrors (array of strings, optional)

## 3) Action Catalog (v1)

Naming rule: <Aggregate>.<Verb>

### 3.1 End-user web actions

- AccountSensor.UpdateSettings
- AccountSensorAlarm.Add
- AccountSensorAlarm.Update
- AccountSensorAlarm.Delete
- Account.AddSensor
- AccountUser.AddMailUser
- AccountUser.RemoveUser
- AccountUser.ChangeDefaultEmail
- AccountSensor.TestMailAlert

### 3.2 API actions

- Measurement.AddViaApiKey
- Measurement.Delete

### 3.3 Admin extension actions (reserved for later)

- Admin.Account.Create
- Admin.Account.Open
- Admin.AccountSensor.RemoveLink
- Admin.Sensor.Create
- Admin.Tool.Command

## 4) Old/New Value Rules

- For updates: include one changes row per modified property
- For creates: oldValue = null, newValue = set value
- For deletes: oldValue = previous value, newValue = null
- For failed/denied before persistence: changes may be omitted; include details.reason

Examples:
- AccountSensor.Name changed from "Tank A" to "Tank North"
- Account.Email changed from "old@x" to "new@x"

## 5) Storage and Retention Policy

### 5.1 File location and naming

- Base folder: logs/audit/
- Filename pattern: audit-YYYY-MM-DD.jsonl
- One active file per UTC day

### 5.2 Rotation and retention

- Rotate daily at UTC date boundary
- Retain 365 days online by default
- Compress files older than 30 days (gzip)
- Optional archive export for long-term storage (out of scope Phase 1)

### 5.3 Integrity and safety

- Append-only writes
- Never log secrets (API keys, auth tokens, raw password-like values)
- For API key auth, store key fingerprint only (not full value)

## 6) Outcome and Error Semantics

- Attempted: action entered business flow
- Succeeded: persisted and completed successfully
- Denied: authorization/permission refusal
- Failed: exception/validation failure

When Denied/Failed:
- Include details.reason
- Include details.exceptionType where relevant

## 7) Non-functional Requirements

- Audit logging must never block core business operation
- On audit write failure, business action continues and fallback app log is emitted
- Audit payload serialization failures should degrade safely and include a minimal fallback event

## 8) Acceptance Criteria for Phase 0 Completion

- Action catalog approved
- Schema approved (required fields, enums, and old/new rules)
- Retention/rotation policy approved
- Explicit decision on JSONL file path accepted

## 9) Example Events

### 9.1 Successful update with old/new values

{
  "timestampUtc": "2026-05-24T14:53:21.123Z",
  "correlationId": "6f2b6e10-24f1-45f8-a86d-d65e9e2183b3",
  "outcome": "Succeeded",
  "action": "AccountSensor.UpdateSettings",
  "actor": {
    "identity": "user@example.com",
    "authType": "Cookie",
    "isAdmin": false
  },
  "client": {
    "ipAddress": "203.0.113.10",
    "userAgent": "Mozilla/5.0",
    "requestPath": "/a/abc/s/def"
  },
  "target": {
    "accountUid": "3d05eb3e-6b20-4f90-b5b8-e167f7a78ad8",
    "sensorUid": "f9d5bcc0-4efa-4b94-a028-2ec3bbff8ab5"
  },
  "changes": [
    {
      "entity": "AccountSensor",
      "key": { "accountUid": "3d05eb3e-6b20-4f90-b5b8-e167f7a78ad8", "sensorUid": "f9d5bcc0-4efa-4b94-a028-2ec3bbff8ab5" },
      "property": "Name",
      "oldValue": "Tank A",
      "newValue": "Tank North"
    }
  ]
}

### 9.2 Denied action

{
  "timestampUtc": "2026-05-24T15:01:40.000Z",
  "correlationId": "7e24c274-a3a9-47b0-b4a4-e6a7d9a220d8",
  "outcome": "Denied",
  "action": "Measurement.Delete",
  "actor": {
    "identity": "user@example.com",
    "authType": "Cookie",
    "isAdmin": false
  },
  "client": {
    "ipAddress": "203.0.113.10",
    "requestPath": "/api/a/x/s/y/m"
  },
  "target": {
    "accountLink": "x",
    "sensorLink": "y"
  },
  "details": {
    "reason": "Admin access required"
  }
}

## 10) Open Decisions to Confirm

1. Keep 365-day retention, or use a different period?
   -- > ok for 365 days, but configurable in case we need to adjust later
2. Keep logs under logs/audit/ in app working directory, or move to configurable absolute path?
    -- > ok for logs/audit/ for now, but configurable in case we want to move later
3. Include read-only actions (view/open/export) now, or only mutating actions?
    -- > only mutating actions for Phase 1, can add read-only later if needed
4. Store full userAgent by default, or truncate/hash it?
    -- > store full userAgent

## 11) Phase 1 - Sample JSONL Lines (Implementation)

Below are representative single-line JSON records that match the current Phase 1 implementation.

### 11.1 AccountSensor.UpdateSettings (attempt)

{"timestampUtc":"2026-05-24T16:20:10.103Z","correlationId":"0HNJ6LF6Q55AN:00000007","outcome":"Attempted","action":"AccountSensor.UpdateSettings","actor":{"identity":"user@example.com","authType":"Cookie","isAdmin":false},"client":{"ipAddress":"203.0.113.10","userAgent":"Mozilla/5.0","requestPath":"/a/abc/s/def"},"target":{"accountLink":"abc","sensorLink":"def"}}

### 11.2 AccountSensor.UpdateSettings (persisted changes)

{"timestampUtc":"2026-05-24T16:20:10.130Z","correlationId":"0HNJ6LF6Q55AN:00000007","outcome":"Succeeded","action":"AccountSensor.UpdateSettings","actor":{"identity":"user@example.com","authType":"Cookie","isAdmin":false},"client":{"ipAddress":"203.0.113.10","userAgent":"Mozilla/5.0","requestPath":"/a/abc/s/def"},"target":{"accountLink":"abc","sensorLink":"def"},"changes":[{"entity":"AccountSensor","key":{"accountUid":"3d05eb3e-6b20-4f90-b5b8-e167f7a78ad8","sensorUid":"f9d5bcc0-4efa-4b94-a028-2ec3bbff8ab5"},"property":"Name","oldValue":"Tank A","newValue":"Tank North"},{"entity":"AccountSensor","key":{"accountUid":"3d05eb3e-6b20-4f90-b5b8-e167f7a78ad8","sensorUid":"f9d5bcc0-4efa-4b94-a028-2ec3bbff8ab5"},"property":"AlertsEnabled","oldValue":false,"newValue":true}],"details":{"message":"Entity changes persisted"}}

### 11.3 AccountSensor.UpdateSettings (action success)

{"timestampUtc":"2026-05-24T16:20:10.132Z","correlationId":"0HNJ6LF6Q55AN:00000007","outcome":"Succeeded","action":"AccountSensor.UpdateSettings","actor":{"identity":"user@example.com","authType":"Cookie","isAdmin":false},"client":{"ipAddress":"203.0.113.10","userAgent":"Mozilla/5.0","requestPath":"/a/abc/s/def"},"target":{"accountLink":"abc","sensorLink":"def"}}

### 11.4 Measurement.Delete (denied)

{"timestampUtc":"2026-05-24T16:22:41.221Z","correlationId":"0HNJ6LF6Q55AN:00000009","outcome":"Denied","action":"Measurement.Delete","actor":{"identity":"user@example.com","authType":"Cookie","isAdmin":false},"client":{"ipAddress":"203.0.113.10","userAgent":"Mozilla/5.0","requestPath":"/api/a/abc/s/def/m"},"target":{"accountLink":"abc","sensorLink":"def"},"details":{"reason":"Admin access required"}}

### 11.5 Measurement.AddViaApiKey (failed)

{"timestampUtc":"2026-05-24T16:25:03.604Z","correlationId":"0HNJ6LF6Q55AN:0000000B","outcome":"Failed","action":"Measurement.AddViaApiKey","actor":{"identity":"authenticated","authType":"ApiKey","isAdmin":false},"client":{"ipAddress":"198.51.100.20","userAgent":"curl/8.5.0","requestPath":"/api/deveui/70B3D57ED006BEEF/Measurements"},"target":{"devEui":"70B3D57ED006BEEF"},"details":{"reason":"Invalid operation","message":"Sensor with DevEUI '70B3D57ED006BEEF' not found","exceptionType":"InvalidOperationException"}}

Note:
- For one user action, multiple audit lines are expected (for example Attempted, persisted entity changes, and final Succeeded).
- The persisted-entity line carries old/new values under `changes`.

