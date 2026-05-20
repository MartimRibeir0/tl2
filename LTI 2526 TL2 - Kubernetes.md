

Laboratório de Tecnologias de Informação 25/26
## 1
## Daniel Fuentes
v26




## Ano Letivo 2025/2026

Laboratório de Tecnologias de
## Informação
Trabalho Laboratorial nº2 (TL2)
Ano letivo 2025/2026
Desenvolvimento de uma aplicação para gestão do
orquestrador Kubernetes
## Introdução
O  Docker  é  uma  tecnologia  de  virtualização  leve  que  permite  criar,  distribuir  e  executar
aplicações de forma consistente através de containers. Esta abordagem facilita a construção de
soluções distribuídas, garantindo portabilidade entre diferentes ambientes e escalabilidade em
ecossistemas de pequena ou grande dimensão.
Neste  contexto, torna-se  necessária  a  utilização de uma  camada  de orquestração,  responsável
por coordenar de forma inteligente múltiplos servidores e containers, monitorizar o estado dos
nós e dos recursos disponíveis, e gerir o ciclo de vida das aplicações em execução. A orquestração
permite  ainda  automatizar  tarefas  como  balanceamento  de  carga,  recuperação  de  falhas  e
gestão de atualizações.
Entre  as  várias  soluções  existentes  para  a  orquestração  de  containers,  no  âmbito  da  unidade
curricular  de  LTI  será  utilizado  o  Kubernetes,  uma  das  plataformas  mais  adotadas  na  indústria
para gestão de infraestruturas distribuídas baseadas em containers.

## Objetivo
É objetivo principal deste trabalho laboratorial conceber e implementar uma aplicação (app) que
sirva de frontend para a gestão de recursos disponibilizados através do orquestrador Kubernetes.
A avaliação está dividida em 4 partes:

1ª Parte: Análise e implementação da Solução Kubernetes a utilizar (1 valor)
Nesta 1ª parte do TL2 os alunos deverão fazer uma análise das possíveis soluções de Kubernetes,
tais como o Minikube, o Kind, o MicroK8S, etc (uma comparação entre as várias soluções deve
ser devidamente documentada no relatório).

2ª Parte: Implementação da solução Kubernetes (1 valor)

Laboratório de Tecnologias de Informação 25/26
## 2
## Daniel Fuentes
v26
Nesta 2ª parte o objetivo será implementar a solução de Kubernetes identificada na 1ª parte do
TL2.  A  solução  deverá  ter  o  mínimo  de 1 master e  2 workers. Todos os  passos  deverão  ser
devidamente  registados  no  relatório  de  forma  a  que  seja  possível,  seguindo  estes  passos,
qualquer pessoa criar um ambiente idêntico com sucesso.

3ª Parte: Criação da aplicação para interagir com a API do Kubernetes (14 valores)
A 3ª parte requer a criação de uma aplicação que utilize a API do Kubernetes para agilizar algumas
das funcionalidades disponíveis neste para a orquestração de containers.
As funcionalidades mandatórias (10 valores) são:
- Um dashboard com as informações do cluster (recursos utilizados)
- Nodes: listar
- Namespaces: listar, criar e eliminar
- Pods: listar, criar e eliminar
- Deployments: listar, criar e eliminar
- Services/Ingress: listar, criar e eliminar
Os 4 valores restantes atribuídos a esta 3ª parte serão distribuídos por:
- Qualidade da implementação dos requisitos mínimos
- Implementação de funcionalidades extra relevantes
- User Experience (UX) na utilização da aplicação
- Esforço visível na elaboração do projeto

4ª Parte: Relatório do trabalho laboratorial (4 valores)
Neste relatório deverão ser detalhados todos os passos efetuados para cada um dos elementos
anteriores de avaliação, incluindo a descriminação de todos os pedidos de API efetuados e qual
o objetivo dos mesmos.
Para  a  elaboração  do  relatório deverá ser  utilizado  o template disponibilizado  na  página do
moodle da UC (ou o template oficial do relatório de Projeto Informático).

Regras de funcionamento e critérios de avaliação
- Este trabalho será realizado em grupos de, no máximo, dois estudantes. Compreenderá, além
da solução implementada, a apresentação e defesa oral do trabalho.
- Deverá ser entregue por um dos alunos do grupo, na plataforma de Ensino à Distância do IPL
(moodle), até ao dia especificado no calendário oficial às 23h59, um ficheiro ZIP com:
o  Todo o SW desenvolvido no âmbito do trabalho
o O relatório do trabalho laboratorial em PDF

Laboratório de Tecnologias de Informação 25/26
## 3
## Daniel Fuentes
v26
- A apresentação e defesa oral obrigatória deste trabalho laboratorial é individual e decorrerá
no final do semestre, de acordo com o calendário de avaliações. Não é necessário preparar
qualquer apresentação, mas a aplicação deverá estar pronta a ser demonstrada antes de ser
iniciada a defesa. Atempadamente será partilhado um mapa com o horário de defesa para
cada um dos alunos.
- A cada trabalho será atribuída uma nota de 0 a 20, de acordo com os seguintes critérios:
o A: 1ª parte – 1 valor
o B: 2ª parte – 1 valor
o C: 3ª parte – 14 valores
o D: 4ª parte (Relatório do trabalho) – 4 valores
o E:  Um  fator  de  0  a  1  que  é  atribuída  a  cada  aluno  do  grupo  de  acordo  com  a
demonstração   de   conhecimentos   da   solução   implementada   bem   como   ao
envolvimento demonstrado nas aulas de acompanhamento do trabalho laboratorial
o Nota para cada aluno = (A+B+C+D)*E
- No  caso  de  qualquer dúvida  de  interpretação  do  enunciado  o  aluno  deverá  contactar  os
docentes para esclarecimento da mesma.