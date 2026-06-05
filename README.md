# KubeManager

Trabalho Laboratorial nº 2 (TL2)

Laboratório de Tecnologias de Informação

EI 2024 / 25

Grupo 7

Martim Salvador Neves Ribeiro 2231018

Fábio Ferreira Nunes 2231014

Leiria, junho de 2026

---

# Resumo

O presente relatório detalha o desenvolvimento e implementação do projeto **KubeManager**, realizado no âmbito da unidade curricular de Laboratório de Tecnologias de Informação (LTI). O projeto foca-se na gestão centralizada de recursos Kubernetes e infraestrutura virtualizada em ambiente Proxmox.

A solução baseia-se num backend desenvolvido em **.NET 9.0**, que interage com as APIs do Kubernetes e Proxmox, e um frontend moderno construído com **Tailwind CSS**. As funcionalidades implementadas incluem a monitorização de recursos em tempo real (via Metrics Server), gestão de Namespaces, Pods, Deployments (incluindo escalonamento), Services e Ingresses. Adicionalmente, a aplicação permite a gestão básica de Máquinas Virtuais no Proxmox, como listagem e controlo de estado (Start/Shutdown), e oferece um editor YAML integrado para modificação direta de recursos.

Ao contrário do planeado inicialmente, a autenticação com o Proxmox é realizada via **Tickets (username/password)** em vez de tokens estáticos, garantindo maior flexibilidade em ambientes laboratoriais. Embora a API suporte operações avançadas como clonagem de VMs, a interface atual foca-se na estabilidade operacional e monitorização analítica.

**Palavras-chave:** Kubernetes, Orquestração, Proxmox, .NET 9.0, API, Monitorização.

---

# Abstract

This report describes the development and implementation of the **KubeManager** project, created for the Information Technology Laboratory (LTI) course. The project aims to provide a centralized management platform for Kubernetes resources and virtualized infrastructure within a Proxmox environment.

The solution features a robust backend built with **.NET 9.0**, interacting with official Kubernetes and Proxmox APIs, and a sleek frontend developed using **Tailwind CSS**. Key features include real-time resource monitoring (leveraging the Metrics Server), management of Namespaces, Pods, Deployments (with scaling support), Services, and Ingresses. Furthermore, the application enables basic management of Proxmox Virtual Machines, such as listing and power state control (Start/Shutdown), alongside an integrated YAML editor for direct resource manipulation.

Authentication with Proxmox is handled via **Ticket-based credentials** (username and password), providing a secure and flexible approach for lab environments. While the backend supports advanced operations like VM cloning, the current user interface prioritizes operational stability and analytical monitoring.

**Keywords:** Kubernetes, Orchestration, Proxmox, .NET 9.0, API, Monitoring.

---

# Índice

1. [Introdução](#1-introdução)
2. [Análise de Soluções Kubernetes](#2-análise-de-soluções-kubernetes)
3. [Implementação da Infraestrutura](#3-implementação-da-infraestrutura)
4. [Arquitetura do Sistema e Desenvolvimento](#4-arquitetura-do-sistema-e-desenvolvimento)
5. [Funcionamento e Demonstração](#5-funcionamento-e-demonstração)
6. [Conclusão](#6-conclusão)
7. [Bibliografia](#7-bibliografia)

---

# 1. Introdução

O projeto KubeManager surge da necessidade de simplificar a gestão de ecossistemas de contentores em ambientes de infraestrutura híbrida. Com a crescente adoção do Kubernetes para orquestração de aplicações, torna-se imperativo possuir ferramentas que permitam visualizar e controlar não só a camada lógica (Pods, Deployments) mas também a camada física/virtualizada (VMs no Proxmox).

O objetivo deste trabalho foi desenvolver uma aplicação Web intuitiva que atue como um "single pane of glass" para administradores de sistemas, permitindo a monitorização dinâmica de recursos e a gestão operacional rápida sem a necessidade de recorrer exclusivamente à linha de comandos (CLI).

---

# 2. Análise de Soluções Kubernetes

O Kubernetes (K8s) é a plataforma líder em orquestração de contentores. No âmbito deste projeto, foram analisadas diversas soluções "lightweight" para ambientes de desenvolvimento e laboratórios:

- **Minikube:** Cria um cluster de nó único dentro de uma VM ou via Docker. Robusto, mas pesado.
- **Kind:** Executa nós como contentores Docker. Ideal para CI/CD.
- **MicroK8s:** Distribuição leve da Canonical com sistema de addons (escolhida para este projeto).
- **K3s:** Otimizada para Edge Computing e IoT.

A seleção recaiu sobre o **MicroK8s** devido à sua facilidade de ativação de componentes críticos como o `metrics-server` e o controlador de `ingress`.

---

# 3. Implementação da Infraestrutura

A infraestrutura foi montada utilizando o **VMware Workstation Pro** para garantir estabilidade e performance.

### 3.1. Especificações Técnicas das VMs

| Tipo de Máquina | vCPUs | RAM | Disco | Hostname |
| :--- | :---: | :---: | :---: | :--- |
| VM Master | 2 | 4GB | 50 GB | kmaster |
| VM Worker 1 | 2 | 2GB | 50 GB | kworker1 |
| VM Worker 2 | 2 | 2GB | 50 GB | kworker2 |

### 3.2. Procedimento de Instalação do MicroK8s

A instalação foi efetuada via Snap nas três instâncias Ubuntu 24.04 LTS. A formação do cluster utilizou o comando `microk8s add-node` no Master para gerar os tokens de junção para os Workers.

### 3.3. Otimização e Addons

Ativaram-se os seguintes componentes críticos:
- **Metrics-Server:** Para leitura de consumo de recursos.
- **DNS:** Resolução interna de nomes.
- **Ingress:** Exposição de serviços via NGINX.
- **Hostpath-Storage:** Persistência de dados local.

Comando: `microk8s enable metrics-server ingress dns hostpath-storage`

### 3.4. Persistência de Dados

A utilização do addon `hostpath-storage` permite que os dados das aplicações sobrevivam a reinícios dos Pods, sendo mapeados diretamente para o sistema de ficheiros das VMs no **VMware**.

---

# 4. Arquitetura do Sistema e Desenvolvimento

### 4.1. Backend (.NET 9.0 Web API)

Desenvolvido em C# com .NET 9.0, o backend serve como ponte entre o Frontend e as APIs oficiais do Kubernetes e Proxmox. Utiliza a biblioteca `KubernetesClient` para orquestração e `HttpClient` para gestão de infraestrutura.

### 4.2. Frontend (Tailwind CSS SPA)

A interface é uma Single Page Application moderna que utiliza Tailwind CSS para o design, Chart.js para métricas e ACE Editor para manipulação de ficheiros YAML.

### 4.3. Funcionalidades Implementadas

- **Dashboard Analítico:** Visualização de métricas de CPU e RAM.
- **Gestão K8s:** CRUD de Namespaces, Pods, Deployments, Services e Ingresses.
- **Escalonamento:** Alteração dinâmica de réplicas.
- **Visualização de Logs:** Diagnóstico direto via interface.
- **Gestão Proxmox:** Ciclo de vida completo das VMs (Ligar, Desligar, Clonar e Eliminar).

### 4.4. Documentação da API

| Rota do Endpoint | Método | Descrição |
| :--- | :---: | :--- |
| `/api/Vms` | GET | Listagem de VMs do Proxmox |
| `/api/Vms/{node}/{vmid}/action` | POST | Ações de energia (Start/Shutdown) |
| `/api/Vms/{node}/{vmid}/clone` | POST | Clonagem dinâmica de VMs |
| `/api/Vms/{node}/{vmid}` | DELETE | Eliminação permanente de VMs |
| `/api/Deployments` | GET | Listagem de Deployments |
| `/api/Nodes` | GET | Listagem de nós do cluster |
| `/api/Events` | GET | Eventos do cluster |
| `/api/Pods/{name}/logs` | GET | Logs de um Pod |
| `/api/Yaml/{resource}/{name}` | GET/PUT | Edição de YAML |


---

# 5. Funcionamento e Demonstração

Nesta secção, detalha-se a experiência de utilização da plataforma KubeManager.

### 5.1. Autenticação e Ligação ao Proxmox
![Ecrã de Login Proxmox](docs/screenshots/01-login.png)
*Autenticação via Ticket para gestão da infraestrutura virtual.*

### 5.2. Dashboard e Monitorização em Tempo Real
![Dashboard Principal](docs/screenshots/02-dashboard.png)
*Gráficos de consumo de recursos processados via Metrics Server.*

### 5.3. Gestão de Workloads e Escalonamento
![Lista de Deployments](docs/screenshots/03-deployments.png)
*Controlo visual de réplicas e estado das aplicações.*

### 5.4. Editor YAML e Visualização de Logs
![Editor YAML](docs/screenshots/04-yaml.png)
*Manipulação direta de manifestos e diagnóstico via logs.*

### 5.5. Gestão de Infraestrutura (VMs)
![Gestão de VMs Proxmox](docs/screenshots/05-vms.png)
*Controlo remoto das máquinas virtuais do laboratório.*

---

# 6. Conclusão

O desenvolvimento do KubeManager permitiu consolidar conhecimentos em orquestração de sistemas e APIs. A integração entre Kubernetes e Proxmox numa interface única reduz a complexidade operacional e melhora a produtividade dos administradores.

---

# 7. Bibliografia

- Kubernetes Documentation: [https://kubernetes.io/docs/](https://kubernetes.io/docs/)
- MicroK8s Documentation: [https://microk8s.io/docs/](https://microk8s.io/docs/)
- Proxmox API Documentation: [https://pve.proxmox.com/pve-docs/api-viewer/](https://pve.proxmox.com/pve-docs/api-viewer/)


request de login

-______
POST /api2/extjs/access/ticket HTTP/1.1
Host: 192.168.24.196:8006
Content-Length: 54
Sec-Ch-Ua-Platform: "Windows"
Accept-Language: pt-PT,pt;q=0.9
Sec-Ch-Ua: "Not)A;Brand";v="8", "Chromium";v="138"
Sec-Ch-Ua-Mobile: ?0
X-Requested-With: XMLHttpRequest
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36
Content-Type: application/x-www-form-urlencoded; charset=UTF-8
Csrfpreventiontoken: null
Accept: */*
Origin: https://192.168.24.196:8006
Sec-Fetch-Site: same-origin
Sec-Fetch-Mode: cors
Sec-Fetch-Dest: empty
Referer: https://192.168.24.196:8006/
Accept-Encoding: gzip, deflate, br
Priority: u=1, i
Connection: keep-alive

username=root&password=12345678&realm=pam&new-format=1
_______