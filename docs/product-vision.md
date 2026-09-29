# Product Vision — PersonalBudget

> **Missão:** Tornar-se a referência em gestão financeira familiar colaborativa no Brasil
> **Data:** Setembro 2026
> **Relacionado:** [Frontend Discovery](../../PersonalBudgetUI/docs/frontend-discovery.md) · [Backend Discovery](backend-discovery.md)

---

## O Que Temos Hoje

O PersonalBudget é um app de orçamento **multi-household** que permite que famílias gerenciem finanças colaborativamente. Sua proposta de valor atual:

| Funcionalidade | Status |
|---------------|--------|
| Múltiplos membros por família (household) | ✅ Implementado |
| Controle de cartões + faturas + parcelamentos | ✅ Implementado |
| Contas bancárias + caixinhas de poupança | ✅ Implementado |
| Transações recorrentes e parceladas | ✅ Implementado |
| Dashboard customizável com 13 widgets | ✅ Implementado |
| Gastos por membro/perfil | ✅ Implementado |
| Simulador financeiro (what-if) | ✅ Implementado |
| Importação de CSV de cartão | ✅ Implementado |
| Deploy em produção (Fly.io) | ✅ Implementado |

**Ponto forte único:** O foco em **responsabilidade compartilhada dentro da família** — quem gastou o quê, por categoria, por membro — é algo que nenhum concorrente direto faz com essa profundidade.

---

## 1. Análise de Mercado

### Concorrentes Brasileiros Diretos

| App | Usuários Est. | Pontos Fortes | Pontos Fracos |
|-----|--------------|---------------|---------------|
| **Mobills** | 6M+ | Importação de extratos, categorias automáticas, popularidade | Sem foco em família/múltiplos membros, freemium agressivo |
| **Organizze** | 1M+ | Interface limpa e simples, confiável | Sem household compartilhado real, muito básico |
| **GuiaBolso** | Descontinuado | Integração bancária automática | Descontinuado em 2022 |
| **Minhas Economias** | 500K+ | Gratuito, importação OFX | Interface datada, sem mobile nativo moderno |
| **Wisewallet** | 100K+ | Visual moderno, multi-moeda | Sem household colaborativo |

### Concorrentes Internacionais de Referência

| App | País | O Que Aprender |
|-----|------|----------------|
| **YNAB** (You Need a Budget) | EUA | Metodologia de orçamento zero (envelope budgeting), comunidade forte, educação financeira |
| **Copilot** | EUA | Design premium, sincronização bancária automática, mobile-first |
| **Honeydue** | EUA | Foco em casais, é o único que tenta fazer "household" mas de forma limitada |
| **Toshl** | Eslovênia | Gamificação, UX divertida, multi-moeda |
| **Money Lover** | Vietnam | Popular na Ásia, simples, forte em mobile |

### Nossa Vantagem Competitiva

> **Nenhum app no mercado brasileiro oferece gestão financeira verdadeiramente colaborativa em nível de família/household.**

O PersonalBudget tem a fundação técnica para isso: múltiplos membros, atribuição de gastos por perfil, view de gastos por pessoa. O que falta é **polimento, funcionalidades de produto** e **distribuição**.

---

## 2. Gaps de Produto para ser Referência

### 2.1 Orçamento por Categoria (Budget Caps) 🔴 Alta

**O que é:** Definir um teto mensal de gastos por categoria (ex: "Alimentação: R$2.000/mês") com alerta quando estiver próximo do limite.

**Por que é essencial:** YNAB construiu seu sucesso inteiro nessa ideia. Sem orçamento, o app é apenas um controle — não uma ferramenta de mudança de comportamento.

**O que implementar:**
- Tela de Orçamento: definir valor alvo por categoria por mês
- Indicador visual no Dashboard (barra de progresso por categoria)
- Alerta quando atingir 80% e 100% do orçamento
- Comparativo realizado vs orçado no relatório mensal

---

### 2.2 Score de Saúde Financeira Familiar 🔴 Alta

**O que é:** Um índice mensal (0-100) que resume a saúde financeira da família, com histórico e tendência.

**Componentes do score:**
- Proporção receita/despesa (peso 40%)
- Progresso de metas de poupança (peso 20%)
- Aderência ao orçamento por categoria (peso 20%)
- Saldo positivo em todas as contas (peso 20%)

**Por que é poderoso:** Gamifica o controle financeiro. A família tem um número para acompanhar que representa "como estamos indo". É único no mercado brasileiro.

---

### 2.3 Importação de Extrato Bancário (OFX/CSV) 🔴 Alta

**O que é:** Importar extratos bancários diretamente de arquivos OFX (Open Financial Exchange) — formato padrão dos bancos brasileiros — ou CSV de bancos específicos.

**Status atual:** Importação de CSV de cartão de crédito já existe. Falta:
- Importação OFX para contas bancárias
- Parser para CSV padrão de Itaú, Nubank, Inter, Bradesco (os bancos já estão no enum `Bank`)
- Deduplicação inteligente (evitar lançar transação duplicada)
- Categorização automática por descrição (heurística simples + aprendizado)

**Por que é crucial:** É o maior pain point de usuários de apps financeiros. Digitar manualmente é barreira de abandono.

---

### 2.4 Metas Financeiras Além de Caixinhas 🟡 Média

**O que é:** Metas estruturadas com prazo, valor alvo e estratégia de aportes. Ex:
- "Viagem para Europa: R$15.000 até Dezembro/2027 → aportar R$600/mês"
- "Reserva de emergência: 6 meses de gastos"
- "Troca do carro: R$40.000 até 2028"

**O que temos:** Caixinhas (savings boxes) com meta de valor — mas sem prazo, cálculo de aportes ou progresso percentual.

**O que adicionar:**
- Prazo e valor alvo na criação da meta
- Cálculo automático de aporte mensal necessário
- Progress bar com projeção de conclusão
- Integração com o Simulador existente

---

### 2.5 Relatórios Exportáveis (PDF/CSV) 🟡 Média

**O que implementar:**
- Relatório mensal em PDF: capa com resumo, breakdown por categoria, evolução de saldo
- Export de transações em CSV com filtros aplicados
- "Helper de IR": lista de despesas dedutíveis (saúde, educação) do ano

**Por que importa:** Usuários com contador precisam disso. É um diferencial de mercado para profissionais liberais e empreendedores.

---

### 2.6 Notificações e Lembretes 🟡 Média

**O que implementar (em fases):**
1. **In-app alerts** (já tem infra): vencimento de fatura, meta atingida, recorrência expirando
2. **Push notification** (PWA): avisos no celular mesmo com app fechado
3. **Email semanal**: resumo financeiro da semana (gastos vs orçamento)
4. **WhatsApp** (futuro): integração via Twilio/Z-API para o mercado brasileiro

---

### 2.7 PWA / App Mobile Instalável 🟡 Média

**O que é:** Transformar o app web em Progressive Web App instalável — aparece na tela inicial do celular, funciona offline básico, recebe push notifications.

**Por que PWA em vez de app nativo:**
- Custo de desenvolvimento 10x menor
- Funciona em iOS e Android com uma codebase
- Deploy imediato (sem App Store review)
- A base React já é compatível

**O que adicionar:**
- `manifest.json` com ícones
- Service Worker básico (Workbox via `vite-plugin-pwa`)
- Responsividade mobile (ver [Frontend Discovery](../../PersonalBudgetUI/docs/frontend-discovery.md))
- Bottom navigation bar em mobile

---

### 2.8 Integração com Open Finance Brasil 🟢 Baixa

**O que é:** O Banco Central do Brasil regulamentou o Open Finance (antiga Open Banking) — bancos são obrigados a expor APIs padronizadas para terceiros autorizados.

**Por que é o futuro:**
- Sincronização automática de saldos e transações sem importação manual
- Usuário autoriza acesso diretamente no app do banco
- GuiaBolso foi descontinuado — há espaço no mercado para um novo player

**Desafio:** Requer registro como Iniciador de Transação de Pagamento (ITP) no Bacen. Processo burocrático mas possível.

**Horizonte:** Fase 3 (6-12 meses).

---

### 2.9 Multi-moeda 🟢 Baixa

**O que é:** Suporte a transações em moedas estrangeiras com conversão automática (via API de câmbio) para a moeda base do household.

**Caso de uso:** Famílias brasileiras com membros no exterior, viagens internacionais, investimentos em dólar.

---

### 2.10 Compartilhamento de Relatório 🟢 Baixa

**O que é:** Gerar um link compartilhável de um relatório mensal (somente leitura) para enviar por WhatsApp/email.

**Caso de uso:** Compartilhar o balanço familiar com o cônjuge que não usa o app, ou enviar para o contador.

---

## 3. Roadmap em 3 Fases

### Fase 1 — Foundation (0 a 3 meses)
**Objetivo:** Fechar gaps críticos de produto e segurança para ter uma base sólida.

| Item | Tipo | Prioridade |
|------|------|-----------|
| Segurança: BCrypt, CORS, Rate Limiting, JWT secrets | Backend | 🔴 Crítico |
| Responsividade mobile + PWA instalável | Frontend | 🔴 Alta |
| Busca global de transações | Frontend | 🔴 Alta |
| Exportação CSV de transações | Frontend + Backend | 🔴 Alta |
| Orçamento por categoria (budget caps) | Full Stack | 🔴 Alta |
| Alertas in-app (vencimento fatura, orçamento) | Full Stack | 🟡 Média |
| Toggle dark/light theme | Frontend | 🟢 Quick Win |

**Resultado esperado:** App seguro, usável em mobile, com a principal funcionalidade de orçamento que competidores têm.

---

### Fase 2 — Growth (3 a 6 meses)
**Objetivo:** Funcionalidades que diferenciam o produto no mercado.

| Item | Tipo | Prioridade |
|------|------|-----------|
| Importação OFX de extratos bancários | Backend + Frontend | 🔴 Alta |
| Score de saúde financeira familiar | Full Stack | 🔴 Alta |
| Metas financeiras com prazo e aportes | Full Stack | 🟡 Média |
| Relatório mensal em PDF | Frontend | 🟡 Média |
| Push notifications (PWA) | Frontend | 🟡 Média |
| Background job para recorrências (deadline Set/2027) | Backend | 🟡 Média |
| Simulator side-by-side com dados reais | Frontend | 🟡 Média |

**Resultado esperado:** PersonalBudget com diferenciais únicos no mercado brasileiro. Score de saúde + importação OFX são features que nenhum concorrente direto oferece com essa qualidade.

---

### Fase 3 — Scale (6 a 12 meses)
**Objetivo:** Expandir alcance e preparar para crescimento de usuários.

| Item | Tipo | Prioridade |
|------|------|-----------|
| Open Finance Brasil (integração automática) | Backend | 🟡 Estratégico |
| Helper de IR (despesas dedutíveis) | Full Stack | 🟡 Média |
| Multi-moeda com câmbio automático | Full Stack | 🟢 Baixa |
| Compartilhamento de relatório (link público) | Full Stack | 🟢 Baixa |
| Gamificação (badges, conquistas financeiras) | Frontend | 🟢 Baixa |
| App nativo (React Native ou capacitor.js) | Mobile | 🟢 Baixa |
| MediatR + CQRS no backend | Backend | 🟢 Refactoring |

---

## 4. Métricas de Sucesso

Para acompanhar se estamos na direção certa:

| Métrica | Hoje | Meta Fase 1 | Meta Fase 2 |
|---------|------|------------|------------|
| Usuários ativos mensais | — | 100 | 1.000 |
| Retenção D30 | — | 40% | 55% |
| Transações inseridas/usuário/mês | — | 20 | 40 |
| NPS (Net Promoter Score) | — | 30 | 50 |
| Score médio de saúde financeira | N/A | N/A | > 65 |

---

## 5. Posicionamento de Mercado

### Mensagem Principal

> "O único app de finanças feito para a família inteira — não só para você."

### Diferenciais Únicos

1. **Responsabilidade compartilhada:** veja quem gastou o quê, por categoria, por membro
2. **Score de saúde familiar:** um número que resume se a família está indo bem
3. **Simulador financeiro:** o "e se" da sua vida financeira
4. **Controle total:** faturas, parcelamentos, caixinhas, recorrências — tudo em um lugar

### Público-Alvo Principal

- Casais que precisam gerenciar finanças juntos
- Famílias com filhos que querem transparência financeira
- Profissionais que querem controle além do básico de apps simples

---

*Documento gerado em Setembro 2026.*
