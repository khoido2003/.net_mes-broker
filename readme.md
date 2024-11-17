# Message Broker

## Database

### Migrate Db

bash

```
dotnet ef migrations add ininitialMigration
```

### Update tool

bash

```
dotnet tool update --global dotnet-ef
```

### Commit DB

bash

```
dotnet ef database update
```
