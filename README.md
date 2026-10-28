# Øvelse – opret game-servere med Kubernetes

**Projekt på GitHub:** [PBSoft-SystemIntegration/KubePodCreator](https://github.com/PBSoft-SystemIntegration/KubePodCreator)

I skal køre det færdige eksempel, undersøge de oprettede ressourcer og afprøve RBAC.
I skal ikke skrive ny C#-kode. Brug den færdige kode fra projektet på GitHub.

## Målet

Efter øvelsen kan I forklare:

- Hvordan et REST-kald får pod-creator til at oprette en game-server-pod og en Service.
- Hvordan ServiceAccount, Role og RoleBinding giver applikationen adgang til Kubernetes-API'et.
- Hvordan klienten får adresse og port og kommunikerer med sin egen game-server.

## 1. Hent projektet og byg images

Brug .NET 10 SDK, Docker Desktop med Kubernetes/kind og **én node** samt kubectl.
Docker Desktop skal køre med Linux-containere. Kommandoerne bruger namespace `default`.

Hent projektet fra GitHub, og åbn PowerShell i repoets rod, hvor `KubePodCreator.sln`
og `BuildAndCopyImages.ps1` ligger. Hvis I bruger Git:

```powershell
git clone https://github.com/PBSoft-SystemIntegration/KubePodCreator.git
cd KubePodCreator
```

Kontrollér, at I bruger Docker Desktops cluster:

```powershell
kubectl config current-context
kubectl get nodes
```

Context skal være `docker-desktop`, og noden `desktop-control-plane` skal være Ready.
Hvis I har valgt en anden context, skift med `kubectl config use-context docker-desktop`.

Byg og indlæs images:

```powershell
.\BuildAndCopyImages.ps1
```

Hvis PowerShell blokerer scriptet, kan I tillade det i det aktuelle terminalvindue:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\BuildAndCopyImages.ps1
```

Scriptet bygger `pod-api:1.0` og `game-server:1.0` og indlæser dem i kind-noden.
Navnene matcher projektets YAML og C#-kode. Kontrollér, at begge images fremgår af scriptets sidste output.

## 2. Start pod-creator

```powershell
kubectl apply -n default -f ./KubePodCreator/k8s/
kubectl rollout status -n default deployment/pod-api-deployment
kubectl get pods,services -n default
```

Åbn [Scalar](http://localhost:8080/scalar). LoadBalancer kan bruge lidt tid på at blive klar.

Der kører nu en pod-creator-pod bag `pod-api-service`. Der oprettes først en ny game-server,
når klienten eller Scalar kalder POST `/PodCreator/CreateGameServer`.

Ved rebuild med samme image-tag skal I først indlæse images igen og derefter genstarte API-pod'en:

```powershell
kubectl rollout restart -n default deployment/pod-api-deployment
kubectl rollout status -n default deployment/pod-api-deployment
```

## 3. Forbind en klient

Kør fra repoets rod:

```powershell
dotnet run --project KubeClient -- http://localhost:8080
```

Klienten viser game-serverens navn, adresse og port og forbinder automatisk.
Send en besked. Game-serveren skal sende samme tekst tilbage.

**Lad klienten være åben**, og start en anden klient i en ny terminal med samme kommando.
Kontrollér, at klienterne får forskellige servernavne og porte, og send beskeder fra begge.

Se ressourcerne i en tredje terminal:

```powershell
kubectl get pods,services -n default -l demo=pod-creator
```

Find hver klients servernavn i outputtet. Der skal være en pod og en Service pr. oprettet game-server.
Navne og porte varierer fra gang til gang og behøver ikke matche undervisningens screenshots.

Hvis der allerede var game-servere fra tidligere forsøg, vil de også fremgå.

## 4. Hent pods gennem Kubernetes-API'et

Åbn Scalar og kald GET `/PodCreator/Pods`. Endpointet skriver pod-navnene i API'ets log.

```powershell
kubectl logs -n default deployment/pod-api-deployment --tail=80
kubectl get pods -n default
```

Sammenlign navnene. Begge kald viser pods i `default` namespace.
Role indeholder `list`, så applikationen må hente samlingen af pods.

## 5. Undersøg RBAC

Fjern RoleBinding:

```powershell
kubectl delete rolebinding pod-creator-binding -n default
```

Kald POST `/PodCreator/CreateGameServer` i Scalar. Oprettelsen skal nu fejle.
HTTP-svaret kan være 500; Kubernetes-fejlen `Forbidden` findes i API'ets log:

```powershell
kubectl logs -n default deployment/pod-api-deployment --tail=80
```

Forklar, hvorfor kontoen stadig findes, men ikke længere må oprette pods.
De allerede oprettede game-servere kan stadig svare; ændringen gælder pod-creators adgang til Kubernetes-API'et.

Gendan rettighederne:

```powershell
kubectl apply -n default -f ./KubePodCreator/k8s/KubePodServiceAccount.yml
```

Start en ny klient, og kontrollér, at oprettelse og TCP-forbindelse virker igen.
Ændrede rettigheder kan bruge et kort øjeblik på at slå igennem.

## 6. Forklar sammenhængen

- Hvilken ServiceAccount bruger pod-creator, og hvor er den valgt?
- Hvad gør Role og RoleBinding hver især?
- Hvordan matcher en game-server-Service den rigtige pod?
- Hvorfor lytter alle game-servere på 7777, selvom klienterne bruger forskellige porte?

### Porttildelingen i vores lokale eksempel

1. Pod-creator opretter en NodePort-Service.
2. Kubernetes tildeler en ledig NodePort, eksempelvis 31306.
3. Pod-creator ændrer samme Service til LoadBalancer med `port: 31306` og `targetPort: 7777`.
4. Pod-creator venter på, at TCP-serveren svarer gennem LoadBalancer.
5. Klienten modtager `127.0.0.1:31306` og forbinder.

Porten vælges af Kubernetes, ikke af en lokal tæller. Forskellige pod-creators kan derfor oprette servere samtidigt.
LoadBalancer-trinnet er vores lokale tilpasning til Docker Desktop/kind, hvor en NodePort ikke automatisk
publiceres til Windows. Det er ikke et generelt krav, at en NodePort-Service skal ændres til LoadBalancer.

For en færdig game-server-Service kan `PORT(S)` eksempelvis vise `31306:31306/TCP`.
Det første tal er Service-porten, og det andet er den reserverede NodePort. `targetPort` er stadig 7777.

Role tillader `get`, `list`, `create` og `update` på pods og Services i `default`.
`update` bruges ved ændringen til LoadBalancer. Applikationen bruger `InClusterConfig()` til at forbinde
med pod'ens ServiceAccount; token og certifikat indlæses af biblioteket.

## 7. Ryd op

Afslut klienterne med en tom linje. Det sletter ikke deres game-servere.
Slet øvelsens game-servere og derefter pod-creator og RBAC:

```powershell
kubectl delete pod,service -n default -l demo=pod-creator
kubectl delete -n default -f ./KubePodCreator/k8s/
```

Den første kommando sletter også game-servere fra tidligere kørsler af dette eksempel.
Kontrollér, at der ikke er flere game-servere med øvelsens label:

```powershell
kubectl get pods,services -n default -l demo=pod-creator
```

Vent om nødvendigt, mens pods afslutter. Til sidst skal der stå `No resources found`.

## I er færdige, når

- [ ] To klienter har været forbundet samtidigt til hver sin server med forskellige porte.
- [ ] Begge klienter har sendt en besked og modtaget samme tekst tilbage.
- [ ] I har sammenlignet pod-navnene fra GET-endpointets log med `kubectl get pods`.
- [ ] I har set oprettelsen fejle uden RoleBinding og virke efter gendannelse.
- [ ] I kan forklare ServiceAccount, Role, RoleBinding og sammenhængen mellem pod, Service og port.
- [ ] I har ryddet øvelsens ressourcer op.

Eksemplet demonstrerer oprettelse, porttildeling, rettigheder og TCP-kommunikation.
Matchmaking, automatisk sletning og produktionsklar fejlhåndtering er ikke med.
