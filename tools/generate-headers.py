#!/usr/bin/env python3
"""Writes the Cloudflare Pages `_headers` file into a Blazor publish output.

Usage: python3 tools/generate-headers.py [publish_wwwroot_dir]
Default dir: bin/Release/net10.0/publish/wwwroot

Must run after every publish: the inline <script type="importmap"> is filled with fingerprinted
asset names at publish time, so its CSP hash changes whenever any asset changes.
"""
import base64
import hashlib
import os
import re
import sys

WEBROOT = sys.argv[1] if len(sys.argv) > 1 else "bin/Release/net10.0/publish/wwwroot"

html = open(os.path.join(WEBROOT, "index.html"), encoding="utf-8").read()
inline_scripts = re.findall(r"<script(?![^>]*\bsrc=)[^>]*>(.*?)</script>", html, re.DOTALL)
script_hashes = " ".join(
    f"'sha256-{base64.b64encode(hashlib.sha256(body.encode('utf-8')).digest()).decode()}'"
    for body in inline_scripts
)

# 'wasm-unsafe-eval' is required to instantiate the .NET runtime; 'unsafe-inline' styles are
# required because Blazor.Diagrams positions nodes through style attributes.
csp = "; ".join([
    "default-src 'self'",
    f"script-src 'self' 'wasm-unsafe-eval' {script_hashes}".rstrip(),
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data:",
    "font-src 'self'",
    "connect-src 'self'",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'none'",
    "frame-ancestors 'none'",
])

headers = f"""/*
  Content-Security-Policy: {csp}
  X-Frame-Options: DENY
  Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=(), usb=()

/_framework/*
  Cache-Control: public, max-age=31536000, immutable
"""

with open(os.path.join(WEBROOT, "_headers"), "w", encoding="utf-8") as f:
    f.write(headers)

print(f"Wrote {os.path.join(WEBROOT, '_headers')} ({len(inline_scripts)} inline script hash(es))")
