if (Get-Command "protoc" -ErrorAction SilentlyContinue) {
    protoc --proto_path=. --csharp_out=. battle_net_product_db.proto
} else {
    Write-Host "protoc is not found. Please install with:"
    Write-Host "  winget install Google.Protobuf"
}


