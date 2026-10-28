$ErrorActionPreference = "Stop"

# Byg begge images.
docker build -f .\KubePodCreator\Dockerfile -t pod-api:1.0 .
if ($LASTEXITCODE -ne 0) { throw "Build af pod-api fejlede." }

docker build -f .\KubeGameServer\Dockerfile -t game-server:1.0 .
if ($LASTEXITCODE -ne 0) { throw "Build af game-server fejlede." }

# Indlæs dem i Docker Desktops kind-node.
# cmd bevarer binære data gennem pipen, også i ældre PowerShell.
cmd /c "docker image save pod-api:1.0 game-server:1.0 | docker exec -i desktop-control-plane ctr -n k8s.io images import -"
if ($LASTEXITCODE -ne 0) { throw "Import til kind fejlede." }

# Vis de importerede images.
docker exec desktop-control-plane crictl images |
    Select-String "pod-api|game-server"

Write-Host "Images er bygget og indlæst i kind." -ForegroundColor Green