# Default values for financialplanner Helm chart.
# This file is a YAML-formatted file.
# Declare variables to be passed into your templates.

Your current PVC (with no storageClassName) will only bind automatically on clusters that have a default StorageClass.
On an RKE2 cluster without a default, the PVC will stay in Pending until:
You install/define a StorageClass and mark it as (default), or
You set storageClassName on the PVC to a specific existing class, or
You create a static PV that your PVC can bind to (more work).

Local cluster (has default StorageClass):

Leave storage.className empty.


RKE2 cluster (no default StorageClass):

Set the class name via values:
Either edit values.yaml for that environment:
storage.className: my-rke2-storageclass
Or override on install/upgrade:
helm upgrade --install financialplanner . --set storage.className=my-rke2-storageclass

QUICK START:

kubectl create namespace finance

kubectl -n finance create secret generic financialplanner-secrets --from-literal=Authentication__Google__ClientId="YOUR_CLIENT_ID" --from-literal=Authentication__Google__ClientSecret="YOUR_CLIENT_SECRET"

local

helm install financialplanner .\financialplanner\ -n finance

RKE-2




debugging steps

kubectl -n finance get deploy financialplanner-financialplanner -o yaml | Select-String -Pattern "env:" -Context 0,20