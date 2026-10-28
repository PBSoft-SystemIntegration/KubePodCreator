# KubePodCreator – ServiceAccount og RBAC

Et enkelt undervisningseksempel med .NET 10:

- **KubePodCreator:** REST-API, som opretter game-server-pods og Services via Kubernetes-API'et.
- **KubeGameServer:** TCP-server på port 7777, der sender beskeder tilbage til klienten.
- **KubeClient:** Beder om en server, får adresse og port og forbinder via TCP.

## Hvordan vælges porten?

1. API'et opretter game-serverens pod og en NodePort-Service.
2. Kubernetes tildeler en ledig NodePort, eksempelvis 31306.
3. API'et ændrer Servicen til LoadBalancer med `port: 31306` og `targetPort: 7777`.
4. API'et venter på pod og LoadBalancer og kontrollerer, at TCP-serveren svarer.
5. Klienten modtager `127.0.0.1:31306` og forbinder uden ekstra netværkskode.

Porten vælges af Kubernetes, ikke af en tæller i API'et. Derfor kan flere pod-creators
oprette servere samtidigt. Hver server får sit eget navn, sin egen Service og sin egen port.

**Klient → LoadBalancer på 8080 → pod-creator → Kubernetes-API → ny game-server + Service.**

Derefter: **Klient → localhost:tildelt port → game-server:7777.**

LoadBalancer bruges til Windows-adgangen, fordi en NodePort i Docker Desktops kind-node
ikke automatisk publiceres på Windows. NodePort bruges her til at reservere en ledig port.
Dette er et lokalt eksempel til **Docker Desktop med kind og én node**; localhost-adressen
og LoadBalancer-publiceringen er tilpasset dette miljø.

## 1. Byg og indlæs images

Forudsætninger: .NET 10 SDK, Docker Desktop med kind og kubectl.
Kør fra repoets rod i PowerShell:

```powershell
.\BuildAndCopyImages.ps1
```

Scriptet bygger og indlæser `pod-api:1.0` og `game-server:1.0` i noden
`desktop-control-plane`. Navnene matcher YAML og C#-koden.

## 2. Start API'et

```powershell
kubectl apply -f ./KubePodCreator/k8s/
kubectl rollout status deployment/pod-api-deployment
```

Ved rebuild med samme image-tag skal en eksisterende API-deployment genstartes:

```powershell
kubectl rollout restart deployment/pod-api-deployment
kubectl rollout status deployment/pod-api-deployment
```

Åbn http://localhost:8080/scalar. POST `/PodCreator/CreateGameServer` opretter en server.
API'et kører med `pod-api-service-account`. Role giver `get`, `create` og `update` på
pods og Services i `default`. RoleBinding knytter kontoen til rollen.
`update` bruges, når API'et ændrer Servicen fra NodePort til LoadBalancer.

## 3. Prøv klienten

```powershell
dotnet run --project KubeClient
```

Klienten opretter en server og forbinder automatisk. Skriv en besked og se samme tekst
komme tilbage. Tom linje afslutter klienten. Start flere klienter i hver sin terminal:
de skal få forskellige porte og kunne sende beskeder samtidigt.

Se ressourcerne:

```powershell
kubectl get pods,services -l demo=pod-creator
```

For en game-server kan `PORT(S)` eksempelvis vise `31306:31306/TCP`.
Det første tal er nu LoadBalancerens Service-port; det andet er den reserverede NodePort.
`targetPort` er fortsat 7777.

## 4. Prøv RBAC

```powershell
kubectl delete rolebinding pod-creator-binding
```

Kald POST-endpointet i Scalar. Det fejler nu, fordi API'et ikke længere må oprette pods.
Se Kubernetes-fejlen `Forbidden` i loggen:

```powershell
kubectl logs deployment/pod-api-deployment --tail=80
```

Gendan rettighederne og prøv igen:

```powershell
kubectl apply -f .\KubePodCreator\k8s\KubePodServiceAccount.yml
```

## Øvelsen er færdig, når du har

- Oprettet en server gennem klienten og modtaget et TCP-svar.
- Kørt to klienter samtidigt med hver sin game-server og port.
- Set oprettelsen fejle uden RoleBinding og virke efter gendannelse.
- Forklaret ServiceAccount (identitet), Role (tilladelser) og RoleBinding (tildeling).
- Ryddet op.

## Oprydning

Game-serverne slettes ikke automatisk, når klienterne afsluttes.
Ved fejl under oprettelsen kan der også være ressourcer tilbage; samme kommando rydder dem op.

```powershell
kubectl delete pod,service -l demo=pod-creator
kubectl delete -f ./KubePodCreator/k8s/
```

Eksemplet holder bevidst fokus på oprettelse, porttildeling, RBAC og TCP-kommunikation.
Det indeholder ikke matchmaking, automatisk levetid eller produktionsklar fejlhåndtering.
