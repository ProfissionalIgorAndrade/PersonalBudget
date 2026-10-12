# PersonalBudget

## Chave JWT (`Jwt:Key`)

A chave que assina os tokens não fica no repositório. Ela precisa ter ao menos 32 bytes; fora de Development a API não inicia sem ela.

**Development:** nada a configurar. Sem chave definida, a API gera uma chave aleatória a cada execução (o banco de desenvolvimento é em memória e também é recriado a cada start). Para fixar uma chave local:

```bash
dotnet user-secrets set "Jwt:Key" "<chave com 32+ caracteres>" --project PersonalBudget.Api
```

**Produção (Fly.io):** definir como secret antes do deploy.

```powershell
fly secrets set Jwt__Key="$([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))" -a personalbudget
```

Trocar a chave invalida os tokens já emitidos, e os usuários precisam fazer login de novo.
