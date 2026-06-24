# Run this in PowerShell inside your project folder
$output = "codebase_dump.txt"
"=== PROJECT STRUCTURE ===" > $output
Get-ChildItem -Recurse -Directory | Where-Object { $_.FullName -notlike '*\bin*' -and $_.FullName -notlike '*\obj*' -and $_.FullName -notlike '*\.git*' -and $_.FullName -notlike '*\wwwroot*' } | Select-Object FullName >> $output

"=== CODE FILES ===" >> $output
Get-ChildItem -Recurse -Include *.cs, *.json -Exclude *.json.user | Where-Object { $_.FullName -notlike '*\bin*' -and $_.FullName -notlike '*\obj*' -and $_.FullName -notlike '*\.git*' -and $_.FullName -notlike '*\wwwroot*' } | ForEach-Object {
    "=== START FILE: $_ ===" >> $output
    Get-Content $_.FullName >> $output
    "=== END FILE ===" >> $output
}