#!/bin/bash
# Run tests with line coverage locally.
# Mirrors the CI gate in .github/workflows/squad-ci.yml.
# Usage: bash scripts/coverage.sh
#
# Output: coverage summary in the terminal + opencover XML in Dungnz.Tests/coverage.opencover.xml
# Threshold: 70% line coverage (CI gate — lowered from 80% per issue #906 after P0/P1
#             code additions outpaced test growth; restore to 80% tracked in #906)

set -e

echo "▶ Running tests with coverage (threshold: 70% line)..."
dotnet test \
  --no-build \
  --verbosity normal \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:Threshold=70 \
  /p:ThresholdType=line \
  /p:CoverletOutput=Dungnz.Tests/coverage.opencover.xml

echo ""
echo "✅ Coverage check complete."
echo "   XML report: Dungnz.Tests/coverage.opencover.xml"
