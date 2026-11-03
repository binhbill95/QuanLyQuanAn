$ErrorActionPreference = 'Stop'

$databaseName = 'RestaurantManagement_AutomationTest'
$containerName = 'restaurant-sqlserver'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$testProject = Join-Path $repositoryRoot 'src\RestaurantManagement.Tests\RestaurantManagement.Tests.csproj'
$tempDirectory = Join-Path $env:TEMP ("$databaseName-" + [Guid]::NewGuid().ToString('N'))

try {
    $running = docker inspect $containerName --format '{{.State.Running}}'
    if ($LASTEXITCODE -ne 0 -or $running -ne 'true') {
        throw "Docker container '$containerName' must be running."
    }

    $passwordLine = docker inspect $containerName --format '{{range .Config.Env}}{{println .}}{{end}}' |
        Where-Object { $_ -like 'MSSQL_SA_PASSWORD=*' } |
        Select-Object -First 1
    if (-not $passwordLine) {
        throw 'Could not obtain the SQL Server credential from the running container environment.'
    }
    $password = $passwordLine.Substring('MSSQL_SA_PASSWORD='.Length)

    $portMapping = docker port $containerName 1433/tcp | Select-Object -First 1
    if ($LASTEXITCODE -ne 0 -or -not $portMapping) {
        throw 'Could not determine the published SQL Server port.'
    }
    $port = ($portMapping -split ':')[-1]

    New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null
    foreach ($script in Get-ChildItem $PSScriptRoot -Filter '*.sql' | Sort-Object Name) {
        $content = [IO.File]::ReadAllText($script.FullName).Replace('RestaurantManagement', $databaseName)
        $copyPath = Join-Path $tempDirectory $script.Name
        [IO.File]::WriteAllText($copyPath, $content, [Text.UTF8Encoding]::new($false))
        $containerPath = "/tmp/$databaseName-$($script.Name)"
        docker cp $copyPath "${containerName}:$containerPath" | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not copy $($script.Name) into the SQL Server container."
        }
    }

    foreach ($name in @('01_schema.sql', '02_seed.sql', '03_views.sql', '04_audit.sql', '05_rules.sql')) {
        $containerPath = "/tmp/$databaseName-$name"
        docker exec -e "SQL_FILE=$containerPath" $containerName /bin/sh -c 'SQLCMD=/opt/mssql-tools18/bin/sqlcmd; [ -x "$SQLCMD" ] || SQLCMD=/opt/mssql-tools/bin/sqlcmd; "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -I -b -i "$SQL_FILE"'
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to apply $name to the isolated test database."
        }
    }

    $env:RESTAURANT_TEST_CONNECTION_STRING = "Server=127.0.0.1,$port;Database=$databaseName;User Id=sa;Password=$password;TrustServerCertificate=True;Encrypt=False"
    dotnet test $testProject
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Remove-Item Env:\RESTAURANT_TEST_CONNECTION_STRING -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}
