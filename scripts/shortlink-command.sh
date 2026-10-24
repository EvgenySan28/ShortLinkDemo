#!/usr/bin/env bash
set -euo pipefail

command="${1:-}"
if [ "$command" != migrate ] && [ "$command" != check-migrations ]; then
  echo "Usage: shortlink-command.sh <migrate|check-migrations>" >&2
  exit 1
fi

: "${POSTGRES_CONNECTION_STRING:?POSTGRES_CONNECTION_STRING is required}"
export ConnectionStrings__Postgres="${POSTGRES_CONNECTION_STRING};GSS Encryption Mode=Disable"

docker run --rm \
  --network postgres \
  --user "$(id -u):$(id -g)" \
  --env ConnectionStrings__Postgres \
  --env "EF_COMMAND=$command" \
  --env HOME=/tmp/home \
  --env DOTNET_CLI_HOME=/tmp/home \
  --env NUGET_PACKAGES=/tmp/nuget \
  --env DOTNET_NOLOGO=1 \
  --env DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  --volume "$PWD":/src \
  --workdir /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -euo pipefail -c '
    mkdir -p "$HOME" "$NUGET_PACKAGES"
    dotnet tool restore
    dotnet restore Data/Data.csproj
    args=(
      --project Data/Data.csproj
      --startup-project Data/Data.csproj
      --no-color
    )
    if [ "$EF_COMMAND" = migrate ]; then
      exec dotnet ef database update "${args[@]}"
    fi
    list="$(dotnet ef migrations list "${args[@]}")"
    printf "%s\n" "$list"
    if printf "%s\n" "$list" | grep -q "(Pending)"; then
      echo "Есть миграции со статусом Pending. Сначала запусти workflow «Накатить миграции»." >&2
      exit 1
    fi
  '
