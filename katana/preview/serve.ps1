# Минимальный статический сервер для превью (без зависимостей).
# Запуск:  powershell -ExecutionPolicy Bypass -File .\serve.ps1
# Открыть: http://127.0.0.1:8123/katana/preview/
param(
    [int]$Port = 8123,
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
$listener.Start()
Write-Host "Serving $Root"
Write-Host "http://127.0.0.1:$Port/katana/preview/"

while ($true) {
    $client = $listener.AcceptTcpClient()
    $stream = $null
    try {
        $stream = $client.GetStream()
        $reader = [System.IO.StreamReader]::new(
            $stream, [System.Text.Encoding]::ASCII, $false, 1024, $true)
        $requestLine = $reader.ReadLine()
        if ([string]::IsNullOrWhiteSpace($requestLine)) { continue }

        do { $header = $reader.ReadLine() } while ($header -ne $null -and $header -ne '')

        $parts = $requestLine.Split(' ')
        $path = if ($parts.Length -ge 2) { $parts[1].Split('?')[0] } else { '/' }
        $path = [System.Uri]::UnescapeDataString($path)
        if ($path -eq '/') { $path = '/katana/preview/' }
        if ($path.EndsWith('/')) { $path += 'index.html' }

        $full = [System.IO.Path]::GetFullPath((Join-Path $Root $path.TrimStart('/')))
        $rootFull = [System.IO.Path]::GetFullPath($Root)

        if (-not $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
            $status = 'HTTP/1.1 403 Forbidden'
            $body = [System.Text.Encoding]::UTF8.GetBytes('403')
            $mime = 'text/plain'
        }
        elseif (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
            $status = 'HTTP/1.1 404 Not Found'
            $body = [System.Text.Encoding]::UTF8.GetBytes("404: $path")
            $mime = 'text/plain; charset=utf-8'
        }
        else {
            $status = 'HTTP/1.1 200 OK'
            $body = [System.IO.File]::ReadAllBytes($full)
            $mime = switch ([System.IO.Path]::GetExtension($full).ToLowerInvariant()) {
                '.html' { 'text/html; charset=utf-8' }
                '.js'   { 'text/javascript; charset=utf-8' }
                '.mjs'  { 'text/javascript; charset=utf-8' }
                '.css'  { 'text/css; charset=utf-8' }
                '.json' { 'application/json; charset=utf-8' }
                '.png'  { 'image/png' }
                '.jpg'  { 'image/jpeg' }
                '.jpeg' { 'image/jpeg' }
                '.svg'  { 'image/svg+xml' }
                '.glb'  { 'model/gltf-binary' }
                '.gltf' { 'model/gltf+json' }
                default { 'application/octet-stream' }
            }
        }

        $headerText = "$status`r`nContent-Type: $mime`r`nContent-Length: $($body.Length)`r`nCache-Control: no-store`r`nConnection: close`r`n`r`n"
        $headerBytes = [System.Text.Encoding]::ASCII.GetBytes($headerText)
        $stream.Write($headerBytes, 0, $headerBytes.Length)
        $stream.Write($body, 0, $body.Length)
        $stream.Flush()
    }
    catch {
        Write-Host "request error: $($_.Exception.Message)"
    }
    finally {
        if ($stream) { $stream.Close() }
        $client.Close()
    }
}
