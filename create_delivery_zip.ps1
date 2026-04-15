$sourcePath = ".\"
$destFolder = ".\DMS_Delivery"
$zipPath = ".\DMS_Delivery.zip"

Write-Host "Paketleme basliyor..."

If (Test-Path $destFolder) { Remove-Item -Recurse -Force $destFolder }
New-Item -ItemType Directory -Force -Path $destFolder

# Kopyalanacak klasörler
$dirsToCopy = @("Controllers", "Models", "Views", "Data", "Migrations", "wwwroot", "Services", "Properties", "App_Data")
foreach ($dir in $dirsToCopy) {
    if (Test-Path "$sourcePath\$dir") {
        Copy-Item -Path "$sourcePath\$dir" -Destination "$destFolder\$dir" -Recurse -Force
    }
}

# Kopyalanacak spesifik dosyalar
$filesToCopy = @("*.sln", "*.csproj", "Program.cs", "appsettings*.json", "README.md")
foreach ($file in $filesToCopy) {
    Get-ChildItem -Path $sourcePath -Filter $file | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination "$destFolder" -Force
    }
}

# DB ve derleme dosyalarının kazara eklenmediğinden emin olmak için temizlik (Gerçi kopyalamadık ama garanti olsun)
Get-ChildItem -Path $destFolder -Include "*.db", "*.db-shm", "*.db-wal" -Recurse | Remove-Item -Force
Get-ChildItem -Path $destFolder -Include "bin", "obj", ".vs" -Recurse -Directory | Remove-Item -Recurse -Force

Write-Host "Sıkıştırma yapılıyor..."
Compress-Archive -Path "$destFolder\*" -DestinationPath $zipPath -Force

Remove-Item -Recurse -Force $destFolder

Write-Host "Teslim paketi basariyla olusturuldu: $zipPath"
