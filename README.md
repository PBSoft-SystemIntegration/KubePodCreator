# Øvelse – game-servere i Kubernetes

Kør det færdige eksempel, undersøg ressourcerne og afprøv RBAC. I skal ikke skrive ny kode.

**Forudsætninger:** .NET 10, Docker Desktop med kind og én node (`desktop-control-plane`) samt kubectl.
Kør kommandoerne fra repoets rod. Ressourcerne oprettes i `default`.

## 1. Byg og start

```powershell
kubectl config use-context docker-desktop
.\BuildAndCopyImages.ps1
kubectl apply -n default -f ./KubePodCreator/k8s/
kubectl rollout status -n default deployment/pod-api-deployment
```

Scalar: http://localhost:8080/scalar

Ved rebuild: kør scriptet igen og derefter `kubectl rollout restart -n default deployment/pod-api-deployment`.

## 2. Prøv to klienter

Kør i to terminaler:

```powershell
dotnet run --project KubeClient -- http://localhost:8080
```

Lad begge klienter være åbne. Send beskeder og kontrollér, at de får svar fra hver sin server med forskellige porte.
Find servernavnene i outputtet:

```powershell
kubectl get pods,services -n default -l demo=pod-creator
```

## 3. List pods og afprøv rettigheder

Kald GET `/PodCreator/Pods` i Scalar, og sammenlign loggen med pod-listen:

```powershell
kubectl logs -n default deployment/pod-api-deployment --tail=80
kubectl get pods -n default
```

Fjern RoleBinding:

```powershell
kubectl delete rolebinding pod-creator-binding -n default
```

Kald POST `/PodCreator/CreateGameServer` i Scalar. Det skal fejle; find `Forbidden` i API'ets log.
Gendan rettighederne og kontrollér, at en ny klient igen kan oprette og forbinde til en server:

```powershell
kubectl apply -n default -f ./KubePodCreator/k8s/KubePodServiceAccount.yml
```

## 4. Ryd op

Afslut klienterne med en tom linje. Slet derefter øvelsens game-servere, pod-creator og RBAC:

```powershell
kubectl delete pod,service -n default -l demo=pod-creator
kubectl delete -n default -f ./KubePodCreator/k8s/
```

## Færdig, når

- To klienter har fået hver sin server og sendt/modtaget beskeder samtidigt.
- I har set pod-navnene i API-loggen og demonstreret fejl uden RoleBinding og succes efter gendannelse.
- I kan forklare ServiceAccount, Role, RoleBinding og hvordan Servicen finder den rigtige pod.
- Øvelsens ressourcer er ryddet op.
