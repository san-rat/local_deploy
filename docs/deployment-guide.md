# Azure Student Deployment Guide

Stage V6 deploys LocalDeploy Lab to Azure using the Azure Portal and an Azure for Students subscription.

This guide uses one Ubuntu virtual machine and the existing Docker Compose production override. That keeps the cloud deployment close to the local architecture while staying friendly to student credits.

## Why This Path

Use:

- Azure for Students subscription
- one Ubuntu VM
- Docker Engine and Docker Compose
- existing `docker-compose.yml` plus `docker-compose.prod.yml`
- public HTTP on port `80`

Avoid for this stage:

- Azure Database for PostgreSQL
- Azure Cache for Redis
- Azure Kubernetes Service
- Application Gateway
- managed Prometheus or Grafana

Those services are useful later, but they add cost and architecture changes.

## Cost Safety

Azure for Students includes credit and free service allowances, but the subscription can be disabled if the credit is exhausted.

Before creating the VM:

1. Open Azure Portal.
2. Go to `Cost Management + Billing`.
3. Open `Budgets`.
4. Add a budget for the student subscription.

Suggested budget:

```text
Name: budget-localdeploy-student
Amount: 5 USD or 10 USD
Alerts: 50%, 80%, 100%
```

Budgets send alerts. They do not automatically stop resources.

## Create The Resource Group

In Azure Portal:

```text
Resource groups -> Create
```

Use:

```text
Subscription: Azure for Students
Resource group: rg-localdeploy-v6
Region: closest available region
```

Keep all project resources in this resource group so cleanup is simple.

## Create The Ubuntu VM

In Azure Portal:

```text
Virtual machines -> Create -> Azure virtual machine
```

Use:

```text
Subscription: Azure for Students
Resource group: rg-localdeploy-v6
Virtual machine name: vm-localdeploy
Region: same region as the resource group
Availability options: No infrastructure redundancy required
Security type: Standard
Image: Ubuntu Server 22.04 LTS or Ubuntu Server 24.04 LTS
Size: B1s first
Authentication type: SSH public key
Username: azureuser
Inbound ports: SSH 22 only
```

`B1s` is the free-friendly starting point. If the full Docker Compose stack is too tight, resize temporarily to `B1ms` or `B2s`, knowing that this may use student credit.

## Configure Networking

After the VM is created, open:

```text
VM -> Networking
```

Add an inbound rule:

```text
Destination port ranges: 80
Protocol: TCP
Source: Any
Action: Allow
Name: Allow-HTTP-80
```

For SSH, restrict port `22` to your own IP address if possible.

Do not expose these ports publicly:

```text
5432 PostgreSQL
6379 Redis
8080 backend
8081 activity-service
3000 Grafana
9090 Prometheus
```

## Connect To The VM

From your local machine:

```bash
ssh azureuser@<VM_PUBLIC_IP>
```

## Install Docker

On the VM:

```bash
sudo apt update
sudo apt install -y ca-certificates curl git
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
```

Log out and back in:

```bash
exit
ssh azureuser@<VM_PUBLIC_IP>
```

Verify:

```bash
docker --version
docker compose version
```

## Clone The Project

On the VM:

```bash
git clone <your-github-repo-url> local_deploy
cd local_deploy
```

For a private repository, use GitHub SSH or a personal access token.

## Configure Environment

Create the env file:

```bash
cp .env.example .env
nano .env
```

Change at least:

```env
POSTGRES_PASSWORD=change_this_to_a_real_password
DB_PASSWORD=change_this_to_a_real_password
GRAFANA_ADMIN_PASSWORD=change_this_if_using_monitoring
ENABLE_SWAGGER=false
NGINX_HTTP_PORT=80
```

Keep these Docker network values:

```env
DB_HOST=database
REDIS_HOST=redis
ACTIVITY_SERVICE_URL=http://activity-service:8081
```

## Start The Cloud Stack

On the VM:

```bash
docker compose --env-file .env \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  up -d --build
```

Check status:

```bash
docker compose --env-file .env \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  ps
```

## Verify The Public App

From your local machine:

```bash
curl http://<VM_PUBLIC_IP>/health
curl http://<VM_PUBLIC_IP>/api/tasks
curl http://<VM_PUBLIC_IP>/api/tasks/summary
curl http://<VM_PUBLIC_IP>/api/activity
```

Open in the browser:

```text
http://<VM_PUBLIC_IP>
```

Swagger should be disabled by default:

```text
http://<VM_PUBLIC_IP>/swagger
```

## Run Recovery Checks

On the VM:

```bash
./scripts/backup-db.sh
./scripts/verify-restore.sh
./scripts/cleanup-backups.sh 7
```

Copy a backup down to your local machine if needed:

```bash
scp azureuser@<VM_PUBLIC_IP>:~/local_deploy/backups/*.sql ./backups/
```

## Run Load Test

From your local machine:

```bash
docker run --rm -i \
  -e BASE_URL=http://<VM_PUBLIC_IP> \
  grafana/k6 run - < tests/load/localdeploy-read-load.js
```

Record results in `docs/performance-results.md`.

## Optional Monitoring

On a small student VM, start Prometheus and Grafana only when needed for screenshots.

On the VM:

```bash
docker compose --profile monitoring --env-file .env \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  up -d --build
```

Use SSH port forwarding instead of opening ports `3000` and `9090` publicly:

```bash
ssh -L 3000:localhost:3000 -L 9090:localhost:9090 azureuser@<VM_PUBLIC_IP>
```

Open locally:

```text
http://localhost:3000
http://localhost:9090
```

Stop monitoring when finished:

```bash
docker compose --profile monitoring --env-file .env \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  down
```

## Stop Or Clean Up

Stop the app containers:

```bash
docker compose --env-file .env \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  down
```

Stop the VM from Azure Portal when you are not using it:

```text
Virtual machines -> vm-localdeploy -> Stop
```

When the stage is complete, delete the resource group:

```text
Resource groups -> rg-localdeploy-v6 -> Delete resource group
```

This removes the VM, disk, IP, network, and related resources.

## Screenshot Targets

Suggested portfolio evidence:

```text
docs/screenshots/azure-vm-overview.png
docs/screenshots/azure-public-app.png
docs/screenshots/azure-health-endpoint.png
docs/screenshots/azure-resource-group.png
```

## References

- Azure for Students: https://azure.microsoft.com/en-us/free/students/
- Azure for Students disabled subscription notes: https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/azurestudents-subscription-disabled
- Create a Linux VM in Azure Portal: https://learn.microsoft.com/en-us/azure/virtual-machines/linux/quick-create-portal
