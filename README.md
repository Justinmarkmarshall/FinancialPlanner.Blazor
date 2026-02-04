For db migrations:
Make a change to the DbContext or the model classes.
Run the following command in the Package Manager Console:
dotnet ef migrations add WhatEverYouWantToCallIt

To update the database with the new migration, run:
dotnet ef database update

Rollback

dotnet ef migration list

20240101_InitialCreate
20240210_AddExpenseType   ← current

dotnet ef database update 20240101_InitialCreate


1. To create the Database, run the following commands in the Package Manager Console:

dotnet ef migrations add InitialCreate
dotnet ef database update

2. Connection strings have been left out of the git commit

for local dev run

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:FinanceDb" "Your-Secret-Connection-String"

for production envs

kubectl create secret generic finance-db-secret \
  --from-literal=ConnectionStrings__FinanceDb="Your-Secret-Connection-String"

3. .github workflow has been setup to build and push docker images to GHCR

4. a simple k8s finance planner.yaml has everything needed to deploy to a k8s cluster

5. Google OAuth integration

kubectl create secret generic financialplanner-secrets \
  --from-literal=Authentication__Google__ClientId="YOUR_CLIENT_ID" \
  --from-literal=Authentication__Google__ClientSecret="YOUR_CLIENT_SECRET"
