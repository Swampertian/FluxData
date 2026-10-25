#!/usr/bin/env python3
"""Static file server for testing the Blazor WASM Release/AOT publish output locally.

Usage: python3 tools/serve-aot.py [publish_wwwroot_dir] [port]
Default dir: bin/Release/net10.0/publish/wwwroot, default port: 8099
"""
import http.server
import functools
import os
import sys

WEBROOT = sys.argv[1] if len(sys.argv) > 1 else "bin/Release/net10.0/publish/wwwroot"
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8099


class CoopCoepHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".wasm"):
            return "application/wasm"
        return super().guess_type(path)

    def translate_path(self, path):
        translated = super().translate_path(path)
        if not os.path.isfile(translated):
            return super().translate_path("/index.html")
        return translated


Handler = functools.partial(CoopCoepHandler, directory=WEBROOT)

with http.server.ThreadingHTTPServer(("0.0.0.0", PORT), Handler) as httpd:
    print(f"Serving {WEBROOT} on http://localhost:{PORT} (COOP/COEP enabled)")
    httpd.serve_forever()
