#!/usr/bin/env bash
# Clean Release/AOT publish ready for Cloudflare Pages: stale fingerprinted assets from earlier
# publishes are wiped, and _headers is regenerated for the new importmap hash.
set -euo pipefail
cd "$(dirname "$0")/.."

rm -rf bin obj
dotnet publish -c Release
python3 tools/generate-headers.py

echo
echo "Deploy: npx wrangler@3 pages deploy bin/Release/net10.0/publish/wwwroot --project-name fooglud --branch main"
