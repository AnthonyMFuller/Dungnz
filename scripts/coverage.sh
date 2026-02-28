#!/bin/bash
# Run tests with line coverage locally.
# Mirrors the CI gate in .github/workflows/squad-ci.yml.
# Usage: bash scripts/coverage.sh
#
# Output: coverage summary in the terminal + opencover XML in Dungnz.Tests/coverage.opencover.xml
# Threshold: 80% line coverage (Anthony directive)

set -e

echo "▶ Running tests with coverage (threshold: 80% line)..."
dotnet test \
  --no-build \
  --verbosity normal \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:Threshold=80 \
  /p:ThresholdType=line \
  /p:CoverletOutput=Dungnz.Tests/coverage.opencover.xml

echo ""
echo "✅ Coverage check complete."
echo "   XML report: Dungnz.Tests/coverage.opencover.xml"
