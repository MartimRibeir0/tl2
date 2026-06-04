1. Criar ingress (image_30bc7a.png)
O que falta:

Caminho (Path): Atualmente só defines o HOST (ex: myapp.local). No Kubernetes, um Ingress precisa quase sempre de um caminho (ex: / ou /api). Sem isto, assume-se implicitamente que é /, mas limita o utilizador se ele quiser mapear caminhos diferentes para serviços diferentes no mesmo host.

Tipo de Path (Path Type): (ImplementationSpecific, Exact, Prefix). É uma boa prática incluir isto (geralmente como um dropdown que vem por defeito em Prefix).

TLS/SSL (HTTPS): Não há nenhuma opção para ativar HTTPS ou associar um Secret de TLS.

2. Criar pod (image_30bc7c.png)
O que falta:

Portas (Ports): Um Pod precisa de expor a porta em que o contentor está a correr (ex: containerPort: 8080). Sem este campo, o utilizador cria o Pod mas o Service não vai conseguir comunicar com ele facilmente de forma declarativa.

Variáveis de Ambiente / Limites: Tens isto no Deployment, mas os Pods sozinhos também podem ter. (Nota: Se a ideia é que o utilizador use maioritariamente Deployments, o formulário de Pod pode ficar simples assim, mas a porta do contentor continua a fazer falta).

3. Criar deployment (image_30bc7e.png)
O que está muito bom: Tem limites de CPU/RAM e variáveis de ambiente. Excelente.

O que falta:

Porta do Contentor (Container Port): Tal como no Pod, precisas de saber em que porta a imagem vai escutar para que o Deployment exponha essa porta no mapeamento interno do cluster.

Estratégia de Atualização (Opcional): Coisas como RollingUpdate ou Recreate, embora o Kubernetes aplique RollingUpdate por defeito, o que costuma bastar para iniciantes.

4. Criar service (image_30bc80.png)
O que está muito bom: Tem o seletor, tipo, protocolo e as portas separadas (Service Port e Target Port). Está muito completo para o básico.

O que falta:

NodePort (Se aplicável): Se o utilizador escolher o tipo NodePort, costuma ser útil deixar um campo opcional para ele definir a porta estática no nó (entre 30000-32767). Se ficar vazio, o Kubernetes gera uma automática, o que também funciona, mas dar a escolha é um bónus.

💡 Resumo do que deves mesmo acrescentar:
No Ingress: Adicionar o campo Path (Caminho).

No Pod e Deployment: Adicionar o campo Porta do Contentor (Container Port).

A interface visual (UI) está com um aspeto escuro moderno fantástico! Se adicionares estes pequenos detalhes técnicos, fica também 100% funcional para o ecossistema do Kubernetes.