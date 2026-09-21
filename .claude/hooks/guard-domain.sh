#!/usr/bin/env bash
# PreToolUse: запрещает тянуть внешние зависимости в *.Domain (AGENTS.md → Границы слоёв).
# Без jq: хук должен работать в Git Bash на Windows.
input=$(cat)

# Только файлы внутри каталога *.Domain (тестовые проекты *.Domain.UnitTests не трогаем).
echo "$input" | grep -qE '"file_path"[[:space:]]*:[[:space:]]*"[^"]*\.Domain[\\/]' || exit 0

if echo "$input" | grep -qE 'PackageReference|Microsoft\.EntityFrameworkCore|Confluent\.Kafka|Microsoft\.Extensions\.AI|System\.Text\.Json|Npgsql'; then
  echo "Заблокировано: в *.Domain нельзя добавлять внешние зависимости (EF, Kafka, AI, System.Text.Json, NuGet-пакеты). См. AGENTS.md, раздел «Границы слоёв». Вынеси это в *.Infrastructure." >&2
  exit 2
fi
exit 0
