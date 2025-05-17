# 📦 Boleto Barcode Functions

Pequeno conjunto de **Azure Functions** para gerar 🏷️ e validar ✅ códigos de barras de boletos brasileiros.

---

## 🚀 Como rodar localmente

1. **Pré-requisitos**  
   - .NET 8 SDK  
   - Azure Functions Core Tools

Crie um `local.settings.json`:

```json
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "{CONNSTRING}"
  },
}
```

2. **Start**

```bash
func start
```

---
## 👨‍💻 Descrição

- fnGeradorDeBoletos: Gera códigos de barras válidos
- fnValidadorDeBoleto: Valída códigos de barras

---
## Imagens

- Functions integrados com o front
![barcode-generator-validato](https://github.com/user-attachments/assets/f3d486b6-7d84-46de-bd01-76211d594c04)

- Fila do Service Bus na Azure
![azure-service-bus](https://github.com/user-attachments/assets/55dd354e-9647-4aa4-83db-daffa8e41f9f)

---


## ✨ Endpoints

| Método | Rota | Descrição |
| ------ | ---- | --------- |
| `POST` | `/barcode-generate` | 🔄 Gera barcode (44 dígitos) + PNG Base64 e publica em fila Service Bus. |
| `POST` | `/barcode-validate` | ✅ Valida barcode informado e retorna data de vencimento. |

---

## 📋 Exemplos

### 🔄 Gerar

```bash
curl -X POST http://localhost:7071/api/barcode-generate \
-H "Content-Type: application/json" \
-d '{"valor": 100, "dataVencimento": "2025-05-22"}'
```

### ✅ Validar

```bash
curl -X POST http://localhost:7071/api/barcode-validate \
-H "Content-Type: application/json" \
-d '{"barcode": "00120250522100000000000000000000000000000000"}'
```

---

## 🛠️ Stack

- **Azure Functions (isolated)**  
- **BarcodeStandard + SkiaSharp** para gerar PNG  
- **Azure Service Bus** para processamento assíncrono  
